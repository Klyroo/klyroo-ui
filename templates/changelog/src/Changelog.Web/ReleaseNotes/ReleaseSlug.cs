using Lux.Templates.Content;

namespace Changelog.Web.ReleaseNotes;

/// <summary>
/// The address of a release page. A tag can hold anything (<c>release/1.0</c>, <c>../../etc</c>, <c>v1.0 beta</c>, text in any script), so it is never put in an
/// address as it is and never used to find a file: it becomes a slug, and the page looks the slug up in the list of releases it already has.
/// </summary>
public static class ReleaseSlug
{
    public const int MaxLength = 80;

    /// <summary>
    /// A segment of letters, digits, dots, hyphens and underscores, lowercase, that cannot be read as a path (<c>.</c> and <c>..</c> are not slugs) and does
    /// not start or end with a separator. A tag that leaves nothing ("***") becomes <c>release</c>.
    /// </summary>
    public static string From(string? tag)
    {
        var slug = Segment(tag ?? "").Trim('.', '-', '_');
        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('.', '-', '_');
        }
        return slug.Length == 0 ? "release" : slug;
    }

    // The shared segment code normalizes the text, which throws for a code point that is not a character (U+FFFE): a tag with one must not stop the list.
    private static string Segment(string tag)
    {
        try
        {
            return ContentUrl.Segment(tag);
        }
        catch (ArgumentException)
        {
            return ContentUrl.Segment(ReleaseFeed.Clean(tag).Replace("\uFFFE", "", StringComparison.Ordinal).Replace("\uFFFF", "", StringComparison.Ordinal));
        }
    }

    /// <summary>One slug for each tag, in order: a tag whose slug is taken gets <c>-2</c>, <c>-3</c>... so every release has its own page.</summary>
    public static IReadOnlyList<string> Assign(IEnumerable<string?> tags)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var slugs = new List<string>();
        foreach (var tag in tags)
        {
            var slug = From(tag);
            var candidate = slug;
            for (var n = 2; !used.Add(candidate); n++)
            {
                candidate = $"{slug}-{n}";
            }
            slugs.Add(candidate);
        }
        return slugs;
    }
}
