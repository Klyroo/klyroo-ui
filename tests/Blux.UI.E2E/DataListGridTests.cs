using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// <see cref="BxDataListLayoutSwitch{TItem}"/>, <see cref="BxDataListSortMenu{TItem}"/> and <see cref="BxDataListGrid{TItem}"/>
/// on <c>/components/data-list-grid</c>, across all four render modes, enhanced navigation, phone viewport and light/dark accessibility checks.
/// </summary>
public class DataListGridTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenPage(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-grid{query}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(SwitchList.Locator("[data-slot=skeleton-row], [data-slot=skeleton-card]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(GridOnlyList.Locator("[data-slot=skeleton-card]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator SwitchList => Page.GetByTestId("list-grid-switch");
    private ILocator GridOnlyList => Page.GetByTestId("list-grid-only");
    private ILocator GroupedList => Page.GetByTestId("list-grouped");
    private ILocator TreeList => Page.GetByTestId("list-tree");
    private ILocator SortPopover => Page.Locator("[data-slot=sort-content]:visible");

    private async Task DarkMode()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    [Fact]
    public async Task Statically_rendered_layout_switch_and_sort_menu_work_via_links()
    {
        await OpenPage("ssr");

        await Assertions.Expect(SwitchList.Locator("[data-slot=table-root]")).ToHaveCountAsync(1);
        await Assertions.Expect(SwitchList.Locator("[data-slot=grid-root]")).ToHaveCountAsync(0);

        // Switch to Grid view via the static link (?grid.l=grid).
        await SwitchList.Locator("a[data-slot=layout-option][data-layout=grid]").ClickAsync();
        await Page.WaitForUrlMatchingAsync(@"grid\.l=grid");
        await Assertions.Expect(SwitchList.Locator("[data-slot=table-root]")).ToHaveCountAsync(0);
        await Assertions.Expect(SwitchList.Locator("[data-slot=grid-card]")).ToHaveCountAsync(10);

        // Open the sort menu popover (native Popover API works without .NET) and click Customer sort link.
        await SwitchList.Locator("[data-slot=sort-trigger]").ClickAsync();
        await Assertions.Expect(SortPopover).ToBeVisibleAsync();
        await SortPopover.Locator("a[data-slot=sort-item][data-sort-key=customer]").ClickAsync();
        await Page.WaitForUrlMatchingAsync(@"grid\.s=customer");
        await Assertions.Expect(SwitchList.Locator("[data-slot=grid-card]").First.Locator("[data-slot=card-customer]")).ToHaveTextAsync("Acme Corp");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Layout_switch_and_sort_menu_and_grid_selection_and_card_removal_work_interactively(string mode)
    {
        await OpenPage(mode);

        // Switch to Grid view -> URL gains grid.l=grid and 10 cards render.
        await SwitchList.Locator("button[data-slot=layout-option][data-layout=grid]").ClickAsync();
        await Page.WaitForUrlMatchingAsync(@"grid\.l=grid");
        await Assertions.Expect(SwitchList.Locator("[data-slot=table-root]")).ToHaveCountAsync(0);
        var cards = SwitchList.Locator("[data-slot=grid-card]");
        await Assertions.Expect(cards).ToHaveCountAsync(10);

        // Sort by Amount descending from the sort menu.
        await SwitchList.Locator("[data-slot=sort-trigger]").ClickAsync();
        await Assertions.Expect(SortPopover).ToBeVisibleAsync();
        await SortPopover.Locator("button[data-slot=sort-item][data-sort-key=amount]").ClickAsync();
        await Page.WaitForUrlMatchingAsync(@"grid\.s=amount");
        await SortPopover.Locator("button[data-slot=sort-direction][data-direction=desc]").ClickAsync();
        await Page.WaitForUrlMatchingAsync(@"grid\.s=-amount");
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(SortPopover).ToBeHiddenAsync();

        // Select the first card in the grid -> selection bar appears.
        await cards.First.Locator("input[data-select=row]").CheckAsync();
        await Assertions.Expect(SwitchList.Locator("[data-slot=selection-bar]")).ToBeVisibleAsync();

        // Switch back to Table view and toggle Summary view with BxDataListViewToggle.
        await SwitchList.Locator("button[data-slot=layout-option][data-layout=table]").ClickAsync();
        await Assertions.Expect(SwitchList.Locator("[data-slot=table]")).ToHaveCountAsync(1);
        var viewToggle = SwitchList.Locator("button[data-slot=view-toggle]");
        await Assertions.Expect(viewToggle).ToHaveAttributeAsync("aria-pressed", "false");
        await viewToggle.ClickAsync();
        await Assertions.Expect(viewToggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(SwitchList.Locator("[data-slot=table]")).ToHaveCountAsync(0);
        await Assertions.Expect(SwitchList.Locator("[data-slot=summary-row]")).ToHaveCountAsync(10);
        await Assertions.Expect(SwitchList.Locator("[data-slot=summary-row]").First.Locator("[data-slot=summary-details]")).ToContainTextAsync("·");
        await viewToggle.ClickAsync();
        await Assertions.Expect(SwitchList.Locator("[data-slot=table]")).ToHaveCountAsync(1);

        // Removing a card in the standalone grid moves focus to the next card's Remove button.
        var removableCards = GridOnlyList.Locator("[data-slot=grid-card]");
        await Assertions.Expect(removableCards).ToHaveCountAsync(6);
        var firstRemove = removableCards.First.Locator("[data-slot=row-action][data-action=remove]");
        await firstRemove.FocusAsync();
        await firstRemove.ClickAsync();
        await Assertions.Expect(removableCards).ToHaveCountAsync(5);
        await Assertions.Expect(removableCards.First.Locator("[data-slot=row-action][data-action=remove]")).ToBeFocusedAsync();

        // Grouped list: Paid, Pending, Overdue (explicit Order), then Draft, then Other (~empty) last; fold/unfold and group selection work in Table and Grid views.
        var groupHeaders = GroupedList.Locator("[data-slot=group-header]");
        await Assertions.Expect(groupHeaders).ToHaveCountAsync(5);
        await Assertions.Expect(groupHeaders.Nth(0).Locator("[data-slot=group-label]")).ToHaveTextAsync("Paid");
        await Assertions.Expect(groupHeaders.Nth(3).Locator("[data-slot=group-label]")).ToHaveTextAsync("Draft");
        await Assertions.Expect(groupHeaders.Nth(4).Locator("[data-slot=group-label]")).ToHaveTextAsync("Other");

        // Tick the Paid group checkbox -> selects all rows in Paid.
        await GroupedList.Locator("input[data-select=group][data-group-key=Paid]").CheckAsync();
        await Assertions.Expect(GroupedList.Locator("[data-slot=selection-bar]")).ToBeVisibleAsync();

        // Fold the Paid group -> its rows collapse and aria-expanded becomes false.
        var paidToggle = GroupedList.Locator("button[data-slot=group-toggle][data-group-key=Paid]");
        await Assertions.Expect(paidToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await paidToggle.ClickAsync();
        await Assertions.Expect(paidToggle).ToHaveAttributeAsync("aria-expanded", "false");

        // Switch the grouped list to Grid view -> Paid stays folded and the remaining groups show their cards.
        await GroupedList.Locator("button[data-slot=layout-option][data-layout=grid]").ClickAsync();
        await Assertions.Expect(GroupedList.Locator("[data-slot=grid-group]")).ToHaveCountAsync(5);
        await Assertions.Expect(GroupedList.Locator("button[data-slot=group-toggle][data-group-key=Paid]")).ToHaveAttributeAsync("aria-expanded", "false");

        // Tree list: starts with DefaultExpandedDepth = 1 (6 visible rows: eng, eng-platform, eng-ui, ops, ops-sec, legal).
        await Assertions.Expect(TreeList.Locator("[data-slot=tree-cell]")).ToHaveCountAsync(6);
        var platformToggle = TreeList.Locator("button[data-slot=tree-toggle][data-key=eng-platform]");
        await Assertions.Expect(platformToggle).ToHaveAttributeAsync("aria-expanded", "false");

        // Keyboard ArrowRight expands Platform -> reveals Runtime and Infrastructure (8 visible rows); ArrowLeft collapses it back to 6.
        await platformToggle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(platformToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(TreeList.Locator("[data-slot=tree-cell]")).ToHaveCountAsync(8);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(platformToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(TreeList.Locator("[data-slot=tree-cell]")).ToHaveCountAsync(6);

        // Expand all and Collapse all buttons.
        await TreeList.Locator("[data-slot=tree-expand-all]").ClickAsync();
        await Assertions.Expect(TreeList.Locator("[data-slot=tree-cell]")).ToHaveCountAsync(8);
        await TreeList.Locator("[data-slot=tree-collapse-all]").ClickAsync();
        await Assertions.Expect(TreeList.Locator("[data-slot=tree-cell]")).ToHaveCountAsync(3);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_grid_stacks_in_one_column_and_the_table_automatically_shows_summary_rows_without_horizontal_overflow(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenPage(mode, "?grid.l=grid");

        await Assertions.Expect(SwitchList.Locator("[data-slot=grid-card]")).ToHaveCountAsync(10);
        Assert.False(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth"), "the grid page overflows horizontally at 375px");

        if (mode != "ssr")
        {
            // Switch to Table layout on a phone: AutoPhone automatically presents compact summary rows, and ViewToggle can switch to the full table.
            await SwitchList.Locator("button[data-slot=layout-option][data-layout=table]").ClickAsync();
            await Assertions.Expect(SwitchList.Locator("[data-slot=summary-row]")).ToHaveCountAsync(10);
            Assert.False(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth"), "the summary-row view overflows horizontally at 375px");
            await SwitchList.Locator("button[data-slot=view-toggle]").ClickAsync();
            await Assertions.Expect(SwitchList.Locator("[data-slot=table]")).ToHaveCountAsync(1);
        }
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Layout_and_sort_apply_after_an_enhanced_navigation(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-grid?grid.l=grid&grid.s=customer");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Assertions.Expect(SwitchList).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(SwitchList.Locator("[data-slot=grid-card]")).ToHaveCountAsync(10, new() { Timeout = 15_000 });
        await Assertions.Expect(SwitchList.Locator("[data-slot=grid-card]").First.Locator("[data-slot=card-customer]")).ToHaveTextAsync("Acme Corp");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Grid_and_sort_menu_page_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenPage(mode, "?grid.l=grid");
        await AssertNoAccessibilityViolations();
        if (mode != "ssr")
        {
            await SwitchList.Locator("[data-slot=sort-trigger]").ClickAsync();
            await Assertions.Expect(SortPopover).ToBeVisibleAsync();
            await AssertNoAccessibilityViolations();
            await Page.Keyboard.PressAsync("Escape");

            // Check summary rows view accessibility too.
            await SwitchList.Locator("button[data-slot=layout-option][data-layout=table]").ClickAsync();
            await SwitchList.Locator("button[data-slot=view-toggle]").ClickAsync();
            await Assertions.Expect(SwitchList.Locator("[data-slot=summary-row]")).ToHaveCountAsync(10);
            await AssertNoAccessibilityViolations();
        }
        await DarkMode();
        await AssertNoAccessibilityViolations();
    }
}
