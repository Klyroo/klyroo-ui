namespace Blux.UI.Tools.Tests;

/// <summary>
/// The remote options engine of Blux.UI as a companion package sees it: only its public surface (this project has no access to the internals of Blux.UI). The data list filter editors list
/// remote options with it, as <c>BxSelectMenu</c> does.
/// </summary>
public class BxRemoteOptionsPublicApiTests
{
    private static readonly string[] Names = ["ann", "bob", "cyd", "dee", "eve"];

    private sealed class Source
    {
        public List<BxOptionsRequest> Requests { get; } = [];

        public bool Fail { get; set; }

        public BxOptionsProvider Provider => (request, token) =>
        {
            Requests.Add(request);
            token.ThrowIfCancellationRequested();
            if (Fail) throw new InvalidOperationException("down");
            var matching = Names.Where(n => request.Search.Length == 0 || n.Contains(request.Search, StringComparison.Ordinal)).ToList();
            var page = matching.Skip(request.Skip).Take(request.Take).Select(n => new BxSelectItem(n, n.ToUpperInvariant())).ToList();
            return ValueTask.FromResult(new BxOptionsResult(page, request.Skip + request.Take < matching.Count));
        };
    }

    [Fact]
    public async Task The_first_page_then_the_next_one_are_listed_in_order_and_the_state_follows()
    {
        var source = new Source();
        var changes = 0;
        using var options = new BxRemoteOptions(source.Provider, () => changes++) { PageSize = 2 };
        Assert.Equal(BxRemoteStatus.Idle, options.Status);

        await options.SearchAsync("");

        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
        Assert.Equal(["ann", "bob"], options.Items.Select(i => i.Value));
        Assert.True(options.HasMore);
        Assert.False(options.Busy);

        await options.LoadMoreAsync();
        await options.LoadMoreAsync();

        Assert.Equal(Names, options.Items.Select(i => i.Value));
        Assert.False(options.HasMore);
        Assert.Equal([new BxOptionsRequest("", 0, 2, null), new BxOptionsRequest("", 2, 2, null), new BxOptionsRequest("", 4, 2, null)], source.Requests);
        Assert.True(changes >= 6);
    }

    [Fact]
    public async Task A_search_replaces_the_list_and_the_same_term_again_asks_nothing()
    {
        var source = new Source();
        using var options = new BxRemoteOptions(source.Provider, () => { }) { PageSize = 5 };
        await options.SearchAsync("");

        await options.SearchAsync("e");
        await options.SearchAsync(" e ");

        Assert.Equal("e", options.Term);
        Assert.Equal(["dee", "eve"], options.Items.Select(i => i.Value));
        Assert.Equal(2, source.Requests.Count);
    }

    [Fact]
    public async Task A_term_shorter_than_the_minimum_lists_nothing_and_asks_nothing()
    {
        var source = new Source();
        using var options = new BxRemoteOptions(source.Provider, () => { }) { MinLength = 3 };
        await options.SearchAsync("");

        await options.SearchAsync("ee");

        Assert.Equal(BxRemoteStatus.TooShort, options.Status);
        Assert.Empty(options.Items);
        Assert.Single(source.Requests);
    }

    [Fact]
    public async Task A_failure_is_the_error_status_and_retry_asks_the_same_page_again()
    {
        var source = new Source { Fail = true };
        using var options = new BxRemoteOptions(source.Provider, () => { });

        await options.SearchAsync("");
        Assert.Equal(BxRemoteStatus.Error, options.Status);
        source.Fail = false;
        await options.RetryAsync();

        Assert.Equal(BxRemoteStatus.Loaded, options.Status);
        Assert.Equal(Names, options.Items.Select(i => i.Value));
        Assert.Equal(2, source.Requests.Count);
    }

    [Fact]
    public async Task Disposing_cancels_the_request_in_flight_and_ignores_its_answer()
    {
        var gate = new TaskCompletionSource<BxOptionsResult>();
        CancellationToken seen = default;
        var options = new BxRemoteOptions((_, token) =>
        {
            seen = token;
            return new ValueTask<BxOptionsResult>(gate.Task);
        }, () => { });

        var search = options.SearchAsync("");
        Assert.Equal(BxRemoteStatus.Loading, options.Status);
        Assert.True(options.Busy);
        options.Dispose();
        gate.SetResult(new BxOptionsResult([new BxSelectItem("late", "Late")]));
        await search;

        Assert.True(seen.IsCancellationRequested);
        Assert.Empty(options.Items);
    }

    [Fact]
    public void The_page_size_is_at_least_one_and_defaults_to_the_documented_size()
    {
        using var options = new BxRemoteOptions((_, _) => ValueTask.FromResult(new BxOptionsResult([])), () => { });

        Assert.Equal(BxRemoteOptions.DefaultPageSize, options.PageSize);
        options.PageSize = 0;
        Assert.Equal(1, options.PageSize);
    }
}
