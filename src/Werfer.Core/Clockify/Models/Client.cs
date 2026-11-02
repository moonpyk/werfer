using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Models;

[PublicAPI]
public class Client
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
