using Avalonia.Media;
using Werfer.Clockify.ViewModels;

namespace Werfer.ViewModels;

/// <summary>
///     A project and task an entry can be started on, with the project's color for display.
/// </summary>
public record ProjectTaskViewModel : ProjectTaskChoice
{
    /// <summary>
    ///     Wraps a choice and parses its project's color once. Gray when Clockify's color isn't valid.
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
    ///     The project's color as is, for the dot (not adjusted for contrast)
    /// </summary>
    public IBrush Brush => field ??= new SolidColorBrush(Color);

    /// <summary>
    ///     Text shown in the autocomplete list, and matched against what is typed
    /// </summary>
    public override string ToString()
    {
        return $"{Project.Name} / {Task.Name}";
    }
}
