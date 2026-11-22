using Werfer.Clockify.Models;
using Werfer.Clockify.ViewModels;

namespace Werfer.Services;

/// <summary>
///     What the timer bar shows: the project, task and description of the running entry, or of the next one to start.
///     <list type="bullet">
///         <item>The draft is sent to Clockify only by <see cref="SaveAsync" />, called when the user acts.</item>
///         <item>
///             The draft takes Clockify's values only when a different entry starts running, or on
///             <see cref="RefreshAsync" />. Not when Clockify answers a save: that would erase what the user typed while
///             waiting.
///         </item>
///     </list>
/// </summary>
public class TimerDraft
{
    // Id of the entry the draft last took its values from: the same id again means no reload
    private string? _loadedEntryId;

    /// <summary>
    ///     Creates the draft. It takes the values of each new running entry from then on.
    /// </summary>
    public TimerDraft(
        TimeTrackingService timeTracking,
        IApplicationSettings settings
    )
    {
        Service = timeTracking;
        Settings = settings;

        Service.RunningEntryChanged += (_, _) =>
        {
            if (Service.RunningEntry is { } running && running.Id != _loadedEntryId)
            {
                Load(running);
            }
        };
    }

    private TimeTrackingService Service { get; }
    private IApplicationSettings Settings { get; }

    /// <summary>
    ///     Every project and task the user can pick, the session's. The running entry's project and task are looked up
    ///     here.
    /// </summary>
    public IReadOnlyList<ProjectTaskChoice> Choices => Service.Session?.Choices ?? [];

    /// <summary>
    ///     The project and task picked. Null when the running entry's are not in <see cref="Choices" /> (archived
    ///     project, no task); a save then leaves the entry's project and task unchanged.
    /// </summary>
    public ProjectTaskChoice? Choice { get; set; }

    /// <summary>
    ///     Free text, usually a Jira ticket key.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Raised when the draft changes its values by itself: a running entry loaded, cleared, continued. Not raised
    ///     when the UI sets <see cref="Choice" /> or <see cref="Description" />: the UI already shows them.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    ///     Sends the draft to the running entry. Does nothing when no entry runs or nothing changed.
    /// </summary>
    public Task SaveAsync(CancellationToken ctk = default)
    {
        if (Service.RunningEntry is not { } running)
        {
            return Task.CompletedTask;
        }

        return Service.UpdateRunningEntryAsync(
            Choice?.Project.Id ?? running.ProjectId,
            Choice?.Task.Id ?? running.TaskId,
            Description,
            ctk
        );
    }

    /// <summary>
    ///     Starts an entry with the draft's values. With no project picked, takes the default project and task when
    ///     the setting is on. Returns false, and starts nothing, when there is still no project.
    /// </summary>
    public async Task<bool> StartAsync(CancellationToken ctk = default)
    {
        if (Choice is null && Settings.DefaultChoiceEnabled)
        {
            Choice = await FindDefaultChoiceAsync(ctk);

            // Only when found: the UI would otherwise clear a search the user typed in the project box
            if (Choice is not null)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        if (Choice is not { } choice)
        {
            return false;
        }

        await Service.StartAsync(
            choice.Project.Id,
            choice.Task.Id,
            Description,
            ctk
        );
        return true;
    }

    /// <summary>
    ///     Saves the draft, then stops the entry: a stopped entry can't be updated anymore. Stops even if the save fails.
    /// </summary>
    public async Task StopAsync(CancellationToken ctk = default)
    {
        try
        {
            await SaveAsync(ctk);
        }
        finally
        {
            await Service.StopAsync(ctk);
        }
    }

    /// <summary>
    ///     Saves the draft, then reloads the running entry from Clockify: it may have been changed in another app.
    /// </summary>
    public async Task RefreshAsync(CancellationToken ctk = default)
    {
        var saved = false;

        try
        {
            await SaveAsync(ctk);
            saved = true;
        }
        finally
        {
            await Service.RefreshRunningEntryAsync(ctk);

            // After a failed save, the draft still holds what was typed: taking Clockify's values would lose it
            if (saved && Service.RunningEntry is { } running)
            {
                Load(running);
            }
        }
    }

    /// <summary>
    ///     Stops the timer and empties the draft. If the stop fails, the draft is kept, so nothing typed is lost.
    /// </summary>
    public async Task ResetAsync(CancellationToken ctk = default)
    {
        if (Service.RunningEntry is not null)
        {
            await StopAsync(ctk);
        }

        Choice = null;
        Description = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Deletes the running entry and empties the draft: what was typed belonged to that entry. If the delete fails,
    ///     the draft is kept.
    /// </summary>
    public async Task DeleteRunningAsync(CancellationToken ctk = default)
    {
        await Service.DeleteRunningEntryAsync(ctk);

        Choice = null;
        Description = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Stops the running entry, if any, then starts a new one with the given project, task and description.
    /// </summary>
    public async Task ContinueAsync(
        ProjectTaskChoice choice,
        string? description,
        CancellationToken ctk = default
    )
    {
        if (Service.RunningEntry is not null)
        {
            await StopAsync(ctk);
        }

        Choice = choice;
        Description = description;
        Changed?.Invoke(this, EventArgs.Empty);

        await StartAsync(ctk);
    }

    private void Load(TimeEntry running)
    {
        _loadedEntryId = running.Id;
        Choice = FindChoice(running);
        Description = running.Description;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private ProjectTaskChoice? FindChoice(TimeEntry entry)
    {
        return FindChoice(entry.ProjectId, entry.TaskId);
    }

    private ProjectTaskChoice? FindChoice(string? projectId, string? taskId)
    {
        return Choices.FirstOrDefault(c =>
            c.Project.Id == projectId &&
            c.Task.Id == taskId
        );
    }

    // Null when the default isn't among the choices anymore (archived, done), or there is no last entry
    private async Task<ProjectTaskChoice?> FindDefaultChoiceAsync(CancellationToken ctk)
    {
        if (Settings.DefaultTaskId is { } taskId)
        {
            return FindChoice(Settings.DefaultProjectId, taskId);
        }

        return await Service.GetLastTimeEntryAsync(ctk) is { } last
            ? FindChoice(last)
            : null;
    }
}
