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

    private const System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes ExtensionMembers =
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties;

    private readonly object _extensionsGate = new();
    private readonly Dictionary<Type, object> _extensions = [];

    /// <summary>
    /// The options of a sister package, created on the first call and shared afterwards: one instance per <typeparamref name="T"/>
    /// and per <see cref="BluxOptions"/>, whichever thread asks first. Its public <see cref="ComponentTheme"/> properties are themes
    /// like those of this class: <see cref="AllThemes"/> lists them, the Tailwind class extractor and the docs bundle read them, and a theme
    /// becomes read-only when a component resolves it, with the same <see cref="InvalidOperationException"/> as the themes of this class.
    /// </summary>
    /// <typeparam name="T">A class of the package with a public parameterless constructor.</typeparam>
    /// <example>
    /// The options of a package, and the <c>AddAcme</c> that configures them next to <c>AddBluxUi</c>:
    /// <code>
    /// public sealed class AcmeOptions
    /// {
    ///     public ComponentTheme Gauge { get; } = new ComponentTheme("gauge", "root").Slot("root", "relative inline-flex");
    /// }
    ///
    /// public static class AcmeServiceCollectionExtensions
    /// {
    ///     public static IServiceCollection AddAcme(this IServiceCollection services, Action&lt;AcmeOptions&gt;? configure = null)
    ///     {
    ///         services.AddBluxUi(options => configure?.Invoke(options.Extension&lt;AcmeOptions&gt;()));
    ///         return services;
    ///     }
    /// }
    /// </code>
    /// A component of the package reads <c>Options.Extension&lt;AcmeOptions&gt;().Gauge</c> and resolves it with the protected <c>ResolveSlots</c> of
    /// <see cref="BxComponentBase"/>; an app overrides it with <c>services.AddAcme(a =&gt; a.Gauge.Slot("root", "..."))</c>.
    /// </example>
    public T Extension<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(ExtensionMembers)] T>() where T : class, new() => (T)Extension(typeof(T));

    /// <summary>The non-generic form of <see cref="Extension{T}"/>, for tooling. <paramref name="type"/> must be a non-abstract class with a public parameterless constructor.</summary>
    public object Extension([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(ExtensionMembers)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type == typeof(BluxOptions))
        {
            throw new ArgumentException($"{nameof(BluxOptions)} cannot be an extension of itself.", nameof(type));
        }
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new ArgumentException($"{type} cannot be an extension of {nameof(BluxOptions)}: it must be a non-abstract class with a public parameterless constructor.", nameof(type));
        }
        lock (_extensionsGate)
        {
            if (!_extensions.TryGetValue(type, out var instance))
            {
                instance = Activator.CreateInstance(type)!;
                _extensions[type] = instance;
            }
            return instance;
        }
    }

    /// <summary>The options objects registered so far with <see cref="Extension{T}"/>, ordered by type name.</summary>
    public IReadOnlyList<object> Extensions
    {
        get
        {
            lock (_extensionsGate)
            {
                return [.. _extensions.Values.OrderBy(e => e.GetType().FullName, StringComparer.Ordinal)];
            }
        }
    }

    /// <summary>
    /// Every theme: the <see cref="ComponentTheme"/> properties of this class, whichever file declares them, then those of each registered
    /// <see cref="Extension{T}"/>. Feeds the Tailwind build and the generated-assets tests.
    /// </summary>
    public IEnumerable<ComponentTheme> AllThemes =>
        ThemesOf(this).Concat(Extensions.SelectMany(ThemesOf));

    /// <summary>The public instance <see cref="ComponentTheme"/> properties of an options object, with their names. Build-time tooling only.</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Only used by build tooling and AllThemes, on BluxOptions or an instance created by Extension, which keeps the public properties of its type.")]
    internal static IEnumerable<KeyValuePair<string, ComponentTheme>> NamedThemesOf(object options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(ComponentTheme) && p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new KeyValuePair<string, ComponentTheme>(p.Name, (ComponentTheme)p.GetValue(options)!));
    }

    private static IEnumerable<ComponentTheme> ThemesOf(object options) => NamedThemesOf(options).Select(p => p.Value);
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
        services.TryAddScoped<IBxCultureStore, BxLocalStorageCultureStore>();
        return services;
    }
}
