using System.Collections.Concurrent;
using System.Globalization;

namespace Blux.UI;

/// <summary>
/// A language of the UI: its code, its name, its writing direction and every text the components render.
/// Equivalent of the locale object of Nuxt UI. Use a built-in one from <see cref="BxLocales"/>, derive one with
/// <see cref="BxLocales.Extend"/>, or create it with <c>new BxLocale(...)</c>.
/// </summary>
/// <param name="Code">A culture name such as "fr" or "pt-BR". It is the value written to the culture cookie by <see cref="BxLocaleSelect"/>, the <c>lang</c> attribute of the page, and the culture used to format dates and numbers.</param>
/// <param name="Name">The name of the language in that language ("Français"), shown by <see cref="BxLocaleSelect"/>.</param>
/// <param name="Dir">The writing direction.</param>
/// <param name="Messages">The texts of the components.</param>
public sealed record BxLocale(string Code, string Name, BxDirection Dir, BxMessages Messages)
{
    private static readonly ConcurrentDictionary<string, CultureInfo> Cultures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shown before the name by <see cref="BxLocaleSelect"/>: a flag emoji, or any short text. Some systems (Windows) draw flag emoji as letters.</summary>
    public string? Flag { get; init; }

    /// <summary>
    /// The culture of <see cref="Code"/>, used by date, number and file size formatting when a component has no
    /// explicit <c>Culture</c>. A code the runtime does not know gives the invariant culture.
    /// </summary>
    public CultureInfo Culture => Cultures.GetOrAdd(Code, static code =>
    {
        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    });

    /// <summary>The value of the <c>dir</c> attribute: "ltr" or "rtl".</summary>
    public string DirAttribute => Dir == BxDirection.Rtl ? "rtl" : "ltr";
}
