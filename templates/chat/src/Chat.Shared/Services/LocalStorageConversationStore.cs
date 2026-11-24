using Lux.UI;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Chat.Shared.Services;

/// <summary>
/// Keeps the conversations in the local storage of the browser (the web view of the MAUI app), one key per conversation. Local storage belongs to one user of one
/// browser and is shared by every app of the same site, so the keys start with a prefix that carries the path base of the app (<see cref="LxPathScope.CookieName"/>):
/// two apps below one host never read each other's conversations. The server never sees them, which is why visitors of a public demo cannot see each other's.
/// </summary>
public sealed class LocalStorageConversationStore(IJSRuntime js, NavigationManager navigation) : IConversationStore
{
    private const string Module = "./_content/Chat.Shared/host.js";

    // The path base as the server sees it: "/templates/chat/demo" for an app served at https://host/templates/chat/demo/, empty at the root.
    private string Prefix => LxPathScope.CookieName("conversations", new Uri(navigation.BaseUri).AbsolutePath.TrimEnd('/'));

    public async Task<StoredData> LoadAsync()
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        // One call, and a JSON text read by hand: no reflection over a type, so it survives trimming.
        var json = await module.InvokeAsync<string>("loadAll", Prefix);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var conversations = root.GetProperty("conversations").EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String && e.TryGetProperty("json", out var j) && j.ValueKind == JsonValueKind.String)
            .Select(e => new StoredConversation(e.GetProperty("id").GetString()!, e.GetProperty("json").GetString()!)).ToList();
        var model = root.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
        var offset = root.TryGetProperty("utcOffsetMinutes", out var o) && o.ValueKind == JsonValueKind.Number ? o.GetInt32() : 0;
        return new StoredData(conversations, model, offset);
    }

    public async Task SaveAsync(string id, string json)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        await module.InvokeVoidAsync("store", $"{Prefix}:c:{id}", json);
    }

    public async Task DeleteAsync(string id)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        await module.InvokeVoidAsync("remove", $"{Prefix}:c:{id}");
    }

    public async Task SaveModelAsync(string model)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", Module);
        await module.InvokeVoidAsync("store", $"{Prefix}:model", model);
    }
}
