using Werfer.Clockify.Models;

namespace Werfer.ViewModels;

/// <summary>
///     An entry of an expanded group; continuing it continues its group, they share project, task and description
/// </summary>
public class TimeEntryLineViewModel(
    TimeEntryGroupViewModel group,
    TimerViewModel timer,
    TimeEntry entry,
    string range,
    string duration
) : TimeEntryItemViewModelBase(timer)
{
    public TimeEntryGroupViewModel Group { get; } = group;

    public override ProjectTaskViewModel? Choice => Group.Choice;

    protected override IReadOnlyList<TimeEntry> TimeEntries { get; } = [entry];

    public string Range { get; } = range;

    public string Duration { get; } = duration;
}
