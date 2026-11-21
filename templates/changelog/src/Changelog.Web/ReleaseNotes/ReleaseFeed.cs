using System.Globalization;
using System.Text;
using System.Xml;

namespace Changelog.Web.ReleaseNotes;

/// <summary>Where the feed lives and how it describes itself.</summary>
/// <param name="SiteUrl">The address of the site with a trailing slash (<c>https://example.com/</c>, or with its path base). Release links are built from it.</param>
/// <param name="Title">The title of the feed.</param>
/// <param name="Subtitle">A line under the title.</param>
public sealed record FeedInfo(string SiteUrl, string Title, string Subtitle);

/// <summary>
/// The Atom feed of the latest releases (<c>/feed.xml</c>). Everything that comes from a release goes through <see cref="XmlWriter"/>, which escapes it, after
/// the characters XML cannot hold (control characters, lone surrogates) are removed: a release name or body must not be able to break the document or add
/// elements to it. The HTML of an entry is the output of <see cref="ReleaseBody"/> and travels as escaped text in a <c>content type="html"</c> element.
/// </summary>
public static class ReleaseFeed
{
    public const int MaxEntries = 20;

    // The copy button of a code block (Content/MarkdownRenderer.cs) works with the script of the site: in a feed reader it would be a word with no use.
    private const string CopyButton = "<button type=\"button\" class=\"code-copy\" data-code-copy>Copy</button><span class=\"code-copy-status\" role=\"status\" aria-live=\"polite\"></span>";

    /// <summary>The document, as UTF-8 text.</summary>
    public static string Build(IReadOnlyList<ReleaseEntry> releases, FeedInfo info, ReleaseBody body, DateTimeOffset fallbackUpdated)
    {
        var entries = releases.Take(MaxEntries).ToList();
        var updated = entries.Select(r => r.PublishedAt).Where(d => d is not null).Max() ?? fallbackUpdated;

        var text = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false), OmitXmlDeclaration = false, CheckCharacters = true };
        using (var writer = new StringWriterWithEncoding(text, settings.Encoding))
        using (var xml = XmlWriter.Create(writer, settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
            xml.WriteAttributeString("xml", "base", null, info.SiteUrl);
            xml.WriteElementString("title", Clean(info.Title));
            xml.WriteElementString("subtitle", Clean(info.Subtitle));
            xml.WriteElementString("id", info.SiteUrl);
            xml.WriteElementString("updated", Date(updated));
            Link(xml, "self", info.SiteUrl + "feed.xml");
            Link(xml, "alternate", info.SiteUrl);

            foreach (var release in entries)
            {
                var url = info.SiteUrl + "releases/" + release.Slug;
                xml.WriteStartElement("entry");
                xml.WriteElementString("id", url);
                xml.WriteElementString("title", Clean(release.Title));
                Link(xml, "alternate", url);
                xml.WriteElementString("updated", Date(release.PublishedAt ?? fallbackUpdated));
                if (release.PublishedAt is { } published)
                {
                    xml.WriteElementString("published", Date(published));
                }
                if (release.Author is { } author)
                {
                    xml.WriteStartElement("author");
                    xml.WriteElementString("name", Clean(author.Login));
                    xml.WriteElementString("uri", author.ProfileUrl);
                    xml.WriteEndElement();
                }
                if (release.Prerelease)
                {
                    xml.WriteStartElement("category");
                    xml.WriteAttributeString("term", "pre-release");
                    xml.WriteEndElement();
                }
                // The same rendering as the release page would cache (relative address of the page): the base of the feed (xml:base above) makes its links
                // absolute for a reader, and the address of the site, which may come from the request, is never part of what is cached.
                var html = body.Render(release, "releases/" + release.Slug, minHeadingLevel: 3).Replace(CopyButton, "", StringComparison.Ordinal);
                if (html.Length > 0)
                {
                    xml.WriteStartElement("content");
                    xml.WriteAttributeString("type", "html");
                    xml.WriteString(Clean(html));
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
        return text.ToString();
    }

    private static void Link(XmlWriter xml, string rel, string href)
    {
        xml.WriteStartElement("link");
        xml.WriteAttributeString("rel", rel);
        xml.WriteAttributeString("href", href);
        xml.WriteEndElement();
    }

    // RFC 3339, in UTC.
    private static string Date(DateTimeOffset date) => date.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>Removes what XML 1.0 cannot hold: control characters other than tab, line feed and carriage return, lone surrogates, U+FFFE and U+FFFF.</summary>
    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }
        var clean = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                clean.Append(c).Append(text[++i]);
            }
            else if (c is '\t' or '\n' or '\r' || (c >= 0x20 && !char.IsSurrogate(c) && c is not '\uFFFE' and not '\uFFFF'))
            {
                clean.Append(c);
            }
        }
        return clean.ToString();
    }

    // A StringWriter reports UTF-16; the declaration of the document must say UTF-8, which is what the endpoint sends.
    private sealed class StringWriterWithEncoding(StringBuilder builder, Encoding encoding) : StringWriter(builder, CultureInfo.InvariantCulture)
    {
        public override Encoding Encoding { get; } = encoding;
    }
}
