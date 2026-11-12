using System.Globalization;
using Werfer.Services;
using Werfer.Utils;

namespace Werfer.ViewModels;

/// <summary>
///     One day of the history, with its total. Finished entries only.
/// </summary>
public class TimeEntryDayViewModel(
    TimeEntryDay day,
    DateOnly today,
    IReadOnlyList<ProjectTaskViewModel> choices,
    TimerViewModel timer
)
{
    /// <summary>
    ///     "Today", "Yesterday", or the day and month in the current culture.
    /// </summary>
    public string Title { get; } = day.Date == today ? "Today"
        : day.Date == today.AddDays(-1) ? "Yesterday"
        : day.Date.ToString("dddd d MMM", CultureInfo.CurrentCulture).FirstCharToUpper();

    /// <summary>
    ///     The day's total as formatted string
    /// </summary>
    public string Total { get; } = TimeEntryHistory.Format(day.Duration);

    /// <summary>
    ///     The day's lines, most recent first. A line whose project or task can't be started anymore has a null Choice.
    /// </summary>
    public List<TimeEntryGroupViewModel> Groups { get; } =
    [
        .. day.Groups.Select(g => new TimeEntryGroupViewModel(
            g,
            choices.FirstOrDefault(c => c.Task.Id == g.TaskId && c.Project.Id == g.ProjectId),
            timer
        ))
    ];
}
