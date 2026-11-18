using Avalonia.Controls;
using Avalonia.Platform;

namespace Werfer.Utils.Taskbar;

/// <summary>
///     The taskbar theme, to pick a tray icon that stands out on it
/// </summary>
public interface ITaskbarThemeProvider
{
    /// <summary>
    ///     The taskbar theme, read again on each call
    /// </summary>
    PlatformThemeVariant Theme { get; }

    /// <summary>
    ///     Calls <paramref name="changed" /> when the user switches theme, through <paramref name="window" />
    /// </summary>
    void OnThemeChanged(TopLevel window, Action changed);
}
