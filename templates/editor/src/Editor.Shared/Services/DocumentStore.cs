namespace Editor.Shared.Services;

/// <summary>
/// What a store tells about a stored document without its text: the id it is kept under and the few fields the list shows. Unchecked: <see cref="DocumentState"/> validates it.
/// <see cref="Usable"/> is false for something that is under the prefix of the app but is not a document it could read (not JSON, too long, over what the wire carries).
/// </summary>
public sealed record StoredEntry(string Id, bool Usable, string? InnerId = null, string? Title = null, bool AutoTitle = true, string? CreatedAt = null, string? UpdatedAt = null, int HtmlLength = 0);

/// <summary>What a store gives back to fill the list: the entries, and the UTC offset of the user in minutes.</summary>
public sealed record StoredIndex(IReadOnlyList<StoredEntry> Entries, int UtcOffsetMinutes);

/// <summary>
/// Where documents are kept between visits. The web app and the MAUI app keep them in the local storage of the browser (or web view) of the user, which
/// is what keeps visitors apart: nothing of a document is held by the server. Replace it to keep them in a database behind your own sign-in; the pages only
/// use <see cref="DocumentState"/>, which checks everything a store returns and keeps the bounds of <see cref="Data.DocumentLimits"/>.
/// </summary>
/// <remarks>
/// The list is read light (<see cref="LoadIndexAsync"/>: no text) and the text of a document only when it is opened (<see cref="LoadAsync"/>), so what crosses from the
/// browser to the server is one document at a time, never the whole library.
/// </remarks>
public interface IDocumentStore
{
    /// <summary>Reads the list: every stored document without its text. Throws when the storage cannot be read (blocked by the browser): the state then starts empty.</summary>
    Task<StoredIndex> LoadIndexAsync();

    /// <summary>Reads one document: its JSON, or null when it is not there (or is not one the app could read). Throws when the storage cannot be read.</summary>
    Task<string?> LoadAsync(string id);

    /// <summary>The ids of the documents whose text holds <paramref name="query"/>, in any case. The titles are searched by the state, which has them.</summary>
    Task<IReadOnlyList<string>> SearchAsync(string query);

    /// <summary>Writes one document. Throws when it cannot be written (storage full or blocked).</summary>
    Task SaveAsync(string id, string json);

    Task DeleteAsync(string id);

    /// <summary>
    /// Calls <paramref name="onChanged"/> whenever a document is changed by someone else (another tab of the same browser, for the local storage), and returns what stops it. A store
    /// that cannot know returns null, and the app then only learns of other changes when it next reads.
    /// </summary>
    Task<IAsyncDisposable?> WatchAsync(Func<Task> onChanged) => Task.FromResult<IAsyncDisposable?>(null);
}
