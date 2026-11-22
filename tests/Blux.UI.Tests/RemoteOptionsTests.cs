using Blux.UI.Internal;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tests;

/// <summary>The loading logic behind OptionsProvider of BxSelectMenu and BxInputMenu, without any rendering.</summary>
public class RemoteOptionsTests
{
    /// <summary>A source of 60 options named Item 1..60 that records its requests and can be held back.</summary>
    private sealed class Source
    {
        public List<BxOptionsRequest> Requests { get; } = [];
        public List<TaskCompletionSource> Gates { get; } = [];
        public bool Hold { get; set; }
        public bool Fail { get; set; }
        public bool Cursors { get; set; }
        public int Total { get; set; } = 60;

        public async ValueTask<BxOptionsResult> Load(BxOptionsRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            if (Hold)
            {
                var gate = new TaskCompletionSource();
                Gates.Add(gate);
                using var registration = ct.Register(() => gate.TrySetCanceled(ct));
                await gate.Task;
            }
            if (Fail) throw new InvalidOperationException("boom");
            var start = Cursors ? (request.Cursor is null ? 0 : int.Parse(request.Cursor)) : request.Skip;
            var matches = Enumerable.Range(1, Total).Select(i => $"Item {i}")
                .Where(l => request.Search.Length == 0 || l.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            var page = matches.Skip(start).Take(request.Take).Select(l => new BxSelectItem(l.Replace("Item ", "i"), l)).ToList();
            var end = start + page.Count;
            return Cursors
                ? new BxOptionsResult(page, NextCursor: end < matches.Count ? end.ToString() : null)
                : new BxOptionsResult(page, HasMore: end < matches.Count);
        }
    }

    private static (BxRemoteOptions Options, Func<int> Changes) Create(Source source, int pageSize = 25)
    {
        var changes = 0;
        var options = new BxRemoteOptions(source.Load, () => changes++) { PageSize = pageSize };
        return (options, () => changes);
    }

    [Fact]
    public async Task The_first_search_lists_one_page_and_the_next_pages_follow_by_skip_and_take()
    {
        var source = new Source();
        var (options, _) = Create(source);
        Assert.Equal(BxRemoteStatus.Idle, options.Status);

        await options.SearchAsync("");

        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
        Assert.Equal(25, options.Items.Count);
        Assert.True(options.HasMore);
        Assert.Equal(new BxOptionsRequest("", 0, 25, null), source.Requests[0]);

        await options.LoadMoreAsync();
        await options.LoadMoreAsync();

        Assert.Equal(60, options.Items.Count);
        Assert.False(options.HasMore);
        Assert.Equal(new BxOptionsRequest("", 25, 25, null), source.Requests[1]);
        Assert.Equal(new BxOptionsRequest("", 50, 25, null), source.Requests[2]);

        await options.LoadMoreAsync();
        Assert.Equal(3, source.Requests.Count);
    }

    [Fact]
    public async Task A_cursor_is_handed_back_for_the_next_page_and_a_cursor_alone_means_there_is_more()
    {
        var source = new Source { Cursors = true };
        var (options, _) = Create(source, 30);

        await options.SearchAsync(null);
        Assert.True(options.HasMore);
        await options.LoadMoreAsync();

        Assert.Equal(new BxOptionsRequest("", 30, 30, "30"), source.Requests[1]);
        Assert.Equal(60, options.Items.Count);
        Assert.False(options.HasMore);
    }

    [Fact]
    public async Task The_term_is_trimmed_and_the_same_term_does_not_ask_again()
    {
        var source = new Source();
        var (options, _) = Create(source);

        await options.SearchAsync("  Item 1 ");
        await options.SearchAsync("Item 1");
        await options.SearchAsync(" Item 1  ");

        Assert.Single(source.Requests);
        Assert.Equal("Item 1", source.Requests[0].Search);
        Assert.Equal("Item 1", options.Term);

        await options.SearchAsync("");
        Assert.Equal(2, source.Requests.Count);
        Assert.Equal(25, options.Items.Count);
    }

    [Fact]
    public async Task An_older_term_never_overwrites_a_newer_one_and_its_request_is_cancelled()
    {
        var source = new Source { Hold = true };
        var (options, _) = Create(source);

        var first = options.SearchAsync("1");
        var second = options.SearchAsync("2");
        Assert.Equal(2, source.Gates.Count);
        // The older request was cancelled by the newer search.
        Assert.True(source.Gates[0].Task.IsCanceled);

        source.Gates[1].SetResult();
        await second;
        var listed = options.Items.Select(i => i.Label).ToList();
        Assert.All(listed, l => Assert.Contains("2", l));

        await first;
        Assert.Equal(listed, options.Items.Select(i => i.Label));
        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
        Assert.Equal("2", options.Term);
    }

    [Fact]
    public async Task A_late_answer_that_the_provider_did_not_stop_is_ignored_too()
    {
        var gates = new List<TaskCompletionSource<BxOptionsResult>>();
        var options = new BxRemoteOptions((_, _) =>
        {
            var gate = new TaskCompletionSource<BxOptionsResult>();
            gates.Add(gate);
            return new ValueTask<BxOptionsResult>(gate.Task);
        }, () => { });

        var first = options.SearchAsync("a");
        var second = options.SearchAsync("b");
        gates[1].SetResult(new BxOptionsResult([new BxSelectItem("b1", "B one")]));
        await second;
        gates[0].SetResult(new BxOptionsResult([new BxSelectItem("a1", "A one")]));
        await first;

        Assert.Equal(["b1"], options.Items.Select(i => i.Value));
    }

    [Fact]
    public async Task A_new_search_while_the_next_page_loads_drops_that_page()
    {
        var source = new Source();
        var (options, _) = Create(source);
        await options.SearchAsync("");
        source.Hold = true;

        var more = options.LoadMoreAsync();
        Assert.Equal(BxRemoteStatus.LoadingMore, options.Status);
        var search = options.SearchAsync("Item 5");
        source.Gates[1].SetResult();
        await search;
        await more;

        Assert.All(options.Items, i => Assert.Contains("Item 5", i.Label));
        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
    }

    [Fact]
    public async Task The_previous_options_stay_listed_while_a_new_term_loads_and_the_generation_moves_when_they_are_replaced()
    {
        var source = new Source();
        var (options, _) = Create(source);
        await options.SearchAsync("");
        var generation = options.Generation;
        await options.LoadMoreAsync();
        Assert.Equal(generation, options.Generation);

        source.Hold = true;
        var search = options.SearchAsync("Item 4");
        Assert.Equal(BxRemoteStatus.Loading, options.Status);
        Assert.Equal(50, options.Items.Count);
        Assert.Equal(generation, options.Generation);

        source.Gates[0].SetResult();
        await search;
        Assert.NotEqual(generation, options.Generation);
    }

    [Fact]
    public async Task MinLength_holds_back_short_terms_but_an_empty_term_lists_everything()
    {
        var source = new Source();
        var (options, _) = Create(source);
        options.MinLength = 3;

        await options.SearchAsync("");
        Assert.Equal(25, options.Items.Count);

        await options.SearchAsync("It");
        Assert.Equal(BxRemoteStatus.TooShort, options.Status);
        Assert.Empty(options.Items);
        Assert.Single(source.Requests);

        await options.SearchAsync("Ite");
        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
        Assert.Equal(2, source.Requests.Count);
        Assert.Equal("Ite", source.Requests[1].Search);
    }

    [Fact]
    public async Task MinLength_counts_the_trimmed_term()
    {
        var source = new Source();
        var (options, _) = Create(source);
        options.MinLength = 3;

        await options.SearchAsync(" ab ");

        Assert.Equal(BxRemoteStatus.TooShort, options.Status);
        Assert.Empty(source.Requests);
    }

    [Fact]
    public async Task A_failure_is_an_error_state_that_retry_repeats()
    {
        var source = new Source { Fail = true };
        var (options, _) = Create(source);

        await options.SearchAsync("");

        Assert.Equal(BxRemoteStatus.Error, options.Status);
        Assert.Empty(options.Items);

        source.Fail = false;
        await options.RetryAsync();

        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
        Assert.Equal(25, options.Items.Count);
        Assert.Equal(source.Requests[0], source.Requests[1]);
    }

    [Fact]
    public async Task A_failed_next_page_keeps_the_options_and_retry_asks_for_the_same_page()
    {
        var source = new Source();
        var (options, _) = Create(source);
        await options.SearchAsync("");
        source.Fail = true;

        await options.LoadMoreAsync();

        Assert.Equal(BxRemoteStatus.Error, options.Status);
        Assert.Equal(25, options.Items.Count);

        // Nothing else loads while the error is shown.
        await options.LoadMoreAsync();
        Assert.Equal(2, source.Requests.Count);

        source.Fail = false;
        await options.RetryAsync();

        Assert.Equal(50, options.Items.Count);
        Assert.Equal(new BxOptionsRequest("", 25, 25, null), source.Requests[^1]);
    }

    [Fact]
    public async Task Searching_again_after_an_error_asks_again_with_the_same_term()
    {
        var source = new Source { Fail = true };
        var (options, _) = Create(source);
        await options.SearchAsync("x");
        source.Fail = false;

        await options.SearchAsync("x");

        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
    }

    [Fact]
    public async Task A_value_returned_twice_is_listed_once_but_the_skip_counts_what_was_received()
    {
        var calls = 0;
        var requests = new List<BxOptionsRequest>();
        var options = new BxRemoteOptions((request, _) =>
        {
            requests.Add(request);
            return new ValueTask<BxOptionsResult>(calls++ == 0
                ? new BxOptionsResult([new("a", "A"), new("b", "B")], HasMore: true)
                : new BxOptionsResult([new("b", "B"), new("c", "C")]));
        }, () => { });

        await options.SearchAsync("");
        await options.LoadMoreAsync();

        Assert.Equal(["a", "b", "c"], options.Items.Select(i => i.Value));
        Assert.Equal(2, requests[1].Skip);
    }

    [Fact]
    public async Task Selected_values_keep_their_label_after_a_search_replaces_the_list()
    {
        var source = new Source();
        var (options, _) = Create(source);
        await options.SearchAsync("");

        options.RememberSelected(["i3"]);
        await options.SearchAsync("Item 4");

        Assert.DoesNotContain(options.Items, i => i.Value == "i3");
        Assert.Equal("Item 3", options.LabelOf("i3"));
        Assert.Equal("unknown", options.LabelOf("unknown"));
        Assert.Equal(["i3", "Item 3"], options.KnownLabels(["i3", "i9"]));
    }

    [Fact]
    public async Task ResolveSelected_runs_once_for_the_values_nobody_knows()
    {
        var (options, _) = Create(new Source());
        options.StaticItems = [new BxSelectItem("given", "Given")];
        var asked = new List<string[]>();
        BxSelectedOptionsResolver resolver = (values, _) =>
        {
            asked.Add([.. values]);
            return new ValueTask<IReadOnlyList<BxSelectItem>>([new BxSelectItem("i7", "Item 7")]);
        };

        await options.ResolveAsync(["i7", "given", "gone"], resolver);
        await options.ResolveAsync(["i7", "given", "gone"], resolver);

        Assert.Single(asked);
        Assert.Equal(["i7", "gone"], asked[0]);
        Assert.Equal("Item 7", options.LabelOf("i7"));
        Assert.Equal("Given", options.LabelOf("given"));
        // Not found: the raw value.
        Assert.Equal("gone", options.LabelOf("gone"));
    }

    [Fact]
    public async Task A_resolver_that_throws_leaves_raw_values_and_is_not_called_again()
    {
        var (options, _) = Create(new Source());
        var calls = 0;
        BxSelectedOptionsResolver resolver = (_, _) =>
        {
            calls++;
            throw new InvalidOperationException("down");
        };

        await options.ResolveAsync(["x"], resolver);
        await options.ResolveAsync(["x"], resolver);

        Assert.Equal(1, calls);
        Assert.Equal("x", options.LabelOf("x"));
    }

    [Fact]
    public async Task Nothing_is_reported_or_loaded_after_dispose()
    {
        var source = new Source { Hold = true };
        var (options, changes) = Create(source);
        var search = options.SearchAsync("a");
        var before = changes();

        options.Dispose();
        source.Gates[0].TrySetResult();
        await search;

        Assert.Equal(before, changes());
        Assert.Empty(options.Items);
        await options.SearchAsync("b");
        Assert.Single(source.Requests);
    }

    [Fact]
    public void PageSize_is_at_least_one()
    {
        var (options, _) = Create(new Source());
        options.PageSize = 0;
        Assert.Equal(1, options.PageSize);
    }

    [Fact]
    public async Task Reset_forgets_pages_term_labels_and_resolutions_and_loads_again_on_the_next_search()
    {
        var source = new Source();
        var (options, _) = Create(source);
        await options.SearchAsync("Item 4");
        options.RememberSelected([options.Items[0].Value]);
        var epoch = options.Epoch;
        var generation = options.Generation;
        var label = options.Items[0].Label;
        Assert.Equal(label, options.LabelOf(options.Items[0].Value));

        options.Reset();

        Assert.Equal(BxRemoteStatus.Idle, options.Status);
        Assert.Empty(options.Items);
        Assert.Equal("", options.Term);
        Assert.False(options.HasMore);
        Assert.NotEqual(epoch, options.Epoch);
        Assert.NotEqual(generation, options.Generation);
        Assert.Equal("i4", options.LabelOf("i4"));

        await options.SearchAsync("Item 4");
        Assert.Equal(2, source.Requests.Count);
        Assert.Equal(new BxOptionsRequest("Item 4", 0, 25, null), source.Requests[1]);
    }

    [Fact]
    public async Task Reset_drops_the_request_in_flight()
    {
        var source = new Source { Hold = true };
        var (options, _) = Create(source);
        var pending = options.SearchAsync("a");

        options.Reset();
        source.Gates[0].TrySetResult();
        await pending;

        Assert.Empty(options.Items);
        Assert.Equal(BxRemoteStatus.Idle, options.Status);
    }

    /// <summary>A resolver whose calls complete when the test says so, in any order, and that ignores cancellation (a source that does not honour the token).</summary>
    private sealed class ControlledResolver
    {
        public List<(IReadOnlyList<string> Values, TaskCompletionSource<IReadOnlyList<BxSelectItem>> Gate, CancellationToken Token)> Calls { get; } = [];

        public ValueTask<IReadOnlyList<BxSelectItem>> Resolve(IReadOnlyList<string> values, CancellationToken ct)
        {
            var gate = new TaskCompletionSource<IReadOnlyList<BxSelectItem>>();
            Calls.Add((values, gate, ct));
            return new ValueTask<IReadOnlyList<BxSelectItem>>(gate.Task);
        }
    }

    [Fact]
    public async Task A_resolve_of_the_old_source_that_finishes_after_Reset_leaves_no_label_and_the_new_source_resolves_the_value()
    {
        var (options, changes) = Create(new Source());
        var resolver = new ControlledResolver();
        BxSelectedOptionsResolver resolve = resolver.Resolve;
        var oldResolve = options.ResolveAsync(["x"], resolve);

        options.Reset();
        var changesAfterReset = changes();
        // The new source asks for the same value; the old answer comes last.
        var newResolve = options.ResolveAsync(["x"], resolve);
        Assert.Equal(2, resolver.Calls.Count);
        Assert.True(resolver.Calls[0].Token.IsCancellationRequested);
        Assert.False(resolver.Calls[1].Token.IsCancellationRequested);
        resolver.Calls[1].Gate.SetResult([new BxSelectItem("x", "New label")]);
        await newResolve;
        resolver.Calls[0].Gate.SetResult([new BxSelectItem("x", "Old label")]);
        await oldResolve;

        Assert.Equal("New label", options.LabelOf("x"));
        Assert.True(changes() > changesAfterReset);
    }

    [Fact]
    public async Task A_resolve_of_the_old_source_that_finishes_after_Reset_does_not_make_the_value_known()
    {
        var (options, changes) = Create(new Source());
        var resolver = new ControlledResolver();
        BxSelectedOptionsResolver resolve = resolver.Resolve;
        var oldResolve = options.ResolveAsync(["x"], resolve);

        options.Reset();
        var changesAfterReset = changes();
        resolver.Calls[0].Gate.SetResult([new BxSelectItem("x", "Old label")]);
        await oldResolve;

        // Nothing knows x for the new source, so the next resolution asks again instead of being blocked by Knows.
        Assert.False(options.Knows("x"));
        Assert.Equal("x", options.LabelOf("x"));
        Assert.Equal(changesAfterReset, changes());
        var again = options.ResolveAsync(["x"], resolve);
        Assert.Equal(2, resolver.Calls.Count);
        resolver.Calls[1].Gate.SetResult([new BxSelectItem("x", "New label")]);
        await again;
        Assert.Equal("New label", options.LabelOf("x"));
    }

    [Fact]
    public async Task A_resolve_of_the_old_source_that_fails_after_Reset_does_not_mark_the_value_as_failed()
    {
        var (options, _) = Create(new Source());
        var resolver = new ControlledResolver();
        BxSelectedOptionsResolver resolve = resolver.Resolve;
        var oldResolve = options.ResolveAsync(["x"], resolve);

        options.Reset();
        var newResolve = options.ResolveAsync(["x"], resolve);
        resolver.Calls[0].Gate.SetException(new InvalidOperationException("old source down"));
        await oldResolve;
        resolver.Calls[1].Gate.SetResult([new BxSelectItem("x", "New label")]);
        await newResolve;

        Assert.Equal("New label", options.LabelOf("x"));
        // The failure of the old source is not remembered: a retry request does not ask again for a value that is known.
        await options.ResolveAsync(["x"], resolve, retryFailed: true);
        Assert.Equal(2, resolver.Calls.Count);
    }

    [Fact]
    public async Task A_resolve_without_a_Reset_still_records_its_labels_and_its_failures()
    {
        var (options, _) = Create(new Source());
        var resolver = new ControlledResolver();
        BxSelectedOptionsResolver resolve = resolver.Resolve;
        var first = options.ResolveAsync(["x"], resolve);
        var second = options.ResolveAsync(["y"], resolve);

        resolver.Calls[1].Gate.SetException(new InvalidOperationException("down"));
        resolver.Calls[0].Gate.SetResult([new BxSelectItem("x", "Ex")]);
        await Task.WhenAll(first, second);

        Assert.Equal("Ex", options.LabelOf("x"));
        Assert.False(options.Knows("y"));
        var retry = options.ResolveAsync(["y"], resolve, retryFailed: true);
        Assert.Equal(3, resolver.Calls.Count);
        resolver.Calls[2].Gate.SetResult([new BxSelectItem("y", "Why")]);
        await retry;
        Assert.Equal("Why", options.LabelOf("y"));
    }

    [Fact]
    public async Task Dispose_cancels_a_resolve_in_flight_and_drops_its_answer()
    {
        var (options, _) = Create(new Source());
        var resolver = new ControlledResolver();
        var pending = options.ResolveAsync(["x"], resolver.Resolve);

        options.Dispose();
        resolver.Calls[0].Gate.SetResult([new BxSelectItem("x", "Ex")]);
        await pending;

        Assert.True(resolver.Calls[0].Token.IsCancellationRequested);
        Assert.False(options.Knows("x"));
    }

    [Fact]
    public async Task A_resolution_that_threw_is_asked_again_only_when_retry_is_requested()
    {
        var (options, _) = Create(new Source());
        var calls = 0;
        BxSelectedOptionsResolver resolver = (values, _) =>
        {
            calls++;
            return calls == 1 ? throw new InvalidOperationException("down") : new ValueTask<IReadOnlyList<BxSelectItem>>([new BxSelectItem("x", "Ex")]);
        };

        await options.ResolveAsync(["x"], resolver);
        await options.ResolveAsync(["x"], resolver);
        Assert.Equal(1, calls);
        Assert.Equal("x", options.LabelOf("x"));

        await options.ResolveAsync(["x"], resolver, retryFailed: true);

        Assert.Equal(2, calls);
        Assert.Equal("Ex", options.LabelOf("x"));
    }

    [Fact]
    public void The_remote_options_messages_are_drafts_in_every_locale_but_en_and_fr()
    {
        Assert.Equal(["en", "fr"], BxLocales.ReviewedCodes);
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "Blux.UI.slnx"))) root = Path.GetDirectoryName(root);
        Assert.NotNull(root);
        foreach (var locale in BxLocales.All.Where(l => !BxLocales.ReviewedCodes.Contains(l.Code)))
        {
            var file = Directory.GetFiles(Path.Combine(root, "src", "Blux.UI", "Localization", "Locales"), "*.cs")
                .Single(f => File.ReadAllText(f).Contains($"new(\"{locale.Code}\"", StringComparison.Ordinal));
            var text = File.ReadAllText(file);
            var draft = text.IndexOf("Draft translations of the four RemoteOptions messages", StringComparison.Ordinal);
            Assert.True(draft >= 0 && draft < text.IndexOf("RemoteOptionsLoading =", StringComparison.Ordinal), $"{locale.Code} is not marked as a draft");
        }
    }
}

