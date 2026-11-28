namespace Klyroo.UI.Maui;

/// <summary>A checkbox of a <see cref="KlyCheckboxGroup"/>.</summary>
/// <param name="Value">Value added to the selection when the checkbox is checked.</param>
/// <param name="Label">Text next to the checkbox.</param>
public sealed record KlyCheckboxItem(object? Value, string Label)
{
    /// <summary>Text under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Disables the checkbox.</summary>
    public bool Disabled { get; init; }
}
