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
