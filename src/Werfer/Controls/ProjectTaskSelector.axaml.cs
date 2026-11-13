using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Werfer.ViewModels;

namespace Werfer.Controls;

/// <summary>
///     A project and task picked by typing: suggestions appear after 3 letters, with the project's color.
/// </summary>
public partial class ProjectTaskSelector : UserControl
{
    public static readonly StyledProperty<Func<string?, CancellationToken, Task<IEnumerable<object>>>?>
        AsyncPopulatorProperty = AvaloniaProperty.Register<ProjectTaskSelector,
            Func<string?, CancellationToken, Task<IEnumerable<object>>>?>(nameof(AsyncPopulator));

    public static readonly StyledProperty<ProjectTaskViewModel?> SelectedItemProperty =
        AvaloniaProperty.Register<ProjectTaskSelector, ProjectTaskViewModel?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay
        );

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ProjectTaskSelector, string?>(
            nameof(Text),
            defaultBindingMode: BindingMode.TwoWay
        );

    public ProjectTaskSelector()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gives the suggestions for what is typed.
    /// </summary>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>>? AsyncPopulator
    {
        get => GetValue(AsyncPopulatorProperty);
        set => SetValue(AsyncPopulatorProperty, value);
    }

    /// <summary>
    ///     The project and task picked; null while nothing is picked or the user is typing.
    /// </summary>
    public ProjectTaskViewModel? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    ///     The text in the box: what is typed, or the pick's label.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty)
        {
            Dot.Fill = SelectedItem?.Brush;
            Dot.IsVisible = SelectedItem is not null;
        }
    }
}
