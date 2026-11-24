using Lux.UI;

namespace Lux.Templates.Content;

/// <summary>A page of the content folder, rendered.</summary>
/// <param name="Slug">The address of the page below the site, without a leading slash: <c>guides/setup</c> for <c>guides/setup.md</c>, the folder for an <c>index.md</c>.</param>
/// <param name="Directory">The folder of the file, as slugs ("" for the root of the content folder).</param>
/// <param name="Meta">Every key of the front matter as written (see <see cref="FrontMatter.Meta"/>), for the data a site adds of its own. Empty when there is none.</param>
public sealed record MarkdownPage(
    string Slug,
    string Directory,
    bool IsIndex,
    string Title,
    string? Description,
    int? Order,
    string? NavigationTitle,
    string? Icon,
    bool Draft,
    string Html,
    IReadOnlyList<MarkdownHeading> Headings,
    string Text,
    string SourcePath,
    IReadOnlyDictionary<string, object?>? Meta = null)
{
    /// <summary>The address to link to: relative to the <c>&lt;base href&gt;</c> of the site, so it is right below a path base too.</summary>
    public string Href => Slug;

    /// <summary>The front matter keys as written; never null.</summary>
    public IReadOnlyDictionary<string, object?> Data => Meta ?? EmptyMeta;

    private static readonly IReadOnlyDictionary<string, object?> EmptyMeta = new Dictionary<string, object?>();

    /// <summary>The text of the link in the navigation.</summary>
    public string LinkTitle => NavigationTitle ?? Title;

    /// <summary>The table of contents: the <c>h2</c> and <c>h3</c> headings, with the ids that <see cref="LxContentToc"/> links to.</summary>
    public IReadOnlyList<LxContentTocLink> Toc =>
        [.. Headings.Where(h => h.Level is 2 or 3).Select(h => new LxContentTocLink(h.Id, h.Text) { Level = h.Level })];
}

/// <summary>A link of a page to a page that does not exist.</summary>
public sealed record BrokenLink(string FromSlug, string Target);

/// <summary>An image of a page with no file at its path below the web root.</summary>
public sealed record MissingImage(string FromSlug, string Path);

/// <summary>
/// The whole content folder, read once: the pages, the navigation, the search index and the previous and next page of each page. It does not change after
/// it is built; <see cref="MarkdownStore"/> builds a new one when a file changes.
/// </summary>
public sealed class MarkdownSite
{
    private readonly Dictionary<string, MarkdownPage> _bySlug;
    private readonly Dictionary<string, int> _position;

    private MarkdownSite(IReadOnlyList<MarkdownPage> pages, IReadOnlyList<LxContentNavigationItem> navigation, IReadOnlyList<LxContentSearchPage> search, IReadOnlyList<BrokenLink> brokenLinks, IReadOnlyList<MissingImage> missingImages)
    {
        Pages = pages;
        Navigation = navigation;
        Search = search;
        BrokenLinks = brokenLinks;
        MissingImages = missingImages;
        _bySlug = pages.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);
        _position = pages.Select((p, i) => (p.Slug, i)).ToDictionary(x => x.Slug, x => x.i, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The pages in the order of the navigation, which is the order of previous and next.</summary>
    public IReadOnlyList<MarkdownPage> Pages { get; }

    /// <summary>The tree for <see cref="LxContentNavigation"/>: a folder is a group, a page is a link.</summary>
    public IReadOnlyList<LxContentNavigationItem> Navigation { get; }

    /// <summary>The pages and their headings for <see cref="LxContentSearch"/>: titles, headings and the start of each text.</summary>
    public IReadOnlyList<LxContentSearchPage> Search { get; }

    /// <summary>Links between pages that lead nowhere. Not an error: the store logs them.</summary>
    public IReadOnlyList<BrokenLink> BrokenLinks { get; }

    /// <summary>
    /// Images that have no file below <see cref="ContentOptions.WebRoot"/>. A relative image path is relative to the folder of the Markdown file, but
    /// images are served from the web root only, so an image placed next to its page is not found: put it in the web root and write its path from there
    /// (<c>/img/chart.png</c>). Empty when no web root is given.
    /// </summary>
    public IReadOnlyList<MissingImage> MissingImages { get; }

    /// <summary>Finds a page by its address, ignoring case and a leading or trailing slash. Null when there is none.</summary>
    public MarkdownPage? Find(string? slug) => _bySlug.GetValueOrDefault((slug ?? "").Trim('/'));

    /// <summary>The page before and the page after, in the order of the navigation. Either is null at the ends.</summary>
    public (LxContentSurroundLink? Previous, LxContentSurroundLink? Next) Surround(MarkdownPage page)
    {
        if (!_position.TryGetValue(page.Slug, out var index))
        {
            return (null, null);
        }
        LxContentSurroundLink? Link(int i) => i >= 0 && i < Pages.Count ? new LxContentSurroundLink(Pages[i].Title, Pages[i].Href) { Description = Pages[i].Description } : null;
        return (Link(index - 1), Link(index + 1));
    }

    /// <summary>Reads a content folder. Throws <see cref="ContentException"/> naming the file when one cannot be used.</summary>
    public static MarkdownSite Load(ContentOptions options, MarkdownRenderer? renderer = null)
    {
        if (!System.IO.Directory.Exists(options.Root))
        {
            throw new ContentException($"The content folder '{options.Root}' does not exist. Create it and put .md files in it.");
        }
        renderer ??= new MarkdownRenderer(options.AllowRawHtml);
        var root = Path.GetFullPath(options.Root);

        var folders = new Dictionary<string, DirectoryMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Walk(root, "_dir.yml"))
        {
            var folder = FolderSlug(root, Path.GetDirectoryName(file)!);
            if (folder.Length > 0)
            {
                folders[folder] = FrontMatterParser.ParseDirectory(File.ReadAllText(file), Relative(root, file));
            }
        }

        var pages = new List<MarkdownPage>();
        var firstFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var internalLinks = new List<(string From, string Target)>();
        var images = new List<(string From, string Path)>();
        foreach (var file in Walk(root, "*.md").Order(StringComparer.Ordinal))
        {
            var relative = Relative(root, file);
            var (data, body) = FrontMatterParser.Parse(File.ReadAllText(file), relative);
            if (data.Draft && !options.IncludeDrafts)
            {
                continue;
            }

            var directory = FolderSlug(root, Path.GetDirectoryName(file)!);
            var name = Path.GetFileNameWithoutExtension(file);
            var isIndex = name.Equals("index", StringComparison.OrdinalIgnoreCase);
            var segment = ContentUrl.Segment(name);
            var slug = isIndex ? directory : directory.Length == 0 ? segment : directory + "/" + segment;
            if (slug.Length == 0)
            {
                // content/index.md would be the home page, which the site writes itself.
                continue;
            }
            if (segment.Length == 0)
            {
                throw new ContentException($"{relative}: the file name has no letter or digit to make an address from.");
            }
            if (firstFile.TryGetValue(slug, out var other))
            {
                throw new ContentException($"{relative} and {other} both make the address '{slug}'. Rename one of them.");
            }
            firstFile[slug] = relative;

            var rendered = renderer.Render(body, directory, slug);
            var page = new MarkdownPage(
                slug, directory, isIndex,
                Clean(data.Title) ?? Humanize(isIndex ? directory.Split('/')[^1] : name),
                Clean(data.Description), data.Order, Clean(data.NavigationTitle), Clean(data.Icon), data.Draft,
                rendered.Html, rendered.Headings, rendered.Text, relative, data.Meta);
            pages.Add(page);
            internalLinks.AddRange(rendered.Links.Select(target => (slug, target)));
            images.AddRange(rendered.Images.Select(path => (slug, path)));
        }

        var tree = BuildTree(pages, folders);
        var ordered = new List<MarkdownPage>();
        var navigation = ToItems(tree, ordered);
        var known = ordered.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var broken = internalLinks.Where(l => l.Target.Length > 0 && !known.Contains(l.Target)).Distinct().Select(l => new BrokenLink(l.From, l.Target)).ToList();
        var missing = options.WebRoot is { Length: > 0 } webRoot
            ? images.Where(i => !ImageExists(webRoot, i.Path)).Distinct().Select(i => new MissingImage(i.From, i.Path)).ToList()
            : [];
        return new MarkdownSite(ordered, navigation, ToSearch(ordered, folders, options.SearchTextLimit), broken, missing);
    }

    /// <summary>True when <paramref name="path"/> (relative to the web root, with or without a leading slash) is a file inside <paramref name="webRoot"/>: a path that climbs out of it never counts.</summary>
    public static bool ImageExists(string webRoot, string path)
    {
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(path);
        }
        catch (UriFormatException)
        {
            decoded = path;
        }
        var root = Path.GetFullPath(webRoot);
        var full = Path.GetFullPath(Path.Combine(root, decoded.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(full);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>"getting-started" as "Getting started".</summary>
    public static string Humanize(string name)
    {
        var words = name.Replace('-', ' ').Replace('_', ' ').Trim();
        return words.Length == 0 ? name : char.ToUpperInvariant(words[0]) + words[1..];
    }

    private static IEnumerable<string> Walk(string folder, string pattern)
    {
        foreach (var file in System.IO.Directory.EnumerateFiles(folder, pattern))
        {
            yield return file;
        }
        foreach (var sub in System.IO.Directory.EnumerateDirectories(folder).Order(StringComparer.Ordinal))
        {
            if (!Path.GetFileName(sub).StartsWith('.'))
            {
                foreach (var file in Walk(sub, pattern))
                {
                    yield return file;
                }
            }
        }
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string FolderSlug(string root, string folder) =>
        string.Join('/', Relative(root, folder).Split('/', StringSplitOptions.RemoveEmptyEntries).Where(s => s != ".").Select(ContentUrl.Segment));

    // ---- Navigation

    private sealed class Folder(string slug)
    {
        public string Slug { get; } = slug;
        public DirectoryMetadata Metadata { get; set; } = new();
        public List<Folder> Folders { get; } = [];
        public List<MarkdownPage> Pages { get; } = [];
        public string Title => Metadata.Title is { Length: > 0 } title ? title : Humanize(Slug.Split('/')[^1]);
    }

    private static Folder BuildTree(List<MarkdownPage> pages, Dictionary<string, DirectoryMetadata> metadata)
    {
        var root = new Folder("");
        var byPath = new Dictionary<string, Folder>(StringComparer.OrdinalIgnoreCase) { [""] = root };

        Folder Ensure(string path)
        {
            if (byPath.TryGetValue(path, out var existing))
            {
                return existing;
            }
            var parent = Ensure(path.Contains('/') ? path[..path.LastIndexOf('/')] : "");
            var folder = new Folder(path) { Metadata = metadata.GetValueOrDefault(path) ?? new DirectoryMetadata() };
            parent.Folders.Add(folder);
            return byPath[path] = folder;
        }

        foreach (var page in pages)
        {
            Ensure(page.Directory).Pages.Add(page);
        }
        return root;
    }

    private abstract record Entry(int Order, string Title, string Slug);

    private sealed record PageEntry(MarkdownPage Page) : Entry(Page.IsIndex ? Page.Order ?? int.MinValue : Page.Order ?? int.MaxValue, Page.LinkTitle, Page.Slug);

    private sealed record FolderEntry(Folder Folder) : Entry(Folder.Metadata.Order ?? int.MaxValue, Folder.Title, Folder.Slug);

    // Pages and folders next to each other are sorted together: by order (the ones without come last), then by title, then by address.
    private static List<Entry> Entries(Folder folder) =>
    [
        .. folder.Pages.Select(p => (Entry)new PageEntry(p))
            .Concat(folder.Folders.Select(f => new FolderEntry(f)))
            .OrderBy(e => e.Order).ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Slug, StringComparer.Ordinal),
    ];

    private static List<LxContentNavigationItem> ToItems(Folder folder, List<MarkdownPage> ordered)
    {
        var items = new List<LxContentNavigationItem>();
        foreach (var entry in Entries(folder))
        {
            switch (entry)
            {
                case PageEntry { Page: var page }:
                    ordered.Add(page);
                    items.Add(new LxContentNavigationItem(page.LinkTitle) { Href = page.Href, Icon = page.Icon, Exact = true });
                    break;
                case FolderEntry { Folder: var child }:
                    var children = ToItems(child, ordered);
                    if (children.Count > 0)
                    {
                        items.Add(new LxContentNavigationItem(child.Title) { Icon = child.Metadata.Icon, Children = children });
                    }
                    break;
            }
        }
        return items;
    }

    // ---- Search

    private static List<LxContentSearchPage> ToSearch(List<MarkdownPage> pages, Dictionary<string, DirectoryMetadata> folders, int textLimit) =>
    [
        .. pages.Select(page =>
        {
            var top = page.Directory.Length == 0 ? null : page.Directory.Split('/')[0];
            string? group = null;
            if (top is not null)
            {
                group = folders.TryGetValue(top, out var meta) && meta.Title is { Length: > 0 } title ? title : Humanize(top);
            }
            return new LxContentSearchPage(page.Title, page.Href)
            {
                Description = page.Description,
                Group = group,
                Keywords = Snippet(page.Text, textLimit),
                Headings = [.. page.Headings.Where(h => h.Level is 2 or 3).Select(h => new LxContentSearchHeading(h.Text, h.Id))],
            };
        }),
    ];

    private static string? Snippet(string text, int limit)
    {
        if (limit <= 0 || text.Length == 0)
        {
            return null;
        }
        if (text.Length <= limit)
        {
            return text;
        }
        // Cut at a word, not in the middle of one.
        var cut = text.LastIndexOf(' ', limit);
        return text[..(cut > 0 ? cut : limit)];
    }
}
