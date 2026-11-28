namespace Klyroo.UI.Maui;

/// <summary>How a layer of <see cref="KlyOverlayHost"/> comes in and goes out. Layers appear at once when the view has no handler yet (unit tests).</summary>
public enum KlyOverlayTransition
{
    /// <summary>No animation.</summary>
    None,
    /// <summary>The backdrop and the content fade.</summary>
    Fade,
    /// <summary>The content slides in from the left edge.</summary>
    SlideFromLeft,
    /// <summary>The content slides in from the right edge.</summary>
    SlideFromRight,
    /// <summary>The content slides in from the top edge.</summary>
    SlideFromTop,
    /// <summary>The content slides in from the bottom edge.</summary>
    SlideFromBottom,
}
