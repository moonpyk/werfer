using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer.ViewModels;

/// <summary>
///     Main page: the timer bar and the week's history (each has its own view model), and the sheet over them, to edit
///     or split an entry.
///     Handles what links them: loading on connection, a finished entry added to the history, the week's total
///     including the running entry, a history line continued in the bar, and the shortcuts disabled while the sheet
///     is open.
/// </summary>
[UsedImplicitly]
public partial class TimerViewModel : ViewModelBase
{
    // The item open in the sheet, highlighted in the history while the sheet is open
    private TimeEntryItemViewModelBase? _editedItem;

    /// <summary>
    ///     Creates the page and its two parts, and links them, see the class summary.
    /// </summary>
    public TimerViewModel(
        IApplicationSettings applicationSettings,
        ILogger<TimerViewModel> logger,
        FlashMessageService flashMessages,
        TimeTrackingService timeTracking
    ) : base(logger, flashMessages)
    {
        TimeTracking = timeTracking;
        Player = new TimerPlayerViewModel(
            logger,
            flashMessages,
            applicationSettings,
            timeTracking
        );
        History = new HistoryViewModel(
            logger,
            flashMessages,
            applicationSettings,
            timeTracking,
            this
        );

        timeTracking.Connected += OnConnected;
        Player.EntryFinished += OnEntryFinished;
        Player.PropertyChanged += OnPlayerPropertyChanged;
    }

    private TimeTrackingService TimeTracking { get; }

    /// <summary>
    ///     The timer bar: the running entry, its start and stop.
    /// </summary>
    public TimerPlayerViewModel Player { get; }

    /// <summary>
    ///     The history: the week's finished entries and total.
    /// </summary>
    public HistoryViewModel History { get; }

    /// <summary>
    ///     The form in the sheet: a <see cref="TimeEntryEditViewModel" /> or a <see cref="TimeEntrySplitViewModel" />,
    ///     each with its own view under the same buttons. Kept after the sheet closes, replaced by <see cref="Edit" /> and
    ///     <see cref="Split" />
    /// </summary>
    [ObservableProperty]
    public partial SheetFormViewModel? SheetContent { get; private set; }

    /// <summary>
    ///     True while the sheet is open. Bound both ways: the sheet closes itself on a click outside
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(ToggleCommand),
        nameof(ClearDescriptionCommand),
        nameof(ResetCommand)
    )]
    [NotifyCanExecuteChangedFor(
        nameof(RefreshCommand),
        nameof(CloseSheetCommand)
    )]
    public partial bool IsSheetOpen { get; set; }

    /// <summary>
    ///     True while the sheet's changes are being sent. The sheet can't be closed then: closing wouldn't stop the
    ///     request
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetCommand))]
    public partial bool IsSheetSaving { get; private set; }

    /// <summary>
    ///     Ctrl+Space, see the window's KeyBindings: the timer bar's Toggle. Null while the sheet is open.
    /// </summary>
    public ICommand? ToggleCommand => IsSheetOpen ? null : Player.ToggleCommand;

    /// <summary>
    ///     Ctrl+L: the timer bar's ClearDescription. Null while the sheet is open.
    /// </summary>
    public ICommand? ClearDescriptionCommand => IsSheetOpen ? null : Player.ClearDescriptionCommand;

    /// <summary>
    ///     Ctrl+N: the timer bar's Reset. Null while the sheet is open.
    /// </summary>
    public ICommand? ResetCommand => IsSheetOpen ? null : Player.ResetCommand;

    private bool CanRefresh => !IsSheetOpen;
    private bool CanCloseSheet => IsSheetOpen && !IsSheetSaving;

    /// <summary>
    ///     Reloads the running entry and the week's history from Clockify: they may have changed in another app.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    public Task RefreshAsync()
    {
        return WithBusyIndicator(async () =>
        {
            await Player.Draft.RefreshAsync();
            await History.LoadImplAsync();
        });
    }

    /// <summary>
    ///     Starts a new entry on a history line's project, task and description, stopping the running one first.
    /// </summary>
    public Task ContinueAsync(TimeEntryGroupViewModel group)
    {
        return group.Choice is null
            ? Task.CompletedTask
            : Player.ContinueAsync(group.Choice, group.Description);
    }

    /// <summary>
    ///     Deletes history entries, see <see cref="HistoryViewModel.DeleteAsync" />.
    /// </summary>
    public Task DeleteAsync(IReadOnlyList<string> ids)
    {
        return History.DeleteAsync(ids);
    }

    /// <summary>
    ///     Opens the edit sheet on a history entry.
    /// </summary>
    public void Edit(TimeEntryItemViewModelBase item, TimeEntry entry)
    {
        SetEditedItem(item);

        SheetContent = new TimeEntryEditViewModel(
            entry,
            item.Choice,
            Player.ProjectChoicesPopulator,
            SaveEditAsync,
            CloseSheet
        );
        IsSheetOpen = true;
    }

    /// <summary>
    ///     Opens the sheet to split a history entry in two.
    /// </summary>
    public void Split(TimeEntryItemViewModelBase item, TimeEntry entry)
    {
        SetEditedItem(item);

        SheetContent = new TimeEntrySplitViewModel(
            entry,
            SplitAsync,
            CloseSheet
        );
        IsSheetOpen = true;
    }

    /// <summary>
    ///     Closes the sheet without saving, for Cancel and Escape. A click outside closes it through the IsSheetOpen
    ///     binding.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCloseSheet))]
    private void CloseSheet()
    {
        if (!IsSheetSaving)
        {
            IsSheetOpen = false;
        }
    }

    private void SetEditedItem(TimeEntryItemViewModelBase? item)
    {
        _editedItem?.IsEditing = false;
        _editedItem = item;
        _editedItem?.IsEditing = true;
    }

    private Task SaveEditAsync(TimeEntryEditViewModel form)
    {
        if (form.Interval is not { } range)
        {
            return Task.CompletedTask;
        }

        // No project picked (an archived one isn't in the list): keep the entry's own project and task
        return SaveSheetAsync(() => TimeTracking.UpdateTimeEntryAsync(
            form.Entry,
            range,
            form.SelectedChoice?.Project.Id ?? form.Entry.ProjectId,
            form.SelectedChoice?.Task.Id ?? form.Entry.TaskId,
            form.Description
        ));
    }

    private Task SplitAsync(TimeEntrySplitViewModel form)
    {
        return form.SplitTime is { } at
            ? SaveSheetAsync(() => TimeTracking.SplitTimeEntryAsync(form.Entry, at))
            : Task.CompletedTask;
    }

    // Sends the sheet's changes, then closes it and reloads the history
    private Task SaveSheetAsync(Func<Task> send)
    {
        return WithBusyIndicator(async () =>
        {
            IsSheetSaving = true;
            var saved = false;

            try
            {
                await send();
                saved = true;
            }
            finally
            {
                IsSheetSaving = false;

                // Closed only once saved: if saving fails (WithBusyIndicator reports it), the sheet stays open and
                // nothing typed is lost
                if (saved)
                {
                    CloseSheet();
                }

                // Reloaded after a failure too: a split may have half happened, the history must show it
                await History.LoadImplAsync();
            }
        });
    }

    // Connected, at startup or from the settings page: load the projects first, then the history, which needs
    // their names
    private void OnConnected(object? sender, EventArgs e)
    {
        _ = WithBusyIndicator(async () =>
        {
            await Player.LoadProjectChoicesAsync();
            History.Choices = Player.ProjectChoices;
            await History.LoadImplAsync();
        });
    }

    // An entry finished, stopped here or in another app: reload the history to show it
    private void OnEntryFinished(object? sender, EventArgs e)
    {
        _ = History.LoadAsync();
    }

    // The week's total includes the running entry: pass on its time every second
    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerPlayerViewModel.RunningElapsed))
        {
            History.RunningElapsed = Player.RunningElapsed;
        }
    }

    partial void OnIsSheetOpenChanged(bool value)
    {
        if (!value)
        {
            SetEditedItem(null);
        }
    }
}
