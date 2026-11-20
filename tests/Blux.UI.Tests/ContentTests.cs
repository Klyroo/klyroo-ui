using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tests;

/// <summary>The "Content" family: ContentNavigation, ContentToc, ContentSurround, ContentSearch and ContentSearchButton.</summary>
public class ContentTests : BluxTestContext
{
    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    // ---- ContentNavigation

    private static readonly BxContentNavigationItem[] Tree =
    [
        new("Getting started")
        {
            Children =
            [
                new("Introduction") { Href = "/docs", Exact = true },
                new("Installation") { Href = "/docs/installation", Badge = "New", Icon = "download" },
            ],
        },
        new("Components")
        {
            Children =
            [
                new("Elements") { Children = [new("Button") { Href = "/components/button" }, new("Badge") { Href = "/components/badge" }] },
                new("Archived") { Href = "/archived", Disabled = true },
            ],
        },
        new("Changelog") { Href = "/changelog" },
    ];

    [Fact]
    public void Navigation_is_a_named_nav_of_nested_lists_with_links_and_group_buttons()
    {
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, Tree));

        var nav = cut.Find("nav");
        Assert.Equal("Documentation", nav.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[role=menu], [role=menuitem], [role=tree]"));
        // Two top-level groups open with buttons, one top-level link, and a nested group inside Components.
        Assert.Equal(["Getting started", "Components", "Elements"], cut.FindAll("button").Select(b => b.TextContent.Trim()));
        Assert.Equal(["Introduction", "Installation", "Button", "Badge", "Changelog"], cut.FindAll("a").Select(a => a.TextContent.Trim().Replace("New", "").Trim()));
        Assert.All(cut.FindAll("button"), b =>
        {
            Assert.Equal("false", b.GetAttribute("aria-expanded"));
            Assert.NotNull(cut.Find("#" + b.GetAttribute("aria-controls")));
        });
        Assert.Equal("primary", nav.GetAttribute("data-color"));
    }

    [Fact]
    public void Navigation_name_and_color_can_be_set()
    {
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, Tree).Add(n => n.AriaLabel, "Guide").Add(n => n.Color, BxColor.Info));

        Assert.Equal("Guide", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("info", cut.Find("nav").GetAttribute("data-color"));
    }

    [Fact]
    public void Navigation_marks_the_current_page_and_opens_only_the_groups_that_hold_it()
    {
        Navigation.NavigateTo("/components/badge");
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, Tree));

        Assert.Equal(["Badge"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim()));
        Assert.Single(cut.FindAll("[aria-current]"));
        var triggers = cut.FindAll("button").ToDictionary(b => b.TextContent.Trim(), b => b.GetAttribute("aria-expanded"));
        Assert.Equal("false", triggers["Getting started"]);
        Assert.Equal("true", triggers["Components"]);
        Assert.Equal("true", triggers["Elements"]);
    }

    [Fact]
    public void Navigation_follows_the_address()
    {
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, Tree));
        Assert.Empty(cut.FindAll("[aria-current]"));

        Navigation.NavigateTo("/changelog");

        cut.WaitForAssertion(() => Assert.Equal(["Changelog"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim())));
    }

    [Fact]
    public void Navigation_exact_items_match_only_their_address_and_others_also_the_pages_below()
    {
        Navigation.NavigateTo("/docs/installation");
        BxContentNavigationItem[] items =
        [
            new("Docs") { Href = "/docs" },
            new("Docs exact") { Href = "/docs", Exact = true },
            new("Installation") { Href = "/docs/installation", Exact = true },
        ];
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, items));

        Assert.Equal(["Docs", "Installation"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim()));
    }

    [Fact]
    public void Navigation_active_can_be_forced_and_open_overrides_the_default()
    {
        BxContentNavigationItem[] items =
        [
            new("A") { Open = true, Children = [new("One") { Href = "/x", Active = false }] },
            new("B") { Children = [new("Two") { Href = "/two", Active = true }] },
            new("C") { Open = false, Children = [new("Three") { Href = "/three", Active = true }] },
        ];
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, items));

        Assert.Equal(["Two", "Three"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim()));
        var open = cut.FindAll("button").ToDictionary(b => b.TextContent.Trim(), b => b.GetAttribute("aria-expanded"));
        Assert.Equal(["true", "true", "false"], [open["A"], open["B"], open["C"]]);
    }

    [Fact]
    public void Navigation_disabled_entries_are_not_links_and_disabled_groups_not_buttons_to_press()
    {
        BxContentNavigationItem[] items =
        [
            new("Archived") { Href = "/archived", Disabled = true },
            new("Locked") { Disabled = true, Children = [new("Inside") { Href = "/inside" }] },
        ];
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, items));

        var span = cut.Find("span[aria-disabled=true]");
        Assert.Equal("Archived", span.TextContent.Trim());
        Assert.Null(span.GetAttribute("href"));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Navigation_shows_icons_and_badges()
    {
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, Tree));

        var link = cut.FindAll("a").First(a => a.TextContent.Contains("Installation"));
        Assert.NotNull(link.QuerySelector("svg[aria-hidden=true][data-icon=download]"));
        Assert.Equal("New", link.QuerySelectorAll("span").Last().TextContent.Trim());
    }

    [Fact]
    public void Navigation_external_links_open_safely()
    {
        BxContentNavigationItem[] items = [new("GitHub") { Href = "https://github.com", Target = "_blank" }];
        var cut = Render<BxContentNavigation>(p => p.Add(n => n.Items, items));

        var link = cut.Find("a");
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }

    [Fact]
    public void Navigation_ui_and_class_override_slots()
    {
        var cut = Render<BxContentNavigation>(p => p
            .Add(n => n.Items, Tree)
            .Add(n => n.Class, "mine")
            .Add(n => n.Ui, new ContentNavigationUi { List = "gap-6" }));

        Assert.Contains("mine", cut.Find("nav").ClassList);
        Assert.Contains("gap-6", cut.Find("nav > ul").ClassList);
        Assert.DoesNotContain("gap-4", cut.Find("nav > ul").ClassList);
    }

    // ---- ContentToc

    private static readonly BxContentTocLink[] Headings =
    [
        new("intro", "Introduction"),
        new("usage", "Usage"),
        new("props", "Props") { Level = 3 },
    ];

    [Fact]
    public void Toc_lists_the_headings_as_fragment_links_with_their_level()
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings));

        var links = cut.FindAll("a");
        // Absolute to the current page: a bare "#id" would resolve against the base href of the app, the root.
        Assert.Equal(["#intro", "#usage", "#props"], links.Select(a => a.GetAttribute("href")![a.GetAttribute("href")!.IndexOf('#')..]));
        Assert.All(links, a => Assert.StartsWith(Navigation.Uri, a.GetAttribute("href")));
        Assert.Equal(["Introduction", "Usage", "Props"], links.Select(a => a.TextContent.Trim()));
        Assert.Equal(["2", "2", "3"], links.Select(a => a.GetAttribute("data-level")));
        Assert.Equal("On this page", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("primary", cut.Find("bx-content-toc").GetAttribute("data-color"));
    }

    [Fact]
    public void Toc_leaves_the_active_state_to_the_browser()
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings));

        Assert.Empty(cut.FindAll("[data-active], [aria-current]"));
        Assert.Null(cut.Find("[data-slot=content]").GetAttribute("data-state"));
    }

    [Fact]
    public void Toc_collapses_behind_a_disclosure_button_below_the_breakpoint()
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings).Add(t => t.Title, "In this article"));

        var button = cut.Find("button[data-slot=trigger]");
        Assert.Equal("false", button.GetAttribute("aria-expanded"));
        Assert.Equal("In this article", button.TextContent.Trim());
        var content = cut.Find("#" + button.GetAttribute("aria-controls"));
        Assert.Equal("content", content.GetAttribute("data-slot"));
        Assert.Contains("lg:hidden", button.ClassList);
        Assert.Contains("lg:block", content.ClassList);
        Assert.Contains("hidden", content.ClassList);
        // The title shows in its place above the breakpoint.
        Assert.Contains("lg:block", cut.Find("[data-slot=title]").ClassList);
        Assert.Equal("In this article", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(BxContentTocBreakpoint.Xl, "xl:")]
    [InlineData(BxContentTocBreakpoint.Xxl, "min-[96rem]:")]
    public void Toc_breakpoint_moves_where_it_stops_collapsing(BxContentTocBreakpoint breakpoint, string prefix)
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings).Add(t => t.Breakpoint, breakpoint));

        Assert.Contains(prefix + "hidden", cut.Find("button").ClassList);
        Assert.Contains(prefix + "block", cut.Find("[data-slot=content]").ClassList);
        Assert.DoesNotContain("lg:hidden", cut.Find("button").ClassList);
    }

    [Fact]
    public void Toc_without_collapsing_has_no_button_and_always_shows_its_list()
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings).Add(t => t.Collapsible, false));

        Assert.Empty(cut.FindAll("button"));
        Assert.DoesNotContain("hidden", cut.Find("[data-slot=content]").ClassList);
        Assert.DoesNotContain("hidden", cut.Find("[data-slot=title]").ClassList);
    }

    [Fact]
    public void Toc_clamps_levels_and_takes_color_ui_and_class()
    {
        var cut = Render<BxContentToc>(p => p
            .Add(t => t.Links, new BxContentTocLink[] { new("a", "A") { Level = 1 }, new("b", "B") { Level = 9 } })
            .Add(t => t.Color, BxColor.Info)
            .Add(t => t.Class, "mine")
            .Add(t => t.Ui, new ContentTocUi { List = "border-s-0" }));

        Assert.Equal(["2", "5"], cut.FindAll("a").Select(a => a.GetAttribute("data-level")));
        Assert.Equal("info", cut.Find("bx-content-toc").GetAttribute("data-color"));
        Assert.Contains("mine", cut.Find("bx-content-toc").ClassList);
        Assert.Contains("border-s-0", cut.Find("ul").ClassList);
    }

    [Fact]
    public void Toc_has_no_state_to_report_to_dotnet()
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings));

        Assert.False(cut.Find("bx-content-toc").HasAttribute("data-bx-seq"));
    }

    // ---- ContentSurround

    private static readonly BxContentSurroundLink Before = new("Installation", "/docs/installation") { Description = "Add the package." };
    private static readonly BxContentSurroundLink After = new("Theming", "/docs/theming");

    [Fact]
    public void Surround_links_the_previous_and_next_pages_with_titles_and_descriptions()
    {
        var cut = Render<BxContentSurround>(p => p.Add(s => s.Previous, Before).Add(s => s.Next, After));

        Assert.Equal("Previous and next pages", cut.Find("nav").GetAttribute("aria-label"));
        var previous = cut.Find("a[rel=prev]");
        var next = cut.Find("a[rel=next]");
        Assert.Equal("/docs/installation", previous.GetAttribute("href"));
        Assert.Equal("/docs/theming", next.GetAttribute("href"));
        Assert.Contains("Previous", previous.TextContent);
        Assert.Contains("Installation", previous.TextContent);
        Assert.Contains("Add the package.", previous.TextContent);
        Assert.Contains("Next", next.TextContent);
        Assert.DoesNotContain("Add the package.", next.TextContent);
        Assert.NotNull(previous.QuerySelector("svg[data-icon], svg[aria-hidden=true]"));
        Assert.DoesNotContain("sm:col-start-2", previous.ClassList);
    }

    [Fact]
    public void Surround_with_only_a_next_page_keeps_it_in_the_second_column()
    {
        var cut = Render<BxContentSurround>(p => p.Add(s => s.Next, After).Add(s => s.AriaLabel, "Pages"));

        Assert.Empty(cut.FindAll("a[rel=prev]"));
        Assert.Contains("sm:col-start-2", cut.Find("a[rel=next]").ClassList);
        Assert.Equal("Pages", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void Surround_renders_nothing_without_pages()
    {
        var cut = Render<BxContentSurround>();

        Assert.Empty(cut.FindAll("nav"));
        Assert.Equal("", cut.Markup.Trim());
    }

    // ---- ContentSearch

    private static readonly BxContentSearchPage[] Pages =
    [
        new("Introduction", "/docs")
        {
            Group = "Getting started",
            Description = "What it is",
            Headings = [new("Why another library", "why"), new("What you get", "features")],
        },
        new("Button", "/components/button") { Group = "Elements", Keywords = "click", Icon = "" },
        new("Theming", "/docs/theming") { Group = "Getting started" },
        new("Home", "/"),
    ];

    [Fact]
    public void Search_is_a_command_palette_in_a_modal_named_for_screen_readers()
    {
        var cut = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages));

        var dialog = cut.Find("dialog");
        Assert.Equal("Search documentation", dialog.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("bx-command-palette"));
        Assert.Equal("Search documentation", cut.Find("bx-command-palette input").GetAttribute("aria-label"));
        Assert.Equal("Search the documentation...", cut.Find("bx-command-palette input").GetAttribute("placeholder"));
        Assert.True(dialog.HasAttribute("data-bx-content-search"));
        Assert.Equal("", dialog.GetAttribute("data-bx-content-search"));
    }

    [Fact]
    public void Search_lists_pages_and_their_headings_under_their_group_in_order()
    {
        var cut = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages));

        // Pages without a group come first, without a heading; then the groups in the order of their first page.
        var options = cut.FindAll("[role=option]");
        Assert.Equal(["Home", "Introduction", "Why another library", "What you get", "Theming", "Button"], options.Select(o => o.QuerySelector("span.truncate")!.TextContent.Trim()));
        Assert.Equal(["Getting started", "Elements"], cut.FindAll("[role=group] > [role=presentation]").Select(h => h.TextContent.Trim()));
        var heading = options.First(o => o.TextContent.Contains("Why another library"));
        Assert.Equal("/docs#why", heading.GetAttribute("href"));
        Assert.Contains("Introduction", heading.TextContent);
        Assert.Equal("/components/button", options.First(o => o.TextContent.Contains("Button")).GetAttribute("href"));
    }

    [Fact]
    public void Search_matches_keywords_and_group_and_the_page_of_a_heading()
    {
        var cut = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages));

        var button = cut.FindAll("[role=option]").First(o => o.GetAttribute("href") == "/components/button");
        Assert.Contains("click", button.GetAttribute("data-search"));
        Assert.Contains("Elements", button.GetAttribute("data-search"));
        var heading = cut.FindAll("[role=option]").First(o => o.GetAttribute("href") == "/docs#why");
        Assert.Contains("Introduction", heading.GetAttribute("data-search"));
        // Selecting closes the modal without a round trip.
        Assert.All(cut.FindAll("[role=option]"), o => Assert.True(o.HasAttribute("data-bx-close")));
    }

    [Fact]
    public void Search_shortcut_is_a_fallback_handled_in_the_browser_after_the_pages_own_hotkeys()
    {
        var cut = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages));

        var trigger = cut.Find("[data-bx-content-hotkey]");
        Assert.Equal("mod+k", trigger.GetAttribute("data-bx-content-hotkey"));
        Assert.True(trigger.HasAttribute("hidden"));
        // data-bx-hotkey is the page-level registry: this search must not compete in it.
        Assert.Empty(cut.FindAll("[data-bx-hotkey]"));

        var custom = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages).Add(s => s.Shortcut, "alt+o").Add(s => s.Name, "docs"));
        Assert.Equal("alt+o", custom.Find("[data-bx-content-hotkey]").GetAttribute("data-bx-content-hotkey"));
        Assert.Equal("docs", custom.Find("dialog").GetAttribute("data-bx-content-search"));
    }

    [Fact]
    public void Search_reports_the_selected_command()
    {
        BxCommandItem? selected = null;
        var cut = Render<BxContentSearch>(p => p
            .Add(s => s.Pages, Pages)
            .Add(s => s.OnSelect, EventCallback.Factory.Create<BxCommandItem>(this, item => selected = item)));

        // The palette reports with bx-change; the handler is only attached when OnSelect is bound.
        cut.Find("bx-command-palette").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "/docs#why", Sequence = 1 });

        Assert.Equal("Why another library", selected?.Label);
        Assert.Equal("/docs#why", selected?.Href);
    }

    [Fact]
    public void Search_without_a_handler_attaches_none()
    {
        var cut = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages));

        Assert.False(cut.Find("bx-command-palette").HasAttribute("data-bx-seq"));
    }

    [Fact]
    public void Search_label_and_placeholder_can_be_set()
    {
        var cut = Render<BxContentSearch>(p => p.Add(s => s.Pages, Pages).Add(s => s.Label, "Find").Add(s => s.Placeholder, "Type..."));

        Assert.Equal("Find", cut.Find("dialog").GetAttribute("aria-label"));
        Assert.Equal("Type...", cut.Find("bx-command-palette input").GetAttribute("placeholder"));
    }

    // ---- ContentSearchButton

    [Fact]
    public void Search_button_is_an_icon_button_named_by_its_label_by_default()
    {
        var cut = Render<BxContentSearchButton>();

        var button = cut.Find("button");
        Assert.Equal("Search", button.GetAttribute("aria-label"));
        Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("", button.GetAttribute("data-bx-content-search-open"));
        Assert.NotNull(button.QuerySelector("svg[aria-hidden=true]"));
        Assert.Empty(cut.FindAll("kbd"));
        Assert.Equal("neutral", button.GetAttribute("data-color"));
    }

    [Fact]
    public void Search_button_expanded_shows_its_label_and_shortcut_keys()
    {
        var cut = Render<BxContentSearchButton>(p => p.Add(b => b.Collapsed, false).Add(b => b.Label, "Find a page").Add(b => b.Keys, ["Alt", "O"]));

        var button = cut.Find("button");
        Assert.Null(button.GetAttribute("aria-label"));
        Assert.Contains("Find a page", button.TextContent);
        Assert.Equal(["Alt", "O"], cut.FindAll("kbd").Select(k => k.TextContent.Trim()));
        Assert.Equal("true", cut.Find("kbd").ParentElement!.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Search_button_default_hint_follows_the_platform_in_the_browser_and_announces_the_shortcut()
    {
        var cut = Render<BxContentSearchButton>(p => p.Add(b => b.Collapsed, false));

        var keys = cut.FindAll("kbd");
        Assert.Equal(2, keys.Count);
        Assert.True(keys[0].HasAttribute("data-bx-mod"));
        Assert.Equal("Ctrl", keys[0].GetAttribute("data-label"));
        Assert.Equal("", keys[0].TextContent);
        Assert.Equal("K", keys[1].TextContent);
        Assert.Equal("Control+K Meta+K", cut.Find("button").GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void Search_button_hosts_the_platform_hint_in_its_own_element_so_no_global_observer_is_needed()
    {
        var cut = Render<BxContentSearchButton>(p => p.Add(b => b.Collapsed, false));

        var host = cut.Find("bx-mod-keys");
        Assert.Equal("true", host.GetAttribute("aria-hidden"));
        Assert.Equal(2, host.QuerySelectorAll("kbd").Length);
        Assert.NotNull(host.QuerySelector("kbd[data-bx-mod]"));
    }

    [Fact]
    public void Search_button_explicit_keys_are_shown_as_given_without_a_platform_hint()
    {
        var cut = Render<BxContentSearchButton>(p => p.Add(b => b.Collapsed, false).Add(b => b.Keys, ["Ctrl", "K"]));

        Assert.Equal(["Ctrl", "K"], cut.FindAll("kbd").Select(k => k.TextContent.Trim()));
        Assert.Empty(cut.FindAll("[data-bx-mod]"));
        Assert.False(cut.Find("button").HasAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void Search_button_names_the_search_it_opens()
    {
        var cut = Render<BxContentSearchButton>(p => p.Add(b => b.For, "docs").Add(b => b.Collapsed, false).Add(b => b.Variant, ButtonVariant.Soft));

        Assert.Equal("docs", cut.Find("button").GetAttribute("data-bx-content-search-open"));
    }

    [Fact]
    public void Search_button_forwards_attributes_and_class()
    {
        var cut = Render<BxContentSearchButton>(p => p
            .Add(b => b.Class, "w-56")
            .AddUnmatched("data-testid", "open-search"));

        var button = cut.Find("button");
        Assert.Equal("open-search", button.GetAttribute("data-testid"));
        Assert.Contains("w-56", button.ClassList);
    }

    // ---- Theme

    [Fact]
    public void Texts_come_from_the_options_and_can_be_localized()
    {
        using var ctx = new Localized();
        var nav = ctx.Render<BxContentNavigation>(p => p.Add(n => n.Items, Tree));
        var toc = ctx.Render<BxContentToc>(p => p.Add(t => t.Links, Headings));
        var surround = ctx.Render<BxContentSurround>(p => p.Add(s => s.Next, After));

        Assert.Equal("Documentation FR", nav.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Sur cette page", toc.Find("nav").GetAttribute("aria-label"));
        Assert.Contains("Suivant", surround.Find("a").TextContent);
    }

    private sealed class Localized() : BluxTestContext(o =>
    {
        o.Locale = BxLocales.Extend(BxLocales.En, messages: m => m with { ContentNavigation = "Documentation FR", ContentToc = "Sur cette page", ContentNext = "Suivant" });
    });


    [Fact]
    public void Toc_links_follow_the_page_url_when_the_query_changes()
    {
        var cut = Render<BxContentToc>(p => p.Add(t => t.Links, Headings));
        Assert.DoesNotContain("tab=2", cut.Find("a").GetAttribute("href"));

        // Only the location changes: the parent does not render, so no parameter is set again.
        cut.InvokeAsync(() => Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("tab", 2)));

        cut.WaitForAssertion(() => Assert.Contains("tab=2", cut.Find("a").GetAttribute("href")));
    }
}
