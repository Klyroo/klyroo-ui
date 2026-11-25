using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using NetVips;

namespace Klyroo.UI.Images.Local;

internal sealed class KlyImageException(int status, string message, int? retryAfterSeconds = null) : Exception(message)
{
    public int Status { get; } = status;

    public int? RetryAfterSeconds { get; } = retryAfterSeconds;
}

/// <summary>
/// A finished variant: on disk (<see cref="Path"/>), and for one that was just computed also in memory (<see cref="Bytes"/>), so sending it
/// does not depend on a cache file that an eviction may have removed in the meantime.
/// </summary>
internal sealed record KlyImageFile(string? Path, byte[]? Bytes, string ContentType, string ETag, bool Svg);

internal readonly record struct KlyRemoteSource(byte[] Bytes, string Stamp);

/// <summary>
/// Loads the source (local or remote), resizes and converts it with libvips, and keeps the result in a size-limited disk cache.
/// Every step is bounded: input size, pixels, output size, remote time, variants per image, concurrent work and cache bytes. Identical requests
/// share one computation, a remote fetch has its own limit and never holds a processing slot, and work stops when nobody waits for it.
/// </summary>
internal sealed class KlyImageProcessor : IDisposable
{
    private static readonly string[] ImageMediaTypes = ["image/png", "image/jpeg", "image/webp", "image/avif", "image/gif", "image/svg+xml"];

    // The libvips loaders the endpoint may use; everything else is blocked in libvips itself, on top of the check of the loader name.
    private static readonly string[] AllowedLoaderClasses = ["VipsForeignLoadJpeg", "VipsForeignLoadPng", "VipsForeignLoadWebp", "VipsForeignLoadHeif", "VipsForeignLoadNsgif"];
    private static int s_svgUnblocked;

    private readonly KlyLocalImagesOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<KlyImageProcessor> _logger;
    private readonly SemaphoreSlim _slots;
    private readonly SemaphoreSlim _fetchSlots;
    private readonly HttpClient _http;
    private readonly KlyImageCache _cache;
    private readonly KlyFlights<KlyImageFile> _variants = new();
    private readonly KlyFlights<KlyRemoteSource> _fetches = new();

    static KlyImageProcessor()
    {
        try
        {
            // The operation cache would keep decoded pixels alive between requests: the disk cache is the cache.
            Cache.Max = 0;
            Cache.MaxMem = 0;
            Cache.MaxFiles = 0;
            // Defense in depth: libvips refuses the loaders it flags untrusted, and every loader outside the list above.
            NetVips.NetVips.BlockUntrusted = true;
            Operation.Block("VipsForeignLoad", true);
            foreach (var loader in AllowedLoaderClasses) Operation.Block(loader, false);
            Operation.Block("VipsForeignLoadSvg", true);
        }
        catch (Exception)
        {
            // A missing native library surfaces on the first image.
        }
    }

    public KlyImageProcessor(KlyLocalImagesOptions options, IWebHostEnvironment environment, ILogger<KlyImageProcessor> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
        _cache = new KlyImageCache(options, logger);
        var concurrency = Math.Max(1, options.MaxConcurrency);
        _slots = new SemaphoreSlim(concurrency);
        _fetchSlots = new SemaphoreSlim(Math.Max(1, options.MaxRemoteFetches));
        _http = new HttpClient(options.RemoteHandlerFactory?.Invoke() ?? CreateHandler(options), disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
        try
        {
            // Each job gets its share of the cores instead of every job spawning a thread per core.
            NetVips.NetVips.Concurrency = Math.Max(1, Environment.ProcessorCount / concurrency);
            // An SVG is only handed to the renderer when rasterizing is on; once any instance asks for it, it stays available.
            if (options.RasterizeSvg && Interlocked.Exchange(ref s_svgUnblocked, 1) == 0) Operation.Block("VipsForeignLoadSvg", false);
        }
        catch (Exception)
        {
            // See the static constructor.
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        _slots.Dispose();
        _fetchSlots.Dispose();
    }

    internal KlyImageCache DiskCache => _cache;

    // Under a case-insensitive file system, spellings that differ by case are one file and must share one key.
    private KlyImageRequest Canonical(KlyImageRequest request) =>
        !request.IsRemote && _cache.CaseInsensitive ? request with { Source = request.Source.ToLowerInvariant() } : request;

    private string SourceDirectory(KlyImageRequest request) => _cache.DirectoryOf(Canonical(request).Source);

    public string ETagOf(KlyImageRequest request, string stamp) => "\"" + KlyImageRequestParser.Hash(Canonical(request).CanonicalKey(stamp))[..32] + "\"";

    /// <summary>
    /// The stamp of a source: a local one is stamped by its modification time and length (and does not exist when it is missing); a remote one by what
    /// was fetched last, which is only known while it is within <see cref="KlyLocalImagesOptions.RemoteCacheTtl"/> (otherwise null: the origin
    /// has to be asked again before anything can be said about it).
    /// </summary>
    public (IFileInfo? File, string? Stamp) Stat(KlyImageRequest request)
    {
        if (request.IsRemote)
        {
            return _cache.ReadMeta(SourceDirectory(request)) is { } meta && DateTime.UtcNow - meta.FetchedUtc <= _options.RemoteCacheTtl ? (null, meta.Stamp) : (null, null);
        }
        var provider = _options.FileProvider ?? _environment.WebRootFileProvider;
        var file = provider.GetFileInfo(request.Source);
        return file.Exists && !file.IsDirectory ? (file, $"{file.LastModified.UtcTicks}-{file.Length}") : (null, "");
    }

    /// <summary>What a cached variant of this request is, for answering a revalidation without computing anything. Null when it is not on disk.</summary>
    public KlyCachedVariant? FindCached(KlyImageRequest request, string stamp)
    {
        var key = KlyImageRequestParser.Hash(Canonical(request).CanonicalKey(stamp));
        return _cache.TryGet(SourceDirectory(request), key, out var variant) ? variant : null;
    }

    /// <summary>The variant for a request. <paramref name="admit"/> is called once, only when this call has to do work that costs CPU or bandwidth (not for cache hits or when joining work in progress), and throws to refuse (429).</summary>
    public async Task<KlyImageFile> GetAsync(KlyImageRequest request, IFileInfo? file, string? stamp, Action admit, CancellationToken cancellation)
    {
        if (!request.IsRemote && file is null) throw new KlyImageException(404, "Image not found.");
        if (file is not null && file.Length > _options.MaxSourceBytes) throw new KlyImageException(413, "The image is too large.");

        var canonical = Canonical(request);
        var directory = _cache.DirectoryOf(canonical.Source);
        KlyRemoteSource? fetched = null;

        if (stamp is not null && TryServeCached(canonical, directory, stamp, out var hit)) return hit;

        if (request.IsRemote)
        {
            // Stale or unknown: ask the origin (once for everyone who is waiting for the same address).
            var source = await _fetches.RunAsync(canonical.Source, token => FetchAndRecordAsync(canonical, directory, admit, token), cancellation).ConfigureAwait(false);
            fetched = source;
            stamp = source.Stamp;
            if (TryServeCached(canonical, directory, stamp, out hit)) return hit;
        }

        var key = KlyImageRequestParser.Hash(canonical.CanonicalKey(stamp!));
        var etag = "\"" + key[..32] + "\"";
        return await _variants.RunAsync(key, token => ComputeAsync(request, canonical, file, fetched?.Bytes, directory, key, etag, admit, token), cancellation).ConfigureAwait(false);
    }

    private bool TryServeCached(KlyImageRequest canonical, string directory, string stamp, out KlyImageFile file)
    {
        var key = KlyImageRequestParser.Hash(canonical.CanonicalKey(stamp));
        if (_cache.TryGet(directory, key, out var variant))
        {
            file = new KlyImageFile(variant.Path, null, variant.ContentType, "\"" + key[..32] + "\"", variant.ContentType == "image/svg+xml");
            return true;
        }
        file = default!;
        return false;
    }

    private async Task<KlyRemoteSource> FetchAndRecordAsync(KlyImageRequest canonical, string directory, Action admit, CancellationToken cancellation)
    {
        admit();
        if (!await _fetchSlots.WaitAsync(_options.QueueTimeout, cancellation).ConfigureAwait(false))
        {
            throw new KlyImageException(503, "The image server is busy.", 1);
        }
        byte[] bytes;
        try
        {
            bytes = await FetchRemoteAsync(canonical.RemoteUri!, cancellation).ConfigureAwait(false);
        }
        finally
        {
            _fetchSlots.Release();
        }
        // What identifies the remote content is the content: the validators of an origin are optional and not always honest.
        var stamp = KlyImageRequestParser.Hash(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)))[..16];
        await _cache.WriteMetaAsync(directory, stamp, cancellation).ConfigureAwait(false);
        return new KlyRemoteSource(bytes, stamp);
    }

    private async Task<KlyImageFile> ComputeAsync(KlyImageRequest request, KlyImageRequest canonical, IFileInfo? file, byte[]? remoteBytes, string directory, string key, string etag, Action admit, CancellationToken cancellation)
    {
        // The source bytes are read before a slot is taken: reading a file or waiting for a disk must not hold up the processing of others.
        byte[] bytes;
        if (remoteBytes is not null)
        {
            bytes = remoteBytes;
        }
        else
        {
            admit();
            bytes = await ReadLocalAsync(file!, cancellation).ConfigureAwait(false);
        }

        if (!await _slots.WaitAsync(_options.QueueTimeout, cancellation).ConfigureAwait(false))
        {
            throw new KlyImageException(503, "The image server is busy.", 1);
        }
        (byte[] Bytes, string ContentType) output;
        try
        {
            // Not canceled by the token: the work stops by itself when the token fires (libvips is told to kill the evaluation), and the slot
            // is only given back once libvips is really done.
            output = await Task.Run(() => Process(bytes, request, cancellation), CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _slots.Release();
        }

        string? path = null;
        try
        {
            path = await _cache.StoreAsync(directory, key, ExtensionOf(output.ContentType), output.Bytes, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A full or read-only disk costs the caching, not the response.
            _logger.LogWarning(e, "The image cache could not store a variant");
        }
        return new KlyImageFile(path, output.Bytes, output.ContentType, etag, output.ContentType == "image/svg+xml");
    }

    private static async Task<byte[]> ReadLocalAsync(IFileInfo file, CancellationToken cancellation)
    {
        await using var stream = file.CreateReadStream();
        var buffer = new byte[file.Length];
        await stream.ReadExactlyAsync(buffer, cancellation).ConfigureAwait(false);
        return buffer;
    }

    private async Task<byte[]> FetchRemoteAsync(Uri uri, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(_options.RemoteTimeout);
        try
        {
            var current = uri;
            for (var hop = 0; hop <= 3; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, current);
                request.Headers.Accept.ParseAdd("image/*");
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect)
                {
                    // Only to the same scheme, host and port: a redirect is how an allowed host would point the server at another one,
                    // at a service on another port, or at the same host without TLS.
                    var location = response.Headers.Location;
                    var next = location is null ? null : location.IsAbsoluteUri ? location : new Uri(current, location);
                    if (next is null
                        || !string.Equals(next.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(next.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
                        || next.Port != uri.Port
                        || !string.IsNullOrEmpty(next.UserInfo))
                    {
                        throw new KlyImageException(502, "The remote image redirects somewhere it may not go.");
                    }
                    current = next;
                    continue;
                }
                if (!response.IsSuccessStatusCode) throw new KlyImageException(response.StatusCode == HttpStatusCode.NotFound ? 404 : 502, "The remote image could not be fetched.");

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                if (contentType is null || !ImageMediaTypes.Contains(contentType)) throw new KlyImageException(415, "The remote file is not a supported image.");
                if (response.Content.Headers.ContentLength > _options.MaxRemoteBytes) throw new KlyImageException(413, "The remote image is too large.");

                await using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                using var memory = new MemoryStream();
                var chunk = new byte[16 * 1024];
                int read;
                while ((read = await body.ReadAsync(chunk, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    if (memory.Length + read > _options.MaxRemoteBytes) throw new KlyImageException(413, "The remote image is too large.");
                    memory.Write(chunk, 0, read);
                }
                return memory.ToArray();
            }
            throw new KlyImageException(502, "The remote image redirects too many times.");
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
        {
            throw new KlyImageException(504, "The remote image took too long.");
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Fetching {Uri} failed", uri);
            throw new KlyImageException(502, "The remote image could not be fetched.");
        }
    }

    private sealed class NoProgress : IProgress<int>
    {
        public static readonly NoProgress Instance = new();

        public void Report(int value)
        {
        }
    }

    private (byte[] Bytes, string ContentType) Process(byte[] bytes, KlyImageRequest request, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        _options.OnProcess?.Invoke(request, cancellation);
        cancellation.ThrowIfCancellationRequested();

        // What the bytes are is decided by the bytes, never by a declared type or a file name.
        var svg = KlySvgSniffer.IsSvg(bytes);
        if (svg)
        {
            // Served as it is, never rasterized unless the option is on and the request names a raster format.
            if (request.Format is null or "svg") return (bytes, "image/svg+xml");
            if (!_options.RasterizeSvg) throw new KlyImageException(400, "Rasterizing SVG is disabled.");
        }
        else if (request.Format == "svg")
        {
            throw new KlyImageException(400, "The source is not an SVG.");
        }

        try
        {
            using var header = Image.NewFromBuffer(bytes, access: Enums.Access.Sequential);
            var loader = header.Contains("vips-loader") ? header.Get("vips-loader") as string : null;
            var kind = LoaderKind(loader);
            if (kind is null || (kind == "svg" && !(svg && _options.RasterizeSvg)) || (svg && kind != "svg"))
            {
                // A loader that is not on the list, or an SVG renderer reached by something that was not recognized as an SVG.
                throw new KlyImageException(415, "The file is not a supported image.");
            }
            if ((long)header.Width * header.Height > _options.MaxInputPixels) throw new KlyImageException(413, "The image has too many pixels.");

            // An animated GIF or WebP loses its animation when resized: it is served untouched, with its own type.
            if (kind is "gif" or "webp" && header.Contains("n-pages") && header.Get("n-pages") is int pages and > 1)
            {
                return (bytes, kind == "gif" ? "image/gif" : "image/webp");
            }

            var (sourceWidth, sourceHeight) = OrientedSize(header);
            var format = request.Format ?? FormatOfSource(kind);
            var plan = KlyImageGeometry.Plan(sourceWidth, sourceHeight, request.Width, request.Height, request.Fit, _options.MaxWidth, _options.MaxHeight);
            cancellation.ThrowIfCancellationRequested();

            using var resized = Resize(bytes, plan, format, kind);
            var output = request.Blur > 0 ? resized.Gaussblur(request.Blur) : resized;
            try
            {
                // From here the pipeline is evaluated by the encoder: a fired token kills that evaluation.
                output.SetProgress(NoProgress.Instance, cancellation);
                cancellation.ThrowIfCancellationRequested();
                return Encode(output, format, request.Quality);
            }
            finally
            {
                if (!ReferenceEquals(output, resized)) output.Dispose();
            }
        }
        catch (VipsException e)
        {
            cancellation.ThrowIfCancellationRequested();
            _logger.LogWarning(e, "libvips could not process an image");
            throw new KlyImageException(415, "The file is not a supported image.");
        }
    }

    private static (int Width, int Height) OrientedSize(Image header)
    {
        // EXIF orientations 5 to 8 turn the picture a quarter: the size that matters is the one after autorot.
        var turned = header.Contains("orientation") && header.Get("orientation") is int orientation and >= 5 and <= 8;
        return turned ? (header.Height, header.Width) : (header.Width, header.Height);
    }

    private static Image Resize(byte[] bytes, KlyGeometryPlan plan, string format, string kind)
    {
        switch (plan.Kind)
        {
            case KlyPlanKind.Letterbox:
            {
                using var inner = Image.ThumbnailBuffer(bytes, plan.InnerWidth, height: plan.InnerHeight, size: Enums.Size.Force);
                return Pad(inner, plan.Width, plan.Height, format);
            }
            case KlyPlanKind.Crop:
                return Image.ThumbnailBuffer(bytes, plan.Width, height: plan.Height, size: Enums.Size.Both, crop: Enums.Interesting.Centre);
            case KlyPlanKind.CropFromSource:
            {
                // The covering size would be enormous: cut the region with the ratio of the box out of the source first. Random access, because
                // autorot and cropping need it; this only happens for extreme ratios.
                using var full = Image.NewFromBuffer(bytes, access: Enums.Access.Random);
                using var upright = full.Autorot();
                using var region = upright.ExtractArea(plan.CropX, plan.CropY, Math.Min(plan.CropWidth, upright.Width - plan.CropX), Math.Min(plan.CropHeight, upright.Height - plan.CropY));
                return region.ThumbnailImage(plan.Width, height: plan.Height, size: Enums.Size.Force);
            }
            default:
                return Image.ThumbnailBuffer(bytes, plan.Width, height: plan.Height, size: Enums.Size.Force);
        }
    }

    // Centers the image in a box of exactly width by height: transparent where the format has alpha, white where it does not.
    private static Image Pad(Image inner, int width, int height, string format)
    {
        var x = (width - inner.Width) / 2;
        var y = (height - inner.Height) / 2;
        using var eight = inner.Format == Enums.BandFormat.Uchar ? inner.Copy() : inner.Cast(Enums.BandFormat.Uchar, shift: true);
        if (format is "png" or "webp" or "avif" or "gif")
        {
            using var withAlpha = eight.HasAlpha() ? eight.Copy() : eight.Bandjoin([255.0]);
            return withAlpha.Embed(x, y, width, height, Enums.Extend.Background, [0.0]);
        }
        using var flat = eight.HasAlpha() ? eight.Flatten(background: [255.0]) : eight.Copy();
        return flat.Embed(x, y, width, height, Enums.Extend.Background, [255.0]);
    }

    // The loader names libvips reports: jpegload_buffer, pngload_buffer, webpload_buffer, heifload_buffer, gifload_buffer (nsgif), svgload_buffer.
    private static string? LoaderKind(string? loader) => loader switch
    {
        null => null,
        _ when loader.StartsWith("jpeg", StringComparison.Ordinal) => "jpeg",
        _ when loader.StartsWith("png", StringComparison.Ordinal) => "png",
        _ when loader.StartsWith("webp", StringComparison.Ordinal) => "webp",
        _ when loader.StartsWith("heif", StringComparison.Ordinal) => "heif",
        _ when loader.StartsWith("gif", StringComparison.Ordinal) => "gif",
        _ when loader.StartsWith("svg", StringComparison.Ordinal) => "svg",
        _ => null,
    };

    // GIF and SVG sources become PNG (a single frame with transparency); a request can still ask for another format.
    private static string FormatOfSource(string kind) => kind switch
    {
        "png" or "gif" or "svg" => "png",
        "webp" => "webp",
        "heif" => "avif",
        _ => "jpeg",
    };

    private static (byte[] Bytes, string ContentType) Encode(Image image, string format, int quality)
    {
        switch (format)
        {
            case "webp":
                return (image.WriteToBuffer($".webp[Q={quality},strip]"), "image/webp");
            case "avif":
                return (image.WriteToBuffer($".avif[Q={quality},strip]"), "image/avif");
            case "png":
                return (image.WriteToBuffer(".png[strip,compression=9]"), "image/png");
            case "gif":
                return (image.WriteToBuffer(".gif[strip]"), "image/gif");
            default:
            {
                // JPEG has no alpha channel: flatten onto white instead of letting transparent pixels turn black.
                using var flat = image.HasAlpha() ? image.Flatten(background: [255.0]) : null;
                return ((flat ?? image).WriteToBuffer($".jpg[Q={quality},strip,optimize_coding]"), "image/jpeg");
            }
        }
    }

    internal static string ExtensionOf(string contentType) => contentType switch
    {
        "image/webp" => ".webp",
        "image/avif" => ".avif",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/svg+xml" => ".svg",
        _ => ".jpg",
    };

    internal static string ContentTypeOf(string extension) => extension.ToLowerInvariant() switch
    {
        ".webp" => "image/webp",
        ".avif" => "image/avif",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        _ => "image/jpeg",
    };

    private static SocketsHttpHandler CreateHandler(KlyLocalImagesOptions options) => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        MaxResponseHeadersLength = 32,
        ConnectTimeout = options.RemoteTimeout,
        ConnectCallback = async (context, cancellation) =>
        {
            // Resolve and check here, so the address that is checked is the address that is connected to (no DNS rebinding in between).
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellation).ConfigureAwait(false);
            var allowed = addresses.Where(a => options.AllowPrivateNetworkAddresses || IsPublic(a)).ToArray();
            if (allowed.Length == 0) throw new HttpRequestException("The host resolves to a non-public address.");
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellation).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };

    internal static bool IsPublic(IPAddress address) => KlyAddressPolicy.IsPublic(address);
}
