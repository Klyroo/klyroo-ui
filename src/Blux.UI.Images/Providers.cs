using System.Globalization;
using System.Text;

namespace Blux.UI.Images;

/// <summary>Shared helpers of the providers.</summary>
internal static class ProviderUrl
{
    public static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static int Quality(int value) => Math.Clamp(value, 1, 100);

    public static bool IsAbsolute(string src) =>
        src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || src.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || src.StartsWith("//", StringComparison.Ordinal);

    public static string Join(string baseUrl, string path) =>
        baseUrl.TrimEnd('/') + (path.StartsWith('/') ? path : "/" + path);

    public static string Query(IEnumerable<(string Key, string Value)> pairs)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in pairs)
        {
            builder.Append(builder.Length == 0 ? '?' : '&').Append(key).Append('=').Append(Uri.EscapeDataString(value));
        }
        return builder.ToString();
    }

    // Percent-encoding that keeps "/" and ":" readable, as the services' own examples show (url=/owl.jpeg, url=https://host/a.jpg).
    public static string ReadableEscape(string value) =>
        Uri.EscapeDataString(value).Replace("%2F", "/", StringComparison.Ordinal).Replace("%3A", ":", StringComparison.Ordinal);

    public static string NormalizeFormat(string format) => format.ToLowerInvariant() switch { "jpg" => "jpeg", var f => f };
}

/// <summary>
/// Cloudinary. Delivery URL structure from https://cloudinary.com/documentation/image_transformations:
/// <c>https://res.cloudinary.com/&lt;cloud_name&gt;/&lt;asset_type&gt;/&lt;delivery_type&gt;/&lt;transformations&gt;/&lt;version&gt;/&lt;public_id&gt;.&lt;extension&gt;</c>,
/// parameters of one component separated by commas, for example
/// <c>https://res.cloudinary.com/demo/image/upload/c_fill,h_400,w_250/q_auto/f_auto/docs/shoes.jpg</c>.
/// Parameters: <c>w_</c>, <c>h_</c> (https://cloudinary.com/documentation/resizing_and_cropping), <c>c_</c> crop modes (<c>fill</c>, <c>fit</c>, <c>scale</c>, <c>limit</c>,
/// https://cloudinary.com/documentation/transformation_reference), <c>q_1..100</c> (https://cloudinary.com/documentation/image_optimization),
/// <c>f_&lt;format&gt;</c> / <c>f_auto</c>, <c>e_blur[:&lt;strength&gt;]</c> (transformation reference).
/// An absolute source uses the fetch delivery type, <c>image/fetch/&lt;transformations&gt;/&lt;remote url&gt;</c> (https://cloudinary.com/documentation/fetch_remote_images);
/// the <c>src</c> is a public id otherwise. <see cref="IBxImageProvider.ValidateDomains"/> is off: fetching is limited by your Cloudinary account settings.
/// Fit mapping: cover is <c>c_fill</c>, contain <c>c_fit</c>, fill <c>c_scale</c>, inside <c>c_limit</c>; outside has no equivalent and uses <c>c_fill</c>.
/// </summary>
public sealed class CloudinaryImageProvider(string cloudName, string name = "cloudinary") : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool SupportsBlur => true;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var parts = new List<string>();
        if (m.Width is not null || m.Height is not null)
        {
            parts.Add("c_" + (m.Fit ?? BxImageFit.Cover) switch
            {
                BxImageFit.Contain => "fit",
                BxImageFit.Fill => "scale",
                BxImageFit.Inside => "limit",
                _ => "fill",
            });
        }
        if (m.Height is { } h) parts.Add("h_" + ProviderUrl.Num(h));
        if (m.Width is { } w) parts.Add("w_" + ProviderUrl.Num(w));
        if (m.Quality is { } q) parts.Add("q_" + ProviderUrl.Quality(q));
        if (m.Format is { } f) parts.Add("f_" + ProviderUrl.NormalizeFormat(f).Replace("jpeg", "jpg"));
        if (m.Blur is { } b) parts.Add("e_blur:" + ProviderUrl.Num(b * 100));
        var transformations = string.Join(",", parts);
        var prefix = $"https://res.cloudinary.com/{Uri.EscapeDataString(cloudName)}/image/";
        if (ProviderUrl.IsAbsolute(src))
        {
            return prefix + "fetch/" + (transformations.Length > 0 ? transformations + "/" : "") + src;
        }
        return prefix + "upload/" + (transformations.Length > 0 ? transformations + "/" : "") + src.TrimStart('/');
    }
}

/// <summary>
/// imgix Rendering API. Query string form, for example <c>https://assets.imgix.net/docs/road-sm.jpg?w=300&amp;h=100&amp;fit=crop</c>
/// (https://docs.imgix.com/apis/rendering/size/fit). <c>w</c>/<c>h</c>: https://docs.imgix.com/apis/rendering/size/w and .../size/h;
/// <c>q</c> 0 to 100 (https://docs.imgix.com/apis/rendering/format/q); <c>fm</c> (https://docs.imgix.com/apis/rendering/format/fm);
/// <c>blur</c> 0 to 2000 (https://docs.imgix.com/apis/rendering/stylize/blur); <c>auto=format</c> when the format is <c>auto</c>
/// (https://docs.imgix.com/apis/rendering/auto/auto). Fit mapping: cover is <c>crop</c>, contain <c>clip</c>, fill <c>scale</c>, inside <c>max</c>;
/// outside has no equivalent and uses <c>crop</c>. <paramref name="domain"/> is the host of your source, for example <c>assets.imgix.net</c>.
/// Absolute sources (imgix Web Proxy sources) are not supported: they need a signing key, which cannot be part of a public URL built on both
/// the server and the client. Use a Web Folder or S3 source and give paths.
/// </summary>
public sealed class ImgixImageProvider(string domain, string name = "imgix") : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool SupportsBlur => true;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var pairs = new List<(string, string)>();
        if (m.Width is { } w) pairs.Add(("w", ProviderUrl.Num(w)));
        if (m.Height is { } h) pairs.Add(("h", ProviderUrl.Num(h)));
        if ((m.Width is not null || m.Height is not null) && m.Fit is not null)
        {
            pairs.Add(("fit", m.Fit switch { BxImageFit.Contain => "clip", BxImageFit.Fill => "scale", BxImageFit.Inside => "max", _ => "crop" }));
        }
        if (m.Quality is { } q) pairs.Add(("q", ProviderUrl.Num(ProviderUrl.Quality(q))));
        if (m.Format is { } f)
        {
            if (f.Equals("auto", StringComparison.OrdinalIgnoreCase)) pairs.Add(("auto", "format"));
            else pairs.Add(("fm", f.ToLowerInvariant() == "jpeg" ? "jpg" : f.ToLowerInvariant()));
        }
        if (m.Blur is { } b) pairs.Add(("blur", ProviderUrl.Num(b * 100)));
        var path = ProviderUrl.IsAbsolute(src) ? "/" + Uri.EscapeDataString(src) : src;
        return ProviderUrl.Join("https://" + domain.Trim().TrimEnd('/'), path) + QueryRaw(pairs);
    }

    // The values here never need escaping, and imgix documents the plain form (w=300&h=100&fit=crop).
    private static string QueryRaw(List<(string Key, string Value)> pairs) =>
        pairs.Count == 0 ? "" : "?" + string.Join("&", pairs.Select(p => $"{p.Key}={p.Value}"));
}

/// <summary>
/// ImageKit URL transformations, path form (https://imagekit.io/docs/transformations):
/// <c>https://ik.imagekit.io/ikmedia/tr:h-300,w-400,f-jpg,cp-false/docs_images/examples/example_food_3.jpg</c>.
/// <c>w-</c>, <c>h-</c> (https://imagekit.io/docs/image-transformation), <c>q-</c> and <c>f-</c> (https://imagekit.io/docs/image-optimization),
/// <c>bl-</c> 1 to 100 (https://imagekit.io/docs/effects-and-enhancements), <c>c-</c> crop strategies (https://imagekit.io/docs/image-resize-and-crop).
/// Fit mapping: cover is <c>c-maintain_ratio</c> (the default strategy, cropping to the exact size), contain and inside <c>c-at_max</c>, fill <c>c-force</c>,
/// outside <c>c-at_least</c>. <paramref name="urlEndpoint"/> is your endpoint, for example <c>https://ik.imagekit.io/ikmedia</c>.
/// </summary>
public sealed class ImageKitImageProvider(string urlEndpoint, string name = "imagekit") : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool SupportsBlur => true;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var parts = new List<string>();
        if (m.Width is { } w) parts.Add("w-" + ProviderUrl.Num(w));
        if (m.Height is { } h) parts.Add("h-" + ProviderUrl.Num(h));
        if (m.Quality is { } q) parts.Add("q-" + ProviderUrl.Num(ProviderUrl.Quality(q)));
        if (m.Format is { } f) parts.Add("f-" + (f.ToLowerInvariant() == "jpeg" ? "jpg" : f.ToLowerInvariant()));
        if ((m.Width is not null && m.Height is not null) && m.Fit is not null)
        {
            parts.Add("c-" + m.Fit switch
            {
                BxImageFit.Contain or BxImageFit.Inside => "at_max",
                BxImageFit.Fill => "force",
                BxImageFit.Outside => "at_least",
                _ => "maintain_ratio",
            });
        }
        if (m.Blur is { } b) parts.Add("bl-" + ProviderUrl.Num(Math.Clamp(b * 5, 1, 100)));
        var path = src.TrimStart('/');
        return ProviderUrl.Join(urlEndpoint, parts.Count > 0 ? "tr:" + string.Join(",", parts) + "/" + path : path);
    }
}

/// <summary>
/// Cloudflare Images transformations via URL (https://developers.cloudflare.com/images/transform-images/transform-via-url/):
/// <c>https://&lt;ZONE&gt;/cdn-cgi/image/&lt;OPTIONS&gt;/&lt;SOURCE-IMAGE&gt;</c>, options comma separated, the source an absolute path on the origin or an absolute URL.
/// Options: <c>width</c>, <c>height</c>, <c>quality</c> (1 to 100), <c>format</c> (auto, avif, webp, jpeg), <c>fit</c> (scale-down, contain, cover, crop, pad, squeeze, scale-up),
/// <c>blur</c> (0 to 250). Fit mapping: cover and outside are <c>cover</c>, contain <c>contain</c>, fill <c>squeeze</c>, inside <c>scale-down</c>.
/// Formats the service does not list (png, gif, svg) are not requested. Remote sources must be listed in <see cref="BxImageSettings.Domains"/>
/// and allowed in your Cloudflare zone. <paramref name="zone"/> is optional, for example <c>https://example.com</c>; without it URLs are root-relative.
/// </summary>
public sealed class CloudflareImagesProvider(string? zone = null, string name = "cloudflare") : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool SupportsBlur => true;
    public bool ValidateDomains => true;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var options = new List<string>();
        if (m.Width is { } w) options.Add("width=" + ProviderUrl.Num(w));
        if (m.Height is { } h) options.Add("height=" + ProviderUrl.Num(h));
        if (m.Quality is { } q) options.Add("quality=" + ProviderUrl.Num(ProviderUrl.Quality(q)));
        if (m.Format is { } f && ProviderUrl.NormalizeFormat(f) is "auto" or "avif" or "webp" or "jpeg") options.Add("format=" + ProviderUrl.NormalizeFormat(f));
        if ((m.Width is not null || m.Height is not null) && m.Fit is not null)
        {
            options.Add("fit=" + m.Fit switch { BxImageFit.Contain => "contain", BxImageFit.Fill => "squeeze", BxImageFit.Inside => "scale-down", _ => "cover" });
        }
        if (m.Blur is { } b) options.Add("blur=" + ProviderUrl.Num(Math.Clamp(b * 10, 0, 250)));
        var prefix = (zone?.TrimEnd('/') ?? "") + "/cdn-cgi/image/" + (options.Count > 0 ? string.Join(",", options) : "format=auto") + "/";
        return prefix + (ProviderUrl.IsAbsolute(src) ? src : src.TrimStart('/'));
    }
}

/// <summary>
/// Netlify Image CDN (https://docs.netlify.com/image-cdn/overview/): <c>/.netlify/images?url=/owl.jpeg&amp;fit=cover&amp;w=50&amp;h=50</c>.
/// <c>url</c>, <c>w</c>, <c>h</c>, <c>fit</c> (contain, cover, fill), <c>fm</c> (avif, jpg, png, webp, gif), <c>q</c> 1 to 100.
/// The service has no blur option (only <c>fm=blurhash</c>), so <see cref="IBxImageProvider.SupportsBlur"/> is false. Fit mapping: cover and outside are <c>cover</c>,
/// contain and inside <c>contain</c>, fill <c>fill</c>. Remote sources need <c>remote_images</c> in <c>netlify.toml</c> and an entry in
/// <see cref="BxImageSettings.Domains"/> here. <paramref name="baseUrl"/> is optional (a site address), for URLs that leave the site.
/// </summary>
public sealed class NetlifyImageProvider(string? baseUrl = null, string name = "netlify") : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool ValidateDomains => true;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var pairs = new List<(string, string)> { ("url", src) };
        var sized = m.Width is not null || m.Height is not null;
        if (sized && (m.Fit is not null || (m.Width is not null && m.Height is not null)))
        {
            pairs.Add(("fit", (m.Fit ?? BxImageFit.Cover) switch { BxImageFit.Contain or BxImageFit.Inside => "contain", BxImageFit.Fill => "fill", _ => "cover" }));
        }
        if (m.Width is { } w) pairs.Add(("w", ProviderUrl.Num(w)));
        if (m.Height is { } h) pairs.Add(("h", ProviderUrl.Num(h)));
        if (m.Format is { } f && ProviderUrl.NormalizeFormat(f) is "avif" or "jpeg" or "png" or "webp" or "gif")
            pairs.Add(("fm", ProviderUrl.NormalizeFormat(f) == "jpeg" ? "jpg" : ProviderUrl.NormalizeFormat(f)));
        if (m.Quality is { } q) pairs.Add(("q", ProviderUrl.Num(ProviderUrl.Quality(q))));
        return (baseUrl?.TrimEnd('/') ?? "") + "/.netlify/images" + QueryKeepSlashes(pairs);
    }

    // Netlify's own examples keep "/" in url=/owl.jpeg readable.
    private static string QueryKeepSlashes(List<(string Key, string Value)> pairs) =>
        "?" + string.Join("&", pairs.Select(p => $"{p.Key}={ProviderUrl.ReadableEscape(p.Value)}"));
}

/// <summary>
/// Vercel Image Optimization (https://vercel.com/docs/image-optimization): <c>/_vercel/image?url={link/to/src/image}&amp;w=3840&amp;q=75</c>.
/// <c>url</c> is a relative or absolute address, <c>w</c> the width ("No height is needed since the source image aspect ratio is preserved") and
/// <c>q</c> 1 to 100. The allowed widths come from the <c>sizes</c> of the <c>images</c> configuration (https://vercel.com/docs/build-output-api/configuration),
/// so <paramref name="sizes"/> must list the same values: the width is rounded up to the next one (the largest when none fits), and when no
/// width is asked for the largest is used because <c>w</c> is required. Height, fit and format are not parameters of the service and are ignored
/// (the docs do not say how the format is chosen). Remote sources must be allowed by the project's <c>domains</c> or <c>remotePatterns</c> and
/// listed in <see cref="BxImageSettings.Domains"/>. The quality must be one of the project's <c>qualities</c> when it sets them; the default is 75, as in the docs.
/// </summary>
public sealed class VercelImageProvider(IReadOnlyList<int>? sizes = null, string name = "vercel") : IBxImageProvider
{
    // The sizes of the example in the build output documentation, plus the 3840 of the url example.
    private readonly int[] _sizes = (sizes is { Count: > 0 } ? sizes : [640, 750, 828, 1080, 1200, 1920, 2048, 3840]).OrderBy(s => s).ToArray();

    public string Name => name;
    public bool CanResize => true;
    public bool ValidateDomains => true;

    public int GetEffectiveWidth(int width) => _sizes.FirstOrDefault(s => s >= width, _sizes[^1]);

    public string GetUrl(string src, BxImageModifiers m)
    {
        var width = m.Width is { } w ? GetEffectiveWidth(w) : _sizes[^1];
        var quality = m.Quality is { } q ? ProviderUrl.Quality(q) : 75;
        return $"/_vercel/image?url={ProviderUrl.ReadableEscape(src)}&w={ProviderUrl.Num(width)}&q={ProviderUrl.Num(quality)}";
    }
}

/// <summary>
/// Bunny Optimizer dynamic images (https://bunny.net/docs/optimizer/dynamic-images/overview.md): query parameters on a Pull Zone URL, for example
/// <c>https://yourzone.b-cdn.net/image.jpg?quality=75&amp;format=webp</c>, <c>?width=500</c>, <c>?height=300</c>. The page documents no fit parameter, no accepted
/// range for quality and no way to pass a remote source, so fit and blur are ignored and the source is a path in the zone.
/// <paramref name="baseUrl"/> is the Pull Zone address, for example <c>https://yourzone.b-cdn.net</c>.
/// </summary>
public sealed class BunnyImageProvider(string baseUrl, string name = "bunny") : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;

    public string GetUrl(string src, BxImageModifiers m)
    {
        var pairs = new List<string>();
        if (m.Width is { } w) pairs.Add("width=" + ProviderUrl.Num(w));
        if (m.Height is { } h) pairs.Add("height=" + ProviderUrl.Num(h));
        if (m.Quality is { } q) pairs.Add("quality=" + ProviderUrl.Num(ProviderUrl.Quality(q)));
        if (m.Format is { } f) pairs.Add("format=" + (ProviderUrl.NormalizeFormat(f) == "jpeg" ? "jpg" : ProviderUrl.NormalizeFormat(f)));
        return ProviderUrl.Join(baseUrl, src) + (pairs.Count > 0 ? "?" + string.Join("&", pairs) : "");
    }
}

/// <summary>
/// Any service whose URLs follow a pattern. Placeholders: <c>{src}</c> (the source as given), <c>{encodedSrc}</c> (percent-encoded),
/// <c>{width}</c>, <c>{height}</c>, <c>{quality}</c>, <c>{format}</c>; a placeholder without a value becomes an empty string. Example:
/// <c>new TemplateImageProvider("acme", "https://img.acme.dev/{width}x{height}/q{quality}.{format}?u={encodedSrc}")</c>.
/// </summary>
public sealed class TemplateImageProvider(string name, string template, bool validateDomains = false) : IBxImageProvider
{
    public string Name => name;
    public bool CanResize => true;
    public bool ValidateDomains => validateDomains;

    public string GetUrl(string src, BxImageModifiers m) => template
        .Replace("{src}", src, StringComparison.Ordinal)
        .Replace("{encodedSrc}", Uri.EscapeDataString(src), StringComparison.Ordinal)
        .Replace("{width}", m.Width is { } w ? ProviderUrl.Num(w) : "", StringComparison.Ordinal)
        .Replace("{height}", m.Height is { } h ? ProviderUrl.Num(h) : "", StringComparison.Ordinal)
        .Replace("{quality}", m.Quality is { } q ? ProviderUrl.Num(ProviderUrl.Quality(q)) : "", StringComparison.Ordinal)
        .Replace("{format}", m.Format ?? "", StringComparison.Ordinal);
}

/// <summary>
/// URLs of the endpoint of <c>Blux.UI.Images.Local</c> (<c>app.MapBluxImages()</c>). This class only builds URLs, so it is safe
/// in a WebAssembly client: register it on the server and the client with <see cref="BxImageProviders.AddLocal"/>.
/// Widths are rounded up to <see cref="Steps"/> and the quality to a multiple of 5, so a page can only ask for a bounded set of variants; a height
/// is scaled with its width so the aspect ratio holds, and <see cref="GetEffectiveWidth"/> reports the rounded width for the <c>srcset</c>. <c>v</c> is a cache-busting token of yours (an application version): with it the server
/// answers with <c>immutable</c> caching.
/// <para>
/// The default endpoint is relative (<see cref="DefaultEndpoint"/>): the browser resolves it against <c>&lt;base href&gt;</c>, so an app deployed under a path base
/// (<c>UsePathBase("/x")</c> with <c>&lt;base href="/x/"&gt;</c>) reaches its own optimizer at <c>/x/_blux/img</c> in every render mode, and not the one of
/// another app on the same origin. An endpoint you pass that starts with "/" is used as it is, which is right only for an app served at the root.
/// </para>
/// </summary>
public sealed class BxLocalImageProvider(string endpoint = BxLocalImageProvider.DefaultEndpoint, string? version = null, string name = "local") : IBxImageProvider
{
    /// <summary>The endpoint when none is given, relative to the base of the app (<c>&lt;base href&gt;</c>), which <c>MapBluxImages</c> maps below the path base.</summary>
    public const string DefaultEndpoint = "_blux/img";

    /// <summary>The widths the local optimizer produces (the device and icon widths of Next.js, which suit most layouts).</summary>
    public static IReadOnlyList<int> Steps { get; } = [16, 32, 48, 64, 96, 128, 256, 384, 512, 640, 750, 828, 1080, 1200, 1920, 2048, 3840];

    public string Name => name;
    public bool CanResize => true;
    public bool ValidateDomains => true;

    /// <summary>The smallest step that is at least <paramref name="width"/>, the largest when none is.</summary>
    public static int SnapWidth(int width) => Steps.FirstOrDefault(s => s >= width, Steps[^1]);

    /// <summary>The quality rounded up to a multiple of 5 within 5 to 100.</summary>
    public static int SnapQuality(int quality) => Math.Clamp((Math.Clamp(quality, 1, 100) + 4) / 5 * 5, 5, 100);

    public int GetEffectiveWidth(int width) => SnapWidth(width);

    public string GetUrl(string src, BxImageModifiers m)
    {
        // The endpoint takes absolute URLs only (a protocol-relative one is how a path would be smuggled in): the scheme-less form is https.
        if (src.StartsWith("//", StringComparison.Ordinal)) src = "https:" + src;
        var pairs = new List<(string, string)> { ("src", src) };
        int? width = m.Width, height = m.Height;
        if (width is { } w)
        {
            var snapped = SnapWidth(w);
            if (height is { } h) height = Math.Max(1, (int)Math.Round((double)h * snapped / w));
            width = snapped;
        }
        if (width is { } sw) pairs.Add(("w", ProviderUrl.Num(sw)));
        if (height is { } sh) pairs.Add(("h", ProviderUrl.Num(sh)));
        if (m.Quality is { } q) pairs.Add(("q", ProviderUrl.Num(SnapQuality(q))));
        if (m.Format is { } f) pairs.Add(("f", ProviderUrl.NormalizeFormat(f)));
        if (m.Fit is { } fit && (m.Width is not null || m.Height is not null)) pairs.Add(("fit", fit.ToString().ToLowerInvariant()));
        if (m.Blur is { } b) pairs.Add(("blur", ProviderUrl.Num(Math.Clamp(b, 1, 20))));
        if (version is not null) pairs.Add(("v", version));
        return endpoint.TrimEnd('/') + ProviderUrl.Query(pairs);
    }

    public bool SupportsBlur => true;
}

/// <summary>Registration helpers: <c>o.Images.AddCloudinary("demo")</c>.</summary>
public static class BxImageProviders
{
    /// <summary>Registers <see cref="CloudinaryImageProvider"/>. <paramref name="makeDefault"/> also makes it the default provider.</summary>
    public static BxImageSettings AddCloudinary(this BxImageSettings images, string cloudName, bool makeDefault = false) => Register(images, new CloudinaryImageProvider(cloudName), makeDefault);

    public static BxImageSettings AddImgix(this BxImageSettings images, string domain, bool makeDefault = false) => Register(images, new ImgixImageProvider(domain), makeDefault);

    public static BxImageSettings AddImageKit(this BxImageSettings images, string urlEndpoint, bool makeDefault = false) => Register(images, new ImageKitImageProvider(urlEndpoint), makeDefault);

    public static BxImageSettings AddCloudflareImages(this BxImageSettings images, string? zone = null, bool makeDefault = false) => Register(images, new CloudflareImagesProvider(zone), makeDefault);

    public static BxImageSettings AddNetlify(this BxImageSettings images, string? baseUrl = null, bool makeDefault = false) => Register(images, new NetlifyImageProvider(baseUrl), makeDefault);

    public static BxImageSettings AddVercel(this BxImageSettings images, IReadOnlyList<int>? sizes = null, bool makeDefault = false) => Register(images, new VercelImageProvider(sizes), makeDefault);

    public static BxImageSettings AddBunny(this BxImageSettings images, string baseUrl, bool makeDefault = false) => Register(images, new BunnyImageProvider(baseUrl), makeDefault);

    public static BxImageSettings AddTemplate(this BxImageSettings images, string name, string template, bool makeDefault = false) => Register(images, new TemplateImageProvider(name, template), makeDefault);

    /// <summary>Registers the URL builder of the local optimizer under the name <c>local</c> (see <see cref="BxLocalImageProvider"/>).</summary>
    public static BxImageSettings AddLocal(this BxImageSettings images, string endpoint = BxLocalImageProvider.DefaultEndpoint, string? version = null, bool makeDefault = false) => Register(images, new BxLocalImageProvider(endpoint, version), makeDefault);

    private static BxImageSettings Register(BxImageSettings images, IBxImageProvider provider, bool makeDefault) =>
        makeDefault ? images.AddDefaultProvider(provider) : images.AddProvider(provider);
}
