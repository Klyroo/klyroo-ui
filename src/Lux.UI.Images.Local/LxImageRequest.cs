using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Lux.UI.Images.Local;

/// <summary>A validated, normalized request: every value is clamped, snapped or rejected before anything touches the disk or the network.</summary>
internal sealed record LxImageRequest(
    string Source,
    Uri? RemoteUri,
    int? Width,
    int? Height,
    int Quality,
    string? Format,
    LxImageFit Fit,
    int Blur,
    bool Versioned)
{
    public bool IsRemote => RemoteUri is not null;

    /// <summary>The canonical text the cache key is built from: equal requests give equal text, whatever the order or spelling of the query.</summary>
    public string CanonicalKey(string sourceStamp) =>
        string.Create(CultureInfo.InvariantCulture, $"g2|{Source}|{sourceStamp}|w{Width}|h{Height}|q{Quality}|f{Format}|{Fit}|b{Blur}");
}

internal readonly record struct LxImageParseResult(LxImageRequest? Request, int Status, string? Message)
{
    public static LxImageParseResult Ok(LxImageRequest request) => new(request, 200, null);
    public static LxImageParseResult Fail(int status, string message) => new(null, status, message);
}

internal static class LxImageRequestParser
{
    internal const int MaxSourceLength = 2048;

    private static readonly string[] SourceExtensions = [".png", ".jpg", ".jpeg", ".webp", ".avif", ".gif", ".svg"];
    private static readonly string[] RasterFormats = ["webp", "avif", "jpeg", "png", "gif"];

    /// <param name="get">Reads a query value, null when absent.</param>
    /// <param name="options">The limits and allowlists.</param>
    public static LxImageParseResult Parse(Func<string, string?> get, LxLocalImagesOptions options)
    {
        var src = get("src");
        if (string.IsNullOrEmpty(src)) return LxImageParseResult.Fail(400, "Missing src.");
        if (src.Length > MaxSourceLength) return LxImageParseResult.Fail(400, "src is too long.");
        foreach (var c in src)
        {
            if (c < 0x20 || c == 0x7f) return LxImageParseResult.Fail(400, "Invalid src.");
        }

        Uri? remote = null;
        string source;
        if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(src, UriKind.Absolute, out remote) || !string.IsNullOrEmpty(remote.UserInfo) || string.IsNullOrEmpty(remote.Host))
            {
                return LxImageParseResult.Fail(400, "Invalid src.");
            }
            if (!options.IsAllowedRemote(remote))
            {
                return LxImageParseResult.Fail(403, "This host is not allowed.");
            }
            // One spelling per address: lower-case scheme and host, no default port, no fragment, no credentials. The query is kept as it is,
            // because an origin may take its image id or a signature from it.
            source = remote.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
            remote = new Uri(source);
        }
        else
        {
            // A local path under the root: no backslash (Windows separator), no drive or stream colon, no dot segment, no protocol-relative start.
            if (src.StartsWith("//", StringComparison.Ordinal) || src.Contains('\\') || src.Contains(':') || src.Contains('?') || src.Contains('#'))
            {
                return LxImageParseResult.Fail(400, "Invalid src.");
            }
            // One spelling per file: empty segments collapse, so /img/a.png, /img//a.png and img///a.png are the same source.
            var segments = src.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (segment is "." or "..") return LxImageParseResult.Fail(400, "Invalid src.");
            }
            source = "/" + string.Join('/', segments);
        }

        var path = remote?.AbsolutePath ?? source;
        if (!SourceExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
        {
            // A remote URL may carry the type in its Content-Type only; a local file must name an image extension.
            if (remote is null) return LxImageParseResult.Fail(415, "Not an image.");
        }

        int? width = null, height = null;
        int? requestedWidth = null;
        if (get("w") is { } w)
        {
            if (!int.TryParse(w, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1) return LxImageParseResult.Fail(400, "Invalid w.");
            requestedWidth = parsed;
            width = SnapWidth(Math.Min(parsed, options.MaxWidth), options);
        }
        if (get("h") is { } h)
        {
            if (!int.TryParse(h, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1) return LxImageParseResult.Fail(400, "Invalid h.");
            // When the width was snapped or clamped, the height follows by the same factor so the crop keeps the ratio that was asked for.
            if (width is { } snapped && requestedWidth is { } asked && snapped != asked) parsed = (int)Math.Max(1, Math.Round((double)parsed * snapped / asked));
            height = Math.Min(parsed, options.MaxHeight);
        }

        var quality = options.DefaultQuality;
        if (get("q") is { } q)
        {
            if (!int.TryParse(q, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) return LxImageParseResult.Fail(400, "Invalid q.");
            quality = LxLocalImageProvider.SnapQuality(Math.Clamp(parsed, 1, 100));
        }

        string? format = null;
        if (get("f") is { Length: > 0 } f)
        {
            format = f.ToLowerInvariant() == "jpg" ? "jpeg" : f.ToLowerInvariant();
            if (format != "svg" && !RasterFormats.Contains(format)) return LxImageParseResult.Fail(400, "Unsupported f.");
        }

        var fit = LxImageFit.Cover;
        if (get("fit") is { Length: > 0 } fitText && !Enum.TryParse(fitText, ignoreCase: true, out fit)) return LxImageParseResult.Fail(400, "Invalid fit.");
        if (!Enum.IsDefined(fit)) return LxImageParseResult.Fail(400, "Invalid fit.");

        var blur = 0;
        if (get("blur") is { } blurText)
        {
            if (!int.TryParse(blurText, NumberStyles.None, CultureInfo.InvariantCulture, out blur)) return LxImageParseResult.Fail(400, "Invalid blur.");
            blur = Math.Clamp(blur, 0, 20);
        }

        return LxImageParseResult.Ok(new LxImageRequest(source, remote, width, height, quality, format, fit, blur, !string.IsNullOrEmpty(get("v"))));
    }

    private static int SnapWidth(int width, LxLocalImagesOptions options)
    {
        var widths = options.Widths;
        if (widths.Count == 0) return width;
        var best = int.MaxValue;
        foreach (var step in widths)
        {
            if (step >= width && step < best) best = step;
        }
        return best == int.MaxValue ? widths.Max() : best;
    }

    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
