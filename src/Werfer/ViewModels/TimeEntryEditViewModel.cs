using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     The edit form of one finished entry, in local time.
/// </summary>
public partial class TimeEntryEditViewModel : ObservableObject
{
    public TimeEntryEditViewModel(
        TimeEntry entry,
        ProjectTaskViewModel? choice,
        Func<string?, CancellationToken, Task<IEnumerable<object>>> projectChoicesPopulator,
        Func<TimeEntryEditViewModel, Task> save,
        Action cancel
    )
    {
        Entry = entry;
        ProjectChoicesPopulator = projectChoicesPopulator;
        SaveAction = save;
        CancelAction = cancel;

        var start = entry.TimeInterval!.Start.ToLocalTime();
        var end = entry.TimeInterval.End!.Value.ToLocalTime();

        // To the minute, as typed; EditedInterval keeps the seconds of an unchanged time
        Day = start.Date;
        Start = TimeEntryHistory.FormatTimeOfDay(TimeOnly.FromDateTime(start.DateTime));
        End = TimeEntryHistory.FormatTimeOfDay(TimeOnly.FromDateTime(end.DateTime));
        Description = entry.Description;
        SelectedChoice = choice;
    }

    public TimeEntry Entry { get; }

    private Func<TimeEntryEditViewModel, Task> SaveAction { get; }
    private Action CancelAction { get; }

    public Func<string?, CancellationToken, Task<IEnumerable<object>>> ProjectChoicesPopulator { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Interval),
        nameof(Duration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial DateTime? Day { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Interval),
        nameof(Duration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Start { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Interval),
        nameof(Duration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? End { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    /// <summary>
    ///     Null keeps the entry's project and task: an archived one isn't among the choices
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBrush))]
    public partial ProjectTaskViewModel? SelectedChoice { get; set; }

    // Flattened, see TimerViewModel.SelectedBrush
    public IBrush? SelectedBrush => SelectedChoice?.Brush;

    /// <summary>
    ///     Null while the date is cleared or a time is invalid, see <see cref="CanSave" />
    /// </summary>
    public TimeRange? Interval =>
        Day is { } day
        && TimeEntryHistory.TryParseTimeOfDay(Start, out var start)
        && TimeEntryHistory.TryParseTimeOfDay(End, out var end)
            ? TimeEntryHistory.EditedInterval(
                DateOnly.FromDateTime(day),
                start,
                end,
                Entry.TimeInterval!,
                TimeZoneInfo.Local
            )
            : null;

    public bool CanSave => Interval is not null;

    public string Duration => Interval is { } i ? TimeEntryHistory.Format(i.Duration) : string.Empty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        return SaveAction(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelAction();
    }
}
