namespace Klyroo.UI.Maui;

/// <summary>How <see cref="KlyOverlayHost.Show"/> presents a view: with or without a backdrop, anchored to a trigger or aligned by the view itself.</summary>
public sealed class KlyOverlayLayerOptions
{
    /// <summary>A backdrop catches every touch outside the content, so the page below is out of reach (and hidden from screen readers). Off for tooltips and toasts, which let the page work.</summary>
    public bool Modal { get; init; } = true;

    /// <summary>Tints the backdrop (the dark scrim of the web library). Only used when <see cref="Modal"/> is on; off gives an invisible backdrop, as a popover has.</summary>
    public bool Dim { get; init; } = true;

    /// <summary>A tap on the backdrop, and the back button of Android, dismiss the layer. <see cref="KlyOverlayHost.Dismiss"/> still works when it is off.</summary>
    public bool Dismissible { get; init; } = true;

    /// <summary>How the layer comes in and goes out.</summary>
    public KlyOverlayTransition Transition { get; init; } = KlyOverlayTransition.Fade;

    /// <summary>The view the content is placed against. Without it the content aligns itself with its own <c>HorizontalOptions</c>, <c>VerticalOptions</c> and <c>Margin</c>.</summary>
    public View? Anchor { get; init; }

    /// <summary>A point in <see cref="Anchor"/> (its own coordinates) to anchor to instead of the whole view, such as the place of a touch. Needs <see cref="Anchor"/>.</summary>
    public Point? AnchorPoint { get; init; }

    /// <summary>Side and alignment against the anchor. Only used with <see cref="Anchor"/>.</summary>
    public KlyPlacement Placement { get; init; } = KlyPlacement.Bottom;

    /// <summary>Gap between the anchor and the content. Only used with <see cref="Anchor"/>.</summary>
    public double Offset { get; init; } = 8;
}
