namespace Klyroo.UI.Maui;

/// <summary>A button of a <see cref="KlyToggleGroup"/>.</summary>
/// <param name="Value">Value of the item in the selection of the group.</param>
/// <param name="Label">Text of the button.</param>
public sealed record KlyToggleItem(string Value, string Label)
{
    /// <summary>Disables the button.</summary>
    public bool Disabled { get; init; }

    /// <summary>A view drawn before the label, such as a <see cref="KlyIcon"/>. A view can only be in one place: give each item its own.</summary>
    public View? Icon { get; init; }
}
