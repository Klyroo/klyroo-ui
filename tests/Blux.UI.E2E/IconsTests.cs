namespace Blux.UI.E2E;

/// <summary>
/// The docs site bundles the Iconify icons its examples name (ph:, mdi:, simple-icons:...) at build time, and a folder of SVG
/// files as custom:blux. Components draw their icons from BluxOptions.Icons, in markup and in script.
/// </summary>
public class IconsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] ComponentPages =
    [
        "alert", "accordion", "select", "select-menu", "checkbox", "table", "pagination", "stepper", "tree", "navigation-menu", "breadcrumb", "banner",
        "modal", "drawer", "slideover", "carousel", "input-number", "input-date", "input-rating", "input-tags", "file-upload", "calendar", "auth-form",
        "chat-prompt", "chat-messages", "chat-attachment", "chat-tool", "chat-reasoning", "chat-prompt-submit", "color-mode-button", "color-mode-switch", "color-mode-select",
        "command-palette", "toast", "pricing-table", "button", "editor-toolbar", "editor-drag-handle", "content-toc", "content-surround", "content-navigation", "dashboard-sidebar", "dashboard-sidebar-collapse", "page-hero", "page-section",
    ];

    private async Task AssertNoMissingIcon(string where)
    {
        var missing = await Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('[data-bx-missing-icon]')].map(e => e.getAttribute('data-bx-missing-icon'))");
        Assert.True(missing.Length == 0, $"{where} renders missing icons: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_icon_page_shows_the_bundled_iconify_icons(string mode)
    {
        await Open(mode, "icon");

        var icons = Page.Locator("[data-testid=icon-collections] svg");
        await Assertions.Expect(icons).ToHaveCountAsync(7);
        var shapes = await Page.EvaluateAsync<int[]>("() => [...document.querySelectorAll('[data-testid=icon-collections] svg')].map(s => s.querySelectorAll('path,circle,g').length)");
        Assert.All(shapes, count => Assert.True(count > 0));
        var boxes = await Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('[data-testid=icon-collections] svg')].map(s => s.getAttribute('viewBox'))");
        // Phosphor is drawn on 256 units, Material Design and Lucide on 24, and the custom file keeps its own.
        Assert.Equal(["0 0 256 256", "0 0 24 24", "0 0 24 24", "0 0 24 24", "0 0 24 24", "0 0 24 24", "0 0 24 24"], boxes);
        // They are really painted: a rendered icon has a size.
        var first = await icons.First.BoundingBoxAsync();
        Assert.True(first!.Width > 10 && first.Height > 10);
        await AssertNoMissingIcon("the icon collections example");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Names_built_at_runtime_work_when_the_project_lists_them(string mode)
    {
        await Open(mode, "icon");

        await Assertions.Expect(Page.Locator("[data-testid=icon-dynamic] svg")).ToHaveCountAsync(3);
        await Assertions.Expect(Page.Locator("[data-testid=icon-dynamic] [data-bx-missing-icon]")).ToHaveCountAsync(0);
        var labels = await Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('[data-testid=icon-dynamic] svg')].map(s => s.getAttribute('aria-label'))");
        Assert.Equal(["Cube", "Cube-fill", "Cube-bold"], labels);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_content_navigation_example_draws_book_open_through_the_bundle(string mode)
    {
        await Open(mode, "content-navigation");

        await Assertions.Expect(Page.Locator("[data-testid=content-navigation-example] [data-bx-missing-icon]")).ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator("[data-testid=content-navigation-example] svg[data-icon], [data-testid=content-navigation-example] svg path").First).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Components_draw_their_icons_and_never_a_missing_one(string mode)
    {
        foreach (var page in ComponentPages)
        {
            await Open(mode, page);
            await AssertNoMissingIcon($"/{mode}/components/{page}");
        }
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Close_buttons_draw_the_close_icon_of_the_app(string mode)
    {
        await Open(mode, "banner");

        var close = Page.GetByTestId("banner").GetByRole(AriaRole.Button, new() { Name = "Close" });
        await Assertions.Expect(close.Locator("svg path")).ToHaveCountAsync(1);
        Assert.Equal("M18 6L6 18M6 6l12 12", await close.Locator("svg path").GetAttributeAsync("d"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Icons_drawn_by_script_come_from_the_app_icons(string mode)
    {
        await Open(mode, "toast");
        await Page.GetByTestId("toast-trigger").ClickAsync();
        var toast = Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Changes saved" });
        await Assertions.Expect(toast).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(toast.GetByRole(AriaRole.Button, new() { Name = "Close" }).Locator("svg path")).ToHaveCountAsync(1);

        await Open(mode, "input-tags");
        await Assertions.Expect(Page.Locator("[data-tag=blazor] button svg path")).ToHaveCountAsync(1);

        await Open(mode, "calendar");
        await Assertions.Expect(Page.Locator("[data-testid=calendar] button[data-nav=prev] svg path")).ToHaveCountAsync(1);
        await Assertions.Expect(Page.Locator("[data-testid=calendar] button[data-nav=next] svg path")).ToHaveCountAsync(1);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_icon_pages_have_no_accessibility_violations(string mode)
    {
        await Open(mode, "icon");
        await AssertNoAccessibilityViolations();

        await OpenGuide("/docs/icons");
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task The_icons_guide_lists_every_key_of_the_app_icons()
    {
        await Page.GotoAsync("/docs/icons");

        var rows = Page.Locator("[data-testid=icon-keys] tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(49);
        await Assertions.Expect(rows.First.Locator("th")).ToHaveTextAsync("ArrowDown");
        await Assertions.Expect(Page.Locator("[data-testid=icon-keys] tr", new() { HasText = "Close" }).First.Locator("td").First).ToHaveTextAsync("i-lucide-x");
    }
}
