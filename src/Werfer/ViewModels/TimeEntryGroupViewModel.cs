using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     One line of the history: entries of a day sharing project, task and description. Its actions go through the
///     timer page, so the card binds them directly.
/// </summary>
public class TimeEntryGroupViewModel(
    TimeEntryGroup group,
    ProjectTaskViewModel? choice,
    TimerViewModel timer
)
{
    /// <summary>
    ///     Null when the project or task isn't startable anymore (archived, done): it can't be continued
    /// </summary>
    public ProjectTaskViewModel? Choice { get; } = choice;

    public bool CanContinue => Choice is not null;

    public string? Description { get; } = group.Description;

    public string DisplayDescription => Description ?? "(no description)";

    // Greyed placeholder
    public double DescriptionOpacity => Description is null ? 0.6 : 1;

    public string ProjectName => Choice?.Project.Name ?? "(unknown project)";

    public string? TaskName => Choice?.Task.Name;

    public string? ClientName => Choice?.Project.ClientName;

    public string Duration { get; } = TimeEntryHistory.Format(group.Duration);

    /// <summary>
    ///     From the first start to the last end of the group's entries
    /// </summary>
    public string Range { get; } = FormatRange(
        group.Entries.Min(e => e.TimeInterval!.Start),
        group.Entries.Max(e => e.TimeInterval!.End!.Value)
    );

    /// <summary>
    ///     What the delete menu removes: every entry of the group
    /// </summary>
    public string[] EntryIds { get; } = [.. group.Entries.Select(e => e.Id)];

    public int Count => Source.Count;

    // Kept rather than capturing the constructor parameter, which the initializers above already read
    private IReadOnlyList<TimeEntry> Source { get; } = group.Entries;

    public bool IsMultiple => Count > 1;

    /// <summary>
    ///     Shown when the group is expanded, most recent first like the groups. Built on first read: lines point back
    ///     to their group, which an initializer can't reference
    /// </summary>
    public List<TimeEntryLineViewModel> Entries => field ??=
    [
        .. Source.Select(e => new TimeEntryLineViewModel(
            this,
            e.Id,
            FormatRange(e.TimeInterval!.Start, e.TimeInterval.End!.Value),
            TimeEntryHistory.Format(TimeEntryExtensions.Sum([e]))
        ))
    ];

    /// <summary>
    ///     Starts a new entry with this line's project, task and description
    /// </summary>
    public Task ContinueAsync()
    {
        return timer.ContinueAsync(this);
    }

    /// <summary>
    ///     Deletes some or all of the group's entries, see <see cref="EntryIds" />
    /// </summary>
    public Task DeleteAsync(IReadOnlyList<string> ids)
    {
        return timer.DeleteAsync(ids);
    }

    private static string FormatRange(DateTimeOffset start, DateTimeOffset end)
    {
        return $"{start.ToLocalTime():HH:mm} - {end.ToLocalTime():HH:mm}";
    }
}

/// <summary>
///     An entry of an expanded group; continuing it continues its group, they share project, task and description
/// </summary>
public record TimeEntryLineViewModel(
    TimeEntryGroupViewModel Group, 
    string Id, 
    string Range, 
    string Duration
);
