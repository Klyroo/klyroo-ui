using Blux.UI;

namespace Blux.Playground.Client.Docs;

/// <summary>The navigation data of the site, built from the catalog: sidebar, search, previous and next page, table of contents.</summary>
public static class DocsNavigation
{
    /// <summary>
    /// The components in the order of the sidebar: group by group, in catalog order inside each group. The previous
    /// and next links of a component page follow it, so they agree with what the reader sees on the left.
    /// </summary>
    public static IReadOnlyList<ComponentDoc> InSidebarOrder { get; } =
        [.. Catalog.Groups.SelectMany(group => Catalog.Components.Where(c => c.Group == group))];

    /// <summary>
    /// The sidebar. <paramref name="prefix"/> is the render mode prefix of the page ("/server", or "" for the default):
    /// component links keep it, so the reader stays in the mode they chose and the current page is found by address
    /// (the guides exist only in the default mode).
    /// </summary>
    public static IReadOnlyList<BxContentNavigationItem> Sidebar(string prefix)
    {
        var items = new List<BxContentNavigationItem>();
        items.AddRange(Catalog.Guides.Select(g => g.Section).Distinct().Select(section => new BxContentNavigationItem(section)
        {
            Open = true,
            Children = [.. Catalog.Guides.Where(g => g.Section == section).Select(g => new BxContentNavigationItem(g.Title) { Href = g.Href, Exact = true })],
        }));
        items.AddRange(Catalog.Groups.Where(group => Catalog.Components.Any(c => c.Group == group)).Select(group => new BxContentNavigationItem(group)
        {
            Open = true,
            Children = [.. Catalog.Components.Where(c => c.Group == group)
                .Select(c => new BxContentNavigationItem(c.Name) { Href = $"{prefix}/components/{c.Slug}", Exact = true })],
        }));
        return items;
    }

    private static readonly Dictionary<string, IReadOnlyList<BxContentSearchPage>> SearchPagesByPrefix = [];

    /// <summary>
    /// The pages the header search finds: the guides and every component, grouped like the sidebar. <paramref name="prefix"/> is
    /// the render mode prefix of the page, as for <see cref="Sidebar"/>: component links keep it, and the guides, which exist
    /// only in the default mode, stay as they are.
    /// </summary>
    public static IReadOnlyList<BxContentSearchPage> SearchPages(string prefix)
    {
        lock (SearchPagesByPrefix)
        {
            if (!SearchPagesByPrefix.TryGetValue(prefix, out var pages))
            {
                SearchPagesByPrefix[prefix] = pages =
                [
                    .. Catalog.Guides.Select(g => new BxContentSearchPage(g.Title, g.Href) { Group = g.Section, Icon = "" }),
                    .. InSidebarOrder.Select(c => new BxContentSearchPage(c.Name, $"{prefix}/components/{c.Slug}")
                    {
                        Group = c.Group,
                        Icon = "",
                        Keywords = c.Slug.Replace('-', ' '),
                    }),
                ];
            }
            return pages;
        }
    }

    /// <summary>The links to the components before and after <paramref name="slug"/>, in sidebar order.</summary>
    public static (BxContentSurroundLink? Previous, BxContentSurroundLink? Next) Surround(string slug, string prefix)
    {
        var index = -1;
        for (var i = 0; i < InSidebarOrder.Count; i++)
        {
            if (InSidebarOrder[i].Slug == slug) index = i;
        }
        if (index < 0) return (null, null);
        BxContentSurroundLink Link(ComponentDoc doc) => new(doc.Name, $"{prefix}/components/{doc.Slug}") { Description = doc.Description };
        return (index > 0 ? Link(InSidebarOrder[index - 1]) : null, index < InSidebarOrder.Count - 1 ? Link(InSidebarOrder[index + 1]) : null);
    }

    /// <summary>The id of the section of an example, as <c>Example.razor</c> gives it.</summary>
    public static string ExampleAnchor(string title) => title.ToLowerInvariant().Replace(' ', '-');

    /// <summary>The table of contents of a component page: its examples, then the API.</summary>
    public static IReadOnlyList<BxContentTocLink> Toc(ComponentDoc doc) =>
    [
        .. doc.Examples.Select(e => new BxContentTocLink(ExampleAnchor(e.Title), e.Title)),
        new BxContentTocLink("api", "API"),
    ];
}
