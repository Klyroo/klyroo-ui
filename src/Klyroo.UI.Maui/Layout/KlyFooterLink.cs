namespace Klyroo.UI.Maui;

/// <summary>A link of a <see cref="KlyFooterColumn"/>. Tapping it raises <see cref="KlyFooterColumns.LinkTapped"/>: the app decides how to follow <see cref="Href"/> (a Shell route, a URL).</summary>
/// <param name="Label">Text of the link.</param>
public sealed record KlyFooterLink(string Label)
{
    /// <summary>Where the link goes, handed to the app in <see cref="KlyFooterColumns.LinkTapped"/>.</summary>
    public string? Href { get; init; }

    /// <summary>The name of an icon, drawn before the label. See <see cref="KlyIconNames"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>Lists the link but does not let it be tapped.</summary>
    public bool Disabled { get; init; }
}
