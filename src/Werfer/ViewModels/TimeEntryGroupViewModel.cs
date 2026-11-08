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
    // Null: it can't be continued
    public override ProjectTaskViewModel? Choice { get; } = choice;

    public bool CanContinue => Choice is not null;

    public IBrush? Brush => Choice?.Brush;

    public Color? Color => Choice?.Color;

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

    public int Count => TimeEntries.Count;

    // Kept rather than capturing the constructor parameter, which the initializers above already read
    protected override IReadOnlyList<TimeEntry> TimeEntries { get; } = group.Entries;

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
