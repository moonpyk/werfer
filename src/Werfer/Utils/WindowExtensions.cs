using Avalonia;
using Avalonia.Controls;

namespace Werfer.Utils;

public static class WindowExtensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="Window" /> type to enhance its functionality
    /// </summary>
    extension<T>(T self) where T : Window
    {
        /// <summary>
        ///     Restores the width of the window using the value specified in the given application settings.
        /// </summary>
        public T RestoreWidth(IApplicationSettings settings)
        {
            if (settings.WindowWidth is { } width)
            {
                self.Width = width;
            }

            return self;
        }

        /// <summary>
        ///     Restores the position of the window using the coordinates specified in the given application settings.
        /// </summary>
        public T RestorePosition(IApplicationSettings settings)
        {
            if (settings is not { WindowX: { } x, WindowY: { } y })
            {
                return self;
            }

            var position = new PixelPoint(x, y);

            if (self.Screens.ScreenFromPoint(position) is null)
            {
                return self;
            }

            self.WindowStartupLocation = WindowStartupLocation.Manual;
            self.Position = position;

            return self;
        }
    }
}
