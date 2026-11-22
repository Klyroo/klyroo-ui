using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>The "Dashboard" family: DashboardGroup, Sidebar (and its collapse and toggle buttons), Panel, ResizeHandle, Navbar, Toolbar, Search and SearchButton.</summary>
public class DashboardTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "dashboard-group", "dashboard-sidebar", "dashboard-sidebar-collapse", "dashboard-sidebar-toggle", "dashboard-panel",
        "dashboard-resize-handle", "dashboard-navbar", "dashboard-toolbar", "dashboard-search", "dashboard-search-button",
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
    public async Task Dashboard_page_has_no_accessibility_violations(string slug, string mode)
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

    private async Task Reload(string mode)
    {
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-dashboard-group')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
    }

    private Task<string?> Stored(string key) => Page.EvaluateAsync<string?>("k => localStorage.getItem(k)", key);


    // ---- Sidebar collapse

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Collapse_button_collapses_the_sidebar_and_it_stays_collapsed_across_a_reload(string mode)
    {
        await Open(mode, "dashboard-group");
        var sidebar = Page.GetByTestId("dashboard-sidebar");
        var collapse = Page.GetByTestId("dashboard-collapse");
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await Assertions.Expect(collapse).ToHaveAttributeAsync("aria-expanded", "true");
        var controls = await collapse.GetAttributeAsync("aria-controls");
        Assert.Equal(await sidebar.GetAttributeAsync("id"), controls);

        await collapse.ClickAsync();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(collapse).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "64px");
        await ExpectCookie("blux-sidebar-playground-dashboard", "1");
        // Collapsed to icons, the links and the search button keep their names.
        await Assertions.Expect(sidebar.GetByRole(AriaRole.Link, new() { Name = "Inbox" })).ToBeVisibleAsync();
        await Assertions.Expect(sidebar.GetByRole(AriaRole.Button, new() { Name = "Search" })).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();

        await Reload(mode);

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(collapse).ToHaveAttributeAsync("aria-expanded", "false");

        await collapse.ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await Assertions.Expect(collapse).ToHaveAttributeAsync("aria-expanded", "true");
        await ExpectCookie("blux-sidebar-playground-dashboard", "0");
    }

    [Fact]
    public async Task Collapsed_sidebar_and_panel_sizes_survive_an_enhanced_navigation()
    {
        await Open("ssr", "dashboard-group");
        var sidebar = Page.GetByTestId("dashboard-sidebar");
        var collapse = Page.GetByTestId("dashboard-collapse");
        var handle = Page.GetByRole(AriaRole.Separator, new() { Name = "Resize inbox" });
        await collapse.ClickAsync();
        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "40");

        await EnhancedReload();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(collapse).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "64px");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "40");
        await Assertions.Expect(Page.GetByTestId("dashboard-inbox")).ToHaveCSSAsync("flex-grow", "40");
        // Still alive: it expands again.
        await collapse.ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_resizes_with_the_keyboard_and_remembers_its_width(string mode)
    {
        await Open(mode, "dashboard-sidebar-collapse");
        var sidebar = Page.GetByTestId("dashboard-sidebar-only");
        var handle = sidebar.GetByRole(AriaRole.Separator, new() { Name = "Resize sidebar" });
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "224");

        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "240px");
        await ExpectCookie("blux-sidebar-playground-dashboard-sidebar-width", "240");

        await Reload(mode);

        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "240px");
    }

    // ---- Mobile

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Toggle_opens_the_sidebar_as_a_panel_on_small_screens(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Open(mode, "dashboard-sidebar-toggle");
        var frame = Page.GetByTestId("dashboard-sidebar-frame");
        var toggle = frame.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" });
        var dialog = frame.GetByRole(AriaRole.Dialog, new() { Name = "Workspace" });
        await Assertions.Expect(Page.GetByTestId("dashboard-sidebar-only")).ToBeHiddenAsync();
        await Assertions.Expect(frame.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" })).ToBeHiddenAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(dialog).ToBeHiddenAsync();

        await toggle.ClickAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        Assert.Equal(await Page.GetByTestId("dashboard-sidebar-only").GetAttributeAsync("id"), await toggle.GetAttributeAsync("aria-controls"));
        // Never collapsed: the labels are visible in the panel.
        await Assertions.Expect(dialog.GetByText("Messages")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(toggle).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Panel_closes_when_the_screen_grows_and_the_collapse_button_returns()
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Open("ssr", "dashboard-sidebar-toggle");
        var frame = Page.GetByTestId("dashboard-sidebar-frame");
        var dialog = frame.GetByRole(AriaRole.Dialog, new() { Name = "Workspace" });
        await frame.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();

        await Page.SetViewportSizeAsync(1280, 800);

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(frame.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" })).ToBeVisibleAsync();
        await Assertions.Expect(frame.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" })).ToBeHiddenAsync();
    }

    // ---- Panels

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Panels_resize_with_the_keyboard_within_their_limits_and_remember_their_sizes(string mode)
    {
        await Open(mode, "dashboard-panel");
        var list = Page.GetByRole(AriaRole.Separator, new() { Name = "Resize list" });
        var detail = Page.GetByRole(AriaRole.Separator, new() { Name = "Resize detail" });
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuenow", "25");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuemin", "15");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuemax", "40");

        await list.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuenow", "30");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuenow", "40");
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuenow", "15");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuenow", "20");
        // The neighbour took the difference, the last panel kept its share.
        await Assertions.Expect(detail).ToHaveAttributeAsync("aria-valuenow", "55");
        await Assertions.Expect(Page.GetByTestId("dashboard-panel-list")).ToHaveCSSAsync("flex-grow", "20");
        await Assertions.Expect(Page.GetByTestId("dashboard-panel-detail")).ToHaveCSSAsync("flex-grow", "55");
        Assert.Equal("20,55,25", await Stored("blux:dashboard:playground-dashboard-panels:panels"));

        // The second handle stops at the minimum of the panel it resizes.
        await detail.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(detail).ToHaveAttributeAsync("aria-valuenow", "30");

        await Reload(mode);

        await Assertions.Expect(list).ToHaveAttributeAsync("aria-valuenow", "20");
        await Assertions.Expect(detail).ToHaveAttributeAsync("aria-valuenow", "30");
    }

    [Fact]
    public async Task Panels_resize_by_dragging_a_handle()
    {
        await Open("ssr", "dashboard-panel");
        var handle = Page.GetByRole(AriaRole.Separator, new() { Name = "Resize list" });
        await handle.ScrollIntoViewIfNeededAsync();
        var box = (await handle.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 + 60, box.Y + box.Height / 2, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        var value = int.Parse((await handle.GetAttributeAsync("aria-valuenow"))!);
        Assert.InRange(value, 26, 40);
        Assert.NotNull(await Stored("blux:dashboard:playground-dashboard-panels:panels"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Standalone_handle_resizes_the_panel_before_it(string mode)
    {
        await Open(mode, "dashboard-resize-handle");
        var handle = Page.GetByTestId("dashboard-handle");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "50");

        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "45");
        await Assertions.Expect(Page.GetByTestId("dashboard-handle-left")).ToHaveCSSAsync("flex-grow", "45");
        await Assertions.Expect(Page.GetByTestId("dashboard-handle-right")).ToHaveCSSAsync("flex-grow", "55");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_sizes_and_collapsed_state_are_reported_to_dotnet_and_set_from_it(string mode)
    {
        await Open(mode, "dashboard-group");
        var frame = Page.GetByTestId("dashboard-bound");
        var handle = frame.GetByRole(AriaRole.Separator, new() { Name = "Resize list" });
        var sizes = Page.GetByTestId("dashboard-bound-sizes");
        var collapsed = Page.GetByTestId("dashboard-bound-collapsed");
        await Assertions.Expect(sizes).ToHaveTextAsync("Sizes: 40,60");

        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Assertions.Expect(sizes).ToHaveTextAsync("Sizes: 45,55");
        await Page.GetByTestId("dashboard-bound-set").ClickAsync();
        await Assertions.Expect(sizes).ToHaveTextAsync("Sizes: 25,75");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "25");
        await Assertions.Expect(Page.GetByTestId("dashboard-bound").Locator("[data-bx-dash-panel]").First).ToHaveCSSAsync("flex-grow", "25");

        await Page.GetByTestId("dashboard-bound-collapse").ClickAsync();
        await Assertions.Expect(collapsed).ToHaveTextAsync("Collapsed: true");
        await Assertions.Expect(Page.GetByTestId("dashboard-bound-sidebar")).ToHaveAttributeAsync("data-state", "collapsed");
    }

    // ---- Search

    private ILocator SearchDialog => Page.GetByRole(AriaRole.Dialog, new() { Name = "Search" });

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Search_opens_with_the_shortcut_filters_and_navigates_to_a_link(string mode)
    {
        await Open(mode, "dashboard-search");
        await Assertions.Expect(SearchDialog).ToBeHiddenAsync();

        await Page.Keyboard.PressAsync("Control+K");

        await Assertions.Expect(SearchDialog).ToBeVisibleAsync();
        var input = SearchDialog.GetByRole(AriaRole.Combobox);
        await Assertions.Expect(input).ToBeFocusedAsync();
        await input.PressSequentiallyAsync("sett");
        await Assertions.Expect(SearchDialog.GetByRole(AriaRole.Option, new() { Name = "Inbox" })).ToBeHiddenAsync();
        await Assertions.Expect(SearchDialog.GetByRole(AriaRole.Option, new() { Name = "Settings" })).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"\?view=settings"));
        await Assertions.Expect(SearchDialog).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Search_button_opens_the_search_and_focus_returns_to_it(string mode)
    {
        await Open(mode, "dashboard-search-button");
        var button = Page.GetByTestId("dashboard-search-example-button");
        await Assertions.Expect(button.Locator("kbd")).ToHaveCountAsync(2);

        await button.ClickAsync();

        await Assertions.Expect(SearchDialog).ToBeVisibleAsync();
        await Assertions.Expect(SearchDialog.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(SearchDialog).ToBeHiddenAsync();
        await Assertions.Expect(button).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Search_runs_an_action_and_closes(string mode)
    {
        await Open(mode, "dashboard-search");

        await Page.Keyboard.PressAsync("Control+K");
        await SearchDialog.GetByRole(AriaRole.Option, new() { Name = "Compose message" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("dashboard-search-example-last")).ToHaveTextAsync("Last command: Compose message");
        await Assertions.Expect(SearchDialog).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Search_button_of_a_sidebar_opens_the_search_of_its_dashboard_and_shrinks_to_an_icon_when_collapsed()
    {
        await Open("ssr", "dashboard-group");
        var button = Page.GetByTestId("dashboard-search-button").First;

        await button.ClickAsync();
        await Assertions.Expect(SearchDialog).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Page.GetByTestId("dashboard-collapse").ClickAsync();
        await Assertions.Expect(button.Locator("kbd").First).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByTestId("dashboard-sidebar")).ToHaveCSSAsync("width", "64px");
        var width = await button.EvaluateAsync<double>("e => e.getBoundingClientRect().width");
        Assert.InRange(width, 20, 48.5);
    }

    [Fact]
    public async Task Search_button_hint_says_Ctrl_on_other_platforms_and_the_command_key_on_Apple()
    {
        await Open("ssr", "dashboard-search-button");
        var kbd = Page.GetByTestId("dashboard-search-example-button").Locator("kbd[data-bx-mod]");
        Assert.Equal("\"Ctrl\"", await kbd.EvaluateAsync<string>("e => getComputedStyle(e, '::before').content"));
        await Assertions.Expect(Page.GetByTestId("dashboard-search-example-button")).ToHaveAttributeAsync("aria-keyshortcuts", "Control+K Meta+K");

        var apple = await NewApplePage();
        await apple.GotoAsync("/ssr/components/dashboard-search-button");
        await apple.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        var appleKbd = apple.GetByTestId("dashboard-search-example-button").Locator("kbd[data-bx-mod]");

        Assert.Equal("\"⌘\"", await appleKbd.EvaluateAsync<string>("e => getComputedStyle(e, '::before').content"));
    }

    // ---- Review findings (second review of waves C and D)

    private Task<string[]> FirstGroupFlex() =>
        Page.EvaluateAsync<string[]>("() => [...document.querySelector('bx-dashboard-group').querySelectorAll('[data-bx-dash-panel]')].map((p) => p.style.flex)");

    [Fact]
    public async Task Group_takes_the_sizes_of_the_dashboard_it_becomes_after_an_enhanced_navigation()
    {
        await Open("ssr", "dashboard-group");
        Assert.Equal(["35 1 0%", "65 1 0%"], await FirstGroupFlex());

        // Two panels on both pages, so only the id of the group tells that the sizes in memory belong to another dashboard.
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo('/ssr/components/dashboard-resize-handle')");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Page.WaitForURLAsync(new Regex("/dashboard-resize-handle$"));

        Assert.Equal(["50 1 0%", "50 1 0%"], await FirstGroupFlex());
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_group_ignores_a_bx_change_that_a_component_inside_it_reports(string mode)
    {
        await Open(mode, "dashboard-group");
        var sizes = Page.GetByTestId("dashboard-bound-sizes");
        await Assertions.Expect(sizes).ToHaveTextAsync("Sizes: 40,60");

        // Any component inside the group may report a bx-change: its value is not the sizes of the panels.
        await Page.GetByTestId("dashboard-bound-sidebar").EvaluateAsync(
            "e => e.dispatchEvent(new CustomEvent('bx-change', { bubbles: true, detail: { value: '10,90', sequence: 99 } }))");
        await Page.WaitForTimeoutAsync(500);

        await Assertions.Expect(sizes).ToHaveTextAsync("Sizes: 40,60");
        // The real report still arrives.
        await Page.GetByTestId("dashboard-bound-set").ClickAsync();
        await Assertions.Expect(sizes).ToHaveTextAsync("Sizes: 25,75");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Shortcut_of_the_open_search_is_consumed_and_leaves_it_open(string mode)
    {
        await Open(mode, "dashboard-search");
        await Page.EvaluateAsync("""
            () => {
                window.__prevented = [];
                window.addEventListener('keydown', (e) => { if (e.ctrlKey && e.key.toLowerCase() === 'k') window.__prevented.push(e.defaultPrevented); });
            }
            """);
        await Page.Keyboard.PressAsync("Control+K");
        await Assertions.Expect(SearchDialog).ToBeVisibleAsync();

        // The browser's own Ctrl+K (focus the address bar) must not run while the search is open.
        await Page.Keyboard.PressAsync("Control+K");

        Assert.Equal(new[] { true, true }, await Page.EvaluateAsync<bool[]>("() => window.__prevented"));
        await Assertions.Expect(SearchDialog).ToBeVisibleAsync();
    }
}
