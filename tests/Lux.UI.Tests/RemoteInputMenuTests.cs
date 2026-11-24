using Microsoft.AspNetCore.Components;

namespace Lux.UI.Tests;

/// <summary>BxInputMenu with an OptionsProvider: the browser's search events, the states, strict values restored by ResolveSelected, and static rendering.</summary>
public class RemoteInputMenuTests : BluxTestContext
{
    private static readonly char Sep = '\u001f';

    private sealed class Fake
    {
        public List<BxOptionsRequest> Requests { get; } = [];
        public bool Fail { get; set; }

        public ValueTask<BxOptionsResult> Load(BxOptionsRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            if (Fail) throw new InvalidOperationException("boom");
            var matches = Enumerable.Range(1, 30).Select(i => $"City {i}")
                .Where(l => request.Search.Length == 0 || l.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            var page = matches.Skip(request.Skip).Take(request.Take).Select(l => new BxSelectItem("c" + l[5..], l)).ToList();
            return new ValueTask<BxOptionsResult>(new BxOptionsResult(page, HasMore: request.Skip + page.Count < matches.Count));
        }
    }

    private IRenderedComponent<BxInputMenu> Open(Fake fake, Action<ComponentParameterCollectionBuilder<BxInputMenu>>? more = null) =>
        Render<BxInputMenu>(p =>
        {
            p.Add(s => s.OptionsProvider, fake.Load);
            more?.Invoke(p);
        });

    private static Task Search(IRenderedComponent<BxInputMenu> cut, string text) =>
        cut.Find("bx-input-menu").TriggerEventAsync("onbxsearch", new BxSearchEventArgs { Value = text, Sequence = 1 });

    private static string[] Labels(IRenderedComponent<BxInputMenu> cut) => [.. cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")!)];

    [Fact]
    public void Nothing_is_requested_before_the_browser_reports_and_the_browser_is_told_it_is_remote()
    {
        var fake = new Fake();
        var cut = Open(fake, p => p.Add(s => s.PageSize, 10));

        Assert.Empty(fake.Requests);
        var host = cut.Find("bx-input-menu");
        Assert.True(host.HasAttribute("data-remote"));
        Assert.False(host.HasAttribute("data-inert-list"));
        Assert.Equal("250", host.GetAttribute("data-debounce"));
        Assert.True(cut.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.Equal("Suggestions", cut.Find("[role=listbox]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task A_report_loads_one_page_beside_a_sentinel_and_the_scroll_loads_the_next()
    {
        var fake = new Fake();
        var cut = Open(fake, p => p.Add(s => s.PageSize, 10));

        await Search(cut, " City 1 ");

        // Trimmed; City 1, 10..19 match.
        Assert.Equal("City 1", fake.Requests[0].Search);
        Assert.Equal(10, Labels(cut).Length);
        var scroller = cut.Find("[data-slot=scroller]");
        Assert.NotNull(scroller.QuerySelector("[role=listbox]"));
        var more = scroller.QuerySelector("bx-infinite-scroll");
        Assert.NotNull(more);
        Assert.Equal("#" + scroller.Id, more.GetAttribute("data-root"));

        await cut.Find("bx-infinite-scroll").TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "more", Sequence = 1 });

        Assert.Equal(11, Labels(cut).Length);
        Assert.Equal(new BxOptionsRequest("City 1", 10, 10, null), fake.Requests[1]);
        Assert.False(cut.Find("bx-infinite-scroll").HasAttribute("data-has-more"));
    }

    [Fact]
    public async Task No_match_and_a_short_text_are_shown_and_announced()
    {
        var fake = new Fake();
        var cut = Open(fake, p => p.Add(s => s.MinLength, 3));

        await Search(cut, "zz");
        Assert.Empty(fake.Requests);
        Assert.Equal("Type at least 3 characters to search.", cut.Find("[data-slot=hint]").TextContent);
        Assert.Equal("Type at least 3 characters to search.", cut.Find("[data-slot=announcer]").TextContent);

        await Search(cut, "zzz");
        Assert.Equal("No results", cut.Find("[data-slot=empty]").TextContent);
        Assert.Equal("No results", cut.Find("[data-slot=announcer]").TextContent);
        Assert.Equal("polite", cut.Find("[data-slot=announcer]").GetAttribute("aria-live"));
    }

    [Fact]
    public async Task A_failure_shows_a_translated_alert_with_a_retry_button()
    {
        var fake = new Fake { Fail = true };
        var cut = Open(fake);

        await Search(cut, "");

        Assert.Contains("The options could not be loaded.", cut.Find("[role=alert]").TextContent);
        fake.Fail = false;
        await cut.Find("[data-slot=retry]").ClickAsync(new());

        Assert.Equal(2, fake.Requests.Count);
        Assert.Empty(cut.FindAll("[role=alert]"));
        Assert.Equal(25, Labels(cut).Length);
    }

    [Fact]
    public async Task A_strict_value_shows_its_label_once_listed_and_keeps_it_after_a_search_drops_it()
    {
        var fake = new Fake();
        var cut = Open(fake, p => p.Add(s => s.Strict, true).Add(s => s.Value, "c3"));
        // Not listed yet: the raw value.
        Assert.Equal("c3", cut.Find("[data-slot=input]").GetAttribute("value"));

        await Search(cut, "");
        Assert.Equal("City 3", cut.Find("[data-slot=input]").GetAttribute("value"));
        Assert.Equal("true", cut.FindAll("[role=option]").First(o => o.GetAttribute("data-value") == "c3").GetAttribute("aria-selected"));
        Assert.Equal("c3" + Sep + "City 3", cut.Find("bx-input-menu").GetAttribute("data-known"));

        await Search(cut, "City 2");
        Assert.DoesNotContain("City 3", Labels(cut));
        Assert.Equal("City 3", cut.Find("[data-slot=input]").GetAttribute("value"));
        Assert.Equal("c3", cut.Find("input[type=hidden]").GetAttribute("value"));
    }

    [Fact]
    public void ResolveSelected_names_a_strict_value_that_was_never_listed()
    {
        var asked = new List<string[]>();
        var cut = Render<BxInputMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load).Add(s => s.Strict, true).Add(s => s.Value, "c7")
            .Add(s => s.ResolveSelected, (values, _) =>
            {
                asked.Add([.. values]);
                return new ValueTask<IReadOnlyList<BxSelectItem>>([new BxSelectItem("c7", "City seven")]);
            }));

        cut.WaitForAssertion(() => Assert.Equal("City seven", cut.Find("[data-slot=input]").GetAttribute("value")));
        Assert.Equal("c7", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Single(asked);
        Assert.Equal(["c7"], asked[0]);
    }

    [Fact]
    public void A_free_text_value_is_not_resolved_and_shows_as_typed()
    {
        var called = false;
        var cut = Render<BxInputMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load).Add(s => s.Value, "typed text")
            .Add(s => s.ResolveSelected, (_, _) =>
            {
                called = true;
                return new ValueTask<IReadOnlyList<BxSelectItem>>([]);
            }));

        Assert.Equal("typed text", cut.Find("[data-slot=input]").GetAttribute("value"));
        Assert.False(called);
        Assert.False(cut.Find("bx-input-menu").HasAttribute("data-known"));
    }

    [Fact]
    public void Without_a_provider_nothing_changes_the_browser_filters_and_the_list_scrolls_by_itself()
    {
        var cut = Render<BxInputMenu>(p => p.Add(s => s.Items, [new BxSelectItem("a", "Alpha")]));

        var host = cut.Find("bx-input-menu");
        Assert.False(host.HasAttribute("data-remote"));
        Assert.False(host.HasAttribute("data-inert-list"));
        Assert.Empty(cut.FindAll("[data-slot=scroller]"));
        Assert.Contains("overflow-y-auto", cut.Find("[role=listbox]").ClassName);
        Assert.False(cut.Find("[role=listbox]").HasAttribute("data-generation"));
        Assert.NotNull(cut.Find("[data-slot=empty]"));
        Assert.False(cut.Find("[data-slot=input]").HasAttribute("readonly"));
    }

    [Fact]
    public void Statically_rendered_a_free_text_field_is_a_plain_field_and_never_opens_a_list()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var fake = new Fake();
        var cut = Render<BxInputMenu>(p => p.Add(s => s.OptionsProvider, fake.Load).Add(s => s.Name, "city").Add(s => s.Value, "Par"));

        Assert.Empty(fake.Requests);
        var host = cut.Find("bx-input-menu");
        Assert.False(host.HasAttribute("data-remote"));
        // The browser side must not pop an empty list open.
        Assert.True(host.HasAttribute("data-inert-list"));
        var input = cut.Find("[data-slot=input]");
        Assert.Equal("Par", input.GetAttribute("value"));
        Assert.False(input.HasAttribute("readonly"));
        Assert.Equal("Par", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-slot=scroller]"));
    }

    [Fact]
    public void Statically_rendered_a_strict_field_shows_its_value_read_only_and_still_posts_it()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<BxInputMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load).Add(s => s.Strict, true).Add(s => s.Name, "city").Add(s => s.Value, "c3"));

        var input = cut.Find("[data-slot=input]");
        Assert.Equal("c3", input.GetAttribute("value"));
        Assert.True(input.HasAttribute("readonly"));
        Assert.Equal("c3", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("city", cut.Find("input[type=hidden]").GetAttribute("name"));
    }

    [Fact]
    public async Task Slots_of_the_states_take_classes_from_Ui()
    {
        var fake = new Fake { Fail = true };
        var cut = Open(fake, p => p.Add(s => s.Ui, new InputMenuUi { Scroller = "scroller-x", Error = "error-x" }));

        await Search(cut, "");

        Assert.Contains("scroller-x", cut.Find("[data-slot=scroller]").ClassName);
        Assert.Contains("error-x", cut.Find("[data-slot=error]").ClassName);
        Assert.Contains("overflow-y-auto", cut.Find("[data-slot=scroller]").ClassName);
        Assert.DoesNotContain("overflow-y-auto", cut.Find("[role=listbox]").ClassName);
    }

    [Fact]
    public void Without_Items_or_OptionsProvider_the_first_render_throws_and_names_both_parameters()
    {
        var error = Assert.ThrowsAny<Exception>(() => Render<BxInputMenu>());

        var message = (error.InnerException ?? error).Message;
        Assert.Contains("BxInputMenu", message);
        Assert.Contains("Items", message);
        Assert.Contains("OptionsProvider", message);
    }

    [Fact]
    public void Statically_rendered_a_strict_value_named_by_Items_travels_to_the_browser_with_its_label()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<BxInputMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load).Add(s => s.Items, [new BxSelectItem("fr", "France")]).Add(s => s.Strict, true).Add(s => s.Value, "fr"));

        Assert.Equal("France", cut.Find("[data-slot=input]").GetAttribute("value"));
        Assert.Equal("fr" + Sep + "France", cut.Find("bx-input-menu").GetAttribute("data-known"));
    }

    [Fact]
    public async Task OptionsKey_changing_resets_the_suggestions_and_a_new_delegate_does_not()
    {
        var fake = new Fake();
        var cut = Open(fake, p => p.Add(s => s.OptionsKey, "a"));
        await Search(cut, "");
        Assert.Equal(25, Labels(cut).Length);

        cut.Render(p => p.Add(s => s.OptionsProvider, new BxOptionsProvider(fake.Load)).Add(s => s.OptionsKey, "a"));
        Assert.Equal(25, Labels(cut).Length);

        cut.Render(p => p.Add(s => s.OptionsProvider, new BxOptionsProvider(fake.Load)).Add(s => s.OptionsKey, "b"));
        Assert.Empty(Labels(cut));
        var requests = fake.Requests.Count;

        await Search(cut, "");
        Assert.Equal(requests + 1, fake.Requests.Count);
        Assert.Equal(25, Labels(cut).Length);
    }
}
