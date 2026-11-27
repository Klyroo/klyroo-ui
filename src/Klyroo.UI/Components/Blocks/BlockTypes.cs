using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>The surface treatment of a read-only block such as <see cref="KlyFacts"/> or <see cref="KlyStat"/>: border, background and padding.</summary>
public enum KlySurface
{
    /// <summary>No border, background or padding.</summary>
    Plain,
    /// <summary>A muted background with padding.</summary>
    Muted,
    /// <summary>A border and padding.</summary>
    Bordered,
    /// <summary>A border, an elevated background, padding and a shadow.</summary>
    Elevated,
}

/// <summary>The direction of the change shown beside a <see cref="KlyStat"/> value. It only chooses the color of the badge.</summary>
public enum KlyDeltaDirection
{
    /// <summary>Success color.</summary>
    Up,
    /// <summary>Error color.</summary>
    Down,
    /// <summary>Neutral color.</summary>
    Flat,
}

/// <summary>A small badge shown in a read-only block, drawn as a subtle small <see cref="KlyBadge"/>.</summary>
/// <param name="Label">The badge text.</param>
public sealed record KlyBadgeItem(string Label)
{
    /// <summary>The badge color. Neutral when not set.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown before the label.</summary>
    public string? Icon { get; init; }
}

/// <summary>A button or link shown in a block, drawn as a small <see cref="KlyButton"/>.</summary>
/// <param name="Label">The button text.</param>
public sealed record KlyActionItem(string Label)
{
    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown before the label.</summary>
    public string? Icon { get; init; }

    /// <summary>The link target. When set, the button navigates instead of only raising <see cref="OnClick"/>.</summary>
    public string? Href { get; init; }

    /// <summary>The <c>target</c> of the link, such as <see cref="KlyLinkTarget.Blank"/>. It applies only with <see cref="Href"/>.</summary>
    public KlyLinkTarget? Target { get; init; }

    /// <summary>The callback invoked when the button is clicked. The button stays disabled until the page is interactive when this is set and there is no <see cref="Href"/>.</summary>
    public EventCallback OnClick { get; init; }

    /// <summary>The button color (neutral by default).</summary>
    public KlyColor Color { get; init; } = KlyColor.Neutral;

    /// <summary>The button look (outline by default).</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Outline;
}
