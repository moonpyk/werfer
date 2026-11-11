using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     One line of the history: entries of a day sharing project, task and description. Its actions go through the
///     timer page, so the card binds them directly.
/// </summary>
public partial class TimeEntryGroupViewModel(
    TimeEntryGroup group,
    ProjectTaskViewModel? choice,
    TimerViewModel timer
) : TimeEntryItemViewModelBase(timer)
{
    /// <summary>
    ///     The line's project and task; null when no longer startable (archived, done): it can't be continued.
    /// </summary>
    public override ProjectTaskViewModel? Choice { get; } = choice;

    /// <summary>
    ///     Whether the line can be continued, see <see cref="Choice" />.
    /// </summary>
    public bool CanContinue => Choice is not null;

    /// <summary>
    ///     The project's color, for the line's dot; null without a choice.
    /// </summary>
    public IBrush? Brush => Choice?.Brush;

    /// <summary>
    ///     The project's color, made readable as the project's name; null without a choice.
    /// </summary>
    public Color? Color => Choice?.Color;

    /// <summary>
    ///     The entries' shared description, null without one.
    /// </summary>
    public string? Description { get; } = group.Description;

    /// <summary>
    ///     The description shown, a placeholder without one.
    /// </summary>
    public string DisplayDescription => Description ?? "(no description)";

    /// <summary>
    ///     Greys the placeholder shown without a description.
    /// </summary>
    public double DescriptionOpacity => Description is null ? 0.6 : 1;

    /// <summary>
    ///     The project's name, a placeholder without a choice.
    /// </summary>
    public string ProjectName => Choice?.Project.Name ?? "(unknown project)";

    /// <summary>
    ///     The task's name, null without a choice.
    /// </summary>
    public string? TaskName => Choice?.Task.Name;

    /// <summary>
    ///     The project's client, null without one.
    /// </summary>
    public string? ClientName => Choice?.Project.ClientName;

    /// <summary>
    ///     The total of the line's entries, hh:mm:ss.
    /// </summary>
    public string Duration { get; } = TimeEntryHistory.Format(group.Duration);

    /// <summary>
    ///     From the first start to the last end of the group's entries
    /// </summary>
    public string Range { get; } = FormatRange(
        group.Entries.Min(e => e.TimeInterval!.Start),
        group.Entries.Max(e => e.TimeInterval!.End!.Value)
    );

    /// <summary>
    ///     How many entries the line groups.
    /// </summary>
    public int Count => TimeEntries.Count;

    // Kept rather than capturing the constructor parameter, which the initializers above already read
    protected override IReadOnlyList<TimeEntry> TimeEntries { get; } = group.Entries;

    /// <summary>
    ///     Whether the line groups several entries: they can be shown, see <see cref="Entries" />.
    /// </summary>
    public bool IsMultiple => Count > 1;

    /// <summary>
    ///     Shown when the group is expanded, most recent first like the groups. Built on first read: lines point back
    ///     to their group, which an initializer can't reference
    /// </summary>
    public List<TimeEntryLineViewModel> Entries => field ??=
    [
        .. TimeEntries.Select(e => new TimeEntryLineViewModel(
            this,
            Timer,
            e,
            FormatRange(e.TimeInterval!.Start, e.TimeInterval.End!.Value),
            TimeEntryHistory.Format(TimeEntryExtensions.Sum([e]))
        ))
    ];

    /// <summary>
    ///     Starts a new entry with this line's project, task and description
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync()
    {
        return Timer.ContinueAsync(this);
    }

    private static string FormatRange(DateTimeOffset start, DateTimeOffset end)
    {
        return $"{start.ToLocalTime():HH:mm} - {end.ToLocalTime():HH:mm}";
    }
}
