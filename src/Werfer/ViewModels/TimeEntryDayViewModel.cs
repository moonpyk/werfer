using System.Globalization;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     A day of the history with its total, finished entries only.
/// </summary>
public class TimeEntryDayViewModel(
    TimeEntryDay day,
    DateOnly today,
    IReadOnlyList<ProjectTaskViewModel> choices,
    TimerViewModel timer
)
{
    public string Title { get; } = day.Date == today ? "Today"
        : day.Date == today.AddDays(-1) ? "Yesterday"
        : day.Date.ToString("dddd d MMM", CultureInfo.CurrentCulture);

    public string Total { get; } = TimeEntryHistory.Format(day.Duration);

    public List<TimeEntryGroupViewModel> Groups { get; } =
    [
        .. day.Groups.Select(g => new TimeEntryGroupViewModel(
            g,
            choices.FirstOrDefault(c => c.Task.Id == g.TaskId && c.Project.Id == g.ProjectId),
            timer
        ))
    ];
}
