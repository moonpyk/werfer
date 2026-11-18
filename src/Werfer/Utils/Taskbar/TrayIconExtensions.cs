using Avalonia.Controls;
using Avalonia.Platform;

namespace Werfer.Utils.Taskbar;

public static class TrayIconExtensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="TrayIcon" /> type
    /// </summary>
    extension(TrayIcon self)
    {
        /// <summary>
        ///     Shows the icon that stands out on the taskbar, black on a light one and white on a dark one like the
        ///     system icons next to it, now and on every theme switch <paramref name="window" /> receives
        /// </summary>
        public void FollowTaskbarTheme(ITaskbarThemeProvider taskbar, TopLevel window)
        {
            void UpdateIcon()
            {
                var name = taskbar.Theme == PlatformThemeVariant.Light
                    ? "Black"
                    : "White";

                self.Icon = new WindowIcon(AssetLoader.Open(
                    new Uri($"avares://Werfer/Assets/Tray/{name}.ico")
                ));
            }

            UpdateIcon();
            taskbar.OnThemeChanged(window, UpdateIcon);
        }
    }
}
