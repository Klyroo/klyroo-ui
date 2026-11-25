using System.Collections.Concurrent;

namespace Klyroo.UI.Theming;

/// <summary>
/// Resolves the classes of every slot of a theme for a given selection of variant values.
/// Results are cached per selection, since a component re-renders far more often than its variants change.
/// </summary>
internal sealed class ThemeResolver(IClassMerger merger)
{
    private const int MaxCachedSelectionsPerTheme = 512;

    private readonly ConcurrentDictionary<ComponentTheme, ConcurrentDictionary<string, IReadOnlyDictionary<string, string>>> _cache = new();

    public IReadOnlyDictionary<string, string> Resolve(ComponentTheme theme, IReadOnlyDictionary<string, string?> selection)
    {
        // From here the theme is read-only: the cache below would otherwise serve classes computed before a later change.
        theme.Freeze();
        var effective = Effective(theme, selection);
        var key = string.Join('|', effective.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value));
        var perTheme = _cache.GetOrAdd(theme, _ => new());
        if (perTheme.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var resolved = Compute(theme, effective);
        // Custom colors or sizes could make the selection space unbounded: stop caching past a limit.
        if (perTheme.Count < MaxCachedSelectionsPerTheme)
        {
            perTheme.TryAdd(key, resolved);
        }
        return resolved;
    }

    /// <summary>Merges per-instance overrides on top of resolved theme classes.</summary>
    public string Merge(params string?[] classes) => merger.Merge(classes);

    private static Dictionary<string, string> Effective(ComponentTheme theme, IReadOnlyDictionary<string, string?> selection)
    {
        var effective = new Dictionary<string, string>(theme.DefaultVariants, StringComparer.Ordinal);
        foreach (var (variant, value) in selection)
        {
            if (value is not null)
            {
                effective[variant] = value;
            }
        }
        return effective;
    }

    private IReadOnlyDictionary<string, string> Compute(ComponentTheme theme, Dictionary<string, string> effective)
    {
        var parts = theme.Slots.ToDictionary(s => s.Key, s => new List<string?> { s.Value }, StringComparer.Ordinal);

        foreach (var (variant, values) in theme.Variants)
        {
            if (effective.TryGetValue(variant, out var value) && values.TryGetValue(value, out var slotClasses))
            {
                Add(parts, slotClasses);
            }
        }

        foreach (var compound in theme.CompoundVariants)
        {
            if (compound.When.All(c => effective.TryGetValue(c.Key, out var v) && v == c.Value))
            {
                Add(parts, compound.Classes);
            }
        }

        return parts.ToDictionary(p => p.Key, p => merger.Merge([.. p.Value]), StringComparer.Ordinal);
    }

    private static void Add(Dictionary<string, List<string?>> parts, SlotClasses classes)
    {
        foreach (var (slot, value) in classes)
        {
            parts[slot].Add(value);
        }
    }
}
