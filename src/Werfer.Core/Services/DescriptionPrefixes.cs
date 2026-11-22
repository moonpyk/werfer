namespace Werfer.Services;

/// <summary>
///     Removes the prefixes listed in the settings from a text pasted in the description.
/// </summary>
public static class DescriptionPrefixes
{
    /// <summary>
    ///     Removes from the start of <paramref name="text" /> every prefix of <paramref name="prefixes" />, ignoring the
    ///     case, as long as one matches, with the spaces around them.
    /// </summary>
    /// <param name="text">The pasted text.</param>
    /// <param name="prefixes">Literal prefixes, not patterns; empty ones are ignored.</param>
    /// <returns><paramref name="text" /> unchanged when no prefix matches, else what follows the prefixes.</returns>
    public static string Strip(string text, IReadOnlyCollection<string> prefixes)
    {
        var result = text.TrimStart();
        var strippedAny = false;
        bool stripped;

        do
        {
            stripped = false;

            foreach (var prefix in prefixes)
            {
                if (prefix.Length == 0 || !result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result = result[prefix.Length..].TrimStart();
                stripped = strippedAny = true;
            }
        } while (stripped);

        return strippedAny ? result : text;
    }
}
