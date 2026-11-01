using Avalonia.Controls;
using Avalonia.Threading;
using JetBrains.Annotations;
using SlClockify.Core.Services;
using SlClockify.Utils;

namespace SlClockify.ViewModels;

/// <summary>
///     Main page: pick a project and task, describe the work, start and stop the timer.
/// </summary>
[UsedImplicitly]
public class TimerViewModel : ViewModelBase
{
    private IDisposable? _ticker;

    public TimerViewModel(TimeTrackingService timeTracking)
    {
        TimeTracking = timeTracking;
        Status = "Not connected, set your Clockify API key in Settings";

        // Connection happens from the settings page, projects are loaded here once it succeeds
        TimeTracking.Connected += (_, _) => _ = RunAsync(LoadProjectChoicesAsync);
        // The running entry may change from another screen, the timer display follows it
        TimeTracking.RunningEntryChanged += (_, _) => OnRunningEntryChanged();
    }

    private TimeTrackingService TimeTracking { get; }

    // Null until connected, the timer panel is hidden meanwhile
    public List<ProjectTaskChoice>? ProjectChoices
    {
        get;
        private set => Set(ref field, value);
    }

    public AutoCompleteFilterPredicate<object?> ChoiceFilter { get; } = AutoComplete.ProjectAutoCompleteFilterPredicate;

    public ProjectTaskChoice? SelectedChoice
    {
        get;
        set => Set(ref field, value);
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

            // Skips values coming from the running entry itself, a nested RunAsync would reset IsBusy mid-operation
            if (TimeTracking.RunningEntry is { } running && running.Description != value)
            {
                RunAsync(() => TimeTracking.UpdateDescriptionAsync(value))
                    .ContinueWith(t => t.Exception?.Handle(_ => true));
            }
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
        return RunAsync(async () =>
        {
            if (SelectedChoice is null)
            {
                Status = "Pick a project and a task first";
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
        return RunAsync(() => TimeTracking.StopAsync());
    }

    private async Task LoadProjectChoicesAsync()
    {
        ProjectChoices = (await TimeTracking.GetProjectTasksAsync())
            .Select(pt => new ProjectTaskChoice(pt.Project, pt.Task))
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
