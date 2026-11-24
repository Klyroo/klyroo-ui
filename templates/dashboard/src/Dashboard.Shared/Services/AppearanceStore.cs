using Lux.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dashboard.Shared.Services;

/// <summary>The names of the cookies of the appearance, scoped to the path base of the app like the other cookies of Blux.</summary>
public static class AppearanceCookies
{
    public const string PrimaryKey = "dashboard-primary";
    public const string NeutralKey = "dashboard-neutral";

    /// <summary>The cookie name for a key: the key at the root, and the key with a slug of the path base below one (<see cref="BxPathScope.CookieName"/>).</summary>
    public static string Name(string key, string? pathBase) => BxPathScope.CookieName(key, pathBase);
}

/// <summary>Where the choice of the colors is kept between visits.</summary>
public interface IAppearanceStore
{
    /// <summary>The names of the saved palettes, null for one that was never saved.</summary>
    ValueTask<(string? Primary, string? Neutral)> LoadAsync();

    /// <summary>Saves the names of the palettes.</summary>
    ValueTask SaveAsync(string primary, string neutral);
}

/// <summary>
/// The web: two cookies, so that the server reads them while it renders and the first paint has the colors. The browser writes them (the app has no endpoint for it), for
/// the path of the app (its path base), <c>SameSite=Lax</c>, <c>Secure</c> on https and for 400 days. The values are palette names, checked against the offered ones when read.
/// </summary>
public sealed class CookieAppearanceStore(IJSRuntime js, NavigationManager navigation) : IAppearanceStore
{
    // The path base as the server sees it: "/demo/dashboard" for an app served at https://host/demo/dashboard/, empty at the root.
    private string PathBase => new Uri(navigation.BaseUri).AbsolutePath.TrimEnd('/');

    public async ValueTask<(string? Primary, string? Neutral)> LoadAsync()
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Dashboard.Shared/host.js");
        return (await module.InvokeAsync<string?>("getCookie", AppearanceCookies.Name(AppearanceCookies.PrimaryKey, PathBase)),
            await module.InvokeAsync<string?>("getCookie", AppearanceCookies.Name(AppearanceCookies.NeutralKey, PathBase)));
    }

    public async ValueTask SaveAsync(string primary, string neutral)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Dashboard.Shared/host.js");
        var path = PathBase.Length == 0 ? "/" : PathBase;
        await module.InvokeVoidAsync("setCookie", AppearanceCookies.Name(AppearanceCookies.PrimaryKey, PathBase), primary, path);
        await module.InvokeVoidAsync("setCookie", AppearanceCookies.Name(AppearanceCookies.NeutralKey, PathBase), neutral, path);
    }
}

/// <summary>The MAUI app: there is no request to read a cookie from, so the web view keeps the choice in its local storage.</summary>
public sealed class LocalStorageAppearanceStore(IJSRuntime js) : IAppearanceStore
{
    public async ValueTask<(string? Primary, string? Neutral)> LoadAsync() =>
        (await js.InvokeAsync<string?>("localStorage.getItem", AppearanceCookies.PrimaryKey), await js.InvokeAsync<string?>("localStorage.getItem", AppearanceCookies.NeutralKey));

    public async ValueTask SaveAsync(string primary, string neutral)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AppearanceCookies.PrimaryKey, primary);
        await js.InvokeVoidAsync("localStorage.setItem", AppearanceCookies.NeutralKey, neutral);
    }
}
