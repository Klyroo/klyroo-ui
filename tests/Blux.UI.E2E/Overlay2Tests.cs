using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

public class Overlay2Tests(PlaygroundServer server) : PlaygroundTest(server)
{
    public static readonly string[] Slugs =
    [
        "drawer", "slideover", "context-menu", "command-palette",
        "color-mode-button", "color-mode-switch", "color-mode-select", "color-mode-avatar", "color-mode-image",
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

    private ILocator Html => Page.Locator("html");

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Drawer_opens_closes_on_Escape_and_returns_focus(string mode)
    {
        await Open(mode, "drawer");
        var trigger = Page.GetByTestId("drawer-trigger");

        await trigger.ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Quick actions" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("d => d.matches(':modal') && d.contains(document.activeElement)"));
        // The slide runs by default (Tailwind animates the individual translate property) and ends docked to the bottom edge.
        Assert.Contains("translate", await dialog.EvaluateAsync<string>("d => getComputedStyle(d).transitionProperty"));
        await Assertions.Expect(dialog).ToHaveCSSAsync("translate", "0px");
        Assert.True(await dialog.EvaluateAsync<bool>("d => Math.abs(d.getBoundingClientRect().bottom - innerHeight) < 1"));

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Drawer_bound_state_follows_open_and_close(string mode)
    {
        await Open(mode, "drawer");

        await Page.GetByTestId("drawer-trigger").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("drawer-state")).ToHaveTextAsync("Drawer: open");
        await Page.GetByTestId("drawer-done").ClickAsync();

        await Assertions.Expect(Page.GetByTestId("drawer-state")).ToHaveTextAsync("Drawer: closed");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Slideover_docks_to_the_right_and_closes_with_its_button(string mode)
    {
        await Open(mode, "slideover");
        var trigger = Page.GetByTestId("slideover-trigger");

        await trigger.ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit profile" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog).ToHaveCSSAsync("translate", "0px");
        Assert.True(await dialog.EvaluateAsync<bool>("d => Math.abs(d.getBoundingClientRect().right - innerWidth) < 1 && d.getBoundingClientRect().height >= innerHeight - 1"));

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Drawer_slide_is_skipped_with_reduced_motion(string mode)
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Open(mode, "drawer");

        await Page.GetByTestId("drawer-trigger").ClickAsync();

        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Quick actions" });
        Assert.Equal("none", await dialog.EvaluateAsync<string>("d => getComputedStyle(d).transitionProperty"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ContextMenu_opens_with_Shift_F10_navigates_and_returns_focus(string mode)
    {
        await Open(mode, "context-menu");
        var area = Page.Locator("bx-context-menu [data-slot=trigger]").First;

        await area.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+F10");

        var menu = Page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(menu).ToBeVisibleAsync();
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Open" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Delete" })).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(menu).ToBeHiddenAsync();
        await Assertions.Expect(area).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ContextMenu_opens_at_the_pointer_on_right_click_and_closes_outside(string mode)
    {
        await Open(mode, "context-menu");
        var area = Page.GetByTestId("context-area");

        await area.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 40, Y = 30 } });

        var menu = Page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(menu).ToBeVisibleAsync();
        var box = (await area.BoundingBoxAsync())!;
        var menuBox = (await menu.BoundingBoxAsync())!;
        Assert.InRange(menuBox.X, box.X + 40 - 2, box.X + 40 + 6);
        Assert.InRange(menuBox.Y, box.Y + 30 - 2, box.Y + 30 + 6);

        await Page.Mouse.ClickAsync(5, 5);

        await Assertions.Expect(menu).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Bound_ContextMenu_stays_at_the_pointer_after_dotnet_answers(string mode)
    {
        await Open(mode, "context-menu");
        var area = Page.GetByTestId("bound-context-area");
        // Scrolling after the menu opened would dismiss it: bring the area into view and let the scroll events pass.
        await area.ScrollIntoViewIfNeededAsync();
        await Page.WaitForTimeoutAsync(300);

        await area.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 40, Y = 30 } });

        var menu = Page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(Page.GetByTestId("bound-context-state")).ToHaveTextAsync("Open: yes");
        await Page.WaitForTimeoutAsync(300);
        var box = (await area.BoundingBoxAsync())!;
        var menuBox = (await menu.BoundingBoxAsync())!;
        Assert.InRange(menuBox.X, box.X + 40 - 2, box.X + 40 + 6);
        Assert.InRange(menuBox.Y, box.Y + 30 - 2, box.Y + 30 + 6);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task ContextMenu_item_reaches_dotnet_and_closes(string mode)
    {
        await Open(mode, "context-menu");

        await Page.GetByTestId("context-area").ClickAsync(new() { Button = MouseButton.Right });
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" }).ClickAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByTestId("context-state")).ToHaveTextAsync("Last action: Rename");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task CommandPalette_filters_in_the_browser_and_shows_the_empty_state(string mode)
    {
        await Open(mode, "command-palette");
        var palette = Page.GetByTestId("palette");
        var input = palette.GetByRole(AriaRole.Combobox);
        var options = palette.GetByRole(AriaRole.Option);
        await Assertions.Expect(options).ToHaveCountAsync(7);

        await input.FillAsync("bill");

        await Assertions.Expect(options).ToHaveCountAsync(1);
        await Assertions.Expect(options.First).ToHaveTextAsync(new Regex("Billing"));
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-activedescendant", new Regex(".+"));
        // The group of a hidden-only section disappears with its options; keywords match too.
        await input.FillAsync("preferences");
        await Assertions.Expect(options).ToHaveCountAsync(1);
        await Assertions.Expect(options.First).ToHaveTextAsync(new Regex("Open settings"));

        await input.FillAsync("zzz");
        await Assertions.Expect(options).ToHaveCountAsync(0);
        await Assertions.Expect(palette.Locator("[data-slot=empty]")).ToBeVisibleAsync();
        await Assertions.Expect(palette.GetByRole(AriaRole.Status)).ToHaveTextAsync("No results found");

        await input.FillAsync("");
        await Assertions.Expect(options).ToHaveCountAsync(7);
        await Assertions.Expect(palette.Locator("[data-slot=empty]")).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task CommandPalette_arrow_keys_and_Enter_select_a_command(string mode)
    {
        await Open(mode, "command-palette");
        var palette = Page.GetByTestId("palette");
        var input = palette.GetByRole(AriaRole.Combobox);

        await input.FillAsync("new");
        await Assertions.Expect(palette.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Page.GetByTestId("palette-state")).ToHaveTextAsync("Last command: New folder");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task CommandPalette_click_selects_a_command(string mode)
    {
        await Open(mode, "command-palette");

        await Page.GetByTestId("palette").GetByRole(AriaRole.Option, new() { Name = "Profile" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("palette-state")).ToHaveTextAsync("Last command: Profile");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Inline_CommandPalette_below_the_fold_does_not_scroll_the_page_on_load(string mode)
    {
        await Page.SetViewportSizeAsync(1280, 260);
        await Open(mode, "command-palette");
        await Page.WaitForTimeoutAsync(300);

        Assert.Equal(0, await Page.EvaluateAsync<int>("() => Math.round(window.scrollY)"));
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task CommandPalette_ignores_Enter_while_an_IME_composition_is_open(string mode)
    {
        await Open(mode, "command-palette");
        var input = Page.GetByTestId("palette").GetByRole(AriaRole.Combobox);
        await input.FocusAsync();

        await input.EvaluateAsync("e => e.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', keyCode: 229, isComposing: true, bubbles: true, cancelable: true }))");

        await Page.WaitForTimeoutAsync(200);
        await Assertions.Expect(Page.GetByTestId("palette-state")).ToHaveTextAsync("Last command: none");
    }

    [Fact]
    public async Task Hotkeys_do_not_fire_while_typing_and_skip_unavailable_targets()
    {
        await Open("ssr", "command-palette");
        var errors = new List<string>();
        Page.PageError += (_, message) => errors.Add(message);
        await Page.EvaluateAsync(@"() => {
            window.__hits = [];
            const add = (tag, attrs) => { const e = document.createElement(tag); for (const k in attrs) e.setAttribute(k, attrs[k]); document.body.append(e); return e; };
            add('input', { id: 'hk-input' });
            const hidden = add('button', { 'data-bx-hotkey': 'j', style: 'display:none' }); hidden.addEventListener('click', () => window.__hits.push('hidden'));
            const disabled = add('button', { 'data-bx-hotkey': 'j', disabled: '' }); disabled.addEventListener('click', () => window.__hits.push('disabled'));
            const live = add('button', { 'data-bx-hotkey': 'j' }); live.addEventListener('click', () => window.__hits.push('live'));
            const bare = add('button', { 'data-bx-hotkey': 'g' }); bare.addEventListener('click', () => window.__hits.push('bare'));
            const combo = add('button', { 'data-bx-hotkey': 'ctrl+g' }); combo.addEventListener('click', () => window.__hits.push('combo'));
        }");

        await Page.Locator("#hk-input").FocusAsync();
        await Page.Keyboard.TypeAsync("g");
        await Assertions.Expect(Page.Locator("#hk-input")).ToHaveValueAsync("g");
        await Page.Keyboard.PressAsync("Control+g");
        await Page.EvaluateAsync("() => document.dispatchEvent(new Event('keydown'))");
        await Page.Locator("#hk-input").BlurAsync();
        await Page.Keyboard.PressAsync("j");

        var hits = await Page.EvaluateAsync<string[]>("() => window.__hits");
        Assert.Equal(["combo", "live"], hits);
        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task CommandPalette_opens_in_a_modal_with_Ctrl_K_and_closes_on_select(string mode)
    {
        await Open(mode, "command-palette");
        await Page.Locator("h1").ClickAsync();

        await Page.Keyboard.PressAsync("Control+k");

        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Command palette" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        var input = dialog.GetByRole(AriaRole.Combobox);
        await Assertions.Expect(input).ToBeFocusedAsync();
        await input.FillAsync("theme");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("palette-modal-state")).ToHaveTextAsync("Last command: Switch theme");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ColorModeButton_toggles_and_persists_across_reload(string mode)
    {
        await Open(mode, "color-mode-button");
        var button = Page.GetByTestId("color-mode-button");
        await Assertions.Expect(Html).Not.ToHaveClassAsync(new Regex("dark"));

        await button.ClickAsync();

        await Assertions.Expect(Html).ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        // The light icon is replaced by the sun, through CSS alone.
        Assert.Equal(1, await button.Locator("svg:visible").CountAsync());

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");

        await Assertions.Expect(Html).ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await button.ClickAsync();
        await Assertions.Expect(Html).Not.ToHaveClassAsync(new Regex("dark"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ColorModeSwitch_follows_the_mode_and_instances_stay_in_sync(string mode)
    {
        await Open(mode, "color-mode-switch");
        var first = Page.GetByRole(AriaRole.Switch).First;
        var second = Page.GetByRole(AriaRole.Switch, new() { Name = "Dark mode", Exact = true });
        await Assertions.Expect(first).Not.ToBeCheckedAsync();

        await first.ClickAsync();

        await Assertions.Expect(Html).ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(first).ToBeCheckedAsync();
        await Assertions.Expect(second).ToBeCheckedAsync();

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");

        await Assertions.Expect(Html).ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(first).ToBeCheckedAsync();
        await second.ClickAsync();
        await Assertions.Expect(Html).Not.ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(first).Not.ToBeCheckedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ColorModeSelect_sets_the_mode_and_persists_across_reload(string mode)
    {
        await Open(mode, "color-mode-select");
        var select = Page.GetByTestId("color-mode-select").GetByRole(AriaRole.Combobox);
        await Assertions.Expect(select).ToHaveValueAsync("system");

        await select.SelectOptionAsync("dark");

        await Assertions.Expect(Html).ToHaveClassAsync(new Regex("dark"));

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");

        await Assertions.Expect(Html).ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(select).ToHaveValueAsync("dark");

        await select.SelectOptionAsync("light");
        await Assertions.Expect(Html).Not.ToHaveClassAsync(new Regex("dark"));
        await Page.ReloadAsync();
        await Assertions.Expect(select).ToHaveValueAsync("light");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ColorModeSelect_follows_a_change_made_elsewhere(string mode)
    {
        await Open(mode, "color-mode-select");
        var select = Page.GetByTestId("color-mode-select").GetByRole(AriaRole.Combobox);

        await Page.EvaluateAsync("() => Blux.setColorMode('dark')");

        await Assertions.Expect(select).ToHaveValueAsync("dark");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ColorModeAvatar_and_Image_show_the_source_of_the_current_mode(string mode)
    {
        await Open(mode, "color-mode-avatar");
        var avatar = Page.Locator("[data-testid=color-mode-avatar]:visible");
        await Assertions.Expect(avatar).ToHaveCountAsync(1);
        await Assertions.Expect(avatar.Locator("img")).ToHaveAttributeAsync("src", new Regex("%23ffd23f"));

        await Page.EvaluateAsync("() => Blux.setColorMode('dark')");

        await Assertions.Expect(avatar).ToHaveCountAsync(1);
        await Assertions.Expect(avatar.Locator("img")).ToHaveAttributeAsync("src", new Regex("%230a0b0c"));

        await Open(mode, "color-mode-image");
        var image = Page.Locator("img[data-testid=color-mode-image]:visible");
        await Assertions.Expect(image).ToHaveCountAsync(1);
        await Assertions.Expect(image).ToHaveAttributeAsync("src", new Regex("%230f172a"));
        await Page.EvaluateAsync("() => Blux.setColorMode('light')");
        await Assertions.Expect(image).ToHaveAttributeAsync("src", new Regex("%23fef3c7"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Img, new() { Name = "A sun by day, a moon by night" })).ToHaveCountAsync(1);
    }
}
