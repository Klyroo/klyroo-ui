namespace Blux.UI.E2E;

public class NavigationTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Tabs_follow_arrow_keys_and_skip_disabled_tabs(string mode)
    {
        await Open(mode, "tabs");
        var tablist = Page.GetByRole(AriaRole.Tablist, new() { Name = "Account settings" });
        var account = tablist.GetByRole(AriaRole.Tab, new() { Name = "Account" });

        await account.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        var password = tablist.GetByRole(AriaRole.Tab, new() { Name = "Password" });
        await Assertions.Expect(password).ToBeFocusedAsync();
        await Assertions.Expect(password).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(Page.GetByRole(AriaRole.Tabpanel, new() { Name = "Password" })).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(account).ToBeFocusedAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Tabs_report_the_selection_to_dotnet(string mode)
    {
        await Open(mode, "tabs");

        await Page.GetByRole(AriaRole.Tab, new() { Name = "Password" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("tab-state")).ToHaveTextAsync("Selected: password");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Accordion_opens_one_item_at_a_time(string mode)
    {
        await Open(mode, "accordion");
        var first = Page.GetByRole(AriaRole.Button, new() { Name = "Which render modes are supported?" });
        var second = Page.GetByRole(AriaRole.Button, new() { Name = "Do I need Tailwind?" });
        await Assertions.Expect(first).ToHaveAttributeAsync("aria-expanded", "true");

        await second.ClickAsync();

        await Assertions.Expect(second).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(first).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Do I need Tailwind?" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Controls_inside_an_accordion_panel_do_not_toggle_it(string mode)
    {
        await Open(mode, "accordion");
        var settings = Page.GetByRole(AriaRole.Button, new() { Name = "Settings" });
        await Assertions.Expect(settings).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Theme" }).ClickAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Assertions.Expect(settings).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Table_sorts_by_column(string mode)
    {
        await Open(mode, "table");
        var header = Page.GetByRole(AriaRole.Columnheader, new() { Name = "Name" });

        await header.GetByRole(AriaRole.Button).ClickAsync();

        await Assertions.Expect(header).ToHaveAttributeAsync("aria-sort", "ascending");
        await Assertions.Expect(Page.Locator("tbody tr").First).ToContainTextAsync("Courtney Henry");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Broken_avatar_image_falls_back_to_initials(string mode)
    {
        await Open(mode, "avatar");

        var broken = Page.Locator("img[src='/missing.png']");
        // Lazy images can load late under load (Firefox): allow more than the default 5 s.
        await Assertions.Expect(broken).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(broken.Locator("xpath=..").GetByText("AL", new() { Exact = true })).ToBeVisibleAsync();
    }
}
