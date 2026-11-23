using Editor.Shared.Data;
using System.Globalization;

namespace Editor.Shared.Services;

/// <summary>What happened to a change that was meant to be kept.</summary>
public enum SaveResult
{
    /// <summary>Written to the store.</summary>
    Saved,

    /// <summary>The document is longer than <see cref="DocumentLimits.MaxHtmlCharacters"/> (or heavier than <see cref="DocumentLimits.MaxTransferBytes"/> on the wire): the change is not kept.</summary>
    TooLong,

    /// <summary>The storage refused the write (full or blocked): the document lives for this visit only.</summary>
    StorageFailed,

    /// <summary>The document is no longer in the list (deleted, here or in another tab): nothing was written, so a change that was waiting does not bring it back.</summary>
    Gone,
}

/// <summary>What happened when a document's text was asked for.</summary>
public enum OpenResult
{
    /// <summary>The text is in memory.</summary>
    Opened,

    /// <summary>The document is not in the storage any more (deleted in another tab) or could not be read as a document: it is no longer in the list.</summary>
    Gone,

    /// <summary>The storage could not be read.</summary>
    Failed,
}

/// <summary>What the app did to the documents without being asked to, and says so.</summary>
public enum NoticeKind
{
    /// <summary>A save passed the bounds and the least recently used documents were removed (<see cref="StateNotice.Count"/> of them).</summary>
    LimitReached,

    /// <summary>The storage was read and held more than the bounds allow, or things that are not documents: <see cref="StateNotice.Count"/> least recently used documents and <see cref="StateNotice.Unreadable"/> entries were removed.</summary>
    Repaired,

    /// <summary>A document was asked for and could not be read as one: it was removed from the list.</summary>
    Unreadable,

    /// <summary>Another tab deleted <see cref="StateNotice.Count"/> documents, which are gone from the list too.</summary>
    RemovedElsewhere,
}

/// <summary>A change the app made to the documents on its own: the page tells the user.</summary>
public sealed record StateNotice(NoticeKind Kind, int Count, int Unreadable = 0)
{
    private static string Plural(int count, string one, string many) => count == 1 ? $"1 {one}" : $"{count:N0} {many}";

    private static string Limits => $"{DocumentLimits.MaxDocuments} documents and {DocumentLimits.MaxTotalHtmlCharacters:N0} characters in all";

    /// <summary>What to tell the user: a short title and the sentences that say what was removed and why.</summary>
    public (string Title, string Description) Describe() => Kind switch
    {
        NoticeKind.LimitReached => (
            "Older documents were removed",
            $"{Plural(Count, "least recently used document was", "least recently used documents were")} removed: this browser keeps at most {Limits}. Delete what you no longer need to choose what goes."),
        NoticeKind.Repaired => (
            "Storage cleaned up",
            string.Join(' ', new[]
            {
                Count > 0 ? $"{Plural(Count, "least recently used document was", "least recently used documents were")} removed because the storage held more than the limits allow ({Limits})." : null,
                Unreadable > 0 ? $"{Plural(Unreadable, "stored entry that was not a readable document was", "stored entries that were not readable documents were")} removed." : null,
            }.Where(part => part is not null))),
        NoticeKind.Unreadable => ("Document removed", "A document could not be read, so it was removed from the list."),
        NoticeKind.RemovedElsewhere => ("Deleted in another tab", $"{Plural(Count, "document was", "documents were")} deleted in another tab of this browser, and {(Count == 1 ? "is" : "are")} gone from the list."),
        _ => ("Documents changed", "The documents were changed."),
    };
}

/// <summary>
/// The documents of the user session, shared by the sidebar and the pages: the list, and the changes to it (a new document, an edit, a rename, a delete). One per
/// session (scoped). It reads the <see cref="IDocumentStore"/> after the first render in the browser, and writes through it on every change. The list is read light (what the
/// sidebar shows); the text of a document is read when the document is opened (<see cref="OpenAsync"/>). What the store returns is checked here
/// (<see cref="DocumentJson.TryRead"/>), and the bounds of <see cref="DocumentLimits"/> are kept. When another tab changes the storage, the list follows.
/// </summary>
public sealed class DocumentState(IDocumentStore store) : IAsyncDisposable
{
    private readonly List<Document> _items = [];
    private readonly Dictionary<string, Task<OpenResult>> _opening = [];
    private readonly HashSet<string> _writing = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private Task? _loading;
    private IAsyncDisposable? _watch;
    private bool _refreshing;
    private bool _refreshAgain;
    private bool _disposed;

    /// <summary>The documents, the most recently changed first.</summary>
    public IReadOnlyList<Document> Items => _items;

    public bool Loaded { get; private set; }

    /// <summary>False once a read or a write of the store failed (storage blocked or full): the documents then live for this visit only.</summary>
    public bool StorageAvailable { get; private set; } = true;

    /// <summary>The UTC offset of the user, for the groups of the list.</summary>
    public TimeSpan UtcOffset { get; private set; }

    /// <summary>The characters of HTML of every document together: what <see cref="DocumentLimits.MaxTotalHtmlCharacters"/> bounds.</summary>
    public long TotalCharacters => _items.Sum(d => (long)d.HtmlLength);

    /// <summary>True when the documents are close to a bound (<see cref="DocumentLimits.WarnFraction"/> of it): the next ones will push the least recently used out.</summary>
    public bool NearLimit =>
        _items.Count >= DocumentLimits.MaxDocuments * DocumentLimits.WarnFraction || TotalCharacters >= DocumentLimits.MaxTotalHtmlCharacters * DocumentLimits.WarnFraction;

    /// <summary>Raised after the list changed.</summary>
    public event Action? Changed;

    /// <summary>Raised when something asks to rename a document: the dialog of the layout listens.</summary>
    public event Action<Document>? RenameRequested;

    /// <summary>Raised when something asks to delete a document: the dialog of the layout listens.</summary>
    public event Action<Document>? DeleteRequested;

    /// <summary>Raised after a document was deleted, with its id: the page that shows it leaves it.</summary>
    public event Action<string>? Deleted;

    /// <summary>Raised when the app removed documents on its own (the bounds, a repair of the storage, another tab): the layout tells the user.</summary>
    public event Action<StateNotice>? Notice;

    /// <summary>Raised when another tab changed a document of the list: its text in memory is no longer the stored one (<see cref="Document.BodyLoaded"/> is false) and <see cref="OpenAsync"/> reads it again.</summary>
    public event Action<Document>? ChangedElsewhere;

    public void RequestRename(Document document) => RenameRequested?.Invoke(document);

    public void RequestDelete(Document document) => DeleteRequested?.Invoke(document);

    public Document? Find(string? id) => id is null ? null : _items.FirstOrDefault(d => d.Id == id);

    /// <summary>Reads the store, once. Safe to call from several components: they all wait for the same read.</summary>
    public Task LoadAsync() => _loading ??= ReadAsync();

    private async Task ReadAsync()
    {
        try
        {
            var index = await store.LoadIndexAsync();
            UtcOffset = TimeSpan.FromMinutes(Math.Clamp(index.UtcOffsetMinutes, -14 * 60, 14 * 60));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unusable = new List<string>();
            foreach (var entry in index.Entries)
            {
                // Kept under its own id and readable: a document. Anything else under our prefix is not ours to keep: it is removed, so it does not fill the storage or the
                // cap on what is read (a value that is too long to be one of ours, or too heavy to hand to the server, is the same).
                if (TryFromEntry(entry, out var document) && seen.Add(document.Id))
                {
                    _items.Add(document);
                }
                else
                {
                    unusable.Add(entry.Id);
                }
            }
            _items.Sort(MostRecentFirst);
            // Repair a storage that holds more than the bounds allow (edited by hand, or written by an older version): the least recently used go.
            var excess = new List<Document>();
            while (OverBounds())
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
            if (unusable.Count > 0 || excess.Count > 0)
            {
                Notice?.Invoke(new StateNotice(NoticeKind.Repaired, excess.Count, unusable.Count));
            }
            await StartWatchingAsync();
        }
        catch (Exception)
        {
            // The storage is blocked (a private window, a policy): the app works, with nothing kept.
            StorageAvailable = false;
        }
        Loaded = true;
        Changed?.Invoke();
    }

    private async Task StartWatchingAsync()
    {
        try
        {
            _watch = await store.WatchAsync(RefreshAsync);
        }
        catch (Exception)
        {
            // No way to hear about other tabs: the app then learns of their changes when it next reads.
        }
    }

    private static int MostRecentFirst(Document a, Document b) => b.UpdatedAt.CompareTo(a.UpdatedAt);

    private bool OverBounds() => _items.Count > DocumentLimits.MaxDocuments || TotalCharacters > DocumentLimits.MaxTotalHtmlCharacters;

    // What the list knows of a stored document: no text, and nothing that is not checked.
    private static bool TryFromEntry(StoredEntry entry, out Document document)
    {
        document = new Document();
        if (!entry.Usable || !DocumentJson.IsValidId(entry.Id) || entry.InnerId != entry.Id || entry.HtmlLength is < 0 or > DocumentLimits.MaxHtmlCharacters
            || !TryDate(entry.CreatedAt, out var created) || !TryDate(entry.UpdatedAt, out var updated))
        {
            return false;
        }
        document = new Document
        {
            Id = entry.Id,
            Title = DocumentJson.ReadTitle(entry.Title),
            AutoTitle = entry.AutoTitle,
            CreatedAt = DocumentJson.Clamp(created),
            UpdatedAt = DocumentJson.Clamp(updated),
            BodyLoaded = false,
            HtmlLength = entry.HtmlLength,
            Persisted = true,
        };
        return true;
    }

    // A date that is missing is the start of time (and then today, like one that is out of range); one that is there and is not a date means the value is not ours.
    private static bool TryDate(string? text, out DateTimeOffset date)
    {
        date = default;
        return text is null || DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date);
    }

    /// <summary>
    /// Reads the text of a document of the list, once: the sidebar only has its title. Returns <see cref="OpenResult.Gone"/> (and removes it from the list) when the store no longer
    /// holds it or holds something that is not a document. Safe to call from several components: they all wait for the same read.
    /// </summary>
    public Task<OpenResult> OpenAsync(Document document)
    {
        if (document.BodyLoaded)
        {
            return Task.FromResult(OpenResult.Opened);
        }
        if (!_opening.TryGetValue(document.Id, out var reading))
        {
            reading = ReadBodyAsync(document);
            _opening[document.Id] = reading;
        }
        return reading;
    }

    private async Task<OpenResult> ReadBodyAsync(Document document)
    {
        // The caller registers this read after it starts: it must not finish (and unregister) before that.
        await Task.Yield();
        try
        {
            string? json;
            try
            {
                json = await store.LoadAsync(document.Id);
            }
            catch (Exception)
            {
                StorageAvailable = false;
                Changed?.Invoke();
                return OpenResult.Failed;
            }
            if (!DocumentJson.TryRead(json, out var read) || read.Id != document.Id)
            {
                await DropAsync(document, json is not null);
                return OpenResult.Gone;
            }
            document.Title = read.Title;
            document.AutoTitle = read.AutoTitle;
            document.CreatedAt = read.CreatedAt;
            document.UpdatedAt = read.UpdatedAt;
            document.SetHtml(read.Html);
            document.Persisted = true;
            _items.Sort(MostRecentFirst);
            Changed?.Invoke();
            return OpenResult.Opened;
        }
        finally
        {
            _opening.Remove(document.Id);
        }
    }

    // Takes a document the store can no longer give out of the list. When there is something under its key that is not a document, it is removed from the storage too.
    private async Task DropAsync(Document document, bool unreadable)
    {
        if (!_items.Remove(document))
        {
            return;
        }
        Changed?.Invoke();
        Deleted?.Invoke(document.Id);
        if (unreadable)
        {
            try
            {
                await store.DeleteAsync(document.Id);
            }
            catch (Exception)
            {
                StorageAvailable = false;
            }
            Notice?.Invoke(new StateNotice(NoticeKind.Unreadable, 0, 1));
        }
    }

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
        if (document.HtmlLength > DocumentLimits.MaxHtmlCharacters || TooHeavy(document))
        {
            document.SetHtml("");
        }
        await PersistAsync(document);
        return document;
    }

    private static bool TooHeavy(Document document) => DocumentJson.TransferBytes(DocumentJson.Write(document)) > DocumentLimits.MaxTransferBytes;

    /// <summary>
    /// Keeps a change to the text: the HTML is cleaned, and the title follows the first line while the document was never renamed. A text past
    /// <see cref="DocumentLimits.MaxHtmlCharacters"/> (or one that would weigh more than <see cref="DocumentLimits.MaxTransferBytes"/> on its way back from the storage) is refused and the document stays as it was.
    /// </summary>
    public async Task<SaveResult> UpdateHtmlAsync(Document document, string? html)
    {
        if (!_items.Contains(document))
        {
            return SaveResult.Gone;
        }
        var clean = DocumentHtml.Clean(html);
        if (clean.Length > DocumentLimits.MaxHtmlCharacters)
        {
            return SaveResult.TooLong;
        }
        var candidate = new Document { Id = document.Id, Title = document.Title, AutoTitle = document.AutoTitle, CreatedAt = document.CreatedAt, UpdatedAt = document.UpdatedAt };
        candidate.SetHtml(clean);
        if (candidate.AutoTitle)
        {
            candidate.Title = Document.TitleFrom(candidate.Text);
        }
        if (TooHeavy(candidate))
        {
            return SaveResult.TooLong;
        }
        document.Adopt(candidate);
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
        _writing.Add(document.Id);
        try
        {
            if (OverBounds() && StorageAvailable)
            {
                // This tab may not have heard of what another one wrote since it read: look at the store again before deciding what goes, so that a document that was
                // edited elsewhere is not removed as if it had not been used.
                await SyncAsync(document.Id);
            }
            var evicted = new List<Document>();
            while (_items.Count > 1 && OverBounds())
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
                document.Persisted = true;
                foreach (var old in evicted)
                {
                    await store.DeleteAsync(old.Id);
                }
                if (evicted.Count > 0)
                {
                    Notice?.Invoke(new StateNotice(NoticeKind.LimitReached, evicted.Count));
                }
                return true;
            }
            catch (Exception)
            {
                StorageAvailable = false;
                return false;
            }
        }
        finally
        {
            _writing.Remove(document.Id);
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
        if (clean.Length == 0 || !_items.Contains(document))
        {
            return true;
        }
        // Written whole: the text has to be here first, or the rename would save the document without it.
        if (await OpenAsync(document) != OpenResult.Opened)
        {
            return false;
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

    /// <summary>
    /// Brings the list in line with the store: documents another tab added, changed or deleted. A document being written here is left alone, and so is one the store never
    /// held (it lives in this visit only). Called when the store says another tab wrote, and before a save decides what to remove.
    /// </summary>
    public async Task SyncAsync(string? exceptId = null)
    {
        await _syncGate.WaitAsync();
        try
        {
            StoredIndex index;
            try
            {
                index = await store.LoadIndexAsync();
            }
            catch (Exception)
            {
                // Cannot look: what this tab knows stays.
                return;
            }
            Merge(index, exceptId);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private void Merge(StoredIndex index, string? exceptId)
    {
        var stored = new Dictionary<string, Document>(StringComparer.Ordinal);
        foreach (var entry in index.Entries)
        {
            if (TryFromEntry(entry, out var document))
            {
                stored[document.Id] = document;
            }
        }
        var gone = new List<Document>();
        var updated = new List<Document>();
        foreach (var document in _items.ToList())
        {
            if (document.Id == exceptId || _writing.Contains(document.Id))
            {
                stored.Remove(document.Id);
                continue;
            }
            if (!stored.Remove(document.Id, out var current))
            {
                if (document.Persisted)
                {
                    _items.Remove(document);
                    gone.Add(document);
                }
                continue;
            }
            if (current.UpdatedAt != document.UpdatedAt || current.HtmlLength != document.HtmlLength)
            {
                document.Title = current.Title;
                document.AutoTitle = current.AutoTitle;
                document.UpdatedAt = current.UpdatedAt;
                document.HtmlLength = current.HtmlLength;
                document.BodyLoaded = false;
                updated.Add(document);
            }
        }
        _items.AddRange(stored.Values);
        if (gone.Count == 0 && updated.Count == 0 && stored.Count == 0)
        {
            return;
        }
        _items.Sort(MostRecentFirst);
        Changed?.Invoke();
        foreach (var document in gone)
        {
            Deleted?.Invoke(document.Id);
        }
        foreach (var document in updated)
        {
            ChangedElsewhere?.Invoke(document);
        }
        if (gone.Count > 0)
        {
            Notice?.Invoke(new StateNotice(NoticeKind.RemovedElsewhere, gone.Count));
        }
    }

    /// <summary>What the store calls when another tab wrote: reads the store again. Calls that come while a read runs are folded into one more read.</summary>
    public async Task RefreshAsync()
    {
        if (!Loaded || _disposed)
        {
            return;
        }
        if (_refreshing)
        {
            _refreshAgain = true;
            return;
        }
        _refreshing = true;
        try
        {
            do
            {
                _refreshAgain = false;
                await SyncAsync();
            }
            while (_refreshAgain);
        }
        finally
        {
            _refreshing = false;
        }
    }

    /// <summary>
    /// The documents whose title or text holds <paramref name="query"/> (any case); all of them when it is empty. The titles are here; the text of the ones that are not
    /// in memory is searched where the documents are kept, and only their ids come back.
    /// </summary>
    public async Task<IReadOnlyList<Document>> SearchAsync(string? query)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return _items;
        }
        var inText = new HashSet<string>(StringComparer.Ordinal);
        if (StorageAvailable)
        {
            try
            {
                inText.UnionWith(await store.SearchAsync(text));
            }
            catch (Exception)
            {
                // The titles and the documents already in memory are still searched.
            }
        }
        return [.. _items.ToList().Where(d => d.Title.Contains(text, StringComparison.OrdinalIgnoreCase) || inText.Contains(d.Id) || (d.BodyLoaded && d.Text.Contains(text, StringComparison.OrdinalIgnoreCase)))];
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        try
        {
            if (_watch is not null)
            {
                await _watch.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // The browser is gone already: its listener went with it.
        }
        _syncGate.Dispose();
    }
}
