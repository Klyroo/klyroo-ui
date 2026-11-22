using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The filter panel of the data list on its playground page, in the four render modes. The sidebar list is "list-panel" (URL prefix "pnl", Live apply mode, 120 invoices: 12 customers, 4 statuses of 30, 40 reviewed,
/// amounts and issue dates as in the number and date lists); the drawer list is "list-panel-slideover" (prefix "pns"), whose panel is a sidebar from 768px and, below it, the content of a slideover in the Submit
/// apply mode. Statically rendered the panel is one GET form: text, number and date fields are sent by its Apply button, operators, options and yes / no are links.
/// </summary>
public class DataListFilterPanelTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-filter-panel{query}");
        await WaitForList(Page, mode);
    }

    private static async Task WaitForList(IPage page, string mode)
    {
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover') && !!customElements.get('bx-collapsible')");
        if (mode != "ssr")
        {
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator List => Page.GetByTestId("list-panel");

    private ILocator Panel => List.Locator("[data-slot=filter-panel]");

    private ILocator Rows => List.Locator("tbody tr[data-slot=row]");

    private ILocator Count => List.Locator("[data-slot=result-count]");

    private ILocator Tags => List.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]");

    private ILocator Tag(string name) => List.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]", new() { HasTextString = name });

    private static ILocator Section(ILocator panel, string key) => panel.Locator($"bx-collapsible[data-filter={key}]");

    private static ILocator Trigger(ILocator panel, string key) => Section(panel, key).Locator("button[data-bx-collapsible-trigger]");

    private static AriaRole Control(string mode) => mode == "ssr" ? AriaRole.Link : AriaRole.Button;

    private Task WaitForQuery(string expected) =>
        Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query) == expected, new() { Timeout = 15_000 });

    // A closed section is opened with its trigger; the section is open when the trigger says so.
    private static async Task Expand(ILocator panel, string key)
    {
        var trigger = Trigger(panel, key);
        if (await trigger.GetAttributeAsync("aria-expanded") != "true") await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    // Interactive, Yes and No apply at once; statically rendered they are radios of the form, sent by Apply.
    private static async Task PickYesNo(ILocator section, string mode, string name)
    {
        if (mode == "ssr")
        {
            await section.GetByRole(AriaRole.Radio, new() { Name = name, Exact = true }).CheckAsync();
            await Apply(section.Locator("xpath=ancestor::*[@data-slot='filter-panel']"));
        }
        else
        {
            await section.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).ClickAsync();
        }
    }

    private static Task Apply(ILocator panel) => panel.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();

    // What a click on a link of the docs does: Blazor fetches the address and merges the response into the document (an enhanced navigation), or the interactive router loads the page.
    private async Task NavigateFromAnotherPage(string mode, string query)
    {
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-filter-panel{query}");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Assertions.Expect(List).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await WaitForList(Page, mode);
    }

    // ---- the sections

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_panel_lists_every_filter_as_a_closed_section_that_opens_and_closes_with_its_trigger(string mode)
    {
        await OpenList(mode);

        foreach (var name in new[] { "Customer", "Status", "Amount", "Issued", "Reviewed" })
        {
            await Assertions.Expect(Panel.GetByRole(AriaRole.Button, new() { Name = name, Exact = true })).ToHaveAttributeAsync("aria-expanded", "false");
        }
        await Assertions.Expect(Panel.GetByLabel("Value for Customer")).ToBeHiddenAsync();

        await Trigger(Panel, "customer").ClickAsync();
        await Assertions.Expect(Panel.GetByLabel("Value for Customer")).ToBeVisibleAsync();
        await Trigger(Panel, "customer").ClickAsync();
        await Assertions.Expect(Panel.GetByLabel("Value for Customer")).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_section_with_a_filter_on_starts_expanded_and_shows_how_many_values_are_on(string mode)
    {
        await OpenList(mode, "?pnl.f.status=Paid,Pending&pnl.f.customer=acme");

        await Assertions.Expect(Trigger(Panel, "status")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Trigger(Panel, "customer")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Trigger(Panel, "amount")).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(Section(Panel, "status").Locator("[data-slot=filter-panel-section-count]")).ToHaveTextAsync("2");
        await Assertions.Expect(Section(Panel, "customer").Locator("[data-slot=filter-panel-section-count]")).ToHaveTextAsync("1");
        await Assertions.Expect(Panel.GetByRole(AriaRole.Button, new() { Name = "Status 2 active" })).ToBeVisibleAsync();
        await Assertions.Expect(Panel.Locator("[data-slot=filter-panel-count]")).ToHaveTextAsync("2");
    }

    // ---- the editors of the kinds

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_text_filter_applies_from_its_section_and_the_section_stays_open(string mode)
    {
        await OpenList(mode);
        await Expand(Panel, "customer");
        var field = Panel.GetByLabel("Value for Customer");

        if (mode == "ssr")
        {
            await field.FillAsync("globex");
            await Apply(Panel);
        }
        else
        {
            await field.PressSequentiallyAsync("globex", new() { Delay = 20 });
        }

        await WaitForQuery("?pnl.f.customer=globex");
        await Assertions.Expect(Tag("Customer contains globex")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 10");
        await Assertions.Expect(Trigger(Panel, "customer")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Section(Panel, "customer").Locator("[data-slot=filter-panel-section-count]")).ToHaveTextAsync("1");
        await Assertions.Expect(Panel.GetByLabel("Value for Customer")).ToHaveValueAsync("globex");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task An_option_applies_at_once_and_the_counts_follow(string mode)
    {
        await OpenList(mode);
        await Expand(Panel, "status");

        await Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).CheckAsync();
        // Statically rendered the checkbox is a field of the form: nothing happens before Apply.
        if (mode == "ssr") await Apply(Panel);

        await WaitForQuery("?pnl.f.status=Paid");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 30");
        await Assertions.Expect(Tag("Status is Paid")).ToBeVisibleAsync();
        await Assertions.Expect(Panel.GetByRole(AriaRole.Button, new() { Name = "Status 1 active" })).ToBeVisibleAsync();
        await Assertions.Expect(Trigger(Panel, "status")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Yes_no_any_applies_a_boolean_filter(string mode)
    {
        await OpenList(mode);
        await Expand(Panel, "reviewed");

        await PickYesNo(Section(Panel, "reviewed"), mode, "Yes");

        await WaitForQuery("?pnl.f.reviewed=true");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 40");
        await PickYesNo(Section(Panel, "reviewed"), mode, "Any");
        await WaitForQuery("");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_range_of_numbers_is_two_fields_and_the_form_or_the_editor_applies_it(string mode)
    {
        await OpenList(mode, "?pnl.f.amount.between=100..200");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 21");
        var to = Section(Panel, "amount").GetByLabel("Amount, to");

        await to.FillAsync("150");
        if (mode == "ssr")
        {
            await Apply(Panel);
            // Two fields of a GET form: no script folds them into one key, and the page reads both.
            await WaitForQuery("?pnl.f.amount.from=100&pnl.f.amount.to=150");
        }
        else
        {
            await to.PressAsync("Enter");
            await WaitForQuery("?pnl.f.amount.between=100..150");
        }

        await Assertions.Expect(Count).ToHaveTextAsync("1–9 of 9");
        await Assertions.Expect(Tag("Amount is between 100.00 – 150.00")).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Changing_the_operator_of_a_number_keeps_the_value(string mode)
    {
        await OpenList(mode, "?pnl.f.amount=500");

        if (mode == "ssr") await Section(Panel, "amount").GetByRole(AriaRole.Link, new() { Name = "is at least" }).ClickAsync();
        else await Section(Panel, "amount").GetByLabel("Operator for Amount").SelectOptionAsync("gte");

        await WaitForQuery("?pnl.f.amount.gte=500");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 33");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_day_is_changed_in_its_section_and_a_whole_day_stays_a_whole_day(string mode)
    {
        await OpenList(mode, "?pnl.f.issued=2026-01-05");
        var field = Section(Panel, "issued").GetByLabel("Value for Issued");
        await Assertions.Expect(field).ToHaveValueAsync("2026-01-05");

        await field.FillAsync("2026-01-07");
        if (mode == "ssr") await Apply(Panel);

        await Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query).Contains("pnl.f.issued=2026-01-07"), new() { Timeout = 15_000 });
        await Assertions.Expect(Tag("Issued is 1/7/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–1 of 1");
    }

    // ---- clear all, history, the rest of the page

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Clear_filters_takes_every_filter_off_and_keeps_the_rest_of_the_address(string mode)
    {
        await OpenList(mode, "?pnl.f.customer=globex&pnl.f.reviewed=true&pnl.s=-id");
        await Assertions.Expect(Panel.Locator("[data-slot=filter-panel-count]")).ToHaveTextAsync("2");

        await Panel.GetByRole(Control(mode), new() { Name = "Clear filters" }).ClickAsync();

        await WaitForQuery("?pnl.s=-id");
        await Assertions.Expect(Tags).ToHaveCountAsync(0);
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
        await Assertions.Expect(Panel.Locator("[data-slot=filter-panel-count]")).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Each_applied_change_is_a_history_entry_and_back_undoes_one(string mode)
    {
        await OpenList(mode, "?pnl.f.status=Paid");
        await Expand(Panel, "reviewed");
        await PickYesNo(Section(Panel, "reviewed"), mode, "Yes");
        await WaitForQuery("?pnl.f.status=Paid&pnl.f.reviewed=true");

        await Page.GoBackAsync();

        await WaitForQuery("?pnl.f.status=Paid");
        await Assertions.Expect(Tag("Status is Paid")).ToBeVisibleAsync();
        await Assertions.Expect(Tag("Reviewed")).ToHaveCountAsync(0);
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 30");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_section_the_user_opened_stays_open_when_another_section_applies_a_filter(string mode)
    {
        await OpenList(mode);
        await Expand(Panel, "amount");
        await Expand(Panel, "status");

        await Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).ClickAsync();

        await WaitForQuery("?pnl.f.status=Paid");
        // Nothing was rendered for the amount section: the browser keeps what it opened, and a closed one stays closed.
        await Assertions.Expect(Trigger(Panel, "amount")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Trigger(Panel, "issued")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_filter_turned_on_by_a_typed_address_opens_its_section(string mode)
    {
        await OpenList(mode, "?pnl.f.status=Paid");
        await Assertions.Expect(Trigger(Panel, "customer")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-filter-panel?pnl.f.status=Paid&pnl.f.customer=acme");

        await WaitForQuery("?pnl.f.status=Paid&pnl.f.customer=acme");
        await Assertions.Expect(Tag("Customer contains acme")).ToBeVisibleAsync();
        await Assertions.Expect(Trigger(Panel, "customer")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task After_arriving_by_navigation_from_another_page_the_panel_opens_its_sections_and_applies(string mode)
    {
        await NavigateFromAnotherPage(mode, "?pnl.f.status=Paid");

        await Assertions.Expect(Trigger(Panel, "status")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expand(Panel, "customer");
        var field = Panel.GetByLabel("Value for Customer");
        if (mode == "ssr")
        {
            await field.FillAsync("umbrella");
            await Apply(Panel);
        }
        else
        {
            await field.PressSequentiallyAsync("umbrella", new() { Delay = 20 });
        }

        await Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query).Contains("pnl.f.customer=umbrella"), new() { Timeout = 15_000 });
        await Assertions.Expect(Tag("Customer contains umbrella")).ToBeVisibleAsync();
        await Assertions.Expect(Tag("Status is Paid")).ToBeVisibleAsync();
        // Umbrella invoices are all paid.
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 10");
    }


    // ---- the form: several options, empty rows kept, the panel result count

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Several_options_are_sent_together_and_read_as_one_list(string mode)
    {
        await OpenList(mode);
        await Expand(Panel, "status");

        await Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).CheckAsync();
        await Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Pending" }).CheckAsync();
        if (mode == "ssr") await Apply(Panel);

        // Statically rendered the address is what the form sends: one parameter per option, which the page reads as one list.
        await WaitForQuery(mode == "ssr" ? "?pnl.f.status=Paid&pnl.f.status=Pending" : "?pnl.f.status=Paid,Pending");
        await Assertions.Expect(Tag("Status is Paid, Pending")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 60");
        await Assertions.Expect(Panel.GetByRole(AriaRole.Button, new() { Name = "Status 2 active" })).ToBeVisibleAsync();
        await Assertions.Expect(Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Pending" })).ToBeCheckedAsync();
    }

    // Every message the circuit sends reaches the server late, in order: the way a slow connection makes a quick user outrun the address.
    private const string SlowCircuit = "(() => { const send = WebSocket.prototype.send; WebSocket.prototype.send = function (data) { setTimeout(() => send.call(this, data), 400); }; })();";

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Options_ticked_back_to_back_are_all_applied_in_one_coherent_address_even_on_a_slow_circuit(string mode)
    {
        await Page.AddInitScriptAsync(SlowCircuit);
        await OpenList(mode);
        await Expand(Panel, "status");
        var status = Section(Panel, "status");

        await status.GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).CheckAsync();
        await status.GetByRole(AriaRole.Checkbox, new() { Name = "Pending" }).CheckAsync();
        await status.GetByRole(AriaRole.Checkbox, new() { Name = "Draft" }).CheckAsync();

        await WaitForQuery("?pnl.f.status=Paid,Pending,Draft");
        await Assertions.Expect(Tag("Status is Paid, Pending +1")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 90");
        foreach (var name in new[] { "Paid", "Pending", "Draft" }) await Assertions.Expect(status.GetByRole(AriaRole.Checkbox, new() { Name = name })).ToBeCheckedAsync();
        await Assertions.Expect(status.GetByRole(AriaRole.Checkbox, new() { Name = "Overdue" })).Not.ToBeCheckedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Options_ticked_back_to_back_in_the_popover_of_a_tag_are_all_applied_even_on_a_slow_circuit(string mode)
    {
        await Page.AddInitScriptAsync(SlowCircuit);
        await OpenList(mode, "?pnl.f.status=Paid");
        await Tag("Status is Paid").ClickAsync();
        var editor = List.GetByRole(AriaRole.Dialog, new() { Name = "Edit filter Status" });
        await Assertions.Expect(editor).ToBeVisibleAsync();

        await editor.GetByRole(AriaRole.Checkbox, new() { Name = "Pending" }).CheckAsync();
        await editor.GetByRole(AriaRole.Checkbox, new() { Name = "Draft" }).CheckAsync();
        await editor.GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).UncheckAsync();

        await WaitForQuery("?pnl.f.status=Pending,Draft");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 60");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task An_option_ticked_and_unticked_back_to_back_ends_unticked_in_the_address_and_in_the_box_even_on_a_slow_circuit(string mode)
    {
        await Page.AddInitScriptAsync(SlowCircuit);
        await OpenList(mode);
        await Expand(Panel, "status");
        var paid = Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" });

        await paid.CheckAsync();
        await paid.UncheckAsync();
        await Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Draft" }).CheckAsync();

        await WaitForQuery("?pnl.f.status=Draft");
        await Assertions.Expect(paid).Not.ToBeCheckedAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 30");
    }

    [Fact]
    public async Task Statically_the_any_radio_of_a_single_choice_option_takes_the_filter_off()
    {
        await OpenList("ssr", "?pnl.f.status.is=Paid");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 30");

        await Section(Panel, "status").GetByRole(AriaRole.Radio, new() { Name = "Any" }).CheckAsync();
        await Apply(Panel);

        await WaitForQuery("");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
    }

    [Fact]
    public async Task Statically_unchecking_every_option_and_applying_takes_the_filter_off()
    {
        await OpenList("ssr", "?pnl.f.status=Paid");

        await Section(Panel, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).UncheckAsync();
        await Apply(Panel);

        await WaitForQuery("");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
    }

    [Fact]
    public async Task Statically_applying_the_form_keeps_the_empty_rows_of_filters_that_were_not_touched()
    {
        await OpenList("ssr", "?pnl.f.status=Paid,~&pnl.f.customer=umbrella,~");
        await Assertions.Expect(Tag("Status is Paid or empty")).ToBeVisibleAsync();
        await Assertions.Expect(Tag("Customer contains umbrella or empty")).ToBeVisibleAsync();

        await Section(Panel, "amount").Locator("xpath=ancestor::*[@data-slot='filter-panel']").GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true }).ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).Query.Contains("pnl.f.status=~"), new() { Timeout = 15_000 });
        await Assertions.Expect(Tag("Status is Paid or empty")).ToBeVisibleAsync();
        await Assertions.Expect(Tag("Customer contains umbrella or empty")).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_panel_counts_the_results_in_a_live_region_that_follows_the_filters(string mode)
    {
        await OpenList(mode);
        var results = Panel.Locator("[data-slot=filter-panel-results]");
        await Assertions.Expect(results).ToHaveTextAsync("Results: 120");
        await Assertions.Expect(results).ToHaveAttributeAsync("role", "status");
        await Assertions.Expect(results).ToHaveAttributeAsync("aria-live", "polite");

        await Page.GotoAsync($"/{mode}/components/data-list-filter-panel?pnl.f.status=Paid");
        await WaitForList(Page, mode);

        await Assertions.Expect(results).ToHaveTextAsync("Results: 30");
    }

    // ---- the Submit mode on a desktop: the sidebar of the drawer list

    private ILocator Sidebar => DrawerList.GetByTestId("panel-sidebar");

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_submit_mode_Clear_filters_stages_the_removal_until_Apply_and_Reset_restores_it(string mode)
    {
        await OpenList(mode, "?pns.f.status=Paid&pns.f.reviewed=true");
        var apply = Sidebar.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true });
        var paid = Section(Sidebar, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" });
        await Assertions.Expect(paid).ToBeCheckedAsync();

        await Sidebar.GetByRole(AriaRole.Button, new() { Name = "Clear filters" }).ClickAsync();

        await Assertions.Expect(paid).Not.ToBeCheckedAsync();
        await Assertions.Expect(apply).Not.ToHaveAttributeAsync("aria-disabled", "true");
        Assert.Equal("?pns.f.status=Paid&pns.f.reviewed=true", Uri.UnescapeDataString(new Uri(Page.Url).Query));
        await Sidebar.GetByRole(AriaRole.Button, new() { Name = "Reset", Exact = true }).ClickAsync();
        await Assertions.Expect(paid).ToBeCheckedAsync();

        await Sidebar.GetByRole(AriaRole.Button, new() { Name = "Clear filters" }).ClickAsync();
        await apply.ClickAsync();

        await WaitForQuery("");
        await Assertions.Expect(DrawerList.Locator("[data-slot=result-count]")).ToHaveTextAsync("1–10 of 120");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_submit_mode_Apply_Reset_and_Clear_keep_the_keyboard_focus_when_they_are_used(string mode)
    {
        await OpenList(mode);
        var apply = Sidebar.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true });
        var reset = Sidebar.GetByRole(AriaRole.Button, new() { Name = "Reset", Exact = true });
        var clear = Sidebar.GetByRole(AriaRole.Button, new() { Name = "Clear filters" });
        await Expand(Sidebar, "status");

        // Idle, they can still be reached and pressed: nothing happens and the focus stays.
        foreach (var button in new[] { apply, reset, clear })
        {
            await button.FocusAsync();
            await Page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(button).ToBeFocusedAsync();
        }

        await Section(Sidebar, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).CheckAsync();
        await reset.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(reset).ToBeFocusedAsync();
        await Assertions.Expect(Section(Sidebar, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" })).Not.ToBeCheckedAsync();

        await Section(Sidebar, "status").GetByRole(AriaRole.Checkbox, new() { Name = "Paid" }).CheckAsync();
        await apply.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForQuery("?pns.f.status=Paid");
        await Assertions.Expect(apply).ToBeFocusedAsync();

        await clear.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(clear).ToBeFocusedAsync();
        await Assertions.Expect(apply).Not.ToHaveAttributeAsync("aria-disabled", "true");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_live_mode_Clear_filters_keeps_the_focus(string mode)
    {
        await OpenList(mode, "?pnl.f.customer=globex");
        var clear = Panel.GetByRole(AriaRole.Button, new() { Name = "Clear filters" });

        await clear.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await WaitForQuery("");
        await Assertions.Expect(clear).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_and_pressing_Apply_at_once_includes_the_text_by_mouse_and_by_keyboard(string mode)
    {
        await OpenList(mode);
        var apply = Sidebar.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true });
        await Expand(Sidebar, "customer");
        var field = Sidebar.GetByLabel("Value for Customer");

        // No pause: the click lands before the debounce of the field ends, and Apply was idle when the typing began.
        await field.PressSequentiallyAsync("stark");
        await apply.ClickAsync();

        await WaitForQuery("?pns.f.customer=stark");
        await Assertions.Expect(DrawerList.Locator("[data-slot=result-count]")).ToHaveTextAsync("1–10 of 10");

        await field.FillAsync("");
        await field.PressSequentiallyAsync("umbrella");
        // Tab leaves the field (that reports the text), then the keyboard reaches Apply, which was idle when the typing began.
        await Page.Keyboard.PressAsync("Tab");
        await apply.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await WaitForQuery("?pns.f.customer=umbrella");
    }

    // ---- sections the user opened by hand

    [Fact]
    public async Task Statically_a_section_opened_by_hand_stays_open_after_the_form_is_applied()
    {
        await OpenList("ssr");
        await Expand(Panel, "amount");
        await Expand(Panel, "customer");
        await Panel.GetByLabel("Value for Customer").FillAsync("globex");

        await Apply(Panel);

        await WaitForQuery("?pnl.f.customer=globex");
        await Assertions.Expect(Trigger(Panel, "customer")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Trigger(Panel, "amount")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Trigger(Panel, "issued")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task Statically_a_section_opened_by_hand_stays_open_after_an_operator_link()
    {
        await OpenList("ssr", "?pnl.f.amount=500");
        await Expand(Panel, "issued");

        await Section(Panel, "amount").GetByRole(AriaRole.Link, new() { Name = "is at least" }).ClickAsync();

        await WaitForQuery("?pnl.f.amount.gte=500");
        await Assertions.Expect(Trigger(Panel, "issued")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    // ---- accessibility and a narrow screen

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_panel_has_no_accessibility_violations_with_sections_open_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?pnl.f.status=Paid&pnl.f.customer=acme&pnl.f.amount.between=100..200&pnl.f.issued=2026-01-05&pnl.f.reviewed=true");
        await Expand(Panel, "status");
        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_sidebar_panel_does_not_scroll_the_page_sideways(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenList(mode, "?pnl.f.status=Paid,Pending&pnl.f.customer=stark industries&pnl.f.amount.between=100..200&pnl.f.reviewed=true");

        var overflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(overflow, "the page scrolls horizontally at 375px with the filter panel");
        var box = await Panel.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.X >= 0 && box.X + box.Width <= 375.5, $"the panel is {box.Width}px wide at x={box.X}");
    }

    // ---- the drawer on a phone

    private ILocator DrawerList => Page.GetByTestId("list-panel-slideover");

    private ILocator Drawer => Page.GetByRole(AriaRole.Dialog, new() { Name = "Filters" });

    private async Task OpenDrawer(string mode, string query = "")
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenList(mode, query);
        await DrawerList.GetByTestId("open-filters").ScrollIntoViewIfNeededAsync();
        await DrawerList.GetByTestId("open-filters").ClickAsync();
        await Assertions.Expect(Drawer).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_panel_is_in_a_slideover_whose_apply_applies_every_section_and_closes_it(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenList(mode);
        await Assertions.Expect(DrawerList.GetByTestId("panel-sidebar")).ToBeHiddenAsync();
        await Assertions.Expect(DrawerList.GetByTestId("open-filters")).ToBeVisibleAsync();
        await DrawerList.GetByTestId("open-filters").ClickAsync();
        await Assertions.Expect(Drawer).ToBeVisibleAsync();

        await Expand(Drawer, "customer");
        if (mode == "ssr")
        {
            await Drawer.GetByLabel("Value for Customer").FillAsync("stark");
            await Apply(Drawer);
        }
        else
        {
            await Expand(Drawer, "reviewed");
            await Drawer.GetByLabel("Value for Customer").PressSequentiallyAsync("stark", new() { Delay = 20 });
            await Drawer.GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true }).ClickAsync();
            // Nothing is applied before Apply.
            await Task.Delay(500, TestContext.Current.CancellationToken);
            Assert.Equal("", Uri.UnescapeDataString(new Uri(Page.Url).Query));
            await Apply(Drawer);
        }

        await WaitForQuery(mode == "ssr" ? "?pns.f.customer=stark" : "?pns.f.customer=stark&pns.f.reviewed=true");
        await Assertions.Expect(Drawer).ToBeHiddenAsync();
        // Every Stark Industries invoice is a multiple of 6, so every one is reviewed.
        await Assertions.Expect(DrawerList.Locator("[data-slot=result-count]")).ToHaveTextAsync("1–10 of 10");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_the_drawer_Reset_drops_what_was_not_applied(string mode)
    {
        await OpenDrawer(mode);
        await Expand(Drawer, "reviewed");
        var apply = Drawer.GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true });
        await Assertions.Expect(apply).ToBeDisabledAsync();

        await Drawer.GetByRole(AriaRole.Button, new() { Name = "Yes", Exact = true }).ClickAsync();
        await Assertions.Expect(apply).ToBeEnabledAsync();
        await Drawer.GetByRole(AriaRole.Button, new() { Name = "Reset", Exact = true }).ClickAsync();

        await Assertions.Expect(apply).ToBeDisabledAsync();
        Assert.Equal("", Uri.UnescapeDataString(new Uri(Page.Url).Query));
        await Assertions.Expect(Drawer.GetByRole(AriaRole.Button, new() { Name = "Any", Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_drawer_has_no_accessibility_violations_and_fits_the_phone_in_light_and_dark(string mode)
    {
        await OpenDrawer(mode, "?pns.f.status=Paid&pns.f.customer=acme&pns.f.amount.between=100..200&pns.f.issued=2026-01-05");

        // The slideover slides in: measured once it stopped.
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        var box = await Drawer.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.X >= 0 && box.X + box.Width <= 375.5, $"the drawer is {box.Width}px wide at x={box.X}");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Drawer).ToBeHiddenAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        await DrawerList.GetByTestId("open-filters").ClickAsync();
        await Assertions.Expect(Drawer).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_desktop_the_same_list_shows_the_panel_as_a_sidebar_and_no_filters_button(string mode)
    {
        await OpenList(mode, "?pns.f.status=Paid");

        await Assertions.Expect(DrawerList.GetByTestId("open-filters")).ToBeHiddenAsync();
        var sidebar = DrawerList.GetByTestId("panel-sidebar");
        await Assertions.Expect(sidebar).ToBeVisibleAsync();
        await Assertions.Expect(Trigger(sidebar, "status")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    // ---- language

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Texts_follow_the_language_of_the_user(string mode)
    {
        var page = await NewPage("fr", "fr");
        await page.GotoAsync($"/{mode}/components/data-list-filter-panel?pnl.f.reviewed=true&pnl.f.status=Paid,Pending");
        await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var panel = page.GetByTestId("list-panel").Locator("[data-slot=filter-panel]");

        await Assertions.Expect(panel.Locator("[data-slot=filter-panel-title]")).ToContainTextAsync("Filtres");
        await Assertions.Expect(panel.GetByRole(AriaRole.Button, new() { Name = "Status 2 actif(s)" })).ToBeVisibleAsync();
        await Assertions.Expect(panel.GetByRole(AriaRole.Button, new() { Name = "Effacer les filtres" })).ToBeVisibleAsync();
    }
}
