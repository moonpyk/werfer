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
    ///     Fills the form from a finished entry, in local time, with its times cut to the minute (see
    ///     <see cref="TimeEntryHistory.EditedInterval" />). The timer page saves and closes the form through the given
    ///     callbacks.
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

        // Cancel is disabled while saving: it would only close the sheet, not stop the update already sent
        SaveCommand.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SaveCommand.IsRunning))
            {
                CancelCommand.NotifyCanExecuteChanged();
            }
        };

        var start = entry.TimeInterval!.Start.ToLocalTime();
        var end = entry.TimeInterval.End!.Value.ToLocalTime();

        // Shown to the minute, as the user types them. EditedInterval keeps the original seconds of a time left unchanged
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
    ///     Fills the project box; the same one as the timer bar's.
    /// </summary>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>> ProjectChoicesPopulator { get; }

    /// <summary>
    ///     The day the entry starts. Null while the date picker is cleared.
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
    ///     The end time as typed. An end before the start means the next day.
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
    ///     The project and task picked. Null keeps the entry's own, for example an archived project, which isn't in the list
    /// </summary>
    [ObservableProperty]
    public partial ProjectTaskViewModel? SelectedChoice { get; set; }

    /// <summary>
    ///     The edited start and end. Null while the date is cleared or a time can't be read, see <see cref="CanSave" />
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
    ///     True when the date and both times can be read, see <see cref="Interval" />.
    /// </summary>
    public bool CanSave => Interval is not null;

    /// <summary>
    ///     The edited duration, hh:mm:ss. Empty while the times can't be read.
    /// </summary>
    public string Duration => Interval is { } i ? TimeEntryHistory.Format(i.Duration) : string.Empty;

    private bool CanCancel => !SaveCommand.IsRunning;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        return SaveAction(this);
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        CancelAction();
    }
}
