using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tests;

public class OverlayTests : BluxTestContext
{
    [Fact]
    public void Modal_renders_a_titled_dialog_never_open_by_attribute()
    {
        var cut = Render<BxModal>(p => p.Add(m => m.Open, true).Add(m => m.Title, "Delete file").Add(m => m.Description, "This cannot be undone.")
            .Add(m => m.ChildContent, "<button>Open</button>").Add(m => m.Body, "<p>Body</p>"));

        Assert.True(cut.Find("bx-modal").HasAttribute("open"));
        var dialog = cut.Find("dialog");
        Assert.False(dialog.HasAttribute("open"));
        Assert.Equal("Delete file", cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("This cannot be undone.", cut.Find($"#{dialog.GetAttribute("aria-describedby")}").TextContent);
        Assert.True(cut.Find("button[aria-label=Close]").HasAttribute("data-bx-close"));
    }

    [Fact]
    public void Modal_not_dismissible_tells_the_element()
    {
        var cut = Render<BxModal>(p => p.Add(m => m.Dismissible, false));

        Assert.False(cut.Find("bx-modal").HasAttribute("dismissible"));
    }

    [Fact]
    public void Modal_bound_state_follows_the_browser()
    {
        var open = true;
        var cut = Render<BxModal>(p => p.Add(m => m.Open, open).Add(m => m.OpenChanged, (bool v) => open = v));

        cut.Find("bx-modal").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = false, Sequence = 2 });

        Assert.False(open);
    }

    [Fact]
    public void Tooltip_is_a_manual_popover_with_tooltip_role()
    {
        var cut = Render<BxTooltip>(p => p.Add(t => t.Text, "Copy").Add(t => t.ChildContent, "<button>C</button>"));

        var tip = cut.Find("[role=tooltip]");
        Assert.Equal("manual", tip.GetAttribute("popover"));
        Assert.Equal("Copy", tip.TextContent.Trim());
    }

    [Fact]
    public void DropdownMenu_renders_items_and_reports_selection()
    {
        BxMenuItem? selected = null;
        BxMenuItem[] items =
        [
            BxMenuItem.Heading("Account"),
            new() { Label = "Profile", Shortcut = "⌘P" },
            new() { Label = "Docs", Href = "/docs" },
            BxMenuItem.Separator(),
            new() { Label = "Delete", Color = BxColor.Error },
            new() { Label = "Archived", Disabled = true },
        ];
        var cut = Render<BxDropdownMenu>(p => p.Add(d => d.Items, items).Add(d => d.OnSelect, (BxMenuItem i) => selected = i)
            .Add(d => d.ChildContent, "<button>Menu</button>"));

        Assert.Equal("menu", cut.Find("[popover]").GetAttribute("role"));
        var menuItems = cut.FindAll("[role=menuitem]");
        Assert.Equal(4, menuItems.Count);
        Assert.Equal("a", menuItems[1].TagName.ToLowerInvariant());
        Assert.Single(cut.FindAll("[role=separator]"));
        Assert.Equal("true", menuItems[3].GetAttribute("aria-disabled"));

        menuItems[2].Click();
        Assert.Equal("Delete", selected?.Label);
        Assert.Throws<MissingEventHandlerException>(() => menuItems[3].Click());
    }

    [Fact]
    public void DropdownMenu_renders_a_submenu_next_to_the_menu_and_names_it_after_its_item()
    {
        BxMenuItem? selected = null;
        BxMenuItem[] items =
        [
            new() { Label = "Copy" },
            new()
            {
                Label = "Send to",
                Children = [new() { Label = "Email" }, new() { Label = "Team", Children = [new() { Label = "Design" }] }, new() { Label = "Hidden", Disabled = true }],
            },
            new() { Label = "Export", Disabled = true, Children = [new() { Label = "PDF" }] },
        ];
        var cut = Render<BxDropdownMenu>(p => p.Add(d => d.Items, items).Add(d => d.OnSelect, (BxMenuItem i) => selected = i)
            .Add(d => d.ChildContent, "<button>Menu</button>"));

        var menus = cut.FindAll("[popover]");
        Assert.Equal(3, menus.Count);
        Assert.Equal("content", menus[0].GetAttribute("data-slot"));
        Assert.All(menus.Skip(1), m => Assert.Equal("submenu", m.GetAttribute("data-slot")));
        // The submenus are siblings of the menu: a role="menu" only owns items.
        Assert.All(menus, m => Assert.Equal("bx-popover", m.ParentElement!.TagName.ToLowerInvariant()));

        var send = cut.Find("button[aria-haspopup=menu][popovertarget]:not([aria-disabled])");
        Assert.Contains("Send to", send.TextContent);
        var sub = cut.Find("#" + send.GetAttribute("popovertarget"));
        Assert.Equal("menu", sub.GetAttribute("role"));
        Assert.Equal(send.Id, sub.GetAttribute("aria-labelledby"));
        Assert.Equal("show", send.GetAttribute("popovertargetaction"));
        Assert.Equal(["Email", "Team", "Hidden"], sub.QuerySelectorAll(":scope > [role=menuitem]").Select(i => i.TextContent.Trim()));

        // A disabled item with children does not open anything; an item with children is never selected.
        var export = cut.Find("button[aria-haspopup=menu][aria-disabled=true]");
        Assert.Equal("true", export.GetAttribute("aria-disabled"));
        Assert.Null(export.GetAttribute("popovertarget"));
        Assert.Throws<MissingEventHandlerException>(() => send.Click());
        Assert.Null(selected);

        cut.FindAll("[role=menuitem]").First(i => i.TextContent.Contains("Design")).Click();
        Assert.Equal("Design", selected?.Label);
    }

    [Fact]
    public void Toaster_exposes_its_theme_to_the_browser()
    {
        var cut = Render<BxToaster>();

        var toaster = cut.Find("bx-toaster");
        Assert.Contains("before:bg-(--bx-color)", toaster.GetAttribute("data-ui-root"));
        Assert.Equal("5000", toaster.GetAttribute("data-duration"));
        Assert.Empty(cut.Find("ol").Children);
    }

    [Fact]
    public async Task ToastService_calls_the_browser_with_primitive_arguments()
    {
        JSInterop.SetupVoid("Blux.toast", _ => true).SetVoidResult();
        var service = Services.GetService(typeof(IToastService)) as IToastService;

        await service!.Add(new BxToast { Title = "Saved", Color = BxColor.Success, Duration = TimeSpan.FromSeconds(2) });

        var call = JSInterop.VerifyInvoke("Blux.toast");
        Assert.Equal(["Saved", null, "success", 2000], call.Arguments);
    }
}
