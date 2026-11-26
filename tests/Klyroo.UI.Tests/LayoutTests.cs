using Microsoft.AspNetCore.Components.Rendering;
using System.Globalization;
using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Tests;

public class LayoutTests : KlyrooTestContext
{
    // ---- App

    [Fact]
    public void App_renders_the_toaster_after_the_content_and_sets_lang_and_dir()
    {
        var cut = Render<KlyApp>(p => p.Add(a => a.ChildContent, "<p>Body</p>").Add(a => a.Lang, "fr").Add(a => a.Dir, KlyDirection.Rtl));

        var root = cut.Find("div.isolate");
        Assert.Equal("fr", root.GetAttribute("lang"));
        Assert.Equal("rtl", root.GetAttribute("dir"));
        Assert.Equal("p", root.Children[0].LocalName);
        Assert.Equal("kly-toaster", root.Children[1].LocalName);
    }

    [Fact]
    public void App_can_leave_the_toaster_out()
    {
        var cut = Render<KlyApp>(p => p.Add(a => a.Toaster, false));

        Assert.Empty(cut.FindAll("kly-toaster"));
        // Without Lang and Dir, the resolved locale (English here) writes them.
        Assert.Equal("en", cut.Find("div").GetAttribute("lang"));
        Assert.Equal("ltr", cut.Find("div").GetAttribute("dir"));
    }

    [Fact]
    public void App_cascades_global_overrides_to_its_components()
    {
        var overrides = new Dictionary<Type, ISlotOverrides> { [typeof(KlyButton)] = new ButtonUi { Base = "rounded-full" } };
        var cut = Render<KlyApp>(p => p.Add(a => a.Toaster, false).Add(a => a.Overrides, overrides)
            .Add(a => a.ChildContent, b => b.AddContent(0, (RenderFragment)(inner =>
            {
                inner.OpenComponent<KlyButton>(0);
                inner.AddAttribute(1, nameof(KlyButton.Label), "Go");
                inner.CloseComponent();
            }))));

        Assert.Contains("rounded-full", cut.Find("button").ClassList);
    }

    // ---- Header

    private static readonly KlyNavigationItem[] NavItems =
    [
        new("Home") { Href = "/" },
        new("Docs") { Href = "/docs" },
    ];

    [Fact]
    public void Header_is_a_sticky_header_with_a_title_link_and_a_named_navigation()
    {
        var cut = Render<KlyHeader>(p => p.Add(h => h.Title, "Acme").Add(h => h.Href, "/start").Add(h => h.Items, NavItems));

        var header = cut.Find("header");
        Assert.Contains("sticky", header.ClassList);
        var title = cut.Find("header a[href='/start']");
        Assert.Equal("Acme", title.TextContent.Trim());
        // The same links appear in the middle and in the mobile menu, under different names.
        Assert.Equal(["Main menu", "Menu"], cut.FindAll("nav").Select(n => n.GetAttribute("aria-label")));
    }

    [Fact]
    public void Header_mobile_menu_is_a_dialog_handled_in_the_browser()
    {
        var cut = Render<KlyHeader>(p => p.Add(h => h.Items, NavItems).Add(h => h.MenuTitle, "Site menu"));

        var button = cut.Find("button[aria-label='Open menu']");
        Assert.NotNull(button);
        var dialog = cut.Find("dialog[data-kly-layout-menu]");
        Assert.Equal("Site menu", cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent);
        // Unbound: no sequence rendered, no handler to attach.
        Assert.False(cut.Find("kly-modal").HasAttribute("data-kly-seq"));
        Assert.Contains("lg:hidden", cut.Find("header > div > div:last-child").ClassList);
    }

    [Fact]
    public void Header_without_links_or_body_has_no_menu_button_and_the_menu_can_be_turned_off()
    {
        Assert.Empty(Render<KlyHeader>(p => p.Add(h => h.Title, "Acme")).FindAll("button"));
        Assert.Empty(Render<KlyHeader>(p => p.Add(h => h.Items, NavItems).Add(h => h.Menu, false)).FindAll("dialog"));
    }

    [Fact]
    public void Header_slots_replace_the_defaults()
    {
        var cut = Render<KlyHeader>(p => p
            .Add(h => h.Title, "Ignored")
            .Add(h => h.Left, "<span id=\"logo\">Logo</span>")
            .Add(h => h.ChildContent, "<nav id=\"mine\">Mine</nav>")
            .Add(h => h.Right, "<button id=\"cta\">Go</button>")
            .Add(h => h.Items, NavItems)
            .Add(h => h.Body, "<p id=\"body\">Custom body</p>")
            .Add(h => h.Sticky, false));

        Assert.NotNull(cut.Find("#logo"));
        Assert.DoesNotContain("Ignored", cut.Markup);
        Assert.NotNull(cut.Find("#mine"));
        Assert.NotNull(cut.Find("#cta"));
        Assert.NotNull(cut.Find("dialog #body"));
        Assert.Empty(cut.FindAll("nav[aria-label=Menu]"));
        Assert.DoesNotContain("sticky", cut.Find("header").ClassList);
    }

    // ---- SkipLink

    [Fact]
    public void SkipLink_is_a_link_to_main_with_the_text_of_the_locale()
    {
        var cut = Render<KlySkipLink>();

        var link = cut.Find("a");
        Assert.Equal("#main", link.GetAttribute("href"));
        Assert.True(link.HasAttribute("data-kly-skip-link"));
        Assert.Equal("Skip to content", link.TextContent);
        // Hidden until it has the focus, but still in the tab order: sr-only, not display:none.
        Assert.Contains("sr-only", link.ClassName);
        Assert.Contains("focus:not-sr-only", link.ClassName);
    }

    [Fact]
    public void SkipLink_names_its_target_and_takes_its_own_text_and_classes()
    {
        var cut = Render<KlySkipLink>(p => p.Add(s => s.Target, "content").Add(s => s.Label, "Jump").Add(s => s.Class, "bg-red-500").Add(s => s.Ui, new SkipLinkUi { Root = "uppercase" }));

        var link = cut.Find("a");
        Assert.Equal("#content", link.GetAttribute("href"));
        Assert.Equal("Jump", link.TextContent);
        Assert.Contains("bg-red-500", link.ClassName);
        Assert.Contains("uppercase", link.ClassName);

        cut.Render(p => p.Add(s => s.ChildContent, "Passer"));
        Assert.Equal("Passer", cut.Find("a").TextContent);
    }

    [Fact]
    public void SkipLink_is_translated_in_every_locale()
    {
        foreach (var locale in KlyLocales.All.Where(l => l.Code != "en"))
        {
            Assert.False(string.IsNullOrWhiteSpace(locale.Messages.SkipToContent), locale.Code);
            Assert.NotEqual(KlyLocales.En.Messages.SkipToContent, locale.Messages.SkipToContent);
        }
    }

    [Fact]
    public void SkipLink_follows_the_locale_of_the_app()
    {
        var cut = Render<KlyApp>(p => p.Add(a => a.Locale, KlyLocales.Fr).Add(a => a.ChildContent, (RenderFragment)(b => { b.OpenComponent<KlySkipLink>(0); b.CloseComponent(); })));

        Assert.Equal(KlyLocales.Fr.Messages.SkipToContent, cut.Find("a[data-kly-skip-link]").TextContent);
    }

    // ---- Footer and FooterColumns

    [Fact]
    public void Footer_is_a_footer_with_only_the_slots_it_was_given()
    {
        var cut = Render<KlyFooter>(p => p.Add(f => f.Left, "<p>Left</p>").Add(f => f.Right, "<p>Right</p>"));

        Assert.Equal("footer", cut.Find("footer").LocalName);
        Assert.Equal(["Left", "Right"], cut.FindAll("footer p").Select(x => x.TextContent));
        Assert.Equal(2, cut.FindAll("footer > div > div").Count);
    }

    [Fact]
    public void Footer_top_and_bottom_are_full_width_areas()
    {
        var cut = Render<KlyFooter>(p => p.Add(f => f.Top, "<p>Top</p>").Add(f => f.Bottom, "<p>Bottom</p>"));

        var children = cut.Find("footer").Children;
        Assert.Equal(2, children.Length);
        Assert.Equal("Top", children[0].TextContent);
        Assert.Equal("Bottom", children[1].TextContent);
    }

    [Fact]
    public void FooterColumns_lists_links_under_headings_in_a_named_navigation()
    {
        KlyFooterColumn[] columns =
        [
            new("Product") { Links = [new("Features") { Href = "/features" }, new("Pricing") { Href = "/pricing" }] },
            new("Company") { Links = [new("Blog") { Href = "https://example.com", Target = "_blank" }, new("Jobs") { Href = "/jobs", Disabled = true }] },
        ];
        var cut = Render<KlyFooterColumns>(p => p.Add(c => c.Columns, columns).Add(c => c.AriaLabel, "Sitemap"));

        Assert.Equal("Sitemap", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal(["Product", "Company"], cut.FindAll("h3").Select(h => h.TextContent));
        Assert.Equal(["/features", "/pricing", "https://example.com"], cut.FindAll("a").Select(a => a.GetAttribute("href")));
        Assert.Equal("noopener noreferrer", cut.Find("a[target=_blank]").GetAttribute("rel"));
        var disabled = cut.Find("li span[aria-disabled=true]");
        Assert.Equal("Jobs", disabled.TextContent.Trim());
    }

    [Fact]
    public void FooterColumns_navigation_has_a_default_name()
    {
        var cut = Render<KlyFooterColumns>(p => p.Add(c => c.Columns, new[] { new KlyFooterColumn("A") }));

        Assert.Equal("Footer", cut.Find("nav").GetAttribute("aria-label"));
    }

    // ---- Main

    [Fact]
    public void Main_is_the_main_landmark_with_a_minimum_height_that_keeps_the_footer_down()
    {
        var cut = Render<KlyMain>(p => p.Add(m => m.ChildContent, "<p>Page</p>"));

        var main = cut.Find("main");
        Assert.Contains("min-h-[calc(100dvh_-_var(--kly-header-height,var(--klyroo-header-height))_-_var(--kly-safe-top)_-_var(--kly-tv-inset,0px))]", main.ClassList);
        Assert.Equal("Page", main.TextContent);
        // The bottom inset belongs to the footer when one follows; the marker lets a dashboard inside leave it to the main block.
        Assert.Contains("has-[~footer]:pb-0", main.ClassList);
        Assert.True(main.HasAttribute("data-kly-main"));
    }

    [Fact]
    public void Main_can_render_another_element_and_takes_class_overrides()
    {
        var cut = Render<KlyMain>(p => p.Add(m => m.As, KlyElement.Div).Add(m => m.Class, "min-h-48"));

        Assert.Empty(cut.FindAll("main"));
        var div = cut.Find("div");
        Assert.Contains("min-h-48", div.ClassList);
        Assert.DoesNotContain("min-h-[calc(100dvh_-_var(--kly-header-height,var(--klyroo-header-height)))]", div.ClassList);
    }

    // ---- Sidebar

    [Fact]
    public void Sidebar_is_a_named_aside_with_a_collapse_toggle_that_is_not_pressed()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.ChildContent, "<p>Nav</p>").Add(s => s.Header, "<b>Logo</b>").Add(s => s.Footer, "<i>User</i>"));

        var aside = cut.Find("aside");
        Assert.Equal("Sidebar", aside.GetAttribute("aria-label"));
        Assert.Equal("expanded", aside.GetAttribute("data-state"));
        Assert.Contains("group/sidebar", aside.ClassList);
        var toggle = cut.Find("aside button[aria-label='Collapse sidebar']");
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.Equal("Logo", cut.Find("aside b").TextContent);
        Assert.Equal("User", cut.Find("aside i").TextContent);
    }

    [Fact]
    public void Sidebar_unbound_renders_no_sequence_and_keeps_a_storage_key_only_with_an_id()
    {
        var plain = Render<KlySidebar>();
        var host = plain.Find("kly-sidebar");
        Assert.False(host.HasAttribute("data-kly-seq"));
        Assert.False(host.HasAttribute("data-kly-sidebar-id"));
        Assert.Contains("--kly-sidebar-width:256px", host.GetAttribute("style"));

        var remembered = Render<KlySidebar>(p => p.Add(s => s.Id, "app").Add(s => s.Width, 300));
        host = remembered.Find("kly-sidebar");
        Assert.Equal("app", host.GetAttribute("data-kly-sidebar-id"));
        Assert.Equal("300", host.GetAttribute("data-kly-width"));
    }

    [Fact]
    public void Sidebar_bound_renders_the_sequence_first_and_follows_the_browser()
    {
        var collapsed = false;
        var cut = Render<KlySidebar>(p => p.Add(s => s.Collapsed, collapsed).Add(s => s.CollapsedChanged, (bool v) => collapsed = v));

        var host = cut.Find("kly-sidebar");
        Assert.Equal("data-kly-seq", host.Attributes.First().Name);
        host.TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = false, Sequence = 1 });

        Assert.True(collapsed);
        Assert.Equal("collapsed", cut.Find("aside").GetAttribute("data-state"));
        Assert.Equal("true", cut.Find("aside button[aria-pressed]").GetAttribute("aria-pressed"));
        Assert.Equal("1", cut.Find("kly-sidebar").GetAttribute("data-kly-seq"));
        Assert.True(cut.Find("kly-sidebar").HasAttribute("collapsed"));
    }

    [Fact]
    public void Sidebar_renders_the_collapsed_state_it_was_given()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.Collapsed, true));

        Assert.Equal("collapsed", cut.Find("aside").GetAttribute("data-state"));
        Assert.Equal("true", cut.Find("aside button[data-kly-sidebar-toggle]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Sidebar_without_collapse_has_no_toggle_and_resizing_adds_a_separator()
    {
        var fixedWidth = Render<KlySidebar>(p => p.Add(s => s.Collapsible, false));
        Assert.Empty(fixedWidth.FindAll("aside button[data-kly-sidebar-toggle]"));
        Assert.Empty(fixedWidth.FindAll("[role=separator]"));

        var resizable = Render<KlySidebar>(p => p.Add(s => s.Resizable, true).Add(s => s.MinWidth, 100).Add(s => s.MaxWidth, 400).Add(s => s.Width, 200));
        var handle = resizable.Find("aside [role=separator]");
        Assert.Equal("0", handle.GetAttribute("tabindex"));
        Assert.Equal("vertical", handle.GetAttribute("aria-orientation"));
        Assert.Equal("Resize sidebar", handle.GetAttribute("aria-label"));
        Assert.Equal(["100", "400", "200"], new[] { "aria-valuemin", "aria-valuemax", "aria-valuenow" }.Select(handle.GetAttribute));
        Assert.True(resizable.Find("kly-sidebar").HasAttribute("resizable"));
    }

    [Fact]
    public void Sidebar_renders_its_content_once_and_the_aside_is_the_off_canvas_popover()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.ChildContent, "<p class=\"nav\">Nav</p>").Add(s => s.Collapsed, true));

        Assert.Single(cut.FindAll("p.nav"));
        Assert.Empty(cut.FindAll("dialog"));
        var aside = cut.Find("aside");
        Assert.Equal("auto", aside.GetAttribute("popover"));
        var id = aside.GetAttribute("id");
        Assert.False(string.IsNullOrEmpty(id));
        // The trigger and the close button are native popover invokers of the aside.
        var trigger = cut.Find("button[aria-label='Open sidebar']");
        Assert.Equal(id, trigger.GetAttribute("popovertarget"));
        Assert.True(trigger.HasAttribute("data-kly-sidebar-open"));
        var close = aside.QuerySelector("button[popovertargetaction=hide]")!;
        Assert.Equal(id, close.GetAttribute("popovertarget"));
        Assert.Equal("Close", close.GetAttribute("aria-label"));
        // The title the dialog takes its name from, shown below lg only.
        var title = aside.QuerySelector("[data-slot=mobile-header] h2")!;
        Assert.Equal("Sidebar", title.TextContent);
        Assert.Contains("lg:hidden", aside.QuerySelector("[data-slot=mobile-header]")!.ClassList);
        Assert.Contains("lg:hidden", cut.Find("kly-sidebar > div").ClassList);
    }

    [Fact]
    public void Sidebar_without_a_trigger_renders_no_button_outside_the_aside_and_titles_the_panel()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.MobileTrigger, false).Add(s => s.MobileTitle, "Workspace"));

        Assert.Empty(cut.FindAll("kly-sidebar > div"));
        Assert.Equal(cut.Find("aside").GetAttribute("id"), cut.Find("aside button[popovertargetaction=hide]").GetAttribute("popovertarget"));
        Assert.Equal("Workspace", cut.Find("aside [data-slot=mobile-header] h2").TextContent);
    }

    [Fact]
    public void Sidebar_As_picks_the_element_and_the_default_is_an_aside()
    {
        var upper = Render<KlySidebar>(p => p.Add(s => s.As, KlyElement.Aside));
        Assert.Equal("aside", upper.Find("[data-slot=root]").LocalName);
        Assert.Equal("Sidebar", upper.Find("[data-slot=root]").GetAttribute("aria-label"));

        var none = Render<KlySidebar>();
        Assert.Equal("aside", none.Find("[data-slot=root]").LocalName);
        Assert.Equal("div", Render<KlySidebar>(p => p.Add(s => s.As, KlyElement.Div)).Find("[data-slot=root]").LocalName);
    }

    [Fact]
    public void Sidebar_panel_id_follows_the_storage_id_and_the_dialog_wrapper_holds_everything_but_the_handle()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.Id, "my app/1").Add(s => s.Resizable, true).Add(s => s.Footer, "<i>F</i>"));

        var aside = cut.Find("[data-slot=root]");
        Assert.Equal("kly-sidebar-my-app-1", aside.GetAttribute("id"));
        var panel = cut.Find("[data-slot=root] > [data-slot=panel]");
        Assert.NotNull(panel.QuerySelector("[data-slot=mobile-header]"));
        Assert.NotNull(panel.QuerySelector("[data-slot=content]"));
        Assert.NotNull(panel.QuerySelector("[data-slot=footer]"));
        Assert.Null(panel.QuerySelector("[data-slot=handle]"));
        Assert.NotNull(cut.Find("[data-slot=root] > [data-slot=handle]"));
        // Neither the aside nor the panel carry a dialog role until the browser opens the panel.
        Assert.False(aside.HasAttribute("role"));
        Assert.False(panel.HasAttribute("role"));
    }

    [Fact]
    public void Sidebar_collapsed_styles_apply_from_lg_only_so_the_server_markup_is_right_at_every_width()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.Collapsed, true).Add(s => s.Header, "<b>H</b>").Add(s => s.Footer, "<i>F</i>").Add(s => s.ChildContent, "<p>C</p>"));

        var classes = cut.FindAll("[data-slot=root], [data-slot=root] *")
            .SelectMany(e => e.ClassList)
            .Where(c => c.Contains("data-[state=collapsed]"))
            .ToList();
        Assert.NotEmpty(classes);
        Assert.All(classes, c => Assert.StartsWith("lg:", c));
    }

    [Fact]
    public void Sidebar_as_div_is_not_a_landmark()
    {
        var cut = Render<KlySidebar>(p => p.Add(s => s.As, KlyElement.Div));

        Assert.Empty(cut.FindAll("aside"));
        var root = cut.Find("[data-slot=root]");
        Assert.Equal("div", root.LocalName);
        Assert.False(root.HasAttribute("aria-label"));
    }

    // ---- Error

    [Fact]
    public void Error_not_found_shows_the_code_a_title_a_message_and_a_way_home()
    {
        var cut = Render<KlyError>(p => p.Add(e => e.StatusCode, 404));

        Assert.Equal("404", cut.Find("p").TextContent);
        Assert.Equal("Page not found", cut.Find("h1").TextContent);
        var home = cut.Find("a");
        Assert.Equal("./", home.GetAttribute("href"));
        Assert.Equal("Back to home", home.TextContent.Trim());
        Assert.Contains("does not exist", cut.Markup);
    }

    [Fact]
    public void Error_unauthorized_asks_to_sign_in_instead_of_saying_access_is_denied()
    {
        var cut = Render<KlyError>(p => p.Add(e => e.StatusCode, 401));
        var forbidden = Render<KlyError>(p => p.Add(e => e.StatusCode, 403));

        Assert.Equal("Sign in required", cut.Find("h1").TextContent);
        Assert.Contains("sign in", cut.Markup);
        Assert.Equal("Access denied", forbidden.Find("h1").TextContent);
    }

    [Fact]
    public void FooterColumns_heading_level_can_be_changed()
    {
        KlyFooterColumn[] columns = [new("Product") { Links = [new("Features") { Href = "/features" }] }];
        var cut = Render<KlyFooterColumns>(p => p.Add(c => c.Columns, columns).Add(c => c.HeadingLevel, 2));

        Assert.Equal("Product", cut.Find("h2").TextContent);
        Assert.Empty(cut.FindAll("h3"));
    }

    [Fact]
    public void Error_takes_explicit_texts_and_a_custom_way_back()
    {
        var cut = Render<KlyError>(p => p.Add(e => e.StatusCode, 404).Add(e => e.StatusMessage, "Gone").Add(e => e.Message, "It left.")
            .Add(e => e.Href, "/start").Add(e => e.ClearLabel, "Start again").Add(e => e.HeadingLevel, 2));

        Assert.Equal("Gone", cut.Find("h2").TextContent);
        Assert.Empty(cut.FindAll("h1"));
        Assert.Contains("It left.", cut.Markup);
        Assert.Equal("/start", cut.Find("a").GetAttribute("href"));
        Assert.Equal("Start again", cut.Find("a").TextContent.Trim());
    }

    [Fact]
    public void Error_hides_the_exception_message_unless_asked()
    {
        var exception = new InvalidOperationException("secret connection string");

        var hidden = Render<KlyError>(p => p.Add(e => e.Exception, exception));
        Assert.Equal("500", hidden.Find("p").TextContent);
        Assert.Equal("Something went wrong", hidden.Find("h1").TextContent);
        Assert.DoesNotContain("secret", hidden.Markup);

        var shown = Render<KlyError>(p => p.Add(e => e.Exception, exception).Add(e => e.ShowDetails, true));
        Assert.Contains("secret connection string", shown.Markup);
    }

    [Fact]
    public void Error_actions_replace_the_back_button_and_clear_can_remove_it()
    {
        var custom = Render<KlyError>(p => p.Add(e => e.StatusCode, 403).Add(e => e.Actions, "<button id=\"retry\">Retry</button>"));
        Assert.NotNull(custom.Find("#retry"));
        Assert.Empty(custom.FindAll("a"));
        Assert.Equal("Access denied", custom.Find("h1").TextContent);

        var none = Render<KlyError>(p => p.Add(e => e.StatusCode, 404).Add(e => e.Clear, false));
        Assert.Empty(none.FindAll("a"));
    }

    // ---- LocaleSelect

    private static readonly KlyLocale[] Locales =
    [
        KlyLocales.En with { Flag = "EN" },
        KlyLocales.Fr with { Flag = "FR" },
    ];

    [Fact]
    public void LocaleSelect_wraps_a_named_select_and_describes_the_cookie_to_write()
    {
        var cut = Render<KlyLocaleSelect>(p => p.Add(l => l.Locales, Locales).Add(l => l.Value, "fr"));

        var host = cut.Find("[data-kly-locale-select]");
        Assert.Equal(".AspNetCore.Culture", host.GetAttribute("data-kly-cookie"));
        Assert.Equal("/", host.GetAttribute("data-kly-cookie-path"));
        Assert.True(host.HasAttribute("data-kly-reload"));
        Assert.Equal("Language", cut.Find("button[role=combobox]").GetAttribute("aria-label"));
        Assert.Equal(["EN English", "FR Français"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")));
        Assert.Equal("fr", cut.Find("kly-select").GetAttribute("value"));
        // The select is unbound: nothing for .NET to hear, and nothing posted.
        Assert.False(cut.Find("kly-select").HasAttribute("data-kly-seq"));
        Assert.False(cut.Find("input[type=hidden]").HasAttribute("name"));
    }

    [Fact]
    public void LocaleSelect_options_cookie_and_reload_are_configurable()
    {
        var cut = Render<KlyLocaleSelect>(p => p.Add(l => l.Locales, Locales).Add(l => l.CookieName, "lang").Add(l => l.CookiePath, "/app")
            .Add(l => l.Reload, false).Add(l => l.AriaLabel, "Langue"));

        var host = cut.Find("[data-kly-locale-select]");
        Assert.Equal("lang", host.GetAttribute("data-kly-cookie"));
        Assert.Equal("/app", host.GetAttribute("data-kly-cookie-path"));
        Assert.False(host.HasAttribute("data-kly-reload"));
        Assert.Equal("Langue", cut.Find("button[role=combobox]").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData("fr-FR", "fr")]
    [InlineData("FR", "fr")]
    [InlineData("en-GB", "en")]
    [InlineData("de-DE", "")]
    public void LocaleSelect_defaults_to_the_current_UI_culture_matched_by_name_then_language(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var cut = Render<KlyLocaleSelect>(p => p.Add(l => l.Locales, Locales));

            Assert.Equal(expected, cut.Find("kly-select").GetAttribute("value"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // ---- Theme

    private static RenderFragment Button(string label, ButtonUi? ui = null) => b =>
    {
        b.OpenComponent<KlyButton>(0);
        b.AddAttribute(1, nameof(KlyButton.Label), label);
        if (ui is not null) b.AddAttribute(2, nameof(KlyButton.Ui), ui);
        b.CloseComponent();
    };

    private IRenderedComponent<KlyTheme> RenderTheme(Action<ComponentParameterCollectionBuilder<KlyTheme>> configure) => Render<KlyTheme>(configure);

    [Fact]
    public void Theme_cascades_overrides_to_components_of_the_given_type_only()
    {
        var cut = RenderTheme(p => p.Add(t => t.For, typeof(KlyButton)).Add(t => t.Ui, new ButtonUi { Base = "font-extrabold" })
            .Add(t => t.ChildContent, b =>
            {
                b.AddContent(0, Button("Go"));
                b.OpenComponent<KlyBadge>(1);
                b.AddAttribute(2, nameof(KlyBadge.Label), "Tag");
                b.CloseComponent();
            }));

        var button = cut.Find("button");
        Assert.Contains("font-extrabold", button.ClassList);
        // The theme's own weight loses to the override instead of both being applied.
        Assert.DoesNotContain("font-medium", button.ClassList);
        Assert.DoesNotContain("font-extrabold", cut.Find("span").ClassList);
    }

    [Fact]
    public void Theme_without_overrides_changes_nothing()
    {
        var themed = RenderTheme(p => p.Add(t => t.ChildContent, Button("Go")));
        var plain = Render<KlyButton>(p => p.Add(b => b.Label, "Go"));

        Assert.Equal(plain.Find("button").GetAttribute("class"), themed.Find("button").GetAttribute("class"));
    }

    [Fact]
    public void Theme_is_absent_from_a_component_that_has_no_enclosing_theme()
    {
        var plain = Render<KlyButton>(p => p.Add(b => b.Label, "Go"));

        Assert.DoesNotContain("font-extrabold", plain.Find("button").ClassList);
    }

    [Fact]
    public void Theme_instance_ui_and_class_win_over_the_scope()
    {
        var cut = RenderTheme(p => p.Add(t => t.For, typeof(KlyButton)).Add(t => t.Ui, new ButtonUi { Base = "font-extrabold uppercase" })
            .Add(t => t.ChildContent, Button("Go", new ButtonUi { Base = "font-normal" })));

        var button = cut.Find("button");
        Assert.Contains("font-normal", button.ClassList);
        Assert.DoesNotContain("font-extrabold", button.ClassList);
        // What the instance does not override still comes from the scope.
        Assert.Contains("uppercase", button.ClassList);
    }

    [Fact]
    public void Theme_nested_scopes_merge_and_the_innermost_wins()
    {
        var outer = new Dictionary<Type, ISlotOverrides> { [typeof(KlyButton)] = new ButtonUi { Base = "rounded-full uppercase" } };
        var cut = RenderTheme(p => p.Add(t => t.Overrides, outer).Add(t => t.ChildContent, b =>
        {
            b.AddContent(0, Button("Outer"));
            b.OpenComponent<KlyTheme>(1);
            b.AddAttribute(2, nameof(KlyTheme.For), typeof(KlyButton));
            b.AddAttribute(3, nameof(KlyTheme.Ui), new ButtonUi { Base = "rounded-none" });
            b.AddAttribute(4, nameof(KlyTheme.ChildContent), Button("Inner"));
            b.CloseComponent();
        }));

        var buttons = cut.FindAll("button");
        Assert.Contains("rounded-full", buttons[0].ClassList);
        Assert.Contains("rounded-none", buttons[1].ClassList);
        Assert.DoesNotContain("rounded-full", buttons[1].ClassList);
        // Outer classes the inner theme does not touch still apply.
        Assert.Contains("uppercase", buttons[1].ClassList);
    }

    [Fact]
    public void Theme_reaches_a_generic_component_through_its_type_definition()
    {
        var overrides = new Dictionary<Type, ISlotOverrides> { [typeof(KlyTable<>)] = new TableUi { Root = "border-2" } };
        var cut = RenderTheme(p => p.Add(t => t.Overrides, overrides).Add(t => t.ChildContent, b =>
        {
            b.OpenComponent<KlyTable<string>>(0);
            b.AddAttribute(1, nameof(KlyTable<string>.Items), Array.Empty<string>());
            b.CloseComponent();
        }));

        Assert.Contains("border-2", cut.Find("[class~='border-2']").ClassList);
    }

    [Fact]
    public void Theme_follows_changed_overrides()
    {
        var cut = RenderTheme(p => p.Add(t => t.For, typeof(KlyButton)).Add(t => t.Ui, new ButtonUi { Base = "italic" }).Add(t => t.ChildContent, Button("Go")));
        Assert.Contains("italic", cut.Find("button").ClassList);

        cut.Render(p => p.Add(t => t.For, typeof(KlyButton)).Add(t => t.Ui, new ButtonUi { Base = "underline" }).Add(t => t.ChildContent, Button("Go")));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("underline", classes);
        Assert.DoesNotContain("italic", classes);
    }

    // A component below a theme counts how often its parameters are set.
    public sealed class ParameterCounter : KlyComponentBase
    {
        public static int Count;
        protected override void OnParametersSet() => Count++;
    }

    public sealed class ThemeHost : ComponentBase
    {
        [Parameter] public int Tick { get; set; }
        [Parameter] public bool Inline { get; set; }
        [Parameter] public IReadOnlyDictionary<Type, ISlotOverrides>? Shared { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.AddContent(0, Tick);
            b.OpenComponent<KlyTheme>(1);
            b.AddAttribute(2, nameof(KlyTheme.Overrides), Inline
                ? new Dictionary<Type, ISlotOverrides> { [typeof(KlyButton)] = new ButtonUi { Base = "font-bold" } }
                : Shared);
            b.AddAttribute(3, nameof(KlyTheme.ChildContent), (RenderFragment)(c => { c.OpenComponent<ParameterCounter>(0); c.CloseComponent(); }));
            b.CloseComponent();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Theme_does_not_render_the_components_below_again_when_nothing_changed(bool inline)
    {
        ParameterCounter.Count = 0;
        var shared = new Dictionary<Type, ISlotOverrides> { [typeof(KlyButton)] = new ButtonUi { Base = "font-bold" } };
        var cut = Render<ThemeHost>(p => p.Add(h => h.Shared, shared).Add(h => h.Inline, inline).Add(h => h.Tick, 0));
        for (var i = 1; i <= 5; i++)
        {
            cut.Render(p => p.Add(h => h.Shared, shared).Add(h => h.Inline, inline).Add(h => h.Tick, i));
        }

        Assert.Equal(1, ParameterCounter.Count);
    }
}
