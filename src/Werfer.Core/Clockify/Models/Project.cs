using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Models;

[PublicAPI]
public class Project
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("clientName")]
    public string? ClientName { get; set; }

    // Only filled when requested with hydrated=true
    [JsonPropertyName("tasks")]
    public List<ProjectTask>? Tasks { get; set; }
}
