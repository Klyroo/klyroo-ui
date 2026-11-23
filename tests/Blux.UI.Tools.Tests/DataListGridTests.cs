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

    [Fact]
    public void GroupBy_orders_groups_places_empty_last_folds_selects_groups_skips_collapsed_in_shift_range_and_persists_toggles()
    {
        var items = new List<Person>
        {
            new(1, "Alice", 30, "Lyon"),
            new(2, "Bob", 25, "Paris"),
            new(3, "Charlie", 35, ""),
            new(4, "Diana", 28, "Lille"),
            new(5, "Eve", 40, "Paris"),
        };

        var groupBy = new DataListGroupBy<Person>
        {
            Field = "city",
            Order = ["Paris"],
        };

        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.GroupBy, groupBy)
            .Add(x => x.PreferencesKey, "people-groups")
            .Add(x => x.Selectable, true));

        // Order: Paris first (explicit Order), then Lille, then Lyon (alphabetical), then Other (~empty) last.
        var headers = cut.FindAll("[data-slot=group-header]");
        Assert.Equal(4, headers.Count);
        Assert.Equal(["Paris", "Lille", "Lyon", "~empty"], headers.Select(h => h.GetAttribute("data-group-key")).ToArray());
        Assert.Equal(["Paris", "Lille", "Lyon", "Other"], headers.Select(h => h.QuerySelector("[data-slot=group-label]")?.TextContent).ToArray());
        Assert.Equal(["2", "1", "1", "1"], headers.Select(h => h.QuerySelector("[data-slot=group-summary]")?.TextContent).ToArray());

        // Select the Paris group via its group checkbox -> ticks Bob (2) and Eve (5).
        cut.Find("input[data-select=group][data-group-key=Paris]").Change(true);
        Assert.True(cut.Find("input[data-select=row][data-key='2']").HasAttribute("checked"));
        Assert.True(cut.Find("input[data-select=row][data-key='5']").HasAttribute("checked"));
        Assert.False(cut.Find("input[data-select=row][data-key='4']").HasAttribute("checked"));

        // Untick Eve (5) -> Paris group checkbox becomes indeterminate (data-indeterminate="1").
        cut.Find("input[data-select=row][data-key='5']").Change(false);
        var parisGroupCheckbox = cut.Find("input[data-select=group][data-group-key=Paris]");
        Assert.Equal("1", parisGroupCheckbox.GetAttribute("data-indeterminate"));

        // Fold Lille group -> its row (Diana, key 4) disappears from the DOM and cookie persists v1!gLille.
        var lilleToggle = cut.Find("button[data-slot=group-toggle][data-group-key=Lille]");
        Assert.Equal("true", lilleToggle.GetAttribute("aria-expanded"));
        lilleToggle.Click();
        Assert.Equal("false", cut.Find("button[data-slot=group-toggle][data-group-key=Lille]").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("input[data-select=row][data-key='4']"));

        var write = JSInterop.Invocations["Blux.tools.writeColumnPrefs"].Last();
        Assert.Equal("people-groups", write.Arguments[0]);
        Assert.Equal("v1!gLille", write.Arguments[1]);

        // Shift-click range from Eve (5, in Paris) to Alice (1, in Lyon) skips Diana (4, in folded Lille).
        var eveBox = cut.Find("input[data-select=row][data-key='5']");
        eveBox.Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ShiftKey = false });
        eveBox.Change(true);

        var aliceBox = cut.Find("input[data-select=row][data-key='1']");
        aliceBox.Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ShiftKey = true });
        aliceBox.Change(true);

        // Unfold Lille and verify Diana (4) was NOT selected while folded.
        cut.Find("button[data-slot=group-toggle][data-group-key=Lille]").Click();
        Assert.True(cut.Find("input[data-select=row][data-key='5']").HasAttribute("checked"));
        Assert.True(cut.Find("input[data-select=row][data-key='1']").HasAttribute("checked"));
        Assert.False(cut.Find("input[data-select=row][data-key='4']").HasAttribute("checked"));

        // Switch to Grid layout -> renders 4 grid-group sections with cards.
        cut.Find("[data-slot=layout-option][data-layout=grid]").Click();
        Assert.Equal(4, cut.FindAll("[data-slot=grid-group]").Count);
        Assert.Equal(5, cut.FindAll("[data-slot=grid-card]").Count);
    }

    [Fact]
    public void Tree_expands_and_collapses_via_click_and_arrow_keys_draws_rails_deduplicates_ids_supports_roots_selection_and_disables_summary_and_groups()
    {
        var roots = new List<Person>
        {
            new(1, "Engineering", 40, "Paris"),
            new(2, "Operations", 35, "Lyon"),
        };
        var childrenByParent = new Dictionary<int, IReadOnlyList<Person>>
        {
            [1] =
            [
                new(11, "Platform", 28, "Paris"),
                new(12, "Frontend", 30, "Paris"),
                new(1, "DuplicateIdShouldBeDropped", 99, "Paris"),
            ],
            [11] =
            [
                new(111, "Runtime", 26, "Paris"),
            ],
        };

        var tree = new DataListTree<Person>
        {
            Children = p => childrenByParent.TryGetValue(p.Id, out var c) ? c : null,
            DefaultExpandedDepth = 1,
            Selectable = DataListTreeSelectionMode.All,
        };
        var summary = new DataListRowSummary<Person> { Title = p => p.Name, DefaultActive = true };
        var groupBy = new DataListGroupBy<Person> { Field = "city" };

        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.Items, roots)
            .Add(x => x.Tree, tree)
            .Add(x => x.RowSummary, summary)
            .Add(x => x.GroupBy, groupBy)
            .Add(x => x.Selectable, true));

        // Tree wins over RowSummary and GroupBy: no ViewToggle, no summary-list, no group-header.
        Assert.Empty(cut.FindAll("[data-slot=view-toggle]"));
        Assert.Empty(cut.FindAll("[data-slot=summary-list]"));
        Assert.Empty(cut.FindAll("[data-slot=group-header]"));

        // DefaultExpandedDepth = 1 expands Engineering (depth 0), showing Platform (11) and Frontend (12), while duplicate id 1 is dropped and Runtime (111, depth 2) is collapsed.
        var treeCells = cut.FindAll("[data-slot=tree-cell]");
        Assert.Equal(4, treeCells.Count);
        Assert.Equal(["0", "1", "1", "0"], treeCells.Select(c => c.GetAttribute("data-depth")).ToArray());

        // Platform (11) has a chevron button with aria-expanded="false"; pressing ArrowRight expands it to reveal Runtime (111).
        var platformToggle = cut.Find("button[data-slot=tree-toggle][data-key='11']");
        Assert.Equal("false", platformToggle.GetAttribute("aria-expanded"));
        platformToggle.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("true", cut.Find("button[data-slot=tree-toggle][data-key='11']").GetAttribute("aria-expanded"));
        Assert.Equal(5, cut.FindAll("[data-slot=tree-cell]").Count);

        // Runtime (111) at depth 2 has 1 ancestor continuation rail (since Platform has sibling Frontend below it) and a last-child rail.
        var runtimeCell = cut.FindAll("[data-slot=tree-cell]")[2];
        Assert.Equal("2", runtimeCell.GetAttribute("data-depth"));
        Assert.Equal("true", runtimeCell.QuerySelector("[data-slot=tree-rail-indent]")?.GetAttribute("data-continue"));
        Assert.Equal("true", runtimeCell.QuerySelector("[data-slot=tree-rail]")?.GetAttribute("data-last"));

        // Pressing ArrowLeft on Platform (11) collapses it again -> Runtime (111) is hidden.
        cut.Find("button[data-slot=tree-toggle][data-key='11']").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal(4, cut.FindAll("[data-slot=tree-cell]").Count);

        // Shift-click range from Platform (11) to Frontend (12) does NOT select hidden child Runtime (111).
        var platformBox = cut.Find("input[data-select=row][data-key='11']");
        platformBox.Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ShiftKey = false });
        platformBox.Change(true);

        var frontendBox = cut.Find("input[data-select=row][data-key='12']");
        frontendBox.Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ShiftKey = true });
        frontendBox.Change(true);

        // Expand Platform (11) by clicking its chevron -> Runtime (111) is unselected.
        cut.Find("button[data-slot=tree-toggle][data-key='11']").Click();
        Assert.True(cut.Find("input[data-select=row][data-key='11']").HasAttribute("checked"));
        Assert.True(cut.Find("input[data-select=row][data-key='12']").HasAttribute("checked"));
        Assert.False(cut.Find("input[data-select=row][data-key='111']").HasAttribute("checked"));
    }

    [Fact]
    public void Summaries_compute_page_filtered_and_selection_scopes_and_support_async_resolve()
    {
        var items = DataListControllerTests.People[..15];
        var summaries = new DataListSummaries<Person>
        {
            Scopes = [DataListSummaryScope.Page, DataListSummaryScope.Filtered, DataListSummaryScope.Selection],
        };

        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.Summaries, summaries)
            .Add(x => x.AgeSummary, DataListSummaryKind.Sum)
            .Add(x => x.CitySummary, DataListSummaryKind.Count)
            .Add(x => x.Selectable, true));

        var footerRows = cut.FindAll("[data-slot=summary-footer-row]");
        Assert.Equal(3, footerRows.Count);
        Assert.Equal(["page", "filtered", "selection"], footerRows.Select(r => r.GetAttribute("data-scope")).ToArray());

        // Page scope aggregates the first 10 rows (Ages 21..30 -> sum 255, count 10 rows).
        var pageRow = cut.Find("[data-slot=summary-footer-row][data-scope=page]");
        Assert.Equal("Page", pageRow.QuerySelector("[data-slot=summary-label]")?.TextContent);
        Assert.Equal("255", pageRow.QuerySelector("[data-slot=summary-cell][data-key=age]")?.TextContent);
        Assert.Equal("10 rows", pageRow.QuerySelector("[data-slot=summary-cell][data-key=city]")?.TextContent);

        // Filtered scope aggregates all 15 matching rows (Ages 21..35 -> sum 420, count 15 rows).
        var filteredRow = cut.Find("[data-slot=summary-footer-row][data-scope=filtered]");
        Assert.Equal("Filtered", filteredRow.QuerySelector("[data-slot=summary-label]")?.TextContent);
        Assert.Equal("420", filteredRow.QuerySelector("[data-slot=summary-cell][data-key=age]")?.TextContent);
        Assert.Equal("15 rows", filteredRow.QuerySelector("[data-slot=summary-cell][data-key=city]")?.TextContent);

        // Selection scope starts at 0, then updates when row 1 (Age 21) is ticked.
        var selectionRow = cut.Find("[data-slot=summary-footer-row][data-scope=selection]");
        Assert.Equal("0", selectionRow.QuerySelector("[data-slot=summary-cell][data-key=age]")?.TextContent);
        Assert.Equal("0 rows", selectionRow.QuerySelector("[data-slot=summary-cell][data-key=city]")?.TextContent);

        cut.Find("input[data-select=row][data-key='1']").Change(true);
        selectionRow = cut.Find("[data-slot=summary-footer-row][data-scope=selection]");
        Assert.Equal("21", selectionRow.QuerySelector("[data-slot=summary-cell][data-key=age]")?.TextContent);
        Assert.Equal("1 row", selectionRow.QuerySelector("[data-slot=summary-cell][data-key=city]")?.TextContent);

        // Async ResolveAsync overrides computed values.
        var asyncSummaries = new DataListSummaries<Person>
        {
            Scope = DataListSummaryScope.Filtered,
            Label = "Server total",
            ResolveAsync = _ => ValueTask.FromResult<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>
            {
                ["age"] = 9999m,
                ["city"] = 42,
            }),
        };

        var asyncCut = Render<DataListGridHarness>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.Summaries, asyncSummaries)
            .Add(x => x.AgeSummary, DataListSummaryKind.Sum)
            .Add(x => x.CitySummary, DataListSummaryKind.Count));

        var asyncRow = asyncCut.Find("[data-slot=summary-footer-row][data-scope=filtered]");
        Assert.Equal("Server total", asyncRow.QuerySelector("[data-slot=summary-label]")?.TextContent);
        Assert.Equal("9999", asyncRow.QuerySelector("[data-slot=summary-cell][data-key=age]")?.TextContent);
        Assert.Equal("42 rows", asyncRow.QuerySelector("[data-slot=summary-cell][data-key=city]")?.TextContent);
    }

    [Fact]
    public async Task UpdateRow_UpdateRows_and_ResetAll_update_loaded_rows_in_place_and_reset_query_selection_and_expansion()
    {
        var calls = 0;
        DataListProvider<Person> provider = (req, _) =>
        {
            calls++;
            return ValueTask.FromResult(DataListGridHarness.DefaultSchema.ExecuteInMemory(DataListControllerTests.People[..5], req));
        };

        var childrenByParent = new Dictionary<int, IReadOnlyList<Person>>
        {
            [1] = [new(11, "Child 11", 20, "Paris")],
        };
        var tree = new DataListTree<Person>
        {
            Children = p => childrenByParent.TryGetValue(p.Id, out var c) ? c : null,
            DefaultExpanded = false,
        };

        var cut = Render<DataListGridHarness>(p => p
            .Add(x => x.Provider, provider)
            .Add(x => x.Tree, tree)
            .Add(x => x.Selectable, true));

        Assert.Equal(1, calls);
        var list = cut.FindComponent<BxDataList<Person>>().Instance;

        // Tick row 1 and expand it.
        cut.Find("input[data-select=row][data-key='1']").Change(true);
        cut.Find("button[data-slot=tree-toggle][data-key='1']").Click();
        Assert.Equal(6, cut.FindAll("[data-slot=tree-cell]").Count);

        // UpdateRow replaces Person 1 in place without calling the provider again.
        await cut.InvokeAsync(() => Assert.True(list.UpdateRow(new Person(1, "Person 01 Updated", 99, "Nice"))));
        cut.Render();
        Assert.Equal(1, calls);
        Assert.Contains("Person 01 Updated", cut.Markup);

        // ResetAll clears selection, collapses tree back to DefaultExpanded = false, and resets query state.
        await cut.InvokeAsync(() => list.ResetAll());
        cut.Render();
        Assert.Equal(5, cut.FindAll("[data-slot=tree-cell]").Count);
        Assert.False(cut.Find("input[data-select=row][data-key='1']").HasAttribute("checked"));
    }

    [Fact]
    public void HeaderMenu_SkeletonKinds_EmptyState_with_first_use_action_and_ProgressLine_work()
    {
        // 1. Per-column skeleton kinds while initial load is in flight.
        var tcs = new TaskCompletionSource<DataListResult<Person>>();
        var initialCut = Render<DataListGridHarness>(p => p
            .Add(x => x.Provider, (_, _) => new ValueTask<DataListResult<Person>>(tcs.Task))
            .Add(x => x.NameSkeleton, DataListSkeletonKind.Avatar)
            .Add(x => x.AgeSkeleton, DataListSkeletonKind.Number)
            .Add(x => x.CitySkeleton, DataListSkeletonKind.Badge));

        var firstSkeletonRow = initialCut.Find("[data-slot=skeleton-row]");
        var skeletonCells = firstSkeletonRow.QuerySelectorAll("td[data-skeleton-kind]");
        Assert.Equal(["avatar", "number", "badge"], skeletonCells.Select(c => c.GetAttribute("data-skeleton-kind")).ToArray());

        // 2. EmptyState with first-use Action shows the action on an unfiltered empty list, and hides it when a search filter is active.
        var created = false;
        var emptyState = new DataListEmptyState
        {
            Title = "No people yet",
            Description = "Add your first person to get started.",
            Action = new DataListEmptyAction
            {
                Label = "Create person",
                Icon = "plus",
                OnClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => created = true),
            },
        };

        var emptyCut = Render<DataListGridHarness>(p => p
            .Add(x => x.Items, Array.Empty<Person>())
            .Add(x => x.EmptyState, emptyState));

        Assert.Contains("No people yet", emptyCut.Markup);
        var createBtn = emptyCut.Find("[data-slot=empty-action]");
        Assert.Contains("Create person", createBtn.TextContent);
        createBtn.Click();
        Assert.True(created);

        // When searched, the filtered empty state replaces the first-use empty state and omits [data-slot=empty-action].
        emptyCut.Find("[data-slot=search] input[type=search]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "Alice" });
        Assert.Empty(emptyCut.FindAll("[data-slot=empty-action]"));
        Assert.Single(emptyCut.FindAll("[data-slot=reset]"));

        // 3. HeaderMenu renders per-column dropdown menu triggers in interactive mode.
        var menuCut = Render<DataListGridHarness>(p => p.Add(x => x.HeaderMenu, true));
        Assert.Equal(3, menuCut.FindAll("[data-slot=header-menu-trigger]").Count);
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
