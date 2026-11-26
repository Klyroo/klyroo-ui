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
    }

    [Fact]
    public void A_locale_refuses_the_auto_direction_however_it_is_built()
    {
        Assert.Throws<ArgumentException>(() => new KlyLocale("en-x", "English", KlyDirection.Auto, new KlyMessages()));
        Assert.Throws<ArgumentException>(() => KlyLocales.Extend(KlyLocales.En, "en-x", "English", KlyDirection.Auto));
        Assert.Throws<ArgumentException>(() => KlyLocales.En with { Dir = KlyDirection.Auto });
        Assert.Throws<ArgumentException>(() => new KlyLocale("en-x", "English", (KlyDirection)42, new KlyMessages()));
        Assert.Equal(KlyDirection.Rtl, (KlyLocales.En with { Dir = KlyDirection.Rtl }).Dir);
    }

    [Fact]
    public void Link_target_keywords_and_names_render_their_attribute_value()
    {
        Assert.Equal("_self", KlyLinkTarget.Self.Value);
        Assert.Equal("_blank", KlyLinkTarget.Blank.Value);
        Assert.Equal("_parent", KlyLinkTarget.Parent.Value);
        Assert.Equal("_top", KlyLinkTarget.Top.Value);
        Assert.Equal("preview", KlyLinkTarget.Named("preview").Value);
        Assert.Equal("_self", default(KlyLinkTarget).Value);
        Assert.Equal(KlyLinkTarget.Blank, KlyLinkTarget.Blank);
        Assert.True(KlyLinkTarget.Blank != KlyLinkTarget.Self);
        Assert.True(KlyLinkTarget.Blank.IsBlank);
        Assert.False(KlyLinkTarget.Named("blank").IsBlank);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("_blank")]
    [InlineData("_custom")]
    public void A_named_target_needs_a_name_without_a_leading_underscore(string name) =>
        Assert.ThrowsAny<ArgumentException>(() => KlyLinkTarget.Named(name));

    [Fact]
    public void A_link_that_opens_a_new_tab_is_protected_and_a_named_frame_is_not()
    {
        var blank = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Target, KlyLinkTarget.Blank)).Find("a");
        Assert.Equal("_blank", blank.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", blank.GetAttribute("rel"));

        var named = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Target, KlyLinkTarget.Named("preview"))).Find("a");
        Assert.Equal("preview", named.GetAttribute("target"));
        Assert.Null(named.GetAttribute("rel"));

        var none = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com")).Find("a");
        Assert.Null(none.GetAttribute("target"));
    }

    [Fact]
    public void A_button_link_that_opens_a_new_tab_is_protected_and_announced()
    {
        var blank = Render<KlyButton>(p => p.Add(b => b.Label, "Docs").Add(b => b.Href, "https://example.com").Add(b => b.Target, KlyLinkTarget.Blank)).Find("a");
        Assert.Equal("noopener noreferrer", blank.GetAttribute("rel"));
        Assert.Contains("(opens in a new tab)", blank.QuerySelector(".sr-only")!.TextContent);

        var named = Render<KlyButton>(p => p.Add(b => b.Label, "Docs").Add(b => b.Href, "https://example.com").Add(b => b.Target, KlyLinkTarget.Named("preview"))).Find("a");
        Assert.Null(named.GetAttribute("rel"));
        Assert.Null(named.QuerySelector(".sr-only"));
    }

    [Fact]
    public void A_button_link_keeps_the_rel_the_caller_passes_next_to_the_protection()
    {
        var blank = Render<KlyButton>(p => p.Add(b => b.Href, "https://example.com").Add(b => b.Target, KlyLinkTarget.Blank)
            .Add(b => b.AdditionalAttributes, new Dictionary<string, object> { ["rel"] = "nofollow noopener" })).Find("a");
        Assert.Equal("nofollow noopener noreferrer", blank.GetAttribute("rel"));

        var self = Render<KlyButton>(p => p.Add(b => b.Href, "https://example.com")
            .Add(b => b.AdditionalAttributes, new Dictionary<string, object> { ["rel"] = "nofollow" })).Find("a");
        Assert.Equal("nofollow", self.GetAttribute("rel"));
    }

    [Fact]
    public void The_links_of_a_banner_a_navigation_menu_and_a_page_hero_that_open_a_new_tab_are_protected()
    {
        var banner = Render<KlyBanner>(p => p.Add(b => b.Title, "News").Add(b => b.Href, "https://example.com").Add(b => b.Target, KlyLinkTarget.Blank)).Find("a");
        Assert.Equal("noopener noreferrer", banner.GetAttribute("rel"));

        var menu = Render<KlyNavigationMenu>(p => p.Add(m => m.Items, new[]
        {
            new KlyNavigationItem("Blog") { Href = "https://example.com", Target = KlyLinkTarget.Blank },
            new KlyNavigationItem("Docs") { Href = "/docs" },
        }));
        var links = menu.FindAll("a");
        Assert.Equal("noopener noreferrer", links[0].GetAttribute("rel"));
        Assert.Contains("(opens in a new tab)", links[0].QuerySelector(".sr-only")!.TextContent);
        Assert.Null(links[1].GetAttribute("rel"));
        Assert.Null(links[1].QuerySelector(".sr-only"));

        var hero = Render<KlyPageHero>(p => p.Add(h => h.Links, new[] { new PageLinkItem("Start", "https://example.com") { Target = KlyLinkTarget.Blank } })).Find("a[target=_blank]");
        Assert.Equal("noopener noreferrer", hero.GetAttribute("rel"));
        Assert.Contains("(opens in a new tab)", hero.QuerySelector(".sr-only")!.TextContent);
    }

    [Fact]
    public void A_link_that_opens_a_new_tab_is_protected_and_announced()
    {
        var blank = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Label, "Docs").Add(l => l.Target, KlyLinkTarget.Blank)).Find("a");
        Assert.Equal("noopener noreferrer", blank.GetAttribute("rel"));
        Assert.Contains("(opens in a new tab)", blank.QuerySelector(".sr-only")!.TextContent);

        var named = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Target, KlyLinkTarget.Named("preview"))).Find("a");
        Assert.Null(named.GetAttribute("rel"));
        Assert.Null(named.QuerySelector(".sr-only"));

        var disabled = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Disabled, true).Add(l => l.Target, KlyLinkTarget.Blank)).Find("a");
        Assert.Null(disabled.GetAttribute("href"));
        Assert.Null(disabled.QuerySelector(".sr-only"));
    }

    [Fact]
    public void A_link_keeps_the_protection_when_the_caller_passes_its_own_rel()
    {
        var blank = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Target, KlyLinkTarget.Blank)
            .Add(l => l.AdditionalAttributes, new Dictionary<string, object> { ["rel"] = "nofollow" })).Find("a");
        Assert.Equal("nofollow noopener noreferrer", blank.GetAttribute("rel"));

        var self = Render<KlyLink>(p => p.Add(l => l.Href, "/docs")
            .Add(l => l.AdditionalAttributes, new Dictionary<string, object> { ["rel"] = "nofollow" })).Find("a");
        Assert.Equal("nofollow", self.GetAttribute("rel"));
    }

    [Fact]
    public void A_user_link_that_opens_a_new_tab_is_protected_announced_and_merges_the_rel_of_the_caller()
    {
        var blank = Render<KlyUser>(p => p.Add(u => u.Name, "Ada").Add(u => u.Href, "https://example.com").Add(u => u.Target, KlyLinkTarget.Blank)
            .Add(u => u.AdditionalAttributes, new Dictionary<string, object> { ["rel"] = "nofollow" })).Find("a");
        Assert.Equal("nofollow noopener noreferrer", blank.GetAttribute("rel"));
        Assert.Contains("(opens in a new tab)", blank.QuerySelector(".sr-only")!.TextContent);

        var self = Render<KlyUser>(p => p.Add(u => u.Name, "Ada").Add(u => u.Href, "/ada")).Find("a");
        Assert.Null(self.GetAttribute("rel"));
        Assert.Null(self.QuerySelector(".sr-only"));
    }
}
