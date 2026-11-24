using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lux.UI.Tests;

public class PageTests : LuxTestContext
{
    private static readonly PageLinkItem[] Links =
    [
        new("Docs", "/docs"),
        new("Source", "https://example.com/") { Target = "_blank", Icon = "code", Color = LxColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    // Page

    [Fact]
    public void Page_center_takes_the_width_the_side_columns_leave()
    {
        var alone = Render<LxPage>(p => p.AddChildContent("<p>main</p>"));
        var one = Render<LxPage>(p => p.AddChildContent("<p>main</p>").Add(c => c.Left, "<nav>left</nav>"));
        var both = Render<LxPage>(p => p.AddChildContent("<p>main</p>").Add(c => c.Left, "<nav>left</nav>").Add(c => c.Right, "<nav>right</nav>"));

        Assert.Contains("lg:col-span-10", alone.Find("[data-slot=center]").ClassList);
        Assert.Empty(alone.FindAll("[data-slot=left],[data-slot=right]"));
        Assert.Contains("lg:col-span-8", one.Find("[data-slot=center]").ClassList);
        Assert.Contains("lg:col-span-6", both.Find("[data-slot=center]").ClassList);
        Assert.Equal("left", both.Find("[data-slot=left]").TextContent);
        Assert.Equal("right", both.Find("[data-slot=right]").TextContent);
    }

    // PageHeader

    [Fact]
    public void PageHeader_renders_the_title_as_the_main_heading_by_default()
    {
        var cut = Render<LxPageHeader>(p => p.Add(c => c.Title, "Changelog").Add(c => c.Headline, "Releases").Add(c => c.Description, "What changed"));

        Assert.Equal("Changelog", cut.Find("h1").TextContent);
        Assert.Equal("Releases", cut.Find("[data-slot=headline]").TextContent);
        Assert.Equal("What changed", cut.Find("[data-slot=description]").TextContent);
        Assert.Single(cut.FindAll("header"));
    }

    [Fact]
    public void PageHeader_heading_level_is_configurable_and_a_missing_title_renders_no_heading()
    {
        var level3 = Render<LxPageHeader>(p => p.Add(c => c.Title, "T").Add(c => c.HeadingLevel, 3));
        var none = Render<LxPageHeader>(p => p.Add(c => c.Description, "only text"));

        Assert.Equal("T", level3.Find("h3").TextContent);
        Assert.Empty(level3.FindAll("h1"));
        Assert.Empty(none.FindAll("h1,h2,h3,h4,h5,h6"));
    }

    [Fact]
    public void PageHeader_links_are_buttons_with_their_own_color_and_variant()
    {
        var cut = Render<LxPageHeader>(p => p.Add(c => c.Title, "T").Add(c => c.Links, Links));

        var anchors = cut.FindAll("a");
        Assert.Equal(2, anchors.Count);
        Assert.Equal("/docs", anchors[0].GetAttribute("href"));
        Assert.Equal("_blank", anchors[1].GetAttribute("target"));
        Assert.Equal("neutral", anchors[1].GetAttribute("data-color"));
        Assert.NotNull(anchors[1].QuerySelector("svg"));
    }

    // PageHero

    [Fact]
    public void PageHero_is_a_section_named_by_its_heading()
    {
        var cut = Render<LxPageHero>(p => p
            .Add(c => c.Title, "Ship faster")
            .Add(c => c.Description, "Short text")
            .Add(c => c.Links, Links)
            .Add(c => c.Media, "<div id=\"demo\">demo</div>"));

        var section = cut.Find("section");
        var heading = cut.Find("h1");
        Assert.Equal(heading.Id, section.GetAttribute("aria-labelledby"));
        Assert.Equal(2, cut.FindAll("a").Count);
        Assert.Equal("demo", cut.Find("[data-slot=media] #demo").TextContent);
    }

    [Fact]
    public void PageHero_orientation_and_reverse_change_the_layout()
    {
        var horizontal = Render<LxPageHero>(p => p.Add(c => c.Title, "T").Add(c => c.Media, "<i>m</i>").Add(c => c.Reverse, true));
        var vertical = Render<LxPageHero>(p => p.Add(c => c.Title, "T").Add(c => c.Orientation, LxOrientation.Vertical));

        Assert.Contains("lg:order-first", horizontal.Find("[data-slot=media]").ClassList);
        Assert.Contains("text-center", vertical.Find("section > div > div").ClassList);
    }

    [Fact]
    public void PageHero_without_a_title_has_no_label_reference()
    {
        var cut = Render<LxPageHero>(p => p.Add(c => c.Description, "text"));

        Assert.False(cut.Find("section").HasAttribute("aria-labelledby"));
    }

    // PageSection and PageFeature

    [Fact]
    public void PageSection_lists_its_features_and_names_itself_by_the_title()
    {
        var cut = Render<LxPageSection>(p => p
            .Add(c => c.Title, "Why Lux")
            .Add(c => c.Headline, "Features")
            .Add(c => c.Icon, "zap")
            .Add(c => c.Features, new PageFeatureItem[]
            {
                new("Fast", "Static first.") { Icon = "zap" },
                new("Themeable", "CSS variables.") { Href = "/docs/theming" },
            }));

        Assert.Equal(cut.Find("h2").Id, cut.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal(2, cut.FindAll("ul > li").Count);
        Assert.Equal(["/docs/theming"], cut.FindAll("li a").Select(a => a.GetAttribute("href")).ToArray());
        Assert.Empty(cut.FindAll("li h3"));
        Assert.Equal("true", cut.Find("[data-slot=leading]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void PageSection_feature_titles_become_headings_on_request()
    {
        var cut = Render<LxPageSection>(p => p
            .Add(c => c.Title, "Why Lux")
            .Add(c => c.FeatureHeadingLevel, 3)
            .Add(c => c.Features, new PageFeatureItem[] { new("Fast", "Static first.") }));

        Assert.Equal("Fast", cut.Find("li h3").TextContent);
    }

    [Fact]
    public void PageFeature_without_href_has_no_link()
    {
        var cut = Render<LxPageFeature>(p => p.Add(c => c.Title, "Fast").Add(c => c.Description, "Static first.").Add(c => c.Icon, "zap"));

        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("Fast", cut.Find("p[data-slot=title]").TextContent);
        Assert.Equal("true", cut.Find("[data-slot=leading]").GetAttribute("aria-hidden"));
        Assert.Equal("primary", cut.Find("div[data-color]").GetAttribute("data-color"));
    }

    [Fact]
    public void PageFeature_with_href_has_one_link_around_the_title_and_stretches_over_the_feature()
    {
        var cut = Render<LxPageFeature>(p => p
            .Add(c => c.Title, "Docs")
            .Add(c => c.Description, "Read them.")
            .Add(c => c.Href, "/docs")
            .Add(c => c.HeadingLevel, 3));

        var link = Assert.Single(cut.FindAll("a"));
        Assert.Equal("/docs", link.GetAttribute("href"));
        Assert.Equal("h3", link.ParentElement!.TagName.ToLowerInvariant());
        Assert.Contains("after:absolute", link.ClassList);
        Assert.DoesNotContain("Read them.", link.TextContent);
    }

    [Fact]
    public void PageFeature_link_that_opens_a_new_tab_says_so()
    {
        var cut = Render<LxPageFeature>(p => p.Add(c => c.Title, "Source").Add(c => c.Href, "https://example.com").Add(c => c.Target, "_blank"));

        var link = cut.Find("a");
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Contains("(opens in a new tab)", link.QuerySelector(".sr-only")!.TextContent);
    }

    // PageGrid and PageColumns

    [Theory]
    [InlineData(1, "sm:grid-cols-2")]
    [InlineData(3, "lg:grid-cols-3")]
    [InlineData(9, "lg:grid-cols-4")]
    public void PageGrid_columns_are_clamped_between_two_and_four(int columns, string expected)
    {
        var cut = Render<LxPageGrid>(p => p.Add(c => c.Columns, columns).AddChildContent("<div>a</div>"));

        Assert.Contains(expected, cut.Find("div").ClassList);
    }

    [Fact]
    public void PageColumns_uses_css_columns_and_keeps_items_whole()
    {
        var cut = Render<LxPageColumns>(p => p.Add(c => c.Columns, 2).AddChildContent("<div>a</div>"));

        var root = cut.Find("div");
        Assert.Contains("sm:columns-2", root.ClassList);
        Assert.Contains("*:break-inside-avoid", root.ClassList);
    }

    // PageCard

    [Fact]
    public void PageCard_with_href_has_a_single_link_inside_the_heading_and_nothing_interactive_around_it()
    {
        var cut = Render<LxPageCard>(p => p
            .Add(c => c.Title, "Documentation")
            .Add(c => c.Description, "Start here.")
            .Add(c => c.Icon, "star")
            .Add(c => c.Href, "/docs"));

        var link = Assert.Single(cut.FindAll("a"));
        Assert.Equal("h3", link.ParentElement!.TagName.ToLowerInvariant());
        Assert.Equal("Documentation", link.TextContent);
        Assert.Contains("after:absolute", link.ClassList);
        Assert.Contains("after:inset-0", link.ClassList);
        Assert.Empty(cut.FindAll("button,input,select,textarea,a a"));
        Assert.Contains("has-[a:focus-visible]:outline-2", cut.Find("div[data-color]").ClassList);
    }

    [Fact]
    public void PageCard_without_href_is_not_interactive()
    {
        var cut = Render<LxPageCard>(p => p.Add(c => c.Title, "Fast").Add(c => c.Description, "Light."));

        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("Fast", cut.Find("h3").TextContent);
    }

    [Fact]
    public void PageCard_heading_level_is_configurable_and_null_renders_a_paragraph()
    {
        var h4 = Render<LxPageCard>(p => p.Add(c => c.Title, "T").Add(c => c.HeadingLevel, 4));
        var plain = Render<LxPageCard>(p => p.Add(c => c.Title, "T").Add(c => c.HeadingLevel, null));

        Assert.Equal("T", h4.Find("h4").TextContent);
        Assert.Empty(plain.FindAll("h1,h2,h3,h4,h5,h6"));
        Assert.Equal("T", plain.Find("p[data-slot=title]").TextContent);
    }

    [Theory]
    [InlineData(PageCardVariant.Outline, false)]
    [InlineData(PageCardVariant.Soft, false)]
    [InlineData(PageCardVariant.Subtle, false)]
    [InlineData(PageCardVariant.Spotlight, true)]
    public void PageCard_renders_the_decorative_spotlight_only_in_its_variant(PageCardVariant variant, bool spotlight)
    {
        var cut = Render<LxPageCard>(p => p.Add(c => c.Title, "T").Add(c => c.Variant, variant));

        var glow = cut.FindAll("[data-slot=spotlight]");
        Assert.Equal(spotlight, glow.Count == 1);
        if (spotlight)
        {
            Assert.Equal("true", glow[0].GetAttribute("aria-hidden"));
        }
    }

    [Fact]
    public void PageCard_ui_overrides_a_slot_and_header_body_footer_render_in_order()
    {
        var cut = Render<LxPageCard>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Ui, new PageCardUi { Title = "text-xl" })
            .Add(c => c.Header, "<img alt=\"\" src=\"x.png\" />")
            .AddChildContent("<span id=\"body\">body</span>")
            .Add(c => c.Footer, "<span id=\"foot\">foot</span>"));

        Assert.Contains("text-xl", cut.Find("h3").ClassList);
        var html = cut.Markup;
        Assert.True(html.IndexOf("x.png", StringComparison.Ordinal) < html.IndexOf("id=\"body\"", StringComparison.Ordinal));
        Assert.True(html.IndexOf("id=\"body\"", StringComparison.Ordinal) < html.IndexOf("id=\"foot\"", StringComparison.Ordinal));
    }

    // PageCta

    [Fact]
    public void PageCta_is_a_section_named_by_its_heading_with_its_links()
    {
        var cut = Render<LxPageCta>(p => p
            .Add(c => c.Title, "Start today")
            .Add(c => c.Description, "It takes a minute.")
            .Add(c => c.Links, Links)
            .Add(c => c.Variant, LxVariant.Solid));

        Assert.Equal(cut.Find("h2").Id, cut.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal(2, cut.FindAll("a").Count);
        Assert.Contains("bg-(--lx-color)", cut.Find("section > div").ClassList);
        Assert.Contains("text-(--lx-color-contrast)", cut.Find("h2").ClassList);
    }

    [Fact]
    public void PageCta_soft_variant_uses_text_tokens_for_the_title()
    {
        var cut = Render<LxPageCta>(p => p.Add(c => c.Title, "T").Add(c => c.Variant, LxVariant.Soft).Add(c => c.HeadingLevel, 3));

        Assert.Contains("text-(--lux-text-highlighted)", cut.Find("h3").ClassList);
        Assert.Contains("bg-(--lx-color)/10", cut.Find("section > div").ClassList);
    }

    // PageLinks

    [Fact]
    public void PageLinks_is_a_navigation_named_by_its_title()
    {
        var cut = Render<LxPageLinks>(p => p.Add(c => c.Title, "Resources").Add(c => c.Links, Links));

        var nav = cut.Find("nav");
        Assert.Equal(cut.Find("p[data-slot=title]").Id, nav.GetAttribute("aria-labelledby"));
        Assert.False(nav.HasAttribute("aria-label"));
        Assert.Equal(2, cut.FindAll("li a").Count);
    }

    [Fact]
    public void PageLinks_without_a_title_uses_the_built_in_label_and_it_can_be_localized()
    {
        var plain = Render<LxPageLinks>(p => p.Add(c => c.Links, Links));
        Assert.Equal("Links", plain.Find("nav").GetAttribute("aria-label"));
        Assert.False(plain.Find("nav").HasAttribute("aria-labelledby"));

        // LuxOptions.Locale is read-only once a component has resolved a locale: a locale for one subtree is a cascading value.
        var liens = LxLocales.Extend(LxLocales.En, messages: m => m with { PageLinks = "Liens" });
        var french = Render<LxPageLinks>(p => p.AddCascadingValue(liens).Add(c => c.Links, Links));
        Assert.Equal("Liens", french.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void PageLinks_title_can_be_a_heading()
    {
        var cut = Render<LxPageLinks>(p => p.Add(c => c.Title, "Resources").Add(c => c.HeadingLevel, 3).Add(c => c.Links, Links));

        Assert.Equal("Resources", cut.Find("h3").TextContent);
    }

    [Fact]
    public void PageLinks_marks_the_active_link_and_the_disabled_one()
    {
        var cut = Render<LxPageLinks>(p => p.Add(c => c.Links, new PageLinkItem[]
        {
            new("Here", "/docs") { Active = true },
            new("There", "/other") { Active = false },
            new("Soon", "/soon") { Active = false, Disabled = true },
        }));

        var anchors = cut.FindAll("a");
        Assert.Equal("page", anchors[0].GetAttribute("aria-current"));
        Assert.False(anchors[1].HasAttribute("aria-current"));
        Assert.Equal("true", anchors[2].GetAttribute("aria-disabled"));
        Assert.False(anchors[2].HasAttribute("href"));
    }

    [Fact]
    public void PageLinks_external_link_gets_an_icon_hidden_from_assistive_technology_and_a_hidden_hint()
    {
        var cut = Render<LxPageLinks>(p => p.Add(c => c.Links, new PageLinkItem[] { new("Source", "https://example.com/") { Target = "_blank", Active = false } }));

        var link = cut.Find("a");
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Equal("true", link.QuerySelector("svg[data-icon=arrow-up-right]")!.GetAttribute("aria-hidden"));
        Assert.Contains("(opens in a new tab)", link.QuerySelector(".sr-only")!.TextContent);
    }

    // PageList

    [Fact]
    public void PageList_is_a_list_with_dividers_by_default()
    {
        var cut = Render<LxPageList>(p => p.AddChildContent("<li>a</li><li>b</li>"));
        var plain = Render<LxPageList>(p => p.Add(c => c.Divide, false).AddChildContent("<li>a</li>"));

        Assert.Equal(2, cut.FindAll("ul > li").Count);
        Assert.Contains("divide-y", cut.Find("ul").ClassList);
        Assert.DoesNotContain("divide-y", plain.Find("ul").ClassList);
        Assert.Contains("gap-8", plain.Find("ul").ClassList);
    }

    // PageLogos

    [Fact]
    public void PageLogos_lists_logos_with_accessible_names()
    {
        var cut = Render<LxPageLogos>(p => p
            .Add(c => c.Title, "Trusted by")
            .Add(c => c.Items, new PageLogoItem[]
            {
                new("Voltline") { Icon = "zap" },
                new("Meridian") { Src = "/meridian.svg" },
            }));

        Assert.Equal("Trusted by", cut.Find("h2").TextContent);
        Assert.Equal(2, cut.FindAll("ul > li").Count);
        var icon = cut.Find("svg[role=img]");
        Assert.Equal("Voltline", icon.GetAttribute("aria-label"));
        Assert.Equal("Meridian", cut.Find("img").GetAttribute("alt"));
    }

    [Fact]
    public void PageLogos_marquee_repeats_the_logos_hidden_from_assistive_technology()
    {
        var cut = Render<LxPageLogos>(p => p
            .Add(c => c.Marquee, true)
            .Add(c => c.Items, new PageLogoItem[] { new("Voltline") { Icon = "zap" } }));

        Assert.NotNull(cut.Find("[data-lx-marquee]"));
        var groups = cut.FindAll("[data-lx-marquee-group]");
        Assert.Equal(2, groups.Count);
        Assert.False(groups[0].HasAttribute("aria-hidden"));
        Assert.Equal("true", groups[1].GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll("ul"));
    }

    [Fact]
    public void PageLogos_title_can_be_a_paragraph()
    {
        var cut = Render<LxPageLogos>(p => p.Add(c => c.Title, "Trusted by").Add(c => c.HeadingLevel, null).AddChildContent("<span>custom</span>"));

        Assert.Empty(cut.FindAll("h1,h2,h3,h4,h5,h6"));
        Assert.Equal("Trusted by", cut.Find("p[data-slot=title]").TextContent);
        Assert.Equal("custom", cut.Find("[data-slot=items]").TextContent);
    }

    // PageAnchors

    [Fact]
    public void PageAnchors_renders_plain_fragment_links_with_no_current_state()
    {
        var cut = Render<LxPageAnchors>(p => p.Add(c => c.Links, new PageLinkItem[]
        {
            new("Install", "#install") { Icon = "download" },
            new("Source", "https://example.com/") { Target = "_blank" },
        }));

        Assert.Equal("On this page", cut.Find("nav").GetAttribute("aria-label"));
        var anchors = cut.FindAll("li a");
        Assert.Equal("/#install", anchors[0].GetAttribute("href"));
        Assert.All(anchors, a => Assert.False(a.HasAttribute("aria-current")));
        Assert.Equal("true", anchors[0].QuerySelector("[aria-hidden=true]")!.GetAttribute("aria-hidden"));
        Assert.Equal("noopener noreferrer", anchors[1].GetAttribute("rel"));
    }

    [Fact]
    public void PageAnchors_fragment_links_keep_the_current_page_so_a_base_href_cannot_redirect_them()
    {
        // With <base href="/">, a bare "#install" resolves to "/#install": the browser would leave the page.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/docs/theming?tab=2#old");
        var cut = Render<LxPageAnchors>(p => p.Add(c => c.Links, new PageLinkItem[] { new("Install", "#install"), new("Other", "/elsewhere#x") }));

        var anchors = cut.FindAll("li a");
        Assert.Equal("/docs/theming?tab=2#install", anchors[0].GetAttribute("href"));
        Assert.Equal("/elsewhere#x", anchors[1].GetAttribute("href"));
    }

    [Fact]
    public void PageAnchors_label_can_be_localized_or_replaced()
    {
        Services.GetRequiredService<LuxOptions>().Locale = LxLocales.Extend(LxLocales.En, messages: m => m with { PageAnchors = "Sur cette page" });
        var localized = Render<LxPageAnchors>(p => p.Add(c => c.Links, new PageLinkItem[] { new("A", "#a") }));
        var named = Render<LxPageAnchors>(p => p.Add(c => c.Links, new PageLinkItem[] { new("A", "#a") }).AddUnmatched("aria-label", "Contents"));

        Assert.Equal("Sur cette page", localized.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Contents", named.Find("nav").GetAttribute("aria-label"));
    }

    // PageAside and PageBody

    [Fact]
    public void PageAside_is_a_sticky_aside_with_optional_top_and_bottom()
    {
        var cut = Render<LxPageAside>(p => p
            .Add(c => c.Top, "<b>top</b>")
            .AddChildContent("<i>body</i>")
            .Add(c => c.Bottom, "<u>bottom</u>"));

        var aside = cut.Find("aside");
        Assert.Contains("sticky", aside.ClassList);
        Assert.Contains("hidden", aside.ClassList);
        Assert.Contains("lg:block", aside.ClassList);
        Assert.Equal("top", cut.Find("[data-slot=top]").TextContent);
        Assert.Equal("bottom", cut.Find("[data-slot=bottom]").TextContent);
    }

    [Fact]
    public void PageAside_can_render_as_a_div_to_avoid_nesting_landmarks()
    {
        var cut = Render<LxPageAside>(p => p.Add(c => c.As, "div").AddChildContent("<i>body</i>"));

        Assert.Empty(cut.FindAll("aside"));
        Assert.Contains("sticky", cut.Find("div").ClassList);
        Assert.Empty(cut.FindAll("[data-slot=top],[data-slot=bottom]"));
    }

    [Fact]
    public void PageBody_wraps_the_content_in_the_prose_styles()
    {
        var cut = Render<LxPageBody>(p => p.AddChildContent("<h2>Title</h2><p>Text</p>"));

        var root = cut.Find("div");
        Assert.Contains("[&_h2]:text-2xl", root.ClassList);
        Assert.Contains("[&_a]:underline", root.ClassList);
        Assert.Equal("Title", cut.Find("h2").TextContent);
    }

    [Fact]
    public void Class_is_added_to_the_root_of_every_page_component()
    {
        Assert.Contains("extra", Render<LxPage>(p => p.Add(c => c.Class, "extra")).Find("div").ClassList);
        Assert.Contains("extra", Render<LxPageHeader>(p => p.Add(c => c.Class, "extra")).Find("header").ClassList);
        Assert.Contains("extra", Render<LxPageHero>(p => p.Add(c => c.Class, "extra")).Find("section").ClassList);
        Assert.Contains("extra", Render<LxPageCta>(p => p.Add(c => c.Class, "extra")).Find("section").ClassList);
        Assert.Contains("extra", Render<LxPageLinks>(p => p.Add(c => c.Class, "extra")).Find("nav").ClassList);
        Assert.Contains("extra", Render<LxPageAnchors>(p => p.Add(c => c.Class, "extra")).Find("nav").ClassList);
        Assert.Contains("extra", Render<LxPageList>(p => p.Add(c => c.Class, "extra")).Find("ul").ClassList);
    }
}
