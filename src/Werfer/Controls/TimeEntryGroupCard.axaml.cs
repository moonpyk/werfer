using Avalonia.Controls;
using Avalonia.Input;
using Werfer.ViewModels;

namespace Werfer.Controls;

/// <summary>
///     A history line: the entries of one day with the same project, task and description. It expands to list them.
/// </summary>
public partial class TimeEntryGroupCard : UserControl
{
    public TimeEntryGroupCard()
    {
        InitializeComponent();
    }

    // A left click on the background of a card or line edits its entry. Clicks on its buttons are handled by
    // the buttons and never get here. A line marks the click handled, so the group card around it doesn't edit too
    private void OnEntryPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left
            || (sender as Control)?.DataContext is not TimeEntryItemViewModelBase item)
        {
            return;
        }

        e.Handled = true;

        if (item.EditCommand.CanExecute(null))
        {
            item.EditCommand.Execute(null);
        }
    }
}
