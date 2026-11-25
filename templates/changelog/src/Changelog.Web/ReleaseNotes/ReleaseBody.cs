using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Klyroo.Templates.Content;

namespace Changelog.Web.ReleaseNotes;

/// <summary>
/// Turns the body of a release into HTML. The body is written by whoever can publish a release, or by a contributor whose pull request title ends up in
/// generated notes, so it is treated as hostile. The work is done by the shared <see cref="MarkdownRenderer"/> with its rules for content from outside
/// (<see cref="UntrustedMarkdown"/>):
/// <list type="bullet">
/// <item>Raw HTML is not rendered, it is shown as text. Attributes written in Markdown (<c>{onclick=...}</c>) are not supported.</item>
/// <item>Links use <c>http</c>, <c>https</c> or <c>mailto</c> and nothing else (<c>javascript:</c>, <c>data:</c>, <c>vbscript:</c> and <c>file:</c> lose their link and keep their text,
///   also when the scheme is written with entities, tabs or capitals). A relative link would point into this site, so it is dropped too.</item>
/// <item>Every link leaves with <c>rel="noopener noreferrer nofollow"</c> and opens in the same tab: there is no <c>target</c> to abuse, and a reader who wants a
///   new tab asks for one.</item>
/// <item>Images must be <c>https</c> (any host: release notes show badges and screenshots from several), load lazily and send no referrer.</item>
/// <item><c>@user</c> and <c>#123</c> become links to <c>github.com/user</c> and to the issue or pull request of this repository, built from the name or the number
///   (letters, digits and hyphens, at most nine digits), never from the text around it.</item>
/// <item>Input that Markdig refuses (nested too deeply to be sensible) is shown as plain text in a <c>pre</c>, not as an error.</item>
/// <item>Ids start with the slug of the release, so two releases on one page never share an id and a heading cannot take the id of the page's own elements.</item>
/// </list>
/// The result is cached with the release, which is replaced when the list is read again.
/// </summary>
public sealed class ReleaseBody
{
    private static readonly MarkdownRenderer Renderer = new(allowRawHtml: false);
    private static readonly ConditionalWeakTable<ReleaseEntry, ConcurrentDictionary<(string, int), string>> Cache = new();

    /// <summary>The most renderings kept for one release. The list page, the release page and the feed need three; a request cannot make more, but this keeps it so.</summary>
    private const int MaxVariants = 8;

    private readonly string? _repositoryUrl;

    /// <param name="repositoryUrl">The repository on GitHub (<c>https://github.com/owner/repo</c>), for <c>#123</c>; null leaves references as text.</param>
    public ReleaseBody(string? repositoryUrl) => _repositoryUrl = repositoryUrl;

    /// <summary>
    /// The HTML of the body of <paramref name="release"/>.
    /// </summary>
    /// <param name="release">The release.</param>
    /// <param name="pageHref">The address of the page that shows it, for links to a heading of the body: relative to the base (<c>releases/v1.0</c>), or absolute in a feed.</param>
    /// <param name="minHeadingLevel">The level of the highest heading of the body: 3 under a release title that is an <c>h2</c>, so the headings keep their order.</param>
    public string Render(ReleaseEntry release, string pageHref, int minHeadingLevel)
    {
        if (string.IsNullOrWhiteSpace(release.Body))
        {
            return "";
        }
        var cache = Cache.GetOrCreateValue(release);
        var key = (pageHref, minHeadingLevel);
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }
        var html = RenderSafely(release, pageHref, minHeadingLevel);
        // The address of the page is part of the key, and an address can come from a request: past a few variants, render without keeping.
        if (cache.Count < MaxVariants)
        {
            cache.TryAdd(key, html);
        }
        return html;
    }

    private string RenderSafely(ReleaseEntry release, string pageHref, int minHeadingLevel)
    {
        try
        {
            return Renderer.Render(
                release.Body,
                "",
                pageHref,
                new UntrustedMarkdown(
                    $"rel-{release.Slug}-",
                    MentionUrlFormat: _repositoryUrl is null ? null : "https://github.com/{0}",
                    IssueUrlFormat: _repositoryUrl is null ? null : _repositoryUrl + "/issues/{0}",
                    MinHeadingLevel: minHeadingLevel)).Html;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InsufficientExecutionStackException)
        {
            // Markdig refuses input nested too deeply to be sensible (twenty thousand "[" in a row), and a body from outside can be anything: the notes are
            // then shown as the plain text they are, never as an error page.
            return $"<pre class=\"release-plain\" tabindex=\"0\">{System.Net.WebUtility.HtmlEncode(release.Body)}</pre>";
        }
    }
}
