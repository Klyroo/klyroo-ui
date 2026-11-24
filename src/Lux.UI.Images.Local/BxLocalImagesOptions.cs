using Microsoft.Extensions.FileProviders;

namespace Lux.UI.Images.Local;

/// <summary>
/// Settings of the local image optimizer. Every limit has a safe default; the endpoint serves local images of the web root and,
/// only for the hosts in <see cref="AllowedRemoteHosts"/>, remote ones.
/// </summary>
public sealed class BxLocalImagesOptions
{
    /// <summary>Where <c>MapLuxImages</c> maps the endpoint and where the <c>local</c> provider points. Default <c>_blux/img</c>, relative to the base of the app (see <see cref="BxLocalImageProvider.DefaultEndpoint"/>).</summary>
    public string Endpoint { get; set; } = BxLocalImageProvider.DefaultEndpoint;

    /// <summary>
    /// A token of yours (an application version, a deploy id) added as <c>v</c> to every URL. A request that carries it is answered with
    /// <c>Cache-Control: public, max-age=31536000, immutable</c>; change it when the source images change. Without it responses are cached
    /// for <see cref="CacheMaxAge"/> and revalidated with the <c>ETag</c>.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>Makes <c>local</c> the default provider of <c>LuxOptions.Images</c>. Default true.</summary>
    public bool MakeDefaultProvider { get; set; } = true;

    /// <summary>
    /// The files local sources are read from, <c>src=/img/a.png</c> being resolved against its root. Defaults to the web root
    /// (<c>wwwroot</c>, through <c>IWebHostEnvironment.WebRootFileProvider</c>). Only files below the root are reachable, and a provider that
    /// excludes hidden files (the physical one does) keeps them out.
    /// </summary>
    public IFileProvider? FileProvider { get; set; }

    /// <summary>
    /// Hosts whose images may be fetched and optimized (<c>"images.example.com"</c>). Empty by default: no remote image is ever fetched.
    /// Only the default port of the scheme (80, 443) is reachable; an entry that names a port (<c>"images.example.com:8443"</c>) allows that port
    /// and no other. They are also added to <c>LuxOptions.Images.Domains</c>, so <c>BxImage</c> only sends these hosts here.
    /// </summary>
    public List<string> AllowedRemoteHosts { get; } = [];

    /// <summary>
    /// Allows fetching from loopback, private, link-local and other non-public addresses. Off by default: a listed host that resolves to an
    /// internal address (a DNS rebinding, a misconfiguration) is refused instead of reaching your network.
    /// </summary>
    public bool AllowPrivateNetworkAddresses { get; set; }

    /// <summary>Largest remote source, in bytes. Default 10 MiB.</summary>
    public long MaxRemoteBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Remote fetches running at the same time. They have their own limit so a slow origin cannot take processing slots away. Default 8.</summary>
    public int MaxRemoteFetches { get; set; } = 8;

    /// <summary>Time allowed to fetch a remote source. Default 10 seconds.</summary>
    public TimeSpan RemoteTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Largest local source, in bytes. Default 25 MiB.</summary>
    public long MaxSourceBytes { get; set; } = 25 * 1024 * 1024;

    /// <summary>
    /// Largest source in pixels (width times height), which bounds the memory of a decode. Default 100 million. A request without a size is
    /// downscaled to <see cref="MaxWidth"/> by <see cref="MaxHeight"/>.
    /// </summary>
    public long MaxInputPixels { get; set; } = 100_000_000;

    /// <summary>Requested widths above this are clamped, and no output is wider. Default 4096.</summary>
    public int MaxWidth { get; set; } = 4096;

    /// <summary>Requested heights above this are clamped, and no output is taller. Default 4096.</summary>
    public int MaxHeight { get; set; } = 4096;

    /// <summary>
    /// The widths the endpoint produces: a requested width is rounded up to the next one (the largest when none fits). That bounds the
    /// variants one image can have, so a client cannot fill the cache or burn CPU by asking for every width. When the endpoint changes the width,
    /// a requested height is scaled by the same factor so the crop keeps its ratio. Defaults to <see cref="BxLocalImageProvider.Steps"/>.
    /// </summary>
    public IReadOnlyList<int> Widths { get; set; } = BxLocalImageProvider.Steps;

    /// <summary>Quality when the request names none. Default 80. A requested quality is clamped to 1 to 100 and rounded up to a multiple of 5.</summary>
    public int DefaultQuality { get; set; } = 80;

    /// <summary>Folder of the disk cache. Defaults to <c>blux-images</c> in the temporary folder; use a folder that survives restarts in production.</summary>
    public string CacheDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "blux-images");

    /// <summary>
    /// Size limit of the disk cache, counted in allocated disk space (every file rounds up to 4 KiB blocks and costs an entry, every folder a block).
    /// Once it is exceeded the variants that were used least recently are deleted, in the background. Default 512 MiB.
    /// </summary>
    public long MaxCacheBytes { get; set; } = 512L * 1024 * 1024;

    /// <summary>
    /// Variants kept per source image. A new variant beyond it replaces the least recently used one, so junk requests cannot lock real variants out.
    /// Default 32.
    /// </summary>
    public int MaxVariantsPerSource { get; set; } = 32;

    /// <summary>Images processed at the same time. Default: the number of processors.</summary>
    public int MaxConcurrency { get; set; } = Math.Max(1, Environment.ProcessorCount);

    /// <summary>How long a request waits for a free processing slot before it answers 503. Default 5 seconds.</summary>
    public TimeSpan QueueTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary><c>max-age</c> of responses to URLs without the <c>v</c> token. Default 1 hour.</summary>
    public TimeSpan CacheMaxAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How long a cached variant of a remote image is reused before it is fetched again. Default 24 hours.</summary>
    public TimeSpan RemoteCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Computations (every request that is not answered from the cache, and every remote fetch) one client may start per <see cref="RateLimitWindow"/>.
    /// Beyond it the endpoint answers 429 with <c>Retry-After</c>; cached variants are always served. Clients are told apart by their address
    /// (the /64 prefix for IPv6), so behind a reverse proxy call <c>UseForwardedHeaders</c> first, otherwise all visitors share one allowance.
    /// 0 turns the limit off. Default 600.
    /// </summary>
    public int RateLimitPermits { get; set; } = 600;

    /// <summary>The window of <see cref="RateLimitPermits"/>. Default 1 minute.</summary>
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Whether an SVG may be rasterized when the request names a raster format (<c>f=webp</c>). An SVG is otherwise served as it is (with a
    /// sandboxing <c>Content-Security-Policy</c>), and libvips never sees it. Rasterizing hands the file to an SVG renderer, so turn it on only
    /// for SVGs you trust. Default false.
    /// </summary>
    public bool RasterizeSvg { get; set; }

    /// <summary>Replaces the handler that fetches remote images (tests, a proxy). The default refuses redirects and non-public addresses.</summary>
    public Func<HttpMessageHandler>? RemoteHandlerFactory { get; set; }

    /// <summary>How often a cache hit refreshes the last-access time that eviction goes by. Default 1 minute.</summary>
    internal TimeSpan AccessTouchInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Test hook: called inside a processing slot, right before libvips works.</summary>
    internal Action<BxImageRequest, CancellationToken>? OnProcess { get; set; }

    /// <summary>
    /// Whether a remote address is on the allowlist: the host must be listed, and the port must be the default one of the scheme unless the
    /// entry names it (<c>host:8443</c>).
    /// </summary>
    internal bool IsAllowedRemote(Uri uri)
    {
        foreach (var entry in AllowedRemoteHosts)
        {
            var text = entry.Trim();
            string host;
            int? port = null;
            if (text.Contains("://", StringComparison.Ordinal) && Uri.TryCreate(text, UriKind.Absolute, out var parsed))
            {
                host = parsed.Host;
                if (!parsed.IsDefaultPort) port = parsed.Port;
            }
            else
            {
                var colon = text.LastIndexOf(':');
                if (colon > 0 && text.IndexOf(':') == colon && int.TryParse(text.AsSpan(colon + 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number))
                {
                    host = text[..colon];
                    port = number;
                }
                else
                {
                    host = text;
                }
            }
            if (!string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase)) continue;
            if (port is { } named ? uri.Port == named : uri.IsDefaultPort) return true;
        }
        return false;
    }
}
