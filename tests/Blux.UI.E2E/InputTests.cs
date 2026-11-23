namespace Blux.UI.E2E;

/// <summary>BxInput Debounce, Enter and ClearOnEscape: done by a script in the browser, so one report per pause in every interactive mode.</summary>
public class InputTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator Debounced => Page.GetByTestId("debounced");
    private ILocator Plain => Page.GetByTestId("plain");
    private ILocator State => Page.GetByTestId("debounced-state");
    private ILocator Reports => Page.GetByTestId("debounced-count");

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_a_word_reports_one_value_after_the_pause(string mode)
    {
        await Open(mode, "input");

        await Debounced.PressSequentiallyAsync("hello", new() { Delay = 40 });

        await Assertions.Expect(State).ToHaveTextAsync("Value: hello");
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 1");
        // Nothing else arrives later: neither a second report for the same text nor one on blur.
        await Debounced.BlurAsync();
        await Page.WaitForTimeoutAsync(800);
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 1");
        await Assertions.Expect(Debounced).ToHaveValueAsync("hello");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Nothing_is_reported_before_the_pause_and_each_pause_reports_once(string mode)
    {
        await Open(mode, "input");

        await Debounced.PressSequentiallyAsync("ab", new() { Delay = 40 });
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 0");
        await Assertions.Expect(State).ToHaveTextAsync("Value: ab");
        await Debounced.PressSequentiallyAsync("cd", new() { Delay = 40 });

        await Assertions.Expect(State).ToHaveTextAsync("Value: abcd");
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 2");
        await Page.WaitForTimeoutAsync(800);
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 2");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Enter_reports_at_once_and_cancels_the_pending_report(string mode)
    {
        await Open(mode, "input");
        await Debounced.FocusAsync();
        await Page.Keyboard.TypeAsync("quick");

        await Page.Keyboard.PressAsync("Enter");

        // Well before the 400 ms pause could have ended.
        await Assertions.Expect(State).ToHaveTextAsync("Value: quick", new() { Timeout = 300 });
        await Page.WaitForTimeoutAsync(800);
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 1");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Escape_clears_the_field_and_reports_the_empty_value(string mode)
    {
        await Open(mode, "input");
        await Debounced.PressSequentiallyAsync("gone", new() { Delay = 40 });
        await Assertions.Expect(State).ToHaveTextAsync("Value: gone");

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(Debounced).ToHaveValueAsync("");
        await Assertions.Expect(State).ToHaveTextAsync("Value:", new() { Timeout = 300 });
        await Assertions.Expect(State).Not.ToContainTextAsync("gone");
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 2");
        await Debounced.PressSequentiallyAsync("x", new() { Delay = 40 });
        await Assertions.Expect(State).ToHaveTextAsync("Value: x");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Escape_before_the_pause_ends_drops_the_text_without_a_stale_report(string mode)
    {
        await Open(mode, "input");
        await Debounced.FocusAsync();
        await Page.Keyboard.TypeAsync("never sent");

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(Debounced).ToHaveValueAsync("");
        await Page.WaitForTimeoutAsync(800);
        await Assertions.Expect(State).Not.ToContainTextAsync("never");
        await Assertions.Expect(Debounced).ToHaveValueAsync("");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Escape_clears_the_text_in_every_mode_including_static(string mode)
    {
        await Open(mode, "input");
        await Debounced.FillAsync("text");

        await Debounced.PressAsync("Escape");

        await Assertions.Expect(Debounced).ToHaveValueAsync("");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Without_Debounce_the_input_reports_on_change_as_before(string mode)
    {
        await Open(mode, "input");

        await Plain.FillAsync("typed");
        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(Page.GetByTestId("plain-state")).ToHaveTextAsync("Plain:");

        await Plain.PressAsync("Enter");

        await Assertions.Expect(Page.GetByTestId("plain-state")).ToHaveTextAsync("Plain: typed");
        await Assertions.Expect(Plain).Not.ToHaveAttributeAsync("data-bx-debounce", new System.Text.RegularExpressions.Regex(".*"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_render_older_than_the_last_report_does_not_overwrite_the_text(string mode)
    {
        await Open(mode, "input");
        await Debounced.FillAsync("abc");
        await Assertions.Expect(State).ToHaveTextAsync("Value: abc");
        var sequence = await Debounced.GetAttributeAsync("data-bx-seq");
        Assert.NotNull(sequence);

        // What Blazor does when a render computed after an earlier report arrives late: it writes the old value
        // into the input. The sequence of that render is lower than the last one reported.
        await Debounced.EvaluateAsync("(el, seq) => { el.setAttribute('data-bx-seq', String(Number(seq) - 1)); el.value = 'old'; }", sequence);
        await Assertions.Expect(Debounced).ToHaveValueAsync("abc");

        // A render that has seen the last report is applied.
        await Debounced.EvaluateAsync("(el, seq) => { el.setAttribute('data-bx-seq', seq); el.value = 'new'; }", sequence);
        await Assertions.Expect(Debounced).ToHaveValueAsync("new");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_render_cannot_overwrite_text_typed_but_not_yet_reported_and_the_caret_stays(string mode)
    {
        await Open(mode, "input");
        await Debounced.FillAsync("abc");
        await Assertions.Expect(State).ToHaveTextAsync("Value: abc");

        await Debounced.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Page.Keyboard.TypeAsync("X");
        // The value of a render of the previous report arrives while "X" waits for its pause.
        await Debounced.EvaluateAsync("el => { el.value = 'abc'; }");
        await Assertions.Expect(Debounced).ToHaveValueAsync("Xabc");
        await Page.Keyboard.TypeAsync("Y");

        await Assertions.Expect(State).ToHaveTextAsync("Value: XYabc");
        await Assertions.Expect(Debounced).ToHaveValueAsync("XYabc");
    }

    private ILocator EscapeOnly => Page.GetByTestId("escape-only");

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Text_deleted_back_to_the_starting_value_does_not_leave_the_field_stuck(string mode)
    {
        await Open(mode, "input");
        await EscapeOnly.FocusAsync();
        await Page.Keyboard.TypeAsync("ab");
        await Page.Keyboard.PressAsync("Backspace");
        await Page.Keyboard.PressAsync("Backspace");

        // No native change event fires for text that is back to what it was; leaving the field must release it.
        await Page.Keyboard.PressAsync("Tab");
        await Page.GetByTestId("escape-only-set").ClickAsync();

        await Assertions.Expect(EscapeOnly).ToHaveValueAsync("from parent");
    }

    [Fact]
    public async Task A_composition_is_not_reported_until_it_ends()
    {
        Assert.SkipUnless(BrowserName == "chromium", "IME composition is driven through the Chromium protocol.");
        await Open("server", "input");
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await Debounced.FocusAsync();

        await cdp.SendAsync("Input.imeSetComposition", new Dictionary<string, object> { ["text"] = "ni", ["selectionStart"] = 2, ["selectionEnd"] = 2 });
        await Page.WaitForTimeoutAsync(900);
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 0");

        await cdp.SendAsync("Input.insertText", new Dictionary<string, object> { ["text"] = "你" });

        await Assertions.Expect(State).ToHaveTextAsync("Value: 你");
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 1");
    }

    [Fact]
    public async Task Leaving_the_field_in_the_middle_of_a_composition_reports_it_once_it_ends()
    {
        Assert.SkipUnless(BrowserName == "chromium", "IME composition is driven through the Chromium protocol.");
        await Open("server", "input");
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await EscapeOnly.FocusAsync();

        await cdp.SendAsync("Input.imeSetComposition", new Dictionary<string, object> { ["text"] = "kana", ["selectionStart"] = 4, ["selectionEnd"] = 4 });
        await Page.GetByTestId("plain").FocusAsync();

        await Assertions.Expect(Page.GetByTestId("escape-only-state")).ToHaveTextAsync("Escape only: kana");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task SetValueAsync_replaces_pending_text_keeps_focus_and_reports_nothing_stale(string mode)
    {
        await Open(mode, "input");
        // The pause is held open (the script reads the attribute when a key is typed), so "d" is still waiting when the reset is clicked
        // however slowly the click is made; the report of "abc" is made by Enter.
        await Debounced.EvaluateAsync("e => e.setAttribute('data-bx-debounce', '600000')");
        await Debounced.PressSequentiallyAsync("abc", new() { Delay = 20 });
        await Debounced.PressAsync("Enter");
        await Assertions.Expect(State).ToHaveTextAsync("Value: abc");
        await Debounced.PressSequentiallyAsync("d", new() { Delay = 20 });

        // "d" is still waiting for its pause.
        await Page.GetByTestId("reset").ClickAsync();

        await Assertions.Expect(Debounced).ToHaveValueAsync("");
        await Assertions.Expect(Debounced).ToBeFocusedAsync();
        await Assertions.Expect(State).ToHaveTextAsync("Value:");
        await Assertions.Expect(State).Not.ToContainTextAsync("abc");
        await Page.WaitForTimeoutAsync(900);
        await Assertions.Expect(Debounced).ToHaveValueAsync("");
        await Assertions.Expect(State).Not.ToContainTextAsync("abcd");
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 2");

        // Typing goes on from the new sequence.
        await Debounced.EvaluateAsync("e => e.setAttribute('data-bx-debounce', '400')");
        await Debounced.PressSequentiallyAsync("z", new() { Delay = 20 });
        await Assertions.Expect(State).ToHaveTextAsync("Value: z");
        await Assertions.Expect(Reports).ToHaveTextAsync("Reports: 3");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_Escape_that_clears_is_consumed_and_the_next_one_closes_the_dialog(string mode)
    {
        await Open(mode, "input");
        await Page.GetByTestId("escape-trigger").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Search" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        var input = Page.GetByTestId("escape-input");
        await input.FillAsync("abc");
        await Assertions.Expect(Page.GetByTestId("escape-state")).ToContainTextAsync("text: abc");

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(input).ToHaveValueAsync("");
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByTestId("escape-state")).ToHaveTextAsync("Dialog: open, text:");
        // Blazor's @onkeydown on an ancestor never saw that Escape.
        await Assertions.Expect(Page.GetByTestId("escape-keys")).ToHaveTextAsync("Escape keydowns seen by the page: 0");

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByTestId("escape-state")).ToContainTextAsync("Dialog: closed");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Escape_does_not_clear_a_read_only_input(string mode)
    {
        await Open(mode, "input");
        await Page.GetByTestId("escape-trigger").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Search" });
        var locked = Page.GetByTestId("escape-readonly");
        await locked.FocusAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(locked).ToHaveValueAsync("locked");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task In_an_EditForm_the_debounced_input_validates_after_the_pause_and_carries_the_field_classes(string mode)
    {
        await Open(mode, "input");
        var email = Page.GetByTestId("email");

        // The pause is held open while the absence of a report is checked (a check made within 400 ms is a race on a loaded machine), then the field is left.
        await email.EvaluateAsync("e => e.setAttribute('data-bx-debounce', '600000')");
        await email.PressSequentiallyAsync("nope", new() { Delay = 30 });
        await Assertions.Expect(email).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(Page.Locator("form .validation-message")).ToBeHiddenAsync();
        await email.PressAsync("Tab");
        await email.EvaluateAsync("e => e.setAttribute('data-bx-debounce', '400')");
        await Assertions.Expect(Page.Locator("form .validation-message")).ToHaveTextAsync("Not an email address.");
        await Assertions.Expect(email).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(email).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bmodified\b.*\binvalid\b"));

        await email.FillAsync("ada@example.com");

        await Assertions.Expect(Page.Locator("form .validation-message")).ToBeHiddenAsync();
        await Assertions.Expect(email).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bmodified\b.*\bvalid\b"));
        await Assertions.Expect(email).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Fact]
    public async Task A_render_delayed_in_transit_does_not_overwrite_what_was_typed_meanwhile()
    {
        // Every message from the Blazor Server circuit is delayed once the test says so: the render of the first report
        // arrives after the second report was sent.
        await Page.AddInitScriptAsync(@"(() => {
            const Native = window.WebSocket;
            window.__bxDelay = 0;
            const wrap = (self, fn) => (event) => { const d = window.__bxDelay; if (d) setTimeout(() => fn.call(self, event), d); else fn.call(self, event); };
            window.WebSocket = class extends Native {
                set onmessage(fn) { super.onmessage = fn ? wrap(this, fn) : null; }
                get onmessage() { return super.onmessage; }
                addEventListener(type, fn, options) { super.addEventListener(type, type === 'message' ? wrap(this, fn) : fn, options); }
            };
        })();");
        await Open("server", "input");
        await Page.EvaluateAsync("() => { window.__bxDelay = 900; }");

        await Debounced.PressSequentiallyAsync("a", new() { Delay = 20 });
        await Page.WaitForTimeoutAsync(550); // report 1 is sent at 400 ms; its render is in transit until about 1300 ms
        await Debounced.PressSequentiallyAsync("b", new() { Delay = 20 });
        var seen = new HashSet<string>();
        for (var i = 0; i < 30; i++)
        {
            seen.Add(await Debounced.InputValueAsync());
            await Page.WaitForTimeoutAsync(100);
        }

        Assert.Equal(["ab"], seen);
        await Assertions.Expect(State).ToHaveTextAsync("Value: ab", new() { Timeout = 10_000 });
    }

    [Theory]
    [InlineData("server")]
    [InlineData("auto")]
    public async Task The_router_moves_the_focus_to_the_heading_after_the_page_turns_interactive(string mode)
    {
        // Why Open also waits for the heading to hold the focus: with the render batches (and the focus call that follows the first one)
        // held back, a field focused as soon as the page reports itself interactive loses the focus when the call arrives.
        var settled = await DelayRenderBatches(Page, 400);
        await Page.GotoAsync($"/{mode}/components/input-menu");
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var field = Page.GetByRole(AriaRole.Combobox, new() { Name = "Assign to" });
        await field.FocusAsync();
        await settled();

        await Page.WaitForFunctionAsync("() => document.activeElement === document.querySelector('h1')");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("auto")]
    public async Task Text_typed_right_after_Open_survives_late_renders(string mode)
    {
        var settled = await DelayRenderBatches(Page, 400);
        await Open(mode, "input-menu");
        var field = Page.GetByRole(AriaRole.Combobox, new() { Name = "Assign to" });

        await field.FocusAsync();
        await Page.Keyboard.TypeAsync("docs");
        await settled();

        await Assertions.Expect(field).ToHaveValueAsync("docs");
        await Assertions.Expect(field).ToBeFocusedAsync();
    }
}
