namespace Klyroo.UI.Maui;

/// <summary>A language <see cref="KlyLocaleSelect"/> offers: a culture name and the name of the language in that language.</summary>
/// <param name="Code">A culture name such as <c>fr</c> or <c>pt-BR</c>. It is what <see cref="KlyCulture"/> stores and applies.</param>
/// <param name="Name">The name of the language in that language ("Français").</param>
public sealed record KlyLocaleOption(string Code, string Name)
{
    private static IReadOnlyList<KlyLocaleOption>? _all;

    /// <summary>Shown before the name: a flag emoji, or any short text. Some systems (Windows) draw flag emoji as letters.</summary>
    public string? Flag { get; init; }

    /// <summary>
    /// The locales of Klyroo.UI: English, French, German, Spanish, Italian, Portuguese, Brazilian Portuguese, Dutch, Japanese, Simplified Chinese, Arabic and Russian.
    /// It is the default of <see cref="KlyLocaleSelect.Locales"/>.
    /// </summary>
    public static IReadOnlyList<KlyLocaleOption> All => _all ??=
    [
        new("en", "English"),
        new("fr", "Français"),
        new("de", "Deutsch"),
        new("es", "Español"),
        new("it", "Italiano"),
        new("pt", "Português"),
        new("pt-BR", "Português (Brasil)"),
        new("nl", "Nederlands"),
        new("ja", "日本語"),
        new("zh-Hans", "简体中文"),
        new("ar", "العربية"),
        new("ru", "Русский"),
    ];

    /// <summary>What the select shows: the name, after the flag when there is one.</summary>
    public override string ToString() => Flag is null ? Name : Flag + " " + Name;
}
