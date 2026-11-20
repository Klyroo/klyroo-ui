using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Blux.UI;

/// <summary>Helpers to give the WebAssembly runtime the culture the server chose.</summary>
public static class BluxCulture
{
    /// <summary>
    /// Gives a WebAssembly client the culture the server rendered with, instead of the language of the browser, so the
    /// prerender and the client pick the same <see cref="BxLocale"/> (and format dates and numbers the same way) for the same user.
    /// The client has no request, so it reads what the server left in the page, in this order:
    /// 1. the <c>blux-culture</c> meta element rendered by <see cref="BxCultureMeta"/> in <c>&lt;head&gt;</c>: the exact culture and UI culture of the request,
    /// whatever provider chose them (query string, cookie or <c>Accept-Language</c>);
    /// 2. the ASP.NET Core culture cookie (<c>c=fr|uic=fr</c>, written by <see cref="BxLocaleSelect"/> and read by <c>UseRequestLocalization</c>);
    /// 3. the <c>lang</c> attribute of the <c>html</c> element, which the server wrote from the locale it resolved
    /// (<c>&lt;html lang="@BxLocales.Resolve().Code"&gt;</c>), so a culture the server picked from <c>Accept-Language</c> reaches the client even without the meta element.
    /// Render <see cref="BxCultureMeta"/> to get the regional culture too ("en-GB" for dates and clocks); the cookie and the attribute only carry what they hold.
    /// Call it in the <c>Program.cs</c> of the client, after <c>Build()</c> and before <c>RunAsync()</c>. Nothing changes when none of them is present.
    /// </summary>
    /// <param name="services">The services of the client host.</param>
    /// <param name="supportedCultures">
    /// The cultures the server supports, the same list as <c>AddSupportedCultures</c>. A culture of the cookie or of the
    /// <c>lang</c> attribute (the meta element is the server's own result and is trusted) that is not in it, nor has one of its parents in it ("pt-PT" is served by "pt"), is ignored, like the server does.
    /// Omit it to accept any culture the runtime knows.
    /// </param>
    /// <param name="cookieName">The name of the culture cookie.</param>
    /// <example><c>var host = builder.Build(); await host.Services.ApplyServerCultureAsync(BxLocales.All.Select(l =&gt; l.Code)); await host.RunAsync();</c></example>
    public static async Task ApplyServerCultureAsync(this IServiceProvider services, IEnumerable<string>? supportedCultures = null, string cookieName = ".AspNetCore.Culture")
    {
        ArgumentNullException.ThrowIfNull(services);
        var js = services.GetRequiredService<IJSRuntime>();
        string?[] source;
        try
        {
            source = await js.InvokeAsync<string?[]>("Blux.serverCulture", cookieName);
        }
        catch (JSException)
        {
            // The browser side of Blux is not loaded yet: keep the culture of the browser.
            return;
        }

        if (Choose(source.ElementAtOrDefault(0), source.ElementAtOrDefault(1), source.ElementAtOrDefault(2), supportedCultures?.ToArray()) is not { } chosen) return;
        CultureInfo.DefaultThreadCurrentCulture = chosen.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = chosen.UICulture;
        CultureInfo.CurrentCulture = chosen.Culture;
        CultureInfo.CurrentUICulture = chosen.UICulture;
    }

    /// <summary>The first version of <see cref="ApplyServerCultureAsync"/>, which only read the cookie and ignored the <c>lang</c> attribute.</summary>
    [Obsolete("Use ApplyServerCultureAsync: it also follows the culture the server picked without a cookie and can check the supported cultures.")]
    public static Task ApplyCultureCookieAsync(this IServiceProvider services, string cookieName = ".AspNetCore.Culture") =>
        ApplyServerCultureAsync(services, null, cookieName);

    internal sealed record ChosenCulture(CultureInfo Culture, CultureInfo UICulture);

    /// <summary>
    /// What <c>UseRequestLocalization</c> chose, from what the page keeps: the meta element (trusted as is), then a valid cookie,
    /// then the <c>lang</c> attribute. Null when none of them names a usable culture.
    /// </summary>
    /// <remarks>
    /// Like the server, <c>c</c> and <c>uic</c> are checked on their own and a missing one takes the value of the other (the cookie
    /// <c>c=fr</c> means both); when only one of the two is supported, the client uses it for both because it does not know the default culture of the server.
    /// </remarks>
    internal static ChosenCulture? Choose(string? meta, string? cookie, string? lang, IReadOnlyList<string>? supported)
    {
        if (Pick(meta, null) is { } fromMeta) return fromMeta;
        if (Pick(cookie, supported) is { } fromCookie) return fromCookie;
        return Match(lang, supported) is { } language ? new ChosenCulture(language, language) : null;
    }

    private static ChosenCulture? Pick(string? value, IReadOnlyList<string>? supported)
    {
        var fields = ParseCookie(value);
        var culture = Match(fields.Culture ?? fields.UICulture, supported);
        var uiCulture = Match(fields.UICulture ?? fields.Culture, supported);
        culture ??= uiCulture;
        uiCulture ??= culture;
        return culture is null || uiCulture is null ? null : new ChosenCulture(culture, uiCulture);
    }

    private static (string? Culture, string? UICulture) ParseCookie(string? value)
    {
        string? culture = null, uiCulture = null;
        foreach (var part in (value ?? "").Split('|'))
        {
            var equals = part.IndexOf('=');
            if (equals <= 0) continue;
            var text = part[(equals + 1)..].Trim();
            if (text.Length == 0) continue;
            switch (part[..equals].Trim().ToLowerInvariant())
            {
                case "c": culture = text; break;
                case "uic": uiCulture = text; break;
            }
        }
        return (culture, uiCulture);
    }

    /// <summary>The culture for a name: the supported entry with that name, else the first one of its parents that is supported; any known culture when nothing is listed.</summary>
    private static CultureInfo? Match(string? name, IReadOnlyList<string>? supported)
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
}
