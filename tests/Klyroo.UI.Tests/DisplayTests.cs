using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Tests;

public class DisplayTests : KlyrooTestContext
{
    [Fact]
    public void Badge_renders_label_color_and_variant()
    {
        var cut = Render<KlyBadge>(p => p.Add(b => b.Label, "New").Add(b => b.Color, KlyColor.Success).Add(b => b.Variant, KlyVariant.Soft));

        var badge = cut.Find("span");
        Assert.Equal("success", badge.GetAttribute("data-color"));
        Assert.Contains("bg-(--kly-color)/10", badge.ClassList);
        Assert.Equal("New", badge.TextContent.Trim());
    }

    [Theory]
    [InlineData(KlySize.Xs, "size-3")]
    [InlineData(KlySize.Md, "size-4")]
    [InlineData(KlySize.Xl, "size-6")]
    public void Badge_icon_sits_in_a_centered_box_of_its_size_and_the_drawing_fills_it(KlySize size, string box)
    {
        var cut = Render<KlyBadge>(p => p
            .Add(b => b.Label, "New")
            .Add(b => b.Size, size)
            .Add(b => b.LeadingIcon, (RenderFragment)(b => b.AddContent(0, (MarkupString)"<svg class=\"size-5\"></svg>")))
            .Add(b => b.TrailingIcon, (RenderFragment)(b => b.AddContent(0, (MarkupString)"<svg></svg>"))));

        foreach (var wrapper in cut.FindAll("span[aria-hidden=true]"))
        {
            Assert.Contains(box, wrapper.ClassList);
            Assert.Contains("inline-flex", wrapper.ClassList);
            Assert.Contains("items-center", wrapper.ClassList);
            Assert.Contains("justify-center", wrapper.ClassList);
            Assert.Contains("[&>svg]:size-full", wrapper.ClassList);
        }
    }

    [Fact]
    public void Avatar_without_image_is_an_image_named_by_alt_showing_initials()
    {
        var cut = Render<KlyAvatar>(p => p.Add(a => a.Alt, "Ada Lovelace King"));

        var root = cut.Find("span");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Equal("Ada Lovelace King", root.GetAttribute("aria-label"));
        Assert.Equal("AL", cut.Find("[aria-hidden=true]").TextContent.Trim());
    }

    [Fact]
    public void Avatar_initials_use_the_text_color_not_the_muted_one_because_the_fill_is_darker_than_the_page()
    {
        var cut = Render<KlyAvatar>(p => p.Add(a => a.Alt, "Ada Lovelace"));

        // text-muted on bg-accented was 3.98:1 in dark mode with the zinc neutrals (see PaletteContrastTests).
        var classes = cut.Find("[aria-hidden=true]").ClassName ?? "";
        Assert.Contains("text-(--klyroo-text)", classes);
        Assert.DoesNotContain("text-muted", classes);
    }

    [Fact]
    public void Avatar_with_image_keeps_the_name_on_the_root_for_when_it_fails()
    {
        var cut = Render<KlyAvatar>(p => p.Add(a => a.Src, "/a.png").Add(a => a.Alt, "Ada"));

        Assert.Equal("Ada", cut.Find("span").GetAttribute("aria-label"));
        var img = cut.Find("img");
        Assert.Equal("", img.GetAttribute("alt"));
        Assert.True(img.HasAttribute("data-kly-avatar-image"));
    }

    [Fact]
    public void Card_renders_only_the_given_sections()
    {
        var cut = Render<KlyCard>(p => p.Add(c => c.Header, "<h3>Title</h3>").Add(c => c.ChildContent, "Body"));

        Assert.Single(cut.FindAll("[data-slot=header]"));
        Assert.Single(cut.FindAll("[data-slot=body]"));
        Assert.Empty(cut.FindAll("[data-slot=footer]"));
    }

    [Fact]
    public void Alert_close_removes_it_and_notifies()
    {
        var closed = false;
        var cut = Render<KlyAlert>(p => p.Add(a => a.Title, "Saved").Add(a => a.Close, true).Add(a => a.OnClose, () => closed = true));

        cut.Find("button[aria-label=Close]").Click();

        Assert.True(closed);
        Assert.Empty(cut.FindAll("[data-color]"));
    }
}

public class DisplayStaticTests() : KlyrooTestContext(interactive: false)
{
    [Fact]
    public void Alert_close_button_is_disabled_until_interactive()
    {
        var cut = Render<KlyAlert>(p => p.Add(a => a.Title, "Saved").Add(a => a.Close, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }
}
