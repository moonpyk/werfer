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

    // Incremented by each load, so only the latest load shows its result, see LoadImplAsync
    private int _loadGeneration;

    private TimeSpan _weekFinished;

    /// <summary>
    ///     Creates the history. It stays empty until connected, and reloads itself at midnight.
    /// </summary>
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

        // "Today" and the week change at midnight, and the app often stays open past it
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

    // The timer page: the history's lines call it to continue, edit or delete an entry
    private TimerViewModel Page { get; }

    /// <summary>
    ///     Every project and task, to show the names of the entries' ones. Null until connected; nothing loads until then
    /// </summary>
    public List<ProjectTaskViewModel>? Choices { get; set; }

    /// <summary>
    ///     Finished entries of the current week by day, null until loaded
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial List<TimeEntryDayViewModel>? Days { get; private set; }

    /// <summary>
    ///     True when there is nothing to list: not connected, loading, or no entry this week. The view then shows a logo
    /// </summary>
    public bool IsEmpty => Days is not { Count: > 0 };

    /// <summary>
    ///     The week's total: finished entries plus the running one. Updated every second while it runs
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
    ///     <see cref="LoadAsync" />, without the busy indicator: for a caller that already shows its own.
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

        // Set before the request, so a failed load is reported once, not retried every minute by the midnight check
        _date = today;

        var load = Interlocked.Increment(ref _loadGeneration);

        var entries = await TimeTracking.GetTimeEntriesSinceAsync(
            TimeEntryHistory.StartOfWeek(now, ApplicationSettings.WeekStart, zone)
        );

        // Several loads can run at once (a stop, F5, a save). Only the last one started shows its result: the last one
        // to answer may have started before a save, and would show the entry unchanged
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
