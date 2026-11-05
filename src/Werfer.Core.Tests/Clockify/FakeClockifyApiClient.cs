using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;

namespace Werfer.Clockify;

/// <summary>
///     Stands in for Clockify, no API key needed: serves the entries set on it and records what would be sent.
///     Only what TimeTrackingService calls is implemented.
/// </summary>
internal sealed class FakeClockifyApiClient : IClockifyApiClient
{
    public User User { get; } = new() { Id = "u1", ActiveWorkspace = "w1" };

    /// <summary>
    ///     What the in-progress query returns, null when nothing runs
    /// </summary>
    public TimeEntry? Running { get; set; }

    /// <summary>
    ///     What re-reading the entry returns, e.g. a stopped copy when another client stopped it
    /// </summary>
    public TimeEntry? Current { get; set; }

    /// <summary>
    ///     When set, the stop waits for it: lets a test queue another call behind a stop in progress
    /// </summary>
    public TaskCompletionSource? StopGate { get; set; }

    public Exception? StopException { get; set; }

    public List<(string Id, CreateTimeEntryRequest Request)> Updates { get; } = [];

    public int TimeEntryReads { get; private set; }

    public Task<User> GetCurrentUserAsync(CancellationToken ctk = default)
    {
        return Task.FromResult(User);
    }

    public Task<List<TimeEntry>> GetTimeEntriesForUserAsync(
        string workspaceId,
        string userId,
        string? description = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        string? project = null,
        string? task = null,
        bool? hydrated = null,
        bool? inProgress = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ctk = default
    )
    {
        return Task.FromResult<List<TimeEntry>>(Running is null ? [] : [Running]);
    }

    public Task<TimeEntry> GetTimeEntryAsync(string workspaceId, string id, CancellationToken ctk = default)
    {
        TimeEntryReads++;
        return Task.FromResult(Current ?? throw new InvalidOperationException("No entry to re-read"));
    }

    public Task<TimeEntry> UpdateTimeEntryAsync(
        string workspaceId,
        string id,
        CreateTimeEntryRequest request,
        CancellationToken ctk = default
    )
    {
        Updates.Add((id, request));

        return Task.FromResult(new TimeEntry
        {
            Id = id,
            ProjectId = request.ProjectId,
            TaskId = request.TaskId,
            Description = request.Description,
            Billable = request.Billable ?? false,
            TagIds = request.TagIds,
            TimeInterval = new TimeInterval { Start = request.Start, End = request.End }
        });
    }

    public async Task<TimeEntry> StopActiveTimeEntryAsync(
        string workspaceId,
        string userId,
        StopTimeEntryRequest request,
        CancellationToken ctk = default
    )
    {
        if (StopGate is not null)
        {
            await StopGate.Task;
        }

        if (StopException is not null)
        {
            throw StopException;
        }

        return Running ?? new TimeEntry();
    }

    public Task<List<Workspace>> GetWorkspacesAsync(CancellationToken ctk = default)
    {
        throw new NotSupportedException();
    }

    public Task<List<Client>> GetClientsAsync(
        string workspaceId,
        string? name = null,
        bool? archived = null,
        int? page = null,
        int? pageSize = null,
        string? sortColumn = null,
        string? sortOrder = null,
        CancellationToken ctk = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<Client> GetClientAsync(string workspaceId, string clientId, CancellationToken ctk = default)
    {
        throw new NotSupportedException();
    }

    public Task<List<Project>> GetProjectsAsync(
        string workspaceId,
        string? name = null,
        bool? archived = null,
        bool? billable = null,
        string[]? clients = null,
        bool? hydrated = null,
        int? page = null,
        int? pageSize = null,
        string? sortColumn = null,
        string? sortOrder = null,
        CancellationToken ctk = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<Project> GetProjectAsync(string workspaceId, string projectId, CancellationToken ctk = default)
    {
        throw new NotSupportedException();
    }

    public Task<TimeEntry> CreateTimeEntryAsync(
        string workspaceId,
        CreateTimeEntryRequest request,
        CancellationToken ctk = default
    )
    {
        throw new NotSupportedException();
    }

    public Task<TimeEntry> CreateTimeEntryForUserAsync(
        string workspaceId,
        string userId,
        CreateTimeEntryRequest request,
        CancellationToken ctk = default
    )
    {
        throw new NotSupportedException();
    }

    public Task DeleteTimeEntryAsync(string workspaceId, string id, CancellationToken ctk = default)
    {
        throw new NotSupportedException();
    }
}
