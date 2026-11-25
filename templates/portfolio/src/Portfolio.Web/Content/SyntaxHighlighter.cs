using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Klyroo.Templates.Content;

/// <summary>
/// A small syntax highlighter for the code blocks of a page: it wraps tokens in <c>&lt;span class="tok-..."&gt;</c> and nothing else, so the colors live in
/// the stylesheet (and follow light and dark mode). It knows the languages a documentation site needs most: bash, json, yaml, toml, csharp and html
/// (with xml and razor). Any other language, and any text its patterns cannot handle in time, is shown without color. The output is HTML-encoded.
/// </summary>
public static class SyntaxHighlighter
{
    private const RegexOptions Options = RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    /// <summary>Code longer than this (in characters) is shown without color: a block that long is not meant to be read as code, and highlighting it costs time.</summary>
    public const int MaxLength = 20_000;

    private static Regex Build(string pattern) => new(pattern, Options, Timeout);

    private const string DoubleQuoted = @"""(?:\\.|[^""\\\n])*""";

    private static readonly Regex Bash = Build(
        """(?<comment>(?<![^\s])#[^\n]*)|(?<string>""" + DoubleQuoted + """|'[^'\n]*')|(?<variable>\$\{[^}\n]*\}|\$[A-Za-z_][A-Za-z0-9_]*|\$[0-9?#@*!$])""" +
        """|(?<flag>(?<![\w-])--?[A-Za-z][\w-]*)|(?<keyword>\b(?:if|then|elif|else|fi|for|do|done|while|case|esac|in|export|function|sudo)\b)""" +
        """|(?<command>(?:(?<=^[ \t]*)|(?<=[|;&]\s*))[A-Za-z_./][\w./-]*)|(?<number>\b\d+\b)""");

    private static readonly Regex Json = Build(
        """(?<property>""" + DoubleQuoted + """(?=\s*:))|(?<string>""" + DoubleQuoted + """)|(?<number>-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)|(?<keyword>\b(?:true|false|null)\b)""");

    private static readonly Regex Yaml = Build(
        """(?<comment>(?<![^\s])#[^\n]*)|(?<property>(?<=^[ \t]*(?:-[ \t]+)?)[\w.\-]+(?=:(?:[ \t]|$)))|(?<string>""" + DoubleQuoted + """|'[^'\n]*')""" +
        """|(?<keyword>\b(?:true|false|null|yes|no)\b|~)|(?<number>(?<![\w.-])-?\d+(?:\.\d+)?(?![\w.-]))""");

    private static readonly Regex Toml = Build(
        """(?<comment>(?<![^\s])#[^\n]*)|(?<keyword>(?<=^[ \t]*)\[\[?[^\]\n]+\]\]?)|(?<property>(?<=^[ \t]*)[\w.\-]+(?=[ \t]*=))|(?<string>""" + DoubleQuoted + """|'[^'\n]*')""" +
        """|(?<keyword>\b(?:true|false)\b)|(?<number>(?<![\w.-])-?\d+(?:\.\d+)?(?![\w.-]))""");

    private static readonly Regex CSharp = Build(
        """(?<comment>//[^\n]*|/\*[\s\S]*?(?:\*/|\z))|(?<string>[$@]{0,2}"(?:\\.|[^"\\\n])*")|(?<keyword>\b(?:abstract|as|async|await|base|bool|break|case|catch|char|class|const|continue|decimal|default|delegate|do|double|else|enum|event|false|finally|float|for|foreach|if|in|int|interface|internal|is|lock|long|namespace|new|null|object|out|override|private|protected|public|readonly|record|ref|return|sealed|static|string|struct|switch|this|throw|true|try|typeof|using|var|virtual|void|while|get|set|init)\b)""" +
        """|(?<type>\b[A-Z][A-Za-z0-9_]*\b)|(?<number>\b\d+(?:\.\d+)?\b)""");

    private static readonly Regex Html = Build(
        """(?<comment><!--[\s\S]*?(?:-->|\z)|@\*[\s\S]*?(?:\*@|\z))|(?<tag></?[A-Za-z][\w:.\-]*)|(?<property>(?<=\s)[\w:@.\-]+(?==))|(?<string>""" + DoubleQuoted + """|'[^'\n]*')|(?<variable>@[\w.]+)""");

    private static readonly Dictionary<string, Regex> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bash"] = Bash, ["sh"] = Bash, ["shell"] = Bash, ["console"] = Bash, ["zsh"] = Bash,
        ["json"] = Json, ["jsonc"] = Json,
        ["yaml"] = Yaml, ["yml"] = Yaml,
        ["toml"] = Toml,
        ["csharp"] = CSharp, ["cs"] = CSharp, ["c#"] = CSharp,
        ["html"] = Html, ["xml"] = Html, ["razor"] = Html, ["svg"] = Html,
    };

    /// <summary>True when the language has patterns (the name is case-insensitive).</summary>
    public static bool Supports(string? language) => language is not null && Languages.ContainsKey(language);

    /// <summary>The code as HTML: encoded, with the tokens of a known language in spans.</summary>
    public static string ToHtml(string code, string? language)
    {
        if (language is null || code.Length > MaxLength || !Languages.TryGetValue(language, out var pattern))
        {
            return WebUtility.HtmlEncode(code);
        }

        try
        {
            var html = new StringBuilder(code.Length * 2);
            var last = 0;
            // The timeout of the pattern is for one match; text made of many short matches that are each slow would add up, so the whole block has a budget too.
            var clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (Match match in pattern.Matches(code))
            {
                if (clock.Elapsed > Timeout)
                {
                    return WebUtility.HtmlEncode(code);
                }
                if (match.Length == 0)
                {
                    continue;
                }
                html.Append(WebUtility.HtmlEncode(code[last..match.Index]));
                var kind = Kind(pattern, match);
                var text = WebUtility.HtmlEncode(match.Value);
                html.Append(kind is null ? text : $"<span class=\"tok-{kind}\">{text}</span>");
                last = match.Index + match.Length;
            }
            html.Append(WebUtility.HtmlEncode(code[last..]));
            return html.ToString();
        }
        catch (RegexMatchTimeoutException)
        {
            return WebUtility.HtmlEncode(code);
        }
    }

    private static string? Kind(Regex pattern, Match match)
    {
        foreach (var name in pattern.GetGroupNames())
        {
            if (name != "0" && match.Groups[name].Success)
            {
                return name;
            }
        }
        return null;
    }
}
