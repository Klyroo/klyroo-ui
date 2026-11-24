using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.TaskLists;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lux.Templates.Content;

/// <summary>A heading of a page: the <c>id</c> the renderer gave it, its text and its level (2 for an <c>h2</c>).</summary>
public sealed record MarkdownHeading(string Id, string Text, int Level);

/// <summary>What <see cref="MarkdownRenderer"/> makes of a Markdown text.</summary>
/// <param name="Html">The HTML of the page body.</param>
/// <param name="Headings">Every heading of the body, in document order.</param>
/// <param name="Text">The text of the page without markup, code included, on one line: what the search index is made of.</param>
/// <param name="Links">The pages the body links to (slugs, no fragment), for the check for broken links.</param>
/// <param name="Images">The local images the body shows, as paths below the web root without a query (the addresses of external images are not listed).</param>
public sealed record RenderedMarkdown(string Html, IReadOnlyList<MarkdownHeading> Headings, string Text, IReadOnlyList<string> Links, IReadOnlyList<string> Images);

/// <summary>
/// Extra rules for Markdown that nobody of the team wrote (release notes, comments, anything fetched from another service). Pass it to
/// <see cref="MarkdownRenderer.Render(string, string, string, UntrustedMarkdown)"/>. On top of what the renderer always does (raw HTML is shown as text,
/// <see cref="ContentUrl.AllowedSchemes"/> only), it makes the output safe to put on a page next to other content:
/// <list type="bullet">
/// <item>A link must be absolute (<c>http</c>, <c>https</c> or <c>mailto</c>) or a <c>#fragment</c> of the body; a relative address would point into this site, so it loses its link.
///   An image must be <c>https</c>: <c>http</c>, relative and protocol-relative images are dropped (the alternative text stays).</item>
/// <item>Links get <c>rel="noopener noreferrer nofollow"</c> and no <c>target</c>; images get <c>loading="lazy"</c>, <c>decoding="async"</c> and
///   <c>referrerpolicy="no-referrer"</c>.</item>
/// <item>Every id the body creates (headings, footnotes) starts with <see cref="IdPrefix"/> and <c>#fragment</c> links follow, so a body cannot take the
///   id of an element of the page and two bodies on one page do not share ids.</item>
/// <item>Headings are not higher than <see cref="MinHeadingLevel"/>.</item>
/// <item><c>@user</c> and <c>#123</c> in running text become links built from <see cref="MentionUrlFormat"/> and <see cref="IssueUrlFormat"/>, never from the text itself.</item>
/// </list>
/// </summary>
/// <param name="IdPrefix">What every id of the body starts with, for example <c>rel-v1-2-0-</c>.</param>
/// <param name="MentionUrlFormat">The address of a user, with <c>{0}</c> for the name (<c>https://github.com/{0}</c>); null leaves <c>@user</c> as text.</param>
/// <param name="IssueUrlFormat">The address of an issue or pull request, with <c>{0}</c> for the number; null leaves <c>#123</c> as text.</param>
/// <param name="MinHeadingLevel">The level of the highest heading of the body (2 to 6): a body shown under an <c>h2</c> passes 3, so a <c>#</c> in it does not outrank the page's own headings. Lower headings are not moved up.</param>
/// <param name="AllowImages">False drops every image and keeps its alternative text: a picture loaded from an address that someone else chose tells that someone when and where the page was read, and can carry data out in its address.</param>
public sealed record UntrustedMarkdown(string IdPrefix, string? MentionUrlFormat = null, string? IssueUrlFormat = null, int MinHeadingLevel = 1, bool AllowImages = true);

/// <summary>
/// Renders Markdown to HTML with Markdig, for content that is shown to visitors.
/// <list type="bullet">
/// <item>Raw HTML is not rendered: it is shown as text (unless <c>allowRawHtml</c>, for content you alone can write).</item>
/// <item>Links and images keep to <see cref="ContentUrl.AllowedSchemes"/> and relative addresses. A link with another scheme loses its link and keeps its text.</item>
/// <item>Relative links are rewritten against the <c>&lt;base href&gt;</c> of the site, see <see cref="ContentUrl.Rewrite"/>, and <c>#fragment</c> links stay on the page.</item>
/// <item>Headings get ids (as GitHub does: lowercase, hyphens, <c>-1</c> for a repeat); code blocks are highlighted and carry a copy button; tables scroll
///   on their own; <c>::: tip</c> containers and <c>&gt; [!NOTE]</c> alerts become callouts.</item>
/// </list>
/// The extensions are listed one by one instead of Markdig's "advanced" set, which includes generic attributes (<c>{onclick=...}</c> would add HTML
/// attributes even with raw HTML off), media links (embeds iframes) and mathematics. Typographic replacement is left out too: it would turn
/// <c>---</c> into a long dash. The instance is thread safe.
/// </summary>
public sealed partial class MarkdownRenderer
{
    // Ids the page layout already uses: a heading called "Main" must not take the id of the main landmark.
    private static readonly HashSet<string> ReservedIds = new(StringComparer.Ordinal) { "main", "search" };

    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer(bool allowRawHtml = false)
    {
        var builder = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseTaskLists()
            .UseFootnotes()
            .UseDefinitionLists()
            .UseEmphasisExtras()
            .UseListExtras()
            .UseAutoLinks()
            .UseAlertBlocks()
            .UseCustomContainers()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub);
        builder.Extensions.Add(new CalloutAndCodeExtension());
        if (!allowRawHtml)
        {
            builder.DisableHtml();
        }
        _pipeline = builder.Build();
    }

    /// <summary>
    /// Renders a page body.
    /// </summary>
    /// <param name="markdown">The Markdown, without its front matter.</param>
    /// <param name="sourceDirectory">The folder of the file, as slugs without a leading slash ("" for the root): relative links start from it.</param>
    /// <param name="pageHref">The address of the page, relative to the base: the target of <c>#fragment</c> links.</param>
    public RenderedMarkdown Render(string markdown, string sourceDirectory, string pageHref) =>
        Render(markdown, sourceDirectory, pageHref, null);

    /// <summary>Renders a body that comes from outside the team, with the extra rules of <paramref name="untrusted"/>.</summary>
    /// <param name="markdown">The Markdown.</param>
    /// <param name="sourceDirectory">The folder of the file ("" when there is none).</param>
    /// <param name="pageHref">The address of the page that shows the body: the target of <c>#fragment</c> links. A feed passes an absolute one.</param>
    /// <param name="untrusted">The rules for content nobody of the team wrote, or null for a page of the site.</param>
    public RenderedMarkdown Render(string markdown, string sourceDirectory, string pageHref, UntrustedMarkdown? untrusted)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        if (untrusted is not null)
        {
            LinkReferences(document, untrusted);
        }
        var links = new List<string>();
        var images = new List<string>();

        // A heading that took an id the layout uses ("main") gets another one, and that one must not be the id of another heading ("main-section" is a
        // heading too): every id of the page is known before one is changed.
        var headingBlocks = document.Descendants<HeadingBlock>().ToList();
        var used = new HashSet<string>(headingBlocks.Select(h => h.GetAttributes().Id).OfType<string>(), StringComparer.Ordinal);
        var headings = new List<MarkdownHeading>();
        foreach (var heading in headingBlocks)
        {
            if (untrusted is not null && heading.Level < untrusted.MinHeadingLevel)
            {
                heading.Level = Math.Min(6, untrusted.MinHeadingLevel);
            }
            var attributes = heading.GetAttributes();
            if (untrusted is null && attributes.Id is { } id && ReservedIds.Contains(id))
            {
                var renamed = id + "-section";
                for (var n = 1; used.Contains(renamed); n++)
                {
                    renamed = $"{id}-section-{n}";
                }
                used.Add(renamed);
                attributes.Id = renamed;
            }
            if (untrusted is not null && attributes.Id is not null)
            {
                attributes.Id = untrusted.IdPrefix + attributes.Id;
            }
            if (attributes.Id is not null)
            {
                headings.Add(new MarkdownHeading(attributes.Id, heading.Inline is null ? "" : InlineText(heading.Inline), heading.Level));
            }
        }

        foreach (var link in document.Descendants<LinkInline>().ToList())
        {
            if (!ContentUrl.IsAllowed(link.Url) || (untrusted is not null && !IsRemote(link, untrusted)))
            {
                // Keep what the reader would have seen: the text of a link, the alt text of an image. The address is gone.
                link.ReplaceBy(new ContainerInline(), copyChildren: true);
                continue;
            }
            if (link.Url is { Length: > 0 } url)
            {
                link.Url = ContentUrl.Rewrite(url, sourceDirectory, out var page);
                if (page is not null && !link.IsImage)
                {
                    links.Add(page);
                }
                if (link.IsImage && !ContentUrl.IsExternal(url) && link.Url is { Length: > 0 } image && image[0] != '#')
                {
                    // Local images are served from the web root, not from the content folder: the site checks that the file is there.
                    images.Add(image.Split('?', '#')[0]);
                }
                if (ContentUrl.IsExternal(url) && !link.IsImage)
                {
                    link.GetAttributes().AddProperty("rel", untrusted is null ? "noopener noreferrer" : "noopener noreferrer nofollow");
                }
                if (untrusted is not null && link.IsImage)
                {
                    var imageAttributes = link.GetAttributes();
                    imageAttributes.AddProperty("loading", "lazy");
                    imageAttributes.AddProperty("decoding", "async");
                    imageAttributes.AddProperty("referrerpolicy", "no-referrer");
                }
            }
        }
        foreach (var auto in document.Descendants<AutolinkInline>().ToList())
        {
            if (!ContentUrl.IsAllowed(auto.Url) || (untrusted is not null && !ContentUrl.HasScheme(auto.Url, "http", "https", "mailto")))
            {
                auto.ReplaceBy(new LiteralInline(auto.Url), copyChildren: false);
            }
            else if (untrusted is not null)
            {
                auto.GetAttributes().AddProperty("rel", "noopener noreferrer nofollow");
            }
        }

        var html = Markdown.ToHtml(document, _pipeline);
        // Footnote links are "#fn:1". Below a <base href> a bare fragment leaves the page, so each one gets the address of its page. Text that looks
        // like this cannot come from the content: a quote in text is encoded.
        html = html.Replace("href=\"#", $"href=\"{WebUtility.HtmlEncode(pageHref)}#{(untrusted is null ? "" : WebUtility.HtmlEncode(untrusted.IdPrefix))}", StringComparison.Ordinal);
        if (untrusted is not null)
        {
            // Footnote ids, like the headings above, must not be the ids of another body or of the page.
            html = html.Replace("id=\"fn", $"id=\"{WebUtility.HtmlEncode(untrusted.IdPrefix)}fn", StringComparison.Ordinal);
        }
        // A wide table scrolls inside its own box, and the box can be reached with the keyboard.
        // Each box is a region with a name of its own (landmarks must be told apart).
        var tables = 0;
        html = Regex.Replace(html, "<table", _ => $"<div class=\"table-scroll\" tabindex=\"0\" role=\"region\" aria-label=\"Table {++tables}\"><table")
            .Replace("</table>", "</table></div>", StringComparison.Ordinal);

        var text = Whitespace().Replace(PlainText(document), " ").Trim();
        return new RenderedMarkdown(html, headings, text, links, images);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // @user and #123 in running text. The lookbehind keeps an e-mail address ("a@b.com"), a path ("owner/repo#1") and a word ("C#12") out of it.
    [GeneratedRegex(@"(?<![\w@/&.])@([A-Za-z0-9](?:[A-Za-z0-9]|-(?=[A-Za-z0-9])){0,38})(?![\w@-])|(?<![\w/&#])#([0-9]{1,9})(?![\w#])")]
    private static partial Regex Reference();

    // Whether a link or an image may stay in a body from outside: an absolute address, a #fragment of the body, an https image.
    private static bool IsRemote(LinkInline link, UntrustedMarkdown untrusted)
    {
        var url = link.Url;
        if (link.IsImage)
        {
            return untrusted.AllowImages && ContentUrl.HasScheme(url, "https");
        }
        return !string.IsNullOrEmpty(url) && (ContentUrl.HasScheme(url, "http", "https", "mailto") || url.TrimStart().StartsWith('#'));
    }

    // Turns "@user" and "#123" of the text (not of code, not of a link) into links, in the tree: the text goes through the renderer's encoding like any other.
    private static void LinkReferences(MarkdownDocument document, UntrustedMarkdown untrusted)
    {
        if (untrusted.MentionUrlFormat is null && untrusted.IssueUrlFormat is null)
        {
            return;
        }
        foreach (var literal in document.Descendants<LiteralInline>().ToList())
        {
            if (literal.Parent is null || literal.FirstParentOfType<LinkInline>() is not null || literal.FirstParentOfType<AutolinkInline>() is not null)
            {
                continue;
            }
            var text = literal.Content.ToString();
            var matches = Reference().Matches(text);
            if (matches.Count == 0)
            {
                continue;
            }
            var replacement = new ContainerInline();
            var position = 0;
            foreach (Match match in matches)
            {
                var mention = match.Groups[1].Success;
                var format = mention ? untrusted.MentionUrlFormat : untrusted.IssueUrlFormat;
                if (format is null)
                {
                    continue;
                }
                if (match.Index > position)
                {
                    replacement.AppendChild(new LiteralInline(text[position..match.Index]));
                }
                var name = mention ? match.Groups[1].Value : match.Groups[2].Value;
                var link = new LinkInline(string.Format(System.Globalization.CultureInfo.InvariantCulture, format, name), string.Empty);
                link.AppendChild(new LiteralInline(match.Value));
                replacement.AppendChild(link);
                position = match.Index + match.Length;
            }
            if (position == 0)
            {
                continue;
            }
            if (position < text.Length)
            {
                replacement.AppendChild(new LiteralInline(text[position..]));
            }
            literal.ReplaceBy(replacement, copyChildren: false);
        }
    }

    private static string InlineText(ContainerInline container)
    {
        var text = new StringBuilder();
        foreach (var inline in container.Descendants())
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case HtmlEntityInline entity:
                    text.Append(entity.Transcoded.ToString());
                    break;
                case LineBreakInline:
                    text.Append(' ');
                    break;
            }
        }
        return text.ToString().Trim();
    }

    private static string PlainText(MarkdownDocument document)
    {
        var text = new StringBuilder();
        foreach (var block in document.Descendants<LeafBlock>())
        {
            if (block is CodeBlock code)
            {
                text.Append(code.Lines.ToString()).Append(' ');
            }
            else if (block.Inline is not null)
            {
                text.Append(InlineText(block.Inline)).Append(' ');
            }
        }
        return text.ToString();
    }

    /// <summary>Callouts and code blocks are written by the page, not by Markdig, to give them the markup the stylesheet and the copy script expect.</summary>
    private sealed class CalloutAndCodeExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer html)
            {
                html.ObjectRenderers.Replace<CodeBlockRenderer>(new HighlightedCodeRenderer());
                html.ObjectRenderers.Replace<HtmlTaskListRenderer>(new TaskBoxRenderer());
                html.ObjectRenderers.Replace<AlertBlockRenderer>(new AlertRenderer());
                html.ObjectRenderers.Replace<HtmlCustomContainerRenderer>(new ContainerRenderer());
            }
        }
    }

    private static readonly Dictionary<string, (string Title, string Color)> Callouts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["note"] = ("Note", "info"),
        ["info"] = ("Note", "info"),
        ["tip"] = ("Tip", "success"),
        ["important"] = ("Important", "primary"),
        ["warning"] = ("Warning", "warning"),
        ["caution"] = ("Caution", "error"),
        ["danger"] = ("Caution", "error"),
    };

    private static void WriteCalloutStart(HtmlRenderer renderer, string kind, string title)
    {
        var (_, color) = Callouts[kind];
        renderer.Write($"<div class=\"callout\" role=\"note\" data-callout=\"{kind.ToLowerInvariant()}\" data-color=\"{color}\">");
        renderer.Write($"<p class=\"callout-title\">{WebUtility.HtmlEncode(title)}</p>");
    }

    private sealed class AlertRenderer : HtmlObjectRenderer<AlertBlock>
    {
        protected override void Write(HtmlRenderer renderer, AlertBlock block)
        {
            var kind = block.Kind.ToString();
            if (!Callouts.TryGetValue(kind, out var known))
            {
                renderer.Write("<div>");
                renderer.WriteChildren(block);
                renderer.Write("</div>");
                return;
            }
            WriteCalloutStart(renderer, kind, known.Title);
            renderer.WriteChildren(block);
            renderer.Write("</div>");
        }
    }

    private sealed class ContainerRenderer : HtmlObjectRenderer<CustomContainer>
    {
        protected override void Write(HtmlRenderer renderer, CustomContainer block)
        {
            var kind = block.Info ?? "";
            if (!Callouts.TryGetValue(kind, out var known))
            {
                renderer.Write("<div>");
                renderer.WriteChildren(block);
                renderer.Write("</div>");
                return;
            }
            // "::: tip Before you start" names the callout; without a name it is called after its kind.
            var title = string.IsNullOrWhiteSpace(block.Arguments) ? known.Title : block.Arguments.Trim();
            WriteCalloutStart(renderer, kind, title);
            renderer.WriteChildren(block);
            renderer.Write("</div>");
        }
    }

    // The box of a task list item is a disabled checkbox. Markdig gives it no label, so the text of its item (not the items nested in it, which are other
    // blocks) names it.
    private sealed class TaskBoxRenderer : HtmlObjectRenderer<TaskList>
    {
        protected override void Write(HtmlRenderer renderer, TaskList box)
        {
            var label = box.Parent?.ParentBlock?.Inline is { } inline ? InlineText(inline) : "";
            renderer.Write($"<input aria-label=\"{WebUtility.HtmlEncode(label)}\" disabled=\"disabled\" type=\"checkbox\"{(box.Checked ? " checked=\"checked\"" : "")} />");
        }
    }

    private sealed class HighlightedCodeRenderer : HtmlObjectRenderer<CodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock block)
        {
            var code = block.Lines.ToString().TrimEnd('\n', '\r');
            var language = block is FencedCodeBlock { Info: { Length: > 0 } info }
                ? info.Split([' ', '\t', '{'], 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                : null;
            var label = language is not null && SyntaxHighlighter.Supports(language) ? language.ToLowerInvariant() : "text";
            renderer.Write($"<div class=\"code-block\" data-language=\"{WebUtility.HtmlEncode(label)}\">");
            renderer.Write("<button type=\"button\" class=\"code-copy\" data-code-copy>Copy</button>");
            renderer.Write("<span class=\"code-copy-status\" role=\"status\" aria-live=\"polite\"></span>");
            renderer.Write($"<pre tabindex=\"0\"><code>{SyntaxHighlighter.ToHtml(code, language)}</code></pre>");
            renderer.Write("</div>");
        }
    }
}
