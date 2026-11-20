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

    private protected static string NewId(string prefix) => $"bx-{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 12)];
}

internal sealed class SlotSet(IReadOnlyDictionary<string, string> classes)
{
    public string this[string slot] => classes[slot];
}
