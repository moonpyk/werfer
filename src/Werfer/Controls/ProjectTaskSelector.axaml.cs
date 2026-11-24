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

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ProjectTaskSelector, object?>(
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

        // The box shows this control's properties. Bound here rather than in the XAML, see AGENTS.md
        Box[!AutoCompleteBox.AsyncPopulatorProperty] = this[!AsyncPopulatorProperty];
        Box[!!AutoCompleteBox.SelectedItemProperty] = this[!!SelectedItemProperty];
        Box[!!AutoCompleteBox.TextProperty] = this[!!TextProperty];
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
    ///     The item picked: a <see cref="ProjectTaskViewModel" />, or any other item the populator gives, such as
    ///     <see cref="LastUsedChoice" />. Null while nothing is picked or the user is typing.
    /// </summary>
    public object? SelectedItem
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
            var choice = SelectedItem as ProjectTaskViewModel;
            Dot.Fill = choice?.Brush;
            Dot.IsVisible = choice is not null;
        }
    }
}
