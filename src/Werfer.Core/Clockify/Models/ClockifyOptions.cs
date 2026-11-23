using JetBrains.Annotations;

namespace Werfer.Clockify.Models;

[PublicAPI]
public class ClockifyOptions
{
    public const string DefaultBaseUrl = "https://api.clockify.me/api/v1";

    /// <summary>
    ///     API key for accessing the Clockify API.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     Base URL for the Clockify API.
    /// </summary>
    /// <remarks>
    ///     Not used for anything yet, but here to stay, a Clockify compatible API may appear someday
    /// </remarks>
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
