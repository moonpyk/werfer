using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;
using Werfer.Utils;

namespace Werfer.ViewModels;

/// <summary>
///     Settings page: the Clockify API key, checked against Clockify before it is saved.
/// </summary>
[UsedImplicitly]
public partial class SettingsViewModel : ViewModelBase
{
    /// <summary>
    ///     Creates the page, with the saved API key in its key box.
    /// </summary>
    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IApplicationSettings applicationSettings,
        FlashMessageService flashMessages,
        TimeTrackingService timeTracking,
        TimerViewModel timer
    ) : base(logger, flashMessages)
    {
        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;
        Player = timer.Player;
        ApiKey = applicationSettings.ApiKey ?? string.Empty;

        // The default project box can only show a saved project once the projects are loaded
        Player.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TimerPlayerViewModel.ProjectChoices))
            {
                OnPropertyChanged(nameof(DefaultChoice));
            }
        };
    }

    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    // The timer bar, for its project choices
    private TimerPlayerViewModel Player { get; }

    /// <summary>
    ///     The key typed in the settings, only saved once Clockify accepted it, see <see cref="ConnectAsync" />.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasApiKey))]
    public partial string ApiKey { get; set; }

    /// <summary>
    ///     True when a key is typed (blank text doesn't count).
    /// </summary>
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    ///     The choices of the first day of the week.
    /// </summary>
    public DayOfWeek[] DaysOfWeek { get; } = Enum.GetValues<DayOfWeek>();

    /// <summary>
    ///     First day of the history's week, saved as soon as it is picked. The timer page then reloads its history
    /// </summary>
    public DayOfWeek WeekStart
    {
        get => ApplicationSettings.WeekStart;
        set
        {
            if (value == ApplicationSettings.WeekStart)
            {
                return;
            }

            ApplicationSettings.WeekStart = value;
            OnPropertyChanged();
            Save();
        }
    }

    /// <summary>
    ///     Keeps the window above the others. Saved as soon as it changes; the window binds to it, so it applies at once
    /// </summary>
    public bool Topmost
    {
        get => ApplicationSettings.Topmost;
        set
        {
            if (value == ApplicationSettings.Topmost)
            {
                return;
            }

            ApplicationSettings.Topmost = value;
            OnPropertyChanged();
            Save();
        }
    }

    /// <summary>
    ///     A timer started with no project picked uses <see cref="DefaultChoice" />. Saved as soon as it changes.
    /// </summary>
    public bool DefaultChoiceEnabled
    {
        get => ApplicationSettings.DefaultChoiceEnabled;
        set
        {
            if (value == ApplicationSettings.DefaultChoiceEnabled)
            {
                return;
            }

            ApplicationSettings.DefaultChoiceEnabled = value;
            OnPropertyChanged();
            Save();
        }
    }

    /// <summary>
    ///     The default project and task: <see cref="LastUsedChoice" />, or a <see cref="ProjectTaskViewModel" />. Null
    ///     while the saved one isn't among the choices (not loaded yet, archived). Saved as soon as it is picked.
    /// </summary>
    public object? DefaultChoice
    {
        get => ApplicationSettings.DefaultTaskId is null
            ? LastUsedChoice.Instance
            : Player.ProjectChoices?.Find(c =>
                c.Project.Id == ApplicationSettings.DefaultProjectId &&
                c.Task.Id == ApplicationSettings.DefaultTaskId
            );
        set
        {
            // Null while the user is typing a search: the saved default stays
            if (value is null || Equals(value, DefaultChoice))
            {
                return;
            }

            var choice = value as ProjectTaskViewModel;
            ApplicationSettings.DefaultProjectId = choice?.Project.Id;
            ApplicationSettings.DefaultTaskId = choice?.Task.Id;
            OnPropertyChanged();
            Save();
        }
    }

    /// <summary>
    ///     Fills the default project box: the timer bar's matches, after "Last used project and task" when it matches too.
    /// </summary>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>> DefaultChoicesPopulator =>
        async (search, ctk) =>
        [
            .. AutoComplete.ProjectAutoCompleteFilterPredicate(search, LastUsedChoice.Instance)
                ? [LastUsedChoice.Instance]
                : Array.Empty<object>(),
            .. await Player.ProjectChoicesPopulator(search, ctk)
        ];

    /// <summary>
    ///     Asks for confirmation before closing the window. Saved as soon as it changes. The dialog's "Don't ask again"
    ///     also turns it off
    /// </summary>
    public bool ConfirmClose
    {
        get => ApplicationSettings.ConfirmClose;
        set
        {
            if (value == ApplicationSettings.ConfirmClose)
            {
                return;
            }

            ApplicationSettings.ConfirmClose = value;
            OnPropertyChanged();
            Save();
        }
    }

    /// <summary>
    ///     Minimizing hides the window behind a tray icon. Saved as soon as it changes; the tray icon binds to it, so
    ///     it shows or hides at once
    /// </summary>
    public bool MinimizeToTray
    {
        get => ApplicationSettings.MinimizeToTray;
        set
        {
            if (value == ApplicationSettings.MinimizeToTray)
            {
                return;
            }

            ApplicationSettings.MinimizeToTray = value;
            OnPropertyChanged();
            Save();
        }
    }

    /// <summary>
    ///     Shows a badge on the settings item: no key, or the last connection failed.
    /// </summary>
    [ObservableProperty]
    public partial bool NeedsAttention { get; private set; }

    /// <summary>
    ///     Connects with the key and saves it only once Clockify accepted it.
    /// </summary>
    [RelayCommand]
    public Task ConnectAsync()
    {
        return WithBusyIndicator(async () =>
        {
            var previousUserId = TimeTracking.User?.Id;

            try
            {
                await TimeTracking.ConnectAsync(ApiKey);
            }
            catch
            {
                NeedsAttention = true;
                throw;
            }

            NeedsAttention = false;

            ApplicationSettings.ApiKey = ApiKey;

            // Another account: the default project was the previous one's, back to the last used project and task
            if (previousUserId is not null && previousUserId != TimeTracking.User?.Id)
            {
                ApplicationSettings.DefaultProjectId = null;
                ApplicationSettings.DefaultTaskId = null;
                OnPropertyChanged(nameof(DefaultChoice));
            }

            Save();
        });
    }

    private void Save()
    {
        if (!ApplicationSettings.TrySave())
        {
            FlashMessages.Push(WellKnownMessages.SettingsNotSaved);
        }
    }

    partial void OnApiKeyChanged(string value)
    {
        _ = value;
        // Only turned on here, for a blank key: a typed key isn't checked yet. ConnectAsync turns it off once Clockify
        // accepts a key
        if (!HasApiKey)
        {
            NeedsAttention = true;
        }
    }
}
