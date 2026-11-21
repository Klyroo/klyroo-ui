using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>BxInfiniteScroll: a sentinel that asks for the next items once, a button for the keyboard, a link for static pages.</summary>
public class InfiniteScrollTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator Feed => Page.GetByTestId("infinite-feed");
    private ILocator Count => Page.GetByTestId("infinite-count");
    private ILocator Requests => Page.GetByTestId("infinite-requests");
    private ILocator More => Page.GetByTestId("infinite-more");

    private static Task ScrollToEnd(ILocator scroller) => scroller.EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Reaching_the_sentinel_asks_once_until_the_load_is_over_and_the_end_is_announced(string mode)
    {
        await Open(mode, "infinite-scroll");
        await Assertions.Expect(Count).ToHaveTextAsync("Showing 20 of 60");
        await Assertions.Expect(Requests).ToHaveTextAsync("Load requests: 0");

        await ScrollToEnd(Feed);

        await Assertions.Expect(Requests).ToHaveTextAsync("Load requests: 1");
        await Assertions.Expect(More.Locator("[data-slot=status]")).ToHaveTextAsync("Loading more...");
        await Assertions.Expect(More).ToHaveAttributeAsync("aria-busy", "true");
        // Leave and come back while the load runs: the sentinel is visible again, and nothing is asked a second time.
        await Feed.EvaluateAsync("e => { e.scrollTop = 0; }");
        await Page.WaitForTimeoutAsync(100);
        await ScrollToEnd(Feed);

        await Assertions.Expect(Count).ToHaveTextAsync("Showing 40 of 60");
        await Assertions.Expect(More).Not.ToHaveAttributeAsync("aria-busy", "true");
        await Assertions.Expect(Requests).ToHaveTextAsync("Load requests: 1");
        await Assertions.Expect(More.Locator("[data-slot=status]")).ToHaveTextAsync("");

        // The new items pushed the sentinel out of view: the next visibility asks again, once.
        await ScrollToEnd(Feed);

        await Assertions.Expect(Count).ToHaveTextAsync("Showing 60 of 60");
        await Assertions.Expect(Requests).ToHaveTextAsync("Load requests: 2");
        await Assertions.Expect(More.Locator("[data-slot=status]")).ToHaveTextAsync("No more items");
        await Assertions.Expect(More.Locator("[data-slot=end]")).ToBeVisibleAsync();
        await Assertions.Expect(More.Locator("[data-slot=trigger]")).ToHaveCountAsync(0);
        await Assertions.Expect(More.Locator("[data-slot=status]")).ToHaveAttributeAsync("aria-live", "polite");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_sentinel_still_in_view_after_a_load_asks_for_the_next_page(string mode)
    {
        await Open(mode, "infinite-scroll");

        // A tall list shows the sentinel without scrolling: every finished load looks at it again.
        await Feed.EvaluateAsync("e => { e.style.height = '4000px'; }");

        await Assertions.Expect(Count).ToHaveTextAsync("Showing 60 of 60");
        await Assertions.Expect(Requests).ToHaveTextAsync("Load requests: 2");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_button_works_by_keyboard_and_keeps_the_focus_until_the_end(string mode)
    {
        // No IntersectionObserver: the button is the only way, and it has to be enough.
        var context = await NewContext(new());
        await context.AddInitScriptAsync("delete window.IntersectionObserver;");
        var page = await context.NewPageAsync();
        await Open(page, mode, "infinite-scroll");
        var feed = page.GetByTestId("infinite-feed");
        var button = page.GetByTestId("infinite-more").GetByRole(AriaRole.Button, new() { Name = "Load more" });
        await ScrollToEnd(feed);
        await page.WaitForTimeoutAsync(800);
        await Assertions.Expect(page.GetByTestId("infinite-requests")).ToHaveTextAsync("Load requests: 0");

        await feed.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(button).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(page.GetByTestId("infinite-count")).ToHaveTextAsync("Showing 40 of 60");
        await Assertions.Expect(button).ToBeFocusedAsync();
        await page.Keyboard.PressAsync(" ");

        await Assertions.Expect(page.GetByTestId("infinite-count")).ToHaveTextAsync("Showing 60 of 60");
        await Assertions.Expect(page.GetByTestId("infinite-requests")).ToHaveTextAsync("Load requests: 2");
        // The button went away with the focus on it: the focus goes to the announcement, not to the top of the page.
        await Assertions.Expect(page.GetByTestId("infinite-more").Locator("[data-slot=status]")).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByTestId("infinite-more").Locator("[data-slot=status]")).ToHaveTextAsync("No more items");
    }

    [Fact]
    public async Task Static_rendering_turns_the_button_into_a_link_to_the_next_page()
    {
        await Open("ssr", "infinite-scroll");
        await Assertions.Expect(Count).ToHaveTextAsync("Showing 20 of 60");
        var link = More.GetByRole(AriaRole.Link, new() { Name = "Load more" });
        await Assertions.Expect(link).ToHaveAttributeAsync("href", new Regex(@"/ssr/components/infinite-scroll\?feed=2$"));

        await link.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"\?feed=2$"));
        await Assertions.Expect(Count).ToHaveTextAsync("Showing 40 of 60");
        await Assertions.Expect(Requests).ToHaveTextAsync("Load requests: 0");

        await More.GetByRole(AriaRole.Link, new() { Name = "Load more" }).ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"\?feed=3$"));
        await Assertions.Expect(Count).ToHaveTextAsync("Showing 60 of 60");
        await Assertions.Expect(More.Locator("[data-slot=status]")).ToHaveTextAsync("No more items");
        await Assertions.Expect(More.Locator("[data-slot=end]")).ToBeVisibleAsync();
        await Assertions.Expect(More.Locator("[data-slot=trigger]")).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_an_interactive_page_the_button_loads_in_place_instead_of_leaving_the_page(string mode)
    {
        await Open(mode, "infinite-scroll");
        var button = More.GetByRole(AriaRole.Button, new() { Name = "Load more" });
        var before = Page.Url;

        await button.ClickAsync();

        await Assertions.Expect(Count).ToHaveTextAsync("Showing 40 of 60");
        Assert.Equal(before, Page.Url);
        await Assertions.Expect(More.GetByRole(AriaRole.Link)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task An_explicit_root_and_a_Loading_owned_by_the_parent_work_too(string mode)
    {
        await Open(mode, "infinite-scroll");
        var log = Page.GetByTestId("log-feed");
        var count = Page.GetByTestId("log-count");
        var more = Page.GetByTestId("log-more");
        await Assertions.Expect(more).ToHaveAttributeAsync("data-root", "#log-scroll");
        await Assertions.Expect(count).ToHaveTextAsync("Lines: 20");

        await ScrollToEnd(log);

        await Assertions.Expect(count).ToHaveTextAsync("Lines: 35");
        await ScrollToEnd(log);
        await Assertions.Expect(count).ToHaveTextAsync("Lines: 50");
        await Assertions.Expect(more.Locator("[data-slot=status]")).ToHaveTextAsync("No more items");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_Loading_that_ends_looks_at_the_sentinel_again(string mode)
    {
        await Open(mode, "infinite-scroll");
        var log = Page.GetByTestId("log-feed");

        // The handler returns at once and only the parent's Loading says the load is running: it turning false re-arms the sentinel.
        await log.EvaluateAsync("e => { e.style.height = '4000px'; }");

        await Assertions.Expect(Page.GetByTestId("log-count")).ToHaveTextAsync("Lines: 50");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_spinner_stops_for_people_who_asked_for_less_motion(string mode)
    {
        foreach (var (reduce, expected) in new[] { (true, "none"), (false, "spin") })
        {
            var context = await NewContext(new() { ReducedMotion = reduce ? ReducedMotion.Reduce : ReducedMotion.NoPreference });
            var page = await context.NewPageAsync();
            await Open(page, mode, "infinite-scroll");

            await page.GetByTestId("infinite-more").GetByRole(AriaRole.Button, new() { Name = "Load more" }).ClickAsync();
            var spinner = page.GetByTestId("infinite-more").Locator("[data-slot=trigger] svg");
            await Assertions.Expect(spinner).ToBeVisibleAsync();

            await Assertions.Expect(spinner).ToHaveCSSAsync("animation-name", expected);
        }
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_has_no_accessibility_violations_in_light_and_dark_also_at_the_end(string mode)
    {
        await Open(mode, "infinite-scroll");
        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await AssertNoAccessibilityViolations();

        if (mode == "ssr") await Page.GotoAsync("/ssr/components/infinite-scroll?feed=3");
        else
        {
            await ScrollToEnd(Feed);
            await Assertions.Expect(Count).ToHaveTextAsync("Showing 40 of 60");
            await ScrollToEnd(Feed);
        }
        await Assertions.Expect(Count).ToHaveTextAsync("Showing 60 of 60");
        await Assertions.Expect(More.Locator("[data-slot=end]")).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
    }
}
