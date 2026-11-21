using Microsoft.AspNetCore.Http;

namespace Blux.UI.Server;

/// <summary>The <see cref="IBxRequestCookies"/> of an ASP.NET Core server: the cookies and the path base of the request <see cref="IHttpContextAccessor"/> holds, null when there is none.</summary>
/// <param name="accessor">The accessor of the current HTTP context.</param>
internal sealed class BxRequestCookies(IHttpContextAccessor accessor) : IBxRequestCookies
{
    /// <inheritdoc />
    public string? PathBase => accessor.HttpContext?.Request.PathBase.Value;

    /// <inheritdoc />
    public string? Get(string name) =>
        accessor.HttpContext?.Request.Cookies.TryGetValue(name, out var value) == true ? value : null;
}
