using JetBrains.Annotations;

namespace Werfer.Controls.Sheet;

/// <summary>
///     The edge of its parent a <see cref="Sheet" /> is docked to, and slides in from.
/// </summary>
[PublicAPI]
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
