namespace Blux.UI.E2E;

/// <summary>BxCheckbox: the indeterminate state, which HTML cannot render and a script of Blux sets on the DOM property.</summary>
public class CheckboxTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator SelectAll => Page.GetByRole(AriaRole.Checkbox, new() { Name = "Select all" });
    private ILocator Mixed => Page.GetByRole(AriaRole.Checkbox, new() { Name = "Always mixed" });

    private static Task ExpectIndeterminate(ILocator box, bool expected) =>
        Assertions.Expect(box).ToHaveJSPropertyAsync("indeterminate", expected);

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Indeterminate_is_set_on_the_input_and_exposed_as_mixed(string mode)
    {
        await Open(mode, "checkbox");

        await ExpectIndeterminate(SelectAll, true);
        await ExpectIndeterminate(Mixed, true);
        await Assertions.Expect(SelectAll).Not.ToBeCheckedAsync();
        await Assertions.Expect(SelectAll).ToMatchAriaSnapshotAsync("- checkbox \"Select all\" [checked=mixed]");
        await Assertions.Expect(Mixed).ToMatchAriaSnapshotAsync("- checkbox \"Always mixed\" [checked=mixed]");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Clicking_an_indeterminate_box_clears_it_and_checks_it(string mode)
    {
        await Open(mode, "checkbox");

        await SelectAll.ClickAsync();

        await Assertions.Expect(SelectAll).ToBeCheckedAsync();
        await ExpectIndeterminate(SelectAll, false);
        if (mode != "ssr")
        {
            // The parent owns the state: all three items are selected now.
            await Assertions.Expect(Page.GetByTestId("select-all-state")).ToHaveTextAsync("Selected: 3 of 3");
        }
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Unticking_an_item_makes_the_select_all_box_mixed_again(string mode)
    {
        await Open(mode, "checkbox");
        await SelectAll.ClickAsync();
        await Assertions.Expect(Page.GetByTestId("select-all-state")).ToHaveTextAsync("Selected: 3 of 3");

        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "SMS" }).UncheckAsync();

        await Assertions.Expect(Page.GetByTestId("select-all-state")).ToHaveTextAsync("Selected: 2 of 3");
        await ExpectIndeterminate(SelectAll, true);
        await Assertions.Expect(SelectAll).Not.ToBeCheckedAsync();

        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Email" }).UncheckAsync();
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Push" }).UncheckAsync();

        await Assertions.Expect(Page.GetByTestId("select-all-state")).ToHaveTextAsync("Selected: 0 of 3");
        await ExpectIndeterminate(SelectAll, false);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_render_that_leaves_Indeterminate_true_puts_the_mixed_state_back(string mode)
    {
        await Open(mode, "checkbox");

        // The click clears it natively and toggles the box; the parent keeps Indeterminate true, so the render puts it back.
        await Mixed.ClickAsync();
        await Assertions.Expect(Mixed).ToBeCheckedAsync();
        await ExpectIndeterminate(Mixed, true);

        // A parent render bumps the box's version, so the mixed state stays applied.
        await Page.GetByTestId("rerender").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("rerender")).ToContainTextAsync("(1)");
        await ExpectIndeterminate(Mixed, true);
    }

    [Fact]
    public async Task Without_interactivity_a_click_clears_the_mixed_state_and_checks_the_box()
    {
        await Open("ssr", "checkbox");

        await Mixed.ClickAsync();

        await Assertions.Expect(Mixed).ToBeCheckedAsync();
        await ExpectIndeterminate(Mixed, false);
    }

    // Enhanced navigation merges server-rendered pages; an interactive page is not merged, so there is no enhancedload there.
    [Fact]
    public async Task Indeterminate_is_applied_again_after_an_enhanced_navigation()
    {
        await Open("ssr", "checkbox");
        await ExpectIndeterminate(Mixed, true);
        // What a merge that reset the element would leave: the attribute is the same, the property is not.
        await Mixed.EvaluateAsync("e => { e.indeterminate = false; }");
        await ExpectIndeterminate(Mixed, false);

        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");

        await ExpectIndeterminate(Mixed, true);
        await ExpectIndeterminate(SelectAll, true);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Indeterminate_boxes_have_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await Open(mode, "checkbox");
        await ExpectIndeterminate(Mixed, true);

        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("dark"));

        await AssertNoAccessibilityViolations();
    }
}
