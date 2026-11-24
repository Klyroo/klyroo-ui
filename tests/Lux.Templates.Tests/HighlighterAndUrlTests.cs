using Blux.Templates.Content;

namespace Lux.Templates.Tests;

public class SyntaxHighlighterTests
{
    [Fact]
    public void Bash_has_commands_flags_strings_variables_and_comments()
    {
        var html = SyntaxHighlighter.ToHtml("export TOKEN=abc\ntern sync ./photos \"s3://bucket\" --dry-run -v | tee $LOG # note", "bash");

        Assert.Contains("<span class=\"tok-keyword\">export</span>", html);
        Assert.Contains("<span class=\"tok-command\">tern</span>", html);
        Assert.Contains("<span class=\"tok-string\">&quot;s3://bucket&quot;</span>", html);
        Assert.Contains("<span class=\"tok-flag\">--dry-run</span>", html);
        Assert.Contains("<span class=\"tok-flag\">-v</span>", html);
        Assert.Contains("<span class=\"tok-command\">tee</span>", html);
        Assert.Contains("<span class=\"tok-variable\">$LOG</span>", html);
        Assert.Contains("<span class=\"tok-comment\"># note</span>", html);
    }

    [Fact]
    public void Json_separates_keys_from_values()
    {
        var html = SyntaxHighlighter.ToHtml("{ \"retries\": 3, \"remote\": \"s3://b\", \"verbose\": true, \"x\": null }", "json");

        Assert.Contains("<span class=\"tok-property\">&quot;retries&quot;</span>", html);
        Assert.Contains("<span class=\"tok-number\">3</span>", html);
        Assert.Contains("<span class=\"tok-string\">&quot;s3://b&quot;</span>", html);
        Assert.Contains("<span class=\"tok-keyword\">true</span>", html);
        Assert.Contains("<span class=\"tok-keyword\">null</span>", html);
    }

    [Fact]
    public void Yaml_has_keys_comments_and_values()
    {
        var html = SyntaxHighlighter.ToHtml("# remote\nremote: \"s3://b\"\nlist:\n  - name: a\n    retries: 3\n    on: true", "yaml");

        Assert.Contains("<span class=\"tok-comment\"># remote</span>", html);
        Assert.Contains("<span class=\"tok-property\">remote</span>", html);
        Assert.Contains("<span class=\"tok-property\">name</span>", html);
        Assert.Contains("<span class=\"tok-property\">retries</span>", html);
        Assert.Contains("<span class=\"tok-number\">3</span>", html);
        Assert.Contains("<span class=\"tok-keyword\">true</span>", html);
    }

    [Fact]
    public void Toml_has_tables_keys_and_values()
    {
        var html = SyntaxHighlighter.ToHtml("[remote]\nurl = \"s3://b\"\nretries = 3", "toml");

        Assert.Contains("<span class=\"tok-keyword\">[remote]</span>", html);
        Assert.Contains("<span class=\"tok-property\">url</span>", html);
        Assert.Contains("<span class=\"tok-string\">&quot;s3://b&quot;</span>", html);
        Assert.Contains("<span class=\"tok-number\">3</span>", html);
    }

    [Fact]
    public void Csharp_and_html_have_their_tokens()
    {
        var cs = SyntaxHighlighter.ToHtml("public static int Count() => 42; // done", "csharp");
        var html = SyntaxHighlighter.ToHtml("<a href=\"x\">@Name</a><!-- c -->", "html");

        Assert.Contains("<span class=\"tok-keyword\">public</span>", cs);
        Assert.Contains("<span class=\"tok-number\">42</span>", cs);
        Assert.Contains("<span class=\"tok-comment\">// done</span>", cs);
        Assert.Contains("<span class=\"tok-tag\">&lt;a</span>", html);
        Assert.Contains("<span class=\"tok-property\">href</span>", html);
        Assert.Contains("<span class=\"tok-variable\">@Name</span>", html);
        Assert.Contains("<span class=\"tok-comment\">&lt;!-- c --&gt;</span>", html);
    }

    [Theory]
    [InlineData("BASH")]
    [InlineData("Sh")]
    [InlineData("yml")]
    [InlineData("c#")]
    public void Language_names_and_aliases_are_not_case_sensitive(string language) => Assert.True(SyntaxHighlighter.Supports(language));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("brainfuck")]
    public void Other_languages_are_shown_without_color_but_encoded(string? language)
    {
        Assert.False(SyntaxHighlighter.Supports(language));
        Assert.Equal("a &lt; b &amp;&amp; c", SyntaxHighlighter.ToHtml("a < b && c", language));
    }

    [Theory]
    [InlineData("bash"), InlineData("json"), InlineData("yaml"), InlineData("toml"), InlineData("csharp"), InlineData("html")]
    public void Markup_in_the_code_never_reaches_the_page_unencoded(string language)
    {
        var html = SyntaxHighlighter.ToHtml("<script>alert('x')</script> \"</span><img src=x onerror=y>\" # <b>", language);

        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("<b>", html);
        // The only tags are the spans of the tokens.
        Assert.DoesNotMatch("<(?!/?span( class=\"tok-[a-z]+\")?>)", html);
    }

    [Fact]
    public void The_text_of_a_highlighted_block_is_the_code()
    {
        const string code = "tern sync ./a --dry-run # go\nexport X=\"1\"";

        var html = SyntaxHighlighter.ToHtml(code, "bash");

        Assert.Equal(code, System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(html, "</?span[^>]*>", "")));
    }

    [Fact]
    public void A_large_hostile_input_does_not_hang()
    {
        var code = new string('"', 50_000) + string.Concat(Enumerable.Repeat("\\", 50_000));

        var started = DateTime.UtcNow;
        _ = SyntaxHighlighter.ToHtml(code, "json");

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5));
    }
}

public class ContentUrlTests
{
    [Theory]
    [InlineData("setup", true)]
    [InlineData("/guides/setup#x", true)]
    [InlineData("../a?b=1", true)]
    [InlineData("#top", true)]
    [InlineData("", true)]
    [InlineData("//cdn.example.com/a", true)]
    [InlineData("http://example.com", true)]
    [InlineData("HTTPS://example.com", true)]
    [InlineData("mailto:a@example.com", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("JaVaScRiPt:alert(1)", false)]
    [InlineData("  javascript:alert(1)", false)]
    [InlineData("java\tscript:alert(1)", false)]
    [InlineData("java\nscript:alert(1)", false)]
    [InlineData("\u0001javascript:alert(1)", false)]
    [InlineData("vbscript:x", false)]
    [InlineData("data:text/html,x", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("tel:+123", false)]
    public void Only_relative_http_https_and_mailto_addresses_are_allowed(string url, bool allowed) => Assert.Equal(allowed, ContentUrl.IsAllowed(url));

    [Theory]
    [InlineData("Getting started", "getting-started")]
    [InlineData("Écran d'accueil", "ecran-d-accueil")]
    [InlineData("v1.2_notes", "v1.2_notes")]
    [InlineData("  --x--  ", "x")]
    public void A_name_becomes_a_segment_of_an_address(string name, string expected) => Assert.Equal(expected, ContentUrl.Segment(name));
}
