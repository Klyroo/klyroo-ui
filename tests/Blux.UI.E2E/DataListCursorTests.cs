using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// Cursor paging, infinite loader (manual, auto scroll, inline error + retry, static SSR link fallback) and row virtualization
/// on <c>/components/data-list-cursor</c>, across all four render modes and light/dark accessibility checks.
/// </summary>
public class DataListCursorTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenPage(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-cursor{query}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-infinite-scroll')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(ManualList.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator ManualList => Page.GetByTestId("list-cursor");
    private ILocator AutoList => Page.GetByTestId("list-cursor-auto");
    private ILocator VirtualList => Page.GetByTestId("list-virtualized");
    private ILocator VirtualCursorList => Page.GetByTestId("list-virtual-cursor");

    private async Task DarkMode()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    [Fact]
    public async Task Statically_rendered_cursor_degrades_to_links_and_virtualization_stays_off()
    {
        await OpenPage("ssr");

        // Manual cursor list starts with 10 rows and a link to the next cursor.
        var rows = ManualList.Locator("tbody tr[data-slot=row]");
        await Assertions.Expect(rows).ToHaveCountAsync(10);
        var next = ManualList.Locator("[data-slot=infinite-loader] a[data-slot=load-more-trigger]");
        await Assertions.Expect(next).ToHaveAttributeAsync("href", new Regex(@"cur\.c="));

        await next.ClickAsync();
        await Page.WaitForUrlMatchingAsync(@"cur\.c=");

        await Assertions.Expect(rows).ToHaveCountAsync(10);
        await Assertions.Expect(rows.First).ToContainTextAsync("11");
        await Assertions.Expect(ManualList.Locator("[data-slot=result-count]")).ToHaveTextAsync("10 of 25");

        // Back to start returns to page 1 without cur.c.
        await ManualList.Locator("[data-slot=cursor-reset]").ClickAsync();
        await Page.WaitForUrlAsync("!url.searchParams.has('cur.c')");
        await Assertions.Expect(rows.First).ToContainTextAsync("1");

        // Virtualized table never virtualizes in static SSR: all 200 rows are rendered directly.
        await Assertions.Expect(VirtualList.Locator("[data-slot=table-root]")).Not.ToHaveAttributeAsync("data-virtualized", "true");
        await Assertions.Expect(VirtualList.Locator("tbody tr[data-slot=row]")).ToHaveCountAsync(200);
        await Assertions.Expect(VirtualCursorList.Locator("[data-slot=table-root]")).Not.ToHaveAttributeAsync("data-virtualized", "true");
        await Assertions.Expect(VirtualCursorList.Locator("tbody tr[data-slot=row]")).ToHaveCountAsync(20);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Manual_cursor_loader_appends_pages_reaches_end_and_resets_on_sort(string mode)
    {
        await OpenPage(mode);

        var rows = ManualList.Locator("tbody tr[data-slot=row]");
        var count = ManualList.Locator("[data-slot=result-count]");
        var loadMore = ManualList.Locator("[data-slot=load-more-trigger]");

        await Assertions.Expect(rows).ToHaveCountAsync(10);
        await Assertions.Expect(count).ToHaveTextAsync("1–10 of 25");

        await loadMore.ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(20, new() { Timeout = 15_000 });
        await Assertions.Expect(count).ToHaveTextAsync("1–20 of 25");

        await loadMore.ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(25, new() { Timeout = 15_000 });
        await Assertions.Expect(count).ToHaveTextAsync("1–25 of 25");
        await Assertions.Expect(loadMore).ToHaveCountAsync(0);
        await Assertions.Expect(ManualList.Locator("[data-slot=end]")).ToHaveTextAsync("No more items");

        // Changing sort restarts from the first page (10 rows).
        await ManualList.GetByRole(AriaRole.Button, new() { Name = "Sort by Customer, ascending" }).ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(10, new() { Timeout = 15_000 });
        await Assertions.Expect(loadMore).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Auto_infinite_loader_fetches_on_scroll_and_retries_after_load_more_error(string mode)
    {
        await OpenPage(mode, "?failmore=1");

        var rows = AutoList.Locator("tbody tr[data-slot=row]");
        var scroller = AutoList.GetByTestId("cursor-scroll");
        await Assertions.Expect(rows).ToHaveCountAsync(10);

        // Scroll to the bottom: triggers auto load-more, which fails once with ?failmore=1.
        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");

        var error = AutoList.Locator("[data-slot=load-more-error]");
        await Assertions.Expect(error).ToBeVisibleAsync(new() { Timeout = 15_000 });
        // Previously loaded rows stay intact.
        await Assertions.Expect(rows).ToHaveCountAsync(10);

        // Clicking Retry recovers and appends page 2.
        await AutoList.Locator("[data-slot=load-more-retry]").ClickAsync();
        await Assertions.Expect(error).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(rows).ToHaveCountAsync(20, new() { Timeout = 15_000 });

        // Scrolling down again automatically loads page 3.
        await scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Assertions.Expect(rows).ToHaveCountAsync(30, new() { Timeout = 15_000 });
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Virtualized_table_renders_a_window_of_rows_and_updates_when_scrolled(string mode)
    {
        await OpenPage(mode);

        var root = VirtualList.Locator("[data-slot=table-root]");
        var rows = VirtualList.Locator("tbody tr[data-slot=row]");

        await Assertions.Expect(root).ToHaveAttributeAsync("data-virtualized", "true");
        await Assertions.Expect(VirtualList.Locator("table")).ToHaveAttributeAsync("aria-rowcount", "201");

        // Wait for Virtualize to settle to a window far smaller than all 200 rows.
        await Page.WaitForFunctionAsync(
            "() => { const n = document.querySelectorAll('[data-testid=\"list-virtualized\"] tbody tr[data-slot=row]').length; return n > 0 && n < 50; }",
            null, new() { Timeout = 15_000 });

        var initialCount = await rows.CountAsync();
        Assert.InRange(initialCount, 5, 49);

        // Scroll deep into the 200-row table (~44px * 120 = 5280px).
        await root.EvaluateAsync("el => el.scrollTop = 5280");
        await Page.WaitForFunctionAsync(
            "() => { const first = document.querySelector('[data-testid=\"list-virtualized\"] tbody tr[data-slot=row]'); return first && Number(first.getAttribute('aria-rowindex')) > 80; }",
            null, new() { Timeout = 15_000 });

        var scrolledCount = await rows.CountAsync();
        Assert.InRange(scrolledCount, 5, 49);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Virtualized_cursor_table_loads_more_on_scroll_refreshes_loaded_pages_restores_virtual_focus_and_resets_from_mid_way_cursor(string mode)
    {
        await OpenPage(mode);

        var root = VirtualCursorList.Locator("[data-slot=table-root]");
        var table = VirtualCursorList.Locator("table");
        var count = VirtualCursorList.Locator("[data-slot=result-count]");

        await Assertions.Expect(root).ToHaveAttributeAsync("data-virtualized", "true");
        await Assertions.Expect(root.Locator("[data-slot=infinite-loader]")).ToHaveCountAsync(1);
        await Assertions.Expect(table).ToHaveAttributeAsync("aria-rowcount", "21");
        await Assertions.Expect(count).ToHaveTextAsync("1–20 of 600");

        // Focus row 1's selection checkbox before scrolling it out of the virtualized window.
        var firstRowCheckbox = VirtualCursorList.Locator("tbody tr[data-slot=row][aria-rowindex='2'] input[data-select=row]");
        await firstRowCheckbox.FocusAsync();
        await Assertions.Expect(firstRowCheckbox).ToBeFocusedAsync();

        // Scroll to the bottom of the virtualized table's scroll box: appends page 2 (rows 21..40) and unmounts row 1.
        await root.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Assertions.Expect(count).ToHaveTextAsync("1–40 of 600", new() { Timeout = 15_000 });
        await Assertions.Expect(table).ToHaveAttributeAsync("aria-rowcount", "41");

        // Scroll further into page 2 so row 1 (aria-rowindex=2) is outside the virtualized overscan window (which also reaches the sentinel and loads page 3).
        await root.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Assertions.Expect(VirtualCursorList.Locator("tbody tr[data-slot=row][aria-rowindex='2']")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(count).ToHaveTextAsync("1–60 of 600", new() { Timeout = 15_000 });

        // Scroll back to the top: when row 1 re-enters the virtualized window, its checkbox receives focus back.
        await root.EvaluateAsync("el => el.scrollTop = 0");
        await Assertions.Expect(firstRowCheckbox).ToBeFocusedAsync(new() { Timeout = 15_000 });

        // Refresh re-fetches all three loaded cursor pages (keeping 1–60 of 600 on screen).
        await VirtualCursorList.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();
        await Assertions.Expect(count).ToHaveTextAsync("1–60 of 600", new() { Timeout = 15_000 });
        await Assertions.Expect(table).ToHaveAttributeAsync("aria-rowcount", "61");

        // Entering the page mid-way at a cursor in interactive mode shows the Back to start button, which resets to page 1 and focuses the table.
        await OpenPage("ssr");
        var midHref = await VirtualCursorList.Locator("[data-slot=infinite-loader] a").GetAttributeAsync("href");
        Assert.NotNull(midHref);
        var queryIndex = midHref.IndexOf('?');
        Assert.True(queryIndex >= 0);

        await OpenPage(mode, midHref[queryIndex..]);
        await Assertions.Expect(count).ToHaveTextAsync("20 of 600");
        var resetButton = VirtualCursorList.Locator("button[data-slot=cursor-reset]");
        await Assertions.Expect(resetButton).ToBeVisibleAsync();

        await resetButton.ClickAsync();
        await Page.WaitForUrlAsync("!url.searchParams.has('vc.c')");
        await Assertions.Expect(count).ToHaveTextAsync("1–20 of 600", new() { Timeout = 15_000 });
        await Assertions.Expect(resetButton).ToHaveCountAsync(0);
        await Assertions.Expect(root).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Cursor_and_virtualized_page_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenPage(mode);
        await AssertNoAccessibilityViolations();
        await DarkMode();
        await AssertNoAccessibilityViolations();
    }
}
