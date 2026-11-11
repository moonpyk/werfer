using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Werfer.Controls;

/// <summary>
///     Who is connected: a round avatar, the name above the email.
/// </summary>
public partial class AccountCard : UserControl
{
    public static readonly StyledProperty<string?> UserNameProperty =
        AvaloniaProperty.Register<AccountCard, string?>(nameof(UserName));

    public static readonly StyledProperty<string?> EmailProperty =
        AvaloniaProperty.Register<AccountCard, string?>(nameof(Email));

    public static readonly StyledProperty<IImage?> AvatarProperty =
        AvaloniaProperty.Register<AccountCard, IImage?>(nameof(Avatar));

    public AccountCard()
    {
        InitializeComponent();
    }

    public string? UserName
    {
        get => GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    public string? Email
    {
        get => GetValue(EmailProperty);
        set => SetValue(EmailProperty, value);
    }

    /// <summary>
    ///     The profile picture; null shows a generic Contact symbol instead
    /// </summary>
    public IImage? Avatar
    {
        get => GetValue(AvatarProperty);
        set => SetValue(AvatarProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == UserNameProperty)
        {
            UserNameText.Text = UserName;
        }
        else if (change.Property == EmailProperty)
        {
            EmailText.Text = Email;
        }
        else if (change.Property == AvatarProperty)
        {
            AvatarImage.Source = Avatar;
            ContactIcon.IsVisible = Avatar is null;
        }
    }
}
