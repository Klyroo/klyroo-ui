using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Blux.UI.BundleGenerator;

/// <summary>
/// Converts a guide page of the docs site (a Razor file whose prose sits in <c>&lt;div class="pg-prose"&gt;</c>) to Markdown.
/// It understands what the guides use: headings, paragraphs, lists, inline code, emphasis, links and <c>CodeBlock</c>
/// components (whose code may be a constant of the page). Live demos and other components are dropped.
/// </summary>
public static partial class GuideMarkdown
{
    /// <param name="razor">The source of the page.</param>
    /// <param name="expressions">Values of the Razor expressions the page prints (<c>@Expression</c>), by expression.</param>
    public static string Convert(string razor, IReadOnlyDictionary<string, string>? expressions = null)
    {
        razor = razor.Replace("\r\n", "\n");
        var codeStart = razor.IndexOf("\n@code", StringComparison.Ordinal);
        var constants = Constants(codeStart < 0 ? "" : razor[codeStart..]);
        var markup = codeStart < 0 ? razor : razor[..codeStart];

        var start = markup.IndexOf("<div class=\"pg-prose\">", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("The page has no <div class=\"pg-prose\"> block.");
        var end = markup.LastIndexOf("</div>", StringComparison.Ordinal);
        markup = markup[(start + "<div class=\"pg-prose\">".Length)..end];

        markup = RazorComment().Replace(markup, "");
        markup = NotProse().Replace(markup, "");
        markup = Eyebrow().Replace(markup, "");

        var blocks = new List<string>();
        var inline = new StringBuilder();
        string? block = null;       // "h1".."h3", "p" or "li" while collecting inline text
        var links = new Stack<string?>();

        void Flush()
        {
            var text = Collapse(inline.ToString());
            inline.Clear();
            if (block is not null && text.Length > 0)
            {
                blocks.Add(block switch
                {
                    "h1" => "# " + text,
                    "h2" => "## " + text,
                    "h3" => "### " + text,
                    "li" => "- " + text,
                    _ => text,
                });
            }
            block = null;
        }

        foreach (Match token in Token().Matches(markup))
        {
            if (!token.Groups["tag"].Success)
            {
                if (block is not null) inline.Append(Text(token.Value, expressions));
                continue;
            }

            var name = token.Groups["name"].Value;
            var closing = token.Groups["close"].Success;
            var attributes = token.Groups["attrs"].Value;
            var selfClosing = token.Groups["self"].Success;

            switch (name)
            {
                case "h1" or "h2" or "h3" or "p" or "li":
                    if (closing) Flush();
                    else
                    {
                        Flush();
                        block = name;
                    }
                    break;
                case "ul" or "ol":
                    if (closing) Flush();
                    break;
                case "code" when block is not null:
                    inline.Append('`');
                    break;
                case "strong" or "b":
                    inline.Append("**");
                    break;
                case "em" or "i":
                    inline.Append('*');
                    break;
                case "a":
                    if (block is null) break;
                    if (closing)
                    {
                        var href = links.Count > 0 ? links.Pop() : null;
                        inline.Append(href is null ? "" : $"]({href})");
                    }
                    else
                    {
                        var href = Attribute(attributes, "href");
                        links.Push(href);
                        if (href is not null) inline.Append('[');
                    }
                    break;
                case "CodeBlock" when !closing:
                    Flush();
                    blocks.Add(CodeBlock(attributes, constants));
                    break;
            }
        }
        Flush();
        return string.Join("\n\n", blocks) + "\n";
    }

    private static string Text(string raw, IReadOnlyDictionary<string, string>? expressions)
    {
        if (expressions is not null)
        {
            foreach (var (expression, value) in expressions) raw = raw.Replace("@" + expression, value, StringComparison.Ordinal);
        }
        raw = raw.Replace("@@", "\u0001");
        raw = Expression().Replace(raw, "");
        return WebUtility.HtmlDecode(raw.Replace("\u0001", "@"));
    }

    private static string Collapse(string text) => Whitespace().Replace(text, " ").Trim();

    /// <summary>The value of an attribute; <c>@("text")</c> (a C# string literal in Razor) gives <c>text</c>, <c>@Name</c> stays as is.</summary>
    private static string? Attribute(string attributes, string name)
    {
        var prefix = "(?<![\\w-])" + Regex.Escape(name) + "\\s*=\\s*";
        var literal = Regex.Match(attributes, prefix + "\"@\\(\"(?<v>.*?)\"\\)\"", RegexOptions.Singleline);
        if (literal.Success) return literal.Groups["v"].Value.Replace("\\\"", "\"");
        var plain = Regex.Match(attributes, prefix + "\"(?<v>[^\"]*)\"");
        return plain.Success ? plain.Groups["v"].Value : null;
    }

    private static string CodeBlock(string attributes, IReadOnlyDictionary<string, string> constants)
    {
        var title = Attribute(attributes, "Title");
        var code = Attribute(attributes, "Code") ?? "";
        if (code.StartsWith('@') && constants.TryGetValue(code[1..], out var constant))
        {
            code = constant;
        }
        code = WebUtility.HtmlDecode(code).Replace("@@", "@").Trim('\n');
        var fence = code.Contains("```") ? "````" : "```";
        var heading = title is null ? "" : $"**{title}**\n\n";
        return $"{heading}{fence}{Language(title, code)}\n{code}\n{fence}";
    }

    private static string Language(string? title, string code)
    {
        var extension = title is null ? "" : Path.GetExtension(title);
        return extension switch
        {
            ".cs" => "csharp",
            ".razor" => "razor",
            ".css" => "css",
            ".json" => "json",
            ".csproj" => "xml",
            _ when code.StartsWith("dotnet ", StringComparison.Ordinal) || code.StartsWith("npm ", StringComparison.Ordinal) || code.StartsWith("claude ", StringComparison.Ordinal) => "bash",
            _ when code.TrimStart().StartsWith('<') => "razor",
            _ when code.TrimStart().StartsWith('{') => "json",
            _ when code.Contains("{") && code.Contains(": ") && !code.Contains("builder") => "css",
            _ => "csharp",
        };
    }

    /// <summary>The <c>const string</c> raw literals of a page's code block, dedented like the compiler does.</summary>
    private static Dictionary<string, string> Constants(string code)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in RawConstant().Matches(code))
        {
            var lines = m.Groups["body"].Value.Split('\n');
            var indent = lines[^1];
            var content = lines.Skip(1).Take(lines.Length - 2)
                .Select(l => l.StartsWith(indent, StringComparison.Ordinal) ? l[indent.Length..] : l.TrimStart());
            result[m.Groups["name"].Value] = string.Join('\n', content);
        }
        return result;
    }

    [GeneratedRegex("""(?<tag><(?<close>/)?(?<name>[A-Za-z][\w.]*)(?<attrs>(?:"[^"]*"|[^>"])*?)(?<self>/)?>)|[^<]+|<""", RegexOptions.Singleline)]
    private static partial Regex Token();

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex RazorComment();

    [GeneratedRegex("""<div class="not-prose[^"]*">.*?</div>""", RegexOptions.Singleline)]
    private static partial Regex NotProse();

    [GeneratedRegex("""<p class="pg-eyebrow">.*?</p>""", RegexOptions.Singleline)]
    private static partial Regex Eyebrow();

    [GeneratedRegex(@"@[\w.]+(\([^)]*\))?")]
    private static partial Regex Expression();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("const string (?<name>\\w+) = \"\"\"(?<body>.*?)\"\"\"\\s*;", RegexOptions.Singleline)]
    private static partial Regex RawConstant();
}
