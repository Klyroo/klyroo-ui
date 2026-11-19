namespace Blux.UI.Theming;

/// <summary>
/// Classes to add to one or more slots, keyed by slot name.
/// </summary>
public sealed class SlotClasses : Dictionary<string, string>
{
    public SlotClasses() : base(StringComparer.Ordinal) { }
}

/// <summary>
/// Classes applied when every condition in <see cref="When"/> matches the selected variants.
/// </summary>
public sealed class CompoundVariant
{
    public required Dictionary<string, string> When { get; init; }
    public required SlotClasses Classes { get; init; }
}

/// <summary>
/// Declarative theme of a component, the C# counterpart of a tailwind-variants definition:
/// base classes per slot, classes per variant value, compound variants and default variant values.
/// Every class must be written as a literal so the Tailwind build can see it.
/// Overriding a theme through <c>AddBluxUi</c> appends classes; conflicts are resolved by tailwind-merge,
/// so the last class wins.
/// </summary>
public sealed class ComponentTheme
{
    public ComponentTheme(string name, params string[] slots)
    {
        Name = name;
        foreach (var slot in slots)
        {
            Slots[slot] = "";
        }
    }

    public string Name { get; }

    /// <summary>Base classes of each slot. The first slot is the root element.</summary>
    public Dictionary<string, string> Slots { get; } = new(StringComparer.Ordinal);

    /// <summary>Variant name → variant value → slot classes. Boolean variants use "true" and "false".</summary>
    public Dictionary<string, Dictionary<string, SlotClasses>> Variants { get; } = new(StringComparer.Ordinal);

    public List<CompoundVariant> CompoundVariants { get; } = [];

    /// <summary>Variant value used when a component passes none.</summary>
    public Dictionary<string, string> DefaultVariants { get; } = new(StringComparer.Ordinal);

    /// <summary>Appends classes to a slot's base classes.</summary>
    public ComponentTheme Slot(string slot, string classes)
    {
        EnsureSlot(slot);
        Slots[slot] = Join(Slots[slot], classes);
        return this;
    }

    /// <summary>Appends classes applied to <paramref name="slot"/> when variant <paramref name="variant"/> equals <paramref name="value"/>.</summary>
    public ComponentTheme Variant(string variant, string value, string slot, string classes)
    {
        EnsureSlot(slot);
        if (!Variants.TryGetValue(variant, out var values))
        {
            Variants[variant] = values = new(StringComparer.Ordinal);
        }
        if (!values.TryGetValue(value, out var slots))
        {
            values[value] = slots = new SlotClasses();
        }
        slots[slot] = slots.TryGetValue(slot, out var existing) ? Join(existing, classes) : classes;
        return this;
    }

    public ComponentTheme Compound(Dictionary<string, string> when, string slot, string classes)
    {
        EnsureSlot(slot);
        CompoundVariants.Add(new CompoundVariant { When = when, Classes = new SlotClasses { [slot] = classes } });
        return this;
    }

    public ComponentTheme Default(string variant, string value)
    {
        DefaultVariants[variant] = value;
        return this;
    }

    /// <summary>Every class token used anywhere in the theme. Feeds the Tailwind build.</summary>
    public IEnumerable<string> AllClassTokens()
    {
        IEnumerable<string> sources = Slots.Values
            .Concat(Variants.Values.SelectMany(v => v.Values).SelectMany(s => s.Values))
            .Concat(CompoundVariants.SelectMany(c => c.Classes.Values));
        return sources.SelectMany(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private void EnsureSlot(string slot)
    {
        if (!Slots.ContainsKey(slot))
        {
            throw new ArgumentException($"Theme '{Name}' has no slot '{slot}'. Slots: {string.Join(", ", Slots.Keys)}.", nameof(slot));
        }
    }

    private static string Join(string a, string b) =>
        string.IsNullOrWhiteSpace(a) ? b : string.IsNullOrWhiteSpace(b) ? a : a + " " + b;
}
