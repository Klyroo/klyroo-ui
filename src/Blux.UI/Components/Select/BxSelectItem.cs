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
    /// Lists the item under a heading. The items without a group are listed first, in their order and with no heading, then
    /// each group in the order of its first item, with all its items, wherever they are in the list (so an item without a group
    /// never sits between two groups, where it would look like part of one). The heading is not an option: the arrow keys and the typeahead skip it,
    /// and a filter hides it when none of its items matches. The list has role group, named by the heading.
    /// </summary>
    public string? Group { get; init; }
}
