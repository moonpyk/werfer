using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using SlClockify.ViewModels;

namespace SlClockify;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Matches the page MainWindowViewModel opens on
        Navigation.SelectedItem = TimerItem;
    }

    private void OnNavigationSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.Navigate(e.IsSettingsSelected);
    }
}
