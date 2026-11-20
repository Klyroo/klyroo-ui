using Microsoft.Extensions.FileProviders;

namespace Blux.UI.Images.Local;

/// <summary>
/// Settings of the local image optimizer. Every limit has a safe default; the endpoint serves local images of the web root and,
/// only for the hosts in <see cref="AllowedRemoteHosts"/>, remote ones.
/// </summary>
public sealed class BxLocalImagesOptions
{
    /// <summary>Where <c>MapBluxImages</c> maps the endpoint and where the <c>local</c> provider points. Default <c>/_blux/img</c>.</summary>
    public string Endpoint { get; set; } = "/_blux/img";

    /// <summary>
    /// A token of yours (an application version, a deploy id) added as <c>v</c> to every URL. A request that carries it is answered with
    /// <c>Cache-Control: public, max-age=31536000, immutable</c>; change it when the source images change. Without it responses are cached
    /// for <see cref="CacheMaxAge"/> and revalidated with the <c>ETag</c>.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>Makes <c>local</c> the default provider of <c>BluxOptions.Images</c>. Default true.</summary>
    public bool MakeDefaultProvider { get; set; } = true;

    /// <summary>
    /// The files local sources are read from, <c>src=/img/a.png</c> being resolved against its root. Defaults to the web root
    /// (<c>wwwroot</c>, through <c>IWebHostEnvironment.WebRootFileProvider</c>). Only files below the root are reachable, and a provider that
    /// excludes hidden files (the physical one does) keeps them out.
    /// </summary>
    public IFileProvider? FileProvider { get; set; }

    /// <summary>
    /// Hosts whose images may be fetched and optimized (<c>"images.example.com"</c>). Empty by default: no remote image is ever fetched.
    /// They are also added to <c>BluxOptions.Images.Domains</c>, so <c>BxImage</c> only sends these hosts here.
    /// </summary>
    public List<string> AllowedRemoteHosts { get; } = [];

    /// <summary>
    /// Allows fetching from loopback, private, link-local and other non-public addresses. Off by default: a listed host that resolves to an
    /// internal address (a DNS rebinding, a misconfiguration) is refused instead of reaching your network.
    /// </summary>
    public bool AllowPrivateNetworkAddresses { get; set; }

    /// <summary>Largest remote source, in bytes. Default 10 MiB.</summary>
    public long MaxRemoteBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Time allowed to fetch a remote source. Default 10 seconds.</summary>
    public TimeSpan RemoteTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Largest local source, in bytes. Default 25 MiB.</summary>
    public long MaxSourceBytes { get; set; } = 25 * 1024 * 1024;

    /// <summary>Largest source in pixels (width times height), which bounds the memory of a decode. Default 100 million.</summary>
    public long MaxInputPixels { get; set; } = 100_000_000;

    /// <summary>Requested widths above this are clamped. Default 4096.</summary>
    public int MaxWidth { get; set; } = 4096;

    /// <summary>Requested heights above this are clamped. Default 4096.</summary>
    public int MaxHeight { get; set; } = 4096;

    /// <summary>
    /// The widths the endpoint produces: a requested width is rounded up to the next one (the largest when none fits). That bounds the
    /// variants one image can have, so a client cannot fill the cache or burn CPU by asking for every width. Defaults to <see cref="BxLocalImageProvider.Steps"/>.
    /// </summary>
    public IReadOnlyList<int> Widths { get; set; } = BxLocalImageProvider.Steps;

    /// <summary>Quality when the request names none. Default 80. A requested quality is clamped to 1 to 100 and rounded up to a multiple of 5.</summary>
    public int DefaultQuality { get; set; } = 80;

    /// <summary>Folder of the disk cache. Defaults to <c>blux-images</c> in the temporary folder; use a folder that survives restarts in production.</summary>
    public string CacheDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "blux-images");

    /// <summary>Size limit of the disk cache, in bytes. The oldest files are deleted once it is exceeded. Default 512 MiB.</summary>
    public long MaxCacheBytes { get; set; } = 512L * 1024 * 1024;

    /// <summary>Variants kept per source image. A request for a new variant beyond it answers 429. Default 32.</summary>
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
    /// Whether an SVG may be rasterized when the request names a raster format (<c>f=webp</c>). An SVG is otherwise served as it is, never
    /// rasterized. Default true.
    /// </summary>
    public bool RasterizeSvg { get; set; } = true;

    /// <summary>Replaces the handler that fetches remote images (tests, a proxy). The default refuses redirects and non-public addresses.</summary>
    public Func<HttpMessageHandler>? RemoteHandlerFactory { get; set; }
}
