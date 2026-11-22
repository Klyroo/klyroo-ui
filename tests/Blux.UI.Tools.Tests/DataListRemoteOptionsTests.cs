using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// An option filter whose options come from a remote source: the first page when the editor is shown, the search, the next page, the error with a retry, the labels of the values of the address and of
/// the options that were picked, the counts of the options that are listed, and static rendering (what the server can resolve).
/// </summary>
public partial class DataListRemoteOptionsTests : BluxToolsTestContext
{
    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    private static string Text(IElement element) => Spaces().Replace(element.TextContent, " ").Trim();

    /// <summary>Five people the remote source knows. Tickets belong to ann, bob and cyd only.</summary>
    internal sealed class Source
    {
        private static readonly (string Value, string Label)[] People =
            [("ann", "Ann Lee"), ("bob", "Bob Ray"), ("cyd", "Cyd Moss"), ("dee", "Dee Fox"), ("eve", "Eve Park")];

        public List<DataListOptionsRequest> Requests { get; } = [];

        public List<string[]> Resolved { get; } = [];

        public bool Fail { get; set; }

        public bool FailResolve { get; set; }

        public Func<IReadOnlyList<string>, Task>? BeforeResolve { get; set; }

        public DataListOptionsProvider Provider => (request, _) =>
        {
            lock (Requests) Requests.Add(request);
            if (Fail) throw new InvalidOperationException("the source is down");
            var matching = People.Where(p => request.Search.Length == 0 || p.Label.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            var page = matching.Skip(request.Skip).Take(request.Take).Select(p => new DataListFilterOption(p.Value, p.Label)).ToList();
            return ValueTask.FromResult(new DataListOptionsPage(page, HasMore: request.Skip + request.Take < matching.Count));
        };

        public DataListOptionsResolver Resolver => async (values, _) =>
        {
            lock (Resolved) Resolved.Add([.. values]);
            if (BeforeResolve is not null) await BeforeResolve(values);
            if (FailResolve) throw new InvalidOperationException("the resolver is down");
            IReadOnlyList<DataListFilterOption> found = [.. People.Where(p => values.Contains(p.Value)).Select(p => new DataListFilterOption(p.Value, p.Label))];
            return found;
        };
    }

    internal static DataListSchema<Ticket> Schema(Source source, bool facets = false, int pageSize = 2, int minLength = 0, Func<DataListFilter, DataListFilter>? more = null)
    {
        var owner = DataListFilter.Option().WithRemoteOptions(source.Provider, source.Resolver, pageSize, minLength);
        if (more is not null) owner = more(owner);
        return DataListFacetTests.Build(DataListFilter.Option(DataListFacetTests.StatusOptions), facets ? owner.WithFacets() : owner, DataListFilter.Boolean());
    }

    private IRenderedComponent<DataListFilterHarness> RenderList(string url, DataListSchema<Ticket> schema)
    {
        Navigation.NavigateTo(url);
        return Render<DataListFilterHarness>(p => p.Add(h => h.Schema, schema));
    }

    private static Task Open(IRenderedComponent<DataListFilterHarness> cut, string key) =>
        cut.Find($"li[data-filter={key}] bx-popover").TriggerEventAsync("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 1 });

    private static IElement Editor(IRenderedComponent<DataListFilterHarness> cut, string key) => cut.Find($"li[data-filter={key}] [data-slot=filter-editor]");

    private static string[] Options(IElement editor) =>
        [.. editor.QuerySelectorAll("[data-slot=filter-options] > li[data-value]").Select(li => Text(li.QuerySelector("label")!))];

    private static string[] Counts(IElement editor) =>
        [.. editor.QuerySelectorAll("[data-slot=filter-options] > li[data-value]").Select(li => li.QuerySelector("[data-slot=filter-option-count]") is { } c ? Text(c).TrimStart(',').Trim() : "")];

    private static string[] Tags(IRenderedComponent<DataListFilterHarness> cut) =>
        [.. cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text)];

    private static Task Type(IElement input, string text) =>
        input.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = text, Sequence = int.Parse(input.GetAttribute("data-bx-seq") ?? "0") + 1 });

    private static IElement Search(IRenderedComponent<DataListFilterHarness> cut) => Editor(cut, "owner").QuerySelector("input[data-slot=filter-options-search]")!;

    // ---- loading

    [Fact]
    public async Task The_first_page_loads_when_the_editor_is_shown_and_not_before()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source));

        // The popover is closed: the page did not call the source.
        Assert.Empty(source.Requests);

        await Open(cut, "owner");

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray"], Options(Editor(cut, "owner"))));
        Assert.Equal([new DataListOptionsRequest("", 0, 2, null)], source.Requests);
    }

    [Fact]
    public async Task The_next_page_loads_when_the_list_asks_for_it()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source));
        await Open(cut, "owner");
        cut.WaitForAssertion(() => Assert.Equal(2, Options(Editor(cut, "owner")).Length));

        await Editor(cut, "owner").QuerySelector("bx-infinite-scroll")!.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Sequence = 1 });

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray", "Cyd Moss", "Dee Fox"], Options(Editor(cut, "owner"))));
        Assert.Equal(new DataListOptionsRequest("", 2, 2, null), source.Requests[1]);
    }

    [Fact]
    public async Task Typing_searches_the_source_and_replaces_the_list()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source, pageSize: 5));
        await Open(cut, "owner");
        cut.WaitForAssertion(() => Assert.Equal(5, Options(Editor(cut, "owner")).Length));

        await Type(Search(cut), "ee");

        // "ee" is in Lee and Dee.
        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Dee Fox"], Options(Editor(cut, "owner"))));
        Assert.Equal(new DataListOptionsRequest("ee", 0, 5, null), source.Requests.Last());
    }

    [Fact]
    public async Task A_search_that_matches_nothing_says_so()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source));
        await Open(cut, "owner");
        cut.WaitForAssertion(() => Assert.Equal(2, Options(Editor(cut, "owner")).Length));

        await Type(Search(cut), "zzz");

        // The selected value (ann) stays in the list so that it can be taken off.
        cut.WaitForAssertion(() => Assert.Equal("No option matches your search", Text(Editor(cut, "owner").QuerySelector("[data-slot=filter-empty]")!)));
    }

    [Fact]
    public async Task A_search_shorter_than_the_minimum_is_not_sent()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source, minLength: 3));
        await Open(cut, "owner");
        cut.WaitForAssertion(() => Assert.Equal(2, Options(Editor(cut, "owner")).Length));

        await Type(Search(cut), "ee");

        cut.WaitForAssertion(() => Assert.NotNull(Editor(cut, "owner").QuerySelector("[data-slot=filter-options-hint]")));
        Assert.Single(source.Requests);
    }

    [Fact]
    public async Task An_error_shows_with_a_retry_button_that_asks_again()
    {
        var source = new Source { Fail = true };
        var cut = RenderList("/t?f.owner=ann", Schema(source));
        await Open(cut, "owner");

        cut.WaitForAssertion(() => Assert.NotNull(Editor(cut, "owner").QuerySelector("[data-slot=filter-options-error][role=alert]")));
        source.Fail = false;
        Editor(cut, "owner").QuerySelector("[data-slot=filter-options-retry]")!.Click();

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray"], Options(Editor(cut, "owner"))));
        Assert.Empty(Editor(cut, "owner").QuerySelectorAll("[data-slot=filter-options-error]"));
        Assert.Equal(2, source.Requests.Count);
    }

    // ---- labels

    [Fact]
    public void The_tag_of_a_value_from_the_address_is_labelled_by_the_resolver_once()
    {
        var source = new Source();

        var cut = RenderList("/t?f.owner=ann,dee", Schema(source));

        cut.WaitForAssertion(() => Assert.Equal(["Owner is Ann Lee, Dee Fox"], Tags(cut)));
        Assert.Equal([["ann", "dee"]], source.Resolved);
        Assert.Empty(source.Requests);
    }

    [Fact]
    public async Task A_selected_value_that_no_loaded_page_holds_is_listed_first_with_its_label()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=eve", Schema(source));
        cut.WaitForAssertion(() => Assert.Equal(["Owner is Eve Park"], Tags(cut)));

        await Open(cut, "owner");

        cut.WaitForAssertion(() => Assert.Equal(["Eve Park", "Ann Lee", "Bob Ray"], Options(Editor(cut, "owner"))));
        Assert.Equal([true, false, false], Editor(cut, "owner").QuerySelectorAll("[data-slot=filter-options] > li[data-value] input[type=checkbox]").Select(i => i.HasAttribute("checked")));
    }

    [Fact]
    public async Task Picking_an_option_writes_the_value_to_the_address_and_its_label_on_the_tag()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source));
        await Open(cut, "owner");
        cut.WaitForAssertion(() => Assert.Equal(2, Options(Editor(cut, "owner")).Length));

        Editor(cut, "owner").QuerySelectorAll("[data-slot=filter-options] > li[data-value] input[type=checkbox]")[1].Change(true);

        cut.WaitForAssertion(() => Assert.Equal(["Owner is Ann Lee, Bob Ray"], Tags(cut)));
        Assert.Equal("?f.owner=ann,bob", Uri.UnescapeDataString(new Uri(Navigation.Uri).Query));
        // Only the value of the address was asked of the resolver: the picked option brought its own label.
        Assert.Equal([["ann"]], source.Resolved);
    }

    // ---- counts

    [Fact]
    public async Task The_options_that_are_listed_carry_the_counts_of_the_facet_and_zero_when_the_facet_is_complete()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source, facets: true, pageSize: 5));

        await Open(cut, "owner");

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray", "Cyd Moss", "Dee Fox", "Eve Park"], Options(Editor(cut, "owner"))));
        // The rule on the owner leaves its own counts alone; dee and eve have no ticket, and the facet holds every owner, so they count 0.
        Assert.Equal(["13", "14", "13", "0", "0"], Counts(Editor(cut, "owner")));
    }

    [Fact]
    public async Task Without_counts_the_remote_options_have_none()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source));

        await Open(cut, "owner");

        cut.WaitForAssertion(() => Assert.Equal(2, Options(Editor(cut, "owner")).Length));
        Assert.Equal(["", ""], Counts(Editor(cut, "owner")));
    }

    // ---- other places an editor shows

    [Fact]
    public async Task The_editor_of_a_filter_panel_section_loads_when_the_section_opens_and_not_when_the_panel_renders()
    {
        var source = new Source();
        Navigation.NavigateTo("/t");

        var cut = Render<DataListPanelHarness>(p => p.Add(h => h.Schema, Schema(source)));

        // Closed sections: nothing was asked of the source.
        Assert.Empty(source.Requests);
        await cut.Find("bx-collapsible[data-filter=owner]").TriggerEventAsync("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 1 });

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray"], Options(cut.Find("[data-filter=owner] [data-slot=filter-editor]"))));
        Assert.Single(source.Requests);
    }

    [Fact]
    public void A_panel_section_that_starts_open_because_its_filter_is_on_loads_at_once()
    {
        var source = new Source();
        Navigation.NavigateTo("/t?f.owner=ann");

        var cut = Render<DataListPanelHarness>(p => p.Add(h => h.Schema, Schema(source)));

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray"], Options(cut.Find("[data-filter=owner] [data-slot=filter-editor]"))));
        Assert.Single(source.Requests);
    }

    [Fact]
    public async Task The_editor_of_the_add_filter_menu_loads_when_its_filter_is_picked()
    {
        var source = new Source();
        Navigation.NavigateTo("/t");
        var cut = Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(source)).Add(h => h.ShowAdd, true));
        Assert.Empty(source.Requests);

        await cut.Find("[data-slot=add-filter-popover]").TriggerEventAsync("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 1 });
        Assert.Empty(source.Requests);
        cut.FindAll("[data-slot=filter-menu] button").First(b => Text(b) == "Owner").Click();

        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray"], Options(cut.Find("[data-slot=filter-editor][data-filter=owner]"))));
        Assert.Single(source.Requests);
    }

    // ---- static rendering

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false) { }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    [Fact]
    public void Static_rendering_labels_the_selected_values_with_the_resolver_and_never_calls_the_source()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=ann,dee");
        var source = new Source();

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(source)));

        Assert.Equal(["Owner is Ann Lee, Dee Fox"], cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text));
        var editor = cut.Find("li[data-filter=owner] [data-slot=filter-editor]");
        Assert.Equal(["Ann Lee", "Dee Fox"], editor.QuerySelectorAll("a[data-slot=filter-option] span.truncate").Select(Text));
        Assert.Equal("Searching the other options needs JavaScript.", Text(editor.QuerySelector("[data-slot=filter-options-script]")!));
        Assert.Empty(source.Requests);
        Assert.Equal([["ann", "dee"]], source.Resolved);
    }

    [Fact]
    public void Static_rendering_lists_the_counted_options_with_their_labels_and_the_selected_ones()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=dee");
        var source = new Source();

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(source, facets: true, pageSize: 25)));

        var editor = cut.Find("li[data-filter=owner] [data-slot=filter-editor]");
        // By count (bob 14, ann 13, cyd 13), then the selected value that has no ticket.
        Assert.Equal(["Bob Ray", "Ann Lee", "Cyd Moss", "Dee Fox"], editor.QuerySelectorAll("a[data-slot=filter-option] span.truncate").Select(Text));
        Assert.Equal(["14", "13", "13", "0"], editor.QuerySelectorAll("a[data-slot=filter-option] [data-slot=filter-option-count]").Select(c => Text(c).TrimStart(',').Trim()));
        Assert.Empty(source.Requests);
    }

    private static Ticket[] WithAnUnknownOwner() => [.. FilterData.All, new Ticket(100, "Ticket 100 zed", null, false, "zed")];

    [Fact]
    public void Static_rendering_lists_at_most_the_option_limit_of_counted_options_and_labels_all_it_lists_in_one_resolver_call()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=cyd");
        var source = new Source();

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(source, facets: true, pageSize: 25, more: f => f.WithOptionLimit(2))).Add(h => h.Items, WithAnUnknownOwner()));

        var editor = cut.Find("li[data-filter=owner] [data-slot=filter-editor]");
        // The two most counted (bob 14, ann 13), then the selected one that is not among them.
        Assert.Equal(["Bob Ray", "Ann Lee", "Cyd Moss"], editor.QuerySelectorAll("a[data-slot=filter-option] span.truncate").Select(Text));
        Assert.Single(source.Resolved);
        Assert.Equal(["ann", "bob", "cyd"], source.Resolved[0].Order());
        Assert.Empty(source.Requests);
    }

    [Fact]
    public void Static_rendering_lists_the_page_size_without_a_limit_and_leaves_out_an_option_the_resolver_does_not_know()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=ann");
        var source = new Source();

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(source, facets: true, pageSize: 4)).Add(h => h.Items, WithAnUnknownOwner()));

        // The four most counted are bob, ann, cyd and zed; the resolver does not know zed, so it is not listed.
        var editor = cut.Find("li[data-filter=owner] [data-slot=filter-editor]");
        Assert.Equal(["Bob Ray", "Ann Lee", "Cyd Moss"], editor.QuerySelectorAll("a[data-slot=filter-option] span.truncate").Select(Text));
        Assert.Single(source.Resolved);
        Assert.Contains("zed", source.Resolved[0]);
    }

    [Fact]
    public void A_selected_value_the_resolver_does_not_know_is_still_listed_as_it_is_so_it_can_be_taken_off()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=zed");

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(new Source(), facets: true, pageSize: 25)).Add(h => h.Items, WithAnUnknownOwner()));

        var labels = cut.Find("li[data-filter=owner] [data-slot=filter-editor]").QuerySelectorAll("a[data-slot=filter-option] span.truncate").Select(Text).ToList();
        Assert.Contains("zed", labels);
        Assert.Equal("Owner is zed", Text(cut.Find("li[data-slot=filter-tag] [data-slot=filter-tag-button]")));
    }

    [Fact]
    public void The_option_sort_does_not_apply_to_remote_options_static_ones_keep_the_count_order_of_the_facet()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=ann");

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Schema(new Source(), facets: true, pageSize: 25, more: f => f.WithOptionSort(DataListOptionSort.Alpha))));

        // Alpha would put Ann first; the facet order is bob 14, ann 13, cyd 13.
        Assert.Equal(["Bob Ray", "Ann Lee", "Cyd Moss"], cut.Find("li[data-filter=owner] [data-slot=filter-editor]").QuerySelectorAll("a[data-slot=filter-option] span.truncate").Select(Text));
    }

    [Fact]
    public async Task The_option_sort_does_not_apply_to_the_interactive_remote_list_either()
    {
        var source = new Source();
        var cut = RenderList("/t?f.owner=ann", Schema(source, facets: true, pageSize: 5, more: f => f.WithOptionSort(DataListOptionSort.Count)));

        await Open(cut, "owner");

        // Counts would put bob (14) first; the list keeps the order of the source.
        cut.WaitForAssertion(() => Assert.Equal(["Ann Lee", "Bob Ray", "Cyd Moss", "Dee Fox", "Eve Park"], Options(Editor(cut, "owner"))));
    }

    [Fact]
    public void Static_rendering_of_the_panel_has_a_checkbox_for_each_counted_option_and_the_hint()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.owner=dee");
        var source = new Source();

        var cut = context.Render<DataListPanelHarness>(p => p.Add(h => h.Schema, Schema(source, facets: true, pageSize: 25)));

        var section = cut.Find("[data-filter=owner] [data-slot=filter-options]");
        Assert.Equal(["Bob Ray", "Ann Lee", "Cyd Moss", "Dee Fox"], section.QuerySelectorAll("[data-slot=filter-option-input]").Select(i => Text(i.ParentElement!)));
        Assert.Equal("dee", section.QuerySelectorAll("[data-slot=filter-option-input][checked]").Single().GetAttribute("value"));
        Assert.NotNull(section.QuerySelector("[data-slot=filter-options-script]"));
        Assert.Empty(source.Requests);
    }
}
