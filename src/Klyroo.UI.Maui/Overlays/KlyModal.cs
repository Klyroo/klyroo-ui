namespace Klyroo.UI.Maui;

/// <summary>
/// A modal dialog above a dimmed page, with a title, a description, a body and a footer. The control itself holds the trigger (its first button opens the dialog)
/// and needs a <see cref="KlyOverlayHost"/> above it. A tap on the backdrop and the back button close it unless <see cref="KlyPanelOverlay.Dismissible"/> is off.
/// </summary>
public class KlyModal : KlyPanelOverlay
{
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyModal), KlySize.Md, propertyChanged: (b, _, _) => ((KlyModal)b).Reconfigure());

    /// <summary>Creates a modal.</summary>
    public KlyModal() : base(close: true)
    {
    }

    /// <summary>Size of the modal, from <c>Xs</c> to <c>Xl</c>: it changes the largest width of the dialog (320, 384, 512, 672 and 896 points), which never exceeds the screen.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private protected override OverlaysPanelMode PanelMode => OverlaysPanelMode.Modal;

    private protected override KlySize PanelSize => Size;
}
