using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Blux.Templates.Content;

/// <summary>A heading of a page: the <c>id</c> the renderer gave it, its text and its level (2 for an <c>h2</c>).</summary>
public sealed record MarkdownHeading(string Id, string Text, int Level);

/// <summary>What <see cref="MarkdownRenderer"/> makes of a Markdown text.</summary>
/// <param name="Html">The HTML of the page body.</param>
/// <param name="Headings">Every heading of the body, in document order.</param>
/// <param name="Text">The text of the page without markup, code included, on one line: what the search index is made of.</param>
/// <param name="Links">The pages the body links to (slugs, no fragment), for the check for broken links.</param>
public sealed record RenderedMarkdown(string Html, IReadOnlyList<MarkdownHeading> Headings, string Text, IReadOnlyList<string> Links);

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
    public RenderedMarkdown Render(string markdown, string sourceDirectory, string pageHref)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        var links = new List<string>();

        var headings = new List<MarkdownHeading>();
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var attributes = heading.GetAttributes();
            if (attributes.Id is { } id && ReservedIds.Contains(id))
            {
                attributes.Id = id + "-section";
            }
            if (attributes.Id is not null)
            {
                headings.Add(new MarkdownHeading(attributes.Id, heading.Inline is null ? "" : InlineText(heading.Inline), heading.Level));
            }
        }

        foreach (var link in document.Descendants<LinkInline>().ToList())
        {
            if (!ContentUrl.IsAllowed(link.Url))
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
                if (ContentUrl.IsExternal(url) && !link.IsImage)
                {
                    link.GetAttributes().AddProperty("rel", "noopener noreferrer");
                }
            }
        }
        foreach (var auto in document.Descendants<AutolinkInline>().ToList())
        {
            if (!ContentUrl.IsAllowed(auto.Url))
            {
                auto.ReplaceBy(new LiteralInline(auto.Url), copyChildren: false);
            }
        }

        var html = Markdown.ToHtml(document, _pipeline);
        // Footnote links are "#fn:1". Below a <base href> a bare fragment leaves the page, so each one gets the address of its page. Text that looks
        // like this cannot come from the content: a quote in text is encoded.
        html = html.Replace("href=\"#", $"href=\"{WebUtility.HtmlEncode(pageHref)}#", StringComparison.Ordinal);
        // A wide table scrolls inside its own box, and the box can be reached with the keyboard.
        // Each box is a region with a name of its own (landmarks must be told apart).
        var tables = 0;
        html = Regex.Replace(html, "<table", _ => $"<div class=\"table-scroll\" tabindex=\"0\" role=\"region\" aria-label=\"Table {++tables}\"><table")
            .Replace("</table>", "</table></div>", StringComparison.Ordinal);

        // A task list box is a disabled checkbox with no label: the text of its item names it.
        html = TaskItem().Replace(html, m => $"<li class=\"task-list-item\"><input aria-label=\"{Tags().Replace(m.Groups[2].Value, "").Trim()}\" {m.Groups[1].Value} />{m.Groups[2].Value}</li>");

        var text = Whitespace().Replace(PlainText(document), " ").Trim();
        return new RenderedMarkdown(html, headings, text, links);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("""<li class="task-list-item"><input (disabled="disabled" type="checkbox"(?: checked="checked")?) />(.*?)</li>""", RegexOptions.Singleline)]
    private static partial Regex TaskItem();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();

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
