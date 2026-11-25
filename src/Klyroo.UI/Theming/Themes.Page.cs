namespace Klyroo.UI.Theming;

/// <summary>Themes of the page family: page layout, header, hero, section, feature, grid, columns, card, call to action, links, list, logos, anchors, aside, body.</summary>
internal static partial class Themes
{
    private const string PageEyebrow = "font-mono text-xs/5 font-medium uppercase tracking-wider text-(--klyroo-primary-text)";

    public static ComponentTheme Page() =>
        new ComponentTheme("page", "root", "left", "center", "right")
            .Slot("root", "flex flex-col gap-8 lg:grid lg:grid-cols-10 lg:gap-10")
            .Slot("left", "min-w-0 lg:col-span-2")
            .Slot("center", "min-w-0")
            .Slot("right", "min-w-0 lg:col-span-2")
            .Variant("center", "6", "center", "lg:col-span-6")
            .Variant("center", "8", "center", "lg:col-span-8")
            .Variant("center", "10", "center", "lg:col-span-10")
            .Default("center", "10");

    public static ComponentTheme PageHeader() =>
        new ComponentTheme("pageHeader", "root", "wrapper", "headline", "title", "description", "links", "body")
            .Slot("root", "relative border-b border-(--klyroo-border) py-8")
            .Slot("wrapper", "flex min-w-0 flex-col")
            .Slot("headline", "mb-2.5 " + PageEyebrow)
            .Slot("title", "text-3xl font-semibold leading-tight tracking-tight text-pretty text-(--klyroo-text-highlighted) sm:text-4xl")
            .Slot("description", "mt-3 max-w-2xl text-base text-pretty text-(--klyroo-text-muted)")
            .Slot("links", "mt-4 flex flex-wrap items-center gap-1.5")
            .Slot("body", "mt-6");

    public static ComponentTheme PageHero() =>
        new ComponentTheme("pageHero", "root", "container", "wrapper", "headline", "title", "description", "links", "media")
            .Slot("root", "relative isolate overflow-hidden")
            .Slot("container", "mx-auto flex w-full max-w-(--breakpoint-xl) flex-col gap-16 px-4 py-20 sm:gap-y-24 sm:px-6 sm:py-24 lg:px-8 lg:py-32")
            .Slot("wrapper", "min-w-0")
            .Slot("headline", "mb-5 " + PageEyebrow)
            .Slot("title", "text-5xl font-semibold leading-[0.95] tracking-[-0.045em] text-pretty text-(--klyroo-text-highlighted) sm:text-6xl lg:text-7xl")
            .Slot("description", "mt-6 text-lg text-pretty text-(--klyroo-text-muted) sm:text-xl/8")
            .Slot("links", "mt-8 flex flex-wrap gap-x-4 gap-y-3")
            .Slot("media", "min-w-0")
            .Variant("orientation", "horizontal", "container", "lg:grid lg:grid-cols-2 lg:items-center lg:gap-16")
            .Variant("orientation", "vertical", "wrapper", "mx-auto text-center")
            .Variant("orientation", "vertical", "description", "mx-auto max-w-2xl")
            .Variant("orientation", "vertical", "links", "justify-center")
            .Variant("reverse", "true", "media", "lg:order-first")
            .Default("orientation", "horizontal")
            .Default("reverse", "false");

    public static ComponentTheme PageSection() =>
        new ComponentTheme("pageSection", "root", "container", "wrapper", "leading", "leadingIcon", "headline", "title", "description", "links", "features", "feature", "media")
            .Slot("root", "relative isolate")
            .Slot("container", "mx-auto flex w-full max-w-(--breakpoint-xl) flex-col gap-8 px-4 py-16 sm:gap-16 sm:px-6 sm:py-24 lg:px-8 lg:py-32")
            .Slot("wrapper", "min-w-0")
            .Slot("leading", "mb-6 inline-flex size-10 items-center justify-center rounded-(--klyroo-radius) bg-(--klyroo-primary)/10 text-(--klyroo-primary-text)")
            .Slot("leadingIcon", "size-6")
            .Slot("headline", "mb-3 " + PageEyebrow)
            .Slot("title", "text-3xl font-semibold tracking-tight text-pretty text-(--klyroo-text-highlighted) sm:text-4xl")
            .Slot("description", "mt-4 max-w-2xl text-base text-pretty text-(--klyroo-text-muted) sm:text-lg")
            .Slot("links", "mt-8 flex flex-wrap gap-x-4 gap-y-3")
            .Slot("features", "mt-10 m-0 grid list-none grid-cols-1 gap-8 p-0")
            .Slot("feature", "min-w-0")
            .Slot("media", "min-w-0")
            .Variant("orientation", "horizontal", "container", "lg:grid lg:grid-cols-2 lg:items-center lg:gap-16")
            .Variant("orientation", "horizontal", "features", "sm:grid-cols-2 lg:grid-cols-1")
            .Variant("orientation", "vertical", "features", "sm:grid-cols-2 lg:grid-cols-3")
            .Variant("reverse", "true", "media", "lg:order-first")
            .Default("orientation", "horizontal")
            .Default("reverse", "false");

    public static ComponentTheme PageFeature() =>
        new ComponentTheme("pageFeature", "root", "leading", "leadingIcon", "wrapper", "title", "description", "link")
            .Slot("root", "relative flex items-start gap-3 rounded-(--klyroo-radius)")
            .Slot("leading", "inline-flex size-9 shrink-0 items-center justify-center rounded-(--klyroo-radius) bg-(--kly-color)/10 text-(--kly-color-text)")
            .Slot("leadingIcon", "size-5")
            .Slot("wrapper", "min-w-0 flex-1")
            .Slot("title", "text-base font-medium text-(--klyroo-text-highlighted)")
            .Slot("description", "mt-1 text-sm text-pretty text-(--klyroo-text-muted)")
            .Slot("link", "after:absolute after:inset-0 hover:underline hover:underline-offset-4 focus-visible:outline-0")
            .Variant("orientation", "horizontal", "root", "flex-row")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("link", "true", "root", "has-[a:focus-visible]:outline-2 has-[a:focus-visible]:outline-offset-4 has-[a:focus-visible]:outline-(--kly-color-text)")
            .Default("orientation", "horizontal")
            .Default("link", "false");

    public static ComponentTheme PageGrid() =>
        new ComponentTheme("pageGrid", "root")
            .Slot("root", "grid grid-cols-1 gap-8")
            .Variant("columns", "2", "root", "sm:grid-cols-2")
            .Variant("columns", "3", "root", "sm:grid-cols-2 lg:grid-cols-3")
            .Variant("columns", "4", "root", "sm:grid-cols-2 lg:grid-cols-4")
            .Default("columns", "3");

    public static ComponentTheme PageColumns() =>
        new ComponentTheme("pageColumns", "root")
            .Slot("root", "columns-1 gap-8 *:mb-8 *:break-inside-avoid")
            .Variant("columns", "2", "root", "sm:columns-2")
            .Variant("columns", "3", "root", "sm:columns-2 lg:columns-3")
            .Variant("columns", "4", "root", "sm:columns-2 lg:columns-4")
            .Default("columns", "3");

    public static ComponentTheme PageCard() =>
        new ComponentTheme("pageCard", "root", "spotlight", "header", "container", "wrapper", "body", "leading", "leadingIcon", "title", "description", "footer", "link")
            .Slot("root", "group/card relative isolate flex min-w-0 flex-col overflow-hidden rounded-[calc(var(--klyroo-radius)*2)] transition-colors motion-reduce:transition-none")
            .Slot("spotlight", "pointer-events-none absolute inset-0 -z-10 opacity-0 transition-opacity duration-300 group-hover/card:opacity-100 motion-reduce:transition-none " +
                               "bg-[radial-gradient(40rem_circle_at_50%_0%,color-mix(in_oklab,var(--kly-color)_16%,transparent),transparent_60%)]")
            .Slot("header", "relative")
            .Slot("container", "flex flex-1 flex-col gap-4 p-4 sm:p-6")
            .Slot("wrapper", "min-w-0")
            .Slot("body", "min-w-0")
            .Slot("leading", "mb-3 inline-flex size-10 items-center justify-center rounded-(--klyroo-radius) bg-(--kly-color)/10 text-(--kly-color-text)")
            .Slot("leadingIcon", "size-5")
            .Slot("title", "text-base font-semibold text-(--klyroo-text-highlighted)")
            .Slot("description", "mt-1 text-sm text-pretty text-(--klyroo-text-muted)")
            // Not positioned, so the stretched link (positioned against the card) covers it; its buttons and links sit above the link.
            .Slot("footer", "px-4 pb-4 sm:px-6 sm:pb-6 [&_:is(a,button,input,select,textarea,summary)]:relative [&_:is(a,button,input,select,textarea,summary)]:z-10")
            .Slot("link", "after:absolute after:inset-0 after:z-0 focus-visible:outline-0")
            .Variant("variant", "outline", "root", "bg-(--klyroo-bg) ring ring-(--klyroo-border)")
            .Variant("variant", "soft", "root", "bg-(--klyroo-bg-elevated)/50")
            .Variant("variant", "subtle", "root", "bg-(--klyroo-bg-elevated)/50 ring ring-(--klyroo-border)")
            .Variant("variant", "spotlight", "root", "bg-(--klyroo-bg-elevated)/50 ring ring-(--klyroo-border) hover:ring-(--kly-color)/50")
            .Variant("orientation", "horizontal", "container", "lg:grid lg:grid-cols-2 lg:items-center lg:gap-8")
            .Variant("reverse", "true", "wrapper", "lg:order-last")
            .Variant("link", "true", "root", "hover:bg-(--klyroo-bg-elevated) has-[a:focus-visible]:outline-2 has-[a:focus-visible]:-outline-offset-2 has-[a:focus-visible]:outline-(--kly-color-text)")
            .Default("variant", "outline")
            .Default("orientation", "vertical")
            .Default("reverse", "false")
            .Default("link", "false");

    public static ComponentTheme PageCta() =>
        new ComponentTheme("pageCta", "root", "container", "wrapper", "title", "description", "links", "body")
            .Slot("root", "relative isolate mx-auto w-full max-w-(--breakpoint-xl) px-4 py-12 sm:px-6 lg:px-8")
            .Slot("container", "flex flex-col gap-8 rounded-[calc(var(--klyroo-radius)*3)] px-6 py-12 sm:px-12 sm:py-16")
            .Slot("wrapper", "min-w-0 max-w-2xl")
            .Slot("title", "text-3xl font-semibold tracking-tight text-pretty sm:text-4xl")
            .Slot("description", "mt-4 text-base text-pretty sm:text-lg")
            .Slot("links", "flex shrink-0 flex-wrap gap-3")
            .Slot("body", "min-w-0")
            .Variant("variant", "solid", "container", "bg-(--kly-color) ring ring-inset ring-(--kly-color-text) text-(--kly-color-contrast)")
            .Variant("variant", "solid", "title", "text-(--kly-color-contrast)")
            .Variant("variant", "solid", "description", "text-(--kly-color-contrast)")
            .Variant("variant", "outline", "container", "ring ring-inset ring-(--kly-color-text)/75")
            .Variant("variant", "outline", "title", "text-(--klyroo-text-highlighted)")
            .Variant("variant", "outline", "description", "text-(--klyroo-text-muted)")
            .Variant("variant", "soft", "container", "bg-(--kly-color)/10")
            .Variant("variant", "soft", "title", "text-(--klyroo-text-highlighted)")
            .Variant("variant", "soft", "description", "text-(--klyroo-text-muted)")
            .Variant("variant", "subtle", "container", "bg-(--kly-color)/10 ring ring-inset ring-(--kly-color-text)/75")
            .Variant("variant", "subtle", "title", "text-(--klyroo-text-highlighted)")
            .Variant("variant", "subtle", "description", "text-(--klyroo-text-muted)")
            .Variant("orientation", "horizontal", "container", "lg:flex-row lg:items-center lg:justify-between lg:gap-12")
            .Variant("orientation", "vertical", "container", "items-center text-center")
            .Variant("orientation", "vertical", "links", "justify-center")
            .Variant("reverse", "true", "container", "lg:flex-row-reverse")
            .Default("variant", "outline")
            .Default("orientation", "horizontal")
            .Default("reverse", "false");

    public static ComponentTheme PageLinks() =>
        new ComponentTheme("pageLinks", "root", "title", "list", "item", "link", "icon", "label", "externalIcon")
            .Slot("root", "flex flex-col gap-3")
            .Slot("title", "text-sm font-semibold text-(--klyroo-text-highlighted)")
            .Slot("list", "m-0 flex list-none flex-col gap-1.5 p-0")
            .Slot("item", "min-w-0")
            .Slot("link", "inline-flex min-w-0 items-center gap-1.5 rounded-sm text-sm text-(--klyroo-text-muted) transition-colors hover:text-(--klyroo-text) " +
                          "aria-[current=page]:font-medium aria-[current=page]:text-(--klyroo-primary-text) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("icon", "size-4 shrink-0")
            .Slot("label", "truncate")
            .Slot("externalIcon", "size-3 shrink-0");

    public static ComponentTheme PageList() =>
        new ComponentTheme("pageList", "root")
            .Slot("root", "relative m-0 flex list-none flex-col p-0")
            .Variant("divide", "true", "root", "divide-y divide-(--klyroo-border) *:py-4 [&>:first-child]:pt-0 [&>:last-child]:pb-0")
            .Variant("divide", "false", "root", "gap-8")
            .Default("divide", "true");

    public static ComponentTheme PageLogos() =>
        new ComponentTheme("pageLogos", "root", "title", "items", "item", "icon", "image", "marquee")
            .Slot("root", "relative flex flex-col gap-8")
            .Slot("title", "text-center text-lg font-medium text-pretty text-(--klyroo-text-highlighted)")
            .Slot("items", "m-0 flex list-none flex-wrap items-center justify-center gap-x-12 gap-y-6 p-0")
            .Slot("item", "inline-flex shrink-0 items-center text-(--klyroo-text-muted)")
            .Slot("icon", "size-10 shrink-0")
            .Slot("image", "h-10 w-auto max-w-40 shrink-0 object-contain")
            .Slot("marquee", "[--kly-gap:3rem]");

    public static ComponentTheme PageAnchors() =>
        new ComponentTheme("pageAnchors", "root", "list", "item", "link", "leading", "leadingIcon", "label", "externalIcon")
            .Slot("root", "")
            .Slot("list", "m-0 flex list-none flex-col gap-0.5 p-0")
            .Slot("item", "min-w-0")
            .Slot("link", "group flex min-w-0 items-center gap-2 rounded-(--klyroo-radius) px-2.5 py-1.5 text-sm font-medium text-(--klyroo-text-muted) transition-colors " +
                          "hover:bg-(--klyroo-bg-elevated) hover:text-(--klyroo-text) " +
                          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("leading", "inline-flex size-6 shrink-0 items-center justify-center rounded-sm bg-(--klyroo-bg-elevated) text-(--klyroo-text-muted) group-hover:text-(--klyroo-primary-text)")
            .Slot("leadingIcon", "size-4")
            .Slot("label", "truncate")
            .Slot("externalIcon", "size-3 shrink-0");

    public static ComponentTheme PageAside() =>
        new ComponentTheme("pageAside", "root", "top", "body", "bottom")
            .Slot("root", "sticky top-[calc(var(--kly-header-height,var(--klyroo-header-height,0px))_+_var(--kly-safe-top))] hidden max-h-[calc(100vh-var(--kly-header-height,var(--klyroo-header-height,0px))-var(--kly-safe-top))] overflow-y-auto py-8 lg:block")
            .Slot("top", "mb-4 border-b border-(--klyroo-border) pb-4")
            .Slot("body", "")
            .Slot("bottom", "mt-4 border-t border-(--klyroo-border) pt-4");

    public static ComponentTheme PageBody() =>
        new ComponentTheme("pageBody", "root")
            .Slot("root", "mt-12 pb-24 text-base/7 text-(--klyroo-text) " +
                          "[&_p]:my-5 [&_h2]:mt-12 [&_h2]:mb-4 [&_h2]:text-2xl [&_h2]:font-semibold [&_h2]:tracking-tight [&_h2]:text-(--klyroo-text-highlighted) " +
                          "[&_h3]:mt-8 [&_h3]:mb-3 [&_h3]:text-xl [&_h3]:font-semibold [&_h3]:text-(--klyroo-text-highlighted) " +
                          "[&_a]:font-medium [&_a]:text-(--klyroo-primary-text) [&_a]:underline [&_a]:underline-offset-4 " +
                          "[&_ul]:my-5 [&_ul]:list-disc [&_ul]:ps-6 [&_ol]:my-5 [&_ol]:list-decimal [&_ol]:ps-6 [&_li]:my-1.5 " +
                          "[&_code]:rounded-sm [&_code]:bg-(--klyroo-bg-elevated) [&_code]:px-1.5 [&_code]:py-0.5 [&_code]:font-mono [&_code]:text-sm " +
                          "[&_pre]:my-5 [&_pre]:overflow-x-auto [&_pre]:rounded-(--klyroo-radius) [&_pre]:bg-(--klyroo-bg-elevated) [&_pre]:p-4 " +
                          "[&_pre_code]:bg-transparent [&_pre_code]:p-0 " +
                          "[&_blockquote]:my-6 [&_blockquote]:border-s-2 [&_blockquote]:border-(--klyroo-border-accented) [&_blockquote]:ps-4 [&_blockquote]:text-(--klyroo-text-muted) " +
                          "[&_hr]:my-10 [&_hr]:border-(--klyroo-border) [&_img]:my-6 [&_img]:rounded-(--klyroo-radius)");
}
