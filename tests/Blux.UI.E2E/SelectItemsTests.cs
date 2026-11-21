using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>Count, Icon and Group of the items of Select, SelectMenu, InputMenu and Listbox (and Count of CommandPalette) in the browser.</summary>
public class SelectItemsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator Mailbox => Page.GetByRole(AriaRole.Combobox, new() { Name = "Mailbox" });

    private static Task<string?> ActiveLabel(ILocator focused) =>
        focused.EvaluateAsync<string?>("e => document.getElementById(e.getAttribute('aria-activedescendant'))?.dataset.label ?? null");

    private async Task Dark()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Select_lists_counts_icons_and_named_groups(string mode)
    {
        await Open(mode, "select");

        await Mailbox.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var listbox = Page.GetByRole(AriaRole.Listbox);
        await Assertions.Expect(listbox.GetByRole(AriaRole.Group)).ToHaveCountAsync(2);
        await Assertions.Expect(listbox.GetByRole(AriaRole.Group, new() { Name = "Mailboxes" })).ToBeVisibleAsync();
        await Assertions.Expect(listbox.GetByRole(AriaRole.Group, new() { Name = "Folders" })).ToBeVisibleAsync();
        // A heading is not an option: seven items, seven options.
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option)).ToHaveCountAsync(7);
        var inbox = listbox.GetByRole(AriaRole.Option, new() { Name = "Inbox" });
        await Assertions.Expect(inbox.Locator("[data-slot=count]")).ToHaveTextAsync("12");
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Archive" }).Locator("[data-slot=count]")).ToHaveTextAsync("1,840");
        await Assertions.Expect(inbox.Locator("svg").First).ToBeVisibleAsync();
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Trash" })).ToHaveAttributeAsync("aria-disabled", "true");
        // The count is at the end of the row, before the check mark: right of the label.
        var label = await inbox.Locator("span").First.BoundingBoxAsync();
        var count = await inbox.Locator("[data-slot=count]").BoundingBoxAsync();
        Assert.True(count!.X > label!.X + label.Width, $"{count.X} <= {label.X + label.Width}");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Select_arrows_skip_the_headings_and_typeahead_still_works(string mode)
    {
        await Open(mode, "select");
        await Mailbox.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(Page.Locator("[data-slot=content]:popover-open")).ToHaveCountAsync(1);
        Assert.Equal("All mail", await ActiveLabel(Mailbox));

        string?[] seen = new string?[7];
        for (var i = 0; i < seen.Length; i++)
        {
            await Page.Keyboard.PressAsync("ArrowDown");
            seen[i] = await ActiveLabel(Mailbox);
        }

        // Down through two groups; the disabled Trash and the headings are never landed on.
        Assert.Equal(["Inbox", "Starred", "Sent", "Archive", "Projects", "Projects", "Projects"], seen);
        await Page.Keyboard.PressAsync("Home");
        Assert.Equal("All mail", await ActiveLabel(Mailbox));
        await Page.Keyboard.PressAsync("End");
        Assert.Equal("Projects", await ActiveLabel(Mailbox));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        Assert.Equal("Sent", await ActiveLabel(Mailbox));

        await Page.Keyboard.PressAsync("s");
        Assert.Equal("Starred", await ActiveLabel(Mailbox));
        await Page.Keyboard.PressAsync("p");
        Assert.Equal("Projects", await ActiveLabel(Mailbox));
        // "f" starts the heading "Folders" and no option: nothing moves.
        await Page.Keyboard.PressAsync("f");
        Assert.Equal("Projects", await ActiveLabel(Mailbox));

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Mailbox.Locator("[data-slot=label]")).ToHaveAttributeAsync("data-label", "Projects");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("mailbox-state")).ToHaveTextAsync("Selected: projects");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task SelectMenu_filter_hides_a_heading_with_its_last_option(string mode)
    {
        await Open(mode, "select-menu");
        var trigger = Page.GetByRole(AriaRole.Combobox, new() { Name = "Destination" });
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var search = Page.Locator("bx-select-menu", new() { Has = Page.GetByTestId("destination") }).Locator("[data-slot=search]");
        await Assertions.Expect(search).ToBeFocusedAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Asia" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Japan" }).Locator("[data-slot=count]")).ToHaveTextAsync("17");

        await Page.Keyboard.TypeAsync("ger");

        await Assertions.Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Germany" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Europe" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Asia" })).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Americas" })).ToBeHiddenAsync();

        await Page.Keyboard.PressAsync("Control+a");
        await Page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Asia" })).ToBeVisibleAsync();

        // Arrow keys step over the headings, from one group into the next.
        Assert.Equal("France", await ActiveLabel(search));
        var seen = new List<string?>();
        for (var i = 0; i < 6; i++)
        {
            await Page.Keyboard.PressAsync("ArrowDown");
            seen.Add(await ActiveLabel(search));
        }
        Assert.Equal(["Germany", "Italy", "Japan", "South Korea", "Brazil", "Canada"], seen);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputMenu_suggestions_keep_their_headings_until_the_last_one_is_filtered_out(string mode)
    {
        await Open(mode, "input-menu");
        var field = Page.GetByRole(AriaRole.Combobox, new() { Name = "Assign to" });

        await field.FocusAsync();
        await Page.Keyboard.TypeAsync("docs");

        await Assertions.Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Docs team" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Teams" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "People" })).ToBeHiddenAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("Docs team", await ActiveLabel(field));
        await Page.Keyboard.PressAsync("Enter");

        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("assign-state")).ToHaveTextAsync("Assigned: docs");
        await field.ClearAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "People" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Alan Turing" }).Locator("[data-slot=count]")).ToHaveTextAsync("0");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Listbox_arrows_and_typeahead_skip_the_headings(string mode)
    {
        await Open(mode, "listbox");
        var listbox = Page.GetByRole(AriaRole.Listbox, new() { Name = "Time zone" });
        await Assertions.Expect(listbox.GetByRole(AriaRole.Group)).ToHaveCountAsync(3);
        await Assertions.Expect(listbox.GetByRole(AriaRole.Group, new() { Name = "Pacific" })).ToBeVisibleAsync();
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "Tokyo" }).Locator("[data-slot=count]")).ToHaveTextAsync("3");

        await listbox.FocusAsync();
        Assert.Equal("Paris", await ActiveLabel(listbox));
        var seen = new List<string?>();
        for (var i = 0; i < 5; i++)
        {
            await Page.Keyboard.PressAsync("ArrowDown");
            seen.Add(await ActiveLabel(listbox));
        }
        Assert.Equal(["London", "Tokyo", "Sydney", "Sydney", "Sydney"], seen);

        await Page.Keyboard.PressAsync("t");
        Assert.Equal("Tokyo", await ActiveLabel(listbox));
        // "e" is the first letter of the heading "Europe", not of an option.
        await Page.Keyboard.PressAsync("e");
        Assert.Equal("Tokyo", await ActiveLabel(listbox));

        await Page.Keyboard.PressAsync("Enter");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("zone-state")).ToHaveTextAsync("Zone: tokyo");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task CommandPalette_count_is_shown_before_the_shortcut_and_not_searched(string mode)
    {
        await Open(mode, "command-palette");
        var palette = Page.GetByTestId("count-palette");
        var inbox = palette.GetByRole(AriaRole.Option, new() { Name = "Inbox" });

        await Assertions.Expect(inbox.Locator("[data-slot=count]")).ToHaveTextAsync("12");
        await Assertions.Expect(palette.GetByRole(AriaRole.Option, new() { Name = "Archive" }).Locator("[data-slot=count]")).ToHaveTextAsync("1,840");
        var count = await inbox.Locator("[data-slot=count]").BoundingBoxAsync();
        var shortcut = await inbox.Locator("kbd").BoundingBoxAsync();
        Assert.True(count!.X + count.Width <= shortcut!.X + 1, $"{count.X + count.Width} > {shortcut.X}");

        await palette.GetByRole(AriaRole.Combobox).FillAsync("1840");
        await Assertions.Expect(palette.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Open_lists_with_groups_and_counts_have_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await Open(mode, "select");
        await AssertNoAccessibilityViolationsOpen(Mailbox, "ArrowDown");
        await Page.Keyboard.PressAsync("Escape");
        await Open(mode, "select-menu");
        var destination = Page.GetByRole(AriaRole.Combobox, new() { Name = "Destination" });
        await AssertNoAccessibilityViolationsOpen(destination, "ArrowDown");
        await Page.Keyboard.PressAsync("Escape");
        await Open(mode, "input-menu");
        await AssertNoAccessibilityViolationsOpen(Page.GetByRole(AriaRole.Combobox, new() { Name = "Assign to" }), "ArrowDown");
        await Page.Keyboard.PressAsync("Escape");

        await Dark();
        await Open(mode, "select");
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await AssertNoAccessibilityViolationsOpen(Mailbox, "ArrowDown");
        await Page.Keyboard.PressAsync("Escape");
        await Open(mode, "select-menu");
        await AssertNoAccessibilityViolationsOpen(destination, "ArrowDown");
        await Page.Keyboard.PressAsync("Escape");
        await Open(mode, "input-menu");
        await AssertNoAccessibilityViolationsOpen(Page.GetByRole(AriaRole.Combobox, new() { Name = "Assign to" }), "ArrowDown");
        await Page.Keyboard.PressAsync("Escape");
    }

    private async Task AssertNoAccessibilityViolationsOpen(ILocator field, string key)
    {
        await field.FocusAsync();
        await Page.Keyboard.PressAsync(key);
        await Assertions.Expect(Page.Locator("[popover]:popover-open")).ToHaveCountAsync(1);
        await AssertNoAccessibilityViolations();
    }
}
