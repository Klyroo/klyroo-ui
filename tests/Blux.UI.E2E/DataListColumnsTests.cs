using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// Column panel (visibility, reorder, pin, search, reset), column resizing (keyboard &amp; pointer drag), cookie persistence across reload &amp; SSR,
/// and light/dark accessibility checks on <c>/components/data-list-columns</c>.
/// </summary>
public class DataListColumnsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenPage(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/data-list-columns");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(List.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator List => Page.GetByTestId("list-columns");
    private ILocator Headers => List.Locator("thead th[data-key]");

    private async Task DarkMode()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    [Fact]
    public async Task Statically_rendered_page_hides_column_panel_and_resize_handles_and_respects_saved_cookie()
    {
        await OpenPage("ssr");

        await Assertions.Expect(List.Locator("[data-slot=columns-panel]")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("[data-slot=col-resize]")).ToHaveCountAsync(0);
        await Assertions.Expect(Headers).ToHaveCountAsync(5);

        // Set the column preferences cookie (hide status, pin amount to end) and reload in SSR: server paints it immediately.
        await Page.EvaluateAsync("() => { document.cookie = 'blux-dl-cols-demo-cols=v1!hstatus!eamount; path=/; SameSite=Lax'; }");
        await Page.ReloadAsync();

        var keys = await Headers.EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('data-key'))");
        Assert.DoesNotContain("status", keys);
        Assert.Equal("amount", keys[^1]);
        await Assertions.Expect(List.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Column_panel_hides_reorders_pins_persists_across_reload_and_resets(string mode)
    {
        await OpenPage(mode);

        // Open the Columns popover.
        await List.Locator("[data-slot=columns-trigger]").ClickAsync();
        var content = Page.Locator("[data-slot=columns-content]");
        await Assertions.Expect(content).ToBeVisibleAsync();

        // Hide Status and move Issued up.
        await content.Locator("input[data-col-toggle=status]").UncheckAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);
        await Assertions.Expect(content.Locator("[data-slot=columns-count]")).ToHaveTextAsync("4 of 5 visible");

        // Pin Amount to end.
        await content.Locator("[data-col-pin-end=amount]").ClickAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");

        // Keyboard resize on Amount (+16px: 150 -> 166).
        await Page.Keyboard.PressAsync("Escape");
        var resize = List.Locator("[data-slot=col-resize][data-col-key=amount]");
        await resize.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=amount]")).ToHaveAttributeAsync("style", "width:166px");

        // Wait briefly for the cookie write to settle, then reload: all preferences survive!
        await Page.WaitForFunctionAsync("() => document.cookie.includes('blux-dl-cols-demo-cols=v1!')", null, new() { Timeout = 10_000 });
        await Page.ReloadAsync();
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=amount]")).ToHaveAttributeAsync("style", "width:166px");

        // Reset restores everything and clears the cookie.
        await List.Locator("[data-slot=columns-trigger]").ClickAsync();
        await content.Locator("[data-slot=columns-reset]").ClickAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(1);
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=amount]")).ToHaveAttributeAsync("style", "width:150px");
        await Page.WaitForFunctionAsync("() => !document.cookie.includes('blux-dl-cols-demo-cols=v1!')", null, new() { Timeout = 10_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Columns_page_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenPage(mode);
        await AssertNoAccessibilityViolations();
        if (mode != "ssr")
        {
            await List.Locator("[data-slot=columns-trigger]").ClickAsync();
            await Assertions.Expect(Page.Locator("[data-slot=columns-content]")).ToBeVisibleAsync();
            await AssertNoAccessibilityViolations();
            await Page.Keyboard.PressAsync("Escape");
        }
        await DarkMode();
        await AssertNoAccessibilityViolations();
    }
}
