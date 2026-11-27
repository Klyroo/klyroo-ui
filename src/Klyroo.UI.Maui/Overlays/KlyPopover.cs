namespace Klyroo.UI.Maui;

/// <summary>
/// A floating panel anchored to its trigger. <see cref="ContentView.Content"/> is the trigger: its first button (or the trigger itself) opens and closes the panel,
/// <see cref="PanelContent"/> is what the panel shows. A tap outside and the back button close it. Needs a <see cref="KlyOverlayHost"/> above it.
/// </summary>
public class KlyPopover : KlyOverlayBase
{
    /// <summary>Identifies <see cref="PanelContent"/>.</summary>
    public static readonly BindableProperty PanelContentProperty = BindableProperty.Create(nameof(PanelContent), typeof(View), typeof(KlyPopover), null, propertyChanged: (b, _, v) => ((KlyPopover)b).ShowPanelContent((View?)v));
    /// <summary>Identifies <see cref="Placement"/>.</summary>
    public static readonly BindableProperty PlacementProperty = BindableProperty.Create(nameof(Placement), typeof(KlyPlacement), typeof(KlyPopover), KlyPlacement.Bottom);
    /// <summary>Identifies <see cref="Offset"/>.</summary>
    public static readonly BindableProperty OffsetProperty = BindableProperty.Create(nameof(Offset), typeof(double), typeof(KlyPopover), 8d);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyPopover), null, propertyChanged: (b, _, v) =>
    {
        if (((KlyPopover)b)._panel is { } panel) SemanticProperties.SetDescription(panel, (string?)v);
    });

    private Border? _panel;

    /// <summary>Creates a popover.</summary>
    public KlyPopover()
    {
    }

    /// <summary>What the panel shows (the <c>Content</c> of the Blazor component).</summary>
    public View? PanelContent { get => (View?)GetValue(PanelContentProperty); set => SetValue(PanelContentProperty, value); }

    /// <summary>Where the panel sits against the trigger. Defaults to <see cref="KlyPlacement.Bottom"/>. The host flips it when there is no room.</summary>
    public KlyPlacement Placement { get => (KlyPlacement)GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Distance between the trigger and the panel, in points. Read when the popover opens.</summary>
    public double Offset { get => (double)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>What a screen reader says for the panel.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>The panel while it is shown, for the tests.</summary>
    internal Border? Panel => _panel;

    /// <inheritdoc />
    protected override KlyOverlayLayer Present(KlyOverlayHost host)
    {
        var panel = new Border { Padding = 0, MinimumWidthRequest = 128 };
        panel.ApplySurface(KlyTheme.RadiusOf(1));
        SemanticProperties.SetDescription(panel, AccessibleName);
        _panel = panel;
        ShowPanelContent(PanelContent);

        var layer = host.Show(panel, new KlyOverlayLayerOptions
        {
            Modal = true,
            Dim = false,
            Anchor = Content ?? this,
            Placement = Placement,
            Offset = Offset,
        });
        layer.HostSizeChanged += (_, size) => panel.MaximumWidthRequest = Math.Max(0, size.Width - 16);
        if (host.Width > 0) panel.MaximumWidthRequest = Math.Max(0, host.Width - 16);
        return layer;
    }

    /// <inheritdoc />
    protected override void OnLayerRemoved()
    {
        // Let go of the content, so it can be shown again or moved elsewhere.
        if (_panel is not null) _panel.Content = null;
        _panel = null;
    }

    private void ShowPanelContent(View? content)
    {
        if (_panel is not { } panel) return;
        if (content is not null) OverlaysSupport.Detach(content);
        panel.Content = content;
    }
}
