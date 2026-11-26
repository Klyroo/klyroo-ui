namespace Klyroo.UI;

/// <summary>
/// A link of the page family. <c>KlyPageHeader</c>, <c>KlyPageHero</c>, <c>KlyPageSection</c> and <c>KlyPageCta</c> render it as a
/// button (<see cref="Color"/> and <see cref="Variant"/> apply); <c>KlyPageLinks</c> and <c>KlyPageAnchors</c> render it as a text link.
/// </summary>
/// <param name="Label">Text of the link.</param>
/// <param name="Href">Address the link points to.</param>
public sealed record PageLinkItem(string Label, string? Href = null)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) shown before the label.</summary>
    public string? Icon { get; init; }

    /// <summary>Name of a built-in icon shown after the label (buttons only).</summary>
    public string? TrailingIcon { get; init; }
    /// <summary>The <c>target</c> attribute of the link, such as <see cref="KlyLinkTarget.Blank"/>.</summary>
    public KlyLinkTarget? Target { get; init; }
    /// <summary>Defaults to <see cref="KlyDefaults.Color"/>, or primary.</summary>
    public KlyColor? Color { get; init; }
    /// <summary>Look of the button when the link is rendered as one.</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Solid;
    /// <summary>Disables the link.</summary>
    public bool Disabled { get; init; }

    /// <summary>Forces the active state of a text link; otherwise it follows the current address.</summary>
    public bool? Active { get; init; }

    /// <summary>Requires the whole path to match for a text link to be active.</summary>
    public bool Exact { get; init; }
}

/// <summary>A feature of a <see cref="KlyPageSection"/>.</summary>
/// <param name="Title">Title of the feature.</param>
/// <param name="Description">Text under the title.</param>
public sealed record PageFeatureItem(string Title, string? Description = null)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) shown above the title.</summary>
    public string? Icon { get; init; }
    /// <summary>Address the feature links to.</summary>
    public string? Href { get; init; }
    /// <summary>The <c>target</c> attribute of the link, such as <see cref="KlyLinkTarget.Blank"/>.</summary>
    public KlyLinkTarget? Target { get; init; }
}

/// <summary>A logo of a <see cref="KlyPageLogos"/>: a built-in icon, or an image. <see cref="Alt"/> is its accessible name.</summary>
/// <param name="Alt">Accessible name of the logo.</param>
public sealed record PageLogoItem(string Alt)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>).</summary>
    public string? Icon { get; init; }

    /// <summary>URL of an image, used when no <see cref="Icon"/> is given.</summary>
    public string? Src { get; init; }
}
