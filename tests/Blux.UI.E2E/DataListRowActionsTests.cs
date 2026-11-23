using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// Row actions of the data list on its playground page (data-list-selection, the list "list-rowactions": 120 invoices, two buttons, a menu with groups and a submenu, a sheet below md), in the four render modes.
/// Invoice 1 is a Draft ("Send to" is disabled, so are its entries; Archive is hidden), 2 is Overdue (everything shown), 4 is Paid (Mark as paid is disabled).
/// Interactive only, except the link: statically rendered the row holds the link and nothing else.
/// </summary>
public class DataListRowActionsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/data-list-selection");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(List.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator List => Page.GetByTestId("list-rowactions");

    private ILocator Rows => List.Locator("tbody tr[data-slot=row]");

    private ILocator Trigger(int id) => List.GetByRole(AriaRole.Button, new() { Name = $"Actions for Invoice {id}", Exact = true });

    private ILocator Menu(string name) => Page.GetByRole(AriaRole.Menu, new() { Name = name, Exact = true });

    private ILocator Item(string menu, string name) => Menu(menu).GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true });

    private ILocator Message => Page.GetByTestId("ra-message");

    private ILocator RemoveList => Page.GetByTestId("list-rowactions-remove");

    private ILocator RemoveTrigger(int id) => RemoveList.GetByRole(AriaRole.Button, new() { Name = $"Actions for Invoice {id}", Exact = true });

    private async Task DarkMode()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    [Fact]
    public async Task Statically_rendered_the_row_holds_the_link_and_nothing_else()
    {
        await OpenList("ssr");

        await Assertions.Expect(Rows).ToHaveCountAsync(10);
        var link = Rows.First.Locator("a[data-action=open]");
        await Assertions.Expect(link).ToHaveAttributeAsync("href", "#invoice-1");
        await Assertions.Expect(Rows.First.Locator("button")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("[data-slot=row-actions-sheet]")).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_first_actions_are_buttons_and_the_rest_are_behind_a_named_menu_button(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        var row = Rows.Nth(1);
        await Assertions.Expect(row.GetByRole(AriaRole.Link, new() { Name = "Open" })).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Copy number" }).ClickAsync();
        await Assertions.Expect(Message).ToHaveTextAsync("Copied the number of invoice 2.");
        // On a wide screen the phone button is not there for assistive technology: one "Actions for ..." button per row.
        await Assertions.Expect(row.GetByRole(AriaRole.Button, new() { Name = "Actions for Invoice 2" })).ToHaveCountAsync(1);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_menu_has_groups_and_a_submenu_that_the_keyboard_opens_and_closes(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(Menu("Actions for Invoice 2")).ToBeVisibleAsync();
        await Assertions.Expect(Item("Actions for Invoice 2", "Send to")).ToBeFocusedAsync();
        // Groups: Share, Manage and Danger are separated by dividers.
        await Assertions.Expect(Menu("Actions for Invoice 2").GetByRole(AriaRole.Separator)).ToHaveCountAsync(2);

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Menu("Send to")).ToBeVisibleAsync();
        await Assertions.Expect(Item("Send to", "Email")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(Menu("Send to")).ToBeHiddenAsync();
        await Assertions.Expect(Item("Actions for Invoice 2", "Send to")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Message).ToHaveTextAsync("Invoice 2 sent as a message.");
        await Assertions.Expect(Menu("Actions for Invoice 2")).ToBeHiddenAsync();
        await Assertions.Expect(Trigger(2)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task An_action_runs_for_its_row_and_a_disabled_or_hidden_one_does_not(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).ClickAsync();
        await Item("Actions for Invoice 2", "Mark as paid").ClickAsync();
        await Assertions.Expect(Message).ToHaveTextAsync("Invoice 2 is paid.");
        await Assertions.Expect(Rows.Nth(1)).ToContainTextAsync("Paid");

        await Trigger(2).ClickAsync();
        await Assertions.Expect(Item("Actions for Invoice 2", "Mark as paid")).ToHaveAttributeAsync("aria-disabled", "true");
        await Page.Keyboard.PressAsync("Escape");

        // A draft cannot be archived: the action is not offered for it, and it cannot be sent.
        await Trigger(1).ClickAsync();
        await Assertions.Expect(Menu("Actions for Invoice 1").GetByRole(AriaRole.Menuitem, new() { Name = "Archive" })).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_failure_leaves_the_row_and_is_reported_without_throwing(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).ClickAsync();
        await Item("Actions for Invoice 2", "Archive").ClickAsync();

        await Assertions.Expect(Message).ToHaveTextAsync("Archive failed for invoice 2: the server refused");
        await Assertions.Expect(Rows).ToHaveCountAsync(10);
        await Assertions.Expect(Trigger(2)).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_confirmed_delete_removes_the_row_and_the_focus_moves_to_the_actions_of_the_next_row(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).ClickAsync();
        await Item("Actions for Invoice 2", "Delete").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete invoice" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog).ToContainTextAsync("Invoice 2 will be removed from the list.");
        await Assertions.Expect(Rows).ToHaveCountAsync(10);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Message).ToHaveTextAsync("Deleted invoice 2.");
        await Assertions.Expect(Trigger(2)).ToHaveCountAsync(0);
        await Assertions.Expect(Trigger(3)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Cancelling_the_confirmation_keeps_the_row_and_gives_the_focus_back(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).ClickAsync();
        await Item("Actions for Invoice 2", "Delete").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete invoice" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(Rows).ToHaveCountAsync(10);
        await Assertions.Expect(Message).ToHaveTextAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Below_md_one_button_opens_a_sheet_with_every_action_and_the_reasons(string mode)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await OpenList(mode);
        await Assertions.Expect(Trigger(4)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        // Only the phone button is shown for the row: no inline buttons, no menu button.
        await Assertions.Expect(Rows.Nth(3).GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await Trigger(4).ClickAsync();
        var sheet = Page.GetByRole(AriaRole.Dialog, new() { Name = "Actions for Invoice 4" });
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        await Assertions.Expect(sheet.GetByRole(AriaRole.Link, new() { Name = "Open" })).ToBeVisibleAsync();
        var paid = sheet.GetByRole(AriaRole.Button, new() { Name = "Mark as paid" });
        await Assertions.Expect(paid).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(paid).ToHaveAccessibleDescriptionAsync("This invoice is already paid.");
        await Assertions.Expect(sheet.GetByText("This invoice is already paid.")).ToBeVisibleAsync();
        await Assertions.Expect(sheet.GetByText("Send to")).ToBeVisibleAsync();

        await sheet.GetByRole(AriaRole.Button, new() { Name = "Copy number" }).ClickAsync();

        await Assertions.Expect(sheet).ToBeHiddenAsync();
        await Assertions.Expect(Message).ToHaveTextAsync("Copied the number of invoice 4.");
        await Assertions.Expect(Trigger(4)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task On_a_phone_a_confirmed_delete_in_the_sheet_moves_the_focus_to_the_next_row(string mode)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await OpenList(mode);
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Actions for Invoice 2" }).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete invoice" }).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(Message).ToHaveTextAsync("Deleted invoice 2.");
        await Assertions.Expect(Trigger(3)).ToBeFocusedAsync();
    }

    // Both actions of the list below remove their row before they return: one leaves it to RefreshOnSuccess, the other refreshes the list itself after a pause.
    [Theory]
    [InlineData("server", "Remove")]
    [InlineData("server", "Remove after a pause")]
    [InlineData("wasm", "Remove")]
    [InlineData("wasm", "Remove after a pause")]
    [InlineData("auto", "Remove")]
    [InlineData("auto", "Remove after a pause")]
    public async Task A_row_removed_by_a_refresh_gives_the_focus_to_the_actions_of_the_next_row(string mode, string action)
    {
        await OpenList(mode);
        await Assertions.Expect(RemoveList.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(RemoveTrigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await RemoveTrigger(2).ClickAsync();
        await Menu("Actions for Invoice 2").GetByRole(AriaRole.Menuitem, new() { Name = action, Exact = true }).ClickAsync();

        await Assertions.Expect(RemoveTrigger(2)).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(RemoveTrigger(3)).ToBeFocusedAsync();
        await Assertions.Expect(RemoveList.Locator("tbody tr[data-slot=row]")).ToHaveCountAsync(10);
    }

    // The render batch that removes the row reaches the browser late (a loaded server): what the browser was told before the action ran is what finds the row.
    [Theory]
    [InlineData("Remove")]
    [InlineData("Remove after a pause")]
    public async Task A_row_removed_by_a_refresh_still_gives_the_focus_to_the_next_row_when_the_render_arrives_late(string action)
    {
        await DelayRenderBatches(Page, 200);
        await Open("server", "data-list-selection");
        await Assertions.Expect(RemoveList.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 30_000 });
        await Assertions.Expect(RemoveTrigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await RemoveTrigger(2).ClickAsync();
        await Menu("Actions for Invoice 2").GetByRole(AriaRole.Menuitem, new() { Name = action, Exact = true }).ClickAsync();

        await Assertions.Expect(RemoveTrigger(2)).ToHaveCountAsync(0, new() { Timeout = 30_000 });
        await Assertions.Expect(RemoveTrigger(3)).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task A_row_removed_when_it_is_the_last_one_gives_the_focus_to_the_table(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(RemoveList.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(RemoveTrigger(10)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        // The last page holds invoices 21 to 30: each removal gives the focus to the next row, and the last one, which has none, to the table.
        await RemoveList.GetByLabel("Page 3", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(RemoveTrigger(21)).ToBeEnabledAsync(new() { Timeout = 15_000 });
        for (var id = 21; id <= 30; id++)
        {
            await RemoveTrigger(id).ClickAsync();
            await Menu($"Actions for Invoice {id}").GetByRole(AriaRole.Menuitem, new() { Name = "Remove", Exact = true }).ClickAsync();
            await Assertions.Expect(RemoveTrigger(id)).ToHaveCountAsync(0, new() { Timeout = 15_000 });
            if (id < 30) await Assertions.Expect(RemoveTrigger(id + 1)).ToBeFocusedAsync();
        }
        // The last one had no next row, and the list went back to the page before it.
        await Assertions.Expect(RemoveList.Locator("[data-slot=table-root]")).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_disabled_action_has_its_reason_as_title_and_a_disabled_parent_opens_nothing(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Trigger(4)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(4).ClickAsync();
        await Assertions.Expect(Item("Actions for Invoice 4", "Mark as paid")).ToHaveAttributeAsync("title", "This invoice is already paid.");
        await Assertions.Expect(Item("Actions for Invoice 4", "Mark as paid")).ToHaveAccessibleDescriptionAsync("This invoice is already paid.");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");

        // A draft cannot be sent: the entry is disabled with its reason and the submenu stays shut.
        await Trigger(1).ClickAsync();
        var send = Item("Actions for Invoice 1", "Send to");
        await Assertions.Expect(send).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(send).ToHaveAttributeAsync("title", "A draft cannot be sent.");
        // Playwright does not click what is aria-disabled: force it, as a user with a pointer would.
        await send.ClickAsync(new() { Force = true });
        await Assertions.Expect(Menu("Send to")).ToBeHiddenAsync();
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Message).ToHaveTextAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task On_a_phone_the_children_of_a_disabled_action_do_not_run_and_the_sheet_says_why(string mode)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await OpenList(mode);
        await Assertions.Expect(Trigger(1)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(1).ClickAsync();
        var sheet = Page.GetByRole(AriaRole.Dialog, new() { Name = "Actions for Invoice 1" });
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        var email = sheet.GetByRole(AriaRole.Button, new() { Name = "Email", Exact = true });
        var message = sheet.GetByRole(AriaRole.Button, new() { Name = "Message", Exact = true });
        await Assertions.Expect(email).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(message).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(email).ToHaveAccessibleDescriptionAsync("A draft cannot be sent.");
        // The reason is said once, under the heading of the group.
        await Assertions.Expect(sheet.GetByText("A draft cannot be sent.")).ToHaveCountAsync(1);
        await AssertNoAccessibilityViolations();

        await email.ClickAsync(new() { Force = true });
        await message.ClickAsync(new() { Force = true });

        // Nothing ran and the sheet is still open.
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        await Assertions.Expect(Message).ToHaveTextAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_closed_row_holds_one_copy_of_its_actions_and_the_sheet_holds_them_only_while_it_is_open(string mode)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await OpenList(mode);
        await Assertions.Expect(Trigger(4)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Assertions.Expect(Rows.Nth(3).Locator("[data-action=copy]")).ToHaveCountAsync(1);
        await Assertions.Expect(List.Locator("[data-slot=row-actions-sheet-list]")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("[data-action=copy]")).ToHaveCountAsync(10);

        await Trigger(4).ClickAsync();
        var sheet = Page.GetByRole(AriaRole.Dialog, new() { Name = "Actions for Invoice 4" });
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        await Assertions.Expect(sheet.Locator("[data-action=copy]")).ToHaveCountAsync(1);
        await Assertions.Expect(List.Locator("[data-slot=row-actions-sheet-list]")).ToHaveCountAsync(1);
        // Ids stay unique with the sheet of a row open.
        var duplicates = await Page.EvaluateAsync<int>("() => { const ids = [...document.querySelectorAll('[id]')].map(e => e.id); return ids.length - new Set(ids).size; }");
        Assert.Equal(0, duplicates);

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(sheet).ToBeHiddenAsync();
        await Assertions.Expect(List.Locator("[data-slot=row-actions-sheet-list]")).ToHaveCountAsync(0);
        await Assertions.Expect(Trigger(4)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Row_actions_work_after_an_enhanced_navigation(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-selection");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Assertions.Expect(List).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(List.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });

        await Trigger(2).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(Item("Actions for Invoice 2", "Send to")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Item("Send to", "Email")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Message).ToHaveTextAsync("Invoice 2 sent by email.");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task An_open_menu_with_its_submenu_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode);
        if (mode != "ssr") await Assertions.Expect(Trigger(2)).ToBeEnabledAsync(new() { Timeout = 30_000 });
        foreach (var dark in new[] { false, true })
        {
            if (dark) await DarkMode();
            if (mode != "ssr")
            {
                await Trigger(2).ClickAsync();
                await Item("Actions for Invoice 2", "Send to").ClickAsync();
                await Assertions.Expect(Menu("Send to")).ToBeVisibleAsync();
            }

            await AssertNoAccessibilityViolations();

            if (mode != "ssr")
            {
                await Page.Keyboard.PressAsync("Escape");
                await Page.Keyboard.PressAsync("Escape");
                await Assertions.Expect(Menu("Actions for Invoice 2")).ToBeHiddenAsync();
            }
        }
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_phone_sheet_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await Page.SetViewportSizeAsync(390, 844);
        await OpenList(mode);
        await Assertions.Expect(Trigger(4)).ToBeEnabledAsync(new() { Timeout = 30_000 });
        foreach (var dark in new[] { false, true })
        {
            if (dark) await DarkMode();
            await Trigger(4).ClickAsync();
            await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Actions for Invoice 4" })).ToBeVisibleAsync();

            await AssertNoAccessibilityViolations();

            await Page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Actions for Invoice 4" })).ToBeHiddenAsync();
        }
    }
}
