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

    private string? _themeCss;
    private bool _themeCssResolved;

    /// <summary>The stylesheet of <see cref="Colors"/>, resolved once (the colors become read-only), or null when they are the defaults.</summary>
    internal string? ThemeCss
    {
        get
        {
            if (!_themeCssResolved)
            {
                lock (Colors)
                {
                    if (!_themeCssResolved)
                    {
                        Colors.Freeze();
                        _themeCss = BxThemeTokens.Compute(Colors).ToCss(Colors);
                        _themeCssResolved = true;
                    }
                }
            }
            return _themeCss;
        }
    }

    private IReadOnlyList<BxLocale> _locales = BxLocales.All;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, BxLocale> _resolved = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The locale every component uses unless a <see cref="BxApp"/> (or a <c>CascadingValue</c> of <see cref="BxLocale"/>)
    /// above it sets one. Leave it unset to follow the culture of the user: the locale is then picked from
    /// <see cref="Locales"/> by <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public BxLocale? Locale { get; set; }

    /// <summary>
    /// The locales the automatic selection chooses from, and that <see cref="BxLocaleSelect"/> offers by default.
    /// Defaults to <see cref="BxLocales.All"/>; set a smaller list, or add your own locales to it.
    /// English (code "en"), or the first locale when there is none, is the fallback of an unmatched culture.
    /// </summary>
    public IReadOnlyList<BxLocale> Locales
    {
        get => _locales;
        set
        {
            _locales = value ?? throw new ArgumentNullException(nameof(value));
            _resolved.Clear();
        }
    }

    /// <summary>
    /// The locale for a culture: <see cref="Locale"/> when set, otherwise the match in <see cref="Locales"/>
    /// (see <see cref="BxLocales.Resolve"/>). The result of each culture is cached.
    /// </summary>
    public BxLocale ResolveLocale(System.Globalization.CultureInfo culture)
    {
        if (Locale is { } fixedLocale) return fixedLocale;
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
