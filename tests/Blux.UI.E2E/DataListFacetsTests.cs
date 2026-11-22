using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The counts next to the options of the data list filters, and the options that come from a remote source, on the playground page, in the four render modes. "list-facets" (URL prefix "fct", 120 invoices:
/// 12 customers with 10 invoices each, one status per customer, 4 statuses of 30, 40 reviewed) counts customers (by count, five before Show more), statuses (declared, by count) and Reviewed. "list-remote"
/// (prefix "rem") loads the customer options from a directory that answers after 400 ms, five per page. Statically rendered the editors are links; a remote editor lists the selected and the counted options.
/// </summary>
public class DataListFacetsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list{query}");
        await WaitForPage(mode);
    }

    private async Task WaitForPage(string mode)
    {
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(Page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator Facets => Page.GetByTestId("list-facets");

    private ILocator Remote => Page.GetByTestId("list-remote");

    private static ILocator Tag(ILocator list, string name) => list.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]", new() { HasTextString = name });

    private static ILocator Editor(ILocator list, string name) => list.GetByRole(AriaRole.Dialog, new() { Name = name });

    private static AriaRole Control(string mode) => mode == "ssr" ? AriaRole.Link : AriaRole.Button;

    // The options of an editor, statically (a link per option) and interactively (a checkbox row): both carry data-value.
    private static ILocator Options(ILocator editor) => editor.Locator("[data-slot=filter-options] [data-value]");

    // A popover of the top layer cannot be scrolled to: the trigger goes to the top of the screen first, so the whole panel fits below it.
    private static Task ToTop(ILocator trigger) => trigger.EvaluateAsync("e => e.scrollIntoView({ block: 'start' })");

    private static async Task OpenTag(ILocator list, string name, string dialog)
    {
        await ToTop(Tag(list, name));
        await Tag(list, name).ClickAsync();
        await Assertions.Expect(Editor(list, dialog)).ToBeVisibleAsync();
    }

    private Task WaitForQuery(string expected) =>
        Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query) == expected, new() { Timeout = 15_000 });

    private async Task DarkMode()
    {
        // A click on the theme button would close the popover (light dismiss): the theme changes first, then the editor opens again.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    // ---- counts

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Counts_leave_out_the_filters_own_rule_follow_the_others_and_sort_by_count(string mode)
    {
        // Initech has the Pending invoices only; the Paid ones belong to Umbrella, Wonka and Aperture (10 each).
        await OpenList(mode, "?fct.f.status=Paid&fct.f.customer=Initech");

        await OpenTag(Facets, "Status is Paid", "Edit filter Status");
        // The rule on the status is left out of its own counts, the one on the customer is not: Pending 10, the others 0 (by count, then by label).
        await Assertions.Expect(Options(Editor(Facets, "Edit filter Status"))).ToContainTextAsync([new Regex("Pending.*10"), new Regex("Draft.*0"), new Regex("Overdue.*0"), new Regex("Paid.*0")]);
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor(Facets, "Edit filter Status")).ToBeHiddenAsync();

        await OpenTag(Facets, "Customer is Initech", "Edit filter Customer");
        // The customers that have Paid invoices, 10 each (ties by label), then the selected customer, which has none under the status filter.
        await Assertions.Expect(Options(Editor(Facets, "Edit filter Customer"))).ToContainTextAsync([new Regex("Aperture.*10"), new Regex("Umbrella.*10"), new Regex("Wonka.*10"), new Regex("Initech.*0")]);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_include_empty_choice_and_yes_and_no_carry_their_counts(string mode)
    {
        await OpenList(mode, "?fct.f.reviewed=true&fct.f.status=Paid");

        await OpenTag(Facets, "Reviewed is Yes", "Edit filter Reviewed");
        // 10 of the 30 Paid invoices are reviewed (the counts leave out the rule on Reviewed itself).
        await Assertions.Expect(Editor(Facets, "Edit filter Reviewed").GetByRole(Control(mode), new() { Name = "Yes (10)", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Editor(Facets, "Edit filter Reviewed").GetByRole(Control(mode), new() { Name = "No (20)", Exact = true })).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");

        await OpenTag(Facets, "Status is Paid", "Edit filter Status");
        // Every invoice has a status: nothing is empty, and the switch says 0.
        await Assertions.Expect(Editor(Facets, "Edit filter Status").Locator("[data-slot=filter-include-empty-row], a[data-slot=filter-include-empty]")).ToContainTextAsync("0");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_count_is_part_of_what_a_screen_reader_hears_for_an_option_and_for_the_empty_values(string mode)
    {
        await OpenList(mode, "?fct.f.status=Paid");
        await OpenTag(Facets, "Status is Paid", "Edit filter Status");
        var editor = Editor(Facets, "Edit filter Status");

        if (mode == "ssr")
        {
            // A link holds its count: it is in its name.
            await Assertions.Expect(editor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^Pending.*30") })).ToBeVisibleAsync();
            await Assertions.Expect(editor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^Include empty values.*0") })).ToBeVisibleAsync();
            return;
        }
        // A checkbox and a switch are named by their label and described by their count.
        await Assertions.Expect(editor.GetByRole(AriaRole.Checkbox, new() { Name = "Pending", Exact = true })).ToHaveAccessibleDescriptionAsync("30");
        await Assertions.Expect(editor.GetByRole(AriaRole.Switch, new() { Name = "Include empty values" })).ToHaveAccessibleDescriptionAsync("0");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_limit_lists_the_first_options_with_show_more_and_a_static_editor_lists_them_all(string mode)
    {
        await OpenList(mode, "?fct.f.customer=Globex");
        await OpenTag(Facets, "Customer is Globex", "Edit filter Customer");
        var editor = Editor(Facets, "Edit filter Customer");

        if (mode == "ssr")
        {
            await Assertions.Expect(Options(editor)).ToHaveCountAsync(12);
            return;
        }
        // Ten invoices each, so by label: Acme Corp, Aperture, Cyberdyne, Globex (selected), Hooli and seven more.
        await Assertions.Expect(Options(editor)).ToHaveCountAsync(5);
        await editor.GetByRole(AriaRole.Button, new() { Name = "Show 7 more" }).ClickAsync();
        await Assertions.Expect(Options(editor)).ToHaveCountAsync(12);
        await Assertions.Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Show 7 more" })).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_counts_are_there_after_an_enhanced_navigation(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list?fct.f.reviewed=true");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Assertions.Expect(Facets).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await WaitForPage(mode);

        await OpenTag(Facets, "Reviewed is Yes", "Edit filter Reviewed");

        await Assertions.Expect(Editor(Facets, "Edit filter Reviewed").GetByRole(Control(mode), new() { Name = "Yes (40)", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Editor(Facets, "Edit filter Reviewed").GetByRole(Control(mode), new() { Name = "No (80)", Exact = true })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Counts_are_kept_while_paging_and_follow_a_new_filter(string mode)
    {
        await OpenList(mode, "?fct.f.status=Paid");
        await OpenTag(Facets, "Status is Paid", "Edit filter Status");
        await Assertions.Expect(Options(Editor(Facets, "Edit filter Status"))).ToContainTextAsync([new Regex("Draft.*30"), new Regex("Overdue.*30"), new Regex("Paid.*30"), new Regex("Pending.*30")]);
        await Page.Keyboard.PressAsync("Escape");

        await Facets.GetByRole(AriaRole.Button, new() { Name = "Page 2" }).ClickAsync();
        await Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query).Contains("fct.p=2", StringComparison.Ordinal), new() { Timeout = 15_000 });
        await OpenTag(Facets, "Status is Paid", "Edit filter Status");
        await Assertions.Expect(Options(Editor(Facets, "Edit filter Status"))).ToContainTextAsync([new Regex("Draft.*30"), new Regex("Overdue.*30"), new Regex("Paid.*30"), new Regex("Pending.*30")]);
        await Page.Keyboard.PressAsync("Escape");

        await Facets.GetByRole(AriaRole.Button, new() { Name = "Remove filter Status" }).ClickAsync();
        await WaitForQuery("");
        await Assertions.Expect(Tag(Facets, "Status")).ToHaveCountAsync(0);
    }

    // ---- remote options

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_remote_editor_loads_a_page_when_it_opens_searches_and_loads_more(string mode)
    {
        await OpenList(mode, "?rem.f.customer=Globex");
        // The resolver labels the value of the address.
        await Assertions.Expect(Tag(Remote, "Customer is Globex (Cypress Creek)")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await OpenTag(Remote, "Customer is Globex (Cypress Creek)", "Edit filter Customer");
        var editor = Editor(Remote, "Edit filter Customer");
        // By name, each with the count of the facet (ten invoices per customer). The pages are five long: the next ones load while the end of the list is near, so the 12 customers arrive in three requests.
        await Assertions.Expect(Options(editor).First).ToContainTextAsync("Acme Corp (Phoenix)", new() { Timeout = 15_000 });
        await Assertions.Expect(Options(editor).First).ToContainTextAsync("10");
        await Assertions.Expect(Options(editor)).ToHaveCountAsync(12, new() { Timeout = 15_000 });
        await Assertions.Expect(Options(editor).Last).ToContainTextAsync("Wonka (London)");

        await editor.GetByLabel("Search options").FillAsync("ar");
        // Stark Industries, and the selected customer, which no page of this search holds, on top.
        await Assertions.Expect(Options(editor)).ToHaveCountAsync(2, new() { Timeout = 15_000 });
        await Assertions.Expect(editor.Locator("[data-slot=filter-options] [data-value='Stark Industries']")).ToContainTextAsync("Stark Industries (New York)");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Picking_a_remote_option_writes_the_value_and_the_tag_shows_its_label(string mode)
    {
        await OpenList(mode, "?rem.f.customer=Globex");
        await OpenTag(Remote, "Customer is Globex (Cypress Creek)", "Edit filter Customer");
        var editor = Editor(Remote, "Edit filter Customer");
        await Assertions.Expect(editor.GetByRole(AriaRole.Checkbox, new() { Name = "Hooli (Palo Alto)" })).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await editor.GetByRole(AriaRole.Checkbox, new() { Name = "Hooli (Palo Alto)" }).CheckAsync();

        await WaitForQuery("?rem.f.customer=Globex,Hooli");
        await Assertions.Expect(Tag(Remote, "Customer is Globex (Cypress Creek), Hooli (Palo Alto)")).ToBeVisibleAsync();
        await Assertions.Expect(Remote.Locator("[data-slot=result-count]")).ToHaveTextAsync("1–10 of 20");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_broken_directory_shows_an_error_and_the_retry_button_loads_once_it_is_repaired(string mode)
    {
        await OpenList(mode, "?fail=1&rem.f.customer=Globex");
        await OpenTag(Remote, "Customer is Globex (Cypress Creek)", "Edit filter Customer");
        var editor = Editor(Remote, "Edit filter Customer");

        await Assertions.Expect(editor.GetByRole(AriaRole.Alert)).ToContainTextAsync("could not be loaded", new() { Timeout = 15_000 });
        // The address changes under the open editor (what the Repair link does, without closing the popover); the failed request stays failed until Retry asks again.
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?rem.f.customer=Globex')");
        await WaitForQuery("?rem.f.customer=Globex");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Retry" }).ClickAsync();

        await Assertions.Expect(Options(editor).First).ToContainTextAsync("Acme Corp (Phoenix)", new() { Timeout = 15_000 });
        await Assertions.Expect(editor.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task An_editor_whose_load_failed_asks_again_when_it_opens_after_the_repair(string mode)
    {
        await OpenList(mode, "?fail=1&rem.f.customer=Globex");
        await OpenTag(Remote, "Customer is Globex (Cypress Creek)", "Edit filter Customer");
        await Assertions.Expect(Editor(Remote, "Edit filter Customer").GetByRole(AriaRole.Alert)).ToContainTextAsync("could not be loaded", new() { Timeout = 15_000 });
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor(Remote, "Edit filter Customer")).ToBeHiddenAsync();

        await Remote.GetByTestId("rem-repair").ClickAsync();
        await WaitForQuery("?rem.f.customer=Globex");
        await OpenTag(Remote, "Customer is Globex (Cypress Creek)", "Edit filter Customer");

        await Assertions.Expect(Options(Editor(Remote, "Edit filter Customer")).First).ToContainTextAsync("Acme Corp (Phoenix)", new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task A_statically_rendered_remote_editor_lists_the_selected_options_with_their_labels_and_the_counted_ones()
    {
        await OpenList("ssr", "?rem.f.customer=Globex,Wonka");

        // The resolver ran on the server.
        await Assertions.Expect(Tag(Remote, "Customer is Globex (Cypress Creek), Wonka (London)")).ToBeVisibleAsync();
        await OpenTag(Remote, "Customer is Globex (Cypress Creek), Wonka (London)", "Edit filter Customer");
        var editor = Editor(Remote, "Edit filter Customer");

        await Assertions.Expect(editor.Locator("[data-slot=filter-options] [data-value=Wonka]")).ToContainTextAsync("Wonka (London)");
        await Assertions.Expect(editor.Locator("[data-slot=filter-options] [data-value=Globex]")).ToContainTextAsync("10");
        await Assertions.Expect(editor.Locator("[data-slot=filter-options-script]")).ToHaveTextAsync("Searching the other options needs JavaScript.");
    }

    // ---- accessibility

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task An_editor_with_counts_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?fct.f.status=Paid&fct.f.customer=Globex&fct.f.reviewed=true");
        await OpenTag(Facets, "Customer is Globex", "Edit filter Customer");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor(Facets, "Edit filter Customer")).ToBeHiddenAsync();

        await DarkMode();
        await OpenTag(Facets, "Customer is Globex", "Edit filter Customer");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_yes_no_counts_have_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?fct.f.reviewed=true");
        await OpenTag(Facets, "Reviewed is Yes", "Edit filter Reviewed");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");

        await DarkMode();
        await OpenTag(Facets, "Reviewed is Yes", "Edit filter Reviewed");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_remote_editor_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?rem.f.customer=Globex");
        await OpenTag(Remote, "Customer is", "Edit filter Customer");
        if (mode != "ssr") await Assertions.Expect(Options(Editor(Remote, "Edit filter Customer")).First).ToContainTextAsync("Acme Corp (Phoenix)", new() { Timeout = 15_000 });
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor(Remote, "Edit filter Customer")).ToBeHiddenAsync();

        await DarkMode();
        await OpenTag(Remote, "Customer is", "Edit filter Customer");
        if (mode != "ssr") await Assertions.Expect(Options(Editor(Remote, "Edit filter Customer")).First).ToContainTextAsync("Acme Corp (Phoenix)", new() { Timeout = 15_000 });

        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task The_data_list_guide_has_no_accessibility_violations_in_light_and_dark()
    {
        await OpenGuide("/docs/data-list");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Data list sources and counts", Level = 1 })).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();

        await DarkMode();

        await AssertNoAccessibilityViolations();
    }
}
