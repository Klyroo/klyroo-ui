namespace Klyroo.UI.Maui;

/// <summary>How <see cref="KlyOverlayHost.ShowAsync{TResult}"/> presents the view it opens.</summary>
public enum KlyOverlayKind
{
    /// <summary>A centered modal dialog, as <see cref="KlyModal"/>.</summary>
    Modal,
    /// <summary>A panel on an edge of the screen, as <see cref="KlySlideover"/>.</summary>
    Slideover,
}
