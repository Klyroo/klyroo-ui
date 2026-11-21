using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>BxInputMenu with an OptionsProvider: debounced search while typing, paging, a strict value restored by ResolveSelected, error and short-text states.</summary>
public class RemoteInputMenuTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator Menu(string testId) => Page.Locator($"bx-input-menu:has([data-testid={testId}])");
    private ILocator Input(string testId) => Page.GetByTestId(testId);
    private static ILocator Options(ILocator menu) => menu.GetByRole(AriaRole.Option);

    private async Task EnhancedReload()
    {
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");
    }

    private async Task Dark()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Clicking_the_field_loads_the_first_page_and_typing_asks_once_after_the_pause(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-free");
        await Assertions.Expect(Page.GetByTestId("remote-free-requests")).ToHaveTextAsync("Free field calls: 0");

        await Input("remote-free").ClickAsync();

        await Assertions.Expect(menu.Locator("[data-slot=announcer]")).ToHaveTextAsync("Loading...");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Assertions.Expect(Page.GetByTestId("remote-free-last")).ToHaveTextAsync("Last call: search \"\", skip 0, take 20");

        await Input("remote-free").PressSequentiallyAsync("germ");

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Germany");
        await Assertions.Expect(Page.GetByTestId("remote-free-requests")).ToHaveTextAsync("Free field calls: 2");
        await Page.WaitForTimeoutAsync(500);
        await Assertions.Expect(Page.GetByTestId("remote-free-requests")).ToHaveTextAsync("Free field calls: 2");
        await Assertions.Expect(Page.GetByTestId("remote-free-last")).ToHaveTextAsync("Last call: search \"germ\", skip 0, take 20");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_free_text_field_takes_the_suggestion_chosen_with_the_keyboard_as_its_text(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-free");

        await Input("remote-free").ClickAsync();
        await Input("remote-free").PressSequentiallyAsync("germ");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Input("remote-free")).ToHaveValueAsync("Germany");
        await Assertions.Expect(Page.GetByTestId("remote-input-state")).ToContainTextAsync("Free: Germany");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Scrolling_loads_the_next_page_before_the_end_and_adds_it_to_the_list(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-free");
        await Input("remote-free").ClickAsync();
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await menu.Locator("[data-slot=scroller]").EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(40);
        await Assertions.Expect(Page.GetByTestId("remote-free-last")).ToHaveTextAsync("Last call: search \"\", skip 20, take 20");
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Argentina");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_strict_value_is_named_by_ResolveSelected_and_keeps_its_label_when_a_search_drops_it(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-strict");

        // France was never listed: one call names it.
        await Assertions.Expect(Input("remote-strict")).ToHaveValueAsync("France");
        await Assertions.Expect(Page.GetByTestId("remote-strict-last")).ToHaveTextAsync("Last call: resolve fr");
        await Assertions.Expect(Page.GetByTestId("remote-strict-requests")).ToHaveTextAsync("Strict field calls: 1");

        // A strict field lists everything when it opens, whatever its text.
        await Input("remote-strict").ClickAsync();
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Assertions.Expect(Page.GetByTestId("remote-strict-last")).ToHaveTextAsync("Last call: search \"\", cursor none, take 20");

        await Input("remote-strict").FillAsync("ital");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Page.GetByTestId("remote-input-state")).ToContainTextAsync("strict: it");
        await Assertions.Expect(Input("remote-strict")).ToHaveValueAsync("Italy");

        // Search for something else, then leave the field: the text goes back to the label of the value, though Italy is not in the list.
        await Input("remote-strict").FillAsync("jap");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Japan");
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(Input("remote-strict")).ToHaveValueAsync("Italy");
        await Assertions.Expect(Page.GetByTestId("remote-input-state")).ToContainTextAsync("strict: it");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_short_text_shows_a_hint_and_a_failing_source_an_error_with_a_retry_button(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-input-error");
        await Page.GetByRole(AriaRole.Switch, new() { Name = "The source is down" }).ClickAsync();

        // The empty text lists everything, so opening already asks, and fails.
        await Input("remote-input-error").ClickAsync();
        var alert = menu.GetByRole(AriaRole.Alert);
        await Assertions.Expect(alert).ToContainTextAsync("The options could not be loaded.");
        await Assertions.Expect(Page.GetByTestId("remote-input-error-requests")).ToHaveTextAsync("Source calls: 1");

        await alert.GetByRole(AriaRole.Button, new() { Name = "Retry" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("remote-input-error-requests")).ToHaveTextAsync("Source calls: 2");
        await Assertions.Expect(alert).ToBeVisibleAsync();

        // Nothing is highlighted, so Enter asks again too.
        await Input("remote-input-error").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Page.GetByTestId("remote-input-error-requests")).ToHaveTextAsync("Source calls: 3");

        await Input("remote-input-error").PressSequentiallyAsync("a");
        await Assertions.Expect(menu.Locator("[data-slot=hint]")).ToHaveTextAsync("Type at least 2 characters to search.");
        await Assertions.Expect(menu.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);

        // The source comes back: a text long enough searches it.
        await Page.Keyboard.PressAsync("Escape");
        await Page.GetByRole(AriaRole.Switch, new() { Name = "The source is down" }).ClickAsync();
        // Leaving the field committed its text: wait for .NET to have it, or the render that follows would put the old text back under the typing.
        await Assertions.Expect(Page.GetByTestId("remote-input-error-state")).ToHaveTextAsync("Text: a");
        await Input("remote-input-error").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.TypeAsync("u");
        await Assertions.Expect(Options(menu).First).ToBeVisibleAsync();
        await Assertions.Expect(menu.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Open_lists_have_no_accessibility_violations_loading_loaded_and_failed_in_light_and_dark(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-free");
        await Input("remote-free").ClickAsync();
        await Assertions.Expect(menu.Locator("[data-slot=announcer]")).ToHaveTextAsync("Loading...");
        await AssertNoAccessibilityViolations();
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await AssertNoAccessibilityViolations();
        await Dark();
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");

        var failing = Menu("remote-input-error");
        await Page.GetByRole(AriaRole.Switch, new() { Name = "The source is down" }).ClickAsync();
        await Input("remote-input-error").ClickAsync();
        await Assertions.Expect(failing.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Open_lists_have_no_accessibility_violations_with_a_hint_and_with_no_results(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-input-error");
        await Input("remote-input-error").ClickAsync();
        await Input("remote-input-error").PressSequentiallyAsync("a");
        await Assertions.Expect(menu.Locator("[data-slot=hint]")).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();

        await Input("remote-input-error").PressSequentiallyAsync("zz");
        await Assertions.Expect(menu.Locator("[data-slot=empty]")).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Statically_rendered_free_text_is_a_plain_field_and_strict_is_read_only_also_after_an_enhanced_navigation()
    {
        await Open("ssr", "input-menu");

        await Assertions.Expect(Input("remote-free")).Not.ToHaveAttributeAsync("readonly", new Regex(".*"));
        await Assertions.Expect(Input("remote-strict")).ToHaveAttributeAsync("readonly", "");
        await Assertions.Expect(Input("remote-strict")).ToHaveValueAsync("fr");

        // Typing in the free field never opens an empty list.
        await Input("remote-free").ClickAsync();
        await Input("remote-free").PressSequentiallyAsync("Par");
        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(Page.Locator("bx-input-menu [data-slot=content]:popover-open")).ToHaveCountAsync(0);

        await EnhancedReload();

        await Assertions.Expect(Input("remote-strict")).ToHaveAttributeAsync("readonly", "");
        await Assertions.Expect(Input("remote-free")).Not.ToHaveAttributeAsync("readonly", new Regex(".*"));
        await Assertions.Expect(Page.GetByTestId("remote-free-requests")).ToHaveTextAsync("Free field calls: 0");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_strict_value_named_by_Items_keeps_its_label_in_every_mode_after_the_browser_took_over(string mode)
    {
        await Open(mode, "input-menu");

        await Assertions.Expect(Input("remote-named-strict")).ToHaveValueAsync("France");
        await Page.WaitForTimeoutAsync(600);
        await Assertions.Expect(Input("remote-named-strict")).ToHaveValueAsync("France");
        await Assertions.Expect(Menu("remote-named-strict")).ToHaveAttributeAsync("data-known", new Regex("France"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_highlight_of_a_strict_field_comes_back_when_the_text_is_the_one_already_answered(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Menu("remote-strict");
        await Input("remote-strict").ClickAsync();
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Input("remote-strict").FillAsync("ital");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Input("remote-strict")).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));

        await Page.Keyboard.PressAsync("Space");
        await Page.WaitForTimeoutAsync(600);
        await Assertions.Expect(Input("remote-strict")).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));

        await Page.Keyboard.PressAsync("Backspace");
        await Page.Keyboard.PressAsync("Space");
        await Page.WaitForTimeoutAsync(700);
        await Assertions.Expect(Input("remote-strict")).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));

        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Page.GetByTestId("remote-input-state")).ToContainTextAsync("strict: it");
    }
}
