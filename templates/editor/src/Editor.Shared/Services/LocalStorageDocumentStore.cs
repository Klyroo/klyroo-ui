using Lux.UI;
using Editor.Shared.Data;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Editor.Shared.Services;

/// <summary>
/// Keeps the documents in the local storage of the browser (the web view of the MAUI app), one key per document. Local storage belongs to one user of one
/// browser and is shared by every app of the same site, so the keys start with a prefix that carries the path base of the app (<see cref="BxPathScope.CookieName"/>):
/// two apps below one host never read each other's documents. The server never sees them, which is why visitors of a public demo cannot see each other's.
/// </summary>
/// <remarks>
/// The browser does the reading: the list is built there (<c>loadIndex</c> in <c>host.js</c>) and only the fields it shows go to the server, a document is handed over when
/// it is opened, and a search runs there and returns ids. In the Server render mode every such answer is a message of the circuit, which is why no message is ever the whole library
/// (see <see cref="DocumentLimits.MaxReceiveMessageBytes"/>).
/// </remarks>
public sealed class LocalStorageDocumentStore(IJSRuntime js, NavigationManager navigation) : IDocumentStore
{
    private const string Module = "./_content/Editor.Shared/host.js";

    // The path base as the server sees it: "/templates/editor/demo" for an app served at https://host/templates/editor/demo/, empty at the root.
    private string Prefix => BxPathScope.CookieName("documents", new Uri(navigation.BaseUri).AbsolutePath.TrimEnd('/'));

    public async Task<StoredIndex> LoadIndexAsync()
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        // A JSON text read by hand: no reflection over a type, so it survives trimming.
        var json = await module.InvokeAsync<string>("loadIndex", Prefix, DocumentJson.MaxJsonCharacters, DocumentLimits.MaxTransferBytes, DocumentLimits.MaxDocumentsRead);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var entries = root.GetProperty("entries").EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Object && Text(e, "id") is not null)
            .Select(e => new StoredEntry(
                Text(e, "id")!,
                e.TryGetProperty("usable", out var usable) && usable.ValueKind == JsonValueKind.True,
                Text(e, "innerId"),
                Text(e, "title"),
                !e.TryGetProperty("autoTitle", out var auto) || auto.ValueKind != JsonValueKind.False,
                Text(e, "createdAt"),
                Text(e, "updatedAt"),
                e.TryGetProperty("htmlLength", out var length) && length.ValueKind == JsonValueKind.Number && length.TryGetInt32(out var characters) ? characters : 0))
            .ToList();
        var offset = root.TryGetProperty("utcOffsetMinutes", out var o) && o.ValueKind == JsonValueKind.Number ? o.GetInt32() : 0;
        return new StoredIndex(entries, offset);
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public async Task<string?> LoadAsync(string id)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        return await module.InvokeAsync<string?>("loadDocument", Prefix, id, DocumentJson.MaxJsonCharacters, DocumentLimits.MaxTransferBytes);
    }

    public async Task<IReadOnlyList<string>> SearchAsync(string query)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        var json = await module.InvokeAsync<string>("searchDocuments", Prefix, query, DocumentJson.MaxJsonCharacters, DocumentLimits.MaxDocumentsRead);
        using var document = JsonDocument.Parse(json);
        return [.. document.RootElement.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!)];
    }

    public async Task SaveAsync(string id, string json)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        await module.InvokeVoidAsync("store", $"{Prefix}:d:{id}", json);
    }

    public async Task DeleteAsync(string id)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        await module.InvokeVoidAsync("remove", $"{Prefix}:d:{id}");
    }

    public async Task<IAsyncDisposable?> WatchAsync(Func<Task> onChanged)
    {
        var watcher = new StorageWatcher(onChanged);
        var reference = DotNetObjectReference.Create(watcher);
        IJSObjectReference? module = null;
        try
        {
            module = await js.InvokeAsync<IJSObjectReference>("import", Module);
            var handle = await module.InvokeAsync<IJSObjectReference>("watchStorage", reference, Prefix);
            return new Watch(handle, module, reference);
        }
        catch
        {
            reference.Dispose();
            if (module is not null) await module.DisposeAsync();
            throw;
        }
    }

    /// <summary>What the browser calls when another tab changed a document.</summary>
    public sealed class StorageWatcher(Func<Task> onChanged)
    {
        [JSInvokable]
        public Task OnStorageChanged() => onChanged();
    }

    private sealed class Watch(IJSObjectReference handle, IJSObjectReference module, IDisposable reference) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                // Removes the storage listener: left in place it would keep calling into a disposed reference.
                await handle.InvokeVoidAsync("dispose");
                await handle.DisposeAsync();
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            finally
            {
                reference.Dispose();
            }
        }
    }
}
