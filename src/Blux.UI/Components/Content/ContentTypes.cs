using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>An entry of <see cref="BxContentNavigation"/>: a page link, or a group of entries when it has children.</summary>
public sealed record BxContentNavigationItem(string Title)
{
    /// <summary>The page the entry links to. An entry with children is a group and has no link of its own.</summary>
    public string? Href { get; init; }

    /// <summary>A built-in Lucide icon name ("book-open", "file"), shown before the title.</summary>
    public string? Icon { get; init; }

    /// <summary>A short label after the title: "New", a count.</summary>
    public string? Badge { get; init; }
    public BxColor? BadgeColor { get; init; }

    /// <summary>The entries below this one. They form a collapsible group.</summary>
    public IReadOnlyList<BxContentNavigationItem>? Children { get; init; }

    /// <summary>Initial state of a group. By default a group is open when it holds the current page.</summary>
    public bool? Open { get; init; }
    public bool Disabled { get; init; }

    /// <summary>The entry is current only on exactly its address; otherwise it is also current on the pages below it.</summary>
    public bool Exact { get; init; }

    /// <summary>Forces the current state instead of comparing <see cref="Href"/> with the current address.</summary>
    public bool? Active { get; init; }
    public string? Target { get; init; }
}

/// <summary>A heading of the page, listed by <see cref="BxContentToc"/>.</summary>
/// <param name="Id">The <c>id</c> of the heading element. The link points at it and the scroll-spy follows it.</param>
/// <param name="Text">The text of the link.</param>
public sealed record BxContentTocLink(string Id, string Text)
{
    /// <summary>The heading level, 2 for an <c>h2</c>. Deeper levels are indented.</summary>
    public int Level { get; init; } = 2;
}

/// <summary>A page linked by <see cref="BxContentSurround"/>.</summary>
public sealed record BxContentSurroundLink(string Title, string Href)
{
    public string? Description { get; init; }
}

/// <summary>A page <see cref="BxContentSearch"/> can find, and optionally the headings inside it.</summary>
public sealed record BxContentSearchPage(string Title, string Href)
{
    /// <summary>A line of context shown under the title.</summary>
    public string? Description { get; init; }

    /// <summary>The section the page is listed under in the results. Pages without one have no heading.</summary>
    public string? Group { get; init; }

    /// <summary>A built-in Lucide icon name, shown before the page. Defaults to a file; an empty string shows none.</summary>
    public string? Icon { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }

    /// <summary>Headings of the page, searchable on their own and linking to <c>Href#Id</c>.</summary>
    public IReadOnlyList<BxContentSearchHeading>? Headings { get; init; }
}

/// <summary>A heading of a <see cref="BxContentSearchPage"/>.</summary>
public sealed record BxContentSearchHeading(string Text, string Id);

/// <summary>The width below which <see cref="BxContentToc"/> collapses behind its title.</summary>
public enum BxContentTocBreakpoint
{
    /// <summary>1024px.</summary>
    Lg,

    /// <summary>1280px.</summary>
    Xl,

    /// <summary>1536px.</summary>
    Xxl,
}

/// <summary>Per-instance class overrides of <see cref="BxContentNavigation"/> slots.</summary>
public sealed record ContentNavigationUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }
    public string? Trigger { get; init; }
    public string? Section { get; init; }
    public string? SectionList { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Badge { get; init; }
    public string? Chevron { get; init; }
    public string? Group { get; init; }
    public string? GroupList { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "trigger" => Trigger,
        "section" => Section,
        "sectionList" => SectionList,
        "icon" => Icon,
        "label" => Label,
        "badge" => Badge,
        "chevron" => Chevron,
        "group" => Group,
        "groupList" => GroupList,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxContentToc"/> slots.</summary>
public sealed record ContentTocUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Trigger { get; init; }
    public string? Title { get; init; }
    public string? Chevron { get; init; }
    public string? Content { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "trigger" => Trigger,
        "title" => Title,
        "chevron" => Chevron,
        "content" => Content,
        "list" => List,
        "item" => Item,
        "link" => Link,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxContentSurround"/> slots.</summary>
public sealed record ContentSurroundUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Link { get; init; }
    public string? Caption { get; init; }
    public string? Icon { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "link" => Link,
        "caption" => Caption,
        "icon" => Icon,
        "title" => Title,
        "description" => Description,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxContentSearchButton"/> slots.</summary>
public sealed record ContentSearchButtonUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Label { get; init; }
    public string? Kbds { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "label" => Label,
        "kbds" => Kbds,
        _ => null,
    };
}
