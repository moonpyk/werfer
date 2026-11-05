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
            _ => default((TimeEntryGroupViewModel?, string[]?))
        };

        if (group is null || ids is null)
        {
            return;
        }

        // Deleting can't be undone, so ask first
        var dialog = new FAContentDialog
        {
            Title = ids.Length > 1 ? $"Delete {ids.Length} entries?" : "Delete this entry?",
            Content = "This can't be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = FAContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == FAContentDialogResult.Primary)
        {
            // Failures are reported by the view model's WithBusyIndicator
            await group.DeleteAsync(ids);
        }
    }
}
