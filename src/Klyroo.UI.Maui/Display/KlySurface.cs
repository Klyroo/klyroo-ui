namespace Klyroo.UI.Maui;

/// <summary>The surface treatment of a read-only block such as <see cref="KlyFacts"/> or <see cref="KlyStat{TValue}"/>: border, background and padding.</summary>
public enum KlySurface
{
    /// <summary>No border, background or padding.</summary>
    Plain,
    /// <summary>A muted background with padding.</summary>
    Muted,
    /// <summary>A border and padding.</summary>
    Bordered,
    /// <summary>A border, an elevated background, padding and a shadow.</summary>
    Elevated,
}
