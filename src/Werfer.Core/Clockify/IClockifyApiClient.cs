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
    ///     Gets every project of a workspace, all pages read.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="archived">Only archived projects when true, only active ones when false, all when null.</param>
    /// <param name="hydrated">True to include the tasks of each project.</param>
    /// <param name="ctk">Cancels the requests.</param>
    Task<List<Project>> GetProjectsAsync(
        string workspaceId,
        bool? archived = null,
        bool? hydrated = null,
        CancellationToken ctk = default
    )
    {
        return GetAllPagesAsync(page => GetRawProjectsAsync(
            workspaceId,
            archived,
            hydrated,
            page,
            PageSize,
            ctk
        ));
    }

    /// <summary>
    ///     Gets every time entry of a user, all pages read.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <param name="userId">The ID of the user whose time entries to get.</param>
    /// <param name="start">Only the entries started at or after this time, when set.</param>
    /// <param name="inProgress">True to get only the running entry.</param>
    /// <param name="ctk">Cancels the requests.</param>
    Task<List<TimeEntry>> GetTimeEntriesForUserAsync(
        string workspaceId,
        string userId,
        DateTimeOffset? start = null,
        bool? inProgress = null,
        CancellationToken ctk = default
    )
    {
        return GetAllPagesAsync(page => GetRawTimeEntriesForUserAsync(
            workspaceId,
            userId,
            start,
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
