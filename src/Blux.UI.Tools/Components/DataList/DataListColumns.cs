using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools;

/// <summary>Which horizontal edge a column of <see cref="BxDataListTable{TItem}"/> is pinned to.</summary>
public enum DataListPin
{
    /// <summary>Scrolls horizontally with the table.</summary>
    None,

    /// <summary>Sticks to the start edge (left in left-to-right languages).</summary>
    Start,

    /// <summary>Sticks to the end edge (right in left-to-right languages).</summary>
    End,
}

/// <summary>The effective state of one column of a data list: visibility, pinning, width and limits.</summary>
/// <param name="Key">The column key (<see cref="BxDataListColumn{TItem}.EffectiveKey"/>).</param>
/// <param name="Title">The display title of the column.</param>
/// <param name="Visible">Whether the column is currently shown.</param>
/// <param name="Required">Whether the column cannot be hidden.</param>
/// <param name="Pinned">The active horizontal pin edge.</param>
/// <param name="Width">The active pixel width, or <see langword="null"/> when automatic.</param>
/// <param name="MinWidth">Minimum width in pixels when resized.</param>
/// <param name="MaxWidth">Maximum width in pixels when resized.</param>
/// <param name="Resizable">Whether the user can resize the column.</param>
public sealed record DataListColumnState(
    string Key,
    string Title,
    bool Visible,
    bool Required,
    DataListPin Pinned,
    int? Width,
    int MinWidth,
    int MaxWidth,
    bool Resizable);

/// <summary>
/// User preferences for the columns of a data list: order, visibility overrides, pinning overrides and pixel widths.
/// Encoded into a compact, cookie-safe string (<c>v1!o...!h...!v...!s...!e...!u...!w...</c>) so the server reads it on the very first SSR or prerender paint.
/// </summary>
internal sealed class DataListColumnPreferences
{
    public const int MinAllowedWidth = 64;
    public const int MaxAllowedWidth = 900;
    private const string CookiePrefix = "blux-dl-cols-";

    public List<string> Order { get; } = [];
    public HashSet<string> Hidden { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Shown { get; } = new(StringComparer.Ordinal);
    public HashSet<string> PinnedStart { get; } = new(StringComparer.Ordinal);
    public HashSet<string> PinnedEnd { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Unpinned { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Widths { get; } = new(StringComparer.Ordinal);

    public bool IsEmpty =>
        Order.Count == 0 && Hidden.Count == 0 && Shown.Count == 0
        && PinnedStart.Count == 0 && PinnedEnd.Count == 0 && Unpinned.Count == 0
        && Widths.Count == 0;

    public void Clear()
    {
        Order.Clear();
        Hidden.Clear();
        Shown.Clear();
        PinnedStart.Clear();
        PinnedEnd.Clear();
        Unpinned.Clear();
        Widths.Clear();
    }

    /// <summary>Sanitizes a <c>PreferencesKey</c> to the cookie-safe subset (<c>[A-Za-z0-9_-]</c>, max 64 chars), or <see langword="null"/> when blank.</summary>
    public static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var sb = new StringBuilder(Math.Min(key.Length, 64));
        foreach (var ch in key)
        {
            if (sb.Length == 64) break;
            if (char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-') sb.Append(ch);
            else sb.Append('_');
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    public static string CookieNameFor(string normalizedKey) => CookiePrefix + normalizedKey;

    /// <summary>Encodes the preferences as a compact cookie-safe token, or empty string when nothing overrides defaults.</summary>
    public string Encode()
    {
        if (IsEmpty) return "";
        var parts = new List<string>(8) { "v1" };
        if (Order.Count > 0) parts.Add("o" + JoinKeys(Order));
        if (Hidden.Count > 0) parts.Add("h" + JoinKeys(Hidden.Order(StringComparer.Ordinal)));
        if (Shown.Count > 0) parts.Add("v" + JoinKeys(Shown.Order(StringComparer.Ordinal)));
        if (PinnedStart.Count > 0) parts.Add("s" + JoinKeys(PinnedStart.Order(StringComparer.Ordinal)));
        if (PinnedEnd.Count > 0) parts.Add("e" + JoinKeys(PinnedEnd.Order(StringComparer.Ordinal)));
        if (Unpinned.Count > 0) parts.Add("u" + JoinKeys(Unpinned.Order(StringComparer.Ordinal)));
        if (Widths.Count > 0)
        {
            parts.Add("w" + string.Join('~', Widths.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{Escape(p.Key)}*{Math.Clamp(p.Value, MinAllowedWidth, MaxAllowedWidth).ToString(CultureInfo.InvariantCulture)}")));
        }
        return string.Join('!', parts);
    }

    /// <summary>Parses an encoded cookie string. Never throws on malformed input: invalid tokens are skipped.</summary>
    public static DataListColumnPreferences Decode(string? raw)
    {
        var prefs = new DataListColumnPreferences();
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 2048) return prefs;
        var sections = raw.Split('!', StringSplitOptions.RemoveEmptyEntries);
        if (sections.Length < 2 || sections[0] != "v1") return prefs;

        for (var i = 1; i < sections.Length; i++)
        {
            var section = sections[i];
            if (section.Length < 2) continue;
            var tag = section[0];
            var body = section[1..];
            switch (tag)
            {
                case 'o':
                    foreach (var key in SplitKeys(body))
                        if (!prefs.Order.Contains(key, StringComparer.Ordinal)) prefs.Order.Add(key);
                    break;
                case 'h':
                    foreach (var key in SplitKeys(body)) prefs.Hidden.Add(key);
                    break;
                case 'v':
                    foreach (var key in SplitKeys(body)) prefs.Shown.Add(key);
                    break;
                case 's':
                    foreach (var key in SplitKeys(body)) prefs.PinnedStart.Add(key);
                    break;
                case 'e':
                    foreach (var key in SplitKeys(body)) prefs.PinnedEnd.Add(key);
                    break;
                case 'u':
                    foreach (var key in SplitKeys(body)) prefs.Unpinned.Add(key);
                    break;
                case 'w':
                    foreach (var entry in body.Split('~', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var star = entry.LastIndexOf('*');
                        if (star <= 0 || star == entry.Length - 1) continue;
                        var key = Unescape(entry[..star]);
                        if (key is null) continue;
                        if (int.TryParse(entry[(star + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var px))
                            prefs.Widths[key] = Math.Clamp(px, MinAllowedWidth, MaxAllowedWidth);
                    }
                    break;
            }
        }
        return prefs;
    }

    private static string JoinKeys(IEnumerable<string> keys) => string.Join('~', keys.Select(Escape));

    private static IEnumerable<string> SplitKeys(string body)
    {
        foreach (var token in body.Split('~', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Unescape(token) is { Length: > 0 } key) yield return key;
        }
    }

    private static string Escape(string key) => Uri.EscapeDataString(key);

    private static string? Unescape(string token)
    {
        try
        {
            return Uri.UnescapeDataString(token);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }
}

/// <summary>Reads column preferences from <see cref="IBxRequestCookies"/> during SSR or prerender so the first paint has the user's columns.</summary>
internal static class ServerColumnPreferences
{
    public static (DataListColumnPreferences Preferences, bool ReadFromServer) Read(IServiceProvider services, string? normalizedKey)
    {
        if (normalizedKey is null || OperatingSystem.IsBrowser()) return (new DataListColumnPreferences(), false);
        if (services.GetService<IBxRequestCookies>() is not { } request) return (new DataListColumnPreferences(), false);
        var cookieName = BxPathScope.CookieName(DataListColumnPreferences.CookieNameFor(normalizedKey), request.PathBase);
        return (DataListColumnPreferences.Decode(request.Get(cookieName)), true);
    }
}
