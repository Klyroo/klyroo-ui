namespace Blux.UI.E2E;

public class ButtonTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_has_no_accessibility_violations(string mode)
    {
        await Open(mode, "button");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Dark_mode_has_no_accessibility_violations(string mode)
    {
        await Open(mode, "button");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("dark"));

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Click_reaches_the_component(string mode)
    {
        await Open(mode, "button");
        var counter = Page.GetByTestId("counter");

        await counter.ClickAsync();
        await counter.ClickAsync();

        await Assertions.Expect(counter).ToHaveTextAsync("Clicked 2 times");
    }
}
