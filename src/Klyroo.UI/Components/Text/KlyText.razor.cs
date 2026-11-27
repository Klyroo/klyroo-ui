namespace Klyroo.UI;

/// <summary>The layout of a <see cref="KlyText"/>.</summary>
public enum KlyTextVariant
{
    /// <summary>Plain paragraphs.</summary>
    Default,
    /// <summary>A block quote: a start border, italic text and a footer with the author and meta text.</summary>
    Quote,
}

/// <summary>One or more paragraphs, or a quote, with an optional title, clamp to a number of lines and footer.</summary>
public partial class KlyText;
