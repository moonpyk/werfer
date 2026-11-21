using Avalonia.Controls;
using Avalonia.Platform;

namespace Werfer.Views;

/// <summary>
///     The About page: the version and the keyboard shortcuts. Static, no view model.
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();

        ThanksText.Text = ReadThanks();
        ThanksText.IsVisible = ThanksText.Text.Length > 0;
    }

    // Assets/Thanks.txt shown as written, without its # comment lines
    private static string ReadThanks()
    {
        using var reader = new StreamReader(
            AssetLoader.Open(new Uri("avares://Werfer/Assets/Thanks.txt"))
        );

        var lines = reader
            .ReadToEnd()
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => !line.TrimStart().StartsWith('#'));

        return string.Join(
            '\n',
            lines
        ).Trim();
    }
}
