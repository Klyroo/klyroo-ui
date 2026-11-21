using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>BxSelectMenu with an OptionsProvider: debounced search, paging while scrolling, restored values, loading and error states.</summary>
public class RemoteSelectMenuTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator Menu(string testId) => Page.Locator($"bx-select-menu:has([data-testid={testId}])");
    private ILocator Trigger(string testId) => Page.GetByTestId(testId);
    private static ILocator Search(ILocator menu) => menu.Locator("[data-slot=search]");
    private static ILocator Options(ILocator menu) => menu.GetByRole(AriaRole.Option);
    private ILocator Calls(string testId = "remote-requests") => Page.GetByTestId(testId);

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

    private async Task OpenList(ILocator menu, string testId)
    {
        await Trigger(testId).ClickAsync();
        await Assertions.Expect(Search(menu)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Opening_the_list_loads_the_first_page_and_the_loading_state_is_busy_and_announced(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await Assertions.Expect(Calls()).ToHaveTextAsync("Source calls: 0");

        await OpenList(menu, "remote-select");

        // The source answers after 400 ms: until then the list says it is loading.
        await Assertions.Expect(menu.Locator("[data-slot=announcer]")).ToHaveTextAsync("Loading...");
        await Assertions.Expect(menu.GetByRole(AriaRole.Listbox, new() { IncludeHidden = true })).ToHaveAttributeAsync("aria-busy", "true");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Argentina");
        await Assertions.Expect(menu.GetByRole(AriaRole.Listbox)).Not.ToHaveAttributeAsync("aria-busy", "true");
        await Assertions.Expect(menu.Locator("[data-slot=announcer]")).ToHaveTextAsync("");
        await Assertions.Expect(Calls()).ToHaveTextAsync("Source calls: 1");
        await Assertions.Expect(Page.GetByTestId("remote-last")).ToHaveTextAsync("Last call: search \"\", skip 0, take 20");
        // The first option is the one the arrows start from.
        await Assertions.Expect(Search(menu)).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_asks_the_source_once_after_the_pause_and_the_matches_replace_the_list(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await Search(menu).PressSequentiallyAsync("germ");

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Germany");
        // Four keys, one call (plus the call of the opening).
        await Assertions.Expect(Calls()).ToHaveTextAsync("Source calls: 2");
        await Page.WaitForTimeoutAsync(500);
        await Assertions.Expect(Calls()).ToHaveTextAsync("Source calls: 2");
        await Assertions.Expect(Page.GetByTestId("remote-last")).ToHaveTextAsync("Last call: search \"germ\", skip 0, take 20");

        await Search(menu).FillAsync("zzz");
        await Assertions.Expect(menu.Locator("[data-slot=empty]")).ToHaveTextAsync("No results");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(0);

        // Nothing typed lists everything again.
        await Search(menu).FillAsync("");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Scrolling_to_the_end_loads_the_next_page_before_it_is_reached(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        var scroller = menu.Locator("[data-slot=scroller]");

        await scroller.EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(40);
        await Assertions.Expect(Page.GetByTestId("remote-last")).ToHaveTextAsync("Last call: search \"\", skip 20, take 20");
        // The first options are still there: a page is added, the list is not replaced.
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Argentina");

        await scroller.EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(60);
        await Assertions.Expect(Calls()).ToHaveTextAsync("Source calls: 3");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_load_more_button_loads_the_next_page_by_hand(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await menu.GetByRole(AriaRole.Button, new() { Name = "Load more" }).ClickAsync();

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(40);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_picked_option_keeps_its_label_when_a_later_search_drops_it_from_the_list(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Search(menu).PressSequentiallyAsync("jap");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Page.GetByTestId("remote-state")).ToHaveTextAsync("Selected: jp");
        var label = Trigger("remote-select").Locator("[data-slot=label]");
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "Japan");
        await Assertions.Expect(Trigger("remote-select")).ToBeFocusedAsync();

        await OpenList(menu, "remote-select");
        await Search(menu).PressSequentiallyAsync("fra");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("France");
        // Japan is no longer in the list; the field still names it.
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "Japan");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_arrow_keys_and_Enter_work_on_loaded_options(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await Trigger("remote-select").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(Search(menu)).ToBeFocusedAsync();
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        // Argentina is highlighted when the list loads, so two arrows land on the third option.
        await Assertions.Expect(Page.GetByTestId("remote-state")).ToHaveTextAsync("Selected: at");
        await Assertions.Expect(Trigger("remote-select").Locator("[data-slot=label]")).ToHaveAttributeAsync("data-label", "Austria");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Restored_values_are_named_by_ResolveSelected_and_MinLength_holds_back_short_texts(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-multiple");
        var label = Trigger("remote-multiple").Locator("[data-slot=label]");

        // Japan and Brazil were never listed: one call names them.
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "Japan, Brazil");
        await Assertions.Expect(Page.GetByTestId("remote-multiple-last")).ToHaveTextAsync("Last call: resolve jp, br");
        await Assertions.Expect(Calls("remote-multiple-requests")).ToHaveTextAsync("Source calls: 1");

        await OpenList(menu, "remote-multiple");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Assertions.Expect(Page.GetByTestId("remote-multiple-last")).ToHaveTextAsync("Last call: search \"\", cursor none, take 20");

        await Search(menu).PressSequentiallyAsync("j");
        await Assertions.Expect(menu.Locator("[data-slot=hint]")).ToHaveTextAsync("Type at least 2 characters to search.");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(0);
        await Page.WaitForTimeoutAsync(400);
        await Assertions.Expect(Calls("remote-multiple-requests")).ToHaveTextAsync("Source calls: 2");

        await Search(menu).PressSequentiallyAsync("a");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Options(menu).First).ToHaveAttributeAsync("aria-selected", "true");
        await Options(menu).First.ClickAsync();

        await Assertions.Expect(Page.GetByTestId("remote-multiple-state")).ToHaveTextAsync("Selected: br");
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "Brazil");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_cursor_is_handed_back_when_the_list_scrolls(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-multiple");
        await OpenList(menu, "remote-multiple");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await menu.Locator("[data-slot=scroller]").EvaluateAsync("e => { e.scrollTop = e.scrollHeight; }");

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(40);
        await Assertions.Expect(Page.GetByTestId("remote-multiple-last")).ToHaveTextAsync("Last call: search \"\", cursor 20, take 20");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_failing_source_shows_an_error_with_a_retry_button_and_recovers(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-error-select");
        await Page.GetByRole(AriaRole.Switch, new() { Name = "The source is down" }).ClickAsync();

        await OpenList(menu, "remote-error-select");

        var alert = menu.GetByRole(AriaRole.Alert);
        await Assertions.Expect(alert).ToContainTextAsync("The options could not be loaded.");
        await Assertions.Expect(Calls("remote-error-requests")).ToHaveTextAsync("Source calls: 1");
        var retry = alert.GetByRole(AriaRole.Button, new() { Name = "Retry" });
        await retry.ClickAsync();
        await Assertions.Expect(Calls("remote-error-requests")).ToHaveTextAsync("Source calls: 2");
        await Assertions.Expect(alert).ToBeVisibleAsync();

        // Nothing is highlighted, so Enter asks again too.
        await Search(menu).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Calls("remote-error-requests")).ToHaveTextAsync("Source calls: 3");
        await Assertions.Expect(alert).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Page.GetByRole(AriaRole.Switch, new() { Name = "The source is down" }).ClickAsync();
        await OpenList(menu, "remote-error-select");

        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Assertions.Expect(menu.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Open_lists_have_no_accessibility_violations_loading_loaded_and_failed_in_light_and_dark(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Assertions.Expect(menu.Locator("[data-slot=announcer]")).ToHaveTextAsync("Loading...");
        await AssertNoAccessibilityViolations();
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        await AssertNoAccessibilityViolations();
        await Dark();
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");

        var failing = Menu("remote-error-select");
        await Page.GetByRole(AriaRole.Switch, new() { Name = "The source is down" }).ClickAsync();
        await OpenList(failing, "remote-error-select");
        await Assertions.Expect(failing.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Open_lists_have_no_accessibility_violations_in_light(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-multiple");
        await OpenList(menu, "remote-multiple");
        await Search(menu).PressSequentiallyAsync("j");
        await Assertions.Expect(menu.Locator("[data-slot=hint]")).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();

        await Search(menu).PressSequentiallyAsync("a");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Statically_rendered_the_field_is_disabled_shows_the_given_values_and_keeps_that_after_an_enhanced_navigation()
    {
        await Open("ssr", "select-menu");
        var label = Trigger("remote-multiple").Locator("[data-slot=label]");

        await Assertions.Expect(Trigger("remote-multiple")).ToBeDisabledAsync();
        await Assertions.Expect(Trigger("remote-select")).ToBeDisabledAsync();
        // No runtime to ask the source: the raw values, no list, no request.
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "jp, br");
        await Assertions.Expect(Menu("remote-multiple")).Not.ToHaveAttributeAsync("data-remote", new Regex(".*"));

        await EnhancedReload();

        await Assertions.Expect(Trigger("remote-multiple")).ToBeDisabledAsync();
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "jp, br");
        await Assertions.Expect(Page.GetByTestId("remote-multiple-requests")).ToHaveTextAsync("Source calls: 0");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_value_named_by_Items_keeps_its_label_in_every_mode_after_the_browser_took_over(string mode)
    {
        await Open(mode, "select-menu");
        var label = Trigger("remote-named").Locator("[data-slot=label]");

        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "France");
        // The browser side paints on its own schedule: it must not write the raw value over the label the server rendered.
        await Page.WaitForTimeoutAsync(600);
        await Assertions.Expect(label).ToHaveAttributeAsync("data-label", "France");
        await Assertions.Expect(Menu("remote-named")).ToHaveAttributeAsync("data-known", new Regex("France"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Create_follows_the_options_that_came_back_not_the_ones_shown_when_the_key_was_pressed(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-create");
        await OpenList(menu, "remote-create");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);

        // Peru is not in the first page: while the answer is on its way, the text matches no option shown, so Create is offered...
        await Search(menu).PressSequentiallyAsync("peru");

        // ...and goes away when the answer brings Peru itself.
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Peru");
        await Assertions.Expect(menu.Locator("[data-slot=create]")).ToBeHiddenAsync();

        await Search(menu).FillAsync("zork");
        await Assertions.Expect(menu.Locator("[data-slot=create]")).ToHaveAttributeAsync("data-label", "Create \"zork\"");
        await Assertions.Expect(menu.Locator("[data-slot=create]")).ToBeVisibleAsync();
        await menu.Locator("[data-slot=create]").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("remote-create-state")).ToHaveTextAsync("Selected: zork");

        // Already a value: nothing is offered to create, and the list says there are no results instead of staying empty.
        await Search(menu).FillAsync("zork");
        await Assertions.Expect(menu.Locator("[data-slot=empty]")).ToHaveTextAsync("No results");
        await Assertions.Expect(menu.Locator("[data-slot=create]")).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_highlight_comes_back_when_the_text_is_the_one_already_answered(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Search(menu).PressSequentiallyAsync("ja");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Search(menu)).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));

        // A trailing space trims to the same text: nothing is asked, and the highlight must not be lost.
        await Page.Keyboard.PressAsync("Space");
        await Page.WaitForTimeoutAsync(600);
        await Assertions.Expect(Search(menu)).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));

        // A letter typed and deleted within the pause: the same.
        await Search(menu).FillAsync("ja");
        await Page.WaitForTimeoutAsync(600);
        await Page.Keyboard.PressAsync("p");
        await Page.Keyboard.PressAsync("Backspace");
        await Page.WaitForTimeoutAsync(700);
        await Assertions.Expect(Search(menu)).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));

        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Page.GetByTestId("remote-state")).ToHaveTextAsync("Selected: jp");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Reopening_keeps_the_previous_options_but_highlights_none_until_the_fresh_answer(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("remote-select");
        await OpenList(menu, "remote-select");
        await Search(menu).PressSequentiallyAsync("ja");
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");

        await Trigger("remote-select").ClickAsync();

        // The list of "ja" is still shown while the empty text is asked again: Enter cannot pick from it.
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(1);
        await Assertions.Expect(Search(menu)).Not.ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));
        await Assertions.Expect(Options(menu)).ToHaveCountAsync(20);
        await Assertions.Expect(Search(menu)).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task OptionsKey_changing_lists_the_options_of_the_new_source_when_the_list_opens_again(string mode)
    {
        await Open(mode, "select-menu");
        var menu = Menu("key-city");
        await OpenList(menu, "key-city");
        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Paris");
        await Assertions.Expect(Page.GetByTestId("key-last")).ToContainTextAsync("cities of fr");
        await Options(menu).First.ClickAsync();
        await Assertions.Expect(Page.GetByTestId("key-state")).ToHaveTextAsync("City: paris");

        await Trigger("key-country").ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Germany" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("key-state")).ToHaveTextAsync("City: none");
        await OpenList(menu, "key-city");

        await Assertions.Expect(Options(menu).First).ToHaveTextAsync("Berlin");
        await Assertions.Expect(Options(menu).Filter(new() { HasText = "Paris" })).ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByTestId("key-last")).ToContainTextAsync("cities of de");
    }
}
