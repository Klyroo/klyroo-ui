namespace Klyroo.UI.Maui;

/// <summary>One item of a <see cref="KlyList"/>.</summary>
/// <param name="Text">The item text. It is a link when <see cref="Href"/> is set.</param>
public sealed record KlyListItem(string Text)
{
    /// <summary>A line under the text.</summary>
    public string? Description { get; init; }

    /// <summary>The name of an icon (see <see cref="KlyIcon"/>) shown instead of the bullet in the plain layout. The steps and checks layouts keep their own marker.</summary>
    public string? Icon { get; init; }

    /// <summary>A color for the bullet or <see cref="Icon"/> in the plain layout.</summary>
    public KlyColor? Color { get; init; }

    /// <summary>Marks the item as done in the steps and checks layouts.</summary>
    public bool Done { get; init; }

    /// <summary>Marks the item as the current step in the steps and checks layouts.</summary>
    public bool Current { get; init; }

    /// <summary>Text shown at the end of the row.</summary>
    public string? Meta { get; init; }

    /// <summary>The address the item links to.</summary>
    public string? Href { get; init; }
}
