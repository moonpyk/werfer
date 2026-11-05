using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;
using Werfer.Utils;

namespace Werfer.ViewModels;

/// <summary>
///     Main page: pick a project and task, describe the work, start and stop the timer.
/// </summary>
[UsedImplicitly]
public class TimerViewModel : ViewModelBase
{
    private IDisposable? _ticker;

    public TimerViewModel(
        IApplicationSettings applicationSettings,
        ILogger<TimerViewModel> logger,
        FlashMessageService flashMessages,
        TimeTrackingService timeTracking
    ) : base(logger, flashMessages)
    {
        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;

        // Connection happens from the settings page, projects are loaded here once it succeeds
        TimeTracking.Connected += (_, _) => _ = WithBusyIndicator(LoadProjectChoicesAsync);

        // The running entry may change from another screen, the timer display follows it
        TimeTracking.RunningEntryChanged += (_, _) => OnRunningEntryChanged();
    }

    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    /// <summary>
    ///     Null until connected, the timer panel is hidden meanwhile
    /// </summary>
    public List<ProjectTaskViewModel>? ProjectChoices
    {
        get;
        private set => Set(ref field, value);
    }

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
    public ProjectTaskViewModel? SelectedChoice
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            Set(ref field, value);
            // Null when the box is cleared, the running task is kept
            PushToRunningEntry(value);
        }
    }

    /// <summary>
    ///     Text of the project box: a null SelectedItem doesn't clear what was typed, ResetAsync empties it here
    /// </summary>
    public string? ProjectSearchText
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Free text, usually a Jira ticket key, pushed to the running entry when edited (on lost focus)
    /// </summary>
    public string? Description
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            Set(ref field, value);
            PushToRunningEntry(description: true);
        }
    }

    /// <summary>
    ///     Indicates whether a time tracking entry is currently running.
    /// </summary>
    public bool IsRunning
    {
        get;
        private set => Set(ref field, value);
    }

    /// <summary>
    ///     Formatted elapsed time for the current running entry, or 00:00:00 when the timer is stopped.
    /// </summary>
    public string Elapsed
    {
        get;
        private set => Set(ref field, value);
    } = "00:00:00";

    /// <summary>
    ///     Starts a time tracking entry for the selected project and task while displaying the busy indicator.
    /// </summary>
    public Task StartAsync()
    {
        return WithBusyIndicator(async () =>
        {
            if (SelectedChoice is null)
            {
                FlashMessages.Push(new FlashMessage(
                    "Pick a project and a task first",
                    Severity: FAInfoBarSeverity.Warning
                ));
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
        return WithBusyIndicator(() => TimeTracking.RefreshRunningEntryAsync());
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

        // Skips values coming from the running entry itself, a nested RunAsync would reset IsBusy mid-operation
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

    private void OnRunningEntryChanged()
    {
        _ticker?.Dispose();
        _ticker = null;

        var running = TimeTracking.RunningEntry;

        IsRunning = running is not null;

        if (running is null)
        {
            Elapsed = "00:00:00";
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
            Elapsed = (DateTimeOffset.UtcNow - start).ToString(@"hh\:mm\:ss");
        }
    }
}
