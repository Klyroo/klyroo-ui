namespace Klyroo.UI;

/// <summary>How the image fills the requested width and height. Providers map it to their own vocabulary.</summary>
public enum KlyImageFit
{
    /// <summary>Fill the box and crop what overflows (default). The local optimizer enlarges a smaller picture to fill the box.</summary>
    Cover,
    /// <summary>Fit inside the box without cropping and without distorting. The local optimizer then pads to exactly the box (transparent, or white for JPEG); other providers leave the free space out.</summary>
    Contain,
    /// <summary>Stretch to the box, distorting the picture when the ratios differ.</summary>
    Fill,
    /// <summary>Resize to stay inside the box, keeping the ratio, never enlarging.</summary>
    Inside,
    /// <summary>Resize to cover the box, keeping the ratio, without cropping.</summary>
    Outside,
}

/// <summary>The format of the image a provider is asked to produce. A format that is not listed goes through <see cref="KlyImageOptions.Modifiers"/>: a custom <see cref="IKlyImageProvider"/> reads it from <see cref="KlyImageModifiers.Extra"/> and writes it into its URL (the built-in providers ignore the modifiers).</summary>
public enum KlyImageFormat
{
    /// <summary>WebP (<c>webp</c>).</summary>
    WebP,
    /// <summary>AVIF (<c>avif</c>).</summary>
    Avif,
    /// <summary>JPEG (<c>jpeg</c>; a service that spells it <c>jpg</c> gets that spelling from its provider).</summary>
    Jpeg,
    /// <summary>PNG (<c>png</c>).</summary>
    Png,
    /// <summary>GIF (<c>gif</c>).</summary>
    Gif,
    /// <summary>SVG (<c>svg</c>), a vector image that is not converted.</summary>
    Svg,
    /// <summary>The service picks the best format the browser accepts (<c>auto</c>). Only a service that negotiates honors it; the local optimizer rejects it.</summary>
    Auto,
}

/// <summary>
/// What <see cref="KlyImage"/> paints behind the image while it loads: <see cref="Auto"/> asks the image provider for a tiny
/// (and, when it can blur, blurred) version of the image, <see cref="Url(string)"/> uses a small image you already have.
/// </summary>
public sealed class KlyImagePlaceholder
{
    private KlyImagePlaceholder(string? address) => Address = address;

    /// <summary>The address of the placeholder image, or <c>null</c> for <see cref="Auto"/>.</summary>
    internal string? Address { get; }

    /// <summary>A tiny version of the image itself, made by the image provider (blurred when it can blur).</summary>
    public static KlyImagePlaceholder Auto { get; } = new(null);

    /// <summary>A small image at <paramref name="address"/>, used as is: a root-relative path or an absolute URL, or a <c>data:</c> URL. An empty address shows no placeholder.</summary>
    /// <param name="address">Where the placeholder image is.</param>
    public static KlyImagePlaceholder Url(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new KlyImagePlaceholder(address);
    }
}

/// <summary>The <c>loading</c> attribute of an image.</summary>
public enum KlyImageLoading
{
    /// <summary>Wait until the image is near the viewport (the default).</summary>
    Lazy,
    /// <summary>Load right away. Use it for the image that is visible at first paint.</summary>
    Eager,
}

/// <summary>The <c>fetchpriority</c> attribute of an image and of its preload link.</summary>
public enum KlyFetchPriority
{
    /// <summary>Lets the browser decide.</summary>
    Auto,
    /// <summary>Loads the image before others.</summary>
    High,
    /// <summary>Loads the image after others.</summary>
    Low,
}

/// <summary>What a provider is asked to produce for one URL: the source resized, converted and compressed as described.</summary>
/// <param name="Width">Pixels, or null to keep the width (the ratio is kept when only one side is given).</param>
/// <param name="Height">Pixels, or null.</param>
/// <param name="Quality">1 to 100, or null for the provider default.</param>
/// <param name="Format">The output format; null keeps the format of the source (or lets the provider negotiate).</param>
/// <param name="Fit">How to fill <paramref name="Width"/> by <paramref name="Height"/>; null means the provider default (<see cref="KlyImageFit.Cover"/>).</param>
/// <param name="Blur">Blur radius for placeholders. Only asked from providers that report <see cref="IKlyImageProvider.SupportsBlur"/>.</param>
/// <param name="Extra">Provider specific modifiers, from the <c>Modifiers</c> parameter (for example <c>"gravity"</c> or <c>"sharpen"</c>). A provider ignores the ones it does not know.</param>
public sealed record KlyImageModifiers(
    int? Width = null,
    int? Height = null,
    int? Quality = null,
    KlyImageFormat? Format = null,
    KlyImageFit? Fit = null,
    int? Blur = null,
    IReadOnlyDictionary<string, string>? Extra = null);

/// <summary>The attributes <see cref="KlyImageSettings.Resolve"/> computed for an image.</summary>
/// <param name="Src">The <c>src</c> attribute: the largest variant when there is a <c>sizes</c> list, the 1x variant otherwise.</param>
/// <param name="SrcSet">The <c>srcset</c> attribute, or null when the provider cannot resize or nothing varies.</param>
/// <param name="Sizes">The <c>sizes</c> attribute, or null when no sizes were asked for.</param>
public sealed record KlyResolvedImage(string Src, string? SrcSet, string? Sizes);

/// <summary>
/// Turns a source image into the URL of a resized, converted version of it.
/// Register providers with <see cref="KlyImageSettings.AddProvider"/> and pick one with <see cref="KlyImageSettings.DefaultProvider"/>
/// or the <c>Provider</c> parameter of <see cref="KlyImage"/>. Implementations must be thread-safe and produce the same URL for the same
/// input on the server and in WebAssembly (no secrets: the URL is public).
/// </summary>
public interface IKlyImageProvider
{
    /// <summary>The name used by <c>Provider</c> parameters and <see cref="KlyImageSettings.DefaultProvider"/>.</summary>
    string Name { get; }

    /// <summary>False when the provider cannot resize (<c>none</c>): <see cref="KlyImage"/> then renders no <c>srcset</c> and no <c>sizes</c>.</summary>
    bool CanResize { get; }

    /// <summary>The URL of <paramref name="src"/> with <paramref name="modifiers"/> applied. <paramref name="src"/> has a leading slash when it is a local path.</summary>
    string GetUrl(string src, KlyImageModifiers modifiers);

    /// <summary>True when the provider can blur (see <see cref="KlyImageModifiers.Blur"/>), which makes <c>Placeholder</c> look better. Defaults to false.</summary>
    bool SupportsBlur => false;

    /// <summary>
    /// The width, in pixels, of the image the URL for <paramref name="width"/> will really return, for the <c>w</c> descriptors of the <c>srcset</c>.
    /// A provider that rounds widths to a fixed set (<c>local</c>, <c>vercel</c>) returns the rounded one, so that a "400w" candidate is not a 512 pixel
    /// image; the others produce the width they are asked for. Defaults to <paramref name="width"/>.
    /// </summary>
    int GetEffectiveWidth(int width) => width;

    /// <summary>
    /// True when <see cref="KlyImageSettings.Domains"/> applies: a remote source (<c>https://host/...</c>) whose host is not listed is passed through
    /// untouched instead of being sent to the provider. Defaults to false, for providers
    /// whose sources are paths in their own storage.
    /// </summary>
    bool ValidateDomains => false;
}
