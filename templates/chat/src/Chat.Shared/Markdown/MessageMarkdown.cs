using System.Net;
using Klyroo.Templates.Content;

namespace Chat.Shared.Markdown;

/// <summary>
/// Turns the text of an answer into HTML. An answer is written by a model that reads text from anywhere (a page, a document, a prompt that was injected),
/// so it is treated as hostile, with the rules the Markdown renderer keeps for content from outside (<see cref="UntrustedMarkdown"/>):
/// <list type="bullet">
/// <item>Raw HTML is not rendered: it is shown as text.</item>
/// <item>A link is <c>http</c>, <c>https</c> or <c>mailto</c> and nothing else (<c>javascript:</c>, <c>data:</c> and the rest lose their link and keep their text); it leaves with
///   <c>rel="noopener noreferrer nofollow"</c>. A relative link would point into this app, so it is dropped too.</item>
/// <item>Images are dropped, their alternative text stays: an address chosen by the model, loaded by the browser, is a way to carry the conversation out.</item>
/// <item>Headings start at level 2 (the title of the page is the <c>h1</c>), and their ids start with the id of the message, so one answer cannot take the id of an element of the page.</item>
/// <item>Code blocks are highlighted (a few languages) and carry a Copy button that <c>host.js</c> brings to life.</item>
/// <item>Text that Markdig refuses (nested too deeply to be sensible) is shown as plain text in a <c>pre</c>, never as an error.</item>
/// </list>
/// The renderer is thread safe and shared.
/// </summary>
public static class MessageMarkdown
{
    private static readonly MarkdownRenderer Renderer = new(allowRawHtml: false);

    /// <summary>The longest text rendered as Markdown (characters). A longer one is shown as plain text: the limits of the app keep answers far below it.</summary>
    public const int MaxCharacters = 100_000;

    /// <param name="markdown">The text of the answer.</param>
    /// <param name="idPrefix">What every id of the answer starts with: <c>m-{message id}-</c>.</param>
    /// <param name="pageHref">The address of the page, relative to the base (<c>c/{id}</c>): the target of a <c>#fragment</c> link, which would otherwise leave the page below a <c>&lt;base href&gt;</c>.</param>
    public static string Render(string markdown, string idPrefix, string pageHref)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "";
        }
        if (markdown.Length > MaxCharacters)
        {
            return Plain(markdown);
        }
        try
        {
            return Renderer.Render(markdown, "", pageHref, new UntrustedMarkdown(idPrefix, MinHeadingLevel: 2, AllowImages: false)).Html;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InsufficientExecutionStackException)
        {
            // Markdig refuses input nested too deeply to be sensible (twenty thousand "[" in a row): the text is shown as the plain text it is.
            return Plain(markdown);
        }
    }

    private static string Plain(string text) =>
        $"<pre class=\"message-plain\" tabindex=\"0\">{WebUtility.HtmlEncode(text)}</pre>";
}
