using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Werfer.Utils;

namespace Werfer.Converters;

/// <summary>
///     Turns a color into a text brush readable on the background of the control drawing it, see
///     <see cref="ReadableColor.EnsureContrast" />. Takes three bound values: the color, the control itself, then the
///     control's
///     ActualThemeVariant, which isn't read: it is only there so the binding runs again when the theme changes.
/// </summary>
public class ReadableTextBrushConverter : IMultiValueConverter
{
    // FluentAvalonia's window background
    private const string BackgroundResourceKey = "SolidBackgroundFillColorBase";

    public static ReadableTextBrushConverter Instance { get; } = new();

    // Cache shared by every project. Few colors times few themes, so it stays small. Only used on the UI thread
    private Dictionary<(Color Text, Color Background), IBrush> Brushes { get; } = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [Color color, Control control, ..])
        {
            // Nothing to color yet (e.g. no selection): keep the theme's own text color
            return AvaloniaProperty.UnsetValue;
        }

        var variant = control.ActualThemeVariant;
        var background = control.TryFindResource(BackgroundResourceKey, variant, out var resource) &&
                         resource is Color found
            ? found
            : variant == ThemeVariant.Dark
                ? Colors.Black
                : Colors.White;

        // ReSharper disable once InvertIf : more readable
        if (!Brushes.TryGetValue((color, background), out var brush))
        {
            brush = new SolidColorBrush(ReadableColor.EnsureContrast(color, background));
            Brushes[(color, background)] = brush;
        }

        return brush;
    }
}
