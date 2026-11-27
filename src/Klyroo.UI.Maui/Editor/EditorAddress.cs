using System.Text.RegularExpressions;

namespace Klyroo.UI.Maui;

/// <summary>
/// What the editor accepts as the address of an image or of a link, the same rules as the web editor: no space or control character,
/// at most 2,048 characters; an image comes from an http or https address; a link follows the list of schemes when there is one.
/// </summary>
internal static class EditorAddress
{
    private const int MaxLength = 2048;

    private static readonly string[] WebSchemes = ["http", "https"];

    // The addresses a link may have when the app lists no schemes: the ones TipTap allows, and relative addresses.
    private static readonly Regex DefaultLink = new(@"^(?:(?:https?|ftps?|mailto|tel|callto|sms|cid|xmpp):|[^a-z]|[a-z0-9+.\-]+(?:[^a-z+.\-:]|$))", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Schemes as the app wrote them (<c>https:</c>, <c>HTTPS</c>) to the form the checks use, or null for an empty list.</summary>
    public static string[]? Normalize(IReadOnlyList<string>? schemes)
    {
        var list = schemes?.Select(s => s.Trim().TrimEnd(':').ToLowerInvariant()).Where(s => s.Length > 0).ToArray();
        return list is { Length: > 0 } ? list : null;
    }

    /// <summary>An absolute address of an http or https scheme the app allows (both when it lists none), with a host and no user name or password.</summary>
    public static bool IsImage(string? address, IReadOnlyList<string>? schemes)
    {
        var value = Plain(address);
        if (value is null) return false;
        var allowed = Normalize(schemes)?.Where(s => WebSchemes.Contains(s)).ToArray() ?? WebSchemes;
        return Absolute(value, allowed);
    }

    /// <summary>With a list of schemes, only absolute addresses of those schemes; without one, the default rules, which also take relative addresses, tel: and a few more.</summary>
    public static bool IsLink(string? address, IReadOnlyList<string>? schemes)
    {
        var value = Plain(address);
        if (value is null) return false;
        var list = Normalize(schemes);
        return list is null ? DefaultLink.IsMatch(value) : Absolute(value, list);
    }

    /// <summary>The sentence that says why an address was refused.</summary>
    public static string Refusal(IReadOnlyList<string>? schemes) =>
        Normalize(schemes) is { } list ? $"Only {string.Join(", ", list)} addresses are accepted." : "This address is not accepted.";

    private static string? Plain(string? address)
    {
        var value = address?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))) return null;
        return value;
    }

    private static bool Absolute(string value, string[] schemes)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        var scheme = uri.Scheme.ToLowerInvariant();
        if (!schemes.Contains(scheme)) return false;
        if (WebSchemes.Contains(scheme))
        {
            // "https:example.com" parses and "https://" does not: a web address has two slashes and a host.
            return value.StartsWith(scheme + "://", StringComparison.OrdinalIgnoreCase) && uri.Host.Length > 0 && uri.UserInfo.Length == 0;
        }
        // "mailto:" with nothing after it.
        return value.Length > scheme.Length + 1;
    }
}
