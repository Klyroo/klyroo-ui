using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The filter parts: the tags and their summaries, the editors per kind (operators, text, options with counts, include empty, yes / no / any), the add filter menu, clear filters,
/// the history (a push per applied change, back on page 1), the Submit apply mode, static rendering as links and GET forms, and the texts in other languages.
/// </summary>
public partial class DataListFilterTests : BluxToolsTestContext
{
    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    private static string Query(NavigationManager navigation) => Uri.UnescapeDataString(new Uri(navigation.Uri).Query);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    private static string Text(IElement element) => Spaces().Replace(element.TextContent, " ").Trim();

    private IRenderedComponent<DataListFilterHarness> RenderList(string? url = null, Action<ComponentParameterCollectionBuilder<DataListFilterHarness>>? configure = null)
    {
        if (url is not null) Navigation.NavigateTo(url);
        return Render<DataListFilterHarness>(p => configure?.Invoke(p));
    }

    private static string[] Tags(IRenderedComponent<DataListFilterHarness> cut) =>
        [.. cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text)];

    private static IElement TagItem(IRenderedComponent<DataListFilterHarness> cut, string key) => cut.Find($"li[data-slot=filter-tag][data-filter={key}]");

    private static string[] Names(IRenderedComponent<DataListFilterHarness> cut) =>
        [.. cut.FindAll("tbody tr[data-slot=row] td:first-child").Select(td => td.TextContent)];

    // What the script of Blux.UI reports: the sequence goes on from the one the last render carried in data-bx-seq.
    private static Task Type(IElement input, string text, int sequence = 0) =>
        input.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = text, Sequence = int.Parse(input.GetAttribute("data-bx-seq") ?? "0") + 1 });

    private static Task WaitForQuery(NavigationManager navigation, string expected) =>
        Task.Run(async () =>
        {
            for (var i = 0; i < 100 && Query(navigation) != expected; i++) await Task.Delay(20);
            Assert.Equal(expected, Query(navigation));
        });

    // ---- the tags

    [Fact]
    public void A_tag_summarizes_each_filter_with_its_name_operator_and_values()
    {
        var cut = RenderList("/t?f.title=ann&f.status=open,paused,closed&f.active=true&f.owner.isNot=ann");

        Assert.Equal(["Title contains ann", "Status is Open, Paused +1", "Active is Yes", "Owner is not ann"], Tags(cut));
    }

    [Fact]
    public void A_filter_that_also_matches_empty_rows_says_so_and_one_that_only_does_says_empty()
    {
        var cut = RenderList("/t?f.status=open,~&f.owner=~");

        Assert.Equal(["Status is Open or empty", "Owner is empty"], Tags(cut));
    }

    [Fact]
    public void The_tags_are_a_list_whose_name_announces_how_many_filters_are_on()
    {
        var cut = RenderList("/t?f.title=ann&f.active=false");

        var list = cut.Find("ul[data-slot=filter-tags]");
        Assert.Equal("Filters, 2 active", list.GetAttribute("aria-label"));
        Assert.Equal(2, list.QuerySelectorAll("li").Length);
    }

    [Fact]
    public void Without_a_filter_there_is_no_list_and_without_the_add_button_nothing_at_all()
    {
        var cut = RenderList();

        Assert.Empty(cut.FindAll("[data-slot=filters]"));
    }

    [Fact]
    public void The_remove_button_of_a_tag_is_named_after_its_filter_and_removes_it_with_a_history_entry_on_the_first_page()
    {
        var cut = RenderList("/t?f.title=ann&f.active=true&p=1");
        var before = Navigation.History.Count;

        cut.Find("button[aria-label='Remove filter Title']").Click();

        cut.WaitForAssertion(() => Assert.Equal(["Active is Yes"], Tags(cut)));
        Assert.Equal("?f.active=true", Query(Navigation));
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry, "a removed filter is one history entry");
    }

    [Fact]
    public void A_tag_name_comes_from_the_column_title_when_the_filter_has_no_label_and_from_the_key_otherwise()
    {
        var cut = RenderList("/t?f.owner=ann");

        // The owner filter has no label: no column titles it, so the key written as words.
        Assert.Equal(["Owner is ann"], Tags(cut));
        Assert.NotNull(cut.Find("button[aria-label='Remove filter Owner']"));
    }

    [Fact]
    public void A_tag_button_opens_the_editor_in_a_popover_named_after_the_filter()
    {
        var cut = RenderList("/t?f.title=ann");

        var popover = cut.Find("li[data-filter=title] bx-popover");
        Assert.NotNull(popover.QuerySelector("button[data-slot=filter-tag-button]"));
        var panel = popover.QuerySelector("[popover]")!;
        Assert.Equal("dialog", panel.GetAttribute("role"));
        Assert.Equal("Edit filter Title", panel.GetAttribute("aria-label"));
        Assert.NotNull(panel.QuerySelector("[data-slot=filter-editor][data-filter=title]"));
    }

    // ---- the operators per kind

    private static string[] OperatorTexts(IRenderedComponent<DataListFilterHarness> cut, string key) =>
        [.. TagItem(cut, key).QuerySelectorAll("select[data-slot=filter-operator] option").Select(o => o.TextContent)];

    [Fact]
    public void A_text_filter_offers_contains_is_and_is_not()
    {
        var cut = RenderList("/t?f.title=ann");

        Assert.Equal(["contains", "is", "is not"], OperatorTexts(cut, "title"));
        Assert.Equal("Operator for Title", TagItem(cut, "title").QuerySelector("select")!.GetAttribute("aria-label"));
        Assert.Equal("contains", TagItem(cut, "title").QuerySelector("option[selected]")!.TextContent);
    }

    [Fact]
    public void An_option_filter_offers_is_any_of_and_is_not_and_is_only_when_its_rule_uses_it()
    {
        var any = RenderList("/t?f.status=open");
        using var other = new Interactive();
        other.Navigation.NavigateTo("/t?f.status.is=open");
        var single = other.Render<DataListFilterHarness>();

        Assert.Equal(["is any of", "is not"], OperatorTexts(any, "status"));
        Assert.Equal(["is any of", "is", "is not"], OperatorTexts(single, "status"));
    }

    [Fact]
    public void A_boolean_filter_has_no_operator_picker_but_a_yes_no_any_group()
    {
        var cut = RenderList("/t?f.active=true");

        var item = TagItem(cut, "active");
        Assert.Empty(item.QuerySelectorAll("select"));
        var group = item.QuerySelector("bx-toggle-group")!;
        Assert.Equal("Value for Active", group.GetAttribute("aria-label"));
        Assert.Equal(["Yes", "No", "Any"], group.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
        Assert.Equal("true", group.GetAttribute("data-values"));
    }

    [Fact]
    public async Task Choosing_another_operator_changes_the_rule_and_keeps_the_value()
    {
        var cut = RenderList("/t?f.title=ann");

        await TagItem(cut, "title").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "isNot" });

        await WaitForQuery(Navigation, "?f.title.isNot=ann");
        cut.WaitForAssertion(() => Assert.Equal(["Title is not ann"], Tags(cut)));
    }

    // ---- the text editor

    [Fact]
    public async Task Typing_a_text_applies_it_goes_back_to_page_one_and_keeps_one_history_entry_for_the_whole_typing()
    {
        var cut = RenderList("/t?f.title=ann&p=1");
        var before = Navigation.History.Count;
        var input = TagItem(cut, "title").QuerySelector("input[aria-label='Value for Title']")!;
        Assert.Equal("300", input.GetAttribute("data-bx-debounce"));
        Assert.Equal("ann", input.GetAttribute("value"));

        await Type(input, "cyd", 1);
        await WaitForQuery(Navigation, "?f.title=cyd");
        var afterFirst = Navigation.History.Count;
        await Type(TagItem(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "cyd 1", 2);
        await WaitForQuery(Navigation, "?f.title=cyd 1");

        Assert.Equal(before + 1, afterFirst);
        Assert.False(Navigation.History.Skip(1).First().Options.ReplaceHistoryEntry);
        // The second pause of the same typing replaces the entry the first one made.
        Assert.Equal(afterFirst, Navigation.History.Count);
        Assert.True(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.Equal(["Title contains cyd 1"], Tags(cut)));
    }

    [Fact]
    public async Task Emptying_the_text_takes_the_filter_off()
    {
        var cut = RenderList("/t?f.title=ann");

        await Type(TagItem(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "  ");

        await WaitForQuery(Navigation, "");
        cut.WaitForAssertion(() => Assert.Empty(Tags(cut)));
    }

    // ---- the option editor

    private static IElement[] OptionRows(IRenderedComponent<DataListFilterHarness> cut, string key) =>
        [.. TagItem(cut, key).QuerySelectorAll("[data-slot=filter-options] li")];

    [Fact]
    public void An_option_filter_lists_its_options_with_the_counts_of_the_list_and_checks_the_selected_ones()
    {
        var cut = RenderList("/t?f.status=open");

        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        var rows = OptionRows(cut, "status");
        Assert.Equal(["Open", "Paused", "Closed", "Archived"], rows.Select(r => r.QuerySelector("label")!.TextContent.Trim()));
        // ExcludeSelf: the counts do not narrow to the filter's own selection.
        Assert.All(rows, r => Assert.Equal("8", r.QuerySelector("[data-slot=filter-option-count]")!.TextContent.Replace(",", "").Trim()));
        Assert.Equal([true, false, false, false], rows.Select(r => r.QuerySelector("input[type=checkbox]")!.HasAttribute("checked")));
        Assert.Equal("Value for Status", TagItem(cut, "status").QuerySelector("[data-slot=filter-options]")!.ParentElement!.GetAttribute("aria-label"));
    }

    [Fact]
    public void Without_declared_options_the_editor_lists_the_values_of_the_counts()
    {
        var cut = RenderList("/t?f.owner=ann");

        cut.WaitForAssertion(() => Assert.Equal(3, OptionRows(cut, "owner").Length));
        Assert.Equal(["ann", "bob", "cyd"], OptionRows(cut, "owner").Select(r => r.QuerySelector("label")!.TextContent.Trim()).Order());
    }

    [Fact]
    public async Task Ticking_options_applies_a_list_with_a_history_entry_each_and_unticking_the_last_takes_the_filter_off()
    {
        var cut = RenderList("/t?f.status=open&p=1");
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        var before = Navigation.History.Count;

        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        await WaitForQuery(Navigation, "?f.status=open,paused");
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.Equal(["Status is Open, Paused"], Tags(cut)));

        OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.Change(false);
        await WaitForQuery(Navigation, "?f.status=paused");
        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(false);
        await WaitForQuery(Navigation, "");
        cut.WaitForAssertion(() => Assert.Empty(Tags(cut)));
    }

    [Fact]
    public async Task The_include_empty_switch_maps_to_the_tilde_token_and_to_include_empty()
    {
        var cut = RenderList("/t?f.status=open");
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        var toggle = TagItem(cut, "status").QuerySelector("input[role=switch]")!;
        Assert.Contains(TagItem(cut, "status").QuerySelectorAll("label"), l => Text(l) == "Include empty values");

        toggle.Change(true);

        await WaitForQuery(Navigation, "?f.status=open,~");
        cut.WaitForAssertion(() => Assert.Equal(["Status is Open or empty"], Tags(cut)));
        // Open or empty: 8 open + 8 without a status.
        cut.WaitForAssertion(() => Assert.Contains("1–10 of 16", cut.Find("[data-testid=count]").TextContent));
    }

    [Fact]
    public async Task Is_not_excludes_the_selected_values()
    {
        var cut = RenderList("/t?f.status=open");

        await TagItem(cut, "status").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "isNot" });

        await WaitForQuery(Navigation, "?f.status.isNot=open");
        cut.WaitForAssertion(() => Assert.Contains("of 32", cut.Find("[data-testid=count]").TextContent));
    }

    [Fact]
    public async Task With_the_is_operator_one_option_replaces_the_other()
    {
        var cut = RenderList("/t?f.status.is=open");
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));

        OptionRows(cut, "status")[2].QuerySelector("input[type=checkbox]")!.Change(true);

        await WaitForQuery(Navigation, "?f.status.is=closed");
    }

    [Fact]
    public async Task The_options_can_be_searched_once_there_are_many_and_the_search_applies_nothing()
    {
        var many = Enumerable.Range(1, 12).Select(i => new DataListFilterOption($"v{i}", $"Option {i} {(i % 2 == 0 ? "even" : "odd")}")).ToArray();
        var schema = DataListSchema.For<Ticket>(s => s
            .RowKey(t => t.Id)
            .Field(t => t.Id, f => f.Sortable())
            .Field(t => t.Status, f => f.Filter(DataListFilter.Option(many).WithLabel("Status")))
            .DefaultSort(t => t.Id));
        Navigation.NavigateTo("/t?f.status=v1");
        var cut = Render<DataListFilterHarness>(p => p.Add(c => c.Schema, schema));

        var search = TagItem(cut, "status").QuerySelector("input[aria-label='Search options']")!;
        await Type(search, "even");

        cut.WaitForAssertion(() => Assert.Equal(6, OptionRows(cut, "status").Length));
        Assert.Equal("?f.status=v1", Query(Navigation));
        await Type(TagItem(cut, "status").QuerySelector("input[aria-label='Search options']")!, "zzz", 2);
        cut.WaitForAssertion(() => Assert.Equal("No option matches your search", Text(TagItem(cut, "status").QuerySelector("[data-slot=filter-empty]")!)));
    }

    // ---- the boolean editor

    [Fact]
    public async Task The_yes_no_any_choice_applies_a_boolean_rule_and_any_takes_it_off()
    {
        var cut = RenderList("/t?f.active=true");
        var before = Navigation.History.Count;

        await TagItem(cut, "active").QuerySelector("bx-toggle-group")!.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "false", Sequence = 1 });
        await WaitForQuery(Navigation, "?f.active=false");
        Assert.Equal(before + 1, Navigation.History.Count);
        cut.WaitForAssertion(() => Assert.Equal(["Active is No"], Tags(cut)));

        await TagItem(cut, "active").QuerySelector("bx-toggle-group")!.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "any", Sequence = 2 });
        await WaitForQuery(Navigation, "");
    }

    // ---- the editor in Submit mode

    [Fact]
    public async Task In_submit_mode_a_change_waits_for_apply_and_cancel_drops_it()
    {
        var cut = RenderList("/t?f.status=open", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        var before = Navigation.History.Count;

        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(true);

        Assert.Equal("?f.status=open", Query(Navigation));
        Assert.Equal(before, Navigation.History.Count);
        Assert.Equal(["Apply", "Cancel"], TagItem(cut, "status").QuerySelectorAll("[data-slot=filter-actions] button").Select(b => b.TextContent.Trim()).Order());

        TagItem(cut, "status").QuerySelector("button[data-slot=filter-cancel]")!.Click();
        cut.WaitForAssertion(() => Assert.False(OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.HasAttribute("checked")));
        Assert.Equal("?f.status=open", Query(Navigation));

        OptionRows(cut, "status")[2].QuerySelector("input[type=checkbox]")!.Change(true);
        TagItem(cut, "status").QuerySelector("button[data-slot=filter-apply]")!.Click();

        await WaitForQuery(Navigation, "?f.status=open,closed");
        Assert.Equal(before + 1, Navigation.History.Count);
    }

    // ---- add filter and clear filters

    [Fact]
    public void The_add_filter_menu_lists_the_filters_that_are_not_on_in_the_order_of_the_schema()
    {
        var cut = RenderList("/t?f.title=ann", p => p.Add(c => c.ShowAdd, true));

        var items = cut.FindAll("[data-slot=filter-menu-item]").Select(Text).ToArray();
        Assert.Equal(["Status", "Active", "Owner"], items);
        Assert.Equal("Add filter", Text(cut.Find("[data-slot=add-filter]")));
        Assert.Equal("Add filter", cut.Find("[data-slot=add-filter-popover] [popover]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Picking_a_filter_of_the_menu_opens_its_editor_and_the_first_change_adds_the_filter_with_a_back_button_to_the_menu()
    {
        var cut = RenderList(null, p => p.Add(c => c.ShowAdd, true));
        var before = Navigation.History.Count;

        cut.Find("[data-slot=filter-menu-item][data-filter=title]").Click();

        var panel = cut.Find("[data-slot=add-filter-popover] [popover]");
        Assert.NotNull(panel.QuerySelector("[data-slot=filter-editor][data-filter=title]"));
        Assert.Equal("Back to the filters", Text(panel.QuerySelector("[data-slot=filter-back]")!));
        await Type(panel.QuerySelector("input[aria-label='Value for Title']")!, "bob");
        await WaitForQuery(Navigation, "?f.title=bob");
        Assert.Equal(before + 1, Navigation.History.Count);
        cut.WaitForAssertion(() => Assert.Equal(["Title contains bob"], Tags(cut)));

        cut.Find("[data-slot=filter-back]").Click();
        cut.WaitForAssertion(() => Assert.Equal(["Status", "Active", "Owner"], cut.FindAll("[data-slot=filter-menu-item]").Select(Text).ToArray()));
    }

    [Fact]
    public void With_many_filters_the_menu_gets_a_search_field_that_narrows_it()
    {
        var schema = DataListSchema.For<Ticket>(s =>
        {
            s.RowKey(t => t.Id).Field(t => t.Id, f => f.Sortable()).DefaultSort(t => t.Id);
            for (var i = 0; i < 9; i++) s.QueryFilter($"extra{i}", DataListFilter.Text().WithLabel($"Extra {i}"));
        });
        var cut = Render<DataListFilterHarness>(p => p.Add(c => c.Schema, schema).Add(c => c.ShowAdd, true));

        Assert.Equal(9, cut.FindAll("[data-slot=filter-menu-item]").Count);
        cut.Find("input[aria-label='Search filters']").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "extra 3", Sequence = 1 });
        cut.WaitForAssertion(() => Assert.Equal(["Extra 3"], cut.FindAll("[data-slot=filter-menu-item]").Select(Text).ToArray()));
    }

    [Fact]
    public void When_every_filter_is_on_the_menu_says_so()
    {
        var cut = RenderList("/t?f.title=a&f.status=open&f.active=true&f.owner=ann", p => p.Add(c => c.ShowAdd, true));

        Assert.Equal("Every filter is in use", Text(cut.Find("[data-slot=add-filter-popover] [data-slot=filter-empty]")));
    }

    [Fact]
    public async Task Clear_filters_takes_every_filter_off_with_one_history_entry_and_keeps_the_search_and_the_sort()
    {
        var cut = RenderList("/t?f.title=ann&f.active=true&q=Ticket&s=-id&p=1", p => p.Add(c => c.ShowClear, true));
        var before = Navigation.History.Count;

        cut.Find("button[data-slot=clear-filters]").Click();

        await WaitForQuery(Navigation, "?q=Ticket&s=-id");
        Assert.Equal(before + 1, Navigation.History.Count);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-slot=clear-filters]")));
    }

    [Fact]
    public void Clear_filters_works_alone_and_shows_only_while_a_filter_is_on()
    {
        var none = Render<BxDataList<Ticket>>(p => p.Add(c => c.Schema, FilterData.Schema).Add(c => c.Items, FilterData.All).Add(c => c.UrlSync, false).Add(c => c.ChildContent, b =>
        {
            b.OpenComponent<BxDataListClearFilters<Ticket>>(0);
            b.CloseComponent();
        }));

        Assert.Empty(none.FindAll("[data-slot=clear-filters]"));
    }

    [Fact]
    public void The_default_layout_shows_the_tags_the_add_button_and_the_clear_button_when_the_schema_has_filters()
    {
        Navigation.NavigateTo("/t?f.active=true");
        var cut = Render<BxDataList<Ticket>>(p => p.Add(c => c.Schema, FilterData.Schema).Add(c => c.Items, FilterData.All));

        Assert.Equal(["Active is Yes"], cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text).ToArray());
        Assert.NotNull(cut.Find("[data-slot=add-filter]"));
        Assert.NotNull(cut.Find("[data-slot=clear-filters]"));
    }

    [Fact]
    public void The_default_layout_has_no_filter_bar_when_the_schema_has_no_filter()
    {
        var cut = Render<BxDataList<Person>>(p => p.Add(c => c.Schema, DataListControllerTests.Schema).Add(c => c.Items, DataListControllerTests.People).Add(c => c.UrlSync, false));

        Assert.Empty(cut.FindAll("[data-slot=filters]"));
    }

    // ---- the URL

    [Fact]
    public void A_typed_url_loads_the_filters_and_drops_what_the_schema_does_not_allow()
    {
        var cut = RenderList("/t?f.title=ann&f.nothing=1&f.title.gt=3&f.active=maybe&f.status.between=a..b");

        Assert.Equal(["Title contains ann"], Tags(cut));
        // 13 titles contain "ann": the rules that were not valid filter nothing.
        cut.WaitForAssertion(() => Assert.Contains("of 13", Text(cut.Find("[data-testid=count]"))));
    }

    [Fact]
    public async Task Back_restores_the_previous_filter_into_the_editor()
    {
        var cut = RenderList("/t?f.status=open");
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        await WaitForQuery(Navigation, "?f.status=open,paused");

        Navigation.NavigateTo("/t?f.status=closed");

        cut.WaitForAssertion(() => Assert.Equal(["Status is Closed"], Tags(cut)));
        Assert.Equal([false, false, true, false], OptionRows(cut, "status").Select(r => r.QuerySelector("input[type=checkbox]")!.HasAttribute("checked")));
    }

    [Fact]
    public async Task Filtering_matches_the_rows_and_the_result_count_announces_them()
    {
        var cut = RenderList("/t?f.active=true&f.owner=ann");

        await cut.WaitForAssertionAsync(() => Assert.Equal("1–10 of 13", Text(cut.Find("[data-testid=count]"))));
        Assert.Equal("status", cut.Find("[data-testid=count]").GetAttribute("role"));
    }

    // ---- other languages

    [Fact]
    public void The_texts_follow_the_culture_of_the_user()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = RenderList("/t?f.title=ann&f.active=true&f.status.isNot=open", p => p.Add(c => c.ShowAdd, true).Add(c => c.ShowClear, true));

            Assert.Equal(["Title contient ann", "Active est Oui", "Status n’est pas Open"], Tags(cut));
            Assert.Equal("Filtres, 3 actif(s)", cut.Find("ul[data-slot=filter-tags]").GetAttribute("aria-label"));
            Assert.NotNull(cut.Find("button[aria-label='Retirer le filtre Title']"));
            Assert.Equal("Ajouter un filtre", Text(cut.Find("[data-slot=add-filter]")));
            Assert.Equal("Effacer les filtres", Text(cut.Find("[data-slot=clear-filters]")));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    // ---- facets reach the provider

    [Fact]
    public async Task A_filter_that_asks_for_counts_puts_a_facet_request_in_every_load()
    {
        var requests = new List<DataListRequest>();
        DataListProvider<Ticket> provider = (request, _) =>
        {
            requests.Add(request);
            return ValueTask.FromResult(new DataListResult<Ticket>([], 0));
        };
        using var controller = new DataListController<Ticket>(FilterData.Schema, provider);

        await controller.LoadAsync(FilterData.Schema.DefaultState);

        Assert.Equal(["status", "owner"], requests[0].Facets.Select(f => f.Key));
        Assert.All(requests[0].Facets, f => Assert.Equal(FacetMode.ExcludeSelf, f.Mode));
    }

    // ---- static rendering

    private sealed class Interactive : BluxToolsTestContext
    {
        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false) { }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    private static (string Name, string? Value)[] Hidden(IElement form) =>
        [.. form.QuerySelectorAll("input[type=hidden]").Select(i => (i.GetAttribute("name")!, i.GetAttribute("value")))];

    private static System.Collections.Specialized.NameValueCollection QueryOf(string href) => System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + href).Query);

    [Fact]
    public void Static_rendering_removes_a_filter_with_a_link_that_keeps_the_rest_and_drops_the_page()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title=ann&f.active=true&p=2&s=-id&utm_source=mail");

        var cut = context.Render<DataListFilterHarness>();

        var remove = cut.Find("a[aria-label='Remove filter Title']");
        Assert.Empty(cut.FindAll("button[data-slot=filter-tag-remove]"));
        var query = QueryOf(remove.GetAttribute("href")!);
        Assert.Null(query["f.title"]);
        Assert.Equal("true", query["f.active"]);
        Assert.Null(query["p"]);
        Assert.Equal("-id", query["s"]);
        Assert.Equal("mail", query["utm_source"]);
    }

    [Fact]
    public void Static_rendering_edits_a_text_filter_with_a_get_form_that_keeps_the_other_parameters()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title.isNot=ann&f.active=true&p=2&utm_source=mail");

        var cut = context.Render<DataListFilterHarness>();

        var form = cut.Find("li[data-filter=title] form[data-slot=filter-form]");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal("/t", form.GetAttribute("action"));
        Assert.NotNull(form.GetAttribute("data-enhance"));
        var input = form.QuerySelector("input[type=text]")!;
        // The operator is in the name of the field: f.<key>.<op> for an operator other than the default one.
        Assert.Equal("f.title.isNot", input.GetAttribute("name"));
        Assert.Equal("ann", input.GetAttribute("value"));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("true", hidden["f.active"]);
        Assert.Equal("mail", hidden["utm_source"]);
        Assert.DoesNotContain("p", hidden.Keys);
        Assert.DoesNotContain("f.title.isNot", hidden.Keys);
        Assert.Equal("Apply", Text(form.QuerySelector("button[type=submit]")!));
    }

    [Fact]
    public void Static_rendering_switches_the_operator_with_links_that_keep_the_value()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title=ann");

        var cut = context.Render<DataListFilterHarness>();

        var links = cut.FindAll("li[data-filter=title] [data-slot=filter-operators] a");
        Assert.Equal(["contains", "is", "is not"], links.Select(Text));
        Assert.Equal("true", links[0].GetAttribute("aria-current"));
        var isNot = QueryOf(links[2].GetAttribute("href")!);
        Assert.Equal("ann", isNot["f.title.isNot"]);
        Assert.Null(isNot["f.title"]);
    }

    [Fact]
    public void Static_rendering_toggles_options_and_include_empty_with_links()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.status=open&p=3");

        var cut = context.Render<DataListFilterHarness>();

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("li[data-filter=status] a[data-slot=filter-option]").Count));
        var options = cut.FindAll("li[data-filter=status] a[data-slot=filter-option]");
        Assert.Equal("true", options[0].GetAttribute("aria-current"));
        Assert.Equal("open,paused", QueryOf(options[1].GetAttribute("href")!)["f.status"]);
        Assert.Null(QueryOf(options[0].GetAttribute("href")!)["f.status"]);
        Assert.Null(QueryOf(options[1].GetAttribute("href")!)["p"]);
        Assert.Equal("open,~", QueryOf(cut.Find("li[data-filter=status] a[data-slot=filter-include-empty]").GetAttribute("href")!)["f.status"]);
    }

    [Fact]
    public void Static_rendering_chooses_yes_no_or_any_with_links()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.active=true");

        var cut = context.Render<DataListFilterHarness>();

        var links = cut.FindAll("li[data-filter=active] [data-slot=filter-boolean] a");
        Assert.Equal(["Yes", "No", "Any"], links.Select(Text));
        Assert.Equal("true", links[0].GetAttribute("aria-current"));
        Assert.Equal("false", QueryOf(links[1].GetAttribute("href")!)["f.active"]);
        Assert.Null(QueryOf(links[2].GetAttribute("href")!)["f.active"]);
    }

    [Fact]
    public void Static_rendering_lists_an_editor_per_missing_filter_in_the_add_menu_and_a_clear_link()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title=ann&q=x");

        var cut = context.Render<DataListFilterHarness>(p => p.Add(c => c.ShowAdd, true).Add(c => c.ShowClear, true));

        var sections = cut.FindAll("[data-slot=add-filter-popover] [data-slot=filter-section] [data-slot=filter-editor]").Select(e => e.GetAttribute("data-filter")).ToArray();
        Assert.Equal(["status", "active", "owner"], sections);
        var clear = cut.Find("a[data-slot=clear-filters]");
        var query = QueryOf(clear.GetAttribute("href")!);
        Assert.Null(query["f.title"]);
        Assert.Equal("x", query["q"]);
    }

    [Fact]
    public void Static_rendering_names_the_url_key_in_the_fields_and_leaves_other_lists_alone()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?tk.f.title=ann&other.f.title=zzz&other.p=2");

        var cut = context.Render<DataListFilterHarness>(p => p.Add(c => c.UrlKey, "tk"));

        var form = cut.Find("li[data-filter=title] form[data-slot=filter-form]");
        Assert.Equal("tk.f.title", form.QuerySelector("input[type=text]")!.GetAttribute("name"));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("zzz", hidden["other.f.title"]);
        Assert.Equal("2", hidden["other.p"]);
    }

    [Fact]
    public void Static_rendering_without_url_sync_has_no_link_and_no_form()
    {
        using var context = new StaticContext();

        var cut = context.Render<DataListFilterHarness>(p => p.Add(c => c.UrlSync, false).Add(c => c.ShowAdd, true));

        Assert.Empty(cut.FindAll("form"));
        Assert.Empty(cut.FindAll("[data-slot=add-filter]"));
    }

    // ---- the schema

    [Fact]
    public void The_filter_definition_carries_a_label_options_and_the_facet_flag_through_its_copies()
    {
        var filter = DataListFilter.Option("a", "b").WithLabel("Kind").WithFacets().WithPersist().WithOperators(FilterOperator.IsAnyOf, FilterOperator.IsNot)
            .WithDefault(new FilterValue.List(["a"]));

        Assert.Equal("Kind", filter.Label);
        Assert.Equal(["a", "b"], filter.Options.Select(o => o.Value));
        Assert.True(filter.Facets);
        Assert.True(filter.Persist);
        Assert.Throws<ArgumentException>(() => DataListFilter.Option(new DataListFilterOption("a", "A"), new DataListFilterOption("a", "Again")));
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Text().WithFacets());
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Boolean().WithOptions(new DataListFilterOption("a", "A")));
    }
}
