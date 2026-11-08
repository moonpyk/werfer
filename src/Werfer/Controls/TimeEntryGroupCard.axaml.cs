using Avalonia.Controls;
using Avalonia.Input;
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

    // A left click on the background of a card or line edits its entry. Its buttons handle their own release, so
    // it doesn't get here; a line marks it handled, so the card around it doesn't edit as well
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
