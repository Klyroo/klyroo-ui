namespace Klyroo.UI.Maui;

/// <summary>
/// Base of <see cref="KlyModal"/>, <see cref="KlySlideover"/> and <see cref="KlyDrawer"/>: a modal panel with a title, a description, a body and a footer.
/// <see cref="ContentView.Content"/> is the trigger (its first button opens the panel), <see cref="Body"/> and <see cref="Footer"/> are the content of the panel.
/// </summary>
public abstract class KlyPanelOverlay : KlyOverlayBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPanelOverlay), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPanelOverlay), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyPanelOverlay), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPanelOverlay), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyPanelOverlay), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Dismissible"/>.</summary>
    public static readonly BindableProperty DismissibleProperty = BindableProperty.Create(nameof(Dismissible), typeof(bool), typeof(KlyPanelOverlay), true, propertyChanged: (b, _, v) =>
    {
        if (((KlyPanelOverlay)b).Layer is { } layer) layer.Dismissible = (bool)v;
    });
    /// <summary>Identifies <see cref="Close"/>.</summary>
    public static readonly BindableProperty CloseProperty = BindableProperty.Create(nameof(Close), typeof(bool), typeof(KlyPanelOverlay), true, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="CloseLabel"/>.</summary>
    public static readonly BindableProperty CloseLabelProperty = BindableProperty.Create(nameof(CloseLabel), typeof(string), typeof(KlyPanelOverlay), "Close", propertyChanged: Refresh);

    private OverlaysPanel? _panel;

    private protected KlyPanelOverlay(bool close)
    {
        Close = close;
    }

    /// <summary>Title of the panel, which also names it for assistive technology.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Text under the title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>What a screen reader says for the panel when there is no <see cref="Title"/>.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>The content of the panel. It scrolls when it is taller than the screen.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>The actions at the bottom of the panel, such as a <c>HorizontalStackLayout</c> of buttons aligned to the end. Close the panel from a button with <see cref="KlyOverlayBase.Hide"/>.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>A tap on the backdrop and the back button close the panel.</summary>
    public bool Dismissible { get => (bool)GetValue(DismissibleProperty); set => SetValue(DismissibleProperty, value); }

    /// <summary>Shows the close button in the header (a title or a description also shows the header).</summary>
    public bool Close { get => (bool)GetValue(CloseProperty); set => SetValue(CloseProperty, value); }

    /// <summary>Accessible name of the close button, in the language of the app.</summary>
    public string CloseLabel { get => (string)GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    /// <summary>Where the panel sits in the host.</summary>
    private protected abstract OverlaysPanelMode PanelMode { get; }

    /// <summary>Width class of a dialog.</summary>
    private protected virtual KlySize PanelSize => KlySize.Md;

    /// <summary>Floats the panel away from the edges of the screen.</summary>
    private protected virtual bool PanelInset => false;

    /// <summary>Shows the grab handle on top and bottom sheets.</summary>
    private protected virtual bool PanelHandle => false;

    /// <summary>The panel while it is shown, for the tests.</summary>
    internal OverlaysPanel? Panel => _panel;

    /// <inheritdoc />
    protected sealed override KlyOverlayLayer Present(KlyOverlayHost host)
    {
        var panel = new OverlaysPanel { Owner = this };
        panel.CloseRequested += (_, _) => IsOpen = false;
        _panel = panel;
        Reconfigure();
        return panel.Present(host, Dismissible);
    }

    /// <inheritdoc />
    protected override void OnLayerRemoved() => _panel = null;

    /// <summary>Applies the position and the texts of the control to the panel that is shown. Derived controls call it when their layout properties change.</summary>
    private protected void Reconfigure()
    {
        if (_panel is not { } panel) return;
        panel.PanelTitle = Title;
        panel.PanelDescription = Description;
        panel.AccessibleName = AccessibleName;
        panel.ShowClose = Close;
        panel.ShowHandle = PanelHandle;
        panel.CloseLabel = CloseLabel;
        panel.BodyView = Body;
        panel.FooterView = Footer;
        panel.Configure(PanelMode, PanelSize, PanelInset);
        panel.Refresh();
        if (Layer?.Host is { Width: > 0 } host) panel.Fit(new Size(host.Width, host.Height));
    }

    private static void Refresh(BindableObject bindable, object oldValue, object newValue) => ((KlyPanelOverlay)bindable).Reconfigure();
}
