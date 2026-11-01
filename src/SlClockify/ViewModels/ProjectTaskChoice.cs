using Avalonia.Media;
using SlClockify.Core.Clockify.Models;

namespace SlClockify.ViewModels;

public record ProjectTaskChoice(
    Project Project,
    ProjectTask Task
)
{
    // The project's color as set in Clockify; the view makes it readable as text, see ReadableTextBrushConverter
    public Color Color { get; } = Color.TryParse(Project.Color, out var color) ? color : Colors.Gray;

    // Kept as is for the dot
    public IBrush Brush => field ??= new SolidColorBrush(Color);

    // Text shown and matched by the autocomplete
    public override string ToString()
    {
        return $"{Project.Name} / {Task.Name}";
    }
}
