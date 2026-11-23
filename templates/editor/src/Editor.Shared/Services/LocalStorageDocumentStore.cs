using Blux.UI;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Editor.Shared.Services;

/// <summary>
/// Keeps the documents in the local storage of the browser (the web view of the MAUI app), one key per document. Local storage belongs to one user of one
/// browser and is shared by every app of the same site, so the keys start with a prefix that carries the path base of the app (<see cref="BxPathScope.CookieName"/>):
/// two apps below one host never read each other's documents. The server never sees them, which is why visitors of a public demo cannot see each other's.
/// </summary>
public sealed class LocalStorageDocumentStore(IJSRuntime js, NavigationManager navigation) : IDocumentStore
{
    private const string Module = "./_content/Editor.Shared/host.js";

    // The path base as the server sees it: "/templates/editor/demo" for an app served at https://host/templates/editor/demo/, empty at the root.
    private string Prefix => BxPathScope.CookieName("documents", new Uri(navigation.BaseUri).AbsolutePath.TrimEnd('/'));

    public async Task<StoredData> LoadAsync()
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        // One call, and a JSON text read by hand: no reflection over a type, so it survives trimming.
        var json = await module.InvokeAsync<string>("loadAll", Prefix, Data.DocumentJson.MaxJsonCharacters);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var documents = root.GetProperty("documents").EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String && e.TryGetProperty("json", out var j) && j.ValueKind == JsonValueKind.String)
            .Select(e => new StoredDocument(e.GetProperty("id").GetString()!, e.GetProperty("json").GetString()!)).ToList();
        var offset = root.TryGetProperty("utcOffsetMinutes", out var o) && o.ValueKind == JsonValueKind.Number ? o.GetInt32() : 0;
        return new StoredData(documents, offset);
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
}
