using Editor.Shared.Data;
using Editor.Shared.Services;

namespace Klyroo.Templates.Tests;

/// <summary>
/// The code of the Editor template that is plain C# (no Razor): the allow-list that cleans the HTML of a document, the text and word count taken from it, the documents
/// and what the browser storage may hand back, and the state that keeps them within bounds. The pages are tested in a browser (Klyroo.UI.E2E).
/// </summary>
public class EditorTemplateTests
{
    // ---------------------------------------------------------------- the allow-list

    [Fact]
    public void What_the_editor_writes_comes_through_unchanged()
    {
        const string html =
            "<h1>Title</h1><p>Some <strong>bold</strong>, <em>italic</em>, <u>underlined</u> and <s>struck</s> text with <code>code</code> and " +
            "<a href=\"https://example.com/a?b=1&amp;c=2\" rel=\"noopener noreferrer nofollow\">a link</a>.</p>" +
            "<ul><li><p>one</p></li><li><p>two</p></li></ul><ol start=\"3\"><li><p>three</p></li></ol>" +
            "<blockquote><p>quoted</p></blockquote><pre><code class=\"language-csharp\">var x = 1 &lt; 2;</code></pre><hr><p>a<br>b</p>" +
            "<img src=\"https://example.com/p.png\" alt=\"a picture\" loading=\"lazy\" referrerpolicy=\"no-referrer\">" +
            "<ul data-type=\"taskList\"><li data-type=\"taskItem\" data-checked=\"true\"><label><input type=\"checkbox\" checked=\"checked\" disabled=\"disabled\"><span></span></label><div><p>done</p></div></li></ul>";

        Assert.Equal(html, DocumentHtml.Clean(html));
    }

    [Theory]
    [InlineData("<script>window.x = 1</script><p>after</p>", "<p>after</p>")]
    [InlineData("<p>a</p><style>p { color: red }</style><p>b</p>", "<p>a</p><p>b</p>")]
    [InlineData("<p onclick=\"x()\" style=\"color:red\" class=\"c\" id=\"i\">text</p>", "<p>text</p>")]
    [InlineData("<iframe src=\"https://evil.example\"></iframe><p>x</p>", "<p>x</p>")]
    [InlineData("<svg onload=\"x()\"><script>y()</script></svg><p>x</p>", "<p>x</p>")]
    [InlineData("<object data=\"x\"></object><embed src=\"x\"><p>x</p>", "<p>x</p>")]
    [InlineData("<form action=\"/x\"><button>go</button></form>", "go")]
    [InlineData("<font color=\"red\">red</font>", "red")]
    [InlineData("<!-- <script>x</script> --><p>x</p>", "<p>x</p>")]
    [InlineData("<!DOCTYPE html><p>x</p>", "<p>x</p>")]
    [InlineData("<SCRIPT>x()</SCRIPT><P>up</P>", "<p>up</p>")]
    [InlineData("<scr<script>ipt>alert(1)</script>", "ipt&gt;alert(1)")]
    public void Elements_and_attributes_that_are_not_listed_are_dropped(string dirty, string expected) =>
        Assert.Equal(expected, DocumentHtml.Clean(dirty));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("java\nscript:alert(1)")]
    [InlineData(" javascript:alert(1)")]
    [InlineData("&#106;avascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("vbscript:x")]
    [InlineData("file:///etc/passwd")]
    [InlineData("tel:+123456")]
    [InlineData("ftp://example.com/file")]
    [InlineData("//example.com/x")]
    [InlineData("/relative")]
    [InlineData("example.com")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("mailto:")]
    [InlineData("")]
    public void A_link_that_is_not_http_https_or_mailto_loses_its_address_and_keeps_its_text(string address) =>
        Assert.Equal("<p>text</p>", DocumentHtml.Clean($"<p><a href=\"{address}\">text</a></p>"));

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("http://example.com/path?q=1#frag")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("HTTPS://EXAMPLE.COM/")]
    public void A_link_to_http_https_or_mailto_is_kept_with_a_rel_that_keeps_the_page_private(string address)
    {
        var clean = DocumentHtml.Clean($"<p><a href=\"{address}\" target=\"_blank\" onclick=\"x()\">text</a></p>");

        Assert.Equal($"<p><a href=\"{address}\" rel=\"noopener noreferrer nofollow\">text</a></p>", clean);
    }

    [Theory]
    [InlineData("http://example.com/p.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    [InlineData("/local.png")]
    [InlineData("//example.com/p.png")]
    [InlineData("ftp://example.com/p.png")]
    [InlineData("mailto:a@example.com")]
    [InlineData("https://user@example.com/p.png")]
    [InlineData("")]
    public void An_image_comes_from_an_https_address_or_is_dropped(string address) =>
        Assert.Equal("<p>x</p>", DocumentHtml.Clean($"<p>x</p><img src=\"{address}\" onerror=\"y()\">"));

    [Theory]
    [InlineData("https://exa mple.com/a b")]
    [InlineData("https://example.com/a b")]
    [InlineData("https://exa mple.com/")]
    [InlineData("java script:alert(1)")]
    [InlineData("https://example.com/a b")]
    [InlineData("https://example.com/a b")]
    [InlineData("https://example.com/a\u0001b")]
    [InlineData("mailto:someone@exa mple.com")]
    public void An_address_with_a_space_inside_is_not_an_address_and_is_never_closed_up_into_another_host(string address)
    {
        Assert.Equal("<p>text</p>", DocumentHtml.Clean($"<p><a href=\"{address}\">text</a></p>"));
        Assert.Null(DocumentHtml.Link(address));
        Assert.Null(DocumentHtml.Image(address));
    }

    [Fact]
    public void The_address_that_used_to_become_another_host_is_dropped_as_a_link_and_as_an_image()
    {
        Assert.Equal("<p>text</p>", DocumentHtml.Clean("<p><a href=\"https://exa mple.com/a b\">text</a></p>"));
        Assert.Equal("<p>x</p>", DocumentHtml.Clean("<p>x</p><img src=\"https://exa mple.com/a b.png\">"));
    }

    [Theory]
    [InlineData("  https://example.com/a  ", "https://example.com/a")]
    [InlineData("\u0001\u001f https://example.com/a \u0002", "https://example.com/a")]
    [InlineData("\thttps://example.com/a\n", "https://example.com/a")]
    [InlineData("https://example.com/a\tb", "https://example.com/ab")]
    [InlineData("https://example.com/a\r\nb", "https://example.com/ab")]
    [InlineData("htt\nps://example.com/", "https://example.com/")]
    public void Like_a_browser_the_ends_are_trimmed_and_tabs_and_line_breaks_are_dropped_anywhere(string address, string expected)
    {
        Assert.Equal(expected, DocumentHtml.Link(address));
        Assert.Equal(expected, DocumentHtml.Image(address));
        Assert.Equal($"<p><a href=\"{expected}\" rel=\"noopener noreferrer nofollow\">text</a></p>", DocumentHtml.Clean($"<p><a href=\"{address.Replace("\n", "&#10;").Replace("\r", "&#13;").Replace("\t", "&#9;")}\">text</a></p>"));
    }

    [Fact]
    public void A_tab_in_the_scheme_still_does_not_make_a_script_address()
    {
        Assert.Null(DocumentHtml.Link("java\tscript:alert(1)"));
        Assert.Null(DocumentHtml.Link("  java\nscript:alert(1)"));
        Assert.Null(DocumentHtml.Link("\u0001javascript:alert(1)"));
    }

    [Fact]
    public void An_image_keeps_its_address_and_alt_text_and_gets_lazy_loading_without_a_referrer()
    {
        var clean = DocumentHtml.Clean("<img src=\"https://example.com/a.png\" alt='say \"hi\"' width=\"9999\" onerror=\"x()\" style=\"x\">");

        Assert.Equal("<img src=\"https://example.com/a.png\" alt=\"say &quot;hi&quot;\" loading=\"lazy\" referrerpolicy=\"no-referrer\">", clean);
    }

    [Fact]
    public void A_task_checkbox_is_shown_checked_or_not_and_cannot_be_changed()
    {
        Assert.Equal("<input type=\"checkbox\" disabled=\"disabled\">", DocumentHtml.Clean("<input type=\"checkbox\">"));
        Assert.Equal("<input type=\"checkbox\" checked=\"checked\" disabled=\"disabled\">", DocumentHtml.Clean("<input type=\"checkbox\" checked>"));
        Assert.Equal("", DocumentHtml.Clean("<input type=\"text\" value=\"x\" autofocus onfocus=\"y()\">"));
    }

    [Fact]
    public void Text_is_written_again_escaped_and_entities_cannot_rebuild_a_tag()
    {
        Assert.Equal("<p>1 &lt; 2 &amp;&amp; 3 &gt; 2</p>", DocumentHtml.Clean("<p>1 < 2 && 3 > 2</p>"));
        Assert.Equal("<p>&lt;script&gt;x()&lt;/script&gt;</p>", DocumentHtml.Clean("<p>&lt;script&gt;x()&lt;/script&gt;</p>"));
        Assert.Equal("<p>&lt;b&gt;</p>", DocumentHtml.Clean("<p>&#60;b&#62;</p>"));
    }

    [Fact]
    public void Markup_that_is_broken_is_closed_and_never_leaves_a_dangling_attribute()
    {
        Assert.Equal("<p><strong>open</strong></p>", DocumentHtml.Clean("<p><strong>open"));
        Assert.Equal("<p>x</p>", DocumentHtml.Clean("<p>x</p></div></span>"));
        Assert.Equal("<p>a</p>", DocumentHtml.Clean("<p>a</p><img src=\"https://example.com/x.png"));
        Assert.Equal("<p>a</p>", DocumentHtml.Clean("<p>a</p><a href=\"https://example.com"));
        Assert.Equal("<p>a</p>&lt;", DocumentHtml.Clean("<p>a</p><"));
        Assert.Equal("<p>a</p>x&lt;", DocumentHtml.Clean("<p>a</p>x<"));
    }

    [Fact]
    public void Nesting_is_bounded()
    {
        var deep = string.Concat(Enumerable.Repeat("<blockquote>", 500)) + "text" + string.Concat(Enumerable.Repeat("</blockquote>", 500));

        var clean = DocumentHtml.Clean(deep);

        Assert.True(clean.Split("<blockquote>").Length - 1 <= 70);
        Assert.Contains("text", clean);
    }

    [Fact]
    public void A_code_class_is_kept_only_for_a_language()
    {
        Assert.Equal("<code class=\"language-c#\">x</code>", DocumentHtml.Clean("<code class=\"language-c#\">x</code>"));
        Assert.Equal("<code>x</code>", DocumentHtml.Clean("<code class=\"language-x onclick=y\">x</code>"));
        Assert.Equal("<code>x</code>", DocumentHtml.Clean("<code class=\"other\">x</code>"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_input_gives_an_empty_document(string? html) => Assert.Equal("", DocumentHtml.Clean(html));

    [Fact]
    public void Cleaning_twice_changes_nothing_more()
    {
        const string dirty = "<p onclick=x>a <b>b</b> <a href=\"javascript:1\">c</a></p><script>1</script><img src=\"http://e.test/x.png\"><ul><li>x";
        var once = DocumentHtml.Clean(dirty);

        Assert.Equal(once, DocumentHtml.Clean(once));
    }

    // ---------------------------------------------------------------- text and words

    [Fact]
    public void The_text_has_a_line_for_each_block_and_none_of_the_markup()
    {
        var text = DocumentHtml.ToText("<h1>Title</h1><p>One <strong>two</strong></p><ul><li><p>three</p></li><li><p>four</p></li></ul><p>a<br>b</p>");

        Assert.Equal("Title\nOne two\nthree\nfour\na\nb", text);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("one", 1)]
    [InlineData("one two  three\nfour", 4)]
    [InlineData("don't stop-and-go 42 times", 4)]
    [InlineData("hello, world! (really)", 3)]
    [InlineData("-- ... !!!", 0)]
    public void Words_are_runs_of_letters_and_digits(string text, int expected) =>
        Assert.Equal(expected, DocumentHtml.CountWords(text));

    // ---------------------------------------------------------------- the documents

    [Fact]
    public void A_document_takes_its_words_and_text_from_cleaned_html_and_its_title_from_the_first_line()
    {
        var document = new Document();
        document.SetHtml("<h1>My notes</h1><p>Two words <script>x()</script></p>");

        Assert.Equal("<h1>My notes</h1><p>Two words </p>", document.Html);
        Assert.Equal("My notes\nTwo words", document.Text);
        Assert.Equal(4, document.Words);
        Assert.Equal("My notes", Document.TitleFrom(document.Text));
        Assert.Equal(Document.UntitledTitle, Document.TitleFrom(""));
        var long_ = Document.TitleFrom(string.Join(' ', Enumerable.Repeat("word", 40)));
        Assert.True(long_.Length <= DocumentLimits.MaxTitle + 3);
        Assert.EndsWith("...", long_);
    }

    [Fact]
    public void A_document_round_trips_through_json_without_its_derived_text()
    {
        var document = new Document { Title = "Plan", AutoTitle = false };
        document.SetHtml("<p>Hello <strong>you</strong></p>");

        var json = DocumentJson.Write(document);
        Assert.DoesNotContain("\"text\"", json);
        Assert.DoesNotContain("\"words\"", json);
        Assert.True(DocumentJson.TryRead(json, out var back));
        Assert.Equal(document.Id, back.Id);
        Assert.Equal("Plan", back.Title);
        Assert.False(back.AutoTitle);
        Assert.Equal(document.Html, back.Html);
        Assert.Equal("Hello you", back.Text);
        Assert.Equal(2, back.Words);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"id\":\"nope\",\"html\":\"<p>x</p>\"}")]
    [InlineData("{\"id\":\"../../etc/passwd\"}")]
    [InlineData("{\"id\":123}")]
    public void What_is_not_a_document_is_refused(string json) =>
        Assert.False(DocumentJson.TryRead(json, out _));

    [Fact]
    public void Html_read_back_from_the_storage_is_cleaned_again()
    {
        var id = Document.NewId();
        var json = $"{{\"id\":\"{id}\",\"title\":\"  A \\n title  \",\"html\":\"<p onclick=\\\"x()\\\">hi</p><script>y()</script><a href=\\\"javascript:z()\\\">l</a>\",\"createdAt\":\"0001-01-01T00:00:00+00:00\",\"updatedAt\":\"9999-12-31T00:00:00+00:00\"}}";

        Assert.True(DocumentJson.TryRead(json, out var document));

        Assert.Equal("<p>hi</p>l", document.Html);
        Assert.Equal("A title", document.Title);
        Assert.InRange(document.CreatedAt.Year, 2000, 2100);
        Assert.InRange(document.UpdatedAt.Year, 2000, 2100);
    }

    [Fact]
    public void A_stored_document_longer_than_the_app_writes_is_refused_before_it_is_parsed()
    {
        var id = Document.NewId();

        Assert.False(DocumentJson.TryRead($"{{\"id\":\"{id}\",\"html\":\"{new string('a', DocumentJson.MaxJsonCharacters)}\"}}", out _));
        Assert.False(DocumentJson.TryRead($"{{\"id\":\"{id}\",\"html\":\"<p>{new string('a', DocumentLimits.MaxHtmlCharacters)}</p>\"}}", out _));
    }

    [Fact]
    public void A_title_is_one_line_of_bounded_length()
    {
        Assert.Equal("a b c", DocumentState.CleanTitle("  a\n\tb   c "));
        Assert.Equal("", DocumentState.CleanTitle("   "));
        Assert.Equal(DocumentLimits.MaxTitle, DocumentState.CleanTitle(new string('x', 500)).Length);
    }

    [Fact]
    public void Documents_are_grouped_by_the_day_of_the_user_not_the_server()
    {
        var now = new DateTimeOffset(2026, 3, 10, 1, 0, 0, TimeSpan.Zero);
        var offset = TimeSpan.FromHours(-5);
        Document At(DateTimeOffset when) => new() { UpdatedAt = when };
        var items = new[]
        {
            At(now),
            At(now.AddHours(-2)),
            At(now.AddDays(-1)),
            At(now.AddDays(-5)),
            At(now.AddDays(-20)),
            At(now.AddDays(-90)),
        };

        var groups = DocumentGroups.Of(items, now, offset);

        Assert.Equal(["Today", "Yesterday", "Previous 7 days", "Previous 30 days", "Older"], groups.Select(g => g.Label));
        // 01:00 UTC is 20:00 the day before for the user: the first two documents are the same day, the one 2 hours earlier is still "Today".
        Assert.Equal([2, 1, 1, 1, 1], groups.Select(g => g.Items.Count));
    }

    // ---------------------------------------------------------------- the guide

    [Fact]
    public void The_sample_guide_is_stored_as_written_so_the_editor_and_the_allow_list_agree()
    {
        var document = new Document();
        document.SetHtml(SampleDocument.Html);

        Assert.Contains("<h1>Getting started</h1>", document.Html);
        Assert.Contains("data-type=\"taskList\"", document.Html);
        Assert.Contains("href=\"https://example.com\"", document.Html);
        Assert.Contains("<pre><code>", document.Html);
        Assert.True(document.Words > 40);
    }
}
