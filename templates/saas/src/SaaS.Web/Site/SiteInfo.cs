using Blux.UI;

namespace SaaS.Web.Site;

// What the site says about itself, as typed C#: its name, the words of the home page, the links of the header and the footer. The pricing is in
// Pricing.cs, the documentation, the blog and the release notes are Markdown files in the content folder. Icon names (Icon = "ph:...") are found by the
// build, which bundles them: add one here and it is available, no other step.

/// <summary>A customer quote of the home page.</summary>
public sealed record Testimonial(string Quote, string Name, string Role);

/// <summary>A step of the "how it works" section.</summary>
public sealed record Step(string Title, string Text)
{
    public required string Icon { get; init; }
}

public static class SiteInfo
{
    /// <summary>The product the pages describe. Tideline and every name on this site are made up.</summary>
    public const string Name = "Tideline";

    public const string Tagline = "Know when a scheduled job stops running.";

    public const string Description =
        "Tideline watches your scheduled jobs. Each job pings it when it finishes, and Tideline alerts your team when a ping does not arrive.";

    /// <summary>Where the "Source" and "Contact" links lead. Placeholders on example.com, which is reserved for documentation: point them at your own.</summary>
    public const string ContactUrl = "mailto:hello@example.com";

    public const string StatusUrl = "https://example.com/status";

    /// <summary>
    /// The "Continue with" buttons of the login and sign-up pages. Demo placeholders: they carry "(demo)" in their label and lead to example.com, because
    /// the template has no authentication. Replace the addresses with the sign-in endpoints of your providers and drop "(demo)", see the README.
    /// </summary>
    public const string GitHubSignInUrl = "https://example.com/auth/github";

    public const string GoogleSignInUrl = "https://example.com/auth/google";

    // Paths are relative to the <base href> of the page, so they are right below a path base too.
    public const string HomeHref = "./";
    public const string SignUpHref = "signup";
    public const string LoginHref = "login";
    public const string ForgotPasswordHref = "forgot-password";
    public const string TermsHref = "legal/terms";
    public const string PrivacyHref = "legal/privacy";
    public const string PricingHref = "pricing";
    public const string DocsHref = "docs";
    public const string BlogHref = "blog";
    public const string ChangelogHref = "changelog";

    public static string Copyright => $"© {DateTime.UtcNow.Year} {Name}. Built with Blux UI.";

    // Layout

    public const string HeaderLabel = "Main";
    public const string HeaderMenuTitle = "Menu";
    public const string FooterLabel = "Footer";

    public static IReadOnlyList<BxNavigationItem> Navigation { get; } =
    [
        new("Features") { Href = "./#features", Active = false },
        new("Pricing") { Href = PricingHref },
        new("Docs") { Href = DocsHref },
        new("Blog") { Href = BlogHref },
        new("Changelog") { Href = ChangelogHref },
    ];

    public static IReadOnlyList<BxFooterColumn> FooterColumns { get; } =
    [
        new("Product")
        {
            Links =
            [
                new("Features") { Href = "./#features" },
                new("Pricing") { Href = PricingHref },
                new("Changelog") { Href = ChangelogHref },
            ],
        },
        new("Resources")
        {
            Links =
            [
                new("Documentation") { Href = DocsHref },
                new("Blog") { Href = BlogHref },
                new("Status") { Href = StatusUrl },
            ],
        },
        new("Legal")
        {
            Links =
            [
                new("Terms of service") { Href = TermsHref },
                new("Privacy policy") { Href = PrivacyHref },
            ],
        },
        new("Account")
        {
            Links =
            [
                new("Sign in") { Href = LoginHref },
                new("Create an account") { Href = SignUpHref },
                new("Contact") { Href = ContactUrl },
            ],
        },
    ];

    // Hero

    public const string HeroHeadline = "Heartbeat monitoring";
    public const string HeroTitle = "Know when a scheduled job stops running";
    public const string HeroDescription =
        "Backups, exports and reports fail without a sound. Tideline expects a ping from each of them, and tells your team the minute one does not arrive.";

    public static IReadOnlyList<PageLinkItem> HeroLinks { get; } =
    [
        new("Start free", SignUpHref) { TrailingIcon = "ph:arrow-right" },
        new("Read the docs", DocsHref) { Color = BxColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    // Features

    public const string FeaturesHeadline = "Features";
    public const string FeaturesTitle = "Everything a quiet failure needs to be noticed";
    public const string FeaturesDescription = "Set a monitor up in a minute, then forget it until it matters.";

    public static IReadOnlyList<PageFeatureItem> Features { get; } =
    [
        new("One URL per job", "A monitor is a URL. Call it from the end of a script, a CI step or a queue worker, with curl or any HTTP client.") { Icon = "ph:plugs-connected" },
        new("Schedules and grace periods", "Say how often a job runs, in cron syntax or in minutes, and how late it may be before it counts as missed.") { Icon = "ph:clock-countdown" },
        new("Alerts where you work", "Email, Slack, webhooks and PagerDuty, with a different channel for each monitor and a reminder until someone acknowledges it.") { Icon = "ph:bell-ringing" },
        new("Start and fail signals", "Report that a job started and that it failed, and Tideline tells a job that never ran from one that ran too long or crashed.") { Icon = "ph:heartbeat" },
        new("History you can read", "Every ping with its duration and exit code, kept for up to a year, so a slow drift shows before it becomes an outage.") { Icon = "ph:chart-line-up" },
        new("Status pages and teams", "Share a public status page, and give each teammate the role they need, with an audit log of who changed what.") { Icon = "ph:users-three" },
    ];

    // How it works

    public const string HowHeadline = "How it works";
    public const string HowTitle = "From a cron line to an alert in three steps";
    public const string HowDescription = "No agent to install and nothing to open in your firewall: your jobs call Tideline, never the other way round.";

    public static IReadOnlyList<Step> Steps { get; } =
    [
        new("Create a monitor", "Name it, give it a schedule and a grace period, and pick who is alerted. You get a ping URL.") { Icon = "ph:rocket-launch" },
        new("Add one line to the job", "Call the URL when the job ends. Add /start at the beginning to measure how long it takes.") { Icon = "ph:terminal-window" },
        new("Get told when it stops", "If no ping arrives in time, Tideline alerts the channels you chose and keeps a record of the incident.") { Icon = "ph:bell-ringing" },
    ];

    /// <summary>The Markdown of the snippet next to the steps: the same renderer as the documentation, so it is highlighted and has a copy button.</summary>
    public const string HowSnippet = """
        ```bash
        # crontab: every night at 02:00
        0 2 * * * /opt/backup/run.sh && curl -fsS -m 10 --retry 3 https://ping.example.com/p/nightly-backup
        ```
        """;

    // Testimonials. The people and the companies are made up.

    public const string TestimonialsHeadline = "Customers";
    public const string TestimonialsTitle = "What teams say after the first missed job";

    public static IReadOnlyList<Testimonial> Testimonials { get; } =
    [
        new("Our nightly export had been failing for nine days before a customer told us. Tideline would have told us in the first ten minutes.", "Ingrid Solberg", "Head of platform, Northgate Freight"),
        new("We moved forty cron lines from a shared inbox that nobody read to Tideline in one afternoon. The grace periods are what made it quiet enough to trust.", "Rahul Menon", "Staff engineer, Pinecrest Analytics"),
        new("The status page is what sold our support team. They stopped asking engineering whether the reports had gone out.", "Lena Okoye", "Operations lead, Halden Health"),
    ];

    // Latest posts

    public const string PostsHeadline = "Blog";
    public const string PostsTitle = "Notes from the people who build it";

    // Call to action

    public const string CtaTitle = "Watch your first job in five minutes";
    public const string CtaDescription = "The free plan has five monitors and needs no payment details.";

    public static IReadOnlyList<PageLinkItem> CtaLinks { get; } =
    [
        new("Create an account", SignUpHref),
        new("Talk to us", ContactUrl) { Color = BxColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    // Documentation home

    public const string DocsHeadline = "Documentation";
    public const string DocsTitle = "Set up monitoring in an afternoon";
    public const string DocsDescription = "Guides for the first monitor, the alert channels and the team, and the reference of the ping API and the command line tool.";

    // Blog

    public const string BlogTitle = "Blog";
    public const string BlogDescription = "Product news, and what we learn from running a service whose job is to notice silence.";

    // Changelog

    public const string ChangelogTitle = "Changelog";
    public const string ChangelogDescription = "What changed in Tideline, newest first.";
}
