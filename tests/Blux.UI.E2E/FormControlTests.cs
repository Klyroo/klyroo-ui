namespace Blux.UI.E2E;

public class FormControlTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Select_is_operable_with_the_keyboard(string mode)
    {
        await Open(mode, "select");
        var combobox = Page.GetByRole(AriaRole.Combobox, new() { Name = "Country" });

        await combobox.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var listbox = Page.GetByRole(AriaRole.Listbox);
        await Assertions.Expect(listbox).ToBeVisibleAsync();
        await Assertions.Expect(combobox).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(listbox).ToBeHiddenAsync();
        await Assertions.Expect(combobox.Locator("[data-slot=label]")).ToHaveAttributeAsync("data-label", "Italy");
        await Assertions.Expect(Page.Locator("bx-select input[type=hidden]")).ToHaveValueAsync("it");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Select_opens_with_Space_and_stays_open(string mode)
    {
        await Open(mode, "select");
        var combobox = Page.GetByRole(AriaRole.Combobox, new() { Name = "Country" });

        await combobox.FocusAsync();
        await Page.Keyboard.PressAsync(" ");
        await Page.WaitForTimeoutAsync(200);

        await Assertions.Expect(Page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Assertions.Expect(combobox).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Select_click_updates_the_bound_value(string mode)
    {
        await Open(mode, "select");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Country" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Spain" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("country-state")).ToHaveTextAsync("Selected: es");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Form_field_shows_validation_then_submits(string mode)
    {
        await Open(mode, "form-field");
        var email = Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" });
        var submit = Page.GetByRole(AriaRole.Button, new() { Name = "Create account" });

        await submit.ClickAsync();
        await Assertions.Expect(Page.Locator("[data-slot=error]")).ToHaveTextAsync("The Email field is required.");
        await Assertions.Expect(email).ToHaveAttributeAsync("aria-invalid", "true");

        await email.FillAsync("ada@example.com");
        // Interactive: leaving the field validates it and removes its error, which moves the fields below, so wait
        // for that first. Static: validation only runs when the form posts.
        await email.PressAsync("Tab");
        if (mode != "ssr") await Assertions.Expect(Page.Locator("[data-slot=error]")).ToHaveCountAsync(0);
        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Role" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Designer" }).ClickAsync();
        await submit.ClickAsync();

        await Assertions.Expect(Page.Locator("[data-slot=title]", new() { HasText = "Account created" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Radio_group_is_a_labelled_group_of_native_radios(string mode)
    {
        await Open(mode, "radio-group");
        var group = Page.GetByRole(AriaRole.Radiogroup, new() { Name = "Plan" });

        await group.GetByRole(AriaRole.Radio, new() { Name = "Free" }).CheckAsync();

        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "Free" })).ToBeCheckedAsync();
        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "Pro" })).Not.ToBeCheckedAsync();
        await Assertions.Expect(group.GetByRole(AriaRole.Radio, new() { Name = "Enterprise" })).ToBeDisabledAsync();
    }
}
