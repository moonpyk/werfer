using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Werfer.Clockify.Models;

namespace Werfer.ViewModels;

/// <summary>
///     A history item, a group or one entry of an expanded group: the entry menu's actions, shared by both.
/// </summary>
public abstract partial class TimeEntryItemViewModelBase(
    TimerViewModel timer
) : ObservableObject
{
    protected TimerViewModel Timer { get; } = timer;

    /// <summary>
    ///     While its edit sheet is open: the card shows which entry is being edited
    /// </summary>
    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    /// <summary>
    ///     Null when the project or task isn't startable anymore (archived, done)
    /// </summary>
    public abstract ProjectTaskViewModel? Choice { get; }

    /// <summary>
    ///     What the menu acts on: every entry of a group, or the line's single entry
    /// </summary>
    protected abstract IReadOnlyList<TimeEntry> TimeEntries { get; }

    // Entries are edited one by one, a group of several is edited through its expanded lines
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
        // Deleting can't be undone, so ask first. Deliberately NOT the Fluent dialog guidelines (action left, Cancel right
        // and default) but the web's, as Clockify does: Cancel left, Delete right and default (accented, triggered by
        // Enter). FAContentDialog sends Enter to the default button whatever has focus and ignores arrow keys, so with
        // Cancel as default, arrowing to Delete then Enter cancelled. Delete is the secondary button, not the close one:
        // Escape and the close button both return None, Delete must be told apart from a dismissal
        var dialog = new FAContentDialog
        {
            Title = TimeEntries.Count > 1 ? $"Delete {TimeEntries.Count} entries?" : "Delete this entry?",
            Content = "This can't be undone.",
            PrimaryButtonText = "Cancel",
            SecondaryButtonText = "Delete",
            DefaultButton = FAContentDialogButton.Secondary
        };

        if (await dialog.ShowAsync() == FAContentDialogResult.Secondary)
        {
            // Failures are reported by the view model's WithBusyIndicator
            await Timer.DeleteAsync([.. TimeEntries.Select(e => e.Id)]);
        }
    }
}
