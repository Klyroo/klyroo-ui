using System.Collections.Concurrent;

namespace Klyroo.UI.Maui;

/// <summary>
/// The design tokens of Klyroo.UI (the <c>--klyroo-*</c> variables of the web library) as light and dark colors.
/// Register or replace semantic colors at startup, before the first control is created: controls read them when they are styled.
/// </summary>
public static class KlyTheme
{
    private static readonly ConcurrentDictionary<string, KlyColorSet> Registry = new(StringComparer.Ordinal)
    {
        ["primary"] = Set("#b6ff00", "#b6ff00", "#3b5900", "#b6ff00", "#0b0f14", "#0b0f14"),
        ["secondary"] = Set("#1a5e68", "#64c3cf", "#1a5e68", "#64c3cf", "#ffffff", "#06181b"),
        ["success"] = Set("#146439", "#7bd88f", "#136238", "#7bd88f", "#ffffff", "#08180c"),
        ["info"] = Set("#2555a0", "#7fb2ff", "#23549e", "#7fb2ff", "#ffffff", "#07101f"),
        ["warning"] = Set("#d86010", "#ff8a4c", "#9c3d08", "#ff8a4c", "#1a0a00", "#1a0a00"),
        ["error"] = Set("#a53125", "#ff6b4a", "#9d2c1a", "#ff7759", "#ffffff", "#1f0700"),
        ["neutral"] = Set("#0b0f14", "#e5e7eb", "#1f2937", "#e5e7eb", "#ffffff", "#0b0f14"),
    };

    /// <summary>Base corner radius in device-independent units (0.5rem on the web). Controls use fractions of it, as the web library does.</summary>
    public const double Radius = 8;

    /// <summary>Default text color.</summary>
    public static KlyThemeColor Text { get; } = KlyThemeColor.FromHex("#1f2937", "#e5e7eb");
    /// <summary>Text color of headings and emphasis.</summary>
    public static KlyThemeColor TextHighlighted { get; } = KlyThemeColor.FromHex("#0b0f14", "#ffffff");
    /// <summary>Text color between <see cref="Text"/> and <see cref="TextMuted"/>, such as the punctuation of a code block.</summary>
    public static KlyThemeColor TextToned { get; } = KlyThemeColor.FromHex("#374151", "#cbd5e1");
    /// <summary>Secondary text color.</summary>
    public static KlyThemeColor TextMuted { get; } = KlyThemeColor.FromHex("#4b5563", "#9ca3af");
    /// <summary>Placeholder and hint text color.</summary>
    public static KlyThemeColor TextDimmed { get; } = KlyThemeColor.FromHex("#626b77", "#808999");
    /// <summary>Text color on <see cref="BackgroundInverted"/>.</summary>
    public static KlyThemeColor TextInverted { get; } = KlyThemeColor.FromHex("#ffffff", "#0b0f14");
    /// <summary>Page background.</summary>
    public static KlyThemeColor Background { get; } = KlyThemeColor.FromHex("#ffffff", "#0b0f14");
    /// <summary>A background slightly apart from the page.</summary>
    public static KlyThemeColor BackgroundMuted { get; } = KlyThemeColor.FromHex("#f7f8fa", "#11161d");
    /// <summary>Background of raised surfaces.</summary>
    public static KlyThemeColor BackgroundElevated { get; } = KlyThemeColor.FromHex("#f1f3f5", "#171e27");
    /// <summary>Background of accented surfaces, such as an avatar without a picture.</summary>
    public static KlyThemeColor BackgroundAccented { get; } = KlyThemeColor.FromHex("#e5e7eb", "#222b36");
    /// <summary>The opposite of the page: dark in the light theme, light in the dark theme.</summary>
    public static KlyThemeColor BackgroundInverted { get; } = KlyThemeColor.FromHex("#0b0f14", "#e5e7eb");
    /// <summary>Border of cards and dividers.</summary>
    public static KlyThemeColor Border { get; } = KlyThemeColor.FromHex("#d4d8de", "#242e3b");
    /// <summary>Border of form controls: at least 3:1 against the page (WCAG 1.4.11).</summary>
    public static KlyThemeColor BorderAccented { get; } = KlyThemeColor.FromHex("#6b7280", "#647080");
    /// <summary>Unchecked track of a switch.</summary>
    public static KlyThemeColor Control { get; } = KlyThemeColor.FromHex("#6b7280", "#647080");

    /// <summary>Adds a semantic color, or replaces a built-in one such as <see cref="KlyColor.Primary"/> to rebrand.</summary>
    public static void Register(KlyColor color, KlyColorSet set)
    {
        if (color.Value is null) throw new ArgumentException("The color has no name.", nameof(color));
        Registry[color.Value] = set;
    }

    /// <summary>The colors of a semantic color.</summary>
    /// <exception cref="InvalidOperationException">The color is not built in and was never registered.</exception>
    public static KlyColorSet Resolve(KlyColor color)
    {
        var name = color.Value ?? KlyColor.Primary.Value;
        return Registry.TryGetValue(name, out var set)
            ? set
            : throw new InvalidOperationException($"The color '{name}' is not built in and was not registered: call KlyTheme.Register(\"{name}\", ...) at startup.");
    }

    /// <summary>Corner radius of a fraction of <see cref="Radius"/>: <c>0.25</c> is the web <c>rounded-xs</c>, <c>0.5</c> <c>sm</c>, <c>0.75</c> <c>md</c>, <c>1</c> <c>lg</c>, <c>2</c> <c>2xl</c>.</summary>
    internal static double RadiusOf(double fraction) => Radius * fraction;

    private static KlyColorSet Set(string fillLight, string fillDark, string textLight, string textDark, string contrastLight, string contrastDark) => new(
        KlyThemeColor.FromHex(fillLight, fillDark),
        KlyThemeColor.FromHex(textLight, textDark),
        KlyThemeColor.FromHex(contrastLight, contrastDark));
}
