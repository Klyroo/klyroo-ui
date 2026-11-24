using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lux.Templates.Content;

/// <summary>
/// What a link or an image in a page may point at, and how a link written in Markdown becomes a link of the site.
/// A site is served below a <c>&lt;base href&gt;</c> (see the PathBase setting), so every address a page produces is relative to that base,
/// never to the page that holds it and never absolute.
/// </summary>
public static partial class ContentUrl
{
    /// <summary>The schemes a link or an image may use. Anything else with a scheme (<c>javascript:</c>, <c>data:</c>, <c>vbscript:</c>, <c>file:</c>) is dropped.</summary>
    public static readonly IReadOnlyList<string> AllowedSchemes = ["http", "https", "mailto"];

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9+.\-]*:")]
    private static partial Regex SchemePattern();

    [GeneratedRegex(@"[^a-z0-9._\-]+")]
    private static partial Regex NotSlugCharacters();

    /// <summary>
    /// True for an address a page may use: no scheme (a relative address), or a scheme of <see cref="AllowedSchemes"/>.
    /// Browsers ignore tabs, new lines and other control characters inside a URL, so <c>java\tscript:</c> is read as <c>javascript:</c>;
    /// the check removes them first, as a browser does.
    /// </summary>
    public static bool IsAllowed(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        var cleaned = Clean(url);
        if (cleaned.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }
        var scheme = SchemePattern().Match(cleaned);
        return !scheme.Success || AllowedSchemes.Contains(scheme.Value[..^1], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>True when the address starts with one of <paramref name="schemes"/> (case does not matter). Tabs and new lines inside it are ignored, as a browser does.</summary>
    public static bool HasScheme(string? url, params string[] schemes)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }
        var scheme = SchemePattern().Match(Clean(url));
        return scheme.Success && schemes.Contains(scheme.Value[..^1], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>True when the address leaves the site: it has a scheme, or starts with <c>//</c>.</summary>
    public static bool IsExternal(string url)
    {
        var cleaned = Clean(url);
        return cleaned.StartsWith("//", StringComparison.Ordinal) || SchemePattern().IsMatch(cleaned);
    }

    private static string Clean(string url)
    {
        var builder = new StringBuilder(url.Length);
        foreach (var c in url)
        {
            if (!char.IsControl(c) && !char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Turns a link of a Markdown file into an address relative to the <c>&lt;base href&gt;</c> of the site. A link that starts with <c>/</c> is from the
    /// root of the site (<c>/guides/setup</c>), any other is from the folder of the file (<c>setup</c>, <c>./setup.md</c>, <c>../guides/setup</c>).
    /// The <c>.md</c> extension and a final <c>index</c> are dropped, and the query and the fragment are kept. A fragment alone (<c>#usage</c>)
    /// and an external address are returned as they are.
    /// </summary>
    /// <param name="url">The address as written.</param>
    /// <param name="sourceDirectory">The folder of the Markdown file, as a path of slugs without a leading slash ("" for the root).</param>
    /// <param name="page">The slug the address points at when it is a page, or null for an external address, a fragment or a file.</param>
    public static string Rewrite(string url, string sourceDirectory, out string? page)
    {
        page = null;
        var trimmed = url.Trim();
        if (trimmed.Length == 0 || trimmed[0] == '#' || IsExternal(trimmed))
        {
            return url;
        }

        var cut = trimmed.IndexOfAny(['?', '#']);
        var path = cut < 0 ? trimmed : trimmed[..cut];
        var rest = cut < 0 ? "" : trimmed[cut..];
        path = path.Replace('\\', '/');
        if (path.Length == 0)
        {
            return url;
        }

        var joined = path[0] == '/' || sourceDirectory.Length == 0 ? path : sourceDirectory + "/" + path;
        var segments = new List<string>();
        foreach (var segment in joined.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }
            if (segment == "..")
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }
                continue;
            }
            segments.Add(segment);
        }

        var last = segments.Count > 0 ? segments[^1] : "";
        var isMarkdown = last.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
        var isPage = isMarkdown || last.Length == 0 || !last.Contains('.');
        if (isMarkdown)
        {
            segments[^1] = last[..^3];
        }
        if (isPage)
        {
            if (segments.Count > 0 && segments[^1].Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                segments.RemoveAt(segments.Count - 1);
            }
            // "Set%20Up.md" is the file "Set Up.md": the name is decoded before it becomes a segment of an address.
            var slug = string.Join('/', segments.Select(segment => Segment(Decode(segment))));
            page = slug;
            return (slug.Length == 0 ? "./" : slug) + rest;
        }
        return string.Join('/', segments) + rest;
    }

    private static string Decode(string segment)
    {
        try
        {
            return Uri.UnescapeDataString(segment);
        }
        catch (UriFormatException)
        {
            return segment;
        }
    }

    /// <summary>A file or folder name as a segment of an address: lowercase, with letters, digits, dots, hyphens and underscores only.</summary>
    public static string Segment(string name)
    {
        var lower = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                plain.Append(c);
            }
        }
        return NotSlugCharacters().Replace(plain.ToString(), "-").Trim('-');
    }
}
