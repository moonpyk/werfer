using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SlClockify.Core.Requests;

[PublicAPI]
public class StopTimeEntryRequest
{
    [JsonPropertyName("end")] public DateTimeOffset End { get; set; }
}
