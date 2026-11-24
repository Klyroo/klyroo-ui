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
    public static IReadOnlyList<BxContentNavigationItem> Sidebar(string prefix, string? activeLibrarySlug = null)
    {
        // Every non-UI LuxStack library gets its own 100% independent multi-page documentation sidebar
        // (Getting Started, Guides, Reference) with zero cross-library clutter.
        if (activeLibrarySlug is not null && !string.Equals(activeLibrarySlug, "ui", StringComparison.OrdinalIgnoreCase))
        {
            var tree = LuxLibraryDocs.FindLibrary(activeLibrarySlug);
            if (tree is not null)
            {
                return
                [
                    .. tree.Pages
                        .Select(p => p.Group)
                        .Distinct()
                        .Select(group => new BxContentNavigationItem(group)
                        {
                            Open = true,
                            Children =
                            [
                                .. tree.Pages
                                    .Where(p => p.Group == group)
                                    .Select(p => new BxContentNavigationItem(p.Title)
                                    {
                                        Href = LuxLibraryDocs.PageHref(tree.LibrarySlug, p.Slug),
                                        Exact = true,
                                    }),
                            ],
                        }),
                ];
            }
        }

        // Lux.UI independent documentation sidebar: full Lux.UI guides + all 125+ components.
        var items = new List<BxContentNavigationItem>();
        items.AddRange(Catalog.Guides
            .Select(g => g.Section)
            .Distinct()
            .Select(section => new BxContentNavigationItem(section)
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

    private static readonly Dictionary<string, IReadOnlyList<BxContentSearchPage>> SearchPagesByKey = [];

    /// <summary>
    /// Header search pages. When browsing a specific LuxStack library, search is strictly scoped to that library
    /// (zero leakage of Lux.UI components into Lux.Query, Lux.Table, Lux.Form, etc.).
    /// </summary>
    public static IReadOnlyList<BxContentSearchPage> SearchPages(string prefix, string? activeLibrarySlug = null)
    {
        var cacheKey = $"{prefix}|{activeLibrarySlug ?? "hub"}";
        lock (SearchPagesByKey)
        {
            if (!SearchPagesByKey.TryGetValue(cacheKey, out var pages))
            {
                if (activeLibrarySlug is not null && !string.Equals(activeLibrarySlug, "ui", StringComparison.OrdinalIgnoreCase))
                {
                    var lib = LuxLibraryDocs.FindLibrary(activeLibrarySlug);
                    pages = lib is null
                        ? []
                        : [
                            new BxContentSearchPage($"{lib.LibraryName} Overview", $"/{lib.LibrarySlug}") { Group = lib.LibraryName, Icon = "" },
                            .. lib.Pages.Select(p => new BxContentSearchPage(p.Title, LuxLibraryDocs.PageHref(lib.LibrarySlug, p.Slug))
                            {
                                Group = p.Group,
                                Icon = "",
                                Keywords = $"{p.Slug} {p.Lead}",
                            }),
                        ];
                }
                else if (string.Equals(activeLibrarySlug, "ui", StringComparison.OrdinalIgnoreCase))
                {
                    pages =
                    [
                        new BxContentSearchPage("Lux.UI Overview", "/ui") { Group = "Lux.UI", Icon = "" },
                        .. Catalog.Guides.Select(g => new BxContentSearchPage(g.Title, g.Href) { Group = g.Section, Icon = "" }),
                        new BxContentSearchPage("Templates", "templates") { Group = "Gallery", Icon = "", Keywords = "starter dashboard landing live demo" },
                        .. InSidebarOrder.Select(c => new BxContentSearchPage(c.Name, $"{prefix}/components/{c.Slug}")
                        {
                            Group = c.Group,
                            Icon = "",
                            Keywords = c.Slug.Replace('-', ' '),
                        }),
                    ];
                }
                else
                {
                    pages =
                    [
                        new BxContentSearchPage("LuxStack Hub", "/") { Group = "LuxStack", Icon = "", Keywords = "luxstack hub libraries" },
                        .. LuxStackProducts.All.Select(p => new BxContentSearchPage($"{p.Name} Overview", $"/{p.Slug}") { Group = "Libraries", Icon = "", Keywords = $"{p.RepoName} {p.CorePackage} {p.UiPackage} {p.Tagline}" }),
                        .. LuxStackProducts.All.Select(p => new BxContentSearchPage($"{p.Name} Documentation", p.DocsHref) { Group = "Documentation", Icon = "", Keywords = $"{p.Slug} docs reference" }),
                    ];
                }

                SearchPagesByKey[cacheKey] = pages;
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
