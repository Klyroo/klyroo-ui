namespace Klyroo.UI.Maui;

/// <summary>
/// A full-height panel that slides in from an edge of the screen over a dimmed page, with a title, a description, a body and a footer.
/// The control itself holds the trigger and needs a <see cref="KlyOverlayHost"/> above it.
/// </summary>
public class KlySlideover : KlyPanelOverlay
{
    /// <summary>Identifies <see cref="Side"/>.</summary>
    public static readonly BindableProperty SideProperty = BindableProperty.Create(nameof(Side), typeof(SlideoverSide), typeof(KlySlideover), SlideoverSide.Right, propertyChanged: (b, _, _) => ((KlySlideover)b).Reconfigure());
    /// <summary>Identifies <see cref="Inset"/>.</summary>
    public static readonly BindableProperty InsetProperty = BindableProperty.Create(nameof(Inset), typeof(bool), typeof(KlySlideover), false, propertyChanged: (b, _, _) => ((KlySlideover)b).Reconfigure());

    /// <summary>Creates a slideover.</summary>
    public KlySlideover() : base(close: true)
    {
    }

    /// <summary>The edge the panel slides in from. A side panel is at most 448 points wide; a top or bottom panel at most 85% of the screen high.</summary>
    public SlideoverSide Side { get => (SlideoverSide)GetValue(SideProperty); set => SetValue(SideProperty, value); }

    /// <summary>Floats the panel 8 points away from the edges of the screen, with rounded corners all around.</summary>
    public bool Inset { get => (bool)GetValue(InsetProperty); set => SetValue(InsetProperty, value); }

    private protected override OverlaysPanelMode PanelMode => KlyOverlayHost.ModeOf(Side);

    private protected override bool PanelInset => Inset;
}
