using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Blux.UI;

/// <summary>
/// Global image configuration (<c>BluxOptions.Images</c>), the counterpart of the <c>image</c> key of Nuxt Image:
/// <c>AddBluxUi(o =&gt; { o.Images.Domains.Add("images.example.com"); o.Images.AddProvider(new MyProvider()); o.Images.DefaultProvider = "my"; })</c>.
/// </summary>
/// <remarks>
/// The settings become read-only the first time an image is resolved, like the colors and the themes: a later change throws
/// <see cref="InvalidOperationException"/>. Configure them identically on the server and in the WebAssembly client, otherwise the
/// URLs change when an Auto render mode switches runtime. Without a provider the default is <c>none</c>: images are rendered as they are,
/// with <c>width</c>, <c>height</c> and <c>loading</c> but no <c>srcset</c>.
/// </remarks>
public sealed class BxImageSettings
{
    private readonly object _gate = new();
    private readonly Dictionary<string, IBxImageProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<int> _densities = [1, 2];
    private string _defaultProvider = NoneProvider.ProviderName;
    private volatile bool _frozen;

    public BxImageSettings()
    {
        _providers[NoneProvider.ProviderName] = NoneProvider.Instance;
        Screens = new GuardedDictionary<string, int>(this, StringComparer.Ordinal)
        {
            ["sm"] = 640, ["md"] = 768, ["lg"] = 1024, ["xl"] = 1280, ["2xl"] = 1536,
        };
        Presets = new GuardedDictionary<string, BxImageOptions>(this, StringComparer.Ordinal);
        Domains = new GuardedSet(this);
    }

    /// <summary>Breakpoints of the <c>Sizes</c> syntax, in pixels. Defaults to Tailwind's: sm 640, md 768, lg 1024, xl 1280, 2xl 1536 (like <c>image.screens</c> in Nuxt Image).</summary>
    public IDictionary<string, int> Screens { get; }

    /// <summary>Named bundles of image options, selected with the <c>Preset</c> parameter (like <c>image.presets</c>).</summary>
    public IDictionary<string, BxImageOptions> Presets { get; }

    /// <summary>
    /// Hosts of the remote images a provider with <see cref="IBxImageProvider.ValidateDomains"/> may process (<c>"images.example.com"</c>;
    /// a full URL is reduced to its host). A remote <c>src</c> from any other host is passed through untouched, like Nuxt Image does.
    /// </summary>
    public ICollection<string> Domains { get; }

    /// <summary>Pixel densities of the generated <c>srcset</c>. Defaults to 1 and 2 (like <c>image.densities</c>). Never empty.</summary>
    public IReadOnlyList<int> Densities
    {
        get => _densities;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count == 0 || value.Any(d => d < 1)) throw new ArgumentException("Densities must hold at least one positive value; use [1] to render the regular size only.", nameof(value));
            ThrowIfFrozen();
            _densities = value.ToArray();
        }
    }

    /// <summary>The name of the provider used when none is asked for. Defaults to <c>none</c>.</summary>
    public string DefaultProvider
    {
        get => _defaultProvider;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            ThrowIfFrozen();
            _defaultProvider = value;
        }
    }

    /// <summary>The registered providers, <c>none</c> included.</summary>
    public IReadOnlyCollection<IBxImageProvider> Providers
    {
        get { lock (_gate) return _providers.Values.ToArray(); }
    }

    /// <summary>Registers a provider under its <see cref="IBxImageProvider.Name"/>, replacing a provider of the same name.</summary>
    public BxImageSettings AddProvider(IBxImageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider.Name);
        lock (_gate)
        {
            ThrowIfFrozen();
            _providers[provider.Name] = provider;
        }
        return this;
    }

    /// <summary>Registers a provider and makes it the default one.</summary>
    public BxImageSettings AddDefaultProvider(IBxImageProvider provider)
    {
        AddProvider(provider);
        DefaultProvider = provider.Name;
        return this;
    }

    internal void Freeze() => _frozen = true;

    internal void ThrowIfFrozen()
    {
        if (_frozen)
        {
            throw new InvalidOperationException("The image settings are read-only once an image was resolved. Configure them in AddBluxUi.");
        }
    }

    /// <summary>
    /// The <c>src</c>, <c>srcset</c> and <c>sizes</c> of an image, computed like <c>$img.getSizes</c> of Nuxt Image. Use it to render your own
    /// <c>img</c> or <c>picture</c>; <see cref="BxImage"/> calls it.
    /// </summary>
    /// <param name="src">A root-relative path ("/img/a.png") or an absolute URL.</param>
    /// <param name="options">Format, quality, sizes… and the name of a provider and of a preset.</param>
    /// <param name="width">Intrinsic width the 1x image should have, in pixels (and the base of the <c>x</c> descriptors).</param>
    /// <param name="height">Intrinsic height, in pixels.</param>
    /// <exception cref="InvalidOperationException">Unknown provider or preset.</exception>
    public BxResolvedImage Resolve(string? src, BxImageOptions? options = null, int? width = null, int? height = null)
    {
        Freeze();
        if (string.IsNullOrEmpty(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || src.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return new BxResolvedImage(src ?? "", null, null);
        }

        options ??= new BxImageOptions();
        BxImageOptions? preset = null;
        if (options.Preset is { Length: > 0 } presetName && !Presets.TryGetValue(presetName, out preset))
        {
            throw new InvalidOperationException($"Unknown image preset '{presetName}'. Add it to BluxOptions.Images.Presets.");
        }
        var merged = options.OverDefaultsOf(preset);
        var providerName = merged.Provider ?? _defaultProvider;
        IBxImageProvider? provider;
        lock (_gate) _providers.TryGetValue(providerName, out provider);
        if (provider is null)
        {
            throw new InvalidOperationException($"Unknown image provider '{providerName}'. Register it with BluxOptions.Images.AddProvider.");
        }

        var remote = IsRemote(src, out var host);
        if (provider.ValidateDomains && remote && !AllowedHost(host))
        {
            return new BxResolvedImage(src, null, null);
        }

        var path = remote || src.StartsWith('/') ? src : "/" + src;
        var densities = ParseDensities(merged.Densities) ?? _densities;
        string Url(int? w, int? h) => provider.GetUrl(path, new BxImageModifiers(w, h, merged.Quality, merged.Format, merged.Fit, Extra: merged.Modifiers));

        if (!provider.CanResize)
        {
            return new BxResolvedImage(provider is NoneProvider ? src : Url(width, height), null, null);
        }

        var sizes = ParseSizes(merged.Sizes);
        if (sizes.Count == 0)
        {
            // Densities only: the same picture at 1x, 2x… Nothing to scale without a width or a height.
            if (width is null && height is null)
            {
                return new BxResolvedImage(Url(null, null), null, null);
            }
            var variants = densities
                .OrderBy(d => d)
                .Select(d => (Density: d, Url: Url(width * d, height * d)))
                .ToList();
            return new BxResolvedImage(
                variants[0].Url,
                string.Join(", ", variants.Select(v => $"{EscapeForSrcSet(v.Url)} {v.Density}x")),
                null);
        }

        return ResolveSizes(sizes, densities, width, height, Url);
    }

    /// <summary>
    /// The URL of a tiny version of the image (quality 50, blurred when the provider can blur), for <c>Placeholder="true"</c>.
    /// Null when the provider cannot resize or the image is remote and not allowed.
    /// </summary>
    public string? ResolvePlaceholder(string? src, BxImageOptions? options, int width, int? height)
    {
        Freeze();
        if (string.IsNullOrEmpty(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
        options ??= new BxImageOptions();
        BxImageOptions? preset = null;
        if (options.Preset is { Length: > 0 } presetName) Presets.TryGetValue(presetName, out preset);
        var merged = options.OverDefaultsOf(preset);
        IBxImageProvider? provider;
        lock (_gate) _providers.TryGetValue(merged.Provider ?? _defaultProvider, out provider);
        if (provider is not { CanResize: true }) return null;
        var remote = IsRemote(src, out var host);
        if (provider.ValidateDomains && remote && !AllowedHost(host)) return null;
        var path = remote || src.StartsWith('/') ? src : "/" + src;
        return provider.GetUrl(path, new BxImageModifiers(width, height, 50, merged.Format, merged.Fit, provider.SupportsBlur ? 3 : null, merged.Modifiers));
    }

    private BxResolvedImage ResolveSizes(List<KeyValuePair<string, string>> sizes, IReadOnlyList<int> densities, int? width, int? height, Func<int?, int?, string> url)
    {
        var hwRatio = width is > 0 && height is > 0 ? (double)height.Value / width.Value : 0;
        var parsed = new List<SizeVariant>();
        foreach (var (key, size) in sizes)
        {
            if (TryParseVariant(key, size, height, hwRatio, out var variant)) parsed.Add(variant);
        }

        // The unprefixed entry applies below the first breakpoint. Nuxt Image computes its width from a 1px viewport, which produces
        // a 1px candidate for "100vw"; here a fluid default is measured against the first breakpoint, or against the widest screen when
        // there is none, so the candidates are usable.
        var widest = Screens.Count > 0 ? Screens.Values.Max() : 1536;
        var firstBreakpoint = parsed.Where(v => !v.IsDefault).Select(v => v.ScreenMaxWidth).DefaultIfEmpty(widest).Min();
        for (var i = 0; i < parsed.Count; i++)
        {
            if (parsed[i].IsDefault && parsed[i].Fluid)
            {
                var cWidth = (int)Math.Round(parsed[i].Percent / 100d * firstBreakpoint, MidpointRounding.AwayFromZero);
                parsed[i] = parsed[i] with { CWidth = cWidth, CHeight = hwRatio > 0 ? (int)Math.Round(cWidth * hwRatio, MidpointRounding.AwayFromZero) : height };
            }
        }

        var srcset = new List<(int Width, string Url)>();
        var sizeVariants = new List<(string Size, int ScreenMaxWidth, string Media)>();
        foreach (var variant in parsed)
        {
            sizeVariants.Add((variant.Size, variant.ScreenMaxWidth, $"(max-width: {variant.ScreenMaxWidth - 1}px)"));
            foreach (var density in densities)
            {
                srcset.Add((variant.CWidth * density, url(variant.CWidth * density, variant.CHeight is { } h ? h * density : null)));
            }
        }

        // Sort by breakpoint, drop repeated media conditions, then shift each media to the next entry:
        // an entry applies from its breakpoint to the next one, the last one to everything above.
        sizeVariants = sizeVariants.OrderBy(v => v.ScreenMaxWidth).ToList();
        string? previousMedia = null;
        for (var i = sizeVariants.Count - 1; i >= 0; i--)
        {
            var media = sizeVariants[i].Media;
            if (media == previousMedia) sizeVariants.RemoveAt(i);
            previousMedia = media;
        }
        var sizeParts = new List<string>(sizeVariants.Count);
        for (var i = 0; i < sizeVariants.Count; i++)
        {
            var media = i + 1 < sizeVariants.Count ? sizeVariants[i + 1].Media : "";
            sizeParts.Add($"{(media.Length > 0 ? media + " " : "")}{sizeVariants[i].Size}");
        }

        srcset = srcset.OrderBy(v => v.Width).ToList();
        int? previousWidth = null;
        for (var i = srcset.Count - 1; i >= 0; i--)
        {
            var current = srcset[i].Width;
            if (current == previousWidth) srcset.RemoveAt(i);
            previousWidth = current;
        }

        if (srcset.Count == 0)
        {
            return new BxResolvedImage(url(width, height), null, null);
        }
        return new BxResolvedImage(
            srcset[^1].Url,
            string.Join(", ", srcset.Select(v => $"{EscapeForSrcSet(v.Url)} {v.Width}w")),
            sizeParts.Count > 0 ? string.Join(", ", sizeParts) : null);
    }

    private sealed record SizeVariant(string Size, int ScreenMaxWidth, int CWidth, int? CHeight, bool IsDefault, bool Fluid, int Percent);

    private bool TryParseVariant(string key, string size, int? height, double hwRatio, out SizeVariant variant)
    {
        variant = default!;
        var isDefault = key == DefaultKey;
        var screenMaxWidth = Screens.TryGetValue(key, out var screen) && screen > 0 ? screen : LeadingInt(key);
        var fluid = size.EndsWith("vw", StringComparison.Ordinal);
        if (!fluid && size.Length > 0 && size.All(char.IsAsciiDigit)) size += "px";
        if (!fluid && !size.EndsWith("px", StringComparison.Ordinal)) return false;
        var cWidth = LeadingInt(size);
        if (screenMaxWidth <= 0 || cWidth <= 0) return false;
        var percent = cWidth;
        if (fluid) cWidth = (int)Math.Round(cWidth / 100d * screenMaxWidth, MidpointRounding.AwayFromZero);
        var cHeight = hwRatio > 0 ? (int)Math.Round(cWidth * hwRatio, MidpointRounding.AwayFromZero) : height;
        variant = new SizeVariant(size, screenMaxWidth, cWidth, cHeight, isDefault, fluid, percent);
        return true;
    }

    private const string DefaultKey = "1px";

    private static int LeadingInt(string text)
    {
        var end = 0;
        while (end < text.Length && char.IsAsciiDigit(text[end])) end++;
        return end > 0 && int.TryParse(text.AsSpan(0, end), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static readonly Regex SizesSeparator = new(@"[\s,]+", RegexOptions.Compiled);

    // "100vw sm:50vw md:400px": an entry without a prefix is the default, stored under the key "1px" like Nuxt Image does.
    private static List<KeyValuePair<string, string>> ParseSizes(string? input)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(input)) return result;
        foreach (var entry in SizesSeparator.Split(input).Where(e => e.Length > 0))
        {
            var parts = entry.Split(':');
            var pair = parts.Length != 2
                ? new KeyValuePair<string, string>(DefaultKey, parts[0].Trim())
                : new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
            var index = result.FindIndex(p => p.Key == pair.Key);
            if (index >= 0) result[index] = pair; else result.Add(pair);
        }
        return result;
    }

    // "x1 x2": null when the text is empty, an error when it holds nothing usable.
    private static IReadOnlyList<int>? ParseDensities(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var densities = new List<int>();
        foreach (var part in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var value = LeadingInt(part.Replace("x", "", StringComparison.OrdinalIgnoreCase));
            if (value > 0 && !densities.Contains(value)) densities.Add(value);
        }
        return densities.Count > 0
            ? densities
            : throw new ArgumentException($"Densities '{input}' hold no usable value; use \"x1\" to render the regular size only.");
    }

    private static bool IsRemote(string src, out string host)
    {
        host = "";
        var start = src.StartsWith("//", StringComparison.Ordinal) ? "http:" + src : src;
        if (!Uri.TryCreate(start, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return false;
        host = uri.Host;
        return true;
    }

    private bool AllowedHost(string host)
    {
        foreach (var domain in Domains)
        {
            var entry = domain.Contains("://", StringComparison.Ordinal) && Uri.TryCreate(domain, UriKind.Absolute, out var uri) ? uri.Host : domain.Trim();
            if (string.Equals(entry, host, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    // Commas and spaces separate the candidates of a srcset: they cannot appear in a URL.
    private static string EscapeForSrcSet(string url) =>
        url.Contains(',') || url.Contains(' ') ? url.Replace(",", "%2C").Replace(" ", "%20") : url;

    private sealed class NoneProvider : IBxImageProvider
    {
        public const string ProviderName = "none";
        public static readonly NoneProvider Instance = new();
        public string Name => ProviderName;
        public bool CanResize => false;
        public string GetUrl(string src, BxImageModifiers modifiers) => src;
    }

    private sealed class GuardedDictionary<TKey, TValue>(BxImageSettings owner, IEqualityComparer<TKey> comparer) : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _inner = new(comparer);

        public TValue this[TKey key]
        {
            get => _inner[key];
            set { owner.ThrowIfFrozen(); _inner[key] = value; }
        }

        public ICollection<TKey> Keys => _inner.Keys;
        public ICollection<TValue> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public void Add(TKey key, TValue value) { owner.ThrowIfFrozen(); _inner.Add(key, value); }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public void Clear() { owner.ThrowIfFrozen(); _inner.Clear(); }
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);
        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
        public bool Remove(TKey key) { owner.ThrowIfFrozen(); return _inner.Remove(key); }
        public bool Remove(KeyValuePair<TKey, TValue> item) { owner.ThrowIfFrozen(); return ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Remove(item); }
        public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value!);
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    }

    private sealed class GuardedSet(BxImageSettings owner) : ICollection<string>
    {
        private readonly HashSet<string> _inner = new(StringComparer.OrdinalIgnoreCase);
        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public void Add(string item) { ArgumentException.ThrowIfNullOrWhiteSpace(item); owner.ThrowIfFrozen(); _inner.Add(item.Trim()); }
        public void Clear() { owner.ThrowIfFrozen(); _inner.Clear(); }
        public bool Contains(string item) => _inner.Contains(item);
        public void CopyTo(string[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public IEnumerator<string> GetEnumerator() => _inner.GetEnumerator();
        public bool Remove(string item) { owner.ThrowIfFrozen(); return _inner.Remove(item); }
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    }
}
