using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;
using Werfer.Utils;

namespace Werfer.ViewModels;

/// <summary>
///     Main page: pick a project and task, describe the work, start and stop the timer.
/// </summary>
[UsedImplicitly]
public partial class TimerViewModel : ViewModelBase
{
    // Day the history was last loaded for
    private DateOnly _historyDate;

    // Id of the entry last seen running: when it changes, that entry has finished and joins the history
    private string? _runningId;
    private IDisposable? _ticker;
    private TimeSpan _weekFinished;

    public TimerViewModel(
        IApplicationSettings applicationSettings,
        ILogger<TimerViewModel> logger,
        FlashMessageService flashMessages,
        TimeTrackingService timeTracking
    ) : base(logger, flashMessages)
    {
        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;
        Elapsed = "00:00:00";
        WeekTotal = "00:00:00";

        // Connection happens from the settings page, projects are loaded here once it succeeds, then the history
        // which needs their names
        TimeTracking.Connected += (_, _) => _ = WithBusyIndicator(async () =>
        {
            await LoadProjectChoicesAsync();
            await LoadHistoryCoreAsync();
        });

        // The running entry may change from another screen, the timer display follows it
        TimeTracking.RunningEntryChanged += (_, _) => OnRunningEntryChanged();

        // "Today" and the week move at midnight, the app often stays open across it
        DispatcherTimer.Run(() =>
        {
            if (History is not null && DateOnly.FromDateTime(DateTime.Now) != _historyDate)
            {
                _ = LoadHistoryAsync();
            }

            return true;
        }, TimeSpan.FromMinutes(1));
    }

    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    /// <summary>
    ///     Null until connected, the timer panel is hidden meanwhile
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
    public partial ProjectTaskViewModel? SelectedChoice { get; set; }

    /// <summary>
    ///     Text of the project box: a null SelectedItem doesn't clear what was typed, ResetAsync empties it here
    /// </summary>
    [ObservableProperty]
    public partial string? ProjectSearchText { get; set; }

    /// <summary>
    ///     Free text, usually a Jira ticket key, pushed to the running entry when edited (on lost focus)
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
    ///     Finished entries of the current week by day, null until loaded
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryEmpty))]
    public partial List<TimeEntryDayViewModel>? History { get; private set; }

    /// <summary>
    ///     Nothing to list yet: not connected, loading or an empty week; the view shows an icon instead
    /// </summary>
    public bool IsHistoryEmpty => History is not { Count: > 0 };

    /// <summary>
    ///     Finished entries of the week plus the running one, ticks with it
    /// </summary>
    [ObservableProperty]
    public partial string WeekTotal { get; private set; }

    /// <summary>
    ///     Starts a time tracking entry for the selected project and task while displaying the busy indicator.
    /// </summary>
    [RelayCommand]
    public Task StartAsync()
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
    public Task StopAsync()
    {
        return WithBusyIndicator(() => TimeTracking.StopAsync());
    }

    /// <summary>
    ///     Starts the timer when it is stopped, or stops the currently running timer when it is active.
    /// </summary>
    public Task ToggleAsync()
    {
        return IsRunning
            ? StopAsync()
            : StartAsync();
    }

    /// <summary>
    ///     Refreshes the currently running time tracking entry while displaying the busy indicator.
    /// </summary>
    /// <return>
    ///     A task that represents the asynchronous refresh operation.
    /// </return>
    public Task RefreshAsync()
    {
        return WithBusyIndicator(async () =>
        {
            await TimeTracking.RefreshRunningEntryAsync();
            // Entries may also have been added or edited elsewhere
            await LoadHistoryCoreAsync();
        });
    }

    /// <summary>
    ///     Reloads the week's history, e.g. once the first day of the week changed. Does nothing until connected.
    /// </summary>
    public Task LoadHistoryAsync()
    {
        return WithBusyIndicator(LoadHistoryCoreAsync);
    }

    /// <summary>
    ///     Deletes history entries, then reloads the history, also after a partial failure, to show what's left.
    /// </summary>
    public Task DeleteAsync(IReadOnlyList<string> ids)
    {
        return WithBusyIndicator(async () =>
        {
            try
            {
                await TimeTracking.DeleteTimeEntriesAsync(ids);
            }
            finally
            {
                await LoadHistoryCoreAsync();
            }
        });
    }

    /// <summary>
    ///     Starts a new entry on a history line's project, task and description, stopping the running one first.
    /// </summary>
    public async Task ContinueAsync(TimeEntryGroupViewModel group)
    {
        // Like the start button: a second click while the first start is pending would start a second entry
        if (group.Choice is null || IsBusy)
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

        SelectedChoice = group.Choice;
        Description = group.Description;
        await StartAsync();
    }

    /// <summary>
    ///     Back to a blank, stopped timer: stops the running entry, then clears the description and the task.
    /// </summary>
    public async Task ResetAsync()
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

    // Each setter only sends its own field, the others come from the running entry, so the order in which
    // OnRunningEntryChanged refreshes them can't send a half-updated state
    private void PushToRunningEntry(
        ProjectTaskViewModel? choice = null,
        bool description = false
    )
    {
        if (TimeTracking.RunningEntry is not { } running)
        {
            return;
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
            return;
        }

        WithBusyIndicator(() => TimeTracking.UpdateRunningEntryAsync(
            projectId,
            taskId,
            text
        )).ContinueWith(t => t.Exception?.Handle(_ => true));
    }

    private async Task LoadProjectChoicesAsync()
    {
        ProjectChoices = (await TimeTracking.GetProjectTasksAsync())
            .Select(pt => new ProjectTaskViewModel(pt))
            // Projects sharing a color end up next to each other in the list
            .OrderBy(c => c.Project.Color)
            .ThenBy(c => c.ToString())
            .ToList();

        // Choices weren't loaded yet when ConnectAsync raised the change
        OnRunningEntryChanged();
    }

    private async Task LoadHistoryCoreAsync()
    {
        if (ProjectChoices is null)
        {
            return;
        }

        var zone = TimeZoneInfo.Local;
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.Date);

        // Set before the request: a failed load is reported once, not retried every minute by the date check
        _historyDate = today;

        var entries = await TimeTracking.GetTimeEntriesSinceAsync(
            TimeEntryHistory.StartOfWeek(now, ApplicationSettings.WeekStart, zone)
        );

        History =
        [
            .. entries
                .GroupBy(zone)
                .Select(d => new TimeEntryDayViewModel(d, today, ProjectChoices, this))
        ];

        _weekFinished = entries.Sum();
        UpdateWeekTotal();
    }

    private void UpdateWeekTotal(TimeSpan running = default)
    {
        WeekTotal = TimeEntryHistory.Format(_weekFinished + running);
    }

    private void OnRunningEntryChanged()
    {
        _ticker?.Dispose();
        _ticker = null;

        var running = TimeTracking.RunningEntry;

        IsRunning = running is not null;

        // The entry last seen running has finished (stopped here or elsewhere), it now belongs to the history
        if (_runningId is not null && _runningId != running?.Id)
        {
            _ = LoadHistoryAsync();
        }

        _runningId = running?.Id;

        if (running is null)
        {
            Elapsed = "00:00:00";
            UpdateWeekTotal();
            return;
        }

        SelectedChoice = ProjectChoices?.Find(c => c.Task.Id == running.TaskId
        ) ?? SelectedChoice;
        Description = running.Description;

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
            var elapsed = DateTimeOffset.UtcNow - start;
            // Not TimeSpan's "hh", which drops whole days: a timer forgotten over a weekend would look short
            Elapsed = TimeEntryHistory.Format(elapsed);
            // shortcut: an entry started before the week counts whole, split it at the week start if that ever matters
            UpdateWeekTotal(elapsed);
        }
    }

    // Null when the box is cleared, the running task is kept
    partial void OnSelectedChoiceChanged(ProjectTaskViewModel? value)
    {
        PushToRunningEntry(value);
    }

    partial void OnDescriptionChanged(string? value)
    {
        PushToRunningEntry(description: true);
    }
}
