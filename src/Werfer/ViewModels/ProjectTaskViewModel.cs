using Avalonia.Media;
using Werfer.Clockify.ViewModels;

namespace Werfer.ViewModels;

public record ProjectTaskViewModel : ProjectTaskChoice
{
    public ProjectTaskViewModel(
        ProjectTaskChoice task
    ) : base(task.Project, task.Task)
    {
        Color = Color.TryParse(Project.Color, out var color)
            ? color
            : Colors.Gray;
    }

    /// <summary>
    ///     The project's color as set in Clockify; the view makes it readable as text, see ReadableTextBrushConverter
    /// </summary>
    public Color Color { get; }

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
