namespace Blux.UI;

/// <summary>
/// A link of the page family. <c>BxPageHeader</c>, <c>BxPageHero</c>, <c>BxPageSection</c> and <c>BxPageCta</c> render it as a
/// button (<see cref="Color"/> and <see cref="Variant"/> apply); <c>BxPageLinks</c> and <c>BxPageAnchors</c> render it as a text link.
/// </summary>
public sealed record PageLinkItem(string Label, string? Href = null)
{
    /// <summary>Name of a built-in icon (see <see cref="BxIcon"/>) shown before the label.</summary>
    public string? Icon { get; init; }

    /// <summary>Name of a built-in icon shown after the label (buttons only).</summary>
    public string? TrailingIcon { get; init; }
    public string? Target { get; init; }
    /// <summary>Defaults to <see cref="BxDefaults.Color"/>, or primary.</summary>
    public BxColor? Color { get; init; }
    public ButtonVariant Variant { get; init; } = ButtonVariant.Solid;
    public bool Disabled { get; init; }

    /// <summary>Forces the active state of a text link; otherwise it follows the current address.</summary>
    public bool? Active { get; init; }

    /// <summary>Requires the whole path to match for a text link to be active.</summary>
    public bool Exact { get; init; }
}

/// <summary>A feature of a <see cref="BxPageSection"/>.</summary>
public sealed record PageFeatureItem(string Title, string? Description = null)
{
    public string? Icon { get; init; }
    public string? Href { get; init; }
    public string? Target { get; init; }
}

/// <summary>A logo of a <see cref="BxPageLogos"/>: a built-in icon, or an image. <see cref="Alt"/> is its accessible name.</summary>
public sealed record PageLogoItem(string Alt)
{
    /// <summary>Name of a built-in icon (see <see cref="BxIcon"/>).</summary>
    public string? Icon { get; init; }

    /// <summary>URL of an image, used when no <see cref="Icon"/> is given.</summary>
    public string? Src { get; init; }
}
