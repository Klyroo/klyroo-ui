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
/// <remarks>
/// The value holds only <c>A-Z a-z 0-9 . _ - ! ~ * '</c>: no <c>%</c>, <c>+</c>, <c>;</c>, <c>,</c>, space or quote, because ASP.NET Core percent-decodes cookie values (and turns <c>+</c> into a space)
/// before the app sees them, and a value that survives that unchanged is the same on the server and in the browser. A column key with another character is written as <c>'</c> and two hex digits per UTF-8 byte.
/// What is decoded is data of the user: it never reaches markup or a style except through a column of the list that has the key (see <see cref="Prune"/>), widths are clamped, and a value over
/// <see cref="MaxEncodedLength"/> or with a token that is not valid is ignored token by token.
/// </remarks>
internal sealed class DataListColumnPreferences
{
    public const int MinAllowedWidth = 64;
    public const int MaxAllowedWidth = 900;

    /// <summary>The longest value written to or read from the cookie. A cookie is limited to about 4 KB with its name, and a request carries every cookie of the site.</summary>
    public const int MaxEncodedLength = 2000;

    private const int MaxKeyLength = 64;
    private const int MaxKeysPerSection = 100;
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

    public DataListColumnPreferences Clone()
    {
        var copy = new DataListColumnPreferences();
        copy.Order.AddRange(Order);
        copy.Hidden.UnionWith(Hidden);
        copy.Shown.UnionWith(Shown);
        copy.PinnedStart.UnionWith(PinnedStart);
        copy.PinnedEnd.UnionWith(PinnedEnd);
        copy.Unpinned.UnionWith(Unpinned);
        foreach (var (key, width) in Widths) copy.Widths[key] = width;
        return copy;
    }

    /// <summary>Drops every key that is not in <paramref name="knownKeys"/>, every key that is in two sections that exclude each other, and the visibility of <paramref name="requiredKeys"/>.</summary>
    public void Prune(IReadOnlySet<string> knownKeys, IReadOnlySet<string> requiredKeys)
    {
        Order.RemoveAll(k => !knownKeys.Contains(k));
        Hidden.RemoveWhere(k => !knownKeys.Contains(k) || requiredKeys.Contains(k));
        Shown.RemoveWhere(k => !knownKeys.Contains(k) || Hidden.Contains(k));
        PinnedStart.RemoveWhere(k => !knownKeys.Contains(k));
        PinnedEnd.RemoveWhere(k => !knownKeys.Contains(k) || PinnedStart.Contains(k));
        Unpinned.RemoveWhere(k => !knownKeys.Contains(k) || PinnedStart.Contains(k) || PinnedEnd.Contains(k));
        foreach (var key in Widths.Keys.Where(k => !knownKeys.Contains(k)).ToList()) Widths.Remove(key);
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

    /// <summary>
    /// Encodes the preferences as a compact cookie-safe token, or empty string when nothing overrides defaults. Returns <see langword="null"/> when the result would pass
    /// <see cref="MaxEncodedLength"/>: the caller keeps the preferences in memory and leaves the cookie as it was.
    /// </summary>
    public string? Encode()
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
        var encoded = string.Join('!', parts);
        return encoded.Length > MaxEncodedLength ? null : encoded;
    }

    /// <summary>Parses an encoded cookie string. Never throws on malformed input: invalid tokens are skipped.</summary>
    public static DataListColumnPreferences Decode(string? raw)
    {
        var prefs = new DataListColumnPreferences();
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > MaxEncodedLength) return prefs;
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
                    var count = 0;
                    foreach (var entry in body.Split('~', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (++count > MaxKeysPerSection) break;
                        var star = entry.LastIndexOf('*');
                        if (star <= 0 || star == entry.Length - 1) continue;
                        var key = Unescape(entry[..star]);
                        if (key is null) continue;
                        var digits = entry[(star + 1)..];
                        if (digits.Length > 5 || !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var px)) continue;
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
        var count = 0;
        foreach (var token in body.Split('~', StringSplitOptions.RemoveEmptyEntries))
        {
            if (++count > MaxKeysPerSection) yield break;
            if (Unescape(token) is { Length: > 0 } key) yield return key;
        }
    }

    private static bool IsSafe(char ch) => char.IsAsciiLetterOrDigit(ch) || ch is '_' or '.' or '-';

    // Characters outside the safe set become ' and two hex digits per UTF-8 byte, so the token holds none of the characters a cookie or a server would change.
    private static string Escape(string key)
    {
        var builder = new StringBuilder(key.Length + 4);
        foreach (var rune in key.EnumerateRunes())
        {
            if (rune.Value < 128 && IsSafe((char)rune.Value))
            {
                builder.Append((char)rune.Value);
                continue;
            }
            foreach (var b in Encoding.UTF8.GetBytes(rune.ToString())) builder.Append('\'').Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }

    // The key of a token, or null when the token is not one this class wrote: a character outside the alphabet, a bad escape, bytes that are not UTF-8, or a key that is too long.
    private static string? Unescape(string token)
    {
        if (token.Length == 0 || token.Length > MaxKeyLength * 12) return null;
        var bytes = new List<byte>(token.Length);
        for (var i = 0; i < token.Length; i++)
        {
            var ch = token[i];
            if (ch == '\'')
            {
                if (i + 2 >= token.Length) return null;
                if (!byte.TryParse(token.AsSpan(i + 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var b)) return null;
                bytes.Add(b);
                i += 2;
            }
            else if (IsSafe(ch)) bytes.Add((byte)ch);
            else return null;
        }
        try
        {
            var key = new UTF8Encoding(false, true).GetString(bytes.ToArray());
            return key.Length is > 0 and <= MaxKeyLength ? key : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

/// <summary>Reads column preferences from <see cref="IBxRequestCookies"/> during SSR or prerender so the first paint has the user's columns.</summary>
internal static class ServerColumnPreferences
{
    /// <summary>
    /// The preferences the cookie of the current request holds. <c>ReadFromServer</c> is true when there was a request to ask (even if it carried no cookie): the browser has nothing to add for this render.
    /// An interactive render has no request (a circuit is not an HTTP request) and the browser has no <see cref="IBxRequestCookies"/>: both read the cookie in the browser instead.
    /// </summary>
    public static (DataListColumnPreferences Preferences, bool ReadFromServer) Read(IServiceProvider services, string? normalizedKey, bool interactive)
    {
        if (normalizedKey is null || interactive || OperatingSystem.IsBrowser()) return (new DataListColumnPreferences(), false);
        if (services.GetService<IBxRequestCookies>() is not { } request)
        {
            services.GetService<MissingServerWarning>()?.WarnColumns(services);
            return (new DataListColumnPreferences(), false);
        }
        var cookieName = BxPathScope.CookieName(DataListColumnPreferences.CookieNameFor(normalizedKey), request.PathBase);
        return (DataListColumnPreferences.Decode(request.Get(cookieName)), true);
    }
}
