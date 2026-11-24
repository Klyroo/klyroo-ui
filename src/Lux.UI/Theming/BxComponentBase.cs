using Microsoft.AspNetCore.Components;

namespace Lux.UI.Theming;

/// <summary>Per-instance class overrides of a component's slots.</summary>
public interface ISlotOverrides
{
    string? For(string slot);
}

/// <summary>
/// Base of styled components: injects the theme services and resolves slot classes in the documented order
/// (theme defaults, <c>AddLuxUi</c> overrides, enclosing <see cref="BxTheme"/> scopes, <c>Ui</c>, then <c>Class</c> on the root slot).
/// </summary>
public abstract class BxComponentBase : ComponentBase
{
    [Inject] internal ThemeResolver Resolver { get; set; } = default!;
    [Inject] protected LuxOptions Options { get; set; } = default!;

    /// <summary>Classes added to the root element, merged last so they win over the theme.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>True when the component renders in a Blazor Hybrid web view (<see cref="LuxHost.IsHybrid"/>).</summary>
    protected bool IsHybrid => LuxHost.IsHybrid(RendererInfo);

    /// <summary>The locale set by an enclosing <see cref="BxApp"/> or by a cascading value.</summary>
    [CascadingParameter] private BxLocale? CascadingLocale { get; set; }

    /// <summary>
    /// The locale of this component: the cascading one, then <see cref="LuxOptions.Locale"/>, then the one matching
    /// the current UI culture. It is the same in every render mode for the same culture.
    /// </summary>
    protected BxLocale CurrentLocale => CascadingLocale ?? Options.ResolveLocale(System.Globalization.CultureInfo.CurrentUICulture);

    /// <summary>
    /// The culture that formats dates, numbers and 12 or 24 hour clocks: the regional <see cref="System.Globalization.CultureInfo.CurrentCulture"/>
    /// when its language is the one of <see cref="CurrentLocale"/> (an "en-GB" user with the English locale keeps 20/11/2026,
    /// Monday first and a 24 hour clock; "fr-CA" keeps its own formats), otherwise the culture of the locale (a locale forced
    /// to another language than the culture of the user). An explicit <c>Culture</c> parameter of a component wins over it.
    /// </summary>
    protected System.Globalization.CultureInfo FormatCulture => BxDates.FormatCulture(CurrentLocale);

    /// <summary>The texts of <see cref="CurrentLocale"/>.</summary>
    protected BxMessages Messages => CurrentLocale.Messages;

    /// <summary>The scope of the closest enclosing <see cref="BxTheme"/>, if any.</summary>
    [CascadingParameter] private protected BxThemeScope? ThemeScope { get; set; }

    /// <summary>Unmatched attributes, forwarded to the main element of the component.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Resolves the classes of every slot of <paramref name="theme"/> for the variant values in <paramref name="selection"/>, in the documented order:
    /// theme defaults, <c>AddLuxUi</c> overrides, enclosing <see cref="BxTheme"/> scopes, <paramref name="ui"/>, then <see cref="Class"/> on <paramref name="rootSlot"/>.
    /// The theme becomes read-only. Components of other packages call it with a theme of their own options object (see <see cref="LuxOptions.Extension{T}"/>).
    /// </summary>
    /// <param name="theme">The theme of the component.</param>
    /// <param name="selection">The value of each variant, null to keep the default of the theme.</param>
    /// <param name="ui">The per-instance slot classes of the <c>Ui</c> parameter, if the component has one.</param>
    /// <param name="rootSlot">The slot that receives <see cref="Class"/>.</param>
    protected SlotSet ResolveSlots(
        ComponentTheme theme,
        IReadOnlyDictionary<string, string?> selection,
        ISlotOverrides? ui,
        string rootSlot)
    {
        var resolved = Resolver.Resolve(theme, selection);
        var result = new Dictionary<string, string>(resolved.Count, StringComparer.Ordinal);
        foreach (var (slot, classes) in resolved)
        {
            var scopeClasses = ThemeScope?.ClassesFor(GetType(), slot);
            var overrideClasses = ui?.For(slot);
            var instanceClasses = slot == rootSlot ? Class : null;
            result[slot] = scopeClasses is null && overrideClasses is null && instanceClasses is null
                ? classes
                : Resolver.Merge(classes, scopeClasses, overrideClasses, instanceClasses);
        }
        return new SlotSet(result);
    }

    // Null-safe: Blazor and reflection-based tooling may read a parameter before the services are injected.

    /// <summary>The <c>Color</c> to use when the parameter is not set: the app default of <see cref="LuxOptions.Defaults"/>, else <paramref name="fallback"/>.</summary>
    protected BxColor DefaultColor(BxColor fallback) => Options is null ? fallback : Options.Defaults.ColorOr(fallback);

    /// <summary>The <c>Size</c> to use when the parameter is not set: the app default of <see cref="LuxOptions.Defaults"/>, else <paramref name="fallback"/>.</summary>
    protected BxSize DefaultSize(BxSize fallback) => Options is null ? fallback : Options.Defaults.SizeOr(fallback);

    /// <summary>An icon of the app as a render fragment: <paramref name="name"/> is a key of <c>Options.Icons</c> (<c>Options.Icons.Close</c>) or any icon name.</summary>
    protected RenderFragment RenderIcon(string name, string? @class = null) => IconRender.Fragment(Options?.Icons, name, @class);

    /// <summary>A short element id such as <c>bx-tabs-1a2b3c4d5e6f</c>, unique per call.</summary>
    protected static string NewId(string prefix) => $"bx-{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 12)];
}

/// <summary>The resolved classes of each slot of a component, returned by <c>ResolveSlots</c> of <see cref="BxComponentBase"/>.</summary>
public sealed class SlotSet
{
    private readonly IReadOnlyDictionary<string, string> _classes;

    internal SlotSet(IReadOnlyDictionary<string, string> classes) => _classes = classes;

    /// <summary>The classes of a slot. Throws <see cref="KeyNotFoundException"/> when the theme has no such slot.</summary>
    public string this[string slot] => _classes[slot];
}
