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

    // Null until connected, the timer panel is hidden meanwhile
    public List<ProjectTaskViewModel>? ProjectChoices
    {
        get;
        private set => Set(ref field, value);
    }

    // Feeds the project box with at most 10 matches, the full list is too long to scroll
    public Func<string?, CancellationToken, Task<IEnumerable<object>>> ProjectChoicesPopulator =>
        (search, _) => Task.FromResult<IEnumerable<object>>(
        [
            .. (ProjectChoices ?? [])
            .Where(c => AutoComplete.ProjectAutoCompleteFilterPredicate(search, c))
            .Take(ApplicationSettings.MaxProjectChoices)
        ]);

    // Pushed to the running entry when changed
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

    // Free text, usually a Jira ticket key, pushed to the running entry when edited (on lost focus)
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

    public bool IsRunning
    {
        get;
        private set => Set(ref field, value);
    }

    public string Elapsed
    {
        get;
        private set => Set(ref field, value);
    } = "00:00:00";

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

    public Task StopAsync()
    {
        return WithBusyIndicator(() => TimeTracking.StopAsync());
    }

    // Keyboard shortcut, ignored while busy: a description update still in flight could restart a stopped entry
    public Task ToggleAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        return IsRunning
            ? StopAsync()
            : StartAsync();
    }

    public Task RefreshAsync()
    {
        return WithBusyIndicator(() => TimeTracking.RefreshRunningEntryAsync());
    }

    // Each setter only sends its own field, the others come from the running entry, so the order in which
    // OnRunningEntryChanged refreshes them can't send a half-updated state
    private void PushToRunningEntry(ProjectTaskViewModel? choice = null, bool description = false)
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
