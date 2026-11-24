namespace Lux.UI;

/// <summary>
/// The HTTP request a component is rendering for, as far as Blux needs it: the cookies and the path base. It exists for the renders that have a request and no browser, a
/// static render and a prerender, where a component can read what the browser left in a cookie (the time zone of a data list, and the columns a user chose). Lux.UI is
/// also loaded by WebAssembly and MAUI apps, where there is no ASP.NET Core request, so the library only knows this abstraction: the package <c>Lux.UI.Server</c> implements
/// it over <c>IHttpContextAccessor</c> and <c>AddLuxServer()</c> registers it.
/// </summary>
/// <remarks>
/// Nothing is registered by default, and the service is absent (<see langword="null"/> from <c>GetService</c>) in WebAssembly, in MAUI Blazor Hybrid and in an app that forgot
/// <c>AddLuxServer()</c>. An interactive render that follows a prerender has no request either (the circuit is not an HTTP request): <see cref="Get"/> then returns <see langword="null"/>.
/// A component reads a cookie only when it is not rendering interactively.
/// </remarks>
public interface IBxRequestCookies
{
    /// <summary>The path base of the current request (<c>Request.PathBase</c>), null or empty at the root or without a request. Cookie names below a path base carry its slug, see <see cref="BxPathScope.CookieName"/>.</summary>
    string? PathBase { get; }

    /// <summary>The value of the cookie called <paramref name="name"/> in the current request, or <see langword="null"/> when there is no request or no such cookie. The name is taken as it is: scope it with <see cref="BxPathScope.CookieName"/>.</summary>
    /// <param name="name">The cookie name.</param>
    string? Get(string name);
}
