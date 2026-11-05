using System.Net;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Refit;
using Werfer.Clockify;
using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;
using Werfer.Clockify.ViewModels;

namespace Werfer.Services;

/// <summary>
///     Clockify session shared by every screen: current user, workspace and running time entry.
/// </summary>
[PublicAPI]
public class TimeTrackingService(
    IClockifyApiClient client,
    IOptions<ClockifyOptions> options
)
{
    /// <summary>
    ///     One operation at a time: overlapping calls would act on a stale RunningEntry, e.g. a description
    ///     update finishing after a stop and restarting the entry. Waiting is async, so queued calls don't block the UI
    /// </summary>
    private SemaphoreSlim Operations { get; } = new(1, 1);

    private IClockifyApiClient Client { get; } = client;
    private IOptions<ClockifyOptions> Options { get; } = options;

    public User? User { get; private set; }

    /// <summary>
    ///     The unique identifier for the active workspace, representing the context in which
    ///     time entries and projects are managed.
    /// </summary>
    public string WorkspaceId { get; private set; } = string.Empty;

    /// <summary>
    ///     The entry currently being timed, null when the timer is stopped.
    /// </summary>
    public TimeEntry? RunningEntry
    {
        get;
        private set
        {
            field = value;
            RunningEntryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private User CurrentUser => User ?? throw new InvalidOperationException("Not connected to Clockify");

    /// <summary>
    ///     Raised whenever <see cref="RunningEntry" /> is replaced, started, updated or stopped.
    /// </summary>
    public event EventHandler? RunningEntryChanged;

    /// <summary>
    ///     Raised after a successful <see cref="ConnectAsync" />, once the user, workspace and running entry are known.
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    ///     Applies the API key, loads the user and picks up a time entry already running on Clockify.
    /// </summary>
    public Task<User> ConnectAsync(string apiKey, CancellationToken ctk = default)
    {
        return ExclusiveAsync(async () =>
        {
            // Same instance the auth handler reads, so the next call uses the new key
            var previousKey = Options.Value.ApiKey;
            Options.Value.ApiKey = apiKey.Trim();

            try
            {
                User = await Client.GetCurrentUserAsync(ctk);
            }
            catch
            {
                // A rejected key mustn't replace the one the current session (and its running timer) works with
                Options.Value.ApiKey = previousKey;
                throw;
            }

            WorkspaceId = User.ActiveWorkspace ?? User.DefaultWorkspace ?? string.Empty;

            await LoadRunningEntryAsync(ctk);

            Connected?.Invoke(this, EventArgs.Empty);

            return User;
        }, ctk);
    }

    /// <summary>
    ///     Picks up the entry running on Clockify, it may have been started or stopped from elsewhere.
    /// </summary>
    public Task RefreshRunningEntryAsync(CancellationToken ctk = default)
    {
        return ExclusiveAsync(() => LoadRunningEntryAsync(ctk), ctk);
    }

    private async Task LoadRunningEntryAsync(CancellationToken ctk)
    {
        // Filtered on in-progress: the latest entry by start may be a finished one added by hand later in the day
        var last = (
            await Client.GetTimeEntriesForUserAsync(
                WorkspaceId,
                CurrentUser.Id,
                inProgress: true,
                pageSize: 1,
                ctk: ctk
            )
        ).FirstOrDefault();

        RunningEntry = last?.TimeInterval is { End: null } ? last : null;
    }

    /// <summary>
    ///     The user's entries started since <paramref name="start" />, running one included.
    /// </summary>
    public Task<List<TimeEntry>> GetTimeEntriesSinceAsync(DateTimeOffset start, CancellationToken ctk = default)
    {
        // Read only, no lock: it would queue behind (and delay) a start or stop for nothing
        // shortcut: single page of 1000 entries, about 20 weeks of a busy user; paginate if history ever spans longer
        return Client.GetTimeEntriesForUserAsync(
            WorkspaceId,
            CurrentUser.Id,
            start: start,
            pageSize: 1000,
            ctk: ctk
        );
    }

    /// <summary>
    ///     Deletes finished entries, one by one: a failure leaves the earlier ones deleted.
    /// </summary>
    public async Task DeleteTimeEntriesAsync(IEnumerable<string> ids, CancellationToken ctk = default)
    {
        foreach (var id in ids)
        {
            await Client.DeleteTimeEntryAsync(WorkspaceId, id, ctk);
        }
    }

    /// <summary>
    ///     Lists the (project, task) pairs a time entry can be started on, active tasks of non-archived projects only.
    /// </summary>
    public async Task<List<ProjectTaskChoice>> GetProjectTasksAsync(CancellationToken ctk = default)
    {
        // shortcut: single page of 5000 projects (Clockify max), paginate if a workspace ever exceeds it
        var projects = await Client.GetProjectsAsync(
            WorkspaceId,
            archived: false,
            hydrated: true,
            pageSize: 5000,
            ctk: ctk
        );

        return (
            from p in projects
            from pt in p.Tasks ?? []
            where pt.Status == "ACTIVE"
            select new ProjectTaskChoice(p, pt)
        ).ToList();
    }

    /// <summary>
    ///     Starts tracking time for the specified project and task in Clockify.
    /// </summary>
    public Task StartAsync(
        string projectId,
        string taskId,
        string? description,
        CancellationToken ctk = default
    )
    {
        return ExclusiveAsync(async () =>
        {
            RunningEntry = await Client.CreateTimeEntryAsync(WorkspaceId, new CreateTimeEntryRequest
            {
                Start = DateTimeOffset.UtcNow,
                ProjectId = projectId,
                TaskId = taskId,
                Description = Normalize(description)
            }, ctk);
        }, ctk);
    }

    public Task StopAsync(CancellationToken ctk = default)
    {
        return ExclusiveAsync(async () =>
        {
            try
            {
                await Client.StopActiveTimeEntryAsync(WorkspaceId, CurrentUser.Id, new StopTimeEntryRequest
                {
                    End = DateTimeOffset.UtcNow
                }, ctk);
            }
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Nothing running anymore, already stopped from another client: the goal is reached
            }

            RunningEntry = null;
        }, ctk);
    }

    /// <summary>
    ///     Changes the project, task and description of the running entry, does nothing when stopped or unchanged.
    /// </summary>
    public Task UpdateRunningEntryAsync(
        string projectId,
        string taskId,
        string? description,
        CancellationToken ctk = default
    )
    {
        // Checked once the lock is held, against the entry as the previous operation left it
        return ExclusiveAsync(
            () => UpdateRunningEntryCoreAsync(
                projectId,
                taskId,
                Normalize(description),
                ctk
            ),
            ctk
        );
    }

    private async Task UpdateRunningEntryCoreAsync(
        string projectId,
        string taskId,
        string? description,
        CancellationToken ctk
    )
    {
        var running = RunningEntry;
        if (running is null
            || (running.ProjectId == projectId && running.TaskId == taskId && running.Description == description))
        {
            return;
        }

        // Re-read it: it may have been stopped or edited from another client, and a PUT without end would restart it
        var current = await Client.GetTimeEntryAsync(
            WorkspaceId,
            running.Id,
            ctk
        );

        if (current.TimeInterval is not { End: null })
        {
            RunningEntry = null;
            throw new InvalidOperationException("The timer was stopped from another client, changes not saved");
        }

        // Clockify's update is a full replace, so every field we know about is resent
        // shortcut: custom field values aren't resent, map CustomFieldValues if the workspace starts using them
        RunningEntry = await Client.UpdateTimeEntryAsync(WorkspaceId, current.Id, new CreateTimeEntryRequest
        {
            Start = current.TimeInterval.Start,
            ProjectId = projectId,
            TaskId = taskId,
            Billable = current.Billable,
            TagIds = current.TagIds,
            Description = description
        }, ctk);
    }

    private Task ExclusiveAsync(Func<Task> action, CancellationToken ctk)
    {
        return ExclusiveAsync(async () =>
        {
            await action();
            return true;
        }, ctk);
    }

    private async Task<T> ExclusiveAsync<T>(Func<Task<T>> action, CancellationToken ctk)
    {
        // No [MethodImpl(MethodImplOptions.AggressiveInlining)] on purpose: every caller then makes an HTTP request
        // (tens to hundreds of ms), next to which a saved call (~1 ns) is noise. The attribute would only remove that
        // call anyway, not the real overhead (closure, async state machine, delegate invocation), and ILC already
        // inlines a method this small on its own when worthwhile.
        await Operations.WaitAsync(ctk);

        try
        {
            return await action();
        }
        finally
        {
            Operations.Release();
        }
    }

    private static string? Normalize(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim()
            ;
    }
}
