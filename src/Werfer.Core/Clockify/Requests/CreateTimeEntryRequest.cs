using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Requests;

[PublicAPI]
public class CreateTimeEntryRequest
{
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; set; }

    [JsonPropertyName("end")]
    public DateTimeOffset? End { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }

    [JsonPropertyName("billable")]
    public bool? Billable { get; set; }

    [JsonPropertyName("tagIds")]
    public List<string>? TagIds { get; set; }

    [JsonPropertyName("customFields")]
    public List<CustomFieldEntryRequest>? CustomFields { get; set; }
}
