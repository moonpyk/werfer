using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Models;

[PublicAPI]
public class Workspace
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("memberships")]
    public List<Membership>? Memberships { get; set; }
}
