namespace Klyroo.UI.Maui;

/// <summary>Look of a message bubble. <see cref="Naked"/> has no surface at all.</summary>
public enum ChatMessageVariant
{
    /// <summary>A filled bubble in the semantic color.</summary>
    Solid,
    /// <summary>A border and no fill.</summary>
    Outline,
    /// <summary>A light tint of the color.</summary>
    Soft,
    /// <summary>A light tint with a border.</summary>
    Subtle,
    /// <summary>No surface.</summary>
    Naked,
}
