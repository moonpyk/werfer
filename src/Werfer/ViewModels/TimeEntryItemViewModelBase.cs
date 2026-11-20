using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Werfer.Clockify.Models;

namespace Werfer.ViewModels;

/// <summary>
///     A history item, either a group or one entry of an expanded group. Holds the entry menu's actions, shared by
///     both.
/// </summary>
public abstract partial class TimeEntryItemViewModelBase(
    TimerViewModel timer
) : ObservableObject
{
    protected TimerViewModel Timer { get; } = timer;

    /// <summary>
    ///     True while this item is open in the edit sheet: the card shows which entry is being edited
    /// </summary>
    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    /// <summary>
    ///     The item's project and task. Null when they can't be started anymore (archived, done)
    /// </summary>
    public abstract ProjectTaskViewModel? Choice { get; }

    /// <summary>
    ///     The entries the menu acts on: all the entries of a group, or the single entry of a line
    /// </summary>
    protected abstract IReadOnlyList<TimeEntry> TimeEntries { get; }

    /// <summary>
    ///     True when the item can be edited. Entries are edited one at a time: a group of several is edited through its
    ///     expanded lines.
    /// </summary>
    public bool CanEdit => TimeEntries.Count == 1;

    // Opens the edit sheet of the timer page
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
        Timer.Edit(this, TimeEntries[0]);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (await ConfirmDeleteAsync(TimeEntries.Count > 1
                ? $"Delete {TimeEntries.Count} entries?"
                : "Delete this entry?"))
        {
            // Failures are reported by the view model's WithBusyIndicator
            await Timer.DeleteAsync(
                [.. TimeEntries.Select(e => e.Id)]
            );
        }
    }

    /// <summary>
    ///     Asks before deleting, which can't be undone. True when Delete was pressed.
    /// </summary>
    public static async Task<bool> ConfirmDeleteAsync(string title)
    {
        // The buttons follow the web convention, as Clockify does, NOT Fluent's (action left, Cancel right and
        // default): Cancel on the left, Delete on the right and default (accented, pressed by Enter). FAContentDialog
        // sends Enter to the default button whatever has the focus, and ignores arrow keys: with Cancel as default,
        // moving to Delete with the arrows then pressing Enter cancelled. Delete is the secondary button, not the close
        // one: Escape and the close button both return None, which must not be taken for Delete
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = "This can't be undone.",
            PrimaryButtonText = "Cancel",
            SecondaryButtonText = "Delete",
            DefaultButton = FAContentDialogButton.Secondary
        };

        return await dialog.ShowAsync() == FAContentDialogResult.Secondary;
    }
}
