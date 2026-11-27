namespace Klyroo.UI.Maui;

/// <summary>A panel of a <see cref="KlySplitter"/>.</summary>
public sealed record KlySplitterPanel
{
    /// <summary>Content of the panel. The panel clips it: put a scroll view inside when it can be longer than the panel.</summary>
    public View? Content { get; init; }

    /// <summary>Initial size in percent. Panels without one share what the others leave.</summary>
    public double? Size { get; init; }

    /// <summary>Smallest size in percent.</summary>
    public double MinSize { get; init; }

    /// <summary>Largest size in percent.</summary>
    public double MaxSize { get; init; } = 100;
}
