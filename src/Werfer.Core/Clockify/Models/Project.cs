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

    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("workspaceId")]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName("billable")]
    public bool Billable { get; set; }

    [JsonPropertyName("memberships")]
    public List<Membership>? Memberships { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("clientName")]
    public string? ClientName { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("template")]
    public bool Template { get; set; }

    [JsonPropertyName("public")]
    public bool? Public { get; set; }

    // Only filled when requested with hydrated=true
    [JsonPropertyName("tasks")]
    public List<ProjectTask>? Tasks { get; set; }
}
