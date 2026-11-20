using System.Globalization;

namespace Blux.UI;

/// <summary>
/// The built-in locales (one property each, defined in <c>Localization/Locales</c>), <see cref="All"/>, and the helpers to
/// derive and resolve locales.
/// </summary>
public static partial class BxLocales
{
    private static IReadOnlyList<BxLocale>? _all;

    /// <summary>
    /// Every built-in locale: English, French, German, Spanish, Italian, Portuguese, Brazilian Portuguese, Dutch, Japanese,
    /// Simplified Chinese, Arabic (right to left) and Russian. It is the default of <see cref="BluxOptions.Locales"/>.
    /// </summary>
    public static IReadOnlyList<BxLocale> All => _all ??= [En, Fr, De, Es, It, Pt, PtBr, Nl, Ja, ZhCn, Ar, Ru];

    /// <summary>
    /// Derives a locale from another one, like <c>extendLocale</c> of Nuxt UI: everything you do not override is kept.
    /// </summary>
    /// <example>
    /// <c>BxLocales.Extend(BxLocales.En, code: "en-AU", name: "English (Australia)", messages: m => m with { CommandPalettePlaceholder = "Search..." })</c>
    /// </example>
    /// <param name="source">The locale to start from.</param>
    /// <param name="code">The new code. Defaults to the code of <paramref name="source"/>.</param>
    /// <param name="name">The new name. Defaults to the name of <paramref name="source"/>.</param>
    /// <param name="dir">The new direction. Defaults to the direction of <paramref name="source"/>.</param>
    /// <param name="messages">Returns the messages to use, usually <c>m =&gt; m with { ... }</c>.</param>
    public static BxLocale Extend(BxLocale source, string? code = null, string? name = null, BxDirection? dir = null, Func<BxMessages, BxMessages>? messages = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source with
        {
            Code = code ?? source.Code,
            Name = name ?? source.Name,
            Dir = dir ?? source.Dir,
            Messages = messages is null ? source.Messages : messages(source.Messages),
        };
    }

    /// <summary>
    /// Picks the locale for a culture the way <c>UseRequestLocalization</c> picks a supported culture, so the server and the
    /// WebAssembly client agree: the exact code ("pt-BR"), then its parent cultures ("pt-PT" gets "pt", "zh-Hans-CN" tries
    /// "zh-Hans" then "zh"), then English. There is no "same language" step: "zh-TW" gets English unless the list holds "zh-TW" or "zh".
    /// </summary>
    /// <param name="culture">The culture to match. Defaults to <see cref="CultureInfo.CurrentUICulture"/>.</param>
    /// <param name="locales">The locales to choose from. Defaults to <see cref="All"/>. English, or the first one when there is no English, is the fallback.</param>
    /// <remarks>Use it in <c>App.razor</c> to write <c>lang</c> and <c>dir</c> on the <c>html</c> element.</remarks>
    public static BxLocale Resolve(CultureInfo? culture = null, IReadOnlyList<BxLocale>? locales = null)
    {
        locales ??= All;
        culture ??= CultureInfo.CurrentUICulture;

        // The culture and its parents, most specific first. The invariant culture has an empty name and stops the walk.
        for (var c = culture; c.Name.Length > 0; c = c.Parent)
        {
            foreach (var locale in locales)
            {
                if (string.Equals(locale.Code, c.Name, StringComparison.OrdinalIgnoreCase)) return locale;
            }
        }

        foreach (var locale in locales)
        {
            if (string.Equals(locale.Code, "en", StringComparison.OrdinalIgnoreCase)) return locale;
        }
        return locales.Count > 0 ? locales[0] : En;
    }
}
