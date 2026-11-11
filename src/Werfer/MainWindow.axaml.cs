using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        // FAAppWindow's TitleBarBackground resource resolves to nothing, leaving its title bar panel without
        // background: not hit-testable, so only the icon and title text could drag the window
        TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = Colors.Transparent;
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
            } when ViewModel is { Timer.IsEditorOpen: false }:
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
            } when ViewModel is { Timer.IsEditorOpen: false }:
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
            } when ViewModel is { CurrentPage: TimerViewModel { IsEditorOpen: false } timer }:
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
            } when ViewModel is { CurrentPage: TimerViewModel { IsEditorOpen: false } timer }:
                Focus();
                _ = timer.ResetAsync();
                e.Handled = true;
                return;

            // Closes the edit sheet, like its Cancel button; unhandled so far, an open dropdown inside it took it first
            case
            {
                KeyModifiers: KeyModifiers.None,
                Key: Key.Escape
            } when ViewModel is { Timer.IsEditorOpen: true }:
                ViewModel.Timer.CloseEditor();
                e.Handled = true;
                return;

            default:
                base.OnKeyDown(e);
                break;
        }
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        // More has no Tag: selecting it follows a navigation already done, see OnMorePageClick
        if (e.SelectedItem is FANavigationViewItem { Tag: AppPageEnum page })
        {
            ViewModel?.Navigate(page);
        }
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
        else if (e.InvokedItemContainer == MoreItem)
        {
            FlyoutBase.ShowAttachedFlyout(MoreItem);
        }
    }

    // A page of the More menu: More, selected by the click that opened the menu, stays selected meanwhile
    private void OnMorePageClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: AppPageEnum page })
        {
            ViewModel?.Navigate(page);
        }
    }

    // Closed without a pick, or on a page with an item of its own: that item gets the selection back. Not when on a
    // page of the More menu, whose item More is
    private void OnMoreMenuClosed(object? sender, EventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var items = Navigation.MenuItems
            .Concat(Navigation.FooterMenuItems);
        
        var pageItem = items
            .OfType<FANavigationViewItem>()
            .FirstOrDefault(i => i.Tag is AppPageEnum page && page == ViewModel.Page);

        if (pageItem is not null)
        {
            Navigation.SelectedItem = pageItem;
        }
    }
}
