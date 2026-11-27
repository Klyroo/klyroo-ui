namespace Klyroo.UI.Maui;

/// <summary>An option of <see cref="KlySelectMenu"/>, <see cref="KlyInputMenu"/> and <see cref="KlyListbox"/>. A list may also hold plain objects: each one shows as its <c>ToString()</c> and is its own value.</summary>
/// <param name="Value">Value the control takes when the option is chosen.</param>
/// <param name="Label">Text of the option.</param>
public sealed record KlySelectItem(object? Value, string Label)
{
    /// <summary>The option is listed but cannot be chosen.</summary>
    public bool Disabled { get; init; }

    /// <summary>A number shown at the end of the row (matches, stock, unread), formatted for the current culture.</summary>
    public int? Count { get; init; }

    /// <summary>An icon drawn before the label: a Lucide name such as <see cref="KlyIconNames.Star"/>, or one registered with <see cref="KlyIcons.Register"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>Lists the item under a heading. The items without a group come first, with no heading, then each group in the order of its first item.</summary>
    public string? Group { get; init; }
}
