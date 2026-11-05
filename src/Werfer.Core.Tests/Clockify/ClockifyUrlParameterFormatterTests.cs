namespace Werfer.Clockify;

public class ClockifyUrlParameterFormatterTests
{
    [Fact]
    public void Format_DateTimeOffset_IsSentInUtc()
    {
        // The week start, local midnight in Paris: sent with its offset or as local time, the query would be 2 h off
        var weekStart = new DateTimeOffset(
            2026, 
            3, 
            30, 
            0, 
            0, 
            0, 
            TimeSpan.FromHours(2)
        );

        var formatted = new ClockifyUrlParameterFormatter().Format(
            weekStart,
            typeof(ClockifyUrlParameterFormatterTests),
            typeof(DateTimeOffset)
        );

        Assert.Equal("2026-03-29T22:00:00Z", formatted);
    }
}
