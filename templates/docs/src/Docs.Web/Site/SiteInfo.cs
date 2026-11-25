using Klyroo.UI;

namespace Docs.Web.Site;

// What the site says about itself: its name, the words of the home page and the links of the footer, as typed C#. The pages of the documentation
// are the Markdown files of the content folder, not this file. Icon names (Icon = "ph:...") are found by the build, which bundles them.

public static class SiteInfo
{
    /// <summary>The product the pages describe. Tern and every name in the sample pages are made up.</summary>
    public const string Name = "Tern";

    public const string Tagline = "Keep a folder in step with remote storage.";

    public const string Description = "The documentation of Tern, a command line tool that copies a folder to remote storage and back, and tells you what it changed.";

    /// <summary>Where the "Source" link of the footer leads. A placeholder on example.com: point it at your repository.</summary>
    public const string SourceUrl = "https://example.com/tern";

    public static string Copyright => $"© {DateTime.UtcNow.Year} {Name}. Built with Klyroo.UI.";

    // Home page

    public const string HeroHeadline = "Documentation";
    public const string HeroTitle = "Keep a folder in step with remote storage";
    public const string HeroDescription =
        "Tern copies what changed between a local folder and a bucket, in either direction, and shows you the plan before it touches anything. These pages explain how to install it, set it up and run it on a schedule.";

    public static IReadOnlyList<PageLinkItem> HeroLinks { get; } =
    [
        new("Get started", "getting-started/installation") { TrailingIcon = "ph:arrow-right" },
        new("Command reference", "reference/commands") { Color = KlyColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    /// <summary>The Markdown of the snippet next to the hero: the same renderer as the pages, so it is highlighted and has a copy button too.</summary>
    public const string HeroSnippet = """
        ```bash
        tern init ./photos s3://my-bucket/photos
        tern sync --dry-run
        tern sync
        ```
        """;

    public const string FeaturesHeadline = "What it does";
    public const string FeaturesTitle = "A small tool with one job";
    public const string FeaturesDescription = "Everything below is in the guides. None of it needs a server of your own.";

    public static IReadOnlyList<PageFeatureItem> Features { get; } =
    [
        new("Plan first", "tern sync --dry-run lists every file it would upload, download or delete, and changes nothing.") { Icon = "ph:list-checks" },
        new("Only what changed", "Tern compares sizes and checksums, so a large folder with a few edits sends a few files.") { Icon = "ph:arrows-clockwise" },
        new("Ignore rules", "A .ternignore file keeps build output, caches and secrets out of the bucket, with the syntax you know from Git.") { Icon = "ph:eye-slash" },
        new("Runs on a schedule", "Run it from cron, a systemd timer or a scheduled task. Exit codes tell the scheduler what happened.") { Icon = "ph:clock" },
        new("One configuration file", "Remote, retries and rules live in tern.yml next to the folder, or in your home directory for all of them.") { Icon = "ph:file-text" },
        new("Scripts can read it", "Every command can print JSON, so a script can read the result without parsing text.") { Icon = "ph:brackets-curly" },
    ];

    public const string CtaTitle = "Start with the installation guide";
    public const string CtaDescription = "It takes about five minutes, and the next page shows your first synchronization.";

    public static IReadOnlyList<PageLinkItem> CtaLinks { get; } =
    [
        new("Install Tern", "getting-started/installation") { TrailingIcon = "ph:arrow-right" },
        new("Read the guides", "guides/configuration") { Color = KlyColor.Neutral, Variant = ButtonVariant.Outline },
    ];
}
