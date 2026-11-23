using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
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

    // The settings' prefixes are removed from the pasted text: the box's own paste is cancelled, the text inserted here
    private async void OnDescriptionPasting(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box || DataContext is not TimerPlayerViewModel
            {
                HasDescriptionPrefixes: true
            } player)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(box)?.Clipboard;

        if (clipboard is null)
        {
            return;
        }

        e.Handled = true;

        try
        {
            var pasted = await clipboard.TryGetTextAsync();

            if (!string.IsNullOrEmpty(pasted))
            {
                box.SelectedText = player.StripDescriptionPrefixes(pasted).ReplaceLineEndings(box.NewLine);
            }
        }
        catch (Exception)
        {
            // The clipboard can be held by another application: the paste is lost, as with a failed native paste
        }
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

    // The flyout closes first, the split sheet opens in its place
    private void OnSplitClick(object? sender, RoutedEventArgs e)
    {
        StopButton.Flyout?.Hide();
        (DataContext as TimerPlayerViewModel)?.RequestSplit();
    }

    private void ApplyStart()
    {
        (DataContext as TimerPlayerViewModel)?.ChangeStartCommand.Execute(null);
        StopButton.Flyout?.Hide();
    }
}
