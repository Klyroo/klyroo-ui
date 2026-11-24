using Microsoft.AspNetCore.Components;

namespace Lux.UI.Tests;

public class DisplayTests : BluxTestContext
{
    [Fact]
    public void Badge_renders_label_color_and_variant()
    {
        var cut = Render<BxBadge>(p => p.Add(b => b.Label, "New").Add(b => b.Color, BxColor.Success).Add(b => b.Variant, BxVariant.Soft));

        var badge = cut.Find("span");
        Assert.Equal("success", badge.GetAttribute("data-color"));
        Assert.Contains("bg-(--bx-color)/10", badge.ClassList);
        Assert.Equal("New", badge.TextContent.Trim());
    }

    [Fact]
    public void Avatar_without_image_is_an_image_named_by_alt_showing_initials()
    {
        var cut = Render<BxAvatar>(p => p.Add(a => a.Alt, "Ada Lovelace King"));

        var root = cut.Find("span");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Equal("Ada Lovelace King", root.GetAttribute("aria-label"));
        Assert.Equal("AL", cut.Find("[aria-hidden=true]").TextContent.Trim());
    }

    [Fact]
    public void Avatar_initials_use_the_text_color_not_the_muted_one_because_the_fill_is_darker_than_the_page()
    {
        var cut = Render<BxAvatar>(p => p.Add(a => a.Alt, "Ada Lovelace"));

        // text-muted on bg-accented was 3.98:1 in dark mode with the zinc neutrals (see PaletteContrastTests).
        var classes = cut.Find("[aria-hidden=true]").ClassName ?? "";
        Assert.Contains("text-(--blux-text)", classes);
        Assert.DoesNotContain("text-muted", classes);
    }

    [Fact]
    public void Avatar_with_image_keeps_the_name_on_the_root_for_when_it_fails()
    {
        var cut = Render<BxAvatar>(p => p.Add(a => a.Src, "/a.png").Add(a => a.Alt, "Ada"));

        Assert.Equal("Ada", cut.Find("span").GetAttribute("aria-label"));
        var img = cut.Find("img");
        Assert.Equal("", img.GetAttribute("alt"));
        Assert.True(img.HasAttribute("data-bx-avatar-image"));
    }

    [Fact]
    public void Card_renders_only_the_given_sections()
    {
        var cut = Render<BxCard>(p => p.Add(c => c.Header, "<h3>Title</h3>").Add(c => c.ChildContent, "Body"));

        Assert.Single(cut.FindAll("[data-slot=header]"));
        Assert.Single(cut.FindAll("[data-slot=body]"));
        Assert.Empty(cut.FindAll("[data-slot=footer]"));
    }

    [Fact]
    public void Alert_close_removes_it_and_notifies()
    {
        var closed = false;
        var cut = Render<BxAlert>(p => p.Add(a => a.Title, "Saved").Add(a => a.Close, true).Add(a => a.OnClose, () => closed = true));

        cut.Find("button[aria-label=Close]").Click();

        Assert.True(closed);
        Assert.Empty(cut.FindAll("[data-color]"));
    }
}

public class DisplayStaticTests() : BluxTestContext(interactive: false)
{
    [Fact]
    public void Alert_close_button_is_disabled_until_interactive()
    {
        var cut = Render<BxAlert>(p => p.Add(a => a.Title, "Saved").Add(a => a.Close, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }
}
