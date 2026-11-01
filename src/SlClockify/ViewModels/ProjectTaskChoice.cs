using Avalonia.Media;
using SlClockify.Core.Clockify.Models;

namespace SlClockify.ViewModels;

public record ProjectTaskChoice(
    Project Project,
    ProjectTask Task
)
{
    public IBrush Brush { get; } = new SolidColorBrush(
        Color.TryParse(Project.Color, out var color) ? color : Colors.Gray
    );

    // Text shown and matched by the autocomplete
    public override string ToString()
    {
        return $"{Project.Name} / {Task.Name}";
    }
}
