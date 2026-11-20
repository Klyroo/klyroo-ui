using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

public class PageTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "page", "page-header", "page-hero", "page-section", "page-feature", "page-grid", "page-columns", "page-card",
        "page-cta", "page-links", "page-list", "page-logos", "page-anchors", "page-aside", "page-body",
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
    public async Task Page_family_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_card_is_one_link_and_a_click_anywhere_on_the_card_follows_it(string mode)
    {
        await Open(mode, "page-card");
        var card = Page.GetByTestId("card-link");

        await Assertions.Expect(card.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
        await Assertions.Expect(card.GetByRole(AriaRole.Link)).ToHaveAccessibleNameAsync("Documentation");

        // The description is not the link: the stretched title box has to catch the click.
        // Forced: Playwright refuses to click an element that something else covers, which is what the stretched link does.
        await card.GetByText("Install the package").ClickAsync(new() { Force = true });

        await Page.WaitForURLAsync(new Regex("/docs/installation$"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_card_link_covers_the_header_and_the_footer_but_not_their_buttons(string mode)
    {
        await Open(mode, "page-card");
        var card = Page.GetByTestId("card-whole");
        var share = card.GetByRole(AriaRole.Button, new() { Name = "Share" });

        // The footer button is above the stretched link: a plain click reaches it and the page stays.
        await share.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        Assert.EndsWith("/components/page-card", new Uri(Page.Url).AbsolutePath);

        await card.GetByTestId("card-footer-text").ClickAsync(new() { Force = true });
        await Page.WaitForURLAsync(new Regex("/docs/installation$"));
        // The address changes as soon as the link is followed, before an interactive router has rendered the new page:
        // going back at that moment races with the render, and the card would still be the old one on screen.
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Installation", Level = 1 })).ToBeVisibleAsync();
        await Page.GoBackAsync();
        await Page.WaitForURLAsync(new Regex($"/{mode}/components/page-card$"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "PageCard", Level = 1 })).ToBeVisibleAsync();
        await Assertions.Expect(card).ToBeVisibleAsync();

        // A forced click lands on whatever is at the center of the header: bring it clear of the sticky site header and of the
        // scroll position the back navigation restores, so that is the card link.
        var header = card.GetByTestId("card-header");
        await header.EvaluateAsync("e => e.scrollIntoView({ block: 'center' })");
        await header.ClickAsync(new() { Force = true });
        await Page.WaitForURLAsync(new Regex("/docs/installation$"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_card_that_opens_a_new_tab_names_it_for_screen_readers(string mode)
    {
        await Open(mode, "page-card");
        var link = Page.GetByTestId("card-external").GetByRole(AriaRole.Link);

        await Assertions.Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Assertions.Expect(link).ToHaveAttributeAsync("rel", "noopener noreferrer");
        await Assertions.Expect(link).ToHaveAccessibleNameAsync(new Regex(@"Source code \(opens in a new tab\)"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_feature_link_covers_the_whole_feature(string mode)
    {
        await Open(mode, "page-feature");

        await Page.GetByTestId("feature-link").GetByText("The whole feature is one link.").ClickAsync(new() { Force = true });

        await Page.WaitForURLAsync(new Regex("/docs/installation$"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_anchors_jump_to_their_section(string mode)
    {
        await Open(mode, "page-anchors");
        var nav = Page.GetByTestId("anchors");
        await Assertions.Expect(nav).ToHaveAttributeAsync("aria-label", "On this page");
        var target = Page.Locator("#anchor-release");
        await Assertions.Expect(target).Not.ToBeInViewportAsync();

        await nav.GetByRole(AriaRole.Link, new() { Name = "Releases" }).ClickAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("#anchor-release$"));
        await Assertions.Expect(target).ToBeInViewportAsync();
        // A fragment link is never "current": the anchors list has no address-based state.
        await Assertions.Expect(nav.Locator("[aria-current]")).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_links_mark_only_the_active_link_as_current(string mode)
    {
        await Open(mode, "page-links");
        var resources = Page.GetByRole(AriaRole.Navigation, new() { Name = "Resources" });

        await Assertions.Expect(resources.GetByRole(AriaRole.Link, new() { Name = "Overview" })).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(resources.GetByRole(AriaRole.Link, new() { Name = "Components" })).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Assertions.Expect(resources.GetByRole(AriaRole.Link, new() { Name = "Source code" })).ToHaveAccessibleNameAsync(new Regex(@"opens in a new tab"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_section_is_a_landmark_named_by_its_heading(string mode)
    {
        await Open(mode, "page-section");

        var region = Page.GetByRole(AriaRole.Region, new() { Name = "Built for the way Blazor renders" });

        await Assertions.Expect(region).ToHaveCountAsync(1);
        await Assertions.Expect(region.GetByRole(AriaRole.Heading, new() { Name = "Built for the way Blazor renders" })).ToHaveCountAsync(1);
    }
}
