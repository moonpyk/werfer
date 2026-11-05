using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using JetBrains.Annotations;
using Werfer.Services;
using Werfer.Utils;
using Werfer.ViewModels;

namespace Werfer;

public partial class MainWindow : FAAppWindow
{
    [UsedImplicitly]
    [Obsolete("Designer only, the running app must go through the view model constructor", true)]
    public MainWindow()
    {
        if (!Design.IsDesignMode)
        {
            throw new InvalidOperationException($"{nameof(MainWindow)} needs its {nameof(MainWindowViewModel)}");
        }

        Initialize();
    }

    public MainWindow(MainWindowViewModel vm)
    {
        Initialize();

        DataContext = ViewModel = vm;

        this.RestoreSize(
            vm.ApplicationSettings
        ).RestorePosition(
            vm.ApplicationSettings
        );
    }

    private MainWindowViewModel? ViewModel { get; }

    private void Initialize()
    {
        InitializeComponent();
        // Matches the page MainWindowViewModel opens on
        Navigation.SelectedItem = TimerItem;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // A minimized window reports a fake far off-screen position, keep the last real one instead
        // (also skips maximized, whose size isn't the one to restore)
        if (ViewModel is not null && WindowState == WindowState.Normal)
        {
            var sts = ViewModel.ApplicationSettings;

            sts.WindowX = Position.X;
            sts.WindowY = Position.Y;
            sts.WindowWidth = ClientSize.Width;
            sts.WindowHeight = ClientSize.Height;
            sts.TrySave();
        }

        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e)
        {
            // Ctrl+space Start/Stop counter
            case
            {
                KeyModifiers: KeyModifiers.Control,
                Key: Key.Space
            } when ViewModel is not null:
                e.Handled = true;

                // Checked before Focus(): committing the description starts an update, which sets IsBusy
                if (ViewModel.Timer.IsBusy)
                {
                    return;
                }

                // Commits the description being typed, it's only pushed on lost focus
                Focus();
                // Failures are reported by the view model's WithBusyIndicator
                _ = ViewModel.Timer.ToggleAsync();
                return;

            // Refresh
            case
            {
                KeyModifiers: KeyModifiers.None,
                Key: Key.F5
            } when ViewModel is not null:
                // Pushes the description being typed first, the refresh would overwrite it otherwise
                Focus();
                _ = ViewModel.Timer.RefreshAsync();
                e.Handled = true;
                return;

            // "cLear", timer page only: the cleared field must be on screen
            case
            {
                KeyModifiers: KeyModifiers.Control,
                Key: Key.L
            } when ViewModel is { CurrentPage: TimerViewModel timer }:
                // Commits the text being typed first: clearing an unchanged value wouldn't reach the TextBox
                Focus();
                timer.Description = null;
                e.Handled = true;
                return;

            // "New": a blank, stopped timer
            case
            {
                KeyModifiers: KeyModifiers.Control,
                Key: Key.N
            } when ViewModel is { CurrentPage: TimerViewModel timer }:
                Focus();
                _ = timer.ResetAsync();
                e.Handled = true;
                return;

            default:
                base.OnKeyDown(e);
                break;
        }
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        ViewModel?.Navigate(ReferenceEquals(e.SelectedItem, SettingsItem));
    }

    private void OnFlashMessageClosed(FAInfoBar sender, FAInfoBarClosedEventArgs e)
    {
        if (sender.DataContext is FlashMessage message)
        {
            ViewModel?.FlashMessages.Remove(message);
        }
    }

    private void OnNavigationItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer == RefreshItem)
        {
            _ = ViewModel?.Timer.RefreshAsync();
        }
    }
}
