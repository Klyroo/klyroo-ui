namespace Chat.Shared.Services;

/// <summary>What a store gives back: the stored conversations as JSON texts (unchecked), the model last chosen, and the UTC offset of the user in minutes.</summary>
public sealed record StoredData(IReadOnlyList<string> Conversations, string? Model, int UtcOffsetMinutes);

/// <summary>
/// Where conversations are kept between visits. The web app and the MAUI app keep them in the local storage of the browser (or web view) of the user, which
/// is what keeps visitors apart: nothing of a conversation is held by the server. Replace it to keep them in a database behind your own sign-in; the pages only
/// use <see cref="ConversationState"/>, which checks everything a store returns.
/// </summary>
public interface IConversationStore
{
    /// <summary>Reads everything. Throws when the storage cannot be read (blocked by the browser): the state then starts empty.</summary>
    Task<StoredData> LoadAsync();

    /// <summary>Writes one conversation. Throws when it cannot be written (storage full or blocked).</summary>
    Task SaveAsync(string id, string json);

    Task DeleteAsync(string id);

    Task SaveModelAsync(string model);
}
