using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The filter parts of the data list on its playground page, in the four render modes: the add filter menu, the editors of a text, an option and a boolean filter, the tags, the operator,
/// include empty, removing a filter and clearing them, the URL (a history entry per applied change), a typed address, the filtered empty state, accessibility with an editor open, a phone and
/// another language. The list of the examples is "list-filters" (URL prefix "flt", 120 invoices: 12 customers, 4 statuses of 30, 40 reviewed); statically rendered the editors are links and GET forms.
/// </summary>
public class DataListFilterTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-filters{query}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(Page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator List => Page.GetByTestId("list-filters");

    private ILocator Rows => List.Locator("tbody tr[data-slot=row]");

    private ILocator Count => List.Locator("[data-slot=result-count]");

    private ILocator Tag(string name) => List.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]", new() { HasTextString = name });

    private ILocator Tags => List.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]");

    private ILocator Editor(string name) => List.GetByRole(AriaRole.Dialog, new() { Name = name });

    private ILocator AddButton => List.GetByRole(AriaRole.Button, new() { Name = "Add filter" });

    private ILocator AddPanel => List.GetByRole(AriaRole.Dialog, new() { Name = "Add filter" });

    // A link statically rendered, a button once there is a .NET to run it.
    private static AriaRole Control(string mode) => mode == "ssr" ? AriaRole.Link : AriaRole.Button;

    private Task WaitForQuery(string expected) =>
        // What a user shares, decoded: the comma of a list is written %2C or not.
        Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query) == expected, new() { Timeout = 15_000 });

    // A popover of the top layer cannot be scrolled to: the trigger goes to the top of the screen first, so the whole panel fits below it.
    private static Task ToTop(ILocator trigger) => trigger.EvaluateAsync("e => e.scrollIntoView({ block: 'start' })");

    private async Task OpenTag(string name, string dialog)
    {
        await ToTop(Tag(name));
        await Tag(name).ClickAsync();
        await Assertions.Expect(Editor(dialog)).ToBeVisibleAsync();
    }

    // ---- add a filter

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_text_filter_is_added_from_the_menu_and_shows_as_a_tag(string mode)
    {
        await OpenList(mode);
        await Assertions.Expect(Tags).ToHaveCountAsync(0);

        await ToTop(AddButton);
        await AddButton.ClickAsync();
        await Assertions.Expect(AddPanel).ToBeVisibleAsync();
        if (mode == "ssr")
        {
            // No pick step without .NET: the menu lists an editor per filter, the text one is a GET form.
            var field = AddPanel.GetByLabel("Value for Customer");
            await field.FillAsync("globex");
            await AddPanel.Locator("[data-slot=filter-form]").GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        }
        else
        {
            await AddPanel.GetByRole(AriaRole.Button, new() { Name = "Customer", Exact = true }).ClickAsync();
            await AddPanel.GetByLabel("Value for Customer").PressSequentiallyAsync("globex", new() { Delay = 20 });
        }

        await WaitForQuery("?flt.f.customer=globex");
        await Assertions.Expect(Tag("Customer contains globex")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 10");
        await Assertions.Expect(Rows.First.Locator("td").Nth(1)).ToHaveTextAsync("Globex");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_add_menu_leaves_out_the_filters_that_are_already_on(string mode)
    {
        await OpenList(mode, "?flt.f.customer=acme");

        await ToTop(AddButton);
        await AddButton.ClickAsync();

        if (mode == "ssr")
        {
            await Assertions.Expect(AddPanel.GetByLabel("Value for Customer")).ToHaveCountAsync(0);
            await Assertions.Expect(AddPanel.Locator("[data-slot=filter-editor]")).ToHaveCountAsync(2);
        }
        else
        {
            await Assertions.Expect(AddPanel.GetByRole(AriaRole.Button, new() { Name = "Customer", Exact = true })).ToHaveCountAsync(0);
            await Assertions.Expect(AddPanel.GetByRole(AriaRole.Button, new() { Name = "Status", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(AddPanel.GetByRole(AriaRole.Button, new() { Name = "Reviewed", Exact = true })).ToBeVisibleAsync();
        }
    }

    // ---- the text editor

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Changing_the_operator_keeps_the_value_and_changes_the_rows(string mode)
    {
        await OpenList(mode, "?flt.f.customer=Globex");
        await OpenTag("Customer contains Globex", "Edit filter Customer");

        if (mode == "ssr")
        {
            await Editor("Edit filter Customer").GetByRole(AriaRole.Link, new() { Name = "is not" }).ClickAsync();
        }
        else
        {
            await Editor("Edit filter Customer").GetByLabel("Operator for Customer").SelectOptionAsync("isNot");
        }

        await WaitForQuery("?flt.f.customer.isNot=Globex");
        await Assertions.Expect(Tag("Customer is not Globex")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 110");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_in_the_editor_applies_after_a_pause_and_back_undoes_the_whole_typing_in_one_step(string mode)
    {
        await OpenList(mode, "?flt.f.customer=a");
        await OpenTag("Customer contains a", "Edit filter Customer");
        var field = Editor("Edit filter Customer").GetByLabel("Value for Customer");

        await field.FillAsync("acme");
        await WaitForQuery("?flt.f.customer=acme");
        await field.PressSequentiallyAsync(" c", new() { Delay = 20 });
        await WaitForQuery("?flt.f.customer=acme c");
        await Assertions.Expect(Tag("Customer contains acme c")).ToBeVisibleAsync();

        // The two pauses are one history entry: Back leaves the typing, to the address before it.
        await Page.GoBackAsync();

        await WaitForQuery("?flt.f.customer=a");
        await Assertions.Expect(Tag("Customer contains a")).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_rule_that_changes_while_text_waits_in_the_debounce_replaces_the_typed_text(string mode)
    {
        await OpenList(mode, "?flt.f.customer=a");
        await OpenTag("Customer contains a", "Edit filter Customer");
        var field = Editor("Edit filter Customer").GetByLabel("Value for Customer");

        // The typing is still inside its 300 ms pause when the address changes under it (what Back or a link does).
        await field.PressSequentiallyAsync("zz", new() { Delay = 10 });
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?flt.f.customer=globex')");

        await WaitForQuery("?flt.f.customer=globex");
        await Assertions.Expect(field).ToHaveValueAsync("globex");
        await Task.Delay(800, TestContext.Current.CancellationToken);
        await Assertions.Expect(field).ToHaveValueAsync("globex");
        Assert.Equal("?flt.f.customer=globex", Uri.UnescapeDataString(new Uri(Page.Url).Query));
    }

    // ---- the option editor

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task An_option_filter_lists_the_options_with_counts_and_ticking_one_adds_it_to_the_list(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid");
        await OpenTag("Status is Paid", "Edit filter Status");
        var panel = Editor("Edit filter Status");

        // 30 invoices per status, and the counts leave out the status filter itself.
        foreach (var option in new[] { "Paid", "Pending", "Overdue", "Draft" })
        {
            await Assertions.Expect(panel.Locator("[data-slot=filter-options] li", new() { HasTextString = option })).ToContainTextAsync("30");
        }
        await panel.GetByRole(mode == "ssr" ? AriaRole.Link : AriaRole.Checkbox, new() { Name = "Pending" }).ClickAsync();

        await WaitForQuery("?flt.f.status=Paid,Pending");
        await Assertions.Expect(Tag("Status is Paid, Pending")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 60");
        await Assertions.Expect(Page.Locator("[data-slot=result-count]").First).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_list_value_is_summarized_with_a_count_of_the_rest(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid,Pending,Overdue,Draft");

        await Assertions.Expect(Tag("Status is Paid, Pending +2")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Counts_follow_the_other_filters(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid&flt.f.reviewed=true");
        await OpenTag("Status is Paid", "Edit filter Status");

        // 40 reviewed invoices, ten per status.
        foreach (var option in new[] { "Paid", "Pending", "Overdue", "Draft" })
        {
            await Assertions.Expect(Editor("Edit filter Status").Locator("[data-slot=filter-options] li", new() { HasTextString = option })).ToContainTextAsync("10");
        }
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Include_empty_adds_the_tilde_token_and_says_so_in_the_tag(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid");
        await OpenTag("Status is Paid", "Edit filter Status");
        var panel = Editor("Edit filter Status");

        if (mode == "ssr") await panel.GetByRole(AriaRole.Link, new() { Name = "Include empty values" }).ClickAsync();
        else await panel.GetByRole(AriaRole.Switch, new() { Name = "Include empty values" }).CheckAsync();

        await WaitForQuery("?flt.f.status=Paid,~");
        await Assertions.Expect(Tag("Status is Paid or empty")).ToBeVisibleAsync();
        // No invoice lacks a status: the same 30.
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 30");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_is_not_operator_excludes_the_selected_options(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid");
        await OpenTag("Status is Paid", "Edit filter Status");

        if (mode == "ssr") await Editor("Edit filter Status").GetByRole(AriaRole.Link, new() { Name = "is not" }).ClickAsync();
        else await Editor("Edit filter Status").GetByLabel("Operator for Status").SelectOptionAsync("isNot");

        await WaitForQuery("?flt.f.status.isNot=Paid");
        await Assertions.Expect(Tag("Status is not Paid")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 90");
    }

    // ---- the boolean editor

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_yes_no_any_choice_applies_a_boolean_filter(string mode)
    {
        await OpenList(mode, "?flt.f.reviewed=true");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 40");
        await OpenTag("Reviewed is Yes", "Edit filter Reviewed");
        var panel = Editor("Edit filter Reviewed");

        await panel.GetByRole(Control(mode), new() { Name = "No", Exact = true }).ClickAsync();

        await WaitForQuery("?flt.f.reviewed=false");
        await Assertions.Expect(Tag("Reviewed is No")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 80");

        // The editor stays open while a change applies at once.
        await Assertions.Expect(Editor("Edit filter Reviewed")).ToBeVisibleAsync();
        await Editor("Edit filter Reviewed").GetByRole(Control(mode), new() { Name = "Any", Exact = true }).ClickAsync();

        await WaitForQuery("");
        await Assertions.Expect(Tags).ToHaveCountAsync(0);
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
    }

    // ---- remove and clear

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_x_of_a_tag_removes_that_filter_and_keeps_the_others(string mode)
    {
        await OpenList(mode, "?flt.f.customer=globex&flt.f.reviewed=true&flt.p=2");

        await List.GetByRole(Control(mode), new() { Name = "Remove filter Customer" }).ClickAsync();

        await WaitForQuery("?flt.f.reviewed=true");
        await Assertions.Expect(Tags).ToHaveCountAsync(1);
        await Assertions.Expect(Tag("Reviewed is Yes")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 40");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task After_removing_a_tag_the_focus_goes_to_the_list_of_tags(string mode)
    {
        await OpenList(mode, "?flt.f.customer=globex&flt.f.reviewed=true");

        await List.GetByRole(AriaRole.Button, new() { Name = "Remove filter Customer" }).ClickAsync();

        await WaitForQuery("?flt.f.reviewed=true");
        await Assertions.Expect(List.GetByRole(AriaRole.List, new() { Name = "Filters, 1 active" })).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Clear_filters_takes_them_all_off_and_Back_brings_them_back(string mode)
    {
        await OpenList(mode, "?flt.f.customer=globex&flt.f.reviewed=true&flt.s=-amount");

        await List.GetByRole(Control(mode), new() { Name = "Clear filters" }).ClickAsync();

        await WaitForQuery("?flt.s=-amount");
        await Assertions.Expect(Tags).ToHaveCountAsync(0);
        await Assertions.Expect(List.GetByRole(Control(mode), new() { Name = "Clear filters" })).ToHaveCountAsync(0);
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");

        await Page.GoBackAsync();

        await WaitForQuery("?flt.f.customer=globex&flt.f.reviewed=true&flt.s=-amount");
        await Assertions.Expect(Tags).ToHaveCountAsync(2);
    }

    // ---- the URL and the history

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Each_applied_change_is_a_history_entry_and_back_undoes_one(string mode)
    {
        await OpenList(mode, "?flt.f.reviewed=true");
        await OpenTag("Reviewed is Yes", "Edit filter Reviewed");
        await Editor("Edit filter Reviewed").GetByRole(Control(mode), new() { Name = "No", Exact = true }).ClickAsync();
        await WaitForQuery("?flt.f.reviewed=false");

        await Page.GoBackAsync();

        await WaitForQuery("?flt.f.reviewed=true");
        await Assertions.Expect(Tag("Reviewed is Yes")).ToBeVisibleAsync();
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 40");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_typed_address_loads_the_filters_and_drops_what_the_schema_does_not_allow(string mode)
    {
        await OpenList(mode, "?flt.f.customer=acme&flt.f.status=Paid,Draft&flt.f.nothing=1&flt.f.status.gt=3");

        await Assertions.Expect(Tags).ToHaveCountAsync(2);
        await Assertions.Expect(Tag("Customer contains acme")).ToBeVisibleAsync();
        await Assertions.Expect(Tag("Status is Paid, Draft")).ToBeVisibleAsync();
        // Every Acme invoice is a draft.
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 10");
        await Assertions.Expect(List.GetByRole(AriaRole.List, new() { Name = "Filters, 2 active" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Filters_that_match_nothing_show_their_own_empty_state_and_Reset_filters_restores_the_list(string mode)
    {
        // Acme invoices are 1, 13, 25...: none is a multiple of 3, so none is reviewed.
        await OpenList(mode, "?flt.f.customer=acme&flt.f.reviewed=true");
        var empty = List.Locator("[data-slot=empty-row]");

        await Assertions.Expect(empty.Locator("[data-slot=title]")).ToHaveTextAsync("No results match the filters");
        await Assertions.Expect(Rows).ToHaveCountAsync(0);
        await Assertions.Expect(Count).ToHaveTextAsync("No results");

        await empty.GetByRole(Control(mode), new() { Name = "Reset filters" }).ClickAsync();

        await WaitForQuery("");
        await Assertions.Expect(Rows).ToHaveCountAsync(10);
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
        await Assertions.Expect(Tags).ToHaveCountAsync(0);
    }

    // ---- the keyboard and the structure

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_tags_are_a_named_list_and_Escape_closes_the_editor_and_returns_to_the_tag(string mode)
    {
        await OpenList(mode, "?flt.f.customer=globex&flt.f.reviewed=true");
        var list = List.GetByRole(AriaRole.List, new() { Name = "Filters, 2 active" });
        await Assertions.Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Assertions.Expect(Tag("Customer contains globex")).ToHaveAttributeAsync("aria-haspopup", "dialog");

        await Tag("Customer contains globex").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Editor("Edit filter Customer")).ToBeVisibleAsync();
        await Assertions.Expect(Tag("Customer contains globex")).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(Editor("Edit filter Customer")).ToBeHiddenAsync();
        await Assertions.Expect(Tag("Customer contains globex")).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_result_count_is_the_live_region_that_announces_the_filtered_rows(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid");

        await Assertions.Expect(Count).ToHaveAttributeAsync("role", "status");
        await Assertions.Expect(Count).ToHaveAttributeAsync("aria-live", "polite");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 30");
    }

    // ---- the Submit mode

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_submit_mode_the_change_waits_for_Apply_and_Cancel_drops_it(string mode)
    {
        await OpenList(mode);
        var list = Page.GetByTestId("list-filters-submit");
        await ToTop(list.GetByRole(AriaRole.Button, new() { Name = "Add filter" }));
        await list.GetByRole(AriaRole.Button, new() { Name = "Add filter" }).ClickAsync();
        var panel = list.GetByRole(AriaRole.Dialog, new() { Name = "Add filter" });
        await panel.GetByRole(AriaRole.Button, new() { Name = "Status", Exact = true }).ClickAsync();

        await panel.GetByRole(AriaRole.Checkbox, new() { Name = "Overdue" }).CheckAsync();

        // Nothing is applied before Apply.
        await Task.Delay(300, TestContext.Current.CancellationToken);
        Assert.Equal("", Uri.UnescapeDataString(new Uri(Page.Url).Query));
        await panel.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
        Assert.Equal("", Uri.UnescapeDataString(new Uri(Page.Url).Query));

        await ToTop(list.GetByRole(AriaRole.Button, new() { Name = "Add filter" }));
        await list.GetByRole(AriaRole.Button, new() { Name = "Add filter" }).ClickAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Status", Exact = true }).ClickAsync();
        await Assertions.Expect(panel.GetByRole(AriaRole.Checkbox, new() { Name = "Overdue" })).Not.ToBeCheckedAsync();
        await panel.GetByRole(AriaRole.Checkbox, new() { Name = "Overdue" }).CheckAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        await WaitForQuery("?sub.f.status=Overdue");
        await Assertions.Expect(list.Locator("[data-slot=filter-tag-button]", new() { HasTextString = "Status is Overdue" })).ToBeVisibleAsync();
        await Assertions.Expect(list.Locator("[data-slot=result-count]")).ToHaveTextAsync("1–10 of 30");
    }

    // ---- accessibility and a narrow screen

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_filters_have_no_accessibility_violations_with_an_editor_open_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?flt.f.status=Paid&flt.f.customer=acme&flt.f.reviewed=true");
        await OpenTag("Status is Paid", "Edit filter Status");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor("Edit filter Status")).ToBeHiddenAsync();

        // A click on the theme button would close the popover (light dismiss): the theme changes first, then the editor opens again.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        await OpenTag("Status is Paid", "Edit filter Status");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_add_menu_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?flt.f.customer=acme");
        await ToTop(AddButton);
        await AddButton.ClickAsync();
        await Assertions.Expect(AddPanel).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(AddPanel).ToBeHiddenAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        await ToTop(AddButton);
        await AddButton.ClickAsync();
        await Assertions.Expect(AddPanel).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_tags_and_the_editor_fit_the_screen(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenList(mode, "?flt.f.status=Paid,Pending,Overdue&flt.f.customer=stark industries&flt.f.reviewed=true");

        var overflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(overflow, "the page scrolls horizontally at 375px with three filter tags");
        foreach (var tag in await Tags.AllAsync())
        {
            var box = await tag.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.X >= 0 && box.X + box.Width <= 375.5, $"a tag is {box.Width}px wide at x={box.X}");
            Assert.True(box.Height >= 24, $"a tag is only {box.Height}px high");
        }

        await OpenTag("Status is Paid, Pending +1", "Edit filter Status");
        var panel = await Editor("Edit filter Status").BoundingBoxAsync();
        Assert.NotNull(panel);
        Assert.True(panel!.X >= 0 && panel.X + panel.Width <= 375.5, $"the editor is {panel.Width}px wide at x={panel.X}");
    }

    // ---- language

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Texts_follow_the_language_of_the_user(string mode)
    {
        var page = await NewPage("fr", "fr");
        await page.GotoAsync($"/{mode}/components/data-list-filters?flt.f.reviewed=true&flt.f.status.isNot=Paid");
        await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var list = page.GetByTestId("list-filters");

        await Assertions.Expect(list.GetByRole(AriaRole.List, new() { Name = "Filtres, 2 actif(s)" })).ToBeVisibleAsync();
        await Assertions.Expect(list.Locator("[data-slot=filter-tag-button]", new() { HasTextString = "Reviewed est Oui" })).ToBeVisibleAsync();
        await Assertions.Expect(list.Locator("[data-slot=filter-tag-button]", new() { HasTextString = "Status n’est pas Paid" })).ToBeVisibleAsync();
        await Assertions.Expect(list.GetByRole(AriaRole.Button, new() { Name = "Retirer le filtre Status" })).ToBeVisibleAsync();
        await Assertions.Expect(list.GetByRole(AriaRole.Button, new() { Name = "Ajouter un filtre" })).ToBeVisibleAsync();
        await Assertions.Expect(list.GetByRole(AriaRole.Button, new() { Name = "Effacer les filtres" })).ToBeVisibleAsync();
    }
}
