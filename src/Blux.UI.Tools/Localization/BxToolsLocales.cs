namespace Blux.UI.Tools;

/// <summary>
/// The message tables of Blux.UI.Tools, one per locale of <see cref="BxLocales.All"/> and keyed by the same codes. A component picks the table
/// of the current <see cref="BxLocale.Code"/> with <see cref="BxLocales.ResolveCode"/>, so "pt-PT" gets "pt" and an unknown language gets English.
/// Change a table with <see cref="BluxToolsOptions.Messages"/>.
/// </summary>
public static partial class BxToolsLocales
{
    private static IReadOnlyDictionary<string, BxToolsMessages>? _tables;

    /// <summary>Every table by locale code: the codes of <see cref="BxLocales.All"/>.</summary>
    public static IReadOnlyDictionary<string, BxToolsMessages> Tables => _tables ??= new Dictionary<string, BxToolsMessages>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = En,
        ["fr"] = Fr,
        ["de"] = De,
        ["es"] = Es,
        ["it"] = It,
        ["pt"] = Pt,
        ["pt-BR"] = PtBr,
        ["nl"] = Nl,
        ["ja"] = Ja,
        ["zh-Hans"] = ZhCn,
        ["ar"] = Ar,
        ["ru"] = Ru,
    };

    /// <summary>The codes whose texts were reviewed by a native speaker. The other tables are drafts waiting for review.</summary>
    public static IReadOnlyList<string> ReviewedCodes { get; } = ["en", "fr"];
}
