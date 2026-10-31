using System.Globalization;
using Avalonia.Controls;

namespace SlClockify.Utils;

public static class AutoComplete
{
    /// <summary>
    ///     Every typed word must appear, in any order, ignoring case, accents and punctuation: "securite pole" finds "Pôle
    ///     sécurité"
    /// </summary>
    public static AutoCompleteFilterPredicate<object?> ProjectAutoCompleteFilterPredicate
    {
        get
        {
            return (search, item) =>
                (search ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .All(word => CultureInfo.InvariantCulture.CompareInfo.IndexOf(
                    item?.ToString() ?? string.Empty,
                    word,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols
                ) >= 0);
        }
    }
}
