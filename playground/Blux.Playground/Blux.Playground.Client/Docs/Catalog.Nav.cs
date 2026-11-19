using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Nav =
    [
        new("breadcrumb", "Breadcrumb", "Navigation", "The path to the current page, as a nav landmark with an ordered list.",
            [new("Breadcrumb", typeof(Examples.BreadcrumbExample), "Pure markup: works in every render mode.")], [typeof(BxBreadcrumb)]),
        new("pagination", "Pagination", "Navigation", "Page links or buttons with ellipses, siblings and first/last controls.",
            [new("Bound page", typeof(Examples.PaginationExample), "Buttons report through PageChanged and stay disabled until the page is interactive."),
             new("Links", typeof(Examples.PaginationLinksExample), "With HrefTemplate every page is a link: it works in static rendering.")],
            [typeof(BxPagination)]),
        new("stepper", "Stepper", "Navigation", "Steps of a process with a current and completed state.",
            [new("Interactive", typeof(Examples.StepperExample), "Bind Current to let users click a step."), new("Vertical", typeof(Examples.StepperVerticalExample))],
            [typeof(BxStepper)]),
        new("timeline", "Timeline", "Navigation", "A chronological list of events.",
            [new("Vertical", typeof(Examples.TimelineExample)), new("Horizontal", typeof(Examples.TimelineHorizontalExample))], [typeof(BxTimeline)]),
        new("navigation-menu", "NavigationMenu", "Navigation", "Links with an active state taken from the address; sub-items open in a panel or a collapsible group.",
            [new("Horizontal", typeof(Examples.NavigationMenuExample), "A disclosure pattern, not role=menu."), new("Vertical", typeof(Examples.NavigationMenuVerticalExample))],
            [typeof(BxNavigationMenu)]),
        new("collapsible", "Collapsible", "Navigation", "A button that shows or hides a region, handled in the browser.",
            [new("Collapsible", typeof(Examples.CollapsibleExample), "Closed content stays findable with find-in-page.")], [typeof(BxCollapsible)]),
        new("tree", "Tree", "Navigation", "A WAI-ARIA tree view with arrow-key navigation and optional selection.",
            [new("Tree", typeof(Examples.TreeExample), "Up, Down, Left, Right, Home, End, Enter and Space; type a letter to jump."),
             new("Links", typeof(Examples.TreeLinksExample), "Items with an Href follow the link on Enter; a parent also toggles once.")], [typeof(BxTree)]),
        new("marquee", "Marquee", "Navigation", "Content that scrolls endlessly; pure CSS, paused on hover and by reduced-motion preferences.",
            [new("Marquee", typeof(Examples.MarqueeExample))], [typeof(BxMarquee)]),
        new("scroll-area", "ScrollArea", "Navigation", "A styled scroll container that stays reachable with the keyboard.",
            [new("Scroll area", typeof(Examples.ScrollAreaExample))], [typeof(BxScrollArea)]),
        new("splitter", "Splitter", "Navigation", "Resizable panels, with the mouse or the arrow keys.",
            [new("Horizontal", typeof(Examples.SplitterExample), "Drag the handle, or focus it and use the arrow keys, Home and End."), new("Vertical", typeof(Examples.SplitterVerticalExample))],
            [typeof(BxSplitter)]),
    ];
}
