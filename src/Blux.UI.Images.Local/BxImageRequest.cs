using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Blux.UI.Images.Local;

/// <summary>A validated, normalized request: every value is clamped, snapped or rejected before anything touches the disk or the network.</summary>
internal sealed record BxImageRequest(
    string Source,
    Uri? RemoteUri,
    int? Width,
    int? Height,
    int Quality,
    string? Format,
    BxImageFit Fit,
    int Blur,
    bool Versioned)
{
    public bool IsRemote => RemoteUri is not null;

    /// <summary>The canonical text the cache key is built from: equal requests give equal text, whatever the order or spelling of the query.</summary>
    public string CanonicalKey(string sourceStamp) =>
        string.Create(CultureInfo.InvariantCulture, $"{Source}|{sourceStamp}|w{Width}|h{Height}|q{Quality}|f{Format}|{Fit}|b{Blur}");
}

internal readonly record struct BxImageParseResult(BxImageRequest? Request, int Status, string? Message)
{
    public static BxImageParseResult Ok(BxImageRequest request) => new(request, 200, null);
    public static BxImageParseResult Fail(int status, string message) => new(null, status, message);
}

internal static class BxImageRequestParser
{
    internal const int MaxSourceLength = 2048;

    private static readonly string[] SourceExtensions = [".png", ".jpg", ".jpeg", ".webp", ".avif", ".gif", ".svg"];
    private static readonly string[] RasterFormats = ["webp", "avif", "jpeg", "png"];

    /// <param name="get">Reads a query value, null when absent.</param>
    /// <param name="options">The limits and allowlists.</param>
    public static BxImageParseResult Parse(Func<string, string?> get, BxLocalImagesOptions options)
    {
        var src = get("src");
        if (string.IsNullOrEmpty(src)) return BxImageParseResult.Fail(400, "Missing src.");
        if (src.Length > MaxSourceLength) return BxImageParseResult.Fail(400, "src is too long.");
        foreach (var c in src)
        {
            if (c < 0x20 || c == 0x7f) return BxImageParseResult.Fail(400, "Invalid src.");
        }

        Uri? remote = null;
        string source;
        if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(src, UriKind.Absolute, out remote) || !string.IsNullOrEmpty(remote.UserInfo) || string.IsNullOrEmpty(remote.Host))
            {
                return BxImageParseResult.Fail(400, "Invalid src.");
            }
            if (!options.AllowedRemoteHosts.Any(h => string.Equals(h, remote.Host, StringComparison.OrdinalIgnoreCase)))
            {
                return BxImageParseResult.Fail(403, "This host is not allowed.");
            }
            source = remote.AbsoluteUri;
        }
        else
        {
            // A local path under the root: no backslash (Windows separator), no drive or stream colon, no dot segment, no protocol-relative start.
            if (src.StartsWith("//", StringComparison.Ordinal) || src.Contains('\\') || src.Contains(':') || src.Contains('?') || src.Contains('#'))
            {
                return BxImageParseResult.Fail(400, "Invalid src.");
            }
            source = src.StartsWith('/') ? src : "/" + src;
            foreach (var segment in source.Split('/'))
            {
                if (segment is "." or "..") return BxImageParseResult.Fail(400, "Invalid src.");
            }
        }

        var path = remote?.AbsolutePath ?? source;
        if (!SourceExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
        {
            // A remote URL may carry the type in its Content-Type only; a local file must name an image extension.
            if (remote is null) return BxImageParseResult.Fail(415, "Not an image.");
        }

        int? width = null, height = null;
        if (get("w") is { } w)
        {
            if (!int.TryParse(w, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1) return BxImageParseResult.Fail(400, "Invalid w.");
            width = SnapWidth(Math.Min(parsed, options.MaxWidth), options);
        }
        if (get("h") is { } h)
        {
            if (!int.TryParse(h, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 1) return BxImageParseResult.Fail(400, "Invalid h.");
            height = Math.Min(parsed, options.MaxHeight);
        }

        var quality = options.DefaultQuality;
        if (get("q") is { } q)
        {
            if (!int.TryParse(q, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) return BxImageParseResult.Fail(400, "Invalid q.");
            quality = BxLocalImageProvider.SnapQuality(Math.Clamp(parsed, 1, 100));
        }

        string? format = null;
        if (get("f") is { Length: > 0 } f)
        {
            format = f.ToLowerInvariant() == "jpg" ? "jpeg" : f.ToLowerInvariant();
            if (format != "svg" && !RasterFormats.Contains(format)) return BxImageParseResult.Fail(400, "Unsupported f.");
        }

        var fit = BxImageFit.Cover;
        if (get("fit") is { Length: > 0 } fitText && !Enum.TryParse(fitText, ignoreCase: true, out fit)) return BxImageParseResult.Fail(400, "Invalid fit.");
        if (!Enum.IsDefined(fit)) return BxImageParseResult.Fail(400, "Invalid fit.");

        var blur = 0;
        if (get("blur") is { } blurText)
        {
            if (!int.TryParse(blurText, NumberStyles.None, CultureInfo.InvariantCulture, out blur)) return BxImageParseResult.Fail(400, "Invalid blur.");
            blur = Math.Clamp(blur, 0, 20);
        }

        return BxImageParseResult.Ok(new BxImageRequest(source, remote, width, height, quality, format, fit, blur, !string.IsNullOrEmpty(get("v"))));
    }

    private static int SnapWidth(int width, BxLocalImagesOptions options)
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
