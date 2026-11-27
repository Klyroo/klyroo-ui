namespace Klyroo.UI.Maui;

/// <summary>A radio button of a <see cref="KlyRadioGroup"/>.</summary>
/// <param name="Value">Value the group takes when the radio button is chosen.</param>
/// <param name="Label">Text next to the radio button.</param>
public sealed record KlyRadioItem(object? Value, string Label)
{
    /// <summary>Text under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Disables the radio button.</summary>
    public bool Disabled { get; init; }
}
