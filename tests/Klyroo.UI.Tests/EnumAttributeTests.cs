namespace Klyroo.UI.Tests;

/// <summary>The enums that replaced closed sets of text render the spelling the markup always had.</summary>
public class EnumAttributeTests : KlyrooTestContext
{
    [Theory]
    [InlineData(KlyPlacement.Top, "top")]
    [InlineData(KlyPlacement.TopStart, "top-start")]
    [InlineData(KlyPlacement.TopEnd, "top-end")]
    [InlineData(KlyPlacement.Right, "right")]
    [InlineData(KlyPlacement.RightStart, "right-start")]
    [InlineData(KlyPlacement.RightEnd, "right-end")]
    [InlineData(KlyPlacement.Bottom, "bottom")]
    [InlineData(KlyPlacement.BottomStart, "bottom-start")]
    [InlineData(KlyPlacement.BottomEnd, "bottom-end")]
    [InlineData(KlyPlacement.Left, "left")]
    [InlineData(KlyPlacement.LeftStart, "left-start")]
    [InlineData(KlyPlacement.LeftEnd, "left-end")]
    public void Placement_is_rendered_in_kebab_case_by_tooltip_popover_and_menu(KlyPlacement placement, string expected)
    {
        Assert.Equal(expected, Render<KlyTooltip>(p => p.Add(t => t.Placement, placement).Add(t => t.Text, "Tip")).Find("kly-tooltip").GetAttribute("placement"));
        Assert.Equal(expected, Render<KlyPopover>(p => p.Add(t => t.Placement, placement)).Find("kly-popover").GetAttribute("placement"));
        Assert.Equal(expected, Render<KlyDropdownMenu>(p => p.Add(t => t.Placement, placement)).Find("kly-popover").GetAttribute("placement"));
    }

    [Fact]
    public void Placement_defaults_keep_their_old_values()
    {
        Assert.Equal("top", Render<KlyTooltip>(p => p.Add(t => t.Text, "Tip")).Find("kly-tooltip").GetAttribute("placement"));
        Assert.Equal("bottom", Render<KlyPopover>().Find("kly-popover").GetAttribute("placement"));
        Assert.Equal("bottom-start", Render<KlyDropdownMenu>().Find("kly-popover").GetAttribute("placement"));
    }

    [Theory]
    [InlineData(InputType.Text, "text")]
    [InlineData(InputType.Email, "email")]
    [InlineData(InputType.Password, "password")]
    [InlineData(InputType.Search, "search")]
    [InlineData(InputType.Tel, "tel")]
    [InlineData(InputType.Url, "url")]
    [InlineData(InputType.Number, "number")]
    [InlineData(InputType.Date, "date")]
    [InlineData(InputType.Time, "time")]
    [InlineData(InputType.DateTimeLocal, "datetime-local")]
    [InlineData(InputType.Month, "month")]
    [InlineData(InputType.Week, "week")]
    public void Input_type_is_the_html_type(InputType type, string expected) =>
        Assert.Equal(expected, Render<KlyInput>(p => p.Add(i => i.Type, type)).Find("input").GetAttribute("type"));

    [Fact]
    public void Input_and_button_default_to_text_and_button()
    {
        Assert.Equal("text", Render<KlyInput>().Find("input").GetAttribute("type"));
        Assert.Equal("button", Render<KlyButton>(p => p.Add(b => b.Label, "Go")).Find("button").GetAttribute("type"));
    }

    [Theory]
    [InlineData(ButtonType.Button, "button")]
    [InlineData(ButtonType.Submit, "submit")]
    [InlineData(ButtonType.Reset, "reset")]
    public void Button_type_is_the_html_type(ButtonType type, string expected) =>
        Assert.Equal(expected, Render<KlyButton>(p => p.Add(b => b.Label, "Go").Add(b => b.Type, type)).Find("button").GetAttribute("type"));

    [Theory]
    [InlineData(KlyElement.Div, "div")]
    [InlineData(KlyElement.Section, "section")]
    [InlineData(KlyElement.Article, "article")]
    [InlineData(KlyElement.Aside, "aside")]
    [InlineData(KlyElement.Header, "header")]
    [InlineData(KlyElement.Footer, "footer")]
    [InlineData(KlyElement.Main, "main")]
    [InlineData(KlyElement.Nav, "nav")]
    public void Container_and_main_render_the_chosen_element(KlyElement element, string expected)
    {
        Assert.Equal(expected, Render<KlyContainer>(p => p.Add(c => c.As, element).AddChildContent("x")).Find(expected).LocalName);
        Assert.Equal(expected, Render<KlyMain>(p => p.Add(c => c.As, element).AddChildContent("x")).Find(expected).LocalName);
    }

    [Fact]
    public void Container_and_main_keep_their_default_elements()
    {
        Assert.NotEmpty(Render<KlyContainer>(p => p.AddChildContent("x")).FindAll("div"));
        Assert.NotEmpty(Render<KlyMain>(p => p.AddChildContent("x")).FindAll("main"));
    }

    [Fact]
    public void Error_title_uses_the_heading_level_and_clamps_it()
    {
        Assert.Equal("h1", Render<KlyError>(p => p.Add(e => e.StatusCode, 404)).Find("h1,h2,h3,h4,h5,h6").LocalName);
        Assert.Equal("h3", Render<KlyError>(p => p.Add(e => e.StatusCode, 404).Add(e => e.HeadingLevel, 3)).Find("h1,h2,h3,h4,h5,h6").LocalName);
        Assert.Equal("h6", Render<KlyError>(p => p.Add(e => e.StatusCode, 404).Add(e => e.HeadingLevel, 9)).Find("h1,h2,h3,h4,h5,h6").LocalName);
    }

    [Theory]
    [InlineData(KlyDirection.Ltr, "ltr")]
    [InlineData(KlyDirection.Rtl, "rtl")]
    [InlineData(KlyDirection.Auto, "auto")]
    public void App_dir_is_the_html_dir(KlyDirection direction, string expected) =>
        Assert.Equal(expected, Render<KlyApp>(p => p.Add(a => a.Toaster, false).Add(a => a.Dir, direction)).Find("[dir]").GetAttribute("dir"));

    [Fact]
    public void Locale_direction_attribute_follows_the_enum()
    {
        Assert.Equal("rtl", KlyLocales.Ar.DirAttribute);
        Assert.Equal("ltr", KlyLocales.En.DirAttribute);
        Assert.Equal("auto", KlyLocales.Extend(KlyLocales.En, "en-x", "English (auto)", KlyDirection.Auto).DirAttribute);
    }
}
