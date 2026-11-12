using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Werfer.Clockify.Models;

[PublicAPI]
public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("activeWorkspace")]
    public string? ActiveWorkspace { get; set; }

    [JsonPropertyName("defaultWorkspace")]
    public string? DefaultWorkspace { get; set; }
    
    [JsonPropertyName("profilePicture")]
    public string? ProfilePicture { get; set; }
}
