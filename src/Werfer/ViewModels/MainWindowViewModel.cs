using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     The main window: holds both pages and knows which one is shown.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    ///     Creates the window's view model with its two pages, and connects right away if an API key was saved before.
    /// </summary>
    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        FlashMessageService flashMessages,
        IApplicationSettings applicationSettings,
        TimeTrackingService timeTracking,
        TimerViewModel timer,
        SettingsViewModel settings,
        IHttpClientFactory httpClientFactory
    ) : base(logger, flashMessages)
    {
        HttpClientFactory = httpClientFactory;
        TimeTracking = timeTracking;
        ApplicationSettings = applicationSettings;
        Timer = timer;
        Settings = settings;

        timeTracking.Connected += OnConnected;
        settings.PropertyChanged += OnSettingsPropertyChanged;

        timer.PropertyChanged += OnPagePropertyChanged;
        timer.Player.PropertyChanged += OnPagePropertyChanged;
        timer.History.PropertyChanged += OnPagePropertyChanged;
        settings.PropertyChanged += OnPagePropertyChanged;

        // With an API key saved before, connect right away; the timer page fills in once connected
        // This is the only message not caused by a user action: without a valid key the app can't do anything
        if (settings.HasApiKey)
        {
            _ = settings.ConnectAsync();
        }
        else
        {
            FlashMessages.ReplaceWith(WellKnownMessages.NoApiKey);
        }
    }

    /// <summary>
    ///     Read by the window for its size, position and close confirmation. It raises no change notifications: a
    ///     setting the window must follow live goes through <see cref="SettingsViewModel" />, like Topmost
    /// </summary>
    public IApplicationSettings ApplicationSettings { get; }

    /// <summary>
    ///     The main page, see <see cref="TimerViewModel" />.
    /// </summary>
    public TimerViewModel Timer { get; }

    /// <summary>
    ///     The settings page, opened from the More menu.
    /// </summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    ///     A status message, shown in the window's status bar.
    /// </summary>
    [ObservableProperty]
    public partial string? Status { get; private set; }

    /// <summary>
    ///     The user's name, as set in their Clockify profile. Shown above the email in the More menu
    /// </summary>
    [ObservableProperty]
    public partial string? UserName { get; private set; }

    /// <summary>
    ///     The Clockify profile picture, null until loaded or when there is none: the More menu shows a generic icon
    /// </summary>
    [ObservableProperty]
    public partial Bitmap? Avatar { get; private set; }

    private IHttpClientFactory HttpClientFactory { get; }

    private TimeTrackingService TimeTracking { get; }

    /// <summary>
    ///     True while any page, or part of a page, is busy: shows the progress ring in the status bar.
    /// </summary>
    [ObservableProperty]
    public partial bool IsAnyPageBusy { get; private set; }

    /// <summary>
    ///     The page shown, see <see cref="CurrentPage" />.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CurrentPage),
        nameof(ShowRefreshButton),
        nameof(ClearDescriptionCommand),
        nameof(ResetCommand)
    )]
    public partial AppPageEnum Page { get; private set; }

    /// <summary>
    ///     The view model of the page shown, mapped to its view by the DataTemplates declared in MainWindow.axaml.
    /// </summary>
    public ViewModelBase CurrentPage => Page switch
    {
        AppPageEnum.Timer => Timer,
        AppPageEnum.Settings => Settings,
        _ => throw new ArgumentOutOfRangeException(nameof(Page), Page, null)
    };

    /// <summary>
    ///     Whether the navigation bar shows its Refresh item: only on the timer page, which it refreshes.
    /// </summary>
    public bool ShowRefreshButton => Page == AppPageEnum.Timer;

    /// <summary>
    ///     Ctrl+L, see the window's KeyBindings: the timer page's ClearDescription. Null while another page is shown, so
    ///     the shortcut only works when the field it clears is on screen.
    /// </summary>
    public ICommand? ClearDescriptionCommand => Page == AppPageEnum.Timer ? Timer.ClearDescriptionCommand : null;

    /// <summary>
    ///     Ctrl+N, see the window's KeyBindings: the timer page's Reset, null while another page is shown.
    /// </summary>
    public ICommand? ResetCommand => Page == AppPageEnum.Timer ? Timer.ResetCommand : null;

    /// <summary>
    ///     The email of the connected user, shown below their name in the More menu.
    /// </summary>
    [ObservableProperty]
    public partial string? UserEmail { get; private set; }

    /// <summary>
    ///     Shows a page. Also a command, for the keyboard shortcuts (Ctrl+, for the settings).
    /// </summary>
    [RelayCommand]
    public void Navigate(AppPageEnum page)
    {
        Page = page;
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        FlashMessages.Clear();

        UserName = TimeTracking.User?.Name;
        UserEmail = TimeTracking.User?.Email;
        _ = LoadAvatarAsync(TimeTracking.User?.ProfilePicture);
    }

    // The first day of the week changed: reload the history for the new week
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.WeekStart))
        {
            _ = Timer.History.LoadAsync();
        }
    }

    // Shows the progress ring when any page is busy, and passes on the timer page's shortcut changes
    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsBusy):
                IsAnyPageBusy = Timer.IsBusy || Timer.Player.IsBusy || Timer.History.IsBusy || Settings.IsBusy;
                break;

            // The timer page's shortcuts change when its edit sheet opens or closes, see TimerViewModel
            case nameof(TimerViewModel.ClearDescriptionCommand):
            case nameof(TimerViewModel.ResetCommand):
                OnPropertyChanged(e.PropertyName);
                break;
        }
    }

    // Best effort: if the picture can't be downloaded or decoded (an SVG, for example), the generic icon stays
    private async Task LoadAvatarAsync(string? url)
    {
        var prev = Avatar;

        Avatar = null;

        prev?.Dispose();

        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            using var client = HttpClientFactory.CreateClient();
            await using var strm = await client.GetStreamAsync(url);

            var ms = new MemoryStream();
            await strm.CopyToAsync(ms);
            ms.Position = 0;

            // Decoded at 64 px, twice its 32 px display size: sharp on a 200 % screen, and much less memory than
            // the full-size picture
            Avatar = Bitmap.DecodeToWidth(
                ms,
                64
            );
        }
        catch (Exception e)
        {
            Logger.LogWarning(
                e,
                "Could not load the profile picture {Url}",
                url
            );
        }
    }
}
