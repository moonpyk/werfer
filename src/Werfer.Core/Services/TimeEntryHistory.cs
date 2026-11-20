using System.Globalization;
using Werfer.Clockify.Models;

namespace Werfer.Services;

/// <summary>
///     Finished entries of one day with the same project, task and description, shown as one history line.
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
    ///     <paramref name="now" />. Its UTC offset is the one at that midnight, which can differ from
    ///     now's because of daylight saving time.
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
    ///     Start and end of an edited entry, from its day and two times of day in <paramref name="zone" />. An end before the
    ///     start means the next day. A time the user didn't change (shown to the minute) keeps <paramref name="original" />'s
    ///     seconds: re-saving an entry mustn't change its duration.
    /// </summary>
    public static TimeRange EditedInterval(
        DateOnly day,
        TimeOnly start,
        TimeOnly end,
        TimeInterval original,
        TimeZoneInfo zone
    )
    {
        return new TimeRange(
            EditedTime(day, start, original.Start, zone),
            // shortcut: an end before the start only means the next day, entries of 24 h or more can't be entered
            EditedTime(end < start ? day.AddDays(1) : day, end, original.End, zone)
        );
    }

    /// <summary>
    ///     A day and a time of day in <paramref name="zone" />. When it is <paramref name="kept" /> shown to the minute,
    ///     <paramref name="kept" /> itself is returned, seconds included.
    /// </summary>
    public static DateTimeOffset EditedTime(
        DateOnly date,
        TimeOnly time,
        DateTimeOffset? kept,
        TimeZoneInfo zone
    )
    {
        var local = date.ToDateTime(time);

        // ReSharper disable once InvertIf : Leads to code duplication
        if (kept is { } k)
        {
            var shown = TimeZoneInfo.ConvertTime(k, zone).DateTime;
            if (shown.AddTicks(-(shown.Ticks % TimeSpan.TicksPerMinute)) == local)
            {
                return k;
            }
        }

        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    /// <summary>
    ///     The whole minutes strictly inside an entry, where it can be split: the first one after its start and the last
    ///     one before its end. Last is before First when the entry is too short to split.
    /// </summary>
    public static (DateTimeOffset First, DateTimeOffset Last) SplitBounds(DateTimeOffset start, DateTimeOffset end)
    {
        var first = ToMinute(start).AddMinutes(1);
        var last = ToMinute(end.AddTicks(-1));
        return (first, last);

        static DateTimeOffset ToMinute(DateTimeOffset time)
        {
            return time.AddTicks(-(time.Ticks % TimeSpan.TicksPerMinute));
        }
    }

    /// <summary>
    ///     Where an entry is split at a time of day in <paramref name="zone" />: on the day it starts, or the next one
    ///     for an entry running past midnight. Null when that falls outside the entry, its start and end included.
    /// </summary>
    public static DateTimeOffset? SplitTime(
        DateTimeOffset start,
        DateTimeOffset end,
        TimeOnly time,
        TimeZoneInfo zone
    )
    {
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, zone).DateTime);
        var at = EditedTime(day, time, null, zone);

        // A time before the start is on the next day, with that day's own offset
        if (at <= start)
        {
            at = EditedTime(day.AddDays(1), time, null, zone);
        }

        return at < end ? at : null;
    }

    /// <summary>
    ///     A time of day as typed: "9:05", "09:05", "905", "0905", or "9" for 09:00.
    /// </summary>
    public static bool TryParseTimeOfDay(string? text, out TimeOnly time)
    {
        var typed = text?.Trim() ?? string.Empty;

        // Digits only: the last two are the minutes, if any
        if (typed.Length is >= 1 and <= 4 && typed.All(char.IsAsciiDigit))
        {
            typed = typed.Length <= 2 ? $"{typed}:00" : $"{typed[..^2]}:{typed[^2..]}";
        }

        return TimeOnly.TryParseExact(typed, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }

    /// <summary>
    ///     Formats a time as HH:mm, which <see cref="TryParseTimeOfDay" /> can read back.
    /// </summary>
    public static string FormatTimeOfDay(TimeOnly time)
    {
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Formats a duration as hh:mm:ss, with hours going past 24: a week's total often does.
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
        ///     Total duration of the finished entries. A running entry counts as zero.
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
