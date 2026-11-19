using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blux.UI.Tests;

public class ChatTests : BluxTestContext
{
    // ---- ChatShimmer

    [Fact]
    public void Shimmer_renders_its_text_with_the_sweep_and_a_static_fallback_for_reduced_motion()
    {
        var cut = Render<BxChatShimmer>(p => p.Add(s => s.Text, "Thinking...").Add(s => s.Duration, 3).Add(s => s.Spread, 1));

        var span = cut.Find("span");
        Assert.Equal("Thinking...", span.TextContent);
        Assert.Equal("animation-duration:3s;background-size:150% 100%", span.GetAttribute("style"));
        Assert.Contains("motion-reduce:animate-none", span.ClassList);
        Assert.Contains("motion-reduce:bg-none", span.ClassList);
    }

    // ---- ChatAttachment

    [Fact]
    public void Attachment_shows_the_name_and_a_description_made_of_the_extension_and_the_size()
    {
        var cut = Render<BxChatAttachment>(p => p.Add(a => a.Name, "report.pdf").Add(a => a.FileSize, 248000L));

        Assert.Contains("report.pdf", cut.Markup);
        Assert.Equal("PDF · 242.2 KB", cut.Find("[data-slot=wrapper] span:nth-child(2)").TextContent);
        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("idle", cut.Find("[data-status]").GetAttribute("data-status"));
    }

    [Fact]
    public void Attachment_removable_has_a_named_button_that_calls_back()
    {
        var removed = 0;
        var cut = Render<BxChatAttachment>(p => p.Add(a => a.Name, "notes.txt").Add(a => a.Removable, true)
            .Add(a => a.OnRemove, (MouseEventArgs _) => removed++));

        cut.Find("button[aria-label='Remove notes.txt']").Click();

        Assert.Equal(1, removed);
    }

    [Fact]
    public void Attachment_removable_is_inert_until_the_page_is_interactive()
    {
        using var context = new StaticContext();
        var cut = context.Render<BxChatAttachment>(p => p.Add(a => a.Name, "notes.txt").Add(a => a.Removable, true)
            .Add(a => a.OnRemove, (MouseEventArgs _) => { }));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Attachment_image_gets_a_preview_and_a_thumbnail_keeps_its_name_for_screen_readers_only()
    {
        var cut = Render<BxChatAttachment>(p => p.Add(a => a.Name, "sunset.png").Add(a => a.Type, "image/png").Add(a => a.Url, "/sunset.png")
            .Add(a => a.Variant, ChatAttachmentVariant.Thumbnail));

        var image = cut.Find("img");
        Assert.Equal("/sunset.png", image.GetAttribute("src"));
        Assert.Equal("sunset.png", image.GetAttribute("alt"));
        Assert.Contains("sr-only", cut.Find("[data-slot=wrapper]").ClassList);
    }

    [Fact]
    public void Attachment_without_an_image_type_shows_the_file_icon_even_with_a_url()
    {
        var cut = Render<BxChatAttachment>(p => p.Add(a => a.Name, "a.pdf").Add(a => a.Type, "application/pdf").Add(a => a.Url, "/a.pdf"));

        Assert.Empty(cut.FindAll("img"));
        Assert.NotNull(cut.Find("svg"));
    }

    [Fact]
    public void Attachment_uploading_is_busy_and_says_so_and_error_shows_the_reason()
    {
        var uploading = Render<BxChatAttachment>(p => p.Add(a => a.Name, "big.zip").Add(a => a.Status, ChatAttachmentStatus.Uploading));
        var failed = Render<BxChatAttachment>(p => p.Add(a => a.Name, "bad.csv").Add(a => a.Status, ChatAttachmentStatus.Error)
            .Add(a => a.Description, "Too large"));

        var root = uploading.Find("[data-status=uploading]");
        Assert.Equal("true", root.GetAttribute("aria-busy"));
        Assert.Contains("(uploading)", uploading.Markup);
        Assert.NotNull(uploading.Find("span[data-bx-chat-shimmer]"));
        Assert.Contains("Too large", failed.Markup);
        Assert.Contains("(failed)", failed.Markup);
    }

    // ---- ChatMessage

    [Fact]
    public void Message_user_sits_on_the_right_with_a_soft_surface_and_is_named_after_its_author()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Role, ChatRole.User).Add(m => m.Text, "Hello"));

        var article = cut.Find("article");
        Assert.Equal("user", article.GetAttribute("data-role"));
        Assert.Equal("You", article.GetAttribute("aria-label"));
        Assert.Contains("justify-end", article.ClassList);
        Assert.Contains("bg-(--bx-color)/10", cut.Find("[data-bx-chat-content]").ClassList);
        Assert.Equal("Hello", cut.Find("[data-bx-chat-content] p").TextContent);
    }

    [Fact]
    public void Message_assistant_sits_on_the_left_with_no_surface()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Text, "Hi"));

        var article = cut.Find("article");
        Assert.Equal("assistant", article.GetAttribute("data-role"));
        Assert.Equal("Assistant", article.GetAttribute("aria-label"));
        Assert.Contains("justify-start", article.ClassList);
        Assert.DoesNotContain("bg-(--bx-color)/10", cut.Find("[data-bx-chat-content]").ClassList);
    }

    [Fact]
    public void Message_side_and_variant_override_the_defaults_of_the_role()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Role, ChatRole.User).Add(m => m.Side, ChatSide.Left)
            .Add(m => m.Variant, ChatMessageVariant.Solid).Add(m => m.Text, "x"));

        Assert.Contains("justify-start", cut.Find("article").ClassList);
        Assert.Contains("bg-(--bx-color)", cut.Find("[data-bx-chat-content]").ClassList);
    }

    [Fact]
    public void Message_streaming_is_marked_busy()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Streaming, true).Add(m => m.Text, "Par"));
        var done = Render<BxChatMessage>(p => p.Add(m => m.Text, "Done"));

        var article = cut.Find("article");
        Assert.Equal("true", article.GetAttribute("data-streaming"));
        Assert.Equal("true", article.GetAttribute("aria-busy"));
        Assert.False(done.Find("article").HasAttribute("data-streaming"));
        Assert.False(done.Find("article").HasAttribute("aria-busy"));
    }

    [Fact]
    public void Message_child_content_wins_over_text_and_is_not_escaped()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Text, "plain").Add(m => m.ChildContent, "<p><strong>bold</strong></p>"));

        Assert.Equal("bold", cut.Find("[data-bx-chat-content] strong").TextContent);
        Assert.DoesNotContain("plain", cut.Markup);
    }

    [Fact]
    public void Message_renders_avatar_header_files_and_actions_only_when_given()
    {
        var bare = Render<BxChatMessage>(p => p.Add(m => m.Text, "x"));
        var full = Render<BxChatMessage>(p => p.Add(m => m.Text, "x").Add(m => m.AvatarText, "B").Add(m => m.AvatarAlt, "Bot")
            .Add(m => m.Header, "Bot, 9:41").Add(m => m.Files, "<span>file</span>").Add(m => m.Actions, "<button>Copy</button>"));

        Assert.Empty(bare.FindAll("[data-slot=leading]"));
        Assert.Empty(bare.FindAll("[data-slot=header]"));
        Assert.Empty(bare.FindAll("[data-slot=files]"));
        Assert.Empty(bare.FindAll("[data-slot=actions]"));
        Assert.Equal("Bot", full.Find("[data-slot=leading] [role=img]").GetAttribute("aria-label"));
        Assert.Equal("Bot, 9:41", full.Find("[data-slot=header]").TextContent);
        Assert.Equal("file", full.Find("[data-slot=files]").TextContent);
        Assert.Equal("Copy", full.Find("[data-slot=actions] button").TextContent);
    }

    [Fact]
    public void Message_passes_class_and_attributes_to_the_article()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Class, "mt-4").AddUnmatched("data-testid", "m").Add(m => m.Text, "x"));

        var article = cut.Find("[data-testid=m]");
        Assert.Equal("article", article.LocalName);
        Assert.Contains("mt-4", article.ClassList);
    }

    // ---- ChatMessages

    [Fact]
    public void Messages_is_a_log_that_is_not_live_and_a_scrollable_region_a_keyboard_can_reach()
    {
        var cut = Render<BxChatMessages>(p => p.Add(m => m.ChildContent, "<p>one</p>"));

        var log = cut.Find("[role=log]");
        Assert.Equal("off", log.GetAttribute("aria-live"));
        Assert.Equal("0", log.GetAttribute("tabindex"));
        Assert.Equal("Conversation", log.GetAttribute("aria-label"));
        Assert.Contains("one", log.TextContent);
        Assert.Equal("idle", cut.Find("bx-chat-messages").GetAttribute("data-status"));
    }

    [Fact]
    public void Messages_has_a_polite_status_region_for_completed_answers_and_a_hidden_jump_button()
    {
        var cut = Render<BxChatMessages>();

        var announce = cut.Find("[data-bx-chat-announce]");
        Assert.Equal("status", announce.GetAttribute("role"));
        Assert.Equal("polite", announce.GetAttribute("aria-live"));
        Assert.Empty(announce.TextContent);
        var jump = cut.Find("button[data-bx-chat-jump]");
        Assert.True(jump.HasAttribute("hidden"));
        Assert.Contains("Jump to latest", jump.TextContent);
    }

    [Fact]
    public void Messages_without_auto_scroll_renders_no_jump_button()
    {
        var cut = Render<BxChatMessages>(p => p.Add(m => m.AutoScroll, false));

        Assert.Empty(cut.FindAll("[data-bx-chat-jump]"));
        Assert.Equal("false", cut.Find("bx-chat-messages").GetAttribute("data-auto-scroll"));
    }

    [Theory]
    [InlineData(ChatStatus.Idle, false)]
    [InlineData(ChatStatus.Submitted, true)]
    [InlineData(ChatStatus.Streaming, true)]
    [InlineData(ChatStatus.Error, false)]
    public void Messages_shows_the_typing_indicator_while_an_answer_is_pending_or_arriving(ChatStatus status, bool indicator)
    {
        var cut = Render<BxChatMessages>(p => p.Add(m => m.Status, status));

        Assert.Equal(indicator, cut.FindAll("[data-slot=indicator]").Count == 1);
        Assert.Equal(indicator ? "true" : null, cut.Find("[role=log]").GetAttribute("aria-busy"));
        if (indicator)
        {
            Assert.Equal("status", cut.Find("[data-slot=indicator]").GetAttribute("role"));
            Assert.Contains("Assistant is responding", cut.Find("[data-slot=indicator]").TextContent);
        }
    }

    [Fact]
    public void Messages_error_status_shows_an_alert_that_can_be_replaced()
    {
        var plain = Render<BxChatMessages>(p => p.Add(m => m.Status, ChatStatus.Error));
        var custom = Render<BxChatMessages>(p => p.Add(m => m.Status, ChatStatus.Error).Add(m => m.Error, "The model is down."));

        Assert.Equal("Something went wrong. Try again.", plain.Find("[role=alert]").TextContent.Trim());
        Assert.Equal("The model is down.", custom.Find("[role=alert]").TextContent.Trim());
    }

    [Fact]
    public void Messages_texts_come_from_the_strings()
    {
        using var localized = new LocalizedContext();
        var cut = localized.Render<BxChatMessages>(p => p.Add(m => m.Status, ChatStatus.Submitted));

        Assert.Equal("Conversation FR", cut.Find("[role=log]").GetAttribute("aria-label"));
        Assert.Contains("Reponse en cours", cut.Find("[data-slot=indicator]").TextContent);
    }

    // ---- ChatPrompt

    [Fact]
    public void Prompt_renders_a_named_textarea_that_submits_on_enter_and_grows()
    {
        var cut = Render<BxChatPrompt>();

        var textarea = cut.Find("textarea");
        Assert.Equal("Message", textarea.GetAttribute("aria-label"));
        Assert.Equal("Type a message...", textarea.GetAttribute("placeholder"));
        Assert.Equal("1", textarea.GetAttribute("rows"));
        Assert.True(textarea.HasAttribute("data-bx-chat-input"));
        Assert.Equal("send", textarea.GetAttribute("enterkeyhint"));
        var element = cut.Find("bx-chat-prompt");
        Assert.Equal("6", element.GetAttribute("data-max-rows"));
        Assert.False(element.HasAttribute("data-submit-on-enter"));
    }

    [Fact]
    public void Prompt_submit_on_enter_false_tells_the_element()
    {
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.SubmitOnEnter, false).Add(x => x.MaxRows, 3));

        Assert.Equal("false", cut.Find("bx-chat-prompt").GetAttribute("data-submit-on-enter"));
        Assert.Equal("3", cut.Find("bx-chat-prompt").GetAttribute("data-max-rows"));
    }

    [Fact]
    public void Prompt_with_a_form_name_posts_in_static_rendering_and_is_not_live()
    {
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.FormName, "chat").Add(x => x.OnSubmit, (string _) => { }));

        var form = cut.Find("form");
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal("Posted[prompt]", cut.Find("textarea").GetAttribute("name"));
        Assert.Equal("chat", cut.Find("input[type=hidden][name='Posted[__form]']").GetAttribute("value"));
        var element = cut.Find("bx-chat-prompt");
        Assert.True(element.HasAttribute("data-bx-post"));
    }

    [Fact]
    public void Prompt_without_a_form_name_does_not_post()
    {
        var cut = Render<BxChatPrompt>();

        Assert.False(cut.Find("form").HasAttribute("method"));
        Assert.False(cut.Find("bx-chat-prompt").HasAttribute("data-bx-post"));
        Assert.Empty(cut.FindAll("input[type=hidden]"));
    }

    [Fact]
    public void Prompt_is_live_only_when_interactive_and_listened_to()
    {
        var interactive = Render<BxChatPrompt>(p => p.Add(x => x.OnSubmit, (string _) => { }));
        var unbound = Render<BxChatPrompt>();

        Assert.True(interactive.Find("bx-chat-prompt").HasAttribute("data-bx-live"));
        Assert.False(unbound.Find("bx-chat-prompt").HasAttribute("data-bx-live"));
    }

    [Fact]
    public void Prompt_is_not_live_before_the_page_is_interactive()
    {
        using var context = new StaticContext();
        var cut = context.Render<BxChatPrompt>(p => p.Add(x => x.OnSubmit, (string _) => { }).Add(x => x.FormName, "chat"));

        Assert.False(cut.Find("bx-chat-prompt").HasAttribute("data-bx-live"));
        Assert.True(cut.Find("bx-chat-prompt").HasAttribute("data-bx-post"));
    }

    [Fact]
    public void Prompt_reports_the_text_on_the_sink_and_calls_back_once()
    {
        var received = new List<string>();
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.OnSubmit, (string text) => received.Add(text)));
        var sink = cut.Find("[data-bx-chat-sink]");
        Assert.Equal("0", sink.GetAttribute("data-bx-seq"));

        sink.TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "Hello there", Sequence = 1 });

        Assert.Equal(["Hello there"], received);
        Assert.Equal("1", cut.Find("[data-bx-chat-sink]").GetAttribute("data-bx-seq"));
    }

    [Fact]
    public void Prompt_unbound_renders_no_sequence_and_no_handler()
    {
        var cut = Render<BxChatPrompt>();

        Assert.False(cut.Find("[data-bx-chat-sink]").HasAttribute("data-bx-seq"));
    }

    [Fact]
    public void Prompt_ignores_the_change_events_of_components_nested_in_it()
    {
        var received = new List<string>();
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.OnSubmit, (string text) => received.Add(text)).Add(x => x.Footer, "<span id=\"inner\">x</span>"));

        // The prompt listens on its sink only: an event raised elsewhere in it never reaches OnSubmit.
        Assert.Single(cut.FindAll("[data-bx-chat-sink]"));
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("#inner").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "x", Sequence = 1 }));
        Assert.Empty(received);
    }

    [Fact]
    public void Prompt_does_not_deliver_while_busy_or_disabled()
    {
        var received = new List<string>();
        var busy = Render<BxChatPrompt>(p => p.Add(x => x.Status, ChatStatus.Streaming).Add(x => x.OnSubmit, (string text) => received.Add(text)));
        var disabled = Render<BxChatPrompt>(p => p.Add(x => x.Disabled, true).Add(x => x.OnSubmit, (string text) => received.Add(text)));

        busy.Find("[data-bx-chat-sink]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "x", Sequence = 1 });
        disabled.Find("[data-bx-chat-sink]").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "x", Sequence = 1 });

        Assert.Empty(received);
        Assert.True(busy.Find("bx-chat-prompt").HasAttribute("data-busy"));
        Assert.True(disabled.Find("bx-chat-prompt").HasAttribute("data-disabled"));
        Assert.True(disabled.Find("textarea").HasAttribute("disabled"));
    }

    [Fact]
    public void Prompt_attachments_leading_trailing_and_footer_slots_render_where_expected()
    {
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.Attachments, "<i>chip</i>").Add(x => x.Leading, "<b>lead</b>")
            .Add(x => x.Trailing, "<u>trail</u>").Add(x => x.Footer, "<em>foot</em>"));

        Assert.Equal("chip", cut.Find("[data-bx-chat-attachments] i").TextContent);
        Assert.Equal("lead", cut.Find("[data-slot=row] b").TextContent);
        Assert.Equal("trail", cut.Find("[data-slot=trailing] u").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=trailing] button"));
        Assert.Equal("foot", cut.Find("[data-slot=footer] em").TextContent);
    }

    [Fact]
    public void Prompt_without_attachments_or_footer_renders_neither()
    {
        var cut = Render<BxChatPrompt>();

        Assert.Empty(cut.FindAll("[data-bx-chat-attachments]"));
        Assert.Empty(cut.FindAll("[data-slot=footer]"));
        Assert.Single(cut.FindAll("[data-slot=trailing] button"));
    }

    [Fact]
    public void Prompt_value_is_the_initial_text()
    {
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.Value, "draft"));

        Assert.Equal("draft", cut.Find("textarea").GetAttribute("value"));
    }

    // ---- ChatPromptSubmit

    [Fact]
    public void Submit_idle_is_a_named_submit_button()
    {
        var cut = Render<BxChatPromptSubmit>();

        var button = cut.Find("button");
        Assert.Equal("submit", button.GetAttribute("type"));
        Assert.Equal("Send", button.GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(ChatStatus.Submitted, "Stop")]
    [InlineData(ChatStatus.Streaming, "Stop")]
    [InlineData(ChatStatus.Error, "Retry")]
    public void Submit_other_statuses_are_plain_buttons_that_stop_or_retry(ChatStatus status, string name)
    {
        var stopped = 0;
        var reloaded = 0;
        var cut = Render<BxChatPromptSubmit>(p => p.Add(s => s.Status, status)
            .Add(s => s.OnStop, () => stopped++).Add(s => s.OnReload, () => reloaded++));

        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal(name, button.GetAttribute("aria-label"));
        button.Click();
        Assert.Equal(status == ChatStatus.Error ? (0, 1) : (1, 0), (stopped, reloaded));
    }

    [Fact]
    public void Submit_follows_the_status_of_the_prompt_around_it()
    {
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.Status, ChatStatus.Streaming));

        var button = cut.Find("[data-slot=trailing] button");
        Assert.Equal("Stop", button.GetAttribute("aria-label"));
        Assert.Equal("streaming", button.GetAttribute("data-status"));
    }

    [Fact]
    public void Submit_stop_goes_through_the_prompt_callback()
    {
        var stopped = 0;
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.Status, ChatStatus.Streaming).Add(x => x.OnStop, () => stopped++));

        cut.Find("[data-slot=trailing] button").Click();

        Assert.Equal(1, stopped);
    }

    [Fact]
    public void Submit_disabled_prompt_disables_the_button()
    {
        var cut = Render<BxChatPrompt>(p => p.Add(x => x.Disabled, true));

        Assert.True(cut.Find("[data-slot=trailing] button").HasAttribute("disabled"));
    }

    // ---- ChatPalette

    [Fact]
    public void Palette_fills_its_parent_with_the_messages_above_the_pinned_prompt()
    {
        var cut = Render<BxChatPalette>(p => p.Add(x => x.ChildContent, "<p id=\"msgs\">m</p>").Add(x => x.Prompt, "<p id=\"prompt\">p</p>"));

        var root = cut.Find("div");
        Assert.Contains("size-full", root.ClassList);
        Assert.Equal("m", cut.Find("[data-slot=content] #msgs").TextContent);
        Assert.Equal("p", cut.Find("[data-slot=prompt] #prompt").TextContent);
        Assert.Contains("border-t", cut.Find("[data-slot=prompt]").ClassList);
    }

    [Fact]
    public void Palette_without_a_prompt_renders_no_prompt_area()
    {
        var cut = Render<BxChatPalette>(p => p.Add(x => x.ChildContent, "<p>m</p>"));

        Assert.Empty(cut.FindAll("[data-slot=prompt]"));
    }

    // ---- ChatReasoning

    [Fact]
    public void Reasoning_is_a_closed_disclosure_with_a_named_region()
    {
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.ChildContent, "<p>why</p>"));

        var element = cut.Find("bx-chat-disclosure");
        Assert.False(element.HasAttribute("data-bx-seq"));
        Assert.Equal("closed", element.GetAttribute("data-state"));
        var trigger = cut.Find("button[data-bx-disclosure-trigger]");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        var content = cut.Find("[data-bx-disclosure-content]");
        Assert.Equal(trigger.GetAttribute("aria-controls"), content.GetAttribute("id"));
        Assert.Equal("until-found", content.GetAttribute("hidden"));
        var region = cut.Find("[role=region]");
        Assert.Equal(trigger.GetAttribute("id"), region.GetAttribute("aria-labelledby"));
        Assert.Equal("0", region.GetAttribute("tabindex"));
        Assert.Contains("Thought", trigger.TextContent);
    }

    [Fact]
    public void Reasoning_streaming_shimmers_its_label_and_tells_the_element_to_open_it()
    {
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.Streaming, true).Add(r => r.AutoCloseDelay, 900));

        var element = cut.Find("bx-chat-disclosure");
        Assert.Equal("true", element.GetAttribute("data-streaming"));
        Assert.True(element.HasAttribute("data-auto-open"));
        Assert.Equal("900", element.GetAttribute("data-close-delay"));
        Assert.Equal("Thinking...", cut.Find("[data-bx-chat-shimmer]").TextContent);
    }

    [Fact]
    public void Reasoning_auto_open_false_leaves_the_opening_to_the_user()
    {
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.AutoOpen, false));

        Assert.False(cut.Find("bx-chat-disclosure").HasAttribute("data-auto-open"));
    }

    [Fact]
    public void Reasoning_label_says_how_long_it_took_once_done()
    {
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.Duration, 7));
        var named = Render<BxChatReasoning>(p => p.Add(r => r.Text, "Reasoning"));

        Assert.Contains("Thought for 7 seconds", cut.Find("button").TextContent);
        Assert.Contains("Reasoning", named.Find("button").TextContent);
    }

    [Fact]
    public void Reasoning_measures_the_duration_between_the_start_and_the_end_of_streaming()
    {
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.Streaming, true));

        cut.Render(p => p.Add(r => r.Streaming, false));

        Assert.Matches("Thought for [0-9]+ seconds", cut.Find("button").TextContent);
        Assert.Empty(cut.FindAll("[data-bx-chat-shimmer]"));
    }

    [Fact]
    public void Reasoning_bound_open_renders_the_sequence_first_and_follows_the_toggle()
    {
        var open = false;
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.Open, false).Add(r => r.OpenChanged, (bool value) => open = value));
        Assert.Equal("data-bx-seq", cut.Find("bx-chat-disclosure").Attributes.First().Name);

        cut.Find("bx-chat-disclosure").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 2 });

        Assert.True(open);
        Assert.Equal("2", cut.Find("bx-chat-disclosure").GetAttribute("data-bx-seq"));
        Assert.True(cut.Find("bx-chat-disclosure").HasAttribute("open"));
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Reasoning_disabled_disables_its_trigger()
    {
        var cut = Render<BxChatReasoning>(p => p.Add(r => r.Disabled, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Reasoning_chevron_position_follows_the_parameter()
    {
        var trailing = Render<BxChatReasoning>();
        var leading = Render<BxChatReasoning>(p => p.Add(r => r.Chevron, ChatChevron.Leading));

        Assert.Equal("svg", trailing.Find("button").LastElementChild!.LocalName);
        Assert.Equal("svg", leading.Find("button").FirstElementChild!.LocalName);
    }

    // ---- ChatTool

    [Fact]
    public void Tool_without_content_is_a_plain_line_and_with_content_a_disclosure()
    {
        var plain = Render<BxChatTool>(p => p.Add(t => t.Text, "Reading").Add(t => t.Suffix, "notes.txt"));
        var collapsible = Render<BxChatTool>(p => p.Add(t => t.Text, "Searched").Add(t => t.ChildContent, "<p>out</p>"));

        Assert.Empty(plain.FindAll("button"));
        Assert.Empty(plain.FindAll("bx-chat-disclosure"));
        Assert.Contains("Reading", plain.Markup);
        Assert.Contains("notes.txt", plain.Markup);
        Assert.NotNull(collapsible.Find("bx-chat-disclosure button[data-bx-disclosure-trigger]"));
        Assert.Equal("false", collapsible.Find("button").GetAttribute("aria-expanded"));
        Assert.Equal("until-found", collapsible.Find("[data-bx-disclosure-content]").GetAttribute("hidden"));
        Assert.Equal("out", collapsible.Find("[role=region] p").TextContent);
        Assert.False(collapsible.Find("bx-chat-disclosure").HasAttribute("data-auto-open"));
    }

    [Fact]
    public void Tool_loading_and_error_are_read_out_after_the_name()
    {
        var loading = Render<BxChatTool>(p => p.Add(t => t.Text, "Reading").Add(t => t.Loading, true));
        var failed = Render<BxChatTool>(p => p.Add(t => t.Text, "Calling").Add(t => t.Error, true));

        Assert.Contains("(running)", loading.Markup);
        Assert.Contains("(failed)", failed.Markup);
        Assert.NotNull(loading.Find("svg"));
        Assert.Contains("text-(--blux-error-text)", failed.Find("span.truncate").ClassList);
    }

    [Fact]
    public void Tool_streaming_shimmers_the_name()
    {
        var cut = Render<BxChatTool>(p => p.Add(t => t.Text, "Searching").Add(t => t.Streaming, true));

        Assert.Equal("Searching", cut.Find("[data-bx-chat-shimmer]").TextContent);
    }

    [Fact]
    public void Tool_actions_sit_outside_the_collapsible_content()
    {
        var cut = Render<BxChatTool>(p => p.Add(t => t.Text, "Delete").Add(t => t.ChildContent, "<p>args</p>").Add(t => t.Actions, "<button id=\"allow\">Allow</button>"));

        Assert.NotNull(cut.Find("[data-slot=actions] #allow"));
        Assert.Empty(cut.FindAll("[data-bx-disclosure-content] #allow"));
    }

    [Fact]
    public void Tool_card_variant_changes_the_surface()
    {
        var cut = Render<BxChatTool>(p => p.Add(t => t.Text, "x").Add(t => t.Variant, ChatToolVariant.Card).Add(t => t.ChildContent, "<p>y</p>"));

        Assert.Contains("ring-inset", cut.Find("bx-chat-disclosure").ClassList);
    }

    [Fact]
    public void Tool_bound_open_reports_and_follows_the_toggle()
    {
        var open = false;
        var cut = Render<BxChatTool>(p => p.Add(t => t.Text, "x").Add(t => t.ChildContent, "<p>y</p>").Add(t => t.OpenChanged, (bool value) => open = value));

        cut.Find("bx-chat-disclosure").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 1 });

        Assert.True(open);
        Assert.Equal("1", cut.Find("bx-chat-disclosure").GetAttribute("data-bx-seq"));
    }

    // ---- Theming

    [Fact]
    public void Ui_overrides_win_over_the_theme()
    {
        var cut = Render<BxChatMessage>(p => p.Add(m => m.Text, "x").Add(m => m.Ui, new ChatMessageUi { Content = "bg-red-500" }));

        Assert.Contains("bg-red-500", cut.Find("[data-bx-chat-content]").ClassList);
    }

    [Fact]
    public void Texts_of_the_family_can_be_localized()
    {
        using var localized = new LocalizedContext();
        var cut = localized.Render<BxChatPromptSubmit>();

        Assert.Equal("Envoyer", cut.Find("button").GetAttribute("aria-label"));
    }

    private sealed class LocalizedContext : BluxTestContext
    {
        public LocalizedContext() : base(o =>
        {
            o.Strings.ChatConversation = "Conversation FR";
            o.Strings.ChatResponding = "Reponse en cours";
            o.Strings.ChatSend = "Envoyer";
        })
        {
        }
    }

    private sealed class StaticContext : BluxTestContext
    {
        public StaticContext() : base(interactive: false)
        {
        }
    }
}
