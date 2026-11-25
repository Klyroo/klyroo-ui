using Klyroo.UI.Theming;

namespace Klyroo.UI;

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
    /// <summary>Semantic color of the badge. Defaults to the color of the navigation.</summary>
    public KlyColor? BadgeColor { get; init; }

    /// <summary>The entries below this one. They form a collapsible group.</summary>
    public IReadOnlyList<KlyContentNavigationItem>? Children { get; init; }

    /// <summary>Initial state of a group. By default a group is open when it holds the current page.</summary>
    public bool? Open { get; init; }
    /// <summary>Disables the entry: it is listed but cannot be followed.</summary>
    public bool Disabled { get; init; }

    /// <summary>The entry is current only on exactly its address; otherwise it is also current on the pages below it.</summary>
    public bool Exact { get; init; }

    /// <summary>Forces the current state instead of comparing <see cref="Href"/> with the current address.</summary>
    public bool? Active { get; init; }
    /// <summary>The <c>target</c> attribute of the link, such as <c>_blank</c>.</summary>
    public string? Target { get; init; }
}

/// <summary>A heading of the page, listed by <see cref="KlyContentToc"/>.</summary>
/// <param name="Id">The <c>id</c> of the heading element. The link points at it and the scroll-spy follows it.</param>
/// <param name="Text">The text of the link.</param>
public sealed record KlyContentTocLink(string Id, string Text)
{
    /// <summary>The heading level, 2 for an <c>h2</c>. Deeper levels are indented.</summary>
    public int Level { get; init; } = 2;
}

/// <summary>A page linked by <see cref="KlyContentSurround"/>.</summary>
/// <param name="Title">Title of the page.</param>
/// <param name="Href">Address of the page.</param>
public sealed record KlyContentSurroundLink(string Title, string Href)
{
    /// <summary>A line of context shown under the title.</summary>
    public string? Description { get; init; }
}

/// <summary>A page <see cref="KlyContentSearch"/> can find, and optionally the headings inside it.</summary>
/// <param name="Title">Title of the page.</param>
/// <param name="Href">Address of the page.</param>
public sealed record KlyContentSearchPage(string Title, string Href)
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
    public IReadOnlyList<KlyContentSearchHeading>? Headings { get; init; }
}

/// <summary>A heading of a <see cref="KlyContentSearchPage"/>.</summary>
/// <param name="Text">Text of the heading.</param>
/// <param name="Id">The <c>id</c> of the heading element, used as the fragment of the address.</param>
public sealed record KlyContentSearchHeading(string Text, string Id);

/// <summary>The width below which <see cref="KlyContentToc"/> collapses behind its title.</summary>
public enum KlyContentTocBreakpoint
{
    /// <summary>1024px.</summary>
    Lg,

    /// <summary>1280px.</summary>
    Xl,

    /// <summary>1536px.</summary>
    Xxl,
}

/// <summary>Per-instance class overrides of <see cref="KlyContentNavigation"/> slots.</summary>
public sealed record ContentNavigationUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>section</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Section { get; init; }
    /// <summary>Classes merged into the <c>sectionList</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? SectionList { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>badge</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Badge { get; init; }
    /// <summary>Classes merged into the <c>chevron</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Chevron { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>groupList</c> slot of <see cref="KlyContentNavigation"/>, after the theme and the app configuration.</summary>
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

/// <summary>Per-instance class overrides of <see cref="KlyContentToc"/> slots.</summary>
public sealed record ContentTocUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>chevron</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? Chevron { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyContentToc"/>, after the theme and the app configuration.</summary>
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

/// <summary>Per-instance class overrides of <see cref="KlyContentSurround"/> slots.</summary>
public sealed record ContentSurroundUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyContentSurround"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyContentSurround"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyContentSurround"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyContentSurround"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyContentSurround"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyContentSurround"/>, after the theme and the app configuration.</summary>
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

/// <summary>Per-instance class overrides of <see cref="KlyContentSearchButton"/> slots.</summary>
public sealed record ContentSearchButtonUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyContentSearchButton"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyContentSearchButton"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>kbds</c> slot of <see cref="KlyContentSearchButton"/>, after the theme and the app configuration.</summary>
    public string? Kbds { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "label" => Label,
        "kbds" => Kbds,
        _ => null,
    };
}
