using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Marketing =
    [
        new("auth-form", "AuthForm", "Page", "A sign-in or sign-up form built from a description of its fields, with OAuth provider buttons, validation and a loading state. It posts in static rendering too.",
            [new("Sign in", typeof(Examples.AuthFormSignInExample), "Fields are described by BxAuthField records. Required fields, email format and a custom Validate callback run on submit; the password has a show/hide button that works without .NET."),
             new("Sign up", typeof(Examples.AuthFormSignUpExample), "Checkbox and select fields, a help text and a different color."),
             new("One-time code", typeof(Examples.AuthFormVerifyExample), "An otp field is a BxPinInput. Server-side messages come back through FieldErrors. Enhance updates the page without a reload.")],
            [typeof(BxAuthForm)]),
        new("pricing-plan", "PricingPlan", "Page", "A card for one plan: title, badge, price with an optional discount, feature list and a call to action.",
            [new("Discount, solid and horizontal", typeof(Examples.PricingPlanExample), "A discount shows the original price struck through, and spells both prices out for screen readers.")],
            [typeof(BxPricingPlan)]),
        new("pricing-plans", "PricingPlans", "Page", "A responsive group of plans, side by side or stacked.",
            [new("Billing cycle switch", typeof(Examples.PricingPlansExample), "The prices are .NET state: the switch is disabled until the page is interactive."),
             new("Stacked", typeof(Examples.PricingPlansVerticalExample))],
            [typeof(BxPricingPlans)]),
        new("pricing-table", "PricingTable", "Page", "A feature comparison across tiers: sticky header, check and minus icons with hidden text, tiers stacked on small screens.",
            [new("Three tiers", typeof(Examples.PricingTableExample), "Cells are a boolean (check or minus), or text. Below the md breakpoint each tier becomes a card and every row names its tier.")],
            [typeof(BxPricingTable)]),
        new("blog-post", "BlogPost", "Page", "A post card with a cover image, a culture formatted date, a badge and authors. The title is the only link and covers the card.",
            [new("Orientation and variants", typeof(Examples.BlogPostExample), "Without Href the card has no link, so its content can hold links of its own.")],
            [typeof(BxBlogPost)]),
        new("blog-posts", "BlogPosts", "Page", "A responsive grid or a stacked list of blog posts.",
            [new("Grid", typeof(Examples.BlogPostsExample), "One author shows a user, several show an avatar group named after them.")],
            [typeof(BxBlogPosts)]),
        new("changelog-version", "ChangelogVersion", "Page", "One release: version, date, badge, rich description and authors, with a timeline indicator.",
            [new("Versions", typeof(Examples.ChangelogVersionsExample))],
            [typeof(BxChangelogVersion)]),
        new("changelog-versions", "ChangelogVersions", "Page", "The timeline that joins changelog versions with a line.",
            [new("Timeline", typeof(Examples.ChangelogVersionsExample))],
            [typeof(BxChangelogVersions)]),
    ];
}
