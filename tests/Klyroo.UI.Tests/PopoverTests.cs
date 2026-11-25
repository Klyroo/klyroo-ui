using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Tests;

public class PopoverTests : KlyrooTestContext
{
    [Fact]
    public void Renders_the_custom_element_with_a_native_popover()
    {
        var cut = Render<KlyPopover>(p => p
            .Add(x => x.Placement, "bottom-start")
            .Add(x => x.ChildContent, "<button>Open</button>")
            .Add(x => x.Content, "<p>Panel</p>"));

        var host = cut.Find("kly-popover");
        Assert.Equal("bottom-start", host.GetAttribute("placement"));
        Assert.False(host.HasAttribute("open"));
        var content = cut.Find("[popover]");
        Assert.Equal("auto", content.GetAttribute("popover"));
        Assert.Equal("dialog", content.GetAttribute("role"));
        Assert.StartsWith("kly-popover-", content.Id);
    }

    [Fact]
    public void Open_is_rendered_on_the_custom_element_only()
    {
        var cut = Render<KlyPopover>(p => p.Add(x => x.Open, true));

        Assert.True(cut.Find("kly-popover").HasAttribute("open"));
        Assert.False(cut.Find("[popover]").HasAttribute("open"));
    }

    [Fact]
    public void Browser_toggle_updates_the_bound_value()
    {
        var open = true;
        var cut = Render<KlyPopover>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, (bool v) => open = v));

        cut.Find("kly-popover").TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = false });

        Assert.False(open);
    }

    [Fact]
    public void Bound_popover_renders_the_last_toggle_sequence_it_has_seen()
    {
        var open = false;
        var cut = Render<KlyPopover>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, (bool v) => open = v));
        Assert.Equal("0", cut.Find("kly-popover").GetAttribute("data-kly-seq"));

        cut.Find("kly-popover").TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = true, Sequence = 3 });

        Assert.Equal("3", cut.Find("kly-popover").GetAttribute("data-kly-seq"));
    }

    [Fact]
    public void Unbound_popover_renders_no_sequence()
    {
        var cut = Render<KlyPopover>(p => p.Add(x => x.Open, true));

        Assert.False(cut.Find("kly-popover").HasAttribute("data-kly-seq"));
    }

    [Fact]
    public void Unbound_popover_does_not_listen_to_toggles()
    {
        var cut = Render<KlyPopover>();

        Assert.Throws<MissingEventHandlerException>(() =>
            cut.Find("kly-popover").TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = true }));
    }
}
