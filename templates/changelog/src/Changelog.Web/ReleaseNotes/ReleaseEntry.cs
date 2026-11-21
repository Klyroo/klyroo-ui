namespace Changelog.Web.ReleaseNotes;

/// <summary>The person who published a release. The login is checked against GitHub's rules and the avatar is an <c>https</c> address on GitHub's avatar host, or null.</summary>
public sealed record ReleaseAuthor(string Login, string? AvatarUrl)
{
    /// <summary>The profile, built from the login and never taken from the data. A bot (<c>name[bot]</c>) has the page of its app.</summary>
    public string ProfileUrl => Login.EndsWith("[bot]", StringComparison.Ordinal) ? $"https://github.com/apps/{Login[..^5]}" : $"https://github.com/{Login}";
}

/// <summary>
/// One release, as the pages and the feed use it. Everything in it came from outside and is untrusted: <see cref="Body"/> is Markdown that only
/// <see cref="ReleaseBody"/> may turn into HTML, and the other text is always encoded where it is written.
/// </summary>
/// <param name="Id">The id GitHub gave the release.</param>
/// <param name="Tag">The tag of the release (<c>v1.4.0</c>). It can hold any character, so it is never used in an address: <see cref="Slug"/> is.</param>
/// <param name="Title">The name of the release, or its tag when it has none.</param>
/// <param name="Slug">The segment of the address of the release page (<c>/releases/v1.4.0</c>): lowercase, letters, digits, dots, hyphens and underscores, unique in the list.</param>
/// <param name="Body">The description, in GitHub Markdown. Empty when there is none.</param>
/// <param name="PublishedAt">When it was published, or null.</param>
/// <param name="Prerelease">Whether GitHub marks it as a pre-release.</param>
/// <param name="Url">The page of the release on GitHub, built from the repository and the tag, or null when the source has no repository.</param>
/// <param name="Author">Who published it, or null.</param>
public sealed record ReleaseEntry(
    long Id,
    string Tag,
    string Title,
    string Slug,
    string Body,
    DateTimeOffset? PublishedAt,
    bool Prerelease,
    string? Url,
    ReleaseAuthor? Author);
