using Chat.Shared.Data;

namespace Chat.Shared.Services;

/// <summary>
/// The conversations of the user session, shared by the sidebar and the page: the list, and the changes to it (a new conversation after its first question, a
/// rename, a delete). One per session (scoped). It reads the <see cref="IConversationStore"/> once, after the first render in the browser, and writes through it on
/// every change. What the store returns is checked here (<see cref="ConversationJson.TryRead"/>), and the bounds of <see cref="ConversationLimits"/> are kept.
/// </summary>
public sealed class ConversationState(IConversationStore store)
{
    private readonly List<Conversation> _items = [];
    private Task? _loading;

    /// <summary>The conversations that have at least one message, the most recently used first.</summary>
    public IReadOnlyList<Conversation> Items => _items;

    public bool Loaded { get; private set; }

    /// <summary>False once a read or a write of the store failed (storage blocked or full): the conversations then live for this visit only.</summary>
    public bool StorageAvailable { get; private set; } = true;

    /// <summary>The UTC offset of the user, for the groups of the list.</summary>
    public TimeSpan UtcOffset { get; private set; }

    /// <summary>The model chosen last, or null.</summary>
    public string? Model { get; private set; }

    /// <summary>Raised after the list changed.</summary>
    public event Action? Changed;

    /// <summary>Raised when something asks to rename a conversation: the dialog of the layout listens.</summary>
    public event Action<Conversation>? RenameRequested;

    /// <summary>Raised when something asks to delete a conversation: the dialog of the layout listens.</summary>
    public event Action<Conversation>? DeleteRequested;

    /// <summary>Raised after a conversation was deleted, with its id: the page that shows it leaves it.</summary>
    public event Action<string>? Deleted;

    public void RequestRename(Conversation conversation) => RenameRequested?.Invoke(conversation);

    public void RequestDelete(Conversation conversation) => DeleteRequested?.Invoke(conversation);

    public Conversation? Find(string? id) => id is null ? null : _items.FirstOrDefault(c => c.Id == id);

    /// <summary>Reads the store, once. Safe to call from several components: they all wait for the same read.</summary>
    public Task LoadAsync() => _loading ??= ReadAsync();

    private async Task ReadAsync()
    {
        try
        {
            var data = await store.LoadAsync();
            UtcOffset = TimeSpan.FromMinutes(Math.Clamp(data.UtcOffsetMinutes, -14 * 60, 14 * 60));
            Model = string.IsNullOrWhiteSpace(data.Model) || data.Model.Length > 100 ? null : data.Model;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unusable = new List<string>();
            foreach (var stored in data.Conversations)
            {
                // Kept under its own id and readable: a conversation. Anything else under our prefix is not ours to keep: it is removed, so it does not fill the storage or the
                // cap on what is read (a conversation that was never written, only a key, has no messages and goes too).
                if (ConversationJson.TryRead(stored.Json, out var conversation) && conversation.Id == stored.Id && conversation.Messages.Count > 0 && seen.Add(conversation.Id))
                {
                    _items.Add(conversation);
                }
                else
                {
                    unusable.Add(stored.Id);
                }
            }
            foreach (var id in unusable)
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
            _items.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
            if (_items.Count > ConversationLimits.MaxConversations)
            {
                _items.RemoveRange(ConversationLimits.MaxConversations, _items.Count - ConversationLimits.MaxConversations);
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

    /// <summary>
    /// Keeps a conversation: adds it to the list when it is new (the oldest one goes when the list is full), moves it to the top and writes it. Returns false when the
    /// storage refused it; the conversation stays in the list for this visit.
    /// </summary>
    public async Task<bool> SaveAsync(Conversation conversation)
    {
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        var evicted = new List<Conversation>();
        if (!_items.Contains(conversation))
        {
            _items.Add(conversation);
        }
        _items.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
        while (_items.Count > ConversationLimits.MaxConversations)
        {
            evicted.Add(_items[^1]);
            _items.RemoveAt(_items.Count - 1);
        }
        Changed?.Invoke();

        var ok = true;
        try
        {
            await store.SaveAsync(conversation.Id, ConversationJson.Write(conversation));
            foreach (var old in evicted)
            {
                await store.DeleteAsync(old.Id);
            }
        }
        catch (Exception)
        {
            ok = false;
            StorageAvailable = false;
        }
        return ok;
    }

    /// <summary>The title as it is kept: on one line, at most <see cref="ConversationLimits.MaxTitle"/> characters. Empty when nothing is left.</summary>
    public static string CleanTitle(string? title)
    {
        var line = System.Text.RegularExpressions.Regex.Replace(title ?? "", @"\s+", " ").Trim();
        return line.Length <= ConversationLimits.MaxTitle ? line : line[..ConversationLimits.MaxTitle].TrimEnd();
    }

    /// <summary>Renames a conversation. An empty title changes nothing. Returns false when the storage refused it.</summary>
    public async Task<bool> RenameAsync(Conversation conversation, string? title)
    {
        var clean = CleanTitle(title);
        if (clean.Length == 0)
        {
            return true;
        }
        conversation.Title = clean;
        return await SaveAsync(conversation);
    }

    public async Task DeleteAsync(Conversation conversation)
    {
        if (!_items.Remove(conversation))
        {
            return;
        }
        Changed?.Invoke();
        Deleted?.Invoke(conversation.Id);
        try
        {
            await store.DeleteAsync(conversation.Id);
        }
        catch (Exception)
        {
            StorageAvailable = false;
        }
    }

    /// <summary>Remembers the model the user chose, for the next new conversation.</summary>
    public async Task SetModelAsync(string model)
    {
        Model = model;
        try
        {
            await store.SaveModelAsync(model);
        }
        catch (Exception)
        {
            StorageAvailable = false;
        }
    }

    /// <summary>The conversations whose title or text holds <paramref name="query"/> (any case); all of them when it is empty.</summary>
    public IReadOnlyList<Conversation> Search(string? query)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return _items;
        }
        return [.. _items.Where(c => c.Title.Contains(text, StringComparison.OrdinalIgnoreCase) || c.Messages.Any(m => m.Content.Contains(text, StringComparison.OrdinalIgnoreCase)))];
    }
}
