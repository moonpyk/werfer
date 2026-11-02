using Avalonia.Media;
using Werfer.Clockify.Models;

namespace Werfer.ViewModels;

public record ProjectTaskChoice(
    Project Project,
    ProjectTask Task
)
{
    /// <summary>
    ///     The project's color as set in Clockify; the view makes it readable as text, see ReadableTextBrushConverter
    /// </summary>
    public Color Color { get; } = Color.TryParse(Project.Color, out var color)
        ? color
        : Colors.Gray;

    /// <summary>
    ///     Kept as is for the dot
    /// </summary>
    public IBrush Brush => field ??= new SolidColorBrush(Color);

    /// <summary>
    ///     Text shown and matched by the autocomplete
    /// </summary>
    public override string ToString()
    {
        return $"{Project.Name} / {Task.Name}";
    }
}
