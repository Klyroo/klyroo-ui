namespace Blux.UI.E2E;

public class NavTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "breadcrumb", "pagination", "stepper", "timeline", "navigation-menu", "collapsible", "tree", "marquee", "scroll-area", "splitter",
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
    public async Task Navigation_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Tree_moves_with_arrow_keys_and_expands_and_collapses(string mode)
    {
        await Open(mode, "tree");
        var tree = Page.GetByRole(AriaRole.Tree, new() { Name = "Project files" });
        var src = tree.GetByRole(AriaRole.Treeitem, new() { Name = "src", Exact = true });
        var components = tree.GetByRole(AriaRole.Treeitem, new() { Name = "components", Exact = true });
        var tests = tree.GetByRole(AriaRole.Treeitem, new() { Name = "tests", Exact = true });
        await Assertions.Expect(src).ToHaveAttributeAsync("aria-expanded", "true");

        await src.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(components).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "true");
        await Page.Keyboard.PressAsync("ArrowRight");
        var button = tree.GetByRole(AriaRole.Treeitem, new() { Name = "Button.razor" });
        await Assertions.Expect(button).ToBeFocusedAsync();

        // Left goes to the parent, then collapses it; the collapsed children leave the navigation order.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(components).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(button).ToBeHiddenAsync();

        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem, new() { Name = "README.md" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(src).ToBeFocusedAsync();

        // Roving tabindex: one tab stop in the whole tree.
        Assert.Equal(1, await tree.Locator("[role=treeitem][tabindex='0']").CountAsync());
        Assert.Equal("-1", await tests.GetAttributeAsync("tabindex"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Tree_selects_with_Enter_and_click(string mode)
    {
        await Open(mode, "tree");
        var tree = Page.GetByRole(AriaRole.Tree);
        var readme = tree.GetByRole(AriaRole.Treeitem, new() { Name = "README.md" });

        await readme.ClickAsync();

        await Assertions.Expect(readme).ToHaveAttributeAsync("aria-selected", "true");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("tree-state")).ToHaveTextAsync("Selected: README.md");

        await tree.GetByRole(AriaRole.Treeitem, new() { Name = "index.js" }).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(tree.GetByRole(AriaRole.Treeitem, new() { Name = "index.js" })).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(readme).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Collapsible_toggles_in_the_browser(string mode)
    {
        await Open(mode, "collapsible");
        var trigger = Page.GetByTestId("plain").GetByRole(AriaRole.Button, new() { Name = "Release notes" });
        var content = Page.GetByTestId("plain-content");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(content).ToBeHiddenAsync();

        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(content).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await Assertions.Expect(content).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Collapsible_reports_its_state_to_dotnet(string mode)
    {
        await Open(mode, "collapsible");

        await Page.GetByTestId("bound").GetByRole(AriaRole.Button).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("open-state")).ToHaveTextAsync("State: open");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Splitter_resizes_with_the_keyboard(string mode)
    {
        await Open(mode, "splitter");
        var handle = Page.GetByTestId("splitter").GetByRole(AriaRole.Separator);
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "40");
        var panel = Page.GetByTestId("splitter").Locator("[data-bx-panel]").First;
        var before = (await panel.BoundingBoxAsync())!.Width;

        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "45");
        Assert.True((await panel.BoundingBoxAsync())!.Width > before);

        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "20");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "20");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Splitter_reports_sizes_to_dotnet_after_a_drag(string mode)
    {
        await Open(mode, "splitter");
        var handle = Page.GetByTestId("splitter").GetByRole(AriaRole.Separator);
        var box = (await handle.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + 120, box.Y + box.Height / 2, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        var now = int.Parse((await handle.GetAttributeAsync("aria-valuenow"))!);
        Assert.True(now > 40, $"expected the first panel to grow, got {now}");
        await Assertions.Expect(Page.GetByTestId("splitter-state")).ToContainTextAsync($"Sizes: {now} /");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Pagination_buttons_change_the_page(string mode)
    {
        await Open(mode, "pagination");
        var nav = Page.GetByRole(AriaRole.Navigation, new() { Name = "Results" });
        await Assertions.Expect(nav.Locator("[aria-current=page]")).ToHaveTextAsync("5");

        await nav.GetByRole(AriaRole.Button, new() { Name = "Page 6" }).ClickAsync();
        await Assertions.Expect(nav.Locator("[aria-current=page]")).ToHaveTextAsync("6");
        await Assertions.Expect(Page.GetByTestId("page-state")).ToHaveTextAsync("Page: 6");

        await nav.GetByRole(AriaRole.Button, new() { Name = "Next page" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("page-state")).ToHaveTextAsync("Page: 7");

        await nav.GetByRole(AriaRole.Button, new() { Name = "First page" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("page-state")).ToHaveTextAsync("Page: 1");
        await Assertions.Expect(nav.GetByRole(AriaRole.Button, new() { Name = "Previous page" })).ToBeDisabledAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Pagination_links_navigate_and_work_without_interactivity(string mode)
    {
        await Open(mode, "pagination");
        var nav = Page.GetByRole(AriaRole.Navigation, new() { Name = "Archive" });

        await nav.GetByRole(AriaRole.Link, new() { Name = "Page 3" }).ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"\?page=3"));
        await Assertions.Expect(nav.Locator("[aria-current=page]")).ToHaveTextAsync("3");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Stepper_steps_are_selectable_when_bound(string mode)
    {
        await Open(mode, "stepper");
        var stepper = Page.GetByRole(AriaRole.List, new() { Name = "Checkout" });
        await Assertions.Expect(stepper.Locator("li[aria-current=step]")).ToContainTextAsync("Payment");

        await stepper.GetByRole(AriaRole.Button, new() { Name = "Review" }).ClickAsync();

        await Assertions.Expect(stepper.Locator("li[aria-current=step]")).ToContainTextAsync("Review");
        await Assertions.Expect(Page.GetByTestId("step-state")).ToHaveTextAsync("Step: 3 of 4");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Navigation_menu_opens_a_panel_with_links(string mode)
    {
        await Open(mode, "navigation-menu");
        var trigger = Page.GetByRole(AriaRole.Navigation, new() { Name = "Main" }).GetByRole(AriaRole.Button, new() { Name = "Components" });

        await trigger.ClickAsync();

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Tabs", Exact = true })).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Scroll_area_becomes_keyboard_focusable_when_it_overflows(string mode)
    {
        await Open(mode, "scroll-area");

        var viewport = Page.GetByRole(AriaRole.Region, new() { Name = "Changelog" });

        await Assertions.Expect(viewport).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Marquee_repeats_its_content_hidden_from_assistive_technology(string mode)
    {
        await Open(mode, "marquee");
        var marquee = Page.GetByTestId("marquee");

        await Assertions.Expect(marquee.Locator("[data-bx-marquee-group][aria-hidden=true]")).ToHaveCountAsync(1);
        Assert.Equal("running", await marquee.Locator("[data-bx-marquee-group]").First.EvaluateAsync<string>("e => getComputedStyle(e).animationPlayState"));
    }
}
