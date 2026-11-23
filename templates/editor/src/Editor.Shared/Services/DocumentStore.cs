namespace Editor.Shared.Services;

/// <summary>A stored document as the store holds it: the id it is kept under and its JSON text, unchecked.</summary>
public sealed record StoredDocument(string Id, string Json);

/// <summary>What a store gives back: the stored documents and the UTC offset of the user in minutes.</summary>
public sealed record StoredData(IReadOnlyList<StoredDocument> Documents, int UtcOffsetMinutes);

/// <summary>
/// Where documents are kept between visits. The web app and the MAUI app keep them in the local storage of the browser (or web view) of the user, which
/// is what keeps visitors apart: nothing of a document is held by the server. Replace it to keep them in a database behind your own sign-in; the pages only
/// use <see cref="DocumentState"/>, which checks everything a store returns and keeps the bounds of <see cref="Data.DocumentLimits"/>.
/// </summary>
public interface IDocumentStore
{
    /// <summary>Reads everything. Throws when the storage cannot be read (blocked by the browser): the state then starts empty.</summary>
    Task<StoredData> LoadAsync();

    /// <summary>Writes one document. Throws when it cannot be written (storage full or blocked).</summary>
    Task SaveAsync(string id, string json);

    Task DeleteAsync(string id);
}
