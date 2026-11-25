namespace Klyroo.UI;

/// <summary>
/// Image settings that a component forwards to <see cref="KlyImage"/>
/// (<c>:image="{ src, format: 'webp', quality: 80 }"</c>). The same shape describes the entries of <see cref="KlyImageSettings.Presets"/>.
/// Every value left null falls through to the preset, then to the global defaults.
/// </summary>
public sealed record KlyImageOptions
{
    /// <summary>Output format: "webp", "avif", "jpeg", "png", "gif" or "svg".</summary>
    public string? Format { get; init; }

    /// <summary>Quality from 1 to 100.</summary>
    public int? Quality { get; init; }

    /// <summary>Name of a registered <see cref="IKlyImageProvider"/>; the default provider when null.</summary>
    public string? Provider { get; init; }

    /// <summary>Name of an entry of <see cref="KlyImageSettings.Presets"/>, applied below the values set here.</summary>
    public string? Preset { get; init; }

    /// <summary>
    /// Responsive sizes: space-separated entries, an unprefixed one for the default and prefixed ones for the
    /// breakpoints of <see cref="KlyImageSettings.Screens"/>, each applying from its breakpoint upward: <c>"100vw sm:50vw md:400px"</c>.
    /// </summary>
    public string? Sizes { get; init; }

    /// <summary>Pixel densities of the <c>srcset</c>, like <c>"x1 x2"</c> (the default is <see cref="KlyImageSettings.Densities"/>).</summary>
    public string? Densities { get; init; }

    /// <summary>How the image fills its width and height (cover when null).</summary>
    public KlyImageFit? Fit { get; init; }

    /// <summary>Provider specific modifiers, merged key by key over the preset's.</summary>
    public IReadOnlyDictionary<string, string>? Modifiers { get; init; }

    /// <summary>Lazy by default, eager when <see cref="Preload"/> is set.</summary>
    public KlyImageLoading? Loading { get; init; }

    /// <summary>Adds a <c>&lt;link rel="preload" as="image"&gt;</c> to the head. Needs a <c>HeadOutlet</c> in the app.</summary>
    public bool? Preload { get; init; }

    /// <summary>
    /// The values of this instance, with the ones it leaves null taken from <paramref name="fallback"/>.
    /// <see cref="Modifiers"/> are merged key by key.
    /// </summary>
    public KlyImageOptions OverDefaultsOf(KlyImageOptions? fallback)
    {
        if (fallback is null) return this;
        IReadOnlyDictionary<string, string>? modifiers = Modifiers;
        if (fallback.Modifiers is { Count: > 0 } below)
        {
            var merged = new Dictionary<string, string>(below, StringComparer.Ordinal);
            if (Modifiers is not null) foreach (var (key, value) in Modifiers) merged[key] = value;
            modifiers = merged;
        }
        return new KlyImageOptions
        {
            Format = Format ?? fallback.Format,
            Quality = Quality ?? fallback.Quality,
            Provider = Provider ?? fallback.Provider,
            Preset = Preset ?? fallback.Preset,
            Sizes = Sizes ?? fallback.Sizes,
            Densities = Densities ?? fallback.Densities,
            Fit = Fit ?? fallback.Fit,
            Modifiers = modifiers,
            Loading = Loading ?? fallback.Loading,
            Preload = Preload ?? fallback.Preload,
        };
    }
}
