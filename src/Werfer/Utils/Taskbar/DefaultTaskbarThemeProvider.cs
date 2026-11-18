using Avalonia.Controls;
using Avalonia.Platform;

namespace Werfer.Utils.Taskbar;

/// <summary>
///     For the platforms whose taskbar theme can't be read: always dark, the most common panel color, and never changes
/// </summary>
public class DefaultTaskbarThemeProvider : ITaskbarThemeProvider
{
    private DefaultTaskbarThemeProvider()
    {
    }

    /// <summary>
    ///     The only instance: it holds no state, registered as the taskbar theme provider outside Windows
    /// </summary>
    public static ITaskbarThemeProvider Instance { get; } = new DefaultTaskbarThemeProvider();

    /// <inheritdoc />
    public PlatformThemeVariant Theme => PlatformThemeVariant.Dark;

    /// <inheritdoc />
    public void OnThemeChanged(TopLevel window, Action changed)
    {
    }
}
