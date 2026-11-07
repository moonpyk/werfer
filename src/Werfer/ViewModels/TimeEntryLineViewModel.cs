namespace Werfer.ViewModels;

/// <summary>
///     An entry of an expanded group; continuing it continues its group, they share project, task and description
/// </summary>
public class TimeEntryLineViewModel(
    TimeEntryGroupViewModel group,
    TimerViewModel timer,
    string id,
    string range,
    string duration
) : TimeEntryItemViewModelBase(timer)
{
    public TimeEntryGroupViewModel Group { get; } = group;

    protected override string[] EntryIds { get; } = [id];

    public string Range { get; } = range;

    public string Duration { get; } = duration;
}
