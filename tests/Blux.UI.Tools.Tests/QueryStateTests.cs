using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>QueryState on bUnit's fake navigation manager: batching, push and replace, claims, subscriptions, defaults and unknown keys.</summary>
public class QueryStateTests : BluxToolsTestContext
{
    private BunitNavigationManager Nav => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    private QueryState State => Services.GetRequiredService<QueryState>();

    // The fake replaces the top history entry on a replace, so the history cannot count them: count the events.
    private int _navigations;

    private int Navigations => _navigations;

    public QueryStateTests() => Nav.LocationChanged += (_, _) => Interlocked.Increment(ref _navigations);

    private NavigationHistory Last => Nav.History.First();

    private static async Task Until(Func<bool> condition, string because)
    {
        for (var deadline = DateTime.UtcNow.AddSeconds(5); DateTime.UtcNow < deadline; await Task.Delay(10, Xunit.TestContext.Current.CancellationToken))
        {
            if (condition()) return;
        }
        Assert.Fail("Timed out waiting for " + because);
    }

    private static async Task Settle() => await Task.Delay(150, Xunit.TestContext.Current.CancellationToken);

    [Fact]
    public async Task Writes_of_one_handler_merge_into_a_single_navigation_after_the_handler_yields()
    {
        Nav.NavigateTo("/list");
        var before = Navigations;

        State.Set("q", "ab");
        State.Set("p", "2");
        State.Set("q", "abc");

        // Nothing has reached the URL yet, but a read already sees the writes.
        Assert.Equal(before, Navigations);
        Assert.Equal("abc", State.Get("q"));
        await Until(() => Navigations > before, "the flush");
        await Settle();
        Assert.Equal(before + 1, Navigations);
        Assert.Equal("http://localhost/list?q=abc&p=2", Nav.Uri);
    }

    [Fact]
    public async Task A_replace_rewrites_the_current_entry_and_a_push_adds_one()
    {
        Nav.NavigateTo("/list");
        State.Set("q", "a", QueryHistory.Replace);
        await Until(() => Nav.Uri.EndsWith("?q=a"), "the replace");
        Assert.True(Last.Options.ReplaceHistoryEntry);

        State.Set("p", "2", QueryHistory.Push);
        await Until(() => Nav.Uri.EndsWith("p=2"), "the push");
        Assert.False(Last.Options.ReplaceHistoryEntry);
    }

    [Fact]
    public async Task A_push_anywhere_in_the_batch_makes_the_whole_batch_push()
    {
        Nav.NavigateTo("/list");
        var before = Navigations;

        State.Set("q", "typed", QueryHistory.Replace);
        State.Set("p", "4", QueryHistory.Push);
        State.Set("s", "name", QueryHistory.Replace);

        await Until(() => Navigations > before, "the flush");
        await Settle();
        Assert.Equal(before + 1, Navigations);
        Assert.False(Last.Options.ReplaceHistoryEntry);
        Assert.Equal("http://localhost/list?q=typed&p=4&s=name", Nav.Uri);
    }

    [Fact]
    public async Task SetMany_is_one_navigation_and_a_null_value_removes_the_key()
    {
        Nav.NavigateTo("/list?a=1&b=2");
        var before = Navigations;

        State.SetMany([new("a", null), new("c", "3"), new("b", "20")], QueryHistory.Push);

        await Until(() => Navigations > before, "the flush");
        await Settle();
        Assert.Equal(before + 1, Navigations);
        Assert.Equal("http://localhost/list?b=20&c=3", Nav.Uri);
    }

    [Fact]
    public async Task A_write_that_changes_nothing_does_not_navigate()
    {
        Nav.NavigateTo("/list?q=a&p=2");
        var before = Navigations;

        State.Set("q", "a");
        State.Set("p", "2");
        State.Set("absent", null);
        State.Set("q", "b");
        State.Set("q", "a");
        await Settle();

        Assert.Equal(before, Navigations);
    }

    [Fact]
    public async Task Unknown_keys_keep_their_text_order_repeats_and_fragment()
    {
        Nav.NavigateTo("/list?utm_source=a%20b&tag=x&tag=y&flag&q=old#section");

        State.Set("q", "new");

        await Until(() => Nav.Uri.Contains("q=new"), "the flush");
        Assert.Equal("http://localhost/list?utm_source=a%20b&tag=x&tag=y&flag&q=new#section", Nav.Uri);
    }

    [Fact]
    public async Task Values_are_percent_encoded_and_read_back_decoded()
    {
        Nav.NavigateTo("/list");

        State.Set("q", "a b&c=d+e/é");

        await Until(() => Nav.Uri.Contains("q="), "the flush");
        Assert.Equal("http://localhost/list?q=a%20b%26c%3Dd%2Be%2F%C3%A9", Nav.Uri);
        Nav.NavigateTo("/list?q=a%20b%26c%3Dd%2Be%2F%C3%A9&r=x+y");
        Assert.Equal("a b&c=d+e/é", State.Get("q"));
        Assert.Equal("x y", State.Get("r"));
    }

    [Fact]
    public void Get_reads_the_first_value_and_null_for_an_absent_key()
    {
        Nav.NavigateTo("/list?a=1&a=2&empty=&bare");

        Assert.Equal("1", State.Get("a"));
        Assert.Equal("", State.Get("empty"));
        Assert.Equal("", State.Get("bare"));
        Assert.Null(State.Get("missing"));
    }

    [Fact]
    public async Task A_value_equal_to_its_default_is_left_out_of_the_url()
    {
        Nav.NavigateTo("/list");
        using var page = State.Bind("p", QueryCodecs.Int32, 1);

        page.Value = 3;
        await Until(() => Nav.Uri.Contains("p=3"), "the write");
        page.Value = 1;
        await Until(() => !Nav.Uri.Contains("p="), "the default being left out");

        Assert.Equal("http://localhost/list", Nav.Uri);
        Assert.Equal(1, page.Value);
    }

    [Fact]
    public void A_value_that_does_not_parse_falls_back_to_the_default()
    {
        Nav.NavigateTo("/list?p=abc&when=2026-13-45&level=x");
        using var page = State.Bind("p", QueryCodecs.Int32, 7);
        using var when = State.Bind("when", QueryCodecs.DateOnly, new DateOnly(2000, 1, 1));

        Assert.Equal(7, page.Value);
        Assert.Equal(new DateOnly(2000, 1, 1), when.Value);
    }

    [Fact]
    public async Task A_bound_value_reads_the_url_and_follows_back_forward_and_typed_urls()
    {
        Nav.NavigateTo("/list?q=first");
        using var q = State.Bind("q", QueryCodecs.String, "");
        var changes = 0;
        q.Changed += () => changes++;

        Assert.Equal("first", q.Value);
        Nav.NavigateTo("/list?q=second");

        Assert.Equal("second", q.Value);
        Assert.Equal(1, changes);
        Nav.NavigateTo("/list");
        Assert.Equal("", q.Value);
        Assert.Equal(2, changes);
        await Settle();
    }

    [Fact]
    public async Task Our_own_navigation_does_not_notify_again()
    {
        Nav.NavigateTo("/list");
        var seen = new List<string>();
        using var subscription = State.Subscribe("", keys => seen.AddRange(keys));

        State.Set("q", "a");
        Assert.Equal(["q"], seen);
        await Until(() => Nav.Uri.Contains("q=a"), "the flush");
        await Settle();

        // One notification for the write; the LocationChanged of our own NavigateTo adds none.
        Assert.Equal(["q"], seen);
    }

    [Fact]
    public void A_navigation_to_the_state_we_already_hold_is_not_notified()
    {
        Nav.NavigateTo("/list?q=a&p=2");
        var calls = 0;
        using var subscription = State.Subscribe("", _ => calls++);
        _ = State.Get("q");

        Nav.NavigateTo("/list?p=2&q=a");
        Nav.NavigateTo("/list?q=a&p=2");

        Assert.Equal(0, calls);
    }

    [Fact]
    public void An_external_navigation_notifies_only_the_keys_that_changed_and_only_matching_subscribers()
    {
        Nav.NavigateTo("/list?users.q=a&users.p=1&other=1");
        _ = State.Get("other");
        var users = new List<string>();
        var orders = new List<string>();
        var everything = new List<string>();
        using var a = State.Subscribe("users", keys => users.AddRange(keys));
        using var b = State.Subscribe("orders", keys => orders.AddRange(keys));
        using var c = State.Subscribe("", keys => everything.AddRange(keys));

        Nav.NavigateTo("/list?users.q=b&users.p=1&other=2");

        Assert.Equal(["users.q"], users);
        Assert.Empty(orders);
        Assert.Equal(["users.q", "other"], everything.OrderBy(k => k == "other"));
    }

    [Fact]
    public void A_prefix_matches_the_key_itself_and_its_dotted_children_but_not_a_longer_name()
    {
        Assert.True(QueryState.KeyMatches("f", "f"));
        Assert.True(QueryState.KeyMatches("f", "f.name"));
        Assert.True(QueryState.KeyMatches("f", "f.name.op"));
        Assert.False(QueryState.KeyMatches("f", "fx"));
        Assert.False(QueryState.KeyMatches("f", "g.f"));
        Assert.True(QueryState.KeyMatches("", "anything"));
    }

    [Fact]
    public void A_disposed_subscription_is_not_called_again()
    {
        Nav.NavigateTo("/list?q=a");
        _ = State.Get("q");
        var calls = 0;
        var subscription = State.Subscribe("q", _ => calls++);

        Nav.NavigateTo("/list?q=b");
        subscription.Dispose();
        Nav.NavigateTo("/list?q=c");

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Back_restores_the_previous_value_after_a_push()
    {
        Nav.NavigateTo("/list?q=old");
        using var q = State.Bind("q", QueryCodecs.String, "");

        q.Set("new", QueryHistory.Push);
        await Until(() => Nav.Uri.EndsWith("q=new"), "the push");
        await Settle();
        Assert.Equal("new", q.Value);
        // What the browser does on Back: location changes to the previous entry, not through NavigateTo of this service.
        Nav.NavigateTo("/list?q=old", new NavigationOptions { ReplaceHistoryEntry = true });

        Assert.Equal("old", q.Value);
    }

    [Fact]
    public void The_url_wins_over_a_write_that_has_not_flushed_when_it_changes_the_same_key()
    {
        Nav.NavigateTo("/list?q=a");
        using var q = State.Bind("q", QueryCodecs.String, "");

        q.Set("typed", QueryHistory.Replace);
        Assert.Equal("typed", q.Value);
        Nav.NavigateTo("/list?q=external");

        Assert.Equal("external", q.Value);
    }

    [Fact]
    public async Task Writes_to_the_page_keep_the_path_and_leave_the_state_of_other_pages_alone()
    {
        Nav.NavigateTo("/items/42?utm=1");

        State.Set("q", "a");

        await Until(() => Nav.Uri.Contains("q=a"), "the flush");
        Assert.Equal("http://localhost/items/42?utm=1&q=a", Nav.Uri);
    }

    [Fact]
    public void HrefWith_gives_a_path_and_query_for_static_links_without_touching_the_state()
    {
        Nav.NavigateTo("/list?q=a&p=2&utm=x");

        var href = State.HrefWith(("p", "3"), ("q", null), ("s", "name desc"));

        Assert.Equal("/list?p=3&utm=x&s=name%20desc", href);
        Assert.Equal("a", State.Get("q"));
        Assert.Equal("2", State.Get("p"));
    }

    [Fact]
    public void HrefWith_leaves_a_default_out_through_a_bound_value()
    {
        Nav.NavigateTo("/list?p=2");
        using var page = State.Bind("p", QueryCodecs.Int32, 1);

        Assert.Equal("/list?p=5", page.HrefWith(5));
        Assert.Equal("/list", page.HrefWith(1));
    }

    [Fact]
    public void A_claim_of_a_prefix_already_claimed_on_the_page_throws_naming_both_owners()
    {
        Nav.NavigateTo("/list");
        using var first = State.Claim("users", "UsersList");

        var error = Assert.Throws<InvalidOperationException>(() => State.Claim("users", "AuditLog"));

        Assert.Contains("UsersList", error.Message);
        Assert.Contains("AuditLog", error.Message);
        Assert.Contains("users", error.Message);
    }

    [Fact]
    public void The_empty_prefix_is_a_prefix_like_any_other()
    {
        Nav.NavigateTo("/list");
        using var first = State.Claim("", "A");

        var error = Assert.Throws<InvalidOperationException>(() => State.Claim("", "B"));

        Assert.Contains("empty prefix", error.Message);
        Assert.Contains("'A'", error.Message);
        Assert.Contains("'B'", error.Message);
    }

    [Fact]
    public void Different_prefixes_and_a_released_prefix_do_not_conflict()
    {
        Nav.NavigateTo("/list");
        var first = State.Claim("users", "A");
        using var other = State.Claim("orders", "B");
        using var empty = State.Claim("", "C");

        first.Dispose();
        using var again = State.Claim("users", "D");
    }

    [Fact]
    public void A_claim_made_on_another_page_replaces_the_one_of_the_page_being_left()
    {
        Nav.NavigateTo("/users");
        var users = State.Claim("", "UsersPage");

        // The router creates the next page before it disposes the previous one.
        Nav.NavigateTo("/orders");
        using var orders = State.Claim("", "OrdersPage");
        users.Dispose();

        // The disposal of the old page must not release the claim of the new one.
        Assert.Throws<InvalidOperationException>(() => State.Claim("", "Third"));
    }

    [Fact]
    public async Task A_group_writes_all_its_properties_in_one_navigation_and_omits_defaults()
    {
        Nav.NavigateTo("/list?utm=1");
        using var group = State.Group(new Filters(), "FilterList", prefix: "f", QueryHistory.Push)
            .Add("q", f => f.Search, (f, v) => f with { Search = v }, QueryCodecs.String)
            .Add("p", f => f.Page, (f, v) => f with { Page = v }, QueryCodecs.Int32)
            .Add("on", f => f.Archived, (f, v) => f with { Archived = v }, QueryCodecs.Boolean);
        var before = Navigations;

        group.Set(new Filters("abc", 3, true));
        await Until(() => Navigations > before, "the flush");
        await Settle();
        Assert.Equal(before + 1, Navigations);
        Assert.False(Last.Options.ReplaceHistoryEntry);
        Assert.Equal("http://localhost/list?utm=1&f.q=abc&f.p=3&f.on=true", Nav.Uri);
        Assert.Equal(new Filters("abc", 3, true), group.Value);

        group.Update(f => f with { Search = "", Archived = false });
        await Until(() => !Nav.Uri.Contains("f.on"), "the defaults being left out");
        Assert.Equal("http://localhost/list?utm=1&f.p=3", Nav.Uri);
    }

    [Fact]
    public void A_group_reads_the_url_and_falls_back_to_the_default_of_a_bad_property()
    {
        Nav.NavigateTo("/list?f.q=x&f.p=zzz&f.on=TRUE&unrelated=1");
        using var group = State.Group(new Filters("d", 9, false), "FilterList", prefix: "f")
            .Add("q", f => f.Search, (f, v) => f with { Search = v }, QueryCodecs.String)
            .Add("p", f => f.Page, (f, v) => f with { Page = v }, QueryCodecs.Int32)
            .Add("on", f => f.Archived, (f, v) => f with { Archived = v }, QueryCodecs.Boolean);

        Assert.Equal(new Filters("x", 9, true), group.Value);
    }

    [Fact]
    public void A_group_raises_Changed_for_its_keys_only()
    {
        Nav.NavigateTo("/list?f.q=a&other=1");
        using var group = State.Group(new Filters(), "FilterList", prefix: "f")
            .Add("q", f => f.Search, (f, v) => f with { Search = v }, QueryCodecs.String);
        var changes = 0;
        group.Changed += () => changes++;

        Nav.NavigateTo("/list?f.q=a&other=2");
        Assert.Equal(0, changes);
        Nav.NavigateTo("/list?f.q=b&other=2");
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Two_groups_with_the_same_prefix_throw_and_a_second_key_of_one_group_must_differ()
    {
        Nav.NavigateTo("/list");
        using var first = State.Group(new Filters(), "A", prefix: "f");

        Assert.Throws<InvalidOperationException>(() => State.Group(new Filters(), "B", prefix: "f"));
        Assert.Throws<ArgumentException>(() => first.Add("q", f => f.Search, (f, v) => f with { Search = v }, QueryCodecs.String)
            .Add("q", f => f.Search, (f, v) => f with { Search = v }, QueryCodecs.String));
    }

    [Fact]
    public void A_group_without_a_prefix_claims_the_empty_one()
    {
        Nav.NavigateTo("/list");
        using var first = State.Group(new Filters(), "A");

        Assert.Throws<InvalidOperationException>(() => State.Group(new Filters(), "B"));
        using var prefixed = State.Group(new Filters(), "C", prefix: "g");
    }

    [Fact]
    public void A_group_gives_a_link_that_carries_a_record()
    {
        Nav.NavigateTo("/list?f.q=a");
        using var group = State.Group(new Filters(), "FilterList", prefix: "f")
            .Add("q", f => f.Search, (f, v) => f with { Search = v }, QueryCodecs.String)
            .Add("p", f => f.Page, (f, v) => f with { Page = v }, QueryCodecs.Int32);

        Assert.Equal("/list?f.q=b&f.p=2", group.HrefWith(new Filters("b", 2, false)));
        Assert.Equal("/list", group.HrefWith(new Filters()));
    }

    [Fact]
    public async Task Nothing_is_written_when_the_renderer_is_not_interactive()
    {
        Nav.NavigateTo("/list?q=a");
        State.UseRenderer(new RendererInfo("Static", false));
        var before = Navigations;

        var written = State.Set("q", "b");
        await Settle();

        Assert.False(written);
        Assert.Equal(before, Navigations);
        Assert.Equal("a", State.Get("q"));
        // Reading and building links still work without interactivity.
        Assert.Equal("/list?q=c", State.HrefWith(("q", "c")));
    }

    [Fact]
    public async Task Without_a_word_about_the_renderer_a_write_that_cannot_navigate_is_dropped_quietly()
    {
        var navigation = new RefusingNavigationManager();
        navigation.Start("http://localhost/", "http://localhost/list");
        using var state = new QueryState(navigation);

        state.Set("q", "a");
        await Task.Delay(150, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(1, navigation.Attempts);
    }

    [Fact]
    public void AddBluxUiTools_registers_the_service_scoped()
    {
        var descriptor = Services.Single(d => d.ServiceType == typeof(QueryState));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public async Task An_echo_that_arrives_after_a_newer_write_does_not_bring_the_old_value_back()
    {
        var navigation = new DelayedNavigationManager();
        navigation.Start("http://localhost/", "http://localhost/list");
        using var state = new QueryState(navigation);
        state.UseRenderer(new RendererInfo("Server", true));
        var seen = new List<string>();
        using var subscription = state.Subscribe("", keys => seen.AddRange(keys));

        state.Set("q", "a");
        await Until(() => navigation.Sent.Count == 1, "the first navigation");
        state.Set("q", "ab");
        await Until(() => navigation.Sent.Count == 2, "the second navigation");
        seen.Clear();

        // The browser reports the two navigations in order, after both were sent.
        navigation.Receive(navigation.Sent[0]);
        Assert.Equal("ab", state.Get("q"));
        navigation.Receive(navigation.Sent[1]);

        Assert.Equal("ab", state.Get("q"));
        Assert.Empty(seen);
    }

    [Fact]
    public async Task Going_back_to_a_url_we_navigated_to_earlier_is_read_as_the_external_navigation_it_is()
    {
        var navigation = new DelayedNavigationManager();
        navigation.Start("http://localhost/", "http://localhost/list?q=a");
        using var state = new QueryState(navigation);
        state.UseRenderer(new RendererInfo("Server", true));
        var seen = new List<string>();
        using var subscription = state.Subscribe("", keys => seen.AddRange(keys));

        state.Set("q", "b", QueryHistory.Push);
        await Until(() => navigation.Sent.Count == 1, "the push");
        navigation.Receive(navigation.Sent[0]);
        seen.Clear();
        navigation.Receive("http://localhost/list?q=a");

        Assert.Equal("a", state.Get("q"));
        Assert.Equal(["q"], seen);
    }

    [Fact]
    public async Task A_bound_component_follows_the_url_and_writes_through_its_buttons()
    {
        Nav.NavigateTo("/list?q=start&p=2");
        var cut = Render<QueryStateProbe>();
        Assert.Equal("start", cut.Find("[data-testid=q]").TextContent);
        Assert.Equal("2", cut.Find("[data-testid=page]").TextContent);

        cut.Find("[data-testid=push]").Click();
        // The click handler writes; the component shows the new value without waiting for the URL.
        Assert.Equal("v1", cut.Find("[data-testid=q]").TextContent);
        await Until(() => Nav.Uri.Contains("q=v1"), "the push");
        Assert.False(Last.Options.ReplaceHistoryEntry);

        cut.Find("[data-testid=replace]").Click();
        await Until(() => Nav.Uri.Contains("q=v2"), "the replace");
        Assert.True(Last.Options.ReplaceHistoryEntry);

        // A typed URL, or Back.
        Nav.NavigateTo("/list?q=typed&p=9");
        cut.WaitForAssertion(() => Assert.Equal("typed", cut.Find("[data-testid=q]").TextContent));
        Assert.Equal("9", cut.Find("[data-testid=page]").TextContent);
    }

    [Fact]
    public async Task Two_writes_of_one_click_are_one_navigation_that_pushes()
    {
        Nav.NavigateTo("/list");
        var cut = Render<QueryStateProbe>();
        var before = Navigations;

        cut.Find("[data-testid=both]").Click();

        await Until(() => Navigations > before, "the flush");
        await Settle();
        Assert.Equal(before + 1, Navigations);
        Assert.False(Last.Options.ReplaceHistoryEntry);
        Assert.Equal("http://localhost/list?q=both&p=3", Nav.Uri);
    }

    [Fact]
    public async Task An_own_navigation_does_not_render_the_component_again()
    {
        Nav.NavigateTo("/list");
        var cut = Render<QueryStateProbe>();
        cut.Find("[data-testid=push]").Click();
        await Until(() => Nav.Uri.Contains("q=v1"), "the push");
        await Settle();
        var rendersAfterWrite = cut.Instance.Renders;

        // The same URL reported again (the echo, a no-op navigation): no change, no render.
        Nav.NavigateTo(Nav.Uri);
        await Settle();

        Assert.Equal(rendersAfterWrite, cut.Instance.Renders);
    }

    [Fact]
    public async Task A_disposed_service_stops_following_the_url()
    {
        Nav.NavigateTo("/list?q=a");
        var state = new QueryState(Nav);
        _ = state.Get("q");
        var calls = 0;
        using var subscription = state.Subscribe("", _ => calls++);

        state.Dispose();
        Nav.NavigateTo("/list?q=b");
        await Settle();

        Assert.Equal(0, calls);
        Assert.Throws<ObjectDisposedException>(() => state.Set("q", "c"));
    }

    private sealed record Filters(string Search = "", int Page = 1, bool Archived = false);

    /// <summary>A navigation manager that sends nothing back until the test says the browser did.</summary>
    private sealed class DelayedNavigationManager : NavigationManager
    {
        public List<string> Sent { get; } = [];

        public void Start(string baseUri, string uri) => Initialize(baseUri, uri);

        public void Receive(string uri)
        {
            Uri = uri;
            NotifyLocationChanged(false);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options) => Sent.Add(uri);
    }

    /// <summary>The navigation manager of a prerender: navigating is a redirect, which surfaces as an exception.</summary>
    private sealed class RefusingNavigationManager : NavigationManager
    {
        public int Attempts { get; private set; }

        public void Start(string baseUri, string uri) => Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Attempts++;
            throw new NavigationException(uri);
        }
    }
}
