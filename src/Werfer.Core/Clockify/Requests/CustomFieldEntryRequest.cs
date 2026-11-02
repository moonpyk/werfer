using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Requests;

[PublicAPI]
public class CustomFieldEntryRequest
{
    [JsonPropertyName("customFieldId")]
    public string CustomFieldId { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}
