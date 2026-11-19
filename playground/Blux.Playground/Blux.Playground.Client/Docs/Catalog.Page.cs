using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Page =
    [
        new("page", "Page", "Page", "A grid of up to three columns: optional left and right asides around the main content.",
            [new("Left, center and right", typeof(Examples.PageLayoutExample), "The side columns show from the lg breakpoint and stack in reading order below it.")], [typeof(BxPage)]),
        new("page-header", "PageHeader", "Page", "The top of a page: a headline, a title, a description and links.",
            [new("Header with links", typeof(Examples.PageHeaderExample), "The title is the main heading of the page by default; set HeadingLevel when it sits under another heading.")], [typeof(BxPageHeader)]),
        new("page-hero", "PageHero", "Page", "A landing hero with a headline, a title, a description, links and an optional media slot.",
            [new("With media", typeof(Examples.PageHeroExample), "The media sits beside the text from the lg breakpoint; Reverse puts it first."),
             new("Vertical", typeof(Examples.PageHeroVerticalExample))], [typeof(BxPageHero)]),
        new("page-section", "PageSection", "Page", "A marketing section with a headline, a title, a description, features, links and media.",
            [new("Features and media", typeof(Examples.PageSectionExample), "The section is a landmark named by its title. A feature with an Href is one link over the whole feature.")],
            [typeof(BxPageSection), typeof(PageFeatureItem), typeof(PageLinkItem)]),
        new("page-feature", "PageFeature", "Page", "An icon, a title and a description, optionally one link over the whole feature.",
            [new("Horizontal, vertical and link", typeof(Examples.PageFeatureExample), "With an Href the title is the only link and its box stretches over the feature: one focus stop, no nested links.")],
            [typeof(BxPageFeature)]),
        new("page-grid", "PageGrid", "Page", "A responsive grid of two to four columns.",
            [new("Three columns", typeof(Examples.PageGridExample))], [typeof(BxPageGrid)]),
        new("page-columns", "PageColumns", "Page", "Masonry-style CSS columns: items of different heights pack without gaps.",
            [new("Three columns", typeof(Examples.PageColumnsExample), "Items run down each column in reading order, then continue in the next.")], [typeof(BxPageColumns)]),
        new("page-card", "PageCard", "Page", "A card with an icon, a title and a description, in four variants, optionally one link over the whole card.",
            [new("Variants", typeof(Examples.PageCardVariantsExample), "The spotlight glow is CSS only and off under prefers-reduced-motion."),
             new("Whole-card link", typeof(Examples.PageCardLinkExample), "The title is the only link; clicking anywhere on the card follows it. Interactive content inside the card needs relative z-10 to stay clickable.")],
            [typeof(BxPageCard)]),
        new("page-cta", "PageCta", "Page", "A call to action band with a title, a description and links, in four variants.",
            [new("Solid and soft", typeof(Examples.PageCtaExample), "In the solid variant use a neutral button: a primary one would disappear into the band.")], [typeof(BxPageCta)]),
        new("page-links", "PageLinks", "Page", "A titled list of links that marks the current page with aria-current.",
            [new("Resources and account", typeof(Examples.PageLinksExample), "The navigation is named by its title; a link opening a new tab says so to screen readers.")],
            [typeof(BxPageLinks), typeof(PageLinkItem)]),
        new("page-list", "PageList", "Page", "A vertical list with optional dividers, for stacks of users, posts or cards.",
            [new("With dividers", typeof(Examples.PageListExample), "The direct children are li elements.")], [typeof(BxPageList)]),
        new("page-logos", "PageLogos", "Page", "A strip of logos, optionally scrolling as a marquee.",
            [new("Logo list", typeof(Examples.PageLogosExample)),
             new("Marquee", typeof(Examples.PageLogosMarqueeExample), "CSS only; the scrolling copies are hidden from assistive technology and the animation stops under prefers-reduced-motion.")],
            [typeof(BxPageLogos), typeof(PageLogoItem)]),
        new("page-anchors", "PageAnchors", "Page", "A list of in-page anchors, typically the table of contents in a right column.",
            [new("Anchors", typeof(Examples.PageAnchorsExample), "Plain anchors: an address-based active state would mark every fragment link as current.")], [typeof(BxPageAnchors)]),
        new("page-aside", "PageAside", "Page", "A sticky aside for page navigation, shown from the lg breakpoint.",
            [new("Top, body and bottom", typeof(Examples.PageAsideExample), "An aside is a complementary landmark: name it when the page has several, and render it as a div when it sits inside another landmark.")],
            [typeof(BxPageAside)]),
        new("page-body", "PageBody", "Page", "A wrapper that styles plain HTML content: headings, paragraphs, lists, code and quotes.",
            [new("Prose", typeof(Examples.PageBodyExample))], [typeof(BxPageBody)]),
    ];
}
