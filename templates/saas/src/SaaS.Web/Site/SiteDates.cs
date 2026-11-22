using System.Globalization;
using System.Text.RegularExpressions;
using Blux.UI;

namespace SaaS.Web.Site;

/// <summary>
/// The date of a post or a version written the way the cards of Blux write it (<c>BxBlogPost</c>, <c>BxChangelogVersion</c>), so that the page of a post
/// and its card never disagree: the long date of the culture without the day name ("March 4, 2026", "4 mars 2026"), and the machine readable
/// <c>yyyy-MM-dd</c> of the <c>time</c> element, which does not depend on the culture. The culture is the regional one of the request when its language
/// is the one of the locale in effect (an "en-GB" visitor with the English locale keeps 4 March 2026), otherwise the culture of the locale: the rule of
/// <c>BxComponentBase.FormatCulture</c>, which a page of the site cannot call because it is a protected member.
/// </summary>
public static partial class SiteDates
{
    /// <summary>The visible date, in the culture of the request.</summary>
    public static string Format(DateTimeOffset date) => Format(date, Culture());

    /// <summary>The visible date, in <paramref name="culture"/>.</summary>
    public static string Format(DateTimeOffset date, CultureInfo culture)
    {
        var pattern = DayName().Replace(culture.DateTimeFormat.LongDatePattern, "").Trim();
        return date.ToString(pattern.Length == 0 ? "D" : pattern, culture);
    }

    /// <summary>The value of the <c>datetime</c> attribute.</summary>
    public static string Iso(DateTimeOffset date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>The culture that formats dates for the current request.</summary>
    public static CultureInfo Culture()
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
