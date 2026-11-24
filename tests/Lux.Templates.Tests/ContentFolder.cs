using Blux.Templates.Content;

namespace Lux.Templates.Tests;

/// <summary>A temporary content folder: files are written from a path and a text, and the folder is deleted with it.</summary>
internal sealed class ContentFolder : IDisposable
{
    public ContentFolder() => Root = Directory.CreateTempSubdirectory("blux-content-").FullName;

    public string Root { get; }

    public ContentFolder Add(string path, string text)
    {
        var full = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
        return this;
    }

    /// <summary>A page with a title (and an order, when given) in its front matter and a one line body.</summary>
    public ContentFolder Page(string path, string title, int? order = null, string body = "Text.", string extra = "") =>
        Add(path, $"---\ntitle: {title}\n{(order is { } o ? $"order: {o}\n" : "")}{extra}---\n{body}\n");

    public MarkdownSite Load(bool drafts = false) => MarkdownSite.Load(new ContentOptions { Root = Root, IncludeDrafts = drafts });

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A watcher may still hold the folder; the temp folder is cleaned by the system.
        }
    }
}
