using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using Blux.Templates.Content;

namespace Portfolio.Web.Site;

/// <summary>A link of a project: "Live site", "Source", "Case study".</summary>
public sealed record ProjectLink(string Label, string Url);

/// <summary>A project of the work page: the page <c>content/work/*.md</c> and the keys of its front matter that the site adds (<c>year</c>, <c>role</c>, <c>stack</c>...).</summary>
public sealed record Project(MarkdownPage Page, int Year, string? Role, string? Client, IReadOnlyList<string> Stack, bool Featured, string? Image, string? ImageAlt, IReadOnlyList<ProjectLink> Links)
{
    public string Title => Page.Title;
    public string? Description => Page.Description;

    /// <summary>The address of the project, relative to the <c>&lt;base href&gt;</c> of the site.</summary>
    public string Href => Page.Href;

    /// <summary>The last part of the address: the file name without <c>.md</c>.</summary>
    public string Name => Page.Slug[(Page.Slug.LastIndexOf('/') + 1)..];
}

/// <summary>An article of the writing page: the page <c>content/writing/*.md</c> and the keys of its front matter that the site adds (<c>date</c>, <c>tags</c>, <c>image</c>).</summary>
public sealed record Article(MarkdownPage Page, DateTimeOffset Date, string? Image, string? ImageAlt, IReadOnlyList<string> Tags)
{
    public string Title => Page.Title;
    public string? Description => Page.Description;

    public string Href => Page.Href;

    public string Name => Page.Slug[(Page.Slug.LastIndexOf('/') + 1)..];

    /// <summary>The reading time, at 200 words a minute (English), never less than one minute.</summary>
    public int ReadingMinutes => Math.Max(1, (int)Math.Ceiling(Page.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length / 200.0));
}

/// <summary>
/// The content folder read as the parts of this site: the projects (<c>content/work</c>), the articles (<c>content/writing</c>), the page about you
/// (<c>content/about.md</c>, which <c>Components/Pages/About.razor</c> lays out) and the plain pages. They live in one folder and are read by one
/// <see cref="MarkdownStore"/>, so that the search covers them all and a link in one reaches a page of another.
/// </summary>
public static class Library
{
    public const string WorkFolder = "work";
    public const string WritingFolder = "writing";

    /// <summary>The slug of the page whose text <c>About.razor</c> shows. It is not a plain page: it has a page of its own.</summary>
    public const string AboutSlug = "about";

    private sealed record Parts(IReadOnlyList<Project> Projects, IReadOnlyList<Article> Articles, MarkdownPage? About, IReadOnlyList<MarkdownPage> Plain);

    // The parts are worked out once for each version of the content (a reload in development makes a new MarkdownSite).
    private static readonly ConditionalWeakTable<MarkdownSite, Parts> Cache = new();

    private static Parts Of(MarkdownSite site) => Cache.GetValue(site, Read);

    /// <summary>The projects, the most recent year first. Throws <see cref="ContentException"/> naming the file when a project has no valid <c>year</c>.</summary>
    public static IReadOnlyList<Project> Projects(this MarkdownSite site) => Of(site).Projects;

    /// <summary>The projects marked <c>featured: true</c>, in the order of <see cref="Projects"/>: the ones the home page shows.</summary>
    public static IReadOnlyList<Project> FeaturedProjects(this MarkdownSite site) => [.. Of(site).Projects.Where(p => p.Featured)];

    /// <summary>The articles, the most recent first. Throws <see cref="ContentException"/> naming the file when an article has no valid <c>date</c>.</summary>
    public static IReadOnlyList<Article> Articles(this MarkdownSite site) => Of(site).Articles;

    /// <summary>The page <c>content/about.md</c>, or null when the site has none.</summary>
    public static MarkdownPage? About(this MarkdownSite site) => Of(site).About;

    /// <summary>
    /// The pages that are not a project, an article or the about page: everything outside <c>work/</c> and <c>writing/</c>, and the files in folders below
    /// them (<c>writing/2026/post.md</c> is a plain page at its own address, not in the list of articles). <c>content/uses.md</c> is one.
    /// </summary>
    public static IReadOnlyList<MarkdownPage> PlainPages(this MarkdownSite site) => Of(site).Plain;

    /// <summary>
    /// What the site checks after it reads the content, to pass to <see cref="ContentOptions.Validate"/>: a project without a valid year, an article
    /// without a valid date, or a cover address that cannot be used, throws <see cref="ContentException"/> naming the file, and the covers that have no
    /// file below <paramref name="webRoot"/> are returned as warnings (the same check as for the images of a body).
    /// </summary>
    public static IEnumerable<string> Validate(MarkdownSite site, string? webRoot)
    {
        var projects = site.Projects();
        var articles = site.Articles();
        if (string.IsNullOrEmpty(webRoot))
        {
            return [];
        }
        bool Missing(string? image) => image is not null && !ContentUrl.IsExternal(image) && !MarkdownSite.ImageExists(webRoot, image.Split('?', '#')[0]);
        return
        [
            .. projects.Where(p => Missing(p.Image))
                .Select(p => $"The project '{p.Page.Slug}' has the cover image '{p.Image}', which is not in the web root. Put the file in wwwroot and write its path from there in the front matter (image: img/work/cover.svg, with or without a leading slash)."),
            .. articles.Where(a => Missing(a.Image))
                .Select(a => $"The article '{a.Page.Slug}' has the cover image '{a.Image}', which is not in the web root. Put the file in wwwroot and write its path from there in the front matter (image: img/writing/cover.svg, with or without a leading slash)."),
        ];
    }

    /// <summary>The tags of the articles, with how many articles have each, the most used first.</summary>
    public static IReadOnlyList<(string Tag, int Count)> Tags(this MarkdownSite site) =>
        [.. site.Articles().SelectMany(a => a.Tags).GroupBy(t => t, StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Count())).OrderByDescending(t => t.Item2).ThenBy(t => t.Key, StringComparer.OrdinalIgnoreCase)];

    private static Parts Read(MarkdownSite site)
    {
        var projects = site.Pages.Where(p => p.Directory == WorkFolder).Select(ToProject).OrderByDescending(p => p.Year).ThenBy(p => p.Page.Order ?? int.MaxValue).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var articles = site.Pages.Where(p => p.Directory == WritingFolder).Select(ToArticle).OrderByDescending(a => a.Date).ThenBy(a => a.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var about = site.Pages.FirstOrDefault(p => string.Equals(p.Slug, AboutSlug, StringComparison.OrdinalIgnoreCase));
        var plain = site.Pages.Where(p => p != about && p.Directory != WorkFolder && p.Directory != WritingFolder).ToList();
        return new Parts(projects, articles, about, plain);
    }

    private static Project ToProject(MarkdownPage page) =>
        new(page, YearOf(page), Text(page, "role"), Text(page, "client"), List(page, "stack"), Flag(page, "featured"), Cover(page), Text(page, "imageAlt"), Links(page));

    private static Article ToArticle(MarkdownPage page) =>
        new(page, DateOf(page), Cover(page), Text(page, "imageAlt"), List(page, "tags"));

    private static string? Text(MarkdownPage page, string key) =>
        page.Data.TryGetValue(key, out var value) && value?.ToString() is { Length: > 0 } text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

    private static bool Flag(MarkdownPage page, string key) => Text(page, key) is { } text && bool.TryParse(text, out var flag) && flag;

    // The cover goes through the same rewrite as the links and images of a body, so that a path from the root (/img/work/a.svg) becomes an address
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

    private static int YearOf(MarkdownPage page)
    {
        var text = Text(page, "year") ?? throw new ContentException($"{page.SourcePath}: the front matter needs a year, written with four digits (year: 2025).");
        return text.Length == 4 && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year >= 1970
            ? year
            : throw new ContentException($"{page.SourcePath}: the year '{text}' is not valid: write it with four digits (year: 2025).");
    }

    private static DateTimeOffset DateOf(MarkdownPage page)
    {
        var text = Text(page, "date") ?? throw new ContentException($"{page.SourcePath}: the front matter needs a date, written yyyy-MM-dd (date: 2026-03-04).");
        return DateTimeOffset.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : throw new ContentException($"{page.SourcePath}: the date '{text}' is not valid: write it yyyy-MM-dd (date: 2026-03-04).");
    }

    // A list, or one word: stack: [C#, Blazor] and stack: Blazor both work. Empty values are dropped.
    private static IReadOnlyList<string> List(MarkdownPage page, string key) =>
        page.Data.TryGetValue(key, out var value)
            ? value switch
            {
                IEnumerable<object> list => [.. list.Select(t => t?.ToString()?.Trim()).Where(t => !string.IsNullOrEmpty(t)).Select(t => t!)],
                string single when !string.IsNullOrWhiteSpace(single) => [single.Trim()],
                _ => [],
            }
            : [];

    // links: a list of { label, url } maps. An entry without both is a mistake that names the file; an address with a scheme has to be http, https or mailto.
    private static IReadOnlyList<ProjectLink> Links(MarkdownPage page)
    {
        if (!page.Data.TryGetValue("links", out var value) || value is null)
        {
            return [];
        }
        if (value is not IEnumerable<object> entries)
        {
            throw new ContentException($"{page.SourcePath}: 'links' is a list of entries with a label and a url.");
        }
        var links = new List<ProjectLink>();
        foreach (var entry in entries)
        {
            if (entry is not IDictionary map)
            {
                throw new ContentException($"{page.SourcePath}: every entry of 'links' needs a label and a url (- label: Live site).");
            }
            string? Get(string name) => map.Contains(name) ? map[name]?.ToString()?.Trim() : null;
            var (label, url) = (Get("label"), Get("url"));
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(url))
            {
                throw new ContentException($"{page.SourcePath}: every entry of 'links' needs a label and a url.");
            }
            if (!ContentUrl.IsAllowed(url))
            {
                throw new ContentException($"{page.SourcePath}: the link '{url}' cannot be used: an address with a scheme has to be http, https or mailto.");
            }
            links.Add(new ProjectLink(label, url));
        }
        return links;
    }
}
