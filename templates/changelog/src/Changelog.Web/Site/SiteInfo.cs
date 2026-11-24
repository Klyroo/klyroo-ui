namespace Changelog.Web.Site;

// What the site says about itself, as typed C#. The releases are not here: they come from GitHub (or from data/releases.json), see ReleaseNotes/.

public static class SiteInfo
{
    /// <summary>The product the releases belong to. Tern and every name in the sample releases are made up.</summary>
    public const string Name = "Tern";

    public const string Title = "Release notes";

    public const string FeedTitle = "Tern releases";

    public const string Description = "What changed in each release of Tern, a command line tool that keeps a folder in step with remote storage.";

    public static string Copyright => $"© {DateTime.UtcNow.Year} {Name}. Built with Lux.UI.";
}
