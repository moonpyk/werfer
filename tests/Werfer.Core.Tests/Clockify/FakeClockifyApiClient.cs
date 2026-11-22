using System.Net;
using Refit;
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
    ///     When set, the stop waits for it: lets a test queue another call behind a stop in progress
    /// </summary>
    public TaskCompletionSource? StopGate { get; set; }

    public Exception? StopException { get; set; }

    /// <summary>
    ///     When set, updating an entry fails with it, e.g. the network dropping while the description is sent
    /// </summary>
    public Exception? UpdateException { get; set; }

    public List<(string Id, CreateTimeEntryRequest Request)> Updates { get; } = [];

    public List<CreateTimeEntryRequest> Creates { get; } = [];

    /// <summary>
    ///     Ids of the deleted entries, in order.
    /// </summary>
    public List<string> Deletes { get; } = [];

    /// <summary>
    ///     How many times the in-progress query was made: on connection, refresh, and before each update
    /// </summary>
    public int RunningReads { get; private set; }

    /// <summary>
    ///     When set, loading the user fails with it, e.g. a 401 for a rejected API key
    /// </summary>
    public Exception? UserException { get; set; }

    /// <summary>
    ///     What the entries query (not in-progress) serves, a page at a time
    /// </summary>
    public List<TimeEntry> Entries { get; } = [];

    /// <summary>
    ///     What the projects query serves, a page at a time, whatever the filters. Give the projects tasks, or nothing
    ///     can be started
    /// </summary>
    public List<Project> Projects { get; } = [];

    /// <summary>
    ///     When set, loading the projects fails with it, e.g. the network dropping during a connection
    /// </summary>
    public Exception? ProjectsException { get; set; }

    /// <summary>
    ///     Page size actually served, whatever size is asked: Clockify also caps it without an error
    /// </summary>
    public int MaxPageSize { get; set; } = int.MaxValue;

    public Task<User> GetCurrentUserAsync(CancellationToken ctk = default)
    {
        return UserException is null
            ? Task.FromResult(User)
            : Task.FromException<User>(UserException);
    }

    public Task<IApiResponse<List<TimeEntry>>> GetRawTimeEntriesForUserAsync(
        string workspaceId,
        string userId,
        DateTimeOffset? start = null,
        bool? inProgress = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ctk = default
    )
    {
        if (inProgress == true)
        {
            RunningReads++;
            return Page<TimeEntry>(Running is null ? [] : [Running], true);
        }

        return PageOf(Entries, page, pageSize);
    }

    public Task<TimeEntry> UpdateTimeEntryAsync(
        string workspaceId,
        string id,
        CreateTimeEntryRequest request,
        CancellationToken ctk = default
    )
    {
        if (UpdateException is not null)
        {
            throw UpdateException;
        }

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

    public Task<IApiResponse<List<Project>>> GetRawProjectsAsync(
        string workspaceId,
        bool? archived = null,
        bool? hydrated = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ctk = default
    )
    {
        return ProjectsException is null
            ? PageOf(Projects, page, pageSize)
            : Task.FromException<IApiResponse<List<Project>>>(ProjectsException);
    }

    public Task<TimeEntry> CreateTimeEntryAsync(
        string workspaceId,
        CreateTimeEntryRequest request,
        CancellationToken ctk = default
    )
    {
        Creates.Add(request);

        Running = new TimeEntry
        {
            Id = $"new{Creates.Count}",
            ProjectId = request.ProjectId,
            TaskId = request.TaskId,
            Description = request.Description,
            TimeInterval = new TimeInterval { Start = request.Start }
        };

        return Task.FromResult(Running);
    }

    public Task DeleteTimeEntryAsync(string workspaceId, string id, CancellationToken ctk = default)
    {
        Deletes.Add(id);
        return Task.CompletedTask;
    }

    // The given page of items, its size capped by MaxPageSize as Clockify does; 50 when none asked, Clockify's default
    private Task<IApiResponse<List<T>>> PageOf<T>(List<T> items, int? page, int? pageSize)
    {
        var size = Math.Min(pageSize ?? 50, MaxPageSize);
        var skip = ((page ?? 1) - 1) * size;

        return Page<T>([.. items.Skip(skip).Take(size)], skip + size >= items.Count);
    }

    private static Task<IApiResponse<List<T>>> Page<T>(List<T> items, bool last)
    {
        var message = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.clockify.me/")
        };
        message.Headers.Add("Last-Page", last ? "true" : "false");

        return Task.FromResult<IApiResponse<List<T>>>(new ApiResponse<List<T>>(message, items, new RefitSettings()));
    }
}
