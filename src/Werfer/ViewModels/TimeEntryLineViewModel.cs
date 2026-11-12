using Werfer.Clockify.Models;

namespace Werfer.ViewModels;

/// <summary>
///     One entry of an expanded group. Continuing it continues the group: all its entries have the same project, task
///     and description
/// </summary>
public class TimeEntryLineViewModel(
    TimeEntryGroupViewModel group,
    TimerViewModel timer,
    TimeEntry entry,
    string range,
    string duration
) : TimeEntryItemViewModelBase(timer)
{
    /// <summary>
    ///     The group the entry belongs to. Continuing the entry continues this group.
    /// </summary>
    public TimeEntryGroupViewModel Group { get; } = group;

    /// <summary>
    ///     The group's project and task, shared by its entries.
    /// </summary>
    public override ProjectTaskViewModel? Choice => Group.Choice;

    /// <summary>
    ///     The entry's start and end times, HH:mm - HH:mm.
    /// </summary>
    public string Range { get; } = range;

    /// <summary>
    ///     The entry's duration, hh:mm:ss.
    /// </summary>
    public string Duration { get; } = duration;

    protected override IReadOnlyList<TimeEntry> TimeEntries { get; } = [entry];
}
