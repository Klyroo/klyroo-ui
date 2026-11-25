namespace Klyroo.UI.Tests;

public class DashboardTests : KlyrooTestContext
{
    // ---- DashboardGroup

    [Fact]
    public void Group_unbound_renders_no_sequence_and_a_storage_id_only_with_a_key()
    {
        var plain = Render<KlyDashboardGroup>();
        var remembered = Render<KlyDashboardGroup>(p => p.Add(g => g.StorageKey, "app"));

        var element = plain.Find("kly-dashboard-group");
        Assert.False(element.HasAttribute("data-kly-seq"));
        Assert.False(element.HasAttribute("data-kly-dashboard-id"));
        Assert.False(element.HasAttribute("sizes"));
        Assert.Equal("app", remembered.Find("kly-dashboard-group").GetAttribute("data-kly-dashboard-id"));
        Assert.Contains("overflow-hidden", element.ClassList);
    }

    [Fact]
    public void Group_bound_renders_the_sequence_first_and_the_sizes()
    {
        var cut = Render<KlyDashboardGroup>(p => p
            .Add(g => g.Sizes, [30.0, 70.0])
            .Add(g => g.SizesChanged, (IReadOnlyList<double> _) => { }));

        var element = cut.Find("kly-dashboard-group");
        Assert.Equal("data-kly-seq", element.Attributes.First().Name);
        Assert.Equal("30,70", element.GetAttribute("sizes"));
    }

    [Fact]
    public void Group_gives_its_key_to_the_sidebar_and_to_the_controls()
    {
        var cut = Render<KlyDashboardGroup>(p => p.Add(g => g.StorageKey, "app").AddChildContent<KlyDashboardSidebar>(s => s
            .Add(x => x.As, "div")
            .Add(x => x.Header, b => b.AddContent(0, (Microsoft.AspNetCore.Components.RenderFragment)(inner =>
            {
                inner.OpenComponent<KlyDashboardSidebarCollapse>(0);
                inner.CloseComponent();
            })))));

        Assert.Equal("app", cut.Find("kly-sidebar").GetAttribute("data-kly-sidebar-id"));
        Assert.All(cut.FindAll("[data-kly-sidebar-collapse]"), c => Assert.Equal("app", c.GetAttribute("data-kly-sidebar-for")));
    }

    [Fact]
    public void Group_listens_for_its_sizes_on_a_childless_sink_and_not_on_itself()
    {
        var reported = new List<IReadOnlyList<double>>();
        var cut = Render<KlyDashboardGroup>(p => p
            .Add(g => g.Sizes, [30.0, 70.0])
            .Add(g => g.SizesChanged, (IReadOnlyList<double> sizes) => reported.Add(sizes))
            .AddChildContent("<p>child</p>"));

        // A listener on the group element would also hear the kly-change of any component inside it.
        Assert.Throws<Bunit.MissingEventHandlerException>(() => cut.Find("kly-dashboard-group").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "1,99", Sequence = 1 }));
        var sink = cut.Find("kly-dashboard-group > [data-kly-dashboard-sink]");
        Assert.Equal(0, sink.ChildElementCount);

        sink.TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "20,80", Sequence = 3 });

        Assert.Equal([20.0, 80.0], Assert.Single(reported));
        Assert.Equal("3", cut.Find("kly-dashboard-group").GetAttribute("data-kly-seq"));
    }

    [Fact]
    public void Group_gives_its_key_to_the_first_sidebar_only_and_numbers_the_next_ones()
    {
        var cut = Render<KlyDashboardGroup>(p => p.Add(g => g.StorageKey, "app").AddChildContent(b =>
        {
            b.OpenComponent<KlyDashboardSidebar>(0);
            b.AddAttribute(1, nameof(KlyDashboardSidebar.As), "div");
            b.CloseComponent();
            b.OpenComponent<KlyDashboardSidebar>(2);
            b.AddAttribute(3, nameof(KlyDashboardSidebar.As), "div");
            b.CloseComponent();
            b.OpenComponent<KlyDashboardSidebar>(4);
            b.AddAttribute(5, nameof(KlyDashboardSidebar.As), "div");
            b.AddAttribute(6, nameof(KlyDashboardSidebar.Id), "mine");
            b.CloseComponent();
        }));

        Assert.Equal(["app", "app-2", "mine"], cut.FindAll("kly-sidebar").Select(s => s.GetAttribute("data-kly-sidebar-id")));
    }

    private sealed class SidebarHost : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Microsoft.AspNetCore.Components.Parameter] public string? StorageKey { get; set; }
        [Microsoft.AspNetCore.Components.Parameter] public bool First { get; set; } = true;
        [Microsoft.AspNetCore.Components.Parameter] public bool Second { get; set; }
        [Microsoft.AspNetCore.Components.Parameter] public bool Third { get; set; }
        [Microsoft.AspNetCore.Components.Parameter] public string? FirstId { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
        {
            b.OpenComponent<KlyDashboardGroup>(0);
            b.AddAttribute(1, nameof(KlyDashboardGroup.StorageKey), StorageKey);
            b.AddAttribute(2, nameof(KlyDashboardGroup.ChildContent), (Microsoft.AspNetCore.Components.RenderFragment)(c =>
            {
                if (First) Sidebar(c, 10, "first", FirstId);
                if (Second) Sidebar(c, 20, "second");
                if (Third) Sidebar(c, 30, "third");
                c.OpenComponent<KlyDashboardSidebarCollapse>(40);
                c.CloseComponent();
            }));
            b.CloseComponent();
        }

        private static void Sidebar(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder c, int seq, string label, string? id = null)
        {
            c.OpenComponent<KlyDashboardSidebar>(seq);
            c.AddAttribute(seq + 1, nameof(KlyDashboardSidebar.As), "div");
            c.AddAttribute(seq + 2, nameof(KlyDashboardSidebar.AriaLabel), label);
            c.AddAttribute(seq + 3, nameof(KlyDashboardSidebar.Id), id);
            c.SetKey(label);
            c.CloseComponent();
        }
    }

    private static string?[] SidebarIds(Bunit.IRenderedComponent<SidebarHost> cut) =>
        [.. cut.FindAll("kly-sidebar").Select(s => s.GetAttribute("data-kly-sidebar-id"))];

    [Fact]
    public void Sidebar_removed_and_added_again_keeps_the_key_of_the_group()
    {
        var cut = Render<SidebarHost>(p => p.Add(h => h.StorageKey, "app"));
        Assert.Equal(["app"], SidebarIds(cut));

        cut.Render(p => p.Add(h => h.StorageKey, "app").Add(h => h.First, false));
        Assert.Empty(SidebarIds(cut));
        cut.Render(p => p.Add(h => h.StorageKey, "app").Add(h => h.First, true));

        Assert.Equal(["app"], SidebarIds(cut));
        Assert.Equal("app", cut.Find("[data-kly-sidebar-collapse]").GetAttribute("data-kly-sidebar-for"));
    }

    [Fact]
    public void Sidebar_removed_leaves_the_others_their_id_and_a_new_one_takes_the_free_slot()
    {
        var cut = Render<SidebarHost>(p => p.Add(h => h.StorageKey, "app").Add(h => h.Second, true));
        Assert.Equal(["app", "app-2"], SidebarIds(cut));

        cut.Render(p => p.Add(h => h.StorageKey, "app").Add(h => h.First, false).Add(h => h.Second, true));
        Assert.Equal(["app-2"], SidebarIds(cut));

        cut.Render(p => p.Add(h => h.StorageKey, "app").Add(h => h.First, false).Add(h => h.Second, true).Add(h => h.Third, true));
        var ids = SidebarIds(cut);
        Assert.Equal(["app-2", "app"], ids);
    }

    [Fact]
    public void Sidebar_with_its_own_id_leaves_the_key_of_the_group_to_the_next_one()
    {
        var cut = Render<SidebarHost>(p => p.Add(h => h.StorageKey, "app").Add(h => h.FirstId, "mine").Add(h => h.Second, true));
        Assert.Equal(["mine", "app"], SidebarIds(cut));

        // Dropping the Id takes the free slot; setting it again gives the slot back.
        cut.Render(p => p.Add(h => h.StorageKey, "app").Add(h => h.FirstId, null).Add(h => h.Second, true));
        Assert.Equal(["app-2", "app"], SidebarIds(cut));
        cut.Render(p => p.Add(h => h.StorageKey, "app").Add(h => h.FirstId, "mine").Add(h => h.Second, true).Add(h => h.Third, true));
        Assert.Equal(["mine", "app", "app-2"], SidebarIds(cut));
    }

    [Fact]
    public void Sidebar_and_controls_follow_a_changed_storage_key_together()
    {
        var cut = Render<SidebarHost>(p => p.Add(h => h.StorageKey, "app").Add(h => h.Second, true));
        Assert.Equal(["app", "app-2"], SidebarIds(cut));

        cut.Render(p => p.Add(h => h.StorageKey, "other").Add(h => h.Second, true));

        Assert.Equal(["other", "other-2"], SidebarIds(cut));
        Assert.Equal("other", cut.Find("[data-kly-sidebar-collapse]").GetAttribute("data-kly-sidebar-for"));
    }

    // ---- DashboardSidebar

    [Fact]
    public void Sidebar_has_no_collapse_button_and_no_trigger_of_its_own_and_renders_its_content_once()
    {
        var cut = Render<KlyDashboardSidebar>(p => p.Add(s => s.ChildContent, "<p class=\"nav\">Nav</p>"));

        var aside = cut.Find("aside");
        Assert.Equal("Sidebar", aside.GetAttribute("aria-label"));
        Assert.Empty(aside.QuerySelectorAll("[data-kly-sidebar-toggle]"));
        // The only button is the close button of the panel; the dashboard's toggle is the one that opens it.
        Assert.Single(aside.QuerySelectorAll("button"));
        Assert.Empty(cut.FindAll("[data-kly-sidebar-open]"));
        Assert.Empty(cut.FindAll("kly-sidebar > div"));
        Assert.Single(cut.FindAll("p.nav"));
    }

    [Fact]
    public void Sidebar_collapsible_adds_its_own_toggle_and_resizable_a_separator()
    {
        var cut = Render<KlyDashboardSidebar>(p => p.Add(s => s.Collapsible, true).Add(s => s.Resizable, true));

        Assert.NotNull(cut.Find("aside button[data-kly-sidebar-toggle]"));
        Assert.Equal("Resize sidebar", cut.Find("aside [role=separator]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Sidebar_bound_state_is_reported_and_rendered()
    {
        var cut = Render<KlyDashboardSidebar>(p => p.Add(s => s.Collapsed, true).Add(s => s.CollapsedChanged, (bool _) => { }));

        Assert.Equal("data-kly-seq", cut.Find("kly-sidebar").Attributes.First().Name);
        Assert.Equal("collapsed", cut.Find("aside").GetAttribute("data-state"));
    }

    [Fact]
    public void Sidebar_passes_attributes_and_class_to_the_landmark()
    {
        var cut = Render<KlyDashboardSidebar>(p => p.Add(s => s.As, "div").Add(s => s.Class, "bg-red-500")
            .AddUnmatched("data-testid", "side"));

        var root = cut.Find("[data-testid=side]");
        Assert.Equal("div", root.LocalName);
        Assert.False(root.HasAttribute("aria-label"));
        Assert.Contains("bg-red-500", root.ClassList);
    }

    // ---- DashboardSidebarCollapse and DashboardSidebarToggle

    [Fact]
    public void Collapse_is_a_named_button_for_the_sidebar_shown_from_lg_up_and_leaves_its_state_to_the_browser()
    {
        var cut = Render<KlyDashboardSidebarCollapse>(p => p.Add(c => c.For, "nav"));

        var button = cut.Find("button");
        Assert.Equal("Collapse sidebar", button.GetAttribute("aria-label"));
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.True(button.HasAttribute("data-kly-sidebar-collapse"));
        Assert.Equal("nav", button.GetAttribute("data-kly-sidebar-for"));
        Assert.Contains("hidden", button.ClassList);
        Assert.Contains("lg:inline-flex", button.ClassList);
        // aria-expanded and aria-controls depend on the stored state: the browser writes them.
        Assert.False(button.HasAttribute("aria-expanded"));
        Assert.False(button.HasAttribute("aria-controls"));
    }

    [Fact]
    public void Toggle_opens_the_panel_below_lg_and_announces_a_dialog()
    {
        var cut = Render<KlyDashboardSidebarToggle>(p => p.Add(c => c.Label, "Menu"));

        var button = cut.Find("button");
        Assert.Equal("Menu", button.GetAttribute("aria-label"));
        Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
        Assert.True(button.HasAttribute("data-kly-sidebar-open"));
        Assert.Contains("lg:hidden", button.ClassList);
        Assert.False(button.HasAttribute("data-kly-sidebar-for"));
    }

    [Fact]
    public void Toggle_names_the_panel_of_its_sidebar_from_the_server_so_it_needs_no_script()
    {
        // Without a group or a target there is nothing to name; the browser side wires it.
        Assert.False(Render<KlyDashboardSidebarToggle>().Find("button").HasAttribute("popovertarget"));
        Assert.Equal("kly-sidebar-nav", Render<KlyDashboardSidebarToggle>(p => p.Add(c => c.For, "nav")).Find("button").GetAttribute("popovertarget"));

        var cut = Render<KlyDashboardGroup>(p => p.Add(g => g.StorageKey, "app").AddChildContent((Microsoft.AspNetCore.Components.RenderFragment)(b =>
        {
            b.OpenComponent<KlyDashboardSidebar>(0);
            b.AddAttribute(1, nameof(KlyDashboardSidebar.As), "div");
            b.CloseComponent();
            b.OpenComponent<KlyDashboardSidebarToggle>(2);
            b.CloseComponent();
        })));

        var panel = cut.Find("kly-sidebar > [data-slot=root]");
        Assert.Equal("kly-sidebar-app", panel.GetAttribute("id"));
        Assert.Equal("kly-sidebar-app", cut.Find("button[data-kly-sidebar-open]").GetAttribute("popovertarget"));
    }

    // ---- DashboardPanel and DashboardResizeHandle

    [Fact]
    public void Panel_describes_its_limits_and_a_starting_flex_weight()
    {
        var cut = Render<KlyDashboardPanel>(p => p.Add(x => x.Size, 30.5).Add(x => x.MinSize, 10).Add(x => x.MaxSize, 60)
            .Add(x => x.Header, "<h2>Head</h2>").Add(x => x.Body, "<p>Body</p>").Add(x => x.Footer, "<p>Foot</p>"));

        var panel = cut.Find("[data-kly-dash-panel]");
        Assert.Equal("30.5", panel.GetAttribute("data-size"));
        Assert.Equal("10", panel.GetAttribute("data-min"));
        Assert.Equal("60", panel.GetAttribute("data-max"));
        Assert.Equal("flex: 30.5 1 0%", panel.GetAttribute("style"));
        Assert.Equal(["header", "body", "footer"], panel.Children.Select(c => c.GetAttribute("data-slot")));
        Assert.Empty(cut.FindAll("[role=separator]"));
    }

    [Fact]
    public void Panel_without_a_size_takes_an_equal_share_and_child_content_replaces_the_slots()
    {
        var cut = Render<KlyDashboardPanel>(p => p.Add(x => x.ChildContent, "<p class=\"all\">All</p>").Add(x => x.Body, "<p>Ignored</p>"));

        var panel = cut.Find("[data-kly-dash-panel]");
        Assert.False(panel.HasAttribute("data-size"));
        Assert.Equal("flex: 1 1 0%", panel.GetAttribute("style"));
        Assert.Single(cut.FindAll("p.all"));
        Assert.Empty(cut.FindAll("[data-slot=body]"));
    }

    [Fact]
    public void Resizable_panel_has_a_separator_that_controls_it()
    {
        var cut = Render<KlyDashboardPanel>(p => p.Add(x => x.Resizable, true).Add(x => x.Size, 40).Add(x => x.ResizeLabel, "Resize list"));

        var panel = cut.Find("[data-kly-dash-panel]");
        var handle = cut.Find("[role=separator]");
        Assert.Equal(panel.Id, handle.GetAttribute("aria-controls"));
        Assert.Equal("Resize list", handle.GetAttribute("aria-label"));
        Assert.Equal("40", handle.GetAttribute("aria-valuenow"));
        Assert.Equal("vertical", handle.GetAttribute("aria-orientation"));
        Assert.Equal("0", handle.GetAttribute("tabindex"));
        Assert.Contains("absolute", handle.ClassList);
        Assert.DoesNotContain("relative", handle.ClassList);
    }

    [Fact]
    public void Resize_handle_is_a_focusable_separator_with_the_values_a_static_page_needs()
    {
        var cut = Render<KlyDashboardResizeHandle>(p => p.Add(h => h.Value, 33.4));

        var handle = cut.Find("[data-kly-dash-handle]");
        Assert.Equal("separator", handle.GetAttribute("role"));
        Assert.Equal("0", handle.GetAttribute("tabindex"));
        Assert.Equal("33", handle.GetAttribute("aria-valuenow"));
        Assert.Equal("0", handle.GetAttribute("aria-valuemin"));
        Assert.Equal("100", handle.GetAttribute("aria-valuemax"));
        Assert.Equal("Resize panels", handle.GetAttribute("aria-label"));
        Assert.Equal("5", handle.GetAttribute("data-step"));
    }

    // ---- DashboardNavbar and DashboardToolbar

    [Fact]
    public void Navbar_has_a_title_heading_and_the_sidebar_toggle_first()
    {
        var cut = Render<KlyDashboardNavbar>(p => p.Add(n => n.Title, "Inbox").Add(n => n.Right, "<button>Act</button>"));

        Assert.Equal("Inbox", cut.Find("header h1").TextContent);
        var left = cut.Find("[data-slot=left]");
        Assert.True(left.Children[0].HasAttribute("data-kly-sidebar-open"));
        Assert.Equal("h1", left.Children[1].LocalName);
        Assert.Single(cut.FindAll("[data-slot=right] button"));
        Assert.Empty(cut.FindAll("[data-slot=center]"));
    }

    [Fact]
    public void Navbar_can_leave_the_toggle_out_and_places_its_areas()
    {
        var cut = Render<KlyDashboardNavbar>(p => p.Add(n => n.Toggle, false)
            .Add(n => n.Leading, "<i class=\"lead\"></i>").Add(n => n.Trailing, "<i class=\"trail\"></i>")
            .Add(n => n.ChildContent, "<i class=\"mid\"></i>"));

        Assert.Empty(cut.FindAll("[data-kly-sidebar-open]"));
        Assert.Empty(cut.FindAll("h1"));
        Assert.Equal(["lead", "trail"], cut.Find("[data-slot=left]").Children.Select(c => c.ClassName));
        Assert.Single(cut.FindAll("[data-slot=center] .mid"));
    }

    [Fact]
    public void Navbar_renders_a_header_and_an_h1_by_default()
    {
        var cut = Render<KlyDashboardNavbar>(p => p.Add(n => n.Title, "Inbox"));

        Assert.Equal("header", cut.Find("[data-slot=left]").ParentElement!.LocalName);
        Assert.Equal("h1", cut.Find("[data-slot=left] > :nth-child(2)").LocalName);
    }

    [Fact]
    public void Navbar_can_be_a_div_with_a_lower_heading_so_a_second_navbar_adds_no_banner_or_h1()
    {
        var cut = Render<KlyDashboardNavbar>(p => p.Add(n => n.Title, "Message").Add(n => n.As, "div").Add(n => n.HeadingLevel, 2)
            .Add(n => n.Right, "<button>Act</button>"));

        Assert.Empty(cut.FindAll("header"));
        Assert.Empty(cut.FindAll("h1"));
        Assert.Equal("div", cut.Find("[data-slot=left]").ParentElement!.LocalName);
        Assert.Equal("Message", cut.Find("h2").TextContent);
        Assert.Single(cut.FindAll("[data-slot=right] button"));
    }

    [Fact]
    public void Navbar_As_is_case_insensitive_and_rejects_other_elements()
    {
        var cut = Render<KlyDashboardNavbar>(p => p.Add(n => n.As, "DIV"));
        Assert.Empty(cut.FindAll("header"));
        Assert.Equal("div", cut.Find("[data-slot=left]").ParentElement!.LocalName);

        var error = Assert.ThrowsAny<Exception>(() => Render<KlyDashboardNavbar>(p => p.Add(n => n.As, "section")));
        Assert.Contains("As must be", (error as ArgumentException ?? error.InnerException as ArgumentException ?? error.GetBaseException() as ArgumentException)!.Message);
    }

    [Fact]
    public void Navbar_and_PageAside_take_a_missing_As_for_their_default_element()
    {
        Assert.Equal("header", Render<KlyDashboardNavbar>(p => p.Add(n => n.As, null!)).Find("[data-slot=left]").ParentElement!.LocalName);
        Assert.Equal("aside", Render<KlyPageAside>(p => p.Add(a => a.As, null!)).Find("[data-slot=body]").ParentElement!.LocalName);
    }

    [Fact]
    public void PageAside_As_is_case_insensitive_and_rejects_other_elements()
    {
        Assert.Equal("div", Render<KlyPageAside>(p => p.Add(a => a.As, "Div")).Find("[data-slot=body]").ParentElement!.LocalName);
        Assert.ThrowsAny<Exception>(() => Render<KlyPageAside>(p => p.Add(a => a.As, "nav")));
    }

    [Fact]
    public void Toolbar_renders_only_the_areas_it_was_given()
    {
        var cut = Render<KlyDashboardToolbar>(p => p.Add(t => t.Left, "<b>L</b>").Add(t => t.Right, "<b>R</b>"));

        Assert.Equal(["left", "right"], cut.Find("div").Children.Select(c => c.GetAttribute("data-slot")));
    }

    // ---- DashboardSearch and DashboardSearchButton

    private static readonly KlyCommandGroup[] Groups =
    [
        new("Go to", [new() { Label = "Inbox", Href = "/inbox" }, new() { Label = "Settings", Href = "/settings" }]),
        new("Actions", [new() { Label = "Sign out" }]),
    ];

    [Fact]
    public void Search_is_a_modal_with_a_command_palette_and_a_hidden_hotkey_trigger()
    {
        var cut = Render<KlyDashboardSearch>(p => p.Add(s => s.Groups, Groups));

        var dialog = cut.Find("dialog");
        Assert.True(dialog.HasAttribute("data-kly-dashboard-search"));
        Assert.Equal("Search", dialog.GetAttribute("aria-label"));
        // A palette is not given a close button: Escape and a selection close it.
        Assert.Empty(dialog.QuerySelectorAll("button[aria-label='Close']"));
        var trigger = cut.Find("kly-modal [data-slot=trigger] button");
        Assert.Equal("mod+k", trigger.GetAttribute("data-kly-hotkey"));
        Assert.True(trigger.HasAttribute("data-kly-hotkey-always"));
        Assert.True(trigger.HasAttribute("hidden"));
        Assert.Equal("Type a command or search...", cut.Find("input[role=combobox]").GetAttribute("placeholder"));
        Assert.Equal(3, cut.FindAll("[role=option]").Count);
        Assert.Equal("/inbox", cut.FindAll("a[role=option]")[0].GetAttribute("href"));
        // Selecting a command closes the modal without a round trip.
        Assert.All(cut.FindAll("[role=option]"), o => Assert.True(o.HasAttribute("data-kly-close")));
        Assert.False(cut.Find("kly-modal").HasAttribute("data-kly-seq"));
    }

    [Fact]
    public void Search_takes_a_custom_shortcut_and_bound_open_state()
    {
        var cut = Render<KlyDashboardSearch>(p => p.Add(s => s.Groups, Groups).Add(s => s.Shortcut, "ctrl+/")
            .Add(s => s.Open, true).Add(s => s.OpenChanged, (bool _) => { }));

        Assert.Equal("ctrl+/", cut.Find("[data-kly-hotkey]").GetAttribute("data-kly-hotkey"));
        Assert.Equal("data-kly-seq", cut.Find("kly-modal").Attributes.First().Name);
        Assert.True(cut.Find("kly-modal").HasAttribute("open"));
    }

    [Fact]
    public void Search_button_opens_the_search_and_shows_the_shortcut_with_kbd()
    {
        var cut = Render<KlyDashboardSearchButton>();

        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
        Assert.True(button.HasAttribute("data-kly-search-open"));
        Assert.Equal("Search", button.QuerySelector("span")!.TextContent);
        Assert.Contains("group-data-[state=collapsed]/sidebar:sr-only", button.QuerySelector("span")!.ClassList);
    }

    [Fact]
    public void Search_button_default_hint_follows_the_platform_in_the_browser_and_announces_the_shortcut()
    {
        var cut = Render<KlyDashboardSearchButton>();

        var keys = cut.FindAll("kbd");
        Assert.Equal(2, keys.Count);
        // The modifier is an attribute label the browser sets ("⌘" on Apple), never text Blazor owns.
        Assert.True(keys[0].HasAttribute("data-kly-mod"));
        Assert.Equal("Ctrl", keys[0].GetAttribute("data-label"));
        Assert.Equal("", keys[0].TextContent);
        Assert.Equal("K", keys[1].TextContent);
        Assert.Equal("Control+K Meta+K", cut.Find("button").GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void Search_button_hosts_the_platform_hint_in_its_own_element_so_no_global_observer_is_needed()
    {
        var cut = Render<KlyDashboardSearchButton>();

        var host = cut.Find("kly-mod-keys");
        Assert.Equal("true", host.GetAttribute("aria-hidden"));
        Assert.Equal(2, host.QuerySelectorAll("kbd").Length);
        Assert.NotNull(host.QuerySelector("kbd[data-kly-mod]"));
    }

    [Fact]
    public void Search_button_explicit_keys_are_shown_as_given_without_a_platform_hint()
    {
        var cut = Render<KlyDashboardSearchButton>(p => p.Add(b => b.Keys, ["Ctrl", "K"]));

        Assert.Equal(["Ctrl", "K"], cut.FindAll("kbd").Select(k => k.TextContent));
        Assert.Empty(cut.FindAll("[data-kly-mod]"));
        Assert.False(cut.Find("button").HasAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void Search_button_texts_and_keys_can_be_changed()
    {
        var cut = Render<KlyDashboardSearchButton>(p => p.Add(b => b.Label, "Find").Add(b => b.Keys, ["/"]));

        Assert.Equal("Find", cut.Find("button span").TextContent);
        Assert.Equal(["/"], cut.FindAll("kbd").Select(k => k.TextContent));
        Assert.Empty(Render<KlyDashboardSearchButton>(p => p.Add(b => b.Keys, [])).FindAll("kbd"));
    }
}

public class DashboardTextsTests() : KlyrooTestContext(o =>
{
    o.Locale = KlyLocales.Extend(KlyLocales.En, messages: m => m with { DashboardSearch = "Rechercher", DashboardSearchButton = "Chercher", CollapseSidebar = "Replier" });
})
{
    [Fact]
    public void Dashboard_texts_come_from_the_options()
    {
        Assert.Equal("Rechercher", Render<KlyDashboardSearch>().Find("dialog").GetAttribute("aria-label"));
        Assert.Equal("Chercher", Render<KlyDashboardSearchButton>().Find("button span").TextContent);
        Assert.Equal("Replier", Render<KlyDashboardSidebarCollapse>().Find("button").GetAttribute("aria-label"));
    }
}
