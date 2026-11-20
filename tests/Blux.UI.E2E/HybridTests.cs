using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The browser side of Blux in a Blazor Hybrid web view, without a device. A web view is recognized by the message bridge
/// blazor.webview.js uses (window.external.sendMessage / receiveMessage): the page is given one before it loads, so the
/// storage, culture and touch behavior of the web view run in the playground. The real thing is the emulator run of the sample.
/// </summary>
public class HybridTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private const string Bridge = "Object.defineProperty(window, 'external', { value: { sendMessage() {}, receiveMessage() {} }, configurable: true });";

    private async Task<IPage> NewWebViewPage(bool touch = false)
    {
        var context = await NewContext(new() { HasTouch = touch });
        await context.AddInitScriptAsync(Bridge);
        return await context.NewPageAsync();
    }

    [Fact]
    public async Task The_page_is_marked_as_running_in_a_web_view_only_when_the_bridge_exists()
    {
        var hybrid = await NewWebViewPage();
        await Open(hybrid, "server", "button");
        await Assertions.Expect(hybrid.Locator("html")).ToHaveAttributeAsync("data-bx-host", "webview");

        await Open("server", "button");
        await Assertions.Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("data-bx-host", "webview");
    }

    [Fact]
    public async Task Sidebar_state_is_kept_in_localStorage_not_in_a_cookie_in_a_web_view()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "sidebar");
        var sidebar = page.GetByTestId("sidebar");

        await sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" }).ClickAsync();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await page.WaitForFunctionAsync("() => localStorage.getItem('blux-sidebar-playground-sidebar') === '1'");
        Assert.DoesNotContain(await page.Context.CookiesAsync(), c => c.Name.StartsWith("blux-sidebar-", StringComparison.Ordinal));

        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-sidebar')");
        await Assertions.Expect(page.GetByTestId("sidebar")).ToHaveAttributeAsync("data-state", "collapsed");
    }

    [Fact]
    public async Task Locale_select_neither_writes_the_ASP_NET_cookie_nor_reloads_in_a_web_view()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "locale-select");
        await page.EvaluateAsync("() => { window.__marker = 'same page'; }");

        await page.GetByTestId("locale-select").GetByRole(AriaRole.Combobox).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(await page.Context.CookiesAsync(), c => c.Name == ".AspNetCore.Culture");
        Assert.Equal("same page", await page.EvaluateAsync<string>("() => window.__marker"));
    }

    [Fact]
    public async Task Server_culture_is_ignored_in_a_web_view()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "button");

        var source = await page.EvaluateAsync<string?[]>("() => Blux.serverCulture()");

        Assert.Equal([null, null, null], source);
    }

    [Fact]
    public async Task Stored_culture_round_trips_through_localStorage()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "button");

        await page.EvaluateAsync("() => Blux.setStoredCulture('blux-culture', 'pt-BR')");

        Assert.Equal("pt-BR", await page.EvaluateAsync<string>("() => Blux.getStoredCulture('blux-culture')"));
    }

    [Fact]
    public async Task Tooltip_ignores_a_tap_and_opens_on_a_long_press()
    {
        var page = await NewWebViewPage(touch: true);
        await Open(page, "server", "tooltip");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "top", Exact = true });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Tooltip on the top" });

        await trigger.TapAsync();
        await Task.Delay(800, TestContext.Current.CancellationToken);
        await Assertions.Expect(tooltip).ToBeHiddenAsync();

        if (BrowserName != "chromium") return;
        var box = await trigger.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchStart",
            ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
        });
        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchEnd",
            ["touchPoints"] = Array.Empty<object>(),
        });
        // It lingers a moment after the finger lifts, then goes.
        await Assertions.Expect(tooltip).ToBeHiddenAsync(new() { Timeout = 6000 });
    }

    [Fact]
    public async Task Tooltip_still_opens_on_mouse_hover()
    {
        await Open("server", "tooltip");

        await Page.GetByRole(AriaRole.Button, new() { Name = "top", Exact = true }).HoverAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Tooltip, new() { Name = "Tooltip on the top" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Spatial_navigation_moves_focus_with_the_arrow_keys_in_tv_mode()
    {
        await Open("server", "tooltip");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("data-bx-tv", "");
        var buttons = Page.Locator("main button", new() { HasTextRegex = new Regex("^(top|right|bottom|left)$") });
        await buttons.First.FocusAsync();
        var first = await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()");

        await Page.Keyboard.PressAsync("ArrowRight");
        var second = await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()");
        await Page.Keyboard.PressAsync("ArrowLeft");
        var back = await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()");

        Assert.Equal("top", first);
        Assert.NotEqual(first, second);
        Assert.Equal(first, back);
    }

    [Fact]
    public async Task Forwarded_remote_keys_move_focus_and_Enter_activates()
    {
        await Open("server", "toast");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("toast-trigger").FocusAsync();

        // The Android TV activity sends the keys this way (untrusted events: no default action).
        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvKey('ArrowRight')"));
        Assert.Equal("Show error", await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()"));
        await Page.EvaluateAsync("() => Blux.tvKey('Enter')");

        await Assertions.Expect(Page.GetByText("Upload failed")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Back_closes_the_topmost_overlay_and_reports_when_nothing_is_left()
    {
        await Open("server", "modal");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        Assert.False(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));

        await Page.GetByTestId("modal-trigger").ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Header_mobile_menu_opens_with_a_tap_in_a_web_view()
    {
        var context = await NewContext(new() { HasTouch = true, IsMobile = true, ViewportSize = new() { Width = 390, Height = 800 } });
        await context.AddInitScriptAsync(Bridge);
        var page = await context.NewPageAsync();
        await Open(page, "server", "header");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Site menu" });

        await page.GetByTestId("header").GetByRole(AriaRole.Button, new() { Name = "Open menu" }).TapAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await page.EvaluateAsync("() => Blux.tvBack()");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Coarse_pointers_get_targets_of_at_least_44_pixels()
    {
        var context = await NewContext(new() { HasTouch = true, IsMobile = true, ViewportSize = new() { Width = 390, Height = 800 } });
        var page = await context.NewPageAsync();
        await Open(page, "server", "button");

        var heights = await page.EvaluateAsync<double[]>("() => [...document.querySelectorAll('main button')].filter((b) => b.getClientRects().length).map((b) => b.getBoundingClientRect().height)");

        Assert.NotEmpty(heights);
        Assert.All(heights, h => Assert.True(h >= 43.5, $"button of {h}px"));
    }
}
