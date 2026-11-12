using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    // Set once Quit is picked in the confirmation, see ConfirmCloseAsync
    private bool _closeConfirmed;

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

        // A page shown by a shortcut (Ctrl+,) gets its navigation item selected, as a click would
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Page))
            {
                SelectPageItem();
            }
        };

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
        // Not e.IsProgrammatic: FAAppWindow's own close button calls Close() too. A Windows shutdown mustn't wait on a dialog
        if (ViewModel is { ApplicationSettings.ConfirmClose: true }
            && !_closeConfirmed
            && e.CloseReason != WindowCloseReason.OSShutdown)
        {
            e.Cancel = true;
            _ = ConfirmCloseAsync(ViewModel.ApplicationSettings);
            return;
        }

        // A minimized window reports a fake far off-screen position, keep the last real one instead
        // (also skips maximized, whose size isn't the one to restore)
        if (ViewModel is not null && WindowState == WindowState.Normal)
        {
            var sts = ViewModel.ApplicationSettings;

            sts.WindowX = Position.X;
            sts.WindowY = Position.Y;
            sts.WindowWidth = ClientSize.Width;
            sts.WindowHeight = ClientSize.Height;
        }

        // Also when maximized: "Don't ask again" must be kept
        ViewModel?.ApplicationSettings.TrySave();

        base.OnClosing(e);
    }

    // Opening the dialog takes the focus off the description box, which sends a description still being typed
    private async Task ConfirmCloseAsync(IApplicationSettings settings)
    {
        var dontAskAgain = new CheckBox
        {
            Content = "Don't ask again"
        };

        // Same button layout as the delete dialog: Cancel left, the action right and default
        var dialog = new FAContentDialog
        {
            Title = "Exit Werfer?",
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "A description still being typed may not reach Clockify." },
                    dontAskAgain
                }
            },
            PrimaryButtonText = "Cancel",
            SecondaryButtonText = "Quit",
            DefaultButton = FAContentDialogButton.Secondary
        };

        if (await dialog.ShowAsync() != FAContentDialogResult.Secondary)
        {
            return;
        }

        if (dontAskAgain.IsChecked == true)
        {
            settings.ConfirmClose = false;
        }

        // OnClosing then saves the window and settings without asking again
        _closeConfirmed = true;
        Close();
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
        SelectPageItem();
    }

    // The item of the page shown, More for a page of its menu
    private void SelectPageItem()
    {
        if (ViewModel is null)
        {
            return;
        }

        var items = Navigation.MenuItems
            .Concat(Navigation.FooterMenuItems);

        Navigation.SelectedItem = items
            .OfType<FANavigationViewItem>()
            .FirstOrDefault(i => i.Tag is AppPageEnum page && page == ViewModel.Page) ?? MoreItem;
    }
}
