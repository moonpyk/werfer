using System.Reflection;

namespace Werfer;

public static class Version
{
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
