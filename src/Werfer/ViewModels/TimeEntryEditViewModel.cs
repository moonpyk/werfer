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
    /// <summary>
    ///     Fills the form from a finished entry, in local time: its times to the minute, see
    ///     <see cref="TimeEntryHistory.EditedInterval" />. The page saves and closes it through the given callbacks.
    /// </summary>
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

        // Cancel would only close the sheet, not stop the update already sent
        SaveCommand.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SaveCommand.IsRunning))
            {
                CancelCommand.NotifyCanExecuteChanged();
            }
        };

        var start = entry.TimeInterval!.Start.ToLocalTime();
        var end = entry.TimeInterval.End!.Value.ToLocalTime();

        // To the minute, as typed; EditedInterval keeps the seconds of an unchanged time
        Day = start.Date;
        Start = TimeEntryHistory.FormatTimeOfDay(TimeOnly.FromDateTime(start.DateTime));
        End = TimeEntryHistory.FormatTimeOfDay(TimeOnly.FromDateTime(end.DateTime));
        Description = entry.Description;
        SelectedChoice = choice;
    }

    /// <summary>
    ///     The entry edited, as it was when the form opened.
    /// </summary>
    public TimeEntry Entry { get; }

    private Func<TimeEntryEditViewModel, Task> SaveAction { get; }
    private Action CancelAction { get; }

    /// <summary>
    ///     Feeds the project box, the timer bar's own.
    /// </summary>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>> ProjectChoicesPopulator { get; }

    /// <summary>
    ///     The entry's day, that of its start; null while the picker is cleared.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Interval),
        nameof(Duration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial DateTime? Day { get; set; }

    /// <summary>
    ///     The start time as typed, see <see cref="TimeEntryHistory.TryParseTimeOfDay" />.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Interval),
        nameof(Duration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Start { get; set; }

    /// <summary>
    ///     The end time as typed; before the start, it falls on the next day.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Interval),
        nameof(Duration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? End { get; set; }

    /// <summary>
    ///     The entry's description, multiline.
    /// </summary>
    [ObservableProperty]
    public partial string? Description { get; set; }

    /// <summary>
    ///     Null keeps the entry's project and task: an archived one isn't among the choices
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBrush))]
    public partial ProjectTaskViewModel? SelectedChoice { get; set; }

    /// <summary>
    ///     The selected project's color, for the box's dot; flattened, a binding to SelectedChoice.Brush logs a warning without a choice.
    /// </summary>
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

    /// <summary>
    ///     Whether the times read, see <see cref="Interval" />.
    /// </summary>
    public bool CanSave => Interval is not null;

    /// <summary>
    ///     The edited duration, hh:mm:ss; empty while the times don't read.
    /// </summary>
    public string Duration => Interval is { } i ? TimeEntryHistory.Format(i.Duration) : string.Empty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        return SaveAction(this);
    }

    private bool CanCancel => !SaveCommand.IsRunning;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        CancelAction();
    }
}
