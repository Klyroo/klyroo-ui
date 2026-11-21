using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Changelog.Web.ReleaseNotes;

/// <summary>
/// Reads the JSON that <c>GET /repos/{owner}/{repo}/releases</c> returns (and the file source holds the same format). The data is untrusted: only the
/// fields the pages use are read, each is checked or limited, and a release that cannot be used is skipped instead of failing the list.
/// </summary>
public static partial class GitHubReleaseJson
{
    /// <summary>The longest body kept, in characters. A larger one is cut: a release note that long is not meant to be read on a page.</summary>
    public const int MaxBodyLength = 60_000;

    private const int MaxTagLength = 255;
    private const int MaxTitleLength = 200;
    private const string AvatarHost = "avatars.githubusercontent.com";

    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})(?:\[bot\])?\z")]
    private static partial Regex LoginPattern();

    /// <summary>
    /// Parses one page of the API: an array of releases, newest first. Drafts are skipped. <paramref name="repository"/> (<c>owner/repo</c>, already
    /// validated) is only used to build the address of each release on GitHub; null leaves it out.
    /// </summary>
    /// <exception cref="JsonException">The text is not JSON or not an array.</exception>
    public static IReadOnlyList<ReleaseEntry> Parse(Stream json, string? repository)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The releases are not a JSON array.");
        }

        var parsed = new List<(long Id, string Tag, string Title, string Body, DateTimeOffset? Published, bool Prerelease, ReleaseAuthor? Author)>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || Bool(item, "draft"))
            {
                continue;
            }
            try
            {
                var tag = Text(item, "tag_name", MaxTagLength);
                if (tag is null)
                {
                    continue;
                }
                var title = Text(item, "name", MaxTitleLength) ?? tag;
                var body = Body(item);
                var published = Date(item, "published_at") ?? Date(item, "created_at");
                var id = item.TryGetProperty("id", out var idValue) && idValue.ValueKind == JsonValueKind.Number && idValue.TryGetInt64(out var number) ? number : 0;
                parsed.Add((id, tag, title, body, published, Bool(item, "prerelease"), Author(item)));
            }
            catch (InvalidOperationException)
            {
                // A string that is not valid text (a lone surrogate in an escape) cannot be read: that release is left out, the others are not.
            }
        }

        var slugs = ReleaseSlug.Assign(parsed.Select(p => p.Tag));
        var releases = new List<ReleaseEntry>(parsed.Count);
        for (var i = 0; i < parsed.Count; i++)
        {
            var p = parsed[i];
            var url = repository is null ? null : $"https://github.com/{repository}/releases/tag/{Uri.EscapeDataString(p.Tag)}";
            releases.Add(new ReleaseEntry(p.Id, p.Tag, p.Title, slugs[i], p.Body, p.Published, p.Prerelease, url, p.Author));
        }
        return releases;
    }

    private static bool Bool(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    // A one line text of at most max characters, or null when there is none. Control characters and line breaks become spaces: they have no use in a name.
    private static string? Text(JsonElement item, string name, int max)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }
        // Characters XML cannot hold (U+FFFE, a lone surrogate) are dropped here: they have no use in a name, and the feed and the address of the page could not take them.
        var text = ReleaseFeed.Clean(value.GetString());
        var clean = new StringBuilder(Math.Min(text.Length, max));
        foreach (var c in text)
        {
            clean.Append(char.IsControl(c) ? ' ' : c);
            if (clean.Length >= max)
            {
                break;
            }
        }
        var result = clean.ToString().Trim();
        return result.Length == 0 ? null : result;
    }

    private static string Body(JsonElement item)
    {
        if (!item.TryGetProperty("body", out var value) || value.ValueKind != JsonValueKind.String)
        {
            return "";
        }
        var body = (value.GetString() ?? "").Replace("\r\n", "\n", StringComparison.Ordinal);
        if (body.Length > MaxBodyLength)
        {
            // Not in the middle of a surrogate pair.
            var length = char.IsHighSurrogate(body[MaxBodyLength - 1]) ? MaxBodyLength - 1 : MaxBodyLength;
            body = body[..length].TrimEnd() + "\n\n…";
        }
        return body;
    }

    private static DateTimeOffset? Date(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
        && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date
            : null;

    private static ReleaseAuthor? Author(JsonElement item)
    {
        if (!item.TryGetProperty("author", out var author) || author.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var login = author.TryGetProperty("login", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString() : null;
        if (login is null || !LoginPattern().IsMatch(login))
        {
            return null;
        }
        string? avatar = null;
        if (author.TryGetProperty("avatar_url", out var a) && a.ValueKind == JsonValueKind.String
            && Uri.TryCreate(a.GetString(), UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps && uri.Host == AvatarHost && uri.UserInfo.Length == 0 && uri.IsDefaultPort)
        {
            // The address as the parser understood it, so what is written in the page is what was checked.
            avatar = uri.AbsoluteUri;
        }
        return new ReleaseAuthor(login, avatar);
    }
}
