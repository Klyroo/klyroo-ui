using Blux.UI.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blux.UI;

/// <summary>
/// Global configuration of Blux UI. Register it from code shared by the server and the WebAssembly client,
/// otherwise components change appearance when an Auto render mode switches runtime.
/// </summary>
/// <remarks>
/// Partial: each family of components declares its themes in its own file.
/// Configure the themes in <c>AddBluxUi</c>: a theme becomes read-only the first time a component renders with it,
/// and a later change throws <see cref="InvalidOperationException"/>. <c>AddBluxUi</c> may be called several times
/// (a library and the app); every call configures the same options.
/// </remarks>
public sealed partial class BluxOptions
{
    public ComponentTheme Button { get; } = Themes.Button();
    public ComponentTheme Badge { get; } = Themes.Badge();
    public ComponentTheme Avatar { get; } = Themes.Avatar();
    public ComponentTheme Card { get; } = Themes.Card();
    public ComponentTheme Alert { get; } = Themes.Alert();
    public ComponentTheme Input { get; } = Themes.Input();
    public ComponentTheme Textarea { get; } = Themes.Textarea();
    public ComponentTheme Checkbox { get; } = Themes.Checkbox();
    public ComponentTheme Switch { get; } = Themes.Switch();
    public ComponentTheme RadioGroup { get; } = Themes.RadioGroup();
    public ComponentTheme Select { get; } = Themes.Select();
    public ComponentTheme FormField { get; } = Themes.FormField();
    public ComponentTheme Popover { get; } = Themes.Popover();
    public ComponentTheme Modal { get; } = Themes.Modal();
    public ComponentTheme Tooltip { get; } = Themes.Tooltip();
    public ComponentTheme DropdownMenu { get; } = Themes.DropdownMenu();
    public ComponentTheme Toast { get; } = Themes.Toaster();
    public ComponentTheme Tabs { get; } = Themes.Tabs();
    public ComponentTheme Accordion { get; } = Themes.Accordion();
    public ComponentTheme Table { get; } = Themes.Table();

    /// <summary>Palettes of the semantic colors (<c>Primary</c>, <c>Neutral</c>…) and extra semantic colors. Render <see cref="BxThemeStyle"/> in <c>&lt;head&gt;</c> to deliver them.</summary>
    public BxColorOptions Colors { get; } = new();

    /// <summary>Default <c>Color</c> and <c>Size</c> of the components, when a parameter is not set.</summary>
    public BxDefaults Defaults { get; } = new();

    private readonly Lazy<string?> _themeCss;

    public BluxOptions()
    {
        // The first resolution freezes the colors and computes the stylesheet, once, however many threads ask.
        _themeCss = new Lazy<string?>(() =>
        {
            Colors.Freeze();
            return BxThemeTokens.Compute(Colors).ToCss(Colors);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// The stylesheet of <see cref="Colors"/>, resolved once (the colors become read-only), or null when they are the defaults.
    /// Throws <see cref="InvalidOperationException"/> when a palette cannot meet the contrast rules and
    /// <see cref="BxColorOptions.AllowLowContrast"/> is off.
    /// </summary>
    internal string? ThemeCss => _themeCss.Value;

    private IReadOnlyList<BxLocale> _locales = BxLocales.All;
    private BxLocale? _locale;
    private volatile bool _localesFrozen;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, BxLocale> _resolved = new(StringComparer.OrdinalIgnoreCase);

    private void ThrowIfLocalesFrozen(string name)
    {
        if (_localesFrozen)
        {
            throw new InvalidOperationException($"{nameof(BluxOptions)}.{name} cannot change once a component has resolved a locale: the choice of each culture is cached. Set it in AddBluxUi, before the app starts rendering.");
        }
    }

    /// <summary>
    /// The locale every component uses unless a <see cref="BxApp"/> (or a <c>CascadingValue</c> of <see cref="BxLocale"/>)
    /// above it sets one. Leave it unset to follow the culture of the user: the locale is then picked from
    /// <see cref="Locales"/> by <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>.
    /// Like the themes, it becomes read-only the first time a locale is resolved: set it in <c>AddBluxUi</c>.
    /// </summary>
    public BxLocale? Locale
    {
        get => _locale;
        set
        {
            ThrowIfLocalesFrozen(nameof(Locale));
            _locale = value;
        }
    }

    /// <summary>
    /// The locales the automatic selection chooses from, and that <see cref="BxLocaleSelect"/> offers by default.
    /// Defaults to <see cref="BxLocales.All"/>; assign a new list to choose fewer or to add your own, for example
    /// <c>o.Locales = [.. BxLocales.All, australian]</c>. The list is copied when assigned, so changing the original
    /// afterwards has no effect, and it becomes read-only the first time a locale is resolved (a later assignment throws
    /// <see cref="InvalidOperationException"/>).
    /// English (code "en"), or the first locale when there is none, is the fallback of an unmatched culture.
    /// </summary>
    public IReadOnlyList<BxLocale> Locales
    {
        get => _locales;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfLocalesFrozen(nameof(Locales));
            var copy = value.ToArray();
            if (Array.IndexOf(copy, null) >= 0) throw new ArgumentException("The list of locales cannot contain null.", nameof(value));
            _locales = Array.AsReadOnly(copy);
        }
    }

    /// <summary>
    /// The locale for a culture: <see cref="Locale"/> when set, otherwise the match in <see cref="Locales"/>
    /// (see <see cref="BxLocales.Resolve"/>). The result of each culture is cached, and the first call makes
    /// <see cref="Locale"/> and <see cref="Locales"/> read-only.
    /// </summary>
    public BxLocale ResolveLocale(System.Globalization.CultureInfo culture)
    {
        _localesFrozen = true;
        if (_locale is { } fixedLocale) return fixedLocale;
        return _resolved.TryGetValue(culture.Name, out var cached)
            ? cached
            : _resolved.GetOrAdd(culture.Name, _ => BxLocales.Resolve(culture, _locales));
    }

    /// <summary>Every theme, whichever file declares it. Feeds the Tailwind build and the generated-assets tests.</summary>
    internal IEnumerable<ComponentTheme> AllThemes =>
        typeof(BluxOptions).GetProperties()
            .Where(p => p.PropertyType == typeof(ComponentTheme))
            .Select(p => (ComponentTheme)p.GetValue(this)!);
}

public static class BluxServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blux UI. Call it in the server <c>Program.cs</c> and in the client one with the same configuration,
    /// ideally through a shared method in the client project.
    /// </summary>
    public static IServiceCollection AddBluxUi(this IServiceCollection services, Action<BluxOptions>? configure = null)
    {
        // A second call (from a library and the app, say) configures the same options instead of being ignored.
        var options = services.FirstOrDefault(d => d.ServiceType == typeof(BluxOptions))?.ImplementationInstance as BluxOptions;
        if (options is null)
        {
            options = new BluxOptions();
            services.AddSingleton(options);
        }
        configure?.Invoke(options);

        services.TryAddSingleton<IClassMerger, TailwindClassMerger>();
        services.TryAddSingleton<ThemeResolver>();
        services.TryAddScoped<IToastService, ToastService>();
        return services;
    }
}
