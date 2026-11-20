using System.Collections.Concurrent;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp;

/// <summary>
/// Serves <see cref="LlmsText"/> without rebuilding it per request. With a configured site URL the links never depend on the
/// request (so a spoofed <c>Host</c> header cannot put another site in them) and the text is generated once. Without one, the
/// request origin is used and the result cached for a few origins; beyond that it is generated per request, so arbitrary
/// <c>Host</c> values cannot grow memory.
/// </summary>
public sealed class LlmsPages
{
    /// <summary>Most origins kept when no site URL is configured.</summary>
    public const int MaxCachedOrigins = 4;

    private readonly DocsBundle _bundle;
    private readonly Lazy<string>? _index;
    private readonly Lazy<string>? _full;
    private readonly ConcurrentDictionary<string, (string Index, string Full)> _byOrigin = new(StringComparer.Ordinal);

    /// <param name="bundle">What to describe.</param>
    /// <param name="siteUrl">Public address of the docs site (an absolute http or https URL), or null to use the request origin.</param>
    /// <exception cref="ArgumentException"><paramref name="siteUrl"/> is not an absolute http(s) URL.</exception>
    public LlmsPages(DocsBundle bundle, string? siteUrl)
    {
        _bundle = bundle;
        SiteUrl = Normalize(siteUrl);
        if (SiteUrl is not null)
        {
            _index = new(() => LlmsText.Index(_bundle, SiteUrl));
            _full = new(() => LlmsText.Full(_bundle, SiteUrl));
        }
    }

    /// <summary>The configured address without a trailing slash, or null.</summary>
    public string? SiteUrl { get; }

    public int CachedOrigins => _byOrigin.Count;

    public string Index(string requestOrigin) => _index?.Value ?? Get(requestOrigin).Index;

    public string Full(string requestOrigin) => _full?.Value ?? Get(requestOrigin).Full;

    private (string Index, string Full) Get(string origin)
    {
        if (_byOrigin.TryGetValue(origin, out var cached)) return cached;
        var built = (LlmsText.Index(_bundle, origin), LlmsText.Full(_bundle, origin));
        if (_byOrigin.Count < MaxCachedOrigins) _byOrigin.TryAdd(origin, built);
        return built;
    }

    private static string? Normalize(string? siteUrl)
    {
        if (string.IsNullOrWhiteSpace(siteUrl)) return null;
        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"The site URL '{siteUrl}' is not an absolute http or https address.", nameof(siteUrl));
        }
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }
}
