using System.Net;
using System.Text.RegularExpressions;

namespace Lux.UI.Internal;

/// <summary>Turns HTML into readable plain text for display; the result is rendered escaped, so no markup survives.</summary>
internal static partial class HtmlText
{
    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 200)]
    private static partial Regex ScriptOrStyle();

    [GeneratedRegex(@"<\s*(br|/p|/div|/li|/h[1-6]|/blockquote|/pre|/tr)\b[^>]*>", RegexOptions.IgnoreCase, 200)]
    private static partial Regex LineBreaks();

    [GeneratedRegex(@"<[^>]*>", RegexOptions.None, 200)]
    private static partial Regex Tags();

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        try
        {
            var text = ScriptOrStyle().Replace(html, "");
            text = LineBreaks().Replace(text, "\n");
            text = Tags().Replace(text, "");
            return WebUtility.HtmlDecode(text).Trim();
        }
        catch (RegexMatchTimeoutException)
        {
            // Pathological input: show nothing rather than risk markup.
            return "";
        }
    }
}
