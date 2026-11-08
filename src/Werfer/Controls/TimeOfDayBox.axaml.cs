using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Werfer.Services;

namespace Werfer.Controls;

/// <summary>
///     A time of day typed as text, see <see cref="TimeEntryHistory.TryParseTimeOfDay" />, with up and down chevrons.
/// </summary>
public partial class TimeOfDayBox : UserControl
{
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<TimeOfDayBox, string?>(
        nameof(Text),
        defaultBindingMode: BindingMode.TwoWay
    );

    public TimeOfDayBox()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // Wraps around midnight; an unreadable time is left for the user to fix
    private void OnSpin(object? sender, RoutedEventArgs e)
    {
        if (TimeEntryHistory.TryParseTimeOfDay(Text, out var time))
        {
            Text = TimeEntryHistory.FormatTimeOfDay(time.AddMinutes(sender is Control { Tag: "-1" } ? -1 : 1));
        }
    }
}
