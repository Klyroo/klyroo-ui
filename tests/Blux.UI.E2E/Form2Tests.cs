namespace Blux.UI.E2E;

/// <summary>The "Form" family: SelectMenu, InputMenu, InputNumber, InputTags, PinInput, Slider, CheckboxGroup, Toggle, ToggleGroup, Listbox, InputRating, Form.</summary>
public class Form2Tests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "select-menu", "input-menu", "input-number", "input-tags", "pin-input", "slider",
        "checkbox-group", "toggle", "toggle-group", "listbox", "input-rating", "form",
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
    public async Task Page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task SelectMenu_is_operable_with_the_keyboard(string mode)
    {
        await Open(mode, "select-menu");
        var trigger = Page.GetByRole(AriaRole.Combobox, new() { Name = "Framework" });

        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var search = Page.GetByRole(AriaRole.Combobox, new() { Name = "Search" }).First;
        await Assertions.Expect(search).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("v");
        await Assertions.Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Blazor" })).ToBeHiddenAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(trigger.Locator("[data-slot=label]")).ToHaveAttributeAsync("data-label", "Vue");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task SelectMenu_multiple_toggles_values_and_creates_one(string mode)
    {
        await Open(mode, "select-menu");
        var trigger = Page.GetByRole(AriaRole.Combobox, new() { Name = "Languages" });

        await trigger.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Rust" }).ClickAsync();
        await Page.Keyboard.TypeAsync("Zig");
        await Page.GetByRole(AriaRole.Option, new() { Name = "Create \"Zig\"" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("multiple-state")).ToHaveTextAsync("Selected: csharp, rust, Zig");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputMenu_filters_and_picks_a_suggestion_with_the_keyboard(string mode)
    {
        await Open(mode, "input-menu");
        var field = Page.GetByRole(AriaRole.Combobox, new() { Name = "Country (a suggestion is required)" });

        await field.FocusAsync();
        await Page.Keyboard.TypeAsync("ger");
        await Assertions.Expect(field).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Page.GetByRole(AriaRole.Option, new() { Name = "France" })).ToBeHiddenAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(field).ToHaveValueAsync("Germany");
        await Assertions.Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Listbox_selects_with_arrows_and_space(string mode)
    {
        await Open(mode, "listbox");
        var listbox = Page.GetByRole(AriaRole.Listbox, new() { Name = "Fruit" });

        await listbox.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Space");

        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Banana" })).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("c");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Cherry" })).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Banana" })).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ToggleGroup_moves_focus_with_arrows_and_presses_with_space(string mode)
    {
        await Open(mode, "toggle-group");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Text alignment" });

        await group.GetByRole(AriaRole.Button, new() { Name = "Left" }).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(group.GetByRole(AriaRole.Button, new() { Name = "Center" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");

        await Assertions.Expect(group.GetByRole(AriaRole.Button, new() { Name = "Center" })).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(group.GetByRole(AriaRole.Button, new() { Name = "Left" })).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Toggle_flips_without_a_binding(string mode)
    {
        await Open(mode, "toggle");
        var italic = Page.GetByRole(AriaRole.Button, new() { Name = "Italic" });

        await italic.ClickAsync();

        await Assertions.Expect(italic).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task PinInput_paste_fills_every_box_and_advances(string mode)
    {
        await Open(mode, "pin-input");
        var cells = Page.GetByRole(AriaRole.Group, new() { Name = "Verification code" }).Locator("[data-slot=cell]");

        await cells.First.FocusAsync();
        await cells.First.EvaluateAsync(
            "el => { const data = new DataTransfer(); data.setData('text', '12-34 56'); el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true })); }");

        for (var i = 0; i < 6; i++) await Assertions.Expect(cells.Nth(i)).ToHaveValueAsync("123456"[i].ToString());
        await Assertions.Expect(cells.Nth(5)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(cells.Nth(5)).ToHaveValueAsync("");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task PinInput_typing_advances_and_ignores_letters_when_numeric(string mode)
    {
        await Open(mode, "pin-input");
        var cells = Page.GetByRole(AriaRole.Group, new() { Name = "Verification code" }).Locator("[data-slot=cell]");

        await cells.First.FocusAsync();
        await Page.Keyboard.TypeAsync("1a2");

        await Assertions.Expect(cells.Nth(0)).ToHaveValueAsync("1");
        await Assertions.Expect(cells.Nth(1)).ToHaveValueAsync("2");
        await Assertions.Expect(cells.Nth(2)).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputTags_adds_with_enter_and_comma_and_removes_with_backspace(string mode)
    {
        await Open(mode, "input-tags");
        var field = Page.GetByRole(AriaRole.Textbox, new() { Name = "Topics" });

        await Assertions.Expect(Page.Locator("[data-tag=blazor]")).ToBeVisibleAsync();
        await field.FocusAsync();
        await Page.Keyboard.TypeAsync("rust");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Page.Locator("[data-tag=rust]")).ToBeVisibleAsync();

        // Three at most: a fourth is ignored.
        await Page.Keyboard.TypeAsync("go,");
        await Assertions.Expect(Page.Locator("[data-tag=go]")).ToHaveCountAsync(0);

        await field.FillAsync("");
        await Page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(Page.Locator("[data-tag=rust]")).ToHaveCountAsync(0);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Remove blazor" }).ClickAsync();
        await Assertions.Expect(Page.Locator("[data-tag=blazor]")).ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator("[data-tag=tailwind]")).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Slider_moves_with_the_arrow_keys_by_its_step(string mode)
    {
        await Open(mode, "slider");
        var volume = Page.GetByRole(AriaRole.Slider, new() { Name = "Volume" });

        await volume.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");

        await Assertions.Expect(volume).ToHaveValueAsync("50");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(volume).ToHaveValueAsync("100");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task RangeSlider_thumbs_do_not_cross(string mode)
    {
        await Open(mode, "slider");
        var minimum = Page.GetByRole(AriaRole.Slider, new() { Name = "Minimum" });
        var maximum = Page.GetByRole(AriaRole.Slider, new() { Name = "Maximum" });

        await minimum.FocusAsync();
        await Page.Keyboard.PressAsync("End");

        await Assertions.Expect(minimum).ToHaveValueAsync(await maximum.InputValueAsync());
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputNumber_steps_with_arrows_and_clamps(string mode)
    {
        await Open(mode, "input-number");
        var quantity = Page.GetByRole(AriaRole.Spinbutton, new() { Name = "Quantity" });

        await quantity.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(quantity).ToHaveValueAsync("3");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(quantity).ToHaveValueAsync("20");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(quantity).ToHaveValueAsync("20");
        await Page.Locator("[data-slot=decrement]").First.ClickAsync();
        await Assertions.Expect(quantity).ToHaveValueAsync("19");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputNumber_formats_for_its_culture(string mode)
    {
        await Open(mode, "input-number");

        var price = Page.GetByRole(AriaRole.Spinbutton, new() { Name = "Price (French format)" });
        var text = await price.InputValueAsync();

        Assert.Contains(",5", text);
        Assert.Equal("1234.5", await Page.Locator("bx-input-number").Nth(1).Locator("input[type=hidden]").InputValueAsync());
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputRating_picks_a_star_and_works_with_arrows(string mode)
    {
        await Open(mode, "input-rating");
        var group = Page.GetByRole(AriaRole.Radiogroup, new() { Name = "Your rating" });

        // The radios are visually hidden, so a person picks a star by clicking its drawn label.
        await group.Locator("label[data-slot=star][for$='-4']").ClickAsync();
        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "4 out of 5" })).ToBeCheckedAsync();
        // Left lowers the rating, like the stars read; filled stars follow the checked one.
        await group.GetByRole(AriaRole.Radio, new() { Name = "4 out of 5" }).PressAsync("ArrowLeft");
        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "3 out of 5" })).ToBeCheckedAsync();
        await group.GetByRole(AriaRole.Radio, new() { Name = "3 out of 5" }).PressAsync("ArrowRight");
        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "4 out of 5" })).ToBeCheckedAsync();
        // Stars fade their fill (transition-colors): wait for exactly four fully filled stars.
        const string fourFilled =
            "() => [...document.querySelectorAll('[role=radiogroup] label[data-slot=star] svg')].slice(0, 5)" +
            ".filter(s => getComputedStyle(s).fill.startsWith('rgb(')).length === 4";
        await Page.WaitForFunctionAsync(fourFilled, null, new() { Timeout = 5_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task CheckboxGroup_toggles_native_checkboxes(string mode)
    {
        await Open(mode, "checkbox-group");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Notifications" });

        await group.GetByRole(AriaRole.Checkbox, new() { Name = "SMS" }).CheckAsync();

        await Assertions.Expect(group.GetByRole(AriaRole.Checkbox, new() { Name = "SMS" })).ToBeCheckedAsync();
        await Assertions.Expect(group.GetByRole(AriaRole.Checkbox, new() { Name = "Push" })).ToBeDisabledAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_values_reach_dotnet(string mode)
    {
        await Open(mode, "toggle-group");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Right" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("toggle-group-state")).ToHaveTextAsync("Alignment: right");

        await Open(mode, "slider");
        await Page.GetByRole(AriaRole.Slider, new() { Name = "Volume" }).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Page.GetByTestId("slider-state")).ToContainTextAsync("Volume: 45");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task SelectMenu_and_InputTags_post_every_value_in_a_form(string mode)
    {
        await Open(mode, "select-menu");
        var form = Page.Locator("form");

        await form.GetByRole(AriaRole.Combobox, new() { Name = "Stack" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = ".NET" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Go" }).ClickAsync();
        await Page.Keyboard.PressAsync("Escape");
        var tags = form.GetByRole(AriaRole.Textbox, new() { Name = "Keywords" });
        await tags.FocusAsync();
        await Page.Keyboard.TypeAsync("alpha");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("beta,");
        await form.GetByRole(AriaRole.Button, new() { Name = "Save profile" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("saved")).ToHaveTextAsync("Saved: dotnet, go / alpha, beta");
    }

    // Unbound list controls own their state in the browser: a child change must not make them re-read the rendered value.

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Unbound_InputTags_keeps_a_tag_the_user_adds(string mode)
    {
        await Open(mode, "input-tags");
        var tags = Page.Locator("bx-input-tags[name=skills]");

        await tags.GetByRole(AriaRole.Textbox).FocusAsync();
        await Page.Keyboard.TypeAsync("rust");
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(tags.Locator("[data-tag=rust]")).ToBeVisibleAsync();
        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(tags.Locator("[data-tag=rust]")).ToBeVisibleAsync();
        await Assertions.Expect(tags.Locator("input[type=hidden]")).ToHaveCountAsync(2);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Unbound_Listbox_keeps_the_option_the_user_picks(string mode)
    {
        await Open(mode, "listbox");
        var listbox = Page.GetByRole(AriaRole.Listbox, new() { Name = "Color" });

        await listbox.GetByRole(AriaRole.Option, new() { Name = "Blue" }).ClickAsync();

        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Blue" })).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Green" })).ToHaveAttributeAsync("aria-selected", "false");
        await Assertions.Expect(Page.Locator("bx-listbox[name=color] input[type=hidden]")).ToHaveValueAsync("blue");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Unbound_ToggleGroup_keeps_what_the_user_presses(string mode)
    {
        await Open(mode, "toggle-group");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Text marks" });

        await group.GetByRole(AriaRole.Button, new() { Name = "Italic" }).ClickAsync();

        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(group.GetByRole(AriaRole.Button, new() { Name = "Italic" })).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(group.GetByRole(AriaRole.Button, new() { Name = "Bold" })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Unbound_SelectMenu_keeps_the_values_the_user_picks(string mode)
    {
        await Open(mode, "select-menu");
        var trigger = Page.GetByRole(AriaRole.Combobox, new() { Name = "Editors" });

        await trigger.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Rider" }).ClickAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Page.WaitForTimeoutAsync(300);
        await Assertions.Expect(Page.Locator("bx-select-menu[name=editors] input[type=hidden]")).ToHaveCountAsync(2);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Unbound_InputNumber_keeps_the_number_when_another_parameter_changes(string mode)
    {
        await Open(mode, "input-number");
        var seats = Page.GetByRole(AriaRole.Spinbutton, new() { Name = "Seats" });

        await seats.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(seats).ToHaveValueAsync("3");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Raise the maximum" }).ClickAsync();
        await Assertions.Expect(Page.Locator("bx-input-number:has(input[name=seats])")).ToHaveAttributeAsync("max", "25");

        await Assertions.Expect(seats).ToHaveValueAsync("3");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task PinInput_keeps_the_digits_in_order_whatever_box_is_clicked(string mode)
    {
        await Open(mode, "pin-input");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Verification code" });
        var cells = group.Locator("[data-slot=cell]");

        await cells.Nth(3).ClickAsync();
        await Page.Keyboard.TypeAsync("5");

        // What the boxes show is what posts: no digit sits in a box after an empty one.
        await Assertions.Expect(cells.Nth(0)).ToHaveValueAsync("5");
        await Assertions.Expect(cells.Nth(3)).ToHaveValueAsync("");
        await Assertions.Expect(group.Locator("input[type=hidden]")).ToHaveValueAsync("5");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputMenu_strict_is_cleared_by_emptying_the_field(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Page.Locator("bx-input-menu").Nth(1);
        var field = menu.GetByRole(AriaRole.Combobox);

        await field.FocusAsync();
        await Page.Keyboard.TypeAsync("ger");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(menu.Locator("input[type=hidden]")).ToHaveValueAsync("de");

        await field.FillAsync("");
        await Page.Keyboard.PressAsync("Tab");

        await Assertions.Expect(field).ToHaveValueAsync("");
        await Assertions.Expect(menu.Locator("input[type=hidden]")).ToHaveValueAsync("");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("menu-state")).ToContainTextAsync("strict: none");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputMenu_marks_the_chosen_suggestion_as_selected(string mode)
    {
        await Open(mode, "input-menu");
        var menu = Page.Locator("bx-input-menu").Nth(1);

        await menu.GetByRole(AriaRole.Combobox).FocusAsync();
        await Page.Keyboard.TypeAsync("ger");
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(menu.Locator("[role=option][data-value=de]")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(menu.Locator("[role=option][data-value=fr]")).Not.ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ToggleGroup_keeps_the_focused_item_as_the_tab_stop_when_pressed(string mode)
    {
        await Open(mode, "toggle-group");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Text marks" });
        var underline = group.GetByRole(AriaRole.Button, new() { Name = "Underline" });

        await underline.ClickAsync();

        await Assertions.Expect(underline).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(underline).ToHaveAttributeAsync("tabindex", "0");
        await Assertions.Expect(group.GetByRole(AriaRole.Button, new() { Name = "Bold" })).ToHaveAttributeAsync("tabindex", "-1");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task SelectMenu_multiple_posts_an_empty_marker_when_cleared(string mode)
    {
        await Open(mode, "select-menu");
        var form = Page.Locator("form", new() { Has = Page.GetByRole(AriaRole.Button, new() { Name = "Save selection" }) });

        await form.GetByRole(AriaRole.Combobox, new() { Name = "Toolset" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = ".NET" }).Last.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Go" }).Last.ClickAsync();
        await Page.Keyboard.PressAsync("Escape");
        await form.GetByRole(AriaRole.Button, new() { Name = "Save selection" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("clear-saved")).ToHaveTextAsync("Saved: none");
    }

    [Fact]
    public async Task Enhanced_post_redraws_the_calendar_grid()
    {
        await Open("ssr", "form");
        var form = Page.Locator("form", new() { Has = Page.GetByRole(AriaRole.Button, new() { Name = "Save enhanced" }) });

        await form.Locator("button[data-date='2026-11-18']").ClickAsync();
        await form.GetByRole(AriaRole.Button, new() { Name = "Save enhanced" }).ClickAsync();
        await Assertions.Expect(Page.GetByTestId("enhanced-saved")).ToContainTextAsync("2026-11-18");

        await Assertions.Expect(form.Locator("[role=grid]")).ToBeVisibleAsync();
        await Assertions.Expect(form.Locator("button[data-date='2026-11-10']")).ToHaveAttributeAsync("data-selected", "");
    }

    [Fact]
    public async Task Enhanced_post_shows_what_the_server_rendered_back()
    {
        await Open("ssr", "form");
        var form = Page.Locator("form", new() { Has = Page.GetByRole(AriaRole.Button, new() { Name = "Save enhanced" }) });
        await Page.EvaluateAsync("window.__notReloaded = true");

        await form.GetByRole(AriaRole.Combobox, new() { Name = "Stack" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Go" }).Last.ClickAsync();
        await Page.Keyboard.PressAsync("Escape");
        await form.GetByRole(AriaRole.Button, new() { Name = "Italic" }).ClickAsync();
        var seats = form.GetByRole(AriaRole.Spinbutton, new() { Name = "Seats" });
        await seats.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(seats).ToHaveValueAsync("3");
        await form.GetByRole(AriaRole.Group, new() { Name = "Code" }).Locator("[data-slot=cell]").First.ClickAsync();
        await Page.Keyboard.TypeAsync("1234");
        await form.Locator("button[data-date='2026-11-18']").ClickAsync();
        await form.GetByRole(AriaRole.Button, new() { Name = "Save enhanced" }).ClickAsync();

        // The server received what the browser showed...
        await Assertions.Expect(Page.GetByTestId("enhanced-saved")).ToHaveTextAsync("Posted: dotnet,go / bold,italic / 3 / 1234 / 2026-11-18");
        Assert.True(await Page.EvaluateAsync<bool>("window.__notReloaded === true"), "The post reloaded the page instead of enhancing it.");
        // ...then reset the model, and the controls show that.
        await Assertions.Expect(form.Locator("bx-select-menu input[type=hidden]")).ToHaveCountAsync(1);
        await Assertions.Expect(form.Locator("bx-select-menu input[type=hidden]")).ToHaveValueAsync("dotnet");
        await Assertions.Expect(form.GetByRole(AriaRole.Button, new() { Name = "Italic" })).ToHaveAttributeAsync("aria-pressed", "false");
        await Assertions.Expect(form.GetByRole(AriaRole.Button, new() { Name = "Bold" })).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(form.GetByRole(AriaRole.Spinbutton, new() { Name = "Seats" })).ToHaveValueAsync("2");
        await Assertions.Expect(form.GetByRole(AriaRole.Group, new() { Name = "Code" }).Locator("input[type=hidden]")).ToHaveValueAsync("");
        await Assertions.Expect(form.GetByRole(AriaRole.Group, new() { Name = "Code" }).Locator("[data-slot=cell]").First).ToHaveValueAsync("");
        await Assertions.Expect(form.Locator("[role=grid]")).ToBeVisibleAsync();
        await Assertions.Expect(form.Locator("button[data-date='2026-11-10']")).ToHaveAttributeAsync("data-selected", "");
        await Assertions.Expect(form.Locator("button[data-date='2026-11-18']")).Not.ToHaveAttributeAsync("data-selected", "");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Form_shows_the_summary_then_is_busy_while_submitting(string mode)
    {
        await Open(mode, "form");
        var submit = Page.GetByRole(AriaRole.Button, new() { Name = "Create workspace" });

        await submit.ClickAsync();
        await Assertions.Expect(Page.Locator("[data-slot=summary] li")).ToHaveTextAsync("The name is required.");

        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Acme");
        if (mode != "ssr")
        {
            // Leaving the field clears its message and the summary shrinks: wait for that so the click lands on the button.
            await Page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(Page.Locator("[data-slot=summary] li")).ToHaveCountAsync(0);
        }
        await submit.ClickAsync();

        await Assertions.Expect(Page.GetByTestId("form-saved")).ToHaveTextAsync("Workspace Acme created.");
    }
}
