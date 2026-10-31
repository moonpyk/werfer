using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SlClockify.Core.Models;

[PublicAPI]
public class Membership
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("targetId")]
    public string? TargetId { get; set; }

    [JsonPropertyName("membershipStatus")]
    public string? MembershipStatus { get; set; }

    [JsonPropertyName("membershipType")]
    public string? MembershipType { get; set; }
}
