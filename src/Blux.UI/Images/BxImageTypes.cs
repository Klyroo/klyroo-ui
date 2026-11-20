namespace Blux.UI;

/// <summary>How the image fills the requested width and height, like the <c>fit</c> prop of <c>NuxtImg</c>. Providers map it to their own vocabulary.</summary>
public enum BxImageFit
{
    /// <summary>Fill the box and crop what overflows (the default of Nuxt Image).</summary>
    Cover,
    /// <summary>Fit inside the box without cropping and without distorting.</summary>
    Contain,
    /// <summary>Stretch to the box, distorting the picture when the ratios differ.</summary>
    Fill,
    /// <summary>Resize to stay inside the box, keeping the ratio, never enlarging.</summary>
    Inside,
    /// <summary>Resize to cover the box, keeping the ratio, without cropping.</summary>
    Outside,
}

/// <summary>The <c>loading</c> attribute of an image.</summary>
public enum BxImageLoading
{
    /// <summary>Wait until the image is near the viewport (the default).</summary>
    Lazy,
    /// <summary>Load right away. Use it for the image that is visible at first paint.</summary>
    Eager,
}

/// <summary>The <c>fetchpriority</c> attribute of an image and of its preload link.</summary>
public enum BxFetchPriority
{
    Auto,
    High,
    Low,
}

/// <summary>What a provider is asked to produce for one URL: the source resized, converted and compressed as described.</summary>
/// <param name="Width">Pixels, or null to keep the width (the ratio is kept when only one side is given).</param>
/// <param name="Height">Pixels, or null.</param>
/// <param name="Quality">1 to 100, or null for the provider default.</param>
/// <param name="Format">"webp", "avif", "jpeg", "png", "gif" or "svg"; null keeps the format of the source (or lets the provider negotiate).</param>
/// <param name="Fit">How to fill <paramref name="Width"/> by <paramref name="Height"/>; null means the provider default (<see cref="BxImageFit.Cover"/>).</param>
/// <param name="Blur">Blur radius for placeholders. Only asked from providers that report <see cref="IBxImageProvider.SupportsBlur"/>.</param>
/// <param name="Extra">Provider specific modifiers, from the <c>Modifiers</c> parameter (for example <c>"gravity"</c> or <c>"sharpen"</c>). A provider ignores the ones it does not know.</param>
public sealed record BxImageModifiers(
    int? Width = null,
    int? Height = null,
    int? Quality = null,
    string? Format = null,
    BxImageFit? Fit = null,
    int? Blur = null,
    IReadOnlyDictionary<string, string>? Extra = null);

/// <summary>The attributes <see cref="BxImageSettings.Resolve"/> computed for an image.</summary>
/// <param name="Src">The <c>src</c> attribute: the largest variant when there is a <c>sizes</c> list, the 1x variant otherwise.</param>
/// <param name="SrcSet">The <c>srcset</c> attribute, or null when the provider cannot resize or nothing varies.</param>
/// <param name="Sizes">The <c>sizes</c> attribute, or null when no sizes were asked for.</param>
public sealed record BxResolvedImage(string Src, string? SrcSet, string? Sizes);

/// <summary>
/// Turns a source image into the URL of a resized, converted version of it.
/// Register providers with <see cref="BxImageSettings.AddProvider"/> and pick one with <see cref="BxImageSettings.DefaultProvider"/>
/// or the <c>Provider</c> parameter of <see cref="BxImage"/>. Implementations must be thread-safe and produce the same URL for the same
/// input on the server and in WebAssembly (no secrets: the URL is public).
/// </summary>
public interface IBxImageProvider
{
    /// <summary>The name used by <c>Provider</c> parameters and <see cref="BxImageSettings.DefaultProvider"/>.</summary>
    string Name { get; }

    /// <summary>False when the provider cannot resize (<c>none</c>): <see cref="BxImage"/> then renders no <c>srcset</c> and no <c>sizes</c>.</summary>
    bool CanResize { get; }

    /// <summary>The URL of <paramref name="src"/> with <paramref name="modifiers"/> applied. <paramref name="src"/> has a leading slash when it is a local path.</summary>
    string GetUrl(string src, BxImageModifiers modifiers);

    /// <summary>True when the provider can blur (see <see cref="BxImageModifiers.Blur"/>), which makes <c>Placeholder</c> look better. Defaults to false.</summary>
    bool SupportsBlur => false;

    /// <summary>
    /// True when <see cref="BxImageSettings.Domains"/> applies: a remote source (<c>https://host/...</c>) whose host is not listed is passed through
    /// untouched instead of being sent to the provider, like Nuxt Image does for IPX, Netlify and Vercel. Defaults to false, for providers
    /// whose sources are paths in their own storage.
    /// </summary>
    bool ValidateDomains => false;
}
