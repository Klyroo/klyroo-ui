using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Content =
    [
        new("content-navigation", "ContentNavigation", "Navigation", "The navigation of a documentation site: a tree of pages with collapsible groups, icons and badges.",
            [new("Nested groups", typeof(Examples.ContentNavigationExample), "Groups open and close in the browser. The current page is marked with aria-current=\"page\" from the address; this site's sidebar is a ContentNavigation.")],
            [typeof(BxContentNavigation)]),
        new("content-search", "ContentSearch", "Navigation", "A documentation search: a command palette in a modal over a list of pages and their headings, opened by a shortcut.",
            [new("Pages and headings", typeof(Examples.ContentSearchExample), "Ctrl+K (Cmd+K on a Mac) opens the search of this site, over the whole catalog. This example has its own Name and shortcut. Filtering and the shortcut run in the browser."),
             new("Index loaded when it opens", typeof(Examples.ContentSearchLazyExample), "With IndexUrl the pages are a JSON file that the browser fetches the first time the search opens, so a large index is not part of every page. The button here, or Alt+L, opens it. The fetch is shown while it runs, and a failure says so with a button to try again.")],
            [typeof(BxContentSearch)]),
        new("content-search-button", "ContentSearchButton", "Navigation", "A button that opens the search and shows its shortcut.",
            [new("Full and icon only", typeof(Examples.ContentSearchButtonExample), "These buttons open the search of this site, the one in the header.")],
            [typeof(BxContentSearchButton)]),
        new("content-surround", "ContentSurround", "Navigation", "Links to the previous and the next page, with their titles and descriptions.",
            [new("Previous and next", typeof(Examples.ContentSurroundExample), "The bottom of every component page of this site is a ContentSurround, in the order of the sidebar.")],
            [typeof(BxContentSurround)]),
        new("content-toc", "ContentToc", "Navigation", "A table of contents that follows the heading being read, and collapses behind its title on small screens.",
            [new("Scroll-spy", typeof(Examples.ContentTocExample), "An IntersectionObserver in the browser marks the link of the heading being read. The right column of every component page is a ContentToc.")],
            [typeof(BxContentToc)]),
    ];
}
