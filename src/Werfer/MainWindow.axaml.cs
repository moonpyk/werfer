using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using JetBrains.Annotations;
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

    public MainWindow(MainWindowViewModel vm)
    {
        Initialize();
        DataContext = ViewModel = vm;
    }

    private MainWindowViewModel? ViewModel { get; }

    private void Initialize()
    {
        InitializeComponent();

        // Matches the page MainWindowViewModel opens on
        Navigation.SelectedItem = TimerItem;
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        ViewModel?.Navigate(e.IsSettingsSelected);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e is { Key: Key.Space, KeyModifiers: KeyModifiers.Control } && ViewModel is not null)
        {
            // Commits the description being typed, it's only pushed on lost focus
            Focus();
            _ = ViewModel.Timer.ToggleAsync();
            e.Handled = true;
            return;
        }

        if (e is { Key: Key.F5, KeyModifiers: KeyModifiers.None } && ViewModel is not null)
        {
            // Pushes the description being typed first, the refresh would overwrite it otherwise
            Focus();
            _ = ViewModel.Timer.RefreshAsync();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnNavigationItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer == RefreshItem)
        {
            _ = ViewModel?.Timer.RefreshAsync();
        }
    }
}
