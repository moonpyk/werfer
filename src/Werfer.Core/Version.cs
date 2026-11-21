using System.Reflection;

namespace Werfer;

public static class Version
{
    /// <summary>
    ///     The version and the short commit it was built from, such as "0.6.0 (487f83e)", for the About page.
    /// </summary>
    public static string Display =>
        $"v{typeof(Version).Assembly.GetName().Version?.ToString(3)}@{GitCommit[..Math.Min(7, GitCommit.Length)]}";

    public static string GitCommit
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            return field = typeof(Version).Assembly.GetCustomAttribute<
                AssemblyInformationalVersionAttribute
            >()?.InformationalVersion ?? "0000000000000000000000000000000000000000";
        }
    }
}
