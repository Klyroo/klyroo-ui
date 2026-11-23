using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blux.UI.Tools;

/// <summary>
/// The time zone the browser told the server in its cookie (<c>blux-timezone</c>, see <c>js/time-zone-cookie.js</c>), for the renders that have a request and no browser: a static render and a
/// prerender. The request is reached only through <see cref="IBxRequestCookies"/> of Blux.UI, which the package <c>Blux.UI.Server</c> implements (<c>AddBluxServer()</c>); in WebAssembly and
/// MAUI nothing implements it and the zone is unknown.
/// </summary>
internal static class ServerTimeZone
{
    /// <summary>The name of the cookie at the root of the site; below a path base it carries the slug of the base (<see cref="BxPathScope.CookieName"/>).</summary>
    public const string CookieName = "blux-timezone";

    /// <summary>
    /// The valid IANA id the cookie of the current request holds, or <see langword="null"/>: no request, no cookie (the first visit, before any script ran) or an id this machine does not know.
    /// A server-rendered app without <c>AddBluxServer()</c> gets <see langword="null"/> too, and one development-time warning.
    /// </summary>
    public static string? Read(IServiceProvider services)
    {
        if (OperatingSystem.IsBrowser()) return null;
        if (services.GetService<IBxRequestCookies>() is not { } request)
        {
            services.GetService<MissingServerWarning>()?.Warn(services);
            return null;
        }
        return request.Get(BxPathScope.CookieName(CookieName, request.PathBase)) is { } id ? DataListDates.ValidId(id) : null;
    }
}

/// <summary>
/// Says once per app, in Development, that a static render or a prerender had no <see cref="IBxRequestCookies"/>: the data list then cuts days in UTC until the browser is interactive. A render
/// outside the browser that is not interactive is a server render, so a missing service there means <c>AddBluxServer()</c> was not called.
/// </summary>
internal sealed class MissingServerWarning(Func<string?> environment)
{
    private int _warned;
    private int _warnedColumns;

    public MissingServerWarning() : this(() => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
    {
    }

    public void Warn(IServiceProvider services)
    {
        if (!string.Equals(environment(), "Development", StringComparison.OrdinalIgnoreCase)) return;
        if (Interlocked.Exchange(ref _warned, 1) != 0) return;
        services.GetService<ILoggerFactory>()?.CreateLogger("Blux.UI.Tools.DataList").LogWarning(
            "A data list rendered on the server (static rendering or prerendering) without access to the request, so it cuts the days of its date filters in UTC until the browser is interactive. Call services.AddBluxServer() from the package Blux.UI.Server in the server Program.cs to use the time zone cookie of the browser.");
    }

    /// <summary>The same warning for a list with a <c>PreferencesKey</c>: its static render and its prerender cannot read the column cookie and paint the default columns.</summary>
    public void WarnColumns(IServiceProvider services)
    {
        if (!string.Equals(environment(), "Development", StringComparison.OrdinalIgnoreCase)) return;
        if (Interlocked.Exchange(ref _warnedColumns, 1) != 0) return;
        services.GetService<ILoggerFactory>()?.CreateLogger("Blux.UI.Tools.DataList").LogWarning(
            "A data list with a PreferencesKey rendered on the server (static rendering or prerendering) without access to the request, so it paints its default columns until the browser is interactive. Call services.AddBluxServer() from the package Blux.UI.Server in the server Program.cs to read the column cookie on the first paint.");
    }
}
