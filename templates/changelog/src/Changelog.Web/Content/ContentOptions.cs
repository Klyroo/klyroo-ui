namespace Blux.Templates.Content;

/// <summary>How a content folder is read and rendered.</summary>
public sealed class ContentOptions
{
    /// <summary>The folder that holds the <c>.md</c> files and the <c>_dir.yml</c> files. It must exist.</summary>
    public required string Root { get; init; }

    /// <summary>
    /// Lets raw HTML in Markdown through to the page, untouched. Off by default: HTML in a page is shown as text. Turn it on only for content
    /// that nobody but you can write, because a raw <c>&lt;script&gt;</c> or <c>onclick</c> in a page then runs for every visitor.
    /// Links and images written in Markdown keep following the scheme rules either way.
    /// </summary>
    public bool AllowRawHtml { get; init; }

    /// <summary>Includes the pages marked <c>draft: true</c>. Off by default, so a draft is not served, listed, searched or linked.</summary>
    public bool IncludeDrafts { get; init; }

    /// <summary>Reloads the content when a file changes. For development: a published site reads its files once.</summary>
    public bool Watch { get; init; }

    /// <summary>
    /// The folder the site serves files from (<c>wwwroot</c>). Images are served from there, not from the content folder, so an image written in
    /// Markdown with a path that has no file in it is reported (<see cref="MarkdownSite.MissingImages"/>). Left out, images are not checked.
    /// </summary>
    public string? WebRoot { get; init; }

    /// <summary>
    /// A check that a site makes of the content it just read, for what only the site knows (a blog post needs a date). It gets the new
    /// <see cref="MarkdownSite"/> and returns warnings, which the store logs, or throws <see cref="ContentException"/> for content that cannot be used.
    /// It runs at every read, so a reload that fails it keeps the previous content and logs the error, exactly as a page that does not parse does,
    /// and the first read stops the start with the message.
    /// </summary>
    public Func<MarkdownSite, IEnumerable<string>>? Validate { get; init; }

    /// <summary>How many characters of a page's text go into the search index, after its title and headings. The index is a JSON file the browser fetches when the search opens.</summary>
    public int SearchTextLimit { get; init; } = 2000;
}
