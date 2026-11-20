using Microsoft.AspNetCore.Components;

namespace Blux.UI.Theming;

/// <summary>Per-instance class overrides of a component's slots.</summary>
public interface ISlotOverrides
{
    string? For(string slot);
}

/// <summary>
/// Base of styled components: injects the theme services and resolves slot classes in the documented order
/// (theme defaults, <c>AddBluxUi</c> overrides, enclosing <see cref="BxTheme"/> scopes, <c>Ui</c>, then <c>Class</c> on the root slot).
/// </summary>
public abstract class BxComponentBase : ComponentBase
{
    [Inject] internal ThemeResolver Resolver { get; set; } = default!;
    [Inject] protected BluxOptions Options { get; set; } = default!;

    /// <summary>Classes added to the root element, merged last so they win over the theme.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>True when the component renders in a Blazor Hybrid web view (<see cref="BluxHost.IsHybrid"/>).</summary>
    protected bool IsHybrid => BluxHost.IsHybrid(RendererInfo);

    /// <summary>The locale set by an enclosing <see cref="BxApp"/> or by a cascading value.</summary>
    [CascadingParameter] private BxLocale? CascadingLocale { get; set; }

    /// <summary>
    /// The locale of this component: the cascading one, then <see cref="BluxOptions.Locale"/>, then the one matching
    /// the current UI culture. It is the same in every render mode for the same culture.
    /// </summary>
    protected BxLocale CurrentLocale => CascadingLocale ?? Options.ResolveLocale(System.Globalization.CultureInfo.CurrentUICulture);

    /// <summary>
    /// The culture that formats dates, numbers and 12 or 24 hour clocks: the regional <see cref="System.Globalization.CultureInfo.CurrentCulture"/>
    /// when its language is the one of <see cref="CurrentLocale"/> (an "en-GB" user with the English locale keeps 20/11/2026,
    /// Monday first and a 24 hour clock; "fr-CA" keeps its own formats), otherwise the culture of the locale (a locale forced
    /// to another language than the culture of the user). An explicit <c>Culture</c> parameter of a component wins over it.
    /// </summary>
    protected System.Globalization.CultureInfo FormatCulture
    {
        get
        {
            var locale = CurrentLocale.Culture;
            var current = System.Globalization.CultureInfo.CurrentCulture;
            return string.Equals(current.TwoLetterISOLanguageName, locale.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) && current.Name.Length > 0
                ? current
                : locale;
        }
    }

    /// <summary>The texts of <see cref="CurrentLocale"/>.</summary>
    protected BxMessages Messages => CurrentLocale.Messages;

    /// <summary>The scope of the closest enclosing <see cref="BxTheme"/>, if any.</summary>
    [CascadingParameter] private protected BxThemeScope? ThemeScope { get; set; }

    /// <summary>Unmatched attributes, forwarded to the main element of the component.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private protected SlotSet ResolveSlots(
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
    private protected BxColor DefaultColor(BxColor fallback) => Options is null ? fallback : Options.Defaults.ColorOr(fallback);
    private protected BxSize DefaultSize(BxSize fallback) => Options is null ? fallback : Options.Defaults.SizeOr(fallback);

    /// <summary>An icon of the app as a render fragment: <paramref name="name"/> is a key of <c>Options.Icons</c> (<c>Options.Icons.Close</c>) or any icon name.</summary>
    private protected RenderFragment RenderIcon(string name, string? @class = null) => IconRender.Fragment(Options?.Icons, name, @class);

    private protected static string NewId(string prefix) => $"bx-{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 12)];
}

internal sealed class SlotSet(IReadOnlyDictionary<string, string> classes)
{
    public string this[string slot] => classes[slot];
}
