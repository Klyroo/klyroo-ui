using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI.Tests;

public class NavTests : BluxTestContext
{
    private static RenderFragment Text(string text) => b => b.AddContent(0, text);

    // ---- Breadcrumb

    [Fact]
    public void Breadcrumb_is_a_named_nav_with_an_ordered_list_and_marks_the_last_item_as_current()
    {
        BxBreadcrumbItem[] items = [new("Home") { Href = "/" }, new("Docs") { Href = "/docs" }, new("Page")];
        var cut = Render<BxBreadcrumb>(p => p.Add(b => b.Items, items));

        var nav = cut.Find("nav");
        Assert.Equal("Breadcrumb", nav.GetAttribute("aria-label"));
        Assert.Equal(3, cut.FindAll("ol > li").Count);
        Assert.Equal(["Home", "Docs"], cut.FindAll("a").Select(a => a.TextContent.Trim()));
        var current = cut.Find("[aria-current=page]");
        Assert.Equal("Page", current.TextContent.Trim());
        Assert.Single(cut.FindAll("[aria-current]"));
        // One separator after each item but the last, hidden from assistive technology.
        Assert.Equal(2, cut.FindAll("li > span[aria-hidden=true]").Count);
    }

    [Fact]
    public void Breadcrumb_last_item_with_a_link_keeps_the_link_and_is_current()
    {
        BxBreadcrumbItem[] items = [new("Home") { Href = "/" }, new("Here") { Href = "/here" }];
        var cut = Render<BxBreadcrumb>(p => p.Add(b => b.Items, items).Add(b => b.AriaLabel, "Trail"));

        Assert.Equal("Trail", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("/here", cut.Find("a[aria-current=page]").GetAttribute("href"));
    }

    // ---- Pagination

    [Theory]
    [InlineData(1, 5, 1, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(1, 20, 1, new[] { 1, 2, 3, 4, 5, 0, 20 })]
    [InlineData(10, 20, 1, new[] { 1, 0, 9, 10, 11, 0, 20 })]
    [InlineData(20, 20, 1, new[] { 1, 0, 16, 17, 18, 19, 20 })]
    public void Pagination_shows_a_stable_window_of_pages(int page, int count, int sibling, int[] expected) =>
        Assert.Equal(expected, BxPagination.Entries(page, count, sibling));

    [Fact]
    public void Pagination_marks_the_current_page_and_disables_previous_on_the_first()
    {
        var cut = Render<BxPagination>(p => p.Add(b => b.Total, 100).Add(b => b.Page, 1));

        Assert.Equal("Pagination", cut.Find("nav").GetAttribute("aria-label"));
        var current = cut.Find("[aria-current=page]");
        Assert.Equal("1", current.TextContent.Trim());
        Assert.Equal("Page 1", current.GetAttribute("aria-label"));
        Assert.True(cut.Find("button[aria-label='Previous page']").HasAttribute("disabled"));
        Assert.False(cut.Find("button[aria-label='Next page']").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("button[aria-label='First page']"));
    }

    [Fact]
    public void Pagination_first_and_last_buttons_are_opt_in()
    {
        var cut = Render<BxPagination>(p => p.Add(b => b.Total, 100).Add(b => b.Page, 5).Add(b => b.ShowFirstLast, true));

        Assert.Single(cut.FindAll("button[aria-label='First page']"));
        Assert.Single(cut.FindAll("button[aria-label='Last page']"));
    }

    [Fact]
    public void Pagination_buttons_report_the_page_through_the_bound_callback()
    {
        var page = 2;
        var cut = Render<BxPagination>(p => p.Add(b => b.Total, 100).Add(b => b.Page, page).Add(b => b.PageChanged, (int v) => page = v));

        cut.Find("button[aria-label='Page 3']").Click();
        Assert.Equal(3, page);
        Assert.Equal("3", cut.Find("[aria-current=page]").TextContent.Trim());

        cut.Find("button[aria-label='Next page']").Click();
        Assert.Equal(4, page);
    }

    [Fact]
    public void Pagination_buttons_are_inert_until_the_page_is_interactive()
    {
        using var ctx = new StaticContext();
        var cut = ctx.Render<BxPagination>(p => p.Add(b => b.Total, 50));

        Assert.All(cut.FindAll("button"), b => Assert.True(b.HasAttribute("disabled")));
    }

    [Fact]
    public void Pagination_renders_links_when_an_href_template_is_given_even_without_interactivity()
    {
        using var ctx = new StaticContext();
        var cut = ctx.Render<BxPagination>(p => p.Add(b => b.Total, 50).Add(b => b.Page, 1).Add(b => b.HrefTemplate, "/items?page={page}"));

        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("/items?page=3", cut.Find("a[aria-label='Page 3']").GetAttribute("href"));
        Assert.Equal("/items?page=2", cut.Find("a[aria-label='Next page']").GetAttribute("href"));
        var previous = cut.Find("a[aria-label='Previous page']");
        Assert.False(previous.HasAttribute("href"));
        Assert.Equal("true", previous.GetAttribute("aria-disabled"));
        Assert.Equal("page", cut.Find("a[aria-current]").GetAttribute("aria-current"));
    }

    private sealed class StaticContext : BluxTestContext
    {
        public StaticContext() : base(interactive: false) { }
    }

    // ---- Stepper

    private static readonly BxStepperItem[] Steps = [new("One") { Description = "First" }, new("Two"), new("Three")];

    [Fact]
    public void Stepper_marks_completed_and_current_steps()
    {
        var cut = Render<BxStepper>(p => p.Add(s => s.Items, Steps).Add(s => s.Current, 1).Add(s => s.AriaLabel, "Wizard"));

        Assert.Equal("Wizard", cut.Find("ol").GetAttribute("aria-label"));
        var items = cut.FindAll("li");
        Assert.Equal(["completed", "active", "inactive"], items.Select(i => i.GetAttribute("data-state")));
        Assert.Equal("step", items[1].GetAttribute("aria-current"));
        Assert.Single(cut.FindAll("[aria-current]"));
        Assert.Contains("(completed)", items[0].TextContent);
        // Not bound: no buttons.
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Stepper_bound_current_makes_steps_selectable()
    {
        var current = 0;
        var cut = Render<BxStepper>(p => p.Add(s => s.Items, Steps).Add(s => s.Current, current).Add(s => s.CurrentChanged, (int v) => current = v));

        cut.FindAll("button")[2].Click();

        Assert.Equal(2, current);
        Assert.Equal("step", cut.FindAll("li")[2].GetAttribute("aria-current"));
    }

    [Fact]
    public void Stepper_shows_the_content_of_the_current_step_only()
    {
        BxStepperItem[] items = [new("A") { Content = Text("alpha") }, new("B") { Content = Text("beta") }];
        var cut = Render<BxStepper>(p => p.Add(s => s.Items, items).Add(s => s.Current, 1));

        Assert.Contains("beta", cut.Markup);
        Assert.DoesNotContain("alpha", cut.Markup);
    }

    // ---- Timeline

    [Fact]
    public void Timeline_highlights_items_up_to_the_active_one()
    {
        BxTimelineItem[] items = [new("A") { Date = "Jan" }, new("B") { Description = "Beta" }, new("C")];
        var cut = Render<BxTimeline>(p => p.Add(t => t.Items, items).Add(t => t.Active, 1));

        Assert.Equal(["active", "active", "inactive"], cut.FindAll("ol > li").Select(i => i.GetAttribute("data-state")));
        Assert.Contains("Jan", cut.Markup);
        Assert.Contains("Beta", cut.Markup);
    }

    // ---- NavigationMenu

    [Fact]
    public void NavigationMenu_marks_the_link_of_the_current_address_and_is_not_a_menu()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/docs/theming");
        BxNavigationItem[] items = [new("Home") { Href = "/" }, new("Docs") { Href = "/docs" }, new("Other") { Href = "/other" }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items).Add(m => m.AriaLabel, "Main"));

        Assert.Equal("Main", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[role=menu], [role=menuitem]"));
        // Prefix match: /docs is active under /docs/theming; the root only on the root.
        Assert.Equal(["Docs"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim()));
    }

    [Fact]
    public void NavigationMenu_exact_items_match_only_their_own_address()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/docs/theming");
        BxNavigationItem[] items = [new("Docs") { Href = "/docs", Exact = true }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items));

        Assert.Empty(cut.FindAll("[aria-current]"));
    }

    [Fact]
    public void NavigationMenu_horizontal_children_open_in_a_popover_panel()
    {
        BxNavigationItem[] items = [new("Components") { Children = [new("Button") { Href = "/button" }, new("Tabs") { Href = "/tabs" }] }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items));

        Assert.Single(cut.FindAll("bx-popover"));
        Assert.Equal(["Button", "Tabs"], cut.FindAll("[popover] a").Select(a => a.TextContent.Trim()));
        Assert.Empty(cut.FindAll("bx-collapsible"));
    }

    [Fact]
    public void NavigationMenu_vertical_children_form_a_collapsible_group_open_when_one_is_active()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/tabs");
        BxNavigationItem[] items = [new("Components") { Children = [new("Button") { Href = "/button" }, new("Tabs") { Href = "/tabs" }] }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items).Add(m => m.Orientation, BxOrientation.Vertical));

        var group = cut.Find("bx-collapsible");
        Assert.True(group.HasAttribute("open"));
        Assert.Equal("true", cut.Find("[data-bx-collapsible-trigger]").GetAttribute("aria-expanded"));
        Assert.Equal("Tabs", cut.Find("a[aria-current=page]").TextContent.Trim());
    }

    [Fact]
    public void NavigationMenu_follows_navigation()
    {
        BxNavigationItem[] items = [new("A") { Href = "/a" }, new("B") { Href = "/b" }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items));
        Assert.Empty(cut.FindAll("[aria-current]"));

        cut.InvokeAsync(() => Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/b"));

        Assert.Equal(["B"], cut.FindAll("[aria-current=page]").Select(a => a.TextContent.Trim()));
    }

    [Fact]
    public void NavigationMenu_matches_case_insensitively()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/Docs/Theming");
        BxNavigationItem[] items = [new("Docs") { Href = "/docs" }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items));

        Assert.Equal(["Docs"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim()));
    }

    [Fact]
    public void NavigationMenu_horizontal_panel_is_not_announced_as_a_dialog()
    {
        BxNavigationItem[] items = [new("Components") { Children = [new("Button") { Href = "/button" }] }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items));

        Assert.Empty(cut.FindAll("[role=dialog]"));
        var panel = cut.Find("[popover]");
        Assert.False(panel.HasAttribute("role"));
        Assert.False(panel.HasAttribute("aria-label"));
    }

    [Fact]
    public void Timeline_marks_the_current_item_for_assistive_technology()
    {
        BxTimelineItem[] items = [new("A"), new("B"), new("C")];
        var cut = Render<BxTimeline>(p => p.Add(t => t.Items, items).Add(t => t.Active, 1));

        Assert.Equal(["B"], cut.FindAll("ol > li[aria-current=step]").Select(i => i.QuerySelector("span:not([aria-hidden])")!.TextContent.Trim()));
    }

    [Fact]
    public void Splitter_handle_has_a_visible_focus_outline()
    {
        var cut = Render<BxSplitter>(p => p.Add(s => s.Panels, TwoPanels));

        var classes = cut.Find("[role=separator]").GetAttribute("class")!;
        Assert.DoesNotContain("focus-visible:outline-none", classes);
        Assert.Contains("focus-visible:outline-2", classes);
    }

    [Fact]
    public void CommandPalette_link_options_are_not_tab_stops()
    {
        BxCommandGroup[] groups = [new("Go", [new() { Label = "Docs", Href = "/docs" }, new() { Label = "Run" }])];
        var cut = Render<BxCommandPalette>(p => p.Add(c => c.Groups, groups));

        Assert.Equal("-1", cut.Find("a[role=option]").GetAttribute("tabindex"));
    }

    // ---- Collapsible

    [Fact]
    public void Collapsible_closed_hides_its_region_until_found_and_links_trigger_to_it()
    {
        var cut = Render<BxCollapsible>(p => p.Add(c => c.Label, "More").Add(c => c.ChildContent, Text("Details")));

        var trigger = cut.Find("button[data-bx-collapsible-trigger]");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        var region = cut.Find("[data-bx-collapsible-content]");
        Assert.Equal("until-found", region.GetAttribute("hidden"));
        Assert.Equal(region.Id, trigger.GetAttribute("aria-controls"));
        Assert.False(cut.Find("bx-collapsible").HasAttribute("open"));
    }

    [Fact]
    public void Collapsible_open_renders_a_visible_region()
    {
        var cut = Render<BxCollapsible>(p => p.Add(c => c.Label, "More").Add(c => c.Open, true));

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.False(cut.Find("[data-bx-collapsible-content]").HasAttribute("hidden"));
        Assert.True(cut.Find("bx-collapsible").HasAttribute("open"));
    }

    [Fact]
    public void Collapsible_bound_open_follows_the_browser_and_unbound_attaches_no_handler()
    {
        var open = false;
        var cut = Render<BxCollapsible>(p => p.Add(c => c.Open, open).Add(c => c.OpenChanged, (bool v) => open = v));
        Assert.Equal("0", cut.Find("bx-collapsible").GetAttribute("data-bx-seq"));

        cut.Find("bx-collapsible").TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 4 });

        Assert.True(open);
        Assert.Equal("4", cut.Find("bx-collapsible").GetAttribute("data-bx-seq"));

        var unbound = Render<BxCollapsible>(p => p.Add(c => c.Label, "x"));
        Assert.False(unbound.Find("bx-collapsible").HasAttribute("data-bx-seq"));
    }

    // ---- Tree

    private static readonly BxTreeItem[] Nodes =
    [
        new("src") { Expanded = true, Children = [new("a.cs"), new("lib") { Children = [new("b.cs")] }] },
        new("README"),
    ];

    [Fact]
    public void Tree_renders_the_treeview_roles_and_expanded_state()
    {
        var cut = Render<BxTree>(p => p.Add(t => t.Items, Nodes).Add(t => t.AriaLabel, "Files"));

        var tree = cut.Find("[role=tree]");
        Assert.Equal("Files", tree.GetAttribute("aria-label"));
        var items = cut.FindAll("[role=treeitem]");
        Assert.Equal(5, items.Count);
        Assert.Equal("true", items[0].GetAttribute("aria-expanded"));
        Assert.Equal("false", items[2].GetAttribute("aria-expanded"));
        Assert.False(items[1].HasAttribute("aria-expanded"));
        var groups = cut.FindAll("[role=group]");
        Assert.Equal(2, groups.Count);
        Assert.False(groups[0].HasAttribute("hidden"));
        Assert.True(groups[1].HasAttribute("hidden"));
        // Every item is named by its own label.
        foreach (var item in items) Assert.NotNull(cut.Find("#" + item.GetAttribute("aria-labelledby")));
    }

    [Fact]
    public void Tree_value_selects_an_item_and_bound_callback_follows_the_browser()
    {
        string? value = "README";
        var cut = Render<BxTree>(p => p.Add(t => t.Items, Nodes).Add(t => t.Value, value).Add(t => t.ValueChanged, (string? v) => value = v));

        Assert.Equal("true", cut.FindAll("[role=treeitem]")[4].GetAttribute("aria-selected"));

        cut.Find("bx-tree").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "a.cs", Sequence = 2 });

        Assert.Equal("a.cs", value);
        Assert.Equal("true", cut.FindAll("[role=treeitem]")[1].GetAttribute("aria-selected"));
        Assert.Equal("2", cut.Find("bx-tree").GetAttribute("data-bx-seq"));
    }

    // ---- Marquee

    [Fact]
    public void Marquee_repeats_the_content_hidden_from_assistive_technology()
    {
        var cut = Render<BxMarquee>(p => p.Add(m => m.ChildContent, Text("Logos")).Add(m => m.Reverse, true).Add(m => m.Duration, 12.5));

        var groups = cut.FindAll("[data-bx-marquee-group]");
        Assert.Equal(2, groups.Count);
        Assert.False(groups[0].HasAttribute("aria-hidden"));
        Assert.Equal("true", groups[1].GetAttribute("aria-hidden"));
        Assert.True(groups[1].HasAttribute("inert"));
        var root = cut.Find("[data-bx-marquee]");
        Assert.Equal("true", root.GetAttribute("data-reverse"));
        Assert.Equal("true", root.GetAttribute("data-pause-on-hover"));
        Assert.Contains("--bx-marquee-duration: 12.5s", root.GetAttribute("style"));
    }

    [Fact]
    public void Marquee_vertical_and_pause_opt_out()
    {
        var cut = Render<BxMarquee>(p => p.Add(m => m.Orientation, BxOrientation.Vertical).Add(m => m.PauseOnHover, false).Add(m => m.Repeat, 2));

        var root = cut.Find("[data-bx-marquee]");
        Assert.Equal("vertical", root.GetAttribute("data-orientation"));
        Assert.False(root.HasAttribute("data-pause-on-hover"));
        Assert.Equal(3, cut.FindAll("[data-bx-marquee-group]").Count);
    }

    // ---- ScrollArea

    [Fact]
    public void ScrollArea_names_its_viewport_and_scrolls_on_the_requested_axis()
    {
        var cut = Render<BxScrollArea>(p => p.Add(s => s.AriaLabel, "Log").Add(s => s.Orientation, BxOrientation.Horizontal).Add(s => s.ChildContent, Text("x")));

        var viewport = cut.Find("[data-bx-scroll-viewport]");
        Assert.Equal("region", viewport.GetAttribute("role"));
        Assert.Equal("Log", viewport.GetAttribute("aria-label"));
        Assert.Contains("overflow-x-auto", viewport.ClassList);
        // Focusability is decided in the browser, once it knows the content overflows.
        Assert.False(viewport.HasAttribute("tabindex"));
    }

    [Fact]
    public void ScrollArea_without_a_label_is_not_a_region()
    {
        var cut = Render<BxScrollArea>(p => p.Add(s => s.Orientation, (BxOrientation?)null));

        Assert.False(cut.Find("[data-bx-scroll-viewport]").HasAttribute("role"));
        Assert.Contains("overflow-auto", cut.Find("[data-bx-scroll-viewport]").ClassList);
    }

    // ---- Splitter

    private static readonly BxSplitterPanel[] TwoPanels =
    [
        new() { Size = 30, MinSize = 10, MaxSize = 80, Content = Text("left") },
        new() { Content = Text("right") },
    ];

    [Fact]
    public void Splitter_renders_a_separator_with_the_size_of_the_first_panel()
    {
        var cut = Render<BxSplitter>(p => p.Add(s => s.Panels, TwoPanels));

        var host = cut.Find("bx-splitter");
        Assert.Equal("30,70", host.GetAttribute("sizes"));
        var separator = cut.Find("[role=separator]");
        Assert.Equal("30", separator.GetAttribute("aria-valuenow"));
        Assert.Equal("10", separator.GetAttribute("aria-valuemin"));
        Assert.Equal("80", separator.GetAttribute("aria-valuemax"));
        // Panels side by side are divided by a vertical separator.
        Assert.Equal("vertical", separator.GetAttribute("aria-orientation"));
        Assert.Equal("0", separator.GetAttribute("tabindex"));
        Assert.Equal("Resize panels", separator.GetAttribute("aria-label"));
        Assert.Equal(cut.FindAll("[data-bx-panel]")[0].Id, separator.GetAttribute("aria-controls"));
        Assert.Contains("flex: 30 1 0%", cut.FindAll("[data-bx-panel]")[0].GetAttribute("style"));
    }

    [Fact]
    public void Splitter_stacked_panels_use_a_horizontal_separator_and_share_unsized_panels_equally()
    {
        BxSplitterPanel[] panels = [new(), new(), new()];
        var cut = Render<BxSplitter>(p => p.Add(s => s.Panels, panels).Add(s => s.Orientation, BxOrientation.Vertical));

        Assert.Equal("horizontal", cut.Find("[role=separator]").GetAttribute("aria-orientation"));
        Assert.Equal(2, cut.FindAll("[role=separator]").Count);
        Assert.Equal("33.33,33.33,33.33", cut.Find("bx-splitter").GetAttribute("sizes"));
        Assert.Equal("Resize panels 1", cut.FindAll("[role=separator]")[0].GetAttribute("aria-label"));
    }

    [Fact]
    public void Splitter_bound_sizes_follow_the_browser()
    {
        IReadOnlyList<double>? sizes = null;
        var cut = Render<BxSplitter>(p => p.Add(s => s.Panels, TwoPanels).Add(s => s.SizesChanged, (IReadOnlyList<double> v) => sizes = v));

        cut.Find("bx-splitter").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "45.5,54.5", Sequence = 3 });

        Assert.Equal([45.5, 54.5], sizes);
        Assert.Equal("45.5,54.5", cut.Find("bx-splitter").GetAttribute("sizes"));
        Assert.Equal("46", cut.Find("[role=separator]").GetAttribute("aria-valuenow"));
        Assert.Equal("3", cut.Find("bx-splitter").GetAttribute("data-bx-seq"));
    }
}

public class NavBasePathTests : BunitContext
{
    private sealed class BasePathNavigation : Microsoft.AspNetCore.Components.NavigationManager
    {
        public BasePathNavigation() => Initialize("http://localhost/app/", "http://localhost/app/docs/theming");
        protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options) { }
    }

    // The base path has to be in place before the first service is resolved, so this does not derive from BluxTestContext.
    public NavBasePathTests()
    {
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(new BasePathNavigation());
        Services.AddBluxUi();
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", true));
    }

    [Fact]
    public void NavigationMenu_does_not_mark_the_app_root_as_active_under_a_base_path()
    {
        BxNavigationItem[] items = [new("Home") { Href = "/app/" }, new("Docs") { Href = "/app/docs" }];
        var cut = Render<BxNavigationMenu>(p => p.Add(m => m.Items, items));

        Assert.Equal(["Docs"], cut.FindAll("a[aria-current=page]").Select(a => a.TextContent.Trim()));
    }
}
