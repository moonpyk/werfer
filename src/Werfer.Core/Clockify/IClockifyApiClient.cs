using JetBrains.Annotations;
using Refit;
using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;

namespace Werfer.Clockify;

/// <summary>
///     Client interface for interacting with the Clockify REST API.
/// </summary>
[PublicAPI]
public interface IClockifyApiClient
{
    /// <summary>
    ///     Gets information about the currently authenticated user.
    /// </summary>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The details of the currently authenticated user.</returns>
    [Get("/user")]
    Task<User> GetCurrentUserAsync(CancellationToken ctk = default);

    /// <summary>
    ///     Gets all workspaces accessible to the currently authenticated user.
    /// </summary>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A list of workspaces.</returns>
    [Get("/workspaces")]
    Task<List<Workspace>> GetWorkspacesAsync(CancellationToken ctk = default);

    /// <summary>
    ///     Gets clients for a workspace based on optional filtering and pagination criteria.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="name">Optional client name filter.</param>
    /// <param name="archived">Optional filter for archived status.</param>
    /// <param name="page">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="sortColumn">Column to sort by.</param>
    /// <param name="sortOrder">Sort order (e.g. "ASCENDING" or "DESCENDING").</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A list of clients matching the criteria.</returns>
    [Get("/workspaces/{workspaceId}/clients")]
    Task<List<Client>> GetClientsAsync(
        string workspaceId,
        [Query] string? name = null,
        [Query] bool? archived = null,
        [Query] int? page = null,
        [Query("page-size")] int? pageSize = null,
        [Query("sort-column")] string? sortColumn = null,
        [Query("sort-order")] string? sortOrder = null,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Gets a specific client by ID within a workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="clientId">The ID of the client to retrieve.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The client details.</returns>
    [Get("/workspaces/{workspaceId}/clients/{clientId}")]
    Task<Client> GetClientAsync(
        string workspaceId,
        string clientId,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Gets projects for a workspace based on optional filtering and pagination criteria.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="name">Optional project name filter.</param>
    /// <param name="archived">Optional filter for archived status.</param>
    /// <param name="billable">Optional filter for billable status.</param>
    /// <param name="clients">Optional list of client IDs to filter projects by.</param>
    /// <param name="hydrated">Optional flag to include the tasks of each project.</param>
    /// <param name="page">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="sortColumn">Column to sort by.</param>
    /// <param name="sortOrder">Sort order (e.g. "ASCENDING" or "DESCENDING").</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A list of projects matching the criteria.</returns>
    [Get("/workspaces/{workspaceId}/projects")]
    Task<List<Project>> GetProjectsAsync(
        string workspaceId,
        [Query] string? name = null,
        [Query] bool? archived = null,
        [Query] bool? billable = null,
        [Query(CollectionFormat.Multi)] string[]? clients = null,
        [Query] bool? hydrated = null,
        [Query] int? page = null,
        [Query("page-size")] int? pageSize = null,
        [Query("sort-column")] string? sortColumn = null,
        [Query("sort-order")] string? sortOrder = null,
        CancellationToken ctk = default);

    /// <summary>
    ///     Gets a specific project by ID within a workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="projectId">The ID of the project to retrieve.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The project details.</returns>
    [Get("/workspaces/{workspaceId}/projects/{projectId}")]
    Task<Project> GetProjectAsync(
        string workspaceId,
        string projectId,
        CancellationToken ctk = default
    );

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
    ///     Creates a new time entry in a workspace for a specific user.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="userId">The ID of the user for whom the time entry is created.</param>
    /// <param name="request">The time entry creation payload.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The created time entry.</returns>
    [Post("/workspaces/{workspaceId}/user/{userId}/time-entries")]
    Task<TimeEntry> CreateTimeEntryForUserAsync(
        string workspaceId,
        string userId,
        [Body] CreateTimeEntryRequest request,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Gets a specific time entry by ID within a workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="id">The ID of the time entry to retrieve.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The time entry details.</returns>
    [Get("/workspaces/{workspaceId}/time-entries/{id}")]
    Task<TimeEntry> GetTimeEntryAsync(
        string workspaceId,
        string id,
        CancellationToken ctk = default
    );

    /// <summary>
    ///     Gets time entries for a specific user within a workspace based on optional filtering and pagination criteria.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="userId">The ID of the user whose time entries to retrieve.</param>
    /// <param name="description">Optional description filter.</param>
    /// <param name="start">Optional start date and time filter.</param>
    /// <param name="end">Optional end date and time filter.</param>
    /// <param name="project">Optional project ID filter.</param>
    /// <param name="task">Optional task ID filter.</param>
    /// <param name="hydrated">Optional flag indicating whether to return hydrated time entry objects.</param>
    /// <param name="page">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A list of time entries matching the criteria.</returns>
    [Get("/workspaces/{workspaceId}/user/{userId}/time-entries")]
    Task<List<TimeEntry>> GetTimeEntriesForUserAsync(
        string workspaceId,
        string userId,
        [Query] string? description = null,
        [Query] DateTimeOffset? start = null,
        [Query] DateTimeOffset? end = null,
        [Query] string? project = null,
        [Query] string? task = null,
        [Query] bool? hydrated = null,
        [Query] int? page = null,
        [Query("page-size")] int? pageSize = null,
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
