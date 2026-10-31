using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SlClockify.Core.Clockify.Models;

[PublicAPI]
public class ProjectTask
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
