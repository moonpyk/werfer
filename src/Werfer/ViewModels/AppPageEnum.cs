namespace Werfer.ViewModels;

/// <summary>
///     The pages the window shows. A navigation item or a More menu entry carries its page in its Tag.
/// </summary>
public enum AppPageEnum
{
    /// <summary>
    ///     The timer bar and the week's history, the main page.
    /// </summary>
    Timer,

    /// <summary>
    ///     The API key and preferences, opened from the More menu.
    /// </summary>
    Settings,

    /// <summary>
    ///     The version and the keyboard shortcuts, opened from the More menu.
    /// </summary>
    About
}
