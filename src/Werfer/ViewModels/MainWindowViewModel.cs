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
        SettingsViewModel settings
    ) : base(logger, flashMessages)
    {
        timeTracking.Connected += (_, _) =>
        {
            // The status bar shows who is connected
            Status = timeTracking.User?.Email;
            FlashMessages.Clear();
        };

        ApplicationSettings = applicationSettings;
        Timer = timer;
        Settings = settings;
        CurrentPage = timer;

        settings.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.WeekStart))
            {
                _ = timer.LoadHistoryAsync();
            }
        };

        foreach (var page in new ViewModelBase[] { timer, settings })
        {
            page.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IsBusy))
                {
                    IsAnyPageBusy = timer.IsBusy || settings.IsBusy;
                }
            };
        }

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

    // Drives the progress ring, whichever page is working
    [ObservableProperty]
    public partial bool IsAnyPageBusy { get; private set; }

    // Shown through the DataTemplates declared in MainWindow.axaml
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRefreshButton))]
    public partial ViewModelBase CurrentPage { get; private set; }

    public bool ShowRefreshButton => CurrentPage is TimerViewModel;

    public void Navigate(bool toSettings)
    {
        CurrentPage = toSettings ? Settings : Timer;
    }
}
