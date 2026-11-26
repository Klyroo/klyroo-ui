using System.Collections.Concurrent;
using System.Globalization;

namespace Klyroo.UI;

/// <summary>
/// A language of the UI: its code, its name, its writing direction and every text the components render.
/// Locale definition for component labels and text direction. Use a built-in one from <see cref="KlyLocales"/>, derive one with
/// <see cref="KlyLocales.Extend"/>, or create it with <c>new KlyLocale(...)</c>.
/// </summary>
/// <param name="Code">A culture name such as "fr" or "pt-BR". It is the value written to the culture cookie by <see cref="KlyLocaleSelect"/>, the <c>lang</c> attribute of the page, and the culture used to format dates and numbers.</param>
/// <param name="Name">The name of the language in that language ("Français"), shown by <see cref="KlyLocaleSelect"/>.</param>
/// <param name="Dir">The writing direction of the language: <see cref="KlyDirection.Ltr"/> or <see cref="KlyDirection.Rtl"/>. <see cref="KlyDirection.Auto"/> is refused, since a language has a direction; only the <c>Dir</c> parameter of <see cref="KlyApp"/> takes it.</param>
/// <param name="Messages">The texts of the components.</param>
public sealed record KlyLocale(string Code, string Name, KlyDirection Dir, KlyMessages Messages)
{
    private static readonly ConcurrentDictionary<string, CultureInfo> Cultures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shown before the name by <see cref="KlyLocaleSelect"/>: a flag emoji, or any short text. Some systems (Windows) draw flag emoji as letters.</summary>
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

    private readonly KlyDirection _dir = CheckDirection(Dir);

    /// <summary>The writing direction of the language, <see cref="KlyDirection.Ltr"/> or <see cref="KlyDirection.Rtl"/>.</summary>
    /// <exception cref="ArgumentException">The value is <see cref="KlyDirection.Auto"/> or not a member of the enum.</exception>
    public KlyDirection Dir
    {
        get => _dir;
        init => _dir = CheckDirection(value);
    }

    /// <summary>The value of the <c>dir</c> attribute: "ltr" or "rtl".</summary>
    public string DirAttribute => Internal.Wire.Of(Dir);

    private static KlyDirection CheckDirection(KlyDirection direction) => direction is KlyDirection.Ltr or KlyDirection.Rtl
        ? direction
        : throw new ArgumentException($"A locale has a direction: KlyDirection.Ltr or KlyDirection.Rtl, not KlyDirection.{direction}. Only KlyApp.Dir takes Auto.", nameof(Dir));
}
