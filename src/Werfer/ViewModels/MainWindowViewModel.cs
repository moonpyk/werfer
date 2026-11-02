using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Werfer.ViewModels;

/// <summary>
///     Window shell: holds both pages and the one currently shown.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        TimerViewModel timer,
        SettingsViewModel settings
    ) : base(logger)
    {
        Timer = timer;
        Settings = settings;
        CurrentPage = timer;

        // The status bar shows the latest message from any page, a page clearing its own doesn't blank it
        Status = timer.Status;

        foreach (var page in new ViewModelBase[] { timer, settings })
        {
            page.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(Status) when page.Status is { } status:
                        Status = status;
                        break;
                    case nameof(IsBusy):
                        IsAnyPageBusy = timer.IsBusy || settings.IsBusy;
                        break;
                }
            };
        }

        // A key saved by a previous run connects right away, the timer page fills in once it's done
        if (settings.HasApiKey)
        {
            _ = settings.ConnectAsync();
        }
    }

    public TimerViewModel Timer { get; }

    public SettingsViewModel Settings { get; }

    // Drives the status bar's progress ring, whichever page is working
    public bool IsAnyPageBusy
    {
        get;
        private set => Set(ref field, value);
    }

    // Shown through the DataTemplates declared in MainWindow.axaml
    public ViewModelBase CurrentPage
    {
        get;
        private set
        {
            Set(ref field, value);
            NotifyPropertyChanged(nameof(ShowRefreshButton));
        }
    }

    public bool ShowRefreshButton => CurrentPage is TimerViewModel;

    public void Navigate(bool toSettings)
    {
        CurrentPage = toSettings ? Settings : Timer;
    }
}
