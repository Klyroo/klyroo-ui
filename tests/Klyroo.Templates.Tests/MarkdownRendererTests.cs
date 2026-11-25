using Klyroo.Templates.Content;

namespace Klyroo.Templates.Tests;

public class MarkdownRendererTests
{
    private static readonly MarkdownRenderer Renderer = new();

    private static string Html(string markdown, string directory = "guides", string page = "guides/setup") => Renderer.Render(markdown, directory, page).Html;

    // ---- Sanitization

    [Theory]
    [InlineData("[go](javascript:alert(1))")]
    [InlineData("[go](JavaScript:alert(1))")]
    [InlineData("[go](java\tscript:alert(1))")]
    [InlineData("[go](&#106;avascript:alert(1))")]
    [InlineData("[go](<javascript:alert(1)>)")]
    [InlineData("[go](vbscript:msgbox)")]
    [InlineData("[go](data:text/html;base64,PHNjcmlwdD4=)")]
    [InlineData("[go](file:///etc/passwd)")]
    public void A_link_with_a_script_scheme_loses_its_link_and_keeps_its_text(string markdown)
    {
        var html = Html(markdown);

        Assert.DoesNotContain("<a", html);
        Assert.DoesNotContain("href", html);
        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("go", html);
    }

    [Fact]
    public void An_autolink_with_a_script_scheme_is_text_not_a_link()
    {
        var html = Html("<javascript:alert(1)>");

        Assert.DoesNotContain("<a", html);
    }

    [Fact]
    public void An_image_with_a_script_scheme_loses_its_source_and_keeps_its_alt_text()
    {
        var html = Html("![a chart](javascript:alert(1)) and ![data](data:image/svg+xml;base64,PHN2Zz4=)");

        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("src", html);
        Assert.Contains("a chart", html);
    }

    [Theory]
    [InlineData("[x](http://example.com/a)", "href=\"http://example.com/a\"")]
    [InlineData("[x](https://example.com/a?b=1#c)", "href=\"https://example.com/a?b=1#c\"")]
    [InlineData("[x](mailto:me@example.com)", "href=\"mailto:me@example.com\"")]
    [InlineData("![x](https://example.com/a.png)", "src=\"https://example.com/a.png\"")]
    public void Http_https_and_mailto_addresses_are_kept(string markdown, string expected) => Assert.Contains(expected, Html(markdown));

    [Fact]
    public void An_external_link_gets_rel_noopener()
    {
        Assert.Contains("rel=\"noopener noreferrer\"", Html("[x](https://example.com)"));
        Assert.DoesNotContain("rel=", Html("[x](setup)"));
    }

    [Fact]
    public void Raw_html_is_shown_as_text()
    {
        var html = Html("Before <script>alert(1)</script> after\n\n<div onclick=\"x()\">block</div>\n\n<img src=x onerror=alert(1)>");

        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<div onclick", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;div onclick=", html);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html);
    }

    [Fact]
    public void Raw_html_goes_through_when_the_owner_allows_it()
    {
        var html = new MarkdownRenderer(allowRawHtml: true).Render("<kbd>Ctrl</kbd> and [x](javascript:alert(1))", "", "p").Html;

        Assert.Contains("<kbd>Ctrl</kbd>", html);
        // Links written in Markdown keep following the rules.
        Assert.DoesNotContain("href", html);
    }

    [Fact]
    public void Attributes_in_braces_are_not_applied()
    {
        var html = Html("[x](setup){onmouseover=\"alert(1)\"}\n\n# Title {#custom onclick=\"x()\"}");

        Assert.DoesNotContain("onmouseover=", html.Replace("onmouseover=&quot;", ""));
        Assert.DoesNotContain("<h1 id=\"custom\"", html);
        Assert.DoesNotContain("onclick=\"", html);
    }

    [Fact]
    public void Quotes_in_text_cannot_break_out_of_an_attribute()
    {
        var html = Html("[a\" onmouseover=\"x](setup \"t\" onfocus=\"y\")\n\n![\"><svg onload=alert(1)>](a.png)");

        Assert.DoesNotContain("<svg", html);
        Assert.DoesNotMatch("<[a-z]+ [^>]*onmouseover=\"", html);
    }

    // ---- Links

    [Theory]
    [InlineData("[x](/guides/setup)", "guides", "href=\"guides/setup\"")]
    [InlineData("[x](setup)", "guides", "href=\"guides/setup\"")]
    [InlineData("[x](./setup.md)", "guides", "href=\"guides/setup\"")]
    [InlineData("[x](../start/install.md#linux)", "guides", "href=\"start/install#linux\"")]
    [InlineData("[x](setup?tab=a)", "guides/deep", "href=\"guides/deep/setup?tab=a\"")]
    [InlineData("[x](/)", "guides", "href=\"./\"")]
    [InlineData("[x](../../../up)", "guides", "href=\"up\"")]
    [InlineData("[x](/img/logo.png)", "guides", "href=\"img/logo.png\"")]
    [InlineData("![x](/img/logo.png)", "guides", "src=\"img/logo.png\"")]
    public void Relative_links_are_made_relative_to_the_base_href(string markdown, string directory, string expected) =>
        Assert.Contains(expected, Html(markdown, directory));

    [Theory]
    [InlineData("[x](/Set%20Up.md)", "href=\"set-up\"")]
    [InlineData("[x](/Guides/Caf%C3%A9.md#top)", "href=\"guides/cafe#top\"")]
    [InlineData("[x](setup%2Dguide)", "href=\"guides/setup-guide\"")]
    [InlineData("[x](/bad%zz)", "href=\"bad-zz\"")]
    public void Percent_encoded_names_are_decoded_before_they_become_an_address(string markdown, string expected) => Assert.Contains(expected, Html(markdown));

    [Fact]
    public void An_index_file_is_the_address_of_its_folder()
    {
        Assert.Contains("href=\"guides\"", Html("[x](/guides/index.md)"));
        Assert.Contains("href=\"guides\"", Html("[x](index.md)", "guides"));
    }

    [Fact]
    public void A_fragment_link_stays_on_the_page_below_a_base_href()
    {
        var html = Html("[down](#usage)\n\n## Usage\n\nText[^1]\n\n[^1]: A note.", page: "guides/setup");

        Assert.Contains("href=\"guides/setup#usage\"", html);
        // The footnote links are written by Markdig as "#fn:1" and "#fnref:1".
        Assert.Contains("href=\"guides/setup#fn:1\"", html);
        Assert.Contains("href=\"guides/setup#fnref:1\"", html);
        Assert.DoesNotContain("href=\"#", html);
    }

    [Fact]
    public void The_pages_a_page_links_to_are_listed_for_the_broken_link_check()
    {
        var rendered = Renderer.Render("[a](/one) [b](two.md#x) [c](https://example.com) [d](#here) ![e](/img/x.png) [f](/)", "dir", "dir/p");

        Assert.Equal(["one", "dir/two", ""], rendered.Links);
    }

    // ---- Headings, table of contents

    [Fact]
    public void Headings_get_github_style_ids_and_a_repeat_gets_a_number()
    {
        var rendered = Renderer.Render("## Install the CLI\n\n### Linux & macOS\n\n## Install the CLI\n\n## `tern sync` options", "", "p");

        Assert.Equal(
            [("install-the-cli", "Install the CLI", 2), ("linux--macos", "Linux & macOS", 3), ("install-the-cli-1", "Install the CLI", 2), ("tern-sync-options", "tern sync options", 2)],
            rendered.Headings.Select(h => (h.Id, h.Text, h.Level)));
        Assert.Contains("<h2 id=\"install-the-cli\">Install the CLI</h2>", rendered.Html);
        Assert.Contains("<h3 id=\"linux--macos\">", rendered.Html);
    }

    [Fact]
    public void A_heading_cannot_take_the_id_of_the_main_landmark()
    {
        var rendered = Renderer.Render("## Main", "", "p");

        Assert.Equal("main-section", Assert.Single(rendered.Headings).Id);
        Assert.Contains("id=\"main-section\"", rendered.Html);
    }

    [Fact]
    public void A_renamed_reserved_id_never_takes_the_id_of_another_heading()
    {
        var rendered = Renderer.Render("## Main\n\n## Main section\n\n## Main\n\n## Search\n\n## Search section", "", "p");

        var ids = rendered.Headings.Select(h => h.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.DoesNotContain("main", ids);
        Assert.DoesNotContain("search", ids);
        Assert.Equal("main-section-1", ids[0]);
        Assert.Equal("main-section", ids[1]);
        Assert.All(ids, id => Assert.Contains($"id=\"{id}\"", rendered.Html));
    }

    [Fact]
    public void Every_toc_id_is_the_id_of_a_heading_in_the_html()
    {
        var rendered = Renderer.Render("## One\n\n### Two\n\n#### Three\n\n## Four", "", "p");
        var page = new MarkdownPage("p", "", false, "P", null, null, null, null, false, rendered.Html, rendered.Headings, rendered.Text, "p.md");

        // The table of contents lists h2 and h3, like the search.
        Assert.Equal(["one", "two", "four"], page.Toc.Select(t => t.Id));
        Assert.Equal([2, 3, 2], page.Toc.Select(t => t.Level));
        Assert.All(page.Toc, link => Assert.Contains($"id=\"{link.Id}\"", rendered.Html));
    }

    // ---- Markdown features

    [Fact]
    public void Tables_task_lists_and_footnotes_render()
    {
        var html = Html("| a | b |\n|---|---|\n| 1 | 2 |\n\n- [x] done\n- [ ] todo\n\nNote[^n]\n\n[^n]: The note.");

        Assert.Contains("<table>", html);
        Assert.Contains("class=\"table-scroll\" tabindex=\"0\"", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("footnotes", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void A_task_list_box_is_named_by_the_text_of_its_item()
    {
        var html = Html("- [x] Install **Tern**\n- [ ] Set the `key`");

        Assert.Contains("<input aria-label=\"Install Tern\" disabled=\"disabled\" type=\"checkbox\" checked=\"checked\" />", html);
        Assert.Contains("<input aria-label=\"Set the key\" disabled=\"disabled\" type=\"checkbox\" />", html);
    }

    [Fact]
    public void Nested_task_items_are_each_named_by_their_own_text()
    {
        var html = Html("- [x] Parent item\n  - [ ] Child one\n  - [x] Child **two**\n    - [ ] Grandchild\n- [ ] Second parent");

        foreach (var label in new[] { "Parent item", "Child one", "Child two", "Grandchild", "Second parent" })
        {
            Assert.Contains($"<input aria-label=\"{label}\" disabled=\"disabled\" type=\"checkbox\"", html);
        }
        Assert.DoesNotContain("Parent item Child", html);
    }

    [Fact]
    public void Local_images_are_listed_and_external_ones_are_not()
    {
        var rendered = Renderer.Render("![a](chart.png) ![b](/img/b.png?v=2) ![c](https://example.com/c.png) ![d](#x) ![e](../up/e.svg)", "guides", "guides/p");

        Assert.Equal(["guides/chart.png", "img/b.png", "up/e.svg"], rendered.Images);
    }

    [Fact]
    public void Three_hyphens_stay_three_hyphens()
    {
        Assert.Contains("a --- b", Html("a --- b"));
    }

    [Fact]
    public void A_fenced_code_block_is_highlighted_and_has_a_copy_button()
    {
        var html = Html("```bash\ntern sync --dry-run # try it\n```");

        Assert.Contains("<div class=\"code-block\" data-language=\"bash\">", html);
        Assert.Contains("<button type=\"button\" class=\"code-copy\" data-code-copy>Copy</button>", html);
        Assert.Contains("<pre tabindex=\"0\"><code>", html);
        Assert.Contains("<span class=\"tok-command\">tern</span>", html);
        Assert.Contains("<span class=\"tok-flag\">--dry-run</span>", html);
        Assert.Contains("<span class=\"tok-comment\"># try it</span>", html);
    }

    [Fact]
    public void Code_is_encoded_even_in_a_language_without_patterns()
    {
        var html = Html("```whatever\n<script>alert(1)</script> & \"x\"\n```");

        Assert.DoesNotContain("<script", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; &amp; &quot;x&quot;", html);
        Assert.Contains("data-language=\"text\"", html);
    }

    [Fact]
    public void A_language_name_cannot_break_out_of_its_attribute()
    {
        var html = Html("```\"><script>x</script>\ncode\n```");

        Assert.DoesNotContain("<script", html);
    }

    // ---- Callouts

    [Theory]
    [InlineData("::: tip\nText.\n:::", "tip", "Tip", "success")]
    [InlineData("::: warning Before you start\nText.\n:::", "warning", "Before you start", "warning")]
    [InlineData("> [!NOTE]\n> Text.", "note", "Note", "info")]
    [InlineData("> [!WARNING]\n> Text.", "warning", "Warning", "warning")]
    [InlineData("> [!CAUTION]\n> Text.", "caution", "Caution", "error")]
    [InlineData("> [!IMPORTANT]\n> Text.", "important", "Important", "primary")]
    [InlineData("> [!TIP]\n> Text.", "tip", "Tip", "success")]
    public void Callouts_come_from_containers_and_github_alerts(string markdown, string kind, string title, string color)
    {
        var html = Html(markdown);

        Assert.Contains($"<div class=\"callout\" role=\"note\" data-callout=\"{kind}\" data-color=\"{color}\">", html);
        Assert.Contains($"<p class=\"callout-title\">{title}</p>", html);
        Assert.Contains("Text.", html);
        Assert.DoesNotContain("[!", html);
    }

    [Fact]
    public void A_callout_title_is_encoded_and_an_unknown_container_is_a_plain_box()
    {
        var html = Html("::: tip <b>x</b>\nText.\n:::\n\n::: whatever\nOther.\n:::");

        Assert.DoesNotContain("<b>", html);
        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", html);
        Assert.Matches("<div>\\s*<p>Other\\.</p>", html);
    }

    // ---- Text for the search

    [Fact]
    public void The_text_has_no_markup_and_includes_code()
    {
        var rendered = Renderer.Render("## Title\n\nSome **bold** text with `code` and a [link](/x).\n\n```bash\ntern sync\n```", "", "p");

        Assert.Equal("Title Some bold text with code and a link. tern sync", rendered.Text);
    }

    [Fact]
    public void Images_of_untrusted_markdown_are_kept_when_https_and_dropped_when_not_allowed()
    {
        const string markdown = "![alt text](https://example.test/a.png)";
        var kept = Renderer.Render(markdown, "", "p", new UntrustedMarkdown("x-"));
        var dropped = Renderer.Render(markdown, "", "p", new UntrustedMarkdown("x-", AllowImages: false));

        Assert.Contains("<img", kept.Html);
        Assert.DoesNotContain("<img", dropped.Html);
        Assert.Contains("alt text", dropped.Html);
    }
}
