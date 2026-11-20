using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>The "Chat" family: ChatMessage, ChatMessages, ChatPrompt, ChatPromptSubmit, ChatPalette, ChatAttachment, ChatReasoning, ChatShimmer and ChatTool.</summary>
public class ChatTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "chat-message", "chat-messages", "chat-prompt", "chat-prompt-submit", "chat-palette",
        "chat-attachment", "chat-reasoning", "chat-shimmer", "chat-tool",
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
    public async Task Chat_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    /// <summary>Runs an enhanced navigation to the same page and waits for Blazor to merge the response.</summary>
    private async Task EnhancedReload()
    {
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");
    }

    private ILocator Textarea(string testId) => Page.GetByTestId(testId).GetByRole(AriaRole.Textbox);

    private static async Task<double> DistanceFromBottom(ILocator viewport) =>
        await viewport.EvaluateAsync<double>("v => v.scrollHeight - v.scrollTop - v.clientHeight");

    private async Task WaitAtBottom(ILocator viewport)
    {
        await Page.WaitForFunctionAsync("v => v.scrollHeight - v.scrollTop - v.clientHeight <= 2", await viewport.ElementHandleAsync());
    }

    // ---- ChatPrompt

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Enter_sends_the_message_and_shows_the_reply_in_every_mode(string mode)
    {
        await Open(mode, "chat-prompt");
        var textarea = Textarea("chat-prompt");

        await textarea.FillAsync("How does streaming work?");
        await textarea.PressAsync("Enter");

        await Assertions.Expect(Page.GetByTestId("chat-prompt-sent")).ToContainTextAsync("How does streaming work?", new() { Timeout = 15_000 });
        await Assertions.Expect(Page.GetByTestId("chat-prompt-reply")).ToBeVisibleAsync();
        await Assertions.Expect(textarea).ToHaveValueAsync("");
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Static_form_post_comes_back_to_the_same_page_with_the_reply()
    {
        await Open("ssr", "chat-prompt");

        await Textarea("chat-prompt").FillAsync("Posted without a runtime");
        await Page.GetByTestId("chat-prompt").GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("chat-prompt-sent")).ToHaveTextAsync("Posted without a runtime", new() { Timeout = 15_000 });
        await Assertions.Expect(Page.GetByTestId("chat-prompt-reply")).ToBeVisibleAsync();
        Assert.EndsWith("/ssr/components/chat-prompt", new Uri(Page.Url).AbsolutePath);
        // Alive again after the post: Enter still sends.
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-chat-prompt')");
        await Textarea("chat-prompt").FillAsync("Second");
        await Textarea("chat-prompt").PressAsync("Enter");
        await Assertions.Expect(Page.GetByTestId("chat-prompt-sent")).ToHaveTextAsync("Second", new() { Timeout = 15_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Shift_enter_adds_a_line_and_the_field_grows_then_scrolls(string mode)
    {
        await Open(mode, "chat-prompt");
        var textarea = Textarea("chat-prompt");
        var initial = await textarea.EvaluateAsync<double>("e => e.getBoundingClientRect().height");

        await textarea.FillAsync("first");
        await textarea.PressAsync("Shift+Enter");
        await textarea.PressAsync("KeyX");

        Assert.Equal("first\nx", await textarea.InputValueAsync());
        await Assertions.Expect(Page.GetByTestId("chat-prompt-sent")).ToHaveCountAsync(0);
        var two = await textarea.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        Assert.True(two > initial, $"two lines ({two}) should be taller than one ({initial})");

        for (var i = 0; i < 12; i++) await textarea.PressAsync("Shift+Enter");

        var capped = await textarea.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        Assert.True(capped > two);
        Assert.True(await textarea.EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"));
        Assert.Equal("auto", await textarea.EvaluateAsync<string>("e => getComputedStyle(e).overflowY"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Empty_messages_and_ime_composition_do_not_send(string mode)
    {
        await Open(mode, "chat-prompt");
        var textarea = Textarea("chat-prompt");

        await textarea.PressAsync("Enter");
        await textarea.FillAsync("   ");
        await textarea.PressAsync("Enter");
        // The Enter that confirms an IME composition is not a submit, however the browser reports it.
        await textarea.FillAsync("kanji");
        var prevented = await textarea.EvaluateAsync<bool[]>(
            """
            el => [
              new KeyboardEvent('keydown', { key: 'Enter', isComposing: true, bubbles: true, cancelable: true }),
              new KeyboardEvent('keydown', { key: 'Enter', keyCode: 229, bubbles: true, cancelable: true }),
            ].map((e) => { el.dispatchEvent(e); return e.defaultPrevented; })
            """);

        Assert.Equal([false, false], prevented);
        await Page.WaitForTimeoutAsync(400);
        await Assertions.Expect(Page.GetByTestId("chat-prompt-sent")).ToHaveCountAsync(0);
        Assert.Equal("kanji", await textarea.InputValueAsync());
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Attachments_can_be_removed_and_a_message_may_be_sent_without_text(string mode)
    {
        await Open(mode, "chat-prompt");
        var prompt = Page.GetByTestId("chat-prompt-attachments");
        var last = Page.GetByTestId("chat-prompt-attachments-last");
        var textarea = Textarea("chat-prompt-attachments");

        await prompt.GetByRole(AriaRole.Button, new() { Name = "Remove budget.xlsx" }).ClickAsync();
        await Assertions.Expect(prompt.GetByText("budget.xlsx")).ToHaveCountAsync(0);
        await textarea.PressAsync("Enter");

        await Assertions.Expect(last).ToHaveTextAsync("Sent \"\" with 1 file(s)");
        // Nothing attached any more and nothing typed: nothing is sent.
        await textarea.PressAsync("Enter");
        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(last).ToHaveTextAsync("Sent \"\" with 1 file(s)");
    }

    // ---- ChatMessages: scrolling

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Messages_start_at_the_bottom_and_jump_to_latest_appears_when_scrolled_up(string mode)
    {
        await Open(mode, "chat-messages");
        var root = Page.GetByTestId("chat-messages-demo");
        var viewport = root.Locator("[data-bx-chat-viewport]");
        var jump = root.Locator("[data-bx-chat-jump]");

        await WaitAtBottom(viewport);
        Assert.True(await viewport.EvaluateAsync<bool>("v => v.scrollHeight > v.clientHeight"));
        await Assertions.Expect(jump).ToBeHiddenAsync();

        await viewport.EvaluateAsync("v => { v.scrollTop = 0; }");
        await Assertions.Expect(jump).ToBeVisibleAsync();
        await Assertions.Expect(jump).ToHaveTextAsync("Jump to latest");
        await jump.ClickAsync();

        await WaitAtBottom(viewport);
        await Assertions.Expect(jump).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Messages_follow_new_messages_at_the_bottom_but_not_after_scrolling_up(string mode)
    {
        await Open(mode, "chat-messages");
        var root = Page.GetByTestId("chat-messages-demo");
        var viewport = root.Locator("[data-bx-chat-viewport]");
        var jump = root.Locator("[data-bx-chat-jump]");
        await WaitAtBottom(viewport);

        await Page.GetByTestId("chat-messages-reply").ClickAsync();
        await Assertions.Expect(root.Locator("[data-bx-chat-content]").GetByText("New answer")).ToHaveCountAsync(1);
        await WaitAtBottom(viewport);

        await viewport.EvaluateAsync("v => { v.scrollTop = 0; }");
        await Assertions.Expect(jump).ToBeVisibleAsync();
        await Page.GetByTestId("chat-messages-reply").ClickAsync();
        await Assertions.Expect(root.Locator("[data-bx-chat-content]").GetByText("New answer")).ToHaveCountAsync(2);
        await Page.WaitForTimeoutAsync(300);
        Assert.True(await viewport.EvaluateAsync<double>("v => v.scrollTop") < 40, "an answer must not pull a user who scrolled up");
        await Assertions.Expect(jump).ToBeVisibleAsync();

        // Sending a message of one's own follows again.
        await Page.GetByTestId("chat-messages-add").ClickAsync();
        await WaitAtBottom(viewport);
        await Assertions.Expect(jump).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_indicator_and_error_follow_the_status_and_a_finished_answer_is_announced_once(string mode)
    {
        await Open(mode, "chat-messages");
        var root = Page.GetByTestId("chat-messages-demo");
        var announce = root.Locator("[data-bx-chat-announce]");
        var log = root.GetByRole(AriaRole.Log);

        await Assertions.Expect(log).ToHaveAttributeAsync("aria-live", "off");
        await Page.GetByTestId("chat-messages-status-streaming").ClickAsync();
        await Assertions.Expect(root.Locator("[data-slot=indicator]")).ToBeVisibleAsync();
        await Assertions.Expect(root.Locator("[data-slot=typing-status]")).ToContainTextAsync("Assistant is responding");
        await Page.GetByTestId("chat-messages-status-error").ClickAsync();
        await Assertions.Expect(root.GetByRole(AriaRole.Alert)).ToContainTextAsync("Something went wrong");
        await Assertions.Expect(root.Locator("[data-slot=indicator]")).ToHaveCountAsync(0);
        await Page.GetByTestId("chat-messages-status-idle").ClickAsync();
        await Assertions.Expect(announce).ToBeEmptyAsync();

        await Page.GetByTestId("chat-messages-reply").ClickAsync();

        await Assertions.Expect(announce).ToHaveTextAsync(new Regex("^New answer [0-9]+$"));
    }

    // ---- ChatPalette: streaming

    private ILocator Assistant => Page.GetByTestId("chat-palette-messages").Locator("[data-bx-chat-message][data-role=assistant]");

    /// <summary>The text of the latest answer, without the avatar's initials.</summary>
    // A prompt that gets the longest canned reply (about five seconds of streaming), for tests that act while it streams.
    private static readonly string LongPrompt = Enumerable.Range(1, 50).Select(i => $"Tell me more {i}")
        .First(p => Blux.Playground.Client.Examples.ChatDemo.Reply(p) == Blux.Playground.Client.Examples.ChatDemo.LongestReply);

    private ILocator LastAnswer =>Assistant.Last.Locator("[data-bx-chat-content]");

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Streaming_reply_goes_submitted_then_streaming_then_idle_and_is_announced_when_complete(string mode)
    {
        await Open(mode, "chat-palette");
        var messages = Page.GetByTestId("chat-palette-messages");
        var prompt = Page.GetByTestId("chat-palette-prompt");
        var announce = messages.Locator("[data-bx-chat-announce]");
        await Assertions.Expect(prompt.GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync();
        await Assertions.Expect(announce).ToBeEmptyAsync();

        // The submitted state lasts 400 ms: record the statuses as they change instead of polling for them.
        await messages.EvaluateAsync("""
            el => {
              window.__statuses = [el.getAttribute('data-status')];
              window.__indicatorWhenSubmitted = false;
              new MutationObserver(() => {
                const s = el.getAttribute('data-status');
                if (s === 'submitted' && el.querySelector('[data-slot=indicator]')) window.__indicatorWhenSubmitted = true;
                if (window.__statuses.at(-1) !== s) window.__statuses.push(s);
              }).observe(el, { attributes: true, subtree: true, childList: true });
            }
            """);
        await Textarea("chat-palette-prompt").FillAsync(LongPrompt);
        await Textarea("chat-palette-prompt").PressAsync("Enter");

        await Assertions.Expect(messages).ToHaveAttributeAsync("data-status", "streaming", new() { Timeout = 10_000 });
        Assert.Equal(["idle", "submitted", "streaming"], await Page.EvaluateAsync<string[]>("() => window.__statuses.slice(0, 3)"));
        Assert.True(await Page.EvaluateAsync<bool>("() => window.__indicatorWhenSubmitted"), "the typing indicator shows while the answer is submitted");
        await Assertions.Expect(prompt.GetByRole(AriaRole.Button, new() { Name = "Stop" })).ToBeVisibleAsync();
        await Assertions.Expect(Assistant.Last).ToHaveAttributeAsync("data-streaming", "true");
        await Assertions.Expect(Assistant.Last).ToHaveAttributeAsync("aria-busy", "true");
        // Sending again while the answer streams does nothing. Checked right after the busy state is seen: the demo
        // stream lasts about two seconds, and a slow browser could otherwise send after it ended, which is allowed.
        await Textarea("chat-palette-prompt").FillAsync("Another one");
        await Assertions.Expect(prompt).ToHaveAttributeAsync("data-busy", new Regex(".*"));
        await Textarea("chat-palette-prompt").PressAsync("Enter");
        await Assertions.Expect(messages.Locator("[data-bx-chat-message][data-role=user]")).ToHaveCountAsync(1);
        // Tokens arrive, but none of them is announced.
        await Assertions.Expect(announce).ToBeEmptyAsync();

        await Assertions.Expect(messages).ToHaveAttributeAsync("data-status", "idle", new() { Timeout = 30_000 });

        await Assertions.Expect(Assistant.Last).Not.ToHaveAttributeAsync("data-streaming", "true");
        await Assertions.Expect(prompt.GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync();
        var reply = Regex.Replace(await LastAnswer.InnerTextAsync(), @"\s+", " ").Trim();
        Assert.True(reply.Length > 100, reply);
        await Assertions.Expect(announce).ToHaveTextAsync(reply, new() { Timeout = 5_000 });
        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Stop_ends_the_stream_and_keeps_what_arrived(string mode)
    {
        await Open(mode, "chat-palette");
        var messages = Page.GetByTestId("chat-palette-messages");
        var prompt = Page.GetByTestId("chat-palette-prompt");
        await Textarea("chat-palette-prompt").FillAsync(LongPrompt);
        await Textarea("chat-palette-prompt").PressAsync("Enter");
        await Assertions.Expect(messages).ToHaveAttributeAsync("data-status", "streaming", new() { Timeout = 10_000 });
        await Assertions.Expect(LastAnswer).Not.ToHaveTextAsync("", new() { Timeout = 5_000 });

        await prompt.GetByRole(AriaRole.Button, new() { Name = "Stop" }).ClickAsync();

        await Assertions.Expect(messages).ToHaveAttributeAsync("data-status", "idle");
        await Assertions.Expect(prompt.GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync();
        await Assertions.Expect(Assistant.Last).Not.ToHaveAttributeAsync("data-streaming", "true");
        var stopped = await LastAnswer.InnerTextAsync();
        await Page.WaitForTimeoutAsync(500);
        Assert.Equal(stopped, await LastAnswer.InnerTextAsync());
        // Compared with the whole canned reply, not a length: under load the click can reach .NET a few tokens late.
        var full = Blux.Playground.Client.Examples.ChatDemo.LongestReply.Trim();
        Assert.True(Regex.Replace(stopped, @"\s+", " ").Trim().Length < full.Length, $"the stream should have been cut short: {stopped}");
    }

    [Fact]
    public async Task Static_palette_posts_the_prompt_and_renders_the_whole_answer()
    {
        await Open("ssr", "chat-palette");

        await Textarea("chat-palette-prompt").FillAsync("Static please");
        await Textarea("chat-palette-prompt").PressAsync("Enter");

        await Assertions.Expect(Page.GetByTestId("chat-palette-messages").Locator("[data-bx-chat-message][data-role=user]")).ToHaveTextAsync("Static please", new() { Timeout = 30_000 });
        await Assertions.Expect(Assistant).ToHaveCountAsync(2);
        Assert.True((await LastAnswer.InnerTextAsync()).Length > 100);
        await Assertions.Expect(Page.GetByTestId("chat-palette-messages")).ToHaveAttributeAsync("data-status", "idle");
        await Assertions.Expect(Assistant.Last).Not.ToHaveAttributeAsync("data-streaming", "true");
    }

    // ---- Enhanced navigation

    [Fact]
    public async Task Prompt_and_messages_keep_working_after_an_enhanced_navigation()
    {
        await Open("ssr", "chat-messages");
        var root = Page.GetByTestId("chat-messages-demo");
        var viewport = root.Locator("[data-bx-chat-viewport]");
        await WaitAtBottom(viewport);

        await EnhancedReload();

        await WaitAtBottom(viewport);
        await viewport.EvaluateAsync("v => { v.scrollTop = 0; }");
        await Assertions.Expect(root.Locator("[data-bx-chat-jump]")).ToBeVisibleAsync();
        await root.Locator("[data-bx-chat-jump]").ClickAsync();
        await WaitAtBottom(viewport);

        await Open("ssr", "chat-prompt");
        await EnhancedReload();
        var textarea = Textarea("chat-prompt");
        var initial = await textarea.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        await textarea.FillAsync("a");
        await textarea.PressAsync("Shift+Enter");
        await textarea.PressAsync("KeyB");
        Assert.True(await textarea.EvaluateAsync<double>("e => e.getBoundingClientRect().height") > initial);
        await textarea.PressAsync("Enter");
        await Assertions.Expect(Page.GetByTestId("chat-prompt-reply")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    // ---- ChatReasoning and ChatTool

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Reasoning_opens_while_streaming_and_closes_after_it_with_its_duration(string mode)
    {
        await Open(mode, "chat-reasoning");
        var block = Page.GetByTestId("chat-reasoning-live");
        var trigger = block.GetByRole(AriaRole.Button);
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.GetByTestId("chat-reasoning-run").ClickAsync();

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true", new() { Timeout = 10_000 });
        await Assertions.Expect(trigger).ToContainTextAsync("Thinking...");
        await Assertions.Expect(block.Locator("[data-bx-chat-shimmer]")).ToBeVisibleAsync();
        await Assertions.Expect(block.GetByRole(AriaRole.Region)).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToContainTextAsync(new Regex("Thought for [0-9]+ seconds"), new() { Timeout = 30_000 });
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false", new() { Timeout = 5_000 });
        await Assertions.Expect(block.Locator("[data-bx-disclosure-content]")).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Reasoning_toggled_by_the_user_while_streaming_is_not_closed_automatically(string mode)
    {
        await Open(mode, "chat-reasoning");
        var block = Page.GetByTestId("chat-reasoning-live");
        var trigger = block.GetByRole(AriaRole.Button);
        await Page.GetByTestId("chat-reasoning-run").ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true", new() { Timeout = 10_000 });

        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.ClickAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        await Assertions.Expect(trigger).ToContainTextAsync(new Regex("Thought for [0-9]+ seconds"), new() { Timeout = 30_000 });
        await Page.WaitForTimeoutAsync(1500);
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Reasoning_and_tool_toggle_in_every_mode_and_stay_accessible_open(string mode)
    {
        await Open(mode, "chat-reasoning");
        var done = Page.GetByTestId("chat-reasoning-done");
        await Assertions.Expect(done.GetByRole(AriaRole.Button)).ToContainTextAsync("Thought for 12 seconds");

        await done.GetByRole(AriaRole.Button).ClickAsync();

        await Assertions.Expect(done.GetByRole(AriaRole.Button)).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(done.GetByRole(AriaRole.Region)).ToBeVisibleAsync();
        await AssertNoAccessibilityViolations();
        await done.GetByRole(AriaRole.Button).ClickAsync();
        await Assertions.Expect(done.GetByRole(AriaRole.Region)).ToBeHiddenAsync();

        await Open(mode, "chat-tool");
        var tool = Page.GetByTestId("chat-tool-done");
        await tool.GetByRole(AriaRole.Button).ClickAsync();
        await Assertions.Expect(tool.GetByRole(AriaRole.Button)).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(tool.GetByRole(AriaRole.Group)).ToContainTextAsync("3 results");
        await Page.GetByTestId("chat-tool-failed").GetByRole(AriaRole.Button).ClickAsync();
        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_reasoning_reports_every_toggle_to_dotnet(string mode)
    {
        await Open(mode, "chat-reasoning");
        var block = Page.GetByTestId("chat-reasoning-bound");
        var state = Page.GetByTestId("chat-reasoning-bound-state");
        await Assertions.Expect(state).ToHaveTextAsync("Open: false");

        await block.GetByRole(AriaRole.Button).ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("Open: true");
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveAttributeAsync("aria-expanded", "true");

        await block.GetByRole(AriaRole.Button).ClickAsync();
        await Assertions.Expect(state).ToHaveTextAsync("Open: false");
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Tool_approval_actions_call_dotnet(string mode)
    {
        await Open(mode, "chat-tool");

        await Page.GetByTestId("chat-tool-allow").ClickAsync();

        await Assertions.Expect(Page.GetByTestId("chat-tool-answer")).ToHaveTextAsync("Allowed");
    }

    // ---- Remaining components

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Attachment_and_message_actions_work_when_interactive(string mode)
    {
        await Open(mode, "chat-attachment");
        await Page.GetByTestId("chat-attachment-removable").GetByRole(AriaRole.Button, new() { Name = "Remove notes.txt" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("chat-attachment-removed")).ToHaveTextAsync("Removed notes.txt");

        await Open(mode, "chat-message");
        await Page.GetByTestId("chat-message-copy").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("chat-message-note")).ToHaveTextAsync("Copied");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Prompt_submit_stop_and_retry_call_back_when_interactive(string mode)
    {
        await Open(mode, "chat-prompt-submit");
        var note = Page.GetByTestId("chat-submit-event");

        await Page.GetByTestId("chat-submit-streaming").ClickAsync();
        await Assertions.Expect(note).ToHaveTextAsync("Clicked: stop");
        await Page.GetByTestId("chat-submit-error").ClickAsync();
        await Assertions.Expect(note).ToHaveTextAsync("Clicked: retry");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Shimmer_is_static_text_with_reduced_motion(string mode)
    {
        await Open(mode, "chat-shimmer");
        var shimmer = Page.GetByTestId("chat-shimmer");
        await Assertions.Expect(shimmer).ToHaveCSSAsync("animation-name", "bx-chat-shimmer");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Assertions.Expect(shimmer).ToHaveCSSAsync("animation-name", "none");
        await Assertions.Expect(shimmer).Not.ToHaveCSSAsync("color", "rgba(0, 0, 0, 0)");
    }


    // ---- Review findings (waves C and D)

    [Fact]
    public async Task Streamed_text_does_not_rescan_every_message_for_each_token()
    {
        await Open("ssr", "chat-messages");
        var root = Page.GetByTestId("chat-messages-demo");
        await Page.EvaluateAsync("""
            () => {
                const host = document.querySelector('[data-testid=chat-messages-demo]').closest('bx-chat-messages') ?? document.querySelector('bx-chat-messages');
                window.__scans = 0;
                const original = host.querySelectorAll.bind(host);
                host.querySelectorAll = (selector) => {
                    if (selector.includes('data-role="assistant"')) window.__scans++;
                    return original(selector);
                };
            }
            """);

        // Twenty tokens arrive, one per task, in the text of the message being streamed.
        await root.Locator("[data-bx-chat-message][data-role=assistant] [data-bx-chat-content]").Last.EvaluateAsync("""
            async (content) => {
                const walker = document.createTreeWalker(content, NodeFilter.SHOW_TEXT);
                const text = walker.nextNode();
                for (let i = 0; i < 20; i++) {
                    text.data += ' token';
                    await new Promise((resolve) => setTimeout(resolve, 0));
                }
            }
            """);

        Assert.True(await Page.EvaluateAsync<int>("() => window.__scans") <= 1, "every token rescanned all the assistant messages");
    }

    // ---- Review findings (second review of waves C and D)

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Prompt_button_keeps_the_focus_when_the_status_changes(string mode)
    {
        await Open(mode, "chat-palette");
        var prompt = Page.GetByTestId("chat-palette-prompt");
        await Textarea("chat-palette-prompt").FillAsync("Tell me about streaming");
        var send = prompt.GetByRole(AriaRole.Button, new() { Name = "Send" });
        await send.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");

        // Send becomes Stop while the answer streams, and then Send again: always the same element, so the focus stays on it.
        var stop = prompt.GetByRole(AriaRole.Button, new() { Name = "Stop" });
        await Assertions.Expect(stop).ToBeFocusedAsync();
        await Assertions.Expect(prompt.GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeFocusedAsync(new() { Timeout = 30_000 });
    }
}
