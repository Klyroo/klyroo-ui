namespace Klyroo.UI.Maui;

/// <summary>
/// A sheet docked to an edge of the screen over a dimmed page, bottom by default, with a grab handle, a title, a description, a body and a footer.
/// The control itself holds the trigger and needs a <see cref="KlyOverlayHost"/> above it.
/// </summary>
public class KlyDrawer : KlyPanelOverlay
{
    /// <summary>Identifies <see cref="Direction"/>.</summary>
    public static readonly BindableProperty DirectionProperty = BindableProperty.Create(nameof(Direction), typeof(DrawerDirection), typeof(KlyDrawer), DrawerDirection.Bottom, propertyChanged: (b, _, _) => ((KlyDrawer)b).Reconfigure());
    /// <summary>Identifies <see cref="Handle"/>.</summary>
    public static readonly BindableProperty HandleProperty = BindableProperty.Create(nameof(Handle), typeof(bool), typeof(KlyDrawer), true, propertyChanged: (b, _, _) => ((KlyDrawer)b).Reconfigure());
    /// <summary>Identifies <see cref="Inset"/>.</summary>
    public static readonly BindableProperty InsetProperty = BindableProperty.Create(nameof(Inset), typeof(bool), typeof(KlyDrawer), false, propertyChanged: (b, _, _) => ((KlyDrawer)b).Reconfigure());

    /// <summary>Creates a drawer. Unlike the modal, it has no close button until <see cref="KlyPanelOverlay.Close"/> is set.</summary>
    public KlyDrawer() : base(close: false)
    {
    }

    /// <summary>The edge the drawer is docked to.</summary>
    public DrawerDirection Direction { get => (DrawerDirection)GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

    /// <summary>The grab handle shown on top and bottom drawers. It is decorative: the drawer closes with a tap on the backdrop, the back button or a close button.</summary>
    public bool Handle { get => (bool)GetValue(HandleProperty); set => SetValue(HandleProperty, value); }

    /// <summary>Floats the drawer 8 points away from the edges of the screen, with rounded corners all around.</summary>
    public bool Inset { get => (bool)GetValue(InsetProperty); set => SetValue(InsetProperty, value); }

    private protected override OverlaysPanelMode PanelMode => KlyOverlayHost.ModeOf(Direction);

    private protected override bool PanelInset => Inset;

    private protected override bool PanelHandle => Handle;
}
