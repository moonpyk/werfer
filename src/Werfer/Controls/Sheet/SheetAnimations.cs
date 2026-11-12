using JetBrains.Annotations;

namespace Werfer.Controls.Sheet;

/// <summary>
///     Which transitions of a <see cref="Sheet" /> are animated. Flags, so opening and closing can be turned on
///     separately.
/// </summary>
[Flags]
[PublicAPI]
public enum SheetAnimations
{
    /// <summary>
    ///     No animation: the sheet shows and hides instantly.
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
