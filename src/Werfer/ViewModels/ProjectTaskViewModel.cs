using Avalonia.Media;
using Werfer.Clockify.ViewModels;

namespace Werfer.ViewModels;

/// <summary>
///     A project and task an entry can be started on, with the project's color for display.
/// </summary>
public record ProjectTaskViewModel : ProjectTaskChoice
{
    /// <summary>
    ///     Wraps a choice, its project's color parsed once; gray when Clockify's isn't a valid color.
    /// </summary>
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
