using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lux.UI.Tests;

public class ButtonTests : BluxTestContext
{
    [Fact]
    public void Renders_a_typed_button_with_color_and_label()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Label, "Save"));

        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("primary", button.GetAttribute("data-color"));
        Assert.Equal("Save", cut.Find("[data-slot=label]").TextContent);
        Assert.Contains("bg-(--bx-color)", button.ClassName);
    }

    [Fact]
    public void Renders_a_link_when_href_is_set()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Href, "/docs").Add(b => b.Label, "Docs"));

        Assert.Equal("/docs", cut.Find("a").GetAttribute("href"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Disabled_link_loses_its_href_and_is_marked_disabled()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Href, "/docs").Add(b => b.Disabled, true));

        var link = cut.Find("a");
        Assert.False(link.HasAttribute("href"));
        Assert.Equal("true", link.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Loading_disables_and_shows_a_spinner()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Loading, true).Add(b => b.Label, "Save"));

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.Contains("animate-spin", cut.Find("[data-slot=leadingIcon]").ClassName);
    }

    [Fact]
    public void Click_invokes_the_callback()
    {
        var clicks = 0;
        var cut = Render<BxButton>(p => p.Add(b => b.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Without_callback_no_click_handler_is_attached()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Label, "Plain"));

        Assert.Throws<MissingEventHandlerException>(() => cut.Find("button").Click());
    }

    [Fact]
    public void Class_and_Ui_override_theme_classes()
    {
        var cut = Render<BxButton>(p => p
            .Add(b => b.Label, "Save")
            .Add(b => b.Class, "px-8 rounded-full")
            .Add(b => b.Ui, new ButtonUi { Label = "font-bold" }));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("px-8", classes);
        Assert.Contains("rounded-full", classes);
        Assert.DoesNotContain("px-2.5", classes);
        Assert.DoesNotContain("rounded-(--blux-radius)", classes);
        Assert.Contains("font-bold", cut.Find("[data-slot=label]").ClassList);
    }

    [Fact]
    public void Square_variant_uses_compound_padding()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Square, true).Add(b => b.Size, BxSize.Lg));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("p-2", classes);
        Assert.DoesNotContain("px-3", classes);
    }

    [Fact]
    public void Custom_color_is_rendered_as_data_attribute()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.Color, (BxColor)"brand"));

        Assert.Equal("brand", cut.Find("button").GetAttribute("data-color"));
    }

    [Fact]
    public void Unmatched_attributes_reach_the_button()
    {
        var cut = Render<BxButton>(p => p.AddUnmatched("aria-label", "Close").AddUnmatched("data-test", "x"));

        Assert.Equal("Close", cut.Find("button").GetAttribute("aria-label"));
    }
}

public class ButtonGlobalThemeTests() : BluxTestContext(o => o.Button.Slot("base", "font-semibold"))
{
    [Fact]
    public void AddLuxUi_override_applies_to_every_button()
    {
        var cut = Render<BxButton>();

        var classes = cut.Find("button").ClassList;
        Assert.Contains("font-semibold", classes);
        Assert.DoesNotContain("font-medium", classes);
    }
}

public class ButtonStaticRenderingTests() : BluxTestContext(interactive: false)
{
    [Fact]
    public void Button_with_a_click_handler_is_disabled_until_interactive()
    {
        var cut = Render<BxButton>(p => p.Add(b => b.OnClick, (MouseEventArgs _) => { }));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Submit_buttons_and_plain_buttons_stay_enabled()
    {
        var submit = Render<BxButton>(p => p.Add(b => b.Type, "submit").Add(b => b.OnClick, (MouseEventArgs _) => { }));
        var plain = Render<BxButton>(p => p.Add(b => b.Label, "Plain"));

        Assert.False(submit.Find("button").HasAttribute("disabled"));
        Assert.False(plain.Find("button").HasAttribute("disabled"));
    }
}
