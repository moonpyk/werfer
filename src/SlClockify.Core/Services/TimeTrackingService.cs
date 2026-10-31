using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using SlClockify.Core.Models;
using SlClockify.Core.Requests;

namespace SlClockify.Core.Services;

/// <summary>
///     Clockify session shared by every screen: current user, workspace and running time entry.
/// </summary>
[PublicAPI]
public class TimeTrackingService(
    IClockifyApiClient client,
    IOptions<ClockifyOptions> options
)
{
    private IClockifyApiClient Client { get; } = client;
    private IOptions<ClockifyOptions> Options { get; } = options;

    public User? User { get; private set; }

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
    ///     Applies the API key, loads the user and picks up a time entry already running on Clockify.
    /// </summary>
    public async Task<User> ConnectAsync(string apiKey, CancellationToken ctk = default)
    {
        // Same instance the auth handler reads, so the next call uses the new key
        Options.Value.ApiKey = apiKey.Trim();

        User = await Client.GetCurrentUserAsync(ctk);
        WorkspaceId = User.ActiveWorkspace ?? User.DefaultWorkspace ?? string.Empty;

        // Latest entry first, it's the running one when it has no end
        var last = (
            await Client.GetTimeEntriesForUserAsync(WorkspaceId, User.Id, pageSize: 1, ctk: ctk)
        ).FirstOrDefault();

        RunningEntry = last?.TimeInterval is { End: null } ? last : null;

        return User;
    }

    /// <summary>
    ///     Lists the (project, task) pairs a time entry can be started on, active tasks of non-archived projects only.
    /// </summary>
    public async Task<
        List<(Project Project, ProjectTask Task)>
    > GetProjectTasksAsync(CancellationToken ctk = default)
    {
        _ = CurrentUser;

        // shortcut: single page of 5000 projects (Clockify max), paginate if a workspace ever exceeds it
        var projects = await Client.GetProjectsAsync(
            WorkspaceId,
            archived: false,
            hydrated: true,
            pageSize: 5000,
            ctk: ctk
        );

        // Every entry needs a task in our organization, so only (project, task) pairs are offered
        return projects
            .SelectMany(p => (p.Tasks ?? []).Where(t => t.Status == "ACTIVE").Select(t => (p, t)))
            .ToList();
    }

    public async Task StartAsync(string projectId, string taskId, string? description, CancellationToken ctk = default)
    {
        _ = CurrentUser;

        RunningEntry = await Client.CreateTimeEntryAsync(WorkspaceId, new CreateTimeEntryRequest
        {
            Start = DateTimeOffset.UtcNow,
            ProjectId = projectId,
            TaskId = taskId,
            Description = Normalize(description)
        }, ctk);
    }

    public async Task StopAsync(CancellationToken ctk = default)
    {
        await Client.StopActiveTimeEntryAsync(WorkspaceId, CurrentUser.Id, new StopTimeEntryRequest
        {
            End = DateTimeOffset.UtcNow
        }, ctk);

        RunningEntry = null;
    }

    /// <summary>
    ///     Changes the description of the running entry, does nothing when stopped or unchanged.
    /// </summary>
    public async Task UpdateDescriptionAsync(
        string? description, 
        CancellationToken ctk = default
    )
    {
        var running = RunningEntry;
        description = Normalize(description);

        if (running is null || running.Description == description)
        {
            return;
        }

        // Clockify's update is a full replace, so every field we know about is resent
        // shortcut: custom field values aren't resent, map CustomFieldValues if the workspace starts using them
        RunningEntry = await Client.UpdateTimeEntryAsync(WorkspaceId, running.Id, new CreateTimeEntryRequest
        {
            Start = running.TimeInterval!.Start,
            ProjectId = running.ProjectId,
            TaskId = running.TaskId,
            Billable = running.Billable,
            TagIds = running.TagIds,
            Description = description
        }, ctk);
    }

    private static string? Normalize(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
