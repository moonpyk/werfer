using System.Globalization;
using System.Reflection;
using Refit;

namespace SlClockify.Core.Clockify;

/// <inheritdoc />
public class ClockifyUrlParameterFormatter : DefaultUrlParameterFormatter
{
    /// <inheritdoc />
    public override string? Format(
        object? value,
        ICustomAttributeProvider attributeProvider,
        Type type
    )
    {
        return value is DateTimeOffset date
                ? date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                : base.Format(value, attributeProvider, type)
            ;
    }
}
