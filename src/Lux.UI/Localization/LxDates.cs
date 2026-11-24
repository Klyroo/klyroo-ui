using System.Globalization;
using System.Text.RegularExpressions;

namespace Lux.UI;

/// <summary>
/// Dates written the way the content components of Lux write them (<see cref="LxBlogPost"/>, <see cref="LxChangelogVersion"/>), for pages
/// that sit next to them and must agree with them: the long date of the culture without the day name, and the machine readable value of a
/// <c>time</c> element.
/// </summary>
public static partial class LxDates
{
    /// <summary>
    /// Formats <paramref name="date"/> with <paramref name="format"/>, or, without one, with the long date pattern of the culture minus the day name
    /// ("March 4, 2026" in en-US, "4 mars 2026" in fr-FR, "4 марта 2026 г." in ru-RU: the order of the culture, not the English one).
    /// Without <paramref name="culture"/> it uses <see cref="FormatCulture()"/>, which knows only the built-in locales: inside a component that
    /// follows <c>LuxOptions.Locale</c> or a cascading <see cref="LxLocale"/>, pass the culture from <see cref="FormatCulture(LxLocale)"/> or use <see cref="FormatFor"/>.
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

    /// <summary>Formats <paramref name="date"/> as the components do for <paramref name="locale"/>: with <see cref="FormatCulture(LxLocale)"/>. Named apart from <c>Format</c> so no call is ambiguous.</summary>
    public static string FormatFor(DateTimeOffset date, LxLocale locale, string? format = null) => Format(date, format, FormatCulture(locale));

    /// <summary>The value of the <c>datetime</c> attribute of a <c>time</c> element: <c>yyyy-MM-dd</c>, the same in every culture.</summary>
    public static string Iso(DateTimeOffset date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// The culture that formats dates for <paramref name="locale"/>: the regional <see cref="CultureInfo.CurrentCulture"/> when its language is the one
    /// of the locale (an "en-GB" visitor with the English locale keeps 4 March 2026), otherwise the culture of the locale. This is the rule of every
    /// component; they pass the locale in effect (the cascading one, then <c>LuxOptions.Locale</c>, then the one of the UI culture).
    /// </summary>
    public static CultureInfo FormatCulture(LxLocale locale)
    {
        var culture = locale.Culture;
        var current = CultureInfo.CurrentCulture;
        return string.Equals(current.TwoLetterISOLanguageName, culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) && current.Name.Length > 0
            ? current
            : culture;
    }

    /// <summary>The same as <see cref="FormatCulture(LxLocale)"/>, for the locale <paramref name="options"/> resolve from the current UI culture (<c>LuxOptions.Locale</c> and <c>Locales</c> included).</summary>
    public static CultureInfo FormatCulture(LuxOptions options) => FormatCulture(options.ResolveLocale(CultureInfo.CurrentUICulture));

    /// <summary>
    /// The same as <see cref="FormatCulture(LxLocale)"/>, for the built-in locale that matches the current UI culture. It cannot see the options of the app
    /// or a cascading locale (a static member has no services): where those are set, use the overload that takes them, or the dates differ from the
    /// ones of the components.
    /// </summary>
    public static CultureInfo FormatCulture() => FormatCulture(LxLocales.Resolve(CultureInfo.CurrentUICulture));

    [GeneratedRegex(@"^\s*dddd[\s,.:/-]*|[\s,.:/-]*dddd\s*$")]
    private static partial Regex DayName();
}
