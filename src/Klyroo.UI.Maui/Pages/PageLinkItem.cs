namespace Klyroo.UI.Maui;

/// <summary>
/// A link of the page family. <c>KlyPageHeader</c>, <c>KlyPageHero</c>, <c>KlyPageSection</c> and <c>KlyPageCta</c> draw it as a
/// button (<see cref="Color"/> and <see cref="Variant"/> apply); <c>KlyPageLinks</c> and <c>KlyPageAnchors</c> draw it as a text link.
/// </summary>
/// <param name="Label">Text of the link.</param>
/// <param name="Href">Address the link points to: reported by the control when the link is followed.</param>
public sealed record PageLinkItem(string Label, string? Href = null)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) shown before the label of a text link. A native button cannot host it, so a button link does not draw it.</summary>
    public string? Icon { get; init; }

    /// <summary>Color of the button when the link is drawn as one. Defaults to primary.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>Look of the button when the link is drawn as one.</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Solid;

    /// <summary>Disables the link.</summary>
    public bool Disabled { get; init; }

    /// <summary>Marks a text link as the current page: it is drawn in the primary color.</summary>
    public bool Active { get; init; }
}
