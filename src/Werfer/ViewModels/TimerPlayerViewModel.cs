using System.ComponentModel;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Werfer.Services;
using Werfer.Utils;

namespace Werfer.ViewModels;

/// <summary>
///     The timer bar: the project, task and description, the start and stop buttons, and the elapsed time.
///     The fields are stored in a <see cref="TimerDraft" />, which decides what is sent to Clockify and when.
/// </summary>
public partial class TimerPlayerViewModel : ViewModelBase
{
    // Id of the running entry we last saw. When it changes, that entry has finished: see EntryFinished
    private string? _runningId;
    private IDisposable? _ticker;

    /// <summary>
    ///     Creates the timer bar. It follows the running entry from then on.
    /// </summary>
    public TimerPlayerViewModel(
        ILogger logger,
        FlashMessageService flashMessages,
        IApplicationSettings applicationSettings,
        TimeTrackingService timeTracking
    ) : base(logger, flashMessages)
    {
        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;
        Draft = new TimerDraft(timeTracking);
        Elapsed = "00:00:00";

        Draft.Changed += (_, _) => OnDraftChanged();
        // The running entry can also change from another screen: the bar updates with it
        TimeTracking.RunningEntryChanged += (_, _) => OnRunningEntryChanged();
    }

    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    /// <summary>
    ///     Stores the bar's fields, see <see cref="TimerDraft" />.
    /// </summary>
    public TimerDraft Draft { get; }

    /// <summary>
    ///     Every project and task the user can pick. Null until connected; the timer bar is disabled until then.
    /// </summary>
    [ObservableProperty]
    public partial List<ProjectTaskViewModel>? ProjectChoices { get; private set; }

    /// <summary>
    ///     Fills the project box with the matches of what is typed. Limited by the MaxProjectChoices setting, because the
    ///     full list is too long to scroll.
    /// </summary>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>> ProjectChoicesPopulator =>
        (search, _) => Task.FromResult<IEnumerable<object>>(
        [
            .. (ProjectChoices ?? [])
            .Where(c => AutoComplete.ProjectAutoCompleteFilterPredicate(search, c))
            .Take(ApplicationSettings.MaxProjectChoices)
        ]);

    /// <summary>
    ///     The project and task picked. Only the project box sets it, so each pick is sent to the running entry at once.
    /// </summary>
    public ProjectTaskViewModel? SelectedChoice
    {
        get => Draft.Choice as ProjectTaskViewModel;
        set
        {
            if (Equals(Draft.Choice, value))
            {
                return;
            }

            Draft.Choice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedBrush));
            _ = SaveDraftAsync();
        }
    }

    /// <summary>
    ///     The picked project's color. A separate property because binding SelectedChoice.Brush logs a warning when
    ///     nothing is picked.
    /// </summary>
    public IBrush? SelectedBrush => SelectedChoice?.Brush;

    /// <summary>
    ///     The text typed in the project box. Clearing the pick does not clear this text, so OnDraftChanged clears it.
    /// </summary>
    [ObservableProperty]
    public partial string? ProjectSearchText { get; set; }

    /// <summary>
    ///     The description. It changes with each key typed, but is sent to Clockify only when the box loses focus (see
    ///     TimerBarView) or before stop, refresh and reset. Sending each key would make too many requests.
    /// </summary>
    public string? Description
    {
        get => Draft.Description;
        set
        {
            if (Draft.Description == value)
            {
                return;
            }

            Draft.Description = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Indicates whether a time tracking entry is currently running.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunning { get; private set; }

    /// <summary>
    ///     Formatted elapsed time for the current running entry, or 00:00:00 when the timer is stopped.
    /// </summary>
    [ObservableProperty]
    public partial string Elapsed { get; private set; }

    /// <summary>
    ///     Time spent on the running entry, zero when stopped. Updated every second; the week's total adds it.
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan RunningElapsed { get; private set; }

    private bool CanToggle => !IsBusy;

    /// <summary>
    ///     Raised when the running entry has finished, stopped here or in another app. The history then reloads to show it.
    /// </summary>
    public event EventHandler? EntryFinished;

    /// <summary>
    ///     Loads every project and task the user can pick. The draft then finds the running entry's project and task in it.
    /// </summary>
    public async Task LoadProjectChoicesAsync()
    {
        ProjectChoices =
        [
            .. (await TimeTracking.GetProjectTasksAsync())
            .Select(pt => new ProjectTaskViewModel(pt))
            // Projects sharing a color end up next to each other in the list
            .OrderBy(c => c.Project.Color, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(c => c.ToString(), StringComparer.CurrentCultureIgnoreCase)
        ];

        Draft.Choices = ProjectChoices;
    }

    /// <summary>
    ///     Sends the fields to the running entry, with the busy indicator. See <see cref="TimerDraft.SaveAsync" />.
    /// </summary>
    public Task SaveDraftAsync()
    {
        return WithBusyIndicator(() => Draft.SaveAsync());
    }

    /// <summary>
    ///     Starts a time tracking entry for the selected project and task while displaying the busy indicator.
    /// </summary>
    [RelayCommand]
    private Task StartAsync()
    {
        return WithBusyIndicator(async () =>
        {
            if (!await Draft.StartAsync())
            {
                FlashMessages.Push(WellKnownMessages.PickProjectFirst);
            }
        });
    }

    /// <summary>
    ///     Stops the currently running time tracking entry while displaying the busy indicator.
    /// </summary>
    [RelayCommand]
    private Task StopAsync()
    {
        return WithBusyIndicator(() => Draft.StopAsync());
    }

    /// <summary>
    ///     Starts the timer when it is stopped, or stops the currently running timer when it is active.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggle))]
    private Task ToggleAsync()
    {
        return IsRunning
            ? StopAsync()
            : StartAsync();
    }

    /// <summary>
    ///     Stops the running entry, then empties the description and the project.
    /// </summary>
    [RelayCommand]
    private Task ResetAsync()
    {
        return WithBusyIndicator(() => Draft.ResetAsync());
    }

    /// <summary>
    ///     Empties the description, and sends the empty description to the running entry.
    /// </summary>
    [RelayCommand]
    private Task ClearDescriptionAsync()
    {
        Description = null;
        return SaveDraftAsync();
    }

    /// <summary>
    ///     Stops the running entry, if any, then starts a new one with the given project, task and description.
    /// </summary>
    public Task ContinueAsync(ProjectTaskViewModel choice, string? description)
    {
        // Ignored while busy, like the start button: a second click during a start would start a second entry
        return IsBusy
            ? Task.CompletedTask
            : WithBusyIndicator(() => Draft.ContinueAsync(choice, description));
    }

    // The draft changed its values by itself (entry loaded, cleared): tell the UI to show the new values
    private void OnDraftChanged()
    {
        OnPropertyChanged(nameof(SelectedChoice));
        OnPropertyChanged(nameof(SelectedBrush));
        OnPropertyChanged(nameof(Description));

        if (SelectedChoice is null)
        {
            ProjectSearchText = string.Empty;
        }
    }

    private void OnRunningEntryChanged()
    {
        _ticker?.Dispose();
        _ticker = null;

        var running = TimeTracking.RunningEntry;

        IsRunning = running is not null;

        if (_runningId is not null && _runningId != running?.Id)
        {
            EntryFinished?.Invoke(this, EventArgs.Empty);
        }

        _runningId = running?.Id;

        if (running is null)
        {
            Elapsed = "00:00:00";
            RunningElapsed = TimeSpan.Zero;
            return;
        }

        var start = running.TimeInterval?.Start ?? DateTimeOffset.UtcNow;

        UpdateElapsed();

        _ticker = DispatcherTimer.Run(() =>
        {
            UpdateElapsed();
            return true;
        }, TimeSpan.FromSeconds(1));
        return;

        void UpdateElapsed()
        {
            RunningElapsed = DateTimeOffset.UtcNow - start;
            // Not TimeSpan's "hh" format: it drops whole days, so a timer left running over a weekend would look short
            Elapsed = TimeEntryHistory.Format(RunningElapsed);
        }
    }

    // IsBusy is declared in the base class, where [NotifyCanExecuteChangedFor] can't be used: refresh the toggle here
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsBusy))
        {
            ToggleCommand.NotifyCanExecuteChanged();
        }
    }
}
