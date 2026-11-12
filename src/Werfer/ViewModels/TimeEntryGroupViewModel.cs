using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     One line of the history: the entries of one day with the same project, task and description. Its commands
///     call the timer page, so the card can bind to them directly.
/// </summary>
public partial class TimeEntryGroupViewModel(
    TimeEntryGroup group,
    ProjectTaskViewModel? choice,
    TimerViewModel timer
) : TimeEntryItemViewModelBase(timer)
{
    /// <summary>
    ///     The line's project and task. Null when they can't be started anymore (archived, done): the line can't be
    ///     continued then.
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
    ///     The project's color, for the project's name (the view adjusts it to stay readable). Null without a choice.
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
    ///     Opacity that greys out the placeholder shown when there is no description.
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
    ///     Time range of the group, from its first start to its last end
    /// </summary>
    public string Range { get; } = FormatRange(
        group.Entries.Min(e => e.TimeInterval!.Start),
        group.Entries.Max(e => e.TimeInterval!.End!.Value)
    );

    /// <summary>
    ///     How many entries the line groups.
    /// </summary>
    public int Count => TimeEntries.Count;

    // Stored in a property rather than captured from the constructor parameter, which the initializers above
    // already read
    protected override IReadOnlyList<TimeEntry> TimeEntries { get; } = group.Entries;

    /// <summary>
    ///     True when the line has several entries, which the user can expand, see <see cref="Entries" />.
    /// </summary>
    public bool IsMultiple => Count > 1;

    /// <summary>
    ///     The entries listed when the group is expanded, most recent first like the groups. Built on first read,
    ///     because each line points back to its group, and an initializer can't use this
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
