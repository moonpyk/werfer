using System.Diagnostics.CodeAnalysis;
using Werfer.Clockify.Models;

namespace Werfer.Services;

[SuppressMessage(
    "Performance",
    "CA1861",
    Justification = "Those are test data, not performance critical"
)]
public class TimeEntryHistoryTests
{
    // Fixed zone with DST, the machine's own may have none
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    [Theory]
    // Wednesday 2026-04-01, week starting Monday: back to Monday 30
    [InlineData("2026-04-01T10:00:00Z", DayOfWeek.Monday, "2026-03-30T00:00:00+02:00")]
    // On the first day itself: that day's midnight
    [InlineData("2026-03-30T10:00:00Z", DayOfWeek.Monday, "2026-03-30T00:00:00+02:00")]
    // Sunday, week starting Monday: six days back
    [InlineData("2026-04-05T10:00:00Z", DayOfWeek.Monday, "2026-03-30T00:00:00+02:00")]
    // Sunday 2026-03-29 is the DST switch: its midnight is still +01:00 while now is +02:00
    [InlineData("2026-04-01T10:00:00Z", DayOfWeek.Sunday, "2026-03-29T00:00:00+01:00")]
    // Monday 00:30 in Paris is still Sunday in UTC: the local day decides
    [InlineData("2026-03-29T22:30:00Z", DayOfWeek.Monday, "2026-03-30T00:00:00+02:00")]
    public void StartOfWeek_IsLocalMidnightOfFirstDay(string now, DayOfWeek firstDay, string expected)
    {
        var start = TimeEntryHistory.StartOfWeek(DateTimeOffset.Parse(now), firstDay, Paris);

        Assert.Equal(DateTimeOffset.Parse(expected), start);
        Assert.Equal(DateTimeOffset.Parse(expected).Offset, start.Offset);
    }

    [Theory]
    [InlineData("9:05", "09:05")]
    [InlineData("09:05", "09:05")]
    [InlineData("905", "09:05")]
    [InlineData("0905", "09:05")]
    [InlineData("9", "09:00")]
    [InlineData(" 23:59 ", "23:59")]
    public void TryParseTimeOfDay_LeadingZeroOptional(string typed, string expected)
    {
        Assert.True(TimeEntryHistory.TryParseTimeOfDay(typed, out var time));
        Assert.Equal(TimeOnly.Parse(expected), time);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("24:00")]
    [InlineData("9:5")]
    [InlineData("960")]
    [InlineData("12345")]
    [InlineData("9h05")]
    public void TryParseTimeOfDay_Invalid(string? typed)
    {
        Assert.False(TimeEntryHistory.TryParseTimeOfDay(typed, out _));
    }

    [Theory]
    // Started yesterday 23:30 in Paris, now 00:20: 00:15 is today's, not yesterday's
    [InlineData("2026-03-31T21:30:00Z", "2026-03-31T22:20:00Z", 0, 15, "2026-04-01T00:15:00+02:00")]
    // Same timer, 23:40: today's would be in the future, so yesterday's
    [InlineData("2026-03-31T21:30:00Z", "2026-03-31T22:20:00Z", 23, 40, "2026-03-31T23:40:00+02:00")]
    // Started today 09:00, now 10:00: an earlier time is today's
    [InlineData("2026-04-01T07:00:00Z", "2026-04-01T08:00:00Z", 8, 0, "2026-04-01T08:00:00+02:00")]
    // Same timer, 11:00 is in the future, and yesterday's is before the start day: refused
    [InlineData("2026-04-01T07:00:00Z", "2026-04-01T08:00:00Z", 11, 0, null)]
    // Unchanged, shown to the minute: the start itself, seconds included
    [InlineData("2026-04-01T07:05:42Z", "2026-04-01T08:00:00Z", 9, 5, "2026-04-01T07:05:42Z")]
    // Unchanged on a timer running for 25 h: still the start, not today's 09:00
    [InlineData("2026-03-31T07:00:00Z", "2026-04-01T08:00:00Z", 9, 0, "2026-03-31T07:00:00Z")]
    public void MovedStart_LatestOccurrenceSinceTheStartDay(
        string start,
        string now,
        int hour,
        int minute,
        string? expected
    )
    {
        var moved = TimeEntryHistory.MovedStart(
            DateTimeOffset.Parse(start),
            new TimeOnly(hour, minute),
            DateTimeOffset.Parse(now),
            Paris
        );

        Assert.Equal(expected is null ? null : DateTimeOffset.Parse(expected), moved);
    }

    [Theory]
    // 15:16:23 to 16:04:10: 15:17 to 16:04
    [InlineData(
        "2026-04-01T13:16:23Z",
        "2026-04-01T14:04:10Z",
        "15:17",
        "16:04"
    )]
    // On whole minutes, 15:16 to 16:04: neither the start nor the end, 15:17 to 16:03
    [InlineData(
        "2026-04-01T13:16:00Z",
        "2026-04-01T14:04:00Z",
        "15:17",
        "16:03"
    )]
    public void SplitBounds_WholeMinutesStrictlyInside(string start, string end, string first, string last)
    {
        var bounds = TimeEntryHistory.SplitBounds(
            DateTimeOffset.Parse(start),
            DateTimeOffset.Parse(end)
        );

        var firstDt = TimeOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(bounds.First, Paris).DateTime
        );
        Assert.Equal(
            first,
            firstDt.ToString("HH:mm")
        );
        var lastDt = TimeOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(bounds.Last, Paris).DateTime
        );
        Assert.Equal(
            last,
            lastDt.ToString("HH:mm"));
    }

    [Fact]
    public void SplitBounds_TooShort_LastBeforeFirst()
    {
        // 15:16:23 to 15:16:50: no whole minute inside
        var (first, last) = TimeEntryHistory.SplitBounds(
            DateTimeOffset.Parse("2026-04-01T13:16:23Z"),
            DateTimeOffset.Parse("2026-04-01T13:16:50Z")
        );

        Assert.True(last < first);
    }

    [Theory]
    // 15:16:23 to 16:04:10 in Paris
    [InlineData("2026-04-01T13:16:23Z", "2026-04-01T14:04:10Z", 15, 40, "2026-04-01T15:40:00+02:00")]
    // The start's own minute and the end's minute aren't inside
    [InlineData("2026-04-01T13:16:23Z", "2026-04-01T14:04:00Z", 15, 16, null)]
    [InlineData("2026-04-01T13:16:23Z", "2026-04-01T14:04:00Z", 16, 4, null)]
    // Outside the entry
    [InlineData("2026-04-01T13:16:23Z", "2026-04-01T14:04:10Z", 10, 0, null)]
    // 23:30 to 01:00, past midnight: 00:15 is the next day
    [InlineData("2026-04-01T21:30:00Z", "2026-04-01T23:00:00Z", 0, 15, "2026-04-02T00:15:00+02:00")]
    public void SplitTime_InsideTheEntryOnly(string start, string end, int hour, int minute, string? expected)
    {
        var at = TimeEntryHistory.SplitTime(
            DateTimeOffset.Parse(start),
            DateTimeOffset.Parse(end),
            new TimeOnly(hour, minute),
            Paris
        );

        Assert.Equal(expected is null ? null : DateTimeOffset.Parse(expected), at);
    }

    [Fact]
    public void EditedInterval_Unchanged_KeepsTheSeconds()
    {
        var original = new TimeInterval
        {
            Start = DateTimeOffset.Parse("2026-04-01T13:40:12Z"),
            End = DateTimeOffset.Parse("2026-04-01T14:29:34Z")
        };

        // As the form shows them in Paris: to the minute
        var (start, end) = TimeEntryHistory.EditedInterval(
            new DateOnly(2026, 4, 1),
            new TimeOnly(15, 40),
            new TimeOnly(16, 29),
            original,
            Paris
        );

        Assert.Equal(original.Start, start);
        Assert.Equal(original.End, end);
    }

    [Fact]
    public void EditedInterval_EndBeforeStart_EndsTheNextDay()
    {
        var original = new TimeInterval
        {
            Start = DateTimeOffset.Parse("2026-04-01T13:40:12Z"),
            End = DateTimeOffset.Parse("2026-04-01T14:29:34Z")
        };

        // Saturday 2026-03-28 22:00 to Sunday 03:30, across the DST switch: each end gets its own offset
        var (start, end) = TimeEntryHistory.EditedInterval(
            new DateOnly(2026, 3, 28),
            new TimeOnly(22, 0),
            new TimeOnly(3, 30),
            original,
            Paris
        );

        Assert.Equal(DateTimeOffset.Parse("2026-03-28T22:00:00+01:00"), start);
        Assert.Equal(DateTimeOffset.Parse("2026-03-29T03:30:00+02:00"), end);
    }

    [Fact]
    public void Group_ByLocalDayThenProjectTaskDescription_MostRecentFirst()
    {
        var entries = new[]
        {
            Entry("1", "p1", "t1", "JIRA-1", "2026-04-01T08:00:00Z", "2026-04-01T09:00:00Z"),
            Entry("2", "p1", "t1", "JIRA-1", "2026-04-01T13:00:00Z", "2026-04-01T13:30:00Z"),
            Entry("3", "p1", "t2", "JIRA-1", "2026-04-01T10:00:00Z", "2026-04-01T10:15:00Z"),
            // null and "" are both "no description"
            Entry("4", "p2", "t3", null, "2026-04-01T11:00:00Z", "2026-04-01T11:10:00Z"),
            Entry("5", "p2", "t3", "", "2026-04-01T12:00:00Z", "2026-04-01T12:05:00Z"),
            // 23:30 UTC is already the next day in Paris
            Entry("6", "p1", "t1", "JIRA-1", "2026-04-01T22:30:00Z", "2026-04-01T23:00:00Z"),
            // Running: left out
            Entry("7", "p1", "t1", "JIRA-1", "2026-04-02T07:00:00Z", null)
        };

        var days = entries.GroupBy(Paris);

        Assert.Collection(
            days,
            thursday =>
            {
                Assert.Equal(new DateOnly(2026, 4, 2), thursday.Date);
                Assert.Equal(new[] { "6" }, Ids(Assert.Single(thursday.Groups)));
            },
            wednesday =>
            {
                Assert.Equal(new DateOnly(2026, 4, 1), wednesday.Date);
                Assert.Collection(
                    wednesday.Groups,
                    g =>
                    {
                        // Most recent entry (13:00) first, inside the group too
                        Assert.Equal(new[] { "2", "1" }, Ids(g));
                        Assert.Equal(TimeSpan.FromMinutes(90), g.Duration);
                    },
                    g =>
                    {
                        Assert.Null(g.Description);
                        Assert.Equal(new[] { "5", "4" }, Ids(g));
                    },
                    g => Assert.Equal(new[] { "3" }, Ids(g))
                );
                Assert.Equal(TimeSpan.FromMinutes(90 + 15 + 15), wednesday.Duration);
            }
        );
    }

    [Fact]
    public void Sum_IgnoresRunningEntry()
    {
        var total = TimeEntryExtensions.Sum([
            Entry("1", "p", "t", null, "2026-04-01T08:00:00Z", "2026-04-01T08:20:00Z"),
            Entry("2", "p", "t", null, "2026-04-01T09:00:00Z", null)
        ]);

        Assert.Equal(TimeSpan.FromMinutes(20), total);
    }

    [Fact]
    public void Format_KeepsHoursPast24()
    {
        Assert.Equal("29:39:57", TimeEntryHistory.Format(new TimeSpan(1, 5, 39, 57)));
        Assert.Equal("00:00:00", TimeEntryHistory.Format(TimeSpan.Zero));
    }

    private static string[] Ids(TimeEntryGroup group)
    {
        return [.. group.Entries.Select(e => e.Id)];
    }

    private static TimeEntry Entry(
        string id,
        string project,
        string task,
        string? description,
        string start,
        string? end
    )
    {
        return new TimeEntry
        {
            Id = id,
            ProjectId = project,
            TaskId = task,
            Description = description,
            TimeInterval = new TimeInterval
            {
                Start = DateTimeOffset.Parse(start),
                End = end is null ? null : DateTimeOffset.Parse(end)
            }
        };
    }
}
