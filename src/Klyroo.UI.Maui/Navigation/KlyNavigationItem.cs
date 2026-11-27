namespace Klyroo.UI.Maui;

/// <summary>A link of a <see cref="KlyNavigationMenu"/>, with optional sub-items.</summary>
/// <param name="Label">Text of the link.</param>
public sealed record KlyNavigationItem(string Label)
{
    /// <summary>
    /// The address or route the item stands for: <see cref="KlyNavigationMenu.ItemInvoked"/> is raised when the user taps it, and the menu compares it with
    /// <see cref="KlyNavigationMenu.Current"/> to mark the active item. An item with children and no address is a group; an item with neither is plain text.
    /// </summary>
    public string? Href { get; init; }

    /// <summary>Icon shown before the label: a Lucide name such as <see cref="KlyIconNames.Menu"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Sub-items. Horizontally they open in a panel under the row, vertically they form a collapsible group.
    /// An item with children has no link of its own.
    /// </summary>
    public IReadOnlyList<KlyNavigationItem>? Children { get; init; }

    /// <summary>Disables the item: it is listed but cannot be followed.</summary>
    public bool Disabled { get; init; }

    /// <summary>The item is active only on exactly its address; otherwise it is also active on the addresses below it.</summary>
    public bool Exact { get; init; }

    /// <summary>Forces the active state instead of comparing <see cref="Href"/> with <see cref="KlyNavigationMenu.Current"/>.</summary>
    public bool? Active { get; init; }
}
