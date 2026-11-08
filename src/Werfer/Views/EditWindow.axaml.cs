using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Werfer.Views;

/// <summary>
///     Edits a history entry, see <see cref="ViewModels.TimeEntryEditViewModel" />. Closes with true when saved.
/// </summary>
public partial class EditWindow : Window
{
    public EditWindow()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
