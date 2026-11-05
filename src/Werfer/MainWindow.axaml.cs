using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Werfer.Services;
using Werfer.Utils;
using Werfer.ViewModels;

namespace Werfer;

public partial class MainWindow : Window
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

    public MainWindow(
        MainWindowViewModel vm,
        ILogger<MainWindow> logger
    )
    {
        Initialize();

        Logger = logger;
        DataContext = ViewModel = vm;

        this.RestoreWidth(
            vm.ApplicationSettings
        ).RestorePosition(
            vm.ApplicationSettings
        );
    }

    private ILogger<MainWindow>? Logger { get; }

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
        // (also skips maximized, whose width isn't the one to restore)
        if (ViewModel is not null && WindowState == WindowState.Normal)
        {
            var sts = ViewModel.ApplicationSettings;

            sts.WindowX = Position.X;
            sts.WindowY = Position.Y;
            sts.WindowWidth = ClientSize.Width;
            sts.Save();
        }

        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e)
        {
            case
            {
                Key: Key.Space,
                KeyModifiers: KeyModifiers.Control
            } when ViewModel is not null:
                e.Handled = true;

                // Checked before Focus(): committing the description starts an update, which sets IsBusy
                if (ViewModel.Timer.IsBusy)
                {
                    return;
                }

                // Commits the description being typed, it's only pushed on lost focus
                Focus();
                try
                {
                    ViewModel.Timer.ToggleAsync();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "While toggling");
                }

                return;

            case
            {
                Key: Key.F5,
                KeyModifiers: KeyModifiers.None
            } when ViewModel is not null:
                // Pushes the description being typed first, the refresh would overwrite it otherwise
                Focus();
                try
                {
                    ViewModel.Timer.RefreshAsync();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "While refreshing");
                }

                e.Handled = true;
                return;

            default:
                base.OnKeyDown(e);
                break;
        }
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        ViewModel?.Navigate(e.IsSettingsSelected);
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
        if (e.InvokedItemContainer != RefreshItem)
        {
            return;
        }

        try
        {
            ViewModel?.Timer.RefreshAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "While refreshing");
        }
    }
}
