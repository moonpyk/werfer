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
///     Main page: the timer bar and the week's history, each its own view model, and the edit sheet over them. Holds
///     what ties them together: loading on connection, a finished entry joining the history, the week's total
///     counting the running entry, a history line continued in the bar, the shortcuts blocked behind the sheet.
/// </summary>
[UsedImplicitly]
public partial class TimerViewModel : ViewModelBase
{
    // The entry shown in the edit sheet, highlighted in the history meanwhile
    private TimeEntryItemViewModelBase? _editedItem;

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

    public TimerPlayerViewModel Player { get; }

    public HistoryViewModel History { get; }

    /// <summary>
    ///     The edit sheet's form; kept once closed, set anew by <see cref="Edit" />
    /// </summary>
    [ObservableProperty]
    public partial TimeEntryEditViewModel? Editor { get; private set; }

    /// <summary>
    ///     Bound both ways to the sheet, which closes itself on a click outside
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(ToggleCommand),
        nameof(ClearDescriptionCommand),
        nameof(ResetCommand)
    )]
    [NotifyCanExecuteChangedFor(
        nameof(RefreshCommand),
        nameof(CloseEditorCommand)
    )]
    public partial bool IsEditorOpen { get; set; }

    /// <summary>
    ///     While the edit sheet's changes are being sent: it can't be closed meanwhile, closing wouldn't stop them
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseEditorCommand))]
    public partial bool IsSavingEdit { get; private set; }

    // The bar's commands for the window's shortcuts, see its KeyBindings: none acts behind the edit sheet
    public ICommand? ToggleCommand => IsEditorOpen ? null : Player.ToggleCommand;
    public ICommand? ClearDescriptionCommand => IsEditorOpen ? null : Player.ClearDescriptionCommand;
    public ICommand? ResetCommand => IsEditorOpen ? null : Player.ResetCommand;

    private bool CanRefresh => !IsEditorOpen;
    private bool CanCloseEditor => IsEditorOpen && !IsSavingEdit;

    /// <summary>
    ///     Fetches the running entry and the week's history, they may have changed elsewhere.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    public Task RefreshAsync()
    {
        return WithBusyIndicator(async () =>
        {
            // A description still being typed goes first, the refreshed entry would overwrite it
            await Player.PushDescriptionAsync();

            await TimeTracking.RefreshRunningEntryAsync();
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
        
        Editor = new TimeEntryEditViewModel(
            entry,
            item.Choice,
            Player.ProjectChoicesPopulator,
            SaveEditAsync,
            CloseEditor
        );
        IsEditorOpen = true;
    }

    /// <summary>
    ///     Closes the edit sheet without saving: Cancel, Escape; a click outside closes it through the binding.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCloseEditor))]
    public void CloseEditor()
    {
        if (!IsSavingEdit)
        {
            IsEditorOpen = false;
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

        return WithBusyIndicator(async () =>
        {
            IsSavingEdit = true;

            try
            {
                // An archived project isn't among the choices: none picked keeps the entry's own
                await TimeTracking.UpdateTimeEntryAsync(
                    form.Entry,
                    range,
                    form.SelectedChoice?.Project.Id ?? form.Entry.ProjectId,
                    form.SelectedChoice?.Task.Id ?? form.Entry.TaskId,
                    form.Description
                );
            }
            finally
            {
                IsSavingEdit = false;
            }

            // Only once saved: on failure (reported by WithBusyIndicator) the sheet stays open, nothing typed is lost
            CloseEditor();
            await History.LoadImplAsync();
        });
    }

    // Connection happens from the settings page: the projects first, then the history, which needs their names
    private void OnConnected(object? sender, EventArgs e)
    {
        _ = WithBusyIndicator(async () =>
        {
            await Player.LoadProjectChoicesAsync();
            History.Choices = Player.ProjectChoices;
            await History.LoadImplAsync();
        });
    }

    // Stopped here or elsewhere: the entry now belongs to the history
    private void OnEntryFinished(object? sender, EventArgs e)
    {
        _ = History.LoadAsync();
    }

    // The week's total counts the running entry, second by second
    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerPlayerViewModel.RunningElapsed))
        {
            History.RunningElapsed = Player.RunningElapsed;
        }
    }

    partial void OnIsEditorOpenChanged(bool value)
    {
        if (!value)
        {
            SetEditedItem(null);
        }
    }
}
