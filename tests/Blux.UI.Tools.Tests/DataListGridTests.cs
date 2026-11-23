using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary><see cref="BxDataListSortMenu{TItem}"/>, <see cref="BxDataListLayoutSwitch{TItem}"/> and <see cref="BxDataListGrid{TItem}"/>.</summary>
public class DataListGridTests : BluxToolsTestContext
{
    [Fact]
    public void LayoutSwitch_toggles_between_Table_and_Grid_without_re_querying_the_provider()
    {
        var calls = 0;
        DataListProvider<Person> provider = (req, ct) =>
        {
            calls++;
            return ValueTask.FromResult(DataListControllerTests.Schema.ExecuteInMemory(DataListControllerTests.People[..6], req));
        };

        var cut = Render<DataListGridHarness>(p => p.Add(x => x.Provider, provider));

        Assert.Equal(1, calls);
        Assert.Single(cut.FindAll("[data-slot=table-root]"));
        Assert.Empty(cut.FindAll("[data-slot=grid-root]"));
        Assert.Equal("true", cut.Find("[data-slot=layout-option][data-layout=table]").GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.Find("[data-slot=layout-option][data-layout=grid]").GetAttribute("aria-pressed"));

        // Switch to Grid view: table hides, grid renders 6 cards, and the provider is not called a second time.
        cut.Find("[data-slot=layout-option][data-layout=grid]").Click();

        Assert.Equal(1, calls);
        Assert.Empty(cut.FindAll("[data-slot=table-root]"));
        Assert.Single(cut.FindAll("[data-slot=grid-root]"));
        Assert.Equal(6, cut.FindAll("[data-slot=grid-card]").Count);
        Assert.Equal("false", cut.Find("[data-slot=layout-option][data-layout=table]").GetAttribute("aria-pressed"));
        Assert.Equal("true", cut.Find("[data-slot=layout-option][data-layout=grid]").GetAttribute("aria-pressed"));

        // Default card renders the column titles and formatted values when no ItemTemplate is provided.
        var firstCard = cut.Find("[data-slot=grid-card]");
        Assert.Contains("Full name", firstCard.TextContent);
        Assert.Contains("Person 01", firstCard.TextContent);

        // Switch back to Table view: still 1 provider call.
        cut.Find("[data-slot=layout-option][data-layout=table]").Click();
        Assert.Equal(1, calls);
        Assert.Single(cut.FindAll("[data-slot=table-root]"));
        Assert.Empty(cut.FindAll("[data-slot=grid-root]"));
    }

    [Fact]
    public void A_table_only_list_keeps_showing_its_table_even_when_the_state_requests_grid_and_a_grid_only_list_shows_its_grid()
    {
        var tableOnly = Render<DataListGridHarness>(p => p.Add(x => x.IncludeGrid, false));
        tableOnly.Find("[data-slot=layout-option][data-layout=grid]").Click();
        Assert.Single(tableOnly.FindAll("[data-slot=table-root]"));

        var gridOnly = Render<DataListGridHarness>(p => p.Add(x => x.IncludeTable, false));
        Assert.Single(gridOnly.FindAll("[data-slot=grid-root]"));
        Assert.Equal(10, gridOnly.FindAll("[data-slot=grid-card]").Count);
    }

    [Fact]
    public void SortMenu_lists_sortable_columns_changes_field_and_direction_and_resets_to_default_sort()
    {
        var cut = Render<DataListGridHarness>();

        // Default sort of DataListControllerTests.Schema is "name" ascending; column title is "Full name".
        Assert.Contains("Sort by Full name", cut.Find("[data-slot=sort-trigger]").TextContent);
        Assert.True(cut.Find("[data-slot=sort-reset]").HasAttribute("disabled"));

        // City has Sortable="false" on its column, so only name and age appear in the sort menu.
        var items = cut.FindAll("[data-slot=sort-item]");
        Assert.Equal(["name", "age"], items.Select(i => i.GetAttribute("data-sort-key")));
        Assert.Equal(["Full name", "Age"], items.Select(i => i.TextContent.Trim()));
        Assert.Equal("true", cut.Find("[data-slot=sort-item][data-sort-key=name]").GetAttribute("aria-current"));

        // Click Age -> sorts by Age ascending.
        cut.Find("[data-slot=sort-item][data-sort-key=age]").Click();
        Assert.Contains("Sort by Age", cut.Find("[data-slot=sort-trigger]").TextContent);
        Assert.False(cut.Find("[data-slot=sort-reset]").HasAttribute("disabled"));
        Assert.Equal("true", cut.Find("[data-slot=sort-direction][data-direction=asc]").GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.Find("[data-slot=sort-direction][data-direction=desc]").GetAttribute("aria-pressed"));

        // Switch direction to Descending -> sorts by Age descending (oldest first among first 10 people).
        cut.Find("[data-slot=sort-direction][data-direction=desc]").Click();
        Assert.Equal("true", cut.Find("[data-slot=sort-direction][data-direction=desc]").GetAttribute("aria-pressed"));
        Assert.Contains("Person 10", cut.FindAll("tbody tr[data-slot=row]")[0].TextContent);

        // Reset restores default sort (name ascending).
        cut.Find("[data-slot=sort-reset]").Click();
        Assert.Contains("Sort by Full name", cut.Find("[data-slot=sort-trigger]").TextContent);
        Assert.True(cut.Find("[data-slot=sort-reset]").HasAttribute("disabled"));
        Assert.Contains("Person 01", cut.FindAll("tbody tr[data-slot=row]")[0].TextContent);
    }

    [Fact]
    public void SortMenu_respects_Layouts_filter_and_explicit_Options()
    {
        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.SortLayouts, [DataListLayout.Grid])
            .Add(x => x.SortOptions, [new DataListSortOption("age", "Years"), new DataListSortOption("unknown", "Ignored")]));

        // In Table view, SortMenu is hidden because SortLayouts only lists Grid.
        Assert.Empty(cut.FindAll("[data-slot=sort-menu]"));

        // Switch to Grid view -> SortMenu appears with only the valid explicit option ("age" -> "Years").
        cut.Find("[data-slot=layout-option][data-layout=grid]").Click();
        Assert.Single(cut.FindAll("[data-slot=sort-menu]"));
        var items = cut.FindAll("[data-slot=sort-item]");
        Assert.Single(items);
        Assert.Equal("age", items[0].GetAttribute("data-sort-key"));
        Assert.Equal("Years", items[0].TextContent.Trim());
    }

    [Fact]
    public void Grid_supports_selection_row_actions_custom_ItemTemplate_and_filtered_empty_state()
    {
        var executed = new List<int>();
        var actions = new DataListRowAction<Person>[]
        {
            new() { Key = "ping", Label = "Ping", OnExecute = ctx => { executed.Add(ctx.Item.Id); return Task.CompletedTask; } },
        };

        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.IncludeTable, false)
            .Add(x => x.Selectable, true)
            .Add(x => x.RowActions, actions)
            .Add(x => x.RowActionsMaxVisible, 1)
            .Add(x => x.ItemTemplate, person => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddAttribute(1, "data-custom-card", person.Name);
                builder.AddContent(2, person.Name);
                builder.CloseElement();
            }));

        Assert.Equal(10, cut.FindAll("[data-custom-card]").Count);
        Assert.Single(cut.FindAll("[data-slot=grid-header] input[data-select=all]"));
        Assert.Equal(10, cut.FindAll("[data-slot=grid-card] input[data-select=row]").Count);

        // Select row 1 in the grid.
        cut.FindAll("[data-slot=grid-card] input[data-select=row]")[0].Change(true);
        Assert.True(cut.FindAll("[data-slot=grid-card] input[data-select=row]")[0].HasAttribute("checked"));

        // Trigger row action on card 1.
        cut.FindAll("[data-slot=grid-card] [data-slot=row-action][data-action=ping]")[0].Click();
        Assert.Equal([1], executed);

        // Search for something that matches nothing -> shows filtered empty state inside the grid.
        cut.Find("[data-slot=search] input[type=search]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "zzzz-no-match" });
        var empty = cut.Find("[data-slot=empty-grid]");
        Assert.Contains("zzzz-no-match", empty.TextContent);
        cut.Find("[data-slot=empty-grid] [data-slot=reset]").Click();
        Assert.Equal(10, cut.FindAll("[data-slot=grid-card]").Count);
    }

    [Fact]
    public void RowSummary_and_ViewToggle_switch_between_table_and_summary_rows_persist_to_cookie_and_hide_in_Grid_layout()
    {
        Person? selected = null;
        var summary = new DataListRowSummary<Person>
        {
            Title = p => p.Name,
            Leading = p => builder => builder.AddContent(0, $"#{p.Id}"),
            Status = p => builder => builder.AddContent(0, p.City),
            Details = p => [$"{p.Age} yrs", "Active"],
            OnSelect = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Person>(this, p => selected = p),
            AutoPhone = false,
        };

        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.RowSummary, summary)
            .Add(x => x.PreferencesKey, "people-summary")
            .Add(x => x.Selectable, true)
            .Add(x => x.SortLayouts, [DataListLayout.Grid]));

        // Starts in full Table view on desktop: ViewToggle is visible and unpressed, SortMenu is hidden (SortLayouts = [Grid]).
        var toggle = cut.Find("button[data-slot=view-toggle]");
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("[data-slot=table]"));
        Assert.Empty(cut.FindAll("[data-slot=summary-list]"));
        Assert.Empty(cut.FindAll("[data-slot=sort-menu]"));

        // Click ViewToggle -> switches to Summary rows view, shows SortMenu (since table headers are gone), and persists v1!m1 to cookie.
        toggle.Click();

        Assert.Equal("true", cut.Find("button[data-slot=view-toggle]").GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll("[data-slot=table]"));
        Assert.Single(cut.FindAll("[data-slot=summary-list]"));
        Assert.Equal(10, cut.FindAll("[data-slot=summary-row]").Count);
        Assert.Single(cut.FindAll("[data-slot=sort-menu]"));

        var firstRow = cut.Find("[data-slot=summary-row]");
        Assert.Equal("#1", firstRow.QuerySelector("[data-slot=summary-leading]")?.TextContent);
        Assert.Equal("Person 01", firstRow.QuerySelector("[data-slot=summary-title]")?.TextContent);
        Assert.Equal("Lyon", firstRow.QuerySelector("[data-slot=summary-status]")?.TextContent);
        Assert.Equal("21 yrs · Active", firstRow.QuerySelector("[data-slot=summary-details]")?.TextContent);

        // Clicking the summary title invokes OnSelect.
        firstRow.QuerySelector("button[data-slot=summary-title]")!.Click();
        Assert.Equal(1, selected?.Id);

        // Cookie wrote v1!m1.
        var write = JSInterop.Invocations["Blux.tools.writeColumnPrefs"].Last();
        Assert.Equal("people-summary", write.Arguments[0]);
        Assert.Equal("v1!m1", write.Arguments[1]);

        // Switching to Grid layout hides ViewToggle.
        cut.Find("[data-slot=layout-option][data-layout=grid]").Click();
        Assert.Empty(cut.FindAll("[data-slot=view-toggle]"));
    }
}

/// <summary>Static SSR behavior of <see cref="BxDataListLayoutSwitch{TItem}"/>, <see cref="BxDataListSortMenu{TItem}"/> and <see cref="BxDataListGrid{TItem}"/>.</summary>
public class DataListGridStaticTests : BluxToolsTestContext
{
    public DataListGridStaticTests() : base(interactive: false) { }

    [Fact]
    public async Task Statically_rendered_layout_switch_and_sort_menu_render_links_and_grid_renders_cards_from_url()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/people");

        var tableCut = Render<DataListGridHarness>(p => p.Add(x => x.UrlSync, true));
        Assert.Single(tableCut.FindAll("[data-slot=table-root]"));
        Assert.Empty(tableCut.FindAll("[data-slot=grid-root]"));

        var gridLink = tableCut.Find("a[data-slot=layout-option][data-layout=grid]");
        var gridHref = gridLink.GetAttribute("href");
        Assert.NotNull(gridHref);
        Assert.Contains("l=grid", gridHref);

        var ageSortLink = tableCut.Find("a[data-slot=sort-item][data-sort-key=age]");
        Assert.Contains("s=age", ageSortLink.GetAttribute("href") ?? "");

        // Follow the grid layout link in SSR.
        await DisposeComponentsAsync();
        nav.NavigateTo(new Uri(new Uri("http://localhost/"), gridHref).ToString());
        var gridCut = Render<DataListGridHarness>(p => p.Add(x => x.UrlSync, true));

        Assert.Empty(gridCut.FindAll("[data-slot=table-root]"));
        Assert.Single(gridCut.FindAll("[data-slot=grid-root]"));
        Assert.Equal(10, gridCut.FindAll("[data-slot=grid-card]").Count);
        Assert.Equal("true", gridCut.Find("a[data-slot=layout-option][data-layout=grid]").GetAttribute("aria-current"));
    }
}
