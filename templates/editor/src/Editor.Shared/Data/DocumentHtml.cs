using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Editor.Shared.Data;

/// <summary>
/// What may be stored and shown of a document. The editor writes HTML, and HTML that comes from a browser's storage, a paste or a hand edit is untrusted: it
/// is rebuilt here from an allow-list, tag by tag, before it is stored, before it is given back to the editor and before the server shows it as markup (the
/// read-only view that precedes the editor). Nothing is passed through: an element, an attribute or an address that is not listed is dropped, and the text is
/// written again, escaped. Links go to http, https or mailto; images come from https addresses only.
/// </summary>
public static partial class DocumentHtml
{
    // Elements kept with no attribute at all.
    private static readonly HashSet<string> Plain = new(StringComparer.Ordinal)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "strong", "em", "s", "u", "code", "pre", "blockquote", "ul", "ol", "li", "label", "div", "span", "br", "hr",
    };

    // Elements with no end tag.
    private static readonly HashSet<string> Void = new(StringComparer.Ordinal) { "br", "hr", "img", "input" };

    // Elements dropped with everything inside them: their content is code or markup for something else, never text to show.
    private static readonly HashSet<string> Raw = new(StringComparer.Ordinal)
    {
        "script", "style", "iframe", "object", "noscript", "template", "textarea", "title", "xmp", "noembed", "noframes", "svg", "math", "head", "select", "audio", "video",
    };

    // Elements that end a line of text, for the plain text of a document.
    private static readonly HashSet<string> Blocks = new(StringComparer.Ordinal)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "pre", "blockquote", "ul", "ol", "li", "div", "br", "hr", "img",
    };

    private const int MaxDepth = 64;
    private const int MaxAddress = 2048;
    private const int MaxAlt = 300;

    [GeneratedRegex("^language-[a-z0-9+#-]{1,24}$")]
    private static partial Regex LanguageClass();

    [GeneratedRegex(@"^\d{1,6}$")]
    private static partial Regex Digits();

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)*")]
    private static partial Regex WordPattern();

    /// <summary>Rebuilds <paramref name="html"/> from the allow-list. Null and blank give an empty string.</summary>
    public static string Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }
        var output = new StringBuilder(html.Length);
        var open = new List<string>();
        var index = 0;
        while (index < html.Length)
        {
            var lt = html.IndexOf('<', index);
            if (lt < 0)
            {
                AppendText(output, html[index..]);
                break;
            }
            if (lt > index)
            {
                AppendText(output, html[index..lt]);
            }
            index = lt;
            if (!TryReadMarkup(html, ref index, out var tag))
            {
                // A "<" that does not start a tag is text.
                output.Append("&lt;");
                index++;
                continue;
            }
            if (tag.Kind == Markup.Skipped)
            {
                continue;
            }
            if (tag.Kind == Markup.End)
            {
                Close(output, open, tag.Name);
                continue;
            }
            if (Raw.Contains(tag.Name))
            {
                index = SkipRaw(html, index, tag.Name);
                continue;
            }
            Open(output, open, tag);
        }
        for (var i = open.Count - 1; i >= 0; i--)
        {
            output.Append("</").Append(open[i]).Append('>');
        }
        return output.ToString();
    }

    private static void Open(StringBuilder output, List<string> open, Tag tag)
    {
        var name = tag.Name;
        var attributes = new StringBuilder();
        switch (name)
        {
            case "a":
                var href = Link(tag.Get("href"));
                if (href is null)
                {
                    // A link that is not allowed is its text.
                    return;
                }
                attributes.Append(" href=\"").Append(Attribute(href)).Append("\" rel=\"noopener noreferrer nofollow\"");
                break;
            case "img":
                var source = Image(tag.Get("src"));
                if (source is null)
                {
                    return;
                }
                attributes.Append(" src=\"").Append(Attribute(source)).Append('"');
                if (tag.Get("alt") is { } alt)
                {
                    attributes.Append(" alt=\"").Append(Attribute(Cut(alt, MaxAlt))).Append('"');
                }
                attributes.Append(" loading=\"lazy\" referrerpolicy=\"no-referrer\"");
                break;
            case "ol":
                if (tag.Get("start") is { } start && Digits().IsMatch(start))
                {
                    attributes.Append(" start=\"").Append(start).Append('"');
                }
                break;
            case "ul":
                if (tag.Get("data-type") == "taskList")
                {
                    attributes.Append(" data-type=\"taskList\"");
                }
                break;
            case "li":
                if (tag.Get("data-type") == "taskItem")
                {
                    attributes.Append(" data-type=\"taskItem\" data-checked=\"")
                        .Append(tag.Get("data-checked") is "true" or "" ? "true" : "false").Append('"');
                }
                break;
            case "input":
                // The checkbox of a task item, which the editor draws itself: here it only shows the state and cannot be changed.
                if (!string.Equals(tag.Get("type"), "checkbox", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                attributes.Append(" type=\"checkbox\"").Append(tag.Has("checked") ? " checked=\"checked\"" : "").Append(" disabled=\"disabled\"");
                break;
            case "code":
                if (tag.Get("class") is { } css && LanguageClass().IsMatch(css))
                {
                    attributes.Append(" class=\"").Append(css).Append('"');
                }
                break;
            default:
                if (!Plain.Contains(name))
                {
                    // Unknown element: its tag goes, what is inside stays as text.
                    return;
                }
                break;
        }
        if (!Void.Contains(name) && open.Count >= MaxDepth)
        {
            // Too deep to be anything the editor wrote: no element opens any deeper, its text stays.
            return;
        }
        output.Append('<').Append(name).Append(attributes).Append('>');
        if (!Void.Contains(name))
        {
            open.Add(name);
        }
    }

    private static void Close(StringBuilder output, List<string> open, string name)
    {
        var at = open.LastIndexOf(name);
        if (at < 0)
        {
            return;
        }
        for (var i = open.Count - 1; i >= at; i--)
        {
            output.Append("</").Append(open[i]).Append('>');
        }
        open.RemoveRange(at, open.Count - at);
    }

    private enum Markup { Start, End, Skipped }

    private sealed record Tag(Markup Kind, string Name, Dictionary<string, string> Attributes)
    {
        public string? Get(string name) => Attributes.TryGetValue(name, out var value) ? value : null;

        public bool Has(string name) => Attributes.ContainsKey(name);
    }

    // Reads what starts at html[index] == '<': a comment, a declaration, an end tag or a start tag. On success index is after it. False: the "<" is text.
    private static bool TryReadMarkup(string html, ref int index, out Tag tag)
    {
        tag = new Tag(Markup.Skipped, "", []);
        var next = index + 1 < html.Length ? html[index + 1] : '\0';
        if (html.AsSpan(index).StartsWith("<!--"))
        {
            var end = html.IndexOf("-->", index + 4, StringComparison.Ordinal);
            index = end < 0 ? html.Length : end + 3;
            return true;
        }
        if (next is '!' or '?')
        {
            var end = html.IndexOf('>', index);
            index = end < 0 ? html.Length : end + 1;
            return true;
        }
        var ending = next == '/';
        var cursor = index + (ending ? 2 : 1);
        if (cursor >= html.Length || !char.IsAsciiLetter(html[cursor]))
        {
            return false;
        }
        var start = cursor;
        while (cursor < html.Length && (char.IsAsciiLetterOrDigit(html[cursor]) || html[cursor] == '-'))
        {
            cursor++;
        }
        var name = html[start..cursor].ToLowerInvariant();
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        while (cursor < html.Length)
        {
            while (cursor < html.Length && (char.IsWhiteSpace(html[cursor]) || html[cursor] == '/'))
            {
                cursor++;
            }
            if (cursor >= html.Length)
            {
                break;
            }
            if (html[cursor] == '>')
            {
                index = cursor + 1;
                tag = new Tag(ending ? Markup.End : Markup.Start, name, attributes);
                return true;
            }
            var nameStart = cursor;
            while (cursor < html.Length && !char.IsWhiteSpace(html[cursor]) && html[cursor] is not ('=' or '>' or '/'))
            {
                cursor++;
            }
            var attribute = html[nameStart..cursor].ToLowerInvariant();
            while (cursor < html.Length && char.IsWhiteSpace(html[cursor]))
            {
                cursor++;
            }
            var value = "";
            if (cursor < html.Length && html[cursor] == '=')
            {
                cursor++;
                while (cursor < html.Length && char.IsWhiteSpace(html[cursor]))
                {
                    cursor++;
                }
                if (cursor < html.Length && (html[cursor] == '"' || html[cursor] == '\''))
                {
                    var quote = html[cursor++];
                    var close = html.IndexOf(quote, cursor);
                    if (close < 0)
                    {
                        // Never closed: the rest of the text belongs to the tag, as a browser reads it, and none of it is kept.
                        index = html.Length;
                        return true;
                    }
                    value = html[cursor..close];
                    cursor = close + 1;
                }
                else
                {
                    var valueStart = cursor;
                    while (cursor < html.Length && !char.IsWhiteSpace(html[cursor]) && html[cursor] != '>')
                    {
                        cursor++;
                    }
                    value = html[valueStart..cursor];
                }
            }
            if (attribute.Length > 0 && !attributes.ContainsKey(attribute))
            {
                attributes[attribute] = WebUtility.HtmlDecode(value);
            }
        }
        // The text ends inside the tag: nothing of it is kept.
        index = html.Length;
        return true;
    }

    // The index after the end tag of a raw element, or the end of the text.
    private static int SkipRaw(string html, int from, string name)
    {
        var end = html.IndexOf("</" + name, from, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            return html.Length;
        }
        var close = html.IndexOf('>', end);
        return close < 0 ? html.Length : close + 1;
    }

    private static void AppendText(StringBuilder output, string text)
    {
        foreach (var c in WebUtility.HtmlDecode(text))
        {
            if (char.IsControl(c) && c is not ('\n' or '\t' or '\r'))
            {
                continue;
            }
            output.Append(c switch { '&' => "&amp;", '<' => "&lt;", '>' => "&gt;", _ => c.ToString() });
        }
    }

    private static string Attribute(string value) => WebUtility.HtmlEncode(value);

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..max];

    // The address as a browser reads it (the URL standard): leading and trailing control characters and spaces go, and a tab or a line break is dropped wherever it is
    // ("java\nscript:" is "javascript:"). Any other space or control character inside what is left means the text is not an address: it is refused, never closed up, because
    // "https://exa mple.com/a b" must not become "https://example.com/ab", an address of another host. Empty when it is not one.
    private static string Normalised(string? address)
    {
        if (address is null)
        {
            return "";
        }
        var trimmed = address.AsSpan().Trim(TrimmedAroundAddress);
        var builder = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (c is '\t' or '\n' or '\r')
            {
                continue;
            }
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                return "";
            }
            builder.Append(c);
        }
        return builder.ToString();
    }

    // The C0 control characters and the space: what the URL standard strips from both ends of an address.
    private static readonly char[] TrimmedAroundAddress = [.. Enumerable.Range(0, 0x21).Select(i => (char)i)];

    /// <summary>The address of a link when it is allowed (http, https or mailto), else null.</summary>
    public static string? Link(string? address)
    {
        var value = Normalised(address);
        if (value.Length == 0 || value.Length > MaxAddress)
        {
            return null;
        }
        if (value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return value.Length > "mailto:".Length ? value : null;
        }
        return IsWebAddress(value, secureOnly: false) ? value : null;
    }

    /// <summary>The address of an image when it is allowed (https), else null.</summary>
    public static string? Image(string? address)
    {
        var value = Normalised(address);
        return value.Length is > 0 and <= MaxAddress && IsWebAddress(value, secureOnly: true) ? value : null;
    }

    private static bool IsWebAddress(string value, bool secureOnly)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Host.Length == 0 || uri.UserInfo.Length > 0)
        {
            return false;
        }
        return uri.Scheme == Uri.UriSchemeHttps || (!secureOnly && uri.Scheme == Uri.UriSchemeHttp);
    }

    /// <summary>
    /// The text of <paramref name="html"/> (cleaned first), a line for each block: what the search looks in and what the word count counts.
    /// </summary>
    public static string ToText(string? html)
    {
        var clean = Clean(html);
        var text = new StringBuilder(clean.Length);
        var index = 0;
        while (index < clean.Length)
        {
            if (clean[index] != '<')
            {
                var next = clean.IndexOf('<', index);
                if (next < 0)
                {
                    next = clean.Length;
                }
                text.Append(WebUtility.HtmlDecode(clean[index..next]));
                index = next;
                continue;
            }
            var end = clean.IndexOf('>', index);
            var name = clean[(index + 1)..end].TrimStart('/').Split(' ')[0];
            if (Blocks.Contains(name) && text.Length > 0 && text[^1] != '\n')
            {
                text.Append('\n');
            }
            index = end + 1;
        }
        return text.ToString().Trim();
    }

    /// <summary>The words of a text: runs of letters and digits (with apostrophes and hyphens inside them).</summary>
    public static int CountWords(string? text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : WordPattern().Matches(text).Count;
}
