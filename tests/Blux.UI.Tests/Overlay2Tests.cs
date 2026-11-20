using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tests;

public class Overlay2Tests : BluxTestContext
{
    [Fact]
    public void Drawer_renders_a_titled_modal_dialog_with_handle_never_open_by_attribute()
    {
        var cut = Render<BxDrawer>(p => p.Add(d => d.Open, true).Add(d => d.Title, "Actions").Add(d => d.Description, "Pick one")
            .Add(d => d.ChildContent, "<button>Open</button>").Add(d => d.Body, "<p>Body</p>"));

        Assert.True(cut.Find("bx-modal").HasAttribute("open"));
        var dialog = cut.Find("dialog");
        Assert.False(dialog.HasAttribute("open"));
        Assert.Equal("bottom", dialog.GetAttribute("data-direction"));
        Assert.Equal("Actions", cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("Pick one", cut.Find($"#{dialog.GetAttribute("aria-describedby")}").TextContent);
        Assert.Equal("true", cut.Find("[data-slot=handle]").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll("button[aria-label=Close]"));
    }

    [Theory]
    [InlineData(DrawerDirection.Top, "top", "-translate-y-full")]
    [InlineData(DrawerDirection.Left, "left", "-translate-x-full")]
    [InlineData(DrawerDirection.Right, "right", "translate-x-full")]
    public void Drawer_direction_picks_the_edge_classes(DrawerDirection direction, string name, string cssClass)
    {
        var cut = Render<BxDrawer>(p => p.Add(d => d.Direction, direction).Add(d => d.AriaLabel, "Panel"));

        var dialog = cut.Find("dialog");
        Assert.Equal(name, dialog.GetAttribute("data-direction"));
        Assert.Contains(cssClass, dialog.ClassList);
        Assert.Equal("Panel", dialog.GetAttribute("aria-label"));
    }

    [Fact]
    public void Drawer_not_dismissible_tells_the_element_and_close_button_is_opt_in()
    {
        var cut = Render<BxDrawer>(p => p.Add(d => d.Dismissible, false).Add(d => d.Close, true).Add(d => d.AriaLabel, "Panel"));

        Assert.False(cut.Find("bx-modal").HasAttribute("dismissible"));
        Assert.True(cut.Find("button[aria-label=Close]").HasAttribute("data-bx-close"));
    }

    [Fact]
    public void Drawer_bound_state_follows_the_browser_and_unbound_has_no_sequence()
    {
        var open = true;
        var bound = Render<BxDrawer>(p => p.Add(d => d.Open, open).Add(d => d.OpenChanged, (bool v) => open = v));
        Assert.True(bound.Find("bx-modal").HasAttribute("data-bx-seq"));

        bound.Find("bx-modal").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = false, Sequence = 3 });

        Assert.False(open);
        Assert.Equal("3", bound.Find("bx-modal").GetAttribute("data-bx-seq"));
        Assert.False(Render<BxDrawer>().Find("bx-modal").HasAttribute("data-bx-seq"));
    }

    [Fact]
    public void Slideover_renders_a_right_side_panel_with_close_button()
    {
        var cut = Render<BxSlideover>(p => p.Add(s => s.Title, "Edit").Add(s => s.Footer, "<button>Save</button>"));

        var dialog = cut.Find("dialog");
        Assert.Equal("right", dialog.GetAttribute("data-side"));
        Assert.Contains("translate-x-full", dialog.ClassList);
        Assert.Equal("Edit", cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent);
        Assert.True(cut.Find("button[aria-label=Close]").HasAttribute("data-bx-close"));
        Assert.Empty(cut.FindAll("[data-slot=handle]"));
    }

    [Fact]
    public void Slideover_side_inset_and_ui_are_applied()
    {
        var cut = Render<BxSlideover>(p => p.Add(s => s.Side, SlideoverSide.Left).Add(s => s.Inset, true)
            .Add(s => s.AriaLabel, "Nav").Add(s => s.Ui, new SlideoverUi { Content = "max-w-sm" }));

        var dialog = cut.Find("dialog");
        Assert.Equal("left", dialog.GetAttribute("data-side"));
        Assert.Contains("start-2", dialog.ClassList);
        Assert.Contains("max-w-sm", dialog.ClassList);
        Assert.DoesNotContain("max-w-md", dialog.ClassList);
    }

    [Fact]
    public void Slideover_bound_state_follows_the_browser()
    {
        var open = true;
        var cut = Render<BxSlideover>(p => p.Add(s => s.Open, open).Add(s => s.OpenChanged, (bool v) => open = v));

        cut.Find("bx-modal").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = false, Sequence = 1 });

        Assert.False(open);
    }

    [Fact]
    public void ContextMenu_renders_a_focusable_area_and_a_manual_menu()
    {
        BxMenuItem[] items =
        [
            BxMenuItem.Heading("File"),
            new() { Label = "Open", Shortcut = "Enter" },
            new() { Label = "Docs", Href = "/docs" },
            BxMenuItem.Separator(),
            new() { Label = "Delete", Color = BxColor.Error },
            new() { Label = "Archived", Disabled = true },
        ];
        var cut = Render<BxContextMenu>(p => p.Add(c => c.Items, items).Add(c => c.ChildContent, "<p>Area</p>").Add(c => c.Label, "File actions"));

        var area = cut.Find("[data-slot=trigger]");
        Assert.Equal("0", area.GetAttribute("tabindex"));
        Assert.Equal("Area", area.TextContent);
        var menu = cut.Find("[popover]");
        Assert.Equal("manual", menu.GetAttribute("popover"));
        Assert.Equal("menu", menu.GetAttribute("role"));
        Assert.Equal("File actions", menu.GetAttribute("aria-label"));
        var menuItems = cut.FindAll("[role=menuitem]");
        Assert.Equal(4, menuItems.Count);
        Assert.Equal("a", menuItems[1].TagName.ToLowerInvariant());
        Assert.Equal("true", menuItems[3].GetAttribute("aria-disabled"));
        Assert.Single(cut.FindAll("[role=separator]"));
        Assert.False(cut.Find("bx-context-menu").HasAttribute("disabled"));
    }

    [Fact]
    public void ContextMenu_reports_selection_and_ignores_disabled_items()
    {
        BxMenuItem? selected = null;
        BxMenuItem[] items = [new() { Label = "Open" }, new() { Label = "Archived", Disabled = true }];
        var cut = Render<BxContextMenu>(p => p.Add(c => c.Items, items).Add(c => c.OnSelect, (BxMenuItem i) => selected = i));

        var menuItems = cut.FindAll("[role=menuitem]");
        menuItems[0].Click();

        Assert.Equal("Open", selected?.Label);
        Assert.Throws<MissingEventHandlerException>(() => menuItems[1].Click());
    }

    [Fact]
    public void ContextMenu_not_focusable_and_disabled_render_their_state()
    {
        var cut = Render<BxContextMenu>(p => p.Add(c => c.Items, new BxMenuItem[] { new() { Label = "Open" } })
            .Add(c => c.Focusable, false).Add(c => c.Disabled, true));

        Assert.False(cut.Find("[data-slot=trigger]").HasAttribute("tabindex"));
        Assert.True(cut.Find("bx-context-menu").HasAttribute("disabled"));
    }

    [Fact]
    public void ContextMenu_bound_state_follows_the_browser()
    {
        var open = true;
        var cut = Render<BxContextMenu>(p => p.Add(c => c.Items, new BxMenuItem[] { new() { Label = "Open" } })
            .Add(c => c.Open, open).Add(c => c.OpenChanged, (bool v) => open = v));

        cut.Find("bx-context-menu").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = false, Sequence = 4 });

        Assert.False(open);
    }

    private static readonly BxCommandGroup[] Commands =
    [
        new("Files", [new() { Label = "New file", Keywords = "create", Shortcut = "N" }, new() { Label = "Open", Description = "Open a file" }]),
        new(null, [new() { Label = "Docs", Href = "/docs" }, new() { Label = "Hidden", Disabled = true }]),
    ];

    [Fact]
    public void CommandPalette_renders_a_combobox_over_a_listbox_with_groups()
    {
        var cut = Render<BxCommandPalette>(p => p.Add(c => c.Groups, Commands).Add(c => c.Placeholder, "Search..."));

        var input = cut.Find("input[role=combobox]");
        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal("Search commands", input.GetAttribute("aria-label"));
        Assert.Equal("Search...", input.GetAttribute("placeholder"));
        var listbox = cut.Find("[role=listbox]");
        Assert.Equal(listbox.Id, input.GetAttribute("aria-controls"));
        Assert.Equal(4, cut.FindAll("[role=option]").Count);
        var group = cut.Find("[role=group]");
        Assert.Equal("Files", cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("Open a file", cut.FindAll("[role=option]")[1].QuerySelector("span span:last-child")!.TextContent);
        Assert.Equal("N", cut.Find("kbd").TextContent);
    }

    [Fact]
    public void CommandPalette_options_carry_what_the_browser_filters_on()
    {
        var cut = Render<BxCommandPalette>(p => p.Add(c => c.Groups, Commands));

        var options = cut.FindAll("[role=option]");
        Assert.Equal("New file create Files", options[0].GetAttribute("data-search"));
        Assert.Equal("Open", options[1].GetAttribute("data-value"));
        Assert.Equal("a", options[2].TagName.ToLowerInvariant());
        Assert.Equal("/docs", options[2].GetAttribute("href"));
        Assert.Equal("true", options[3].GetAttribute("aria-disabled"));
        Assert.Equal("No results found", cut.Find("[data-slot=empty]").TextContent);
        Assert.Equal("status", cut.Find("[data-slot=status]").GetAttribute("role"));
    }

    [Fact]
    public void CommandPalette_selection_reaches_dotnet_only_when_bound()
    {
        BxCommandItem? selected = null;
        var bound = Render<BxCommandPalette>(p => p.Add(c => c.Groups, Commands).Add(c => c.OnSelect, (BxCommandItem i) => selected = i));

        bound.Find("bx-command-palette").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "New file", Sequence = 1 });

        Assert.Equal("New file", selected?.Label);
        Assert.Throws<MissingEventHandlerException>(() =>
            Render<BxCommandPalette>(p => p.Add(c => c.Groups, Commands)).Find("bx-command-palette")
                .TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "Open" }));
    }

    [Fact]
    public void CommandPalette_close_on_select_marks_the_options_for_the_enclosing_modal()
    {
        var cut = Render<BxCommandPalette>(p => p.Add(c => c.Groups, Commands).Add(c => c.CloseOnSelect, true));

        Assert.All(cut.FindAll("[role=option]"), o => Assert.True(o.HasAttribute("data-bx-close")));
        Assert.All(Render<BxCommandPalette>(p => p.Add(c => c.Groups, Commands)).FindAll("[role=option]"), o => Assert.False(o.HasAttribute("data-bx-close")));
    }

    [Fact]
    public void ColorModeButton_is_an_icon_button_wired_by_attribute()
    {
        var cut = Render<BxColorModeButton>();

        var button = cut.Find("button");
        Assert.Equal("toggle", button.GetAttribute("data-bx-color-mode"));
        Assert.Equal("Toggle dark mode", button.GetAttribute("aria-label"));
        Assert.Equal(2, cut.FindAll("svg").Count);
        Assert.Contains("dark:hidden", cut.FindAll("svg")[0].ClassList);
    }

    [Fact]
    public void ColorModeButton_with_a_label_is_named_by_it()
    {
        var cut = Render<BxColorModeButton>(p => p.Add(b => b.Label, "Theme"));

        var button = cut.Find("button");
        Assert.False(button.HasAttribute("aria-label"));
        Assert.Contains("Theme", button.TextContent);
    }

    [Fact]
    public void ColorModeSwitch_is_a_named_switch_driven_by_the_browser()
    {
        var cut = Render<BxColorModeSwitch>();

        var input = cut.Find("input");
        Assert.Equal("switch", input.GetAttribute("role"));
        Assert.Equal("switch", input.GetAttribute("data-bx-color-mode"));
        Assert.Equal("Toggle dark mode", input.GetAttribute("aria-label"));
        Assert.False(input.HasAttribute("checked"));
    }

    [Fact]
    public void ColorModeSwitch_with_a_label_uses_a_label_element()
    {
        var cut = Render<BxColorModeSwitch>(p => p.Add(s => s.Label, "Dark mode"));

        var input = cut.Find("input");
        Assert.False(input.HasAttribute("aria-label"));
        Assert.Equal(input.Id, cut.Find("label[for]").GetAttribute("for"));
    }

    [Fact]
    public void ColorModeSwitch_wraps_its_track_in_a_label_like_BxSwitch_so_the_hit_area_can_toggle_it()
    {
        var cut = Render<BxColorModeSwitch>();

        var wrapper = cut.Find("label");
        Assert.Equal("input", wrapper.Children[0].LocalName);
        Assert.Equal("switch", wrapper.Children[0].GetAttribute("role"));
        Assert.Equal("thumb", wrapper.Children[1].GetAttribute("data-slot"));
    }

    [Fact]
    public void ColorModeSelect_offers_system_light_and_dark()
    {
        var cut = Render<BxColorModeSelect>();

        var select = cut.Find("select");
        Assert.Equal("select", select.GetAttribute("data-bx-color-mode"));
        Assert.Equal("Color mode", select.GetAttribute("aria-label"));
        Assert.Equal(["system", "light", "dark"], cut.FindAll("option").Select(o => o.GetAttribute("value")));
        Assert.Equal(["System", "Light", "Dark"], cut.FindAll("option").Select(o => o.TextContent));
    }

    [Fact]
    public void ColorModeAvatar_renders_both_modes_and_lets_css_choose()
    {
        var cut = Render<BxColorModeAvatar>(p => p.Add(a => a.Light, "/light.png").Add(a => a.Dark, "/dark.png").Add(a => a.Alt, "Ada Lovelace"));

        var avatars = cut.FindAll("[role=img]");
        Assert.Equal(2, avatars.Count);
        Assert.Contains("dark:hidden", avatars[0].ClassList);
        Assert.Contains("hidden", avatars[1].ClassList);
        Assert.Contains("dark:inline-flex", avatars[1].ClassList);
        Assert.Equal(["/light.png", "/dark.png"], cut.FindAll("img").Select(i => i.GetAttribute("src")));
        Assert.All(avatars, a => Assert.Equal("Ada Lovelace", a.GetAttribute("aria-label")));
    }

    [Fact]
    public void ColorModeImage_renders_both_sources_and_forwards_attributes_and_class()
    {
        var cut = Render<BxColorModeImage>(p => p.Add(i => i.Light, "/light.png").Add(i => i.Dark, "/dark.png").Add(i => i.Alt, "Logo")
            .Add(i => i.Class, "rounded-lg").AddUnmatched("width", "120"));

        var images = cut.FindAll("img");
        Assert.Equal(2, images.Count);
        Assert.Equal(["/light.png", "/dark.png"], images.Select(i => i.GetAttribute("src")));
        Assert.Contains("dark:hidden", images[0].ClassList);
        Assert.Contains("hidden", images[1].ClassList);
        Assert.Contains("dark:block", images[1].ClassList);
        Assert.All(images, i =>
        {
            Assert.Contains("rounded-lg", i.ClassList);
            Assert.Equal("Logo", i.GetAttribute("alt"));
            Assert.Equal("120", i.GetAttribute("width"));
        });
    }

    [Fact]
    public void ColorModeImage_without_a_dark_source_reuses_the_light_one()
    {
        var cut = Render<BxColorModeImage>(p => p.Add(i => i.Light, "/logo.png"));

        Assert.Equal(["/logo.png", "/logo.png"], cut.FindAll("img").Select(i => i.GetAttribute("src")));
        Assert.All(cut.FindAll("img"), i => Assert.Equal("", i.GetAttribute("alt")));
    }
}
