using System.Collections.Concurrent;

namespace Klyroo.UI.Maui;

/// <summary>
/// The icons <see cref="KlyIcon"/> can draw: the Lucide icons built into the library, and the ones you register.
/// A name is a bare Lucide name (<c>search</c>), or the Klyroo.UI forms <c>i-lucide-search</c> and <c>lucide:search</c>, which mean the same.
/// An icon of another set is registered under the full name you will use, for example <c>mdi:home</c>.
/// </summary>
public static class KlyIcons
{
    private static readonly ConcurrentDictionary<string, string[]> Registered = new(StringComparer.Ordinal);

    /// <summary>Registers an icon, or replaces a built-in one. Each string is SVG path data in a 24x24 box, drawn with a 2 unit round stroke like the Lucide icons.</summary>
    /// <param name="name">The name to use in <see cref="KlyIcon.Name"/>.</param>
    /// <param name="paths">The shapes of the icon, as SVG path data.</param>
    public static void Register(string name, params string[] paths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfZero(paths.Length);
        Registered[Normalize(name)] = paths;
    }

    /// <summary>The shapes of an icon, as SVG path data, or <c>false</c> when the name is neither registered nor a built-in Lucide icon.</summary>
    public static bool TryGet(string? name, out string[] paths)
    {
        paths = [];
        if (string.IsNullOrWhiteSpace(name)) return false;
        var key = Normalize(name);
        return Registered.TryGetValue(key, out paths!) || BuiltinIcons.Lucide.TryGetValue(key, out paths!);
    }

    private static string Normalize(string name)
    {
        name = name.Trim();
        if (name.StartsWith("i-lucide-", StringComparison.Ordinal)) return name["i-lucide-".Length..];
        if (name.StartsWith("lucide:", StringComparison.Ordinal)) return name["lucide:".Length..];
        return name;
    }
}
