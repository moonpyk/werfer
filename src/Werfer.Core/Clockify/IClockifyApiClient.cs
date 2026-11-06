using JetBrains.Annotations;
using Refit;
using Werfer.Clockify.Models;

namespace Werfer.Clockify;

/// <summary>
///     The Clockify API as the app uses it: list endpoints read whole, pagination is handled here, not by callers.
///     Refit generates it like <see cref="IClockifyRawApiClient" />, the methods below have a body so it leaves them be.
/// </summary>
[PublicAPI]
public interface IClockifyApiClient : IClockifyRawApiClient
{
    /// <summary>
    ///     Clockify's maximum, fewest requests
    /// </summary>
    private const int PageSize = 5000;

    /// <summary>
    ///     Gets every project of a workspace matching the optional filters, all pages read.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="name">Optional project name filter.</param>
    /// <param name="archived">Optional filter for archived status.</param>
    /// <param name="billable">Optional filter for billable status.</param>
    /// <param name="clients">Optional list of client IDs to filter projects by.</param>
    /// <param name="hydrated">Optional flag to include the tasks of each project.</param>
    /// <param name="sortColumn">Column to sort by.</param>
    /// <param name="sortOrder">Sort order (e.g. "ASCENDING" or "DESCENDING").</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    Task<List<Project>> GetProjectsAsync(
        string workspaceId,
        string? name = null,
        bool? archived = null,
        bool? billable = null,
        string[]? clients = null,
        bool? hydrated = null,
        string? sortColumn = null,
        string? sortOrder = null,
        CancellationToken ctk = default
    )
    {
        return GetAllPagesAsync(page => GetRawProjectsAsync(
            workspaceId,
            name,
            archived,
            billable,
            clients,
            hydrated,
            page,
            PageSize,
            sortColumn,
            sortOrder,
            ctk
        ));
    }

    /// <summary>
    ///     Gets every time entry of a user matching the optional filters, all pages read.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="userId">The ID of the user whose time entries to retrieve.</param>
    /// <param name="description">Optional description filter.</param>
    /// <param name="start">Optional start date and time filter.</param>
    /// <param name="end">Optional end date and time filter.</param>
    /// <param name="project">Optional project ID filter.</param>
    /// <param name="task">Optional task ID filter.</param>
    /// <param name="hydrated">Optional flag indicating whether to return hydrated time entry objects.</param>
    /// <param name="inProgress">Optional flag to return only the running time entry.</param>
    /// <param name="ctk">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    Task<List<TimeEntry>> GetTimeEntriesForUserAsync(
        string workspaceId,
        string userId,
        string? description = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        string? project = null,
        string? task = null,
        bool? hydrated = null,
        bool? inProgress = null,
        CancellationToken ctk = default
    )
    {
        return GetAllPagesAsync(page => GetRawTimeEntriesForUserAsync(
            workspaceId,
            userId,
            description,
            start,
            end,
            project,
            task,
            hydrated,
            inProgress,
            page,
            PageSize,
            ctk
        ));
    }

    /// <summary>
    ///     Reads every page, <paramref name="getPage" /> fetching the given one (1-based). Clockify may serve fewer items
    ///     than the page size asked, without error, so only its Last-Page header (or an empty page, should the header be
    ///     missing) ends the reading.
    /// </summary>
    private static async Task<List<T>> GetAllPagesAsync<T>(Func<int, Task<IApiResponse<List<T>>>> getPage)
    {
        List<T> all = [];

        for (var page = 1;; page++)
        {
            using var response = await getPage(page);
            await response.EnsureSuccessfulAsync();

            if (response.Content is not { Count: > 0 } items)
            {
                return all;
            }

            all.AddRange(items);

            if (response.Headers is { } headers && headers.TryGetValues("Last-Page", out var lastPage)
                                                && bool.TryParse(lastPage.FirstOrDefault(), out var isLast)
                                                && isLast)
            {
                return all;
            }
        }
    }
}
