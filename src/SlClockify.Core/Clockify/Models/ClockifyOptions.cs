using JetBrains.Annotations;

namespace SlClockify.Core.Clockify.Models;

[PublicAPI]
public class ClockifyOptions
{
    public const string DefaultBaseUrl = "https://api.clockify.me/api/v1";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
