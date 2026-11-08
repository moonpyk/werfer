using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Werfer.Controls;

/// <summary>
///     The edge of its parent a <see cref="Sheet" /> is docked to, and slides in from.
/// </summary>
public enum SheetPlacement
{
    /// <summary>
    ///     Docked to the bottom edge, sliding up.
    /// </summary>
    Bottom,

    /// <summary>
    ///     Docked to the top edge, sliding down.
    /// </summary>
    Top
}

/// <summary>
///     The transitions a <see cref="Sheet" /> animates; flags, to keep one direction only.
/// </summary>
[Flags]
public enum SheetAnimations
{
    /// <summary>
    ///     Shown and hidden at once.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The backdrop fades in and the sheet slides in from its edge.
    /// </summary>
    Opening = 1,

    /// <summary>
    ///     The backdrop fades out and the sheet slides back out to its edge.
    /// </summary>
    Closing = 2,

    /// <summary>
    ///     Both <see cref="Opening" /> and <see cref="Closing" />.
    /// </summary>
    All = Opening | Closing
}

/// <summary>
///     A modal sheet docked to the top or bottom edge of its parent, sliding in over a dimmed backdrop.
/// </summary>
/// <remarks>
///     <para>
///         Place it last in a <see cref="Panel" /> or a single-cell <see cref="Grid" /> holding the content it should
///         cover, so it stretches over that content. While <see cref="IsOpen" /> is <see langword="false" />, nothing
///         is shown and nothing is hit-tested.
///     </para>
///     <para>
///         When open, the backdrop covers the whole control and catches pointer input meant for the content below.
///         The sheet itself spans the full width, is docked to the edge set by <see cref="Placement" /> and is sized
///         by its <see cref="ContentControl.Content" />, inside <see cref="TemplatedControl.Padding" />.
///     </para>
///     <para>
///         Opening fades the backdrop in and slides the sheet in from its edge; closing reverses both. Each of the
///         four can be replaced by an <see cref="Animation" /> of one's own: <see cref="SheetOpeningAnimation" />,
///         <see cref="SheetClosingAnimation" />, <see cref="BackdropOpeningAnimation" /> and
///         <see cref="BackdropClosingAnimation" />; <see cref="Animations" /> turns opening or closing animations off.
///     </para>
///     <para>
///         A press on the backdrop closes the sheet (light dismiss), which is why <see cref="IsOpen" /> binds
///         two-way by default. Any other way to close it, such as the Escape key or buttons in its content, is left
///         to the caller.
///     </para>
///     <para>
///         The control is self-contained: its template is built in code, so it needs no style or resource
///         dictionary to be included. The sheet's background and border use the Fluent theme resources
///         <c>SolidBackgroundFillColorBaseBrush</c> and <c>CardStrokeColorDefaultBrush</c>, and follow theme
///         changes; outside a Fluent theme, define resources with these keys.
///     </para>
/// </remarks>
/// <example>
///     <code language="xml"><![CDATA[
/// <Panel>
///     <local:PageContent />
///     <local:Sheet IsOpen="{Binding IsEditorOpen}" Placement="Bottom">
///         <local:EditorForm />
///     </local:Sheet>
/// </Panel>
/// ]]></code>
/// </example>
public class Sheet : ContentControl
{
    /// <summary>
    ///     Defines the <see cref="IsOpen" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<Sheet, bool>(
        nameof(IsOpen),
        defaultBindingMode: BindingMode.TwoWay
    );

    /// <summary>
    ///     Defines the <see cref="Placement" /> property.
    /// </summary>
    public static readonly StyledProperty<SheetPlacement> PlacementProperty =
        AvaloniaProperty.Register<Sheet, SheetPlacement>(nameof(Placement));

    /// <summary>
    ///     Defines the <see cref="Animations" /> property.
    /// </summary>
    public static readonly StyledProperty<SheetAnimations> AnimationsProperty =
        AvaloniaProperty.Register<Sheet, SheetAnimations>(nameof(Animations), SheetAnimations.All);

    /// <summary>
    ///     Defines the <see cref="SheetOpeningAnimation" /> property.
    /// </summary>
    public static readonly StyledProperty<Animation?> SheetOpeningAnimationProperty =
        AvaloniaProperty.Register<Sheet, Animation?>(nameof(SheetOpeningAnimation));

    /// <summary>
    ///     Defines the <see cref="SheetClosingAnimation" /> property.
    /// </summary>
    public static readonly StyledProperty<Animation?> SheetClosingAnimationProperty =
        AvaloniaProperty.Register<Sheet, Animation?>(nameof(SheetClosingAnimation));

    /// <summary>
    ///     Defines the <see cref="BackdropOpeningAnimation" /> property.
    /// </summary>
    public static readonly StyledProperty<Animation?> BackdropOpeningAnimationProperty =
        AvaloniaProperty.Register<Sheet, Animation?>(nameof(BackdropOpeningAnimation));

    /// <summary>
    ///     Defines the <see cref="BackdropClosingAnimation" /> property.
    /// </summary>
    public static readonly StyledProperty<Animation?> BackdropClosingAnimationProperty =
        AvaloniaProperty.Register<Sheet, Animation?>(nameof(BackdropClosingAnimation));

    // Half Fluent's dialog smoke (SmokeFillColorDefault, 30 % black), which is too dark for a sheet; Fluent's
    // light-dismiss overlays are transparent. A brush either way, so the backdrop is hit-tested
    private static readonly IBrush BackdropBrush = new SolidColorBrush(Color.FromArgb(0x26, 0, 0, 0));

    // Shared, an animation holds no per-target state. In from the sheet's edge, decelerating; out to it, faster and
    // accelerating, as Fluent's own transitions
    private static readonly Animation FadeIn = Tween(200, new CubicEaseOut(), [(OpacityProperty, 0, 1)]);
    private static readonly Animation FadeOut = Tween(150, new CubicEaseIn(), [(OpacityProperty, 1, 0)]);
    private static readonly Animation SlideInUp = SlideIn(80);
    private static readonly Animation SlideInDown = SlideIn(-80);
    private static readonly Animation SlideOutDown = SlideOut(80);
    private static readonly Animation SlideOutUp = SlideOut(-80);

    private Border? _backdrop;
    private Panel? _root;
    private Border? _sheet;

    // Cancelled by the next transition: also releases the values the previous one holds (FillMode.Forward)
    private CancellationTokenSource? _transition;

    static Sheet()
    {
        PaddingProperty.OverrideDefaultValue<Sheet>(new Thickness(12));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Sheet" /> class, with its built-in template.
    /// </summary>
    /// <remarks>
    ///     The template has four named parts: <c>PART_Root</c> (the <see cref="Panel" /> shown while open),
    ///     <c>PART_Backdrop</c> (a <see cref="Border" /> covering the control), <c>PART_Sheet</c> (the docked
    ///     <see cref="Border" />) and <c>PART_ContentPresenter</c>. Setting <see cref="TemplatedControl.Template" />
    ///     replaces it; a replacement should provide <c>PART_Root</c> to be shown and hidden, <c>PART_Backdrop</c> for
    ///     light dismiss and <c>PART_Sheet</c> for the docking; transitions run on those found.
    /// </remarks>
    public Sheet()
    {
        Template = new FuncControlTemplate<Sheet>((owner, scope) =>
        {
            var sheet = new Border
            {
                Name = "PART_Sheet",
                // Both, so custom animations can scale it as well as move it
                RenderTransform = new TransformGroup
                {
                    Children = { new ScaleTransform(), new TranslateTransform() }
                },
                [!Decorator.PaddingProperty] = owner[!PaddingProperty],
                Child = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = owner[!ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = owner[!ContentTemplateProperty]
                }.RegisterInNameScope(scope)
            }.RegisterInNameScope(scope);

            // Theme brushes, followed when the theme changes
            sheet.Bind(
                Border.BackgroundProperty,
                sheet.GetResourceObservable("SolidBackgroundFillColorBaseBrush")
            );
            sheet.Bind(
                Border.BorderBrushProperty,
                sheet.GetResourceObservable("CardStrokeColorDefaultBrush")
            );

            // Shown and hidden in code, around the transitions, see TransitionAsync
            return new Panel
            {
                Name = "PART_Root",
                Children =
                {
                    new Border { Name = "PART_Backdrop", Background = BackdropBrush }.RegisterInNameScope(scope),
                    sheet
                }
            }.RegisterInNameScope(scope);
        });
    }

    /// <summary>
    ///     Gets or sets whether the sheet is shown.
    /// </summary>
    /// <value>
    ///     <see langword="true" /> to fade the backdrop in and slide the sheet in from its edge;
    ///     <see langword="false" /> to fade it out and slide the sheet back out, which no longer catches pointer
    ///     input meanwhile. Either at once instead, as set by <see cref="Animations" />. The default is
    ///     <see langword="false" />.
    /// </value>
    /// <remarks>
    ///     Binds two-way by default: a press on the backdrop sets it to <see langword="false" />, through
    ///     <see cref="AvaloniaObject.SetCurrentValue{T}(StyledProperty{T}, T)" />, so a binding on it is kept and its
    ///     source updated.
    /// </remarks>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    ///     Gets or sets the edge the sheet is docked to and slides in from.
    /// </summary>
    /// <value>
    ///     The default is <see cref="SheetPlacement.Bottom" />. The sheet's corners are rounded and its border drawn
    ///     on the side facing the content, away from that edge.
    /// </value>
    public SheetPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <summary>
    ///     Gets or sets which of the opening and closing transitions are animated.
    /// </summary>
    /// <value>
    ///     The default is <see cref="SheetAnimations.All" />. A transition left out shows or hides the backdrop and
    ///     the sheet at once; one under way when this changes runs to its end.
    /// </value>
    public SheetAnimations Animations
    {
        get => GetValue(AnimationsProperty);
        set => SetValue(AnimationsProperty, value);
    }

    /// <summary>
    ///     Gets or sets the animation run on the sheet when it opens.
    /// </summary>
    /// <value>
    ///     The default is <see langword="null" />: the sheet slides in from the edge set by <see cref="Placement" />
    ///     and fades in.
    /// </value>
    /// <remarks>
    ///     The sheet's <see cref="Visual.RenderTransform" /> is a <see cref="TransformGroup" /> of a
    ///     <see cref="ScaleTransform" /> and a <see cref="TranslateTransform" />, so setters on properties of either,
    ///     such as <c>ScaleTransform.ScaleY</c> or <c>TranslateTransform.Y</c>, animate it. Use
    ///     <see cref="FillMode.Forward" /> to keep the final values once it ends.
    /// </remarks>
    /// <example>
    ///     <code language="xml"><![CDATA[
    /// <local:Sheet.SheetOpeningAnimation>
    ///     <Animation Duration="0:0:0.3" Easing="BackEaseOut" FillMode="Forward">
    ///         <KeyFrame Cue="0%"><Setter Property="ScaleTransform.ScaleY" Value="0.8" /></KeyFrame>
    ///         <KeyFrame Cue="100%"><Setter Property="ScaleTransform.ScaleY" Value="1" /></KeyFrame>
    ///     </Animation>
    /// </local:Sheet.SheetOpeningAnimation>
    /// ]]></code>
    /// </example>
    public Animation? SheetOpeningAnimation
    {
        get => GetValue(SheetOpeningAnimationProperty);
        set => SetValue(SheetOpeningAnimationProperty, value);
    }

    /// <summary>
    ///     Gets or sets the animation run on the sheet when it closes, before it's hidden.
    /// </summary>
    /// <value>
    ///     The default is <see langword="null" />: the sheet slides back out to the edge set by
    ///     <see cref="Placement" /> and fades out.
    /// </value>
    /// <remarks>
    ///     Targets the same transforms as <see cref="SheetOpeningAnimation" />. Use <see cref="FillMode.Forward" />:
    ///     the sheet is only hidden once the animation ends, without it the sheet would flash back in place first.
    /// </remarks>
    public Animation? SheetClosingAnimation
    {
        get => GetValue(SheetClosingAnimationProperty);
        set => SetValue(SheetClosingAnimationProperty, value);
    }

    /// <summary>
    ///     Gets or sets the animation run on the backdrop when the sheet opens.
    /// </summary>
    /// <value>
    ///     The default is <see langword="null" />: the backdrop fades in.
    /// </value>
    public Animation? BackdropOpeningAnimation
    {
        get => GetValue(BackdropOpeningAnimationProperty);
        set => SetValue(BackdropOpeningAnimationProperty, value);
    }

    /// <summary>
    ///     Gets or sets the animation run on the backdrop when the sheet closes, before it's hidden.
    /// </summary>
    /// <value>
    ///     The default is <see langword="null" />: the backdrop fades out.
    /// </value>
    /// <remarks>
    ///     Use <see cref="FillMode.Forward" />, as for <see cref="SheetClosingAnimation" />.
    /// </remarks>
    public Animation? BackdropClosingAnimation
    {
        get => GetValue(BackdropClosingAnimationProperty);
        set => SetValue(BackdropClosingAnimationProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _backdrop?.PointerPressed -= OnBackdropPressed;
        _backdrop = e.NameScope.Find<Border>("PART_Backdrop");
        _backdrop?.PointerPressed += OnBackdropPressed;

        _sheet = e.NameScope.Find<Border>("PART_Sheet");
        ApplyPlacement();

        // Initial state, without transition
        _root = e.NameScope.Find<Panel>("PART_Root");
        _root?.IsVisible = IsOpen;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PlacementProperty)
        {
            ApplyPlacement();
        }
        else if (change.Property == IsOpenProperty)
        {
            _ = TransitionAsync(change.GetNewValue<bool>());
        }
    }

    // Animates the given properties of its target from their first value to their second
    private static Animation Tween(
        int milliseconds,
        Easing easing,
        (AvaloniaProperty Property, double From, double To)[] values
    )
    {
        var start = new KeyFrame { Cue = new Cue(0) };
        var end = new KeyFrame { Cue = new Cue(1) };

        foreach (var (property, from, to) in values)
        {
            start.Setters.Add(new Setter(property, from));
            end.Setters.Add(new Setter(property, to));
        }

        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            Easing = easing,
            FillMode = FillMode.Forward,
            Children = { start, end }
        };
    }

    // From `offset` (positive: below its place) and transparent, to its place and opaque
    private static Animation SlideIn(double offset)
    {
        return Tween(200, new CubicEaseOut(), [(TranslateTransform.YProperty, offset, 0), (OpacityProperty, 0, 1)]);
    }

    // The reverse, faster
    private static Animation SlideOut(double offset)
    {
        return Tween(150, new CubicEaseIn(), [(TranslateTransform.YProperty, 0, offset), (OpacityProperty, 1, 0)]);
    }

    // Shows the root then slides in, or slides out then hides it; a transition started meanwhile takes over
    private async Task TransitionAsync(bool open)
    {
        if (_root is null)
        {
            return;
        }

        // Also when not animated: a held FadeOut or SlideOut value would leave the opened sheet transparent
        _transition?.Cancel();
        var transition = _transition = new CancellationTokenSource();

        // Closing: clicks go through to the content right away, not after the animation
        _root.IsHitTestVisible = open;

        if (!Animations.HasFlag(open ? SheetAnimations.Opening : SheetAnimations.Closing))
        {
            _root.IsVisible = open;
            return;
        }

        if (open)
        {
            _root.IsVisible = true;
        }

        var top = Placement == SheetPlacement.Top;
        var animations = new List<Task>();

        if (_backdrop is not null)
        {
            var fade = open ? BackdropOpeningAnimation ?? FadeIn : BackdropClosingAnimation ?? FadeOut;
            animations.Add(fade.RunAsync(_backdrop, transition.Token));
        }

        if (_sheet is not null)
        {
            var slide = open
                ? SheetOpeningAnimation ?? (top ? SlideInDown : SlideInUp)
                : SheetClosingAnimation ?? (top ? SlideOutUp : SlideOutDown);
            animations.Add(slide.RunAsync(_sheet, transition.Token));
        }

        await Task.WhenAll(animations);

        if (!open && !transition.IsCancellationRequested)
        {
            _root.IsVisible = false;
        }
    }

    // Docks the sheet to its edge, its rounded corners and border on the side facing the content
    private void ApplyPlacement()
    {
        if (_sheet is null)
        {
            return;
        }

        var top = Placement == SheetPlacement.Top;

        _sheet.VerticalAlignment = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        _sheet.CornerRadius = top ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(8, 8, 0, 0);
        _sheet.BorderThickness = top ? new Thickness(0, 0, 0, 1) : new Thickness(0, 1, 0, 0);
    }

    // Light dismiss
    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        // Not SetValue: that would replace a binding instead of updating its source
        SetCurrentValue(IsOpenProperty, false);
        e.Handled = true;
    }
}
