using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Blux.UI;

/// <summary>Helpers to give the WebAssembly runtime the culture the server chose.</summary>
public static class BluxCulture
{
    /// <summary>
    /// Sets the culture of a WebAssembly client from the ASP.NET Core culture cookie (the one <see cref="BxLocaleSelect"/>
    /// writes and <c>UseRequestLocalization</c> reads), so the prerender on the server and the client pick the same
    /// <see cref="BxLocale"/> for the same user. Without it the client uses the language of the browser.
    /// Call it in the <c>Program.cs</c> of the client, after <c>Build()</c> and before <c>RunAsync()</c>. Nothing changes when there is no cookie.
    /// </summary>
    /// <example><c>var host = builder.Build(); await host.Services.ApplyCultureCookieAsync(); await host.RunAsync();</c></example>
    public static async Task ApplyCultureCookieAsync(this IServiceProvider services, string cookieName = ".AspNetCore.Culture")
    {
        ArgumentNullException.ThrowIfNull(services);
        var js = services.GetRequiredService<IJSRuntime>();
        string? name;
        try
        {
            name = await js.InvokeAsync<string?>("Blux.cultureFromCookie", cookieName);
        }
        catch (JSException)
        {
            // The browser side of Blux is not loaded yet: keep the culture of the browser.
            return;
        }
        if (string.IsNullOrEmpty(name)) return;

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return;
        }
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
