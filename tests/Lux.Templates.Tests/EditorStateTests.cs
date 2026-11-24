using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Editor.Shared.Data;
using Editor.Shared.Services;

namespace Lux.Templates.Tests;

/// <summary>A stored value as a test puts it in the store: the key it is kept under and its text, unchecked.</summary>
internal sealed record StoredDocument(string Id, string Json);

/// <summary>
/// A store in memory that behaves like the local storage of a browser from the side of the app: it hands out the list without any text, the text of one document at a time, the ids that a
/// search finds, and tells when "another tab" wrote. It records what was read and deleted.
/// </summary>
internal sealed class MemoryStore : IDocumentStore
{
    public List<StoredDocument> Rows { get; } = [];

    public List<StoredDocument> Initial => Rows;

    public List<string> Deleted { get; } = [];

    public List<string> BodiesRead { get; } = [];

    public int IndexReads { get; private set; }

    public bool Fail { get; set; }

    public bool FailLoad { get; set; }

    /// <summary>Set to make reading the list take a while, as a call to the browser does.</summary>
    public bool SlowIndex { get; set; }

    /// <summary>Set to hold every write until the test lets it go.</summary>
    public Func<string, Task>? BeforeSave { get; set; }

    public List<string> Writes { get; } = [];

    private Func<Task>? _changed;

    public Dictionary<string, string> Saved
    {
        get
        {
            var all = new Dictionary<string, string>();
            foreach (var row in Rows)
            {
                all[row.Id] = row.Json;
            }
            return all;
        }
    }

    public bool Watching => _changed is not null;

    public async Task<StoredIndex> LoadIndexAsync()
    {
        if (FailLoad) throw new InvalidOperationException("blocked");
        IndexReads++;
        if (SlowIndex) await Task.Delay(30);
        return new StoredIndex([.. Rows.Select(r => EntryOf(r.Id, r.Json))], 120);
    }

    // What host.js does: the fields of the list, and unusable for what is not a document or is too heavy to hand over.
    private static StoredEntry EntryOf(string key, string json)
    {
        try
        {
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new StoredEntry(key, false);
            string? Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            var usable = json.Length <= DocumentJson.MaxJsonCharacters && DocumentJson.TransferBytes(json) <= DocumentLimits.MaxTransferBytes;
            var auto = !(root.TryGetProperty("autoTitle", out var a) && a.ValueKind == JsonValueKind.False);
            return new StoredEntry(key, usable, Text("id"), Text("title"), auto, Text("createdAt"), Text("updatedAt"), Text("html")?.Length ?? 0);
        }
        catch (JsonException)
        {
            return new StoredEntry(key, false);
        }
    }

    public Task<string?> LoadAsync(string id)
    {
        if (FailLoad) throw new InvalidOperationException("blocked");
        BodiesRead.Add(id);
        var row = Rows.FirstOrDefault(r => r.Id == id);
        return Task.FromResult(row is not null && DocumentJson.TransferBytes(row.Json) <= DocumentLimits.MaxTransferBytes ? row.Json : null);
    }

    public Task<IReadOnlyList<string>> SearchAsync(string query)
    {
        var found = Rows.Where(r => DocumentJson.TryRead(r.Json, out var d) && d.Text.Contains(query, StringComparison.OrdinalIgnoreCase)).Select(r => r.Id).ToList();
        return Task.FromResult<IReadOnlyList<string>>(found);
    }

    public async Task SaveAsync(string id, string json)
    {
        if (BeforeSave is not null) await BeforeSave(id);
        if (Fail) throw new InvalidOperationException("full");
        Writes.Add(id);
        Rows.RemoveAll(r => r.Id == id);
        Rows.Add(new StoredDocument(id, json));
    }

    public Task DeleteAsync(string id)
    {
        Deleted.Add(id);
        Rows.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable?> WatchAsync(Func<Task> onChanged)
    {
        _changed = onChanged;
        return Task.FromResult<IAsyncDisposable?>(new Stop(this));
    }

    /// <summary>What another tab does: writes a document to the same storage. The tab that reads is not told until <see cref="OtherTabChangedAsync"/>.</summary>
    public void OtherTabWrites(Document document) =>
        Rows.Add(new StoredDocument(document.Id, DocumentJson.Write(document)));

    public void OtherTabReplaces(Document document)
    {
        Rows.RemoveAll(r => r.Id == document.Id);
        OtherTabWrites(document);
    }

    public void OtherTabDeletes(string id) => Rows.RemoveAll(r => r.Id == id);

    /// <summary>The "storage" event of the browser.</summary>
    public Task OtherTabChangedAsync() => _changed?.Invoke() ?? Task.CompletedTask;

    private sealed class Stop(MemoryStore store) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            store._changed = null;
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>The state of the Editor template: the list read without any text, one document at a time, the bounds, the other tabs and the order of the saves.</summary>
public class EditorStateTests
{
    private static StoredDocument Stored(string html = "<p>x</p>", DateTimeOffset? updated = null, string? id = null, string? title = null)
    {
        var document = new Document { UpdatedAt = updated ?? DateTimeOffset.UtcNow };
        if (id is not null) document.Id = id;
        if (title is not null)
        {
            document.Title = title;
            document.AutoTitle = false;
        }
        document.SetHtml(html);
        return new StoredDocument(document.Id, DocumentJson.Write(document));
    }

    private static Document Read(StoredDocument stored)
    {
        Assert.True(DocumentJson.TryRead(stored.Json, out var document));
        return document;
    }

    // ---------------------------------------------------------------- reading

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
        Assert.Equal(1, store.IndexReads);
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
    public async Task The_list_is_read_without_the_text_and_the_text_of_a_document_only_when_it_is_opened()
    {
        var store = new MemoryStore();
        var first = Stored("<h1>First</h1><p>one two three</p>", DateTimeOffset.UtcNow.AddMinutes(-2));
        var second = Stored("<p>" + new string('b', 5000) + "</p>", DateTimeOffset.UtcNow.AddMinutes(-1), title: "Second");
        store.Rows.AddRange([first, second]);
        var state = new DocumentState(store);
        await state.LoadAsync();

        // The sidebar has everything it shows, and no text was handed over.
        Assert.Empty(store.BodiesRead);
        Assert.Equal(["Second", "Untitled"], state.Items.Select(d => d.Title));
        Assert.All(state.Items, d =>
        {
            Assert.False(d.BodyLoaded);
            Assert.Equal("", d.Html);
        });
        Assert.Equal(5007, state.Items[0].HtmlLength);
        Assert.Equal(state.Items.Sum(d => (long)d.HtmlLength), state.TotalCharacters);

        // Opening reads one document, once, even when two components ask at the same time.
        var document = state.Find(Read(first).Id)!;
        var results = await Task.WhenAll(state.OpenAsync(document), state.OpenAsync(document));
        await state.OpenAsync(document);

        Assert.All(results, r => Assert.Equal(OpenResult.Opened, r));
        Assert.Equal([document.Id], store.BodiesRead);
        Assert.True(document.BodyLoaded);
        Assert.Equal("<h1>First</h1><p>one two three</p>", document.Html);
        Assert.Equal(4, document.Words);
        Assert.False(state.Find(Read(second).Id)!.BodyLoaded);
    }

    [Fact]
    public async Task A_document_that_is_gone_or_unreadable_when_it_is_opened_leaves_the_list_and_says_so()
    {
        var store = new MemoryStore();
        var vanished = Stored("<p>vanished</p>");
        var broken = Stored("<p>broken</p>");
        store.Rows.AddRange([vanished, broken]);
        var state = new DocumentState(store);
        await state.LoadAsync();
        var notices = new List<StateNotice>();
        var deleted = new List<string>();
        state.Notice += notices.Add;
        state.Deleted += deleted.Add;
        store.Rows.RemoveAll(r => r.Id == vanished.Id);
        store.Rows.RemoveAll(r => r.Id == broken.Id);
        store.Rows.Add(new StoredDocument(broken.Id, "{\"id\":\"" + broken.Id + "\",\"html\":123}"));

        Assert.Equal(OpenResult.Gone, await state.OpenAsync(state.Find(vanished.Id)!));
        Assert.Equal(OpenResult.Gone, await state.OpenAsync(state.Find(broken.Id)!));

        Assert.Empty(state.Items);
        Assert.Equal([vanished.Id, broken.Id], deleted);
        // Only what was not a document is removed from the storage and announced.
        Assert.Equal([broken.Id], store.Deleted);
        Assert.Equal([new StateNotice(NoticeKind.Unreadable, 0, 1)], notices);
    }

    [Fact]
    public async Task Failing_to_read_a_document_does_not_empty_it()
    {
        var store = new MemoryStore();
        var stored = Stored("<p>precious</p>");
        store.Rows.Add(stored);
        var state = new DocumentState(store);
        await state.LoadAsync();
        store.FailLoad = true;
        var document = state.Items[0];

        Assert.Equal(OpenResult.Failed, await state.OpenAsync(document));

        Assert.Contains(document, state.Items);
        Assert.False(document.BodyLoaded);
        Assert.False(state.StorageAvailable);
        Assert.Contains(stored.Id, store.Saved.Keys);
    }

    [Fact]
    public async Task Renaming_a_document_whose_text_is_not_in_memory_reads_it_first_so_the_rename_never_saves_it_empty()
    {
        var store = new MemoryStore();
        var stored = Stored("<p>the whole text</p>");
        store.Rows.Add(stored);
        var state = new DocumentState(store);
        await state.LoadAsync();

        Assert.True(await state.RenameAsync(state.Items[0], "Renamed"));

        Assert.True(DocumentJson.TryRead(store.Saved[stored.Id], out var back));
        Assert.Equal("Renamed", back.Title);
        Assert.Equal("<p>the whole text</p>", back.Html);
    }

    [Fact]
    public async Task A_storage_over_the_bounds_is_repaired_by_removing_the_least_recently_used_and_the_user_is_told_how_many()
    {
        var store = new MemoryStore();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < DocumentLimits.MaxDocuments + 5; i++)
        {
            store.Initial.Add(Stored("<p>x</p>", now.AddMinutes(-i)));
        }
        store.Initial.Add(new StoredDocument(Document.NewId(), "garbage"));
        store.Initial.Add(new StoredDocument(Document.NewId(), "[1]"));
        var state = new DocumentState(store);
        var notices = new List<StateNotice>();
        state.Notice += notices.Add;

        await state.LoadAsync();

        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
        Assert.Equal(7, store.Deleted.Count);
        Assert.All(store.Deleted, id => Assert.DoesNotContain(state.Items, d => d.Id == id));
        var notice = Assert.Single(notices);
        Assert.Equal(new StateNotice(NoticeKind.Repaired, 5, 2), notice);
        var (title, description) = notice.Describe();
        Assert.Equal("Storage cleaned up", title);
        Assert.Contains("5 least recently used documents were removed", description);
        Assert.Contains("2 stored entries that were not readable documents were removed", description);

        // By size: documents that together hold more than the total bound, counted from the list alone.
        var big = new MemoryStore();
        var paragraph = "<p>" + new string('a', 100_000) + "</p>";
        for (var i = 0; i < 14; i++)
        {
            big.Initial.Add(Stored(paragraph, now.AddMinutes(-i)));
        }
        var bigState = new DocumentState(big);

        await bigState.LoadAsync();

        Assert.True(bigState.TotalCharacters <= DocumentLimits.MaxTotalHtmlCharacters);
        Assert.Equal(14 - bigState.Items.Count, big.Deleted.Count);
        Assert.Empty(big.BodiesRead);
    }

    [Fact]
    public async Task A_storage_within_the_bounds_is_read_without_a_word_to_the_user()
    {
        var store = new MemoryStore();
        store.Rows.AddRange([Stored(), Stored()]);
        var state = new DocumentState(store);
        var notices = new List<StateNotice>();
        state.Notice += notices.Add;

        await state.LoadAsync();

        Assert.Empty(notices);
    }

    // ---------------------------------------------------------------- writing and the bounds

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
    public async Task Creating_past_the_bounds_drops_the_least_recently_used_but_never_the_document_being_saved_and_says_how_many()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        await state.LoadAsync();
        var first = await state.CreateAsync();
        var deleted = new List<string>();
        var notices = new List<StateNotice>();
        state.Deleted += deleted.Add;
        state.Notice += notices.Add;
        for (var i = 0; i < DocumentLimits.MaxDocuments; i++)
        {
            await state.CreateAsync();
        }

        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
        Assert.DoesNotContain(first, state.Items);
        Assert.Contains(first.Id, store.Deleted);
        Assert.Contains(first.Id, deleted);
        // One notice, for the one document that had to go.
        var notice = Assert.Single(notices);
        Assert.Equal(new StateNotice(NoticeKind.LimitReached, 1), notice);
        var (title, description) = notice.Describe();
        Assert.Equal("Older documents were removed", title);
        Assert.Contains("1 least recently used document was removed", description);
        Assert.Contains("100 documents and 1,000,000 characters", description);

        // Documents that fill the total bound: the one being edited stays, the oldest go, and the notice counts them.
        notices.Clear();
        var heavy = await state.CreateAsync();
        var paragraph = "<p>" + new string('a', 100_000) + "</p>";
        await state.UpdateHtmlAsync(heavy, paragraph);
        for (var i = 0; i < 12; i++)
        {
            var next = await state.CreateAsync();
            await state.UpdateHtmlAsync(next, paragraph);
        }

        Assert.True(state.TotalCharacters <= DocumentLimits.MaxTotalHtmlCharacters);
        Assert.True(notices.Count > 0);
        Assert.All(notices, n => Assert.Equal(NoticeKind.LimitReached, n.Kind));
    }

    [Fact]
    public async Task The_state_warns_when_it_is_close_to_a_bound_before_anything_is_removed()
    {
        var store = new MemoryStore();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 89; i++)
        {
            store.Rows.Add(Stored("<p>x</p>", now.AddMinutes(-i)));
        }
        var state = new DocumentState(store);
        await state.LoadAsync();

        Assert.False(state.NearLimit);
        await state.CreateAsync();
        Assert.Equal(90, state.Items.Count);
        Assert.True(state.NearLimit);

        // By size: the total passes nine tenths of its bound.
        var big = new MemoryStore();
        var paragraph = "<p>" + new string('a', 100_000) + "</p>";
        for (var i = 0; i < 8; i++)
        {
            big.Rows.Add(Stored(paragraph, now.AddMinutes(-i)));
        }
        var bigState = new DocumentState(big);
        await bigState.LoadAsync();
        Assert.False(bigState.NearLimit);
        big.Rows.Add(Stored(paragraph, now.AddMinutes(-9)));
        await bigState.SyncAsync();
        Assert.True(bigState.NearLimit);
        Assert.Empty(big.Deleted);
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
    public async Task A_change_or_a_rename_that_was_waiting_does_not_bring_a_deleted_document_back()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync("Doomed", "<p>text</p>");
        await state.DeleteAsync(document);

        var saved = await state.UpdateHtmlAsync(document, "<p>late change</p>");
        var renamed = await state.RenameAsync(document, "Late rename");

        Assert.Equal(SaveResult.Gone, saved);
        Assert.True(renamed);
        Assert.Empty(state.Items);
        Assert.Empty(store.Saved);
    }

    // ---------------------------------------------------------------- what travels

    [Theory]
    [InlineData("", 2)]
    [InlineData("ab", 4)]
    [InlineData("\"", 4)]
    [InlineData("\\", 4)]
    [InlineData("a\nb", 6)]
    [InlineData("\u0001", 8)]
    [InlineData("é", 4)]
    [InlineData("日本", 8)]
    [InlineData("😀", 6)]
    public void The_bytes_of_a_text_on_the_wire_are_counted_as_JSON_in_UTF8(string text, int expected) =>
        Assert.Equal(expected, DocumentJson.TransferBytes(text));

    [Fact]
    public void The_count_agrees_with_what_a_JSON_writer_produces()
    {
        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        foreach (var text in new[] { "plain text", "say \"hi\" \\ there", "line\nbreak\ttab\rreturn", "bell\u0007 and \u001f", "café 日本語 ", "<p class=\"a\">\"q\"</p>" })
        {
            Assert.Equal(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(text, options)), DocumentJson.TransferBytes(text));
        }
    }

    [Fact]
    public void A_document_at_the_length_limit_in_any_script_fits_the_bound_of_a_message_with_room_to_spare()
    {
        Assert.Equal(512 * 1024, DocumentLimits.MaxReceiveMessageBytes);
        Assert.True(DocumentLimits.MaxTransferBytes <= DocumentLimits.MaxReceiveMessageBytes - 16 * 1024);

        // The worst ordinary document: the whole limit in a script that takes three bytes a character.
        var cjk = new Document();
        cjk.SetHtml("<p>" + new string('日', DocumentLimits.MaxHtmlCharacters - "<p></p>".Length) + "</p>");
        Assert.Equal(DocumentLimits.MaxHtmlCharacters, cjk.HtmlLength);
        Assert.True(DocumentJson.TransferBytes(DocumentJson.Write(cjk)) <= DocumentLimits.MaxTransferBytes);
        // What the editor sends for it is the text itself, once escaped, in the one message.
        Assert.True(DocumentJson.TransferBytes(cjk.Html) + 1024 <= DocumentLimits.MaxReceiveMessageBytes);

        // Source code in a block: a quote in one character of ten, and a backslash in one of twenty.
        var code = new Document();
        code.SetHtml("<pre><code>" + string.Concat(Enumerable.Repeat("x = \"a\\b\"; ", (DocumentLimits.MaxHtmlCharacters - 25) / 11)) + "</code></pre>");
        Assert.True(DocumentJson.TransferBytes(DocumentJson.Write(code)) <= DocumentLimits.MaxTransferBytes);
    }

    [Fact]
    public async Task A_document_that_would_weigh_more_on_the_wire_than_a_message_may_carry_is_refused_like_one_that_is_too_long()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync();
        await state.UpdateHtmlAsync(document, "<p>short</p>");
        // Within the limit of characters, but every one is a quote: twice over in the document, twice again on the wire.
        var quotes = "<p>" + new string('"', DocumentLimits.MaxHtmlCharacters - "<p></p>".Length) + "</p>";

        var result = await state.UpdateHtmlAsync(document, quotes);

        Assert.Equal(SaveResult.TooLong, result);
        Assert.Equal("<p>short</p>", document.Html);
        Assert.True(DocumentJson.TryRead(store.Saved[document.Id], out var stored));
        Assert.Equal("<p>short</p>", stored.Html);
        // And a new document made of it starts empty rather than unreadable.
        var made = await state.CreateAsync("Quotes", quotes);
        Assert.Equal("", made.Html);
    }

    [Fact]
    public async Task A_stored_document_too_heavy_to_hand_over_is_not_listed_and_is_removed()
    {
        var store = new MemoryStore();
        var heavy = new Document();
        heavy.SetHtml("<p>" + new string('"', DocumentLimits.MaxHtmlCharacters - "<p></p>".Length) + "</p>");
        var fine = Stored("<p>fine</p>");
        store.Rows.AddRange([new StoredDocument(heavy.Id, DocumentJson.Write(heavy)), fine]);
        var state = new DocumentState(store);

        await state.LoadAsync();

        Assert.Equal([fine.Id], state.Items.Select(d => d.Id));
        Assert.Equal([heavy.Id], store.Deleted);
    }

    // ---------------------------------------------------------------- search

    [Fact]
    public async Task Search_looks_in_the_title_and_the_text_in_any_case_and_not_in_the_markup()
    {
        var state = new DocumentState(new MemoryStore());
        var shopping = await state.CreateAsync("Shopping", "<p>Buy <strong>Oat</strong> milk</p>");
        var work = await state.CreateAsync("Work", "<p>Write the report</p>");

        Assert.Equal(2, (await state.SearchAsync(null)).Count);
        Assert.Equal(2, (await state.SearchAsync("  ")).Count);
        Assert.Equal([shopping], await state.SearchAsync("oat MILK"));
        Assert.Equal([work], await state.SearchAsync("REPORT"));
        Assert.Equal([shopping], await state.SearchAsync("shop"));
        Assert.Empty(await state.SearchAsync("strong"));
        Assert.Empty(await state.SearchAsync("<p>"));
    }

    [Fact]
    public async Task Search_finds_the_text_of_documents_that_are_not_in_memory_by_asking_the_store_for_ids_only()
    {
        var store = new MemoryStore();
        var notes = Stored("<p>Remember the train tickets</p>", title: "Notes");
        var other = Stored("<p>Nothing here</p>", title: "Other");
        store.Rows.AddRange([notes, other]);
        var state = new DocumentState(store);
        await state.LoadAsync();

        var byText = await state.SearchAsync("TRAIN");
        var byTitle = await state.SearchAsync("other");

        Assert.Equal([notes.Id], byText.Select(d => d.Id));
        Assert.Equal([other.Id], byTitle.Select(d => d.Id));
        // The text of nothing was brought over to look in it.
        Assert.Empty(store.BodiesRead);
        Assert.All(state.Items, d => Assert.False(d.BodyLoaded));
    }

    // ---------------------------------------------------------------- other tabs

    [Fact]
    public async Task The_state_listens_for_other_tabs_once_it_has_read_and_stops_when_it_is_disposed()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);

        Assert.False(store.Watching);
        await state.LoadAsync();
        Assert.True(store.Watching);

        await state.DisposeAsync();
        Assert.False(store.Watching);
    }

    [Fact]
    public async Task What_another_tab_adds_changes_and_deletes_reaches_the_list_and_the_page_that_shows_it()
    {
        var store = new MemoryStore();
        var now = DateTimeOffset.UtcNow;
        var kept = Stored("<p>kept</p>", now.AddMinutes(-3));
        var edited = Stored("<p>before</p>", now.AddMinutes(-2));
        var removed = Stored("<p>removed</p>", now.AddMinutes(-1));
        store.Rows.AddRange([kept, edited, removed]);
        var state = new DocumentState(store);
        await state.LoadAsync();
        var open = state.Find(Read(edited).Id)!;
        await state.OpenAsync(open);
        var changedElsewhere = new List<Document>();
        var deleted = new List<string>();
        var notices = new List<StateNotice>();
        var lists = 0;
        state.ChangedElsewhere += changedElsewhere.Add;
        state.Deleted += deleted.Add;
        state.Notice += notices.Add;
        state.Changed += () => lists++;

        // Another tab: a new document, an edit of the open one, the deletion of a third.
        var added = Read(Stored("<p>added</p>", now));
        store.OtherTabWrites(added);
        var edit = Read(edited);
        edit.SetHtml("<p>after, and longer</p>");
        edit.Title = "Edited elsewhere";
        edit.UpdatedAt = now;
        store.OtherTabReplaces(edit);
        store.OtherTabDeletes(removed.Id);
        await store.OtherTabChangedAsync();

        Assert.Equal(3, state.Items.Count);
        Assert.Contains(state.Items, d => d.Id == added.Id);
        Assert.DoesNotContain(state.Items, d => d.Id == removed.Id);
        Assert.Equal([removed.Id], deleted);
        Assert.Equal([new StateNotice(NoticeKind.RemovedElsewhere, 1)], notices);
        Assert.Equal("Edited elsewhere", open.Title);
        // The text in memory is the old one, and says so; the page asks again.
        Assert.Equal([open], changedElsewhere);
        Assert.False(open.BodyLoaded);
        Assert.Equal(OpenResult.Opened, await state.OpenAsync(open));
        Assert.Equal("<p>after, and longer</p>", open.Html);
        Assert.True(lists > 0);
    }

    [Fact]
    public async Task A_change_that_is_nothing_new_to_this_tab_is_not_reported_as_one()
    {
        var store = new MemoryStore();
        var stored = Stored("<p>same</p>");
        store.Rows.Add(stored);
        var state = new DocumentState(store);
        await state.LoadAsync();
        var changed = 0;
        state.ChangedElsewhere += _ => changed++;
        var lists = 0;
        state.Changed += () => lists++;
        // This tab's own write, heard back as an event by a browser that reports it to every other tab only: nothing differs.
        var created = await state.CreateAsync();
        lists = 0;

        await store.OtherTabChangedAsync();

        Assert.Equal(0, changed);
        Assert.Equal(0, lists);
        Assert.Equal(2, state.Items.Count);
        Assert.Contains(created, state.Items);
    }

    [Fact]
    public async Task A_document_this_tab_keeps_in_memory_only_survives_a_look_at_the_store()
    {
        var store = new MemoryStore { Fail = true };
        var state = new DocumentState(store);
        await state.LoadAsync();
        var unsaved = await state.CreateAsync("Unsaved", "<p>only here</p>");
        store.Fail = false;

        await store.OtherTabChangedAsync();

        Assert.Contains(unsaved, state.Items);
    }

    [Fact]
    public async Task Several_signals_from_other_tabs_at_once_cost_at_most_one_more_read()
    {
        var store = new MemoryStore();
        store.Rows.Add(Stored());
        var state = new DocumentState(store);
        await state.LoadAsync();
        // A read of the storage takes time (it is a call to the browser): the signals that come meanwhile are folded into one more.
        store.SlowIndex = true;
        var before = store.IndexReads;

        var signals = Enumerable.Range(0, 20).Select(_ => store.OtherTabChangedAsync()).ToList();
        await Task.WhenAll(signals);

        Assert.InRange(store.IndexReads - before, 1, 2);
    }

    [Fact]
    public async Task A_stale_tab_does_not_remove_a_document_that_another_tab_edited_since()
    {
        var store = new MemoryStore();
        var now = DateTimeOffset.UtcNow;
        var oldest = Stored("<p>oldest, in use in another tab</p>", now.AddHours(-5));
        var next = Stored("<p>next oldest</p>", now.AddHours(-4));
        store.Rows.Add(oldest);
        store.Rows.Add(next);
        for (var i = 0; i < DocumentLimits.MaxDocuments - 2; i++)
        {
            store.Rows.Add(Stored("<p>x</p>", now.AddMinutes(-i - 1)));
        }
        var state = new DocumentState(store);
        await state.LoadAsync();
        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
        Assert.Equal(Read(oldest).Id, state.Items[^1].Id);

        // Another tab edits the oldest document now. This tab has not heard of it (no event was delivered).
        var edit = Read(oldest);
        edit.SetHtml("<p>oldest, edited a moment ago</p>");
        edit.UpdatedAt = DateTimeOffset.UtcNow;
        store.OtherTabReplaces(edit);

        var created = await state.CreateAsync();

        // The least recently used is now the next one: the edited document stays, in the list and in the storage.
        Assert.Contains(state.Items, d => d.Id == edit.Id);
        Assert.DoesNotContain(edit.Id, store.Deleted);
        Assert.Contains(edit.Id, store.Saved.Keys);
        Assert.Contains(Read(next).Id, store.Deleted);
        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
        Assert.Contains(created, state.Items);
        // And what it knows of the edited one is the stored version.
        var known = state.Find(edit.Id)!;
        Assert.False(known.BodyLoaded);
        Assert.Equal(edit.UpdatedAt, known.UpdatedAt);
    }

    [Fact]
    public async Task A_stale_tab_does_not_count_a_document_another_tab_already_removed_against_the_bounds()
    {
        var store = new MemoryStore();
        var now = DateTimeOffset.UtcNow;
        var rows = Enumerable.Range(0, DocumentLimits.MaxDocuments).Select(i => Stored("<p>x</p>", now.AddMinutes(-i - 1))).ToList();
        store.Rows.AddRange(rows);
        var state = new DocumentState(store);
        await state.LoadAsync();
        // Another tab deleted the oldest one meanwhile: there is room, so this tab removes nothing.
        store.OtherTabDeletes(rows[^1].Id);

        await state.CreateAsync();

        Assert.Empty(store.Deleted);
        Assert.Equal(DocumentLimits.MaxDocuments, state.Items.Count);
    }

    // ---------------------------------------------------------------- the order of the saves

    [Fact]
    public async Task While_a_change_is_being_written_the_next_ones_wait_and_only_the_last_is_written_after_it()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync();
        store.Writes.Clear();
        var gates = new List<TaskCompletionSource>();
        store.BeforeSave = _ =>
        {
            var gate = new TaskCompletionSource();
            gates.Add(gate);
            return gate.Task;
        };
        var saver = new DocumentSaver(state);
        var finished = new List<(Document, SaveResult)>();
        saver.Finished += (d, r) => finished.Add((d, r));

        var first = saver.SubmitAsync(document, "<p>one</p>");
        var second = saver.SubmitAsync(document, "<p>two</p>");
        var third = saver.SubmitAsync(document, "<p>three</p>");
        await Task.Yield();

        Assert.True(saver.IsBusy(document));
        Assert.Single(gates);
        // The first write finishing does not say "saved": newer text is waiting.
        gates[0].SetResult();
        await WaitFor(() => gates.Count == 2);
        Assert.Empty(finished);
        Assert.True(saver.IsBusy(document));

        gates[1].SetResult();
        await Task.WhenAll(first, second, third);

        var (done, result) = Assert.Single(finished);
        Assert.Equal(document, done);
        Assert.Equal(SaveResult.Saved, result);
        Assert.False(saver.IsBusy(document));
        // The second text never needed writing: the third replaced it, and it is the one stored, last.
        Assert.True(DocumentJson.TryRead(store.Saved[document.Id], out var stored));
        Assert.Equal("<p>three</p>", stored.Html);
        Assert.Equal(2, store.Writes.Count);
    }

    [Fact]
    public async Task A_later_save_that_fails_is_what_the_status_ends_up_saying_even_if_the_first_one_succeeded()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var document = await state.CreateAsync();
        var gate = new TaskCompletionSource();
        var calls = 0;
        store.BeforeSave = _ =>
        {
            if (++calls == 1) return gate.Task;
            store.Fail = true;
            return Task.CompletedTask;
        };
        var saver = new DocumentSaver(state);
        var finished = new List<SaveResult>();
        saver.Finished += (_, r) => finished.Add(r);

        var first = saver.SubmitAsync(document, "<p>one</p>");
        var second = saver.SubmitAsync(document, "<p>two</p>");
        await Task.Yield();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal([SaveResult.StorageFailed], finished);
    }

    [Fact]
    public async Task Changes_to_different_documents_are_each_written_and_each_speak()
    {
        var store = new MemoryStore();
        var state = new DocumentState(store);
        var one = await state.CreateAsync();
        var two = await state.CreateAsync();
        var gate = new TaskCompletionSource();
        var calls = 0;
        store.BeforeSave = _ => ++calls == 1 ? gate.Task : Task.CompletedTask;
        var saver = new DocumentSaver(state);
        var finished = new List<Document>();
        saver.Finished += (d, _) => finished.Add(d);

        var a = saver.SubmitAsync(one, "<p>a</p>");
        var b = saver.SubmitAsync(two, "<p>b</p>");
        await Task.Yield();
        gate.SetResult();
        await Task.WhenAll(a, b);

        Assert.Equal([one, two], finished);
        Assert.Equal("<p>a</p>", one.Html);
        Assert.Equal("<p>b</p>", two.Html);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }
        Assert.True(condition());
    }
}
