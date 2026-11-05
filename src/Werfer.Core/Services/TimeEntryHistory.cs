using Werfer.Clockify.Models;

namespace Werfer.Services;

/// <summary>
///     Finished entries sharing project, task and description on the same day, shown as one line.
/// </summary>
public record TimeEntryGroup(
    string? ProjectId,
    string? TaskId,
    string? Description,
    IReadOnlyList<TimeEntry> Entries
)
{
    public TimeSpan Duration => Entries.Sum();
}

/// <summary>
///     The groups of one local day, most recent first.
/// </summary>
public record TimeEntryDay(
    DateOnly Date,
    IReadOnlyList<TimeEntryGroup> Groups
)
{
    /// <summary>
    ///     Gets the total duration of all time entry groups for the day.
    /// </summary>
    public TimeSpan Duration => Groups.Aggregate(
        TimeSpan.Zero,
        (total, g) => total + g.Duration
    );
}

/// <summary>
///     Week boundaries and grouping of finished time entries, in the user's time zone.
/// </summary>
public static class TimeEntryHistory
{
    /// <summary>
    ///     Midnight, in <paramref name="zone" />, of the last <paramref name="firstDay" /> on or before
    ///     <paramref name="now" />. Its offset is the one in effect that midnight, which may differ from now's (DST).
    /// </summary>
    public static DateTimeOffset StartOfWeek(
        DateTimeOffset now,
        DayOfWeek firstDay,
        TimeZoneInfo zone
    )
    {
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var date = local.Date.AddDays(-(((int)local.DayOfWeek - (int)firstDay + 7) % 7));

        return new DateTimeOffset(date, zone.GetUtcOffset(date));
    }

    /// <summary>
    ///     hh:mm:ss with hours past 24, a week total often is.
    /// </summary>
    public static string Format(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }
}

public static class TimeEntryExtensions
{
    extension(IEnumerable<TimeEntry> entries)
    {
        /// <summary>
        ///     Groups finished entries by local day, then by (project, task, description). The running entry is left out,
        ///     the timer bar shows it. Days and groups come most recent first.
        /// </summary>
        public List<TimeEntryDay> GroupBy(TimeZoneInfo zone)
        {
            // GroupBy keeps the order of first appearance, so this sort carries over to the days and to their groups
            var finished = entries
                .Where(e => e.TimeInterval is { End: not null })
                .OrderByDescending(e => e.TimeInterval!.Start);

            return
            [
                .. finished
                    .GroupBy(LocalDay)
                    .Select(day => new TimeEntryDay(
                        day.Key,
                        [
                            .. day
                                .GroupBy(e => e.WerferGroupingKey)
                                .Select(line => new TimeEntryGroup(
                                    line.Key.ProjectId,
                                    line.Key.TaskId,
                                    line.Key.Description,
                                    [.. line]
                                ))
                        ]
                    ))
            ];

            DateOnly LocalDay(TimeEntry e)
            {
                return DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(e.TimeInterval!.Start, zone).Date
                );
            }
        }

        /// <summary>
        ///     Total of the finished entries, a running one counts for nothing.
        /// </summary>
        public TimeSpan Sum()
        {
            return entries.Aggregate(
                TimeSpan.Zero,
                (total, e) => e.TimeInterval is
                {
                    End: { } end
                } i
                    ? total + (end - i.Start)
                    : total
            );
        }
    }
}
