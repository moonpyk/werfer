using JetBrains.Annotations;

namespace SlClockify.ViewModels;

/// <summary>
///     Window shell: holds both pages and the one currently shown.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(TimerViewModel timer, SettingsViewModel settings)
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
                if (e.PropertyName == nameof(Status) && page.Status is { } status)
                {
                    Status = status;
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

    // Shown through the DataTemplates declared in MainWindow.axaml
    public ViewModelBase CurrentPage
    {
        get;
        private set => Set(ref field, value);
    }

    public void Navigate(bool toSettings)
    {
        CurrentPage = toSettings ? Settings : Timer;
    }
}
