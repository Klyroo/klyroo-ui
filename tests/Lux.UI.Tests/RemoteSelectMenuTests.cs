using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;

namespace Lux.UI.Tests;

/// <summary>LxSelectMenu with an OptionsProvider: the markup of each state, what the browser's events do, and static rendering.</summary>
public class RemoteSelectMenuTests : LuxTestContext
{
    private static readonly char Sep = '\u001f';

    private sealed class Fake
    {
        public List<LxOptionsRequest> Requests { get; } = [];
        public List<TaskCompletionSource> Gates { get; } = [];
        public bool Hold { get; set; }
        public bool Fail { get; set; }
        public int Total { get; set; } = 40;

        public async ValueTask<LxOptionsResult> Load(LxOptionsRequest request, CancellationToken ct)
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
            var matches = Enumerable.Range(1, Total).Select(i => $"Item {i}")
                .Where(l => request.Search.Length == 0 || l.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            var page = matches.Skip(request.Skip).Take(request.Take)
                .Select(l => new LxSelectItem("i" + l[5..], l) { Icon = request.Search == "icon" ? "star" : null }).ToList();
            return new LxOptionsResult(page, HasMore: request.Skip + page.Count < matches.Count);
        }
    }

    private IRenderedComponent<LxSelectMenu> Open(Fake fake, Action<ComponentParameterCollectionBuilder<LxSelectMenu>>? more = null) =>
        Render<LxSelectMenu>(p =>
        {
            p.Add(s => s.OptionsProvider, fake.Load).Add(s => s.PageSize, 10);
            more?.Invoke(p);
        });

    private static Task Search(IRenderedComponent<LxSelectMenu> cut, string text) =>
        cut.Find("lx-select-menu").TriggerEventAsync("onbxsearch", new LxSearchEventArgs { Value = text, Sequence = 1 });

    private static string[] Labels(IRenderedComponent<LxSelectMenu> cut) => [.. cut.FindAll("[role=option]:not([data-slot=create])").Select(o => o.GetAttribute("data-label")!)];

    [Fact]
    public void Nothing_is_requested_before_the_list_opens_and_the_browser_is_told_it_is_remote()
    {
        var fake = new Fake();
        var cut = Open(fake);

        Assert.Empty(fake.Requests);
        var host = cut.Find("lx-select-menu");
        Assert.True(host.HasAttribute("data-remote"));
        Assert.Equal("250", host.GetAttribute("data-debounce"));
        Assert.Empty(cut.FindAll("[role=option]"));
        // An empty listbox is not exposed: it would own no options.
        Assert.True(cut.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.False(cut.Find("[data-slot=trigger]").HasAttribute("disabled"));
        Assert.Equal("0", cut.Find("[role=listbox]").GetAttribute("data-generation"));
    }

    [Fact]
    public async Task The_first_report_loads_a_page_and_renders_the_options_with_a_sentinel_for_the_next_page()
    {
        var fake = new Fake();
        var cut = Open(fake);

        await Search(cut, "");

        Assert.Equal(10, Labels(cut).Length);
        Assert.Equal("Item 1", Labels(cut)[0]);
        Assert.False(cut.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.False(cut.Find("[role=listbox]").HasAttribute("aria-busy"));
        // The scrolled area holds the listbox and, beside it, the sentinel the next page is loaded by.
        var scroller = cut.Find("[data-slot=scroller]");
        Assert.NotNull(scroller.QuerySelector("[role=listbox]"));
        var more = scroller.QuerySelector("lx-infinite-scroll");
        Assert.NotNull(more);
        Assert.Equal("#" + scroller.Id, more.GetAttribute("data-root"));
        Assert.Equal("0px 0px 100% 0px", more.GetAttribute("data-root-margin"));
        Assert.True(more.HasAttribute("data-has-more"));
        Assert.Empty(cut.Find("[role=listbox]").QuerySelectorAll("lx-infinite-scroll"));
        Assert.Equal("1", cut.Find("[role=listbox]").GetAttribute("data-generation"));
    }

    [Fact]
    public async Task Loading_is_shown_busy_and_announced_and_the_list_is_replaced_when_it_ends()
    {
        var fake = new Fake { Hold = true };
        var cut = Open(fake);

        var pending = Search(cut, "");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-busy"));
            Assert.Equal("Loading...", cut.Find("[data-slot=announcer]").TextContent);
            Assert.Equal("Loading...", cut.Find("[data-slot=loading]").TextContent.Trim());
            Assert.Equal("true", cut.Find("[data-slot=loading]").GetAttribute("aria-hidden"));
        });

        fake.Gates[0].SetResult();
        await pending;

        Assert.Empty(cut.FindAll("[data-slot=loading]"));
        Assert.Equal("", cut.Find("[data-slot=announcer]").TextContent);
        Assert.False(cut.Find("[role=listbox]").HasAttribute("aria-busy"));
    }

    [Fact]
    public async Task The_status_is_a_polite_live_region()
    {
        var cut = Open(new Fake());
        await Search(cut, "");

        var status = cut.Find("[data-slot=announcer]");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("polite", status.GetAttribute("aria-live"));
        Assert.Contains("sr-only", status.ClassName);
    }

    [Fact]
    public async Task No_match_shows_and_announces_the_no_results_text_beside_a_hidden_listbox()
    {
        var cut = Open(new Fake());

        await Search(cut, "zzz");

        Assert.Equal("No results", cut.Find("[data-slot=empty]").TextContent);
        Assert.Equal("No results", cut.Find("[data-slot=announcer]").TextContent);
        Assert.True(cut.Find("[role=listbox]").HasAttribute("hidden"));
        // Nothing is left to load.
        Assert.Empty(cut.FindAll("lx-infinite-scroll"));
    }

    [Fact]
    public async Task With_Create_the_typed_value_is_offered_instead_of_no_results()
    {
        var cut = Open(new Fake(), p => p.Add(s => s.Create, true));

        await Search(cut, "zzz");

        Assert.Empty(cut.FindAll("[data-slot=empty]"));
        Assert.False(cut.Find("[role=listbox]").HasAttribute("hidden"));
        Assert.NotNull(cut.Find("[data-slot=create]"));
    }

    [Fact]
    public async Task The_text_is_trimmed_and_MinLength_shows_a_hint_instead_of_asking()
    {
        var fake = new Fake();
        var cut = Open(fake, p => p.Add(s => s.MinLength, 3));

        await Search(cut, " ab ");

        Assert.Empty(fake.Requests);
        Assert.Equal("Type at least 3 characters to search.", cut.Find("[data-slot=hint]").TextContent);
        Assert.Equal("Type at least 3 characters to search.", cut.Find("[data-slot=announcer]").TextContent);

        await Search(cut, " abc ");

        Assert.Equal("abc", fake.Requests[0].Search);
        Assert.Empty(cut.FindAll("[data-slot=hint]"));
    }

    [Fact]
    public async Task A_failure_shows_a_translated_alert_with_a_retry_button_that_asks_again()
    {
        var fake = new Fake { Fail = true };
        var cut = Open(fake);

        await Search(cut, "");

        var alert = cut.Find("[role=alert]");
        Assert.Contains("The options could not be loaded.", alert.TextContent);
        var retry = cut.Find("[data-slot=retry]");
        Assert.Equal("Retry", retry.TextContent.Trim());
        Assert.Equal("button", retry.LocalName);

        fake.Fail = false;
        await retry.ClickAsync(new());

        Assert.Equal(2, fake.Requests.Count);
        Assert.Empty(cut.FindAll("[role=alert]"));
        Assert.Equal(10, Labels(cut).Length);
    }

    [Fact]
    public async Task The_sentinel_loads_the_next_page_and_stops_when_the_source_has_no_more()
    {
        var fake = new Fake { Total = 25 };
        var cut = Open(fake);
        await Search(cut, "");

        await cut.Find("lx-infinite-scroll").TriggerEventAsync("onbxchange", new LxChangeEventArgs { Value = "more", Sequence = 1 });
        Assert.Equal(20, Labels(cut).Length);
        Assert.Equal(new LxOptionsRequest("", 10, 10, null), fake.Requests[1]);

        await cut.Find("lx-infinite-scroll").TriggerEventAsync("onbxchange", new LxChangeEventArgs { Value = "more", Sequence = 2 });
        Assert.Equal(25, Labels(cut).Length);

        var end = cut.Find("lx-infinite-scroll");
        Assert.False(end.HasAttribute("data-has-more"));
        Assert.Equal("No more items", cut.Find("lx-infinite-scroll [data-slot=status]").TextContent);
    }

    [Fact]
    public async Task A_report_while_the_list_is_replaced_for_a_new_term_does_not_mix_pages()
    {
        var fake = new Fake();
        var cut = Open(fake);
        await Search(cut, "");
        fake.Hold = true;

        var pending = Search(cut, "Item 3");
        fake.Hold = false;
        await Search(cut, "Item 2");
        fake.Gates[0].TrySetResult();
        await pending;

        Assert.All(Labels(cut), l => Assert.Contains("Item 2", l));
    }

    [Fact]
    public async Task Selected_values_show_their_label_and_hand_it_to_the_browser_after_the_list_changes()
    {
        var fake = new Fake();
        IReadOnlyList<string> value = ["i3"];
        var cut = Open(fake, p => p.Add(s => s.Value, value).Add(s => s.ValueChanged, (IReadOnlyList<string>? v) => value = v ?? []));
        // Not listed yet: the raw value.
        Assert.Equal("i3", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Null(cut.Find("lx-select-menu").GetAttribute("data-known"));

        await Search(cut, "");
        Assert.Equal("Item 3", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal("i3" + Sep + "Item 3", cut.Find("lx-select-menu").GetAttribute("data-known"));

        await Search(cut, "Item 4");
        Assert.DoesNotContain("Item 3", Labels(cut));
        Assert.Equal("Item 3", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal("i3" + Sep + "Item 3", cut.Find("lx-select-menu").GetAttribute("data-known"));
    }

    [Fact]
    public void ResolveSelected_names_values_that_were_never_listed_when_the_component_is_interactive()
    {
        var asked = new List<string[]>();
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load)
            .Add(s => s.Multiple, true)
            .Add(s => s.Value, ["i7", "i9"])
            .Add(s => s.ResolveSelected, (values, _) =>
            {
                asked.Add([.. values]);
                return new ValueTask<IReadOnlyList<LxSelectItem>>([new LxSelectItem("i7", "Item seven")]);
            }));

        cut.WaitForAssertion(() => Assert.Equal("Item seven, i9", cut.Find("[data-slot=label]").GetAttribute("data-label")));
        Assert.Single(asked);
        Assert.Equal(["i7", "i9"], asked[0]);
    }

    [Fact]
    public void Static_Items_name_a_value_without_listing_it()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load)
            .Add(s => s.Items, [new LxSelectItem("x", "Known label")]).Add(s => s.Value, ["x"]));

        Assert.Equal("Known label", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Empty(cut.FindAll("[role=option]"));
        Assert.Equal("x" + Sep + "Known label", cut.Find("lx-select-menu").GetAttribute("data-known"));
    }

    [Fact]
    public async Task Icons_group_headings_and_counts_of_loaded_options_render_like_static_ones()
    {
        var provider = new LxOptionsProvider((_, _) => new ValueTask<LxOptionsResult>(new LxOptionsResult(
        [
            new LxSelectItem("a", "Alpha") { Group = "Letters", Count = 1200, Icon = "star" },
            new LxSelectItem("b", "Beta") { Group = "Letters" },
        ])));
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, provider));

        await Search(cut, "");

        Assert.Equal("Letters", cut.Find("[role=group] [data-slot=group-label]").TextContent);
        Assert.Equal("1,200", cut.Find("[data-slot=count]").TextContent);
        Assert.NotNull(cut.Find("[data-has-icon]").QuerySelector("svg"));
        Assert.NotNull(cut.Find("[data-slot=selectedIcon]"));
    }

    [Fact]
    public async Task Debounce_and_PageSize_reach_the_browser_and_the_provider()
    {
        var fake = new Fake();
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, fake.Load).Add(s => s.Debounce, 400).Add(s => s.PageSize, 7));

        await Search(cut, "");

        Assert.Equal("400", cut.Find("lx-select-menu").GetAttribute("data-debounce"));
        Assert.Equal(7, fake.Requests[0].Take);
        Assert.Equal(7, Labels(cut).Length);
    }

    [Fact]
    public async Task A_parent_rendering_again_keeps_the_loaded_options()
    {
        var fake = new Fake();
        var cut = Open(fake);
        await Search(cut, "");

        cut.Render(p => p.Add(s => s.OptionsProvider, new LxOptionsProvider(fake.Load)).Add(s => s.Placeholder, "Pick"));

        Assert.Equal(10, Labels(cut).Length);
        Assert.Single(fake.Requests);
    }

    [Fact]
    public async Task Slots_of_the_states_take_classes_from_Ui()
    {
        var fake = new Fake { Fail = true };
        var cut = Open(fake, p => p.Add(s => s.Ui, new SelectMenuUi { Scroller = "scroller-x", Error = "error-x", Announcer = "announcer-x" }));

        await Search(cut, "");

        Assert.Contains("scroller-x", cut.Find("[data-slot=scroller]").ClassName);
        Assert.Contains("error-x", cut.Find("[data-slot=error]").ClassName);
        Assert.Contains("announcer-x", cut.Find("[data-slot=announcer]").ClassName);
        // Remote, the list scrolls in the scroller and the listbox does not.
        Assert.Contains("overflow-y-auto", cut.Find("[data-slot=scroller]").ClassName);
        Assert.DoesNotContain("overflow-y-auto", cut.Find("[role=listbox]").ClassName);
    }

    [Fact]
    public void Without_a_provider_nothing_changes_the_list_scrolls_by_itself_and_the_browser_filters()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, [new LxSelectItem("a", "Alpha")]));

        var host = cut.Find("lx-select-menu");
        Assert.False(host.HasAttribute("data-remote"));
        Assert.False(host.HasAttribute("data-debounce"));
        Assert.False(host.HasAttribute("data-known"));
        Assert.Empty(cut.FindAll("[data-slot=scroller]"));
        Assert.Contains("overflow-y-auto", cut.Find("[role=listbox]").ClassName);
        Assert.False(cut.Find("[role=listbox]").HasAttribute("data-generation"));
        Assert.NotNull(cut.Find("[data-slot=empty]"));
    }

    [Fact]
    public void Statically_rendered_the_field_shows_its_values_posts_them_and_cannot_open_an_empty_list()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var fake = new Fake();
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, fake.Load).Add(s => s.Name, "pick")
            .Add(s => s.Value, ["i3"]).Add(s => s.Placeholder, "Pick"));

        // The provider is never called without a runtime to hear the browser.
        Assert.Empty(fake.Requests);
        var host = cut.Find("lx-select-menu");
        Assert.False(host.HasAttribute("data-remote"));
        Assert.Equal("i3", host.GetAttribute("data-values"));
        Assert.Equal("pick", host.GetAttribute("name"));
        Assert.False(host.HasAttribute("disabled"));
        Assert.Equal("i3", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.True(cut.Find("[data-slot=trigger]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[role=option]"));
        Assert.Empty(cut.FindAll("[data-slot=scroller]"));
        Assert.True(cut.Find("[role=listbox]").HasAttribute("hidden"));
    }

    [Fact]
    public void Statically_rendered_without_a_provider_the_trigger_stays_enabled()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, [new LxSelectItem("a", "Alpha")]));

        Assert.False(cut.Find("[data-slot=trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public void Without_Items_or_OptionsProvider_the_first_render_throws_and_names_both_parameters()
    {
        var error = Assert.ThrowsAny<Exception>(() => Render<LxSelectMenu>());

        var message = (error.InnerException ?? error).Message;
        Assert.Contains("LxSelectMenu", message);
        Assert.Contains("Items", message);
        Assert.Contains("OptionsProvider", message);
    }

    [Fact]
    public void An_empty_Items_list_is_a_set_Items()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, []));

        Assert.NotNull(cut.Find("lx-select-menu"));
    }

    [Fact]
    public void Statically_rendered_the_labels_of_Items_travel_to_the_browser_too()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load).Add(s => s.Items, [new LxSelectItem("fr", "France")]).Add(s => s.Value, ["fr"]));

        // The browser paints from data-known, so it keeps France instead of writing the raw value over it.
        Assert.Equal("fr" + Sep + "France", cut.Find("lx-select-menu").GetAttribute("data-known"));
        Assert.Equal("France", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal("0", cut.Find("lx-select-menu").GetAttribute("data-epoch"));
    }

    [Fact]
    public async Task A_text_that_is_already_a_value_shows_no_results_and_no_create_option()
    {
        var cut = Open(new Fake(), p => p.Add(s => s.Create, true).Add(s => s.Multiple, true).Add(s => s.Value, ["zork"]));

        await Search(cut, "zork");
        Assert.Equal("No results", cut.Find("[data-slot=empty]").TextContent);

        await Search(cut, "zork2");
        Assert.Empty(cut.FindAll("[data-slot=empty]"));
    }

    [Fact]
    public async Task OptionsKey_changing_resets_the_list_the_labels_and_asks_again_but_a_new_delegate_does_not()
    {
        var fake = new Fake();
        var resolved = 0;
        LxSelectedOptionsResolver resolver = (values, _) =>
        {
            resolved++;
            return new ValueTask<IReadOnlyList<LxSelectItem>>([new LxSelectItem("i7", "Item seven")]);
        };
        var cut = Open(fake, p => p.Add(s => s.OptionsKey, "fr").Add(s => s.ResolveSelected, resolver).Add(s => s.Value, ["i7"]));
        cut.WaitForAssertion(() => Assert.Equal("Item seven", cut.Find("[data-slot=label]").GetAttribute("data-label")));
        await Search(cut, "");
        Assert.Equal(10, Labels(cut).Length);
        Assert.Equal(1, resolved);

        cut.Render(p => p.Add(s => s.OptionsProvider, new LxOptionsProvider(fake.Load)).Add(s => s.OptionsKey, "fr").Add(s => s.ResolveSelected, resolver).Add(s => s.Value, ["i7"]));
        Assert.Equal(10, Labels(cut).Length);
        Assert.Equal(1, resolved);
        var epoch = cut.Find("lx-select-menu").GetAttribute("data-epoch");

        cut.Render(p => p.Add(s => s.OptionsProvider, new LxOptionsProvider(fake.Load)).Add(s => s.OptionsKey, "de").Add(s => s.ResolveSelected, resolver).Add(s => s.Value, ["i7"]));

        Assert.Empty(Labels(cut));
        Assert.NotEqual(epoch, cut.Find("lx-select-menu").GetAttribute("data-epoch"));
        cut.WaitForAssertion(() => Assert.Equal(2, resolved));
        var requests = fake.Requests.Count;
        await Search(cut, "");
        Assert.Equal(requests + 1, fake.Requests.Count);
        Assert.Equal(10, Labels(cut).Length);
    }

    [Fact]
    public void A_resolution_that_threw_is_tried_again_when_the_list_opens()
    {
        var calls = 0;
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, new Fake().Load).Add(s => s.Value, ["i7"])
            .Add(s => s.ResolveSelected, (_, _) =>
            {
                calls++;
                return calls == 1 ? throw new InvalidOperationException("down") : new ValueTask<IReadOnlyList<LxSelectItem>>([new LxSelectItem("i7", "Item seven")]);
            }));
        cut.WaitForAssertion(() => Assert.Equal(1, calls));
        Assert.Equal("i7", cut.Find("[data-slot=label]").GetAttribute("data-label"));

        // Typing is not opening: only the empty text the list reports when it opens retries.
        _ = Search(cut, "x");
        Assert.Equal(1, calls);
        _ = Search(cut, "");

        cut.WaitForAssertion(() => Assert.Equal("Item seven", cut.Find("[data-slot=label]").GetAttribute("data-label")));
        Assert.Equal(2, calls);
    }
}
