namespace Klyroo.UI.Maui;

/// <summary>A view shown above the page by a <see cref="KlyOverlayHost"/>. Keep it to hide the view or to learn that the user dismissed it.</summary>
public sealed class KlyOverlayLayer
{
    internal KlyOverlayLayer(KlyOverlayHost host, View content, KlyOverlayLayerOptions options)
    {
        Host = host;
        Content = content;
        Options = options;
        Dismissible = options.Dismissible;
    }

    /// <summary>The host that shows the layer.</summary>
    public KlyOverlayHost Host { get; }

    /// <summary>The view of the layer.</summary>
    public View Content { get; }

    /// <summary>How the layer was presented.</summary>
    public KlyOverlayLayerOptions Options { get; }

    /// <summary>A tap outside the content and the back button dismiss the layer. Starts as <see cref="KlyOverlayLayerOptions.Dismissible"/>.</summary>
    public bool Dismissible { get; set; }

    /// <summary>True from <see cref="KlyOverlayHost.Show"/> until the layer is hidden or dismissed.</summary>
    public bool IsShown { get; internal set; }

    /// <summary>Raised once when the user dismisses the layer (a tap outside, the back button, or <see cref="KlyOverlayHost.Dismiss"/>), after it left <see cref="KlyOverlayHost.Layers"/>. Not raised by <see cref="KlyOverlayHost.Hide"/>.</summary>
    public event EventHandler? Dismissed;

    /// <summary>Raised with the size of the host when the layer is shown and whenever the host is resized, so the content can fit it (a panel narrower than the screen, a menu shorter than it).</summary>
    public event EventHandler<Size>? HostSizeChanged;

    internal Grid Container { get; set; } = null!;

    internal BoxView? Backdrop { get; set; }

    internal EventHandler? ContentSizeChanged { get; set; }

    internal void RaiseDismissed() => Dismissed?.Invoke(this, EventArgs.Empty);

    internal void RaiseHostSizeChanged(Size size) => HostSizeChanged?.Invoke(this, size);
}
