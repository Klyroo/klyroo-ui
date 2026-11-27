namespace Klyroo.UI.Maui;

/// <summary>Look of a <see cref="KlyBlogPost"/> card.</summary>
public enum BlogPostVariant
{
    /// <summary>A border around the card.</summary>
    Outline,
    /// <summary>A tinted background.</summary>
    Soft,
    /// <summary>A tinted background with a border.</summary>
    Subtle,
    /// <summary>A transparent card with the usual padding.</summary>
    Ghost,
    /// <summary>A transparent card that does not clip its content.</summary>
    Naked,
}
