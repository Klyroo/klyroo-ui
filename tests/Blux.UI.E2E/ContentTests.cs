using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The "Content" family: ContentNavigation, ContentToc, ContentSurround, ContentSearch and ContentSearchButton. The docs
/// site is built with them (sidebar, table of contents, previous/next links, header search), so those are tested too.
/// </summary>
public class ContentTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
        ["content-navigation", "content-search", "content-search-button", "content-surround", "content-toc"];

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
    public async Task Content_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [InlineData("content-navigation")]
    [InlineData("content-toc")]
    [InlineData("content-surround")]
    [InlineData("content-search-button")]
    public async Task Content_page_has_no_accessibility_violations_in_dark_mode(string slug)
    {
        await Open("ssr", slug);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));

        await AssertNoAccessibilityViolations();
    }

    private async Task EnhancedReload()
    {
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");
    }

    // ---- ContentNavigation

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Navigation_groups_open_and_close_with_the_pointer_and_the_keyboard(string mode)
    {
        await Open(mode, "content-navigation");
        var nav = Page.GetByRole(AriaRole.Navigation, new() { Name = "Example documentation" });
        var components = nav.GetByRole(AriaRole.Button, new() { Name = "Components" });
        var form = nav.GetByRole(AriaRole.Button, new() { Name = "Form" });
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Input" })).ToBeHiddenAsync();

        await components.ClickAsync();

        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Button" })).ToBeVisibleAsync();
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Input" })).ToBeHiddenAsync();

        await form.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Input" })).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Input" })).ToBeHiddenAsync();
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Navigation_shows_the_current_page_badges_and_a_disabled_entry()
    {
        await Open("ssr", "content-navigation");
        var nav = Page.GetByRole(AriaRole.Navigation, new() { Name = "Example documentation" });

        await Assertions.Expect(nav.Locator("a[aria-current=page]")).ToHaveCountAsync(1);
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Introduction" })).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Theming" })).ToContainTextAsync("New");
        // A disabled entry is not a link.
        await nav.GetByRole(AriaRole.Button, new() { Name = "Components" }).ClickAsync();
        await Assertions.Expect(nav.Locator("[aria-disabled=true]")).ToHaveTextAsync("Archived");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Archived" })).ToHaveCountAsync(0);
    }

    // The sidebar of the site is a ContentNavigation: the current page is marked and clicking a link moves the mark.
    private ILocator Sidebar => Page.GetByRole(AriaRole.Navigation, new() { Name = "Documentation", Exact = true });

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Docs_sidebar_marks_the_current_page_and_keeps_the_render_mode_when_following_a_link(string mode)
    {
        await Open(mode, "button");

        await Assertions.Expect(Sidebar.Locator("a[aria-current=page]")).ToHaveCountAsync(1);
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Link, new() { Name = "Button", Exact = true })).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Button, new() { Name = "Elements" })).ToHaveAttributeAsync("aria-expanded", "true");

        await Sidebar.GetByRole(AriaRole.Link, new() { Name = "Badge", Exact = true }).ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"/{mode}/components/badge$"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Badge", Level = 1 })).ToBeVisibleAsync();
        await Assertions.Expect(Sidebar.Locator("a[aria-current=page]")).ToHaveCountAsync(1);
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Link, new() { Name = "Badge", Exact = true })).ToHaveAttributeAsync("aria-current", "page");
    }

    [Fact]
    public async Task Docs_sidebar_groups_collapse_and_every_page_is_still_listed()
    {
        await Open("ssr", "button");
        var elements = Sidebar.GetByRole(AriaRole.Button, new() { Name = "Elements" });

        await elements.ClickAsync();

        await Assertions.Expect(elements).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Link, new() { Name = "Avatar", Exact = true })).ToBeHiddenAsync();
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Link, new() { Name = "Input", Exact = true })).ToBeVisibleAsync();
        // The pages of the new families are in the sidebar.
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Link, new() { Name = "ContentToc", Exact = true })).ToHaveAttributeAsync("href", "/ssr/components/content-toc");
        await Assertions.Expect(Sidebar.GetByRole(AriaRole.Link, new() { Name = "Chart", Exact = true })).ToHaveAttributeAsync("href", "/ssr/components/chart");
    }

    // ---- ContentToc

    private async Task ScrollHeadingToTop(string id) =>
        await Page.EvaluateAsync("id => { const el = document.getElementById(id); window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 80); }", id);

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Toc_marks_the_heading_being_read_as_the_page_scrolls(string mode)
    {
        await Open(mode, "content-toc");
        var toc = Page.GetByTestId("content-toc-example-toc");
        var current = toc.Locator("a[aria-current=location]");

        await ScrollHeadingToTop("toc-usage");

        await Assertions.Expect(current).ToHaveCountAsync(1);
        await Assertions.Expect(current).ToHaveTextAsync("Usage");
        await Assertions.Expect(current).ToHaveAttributeAsync("data-active", "");

        await ScrollHeadingToTop("toc-theme");

        await Assertions.Expect(current).ToHaveTextAsync("Theming");
        await Assertions.Expect(toc.Locator("[data-active]")).ToHaveCountAsync(1);

        await ScrollHeadingToTop("toc-intro");

        await Assertions.Expect(current).ToHaveTextAsync("Introduction");
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Toc_link_scrolls_to_its_heading_and_becomes_current()
    {
        await Open("ssr", "content-toc");
        var toc = Page.GetByTestId("content-toc-example-toc");

        await toc.GetByRole(AriaRole.Link, new() { Name = "Props" }).ClickAsync();

        await Assertions.Expect(toc.Locator("a[aria-current=location]")).ToHaveTextAsync("Props");
        await Assertions.Expect(Page.GetByTestId("toc-heading-toc-props")).ToBeInViewportAsync();
        Assert.EndsWith("#toc-props", Page.Url);
    }

    [Fact]
    public async Task Toc_keeps_following_the_page_after_an_enhanced_navigation()
    {
        await Open("ssr", "content-toc");
        var toc = Page.GetByTestId("content-toc-example-toc");
        await ScrollHeadingToTop("toc-usage");
        await Assertions.Expect(toc.Locator("a[aria-current=location]")).ToHaveTextAsync("Usage");

        await EnhancedReload();

        // The merge replaced the links; the element marks the heading that is being read again, and keeps following.
        await Assertions.Expect(toc.Locator("a[aria-current=location]")).ToHaveCountAsync(1);
        await ScrollHeadingToTop("toc-theme");
        await Assertions.Expect(toc.Locator("a[aria-current=location]")).ToHaveTextAsync("Theming");
    }

    [Fact]
    public async Task Toc_collapses_behind_its_title_on_a_narrow_screen()
    {
        await Page.SetViewportSizeAsync(600, 900);
        await Open("ssr", "content-toc");
        var toc = Page.GetByTestId("content-toc-example-toc");
        var trigger = toc.GetByRole(AriaRole.Button, new() { Name = "In this article" });
        var intro = toc.GetByRole(AriaRole.Link, new() { Name = "Introduction" });

        await Assertions.Expect(trigger).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(intro).ToBeHiddenAsync();

        await trigger.ClickAsync();

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(intro).ToBeVisibleAsync();

        await toc.GetByRole(AriaRole.Link, new() { Name = "Usage" }).ClickAsync();

        // Following a link closes the list again.
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(intro).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Toc_is_always_open_without_a_button_on_a_wide_screen()
    {
        await Open("ssr", "content-toc");
        var toc = Page.GetByTestId("content-toc-example-toc");

        await Assertions.Expect(toc.GetByRole(AriaRole.Button)).ToBeHiddenAsync();
        await Assertions.Expect(toc.GetByRole(AriaRole.Link, new() { Name = "Introduction" })).ToBeVisibleAsync();
        await Assertions.Expect(toc.GetByText("In this article", new() { Exact = true }).Last).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Component_pages_have_a_toc_of_their_examples_and_the_api_in_the_margin_on_a_wide_screen()
    {
        await Page.SetViewportSizeAsync(1700, 900);
        await Open("ssr", "button");
        var toc = Page.GetByTestId("page-toc");

        await Assertions.Expect(toc.GetByRole(AriaRole.Button)).ToBeHiddenAsync();
        await Assertions.Expect(toc.GetByRole(AriaRole.Link)).ToHaveTextAsync(["Variants", "Sizes and icons", "States", "API"]);

        await ScrollHeadingToTop("api");

        await Assertions.Expect(toc.Locator("a[aria-current=location]")).ToHaveTextAsync("API");
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Component_pages_collapse_their_toc_below_the_2xl_breakpoint()
    {
        await Open("ssr", "button");
        var toc = Page.GetByTestId("page-toc");
        var trigger = toc.GetByRole(AriaRole.Button, new() { Name = "Contents" });

        await Assertions.Expect(trigger).ToBeVisibleAsync();
        await Assertions.Expect(toc.GetByRole(AriaRole.Link, new() { Name = "Variants" })).ToBeHiddenAsync();

        await trigger.ClickAsync();

        await Assertions.Expect(toc.GetByRole(AriaRole.Link, new() { Name = "Variants" })).ToBeVisibleAsync();
    }

    // ---- ContentSurround

    private ILocator Surround => Page.GetByTestId("page-surround");

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Surround_links_the_neighbours_in_the_order_of_the_sidebar_and_keeps_the_render_mode(string mode)
    {
        await Open(mode, "badge");

        var previous = Surround.GetByRole(AriaRole.Link).Filter(new() { HasText = "Previous" });
        var next = Surround.GetByRole(AriaRole.Link).Filter(new() { HasText = "Next" });
        await Assertions.Expect(previous).ToContainTextAsync("Button");
        await Assertions.Expect(previous).ToHaveAttributeAsync("href", $"/{mode}/components/button");
        await Assertions.Expect(previous).ToHaveAttributeAsync("rel", "prev");
        await Assertions.Expect(next).ToContainTextAsync("Avatar");
        await Assertions.Expect(next).ToContainTextAsync("An image of a person");
        await Assertions.Expect(next).ToHaveAttributeAsync("rel", "next");

        await next.ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"/{mode}/components/avatar$"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Avatar", Level = 1 })).ToBeVisibleAsync();
        await Assertions.Expect(Surround.GetByRole(AriaRole.Link).Filter(new() { HasText = "Previous" })).ToContainTextAsync("Badge");
    }

    [Fact]
    public async Task Surround_has_no_previous_link_on_the_first_page_and_no_next_link_on_the_last()
    {
        await Open("ssr", "button");
        await Assertions.Expect(Surround.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
        await Assertions.Expect(Surround.GetByRole(AriaRole.Link)).ToContainTextAsync("Next");

        await Open("ssr", "dashboard-search-button");
        await Assertions.Expect(Surround.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
        await Assertions.Expect(Surround.GetByRole(AriaRole.Link)).ToContainTextAsync("Previous");
        await Assertions.Expect(Surround.GetByRole(AriaRole.Link)).ToContainTextAsync("DashboardSearch");
    }

    [Fact]
    public async Task Surround_example_follows_a_link()
    {
        await Open("ssr", "content-surround");
        var example = Page.GetByTestId("content-surround-example");

        await Assertions.Expect(example.GetByRole(AriaRole.Link)).ToHaveCountAsync(2);
        await example.GetByRole(AriaRole.Link).Filter(new() { HasText = "Theming" }).ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/docs/theming$"));
    }

    // ---- ContentSearch (the search of the site, in the header)

    private ILocator SiteSearch => Page.GetByRole(AriaRole.Dialog, new() { Name = "Search documentation", Exact = true });

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Search_opens_with_the_shortcut_filters_the_catalog_and_navigates(string mode)
    {
        await Open(mode, "button");
        await Assertions.Expect(SiteSearch).ToBeHiddenAsync();

        await Page.Keyboard.PressAsync("Control+K");

        await Assertions.Expect(SiteSearch).ToBeVisibleAsync();
        var input = SiteSearch.GetByRole(AriaRole.Combobox);
        await Assertions.Expect(input).ToBeFocusedAsync();
        await input.PressSequentiallyAsync("tooltip");
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Option, new() { Name = "Badge" })).ToBeHiddenAsync();
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Option, new() { Name = "Tooltip" })).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/components/tooltip$"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Tooltip", Level = 1 })).ToBeVisibleAsync();
        await Assertions.Expect(SiteSearch).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Search_lists_pages_under_their_groups_and_matches_the_new_families()
    {
        await Open("ssr", "button");
        await Page.Keyboard.PressAsync("Control+K");

        var input = SiteSearch.GetByRole(AriaRole.Combobox);
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Group, new() { Name = "Getting started" })).ToBeVisibleAsync();
        await input.PressSequentiallyAsync("surround");
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Option, new() { Name = "ContentSurround" })).ToBeVisibleAsync();
        await input.FillAsync("");
        await input.PressSequentiallyAsync("chart");
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Option, new() { Name = "Chart" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Search_button_in_the_header_opens_the_search_and_focus_returns_to_it(string mode)
    {
        await Open(mode, "button");
        var button = Page.Locator("header").GetByRole(AriaRole.Button, new() { Name = "Search" });
        await Assertions.Expect(button.Locator("kbd")).ToHaveCountAsync(2);

        await button.ClickAsync();

        await Assertions.Expect(SiteSearch).ToBeVisibleAsync();
        await Assertions.Expect(SiteSearch.GetByRole(AriaRole.Combobox)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(SiteSearch).ToBeHiddenAsync();
        await Assertions.Expect(button).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Search_shortcut_yields_to_a_page_that_registers_the_same_one()
    {
        await Open("ssr", "dashboard-search");

        await Page.Keyboard.PressAsync("Control+K");

        // The dashboard's own search opens, not the one of the site.
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Search", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(SiteSearch).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Search_example_opens_with_its_button_and_its_own_shortcut_and_finds_headings(string mode)
    {
        await Open(mode, "content-search");
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Search the example docs" });
        var button = Page.GetByTestId("content-search-example-button");

        await button.ClickAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(SiteSearch).ToBeHiddenAsync();
        var input = dialog.GetByRole(AriaRole.Combobox);
        await input.PressSequentiallyAsync("services");
        // A heading of a page: its own result, with the page as context.
        var heading = dialog.GetByRole(AriaRole.Option, new() { Name = "Register the services" });
        await Assertions.Expect(heading).ToBeVisibleAsync();
        await Assertions.Expect(heading).ToContainTextAsync("Installation");
        await Assertions.Expect(heading).ToHaveAttributeAsync("href", "?section=installation#services");
        // The first Escape clears the query, the second closes.
        await input.FillAsync("");
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Alt+O");

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(SiteSearch).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Search_example_reports_the_selected_page(string mode)
    {
        await Open(mode, "content-search");
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Search the example docs" });

        await Page.GetByTestId("content-search-example-button").ClickAsync();
        await dialog.GetByRole(AriaRole.Option, new() { Name = "Theming" }).ClickAsync();

        // The option is a link: it navigates and closes the search.
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex(@"\?section=theming$"));
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Search_buttons_of_the_example_page_open_the_search_of_the_site()
    {
        await Open("ssr", "content-search-button");
        var icon = Page.GetByTestId("content-search-button-icon");

        await Assertions.Expect(icon).ToHaveAttributeAsync("aria-label", "Search");
        await icon.ClickAsync();

        await Assertions.Expect(SiteSearch).ToBeVisibleAsync();
    }
}
