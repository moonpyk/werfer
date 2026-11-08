using JetBrains.Annotations;

namespace Werfer.Controls.Sheet;

/// <summary>
///     The transitions a <see cref="Sheet" /> animates; flags, to keep one direction only.
/// </summary>
[Flags]
[PublicAPI]
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
