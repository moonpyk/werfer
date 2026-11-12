using JetBrains.Annotations;
using Refit;
using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;

namespace Werfer.Clockify;

/// <summary>
///     Client interface for interacting with the Clockify REST API.
/// </summary>
[PublicAPI]
public interface IClockifyRawApiClient
{
    /// <summary>
    ///     Gets information about the currently authenticated user.
    /// </summary>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The details of the currently authenticated user.</returns>
    [Get("/user")]
    Task<User> GetCurrentUserAsync(CancellationToken ctk = default);

    /// <summary>
    ///     Gets one page of the projects of a workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="archived">Only archived projects when true, only active ones when false, all when null.</param>
    /// <param name="hydrated">True to include the tasks of each project.</param>
    /// <param name="page">Page number, 1-based.</param>
    /// <param name="pageSize">Number of projects per page; Clockify may serve fewer.</param>
    /// <param name="ctk">Cancels the request.</param>
    /// <returns>One page of projects. Its Last-Page header tells whether more pages follow.</returns>
    [Get("/workspaces/{workspaceId}/projects")]
    Task<IApiResponse<List<Project>>> GetRawProjectsAsync(
        string workspaceId,
        [Query] bool? archived = null,
        [Query] bool? hydrated = null,
        [Query] int? page = null,
        [AliasAs("page-size")] int? pageSize = null,
        CancellationToken ctk = default);

    /// <summary>
    ///     Creates a new time entry in a workspace for the currently authenticated user.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="request">The time entry creation payload.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The created time entry.</returns>
    [Post("/workspaces/{workspaceId}/time-entries")]
    Task<TimeEntry> CreateTimeEntryAsync(
        string workspaceId,
        [Body] CreateTimeEntryRequest request,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Gets one page of the time entries of a user, most recent first.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="userId">The ID of the user whose time entries to get.</param>
    /// <param name="start">Only the entries started at or after this time, when set.</param>
    /// <param name="inProgress">True to get only the running entry.</param>
    /// <param name="page">Page number, 1-based.</param>
    /// <param name="pageSize">Number of entries per page; Clockify may serve fewer.</param>
    /// <param name="ctk">Cancels the request.</param>
    /// <returns>One page of entries. Its Last-Page header tells whether more pages follow.</returns>
    [Get("/workspaces/{workspaceId}/user/{userId}/time-entries")]
    Task<IApiResponse<List<TimeEntry>>> GetRawTimeEntriesForUserAsync(
        string workspaceId,
        string userId,
        [Query] DateTimeOffset? start = null,
        [AliasAs("in-progress")] bool? inProgress = null,
        [Query] int? page = null,
        [AliasAs("page-size")] int? pageSize = null,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Replaces a specific time entry by ID within a workspace, omitted fields are cleared.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="id">The ID of the time entry to update.</param>
    /// <param name="request">The full time entry payload, same shape as the creation one.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The updated time entry.</returns>
    [Put("/workspaces/{workspaceId}/time-entries/{id}")]
    Task<TimeEntry> UpdateTimeEntryAsync(
        string workspaceId,
        string id,
        [Body] CreateTimeEntryRequest request,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Deletes a specific time entry by ID within a workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="id">The ID of the time entry to delete.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Delete("/workspaces/{workspaceId}/time-entries/{id}")]
    Task DeleteTimeEntryAsync(
        string workspaceId,
        string id,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Stops the currently active time entry for a specific user within a workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="userId">The ID of the user whose running time entry should be stopped.</param>
    /// <param name="request">The request payload containing the end time.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The stopped time entry.</returns>
    [Patch("/workspaces/{workspaceId}/user/{userId}/time-entries")]
    Task<TimeEntry> StopActiveTimeEntryAsync(
        string workspaceId,
        string userId,
        [Body] StopTimeEntryRequest request,
        CancellationToken ctk = default
    );
}
