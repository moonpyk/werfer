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
///     The timer bar: project, task and description of the entry being timed, its start and stop, its elapsed time.
/// </summary>
public partial class TimerPlayerViewModel : ViewModelBase
{
    // Id of the entry last seen running: when it changes, that entry has finished, see EntryFinished
    private string? _runningId;

    // Description of the entry last seen running: the box still showing it means nothing typed is waiting to be sent
    private string? _runningDescription;
    private IDisposable? _ticker;
    
    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }
    
    /// <summary>
    ///     Creates the player, following the running entry from then on.
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
        Elapsed = "00:00:00";

        // The running entry may change from another screen, the bar follows it
        TimeTracking.RunningEntryChanged += (_, _) => OnRunningEntryChanged();
    }

    /// <summary>
    ///     Raised when the entry last seen running has finished, stopped here or elsewhere: it now belongs to the history.
    /// </summary>
    public event EventHandler? EntryFinished;
    
    /// <summary>
    ///     Null until connected, the timer panel is disabled meanwhile
    /// </summary>
    [ObservableProperty]
    public partial List<ProjectTaskViewModel>? ProjectChoices { get; private set; }

    /// <summary>
    ///     Feeds the project box with at most 10 matches, the full list is too long to scroll
    /// </summary>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>> ProjectChoicesPopulator =>
        (search, _) => Task.FromResult<IEnumerable<object>>(
        [
            .. (ProjectChoices ?? [])
            .Where(c => AutoComplete.ProjectAutoCompleteFilterPredicate(search, c))
            .Take(ApplicationSettings.MaxProjectChoices)
        ]);

    /// <summary>
    ///     Pushed to the running entry when changed
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBrush))]
    public partial ProjectTaskViewModel? SelectedChoice { get; set; }

    /// <summary>
    ///     Flattened: a binding to SelectedChoice.Brush logs a warning whenever nothing is selected
    /// </summary>
    public IBrush? SelectedBrush => SelectedChoice?.Brush;

    /// <summary>
    ///     Text of the project box: a null SelectedItem doesn't clear what was typed, ResetAsync empties it here
    /// </summary>
    [ObservableProperty]
    public partial string? ProjectSearchText { get; set; }

    /// <summary>
    ///     Free text, usually a Jira ticket key. Follows the typing, but is only pushed to the running entry at given
    ///     moments, see <see cref="PushDescriptionAsync" />: each key sent to Clockify would be too many updates
    /// </summary>
    [ObservableProperty]
    public partial string? Description { get; set; }

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
    ///     The running entry's time, zero when stopped; ticks every second, the week's total counts it
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan RunningElapsed { get; private set; }

    private bool CanToggle => !IsBusy;

    /// <summary>
    ///     Loads the (project, task) pairs an entry can be started on, then shows the running entry with them.
    /// </summary>
    public async Task LoadProjectChoicesAsync()
    {
        ProjectChoices = [.. (await TimeTracking.GetProjectTasksAsync())
            .Select(pt => new ProjectTaskViewModel(pt))
            // Projects sharing a color end up next to each other in the list
            .OrderBy(c => c.Project.Color, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(c => c.ToString(), StringComparer.CurrentCultureIgnoreCase)];

        // Choices weren't loaded yet when ConnectAsync raised the change
        OnRunningEntryChanged();
    }

    /// <summary>
    ///     Starts a time tracking entry for the selected project and task while displaying the busy indicator.
    /// </summary>
    [RelayCommand]
    private Task StartAsync()
    {
        return WithBusyIndicator(async () =>
        {
            if (SelectedChoice is null)
            {
                FlashMessages.Push(WellKnownMessages.PickProjectFirst);
                return;
            }

            await TimeTracking.StartAsync(
                SelectedChoice.Project.Id,
                SelectedChoice.Task.Id,
                Description
            );
        });
    }

    /// <summary>
    ///     Stops the currently running time tracking entry while displaying the busy indicator.
    /// </summary>
    [RelayCommand]
    private Task StopAsync()
    {
        // One busy span, no flicker in between. A description still being typed goes first: once stopped, the entry
        // can't take it anymore. Its own failure is reported by its own WithBusyIndicator, the stop still happens
        return WithBusyIndicator(async () =>
        {
            await PushDescriptionAsync();
            await TimeTracking.StopAsync();
        });
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
    ///     Back to a blank, stopped timer: stops the running entry, then clears the description and the task.
    /// </summary>
    [RelayCommand]
    private async Task ResetAsync()
    {
        if (IsRunning)
        {
            await StopAsync();
        }

        // The stop failed (already reported): clearing now would push blank fields to the still running entry
        if (IsRunning)
        {
            return;
        }

        Description = null;
        SelectedChoice = null;
        ProjectSearchText = string.Empty;
    }

    /// <summary>
    ///     Clears the description, also on the running entry.
    /// </summary>
    [RelayCommand]
    private Task ClearDescriptionAsync()
    {
        Description = null;
        return PushDescriptionAsync();
    }

    /// <summary>
    ///     Starts a new entry on a project, task and description, stopping the running one first.
    /// </summary>
    public async Task ContinueAsync(ProjectTaskViewModel choice, string? description)
    {
        // Like the start button: a second click while the first start is pending would start a second entry
        if (IsBusy)
        {
            return;
        }

        if (IsRunning)
        {
            await StopAsync();
        }

        // Same as ResetAsync: changing the fields of a still running entry would push them to it
        if (IsRunning)
        {
            return;
        }

        SelectedChoice = choice;
        Description = description;
        await StartAsync();
    }

    /// <summary>
    ///     Pushes the description to the running entry, if it differs: once typed (on lost focus, see TimerBarView),
    ///     and before an operation that would lose it.
    /// </summary>
    public Task PushDescriptionAsync()
    {
        return PushToRunningEntry(description: true);
    }

    // Each call only sends its own field, the others come from the running entry, so the order in which
    // OnRunningEntryChanged refreshes them can't send a half-updated state
    private Task PushToRunningEntry(
        ProjectTaskViewModel? choice = null,
        bool description = false
    )
    {
        if (TimeTracking.RunningEntry is not { } running)
        {
            return Task.CompletedTask;
        }

        var projectId = choice?.Project.Id ?? running.ProjectId;
        var taskId = choice?.Task.Id ?? running.TaskId;
        var text = description ? Description : running.Description;

        // Skips values coming from the running entry itself, nothing to send
        if (
            projectId is null ||
            taskId is null ||
            (projectId == running.ProjectId && taskId == running.TaskId && text == running.Description))
        {
            return Task.CompletedTask;
        }

        return WithBusyIndicator(() => TimeTracking.UpdateRunningEntryAsync(
            projectId,
            taskId,
            text
        ));
    }

    private void OnRunningEntryChanged()
    {
        _ticker?.Dispose();
        _ticker = null;

        var running = TimeTracking.RunningEntry;

        IsRunning = running is not null;

        var isNewEntry = _runningId != running?.Id;

        if (_runningId is not null && isNewEntry)
        {
            EntryFinished?.Invoke(this, EventArgs.Empty);
        }

        _runningId = running?.Id;

        // A reply arriving while the user types (an update sent by a project pick) mustn't wipe the typed text
        var hasUnsentText = (Description?.Trim() ?? string.Empty) != (_runningDescription ?? string.Empty);
        _runningDescription = running?.Description;

        if (running is null)
        {
            Elapsed = "00:00:00";
            RunningElapsed = TimeSpan.Zero;
            return;
        }

        SelectedChoice = ProjectChoices?.Find(c => c.Task.Id == running.TaskId) ?? SelectedChoice;
        if (isNewEntry || !hasUnsentText)
        {
            Description = running.Description;
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
            // Not TimeSpan's "hh", which drops whole days: a timer forgotten over a weekend would look short
            Elapsed = TimeEntryHistory.Format(RunningElapsed);
        }
    }

    // Null when the box is cleared, the running task is kept
    partial void OnSelectedChoiceChanged(ProjectTaskViewModel? value)
    {
        _ = PushToRunningEntry(value);
    }

    // IsBusy lives in the base class, out of [NotifyCanExecuteChangedFor]'s reach
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsBusy))
        {
            ToggleCommand.NotifyCanExecuteChanged();
        }
    }
}
