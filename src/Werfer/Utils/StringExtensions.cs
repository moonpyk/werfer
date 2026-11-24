using System.Globalization;

namespace Werfer.Utils;

public static class StringExtensions
{
    /// <param name="input">The string whose first character is converted to uppercase.</param>
    extension(string input)
    {
        /// <summary>
        ///     Converts the first character of the specified string to uppercase while leaving the remaining characters unchanged.
        /// </summary>
        /// <returns>A string with the first character converted to uppercase.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input" /> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="input" /> is empty.</exception>
        public string FirstCharToUpper()
        {
            return input.FirstCharToUpper(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Converts the first character of the specified string to uppercase while leaving the remaining characters unchanged.
        /// </summary>
        /// <returns>A string with the first character converted to uppercase.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input" /> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="input" /> is empty.</exception>
        public string FirstCharToUpper(CultureInfo cultureInfo)
        {
            return input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(
                    input[0].ToString().ToUpper(cultureInfo),
                    input.AsSpan(1)
                )
            };
        }
    }
}
