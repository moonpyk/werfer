namespace Werfer;

/// <summary>
///     Provides a contract for managing application settings such as configuration values,
///     with functionality to persist those settings.
/// </summary>
public interface IApplicationSettings
{
    /// <summary>
    ///     Gets or sets the API key used to authenticate requests with Clockify
    /// </summary>
    string? ApiKey { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of project choices that can be displayed or selected
    ///     in the timer control.
    /// </summary>
    int MaxProjectChoices { get; set; }

    /// <summary>
    ///     Saves the current application settings to a persistent storage medium.
    /// </summary>
    void Save();
}
