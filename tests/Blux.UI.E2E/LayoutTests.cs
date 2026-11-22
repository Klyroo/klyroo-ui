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
        await ExpectCookie("blux-sidebar-playground-sidebar", "1");
        // Collapsed to icons, the links keep their names for assistive technology.
        await Assertions.Expect(sidebar.GetByRole(AriaRole.Link, new() { Name = "Inbox" })).ToBeVisibleAsync();

        await Reload(mode);

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");

        await toggle.ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await Assertions.Expect(sidebar).ToHaveCSSAsync("width", "256px");
        await ExpectCookie("blux-sidebar-playground-sidebar", "0");
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
    public async Task Sidebar_remembered_state_is_painted_before_any_script_module_runs()
    {
        // Without blazor.web.js nothing but the inline first-paint script can apply the cookie.
        await Page.Context.AddCookiesAsync([new Cookie { Name = "blux-sidebar-playground-sidebar", Value = "1", Url = BaseUrl }]);
        await Page.RouteAsync("**/_framework/blazor.web*.js", route => route.AbortAsync());

        await Page.GotoAsync("/ssr/components/sidebar");

        var sidebar = Page.GetByTestId("sidebar");
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await Assertions.Expect(sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" })).ToHaveAttributeAsync("aria-pressed", "true");
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
        await ExpectCookie("blux-sidebar-playground-sidebar-resizable-width", "208");

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

    // The sidebar with real components in it (a data list search focused by id, a radio group with a Name, an input): the content exists once.
    private ILocator ContentDemo => Page.GetByTestId("sidebar-content-demo");

    private async Task OpenContentDemo(string mode)
    {
        await Open(mode, "sidebar");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-sidebar')");
        await ContentDemo.ScrollIntoViewIfNeededAsync();
        // The panel is wired by the browser once the element is set up; the search is a component that needs the interactive modes to be ready.
        await Assertions.Expect(ContentDemo.Locator("[data-testid=sidebar-content]")).ToBeAttachedAsync();
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
    }

    private async Task<bool> IsPanelOpen() =>
        await ContentDemo.Locator("[data-testid=sidebar-content]").EvaluateAsync<bool>("e => e.matches(':popover-open')");

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_content_exists_once_with_every_id_name_and_test_id_unique(string mode)
    {
        await OpenContentDemo(mode);

        var duplicates = await Page.EvaluateAsync<string[]>(
            "() => { const seen = new Set(), dup = new Set(); for (const e of document.querySelectorAll('[id]')) { if (seen.has(e.id)) dup.add(e.id); seen.add(e.id); } return [...dup]; }");
        Assert.Empty(duplicates);
        var testIds = await ContentDemo.EvaluateAsync<string[]>(
            "demo => { const seen = new Set(), dup = new Set(); for (const e of demo.querySelectorAll('[data-testid]')) { const v = e.getAttribute('data-testid'); if (seen.has(v)) dup.add(v); seen.add(v); } return [...dup]; }");
        Assert.Empty(testIds);
        Assert.Equal(2, await ContentDemo.Locator("input[name=sidebar-content-sort]").CountAsync());
        Assert.Equal(1, await ContentDemo.Locator("input[name=sidebar-content-note]").CountAsync());
        Assert.Equal(1, await ContentDemo.Locator("input[type=search], [data-testid=sidebar-content-search] input, input[aria-label='Search invoices']").CountAsync());
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_panel_is_a_modal_that_keeps_the_focus_and_gives_it_back(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        var aside = ContentDemo.Locator("[data-testid=sidebar-content]");
        var open = ContentDemo.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" });
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Sidebar" });
        await Assertions.Expect(aside).ToBeHiddenAsync();

        await open.ClickAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.Equal("true", await dialog.GetAttributeAsync("aria-modal"));
        // The page behind cannot be reached: its trigger is inert, and the focus went into the panel.
        Assert.True(await open.EvaluateAsync<bool>("e => !!e.closest('[inert]')"));
        Assert.True(await aside.EvaluateAsync<bool>("e => e.contains(document.activeElement)"));
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Close" })).ToBeFocusedAsync();
        // Tab, and Shift+Tab, never leave the panel.
        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.True(await aside.EvaluateAsync<bool>("e => e.contains(document.activeElement)"), $"Tab {i}");
        }
        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Shift+Tab");
            Assert.True(await aside.EvaluateAsync<bool>("e => e.contains(document.activeElement)"), $"Shift+Tab {i}");
        }
        // The search is a data list component focused by id: it takes typing in the panel.
        var search = dialog.GetByRole(AriaRole.Searchbox, new() { Name = "Search invoices" });
        await search.FillAsync("Fox");
        await Assertions.Expect(search).ToHaveValueAsync("Fox");

        // Escape in a search field clears it first; the next one closes the panel.
        await Page.Keyboard.PressAsync("Escape");
        if (await IsPanelOpen()) await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(aside).ToBeHiddenAsync();
        Assert.False(await open.EvaluateAsync<bool>("e => !!e.closest('[inert]')"));
        await Assertions.Expect(open).ToBeFocusedAsync();

        // The backdrop closes it, and so does the close button.
        await open.ClickAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Page.Mouse.ClickAsync(375, 700);
        await Assertions.Expect(aside).ToBeHiddenAsync();
        await Assertions.Expect(open).ToBeFocusedAsync();
        await open.ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Assertions.Expect(aside).ToBeHiddenAsync();
        await Assertions.Expect(open).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_panel_closes_when_the_screen_grows_past_lg_and_is_a_column_there(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        var aside = ContentDemo.Locator("[data-testid=sidebar-content]");
        var open = ContentDemo.Locator("button[data-bx-sidebar-open]");
        await open.ClickAsync();
        Assert.True(await IsPanelOpen());

        await Page.SetViewportSizeAsync(1280, 800);

        await Assertions.Expect(aside).ToBeVisibleAsync();
        Assert.False(await IsPanelOpen());
        Assert.False(await open.EvaluateAsync<bool>("e => !!e.closest('[inert]')"));
        Assert.Null(await aside.GetAttributeAsync("role"));
        Assert.Equal(0, await ContentDemo.Locator("[inert]").CountAsync());
        Assert.Equal("relative", await aside.EvaluateAsync<string>("e => getComputedStyle(e).position"));
        // The title and the close button of the panel exist only below lg.
        await Assertions.Expect(aside.GetByRole(AriaRole.Button, new() { Name = "Close" })).ToBeHiddenAsync();
        await Assertions.Expect(open).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_panel_has_no_accessibility_violations_open_in_light_and_dark(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await ContentDemo.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Sidebar" })).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();

        await Page.EvaluateAsync("() => document.documentElement.classList.add('dark')");
        await AssertNoAccessibilityViolations();
    }

    private async Task OpenPanel()
    {
        await ContentDemo.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Sidebar" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_panel_keeps_the_landmark_and_puts_the_dialog_role_on_its_inner_panel(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        var aside = ContentDemo.Locator("[data-testid=sidebar-content]");
        var panel = aside.Locator("> [data-slot=panel]");
        Assert.Null(await panel.GetAttributeAsync("role"));

        await OpenPanel();

        // The aside is never given a dialog role (it is not allowed on it); the inner element is the dialog, named by the title.
        Assert.Null(await aside.GetAttributeAsync("role"));
        Assert.Equal("dialog", await panel.GetAttributeAsync("role"));
        Assert.Equal("true", await panel.GetAttributeAsync("aria-modal"));
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(aside).ToBeHiddenAsync();
        Assert.Null(await panel.GetAttributeAsync("role"));
        Assert.Null(await panel.GetAttributeAsync("aria-modal"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_panel_does_not_slide_under_reduced_motion(string mode)
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();

        var aside = ContentDemo.Locator("[data-testid=sidebar-content]");
        Assert.Equal("none", await aside.EvaluateAsync<string>("e => getComputedStyle(e).transitionProperty"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Sidebar_panel_slides_in_without_reduced_motion(string mode)
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();

        var aside = ContentDemo.Locator("[data-testid=sidebar-content]");
        Assert.Equal("translate", await aside.EvaluateAsync<string>("e => getComputedStyle(e).transitionProperty"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_modal_declared_inside_the_panel_keeps_Tab_and_the_panel_open(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();
        var inner = Page.GetByRole(AriaRole.Dialog, new() { Name = "Inside the sidebar" });

        await Page.GetByTestId("sidebar-inner-open").ClickAsync();

        await Assertions.Expect(inner).ToBeVisibleAsync();
        // A native modal dialog lets Tab leave the document at its ends (the browser's own controls): what matters is that Tab moves through the dialog's controls
        // and never lands on the panel behind it, whose close button is inert.
        var visited = new HashSet<string>();
        foreach (var key in new[] { "Tab", "Tab", "Tab", "Tab", "Shift+Tab", "Shift+Tab", "Shift+Tab", "Shift+Tab" })
        {
            await Page.Keyboard.PressAsync(key);
            var where = await inner.EvaluateAsync<string>("e => document.activeElement === document.body ? 'body' : e.contains(document.activeElement) ? 'dialog:' + (document.activeElement.getAttribute('data-testid') ?? document.activeElement.getAttribute('aria-label') ?? document.activeElement.localName) : 'outside:' + document.activeElement.outerHTML.slice(0, 60)");
            Assert.False(where.StartsWith("outside:", StringComparison.Ordinal), $"{key} landed outside the modal: {where}");
            visited.Add(where);
        }
        Assert.True(visited.Count(v => v.StartsWith("dialog:", StringComparison.Ordinal)) >= 2, "Tab reached the controls of the modal: " + string.Join(", ", visited));
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(inner).ToBeHiddenAsync();
        Assert.True(await IsPanelOpen());
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_modal_declared_outside_the_panel_closes_it_first_and_gives_the_focus_back_to_the_button(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();
        var outer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Outside the sidebar" });

        await Page.GetByTestId("sidebar-outer-open").ClickAsync();

        await Assertions.Expect(outer).ToBeVisibleAsync();
        Assert.False(await IsPanelOpen());
        // The modal is usable (the page was inert while the panel was open).
        await outer.GetByRole(AriaRole.Textbox, new() { Name = "Outer field" }).FillAsync("hello");
        await Assertions.Expect(outer.GetByRole(AriaRole.Textbox, new() { Name = "Outer field" })).ToHaveValueAsync("hello");
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(outer).ToBeHiddenAsync();
        await Assertions.Expect(ContentDemo.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" })).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_form_submitted_in_the_panel_closes_it(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();
        // Keep the page where it is, so what closes the panel is the submit itself.
        await Page.EvaluateAsync("() => document.querySelector('[data-testid=sidebar-content-form]').addEventListener('submit', (e) => e.preventDefault())");

        await Page.GetByTestId("sidebar-content-form-submit").ClickAsync();

        Assert.False(await IsPanelOpen());
        Assert.Equal(0, await ContentDemo.Locator("[inert]").CountAsync());
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_panel_closes_on_a_navigation_to_another_path_and_stays_for_a_change_of_the_query(string mode)
    {
        Assert.SkipUnless(BrowserName == "chromium", "The Navigation API is not in every engine.");
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();
        var path = new Uri(Page.Url).AbsolutePath;

        await Page.EvaluateAsync("p => history.replaceState({}, '', p + '?same=1')", path);
        await Page.EvaluateAsync("p => history.pushState({}, '', p + '?other=1')", path);
        Assert.True(await IsPanelOpen());

        await Page.EvaluateAsync("() => history.pushState({}, '', '/elsewhere-in-the-site')");
        Assert.False(await IsPanelOpen());
        Assert.Equal(0, await ContentDemo.Locator("[inert]").CountAsync());
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_merge_that_strips_the_modal_state_is_repaired_by_refresh(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await OpenContentDemo(mode);
        await OpenPanel();
        var panel = ContentDemo.Locator("[data-testid=sidebar-content] > [data-slot=panel]");
        // What an enhanced navigation does to a page it merges: the attributes the browser side set are gone, the open popover is not.
        await Page.EvaluateAsync("""
            () => {
                document.querySelectorAll('[inert]').forEach((e) => e.removeAttribute('inert'));
                const panel = document.querySelector('[data-testid=sidebar-content] > [data-slot=panel]');
                ['role', 'aria-modal', 'aria-labelledby'].forEach((a) => panel.removeAttribute(a));
            }
            """);
        Assert.Null(await panel.GetAttributeAsync("role"));

        await Page.EvaluateAsync("() => document.querySelector('[data-testid=sidebar-content-demo] bx-sidebar').refresh()");

        Assert.Equal("dialog", await panel.GetAttributeAsync("role"));
        Assert.True(await ContentDemo.Locator("bx-sidebar > div:not([data-slot])").EvaluateAsync<bool>("e => e.inert"));
    }

    [Fact]
    public async Task The_dashboard_toggle_opens_the_panel_with_no_script_at_all()
    {
        var context = await NewContext(new() { JavaScriptEnabled = false, ViewportSize = new() { Width = 390, Height = 800 } });
        var page = await context.NewPageAsync();
        await page.GotoAsync("/ssr/components/dashboard-sidebar");
        var toggle = page.Locator("button[data-bx-sidebar-open]").First;
        var target = await toggle.GetAttributeAsync("popovertarget");
        Assert.False(string.IsNullOrEmpty(target));
        var aside = page.Locator($"[id='{target}']");
        Assert.Equal(1, await aside.CountAsync());
        Assert.False(await aside.EvaluateAsync<bool>("e => e.matches(':popover-open')"));

        await toggle.ClickAsync();

        Assert.True(await aside.EvaluateAsync<bool>("e => e.matches(':popover-open')"));
        await Assertions.Expect(aside).ToBeVisibleAsync();
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
        // The server reads the cookie, and the WebAssembly client reads it too (ApplyServerCultureAsync): every mode shows the new culture.
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(Page.GetByTestId("locale-current")).ToHaveTextAsync("fr");
        await Assertions.Expect(Page.GetByTestId("locale-resolved")).ToHaveTextAsync("fr");
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
        await Assertions.Expect(error.GetByRole(AriaRole.Link, new() { Name = "Back to home" })).ToHaveAttributeAsync("href", "./");
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
