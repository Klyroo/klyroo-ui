namespace Blux.UI.Theming;

// Themes of the "Marketing" family: AuthForm, PricingPlan, PricingPlans, PricingTable, BlogPost, BlogPosts,
// ChangelogVersion and ChangelogVersions.
internal static partial class Themes
{
    private const string StretchedLinkFocus =
        "has-[[data-slot=link]:focus-visible]:outline-2 has-[[data-slot=link]:focus-visible]:outline-offset-2 " +
        "has-[[data-slot=link]:focus-visible]:outline-(--blux-primary-text)";

    public static ComponentTheme AuthForm() =>
        new ComponentTheme("authForm", "root", "header", "leading", "title", "description", "providers", "form", "fieldset", "error",
                           "footer", "passwordRoot", "passwordInput", "passwordButton", "passwordShow", "passwordHide")
            .Slot("root", "flex w-full max-w-sm flex-col gap-6")
            .Slot("header", "flex flex-col items-center gap-2 text-center")
            .Slot("leading", "inline-flex size-10 items-center justify-center rounded-full bg-(--bx-color)/10 text-(--bx-color-text) [&_svg]:size-5")
            .Slot("title", "text-xl font-semibold text-pretty text-(--blux-text-highlighted)")
            .Slot("description", "text-sm text-pretty text-(--blux-text-muted)")
            .Slot("providers", "flex flex-col gap-3")
            .Slot("form", "flex flex-col gap-5")
            .Slot("fieldset", "m-0 flex min-w-0 flex-col gap-5 border-0 p-0")
            .Slot("error", "")
            .Slot("footer", "text-center text-sm text-pretty text-(--blux-text-muted)")
            .Slot("passwordRoot", "relative block")
            .Slot("passwordInput", "pe-11")
            .Slot("passwordButton", "group absolute inset-y-0 end-0 flex cursor-pointer items-center rounded-e-(--blux-radius) px-3 text-(--blux-text-muted) " +
                                    "hover:text-(--blux-text) focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-(--bx-color-text) " +
                                    "disabled:cursor-not-allowed disabled:opacity-60")
            .Slot("passwordShow", "size-5 group-aria-pressed:hidden")
            .Slot("passwordHide", "hidden size-5 group-aria-pressed:block");

    public static ComponentTheme PricingPlan() =>
        new ComponentTheme("pricingPlan", "root", "intro", "header", "titleRow", "title", "badge", "description", "tagline", "pricing", "priceRow",
                           "price", "discount", "billingCycle", "terms", "features", "feature", "featureIcon", "footer", "srOnly")
            .Slot("root", "relative flex flex-col gap-6 rounded-[calc(var(--blux-radius)*2)] p-6 sm:p-8")
            .Slot("intro", "flex flex-col gap-6")
            .Slot("header", "flex flex-col gap-2")
            .Slot("titleRow", "flex flex-wrap items-center gap-2")
            .Slot("title", "text-xl font-semibold text-(--blux-text-highlighted)")
            .Slot("badge", "")
            .Slot("description", "text-sm text-pretty text-(--blux-text-muted)")
            .Slot("tagline", "text-sm font-medium text-(--bx-color-text)")
            .Slot("pricing", "flex flex-col gap-1")
            .Slot("priceRow", "flex flex-wrap items-baseline gap-x-2")
            .Slot("price", "text-4xl font-semibold tracking-tight text-(--blux-text-highlighted) tabular-nums")
            .Slot("discount", "text-xl text-(--blux-text-muted) line-through tabular-nums")
            .Slot("billingCycle", "text-sm text-(--blux-text-muted)")
            .Slot("terms", "text-xs text-pretty text-(--blux-text-muted)")
            .Slot("features", "m-0 flex list-none flex-col gap-2 p-0 text-sm text-(--blux-text) lg:flex-1")
            .Slot("feature", "flex items-start gap-2")
            .Slot("featureIcon", "mt-0.5 size-4 shrink-0 text-(--bx-color-text)")
            .Slot("footer", "mt-auto")
            .Slot("srOnly", "sr-only")
            .Variant("variant", "outline", "root", "bg-(--blux-bg) ring ring-inset ring-(--blux-border)")
            .Variant("variant", "soft", "root", "bg-(--blux-bg-elevated)/50")
            .Variant("variant", "subtle", "root", "bg-(--blux-bg-elevated)/50 ring ring-inset ring-(--blux-border)")
            .Variant("variant", "solid", "root", "bg-(--bx-color) ring ring-inset ring-(--bx-color-text) text-(--bx-color-contrast)")
            .Variant("variant", "solid", "title", "text-current")
            .Variant("variant", "solid", "description", "text-current")
            .Variant("variant", "solid", "tagline", "text-current")
            .Variant("variant", "solid", "price", "text-current")
            .Variant("variant", "solid", "discount", "text-current")
            .Variant("variant", "solid", "billingCycle", "text-current")
            .Variant("variant", "solid", "terms", "text-current")
            .Variant("variant", "solid", "features", "text-current")
            .Variant("variant", "solid", "featureIcon", "text-current")
            .Variant("highlight", "true", "root", "ring-2 ring-inset ring-(--bx-color-text)")
            .Compound(new() { ["highlight"] = "true", ["variant"] = "solid" }, "root", "ring-(--blux-text-highlighted)")
            .Variant("scale", "true", "root", "lg:z-10 lg:scale-105")
            .Variant("orientation", "horizontal", "root", "lg:flex-row lg:items-center lg:gap-10")
            .Variant("orientation", "horizontal", "intro", "lg:w-1/3 lg:shrink-0")
            .Variant("orientation", "horizontal", "footer", "lg:mt-0 lg:w-48 lg:shrink-0")
            .Default("variant", "outline")
            .Default("highlight", "false")
            .Default("scale", "false")
            .Default("orientation", "vertical");

    public static ComponentTheme PricingPlans() =>
        new ComponentTheme("pricingPlans", "root")
            .Slot("root", "w-full gap-6")
            .Variant("orientation", "horizontal", "root", "grid grid-cols-[repeat(auto-fit,minmax(min(100%,17rem),1fr))] items-stretch")
            .Variant("orientation", "vertical", "root", "flex flex-col")
            .Default("orientation", "horizontal");

    public static ComponentTheme PricingTable() =>
        new ComponentTheme("pricingTable", "root", "table", "caption", "thead", "headRow", "corner", "tier", "tierTitle", "tierBadge", "tierDescription",
                           "tierPrice", "tierDiscount", "tierCycle", "tierButton", "tbody", "sectionRow", "section", "row", "feature", "cell",
                           "valueIcon", "included", "notIncluded", "srOnly")
            .Slot("root", "w-full")
            .Slot("table", "block w-full text-start md:table md:border-separate md:border-spacing-0")
            .Slot("caption", "sr-only")
            .Slot("thead", "block md:table-header-group")
            .Slot("headRow", "block md:table-row")
            .Slot("corner", "hidden md:table-cell md:w-1/3 md:border-b md:border-(--blux-border) md:bg-(--blux-bg)")
            .Slot("tier", "mb-4 block rounded-[calc(var(--blux-radius)*2)] border border-(--blux-border) bg-(--blux-bg) p-4 text-start align-top font-normal " +
                          "data-highlight:border-(--bx-color-text) md:mb-0 md:table-cell md:rounded-none md:border-0 md:border-b md:px-4 md:py-5 " +
                          "md:data-highlight:border-b-2 md:data-highlight:border-(--bx-color-text)")
            .Slot("tierTitle", "block text-base font-semibold text-(--blux-text-highlighted)")
            .Slot("tierBadge", "mt-1")
            .Slot("tierDescription", "mt-1 block text-sm font-normal text-(--blux-text-muted)")
            .Slot("tierPrice", "mt-3 flex flex-wrap items-baseline gap-x-1.5 text-2xl font-semibold text-(--blux-text-highlighted) tabular-nums")
            .Slot("tierDiscount", "text-base font-normal text-(--blux-text-muted) line-through")
            .Slot("tierCycle", "text-sm font-normal text-(--blux-text-muted)")
            .Slot("tierButton", "mt-4")
            .Slot("tbody", "block md:table-row-group")
            .Slot("sectionRow", "block md:table-row")
            .Slot("section", "block pt-8 pb-2 text-start text-sm font-semibold text-(--blux-text-highlighted) md:table-cell md:border-b md:border-(--blux-border) md:px-4")
            .Slot("row", "block border-b border-(--blux-border) py-3 md:table-row md:border-0 md:py-0")
            .Slot("feature", "block pb-2 text-start text-sm font-medium text-(--blux-text) md:table-cell md:border-b md:border-(--blux-border) md:px-4 md:py-3 md:font-normal")
            .Slot("cell", "flex items-center justify-between gap-4 py-1 text-sm text-(--blux-text) before:font-medium before:text-(--blux-text-muted) " +
                          "before:content-[attr(data-label)] md:table-cell md:border-b md:border-(--blux-border) md:px-4 md:py-3 md:before:content-none")
            .Slot("valueIcon", "size-5")
            .Slot("included", "text-(--blux-success-text)")
            .Slot("notIncluded", "text-(--blux-text-dimmed)")
            .Slot("srOnly", "sr-only")
            .Variant("sticky", "true", "tier", "md:sticky md:top-[var(--bx-sticky-offset,0px)] md:z-10")
            .Variant("sticky", "true", "corner", "md:sticky md:top-[var(--bx-sticky-offset,0px)] md:z-10")
            .Default("sticky", "true");

    public static ComponentTheme BlogPost() =>
        new ComponentTheme("blogPost", "root", "header", "image", "body", "meta", "date", "badge", "title", "link", "description", "authors", "footer")
            .Slot("root", "group relative flex min-w-0 flex-col overflow-hidden rounded-[calc(var(--blux-radius)*2)] " + StretchedLinkFocus)
            .Slot("header", "aspect-video shrink-0 overflow-hidden bg-(--blux-bg-elevated)")
            .Slot("image", "size-full object-cover")
            .Slot("body", "flex min-w-0 flex-1 flex-col gap-2")
            .Slot("meta", "flex flex-wrap items-center gap-x-3 gap-y-1")
            .Slot("date", "text-sm text-(--blux-text-muted)")
            .Slot("badge", "")
            .Slot("title", "m-0 text-lg font-semibold text-pretty text-(--blux-text-highlighted)")
            .Slot("link", "after:absolute after:inset-0 focus:outline-none")
            .Slot("description", "text-sm text-pretty text-(--blux-text-muted)")
            .Slot("authors", "mt-auto flex items-center gap-3 pt-2")
            .Slot("footer", "")
            .Variant("variant", "outline", "root", "bg-(--blux-bg) ring ring-inset ring-(--blux-border)")
            .Variant("variant", "soft", "root", "bg-(--blux-bg-elevated)/50")
            .Variant("variant", "subtle", "root", "bg-(--blux-bg-elevated)/50 ring ring-inset ring-(--blux-border)")
            .Variant("variant", "ghost", "root", "bg-transparent")
            .Variant("variant", "naked", "root", "overflow-visible bg-transparent")
            .Variant("variant", "outline", "body", "p-4 sm:p-6")
            .Variant("variant", "soft", "body", "p-4 sm:p-6")
            .Variant("variant", "subtle", "body", "p-4 sm:p-6")
            .Variant("variant", "ghost", "body", "p-4 sm:p-6")
            .Variant("variant", "naked", "body", "pt-4 md:ps-6 md:pt-0")
            .Variant("variant", "naked", "header", "rounded-[calc(var(--blux-radius)*2)]")
            .Variant("link", "true", "root", "transition-colors hover:bg-(--blux-bg-elevated)/70")
            .Variant("orientation", "vertical", "root", "flex-col")
            .Variant("orientation", "horizontal", "root", "md:flex-row")
            .Variant("orientation", "horizontal", "header", "md:aspect-auto md:w-2/5")
            .Variant("orientation", "horizontal", "body", "md:justify-center")
            .Default("variant", "outline")
            .Default("link", "false")
            .Default("orientation", "vertical");

    public static ComponentTheme BlogPosts() =>
        new ComponentTheme("blogPosts", "root")
            .Slot("root", "w-full gap-6")
            .Variant("orientation", "horizontal", "root", "grid grid-cols-[repeat(auto-fill,minmax(min(100%,18rem),1fr))]")
            .Variant("orientation", "vertical", "root", "flex flex-col")
            .Default("orientation", "horizontal");

    public static ComponentTheme ChangelogVersion() =>
        new ComponentTheme("changelogVersion", "root", "indicator", "container", "header", "meta", "date", "badge", "title", "link", "description",
                           "image", "imageElement", "body", "authors", "actions", "footer")
            .Slot("root", "relative min-w-0")
            .Slot("indicator", "absolute start-0 top-1.5 size-4 rounded-full border-2 border-(--bx-color-text) bg-(--blux-bg)")
            .Slot("container", "flex min-w-0 flex-col gap-3")
            .Slot("header", "flex flex-col gap-1")
            .Slot("meta", "flex flex-wrap items-center gap-x-3 gap-y-1")
            .Slot("date", "text-sm text-(--blux-text-muted)")
            .Slot("badge", "")
            .Slot("title", "m-0 text-xl font-semibold text-pretty text-(--blux-text-highlighted)")
            .Slot("link", "rounded-sm hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--blux-primary-text)")
            .Slot("description", "text-pretty text-(--blux-text-muted)")
            .Slot("image", "overflow-hidden rounded-(--blux-radius) ring ring-inset ring-(--blux-border)")
            .Slot("imageElement", "w-full")
            .Slot("body", "text-sm text-pretty text-(--blux-text) [&_a]:underline [&_li]:mt-1 [&_p+p]:mt-3 [&_ul]:list-disc [&_ul]:ps-5")
            .Slot("authors", "flex flex-wrap items-center gap-4")
            .Slot("actions", "flex flex-wrap items-center gap-2")
            .Slot("footer", "")
            .Variant("indicator", "true", "root", "pb-12 ps-8 before:absolute before:start-[7px] before:top-7 before:bottom-0 before:w-px before:bg-(--blux-border) last:pb-0 last:before:hidden")
            .Default("indicator", "true");

    public static ComponentTheme ChangelogVersions() =>
        new ComponentTheme("changelogVersions", "root")
            .Slot("root", "relative flex w-full flex-col");
}
