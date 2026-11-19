using Microsoft.AspNetCore.Components;

namespace Blux.UI.Theming;

/// <summary>Per-instance class overrides of a component's slots.</summary>
public interface ISlotOverrides
{
    string? For(string slot);
}

/// <summary>
/// Base of styled components: injects the theme services and resolves slot classes in the documented order
/// (theme defaults, <c>AddBluxUi</c> overrides, <c>Ui</c>, then <c>Class</c> on the root slot).
/// </summary>
public abstract class BxComponentBase : ComponentBase
{
    [Inject] internal ThemeResolver Resolver { get; set; } = default!;
    [Inject] protected BluxOptions Options { get; set; } = default!;

    /// <summary>Classes added to the root element, merged last so they win over the theme.</summary>
    [Parameter] public string? Class { get; set; }

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
            var overrideClasses = ui?.For(slot);
            var instanceClasses = slot == rootSlot ? Class : null;
            result[slot] = overrideClasses is null && instanceClasses is null
                ? classes
                : Resolver.Merge(classes, overrideClasses, instanceClasses);
        }
        return new SlotSet(result);
    }

    private protected static string NewId(string prefix) => $"bx-{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 12)];
}

internal sealed class SlotSet(IReadOnlyDictionary<string, string> classes)
{
    public string this[string slot] => classes[slot];
}
