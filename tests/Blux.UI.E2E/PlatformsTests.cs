namespace Blux.UI.E2E;

/// <summary>The "web, phone, TV" section of the home page and the screenshots of the MAUI guide.</summary>
public class PlatformsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    /// <summary>Waits until a lazy image is loaded and decoded (naturalWidth is 0 for a broken one).</summary>
    private Task WaitForImage(string testId) => Page.WaitForFunctionAsync(
        "id => { const i = document.querySelector(`[data-testid=${id}]`); return i && i.complete && i.naturalWidth > 0; }",
        testId, new() { Timeout = 30_000 });

    private async Task OpenHome()
    {
        await Page.GotoAsync("/");
        await Assertions.Expect(Page.GetByTestId("renderer").First).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover') && document.title.length > 0");
        await Page.Locator("#platforms").ScrollIntoViewIfNeededAsync();
    }

    [Fact]
    public async Task The_home_page_shows_the_platform_screenshots_and_an_honest_status_per_target()
    {
        await OpenHome();

        await Assertions.Expect(Page.GetByTestId("platform-phone")).ToHaveAttributeAsync("width", "540");
        await Assertions.Expect(Page.GetByTestId("platform-tv")).ToHaveAttributeAsync("width", "1280");
        await WaitForImage("platform-phone");
        await WaitForImage("platform-tv");
        var list = Page.GetByRole(AriaRole.List, new() { Name = "Platform support" });
        await Assertions.Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(7);
        await Assertions.Expect(list).ToContainTextAsync("Experimental");
        await Assertions.Expect(list).ToContainTextAsync("Not supported");
        // Targets that were never built or run must not read as tested.
        foreach (var target in new[] { "iOS 17+", "Windows", "macOS" })
        {
            await Assertions.Expect(list.GetByRole(AriaRole.Listitem).Filter(new() { HasText = target })).ToContainTextAsync("Untested");
        }
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Read the MAUI guide" })).ToHaveAttributeAsync("href", "/docs/maui");
    }

    [Fact]
    public async Task The_maui_guide_shows_a_phone_and_a_tv_screenshot()
    {
        await OpenGuide("/docs/maui");
        await Page.GetByTestId("maui-tv").ScrollIntoViewIfNeededAsync();
        await WaitForImage("maui-phone");
        await WaitForImage("maui-tv");
    }

    [Fact]
    public async Task The_home_page_has_no_accessibility_violations_in_light_mode()
    {
        await OpenHome();

        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task The_home_page_has_no_accessibility_violations_in_dark_mode()
    {
        await OpenHome();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).First.ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("dark"));

        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task The_home_page_does_not_scroll_sideways_on_a_phone()
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenHome();

        var overflow = await Page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        Assert.True(overflow <= 0, $"The page is {overflow}px wider than the viewport.");
    }
}
