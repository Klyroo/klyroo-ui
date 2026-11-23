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

    /// <summary>The longest encoded preferences value for one list. Lists share one grouped cookie per path base (<see cref="CookieName"/>, capped at <see cref="MaxCookieLength"/>).</summary>
    public const int MaxEncodedLength = 600;

    /// <summary>The longest grouped cookie value (<see cref="CookieName"/>) across all lists of the app.</summary>
    public const int MaxCookieLength = 3000;

    /// <summary>The maximum number of lists kept in the grouped cookie (<see cref="CookieName"/>); older entries are evicted in LRU order.</summary>
    public const int MaxCookieEntries = 32;

    public const int MaxKeyLength = 64;
    public const int MaxKeysPerSection = 100;
    public const string CookieName = "blux-dl-cols";

    public List<string> Order { get; } = [];
    public HashSet<string> Hidden { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Shown { get; } = new(StringComparer.Ordinal);
    public HashSet<string> PinnedStart { get; } = new(StringComparer.Ordinal);
    public HashSet<string> PinnedEnd { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Unpinned { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Widths { get; } = new(StringComparer.Ordinal);
    public bool? SummaryView { get; set; }
    public HashSet<string> ToggledGroups { get; } = new(StringComparer.Ordinal);

    public bool HasColumnOverrides =>
        Order.Count > 0 || Hidden.Count > 0 || Shown.Count > 0
        || PinnedStart.Count > 0 || PinnedEnd.Count > 0 || Unpinned.Count > 0
        || Widths.Count > 0;

    public bool IsEmpty =>
        !HasColumnOverrides && SummaryView is null && ToggledGroups.Count == 0;

    public void ClearColumns()
    {
        Order.Clear();
        Hidden.Clear();
        Shown.Clear();
        PinnedStart.Clear();
        PinnedEnd.Clear();
        Unpinned.Clear();
        Widths.Clear();
    }

    public void Clear()
    {
        ClearColumns();
        SummaryView = null;
        ToggledGroups.Clear();
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
        copy.SummaryView = SummaryView;
        copy.ToggledGroups.UnionWith(ToggledGroups);
        return copy;
    }

    /// <summary>Drops every key that is not in <paramref name="knownKeys"/>, every key that is in two sections that exclude each other, and the visibility of <paramref name="requiredKeys"/>.</summary>
    public void Prune(IReadOnlySet<string> knownKeys, IReadOnlySet<string> requiredKeys)
    {
        static bool ValidKey(string k, IReadOnlySet<string> known) => k.Length is > 0 and <= MaxKeyLength && known.Contains(k);
        Order.RemoveAll(k => !ValidKey(k, knownKeys));
        Hidden.RemoveWhere(k => !ValidKey(k, knownKeys) || requiredKeys.Contains(k));
        Shown.RemoveWhere(k => !ValidKey(k, knownKeys) || Hidden.Contains(k));
        PinnedStart.RemoveWhere(k => !ValidKey(k, knownKeys));
        PinnedEnd.RemoveWhere(k => !ValidKey(k, knownKeys) || PinnedStart.Contains(k));
        Unpinned.RemoveWhere(k => !ValidKey(k, knownKeys) || PinnedStart.Contains(k) || PinnedEnd.Contains(k));
        foreach (var key in Widths.Keys.Where(k => !ValidKey(k, knownKeys)).ToList()) Widths.Remove(key);
        ToggledGroups.RemoveWhere(k => k.Length is 0 or > MaxKeyLength);
    }

    /// <summary>Sanitizes a <c>PreferencesKey</c> to the cookie-safe subset (<c>[A-Za-z0-9_-]</c>, max 64 chars), or <see langword="null"/> when blank.</summary>
    public static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var sb = new StringBuilder(Math.Min(key.Length, MaxKeyLength));
        foreach (var ch in key)
        {
            if (sb.Length == MaxKeyLength) break;
            if (char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-') sb.Append(ch);
            else sb.Append('_');
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// Encodes the preferences as a compact cookie-safe token, or empty string when nothing overrides defaults. Returns <see langword="null"/> when any key exceeds <see cref="MaxKeyLength"/>,
    /// any section exceeds <see cref="MaxKeysPerSection"/>, or the result would pass <see cref="MaxEncodedLength"/>: the caller keeps the preferences in memory and leaves the cookie as it was.
    /// </summary>
    public string? Encode()
    {
        if (IsEmpty) return "";
        if (!ValidSection(Order) || !ValidSection(Hidden) || !ValidSection(Shown)
            || !ValidSection(PinnedStart) || !ValidSection(PinnedEnd) || !ValidSection(Unpinned)
            || !ValidSection(Widths.Keys) || !ValidSection(ToggledGroups))
        {
            return null;
        }
        var parts = new List<string>(9) { "v1" };
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
        if (SummaryView is { } sv) parts.Add(sv ? "m1" : "m0");
        if (ToggledGroups.Count > 0) parts.Add("g" + JoinKeys(ToggledGroups.Order(StringComparer.Ordinal)));
        var encoded = string.Join('!', parts);
        return encoded.Length > MaxEncodedLength ? null : encoded;
    }

    private static bool ValidSection(IReadOnlyCollection<string> keys) =>
        keys.Count <= MaxKeysPerSection && keys.All(k => k.Length is > 0 and <= MaxKeyLength);

    /// <summary>Extracts and decodes the preferences of <paramref name="normalizedKey"/> from the grouped <see cref="CookieName"/> cookie value (<c>key1.v1!...!!key2.v1!...</c>).</summary>
    public static DataListColumnPreferences DecodeFromCookie(string? rawCookie, string? normalizedKey)
    {
        if (string.IsNullOrWhiteSpace(rawCookie) || rawCookie.Length > MaxCookieLength || string.IsNullOrEmpty(normalizedKey))
            return new DataListColumnPreferences();
        var count = 0;
        foreach (var entry in rawCookie.Split("!!", StringSplitOptions.RemoveEmptyEntries))
        {
            if (++count > MaxCookieEntries) break;
            var dot = entry.IndexOf('.');
            if (dot <= 0) continue;
            if (entry.AsSpan(0, dot).SequenceEqual(normalizedKey.AsSpan()))
                return Decode(entry[(dot + 1)..]);
        }
        return new DataListColumnPreferences();
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
                case 'm':
                    if (body == "1") prefs.SummaryView = true;
                    else if (body == "0") prefs.SummaryView = false;
                    break;
                case 'g':
                    foreach (var key in SplitKeys(body)) prefs.ToggledGroups.Add(key);
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
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IServiceProvider, HashSet<string>> ClaimedKeys = new();

    public static string Claim(IServiceProvider services, string normalizedKey)
    {
        var keys = ClaimedKeys.GetOrCreateValue(services);
        lock (keys)
        {
            if (!keys.Add(normalizedKey))
            {
                throw new InvalidOperationException($"Two data lists on the same page use the PreferencesKey '{normalizedKey}': give each list its own PreferencesKey.");
            }
        }
        return normalizedKey;
    }

    public static void Release(IServiceProvider services, string? claimedKey)
    {
        if (claimedKey is null || !ClaimedKeys.TryGetValue(services, out var keys)) return;
        lock (keys) keys.Remove(claimedKey);
    }

    /// <summary>
    /// The preferences the grouped cookie of the current request holds for <paramref name="normalizedKey"/>. <c>ReadFromServer</c> is true when there was a request to ask (even if it carried no cookie):
    /// the browser has nothing to add for this render. An interactive render has no request (a circuit is not an HTTP request) and the browser has no <see cref="IBxRequestCookies"/>: both read the cookie in the browser instead.
    /// </summary>
    public static (DataListColumnPreferences Preferences, bool ReadFromServer) Read(IServiceProvider services, string? normalizedKey, bool interactive)
    {
        if (normalizedKey is null || interactive || OperatingSystem.IsBrowser()) return (new DataListColumnPreferences(), false);
        if (services.GetService<IBxRequestCookies>() is not { } request)
        {
            services.GetService<MissingServerWarning>()?.WarnColumns(services);
            return (new DataListColumnPreferences(), false);
        }
        var cookieName = BxPathScope.CookieName(DataListColumnPreferences.CookieName, request.PathBase);
        return (DataListColumnPreferences.DecodeFromCookie(request.Get(cookieName), normalizedKey), true);
    }
}
