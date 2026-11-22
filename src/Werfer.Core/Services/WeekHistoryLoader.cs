using Werfer.Clockify.Models;

namespace Werfer.Services;

/// <summary>
///     Loads the week's entries for the history. Several loads can run at once (a stop, F5, a save): only the last one
///     started gets its result, see <see cref="LoadAsync" />.
/// </summary>
public class WeekHistoryLoader(
    TimeTrackingService timeTracking,
    IApplicationSettings settings
)
{
    // Incremented by each load, so only the latest load gets its result
    private int _generation;

    // Id of the user the last load was started for
    private string? _userId;

    /// <summary>
    ///     True when the connected user isn't the one the last load was for: what the history shows is another
    ///     account's, to empty before loading.
    /// </summary>
    public bool IsAnotherAccount => timeTracking.User?.Id != _userId;

    /// <summary>
    ///     The connected user's entries since the start of the week holding <paramref name="now" />, in
    ///     <paramref name="zone" />. Null when another load started while this one waited: this one may have started
    ///     before a save, and would show the entry unchanged.
    /// </summary>
    public async Task<List<TimeEntry>?> LoadAsync(
        DateTimeOffset now,
        TimeZoneInfo zone,
        CancellationToken ctk = default
    )
    {
        _userId = timeTracking.User?.Id;
        var load = Interlocked.Increment(ref _generation);

        var entries = await timeTracking.GetTimeEntriesSinceAsync(
            TimeEntryHistory.StartOfWeek(now, settings.WeekStart, zone),
            ctk
        );

        return load == _generation ? entries : null;
    }
}
