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
    ///     Gets or sets whether the application window should always be on top
    /// </summary>
    bool Topmost { get; set; }

    /// <summary>
    ///     Gets or sets whether closing the window asks for confirmation first
    /// </summary>
    bool ConfirmClose { get; set; }

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
    ///     Gets or sets the first day of the week the history totals start from; the current culture's until set
    /// </summary>
    DayOfWeek WeekStart { get; set; }

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
