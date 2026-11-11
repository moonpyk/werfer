using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     The week's finished entries, grouped by day, and the week's total.
/// </summary>
public partial class HistoryViewModel : ViewModelBase
{
    // Day the history was last loaded for
    private DateOnly _date;

    // Numbers the loads, see LoadImplAsync
    private int _loadGeneration;

    private TimeSpan _weekFinished;

    public HistoryViewModel(
        ILogger logger,
        FlashMessageService flashMessages,
        IApplicationSettings applicationSettings,
        TimeTrackingService timeTracking,
        TimerViewModel page
    ) : base(logger, flashMessages)
    {
        ApplicationSettings = applicationSettings;
        TimeTracking = timeTracking;
        Page = page;
        WeekTotal = "00:00:00";

        // "Today" and the week move at midnight, the app often stays open across it
        DispatcherTimer.Run(() =>
        {
            if (Days is not null && DateOnly.FromDateTime(DateTime.Now) != _date)
            {
                _ = LoadAsync();
            }

            return true;
        }, TimeSpan.FromMinutes(1));
    }

    private IApplicationSettings ApplicationSettings { get; }
    private TimeTrackingService TimeTracking { get; }

    // The history's lines act through it: continue, edit, delete
    private TimerViewModel Page { get; }

    /// <summary>
    ///     The project and task choices, to name the entries' ones; null until connected, nothing loads meanwhile
    /// </summary>
    public List<ProjectTaskViewModel>? Choices { get; set; }

    /// <summary>
    ///     Finished entries of the current week by day, null until loaded
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial List<TimeEntryDayViewModel>? Days { get; private set; }

    /// <summary>
    ///     Nothing to list yet: not connected, loading or an empty week; the view shows a logo instead
    /// </summary>
    public bool IsEmpty => Days is not { Count: > 0 };

    /// <summary>
    ///     Finished entries of the week plus the running one, ticks with it
    /// </summary>
    [ObservableProperty]
    public partial string WeekTotal { get; private set; }

    /// <summary>
    ///     The running entry's time, counted in <see cref="WeekTotal" />
    /// </summary>
    public TimeSpan RunningElapsed
    {
        get;
        set
        {
            field = value;
            UpdateWeekTotal();
        }
    }

    /// <summary>
    ///     Reloads the week's history, e.g. once the first day of the week changed. Does nothing until connected.
    /// </summary>
    public Task LoadAsync()
    {
        return WithBusyIndicator(LoadImplAsync);
    }

    /// <summary>
    ///     Deletes entries, then reloads the history, also after a partial failure, to show what's left.
    /// </summary>
    public Task DeleteAsync(IReadOnlyList<string> ids)
    {
        return WithBusyIndicator(async () =>
        {
            try
            {
                await TimeTracking.DeleteTimeEntriesAsync(ids);
            }
            finally
            {
                await LoadImplAsync();
            }
        });
    }

    /// <summary>
    ///     <see cref="LoadAsync" /> without its busy indicator, for a caller already showing its own.
    /// </summary>
    public async Task LoadImplAsync()
    {
        if (Choices is null)
        {
            return;
        }

        var zone = TimeZoneInfo.Local;
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.Date);

        // Set before the request: a failed load is reported once, not retried every minute by the date check
        _date = today;

        var load = Interlocked.Increment(ref _loadGeneration);

        var entries = await TimeTracking.GetTimeEntriesSinceAsync(
            TimeEntryHistory.StartOfWeek(now, ApplicationSettings.WeekStart, zone)
        );

        // Loads overlap (a stop, F5, a save): the last one started wins, not the last one to answer, which may
        // predate a save and show the entry unchanged
        if (load != _loadGeneration)
        {
            return;
        }

        Days =
        [
            .. entries
                .GroupBy(zone)
                .Select(d => new TimeEntryDayViewModel(d, today, Choices, Page))
        ];

        _weekFinished = entries.Sum();
        UpdateWeekTotal();
    }

    private void UpdateWeekTotal()
    {
        // shortcut: an entry started before the week counts whole, split it at the week start if that ever matters
        WeekTotal = TimeEntryHistory.Format(_weekFinished + RunningElapsed);
    }
}
