using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Blux.Playground.Client.Docs;

/// <summary>Small Razor/HTML/C# highlighter for the code blocks: enough to read examples, nothing more.</summary>
public static partial class Highlighter
{
    private static readonly HashSet<string> Keywords =
    [
        "private", "public", "static", "readonly", "new", "var", "return", "async", "await", "void", "bool", "int", "string",
        "true", "false", "null", "if", "else", "foreach", "in", "for", "using", "record", "class", "sealed", "get", "set", "init",
    ];

    [GeneratedRegex("""(?<comment>@\*[\s\S]*?\*@|//[^\n]*)|(?<string>"[^"\n]*")|(?<tag></?[A-Za-z][\w.]*)|(?<razor>@[\w.]+|@\()|(?<attr>[\w@:-]+(?==))|(?<word>\b[A-Za-z_]\w*\b)""")]
    private static partial Regex Tokens();

    public static string ToHtml(string code)
    {
        var sb = new StringBuilder();
        var last = 0;
        foreach (Match m in Tokens().Matches(code))
        {
            sb.Append(WebUtility.HtmlEncode(code[last..m.Index]));
            // Tuned for the near-black code background (all at least 4.5:1).
            var css = m.Groups["comment"].Success ? "text-[#7d858a] italic"
                : m.Groups["string"].Success ? "text-[#a9d97a]"
                : m.Groups["tag"].Success ? "text-[#7dcfff]"
                : m.Groups["razor"].Success ? "text-[#ffd23f]"
                : m.Groups["attr"].Success ? "text-[#e7c38f]"
                : m.Groups["word"].Success && Keywords.Contains(m.Value) ? "text-[#8fb2ff]"
                : null;
            var text = WebUtility.HtmlEncode(m.Value);
            sb.Append(css is null ? text : $"<span class=\"{css}\">{text}</span>");
            last = m.Index + m.Length;
        }
        sb.Append(WebUtility.HtmlEncode(code[last..]));
        return sb.ToString();
    }
}
