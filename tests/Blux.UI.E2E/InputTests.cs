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
}
