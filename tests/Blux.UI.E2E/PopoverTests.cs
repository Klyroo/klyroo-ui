namespace Blux.UI.E2E;

public class PopoverTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Opens_moves_focus_inside_and_closes_on_Escape(string mode)
    {
        await Open(mode, "popover");
        var trigger = Page.GetByTestId("unbound-trigger");
        var panel = Page.GetByTestId("unbound-panel");

        await trigger.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Page.GetByTestId("unbound-input")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(panel).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Is_positioned_next_to_its_trigger(string mode)
    {
        await Open(mode, "popover");
        var trigger = Page.GetByTestId("unbound-trigger");

        await trigger.ClickAsync();

        var t = (await trigger.BoundingBoxAsync())!;
        var p = (await Page.Locator("[data-testid=unbound-panel]").Locator("xpath=ancestor::*[@popover][1]").BoundingBoxAsync())!;
        // Placement "right" with an 8px offset.
        Assert.InRange(p.X - (t.X + t.Width), 6, 10);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_state_follows_light_dismiss_and_programmatic_close(string mode)
    {
        await Open(mode, "popover");
        var state = Page.GetByTestId("bound-state");

        await Page.GetByTestId("bound-trigger").ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("State: open");

        await Page.GetByRole(AriaRole.Heading, new() { Name = "Popover", Level = 1 }).ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("State: closed");
        await Assertions.Expect(Page.GetByTestId("bound-panel")).ToBeHiddenAsync();

        await Page.GetByTestId("bound-trigger").ClickAsync();
        await Page.GetByTestId("bound-close").ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("State: closed");
        await Assertions.Expect(Page.GetByTestId("bound-panel")).ToBeHiddenAsync();
    }

    // The native popover closes at once and its "toggle" event, the report to .NET, comes a task later. A render that still says "open" (the echo of the opening,
    // on a busy circuit or a busy main thread) that lands in between must not reopen what the user has just closed. The page below does what the browser does for
    // Escape, then what Blazor does for that render, in one task: no timing is left to chance.
    [Fact]
    public async Task A_bound_popover_closed_by_the_user_is_not_reopened_by_a_render_that_was_made_before()
    {
        await Open("server", "popover");
        var panel = Page.GetByTestId("bound-panel");
        await Page.GetByTestId("bound-trigger").ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByTestId("bound-state")).ToHaveTextAsync("State: open");

        var open = await Page.EvaluateAsync<bool>(@"() => {
            const element = document.querySelector('[data-testid=bound-panel]').closest('bx-popover');
            const content = element.querySelector(':scope > [popover]');
            const seq = element.getAttribute('data-bx-seq');
            content.hidePopover();
            element.removeAttribute('open');
            element.setAttribute('data-bx-seq', seq);
            element.setAttribute('open', '');
            return content.matches(':popover-open');
        }");

        Assert.False(open, "a render older than the close reopened the popover");
        await Assertions.Expect(Page.GetByTestId("bound-state")).ToHaveTextAsync("State: closed");
        await Assertions.Expect(panel).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_has_no_accessibility_violations_while_open(string mode)
    {
        await Open(mode, "popover");
        await Page.GetByTestId("unbound-trigger").ClickAsync();

        await AssertNoAccessibilityViolations();
    }
}
