using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using NetVips;

namespace Blux.UI.Images.Local;

internal sealed class BxImageException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

/// <summary>A finished variant on disk, ready to be sent.</summary>
internal sealed record BxImageFile(string Path, string ContentType, string ETag, bool Svg);

/// <summary>
/// Loads the source (local or remote), resizes and converts it with libvips, and keeps the result in a size-limited disk cache.
/// Every step is bounded: input size, pixels, remote time, variants per image, concurrent work and cache bytes.
/// </summary>
internal sealed class BxImageProcessor : IDisposable
{
    private static readonly string[] ImageMediaTypes = ["image/png", "image/jpeg", "image/webp", "image/avif", "image/gif", "image/svg+xml"];

    private readonly BxLocalImagesOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BxImageProcessor> _logger;
    private readonly SemaphoreSlim _slots;
    private readonly HttpClient _http;
    private readonly object _pruneGate = new();
    private long _cacheBytes = -1;

    static BxImageProcessor()
    {
        // The operation cache would keep decoded pixels alive between requests: the disk cache is the cache.
        try { Cache.Max = 0; Cache.MaxMem = 0; } catch (Exception) { /* a missing native library surfaces on the first image */ }
    }

    public BxImageProcessor(BxLocalImagesOptions options, IWebHostEnvironment environment, ILogger<BxImageProcessor> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
        _slots = new SemaphoreSlim(Math.Max(1, options.MaxConcurrency));
        _http = new HttpClient(options.RemoteHandlerFactory?.Invoke() ?? CreateHandler(options), disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public void Dispose()
    {
        _http.Dispose();
        _slots.Dispose();
    }

    public string ETagOf(BxImageRequest request, string stamp) => "\"" + BxImageRequestParser.Hash(request.CanonicalKey(stamp))[..32] + "\"";

    /// <summary>The stamp of a local source (its modification time and length), or null when it does not exist. Remote sources have no stamp.</summary>
    public (IFileInfo? File, string Stamp) Stat(BxImageRequest request)
    {
        if (request.IsRemote) return (null, "remote");
        var provider = _options.FileProvider ?? _environment.WebRootFileProvider;
        var file = provider.GetFileInfo(request.Source);
        return file.Exists && !file.IsDirectory ? (file, $"{file.LastModified.UtcTicks}-{file.Length}") : (null, "");
    }

    public async Task<BxImageFile> GetAsync(BxImageRequest request, IFileInfo? file, string stamp, CancellationToken cancellation)
    {
        if (!request.IsRemote && file is null) throw new BxImageException(404, "Image not found.");
        if (file is not null && file.Length > _options.MaxSourceBytes) throw new BxImageException(413, "The image is too large.");

        var key = BxImageRequestParser.Hash(request.CanonicalKey(stamp));
        var directory = Path.Combine(_options.CacheDirectory, BxImageRequestParser.Hash(request.Source + "|" + stamp)[..32]);
        var etag = "\"" + key[..32] + "\"";
        if (TryFindCached(directory, key, request, out var cached)) return cached with { ETag = etag };

        if (Directory.Exists(directory) && Directory.EnumerateFiles(directory).Count() >= _options.MaxVariantsPerSource)
        {
            throw new BxImageException(429, "Too many variants of this image.");
        }

        if (!await _slots.WaitAsync(_options.QueueTimeout, cancellation).ConfigureAwait(false))
        {
            throw new BxImageException(503, "The image server is busy.");
        }
        try
        {
            if (TryFindCached(directory, key, request, out cached)) return cached with { ETag = etag };

            var (bytes, remoteType) = request.IsRemote
                ? await FetchRemoteAsync(request.RemoteUri!, cancellation).ConfigureAwait(false)
                : (await ReadLocalAsync(file!, cancellation).ConfigureAwait(false), null);

            var (output, contentType) = await Task.Run(() => Process(bytes, remoteType, request), cancellation).ConfigureAwait(false);

            Directory.CreateDirectory(directory);
            var target = Path.Combine(directory, key + ExtensionOf(contentType));
            var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllBytesAsync(temporary, output, cancellation).ConfigureAwait(false);
            File.Move(temporary, target, overwrite: true);
            Track(output.LongLength);
            return new BxImageFile(target, contentType, etag, contentType == "image/svg+xml");
        }
        finally
        {
            _slots.Release();
        }
    }

    private bool TryFindCached(string directory, string key, BxImageRequest request, out BxImageFile file)
    {
        file = default!;
        if (!Directory.Exists(directory)) return false;
        foreach (var path in Directory.EnumerateFiles(directory, key + ".*"))
        {
            if (path.EndsWith(".tmp", StringComparison.Ordinal)) continue;
            if (request.IsRemote && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _options.RemoteCacheTtl)
            {
                try { File.Delete(path); } catch (IOException) { }
                return false;
            }
            var contentType = ContentTypeOf(Path.GetExtension(path));
            file = new BxImageFile(path, contentType, "", contentType == "image/svg+xml");
            return true;
        }
        return false;
    }

    private async Task<byte[]> ReadLocalAsync(IFileInfo file, CancellationToken cancellation)
    {
        await using var stream = file.CreateReadStream();
        var buffer = new byte[file.Length];
        await stream.ReadExactlyAsync(buffer, cancellation).ConfigureAwait(false);
        return buffer;
    }

    private async Task<(byte[] Bytes, string? ContentType)> FetchRemoteAsync(Uri uri, CancellationToken cancellation)
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
                    // Only inside the same host: a redirect is how an allowed host would point the server at another one.
                    var location = response.Headers.Location;
                    var next = location is null ? null : location.IsAbsoluteUri ? location : new Uri(current, location);
                    if (next is null || !string.Equals(next.Host, uri.Host, StringComparison.OrdinalIgnoreCase) || next.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(next.UserInfo))
                    {
                        throw new BxImageException(502, "The remote image redirects to another host.");
                    }
                    current = next;
                    continue;
                }
                if (!response.IsSuccessStatusCode) throw new BxImageException(response.StatusCode == HttpStatusCode.NotFound ? 404 : 502, "The remote image could not be fetched.");

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                if (contentType is null || !ImageMediaTypes.Contains(contentType)) throw new BxImageException(415, "The remote file is not a supported image.");
                if (response.Content.Headers.ContentLength > _options.MaxRemoteBytes) throw new BxImageException(413, "The remote image is too large.");

                await using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                using var memory = new MemoryStream();
                var chunk = new byte[16 * 1024];
                int read;
                while ((read = await body.ReadAsync(chunk, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    if (memory.Length + read > _options.MaxRemoteBytes) throw new BxImageException(413, "The remote image is too large.");
                    memory.Write(chunk, 0, read);
                }
                return (memory.ToArray(), contentType);
            }
            throw new BxImageException(502, "The remote image redirects too many times.");
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
        {
            throw new BxImageException(504, "The remote image took too long.");
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Fetching {Uri} failed", uri);
            throw new BxImageException(502, "The remote image could not be fetched.");
        }
    }

    private (byte[] Bytes, string ContentType) Process(byte[] bytes, string? declaredType, BxImageRequest request)
    {
        var svg = declaredType == "image/svg+xml" || LooksLikeSvg(bytes);
        if (svg)
        {
            // Served as it is, never rasterized unless the request names a raster format.
            if (request.Format is null or "svg") return (bytes, "image/svg+xml");
            if (!_options.RasterizeSvg) throw new BxImageException(400, "Rasterizing SVG is disabled.");
        }
        else if (request.Format == "svg")
        {
            throw new BxImageException(400, "The source is not an SVG.");
        }

        try
        {
            using var header = Image.NewFromBuffer(bytes, access: Enums.Access.Sequential);
            if ((long)header.Width * header.Height > _options.MaxInputPixels) throw new BxImageException(413, "The image has too many pixels.");
            // An animated GIF or WebP loses its animation when resized: it is served untouched.
            if (header.Contains("n-pages") && header.Get("n-pages") is int pages and > 1)
            {
                return (bytes, header.Contains("vips-loader") && header.Get("vips-loader") is string loader && loader.StartsWith("gif", StringComparison.Ordinal) ? "image/gif" : "image/webp");
            }

            var sourceWidth = header.Width;
            var sourceHeight = header.Height;
            using var resized = Resize(bytes, request, sourceWidth, sourceHeight);
            var output = request.Blur > 0 ? resized.Gaussblur(request.Blur) : resized;
            try
            {
                var format = request.Format ?? FormatOfSource(header);
                return Encode(output, format, request.Quality);
            }
            finally
            {
                if (!ReferenceEquals(output, resized)) output.Dispose();
            }
        }
        catch (VipsException e)
        {
            _logger.LogWarning(e, "libvips could not process an image");
            throw new BxImageException(415, "The file is not a supported image.");
        }
    }

    private static Image Resize(byte[] bytes, BxImageRequest request, int sourceWidth, int sourceHeight)
    {
        const int Unbounded = 1_000_000;
        var w = request.Width;
        var h = request.Height;
        if (w is null && h is null)
        {
            return Image.NewFromBuffer(bytes, access: Enums.Access.Sequential).Autorot();
        }

        var bothSides = w is not null && h is not null;
        switch (request.Fit)
        {
            case BxImageFit.Fill when bothSides:
                return Image.ThumbnailBuffer(bytes, w!.Value, height: h!.Value, size: Enums.Size.Force);
            case BxImageFit.Contain:
                return Image.ThumbnailBuffer(bytes, w ?? Unbounded, height: h ?? Unbounded, size: Enums.Size.Both);
            case BxImageFit.Inside:
                return Image.ThumbnailBuffer(bytes, w ?? Unbounded, height: h ?? Unbounded, size: Enums.Size.Down);
            case BxImageFit.Outside when bothSides:
            {
                // The smallest size that covers the box, without cropping.
                var scale = Math.Max((double)w!.Value / sourceWidth, (double)h!.Value / sourceHeight);
                return Image.ThumbnailBuffer(bytes, Math.Max(1, (int)Math.Ceiling(sourceWidth * scale)), size: Enums.Size.Down);
            }
            case BxImageFit.Cover when bothSides:
                return Image.ThumbnailBuffer(bytes, w!.Value, height: h!.Value, size: Enums.Size.Down, crop: Enums.Interesting.Centre);
            default:
                return Image.ThumbnailBuffer(bytes, w ?? Unbounded, height: h ?? Unbounded, size: Enums.Size.Down);
        }
    }

    private static string FormatOfSource(Image header)
    {
        var loader = header.Contains("vips-loader") ? header.Get("vips-loader") as string : null;
        return loader switch
        {
            not null when loader.StartsWith("png", StringComparison.Ordinal) => "png",
            not null when loader.StartsWith("webp", StringComparison.Ordinal) => "webp",
            not null when loader.StartsWith("heif", StringComparison.Ordinal) => "avif",
            // GIF and SVG sources become PNG (a single frame with transparency); a request can still ask for another format.
            not null when loader.StartsWith("gif", StringComparison.Ordinal) || loader.StartsWith("svg", StringComparison.Ordinal) => "png",
            _ => "jpeg",
        };
    }

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
            default:
            {
                // JPEG has no alpha channel: flatten onto white instead of letting transparent pixels turn black.
                using var flat = image.HasAlpha() ? image.Flatten(background: [255.0]) : null;
                return ((flat ?? image).WriteToBuffer($".jpg[Q={quality},strip,optimize_coding]"), "image/jpeg");
            }
        }
    }

    private static bool LooksLikeSvg(byte[] bytes)
    {
        var head = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 1024));
        return head.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtensionOf(string contentType) => contentType switch
    {
        "image/webp" => ".webp",
        "image/avif" => ".avif",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/svg+xml" => ".svg",
        _ => ".jpg",
    };

    private static string ContentTypeOf(string extension) => extension.ToLowerInvariant() switch
    {
        ".webp" => "image/webp",
        ".avif" => "image/avif",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        _ => "image/jpeg",
    };

    // The cache is bounded: once the total exceeds the limit, the oldest files go until it is back to 90 percent.
    private void Track(long added)
    {
        if (Interlocked.Read(ref _cacheBytes) < 0)
        {
            lock (_pruneGate)
            {
                if (_cacheBytes < 0) _cacheBytes = Directory.Exists(_options.CacheDirectory) ? SumBytes() : 0;
            }
        }
        if (Interlocked.Add(ref _cacheBytes, added) > _options.MaxCacheBytes) Prune();
    }

    private long SumBytes() => new DirectoryInfo(_options.CacheDirectory).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

    private void Prune()
    {
        lock (_pruneGate)
        {
            var root = new DirectoryInfo(_options.CacheDirectory);
            if (!root.Exists) return;
            var files = root.EnumerateFiles("*", SearchOption.AllDirectories).OrderBy(f => f.LastWriteTimeUtc).ToList();
            var total = files.Sum(f => f.Length);
            var target = (long)(_options.MaxCacheBytes * 0.9);
            foreach (var file in files)
            {
                if (total <= target) break;
                try
                {
                    total -= file.Length;
                    file.Delete();
                    if (file.Directory is { } dir && !dir.EnumerateFileSystemInfos().Any()) dir.Delete();
                }
                catch (IOException) { }
            }
            Interlocked.Exchange(ref _cacheBytes, total);
        }
    }

    private static SocketsHttpHandler CreateHandler(BxLocalImagesOptions options) => new()
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

    internal static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal) return false;
        var b = address.GetAddressBytes();
        if (b.Length == 16) return (b[0] & 0xFE) != 0xFC;
        return !(b[0] == 0 || b[0] == 10 || b[0] >= 224
            || (b[0] == 100 && b[1] is >= 64 and <= 127)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 192 && b[1] == 0 && b[2] == 0));
    }
}
