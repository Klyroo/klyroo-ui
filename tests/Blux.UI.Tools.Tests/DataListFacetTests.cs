using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The counts of the option and boolean filters (next to the options, Yes and No and the include empty switch; exclude-self and include-self; sort by count or label; the limit and "show more"),
/// interactive and statically rendered. Tickets: 40, owners ann 13 / bob 14 / cyd 13, status open, paused, closed and archived 8 each and 8 without one, active (every third) 13 of 40.
/// </summary>
public partial class DataListFacetTests : BluxToolsTestContext
{
    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    private static string Text(IElement element) => Spaces().Replace(element.TextContent, " ").Trim();

    internal static readonly DataListFilterOption[] StatusOptions =
        [new("open", "Open"), new("paused", "Paused"), new("closed", "Closed"), new("archived", "Archived")];

    internal static DataListSchema<Ticket> Build(DataListFilter status, DataListFilter owner, DataListFilter active) =>
        DataListSchema.For<Ticket>(s => s
            .RowKey(t => t.Id)
            .Search(t => t.Title)
            .Field(t => t.Id, f => f.Sortable())
            .Field(t => t.Title, f => f.Sortable().Filter(DataListFilter.Text().WithLabel("Title")))
            .Field(t => t.Status, f => f.Filter(status.WithLabel("Status")))
            .Field(t => t.Active, f => f.Filter(active.WithLabel("Active")))
            .Field(t => t.Owner, f => f.Filter(owner.WithLabel("Owner")))
            .DefaultSort(t => t.Id)
            .DefaultPageSize(10));

    private IRenderedComponent<DataListFilterHarness> RenderList(string url, DataListSchema<Ticket> schema)
    {
        Navigation.NavigateTo(url);
        return Render<DataListFilterHarness>(p => p.Add(h => h.Schema, schema));
    }

    private static IElement Editor(IRenderedComponent<DataListFilterHarness> cut, string key) => cut.Find($"li[data-filter={key}] [data-slot=filter-editor]");

    private static string[] Options(IElement editor) =>
        [.. editor.QuerySelectorAll("[data-slot=filter-options] > li[data-value]").Select(li => Text(li.QuerySelector("label")!))];

    private static string[] Counts(IElement editor) =>
        [.. editor.QuerySelectorAll("[data-slot=filter-options] > li[data-value]").Select(li => Text(li.QuerySelector("[data-slot=filter-option-count]")!).TrimStart(',').Trim())];

    private static string[] BooleanChoices(IElement editor) =>
        [.. editor.QuerySelectorAll("[data-slot=filter-boolean] button").Select(Text)];

    // ---- boolean counts

    [Fact]
    public void A_boolean_filter_with_counts_writes_them_next_to_yes_and_no_without_its_own_rule_by_default()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option(), DataListFilter.Boolean().WithFacets());
        var cut = RenderList("/t?f.active=true", schema);

        // ExcludeSelf: the rule on Active does not narrow its own counts.
        cut.WaitForAssertion(() => Assert.Equal(["Yes (13)", "No (27)", "Any"], BooleanChoices(Editor(cut, "active"))));
    }

    [Fact]
    public void A_boolean_facet_counts_the_rows_the_other_filters_leave()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option(), DataListFilter.Boolean().WithFacets());
        var cut = RenderList("/t?f.active=true&f.owner=ann", schema);

        // Every ticket of ann is active (ids that are multiples of three).
        cut.WaitForAssertion(() => Assert.Equal(["Yes (13)", "No (0)", "Any"], BooleanChoices(Editor(cut, "active"))));
    }

    [Fact]
    public void A_boolean_filter_in_include_self_mode_counts_with_its_own_rule()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option(), DataListFilter.Boolean().WithFacets().WithFacetMode(FacetMode.IncludeSelf));
        var cut = RenderList("/t?f.active=true", schema);

        cut.WaitForAssertion(() => Assert.Equal(["Yes (13)", "No (0)", "Any"], BooleanChoices(Editor(cut, "active"))));
    }

    [Fact]
    public void A_boolean_filter_without_counts_keeps_plain_labels()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option(), DataListFilter.Boolean());
        var cut = RenderList("/t?f.active=true", schema);

        Assert.Equal(["Yes", "No", "Any"], BooleanChoices(Editor(cut, "active")));
    }

    // ---- the empty values

    [Fact]
    public void The_include_empty_switch_shows_how_many_rows_have_no_value()
    {
        var schema = Build(DataListFilter.Option(StatusOptions).WithFacets(), DataListFilter.Option(), DataListFilter.Boolean());
        var cut = RenderList("/t?f.status=open", schema);

        var row = Editor(cut, "status").QuerySelector("[data-slot=filter-include-empty-row]")!;
        Assert.Equal("8", Text(row.QuerySelector("[data-slot=filter-option-count]")!).TrimStart(',').Trim());
        Assert.NotNull(row.QuerySelector("[data-slot=filter-include-empty]"));
    }

    [Fact]
    public void Without_counts_the_include_empty_switch_has_no_count()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option(), DataListFilter.Boolean());
        var cut = RenderList("/t?f.status=open", schema);

        Assert.Empty(Editor(cut, "status").QuerySelectorAll("[data-slot=filter-include-empty-row]"));
        Assert.NotNull(Editor(cut, "status").QuerySelector("[data-slot=filter-include-empty]"));
    }

    // ---- exclude-self and include-self

    [Fact]
    public void Counts_leave_the_filters_own_rule_out_by_default_and_keep_it_in_include_self_mode()
    {
        var exclude = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option().WithFacets(), DataListFilter.Boolean());
        var cut = RenderList("/t?f.owner=ann", exclude);
        cut.WaitForAssertion(() => Assert.Equal(["bob", "ann", "cyd"], Options(Editor(cut, "owner"))));
        Assert.Equal(["14", "13", "13"], Counts(Editor(cut, "owner")));
        cut.Dispose();

        using var include = new FacetContext();
        include.Navigation.NavigateTo("/t?f.owner=ann");
        var withSelf = include.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Build(DataListFilter.Option(StatusOptions), DataListFilter.Option().WithFacets().WithFacetMode(FacetMode.IncludeSelf), DataListFilter.Boolean())));
        // IncludeSelf: only the tickets of ann are counted; the selected value is the only one with rows.
        withSelf.WaitForAssertion(() => Assert.Equal(["ann"], Options(Editor(withSelf, "owner"))));
        Assert.Equal(["13"], Counts(Editor(withSelf, "owner")));
    }

    private sealed class FacetContext : BluxToolsTestContext
    {
        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    // ---- the order and the limit

    [Fact]
    public void Declared_options_keep_their_order_unless_the_filter_sorts_them_by_count_or_label()
    {
        const string url = "/t?f.owner=ann&f.status=open";
        // With the filter on owner, the status counts are: open 2, paused 3, closed 2, archived 3 (the rows of ann).
        var declared = Build(DataListFilter.Option(StatusOptions).WithFacets(), DataListFilter.Option(), DataListFilter.Boolean());
        var cut = RenderList(url, declared);
        cut.WaitForAssertion(() => Assert.Equal(["Open", "Paused", "Closed", "Archived"], Options(Editor(cut, "status"))));
        Assert.Equal(["2", "3", "2", "3"], Counts(Editor(cut, "status")));
        cut.Dispose();

        using var byCount = new FacetContext();
        byCount.Navigation.NavigateTo(url);
        var counted = byCount.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Build(DataListFilter.Option(StatusOptions).WithFacets().WithOptionSort(DataListOptionSort.Count), DataListFilter.Option(), DataListFilter.Boolean())));
        counted.WaitForAssertion(() => Assert.Equal(["Archived", "Paused", "Closed", "Open"], Options(Editor(counted, "status"))));
        Assert.Equal(["3", "3", "2", "2"], Counts(Editor(counted, "status")));

        using var byLabel = new FacetContext();
        byLabel.Navigation.NavigateTo(url);
        var alpha = byLabel.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, Build(DataListFilter.Option(StatusOptions).WithFacets().WithOptionSort(DataListOptionSort.Alpha), DataListFilter.Option(), DataListFilter.Boolean())));
        alpha.WaitForAssertion(() => Assert.Equal(["Archived", "Closed", "Open", "Paused"], Options(Editor(alpha, "status"))));
    }

    [Fact]
    public void A_limit_never_holds_back_a_selected_option()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option().WithFacets().WithOptionLimit(2), DataListFilter.Boolean());
        var cut = RenderList("/t?f.owner=cyd", schema);

        // bob and ann come first by count, and cyd is selected: nothing is held back.
        cut.WaitForAssertion(() => Assert.Equal(["bob", "ann", "cyd"], Options(Editor(cut, "owner"))));
        Assert.Empty(Editor(cut, "owner").QuerySelectorAll("[data-slot=filter-options-more]"));
    }

    [Fact]
    public void The_show_more_button_lists_every_option()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option().WithFacets().WithOptionLimit(1), DataListFilter.Boolean());
        var cut = RenderList("/t?f.owner=bob", schema);

        cut.WaitForAssertion(() => Assert.Equal(["bob"], Options(Editor(cut, "owner"))));
        var more = Editor(cut, "owner").QuerySelector("[data-slot=filter-options-more]")!;
        Assert.Equal("Show 2 more", Text(more));

        more.Click();

        cut.WaitForAssertion(() => Assert.Equal(["bob", "ann", "cyd"], Options(Editor(cut, "owner"))));
        Assert.Empty(Editor(cut, "owner").QuerySelectorAll("[data-slot=filter-options-more]"));
    }

    // ---- the count is part of the accessible name

    private static string DescribedBy(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> cut, IElement control)
    {
        var id = control.GetAttribute("aria-describedby");
        Assert.False(string.IsNullOrEmpty(id), "the control has no description");
        return Text(cut.Find("#" + id));
    }

    [Fact]
    public void The_count_of_an_option_and_of_the_empty_values_is_the_description_of_their_checkbox_and_switch()
    {
        var schema = Build(DataListFilter.Option(StatusOptions).WithFacets(), DataListFilter.Option(), DataListFilter.Boolean());
        var cut = RenderList("/t?f.status=open", schema);
        var editor = Editor(cut, "status");

        var first = editor.QuerySelector("[data-slot=filter-options] > li[data-value=open]")!;
        Assert.Equal("8", DescribedBy(cut, first.QuerySelector("input[type=checkbox]")!));
        var empty = editor.QuerySelector("[data-slot=filter-include-empty-row]")!;
        Assert.Equal("8", DescribedBy(cut, empty.QuerySelector("input[role=switch]")!));
        // The number at the end of the row is not read a second time.
        Assert.All(editor.QuerySelectorAll("[data-slot=filter-option-count]"), c => Assert.Equal("true", c.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Without_counts_a_checkbox_has_no_description()
    {
        var schema = Build(DataListFilter.Option(StatusOptions), DataListFilter.Option(), DataListFilter.Boolean());
        var cut = RenderList("/t?f.status=open", schema);

        Assert.Null(Editor(cut, "status").QuerySelector("[data-slot=filter-options] > li[data-value=open] input[type=checkbox]")!.GetAttribute("aria-describedby"));
    }

    // ---- static rendering

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false) { }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    [Fact]
    public void Static_rendering_writes_the_counts_in_the_option_links_the_include_empty_link_and_the_boolean_links()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.status=open&f.active=true");
        var schema = Build(DataListFilter.Option(StatusOptions).WithFacets(), DataListFilter.Option(), DataListFilter.Boolean().WithFacets());

        var cut = context.Render<DataListFilterHarness>(p => p.Add(h => h.Schema, schema));

        var status = cut.Find("li[data-filter=status] [data-slot=filter-editor]");
        Assert.Equal(["Open", "Paused", "Closed", "Archived"], status.QuerySelectorAll("a[data-slot=filter-option]").Select(a => Text(a.QuerySelector("span.truncate")!)));
        // The counts leave out the rule on status and keep the one on active (every third ticket): open 2, paused 3, closed 2, archived 3, none 3.
        Assert.Equal(["2", "3", "2", "3"], status.QuerySelectorAll("a[data-slot=filter-option] [data-slot=filter-option-count]").Select(c => Text(c).TrimStart(',').Trim()));
        Assert.Equal("3", Text(status.QuerySelector("a[data-slot=filter-include-empty] [data-slot=filter-option-count]")!).TrimStart(',').Trim());
        var active = cut.Find("li[data-filter=active] [data-slot=filter-editor]");
        // The rule on status (8 tickets, 2 of them active) narrows the counts of active, not its own.
        Assert.Equal(["Yes (2)", "No (6)", "Any"], active.QuerySelectorAll("[data-slot=filter-choice]").Select(Text));
    }

    [Fact]
    public void Static_rendering_of_the_panel_describes_each_checkbox_by_its_count()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");
        var schema = Build(DataListFilter.Option(StatusOptions).WithFacets(), DataListFilter.Option(), DataListFilter.Boolean());

        var cut = context.Render<DataListPanelHarness>(p => p.Add(h => h.Schema, schema));

        var section = cut.Find("[data-filter=status] [data-slot=filter-options]");
        foreach (var input in section.QuerySelectorAll("[data-slot=filter-option-input], [data-slot=filter-include-empty]")) Assert.Equal("8", DescribedBy(cut, input));
        // Statically rendered links hold their count inside the link, so it is part of their name.
        Assert.Contains("8", Text(cut.Find("[data-filter=status] [data-slot=filter-options]")));
    }

    [Fact]
    public void Static_rendering_of_the_panel_writes_the_counts_in_the_checkboxes_and_the_yes_no_radios()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");
        var schema = Build(DataListFilter.Option(StatusOptions).WithFacets(), DataListFilter.Option(), DataListFilter.Boolean().WithFacets());

        var cut = context.Render<DataListPanelHarness>(p => p.Add(h => h.Schema, schema));

        var status = cut.Find("[data-filter=status] [data-slot=filter-options]");
        Assert.Equal(["8", "8", "8", "8", "8"], status.QuerySelectorAll("[data-slot=filter-option-count]").Select(c => Text(c).TrimStart(',').Trim()));
        var radios = cut.Find("[data-filter=active] [data-slot=filter-boolean]");
        Assert.Contains("Yes (13)", Text(radios));
        Assert.Contains("No (27)", Text(radios));
    }
}
