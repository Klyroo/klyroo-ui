using System.Globalization;
using System.Runtime.CompilerServices;
using Klyroo.Templates.Content;
using Klyroo.UI;

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

    public IReadOnlyList<KlyPostAuthor> Authors => Author is null ? [] : [new KlyPostAuthor(Author) { Description = AuthorRole }];
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
/// them all and a link in one reaches a page of another. Every other page of the folder (<c>content/legal/terms.md</c>) is a plain page, served by
/// <c>Components/Pages/PlainPage.razor</c> at its own address.
/// </summary>
public static class Library
{
    public const string DocsFolder = "docs";
    public const string BlogFolder = "blog";
    public const string ChangelogFolder = "changelog";

    private sealed record Parts(IReadOnlyList<Post> Posts, IReadOnlyList<Release> Releases, IReadOnlyList<MarkdownPage> Docs, IReadOnlyList<KlyContentNavigationItem> DocsNavigation, IReadOnlyList<MarkdownPage> Plain);

    // The parts are worked out once for each version of the content (a reload in development makes a new MarkdownSite).
    private static readonly ConditionalWeakTable<MarkdownSite, Parts> Cache = new();

    private static Parts Of(MarkdownSite site) => Cache.GetValue(site, Read);

    /// <summary>The posts of the blog, the most recent first. Throws <see cref="ContentException"/> naming the file when a post has no valid date.</summary>
    public static IReadOnlyList<Post> Posts(this MarkdownSite site) => Of(site).Posts;

    /// <summary>The versions of the changelog, the most recent first.</summary>
    public static IReadOnlyList<Release> Releases(this MarkdownSite site) => Of(site).Releases;

    /// <summary>The pages of the documentation, in the order of its navigation: the order of the previous and next links.</summary>
    public static IReadOnlyList<MarkdownPage> DocsPages(this MarkdownSite site) => Of(site).Docs;

    /// <summary>The tree of the documentation, for <see cref="KlyContentNavigation"/>.</summary>
    public static IReadOnlyList<KlyContentNavigationItem> DocsNavigation(this MarkdownSite site) => Of(site).DocsNavigation;

    /// <summary>The page before and the page after in the documentation only, not the posts or the versions that follow it in the whole site.</summary>
    public static (KlyContentSurroundLink? Previous, KlyContentSurroundLink? Next) DocsSurround(this MarkdownSite site, MarkdownPage page)
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
        KlyContentSurroundLink? Link(int i) => i >= 0 && i < docs.Count ? new KlyContentSurroundLink(docs[i].Title, docs[i].Href) { Description = docs[i].Description } : null;
        return (Link(index - 1), Link(index + 1));
    }

    /// <summary>
    /// The pages that are not documentation, a post or a version: everything outside <c>docs/</c> except the files directly in <c>blog/</c> and
    /// <c>changelog/</c>. A file in a folder below <c>blog/</c> (<c>blog/2026/post.md</c>) is a plain page too, not a post: it is served at its own
    /// address and is not in the list of posts.
    /// </summary>
    public static IReadOnlyList<MarkdownPage> PlainPages(this MarkdownSite site) => Of(site).Plain;

    /// <summary>
    /// What the site checks after it reads the content, to pass to <see cref="ContentOptions.Validate"/>: a post or a version without a valid date, or with
    /// a cover address that cannot be used, throws <see cref="ContentException"/> naming the file, and the cover images that have no file below
    /// <paramref name="webRoot"/> are returned as warnings (the same check as for the images of a body).
    /// </summary>
    public static IEnumerable<string> Validate(MarkdownSite site, string? webRoot)
    {
        var posts = site.Posts();
        _ = site.Releases();
        if (string.IsNullOrEmpty(webRoot))
        {
            return [];
        }
        return
        [
            .. posts.Where(p => p.Image is { } image && !ContentUrl.IsExternal(image) && !MarkdownSite.ImageExists(webRoot, image.Split('?', '#')[0]))
                .Select(p => $"The post '{p.Page.Slug}' has the cover image '{p.Image}', which is not in the web root. Put the file in wwwroot and write its path from there in the front matter (image: img/blog/cover.svg, with or without a leading slash)."),
        ];
    }

    /// <summary>The tags of the posts, with how many posts have each, the most used first.</summary>
    public static IReadOnlyList<(string Tag, int Count)> Tags(this MarkdownSite site) =>
        [.. site.Posts().SelectMany(p => p.Tags).GroupBy(t => t, StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Count())).OrderByDescending(t => t.Item2).ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)];

    private static Parts Read(MarkdownSite site)
    {
        var posts = site.Pages.Where(p => p.Directory == BlogFolder).Select(ToPost).OrderByDescending(p => p.Date).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var releases = site.Pages.Where(p => p.Directory == ChangelogFolder).Select(ToRelease).OrderByDescending(r => r.Date).ThenByDescending(r => r.Page.Slug, StringComparer.Ordinal).ToList();
        var docs = site.Pages.Where(p => p.Directory == DocsFolder || p.Directory.StartsWith(DocsFolder + "/", StringComparison.Ordinal)).ToList();
        var plain = site.Pages.Where(p => !docs.Contains(p) && p.Directory != BlogFolder && p.Directory != ChangelogFolder).ToList();
        return new Parts(posts, releases, docs, DocsTree(site), plain);
    }

    // The folder "docs" of the content is one group of the navigation tree; the tree of the documentation is what is in it.
    private static IReadOnlyList<KlyContentNavigationItem> DocsTree(MarkdownSite site) =>
        [.. site.Navigation.Where(item => FirstHref(item)?.StartsWith(DocsFolder + "/", StringComparison.Ordinal) == true).SelectMany(item => item.Children ?? [])];

    private static string? FirstHref(KlyContentNavigationItem item) =>
        item.Href ?? item.Children?.Select(FirstHref).FirstOrDefault(href => href is not null);

    private static Post ToPost(MarkdownPage page) =>
        new(page, DateOf(page), Text(page, "author"), Text(page, "authorRole"), Cover(page), Text(page, "imageAlt"), Tags(page));

    private static Release ToRelease(MarkdownPage page) =>
        new(page, DateOf(page), Text(page, "version") ?? page.Title, Text(page, "badge"));

    private static string? Text(MarkdownPage page, string key) =>
        page.Data.TryGetValue(key, out var value) && value?.ToString() is { Length: > 0 } text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

    // The cover goes through the same rewrite as the links and images of a body, so that a path from the root (/img/blog/a.svg) becomes an address
    // relative to the <base href> and works below a path base. Unlike the images of a body, a path is always from the root of the site, never from the
    // folder of the file. An external cover has to be https.
    private static string? Cover(MarkdownPage page)
    {
        if (Text(page, "image") is not { } image)
        {
            return null;
        }
        if (ContentUrl.IsExternal(image))
        {
            return ContentUrl.HasScheme(image, "https") ? image : throw new ContentException($"{page.SourcePath}: the image '{image}' cannot be used: an address outside the site has to start with https://.");
        }
        return ContentUrl.Rewrite(image, "", out _);
    }

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
