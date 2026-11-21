using System.Text.RegularExpressions;

namespace Changelog.Web.ReleaseNotes;

/// <summary>Where the releases come from.</summary>
public enum ChangelogSource
{
    /// <summary>The releases of a repository, read from the GitHub REST API while the site runs.</summary>
    GitHub,

    /// <summary>A JSON file in the format of the GitHub API (the sample releases of this template, or your own): no network.</summary>
    File,
}

/// <summary>
/// The settings under <c>Changelog</c> (<c>appsettings.json</c>, or environment variables: <c>Changelog__Repository=owner/repo</c>).
/// </summary>
public sealed partial class ChangelogOptions
{
    public const string Section = "Changelog";

    /// <summary>
    /// <see cref="ChangelogSource.GitHub"/> or <see cref="ChangelogSource.File"/>. Optional: when it is not set, the sample file is used while
    /// <see cref="Repository"/> is empty and GitHub as soon as a repository is set. A value that is set always wins.
    /// </summary>
    public ChangelogSource? Source { get; set; }

    /// <summary>The source in use: <see cref="Source"/>, or the one the repository setting implies.</summary>
    public ChangelogSource EffectiveSource => Source ?? (string.IsNullOrWhiteSpace(Repository) ? ChangelogSource.File : ChangelogSource.GitHub);

    /// <summary>True when the pages show the sample releases because there is no repository to show: they say so.</summary>
    public bool IsSample => EffectiveSource == ChangelogSource.File && !IsRepository(Repository);

    /// <summary>The repository whose releases are shown, <c>owner/repo</c>. Required for the GitHub source; with the file source it only decides where the links to GitHub point.</summary>
    public string? Repository { get; set; }

    /// <summary>
    /// A GitHub token, optional: without one the API allows 60 requests an hour per address, with one 5,000. A fine-grained token with no permission at all
    /// is enough for a public repository. It is only sent to the API, as a header; it is never logged and never part of a page. Keep it out of
    /// <c>appsettings.json</c>: use <c>Changelog__GitHubToken</c> or <c>dotnet user-secrets</c>.
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>The file of the file source, relative to the folder of the site. It holds the array the API returns for <c>GET /repos/{owner}/{repo}/releases</c>.</summary>
    public string File { get; set; } = "data/releases.json";

    /// <summary>The address of the GitHub API. Change it for GitHub Enterprise Server (<c>https://github.example.com/api/v3/</c>); the token goes to this host only.</summary>
    public string ApiUrl { get; set; } = "https://api.github.com/";

    /// <summary>How long the releases are used before they are asked for again. One hour by default; the site keeps showing the last copy while it refreshes.</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How long one request to GitHub may take.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Releases per page of the list.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>How many releases are fetched and kept (the newest ones). Each request asks for up to 100.</summary>
    public int MaxReleases { get; set; } = 100;

    /// <summary>The address of the site, with its path base (<c>https://example.com/</c>), for the links of the feed. Without it they are built from the request: set it behind a cache or a proxy.</summary>
    public string? SiteUrl { get; set; }

    [GeneratedRegex(@"^(?!\.{1,2}/)[A-Za-z0-9_.\-]{1,100}/(?!\.{1,2}\z)[A-Za-z0-9_.\-]{1,100}\z")]
    private static partial Regex RepositoryPattern();

    /// <summary>True for <c>owner/repo</c> made of the characters GitHub allows. The value goes into an address, so nothing else is accepted.</summary>
    public static bool IsRepository(string? value) => value is not null && RepositoryPattern().IsMatch(value);

    /// <summary>What is wrong with the settings, or null. A message names the setting, never a value that could be a secret.</summary>
    public string? Problem()
    {
        if (EffectiveSource == ChangelogSource.GitHub)
        {
            if (string.IsNullOrWhiteSpace(Repository))
            {
                return "Changelog:Repository is not set. Set it to owner/repo (for example Changelog__Repository=dotnet/aspnetcore), or use Changelog:Source=File.";
            }
            if (!IsRepository(Repository))
            {
                return "Changelog:Repository must look like owner/repo: letters, digits, dots, hyphens and underscores.";
            }
        }
        else if (!string.IsNullOrWhiteSpace(Repository) && !IsRepository(Repository))
        {
            return "Changelog:Repository must look like owner/repo: letters, digits, dots, hyphens and underscores.";
        }
        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out var api) || api.Scheme != Uri.UriSchemeHttps)
        {
            return "Changelog:ApiUrl must be an https address.";
        }
        if (CacheDuration < TimeSpan.Zero || RequestTimeout <= TimeSpan.Zero || PageSize < 1 || MaxReleases < 1)
        {
            return "Changelog:CacheDuration, RequestTimeout, PageSize and MaxReleases must be positive.";
        }
        return null;
    }

    /// <summary>The page of the repository on GitHub (<c>https://github.com/owner/repo</c>), or null when no valid repository is set.</summary>
    public string? RepositoryUrl => IsRepository(Repository) ? $"https://github.com/{Repository}" : null;
}
