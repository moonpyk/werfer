using System.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     Window shell: holds both pages and the one currently shown.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public partial class MainWindowViewModel : ViewModelBase
{
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
        settings.PropertyChanged += OnPagePropertyChanged;

        // A key saved by a previous run connects right away, the timer page fills in once it's done
        // The only message not caused by a user action: without a valid key the app can't do anything
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
    ///     Bound directly by the window (Topmost, ...).
    ///     shortcut: settings don't raise change notifications, values are read once; implement INotifyPropertyChanged
    ///     on ApplicationSettings once the settings page can change them
    /// </summary>
    public IApplicationSettings ApplicationSettings { get; }

    public TimerViewModel Timer { get; }

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    public partial string? Status { get; private set; }

    /// <summary>
    ///     As set in Clockify's profile, shown above the email (<see cref="Status" />) in the More menu
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

    // Drives the progress ring, whichever page is working
    [ObservableProperty]
    public partial bool IsAnyPageBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CurrentPage),
        nameof(ShowRefreshButton)
    )]
    public partial AppPageEnum Page { get; private set; }

    // Shown through the DataTemplates declared in MainWindow.axaml
    public ViewModelBase CurrentPage => Page switch
    {
        AppPageEnum.Timer => Timer,
        AppPageEnum.Settings => Settings,
        _ => throw new ArgumentOutOfRangeException(nameof(Page), Page, null)
    };

    public bool ShowRefreshButton => Page == AppPageEnum.Timer;

    [ObservableProperty]
    public partial string? UserEmail { get; private set; }

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

    // The first day of the week changed: the history's week moves with it
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.WeekStart))
        {
            _ = Timer.LoadHistoryAsync();
        }
    }

    // Drives the progress ring, whichever page is working
    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsBusy):
                IsAnyPageBusy = Timer.IsBusy || Settings.IsBusy;
                break;
        }
    }

    // Best effort: an unreachable or undecodable picture (an SVG one, say) keeps the generic icon
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
            
            // Decoded at twice its 32 px display size, sharp on a 200 % screen
            // a fraction of what the full picture's memory can be
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
