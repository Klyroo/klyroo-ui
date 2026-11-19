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
        await group.GetByRole(AriaRole.Radio, new() { Name = "4 out of 5" }).PressAsync("ArrowLeft");

        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "5 out of 5" })).ToBeCheckedAsync();
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
