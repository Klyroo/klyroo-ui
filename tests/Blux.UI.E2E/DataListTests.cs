using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The data list on its playground page, in the four render modes: sort (button or link), pagination, page size, the URL, Back, a typed URL, refresh,
/// the error state and its retry, the single load after a prerender, accessibility and a narrow screen. The lists of the page are told apart by their test ids:
/// "list-url" has no URL prefix and counts its provider calls, "list-memory" uses the prefix "mem", "list-error" fails on demand.
/// </summary>
public class DataListTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list{query}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        // The provider list waits 600 ms before it shows rows: the page is ready when no list shows placeholders.
        await Assertions.Expect(Page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator Url => Page.GetByTestId("list-url");

    private ILocator Rows(ILocator list) => list.Locator("tbody tr[data-slot=row]");

    private ILocator Cell(ILocator list, int row, int column) => Rows(list).Nth(row).Locator("td").Nth(column);

    private ILocator Count => Page.GetByTestId("url-count");

    private ILocator Calls => Page.GetByTestId("url-calls");

    private ILocator Query => Page.GetByTestId("url-query");

    private static AriaRole SortRole(string mode) => mode == "ssr" ? AriaRole.Link : AriaRole.Button;

    private ILocator Sort(string mode, string name, string direction) =>
        Url.GetByRole(SortRole(mode), new() { Name = $"Sort by {name}, {direction}" });

    private Task WaitForQuery(string expected) =>
        // The address bar, not the text of the page: this is what a user shares.
        Page.WaitForURLAsync(url => new Uri(url).Query == expected, new() { Timeout = 15_000 });

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_list_renders_a_table_with_the_first_page_and_a_live_result_count(string mode)
    {
        await OpenList(mode);

        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(10);
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("1");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");
        await Assertions.Expect(Count).ToHaveAttributeAsync("role", "status");
        await Assertions.Expect(Count).ToHaveAttributeAsync("aria-live", "polite");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "Customer" })).ToHaveAttributeAsync("aria-sort", "none");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "No." })).ToHaveAttributeAsync("aria-sort", "ascending");
        // The table scrolls inside a named region.
        await Assertions.Expect(Url.GetByRole(AriaRole.Region, new() { Name = "Invoices in the URL" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sorting_a_column_by_click_or_link_updates_the_rows_and_the_url(string mode)
    {
        await OpenList(mode);

        // Sorted by No. ascending: the next click on it sorts descending.
        await Sort(mode, "No.", "descending").ClickAsync();

        await WaitForQuery("?s=-id");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("120");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "No." })).ToHaveAttributeAsync("aria-sort", "descending");
        await Assertions.Expect(Sort(mode, "No.", "ascending")).ToHaveCountAsync(0);
        await Assertions.Expect(Url.GetByRole(SortRole(mode), new() { Name = "Clear the sort by No." })).ToBeVisibleAsync();

        await Sort(mode, "Amount", "ascending").ClickAsync();

        await WaitForQuery("?s=amount");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("95");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "Amount" })).ToHaveAttributeAsync("aria-sort", "ascending");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "No." })).ToHaveAttributeAsync("aria-sort", "none");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Paging_changes_the_rows_the_count_and_the_url(string mode)
    {
        await OpenList(mode);
        var pagination = Page.GetByTestId("url-pagination");

        await pagination.GetByLabel("Page 2", new() { Exact = true }).ClickAsync();

        await WaitForQuery("?p=2");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("11");
        await Assertions.Expect(Count).ToHaveTextAsync("11–20 of 120");
        await Assertions.Expect(pagination.GetByLabel("Page 2", new() { Exact = true })).ToHaveAttributeAsync("aria-current", "page");

        await pagination.GetByLabel("Next page").ClickAsync();

        await WaitForQuery("?p=3");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("21");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_new_page_size_applies_goes_back_to_the_first_page_and_is_in_the_url(string mode)
    {
        await OpenList(mode, "?p=3");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("21");

        var select = Page.GetByTestId("url-pagination").GetByLabel("Rows per page");
        await select.SelectOptionAsync("25");
        if (mode == "ssr")
        {
            // Without a circuit the picker is a GET form: nothing happens before the button is pressed.
            await Page.GetByTestId("url-pagination").GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        }

        await WaitForQuery("?ps=25");
        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(25);
        await Assertions.Expect(Count).ToHaveTextAsync("1–25 of 120");
        await Assertions.Expect(select).ToHaveValueAsync("25");
    }

    [Fact]
    public async Task The_page_size_form_keeps_the_sort_and_the_other_parameters()
    {
        await OpenList("ssr", "?s=-amount&utm_source=mail&mem.p=2");

        await Page.GetByTestId("url-pagination").GetByLabel("Rows per page").SelectOptionAsync("50");
        await Page.GetByTestId("url-pagination").GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).Query.Contains("ps=50"));
        var query = new Uri(Page.Url).Query;
        Assert.Contains("s=-amount", query);
        Assert.Contains("utm_source=mail", query);
        // The other list of the page keeps its own page.
        Assert.Contains("mem.p=2", query);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Back_restores_the_previous_list_state(string mode)
    {
        await OpenList(mode);
        await Page.GetByTestId("url-pagination").GetByLabel("Page 2", new() { Exact = true }).ClickAsync();
        await WaitForQuery("?p=2");
        await Sort(mode, "No.", "descending").ClickAsync();
        await WaitForQuery("?s=-id");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("120");

        await Page.GoBackAsync();

        await WaitForQuery("?p=2");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("11");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "No." })).ToHaveAttributeAsync("aria-sort", "ascending");

        await Page.GoBackAsync();

        await WaitForQuery("");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("1");
        await Assertions.Expect(Count).ToHaveTextAsync("1–10 of 120");

        await Page.GoForwardAsync();

        await WaitForQuery("?p=2");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("11");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_typed_url_loads_its_state(string mode)
    {
        await OpenList(mode, "?s=-amount&p=2&ps=5&utm_source=mail");

        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(5);
        await Assertions.Expect(Count).ToHaveTextAsync("6–10 of 120");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "Amount" })).ToHaveAttributeAsync("aria-sort", "descending");
        await Assertions.Expect(Page.GetByTestId("url-pagination").GetByLabel("Rows per page")).ToHaveValueAsync("5");
        await Assertions.Expect(Page.GetByTestId("url-pagination").GetByLabel("Page 2", new() { Exact = true })).ToHaveAttributeAsync("aria-current", "page");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_page_beyond_the_last_one_shows_the_last_page_and_an_interactive_list_fixes_the_url(string mode)
    {
        await OpenList(mode, "?p=99&ps=50");

        await Assertions.Expect(Count).ToHaveTextAsync("101–120 of 120");
        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(20);
        if (mode != "ssr")
        {
            // Normalization replaces the entry: the address shows the page that is there.
            await WaitForQuery("?p=3&ps=50");
        }
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Two_lists_on_a_page_keep_separate_urls(string mode)
    {
        await OpenList(mode);
        var memory = Page.GetByTestId("list-memory");

        await memory.GetByLabel("Page 3", new() { Exact = true }).ClickAsync();

        await WaitForQuery("?mem.p=3");
        await Assertions.Expect(Cell(memory, 0, 0)).ToHaveTextAsync("21");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("1");

        await Page.GetByTestId("url-pagination").GetByLabel("Page 2", new() { Exact = true }).ClickAsync();

        await WaitForQuery("?mem.p=3&p=2");
        await Assertions.Expect(Cell(memory, 0, 0)).ToHaveTextAsync("21");
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("11");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Refresh_loads_the_rows_again(string mode)
    {
        await OpenList(mode);
        var before = int.Parse(await Calls.InnerTextAsync());

        await Url.GetByRole(mode == "ssr" ? AriaRole.Link : AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        if (mode == "ssr")
        {
            // The link reloads the page, which loads the list once more.
            await Assertions.Expect(Calls).ToHaveTextAsync("1");
        }
        else
        {
            await Assertions.Expect(Calls).ToHaveTextAsync((before + 1).ToString());
        }
        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(10);
        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("1");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_prerendered_list_is_not_loaded_a_second_time(string mode)
    {
        await OpenList(mode);

        // The interactive render shows the result of the prerender, with its rows and no placeholders, and calls the provider zero times.
        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(10);
        await Assertions.Expect(Calls).ToHaveTextAsync("0");
        await Page.WaitForTimeoutAsync(500);
        await Assertions.Expect(Calls).ToHaveTextAsync("0");

        // The next request is the first one of this render.
        await Sort(mode, "Customer", "ascending").ClickAsync();

        await Assertions.Expect(Calls).ToHaveTextAsync("1");
        await Assertions.Expect(Url.GetByRole(AriaRole.Columnheader, new() { Name = "Customer" })).ToHaveAttributeAsync("aria-sort", "ascending");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_prerendered_list_for_a_typed_state_is_adopted_too(string mode)
    {
        // The persisted result is the one of this very state, a page past the end included: the clamped page is adopted, not loaded again.
        await OpenList(mode, "?s=-amount&p=99&ps=50");

        await Assertions.Expect(Count).ToHaveTextAsync("101–120 of 120");
        await Assertions.Expect(Calls).ToHaveTextAsync("0");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public async Task With_per_page_interactivity_a_prerendered_list_is_not_loaded_a_second_time(string mode)
    {
        // The router renders statically and only this page has a render mode: the component that registers the persisted result sees no mode from a parent.
        await Page.GotoAsync($"/perpage/{mode}");
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });

        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(10);
        await Assertions.Expect(Calls).ToHaveTextAsync("0");
        await Page.WaitForTimeoutAsync(500);
        await Assertions.Expect(Calls).ToHaveTextAsync("0");

        await Sort(mode, "Customer", "ascending").ClickAsync();

        await Assertions.Expect(Calls).ToHaveTextAsync("1");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Every_list_has_a_named_table_and_its_own_pagination_landmark(string mode)
    {
        await OpenList(mode);

        foreach (var label in new[] { "Invoices in memory", "Invoices from a provider", "Invoices in the URL", "Invoices from a flaky backend" })
        {
            await Assertions.Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = $"Pagination, {label}", Exact = true })).ToHaveCountAsync(1);
        }
        await Assertions.Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Invoices", Exact = true })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task A_static_render_loads_the_list_once()
    {
        await OpenList("ssr");

        await Assertions.Expect(Calls).ToHaveTextAsync("1");
        await Assertions.Expect(Page.GetByTestId("url-zone")).ToHaveTextAsync("none");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task An_interactive_request_carries_the_time_zone_of_the_browser(string mode)
    {
        await OpenList(mode);
        var zone = await Page.EvaluateAsync<string>("() => Intl.DateTimeFormat().resolvedOptions().timeZone");

        await Sort(mode, "Customer", "ascending").ClickAsync();

        await Assertions.Expect(Page.GetByTestId("url-zone")).ToHaveTextAsync(zone);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Focus_stays_in_the_pagination_after_a_page_change(string mode)
    {
        await OpenList(mode);
        var pagination = Page.GetByTestId("url-pagination");

        await pagination.GetByLabel("Next page").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("11");
        Assert.True(await pagination.EvaluateAsync<bool>("el => el.contains(document.activeElement)"), "focus left the pagination");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Focus_stays_in_the_pagination_when_the_pressed_button_is_disabled_by_the_change(string mode)
    {
        await OpenList(mode, "?p=11");
        var pagination = Page.GetByTestId("url-pagination");

        // Page 12 is the last: the next button, which had focus, is disabled by the change.
        await pagination.GetByLabel("Next page").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Cell(Url, 0, 0)).ToHaveTextAsync("111");
        await Assertions.Expect(pagination.GetByLabel("Next page")).ToBeDisabledAsync();
        Assert.True(await pagination.EvaluateAsync<bool>("el => el.contains(document.activeElement)"), "focus left the pagination on the last page");
        await Assertions.Expect(pagination.GetByLabel("Page 12", new() { Exact = true })).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task An_error_is_shown_with_a_retry_and_the_list_recovers(string mode)
    {
        await OpenList(mode, "?broken=1");
        var list = Page.GetByTestId("list-error");
        var alert = Page.GetByTestId("err-state");

        await Assertions.Expect(alert).ToBeVisibleAsync();
        await Assertions.Expect(alert).ToHaveAttributeAsync("role", "alert");
        await Assertions.Expect(alert).ToContainTextAsync("Could not load the data");
        await Assertions.Expect(Rows(list)).ToHaveCountAsync(0);
        await Assertions.Expect(list.Locator("[data-slot=empty-row]")).ToHaveCountAsync(0);

        if (mode == "ssr")
        {
            // A link to the same address: the server asks the provider again, which still fails.
            await alert.GetByRole(AriaRole.Link, new() { Name = "Try again" }).ClickAsync();
            await Assertions.Expect(alert).ToBeVisibleAsync();
            await Page.GetByTestId("err-repair").ClickAsync();
        }
        else
        {
            await Page.GetByTestId("err-repair").ClickAsync();
            await WaitForQuery("");
            await alert.GetByRole(AriaRole.Button, new() { Name = "Try again" }).ClickAsync();
        }

        await Assertions.Expect(alert).ToHaveCountAsync(0);
        await Assertions.Expect(Rows(list)).ToHaveCountAsync(10);
        await Assertions.Expect(Cell(list, 0, 0)).ToHaveTextAsync("1");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_failed_reload_keeps_the_rows_on_screen(string mode)
    {
        await OpenList(mode);
        var list = Page.GetByTestId("list-error");
        await Assertions.Expect(Rows(list)).ToHaveCountAsync(10);

        await Page.GetByTestId("err-break").ClickAsync();
        await WaitForQuery("?broken=1");
        await list.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("err-state")).ToBeVisibleAsync();
        await Assertions.Expect(Rows(list)).ToHaveCountAsync(10);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_list_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?p=2&s=-amount");
        await Assertions.Expect(Rows(Url)).ToHaveCountAsync(10);

        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        // A color transition still running would be measured half way by the contrast rule.
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_error_state_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?broken=1");
        await Assertions.Expect(Page.GetByTestId("err-state")).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_page_does_not_scroll_sideways_and_the_table_scrolls_inside_its_container(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenList(mode);

        var overflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(overflow, "the page scrolls horizontally at 375px");
        var region = Page.GetByTestId("list-memory").Locator("[data-slot=table-root]");
        var scrolls = await region.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth");
        Assert.True(scrolls, "the table does not scroll inside its container");
        // The container itself fits the screen.
        var box = await region.BoundingBoxAsync();
        Assert.True(box!.X + box.Width <= 375.5, $"the table container is {box.Width}px wide at x={box.X}");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Texts_follow_the_language_of_the_user(string mode)
    {
        var page = await NewPage("fr", "fr");
        await page.GotoAsync($"/{mode}/components/data-list");
        await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var list = page.GetByTestId("list-url");

        await Assertions.Expect(list.GetByRole(AriaRole.Button, new() { Name = "Trier par Customer, croissant" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("url-count")).ToHaveTextAsync("1–10 sur 120");
        await Assertions.Expect(page.GetByTestId("url-pagination").GetByLabel("Lignes par page")).ToBeVisibleAsync();
    }
}
