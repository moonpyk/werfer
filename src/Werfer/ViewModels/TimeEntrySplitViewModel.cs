using CommunityToolkit.Mvvm.ComponentModel;
using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     The split form of one entry, finished or running, in local time: a time typed or picked on a slider, on a whole
///     minute strictly inside the entry. A running entry ends, for the form, when it opens.
/// </summary>
public partial class TimeEntrySplitViewModel : SheetFormViewModel
{
    /// <summary>
    ///     Fills the form from an entry, split in its middle. The timer page splits and closes the form through the given
    ///     callbacks.
    /// </summary>
    public TimeEntrySplitViewModel(
        TimeEntry entry,
        Func<TimeEntrySplitViewModel, Task> split,
        Action cancel
    ) : base(cancel)
    {
        Entry = entry;
        SplitAction = split;

        Start = entry.TimeInterval!.Start.ToLocalTime();
        End = (entry.TimeInterval.End ?? DateTimeOffset.Now).ToLocalTime();

        var (first, last) = TimeEntryHistory.SplitBounds(
            Start, 
            End
        );
        FirstMinute = first;
        SliderMaximum = Math.Max(0, (last - first).TotalMinutes);

        SliderValue = Math.Floor(SliderMaximum / 2);
    }

    /// <summary>
    ///     The entry split, as it was when the form opened.
    /// </summary>
    public TimeEntry Entry { get; }

    private Func<TimeEntrySplitViewModel, Task> SplitAction { get; }

    private DateTimeOffset Start { get; }
    private DateTimeOffset End { get; }

    // The slider's 0
    private DateTimeOffset FirstMinute { get; }

    /// <summary>
    ///     The entry's start, HH:mm, at the slider's left.
    /// </summary>
    public string StartText => TimeOfDay(Start);

    /// <summary>
    ///     The entry's end, HH:mm, at the slider's right.
    /// </summary>
    public string EndText => TimeOfDay(End);

    /// <summary>
    ///     The split time as typed, see <see cref="TimeEntryHistory.TryParseTimeOfDay" />. The slider sets it too.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(SplitTime),
        nameof(SliderValue),
        nameof(FirstDuration),
        nameof(SecondDuration),
        nameof(CanSave)
    )]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? SplitAt { get; set; }

    /// <summary>
    ///     Where the entry is split. Null while <see cref="SplitAt" /> can't be read or falls outside the entry.
    /// </summary>
    public DateTimeOffset? SplitTime => TimeEntryHistory.TryParseTimeOfDay(SplitAt, out var time)
        ? TimeEntryHistory.SplitTime(Start, End, time, TimeZoneInfo.Local)
        : null;

    /// <summary>
    ///     The slider's last position: the minutes from the first whole minute to the last one inside the entry. 0 when
    ///     the entry is too short to split.
    /// </summary>
    public double SliderMaximum { get; }

    /// <summary>
    ///     The slider's position, in minutes from the first whole minute inside the entry. Moving it sets
    ///     <see cref="SplitAt" />; a time typed moves it. It stays put while the typed time can't be read.
    /// </summary>
    public double SliderValue
    {
        get => SplitTime is { } at ? (at - FirstMinute).TotalMinutes : field;
        set
        {
            field = Math.Round(value);
            SplitAt = TimeOfDay(FirstMinute.AddMinutes(field));
        }
    }

    /// <summary>
    ///     The first part's duration, hh:mm:ss. Empty while <see cref="SplitTime" /> is null.
    /// </summary>
    public string FirstDuration => SplitTime is { } at
        ? TimeEntryHistory.Format(at - Start)
        : string.Empty;

    /// <summary>
    ///     The second part's duration, hh:mm:ss. Empty while <see cref="SplitTime" /> is null.
    /// </summary>
    public string SecondDuration => SplitTime is { } at
        ? TimeEntryHistory.Format(End - at)
        : string.Empty;

    /// <summary>
    ///     True when <see cref="SplitTime" /> falls inside the entry.
    /// </summary>
    public override bool CanSave => SplitTime is not null;

    private static string TimeOfDay(DateTimeOffset time)
    {
        return TimeEntryHistory.FormatTimeOfDay(TimeOnly.FromDateTime(time.DateTime));
    }

    protected override Task SaveImplAsync()
    {
        return SplitAction(this);
    }
}
