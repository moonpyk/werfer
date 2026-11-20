using Avalonia.Controls;
using Avalonia.Input;
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

    // The user is done typing: send the description to the running entry
    private void OnDescriptionLostFocus(object? sender, RoutedEventArgs e)
    {
        _ = (DataContext as TimerPlayerViewModel)?.SaveDraftAsync();
    }

    // Shows the current start each time, an edit left unapplied is dropped
    private void OnStartFlyoutOpening(object? sender, EventArgs e)
    {
        (DataContext as TimerPlayerViewModel)?.ResetStartTime();
    }

    // Run here rather than bound to the button: closing the flyout detaches it, the bound command would then be lost
    private void OnStartApplyClick(object? sender, RoutedEventArgs e)
    {
        ApplyStart();
    }

    // Enter in the start time applies it, like the Apply button
    private void OnStartKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        ApplyStart();
    }

    // The flyout closes first, the confirmation dialog opens in its place
    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        StopButton.Flyout?.Hide();
        (DataContext as TimerPlayerViewModel)?.DeleteCommand.Execute(null);
    }

    private void ApplyStart()
    {
        (DataContext as TimerPlayerViewModel)?.ChangeStartCommand.Execute(null);
        StopButton.Flyout?.Hide();
    }
}
