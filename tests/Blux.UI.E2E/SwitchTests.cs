namespace Blux.UI.E2E;

public class SwitchTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_has_no_accessibility_violations(string mode)
    {
        await Open(mode, "switch");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Toggling_updates_the_bound_value(string mode)
    {
        await Open(mode, "switch");
        var toggle = Page.GetByRole(AriaRole.Switch, new() { Name = "Email notifications" });

        await toggle.ClickAsync();

        await Assertions.Expect(toggle).ToBeCheckedAsync();
        await Assertions.Expect(Page.GetByTestId("notify-state")).ToHaveTextAsync("Notifications: on");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Clicking_the_label_toggles_the_switch(string mode)
    {
        await Open(mode, "switch");
        var toggle = Page.GetByRole(AriaRole.Switch, new() { Name = "Email notifications" });

        await Page.Locator("label", new() { HasText = "Email notifications" }).ClickAsync();

        await Assertions.Expect(toggle).ToBeCheckedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Form_validates_then_submits(string mode)
    {
        await Open(mode, "switch");
        var save = Page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await save.ClickAsync();
        await Assertions.Expect(Page.Locator("form").GetByText("You must accept the terms.")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Switch, new() { Name = "I accept the terms" }).CheckAsync();
        await save.ClickAsync();

        await Assertions.Expect(Page.GetByTestId("saved")).ToBeVisibleAsync();
    }
}
