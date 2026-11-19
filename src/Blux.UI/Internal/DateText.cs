using System.Globalization;
using System.Text.RegularExpressions;

namespace Blux.UI.Internal;

/// <summary>Culture formatted dates for content components (blog posts, changelog versions).</summary>
internal static partial class DateText
{
    /// <summary>
    /// Formats with <paramref name="format"/>, or, without one, with the long date pattern of the current culture
    /// minus the day name ("March 3, 2026" in en-US, "3 mars 2026" in fr-FR).
    /// </summary>
    public static string Format(DateTimeOffset date, string? format)
    {
        var culture = CultureInfo.CurrentCulture;
        if (format is not null)
        {
            return date.ToString(format, culture);
        }
        var pattern = DayName().Replace(culture.DateTimeFormat.LongDatePattern, "").Trim();
        return date.ToString(pattern.Length == 0 ? "D" : pattern, culture);
    }

    /// <summary>The machine readable value of a <c>time</c> element.</summary>
    public static string Iso(DateTimeOffset date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^\s*dddd[\s,.:/-]*|[\s,.:/-]*dddd\s*$")]
    private static partial Regex DayName();
}
