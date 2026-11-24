using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Lux.UI;

/// <summary>
/// Where a Blazor Hybrid app keeps the culture the user chose (<see cref="LxLocaleSelect"/> writes it, <see cref="LxApp"/> reads it).
/// A browser app keeps it in the ASP.NET Core culture cookie instead and never uses this. Register your own implementation
/// (<c>services.AddSingleton&lt;ILxCultureStore, PreferencesCultureStore&gt;()</c>) to keep it in <c>Preferences</c>, a file or a
/// server; the default is the web view's <c>localStorage</c>.
/// </summary>
/// <remarks>
/// The culture of a .NET process comes from the device and is read once at start, so a new choice only takes full effect when the app
/// starts again, unless <see cref="LxLocaleSelect.Reload"/> is on. A MAUI app can apply the stored culture even earlier, in
/// <c>MauiProgram</c>, before the first page: read the value from the same place and set <c>CultureInfo.DefaultThreadCurrentCulture</c> and
/// <c>DefaultThreadCurrentUICulture</c>.
/// </remarks>
public interface ILxCultureStore
{
    /// <summary>The stored culture name (<c>fr</c>, <c>pt-BR</c>), or null when none was chosen.</summary>
    ValueTask<string?> GetAsync();

    /// <summary>Remembers the culture name the user chose.</summary>
    ValueTask SetAsync(string culture);
}

/// <summary>The default <see cref="ILxCultureStore"/>: the <c>localStorage</c> of the web view, under the key <c>lux-culture</c>.</summary>
public sealed class LxLocalStorageCultureStore(IJSRuntime js) : ILxCultureStore
{
    /// <summary>The <c>localStorage</c> key.</summary>
    public const string Key = "lux-culture";

    /// <inheritdoc />
    public async ValueTask<string?> GetAsync()
    {
        try
        {
            return await js.InvokeAsync<string?>("Lux.getStoredCulture", Key);
        }
        catch (Exception e) when (e is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // The browser side of Lux is not loaded (yet), or the circuit is gone: nothing stored.
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(string culture)
    {
        try
        {
            await js.InvokeVoidAsync("Lux.setStoredCulture", Key, culture);
        }
        catch (Exception e) when (e is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Not stored: the choice lasts until the app restarts.
        }
    }
}

public static class LxCultureStoreExtensions
{
    /// <summary>
    /// Applies the culture of <see cref="ILxCultureStore"/> to the process (<c>CultureInfo.DefaultThreadCurrentCulture</c> and the
    /// current thread), for a Blazor Hybrid app that wants it before the first render. <see cref="LxApp"/> does it by itself in a web view,
    /// so this is only for an app that reads the store from its own startup code. Returns the culture applied, or null when the store
    /// is empty or holds a culture that is not supported.
    /// </summary>
    /// <param name="services">The service provider of the app.</param>
    /// <param name="supportedCultures">The cultures the app supports, like the <see cref="LuxOptions.Locales"/> codes. Omit it to accept any culture the runtime knows.</param>
    public static async Task<CultureInfo?> ApplyStoredCultureAsync(this IServiceProvider services, IEnumerable<string>? supportedCultures = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var store = services.GetService<ILxCultureStore>();
        if (store is null) return null;
        var supported = supportedCultures?.ToArray();
        var culture = LxCultureStoreMatch.Match(await store.GetAsync(), supported);
        if (culture is null) return null;
        LxCultureStoreMatch.Apply(culture);
        return culture;
    }
}

internal static class LxCultureStoreMatch
{
    /// <summary>The culture for a stored name: the supported entry with that name, else the first of its parents that is supported.</summary>
    public static CultureInfo? Match(string? name, IReadOnlyList<string>? supported)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        CultureInfo requested;
        try
        {
            requested = CultureInfo.GetCultureInfo(name.Trim());
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        if (requested.Name.Length == 0) return null;
        if (supported is null) return requested;

        for (var c = requested; c.Name.Length > 0; c = c.Parent)
        {
            foreach (var entry in supported)
            {
                if (string.Equals(entry, c.Name, StringComparison.OrdinalIgnoreCase)) return CultureInfo.GetCultureInfo(entry);
            }
        }
        return null;
    }

    public static void Apply(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
