using Avalonia.Media;

namespace Werfer.Utils;

/// <summary>
///     Makes a color readable as text on a given background while keeping its hue.
/// </summary>
/// <remarks>
///     <para>
///         The readability criterion is the WCAG 2 contrast ratio (see <see cref="Contrast(Color, Color)" />). When a
///         color
///         falls short, only its lightness is changed, in the OKLab perceptual color space: its a/b coordinates, so its
///         OKLCH chroma and hue, stay the same. In HSL, the same operation visibly shifts the hue (a darkened yellow turns
///         olive, a darkened blue drifts to purple), because HSL lightness doesn't match perceived lightness.
///     </para>
///     <para>Sources:</para>
///     <list type="bullet">
///         <item>
///             WCAG 2.2, relative luminance and contrast ratio definitions:
///             https://www.w3.org/TR/WCAG22/#dfn-relative-luminance and https://www.w3.org/TR/WCAG22/#dfn-contrast-ratio
///         </item>
///         <item>
///             WCAG 2.2, success criterion 1.4.3 Contrast (Minimum), the 4.5:1 threshold:
///             https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html
///         </item>
///         <item>
///             sRGB transfer function, IEC 61966-2-1:1999, as summarized in
///             https://en.wikipedia.org/wiki/SRGB#Transfer_function_(%22gamma%22)
///         </item>
///         <item>
///             OKLab, Björn Ottosson, "A perceptual color space for image processing" (2020): the matrices and the
///             structure of <see cref="ToOkLab" /> and <see cref="FromOkLab" /> are taken from the reference code of
///             https://bottosson.github.io/posts/oklab/
///         </item>
///         <item>
///             OKLCH as the polar form of OKLab, CSS Color Module Level 4: https://www.w3.org/TR/css-color-4/#ok-lab
///         </item>
///     </list>
///     <para>
///         <see cref="EnsureContrast" />'s binary search and the black/white switch point are this project's own,
///         derived from the definitions above.
///     </para>
/// </remarks>
/// <seealso href="https://bottosson.github.io/posts/oklab/">Björn Ottosson, A perceptual color space for image processing</seealso>
/// <seealso href="https://www.w3.org/TR/WCAG22/#dfn-contrast-ratio">WCAG 2.2, contrast ratio</seealso>
public static class ReadableColor
{
    /// <summary>
    ///     WCAG 2 level AA minimum contrast ratio for normal-size text (success criterion 1.4.3).
    ///     Large or bold text only needs 3:1, level AAA asks for 7:1.
    /// </summary>
    public const double MinTextContrast = 4.5;

    /// <summary>
    ///     Relative luminance at which black and white text contrast equally with a background.
    ///     Solving <c>(L + 0.05) / 0.05 = 1.05 / (L + 0.05)</c> gives <c>L = √0.0525 − 0.05 ≈ 0.179</c>.
    ///     Above it a background is "light" (dark text reads best), below it "dark" (light text reads best).
    /// </summary>
    private const double BlackWhiteSwitchLuminance = 0.179;

    /// <summary>
    ///     Number of halvings in <see cref="EnsureContrast" />'s binary search: 2^-24 on a 0..1 lightness is far finer
    ///     than the 8-bit channels it ends up in, so more steps wouldn't change the result.
    /// </summary>
    private const int SearchSteps = 24;

    /// <summary>
    ///     Returns a color with the same hue as <paramref name="color" /> that reaches <paramref name="minContrast" />
    ///     against <paramref name="background" />.
    /// </summary>
    /// <remarks>
    ///     A color that already passes is returned unchanged. Otherwise its OKLab lightness is moved toward black on a
    ///     light background, or toward white on a dark one, by binary search between the original lightness (failing)
    ///     and that extreme. The result is the passing lightness closest to the original, so the color changes as little
    ///     as possible.
    ///     <para>
    ///         Black or white always reach at least 4.58:1 against a background on their side of
    ///         <see cref="BlackWhiteSwitchLuminance" /> (the worst case, at the switch point, is 0.229 / 0.05), so 4.5:1 is
    ///         always reachable. A higher <paramref name="minContrast" /> may not be on mid-tone backgrounds: the search then
    ///         ends on black or white, the best contrast possible.
    ///     </para>
    /// </remarks>
    /// <param name="color">Text color to make readable, typically a user-chosen project color.</param>
    /// <param name="background">Color the text is drawn on. Its alpha is ignored, pass an opaque color.</param>
    /// <param name="minContrast">WCAG contrast ratio to reach, from 1 to 21.</param>
    /// <returns>
    ///     An opaque color reaching <paramref name="minContrast" /> against <paramref name="background" />, or the closest
    ///     to it that exists when it can't be reached (see remarks).
    /// </returns>
    public static Color EnsureContrast(Color color, Color background, double minContrast = MinTextContrast)
    {
        var backgroundLuminance = Luminance(background);

        if (Contrast(Luminance(color), backgroundLuminance) >= minContrast)
        {
            return color;
        }

        var (lightness, a, b) = ToOkLab(color);

        // Darken toward black on a light background, lighten toward white on a dark one
        var passing = backgroundLuminance > BlackWhiteSwitchLuminance ? 0.0 : 1.0;
        var failing = lightness;

        for (var i = 0; i < SearchSteps; i++)
        {
            var middle = (passing + failing) / 2;

            if (Contrast(Luminance(FromOkLab(middle, a, b)), backgroundLuminance) >= minContrast)
            {
                passing = middle;
            }
            else
            {
                failing = middle;
            }
        }

        return FromOkLab(passing, a, b);
    }

    /// <summary>
    ///     WCAG 2 contrast ratio between two colors: <c>(L1 + 0.05) / (L2 + 0.05)</c>, L1 being the relative luminance of
    ///     the lighter one. Ranges from 1 (identical luminance) to 21 (black on white). The order of the arguments
    ///     doesn't matter and alpha is ignored.
    /// </summary>
    /// <param name="first">One of the two colors.</param>
    /// <param name="second">The other color.</param>
    /// <returns>The contrast ratio, between 1 and 21.</returns>
    public static double Contrast(Color first, Color second)
    {
        return Contrast(Luminance(first), Luminance(second));
    }

    /// <summary>
    ///     WCAG 2 contrast ratio from two relative luminances already computed, see <see cref="Contrast(Color, Color)" />.
    ///     The 0.05 offset accounts for ambient light reflected by the screen (flare), per the WCAG definition.
    /// </summary>
    private static double Contrast(double firstLuminance, double secondLuminance)
    {
        return (Math.Max(firstLuminance, secondLuminance) + 0.05) / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    /// <summary>
    ///     WCAG 2 relative luminance of an sRGB color, from 0 (black) to 1 (white): the linear channels weighted by the
    ///     eye's sensitivity to each primary (ITU-R BT.709 coefficients, the ones of sRGB).
    /// </summary>
    private static double Luminance(Color color)
    {
        return 0.2126 * ToLinear(color.R) + 0.7152 * ToLinear(color.G) + 0.0722 * ToLinear(color.B);
    }

    /// <summary>
    ///     sRGB decoding (IEC 61966-2-1): turns an 8-bit gamma-encoded channel into linear light, from 0 to 1.
    /// </summary>
    /// <remarks>
    ///     Uses the 0.04045 threshold of the sRGB standard. WCAG 2.0 printed 0.03928, from an older sRGB draft; for 8-bit
    ///     channels both give the same result, since no value falls between them (10/255 ≈ 0.0392 is below both,
    ///     11/255 ≈ 0.0431 above both).
    /// </remarks>
    private static double ToLinear(byte channel)
    {
        var c = channel / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    ///     sRGB encoding (IEC 61966-2-1), the inverse of <see cref="ToLinear" />: turns linear light into an 8-bit
    ///     gamma-encoded channel.
    /// </summary>
    /// <remarks>
    ///     Values outside 0..1, which OKLab produces for colors outside the sRGB gamut, are clipped per channel. That can
    ///     shift the hue of a very saturated color slightly; reducing the chroma instead would keep it exact, at the cost
    ///     of a gamut-mapping algorithm this use doesn't need.
    /// </remarks>
    private static byte FromLinear(double linear)
    {
        var c = Math.Clamp(linear, 0, 1);
        c = c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
        return (byte)Math.Round(c * 255);
    }

    /// <summary>
    ///     Converts an sRGB color to OKLab: linear sRGB, then an approximate cone response (LMS), a cube root, and a
    ///     final matrix. L is the perceived lightness (0 black, 1 white), a and b the green-red and blue-yellow axes.
    /// </summary>
    /// <remarks>
    ///     Matrices and steps from Björn Ottosson's reference implementation, https://bottosson.github.io/posts/oklab/
    ///     (function <c>linear_srgb_to_oklab</c>), with the sRGB decoding of <see cref="ToLinear" /> applied first.
    /// </remarks>
    /// <returns>The OKLab coordinates, L in 0..1, a and b roughly in -0.4..0.4.</returns>
    private static (double L, double A, double B) ToOkLab(Color color)
    {
        double r = ToLinear(color.R), g = ToLinear(color.G), b = ToLinear(color.B);

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        return (
            0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s
        );
    }

    /// <summary>
    ///     Converts OKLab coordinates back to an opaque sRGB color, the inverse of <see cref="ToOkLab" />.
    /// </summary>
    /// <remarks>
    ///     Matrices and steps from Björn Ottosson's reference implementation, https://bottosson.github.io/posts/oklab/
    ///     (function <c>oklab_to_linear_srgb</c>), followed by the sRGB encoding and gamut clipping of
    ///     <see cref="FromLinear" />.
    /// </remarks>
    /// <param name="lightness">OKLab L, from 0 (black) to 1 (white).</param>
    /// <param name="a">OKLab a, the green (negative) to red (positive) axis.</param>
    /// <param name="b">OKLab b, the blue (negative) to yellow (positive) axis.</param>
    private static Color FromOkLab(double lightness, double a, double b)
    {
        var l = Math.Pow(lightness + 0.3963377774 * a + 0.2158037573 * b, 3);
        var m = Math.Pow(lightness - 0.1055613458 * a - 0.0638541728 * b, 3);
        var s = Math.Pow(lightness - 0.0894841775 * a - 1.2914855480 * b, 3);

        return Color.FromRgb(
            FromLinear(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s),
            FromLinear(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s),
            FromLinear(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s)
        );
    }
}
