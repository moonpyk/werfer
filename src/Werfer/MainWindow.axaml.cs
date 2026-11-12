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
    // True once the user clicked Quit in the confirmation dialog, see ConfirmCloseAsync
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

        // When a shortcut (Ctrl+,) changes the page, select its navigation item, as a click would
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
        // FAAppWindow's TitleBarBackground resource is empty, so the title bar has no background. Without a
        // background it ignores the mouse, and only the icon and title text could drag the window
        TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = Colors.Transparent;
        // Same page as the one MainWindowViewModel starts on
        Navigation.SelectedItem = TimerItem;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Our own flag, not e.IsProgrammatic: FAAppWindow's close button also calls Close() from code.
        // No dialog when Windows shuts down, it must not wait
        if (ViewModel is { ApplicationSettings.ConfirmClose: true }
            && !_closeConfirmed
            && e.CloseReason != WindowCloseReason.OSShutdown)
        {
            e.Cancel = true;
            _ = ConfirmCloseAsync(ViewModel.ApplicationSettings);
            return;
        }

        // Saved only for a normal window: a minimized one reports a fake position far off screen, and a maximized
        // one's size is not the size to restore
        if (ViewModel is not null && WindowState == WindowState.Normal)
        {
            var sts = ViewModel.ApplicationSettings;

            sts.WindowX = Position.X;
            sts.WindowY = Position.Y;
            sts.WindowWidth = ClientSize.Width;
            sts.WindowHeight = ClientSize.Height;
        }

        // Saved in every window state, so a "Don't ask again" ticked while maximized is kept
        ViewModel?.ApplicationSettings.TrySave();

        base.OnClosing(e);
    }

    // Opening the dialog takes the focus away from the description box, which sends any description being typed
    private async Task ConfirmCloseAsync(IApplicationSettings settings)
    {
        var dontAskAgain = new CheckBox
        {
            Content = "Don't ask again"
        };

        // Same buttons as the delete dialog: Cancel on the left, the action on the right and pressed by Enter
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

        // Close again: OnClosing sees the flag, saves the window and settings, and doesn't ask
        _closeConfirmed = true;
        Close();
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        // More has no Tag: it only gets selected after OnMorePageClick already changed the page
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

    // A page picked in the More menu. More stays selected, the click that opened the menu selected it
    private void OnMorePageClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: AppPageEnum page })
        {
            ViewModel?.Navigate(page);
        }
    }

    // The More menu closed: select the item of the page shown again, in case More was selected without a pick
    private void OnMoreMenuClosed(object? sender, EventArgs e)
    {
        SelectPageItem();
    }

    // Selects the navigation item of the page shown; for a page of the More menu, that is More
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
