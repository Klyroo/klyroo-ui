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
    [MemberData(nameof(AllModes))]
    public async Task Cursor_and_virtualized_page_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenPage(mode);
        await AssertNoAccessibilityViolations();
        await DarkMode();
        await AssertNoAccessibilityViolations();
    }
}
