using System.Collections.Concurrent;

namespace Klyroo.UI.Maui;

/// <summary>
/// Reads and writes a string in the <see cref="Preferences"/> of the app. Where the platform has no preferences (the plain framework
/// target the unit tests run on), the value lives in memory for the life of the process.
/// </summary>
internal static class NavigationPreferences
{
    private static readonly ConcurrentDictionary<string, string> Memory = new(StringComparer.Ordinal);

    public static string? Get(string key)
    {
        try
        {
            return Preferences.Default.Get<string?>(key, null);
        }
        catch (NotImplementedException)
        {
            return Memory.TryGetValue(key, out var value) ? value : null;
        }
    }

    public static void Set(string key, string value)
    {
        try
        {
            Preferences.Default.Set(key, value);
        }
        catch (NotImplementedException)
        {
            Memory[key] = value;
        }
    }

    /// <summary>Forgets a value: for the tests, which share the process.</summary>
    public static void Remove(string key)
    {
        try
        {
            Preferences.Default.Remove(key);
        }
        catch (NotImplementedException)
        {
            Memory.TryRemove(key, out _);
        }
    }
}
