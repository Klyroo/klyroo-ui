using System.Collections;

namespace Blux.UI.Theming;

/// <summary>
/// Classes to add to one or more slots, keyed by slot name. Read-only: a <see cref="ComponentTheme"/> builds them
/// through its fluent methods.
/// </summary>
public sealed class SlotClasses : IReadOnlyDictionary<string, string>
{
    private readonly Dictionary<string, string> _items = new(StringComparer.Ordinal);

    internal SlotClasses() { }

    internal void Set(string slot, string classes) => _items[slot] = classes;

    public string this[string key] => _items[key];
    public IEnumerable<string> Keys => _items.Keys;
    public IEnumerable<string> Values => _items.Values;
    public int Count => _items.Count;
    public bool ContainsKey(string key) => _items.ContainsKey(key);
    public bool TryGetValue(string key, out string value) => _items.TryGetValue(key, out value!);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Classes applied when every condition in <see cref="When"/> matches the selected variants.
/// </summary>
public sealed class CompoundVariant
{
    internal CompoundVariant(IReadOnlyDictionary<string, string> when, SlotClasses classes)
    {
        When = when;
        Classes = classes;
    }

    public IReadOnlyDictionary<string, string> When { get; }
    public SlotClasses Classes { get; }
}

/// <summary>
/// Declarative theme of a component, the C# counterpart of a tailwind-variants definition:
/// base classes per slot, classes per variant value, compound variants and default variant values.
/// Every class must be written as a literal so the Tailwind build can see it.
/// Overriding a theme through <c>AddBluxUi</c> appends classes; conflicts are resolved by tailwind-merge,
/// so the last class wins.
/// </summary>
/// <remarks>
/// A theme becomes read-only the first time a component resolves it. Any fluent method called afterwards throws
/// <see cref="InvalidOperationException"/>, because resolved classes are cached and a late change would apply to some
/// selections and not others. Configure themes in <c>AddBluxUi</c>, which runs before the first render and may be
/// called several times. The collections are read-only views; the fluent methods are the way to configure.
/// </remarks>
public sealed class ComponentTheme
{
    private readonly object _gate = new();
    private volatile bool _frozen;

    private readonly Dictionary<string, string> _slots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, SlotClasses>> _variants = new(StringComparer.Ordinal);
    private readonly List<CompoundVariant> _compounds = [];
    private readonly Dictionary<string, string> _defaults = new(StringComparer.Ordinal);

    public ComponentTheme(string name, params string[] slots)
    {
        Name = name;
        foreach (var slot in slots)
        {
            _slots[slot] = "";
        }
    }

    public string Name { get; }

    /// <summary>Base classes of each slot. The first slot is the root element.</summary>
    public IReadOnlyDictionary<string, string> Slots => new MapView<string, string, string>(_slots, v => v);

    /// <summary>Variant name → variant value → slot classes. Boolean variants use "true" and "false".</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, SlotClasses>> Variants =>
        new MapView<string, Dictionary<string, SlotClasses>, IReadOnlyDictionary<string, SlotClasses>>(
            _variants, values => new MapView<string, SlotClasses, SlotClasses>(values, v => v));

    public IReadOnlyList<CompoundVariant> CompoundVariants => new ListView<CompoundVariant>(_compounds);

    /// <summary>Variant value used when a component passes none.</summary>
    public IReadOnlyDictionary<string, string> DefaultVariants => new MapView<string, string, string>(_defaults, v => v);

    /// <summary>Appends classes to a slot's base classes.</summary>
    /// <exception cref="InvalidOperationException">The theme has already been resolved by a component.</exception>
    public ComponentTheme Slot(string slot, string classes)
    {
        lock (_gate)
        {
            ThrowIfFrozen();
            EnsureSlot(slot);
            _slots[slot] = Join(_slots[slot], classes);
        }
        return this;
    }

    /// <summary>Appends classes applied to <paramref name="slot"/> when variant <paramref name="variant"/> equals <paramref name="value"/>.</summary>
    /// <exception cref="InvalidOperationException">The theme has already been resolved by a component.</exception>
    public ComponentTheme Variant(string variant, string value, string slot, string classes)
    {
        lock (_gate)
        {
            ThrowIfFrozen();
            EnsureSlot(slot);
            if (!_variants.TryGetValue(variant, out var values))
            {
                _variants[variant] = values = new(StringComparer.Ordinal);
            }
            if (!values.TryGetValue(value, out var slots))
            {
                values[value] = slots = new SlotClasses();
            }
            slots.Set(slot, slots.TryGetValue(slot, out var existing) ? Join(existing, classes) : classes);
        }
        return this;
    }

    /// <summary>Appends classes applied to <paramref name="slot"/> when every entry of <paramref name="when"/> matches. The dictionary is copied.</summary>
    /// <exception cref="InvalidOperationException">The theme has already been resolved by a component.</exception>
    public ComponentTheme Compound(Dictionary<string, string> when, string slot, string classes)
    {
        lock (_gate)
        {
            ThrowIfFrozen();
            EnsureSlot(slot);
            var copy = new Dictionary<string, string>(when, StringComparer.Ordinal);
            var slotClasses = new SlotClasses();
            slotClasses.Set(slot, classes);
            _compounds.Add(new CompoundVariant(new MapView<string, string, string>(copy, v => v), slotClasses));
        }
        return this;
    }

    /// <exception cref="InvalidOperationException">The theme has already been resolved by a component.</exception>
    public ComponentTheme Default(string variant, string value)
    {
        lock (_gate)
        {
            ThrowIfFrozen();
            _defaults[variant] = value;
        }
        return this;
    }

    /// <summary>Makes the theme read-only. Called by <see cref="ThemeResolver"/> before it reads the theme; idempotent.</summary>
    internal void Freeze()
    {
        if (_frozen)
        {
            return;
        }
        lock (_gate)
        {
            _frozen = true;
        }
    }

    /// <summary>Every class token used anywhere in the theme. Feeds the Tailwind build.</summary>
    public IEnumerable<string> AllClassTokens()
    {
        IEnumerable<string> sources = _slots.Values
            .Concat(_variants.Values.SelectMany(v => v.Values).SelectMany(s => s.Values))
            .Concat(_compounds.SelectMany(c => c.Classes.Values));
        return sources.SelectMany(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private void ThrowIfFrozen()
    {
        if (_frozen)
        {
            throw new InvalidOperationException(
                $"Theme '{Name}' is read-only: a component has already rendered with it, and its resolved classes are cached. " +
                "Configure themes in AddBluxUi, before the first render.");
        }
    }

    private void EnsureSlot(string slot)
    {
        if (!_slots.ContainsKey(slot))
        {
            throw new ArgumentException($"Theme '{Name}' has no slot '{slot}'. Slots: {string.Join(", ", _slots.Keys)}.", nameof(slot));
        }
    }

    private static string Join(string a, string b) =>
        string.IsNullOrWhiteSpace(a) ? b : string.IsNullOrWhiteSpace(b) ? a : a + " " + b;

    private sealed class MapView<TKey, TSource, TValue>(Dictionary<TKey, TSource> source, Func<TSource, TValue> project)
        : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        public TValue this[TKey key] => project(source[key]);
        public IEnumerable<TKey> Keys => source.Keys;
        public IEnumerable<TValue> Values => source.Values.Select(project);
        public int Count => source.Count;
        public bool ContainsKey(TKey key) => source.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (source.TryGetValue(key, out var s))
            {
                value = project(s);
                return true;
            }
            value = default!;
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
            source.Select(p => KeyValuePair.Create(p.Key, project(p.Value))).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ListView<T>(List<T> source) : IReadOnlyList<T>
    {
        public T this[int index] => source[index];
        public int Count => source.Count;
        public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
