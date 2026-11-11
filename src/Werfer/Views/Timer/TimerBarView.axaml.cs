using Avalonia.Controls;
using Avalonia.Interactivity;
using Werfer.ViewModels;

namespace Werfer.Views.Timer;

/// <summary>
///     Project, task and description of the entry, with the start/stop button.
/// </summary>
public partial class TimerBarView : UserControl
{
    public TimerBarView()
    {
        InitializeComponent();
    }

    // Typed: now it goes to the running entry
    private void OnDescriptionLostFocus(object? sender, RoutedEventArgs e)
    {
        _ = (DataContext as TimerPlayerViewModel)?.PushDescriptionAsync();
    }
}
