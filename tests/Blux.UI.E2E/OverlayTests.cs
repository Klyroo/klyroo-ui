namespace Blux.UI.E2E;

public class OverlayTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Modal_traps_focus_and_closes_on_Escape(string mode)
    {
        await Open(mode, "modal");
        var trigger = Page.GetByTestId("modal-trigger");

        await trigger.ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit profile" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("d => d.matches(':modal') && d.contains(document.activeElement)"));

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Page_does_not_scroll_behind_an_open_modal(string mode)
    {
        await Open(mode, "modal");
        var overflow = () => Page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).overflowY");
        Assert.NotEqual("hidden", await overflow());

        await Page.GetByTestId("modal-trigger").ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit profile" })).ToBeVisibleAsync();
        var before = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Mouse.MoveAsync(10, 400);
        await Page.Mouse.WheelAsync(0, 800);
        await Page.WaitForTimeoutAsync(300);

        Assert.Equal("hidden", await overflow());
        Assert.Equal(before, await Page.EvaluateAsync<double>("() => window.scrollY"));

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit profile" })).ToBeHiddenAsync();
        Assert.NotEqual("hidden", await overflow());
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Modal_close_buttons_update_the_bound_state(string mode)
    {
        await Open(mode, "modal");

        await Page.GetByTestId("modal-trigger").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("modal-state")).ToHaveTextAsync("Dialog: open");
        await Page.GetByTestId("modal-save").ClickAsync();

        await Assertions.Expect(Page.GetByTestId("modal-state")).ToHaveTextAsync("Dialog: closed");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Non_dismissible_modal_ignores_Escape(string mode)
    {
        await Open(mode, "modal");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete project" }).ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete project?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        // Twice: browsers may stop honoring a canceled Escape when it repeats without user activation in between.
        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(200);

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Closing_a_nested_modal_keeps_its_parent_open(string mode)
    {
        await Open(mode, "modal");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Edit invoice" }).ClickAsync();
        var outer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit invoice" });
        await outer.GetByRole(AriaRole.Button, new() { Name = "Discard" }).ClickAsync();
        var inner = Page.GetByRole(AriaRole.Dialog, new() { Name = "Discard changes?" });
        await Assertions.Expect(inner).ToBeVisibleAsync();

        await inner.GetByRole(AriaRole.Button, new() { Name = "Keep editing" }).ClickAsync();

        await Assertions.Expect(inner).ToBeHiddenAsync();
        await Assertions.Expect(outer).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Dropdown_menu_is_keyboard_navigable(string mode)
    {
        await Open(mode, "dropdown-menu");
        var trigger = Page.GetByTestId("menu-trigger");

        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var menu = Page.GetByRole(AriaRole.Menu);
        await Assertions.Expect(menu).ToBeVisibleAsync();
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Profile" })).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Billing" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = "Delete account" })).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menu).ToBeHiddenAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Dropdown_menu_item_reaches_dotnet_and_closes(string mode)
    {
        await Open(mode, "dropdown-menu");

        await Page.GetByTestId("menu-trigger").ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Billing" }).ClickAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByTestId("menu-state")).ToHaveTextAsync("Last action: Billing");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Tooltip_shows_on_keyboard_focus_and_hides_on_Escape(string mode)
    {
        await Open(mode, "tooltip");
        var trigger = Page.GetByRole(AriaRole.Button, new() { Name = "top", Exact = true });

        await Page.Keyboard.PressAsync("Tab");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");

        var tooltip = Page.GetByRole(AriaRole.Tooltip, new() { Name = "Tooltip on the top" });
        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAccessibleDescriptionAsync("Tooltip on the top");

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Toast_appears_from_dotnet(string mode)
    {
        await Open(mode, "toast");

        await Page.GetByTestId("toast-trigger").ClickAsync();

        var toast = Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Changes saved" });
        // The first JS interop call of a WebAssembly runtime can be slow when the whole suite runs in parallel.
        await Assertions.Expect(toast).ToBeVisibleAsync(new() { Timeout = 15_000 });
        // Hovering pauses the auto-dismiss countdown, as it would for a user reaching for the button.
        await toast.HoverAsync();
        await toast.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Assertions.Expect(toast).ToHaveCountAsync(0);
    }
}
