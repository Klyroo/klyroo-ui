using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

public class LayoutTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "app", "header", "footer", "footer-columns", "main", "sidebar", "error", "locale-select", "theme",
    ];

    public static TheoryData<string, string> EverySlugInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var slug in Slugs)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(slug, mode);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EverySlugInEveryMode))]
    public async Task Layout_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    /// <summary>Runs an enhanced navigation to the same page and waits for Blazor to merge the response.</summary>
    private async Task EnhancedReload()
    {
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");
    }

    private async Task<string?> SidebarCookie(string name) =>
        (await Page.Context.CookiesAsync()).FirstOrDefault(c => c.Name == name)?.Value;

    private async Task Reload(string mode)
    {
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-sidebar')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
    }

    // ---- Header

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Header_mobile_menu_opens_and_closes(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Open(mode, "header");
        var header = Page.GetByTestId("header");
        var open = header.GetByRole(AriaRole.Button, new() { Name = "Open menu" });
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Site menu" });
        // Below lg the links give way to the button.
        await Assertions.Expect(header.GetByRole(AriaRole.Navigation, new() { Name = "Site" })).ToBeHiddenAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();

        await open.ClickAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(open).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Docs" })).ToBeVisibleAsync();

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(open).ToHaveAttributeAsync("aria-expanded", "false");

        await open.ClickAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(open).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Header_menu_closes_when_a_link_is_followed_and_when_the_screen_grows(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Open(mode, "header");
        var open = Page.GetByTestId("header").GetByRole(AriaRole.Button, new() { Name = "Open menu" });
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Site menu" });
        // Keep the page where it is so the test sees the panel closing, not the page being replaced.
        await Page.EvaluateAsync("() => document.addEventListener('click', (e) => { if (e.target.closest('a')) e.preventDefault(); }, true)");

        await open.ClickAsync();
        await dialog.GetByRole(AriaRole.Link, new() { Name = "Docs" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();

        await open.ClickAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Page.SetViewportSizeAsync(1280, 800);
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        // A closed dialog is hidden from the role query: reach it by tag.
        Assert.False(await Page.GetByTestId("header").Locator("dialog").EvaluateAsync<bool>("d => d.open"));
        // The links are back in the header.
        await Assertions.Expect(Page.GetByTestId("header").GetByRole(AriaRole.Navigation, new() { Name = "Site" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Header_shows_its_links_and_no_menu_button_on_wide_screens(string mode)
    {
        await Open(mode, "header");
        var header = Page.GetByTestId("header");

        await Assertions.Expect(header.GetByRole(AriaRole.Navigation, new() { Name = "Site" })).ToBeVisibleAsync();
        await Assertions.Expect(header.GetByRole(AriaRole.Button, new() { Name = "Open menu" })).ToBeHiddenAsync();
    }

    // ---- Sidebar

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_collapse_is_remembered_across_a_reload(string mode)
    {
        await Open(mode, "sidebar");
        var sidebar = Page.GetByTestId("sidebar");
        var toggle = sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" });
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");

        await toggle.ClickAsync();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "64px");
        Assert.Equal("1", await SidebarCookie("blux-sidebar-playground-sidebar"));
        // Collapsed to icons, the links keep their names for assistive technology.
        await Assertions.Expect(sidebar.GetByRole(AriaRole.Link, new() { Name = "Inbox" })).ToBeVisibleAsync();

        await Reload(mode);

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");

        await toggle.ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "256px");
        Assert.Equal("0", await SidebarCookie("blux-sidebar-playground-sidebar"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_remembered_state_is_applied_on_load_without_animating(string mode)
    {
        // The browser remembers the state in a cookie that the server can read too.
        await Page.Context.AddCookiesAsync([new Cookie { Name = "blux-sidebar-playground-sidebar", Value = "1", Url = BaseUrl }]);
        await Page.Context.AddInitScriptAsync(
            "window.__widthTransitions = 0; document.addEventListener('transitionrun', (e) => { if (e.propertyName === 'width' && e.target.dataset?.testid === 'sidebar') window.__widthTransitions++; }, true);");

        await Open(mode, "sidebar");
        var sidebar = Page.GetByTestId("sidebar");

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "64px");
        // Longer than the width transition (200 ms): it would have started by now.
        await Page.WaitForTimeoutAsync(400);
        Assert.Equal(0, await Page.EvaluateAsync<int>("() => window.__widthTransitions"));
        // Once initialized, a user's toggle does animate.
        await sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" }).ClickAsync();
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "256px");
        Assert.True(await Page.EvaluateAsync<int>("() => window.__widthTransitions") > 0);
    }

    [Fact]
    public async Task Sidebar_keeps_its_collapsed_state_after_an_enhanced_navigation()
    {
        await Open("ssr", "sidebar");
        var sidebar = Page.GetByTestId("sidebar");
        var toggle = sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" });
        await toggle.ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");

        await EnhancedReload();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "64px");
        // Still alive: it expands again.
        await toggle.ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_bound_state_is_reported_to_dotnet_and_set_from_it(string mode)
    {
        await Open(mode, "sidebar");
        var sidebar = Page.GetByTestId("sidebar-bound");
        var toggle = sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" });

        await toggle.ClickAsync();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        if (mode == "ssr") return;

        await Assertions.Expect(Page.GetByTestId("sidebar-state")).ToHaveTextAsync("Collapsed: true");
        await Page.GetByTestId("sidebar-bound-toggle").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("sidebar-state")).ToHaveTextAsync("Collapsed: false");
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");

        await toggle.ClickAsync();
        await Assertions.Expect(Page.GetByTestId("sidebar-state")).ToHaveTextAsync("Collapsed: true");
        await Page.GetByTestId("sidebar-bound-toggle").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("sidebar-state")).ToHaveTextAsync("Collapsed: false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_resizes_with_the_keyboard_and_remembers_its_width(string mode)
    {
        await Open(mode, "sidebar");
        var sidebar = Page.GetByTestId("sidebar-resizable");
        var handle = sidebar.GetByRole(AriaRole.Separator, new() { Name = "Resize sidebar" });
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "224");

        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "240");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "240px");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "448");
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "192px");
        await Page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("208", await SidebarCookie("blux-sidebar-playground-sidebar-resizable-width"));

        await Reload(mode);

        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "208px");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "208");
    }

    [Fact]
    public async Task Sidebar_resizes_by_dragging_its_edge()
    {
        await Open("ssr", "sidebar");
        var sidebar = Page.GetByTestId("sidebar-resizable");
        var handle = sidebar.GetByRole(AriaRole.Separator, new() { Name = "Resize sidebar" });
        await handle.ScrollIntoViewIfNeededAsync();
        var box = (await handle.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 + 60, box.Y + box.Height / 2, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        var width = await sidebar.EvaluateAsync<double>("e => e.getBoundingClientRect().width");
        Assert.InRange(width, 270, 300);
        Assert.Equal(width.ToString("0"), await handle.GetAttributeAsync("aria-valuenow"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_becomes_an_off_canvas_panel_on_small_screens(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Open(mode, "sidebar");
        var host = Page.Locator("bx-sidebar").First;
        var open = host.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" });
        var dialog = host.GetByRole(AriaRole.Dialog, new() { Name = "Sidebar" });
        await Assertions.Expect(Page.GetByTestId("sidebar")).ToBeHiddenAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();

        await open.ClickAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        // Never collapsed: the labels are visible in the panel.
        await Assertions.Expect(dialog.GetByText("Inbox")).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    // ---- LocaleSelect

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task LocaleSelect_writes_the_culture_cookie_and_reloads(string mode)
    {
        await Open(mode, "locale-select");
        var select = Page.GetByTestId("locale-select");
        await Page.EvaluateAsync("() => { window.__beforeReload = true; }");

        await select.GetByRole(AriaRole.Combobox, new() { Name = "Language" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();

        await Page.WaitForFunctionAsync("() => window.__beforeReload === undefined");
        var cookie = (await Page.Context.CookiesAsync()).Single(c => c.Name == ".AspNetCore.Culture");
        Assert.Equal("c=fr|uic=fr", Uri.UnescapeDataString(cookie.Value));
        Assert.Equal("/", cookie.Path);
        // The server reads the cookie: statically rendered and server interactive pages show the new culture.
        if (mode is "ssr" or "server")
        {
            await Assertions.Expect(Page.GetByTestId("locale-current")).ToHaveTextAsync("fr");
        }
    }

    // ---- Theme

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Theme_restyles_the_components_below_it_and_instances_still_win(string mode)
    {
        await Open(mode, "theme");

        Assert.Equal("500", await Page.GetByTestId("theme-default").EvaluateAsync<string>("e => getComputedStyle(e).fontWeight"));
        Assert.Equal("800", await Page.GetByTestId("theme-button").EvaluateAsync<string>("e => getComputedStyle(e).fontWeight"));
        Assert.Equal("400", await Page.GetByTestId("theme-instance").EvaluateAsync<string>("e => getComputedStyle(e).fontWeight"));
        Assert.Equal("uppercase", await Page.GetByTestId("theme-button").EvaluateAsync<string>("e => getComputedStyle(e).textTransform"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Theme_nested_scopes_apply_the_innermost_override(string mode)
    {
        await Open(mode, "theme");
        var outer = Page.GetByTestId("theme-outer").GetByRole(AriaRole.Button, new() { Name = "Outer" });
        var inner = Page.GetByTestId("theme-inner");

        Assert.NotEqual("0px", await outer.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius"));
        Assert.Equal("0px", await inner.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius"));
    }

    // ---- Error, Footer, Main

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Error_shows_the_status_and_a_way_back_home(string mode)
    {
        await Open(mode, "error");
        var error = Page.GetByTestId("error");

        await Assertions.Expect(error.GetByRole(AriaRole.Heading, new() { Name = "Page not found" })).ToBeVisibleAsync();
        await Assertions.Expect(error.GetByText("404", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(error.GetByRole(AriaRole.Link, new() { Name = "Back to home" })).ToHaveAttributeAsync("href", "/");
        var boundary = Page.GetByTestId("error-boundary");
        await Assertions.Expect(boundary.GetByText("The report could not be generated.")).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Footer_columns_are_a_named_navigation_of_link_groups(string mode)
    {
        await Open(mode, "footer-columns");
        var nav = Page.GetByTestId("footer-columns").GetByRole(AriaRole.Navigation, new() { Name = "Sitemap" });

        await Assertions.Expect(nav.GetByRole(AriaRole.Heading, new() { Name = "Product" })).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Features" })).ToHaveAttributeAsync("href", "/components/header");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Blog" })).ToHaveAttributeAsync("rel", new Regex("noopener"));
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Careers" })).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Main_keeps_its_minimum_height_override_inside_the_frame(string mode)
    {
        await Open(mode, "main");
        var main = Page.GetByTestId("main");

        var height = await main.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        Assert.InRange(height, 192, 400);
    }
}
