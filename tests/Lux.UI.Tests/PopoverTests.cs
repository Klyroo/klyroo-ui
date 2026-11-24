using Microsoft.AspNetCore.Components;

namespace Lux.UI.Tests;

public class PopoverTests : LuxTestContext
{
    [Fact]
    public void Renders_the_custom_element_with_a_native_popover()
    {
        var cut = Render<LxPopover>(p => p
            .Add(x => x.Placement, "bottom-start")
            .Add(x => x.ChildContent, "<button>Open</button>")
            .Add(x => x.Content, "<p>Panel</p>"));

        var host = cut.Find("lx-popover");
        Assert.Equal("bottom-start", host.GetAttribute("placement"));
        Assert.False(host.HasAttribute("open"));
        var content = cut.Find("[popover]");
        Assert.Equal("auto", content.GetAttribute("popover"));
        Assert.Equal("dialog", content.GetAttribute("role"));
        Assert.StartsWith("lx-popover-", content.Id);
    }

    [Fact]
    public void Open_is_rendered_on_the_custom_element_only()
    {
        var cut = Render<LxPopover>(p => p.Add(x => x.Open, true));

        Assert.True(cut.Find("lx-popover").HasAttribute("open"));
        Assert.False(cut.Find("[popover]").HasAttribute("open"));
    }

    [Fact]
    public void Browser_toggle_updates_the_bound_value()
    {
        var open = true;
        var cut = Render<LxPopover>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, (bool v) => open = v));

        cut.Find("lx-popover").TriggerEvent("onlxtoggle", new LxToggleEventArgs { Open = false });

        Assert.False(open);
    }

    [Fact]
    public void Bound_popover_renders_the_last_toggle_sequence_it_has_seen()
    {
        var open = false;
        var cut = Render<LxPopover>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, (bool v) => open = v));
        Assert.Equal("0", cut.Find("lx-popover").GetAttribute("data-lx-seq"));

        cut.Find("lx-popover").TriggerEvent("onlxtoggle", new LxToggleEventArgs { Open = true, Sequence = 3 });

        Assert.Equal("3", cut.Find("lx-popover").GetAttribute("data-lx-seq"));
    }

    [Fact]
    public void Unbound_popover_renders_no_sequence()
    {
        var cut = Render<LxPopover>(p => p.Add(x => x.Open, true));

        Assert.False(cut.Find("lx-popover").HasAttribute("data-lx-seq"));
    }

    [Fact]
    public void Unbound_popover_does_not_listen_to_toggles()
    {
        var cut = Render<LxPopover>();

        Assert.Throws<MissingEventHandlerException>(() =>
            cut.Find("lx-popover").TriggerEvent("onlxtoggle", new LxToggleEventArgs { Open = true }));
    }
}
