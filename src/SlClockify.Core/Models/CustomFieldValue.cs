using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SlClockify.Core.Models;

[PublicAPI]
public class CustomFieldValue
{
    [JsonPropertyName("customFieldId")]
    public string CustomFieldId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}
