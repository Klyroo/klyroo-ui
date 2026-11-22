using System.Globalization;
using System.Text.RegularExpressions;

namespace Blux.UI;

/// <summary>
/// Dates written the way the content components of Blux write them (<see cref="BxBlogPost"/>, <see cref="BxChangelogVersion"/>), for pages
/// that sit next to them and must agree with them: the long date of the culture without the day name, and the machine readable value of a
/// <c>time</c> element.
/// </summary>
public static partial class BxDates
{
    /// <summary>
    /// Formats <paramref name="date"/> with <paramref name="format"/>, or, without one, with the long date pattern of the culture minus the day name
    /// ("March 4, 2026" in en-US, "4 mars 2026" in fr-FR, "4 марта 2026 г." in ru-RU: the order of the culture, not the English one).
    /// <paramref name="culture"/> defaults to <see cref="FormatCulture"/>.
    /// </summary>
    public static string Format(DateTimeOffset date, string? format = null, CultureInfo? culture = null)
    {
        culture ??= FormatCulture();
        if (format is not null)
        {
            return date.ToString(format, culture);
        }
        var pattern = DayName().Replace(culture.DateTimeFormat.LongDatePattern, "").Trim();
        return date.ToString(pattern.Length == 0 ? "D" : pattern, culture);
    }

    /// <summary>The value of the <c>datetime</c> attribute of a <c>time</c> element: <c>yyyy-MM-dd</c>, the same in every culture.</summary>
    public static string Iso(DateTimeOffset date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// The culture that formats dates for the current request: the regional <see cref="CultureInfo.CurrentCulture"/> when its language is the one of the
    /// locale in effect (an "en-GB" visitor with the English locale keeps 4 March 2026), otherwise the culture of the locale. It is the rule the
    /// components follow when they have no <c>Culture</c> parameter, resolved from <see cref="CultureInfo.CurrentUICulture"/> among the built-in locales.
    /// </summary>
    public static CultureInfo FormatCulture()
    {
        var locale = BxLocales.Resolve(CultureInfo.CurrentUICulture).Culture;
        var current = CultureInfo.CurrentCulture;
        return string.Equals(current.TwoLetterISOLanguageName, locale.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) && current.Name.Length > 0
            ? current
            : locale;
    }

    [GeneratedRegex(@"^\s*dddd[\s,.:/-]*|[\s,.:/-]*dddd\s*$")]
    private static partial Regex DayName();
}
