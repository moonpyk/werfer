using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SlClockify.Core.Clockify.Models;

[PublicAPI]
public class TimeInterval
{
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; set; }

    [JsonPropertyName("end")]
    public DateTimeOffset? End { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
}
