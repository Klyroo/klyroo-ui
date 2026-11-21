namespace Blux.UI;

/// <summary>An option of <see cref="BxSelect"/>, <see cref="BxSelectMenu"/>, <see cref="BxInputMenu"/> and <see cref="BxListbox"/>.</summary>
public sealed record BxSelectItem(string Value, string Label)
{
    public bool Disabled { get; init; }

    /// <summary>A number shown at the end of the row (matches, stock, unread): formatted for the locale, and not searched or typed ahead.</summary>
    public int? Count { get; init; }

    /// <summary>An icon drawn before the label, by name like every icon of Blux (a Lucide name, or any Iconify name the build bundles).</summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Lists the item under a heading. Items with the same group are listed together, where the first of them is; items
    /// without a group stay where they are, with no heading. The heading is not an option: the arrow keys and the typeahead skip it,
    /// and a filter hides it when none of its items matches. The list has role group, named by the heading.
    /// </summary>
    public string? Group { get; init; }
}
