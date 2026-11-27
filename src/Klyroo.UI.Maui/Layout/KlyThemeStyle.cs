namespace Klyroo.UI.Maui;

/// <summary>
/// The palette of an app, declared in one place and applied to the theme: the native counterpart of the <c>KlyThemeStyle</c> that writes the color variables into the head of a web page.
/// Each entry replaces a built-in color (to rebrand) or adds a new semantic one, through <see cref="KlyTheme.Register"/>.
/// Apply it at startup, before the first control is created: controls read the colors when they are styled.
/// </summary>
/// <example>
/// <code>
/// new KlyThemeStyle
/// {
///     Colors =
///     {
///         [KlyColor.Primary] = new KlyColorSet(KlyThemeColor.FromHex("#2563eb", "#60a5fa"), KlyThemeColor.FromHex("#1d4ed8", "#93c5fd"), KlyThemeColor.FromHex("#ffffff", "#0b0f14")),
///     },
/// }.Apply();
/// </code>
/// </example>
public sealed class KlyThemeStyle
{
    /// <summary>The colors to register, by semantic color: the fill, the text color and the contrast color, each in light and dark.</summary>
    public IDictionary<KlyColor, KlyColorSet> Colors { get; } = new Dictionary<KlyColor, KlyColorSet>();

    /// <summary>Registers every entry of <see cref="Colors"/> with <see cref="KlyTheme.Register"/>. Returns this style, so it can be chained.</summary>
    public KlyThemeStyle Apply()
    {
        foreach (var (color, set) in Colors) KlyTheme.Register(color, set);
        return this;
    }
}
