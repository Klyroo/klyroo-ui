using System.Globalization;
using System.Runtime.CompilerServices;
using Blux.Templates.Content;
using Blux.UI;

namespace SaaS.Web.Site;

/// <summary>A post of the blog: the page <c>content/blog/*.md</c> and the keys of its front matter that the blog adds (<c>date</c>, <c>author</c>, <c>tags</c>...).</summary>
public sealed record Post(MarkdownPage Page, DateTimeOffset Date, string? Author, string? AuthorRole, string? Image, string? ImageAlt, IReadOnlyList<string> Tags)
{
    public string Title => Page.Title;
    public string? Description => Page.Description;

    /// <summary>The address of the post, relative to the <c>&lt;base href&gt;</c> of the site.</summary>
    public string Href => Page.Href;

    /// <summary>The last part of the address: the file name without <c>.md</c>.</summary>
    public string Name => Page.Slug[(Page.Slug.LastIndexOf('/') + 1)..];

    /// <summary>The reading time, at 200 words a minute (English), never less than one minute.</summary>
    public int ReadingMinutes => Math.Max(1, (int)Math.Ceiling(Page.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length / 200.0));

    public IReadOnlyList<BxPostAuthor> Authors => Author is null ? [] : [new BxPostAuthor(Author) { Description = AuthorRole }];
}

/// <summary>A version of the changelog: the page <c>content/changelog/*.md</c>, its date and the version it announces.</summary>
public sealed record Release(MarkdownPage Page, DateTimeOffset Date, string Version, string? Badge)
{
    public string Href => Page.Href;
    public string Name => Page.Slug[(Page.Slug.LastIndexOf('/') + 1)..];
}

/// <summary>
/// The content folder read as the parts of this site: the blog posts, the versions of the changelog and the documentation. The three live in one
/// folder (<c>content/docs</c>, <c>content/blog</c>, <c>content/changelog</c>) and are read by one <see cref="MarkdownStore"/>, so that the search covers
/// them all and a link in one reaches a page of another.
/// </summary>
public static class Library
{
    public const string DocsFolder = "docs";
    public const string BlogFolder = "blog";
    public const string ChangelogFolder = "changelog";

    private sealed record Parts(IReadOnlyList<Post> Posts, IReadOnlyList<Release> Releases, IReadOnlyList<MarkdownPage> Docs, IReadOnlyList<BxContentNavigationItem> DocsNavigation);

    // The parts are worked out once for each version of the content (a reload in development makes a new MarkdownSite).
    private static readonly ConditionalWeakTable<MarkdownSite, Parts> Cache = new();

    private static Parts Of(MarkdownSite site) => Cache.GetValue(site, Read);

    /// <summary>The posts of the blog, the most recent first. Throws <see cref="ContentException"/> naming the file when a post has no valid date.</summary>
    public static IReadOnlyList<Post> Posts(this MarkdownSite site) => Of(site).Posts;

    /// <summary>The versions of the changelog, the most recent first.</summary>
    public static IReadOnlyList<Release> Releases(this MarkdownSite site) => Of(site).Releases;

    /// <summary>The pages of the documentation, in the order of its navigation: the order of the previous and next links.</summary>
    public static IReadOnlyList<MarkdownPage> DocsPages(this MarkdownSite site) => Of(site).Docs;

    /// <summary>The tree of the documentation, for <see cref="BxContentNavigation"/>.</summary>
    public static IReadOnlyList<BxContentNavigationItem> DocsNavigation(this MarkdownSite site) => Of(site).DocsNavigation;

    /// <summary>The page before and the page after in the documentation only, not the posts or the versions that follow it in the whole site.</summary>
    public static (BxContentSurroundLink? Previous, BxContentSurroundLink? Next) DocsSurround(this MarkdownSite site, MarkdownPage page)
    {
        var docs = Of(site).Docs;
        var index = -1;
        for (var i = 0; i < docs.Count; i++)
        {
            if (docs[i].Slug == page.Slug)
            {
                index = i;
            }
        }
        if (index < 0)
        {
            return (null, null);
        }
        BxContentSurroundLink? Link(int i) => i >= 0 && i < docs.Count ? new BxContentSurroundLink(docs[i].Title, docs[i].Href) { Description = docs[i].Description } : null;
        return (Link(index - 1), Link(index + 1));
    }

    /// <summary>The tags of the posts, with how many posts have each, the most used first.</summary>
    public static IReadOnlyList<(string Tag, int Count)> Tags(this MarkdownSite site) =>
        [.. site.Posts().SelectMany(p => p.Tags).GroupBy(t => t, StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Count())).OrderByDescending(t => t.Item2).ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)];

    private static Parts Read(MarkdownSite site)
    {
        var posts = site.Pages.Where(p => p.Directory == BlogFolder).Select(ToPost).OrderByDescending(p => p.Date).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var releases = site.Pages.Where(p => p.Directory == ChangelogFolder).Select(ToRelease).OrderByDescending(r => r.Date).ThenByDescending(r => r.Page.Slug, StringComparer.Ordinal).ToList();
        var docs = site.Pages.Where(p => p.Directory == DocsFolder || p.Directory.StartsWith(DocsFolder + "/", StringComparison.Ordinal)).ToList();
        return new Parts(posts, releases, docs, DocsTree(site));
    }

    // The folder "docs" of the content is one group of the navigation tree; the tree of the documentation is what is in it.
    private static IReadOnlyList<BxContentNavigationItem> DocsTree(MarkdownSite site) =>
        [.. site.Navigation.Where(item => FirstHref(item)?.StartsWith(DocsFolder + "/", StringComparison.Ordinal) == true).SelectMany(item => item.Children ?? [])];

    private static string? FirstHref(BxContentNavigationItem item) =>
        item.Href ?? item.Children?.Select(FirstHref).FirstOrDefault(href => href is not null);

    private static Post ToPost(MarkdownPage page) =>
        new(page, DateOf(page), Text(page, "author"), Text(page, "authorRole"), Text(page, "image"), Text(page, "imageAlt"), Tags(page));

    private static Release ToRelease(MarkdownPage page) =>
        new(page, DateOf(page), Text(page, "version") ?? page.Title, Text(page, "badge"));

    private static string? Text(MarkdownPage page, string key) =>
        page.Data.TryGetValue(key, out var value) && value?.ToString() is { Length: > 0 } text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

    private static DateTimeOffset DateOf(MarkdownPage page)
    {
        var text = Text(page, "date") ?? throw new ContentException($"{page.SourcePath}: the front matter needs a date, written yyyy-MM-dd (date: 2026-03-04).");
        return DateTimeOffset.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : throw new ContentException($"{page.SourcePath}: the date '{text}' is not valid: write it yyyy-MM-dd (date: 2026-03-04).");
    }

    private static IReadOnlyList<string> Tags(MarkdownPage page) =>
        page.Data.TryGetValue("tags", out var value)
            ? value switch
            {
                IEnumerable<object> list => [.. list.Select(t => t?.ToString()?.Trim()).Where(t => !string.IsNullOrEmpty(t)).Select(t => t!)],
                string single when !string.IsNullOrWhiteSpace(single) => [single.Trim()],
                _ => [],
            }
            : [];
}
