namespace Klyroo.UI.Maui;

/// <summary>A color in its light and its dark value. Controls follow the app theme (<c>Application.RequestedTheme</c>, or <c>UserAppTheme</c> when the app sets it).</summary>
/// <param name="Light">The color in the light theme.</param>
/// <param name="Dark">The color in the dark theme.</param>
public readonly record struct KlyThemeColor(Color Light, Color Dark)
{
    /// <summary>A transparent color in both themes.</summary>
    public static KlyThemeColor Transparent { get; } = new(Colors.Transparent, Colors.Transparent);

    /// <summary>Creates a themed color from two hex strings such as <c>#1f2937</c>.</summary>
    public static KlyThemeColor FromHex(string light, string dark) => new(Color.FromArgb(light), Color.FromArgb(dark));

    /// <summary>The color of the theme the app shows now: <see cref="Dark"/> when <c>Application.Current.RequestedTheme</c> is dark (it follows <c>UserAppTheme</c> when the app sets it), else <see cref="Light"/>. For drawing code, which has no property to bind.</summary>
    public Color Resolve() => Application.Current?.RequestedTheme == AppTheme.Dark ? Dark : Light;

    /// <summary>The same color with an alpha channel from 0 to 1, in both themes.</summary>
    public KlyThemeColor WithAlpha(float alpha) => new(Light.WithAlpha(alpha), Dark.WithAlpha(alpha));
}

/// <summary>The three colors every semantic color provides: the fill, the text color on the page, and the text color on the fill.</summary>
/// <param name="Fill">The fill of solid surfaces, and the tint of soft ones.</param>
/// <param name="Text">The color of text and borders drawn on the page background.</param>
/// <param name="Contrast">The color of text drawn on <paramref name="Fill"/>.</param>
public readonly record struct KlyColorSet(KlyThemeColor Fill, KlyThemeColor Text, KlyThemeColor Contrast);
