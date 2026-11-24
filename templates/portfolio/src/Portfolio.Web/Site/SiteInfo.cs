using Lux.UI;

namespace Portfolio.Web.Site;

// What the site says about its owner, as typed C#: the name, the links of the header and the footer, the words of the home page and of the pages that
// are laid out in Razor. The projects, the articles, the text about you and the "uses" page are Markdown files in the content folder; the experience and
// the talks are in Resume.cs and Talks.cs. Icon names (Icon = "ph:...") are found by the build, which bundles them: add one here and it is available.

/// <summary>A link to an account of yours: GitHub, LinkedIn, email.</summary>
public sealed record SocialLink(string Label, string Href)
{
    /// <summary>Written as <c>Icon = "ph:..."</c> so that the build finds the name and bundles the icon.</summary>
    public required string Icon { get; init; }
}

public static class SiteInfo
{
    /// <summary>The person the pages describe. Ines Calloway, her employers, clients, projects and talks are made up.</summary>
    public const string Name = "Ines Calloway";

    public const string Role = "Design engineer";

    public const string Location = "Lisbon, Portugal";

    public const string Tagline = "Design engineer in Lisbon. Interfaces that are quick to learn and hard to break.";

    public const string Description =
        "Ines Calloway is a design engineer in Lisbon. She designs and builds interfaces and design systems for product teams, with a focus on accessibility and on code that stays easy to change.";

    /// <summary>Where the contact links lead. Placeholders on example.com, which is reserved for documentation: point them at your own accounts.</summary>
    public const string Email = "hello@example.com";

    public const string GitHubUrl = "https://example.com/ines/github";
    public const string LinkedInUrl = "https://example.com/ines/linkedin";
    public const string MastodonUrl = "https://example.com/ines/mastodon";

    // Paths are relative to the <base href> of the page, so they are right below a path base too.
    public const string HomeHref = "./";
    public const string WorkHref = "work";
    public const string AboutHref = "about";
    public const string WritingHref = "writing";
    public const string SpeakingHref = "speaking";
    public const string ContactHref = "contact";
    public const string UsesHref = "uses";

    public static string Copyright => $"© {DateTime.UtcNow.Year} {Name}. Built with Lux.UI.";

    // Layout

    public const string HeaderLabel = "Main";
    public const string HeaderMenuTitle = "Menu";
    public const string FooterLabel = "Footer";

    public static IReadOnlyList<LxNavigationItem> Navigation { get; } =
    [
        new("Work") { Href = WorkHref },
        new("About") { Href = AboutHref },
        new("Writing") { Href = WritingHref },
        new("Speaking") { Href = SpeakingHref },
    ];

    public static IReadOnlyList<SocialLink> Social { get; } =
    [
        new("GitHub", GitHubUrl) { Icon = "ph:github-logo" },
        new("LinkedIn", LinkedInUrl) { Icon = "ph:linkedin-logo" },
        new("Mastodon", MastodonUrl) { Icon = "ph:mastodon-logo" },
        new("Email", "mailto:" + Email) { Icon = "ph:envelope-simple" },
    ];

    public static IReadOnlyList<LxFooterColumn> FooterColumns { get; } =
    [
        new("Site")
        {
            Links =
            [
                new("Work") { Href = WorkHref },
                new("About") { Href = AboutHref },
                new("Writing") { Href = WritingHref },
                new("Speaking") { Href = SpeakingHref },
            ],
        },
        new("More")
        {
            Links =
            [
                new("Uses") { Href = UsesHref },
                new("Contact") { Href = ContactHref },
            ],
        },
    ];

    // Hero

    public const string HeroHeadline = "Design engineer in Lisbon";
    public const string HeroTitle = "Interfaces that are quick to learn and hard to break";
    public const string HeroDescription =
        "I am Ines. I design and build interfaces and design systems for product teams, and I care most about the parts nobody screenshots: keyboard paths, empty states and the second year of a codebase.";

    public static IReadOnlyList<PageLinkItem> HeroLinks { get; } =
    [
        new("See my work", WorkHref) { TrailingIcon = "ph:arrow-right" },
        new("Get in touch", ContactHref) { Color = LxColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    /// <summary>The facts in the card next to the introduction.</summary>
    public static IReadOnlyList<(string Label, string Value)> Now { get; } =
    [
        ("Status", "Booking projects from February 2027"),
        ("Based in", "Lisbon, working in UTC and Central European time"),
        ("Works with", "Product teams of 5 to 50 people, in design or in code"),
        ("Replies", "Within two working days"),
    ];

    // Home: work

    public const string WorkHeadline = "Selected work";
    public const string WorkTitle = "Recent projects";
    public const string WorkDescription = "Five projects from the last four years, from a public data site to the design system of a bank.";

    // Home: what I do

    public const string SkillsHeadline = "What I do";
    public const string SkillsTitle = "The work between design and code";

    public static IReadOnlyList<PageFeatureItem> Skills { get; } =
    [
        new("Interface engineering", "Screens that behave well on a slow phone, with a keyboard and with a screen reader, built in TypeScript or in .NET.") { Icon = "ph:browsers" },
        new("Design systems", "Tokens, components and the documentation that makes a team use them, starting from an audit of what already ships.") { Icon = "ph:stack" },
        new("Accessibility", "Audits that end in fixed code, and the habits that keep a product accessible after I leave: contrast checks, focus rules and tests.") { Icon = "ph:person-arms-spread" },
        new("Prototyping", "Working prototypes in a week, made to be thrown away or to become the first version, whichever the test says.") { Icon = "ph:flask" },
    ];

    // Home: writing

    public const string WritingHeadline = "Writing";
    public const string WritingTitle = "Notes from the work";

    // Home: call to action

    public const string CtaTitle = "Have something in mind?";
    public const string CtaDescription = "Tell me what you are building and when it has to ship. I answer every message within two working days.";

    public static IReadOnlyList<PageLinkItem> CtaLinks { get; } =
    [
        new("Send a message", ContactHref),
        new("Write directly", "mailto:" + Email) { Color = LxColor.Neutral, Variant = ButtonVariant.Outline, Icon = "ph:envelope-simple" },
    ];

    // Work

    public const string WorkPageTitle = "Work";
    public const string WorkPageDescription = "Projects I designed and built, with the problem, what I did and what came out of it.";

    // About

    public const string AboutTitle = "About";
    public const string AboutDescription = "Who I am, where I have worked and what I use.";

    // Writing

    public const string WritingPageTitle = "Writing";
    public const string WritingPageDescription = "Articles about interface engineering, design systems and working on my own.";

    // Speaking

    public const string SpeakingTitle = "Speaking";
    public const string SpeakingDescription = "Talks and workshops I have given, with the slides and recordings where there are some.";

    public const string SpeakingInvite = "I give talks and workshops about accessible interfaces and design systems. Write to me with the date, the audience and the format, and I will say whether it fits.";

    // Contact

    public const string ContactTitle = "Contact";
    public const string ContactDescription = "Tell me about the project, the talk or the question. I answer within two working days.";

    /// <summary>
    /// The choices of the topic of the contact form: radio buttons, which the browser posts by itself. The form has to work with JavaScript off, and
    /// the scripted menus of Lux (<c>LxSelect</c>) post through a script.
    /// </summary>
    public static IReadOnlyList<LxRadioItem<string>> Topics { get; } =
    [
        new("project", "A project"),
        new("speaking", "A talk or a workshop"),
        new("other", "Something else"),
    ];
}
