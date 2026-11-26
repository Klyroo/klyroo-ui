using System.Net;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Internal;

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
            // A busy server or pathological input: drop every tag in one linear pass instead of showing nothing. The result is
            // still rendered escaped, so no markup can survive either way; only the line breaks of block elements are lost.
            return WebUtility.HtmlDecode(StripTags(html)).Trim();
        }
    }

    // Everything outside < ... >, and nothing inside script or style elements.
    internal static string StripTags(string html)
    {
        var text = new System.Text.StringBuilder(html.Length);
        var i = 0;
        while (i < html.Length)
        {
            var c = html[i];
            if (c != '<')
            {
                text.Append(c);
                i++;
                continue;
            }
            var close = html.IndexOf('>', i + 1);
            if (close < 0) break;
            var tag = html.AsSpan(i + 1, close - i - 1).TrimStart();
            i = close + 1;
            foreach (var hidden in (ReadOnlySpan<string>)["script", "style"])
            {
                if (tag.StartsWith(hidden, StringComparison.OrdinalIgnoreCase)
                    && (tag.Length == hidden.Length || !char.IsLetterOrDigit(tag[hidden.Length])))
                {
                    var end = html.IndexOf("</" + hidden, i, StringComparison.OrdinalIgnoreCase);
                    i = end < 0 ? html.Length : end;
                    break;
                }
            }
        }
        return text.ToString();
    }
}
