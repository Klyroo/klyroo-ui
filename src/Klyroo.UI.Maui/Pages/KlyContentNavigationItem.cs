namespace Klyroo.UI.Maui;

/// <summary>An entry of <see cref="KlyContentNavigation"/>: a page link, or a group of entries when it has children.</summary>
/// <param name="Title">Text of the entry.</param>
public sealed record KlyContentNavigationItem(string Title)
{
    /// <summary>The page the entry links to. An entry with children is a group and has no link of its own.</summary>
    public string? Href { get; init; }

    /// <summary>A built-in Lucide icon name ("book-open", "file"), shown before the title.</summary>
    public string? Icon { get; init; }

    /// <summary>A short label after the title: "New", a count.</summary>
    public string? Badge { get; init; }

    /// <summary>Semantic color of the badge. Defaults to neutral.</summary>
    public KlyColor? BadgeColor { get; init; }

    /// <summary>The entries below this one. They form a collapsible group.</summary>
    public IReadOnlyList<KlyContentNavigationItem>? Children { get; init; }

    /// <summary>Initial state of a group. By default a group is open when it holds the current page.</summary>
    public bool? Open { get; init; }

    /// <summary>Disables the entry: it is listed but cannot be followed.</summary>
    public bool Disabled { get; init; }

    /// <summary>The entry is current only on exactly its address; otherwise it is also current on the pages below it.</summary>
    public bool Exact { get; init; }

    /// <summary>Forces the current state instead of comparing <see cref="Href"/> with <see cref="KlyContentNavigation.CurrentHref"/>.</summary>
    public bool? Active { get; init; }
}
