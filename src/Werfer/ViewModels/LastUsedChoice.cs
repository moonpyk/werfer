namespace Werfer.ViewModels;

/// <summary>
///     The "Last used project and task" item of the default project box, next to the real project and task pairs.
/// </summary>
public sealed class LastUsedChoice
{
    private LastUsedChoice()
    {
    }

    public static LastUsedChoice Instance { get; } = new();

    /// <summary>
    ///     Text shown in the box once picked, and matched against what is typed
    /// </summary>
    public override string ToString()
    {
        return "Last used project and task";
    }
}
