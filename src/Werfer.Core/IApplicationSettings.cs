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
    ///     Gets or sets the maximum number of matches the project box lists.
    /// </summary>
    int MaxProjectChoices { get; set; }

    /// <summary>
    ///     Gets or sets whether the application window should always be on top
    /// </summary>
    bool Topmost { get; set; }

    /// <summary>
    ///     Gets or sets whether a timer started with no project picked uses the default project and task
    /// </summary>
    bool DefaultChoiceEnabled { get; set; }

    /// <summary>
    ///     Gets or sets the default project; null, with <see cref="DefaultTaskId" />, means the last used project and task
    /// </summary>
    string? DefaultProjectId { get; set; }

    /// <inheritdoc cref="DefaultProjectId" />
    string? DefaultTaskId { get; set; }

    /// <summary>
    ///     Gets or sets whether closing the window asks for confirmation first
    /// </summary>
    bool ConfirmClose { get; set; }

    /// <summary>
    ///     Gets or sets whether minimizing the window hides it in the notification area, behind a tray icon
    /// </summary>
    bool MinimizeToTray { get; set; }

    /// <summary>
    ///     Gets or sets the window position, in screen pixels, at the last close; null until it has been saved once
    /// </summary>
    int? WindowX { get; set; }

    /// <inheritdoc cref="WindowX" />
    int? WindowY { get; set; }

    /// <summary>
    ///     Gets or sets the window width, in device-independent pixels, at the last close; null until it has been saved
    ///     once
    /// </summary>
    double? WindowWidth { get; set; }

    /// <inheritdoc cref="WindowWidth" />
    double? WindowHeight { get; set; }

    /// <summary>
    ///     Gets or sets the first day of the history's week. Until the user picks one, the current culture's first day
    /// </summary>
    DayOfWeek WeekStart { get; set; }

    /// <summary>
    ///     Gets or sets the literal prefixes removed from a text pasted in the timer's description
    /// </summary>
    string[] StrippedDescriptionPrefixes { get; set; }

    /// <summary>
    ///     Attempts to persist the current application settings.
    /// </summary>
    /// <remarks>
    ///     Implementations should handle persistence failures without propagating them to the caller.
    /// </remarks>
    /// <return>
    ///     <c>true</c> if the settings were saved successfully; otherwise, <c>false</c>.
    /// </return>
    bool TrySave();
}
