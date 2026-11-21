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
    /// The codes whose texts were reviewed by a native speaker. The other locales hold drafts, at least the messages that a comment in their file marks as
    /// "Draft translations" (the messages added since the first review: the <c>RemoteOptions</c> ones), until a native speaker reviews them.
    /// </summary>
    public static IReadOnlyList<string> ReviewedCodes { get; } = ["en", "fr"];

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

        var index = IndexOfCode(culture, locales.Select(l => l.Code));
        return index >= 0 ? locales[index] : En;
    }

    /// <summary>
    /// Picks one of <paramref name="available"/> for a locale code with the same rules as <see cref="Resolve"/>, so a package
    /// with its own message tables (keyed by locale code) follows the locale of the app: pass <see cref="BxLocale.Code"/>.
    /// The exact code ("pt-BR"), then its parent cultures ("pt-PT" gets "pt", "zh-CN" and "zh-Hans-CN" get "zh-Hans" then "zh"), then
    /// "en", then the first available code. Codes compare case-insensitively and the result is the spelling found in <paramref name="available"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Resolve"/> walks <see cref="CultureInfo.Parent"/>, which needs culture data. When the runtime does not know the code
    /// (invariant globalization, or a name it has no data for), <see cref="ResolveCode"/> trims the last "-" segment instead, which cannot
    /// know that "zh-CN" belongs to "zh-Hans": it then falls back to "en". Passing <see cref="BxLocale.Code"/> of a locale of
    /// <see cref="All"/> is unaffected, since such a code is matched exactly.
    /// </remarks>
    /// <param name="code">A culture name, usually <see cref="BxLocale.Code"/>. A name the runtime does not know is walked by trimming its last "-" segment.</param>
    /// <param name="available">The codes the package has a table for. It cannot be empty.</param>
    /// <example><c>var table = tables[BxLocales.ResolveCode(CurrentLocale.Code, tables.Keys)];</c></example>
    public static string ResolveCode(string code, IEnumerable<string> available)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(available);
        var codes = available as IReadOnlyList<string> ?? [.. available];
        if (codes.Count == 0) throw new ArgumentException("At least one available code is required.", nameof(available));

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return codes[IndexOfCode(UnknownChain(code), codes)];
        }
        return codes[IndexOfCode(culture, codes)];
    }

    // The culture and its parents, most specific first. The invariant culture has an empty name and stops the walk.
    private static IEnumerable<string> Chain(CultureInfo culture)
    {
        for (var c = culture; c.Name.Length > 0; c = c.Parent) yield return c.Name;
    }

    private static IEnumerable<string> UnknownChain(string code)
    {
        for (var name = code; name.Length > 0; )
        {
            yield return name;
            var dash = name.LastIndexOf('-');
            name = dash < 0 ? "" : name[..dash];
        }
    }

    private static int IndexOfCode(CultureInfo culture, IEnumerable<string> codes) => IndexOfCode(Chain(culture), codes);

    /// <summary>The index in <paramref name="codes"/> of the first match of the chain, else of "en", else 0, else -1 when there is no code.</summary>
    private static int IndexOfCode(IEnumerable<string> chain, IEnumerable<string> codes)
    {
        var list = codes as IReadOnlyList<string> ?? [.. codes];
        foreach (var name in chain)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            }
        }
        for (var i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], "en", StringComparison.OrdinalIgnoreCase)) return i;
        }
        return list.Count > 0 ? 0 : -1;
    }
}
