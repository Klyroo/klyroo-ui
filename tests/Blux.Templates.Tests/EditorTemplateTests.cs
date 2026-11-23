using Editor.Shared.Data;
using Editor.Shared.Services;

namespace Blux.Templates.Tests;

/// <summary>
/// The code of the Editor template that is plain C# (no Razor): the allow-list that cleans the HTML of a document, the text and word count taken from it, the documents
/// and what the browser storage may hand back, and the state that keeps them within bounds. The pages are tested in a browser (Blux.UI.E2E).
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

    // ---------------------------------------------------------------- the state

    private sealed class MemoryStore : IDocumentStore
    {
        public Dictionary<string, string> Saved { get; } = [];

        public List<StoredDocument> Initial { get; } = [];

        public List<string> Deleted { get; } = [];

        public bool Fail { get; set; }

        public bool FailLoad { get; set; }

        public Task<StoredData> LoadAsync() => FailLoad ? throw new InvalidOperationException("blocked") : Task.FromResult(new StoredData(Initial, 120));

        public Task SaveAsync(string id, string json)
        {
            if (Fail) throw new InvalidOperationException("full");
            Saved[id] = json;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            Deleted.Add(id);
            Saved.Remove(id);
            return Task.CompletedTask;
        }
    }

    private static StoredDocument Stored(string html = "<p>x</p>", DateTimeOffset? updated = null, string? id = null)
    {
        var document = new Document { UpdatedAt = updated ?? DateTimeOffset.UtcNow };
        if (id is not null) document.Id = id;
        document.SetHtml(html);
        return new StoredDocument(document.Id, DocumentJson.Write(document));
    }

    [Fact]
    public async Task The_state_reads_the_store_once_checks_it_and_removes_what_is_not_a_document()
    {
        var store = new MemoryStore();
        var good = Stored("<p>keep</p>", DateTimeOffset.UtcNow.AddMinutes(-5));
        var newer = Stored("<p>newer</p>");
        var other = Stored();
        store.Initial.AddRange([good, newer, new StoredDocument(Document.NewId(), "garbage"), new StoredDocument(other.Id[..31] + "z", other.Json), new StoredDocument(good.Id, good.Json)]);
        var state = new DocumentState(store);

        await Task.WhenAll(state.LoadAsync(), state.LoadAsync());

        Assert.True(state.Loaded);
        Assert.Equal([newer.Id, good.Id], state.Items.Select(d => d.Id));
        Assert.Equal(TimeSpan.FromHours(2), state.UtcOffset);
        Assert.Equal(3, store.Deleted.Count);
        Assert.True(state.StorageAvailable);
    }

    [Fact]
    public async Task A_storage_that_cannot_be_read_starts_empty_and_says_so()
    {
        var state = new DocumentState(new MemoryStore { FailLoad = true });

        await state.LoadAsync();

        Assert.True(state.Loaded);
        Assert.False(state.StorageAvailable);
        Assert.Empty(state.Items);
    }

    [Fact]
    public async Task A_storage_over_the_bounds_is_repaired_by_removing_the_least_recently_used()
    {
        var store = new MemoryStore();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < DocumentLimits.MaxDocuments + 5; i++)
        {
            store.Initial.Add(Stored("<p>x</p>", now.AddMinutes(-i)));
        }
        var state = new DocumentState(store);

        await state.LoadAsync();

        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
        Assert.Equal(5, store.Deleted.Count);
        Assert.All(store.Deleted, id => Assert.DoesNotContain(state.Items, d => d.Id == id));

        // By size: documents that together hold more than the total bound.
        var big = new MemoryStore();
        var paragraph = "<p>" + new string('a', 100_000) + "</p>";
        for (var i = 0; i < 14; i++)
        {
            big.Initial.Add(Stored(paragraph, now.AddMinutes(-i)));
        }
        var bigState = new DocumentState(big);

        await bigState.LoadAsync();

        Assert.True(bigState.Items.Sum(d => (long)d.Html.Length) <= DocumentLimits.MaxTotalHtmlCharacters);
        Assert.Equal(14 - bigState.Items.Count, big.Deleted.Count);
    }

    [Fact]
    public async Task A_new_document_is_written_and_a_title_given_to_it_stops_following_the_text()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        await state.LoadAsync();

        var blank = await state.CreateAsync();
        var named = await state.CreateAsync("Guide", "<p>Body <script>x()</script></p>");

        Assert.Equal([named.Id, blank.Id], state.Items.Select(d => d.Id));
        Assert.True(blank.AutoTitle);
        Assert.False(named.AutoTitle);
        Assert.Equal("<p>Body </p>", named.Html);
        Assert.Equal(2, store.Saved.Count);
    }

    [Fact]
    public async Task An_edit_is_cleaned_saved_and_gives_a_new_document_its_title_from_the_text()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync();

        var result = await state.UpdateHtmlAsync(document, "<h1>Plan for May</h1><p onclick=\"x()\">Details</p><script>y()</script>");

        Assert.Equal(SaveResult.Saved, result);
        Assert.Equal("<h1>Plan for May</h1><p>Details</p>", document.Html);
        Assert.Equal("Plan for May", document.Title);
        Assert.Equal(4, document.Words);
        Assert.True(DocumentJson.TryRead(store.Saved[document.Id], out var stored));
        Assert.Equal(document.Html, stored.Html);

        await state.RenameAsync(document, "  My plan ");
        await state.UpdateHtmlAsync(document, "<h1>Something else</h1>");
        Assert.Equal("My plan", document.Title);
        Assert.False(document.AutoTitle);
    }

    [Fact]
    public async Task An_edit_past_the_length_limit_is_refused_and_leaves_the_document_as_it_was()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync();
        await state.UpdateHtmlAsync(document, "<p>short</p>");

        var result = await state.UpdateHtmlAsync(document, "<p>" + new string('a', DocumentLimits.MaxHtmlCharacters) + "</p>");

        Assert.Equal(SaveResult.TooLong, result);
        Assert.Equal("<p>short</p>", document.Html);
        Assert.True(DocumentJson.TryRead(store.Saved[document.Id], out var stored));
        Assert.Equal("<p>short</p>", stored.Html);
    }

    [Fact]
    public async Task A_write_the_storage_refuses_keeps_the_document_for_the_visit_and_says_so()
    {
        var store = new MemoryStore { Fail = true };
        var state = new DocumentState(store);
        var document = await state.CreateAsync();

        var result = await state.UpdateHtmlAsync(document, "<p>kept in memory</p>");

        Assert.Equal(SaveResult.StorageFailed, result);
        Assert.False(state.StorageAvailable);
        Assert.Contains(document, state.Items);
        Assert.Equal("<p>kept in memory</p>", document.Html);
    }

    [Fact]
    public async Task Creating_past_the_bounds_drops_the_least_recently_used_but_never_the_document_being_saved()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        await state.LoadAsync();
        var first = await state.CreateAsync();
        var deleted = new List<string>();
        state.Deleted += deleted.Add;
        for (var i = 0; i < DocumentLimits.MaxDocuments; i++)
        {
            await state.CreateAsync();
        }

        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
        Assert.DoesNotContain(first, state.Items);
        Assert.Contains(first.Id, store.Deleted);
        Assert.Contains(first.Id, deleted);

        // Documents that fill the total bound: the one being edited stays, the oldest go.
        var heavy = await state.CreateAsync();
        var paragraph = "<p>" + new string('a', 100_000) + "</p>";
        await state.UpdateHtmlAsync(heavy, paragraph);
        for (var i = 0; i < 12; i++)
        {
            var next = await state.CreateAsync();
            await state.UpdateHtmlAsync(next, paragraph);
        }

        Assert.True(state.Items.Sum(d => (long)d.Html.Length) <= DocumentLimits.MaxTotalHtmlCharacters);
    }

    [Fact]
    public async Task Rename_changes_the_title_ignores_an_empty_one_and_delete_removes_the_document_and_tells_the_page()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync("Old");
        var deleted = new List<string>();
        state.Deleted += deleted.Add;

        Assert.True(await state.RenameAsync(document, "New\ntitle"));
        Assert.Equal("New title", document.Title);
        Assert.True(await state.RenameAsync(document, "   "));
        Assert.Equal("New title", document.Title);

        await state.DeleteAsync(document);
        await state.DeleteAsync(document);

        Assert.Empty(state.Items);
        Assert.Equal([document.Id], deleted);
        Assert.Equal([document.Id], store.Deleted);
    }

    [Fact]
    public async Task Search_looks_in_the_title_and_the_text_in_any_case_and_not_in_the_markup()
    {
        var state = new DocumentState(new MemoryStore());
        var shopping = await state.CreateAsync("Shopping", "<p>Buy <strong>Oat</strong> milk</p>");
        var work = await state.CreateAsync("Work", "<p>Write the report</p>");

        Assert.Equal(2, state.Search(null).Count);
        Assert.Equal(2, state.Search("  ").Count);
        Assert.Equal([shopping], state.Search("oat MILK"));
        Assert.Equal([work], state.Search("REPORT"));
        Assert.Equal([shopping], state.Search("shop"));
        Assert.Empty(state.Search("strong"));
        Assert.Empty(state.Search("<p>"));
    }

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
