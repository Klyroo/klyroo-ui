using Microsoft.AspNetCore.Http;

namespace Klyroo.UI.Server;

/// <summary>The <see cref="IKlyRequestCookies"/> of an ASP.NET Core server: the cookies and the path base of the request <see cref="IHttpContextAccessor"/> holds, null when there is none.</summary>
/// <param name="accessor">The accessor of the current HTTP context.</param>
/// <param name="options">The server options.</param>
internal sealed class KlyRequestCookies(IHttpContextAccessor accessor, KlyrooServerOptions? options = null) : IKlyRequestCookies
{
    private readonly KlyrooServerOptions _options = options ?? new KlyrooServerOptions();

    /// <inheritdoc />
    public string? PathBase => accessor.HttpContext?.Request.PathBase.Value;

    /// <inheritdoc />
    public string? Get(string name)
    {
        if (accessor.HttpContext is not { } context) return null;
        if (_options.SetCacheHeaders && !context.Response.HasStarted)
        {
            MarkResponsePrivate(context.Response.Headers);
        }
        return context.Request.Cookies.TryGetValue(name, out var value) ? value : null;
    }

    // Any HTML render that reads a request cookie depends on the Cookie header (whether the cookie was sent on this request or not):
    // mark the response private and vary on Cookie so a shared proxy or CDN never caches one user's rendered page for another.
    private static void MarkResponsePrivate(IHeaderDictionary headers)
    {
        var vary = headers.Vary.ToString();
        if (!HasToken(vary, "*") && !HasToken(vary, "Cookie"))
        {
            headers.Vary = string.IsNullOrWhiteSpace(vary) ? "Cookie" : vary + ", Cookie";
        }

        var cacheControl = headers.CacheControl.ToString();
        if (string.IsNullOrWhiteSpace(cacheControl))
        {
            headers.CacheControl = "private";
        }
        else if (!HasToken(cacheControl, "private") && !HasToken(cacheControl, "no-store"))
        {
            headers.CacheControl = ReplacePublicOrPrependPrivate(cacheControl);
        }
    }

    private static bool HasToken(string header, string token)
    {
        if (string.IsNullOrWhiteSpace(header)) return false;
        foreach (var part in header.Split(','))
        {
            var piece = part.AsSpan().Trim();
            var eq = piece.IndexOf('=');
            var name = eq >= 0 ? piece[..eq].Trim() : piece;
            if (name.Equals(token.AsSpan(), StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string ReplacePublicOrPrependPrivate(string cacheControl)
    {
        var parts = cacheControl.Split(',');
        var replaced = false;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].AsSpan().Trim().Equals("public".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = "private";
                replaced = true;
            }
            else
            {
                parts[i] = parts[i].Trim();
            }
        }
        return replaced ? string.Join(", ", parts) : "private, " + cacheControl.Trim();
    }
}
