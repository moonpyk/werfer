using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Werfer.ViewModels;

namespace Werfer.Controls;

/// <summary>
///     A history line: entries of a day sharing project, task and description, expandable to list them.
/// </summary>
public partial class TimeEntryGroupCard : UserControl
{
    public TimeEntryGroupCard()
    {
        InitializeComponent();
    }

    // Shared by the group menu (all its entries) and the expanded lines' menus (one entry)
    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        var (group, ids) = (sender as MenuItem)?.DataContext switch
        {
            TimeEntryGroupViewModel g => (g, g.EntryIds),
            TimeEntryLineViewModel line => (line.Group, new[] { line.Id }),
            _ => default
        };

        if (group is null || ids is null)
        {
            return;
        }

        // Deleting can't be undone, so ask first. Deliberately NOT the Fluent dialog guidelines (action left, Cancel right
        // and default) but the web's, as Clockify does: Cancel left, Delete right and default (accented, triggered by
        // Enter). FAContentDialog sends Enter to the default button whatever has focus and ignores arrow keys, so with
        // Cancel as default, arrowing to Delete then Enter cancelled. Delete is the secondary button, not the close one:
        // Escape and the close button both return None, Delete must be told apart from a dismissal
        var dialog = new FAContentDialog
        {
            Title = ids.Length > 1 ? $"Delete {ids.Length} entries?" : "Delete this entry?",
            Content = "This can't be undone.",
            PrimaryButtonText = "Cancel",
            SecondaryButtonText = "Delete",
            DefaultButton = FAContentDialogButton.Secondary
        };

        if (await dialog.ShowAsync() == FAContentDialogResult.Secondary)
        {
            // Failures are reported by the view model's WithBusyIndicator
            await group.DeleteAsync(ids);
        }
    }
}
