using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// Row selection and actions of the data list on its playground page (data-list-selection), in the four render modes. "list-selection" is a list in memory (URL prefix "sel", 120 invoices, a delete with
/// a confirmation, a toolbar menu), "list-selection-provider" a provider with bound keys (prefix "selp"). Selection and actions are interactive only: statically rendered there is no checkbox column, no
/// bar and no menu. Ticking, the header box, Shift ranges, the keyboard, the scope rules, pruning, the every-result scope with a provider, the dialog, an enhanced navigation and accessibility in light and dark.
/// </summary>
public class DataListSelectionTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-selection{query}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(Page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator Memory => Page.GetByTestId("list-selection");

    private ILocator Provider => Page.GetByTestId("list-selection-provider");

    private static ILocator Rows(ILocator list) => list.Locator("tbody tr[data-slot=row]");

    private static ILocator Box(ILocator list, int id) => list.Locator($"input[data-select=row][data-key='{id}']");

    private static ILocator Header(ILocator list) => list.Locator("input[data-select=all]");

    private static ILocator Status(ILocator list) => list.Locator("[data-slot=selection-status]");

    private static ILocator Bar(ILocator list) => list.Locator("[data-slot=selection-bar]");

    private static ILocator Count(ILocator list) => list.Locator("[data-slot=result-count]");

    private async Task DarkMode()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    [Fact]
    public async Task Statically_rendered_there_is_no_checkbox_column_no_bar_and_no_menu()
    {
        await OpenList("ssr");

        await Assertions.Expect(Rows(Memory)).ToHaveCountAsync(10);
        await Assertions.Expect(Memory.Locator("input[type=checkbox]")).ToHaveCountAsync(0);
        await Assertions.Expect(Memory.Locator("[data-slot=selection-actions]")).ToHaveCountAsync(0);
        await Assertions.Expect(Memory.Locator("[data-slot=actions-dropdown]")).ToHaveCountAsync(0);
        await Assertions.Expect(Provider.Locator("input[type=checkbox]")).ToHaveCountAsync(0);
        // The columns are the ones of the data: no empty first column.
        await Assertions.Expect(Memory.Locator("thead th").First).ToContainTextAsync("No.");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Ticking_a_row_shows_the_bar_and_announces_the_count(string mode)
    {
        await OpenList(mode);

        await Assertions.Expect(Status(Memory)).ToHaveAttributeAsync("role", "status");
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("");
        await Assertions.Expect(Bar(Memory)).ToHaveCountAsync(0);
        await Assertions.Expect(Box(Memory, 3)).ToHaveAccessibleNameAsync("Select Invoice 3");

        await Box(Memory, 3).CheckAsync();

        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("1 selected");
        await Assertions.Expect(Bar(Memory)).ToBeVisibleAsync();
        await Assertions.Expect(Memory.GetByRole(AriaRole.Button, new() { Name = "Select all 120 results" })).ToBeVisibleAsync();

        await Box(Memory, 3).UncheckAsync();

        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("");
        await Assertions.Expect(Bar(Memory)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_header_box_selects_the_page_and_is_mixed_while_some_rows_are_ticked(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Header(Memory)).ToHaveAccessibleNameAsync("Select all rows on this page");

        await Box(Memory, 2).CheckAsync();
        await Assertions.Expect(Header(Memory)).ToHaveAttributeAsync("data-indeterminate", new Regex(".*"));
        Assert.True(await Header(Memory).EvaluateAsync<bool>("e => e.indeterminate"));

        await Header(Memory).CheckAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("10 selected");
        await Assertions.Expect(Header(Memory)).ToBeCheckedAsync();
        Assert.False(await Header(Memory).EvaluateAsync<bool>("e => e.indeterminate"));

        await Box(Memory, 5).UncheckAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("9 selected");
        Assert.True(await Header(Memory).EvaluateAsync<bool>("e => e.indeterminate"));

        // From the mixed state a click ticks the page, and the next one unticks it.
        await Header(Memory).ClickAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("10 selected");
        await Header(Memory).ClickAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Shift_click_ticks_the_range_over_the_rows_on_screen(string mode)
    {
        await OpenList(mode);

        await Box(Memory, 2).CheckAsync();
        await Box(Memory, 6).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("5 selected");
        foreach (var id in new[] { 2, 3, 4, 5, 6 }) await Assertions.Expect(Box(Memory, id)).ToBeCheckedAsync();
        await Assertions.Expect(Box(Memory, 7)).Not.ToBeCheckedAsync();

        // Shift + click on a ticked row unticks the range up to it, from the row touched last.
        await Box(Memory, 3).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("1 selected");
        await Assertions.Expect(Box(Memory, 2)).ToBeCheckedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_space_key_ticks_a_focused_row_and_the_bar_is_reachable_with_the_keyboard(string mode)
    {
        await OpenList(mode);

        await Box(Memory, 4).FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(Box(Memory, 4)).ToBeCheckedAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("1 selected");

        // Tab from the table region reaches the bar's buttons in order: the every-result switch, Export, Delete, Clear selection.
        var names = new List<string>();
        await Bar(Memory).GetByRole(AriaRole.Button).First.FocusAsync();
        for (var i = 0; i < 4; i++)
        {
            names.Add(await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()"));
            await Page.Keyboard.PressAsync("Tab");
        }
        Assert.Equal(["Select all 120 results", "Export", "Delete", "Clear selection"], names);

        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Clear selection" }).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_list_in_memory_keeps_the_selection_across_pages_and_a_search_drops_the_rows_that_no_longer_match(string mode)
    {
        await OpenList(mode);

        await Header(Memory).CheckAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("10 selected");
        await Memory.GetByRole(AriaRole.Button, new() { Name = "Page 2", Exact = true }).ClickAsync();
        await Assertions.Expect(Box(Memory, 12)).ToBeVisibleAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("10 selected");
        await Box(Memory, 12).CheckAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("11 selected");

        // Invoices 1 to 10 and 12: Globex has the ids 2, 14, 26... so only invoice 2 matches "Globex" and is selected.
        await Memory.GetByRole(AriaRole.Searchbox).FillAsync("Globex");
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("1 selected", new() { Timeout = 15_000 });
        await Assertions.Expect(Box(Memory, 2)).ToBeCheckedAsync();

        // A search that matches none of them empties the selection and takes the bar away.
        await Memory.GetByRole(AriaRole.Searchbox).FillAsync("zzzzqq");
        await Assertions.Expect(Bar(Memory)).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_provider_list_keeps_only_the_rows_of_the_page(string mode)
    {
        await OpenList(mode);

        await Box(Provider, 1).CheckAsync();
        await Box(Provider, 2).CheckAsync();
        await Assertions.Expect(Page.GetByTestId("selp-keys")).ToHaveTextAsync("Selected keys: 1, 2");
        await Provider.GetByRole(AriaRole.Button, new() { Name = "Page 2", Exact = true }).ClickAsync();
        await Assertions.Expect(Box(Provider, 12)).ToBeVisibleAsync();

        await Assertions.Expect(Status(Provider)).ToHaveTextAsync("");
        await Assertions.Expect(Page.GetByTestId("selp-keys")).ToHaveTextAsync("Selected keys: none");
        await Assertions.Expect(Bar(Provider)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_keys_are_set_from_outside_and_reported_back(string mode)
    {
        await OpenList(mode);

        await Page.GetByTestId("selp-two").ClickAsync();

        await Assertions.Expect(Page.GetByTestId("selp-keys")).ToHaveTextAsync("Selected keys: 2, 4");
        await Assertions.Expect(Box(Provider, 2)).ToBeCheckedAsync();
        await Assertions.Expect(Box(Provider, 4)).ToBeCheckedAsync();
        await Assertions.Expect(Status(Provider)).ToHaveTextAsync("2 selected");

        await Box(Provider, 5).CheckAsync();
        await Assertions.Expect(Page.GetByTestId("selp-keys")).ToHaveTextAsync("Selected keys: 2, 4, 5");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Every_result_over_a_provider_sends_the_request_and_follows_the_filters(string mode)
    {
        await OpenList(mode, "?selp.f.status=Paid");
        await Assertions.Expect(Count(Provider)).ToHaveTextAsync("1–10 of 30");

        // The paid invoices are 4, 8, 12...
        await Box(Provider, 4).CheckAsync();
        await Provider.GetByRole(AriaRole.Button, new() { Name = "Select all 30 results" }).ClickAsync();
        await Assertions.Expect(Status(Provider)).ToHaveTextAsync("All 30 results selected");
        await Provider.GetByRole(AriaRole.Button, new() { Name = "Archive" }).ClickAsync();

        // The action got no rows: it got the request, with the filter, no search and no paging, and the number of matches.
        await Assertions.Expect(Page.GetByTestId("selp-message")).ToHaveTextAsync("Archive every result: 30 invoices, search none, 1 filters, no paging.");
        // Choosing a row again goes back to the ticked rows.
        await Box(Provider, 8).CheckAsync();
        await Assertions.Expect(Status(Provider)).ToHaveTextAsync("2 selected");
    }

    // The call that moves the focus is made right after the render that removes the bar, but a batch waits for the browser to acknowledge the one before it, so on a busy circuit
    // the call arrives first. The bar, and the Delete button the focus went back to when the dialog closed, are still on the page when it runs.
    [Fact]
    public async Task The_focus_goes_to_the_select_all_box_when_the_render_that_removes_the_bar_arrives_after_the_call()
    {
        await DelayRenderBatches(Page, 150);
        await OpenList("server");
        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 120", new() { Timeout = 15_000 });

        await Box(Memory, 1).CheckAsync();
        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("[data-slot=action-dialog]").GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 119", new() { Timeout = 15_000 });
        await Assertions.Expect(Bar(Memory)).ToHaveCountAsync(0);
        await Assertions.Expect(Header(Memory)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_confirmation_asks_before_deleting_and_the_selection_is_cleared_on_success(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 120");

        await Box(Memory, 1).CheckAsync();
        await Box(Memory, 2).CheckAsync();
        await Box(Memory, 3).CheckAsync();
        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        var dialog = Page.Locator("[data-slot=action-dialog]");
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog).ToContainTextAsync("3 invoices will be removed from the list.");
        // Cancel closes it and deletes nothing.
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 120");
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("3 selected");

        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("sel-message")).ToHaveTextAsync("Deleted 3 invoices.");
        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 117", new() { Timeout = 15_000 });
        await Assertions.Expect(Bar(Memory)).ToHaveCountAsync(0);
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("");
        // The Delete button left with the bar: the focus is on the select-all box, not lost to the page.
        await Assertions.Expect(Header(Memory)).ToBeFocusedAsync(new() { Timeout = 15_000 });
        // The toolbar menu offers to bring them back, because some are gone.
        await Memory.GetByRole(AriaRole.Button, new() { Name = "Actions" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Restore all invoices" }).ClickAsync();
        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 120", new() { Timeout = 15_000 });
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Every_result_of_a_list_in_memory_holds_every_matching_row_and_the_scope_follows_the_query(string mode)
    {
        await OpenList(mode, "?sel.f.status=Draft");
        await Assertions.Expect(Count(Memory)).ToHaveTextAsync("1–10 of 30");

        await Header(Memory).CheckAsync();
        await Memory.GetByRole(AriaRole.Button, new() { Name = "Select all 30 results" }).ClickAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("All 30 results selected");
        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Export" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("sel-message")).ToHaveTextAsync("Exported 30 invoices (All).");

        // A search changes the query: "every result" no longer describes it, and the ticked rows of the page that still match stay. The draft invoices are the ids 1, 5, 9...
        // and Acme has the ids 1, 13, 25, 37...: four of the ten rows ticked match.
        await Memory.GetByRole(AriaRole.Searchbox).FillAsync("Acme");
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("4 selected", new() { Timeout = 15_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Selection_works_after_an_enhanced_navigation(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-selection");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Assertions.Expect(Memory).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(Page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });

        if (mode == "ssr")
        {
            await Assertions.Expect(Memory.Locator("input[type=checkbox]")).ToHaveCountAsync(0);
            return;
        }
        await Assertions.Expect(Box(Memory, 3)).ToBeEnabledAsync(new() { Timeout = 30_000 });
        await Box(Memory, 3).CheckAsync();
        await Assertions.Expect(Status(Memory)).ToHaveTextAsync("1 selected");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_selection_bar_and_the_dialog_have_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode);
        await Header(Memory).CheckAsync();
        await Box(Provider, 1).CheckAsync();
        await Assertions.Expect(Bar(Memory)).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();

        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Assertions.Expect(Page.Locator("[data-slot=action-dialog]")).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Page.Locator("[data-slot=action-dialog]")).ToBeHiddenAsync();

        await DarkMode();
        await AssertNoAccessibilityViolations();
        await Bar(Memory).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Assertions.Expect(Page.Locator("[data-slot=action-dialog]")).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_toolbar_menu_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode);
        await Memory.GetByRole(AriaRole.Button, new() { Name = "Actions" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Menuitem, new() { Name = "Print the list" })).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");

        await DarkMode();
        await Memory.GetByRole(AriaRole.Button, new() { Name = "Actions" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Menuitem, new() { Name = "Print the list" })).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Texts_follow_the_language_of_the_user(string mode)
    {
        var page = await NewPage("fr", "fr");
        await page.GotoAsync($"/{mode}/components/data-list-selection");
        await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var list = page.GetByTestId("list-selection");
        await Assertions.Expect(list.Locator("input[data-select=row]").First).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Assertions.Expect(list.Locator("input[data-select=all]")).ToHaveAccessibleNameAsync("Sélectionner toutes les lignes de cette page");
        await list.Locator("input[data-select=row][data-key='3']").CheckAsync();
        await Assertions.Expect(list.Locator("[data-slot=selection-status]")).ToHaveTextAsync("1 sélectionné(s)");
        await Assertions.Expect(list.GetByRole(AriaRole.Button, new() { Name = "Sélectionner les 120 résultats" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_bar_fits_a_phone_screen(string mode)
    {
        await Page.SetViewportSizeAsync(375, 700);
        await OpenList(mode);

        await Header(Memory).CheckAsync();

        var box = await Bar(Memory).BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.X >= 0 && box.X + box.Width <= 375.5, $"the bar is {box.Width}px wide at x={box.X}");
        Assert.True(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth + 1"), "the page scrolls sideways");
    }
}
