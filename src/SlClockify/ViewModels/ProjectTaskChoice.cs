using Avalonia.Media;
using SlClockify.Core.Models;

namespace SlClockify.ViewModels;

public record ProjectTaskChoice(Project Project, ProjectTask Task)
{
    public IBrush Brush { get; } = new SolidColorBrush(
        Color.TryParse(Project.Color, out var color) ? color : Colors.Gray
    );

    // Text shown and matched by the autocomplete
    public override string ToString() => $"{Project.Name} / {Task.Name}";
}
