using System.Text.RegularExpressions;
using Changelog.Web.ReleaseNotes;

namespace Klyroo.Templates.Tests;

/// <summary>
/// Release notes are untrusted input. Every test here renders a hostile body and then checks the HTML itself, not only the one thing the vector aims at:
/// <see cref="AssertSafe"/> parses the tags and attributes that came out and fails on any tag, attribute or address that Markdown from a trusted page could not produce.
/// </summary>
public partial class ReleaseBodyTests
{
    private static readonly ReleaseBody Body = new("https://github.com/octo/tern");

    private static string Html(string markdown, string slug = "v1", string pageHref = "releases/v1", int minHeading = 3) =>
        Body.Render(ChangelogSamples.Entry("v1", markdown, slug), pageHref, minHeading);

    private static readonly HashSet<string> Tags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "a", "ul", "ol", "li", "code", "pre", "div", "span", "button", "h2", "h3", "h4", "h5", "h6", "em", "strong", "del", "ins", "mark", "sub", "sup",
        "blockquote", "table", "thead", "tbody", "tr", "th", "td", "img", "br", "hr", "input", "dl", "dt", "dd", "section", "sup",
    };

    [GeneratedRegex(@"<\s*/?\s*([A-Za-z][A-Za-z0-9\-]*)([^>]*)>")]
    private static partial Regex TagPattern();

    [GeneratedRegex("\\s([A-Za-z_:][A-Za-z0-9_:.\\-]*)(?:=\"([^\"]*)\")?")]
    private static partial Regex AttributePattern();

    /// <summary>Fails when the output has a tag, an attribute or an address that is not one the renderer is meant to write.</summary>
    private static void AssertSafe(string html)
    {
        foreach (Match tag in TagPattern().Matches(html))
        {
            var name = tag.Groups[1].Value;
            Assert.True(Tags.Contains(name), $"<{name}> is not an expected tag in: {html}");
            // The rest of the tag must be attributes in the form name="value", and nothing else (a quote that escaped would leave a bare word).
            var rest = tag.Groups[2].Value.TrimEnd('/', ' ');
            var consumed = AttributePattern().Replace(rest, "");
            Assert.True(consumed.Trim().Length == 0, $"<{name}{tag.Groups[2].Value}> has something that is not an attribute: {consumed}");
            foreach (Match attribute in AttributePattern().Matches(rest))
            {
                var key = attribute.Groups[1].Value.ToLowerInvariant();
                var value = attribute.Groups[2].Value;
                Assert.False(key.StartsWith("on", StringComparison.Ordinal), $"event handler {key} in: {html}");
                switch (key)
                {
                    case "href":
                        Assert.Matches(@"^(https?://|mailto:|releases/|\./|\?|#)", value);
                        Assert.DoesNotContain("javascript", value, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "src":
                        Assert.StartsWith("https://", value);
                        break;
                    case "style":
                        Assert.Matches(@"^text-align:\s*(left|right|center);?$", value);
                        break;
                    case "target":
                    case "srcset":
                    case "srcdoc":
                    case "formaction":
                        Assert.Fail($"{key} must never be written: {html}");
                        break;
                }
            }
        }
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Raw HTML

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<IMG SRC=x ONERROR=alert(1)>")]
    [InlineData("<svg onload=alert(1)>")]
    [InlineData("<iframe src=\"javascript:alert(1)\"></iframe>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<details open ontoggle=alert(1)>")]
    [InlineData("<body onload=alert(1)>")]
    [InlineData("<style>*{background:url(javascript:alert(1))}</style>")]
    [InlineData("<object data=\"data:text/html,<script>alert(1)</script>\"></object>")]
    [InlineData("<form action=\"https://evil.example\"><button formaction=\"javascript:alert(1)\">x</button></form>")]
    [InlineData("<meta http-equiv=\"refresh\" content=\"0;url=https://evil.example\">")]
    [InlineData("<base href=\"https://evil.example/\">")]
    [InlineData("<link rel=\"stylesheet\" href=\"https://evil.example/x.css\">")]
    [InlineData("<math><mtext><table><mglyph><style><!--</style><img title=\"--&gt;&lt;img src=x onerror=alert(1)&gt;\">")]
    [InlineData("<!--[if IE]><script>alert(1)</script><![endif]-->")]
    [InlineData("<![CDATA[<script>alert(1)</script>]]>")]
    public void Raw_html_is_text_not_markup(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        // Never an element of the page: where the vector wrote a tag, the output has the escaped characters.
        Assert.DoesNotMatch(@"<(script|iframe|svg|style|object|form|meta|base|link|body|details|math)\b", html);
    }

    [Fact]
    public void Raw_html_in_the_middle_of_a_sentence_and_in_a_block_is_escaped()
    {
        var html = Html("Before <b onmouseover=alert(1)>bold</b> after\n\n<div onclick=\"alert(1)\">\nblock\n</div>");

        AssertSafe(html);
        Assert.Contains("&lt;b onmouseover=alert(1)&gt;", html);
        Assert.Contains("&lt;div onclick=", html);
    }

    // ---- Links and images

    [Theory]
    [InlineData("[x](javascript:alert(1))")]
    [InlineData("[x](JaVaScRiPt:alert(1))")]
    [InlineData("[x]( javascript:alert(1))")]
    [InlineData("[x](java\tscript:alert(1))")]
    [InlineData("[x](java\nscript:alert(1))")]
    [InlineData("[x](&#106;avascript:alert(1))")]
    [InlineData("[x](&#x6A;avascript:alert(1))")]
    [InlineData("[x](&#0000106;avascript:alert(1))")]
    [InlineData("[x](&Tab;javascript:alert(1))")]
    [InlineData("[x](javascript&colon;alert(1))")]
    [InlineData("[x](javascript%3Aalert(1))")]
    [InlineData("[x](<javascript:alert(1)>)")]
    [InlineData("[x](vbscript:msgbox(1))")]
    [InlineData("[x](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)")]
    [InlineData("[x](file:///etc/passwd)")]
    [InlineData("[x](blob:https://example.com/uuid)")]
    [InlineData("[x](ftp://example.com/file)")]
    [InlineData("[x](tel:+123456)")]
    [InlineData("[x](ms-msdt:/id)")]
    [InlineData("[x][ref]\n\n[ref]: javascript:alert(1)")]
    [InlineData("[x]\n\n[x]: JAVASCRIPT:alert(1)")]
    public void A_link_with_a_scheme_that_is_not_http_https_or_mailto_loses_its_link_and_keeps_its_text(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.DoesNotContain("<a", html);
        Assert.DoesNotContain("href", html);
        Assert.Contains("x", html);
    }

    [Theory]
    [InlineData("<javascript:alert(1)>")]
    [InlineData("<JAVASCRIPT:alert(1)>")]
    [InlineData("<data:text/html;base64,PHNjcmlwdD4=>")]
    [InlineData("<vbscript:x>")]
    [InlineData("<file:///etc/passwd>")]
    [InlineData("<ftp://example.com>")]
    public void An_autolink_with_another_scheme_is_text(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.DoesNotContain("<a", html);
    }

    [Theory]
    [InlineData("[x](/pricing)")]
    [InlineData("[x](../../admin)")]
    [InlineData("[x](guide.md)")]
    [InlineData("[x](//evil.example/x)")]
    [InlineData("[x](\\\\evil.example\\share)")]
    [InlineData("[x](?page=2)")]
    [InlineData("[x]()")]
    public void A_relative_link_would_point_into_this_site_so_it_loses_its_link(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.DoesNotContain("<a", html);
    }

    [Theory]
    [InlineData("![x](http://example.com/a.png)")]
    [InlineData("![x](//example.com/a.png)")]
    [InlineData("![x](/a.png)")]
    [InlineData("![x](a.png)")]
    [InlineData("![x](data:image/svg+xml;base64,PHN2ZyBvbmxvYWQ9YWxlcnQoMSk+)")]
    [InlineData("![x](javascript:alert(1))")]
    [InlineData("![x](mailto:me@example.com)")]
    [InlineData("![x][r]\n\n[r]: http://example.com/a.png")]
    public void An_image_must_be_https_every_other_image_is_its_alt_text(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("x", html);
    }

    [Fact]
    public void An_https_image_is_kept_lazy_with_no_referrer()
    {
        var html = Html("![build](https://img.shields.io/badge/build-passing-green.svg)");

        AssertSafe(html);
        Assert.Contains("<img src=\"https://img.shields.io/badge/build-passing-green.svg\"", html);
        Assert.Contains("alt=\"build\"", html);
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("decoding=\"async\"", html);
        Assert.Contains("referrerpolicy=\"no-referrer\"", html);
    }

    [Fact]
    public void Github_user_attachments_images_are_kept()
    {
        var html = Html("![shot](https://github.com/user-attachments/assets/0b1c2d3e-0000-4000-8000-123456789abc)\n\n![old](https://user-images.githubusercontent.com/1/2.png)");

        AssertSafe(html);
        Assert.Equal(2, Regex.Matches(html, "<img ").Count);
    }

    [Fact]
    public void An_image_inside_a_link_keeps_both_when_both_are_https()
    {
        var html = Html("[![badge](https://example.com/b.svg)](https://example.com/page)");

        AssertSafe(html);
        Assert.Contains("<a href=\"https://example.com/page\"", html);
        Assert.Contains("<img src=\"https://example.com/b.svg\"", html);
    }

    [Theory]
    [InlineData("[x](https://example.com/a)", "https://example.com/a")]
    [InlineData("[x](http://example.com/a)", "http://example.com/a")]
    [InlineData("[x](mailto:me@example.com)", "mailto:me@example.com")]
    [InlineData("<https://example.com/auto>", "https://example.com/auto")]
    [InlineData("https://example.com/bare and www.example.com/bare2", "https://example.com/bare")]
    public void A_remote_link_keeps_its_address_and_leaves_with_noopener_noreferrer_nofollow_and_no_target(string markdown, string href)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.Contains($"href=\"{href}\"", html);
        Assert.Contains("rel=\"noopener noreferrer nofollow\"", html);
        Assert.DoesNotContain("target=", html);
    }

    [Fact]
    public void Every_link_of_a_body_has_the_rel()
    {
        var html = Html("[a](https://a.example) [b](http://b.example) <https://c.example> https://d.example @user #12 [e](mailto:e@example.com)");

        var links = Regex.Matches(html, "<a [^>]*>");
        Assert.Equal(7, links.Count);
        Assert.All(links, link => Assert.Contains("rel=\"noopener noreferrer nofollow\"", link.Value));
    }

    [Fact]
    public void A_title_cannot_break_out_of_its_attribute()
    {
        var html = Html("[x](https://example.com \"\\\" onmouseover=\\\"alert(1)\")\n\n[y](https://example.com 'a\" autofocus onfocus=\"alert(1)')");

        AssertSafe(html);
        Assert.DoesNotMatch(@"\sonmouseover=""", html);
        Assert.DoesNotMatch(@"\sonfocus=""", html);
    }

    [Fact]
    public void An_address_with_quotes_and_angle_brackets_stays_in_its_attribute()
    {
        var html = Html("[x](https://example.com/a\"onmouseover=\"alert(1)) and [y](<https://example.com/b\"><img src=x onerror=alert(1)>)");

        AssertSafe(html);
        Assert.DoesNotMatch(@"\sonmouseover=""", html);
        Assert.DoesNotContain("<img src=x", html);
    }

    // ---- Entities and other tricks

    [Theory]
    [InlineData("&lt;script&gt;alert(1)&lt;/script&gt;")]
    [InlineData("&#60;script&#62;alert(1)&#60;/script&#62;")]
    [InlineData("&#x3C;script&#x3E;alert(1)&#x3C;/script&#x3E;")]
    [InlineData("&lt;img src=x onerror=alert(1)&gt;")]
    [InlineData("&amp;lt;script&amp;gt;")]
    [InlineData("\\<script>alert(1)\\</script>")]
    [InlineData("`<script>alert(1)</script>`")]
    [InlineData("```html\n<script>alert(1)</script>\n```")]
    [InlineData("    <script>alert(1)</script>")]
    public void Entities_and_escapes_never_turn_back_into_markup(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_null_byte_and_other_control_characters_do_not_end_up_in_a_tag()
    {
        var html = Html("<scr\0ipt>alert(1)</scr\0ipt>\n\n[x](java\0script:alert(1))\n\n\u0001<img src=x onerror=alert(1)>");

        AssertSafe(html);
        Assert.DoesNotContain("<a", html);
    }

    [Fact]
    public void Attributes_written_in_markdown_are_not_supported()
    {
        var html = Html("[x](https://example.com){onclick=alert(1)}\n\n# Heading {#custom-id .cls onmouseover=alert(1)}\n\n![i](https://example.com/i.png){onerror=alert(1)}");

        AssertSafe(html);
        Assert.DoesNotContain("onclick", Regex.Replace(html, @"\{[^}]*\}", ""));
        Assert.DoesNotContain("id=\"custom-id\"", html);
    }

    [Fact]
    public void Math_media_and_embeds_are_not_extensions_of_the_renderer()
    {
        var html = Html("$$<script>alert(1)</script>$$\n\n![video](https://www.youtube.com/watch?v=abc)\n\n![audio](https://example.com/a.mp3)");

        AssertSafe(html);
        Assert.DoesNotContain("<iframe", html);
        Assert.DoesNotContain("<video", html);
        Assert.DoesNotContain("<audio", html);
    }

    // ---- @mentions and #123

    [Fact]
    public void A_mention_and_an_issue_number_become_links_to_github()
    {
        var html = Html("Thanks @mira-okafor, see #412 and (#7).");

        AssertSafe(html);
        Assert.Contains("<a href=\"https://github.com/mira-okafor\" rel=\"noopener noreferrer nofollow\">@mira-okafor</a>", html);
        Assert.Contains("<a href=\"https://github.com/octo/tern/issues/412\" rel=\"noopener noreferrer nofollow\">#412</a>", html);
        Assert.Contains("<a href=\"https://github.com/octo/tern/issues/7\"", html);
    }

    [Theory]
    [InlineData("a@example.com")]
    [InlineData("me@@user")]
    [InlineData("@-leading")]
    [InlineData("@trailing-")]
    [InlineData("@under_score")]
    [InlineData("C#12 and F#")]
    [InlineData("see octo/tern#5")]
    [InlineData("#1234567890")]
    [InlineData("#12abc")]
    [InlineData("##5")]
    [InlineData("&#35;12")]
    [InlineData("https://example.com/@user and https://example.com/#12")]
    public void Text_that_only_looks_like_a_reference_is_not_linked(string markdown)
    {
        var html = Html(markdown);

        AssertSafe(html);
        Assert.DoesNotContain("https://github.com/", html);
    }

    [Fact]
    public void A_mention_that_is_too_long_is_not_linked()
    {
        var html = Html("@" + new string('a', 40) + " @" + new string('b', 39));

        AssertSafe(html);
        Assert.Single(Regex.Matches(html, "<a "));
    }

    [Fact]
    public void A_reference_is_never_linked_in_code_in_a_link_or_in_a_code_block()
    {
        var html = Html("`@user #1`\n\n[@user #2](https://example.com)\n\n```\n@user #3\n```\n\n<https://example.com/@user#4>");

        AssertSafe(html);
        Assert.DoesNotContain("https://github.com/", html);
        Assert.DoesNotContain("issues/", html);
    }

    [Fact]
    public void Markup_around_a_reference_cannot_change_where_it_leads()
    {
        var html = Html("@user\"><script>alert(1)</script> and @user</a><a href=\"javascript:alert(1)\">x</a> and #1\"><img src=x>");

        AssertSafe(html);
        Assert.Equal(["https://github.com/user", "https://github.com/user", "https://github.com/octo/tern/issues/1"],
            Regex.Matches(html, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value));
    }

    [Fact]
    public void Without_a_repository_references_stay_text()
    {
        var html = new ReleaseBody(null).Render(ChangelogSamples.Entry("v1", "Thanks @user, fixes #5"), "releases/v1", 3);

        Assert.DoesNotContain("<a", html);
        Assert.Contains("@user", html);
    }

    // ---- Ids, headings, footnotes

    [Fact]
    public void Ids_start_with_the_slug_of_the_release_so_a_heading_cannot_take_an_id_of_the_page()
    {
        var html = Html("# main\n\n## search\n\n### Release notes", "v1.2");

        AssertSafe(html);
        Assert.DoesNotMatch("id=\"(main|search|release-notes)\"", html);
        Assert.Contains("id=\"rel-v1.2-main\"", html);
        Assert.Contains("id=\"rel-v1.2-search\"", html);
    }

    [Fact]
    public void Two_releases_on_one_page_never_share_an_id()
    {
        var one = Body.Render(ChangelogSamples.Entry("v1", "## What's changed\n\nText[^1]\n\n[^1]: note", "v1"), "./", 3);
        var two = Body.Render(ChangelogSamples.Entry("v2", "## What's changed\n\nText[^1]\n\n[^1]: note", "v2"), "./", 3);

        var ids = Regex.Matches(one + two, "id=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.Matches("^rel-v[12]-", id));
    }

    [Fact]
    public void A_fragment_link_leads_to_the_prefixed_id_on_the_page_of_the_release()
    {
        var html = Html("[go](#whats-changed)\n\n## What's changed\n\nText[^1]\n\n[^1]: note", "v1", "releases/v1");

        AssertSafe(html);
        Assert.Contains("href=\"releases/v1#rel-v1-whats-changed\"", html);
        Assert.Contains("id=\"rel-v1-whats-changed\"", html);
        Assert.Contains("href=\"releases/v1#rel-v1-fn:1\"", html);
        Assert.Contains("id=\"rel-v1-fn:1\"", html);
    }

    [Fact]
    public void A_fragment_link_in_a_feed_is_absolute()
    {
        var html = Body.Render(ChangelogSamples.Entry("v1", "[go](#top)\n\n## Top"), "https://example.com/releases/v1", 3);

        Assert.Contains("href=\"https://example.com/releases/v1#rel-v1-top\"", html);
    }

    [Fact]
    public void Headings_are_never_higher_than_the_level_the_page_allows()
    {
        var html = Html("# One\n\n## Two\n\n### Three\n\n#### Four\n\n##### Five\n\n###### Six", minHeading: 3);

        Assert.Equal(["h3", "h3", "h3", "h4", "h5", "h6"], Regex.Matches(html, "<(h[1-6]) ").Select(m => m.Groups[1].Value));
        Assert.Equal(["h2", "h2", "h3", "h4", "h5", "h6"], Regex.Matches(Html("# One\n\n## Two\n\n### Three\n\n#### Four\n\n##### Five\n\n###### Six", minHeading: 2), "<(h[1-6]) ").Select(m => m.Groups[1].Value));
    }

    [Fact]
    public void An_empty_or_blank_body_is_an_empty_string()
    {
        Assert.Equal("", Html(""));
        Assert.Equal("", Html("   \n\n  "));
    }

    // ---- What a release note is for

    [Fact]
    public void A_typical_generated_body_renders_with_code_tables_lists_and_callouts()
    {
        var html = Html("""
            ## What's Changed
            * Fix the thing by @octocat in #12
            * Add the other thing by @hubot in #13

            ```bash
            tern sync --resume
            ```

            | a | b |
            |:-:|---|
            | 1 | 2 |

            > [!WARNING]
            > Back up first.

            - [x] done
            - [ ] open

            **Full comparison**: https://github.com/octo/tern/compare/v0...v1
            """);

        AssertSafe(html);
        Assert.Contains("<h3 id=\"rel-v1-whats-changed\">", html);
        Assert.Contains("class=\"code-block\" data-language=\"bash\"", html);
        Assert.Contains("class=\"table-scroll\"", html);
        Assert.Contains("data-callout=\"warning\"", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("href=\"https://github.com/octocat\"", html);
    }

    [Fact]
    public void The_result_is_cached_with_the_release_and_not_shared_between_variants()
    {
        var release = ChangelogSamples.Entry("v1", "# Title");

        var a = Body.Render(release, "releases/v1", 3);
        var again = Body.Render(release, "releases/v1", 3);
        var other = Body.Render(release, "releases/v1", 2);

        Assert.Same(a, again);
        Assert.Contains("<h3", a);
        Assert.Contains("<h2", other);
    }

    [Fact]
    public void Addresses_that_vary_per_request_cannot_make_the_cache_grow_without_limit()
    {
        var release = ChangelogSamples.Entry("v1", "[go](#top)\n\n## Top");

        var first = Enumerable.Range(0, 30).Select(i => Body.Render(release, "https://h" + i + ".example/releases/v1", 3)).ToList();
        var again = Enumerable.Range(0, 30).Select(i => Body.Render(release, "https://h" + i + ".example/releases/v1", 3)).ToList();

        // The first few are kept, the rest are rendered again each time, and every one is still correct.
        Assert.True(first.Zip(again).Count(pair => ReferenceEquals(pair.First, pair.Second)) <= 8);
        Assert.Equal(first, again);
        Assert.Contains("href=\"https://h29.example/releases/v1#rel-v1-top\"", again[29]);
    }

    [Theory]
    [InlineData("csharp", "/*A", 19_990)]
    [InlineData("html", "<!--a ", 9_990)]
    [InlineData("razor", "@*a ", 14_990)]
    [InlineData("bash", "'a ", 19_990)]
    [InlineData("yaml", "\"a\\", 19_990)]
    public void A_code_block_made_to_be_slow_to_highlight_does_not_cost_seconds(string language, string unit, int repeat)
    {
        var code = string.Concat(Enumerable.Repeat(unit, repeat));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var html = Html("```" + language + "\n" + code + "\n```");

        AssertSafe(html);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"{language} took {watch.Elapsed}");
        Assert.Contains("code-block", html);
    }

    [Fact]
    public void Hostile_input_of_a_large_size_renders_in_bounded_time()
    {
        var hostile = string.Concat(Enumerable.Repeat("[", 20_000)) + string.Concat(Enumerable.Repeat("*", 20_000)) + string.Concat(Enumerable.Repeat("> ", 500)) + "x\n"
            + string.Concat(Enumerable.Repeat("- ", 400)) + "y\n" + string.Concat(Enumerable.Repeat("@a #1 ", 5_000));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var html = Html(hostile);

        AssertSafe(html);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
    }
}
