using Editor.Shared.Data;

namespace Editor.Shared.Services;

/// <summary>What happened to a change that was meant to be kept.</summary>
public enum SaveResult
{
    /// <summary>Written to the store.</summary>
    Saved,

    /// <summary>The document is longer than <see cref="DocumentLimits.MaxHtmlCharacters"/>: the change is not kept.</summary>
    TooLong,

    /// <summary>The storage refused the write (full or blocked): the document lives for this visit only.</summary>
    StorageFailed,
}

/// <summary>
/// The documents of the user session, shared by the sidebar and the pages: the list, and the changes to it (a new document, an edit, a rename, a delete). One per
/// session (scoped). It reads the <see cref="IDocumentStore"/> once, after the first render in the browser, and writes through it on every change. What the
/// store returns is checked here (<see cref="DocumentJson.TryRead"/>), and the bounds of <see cref="DocumentLimits"/> are kept.
/// </summary>
public sealed class DocumentState(IDocumentStore store)
{
    private readonly List<Document> _items = [];
    private Task? _loading;

    /// <summary>The documents, the most recently changed first.</summary>
    public IReadOnlyList<Document> Items => _items;

    public bool Loaded { get; private set; }

    /// <summary>False once a read or a write of the store failed (storage blocked or full): the documents then live for this visit only.</summary>
    public bool StorageAvailable { get; private set; } = true;

    /// <summary>The UTC offset of the user, for the groups of the list.</summary>
    public TimeSpan UtcOffset { get; private set; }

    /// <summary>Raised after the list changed.</summary>
    public event Action? Changed;

    /// <summary>Raised when something asks to rename a document: the dialog of the layout listens.</summary>
    public event Action<Document>? RenameRequested;

    /// <summary>Raised when something asks to delete a document: the dialog of the layout listens.</summary>
    public event Action<Document>? DeleteRequested;

    /// <summary>Raised after a document was deleted, with its id: the page that shows it leaves it.</summary>
    public event Action<string>? Deleted;

    public void RequestRename(Document document) => RenameRequested?.Invoke(document);

    public void RequestDelete(Document document) => DeleteRequested?.Invoke(document);

    public Document? Find(string? id) => id is null ? null : _items.FirstOrDefault(d => d.Id == id);

    /// <summary>Reads the store, once. Safe to call from several components: they all wait for the same read.</summary>
    public Task LoadAsync() => _loading ??= ReadAsync();

    private async Task ReadAsync()
    {
        try
        {
            var data = await store.LoadAsync();
            UtcOffset = TimeSpan.FromMinutes(Math.Clamp(data.UtcOffsetMinutes, -14 * 60, 14 * 60));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unusable = new List<string>();
            foreach (var stored in data.Documents)
            {
                // Kept under its own id and readable: a document. Anything else under our prefix is not ours to keep: it is removed, so it does not fill the storage or the
                // cap on what is read (a value that is too long to be one of ours is the same).
                if (DocumentJson.TryRead(stored.Json, out var document) && document.Id == stored.Id && seen.Add(document.Id))
                {
                    _items.Add(document);
                }
                else
                {
                    unusable.Add(stored.Id);
                }
            }
            _items.Sort(MostRecentFirst);
            // Repair a storage that holds more than the bounds allow (edited by hand, or written by an older version): the least recently used go.
            var excess = new List<Document>();
            while (_items.Count > DocumentLimits.MaxDocuments || TotalCharacters() > DocumentLimits.MaxTotalHtmlCharacters)
            {
                excess.Add(_items[^1]);
                _items.RemoveAt(_items.Count - 1);
            }
            foreach (var id in unusable.Concat(excess.Select(d => d.Id)))
            {
                try
                {
                    await store.DeleteAsync(id);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // The storage is blocked (a private window, a policy): the app works, with nothing kept.
            StorageAvailable = false;
        }
        Loaded = true;
        Changed?.Invoke();
    }

    private static int MostRecentFirst(Document a, Document b) => b.UpdatedAt.CompareTo(a.UpdatedAt);

    private long TotalCharacters() => _items.Sum(d => (long)d.Html.Length);

    /// <summary>Starts a document: it is added at the top of the list (the least recently used ones go when the bounds are reached) and written.</summary>
    public async Task<Document> CreateAsync(string? title = null, string? html = null)
    {
        var document = new Document();
        if (!string.IsNullOrWhiteSpace(title))
        {
            document.Title = CleanTitle(title);
            document.AutoTitle = false;
        }
        document.SetHtml(html);
        if (document.Html.Length > DocumentLimits.MaxHtmlCharacters)
        {
            document.SetHtml("");
        }
        await PersistAsync(document);
        return document;
    }

    /// <summary>
    /// Keeps a change to the text: the HTML is cleaned, and the title follows the first line while the document was never renamed. A text past
    /// <see cref="DocumentLimits.MaxHtmlCharacters"/> is refused and the document stays as it was.
    /// </summary>
    public async Task<SaveResult> UpdateHtmlAsync(Document document, string? html)
    {
        var clean = DocumentHtml.Clean(html);
        if (clean.Length > DocumentLimits.MaxHtmlCharacters)
        {
            return SaveResult.TooLong;
        }
        document.SetHtml(clean);
        if (document.AutoTitle)
        {
            document.Title = Document.TitleFrom(document.Text);
        }
        return await PersistAsync(document) ? SaveResult.Saved : SaveResult.StorageFailed;
    }

    // Moves the document to the top, adds it when it is new, drops what the bounds do not allow (never the document itself) and writes. False when the store refused.
    private async Task<bool> PersistAsync(Document document)
    {
        document.UpdatedAt = DateTimeOffset.UtcNow;
        if (!_items.Contains(document))
        {
            _items.Add(document);
        }
        _items.Sort(MostRecentFirst);
        var evicted = new List<Document>();
        while (_items.Count > 1 && (_items.Count > DocumentLimits.MaxDocuments || TotalCharacters() > DocumentLimits.MaxTotalHtmlCharacters))
        {
            evicted.Add(_items[^1]);
            _items.RemoveAt(_items.Count - 1);
        }
        Changed?.Invoke();
        foreach (var old in evicted)
        {
            Deleted?.Invoke(old.Id);
        }

        try
        {
            await store.SaveAsync(document.Id, DocumentJson.Write(document));
            foreach (var old in evicted)
            {
                await store.DeleteAsync(old.Id);
            }
            return true;
        }
        catch (Exception)
        {
            StorageAvailable = false;
            return false;
        }
    }

    /// <summary>The title as it is kept: on one line, at most <see cref="DocumentLimits.MaxTitle"/> characters. Empty when nothing is left.</summary>
    public static string CleanTitle(string? title)
    {
        var line = System.Text.RegularExpressions.Regex.Replace(title ?? "", @"\s+", " ").Trim();
        return line.Length <= DocumentLimits.MaxTitle ? line : line[..DocumentLimits.MaxTitle].TrimEnd();
    }

    /// <summary>Renames a document, which stops its title from following the text. An empty title changes nothing. Returns false when the storage refused it.</summary>
    public async Task<bool> RenameAsync(Document document, string? title)
    {
        var clean = CleanTitle(title);
        if (clean.Length == 0)
        {
            return true;
        }
        document.Title = clean;
        document.AutoTitle = false;
        return await PersistAsync(document);
    }

    public async Task DeleteAsync(Document document)
    {
        if (!_items.Remove(document))
        {
            return;
        }
        Changed?.Invoke();
        Deleted?.Invoke(document.Id);
        try
        {
            await store.DeleteAsync(document.Id);
        }
        catch (Exception)
        {
            StorageAvailable = false;
        }
    }

    /// <summary>The documents whose title or text holds <paramref name="query"/> (any case); all of them when it is empty.</summary>
    public IReadOnlyList<Document> Search(string? query)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return _items;
        }
        return [.. _items.Where(d => d.Title.Contains(text, StringComparison.OrdinalIgnoreCase) || d.Text.Contains(text, StringComparison.OrdinalIgnoreCase))];
    }
}
