using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// The class overrides a <see cref="KlyTheme"/> cascades to the components below it, keyed by component type.
/// Created by <see cref="KlyTheme"/> and read by <see cref="KlyComponentBase"/>.
/// </summary>
internal sealed class KlyThemeScope
{
    private readonly KlyThemeScope? _parent;
    private readonly IReadOnlyDictionary<Type, ISlotOverrides> _overrides;

    internal KlyThemeScope(KlyThemeScope? parent, IReadOnlyDictionary<Type, ISlotOverrides> overrides)
    {
        _parent = parent;
        _overrides = overrides;
    }

    /// <summary>
    /// The classes the enclosing themes add to a slot of a component, outermost first so that the innermost theme wins
    /// when the classes are merged. Null when no theme says anything about the slot.
    /// </summary>
    internal string? ClassesFor(Type component, string slot)
    {
        var own = Find(component, slot);
        var inherited = _parent?.ClassesFor(component, slot);
        if (inherited is null) return own;
        return own is null ? inherited : inherited + " " + own;
    }

    private string? Find(Type component, string slot)
    {
        if (_overrides.TryGetValue(component, out var ui)) return ui.For(slot);
        if (component.IsGenericType && _overrides.TryGetValue(component.GetGenericTypeDefinition(), out ui)) return ui.For(slot);
        return null;
    }
}
