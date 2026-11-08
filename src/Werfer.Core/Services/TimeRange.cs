namespace Werfer.Services;

/// <summary>
///     A finished span of time, the start and end of an entry.
/// </summary>
public record TimeRange(
    DateTimeOffset Start, 
    DateTimeOffset End
)
{
    public TimeSpan Duration => End - Start;
}
