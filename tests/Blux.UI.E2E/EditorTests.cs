using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>The "Editor" family: Editor, EditorToolbar, EditorDragHandle, EditorEmojiMenu, EditorMentionMenu and EditorSuggestionMenu.</summary>
public class EditorTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
        ["editor", "editor-toolbar", "editor-drag-handle", "editor-emoji-menu", "editor-mention-menu", "editor-suggestion-menu"];

    public static TheoryData<string, string> EverySlugInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var slug in Slugs)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(slug, mode);
        }
        return data;
    }

    /// <summary>Opens the editor page and waits until every editor on it is ready (the engine loads from its own file).</summary>
    private async Task OpenEditor(string mode, string slug = "editor")
    {
        await Open(mode, slug);
        await WaitForEditors();
    }

    private Task WaitForEditors() =>
        Page.WaitForFunctionAsync("() => document.querySelectorAll('bx-editor').length > 0 && [...document.querySelectorAll('bx-editor')].every((e) => e.hasAttribute('data-ready') && e.querySelector('.ProseMirror'))",
            null, new() { Timeout = 30_000 });

    private ILocator Surface(string testId) => Page.GetByTestId(testId).Locator(".ProseMirror");

    private async Task TypeAtEnd(string testId, string text)
    {
        await Surface(testId).ClickAsync();
        await Page.Keyboard.PressAsync("Control+End");
        await Page.Keyboard.TypeAsync(text);
    }

    private Task<string> Posted(string name) => Page.Locator($"input[type=hidden][name='{name}']").InputValueAsync();

    /// <summary>Runs an enhanced navigation to the same page and waits for Blazor to merge the response.</summary>
    private async Task EnhancedReload()
    {
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");
    }

    [Theory]
    [MemberData(nameof(EverySlugInEveryMode))]
    public async Task Editor_page_has_no_accessibility_violations(string slug, string mode)
    {
        await OpenEditor(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    // ---- Loading

    [Fact]
    public async Task A_page_without_an_editor_never_requests_the_editor_bundle()
    {
        var requested = new List<string>();
        Page.Request += (_, request) => requested.Add(request.Url);

        await Open("ssr", "button");
        await Page.WaitForTimeoutAsync(1500);

        Assert.DoesNotContain(requested, url => url.Contains("blux-editor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_page_with_an_editor_requests_the_editor_bundle_once()
    {
        var requested = new List<string>();
        Page.Request += (_, request) => requested.Add(request.Url);

        await OpenEditor("ssr");

        Assert.Single(requested, url => url.Contains("blux-editor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Static_page_shows_the_value_read_only_until_the_editor_is_ready_and_when_it_cannot_load()
    {
        await Page.RouteAsync("**/blux-editor*.js", route => route.AbortAsync());
        await Open("ssr", "editor");

        var editor = Page.GetByTestId("editor");
        await Assertions.Expect(editor.Locator("[data-slot=fallback]")).ToBeVisibleAsync();
        // Without a configured sanitizer the read-only view shows the text, never the posted markup.
        await Assertions.Expect(editor.Locator("[data-slot=fallback]")).ToContainTextAsync("Hello Blux.");
        await Assertions.Expect(editor.Locator("[data-slot=fallback] strong")).ToHaveCountAsync(0);
        await Assertions.Expect(editor.Locator(".ProseMirror")).ToHaveCountAsync(0);
        await Assertions.Expect(editor).ToHaveAttributeAsync("data-error", "");
        // The value still posts without the engine.
        await Assertions.Expect(editor.Locator("input[type=hidden]")).ToHaveValueAsync("<p>Hello <strong>Blux</strong>.</p><p>Second paragraph.</p>");
    }

    [Fact]
    public async Task Editor_replaces_the_fallback_once_ready_and_names_the_text_area_by_the_label()
    {
        await OpenEditor("ssr");
        var editor = Page.GetByTestId("editor");

        await Assertions.Expect(editor.Locator("[data-slot=fallback]")).ToBeHiddenAsync();
        var surface = editor.Locator(".ProseMirror");
        await Assertions.Expect(surface).ToHaveAttributeAsync("role", "textbox");
        await Assertions.Expect(surface).ToHaveAttributeAsync("aria-multiline", "true");
        await Assertions.Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Release notes" })).ToBeVisibleAsync();
    }

    // ---- Typing and formatting

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_updates_the_bound_html_after_a_pause(string mode)
    {
        await OpenEditor(mode);
        var html = Page.GetByTestId("editor-html");
        await Assertions.Expect(html).ToContainTextAsync("Second paragraph.");

        await TypeAtEnd("editor", " added");

        await Assertions.Expect(html).ToContainTextAsync("<p>Second paragraph. added</p>");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bold_from_the_toolbar_and_from_the_shortcut_updates_the_bound_html(string mode)
    {
        await OpenEditor(mode);
        var editor = Page.GetByTestId("editor");
        var bold = editor.GetByRole(AriaRole.Button, new() { Name = "Bold" });
        var html = Page.GetByTestId("editor-html");

        await Surface("editor").ClickAsync();
        await Page.Keyboard.PressAsync("Control+a");
        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
        await bold.ClickAsync();

        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(html).ToContainTextAsync("<p><strong>Hello Blux.</strong></p>");

        await Page.Keyboard.PressAsync("Control+b");

        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
        await Assertions.Expect(html).Not.ToContainTextAsync("<strong>");
    }

    [Fact]
    public async Task Toolbar_buttons_follow_the_selection_and_undo_is_only_enabled_after_a_change()
    {
        await OpenEditor("ssr");
        var editor = Page.GetByTestId("editor");
        var bold = editor.GetByRole(AriaRole.Button, new() { Name = "Bold" });
        var undo = editor.GetByRole(AriaRole.Button, new() { Name = "Undo" });
        await Assertions.Expect(undo).ToBeDisabledAsync();

        await editor.Locator(".ProseMirror strong").DblClickAsync();

        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Italic" })).ToHaveAttributeAsync("aria-pressed", "false");

        await bold.ClickAsync();
        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
        await Assertions.Expect(undo).ToBeEnabledAsync();
        await undo.ClickAsync();
        await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Fact]
    public async Task Toolbar_is_one_tab_stop_with_arrow_key_navigation()
    {
        await OpenEditor("ssr");
        var toolbar = Page.GetByTestId("editor").GetByRole(AriaRole.Toolbar);
        var stops = toolbar.Locator("button[tabindex='0']:not([disabled])");
        await Assertions.Expect(stops).ToHaveCountAsync(1);

        await Surface("editor").ClickAsync();
        await Page.Keyboard.PressAsync("Alt+F10");
        var bold = toolbar.GetByRole(AriaRole.Button, new() { Name = "Bold" });
        await Assertions.Expect(bold).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Italic" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Redo" })).ToBeDisabledAsync();
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Link" })).ToBeFocusedAsync();
        await Assertions.Expect(stops).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(bold).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Surface("editor")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Link_button_asks_for_an_address_and_sets_the_link()
    {
        await OpenEditor("ssr", "editor");
        await Surface("editor-form").ClickAsync();
        await Page.Keyboard.PressAsync("Control+a");
        var toolbar = Page.GetByTestId("editor-form").GetByRole(AriaRole.Toolbar);

        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Link", Exact = true }).ClickAsync();
        var address = toolbar.GetByRole(AriaRole.Textbox, new() { Name = "Link address" });
        await Assertions.Expect(address).ToBeFocusedAsync();
        await address.FillAsync("https://example.com");
        await address.PressAsync("Enter");

        await Assertions.Expect(Surface("editor-form").Locator("a")).ToHaveAttributeAsync("href", "https://example.com");
        await Assertions.Expect(Surface("editor-form")).ToBeFocusedAsync();
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Link", Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    // ---- Floating toolbar, read only

    [Fact]
    public async Task Bubble_toolbar_shows_over_a_selection_and_formats_it()
    {
        await OpenEditor("ssr");
        var bubble = Page.GetByTestId("editor-bubble").GetByRole(AriaRole.Toolbar);
        await Assertions.Expect(bubble).ToBeHiddenAsync();

        await Surface("editor-bubble").ClickAsync();
        await Page.Keyboard.PressAsync("Control+a");

        await Assertions.Expect(bubble).ToBeVisibleAsync();
        await bubble.GetByRole(AriaRole.Button, new() { Name = "Italic" }).ClickAsync();
        await Assertions.Expect(Surface("editor-bubble").Locator("em")).ToHaveCountAsync(1);
        Assert.Contains("<em>", await Posted("summary"));

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(bubble).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Read_only_editor_cannot_be_edited_but_still_posts()
    {
        await OpenEditor("ssr");
        var surface = Surface("editor-readonly");
        await Assertions.Expect(surface).ToHaveAttributeAsync("contenteditable", "false");
        await Assertions.Expect(surface).ToHaveAttributeAsync("aria-readonly", "true");

        await surface.ClickAsync();
        await Page.Keyboard.TypeAsync("nope");

        await Assertions.Expect(surface).Not.ToContainTextAsync("nope");
    }

    // ---- Forms

    [Fact]
    public async Task Static_form_posts_the_html_and_the_server_shows_it()
    {
        await OpenEditor("ssr");
        await TypeAtEnd("editor-form", " plus");
        Assert.Equal("<p>Draft plus</p>", await Posted("Model.Notes"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("saved")).ToHaveTextAsync("Saved: <p>Draft plus</p>");
    }

    [Fact]
    public async Task Static_form_validates_an_empty_editor_and_marks_it_invalid()
    {
        await OpenEditor("ssr");
        await Surface("editor-form").ClickAsync();
        await Page.Keyboard.PressAsync("Control+a");
        await Page.Keyboard.PressAsync("Delete");
        Assert.Equal("", await Posted("Model.Notes"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Assertions.Expect(Page.Locator("[data-slot=error]")).ToHaveTextAsync("Write something first.");
        await Page.WaitForFunctionAsync("() => document.querySelector('[data-testid=editor-form] .ProseMirror')?.getAttribute('aria-invalid') === 'true'");
        await Assertions.Expect(Surface("editor-form")).ToHaveAttributeAsync("aria-describedby", new Regex("error"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Interactive_form_posts_the_html_to_the_model(string mode)
    {
        await OpenEditor(mode);
        await TypeAtEnd("editor-form", " plus");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("saved")).ToHaveTextAsync("Saved: <p>Draft plus</p>", new() { Timeout = 10_000 });
    }

    // ---- Unbound, enhanced navigation

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Unbound_editor_keeps_its_text_in_the_browser_and_posts_it(string mode)
    {
        await OpenEditor(mode);

        await TypeAtEnd("editor-bubble", " more");

        Assert.Equal("<p>Select a few of these words, then use the floating toolbar or press Ctrl+B. more</p>", await Posted("summary"));
        await Assertions.Expect(Surface("editor-bubble")).ToContainTextAsync("more");
    }

    [Fact]
    public async Task Editor_survives_an_enhanced_navigation_with_its_text_and_keeps_working()
    {
        await OpenEditor("ssr");
        await TypeAtEnd("editor-bubble", " kept");

        await EnhancedReload();

        var surface = Surface("editor-bubble");
        await Assertions.Expect(surface).ToContainTextAsync("kept");
        await Assertions.Expect(Page.GetByTestId("editor-bubble").Locator("[data-slot=fallback]")).ToBeHiddenAsync();
        await TypeAtEnd("editor-bubble", "!");
        Assert.EndsWith("kept!</p>", await Posted("summary"));
        var bold = Page.GetByTestId("editor").GetByRole(AriaRole.Button, new() { Name = "Bold" });
        await Surface("editor").ClickAsync();
        await Assertions.Expect(bold).ToBeEnabledAsync();
    }

    // ---- Menus

    [Fact]
    public async Task Slash_menu_filters_with_the_keyboard_and_inserts_a_heading()
    {
        await OpenEditor("ssr", "editor-suggestion-menu");
        var surface = Surface("editor-slash");
        await surface.ClickAsync();

        await Page.Keyboard.TypeAsync("/");
        var list = Page.GetByRole(AriaRole.Listbox, new() { Name = "Commands" });
        await Assertions.Expect(list).ToBeVisibleAsync();
        await Assertions.Expect(surface).ToHaveAttributeAsync("role", "combobox");
        await Assertions.Expect(surface).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(5);

        await Page.Keyboard.TypeAsync("sec");
        await Assertions.Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        var option = list.GetByRole(AriaRole.Option, new() { Name = "Section" });
        await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", "true");
        Assert.Equal(await option.GetAttributeAsync("id"), await surface.GetAttributeAsync("aria-activedescendant"));
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(surface.Locator("h2")).ToHaveCountAsync(1);
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(surface).ToHaveAttributeAsync("role", "textbox");
        await Page.Keyboard.TypeAsync("Title");
        // TipTap keeps an empty paragraph after a last heading, to have somewhere to type next.
        Assert.StartsWith("<h2>Title</h2>", await Posted("slash"));
    }

    [Fact]
    public async Task Slash_menu_moves_with_arrows_closes_with_escape_and_shows_no_results()
    {
        await OpenEditor("ssr", "editor-suggestion-menu");
        var surface = Surface("editor-slash");
        await surface.ClickAsync();
        await Page.Keyboard.TypeAsync("/");
        var list = Page.GetByRole(AriaRole.Listbox, new() { Name = "Commands" });
        var options = list.GetByRole(AriaRole.Option);
        await Assertions.Expect(options.First).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(options.Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(options.Last).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.TypeAsync("zzz");
        await Assertions.Expect(Page.GetByText("No results").First).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(surface).ToHaveAttributeAsync("role", "textbox");
    }

    [Fact]
    public async Task Slash_menu_chooses_with_the_pointer()
    {
        await OpenEditor("ssr", "editor-suggestion-menu");
        var surface = Surface("editor-slash");
        await surface.ClickAsync();
        await Page.Keyboard.TypeAsync("/");

        await Page.GetByRole(AriaRole.Option, new() { Name = "Bullets" }).ClickAsync();

        await Assertions.Expect(surface.Locator("ul > li")).ToHaveCountAsync(1);
        await Assertions.Expect(surface).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Mention_menu_inserts_a_mention_and_the_bound_html_has_its_id(string mode)
    {
        await OpenEditor(mode, "editor-mention-menu");
        var surface = Surface("editor-mention");
        await surface.ClickAsync();

        await Page.Keyboard.TypeAsync("Hi @ad");
        var list = Page.GetByRole(AriaRole.Listbox, new() { Name = "Mentions" });
        await Assertions.Expect(list.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(surface.Locator(".bx-mention")).ToHaveTextAsync("@Ada Lovelace");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByTestId("mention-html")).ToContainTextAsync("data-id=\"ada\"");
    }

    [Fact]
    public async Task Mention_menu_lists_every_item_after_a_bare_at_sign()
    {
        await OpenEditor("ssr", "editor-mention-menu");
        await Surface("editor-mention").ClickAsync();

        await Page.Keyboard.TypeAsync("@");

        await Assertions.Expect(Page.GetByRole(AriaRole.Listbox, new() { Name = "Mentions" }).GetByRole(AriaRole.Option)).ToHaveCountAsync(4);
        // An address is not a mention.
        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.TypeAsync(" me@example.com");
        await Assertions.Expect(Page.GetByRole(AriaRole.Listbox, new() { Name = "Mentions" })).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Emoji_menu_opens_after_a_colon_and_a_letter_and_inserts_the_emoji()
    {
        await OpenEditor("ssr", "editor-emoji-menu");
        var surface = Surface("editor-emoji");
        await surface.ClickAsync();
        var list = Page.GetByRole(AriaRole.Listbox, new() { Name = "Emoji" });

        await Page.Keyboard.TypeAsync("at 12:30 ");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Page.Keyboard.TypeAsync(":");
        await Assertions.Expect(list).ToBeHiddenAsync();
        await Page.Keyboard.TypeAsync("smi");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(surface).ToContainTextAsync("😄");
        Assert.Contains("😄", await Posted("emoji"));
    }

    // ---- Blocks

    [Fact]
    public async Task Alt_and_the_arrow_keys_move_the_block_with_the_cursor()
    {
        await OpenEditor("ssr", "editor-drag-handle");
        var surface = Surface("editor-drag");
        await surface.Locator("p", new() { HasText = "Two" }).ClickAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        Assert.Equal("<p>Two</p><p>One</p><p>Three</p>", await Posted("blocks"));
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");

        Assert.Equal("<p>One</p><p>Three</p><p>Two</p>", await Posted("blocks"));
        // Past the last block nothing moves.
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        Assert.Equal("<p>One</p><p>Three</p><p>Two</p>", await Posted("blocks"));
        await Assertions.Expect(surface).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Drag_handle_follows_the_pointer_and_moves_blocks_with_the_arrow_keys()
    {
        await OpenEditor("ssr", "editor-drag-handle");
        var handle = Page.GetByTestId("editor-drag").GetByRole(AriaRole.Button, new() { Name = "Drag to move block" });
        await Assertions.Expect(handle).ToBeHiddenAsync();

        await Surface("editor-drag").Locator("p", new() { HasText = "Two" }).HoverAsync();

        await Assertions.Expect(handle).ToBeVisibleAsync();
        await handle.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Assertions.Expect(handle).ToBeFocusedAsync();
        Assert.Equal("<p>One</p><p>Three</p><p>Two</p>", await Posted("blocks"));
    }

    [Fact]
    public async Task Drag_handle_drags_a_block_to_a_new_place()
    {
        await OpenEditor("ssr", "editor-drag-handle");
        var surface = Surface("editor-drag");
        var handle = Page.GetByTestId("editor-drag").GetByRole(AriaRole.Button, new() { Name = "Drag to move block" });
        await surface.EvaluateAsync("e => e.scrollIntoView({ block: 'center' })");
        await surface.Locator("p", new() { HasText = "One" }).HoverAsync();
        await Assertions.Expect(handle).ToBeVisibleAsync();

        var from = (await handle.BoundingBoxAsync())!;
        var target = (await surface.Locator("p", new() { HasText = "Three" }).BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(target.X + 30, target.Y + target.Height - 2, new() { Steps = 12 });
        await Page.Mouse.UpAsync();

        await Page.WaitForFunctionAsync("() => document.querySelector('input[name=blocks]').value !== '<p>One</p><p>Two</p><p>Three</p>'");
        Assert.Equal("<p>Two</p><p>Three</p><p>One</p>", await Posted("blocks"));
    }

    [Fact]
    public async Task Toolbar_move_buttons_move_the_block_with_the_cursor()
    {
        await OpenEditor("ssr", "editor-toolbar");
        var toolbar = Page.GetByTestId("editor-toolbar").GetByRole(AriaRole.Toolbar);
        await Surface("editor-toolbar").Locator("p", new() { HasText = "First" }).ClickAsync();
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Move block up" })).ToBeDisabledAsync();

        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Move block down" }).ClickAsync();

        Assert.Equal("<p>Second block</p><p>First block</p>", await Posted("custom"));
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Move block down" })).ToBeDisabledAsync();
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Move block up" })).ToBeEnabledAsync();
    }
}
