using System.ComponentModel;
using System.Runtime.CompilerServices;
using TextLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui.Tests;

public class LayoutTests
{
    // A binding hands changes of its source to the dispatcher of the target. A test has none, so it gets one that runs the work right away.
    static LayoutTests() => DispatcherProvider.SetCurrent(new InlineDispatcherProvider());

    private sealed class InlineDispatcherProvider : IDispatcherProvider
    {
        public IDispatcher? GetForCurrentThread() => new InlineDispatcher();
    }

    private sealed class InlineDispatcher : IDispatcher
    {
        public bool IsDispatchRequired => false;

        public IDispatcherTimer CreateTimer() => throw new NotSupportedException();

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();
    }

    private sealed class Vm : INotifyPropertyChanged
    {
        private bool _collapsed;
        private bool _open;
        private bool _menuOpen;
        private string _text = "";
        private IReadOnlyList<double>? _sizes;
        private double _width = 256;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Collapsed { get => _collapsed; set => Set(ref _collapsed, value); }

        public bool Open { get => _open; set => Set(ref _open, value); }

        public bool MenuOpen { get => _menuOpen; set => Set(ref _menuOpen, value); }

        public string Text { get => _text; set => Set(ref _text, value); }

        public IReadOnlyList<double>? Sizes { get => _sizes; set => Set(ref _sizes, value); }

        public double Width { get => _width; set => Set(ref _width, value); }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private static T Slot<T>(View inside)
        where T : class => (T)(object)inside.Parent;

    private static IEnumerable<LayoutIconButton> IconButtons(Element root) => LayoutTree.Descendants<LayoutIconButton>(root);

    // KlyApp

    [Fact]
    public void App_fills_the_page_keeps_the_safe_area_and_follows_the_theme_background()
    {
        var app = new KlyApp();

        Assert.Equal(LayoutOptions.Fill, app.HorizontalOptions);
        Assert.Equal(LayoutOptions.Fill, app.VerticalOptions);
        Assert.True(app.SafeArea);
        Assert.Equal(SafeAreaEdges.All, app.SafeAreaEdges);
        Assert.Equal(AppTheme.Unspecified, app.ColorMode);

        app.SafeArea = false;
        Assert.Equal(SafeAreaEdges.None, app.SafeAreaEdges);
        app.SafeArea = true;
        Assert.Equal(SafeAreaEdges.All, app.SafeAreaEdges);
    }

    [Fact]
    public void App_wraps_its_content()
    {
        var content = new Label { Text = "Hello" };
        var app = new KlyApp { Content = content };

        Assert.Same(content, app.Content);
    }

    [Fact]
    public void App_color_mode_without_an_application_is_kept_and_does_not_throw()
    {
        var previous = Application.Current;
        Application.Current = null;
        try
        {
            var app = new KlyApp { ColorMode = AppTheme.Dark };

            Assert.Equal(AppTheme.Dark, app.ColorMode);
        }
        finally
        {
            Application.Current = previous;
        }
    }

    [Fact]
    public void App_color_mode_sets_the_user_theme_of_the_application_and_the_default_leaves_it_alone()
    {
        var previous = Application.Current;
        var application = new Application { UserAppTheme = AppTheme.Light };
        Application.Current = application;
        try
        {
            var app = new KlyApp();
            Assert.Equal(AppTheme.Light, application.UserAppTheme);

            app.ColorMode = AppTheme.Dark;
            Assert.Equal(AppTheme.Dark, application.UserAppTheme);

            app.ColorMode = AppTheme.Unspecified;
            Assert.Equal(AppTheme.Unspecified, application.UserAppTheme);
        }
        finally
        {
            Application.Current = previous;
        }
    }

    // KlyContainer

    [Theory]
    [InlineData(KlySize.Xs, 672)]
    [InlineData(KlySize.Sm, 896)]
    [InlineData(KlySize.Md, 1280)]
    [InlineData(KlySize.Lg, 1536)]
    public void Container_maximum_width_follows_its_size(KlySize size, double expected) =>
        Assert.Equal(expected, KlyContainer.MaxWidthOf(size));

    [Fact]
    public void Container_has_no_maximum_width_for_the_extra_large_size() =>
        Assert.True(double.IsPositiveInfinity(KlyContainer.MaxWidthOf(KlySize.Xl)));

    [Theory]
    [InlineData(500, 16, 468)]
    [InlineData(800, 24, 752)]
    [InlineData(1100, 32, 1036)]
    public void Container_pads_more_as_it_gets_wider(double width, double inset, double content)
    {
        var area = KlyContainer.ContentArea(new Rect(0, 0, width, 300), KlySize.Xl);

        Assert.Equal(inset, area.X);
        Assert.Equal(content, area.Width);
    }

    [Fact]
    public void Container_centers_its_content_at_the_maximum_width()
    {
        var area = KlyContainer.ContentArea(new Rect(0, 10, 2000, 300), KlySize.Md);

        // 1280 wide, centered in 2000: 360 on each side, then the 32 of padding.
        Assert.Equal(392, area.X);
        Assert.Equal(1216, area.Width);
        Assert.Equal(10, area.Y);
        Assert.Equal(300, area.Height);
    }

    [Fact]
    public void Container_arranges_its_children_inside_the_centered_area()
    {
        var child = new BoxView { HeightRequest = 50 };
        var container = new KlyContainer { Size = KlySize.Sm, Children = { child } };
        var layout = (ICrossPlatformLayout)container;

        layout.CrossPlatformMeasure(2000, 1000);
        layout.CrossPlatformArrange(new Rect(0, 0, 2000, 1000));

        // 896 wide, centered in 2000 (552 each side), then 24 of padding (it is over 640 wide, under 1024).
        Assert.Equal(576, child.Frame.X);
        Assert.Equal(848, child.Frame.Width);
    }

    // KlyMain

    [Fact]
    public void Main_scrolls_its_content_by_default_and_can_stop()
    {
        var content = new Label { Text = "Page" };
        var main = new KlyMain { ChildContent = content };

        var scroll = Assert.IsType<ScrollView>(main.Content);
        Assert.Same(content, scroll.Content);
        Assert.True(main.Scrollable);

        main.Scrollable = false;
        Assert.Same(content, main.Content);

        main.Scrollable = true;
        Assert.IsType<ScrollView>(main.Content);
        Assert.Same(content, ((ScrollView)main.Content!).Content);
    }

    [Fact]
    public void Main_takes_the_space_the_header_and_footer_leave()
    {
        var main = new KlyMain();

        Assert.Equal(LayoutOptions.Fill, main.HorizontalOptions);
        Assert.Equal(LayoutOptions.Fill, main.VerticalOptions);
    }

    // KlyHeader

    [Fact]
    public void Header_starts_compact_and_lines_up_from_1024_wide()
    {
        var header = new KlyHeader();
        Assert.True(header.IsCompact);

        header.UpdateWidth(1023);
        Assert.True(header.IsCompact);

        header.UpdateWidth(1024);
        Assert.False(header.IsCompact);

        header.UpdateWidth(500);
        Assert.True(header.IsCompact);
    }

    [Fact]
    public void Header_shows_the_title_until_a_left_view_replaces_it()
    {
        var header = new KlyHeader { Title = "Klyroo" };
        var title = LayoutTree.Descendants<TextLabel>(header).Single();

        Assert.Equal("Klyroo", title.Text);
        Assert.True(title.Parent is ContentView { IsVisible: true });
        Assert.Equal(18, title.FontSize);

        var logo = new BoxView();
        header.Left = logo;
        Assert.True(((ContentView)logo.Parent).IsVisible);
        Assert.Null(title.Parent);

        header.Left = null;
        Assert.Same(title, ((ContentView)title.Parent!).Content);
    }

    [Fact]
    public void Header_title_tap_raises_one_event()
    {
        var header = new KlyHeader { Title = "Klyroo" };
        var taps = 0;
        header.TitleTapped += (_, _) => taps++;

        var title = LayoutTree.Descendants<TextLabel>(header).Single();
        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(title.GestureRecognizers));
        tap.Command!.Execute(null);

        Assert.Equal(1, taps);
    }

    [Fact]
    public void Header_hides_the_middle_and_shows_a_menu_button_when_compact()
    {
        var middle = new Label { Text = "Links" };
        var header = new KlyHeader { ChildContent = middle, Body = new Label { Text = "Menu body" } };
        var button = IconButtons(header).Single();

        header.UpdateWidth(500);
        Assert.False(((ContentView)middle.Parent).IsVisible);
        Assert.True(button.IsVisible);

        header.UpdateWidth(1200);
        Assert.True(((ContentView)middle.Parent).IsVisible);
        Assert.False(button.IsVisible);
    }

    [Fact]
    public void Header_has_no_menu_button_without_a_body_or_when_the_menu_is_off()
    {
        var header = new KlyHeader { ChildContent = new Label() };
        var button = IconButtons(header).Single();
        header.UpdateWidth(500);
        Assert.False(button.IsVisible);

        header.Body = new Label();
        Assert.True(button.IsVisible);

        header.Menu = false;
        Assert.False(button.IsVisible);
    }

    [Fact]
    public void Header_menu_button_opens_and_closes_the_panel_and_widening_closes_it()
    {
        var body = new Label { Text = "Menu body" };
        var header = new KlyHeader { Body = body };
        var button = IconButtons(header).Single();
        var panel = (ContentView)body.Parent;
        header.UpdateWidth(500);
        Assert.False(panel.IsVisible);

        button.SendClicked();
        Assert.True(header.MenuOpen);
        Assert.True(panel.IsVisible);

        button.SendClicked();
        Assert.False(header.MenuOpen);
        Assert.False(panel.IsVisible);

        header.MenuOpen = true;
        header.UpdateWidth(1300);
        Assert.False(header.MenuOpen);
        Assert.False(panel.IsVisible);
    }

    [Fact]
    public void Header_menu_state_is_two_way_bindable()
    {
        var vm = new Vm();
        var header = new KlyHeader { BindingContext = vm, Body = new Label() };
        header.SetBinding(KlyHeader.MenuOpenProperty, new Binding(nameof(Vm.MenuOpen), BindingMode.TwoWay));
        header.UpdateWidth(500);

        vm.MenuOpen = true;
        Assert.True(header.MenuOpen);

        IconButtons(header).Single().SendClicked();
        Assert.False(vm.MenuOpen);
    }

    [Fact]
    public void Header_menu_button_has_an_accessible_name_that_can_be_translated()
    {
        var header = new KlyHeader();
        var button = IconButtons(header).Single();
        Assert.Equal("Open menu", button.Label);

        header.MenuLabel = "Ouvrir le menu";
        Assert.Equal("Ouvrir le menu", button.Label);
    }

    [Fact]
    public void Header_bar_is_four_rem_high()
    {
        var header = new KlyHeader();

        var row = LayoutTree.Descendants<Grid>(header).First(g => g.HeightRequest > 0);
        Assert.Equal(64, row.HeightRequest);
    }

    // KlyFooter

    [Fact]
    public void Footer_starts_compact_and_lines_up_from_1024_wide()
    {
        var footer = new KlyFooter();
        Assert.True(footer.IsCompact);

        footer.UpdateWidth(1024);
        Assert.False(footer.IsCompact);
    }

    [Fact]
    public void Footer_lines_left_center_and_right_up_in_a_row_when_wide_and_stacks_them_when_compact()
    {
        var left = new Label();
        var center = new Label();
        var right = new Label();
        var footer = new KlyFooter { Left = left, ChildContent = center, Right = right };

        footer.UpdateWidth(500);
        Assert.Equal([0, 1, 2], new[] { Grid.GetRow((BindableObject)left.Parent), Grid.GetRow((BindableObject)center.Parent), Grid.GetRow((BindableObject)right.Parent) });
        Assert.Equal(LayoutOptions.Center, ((View)left.Parent).HorizontalOptions);

        footer.UpdateWidth(1200);
        Assert.Equal([0, 1, 2], new[] { Grid.GetColumn((BindableObject)left.Parent), Grid.GetColumn((BindableObject)center.Parent), Grid.GetColumn((BindableObject)right.Parent) });
        Assert.Equal(LayoutOptions.Start, ((View)left.Parent).HorizontalOptions);
        Assert.Equal(LayoutOptions.End, ((View)right.Parent).HorizontalOptions);
    }

    [Fact]
    public void Footer_shows_the_top_and_bottom_areas_only_when_they_have_content()
    {
        var top = new Label();
        var bottom = new Label();
        var footer = new KlyFooter();
        var topArea = (Grid)footer.Children[1];
        var bottomArea = (Grid)footer.Children[3];

        Assert.False(topArea.IsVisible);
        Assert.False(bottomArea.IsVisible);

        footer.Top = top;
        footer.Bottom = bottom;
        Assert.True(topArea.IsVisible);
        Assert.True(bottomArea.IsVisible);
        Assert.True(((ContentView)top.Parent).IsVisible);
        Assert.True(((ContentView)bottom.Parent).IsVisible);

        footer.Top = null;
        footer.Bottom = null;
        Assert.False(topArea.IsVisible);
        Assert.False(bottomArea.IsVisible);
    }

    [Fact]
    public void Footer_row_is_gone_without_left_center_and_right()
    {
        var footer = new KlyFooter();
        var row = footer.Children.OfType<KlyContainer>().Single();
        Assert.False(row.IsVisible);

        footer.Left = new Label();
        Assert.True(row.IsVisible);
    }

    // KlyFooterColumns

    private static IReadOnlyList<KlyFooterColumn> SampleColumns() =>
    [
        new KlyFooterColumn("Product") { Links = [new KlyFooterLink("Docs") { Href = "/docs", Icon = KlyIconNames.File }, new KlyFooterLink("Pricing") { Href = "/pricing" }] },
        new KlyFooterColumn("Company") { Links = [new KlyFooterLink("About"), new KlyFooterLink("Jobs") { Disabled = true }] },
        new KlyFooterColumn("Legal") { Links = [new KlyFooterLink("Terms")] },
    ];

    [Theory]
    [InlineData(400, 2)]
    [InlineData(767, 2)]
    [InlineData(768, 3)]
    [InlineData(1500, 3)]
    public void Footer_columns_flow_two_per_row_below_768_and_three_above(double width, int expected)
    {
        var columns = new KlyFooterColumns { Columns = SampleColumns() };

        columns.UpdateWidth(width);

        Assert.Equal(expected, columns.NavColumnCount);
    }

    [Fact]
    public void Footer_columns_show_each_heading_with_its_level_and_its_links()
    {
        var columns = new KlyFooterColumns { Columns = SampleColumns(), HeadingLevel = 2 };

        var headings = LayoutTree.Descendants<TextLabel>(columns).Where(l => SemanticProperties.GetHeadingLevel(l) != SemanticHeadingLevel.None).ToList();
        Assert.Equal(["Product", "Company", "Legal"], headings.Select(h => h.Text));
        Assert.All(headings, h => Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(h)));

        var texts = LayoutTree.Descendants<TextLabel>(columns).Select(l => l.Text).ToList();
        Assert.Contains("Docs", texts);
        Assert.Contains("Pricing", texts);
        Assert.Contains("Terms", texts);
    }

    [Fact]
    public void Footer_columns_rebuild_when_the_list_changes_and_the_row_count_follows_the_width()
    {
        var columns = new KlyFooterColumns { Columns = SampleColumns() };
        var nav = LayoutTree.Descendants<Grid>(columns).First(g => g.ColumnSpacing == 32 && g.RowSpacing == 32);

        columns.UpdateWidth(400);
        Assert.Equal(2, nav.RowDefinitions.Count);

        columns.UpdateWidth(900);
        Assert.Single(nav.RowDefinitions);

        columns.Columns = [new KlyFooterColumn("Only")];
        Assert.Single(nav.Children);
    }

    [Fact]
    public void Footer_columns_raise_one_event_for_an_enabled_link_and_none_for_a_disabled_one()
    {
        var columns = new KlyFooterColumns { Columns = SampleColumns() };
        var tapped = new List<KlyFooterLink>();
        columns.LinkTapped += (_, link) => tapped.Add(link);

        columns.Tap(SampleColumns()[0].Links[0]);
        columns.Tap(new KlyFooterLink("Jobs") { Disabled = true });

        Assert.Equal(["Docs"], tapped.Select(l => l.Label));
        Assert.Equal("/docs", tapped[0].Href);
    }

    [Fact]
    public void Footer_columns_do_not_make_a_disabled_link_tappable()
    {
        var columns = new KlyFooterColumns { Columns = SampleColumns() };

        var jobs = LayoutTree.Descendants<HorizontalStackLayout>(columns).Single(r => r.Children.OfType<TextLabel>().Any(l => l.Text == "Jobs"));
        var docs = LayoutTree.Descendants<HorizontalStackLayout>(columns).Single(r => r.Children.OfType<TextLabel>().Any(l => l.Text == "Docs"));

        Assert.False(jobs.IsEnabled);
        Assert.Empty(jobs.GestureRecognizers);
        Assert.Single(docs.GestureRecognizers);
        Assert.Single(docs.Children.OfType<KlyIcon>());
    }

    [Fact]
    public void Footer_links_and_the_header_title_are_touch_targets_of_forty_units()
    {
        var columns = new KlyFooterColumns { Columns = SampleColumns() };
        var header = new KlyHeader { Title = "Klyroo" };

        var links = LayoutTree.Descendants<HorizontalStackLayout>(columns).ToList();
        Assert.NotEmpty(links);
        Assert.All(links, l => Assert.True(l.MinimumHeightRequest >= 40));
        Assert.True(LayoutTree.Descendants<TextLabel>(header).Single().MinimumHeightRequest >= 40);
    }

    [Fact]
    public void Footer_columns_put_left_beside_the_links_only_from_1280_wide()
    {
        var left = new Label();
        var right = new Label();
        var columns = new KlyFooterColumns { Columns = SampleColumns(), Left = left, Right = right };

        columns.UpdateWidth(1000);
        Assert.Equal(0, Grid.GetRow((BindableObject)left.Parent));
        Assert.Equal(2, Grid.GetRow((BindableObject)right.Parent));

        columns.UpdateWidth(1300);
        Assert.Equal(0, Grid.GetRow((BindableObject)left.Parent));
        Assert.Equal(1, Grid.GetRow((BindableObject)right.Parent));
        Assert.Equal(0, Grid.GetColumn((BindableObject)right.Parent));
    }

    // KlySidebar

    [Fact]
    public void Sidebar_defaults()
    {
        var sidebar = new KlySidebar();

        Assert.True(sidebar.IsPanel);
        Assert.True(sidebar.Collapsible);
        Assert.False(sidebar.Collapsed);
        Assert.False(sidebar.Resizable);
        Assert.Equal(256, sidebar.SidebarWidth);
        Assert.Equal(192, sidebar.MinWidth);
        Assert.Equal(448, sidebar.MaxWidth);
        Assert.True(sidebar.MobileTrigger);
        Assert.False(sidebar.IsOpen);
        Assert.Equal("Sidebar", sidebar.MobileTitle);
        Assert.Null(sidebar.SidebarId);
    }

    [Fact]
    public void Sidebar_is_a_column_from_1024_wide_and_a_panel_below()
    {
        var sidebar = new KlySidebar();
        var host = (Grid)sidebar.Children[1];

        sidebar.UpdateAvailableWidth(1200);
        Assert.False(sidebar.IsPanel);
        Assert.Equal(256, host.WidthRequest);
        Assert.Equal(LayoutOptions.Start, sidebar.HorizontalOptions);
        Assert.True(host.IsVisible);

        sidebar.UpdateAvailableWidth(800);
        Assert.True(sidebar.IsPanel);
        Assert.Equal(LayoutOptions.Fill, sidebar.HorizontalOptions);
        Assert.False(host.IsVisible);
    }

    [Fact]
    public void Sidebar_collapses_to_a_sixty_four_wide_rail_and_raises_one_event_per_change()
    {
        var sidebar = new KlySidebar();
        sidebar.UpdateAvailableWidth(1200);
        var host = (Grid)sidebar.Children[1];
        var events = new List<bool>();
        sidebar.CollapsedChanged += (_, e) => events.Add(e.Value);

        sidebar.Collapsed = true;
        Assert.Equal(64, host.WidthRequest);

        sidebar.Collapsed = true;
        sidebar.Collapsed = false;
        Assert.Equal(256, host.WidthRequest);
        Assert.Equal([true, false], events);
    }

    [Fact]
    public void Sidebar_collapse_button_toggles_the_state_and_changes_its_icon()
    {
        var sidebar = new KlySidebar();
        sidebar.UpdateAvailableWidth(1200);
        var collapse = IconButtons(sidebar).Single(b => b.Label == "Collapse sidebar");
        Assert.True(collapse.IsVisible);

        collapse.SendClicked();
        Assert.True(sidebar.Collapsed);
        Assert.Equal(KlyIconNames.ChevronRight, collapse.IconName);

        collapse.SendClicked();
        Assert.False(sidebar.Collapsed);
        Assert.Equal(KlyIconNames.ChevronLeft, collapse.IconName);
    }

    [Fact]
    public void Sidebar_hides_the_collapse_button_when_it_is_not_collapsible_or_is_a_panel()
    {
        var sidebar = new KlySidebar();
        var collapse = IconButtons(sidebar).Single(b => b.Label == "Collapse sidebar");

        Assert.False(collapse.IsVisible);

        sidebar.UpdateAvailableWidth(1200);
        Assert.True(collapse.IsVisible);

        sidebar.Collapsible = false;
        Assert.False(collapse.IsVisible);
    }

    [Fact]
    public void Sidebar_collapsed_and_open_states_are_two_way_bindable()
    {
        var vm = new Vm();
        var sidebar = new KlySidebar { BindingContext = vm };
        sidebar.SetBinding(KlySidebar.CollapsedProperty, new Binding(nameof(Vm.Collapsed), BindingMode.TwoWay));
        sidebar.SetBinding(KlySidebar.IsOpenProperty, new Binding(nameof(Vm.Open), BindingMode.TwoWay));
        sidebar.SetBinding(KlySidebar.SidebarWidthProperty, new Binding(nameof(Vm.Width), BindingMode.TwoWay));
        sidebar.UpdateAvailableWidth(1200);

        vm.Collapsed = true;
        Assert.True(sidebar.Collapsed);
        sidebar.Collapsed = false;
        Assert.False(vm.Collapsed);

        sidebar.UpdateAvailableWidth(700);
        sidebar.IsOpen = true;
        Assert.True(vm.Open);
        vm.Open = false;
        Assert.False(sidebar.IsOpen);

        sidebar.UpdateAvailableWidth(1200);
        sidebar.Resizable = true;
        sidebar.ResizeBy(40);
        Assert.Equal(296, vm.Width);
    }

    [Fact]
    public void Sidebar_resize_stays_between_its_limits_and_only_applies_to_an_expanded_column()
    {
        var sidebar = new KlySidebar { Resizable = true };
        var handle = Assert.Single(LayoutTree.Descendants<KlyDashboardResizeHandle>(sidebar));
        sidebar.UpdateAvailableWidth(1200);
        Assert.True(handle.IsVisible);

        sidebar.ResizeBy(1000);
        Assert.Equal(448, sidebar.SidebarWidth);

        sidebar.ResizeBy(-1000);
        Assert.Equal(192, sidebar.SidebarWidth);

        sidebar.Collapsed = true;
        Assert.False(handle.IsVisible);
        sidebar.ResizeBy(100);
        Assert.Equal(192, sidebar.SidebarWidth);
    }

    [Fact]
    public void Sidebar_handle_drag_resizes_the_column()
    {
        var sidebar = new KlySidebar { Resizable = true };
        sidebar.UpdateAvailableWidth(1200);
        var handle = Assert.Single(LayoutTree.Descendants<KlyDashboardResizeHandle>(sidebar));

        handle.Drag(30);

        Assert.Equal(286, sidebar.SidebarWidth);
    }

    [Fact]
    public void Sidebar_handle_is_hidden_unless_resizable()
    {
        var sidebar = new KlySidebar();
        sidebar.UpdateAvailableWidth(1200);
        var handle = Assert.Single(LayoutTree.Descendants<KlyDashboardResizeHandle>(sidebar));

        Assert.False(handle.IsVisible);
        sidebar.Resizable = true;
        Assert.True(handle.IsVisible);
        Assert.Equal("Resize sidebar", handle.Label);

        sidebar.ResizeLabel = "Redimensionner";
        Assert.Equal("Redimensionner", handle.Label);
    }

    [Fact]
    public void Sidebar_panel_opens_over_a_scrim_and_closes_from_the_scrim_the_button_or_a_wider_screen()
    {
        var sidebar = new KlySidebar();
        var scrim = (Border)sidebar.Children[0];
        var host = (Grid)sidebar.Children[1];
        var events = new List<bool>();
        sidebar.OpenChanged += (_, e) => events.Add(e.Value);
        sidebar.UpdateAvailableWidth(600);
        Assert.False(scrim.IsVisible);
        Assert.True(sidebar.InputTransparent);

        sidebar.IsOpen = true;
        Assert.True(scrim.IsVisible);
        Assert.True(host.IsVisible);
        Assert.False(sidebar.InputTransparent);
        Assert.False(sidebar.CascadeInputTransparent);
        Assert.Equal(10, sidebar.ZIndex);

        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(scrim.GestureRecognizers));
        tap.Command!.Execute(null);
        Assert.False(sidebar.IsOpen);

        sidebar.IsOpen = true;
        IconButtons(sidebar).Single(b => b.Label == "Close").SendClicked();
        Assert.False(sidebar.IsOpen);

        sidebar.IsOpen = true;
        sidebar.UpdateAvailableWidth(1300);
        Assert.False(sidebar.IsOpen);
        Assert.Equal([true, false, true, false, true, false], events);
    }

    [Fact]
    public void Sidebar_panel_width_is_at_most_320_and_85_percent_of_the_space()
    {
        var sidebar = new KlySidebar();
        var host = (Grid)sidebar.Children[1];

        sidebar.UpdateAvailableWidth(900);
        Assert.Equal(320, host.WidthRequest);

        sidebar.UpdateAvailableWidth(300);
        Assert.Equal(255, host.WidthRequest);
    }

    [Fact]
    public void Sidebar_menu_button_shows_over_a_closed_panel_only_when_asked_and_opens_it()
    {
        var sidebar = new KlySidebar();
        sidebar.UpdateAvailableWidth(600);
        var trigger = IconButtons(sidebar).Single(b => b.Label == "Open sidebar");

        Assert.True(trigger.IsVisible);

        trigger.SendClicked();
        Assert.True(sidebar.IsOpen);
        Assert.False(trigger.IsVisible);

        sidebar.IsOpen = false;
        sidebar.MobileTrigger = false;
        Assert.False(trigger.IsVisible);

        sidebar.MobileTrigger = true;
        sidebar.UpdateAvailableWidth(1200);
        Assert.False(trigger.IsVisible);
    }

    [Fact]
    public void Sidebar_labels_and_title_are_translatable()
    {
        var sidebar = new KlySidebar { MobileTitle = "Barre", OpenLabel = "Ouvrir", CloseLabel = "Fermer", CollapseLabel = "Replier" };

        var labels = IconButtons(sidebar).Select(b => b.Label).ToList();
        Assert.Contains("Ouvrir", labels);
        Assert.Contains("Fermer", labels);
        Assert.Contains("Replier", labels);
        var title = LayoutTree.Descendants<TextLabel>(sidebar).Single(l => l.Text == "Barre");
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(title));
    }

    [Fact]
    public void Sidebar_header_content_hides_while_collapsed_and_the_header_row_follows_its_content()
    {
        var logo = new Label { Text = "Logo" };
        var sidebar = new KlySidebar { Header = logo, Collapsible = false };
        sidebar.UpdateAvailableWidth(1200);
        var slot = (ContentView)logo.Parent;
        var header = (Grid)slot.Parent;

        Assert.True(slot.IsVisible);
        Assert.True(header.IsVisible);

        sidebar.Collapsed = true;
        Assert.False(slot.IsVisible);
        Assert.False(header.IsVisible);

        sidebar.Collapsible = true;
        Assert.True(header.IsVisible);
    }

    [Fact]
    public void Sidebar_hosts_its_content_and_shows_the_footer_only_while_it_has_one()
    {
        var footer = new Label();
        var body = new Label();
        var sidebar = new KlySidebar { ChildContent = body };

        Assert.Same(body, ((ContentView)body.Parent).Content);

        sidebar.Footer = footer;
        var slot = (ContentView)footer.Parent;
        Assert.True(slot.IsVisible);

        sidebar.Footer = null;
        Assert.False(slot.IsVisible);
    }

    private sealed class Probe : ContentView, ILayoutSidebarPart
    {
        public List<(bool Collapsed, bool Panel)> Seen { get; } = [];

        public void SidebarChanged(KlySidebar sidebar) => Seen.Add((sidebar.Collapsed, sidebar.IsPanel));
    }

    [Fact]
    public void Sidebar_tells_the_views_inside_when_it_collapses_or_changes_form()
    {
        var probe = new Probe();
        var sidebar = new KlySidebar { ChildContent = probe };
        sidebar.UpdateAvailableWidth(1200);
        probe.Seen.Clear();

        sidebar.Collapsed = true;
        sidebar.UpdateAvailableWidth(500);

        Assert.Equal([(true, false), (true, true)], probe.Seen);
    }

    [Fact]
    public void Sidebar_follows_the_width_a_dashboard_gives_it_and_not_the_page()
    {
        var sidebar = new KlySidebar();
        sidebar.UpdateAvailableWidth(500, external: true);

        sidebar.UpdateAvailableWidth(1500);

        Assert.True(sidebar.IsPanel);
    }

    // KlyDashboardSidebar

    [Fact]
    public void Dashboard_sidebar_has_neither_a_menu_button_nor_a_collapse_button_by_default()
    {
        var sidebar = new KlyDashboardSidebar();

        Assert.False(sidebar.MobileTrigger);
        Assert.False(sidebar.Collapsible);
        Assert.IsAssignableFrom<KlySidebar>(sidebar);
    }

    // Dashboard group

    private static (KlyDashboardGroup Group, KlyDashboardSidebar Sidebar, KlyDashboardPanel List, KlyDashboardPanel Detail) Dashboard(double? listSize = null, double? detailSize = null)
    {
        var sidebar = new KlyDashboardSidebar { SidebarId = "main" };
        var list = new KlyDashboardPanel { Size = listSize };
        var detail = new KlyDashboardPanel { Size = detailSize };
        var group = new KlyDashboardGroup { Children = { sidebar, list, detail } };
        return (group, sidebar, list, detail);
    }

    private static double[] Stars(KlyDashboardGroup group) =>
        group.ColumnDefinitions.Where(c => c.Width.IsStar).Select(c => c.Width.Value).ToArray();

    [Fact]
    public void Dashboard_group_starts_narrow_and_is_wide_from_1024()
    {
        var (group, _, _, _) = Dashboard();
        Assert.True(group.IsNarrow);

        group.UpdateWidth(1024);
        Assert.False(group.IsNarrow);

        group.UpdateWidth(1023);
        Assert.True(group.IsNarrow);
    }

    [Fact]
    public void Dashboard_group_puts_the_sidebar_in_its_own_column_when_wide_and_over_the_panels_when_narrow()
    {
        var (group, sidebar, list, detail) = Dashboard();

        group.UpdateWidth(1200);
        Assert.False(sidebar.IsPanel);
        Assert.Equal(0, Grid.GetColumn((BindableObject)sidebar));
        Assert.Equal(1, Grid.GetColumn((BindableObject)list));
        Assert.Equal(2, Grid.GetColumn((BindableObject)detail));
        Assert.True(group.ColumnDefinitions[0].Width.IsAuto);

        group.UpdateWidth(600);
        Assert.True(sidebar.IsPanel);
        Assert.Equal(0, Grid.GetColumn((BindableObject)sidebar));
        Assert.Equal(2, Grid.GetColumnSpan((BindableObject)sidebar));
        Assert.Equal(2, group.ColumnDefinitions.Count);
        Assert.Equal(0, Grid.GetColumn((BindableObject)list));
        Assert.Equal(1, Grid.GetColumn((BindableObject)detail));
    }

    [Fact]
    public void Dashboard_group_shares_the_width_equally_then_by_the_sizes_of_the_panels()
    {
        var (group, _, _, _) = Dashboard();
        group.UpdateWidth(1200);
        Assert.Equal([50d, 50d], Stars(group));

        var (sized, _, _, _) = Dashboard(listSize: 30);
        sized.UpdateWidth(1200);
        Assert.Equal([30d, 70d], Stars(sized));

        var (both, _, _, _) = Dashboard(listSize: 25, detailSize: 25);
        both.UpdateWidth(1200);
        Assert.Equal([25d, 25d], Stars(both));
    }

    [Fact]
    public void Dashboard_group_changes_a_size_when_a_panel_gets_one()
    {
        var (group, _, list, _) = Dashboard();
        group.UpdateWidth(1200);

        list.Size = 20;

        Assert.Equal([20d, 80d], Stars(group));
    }

    [Fact]
    public void Dashboard_group_draws_a_border_on_every_panel_but_the_last()
    {
        var (group, _, list, detail) = Dashboard();
        group.UpdateWidth(1200);

        Assert.Contains(list.Children.OfType<Border>(), b => b.IsVisible && b.WidthRequest == 1);
        Assert.DoesNotContain(detail.Children.OfType<Border>(), b => b.IsVisible && b.WidthRequest == 1);
    }

    [Fact]
    public void Dashboard_group_resize_moves_the_edge_and_raises_one_event_with_the_new_sizes()
    {
        var (group, _, _, _) = Dashboard(listSize: 40, detailSize: 60);
        group.UpdateWidth(1200);
        var events = new List<IReadOnlyList<double>>();
        group.SizesChanged += (_, sizes) => events.Add(sizes);

        group.ResizeBy(0, 10);

        Assert.Equal([50d, 50d], Stars(group));
        Assert.Equal([50d, 50d], group.Sizes);
        var raised = Assert.Single(events);
        Assert.Equal([50d, 50d], raised);
    }

    [Fact]
    public void Dashboard_group_resize_respects_the_limits_of_both_panels()
    {
        var (group, _, list, detail) = Dashboard(listSize: 40, detailSize: 60);
        list.MaxSize = 55;
        detail.MinSize = 30;
        group.UpdateWidth(1200);

        group.ResizeBy(0, 50);
        Assert.Equal([55d, 45d], group.Sizes);

        list.MinSize = 10;
        group.ResizeBy(0, -80);
        Assert.Equal([10d, 90d], group.Sizes);
    }

    [Fact]
    public void Dashboard_group_resize_ignores_an_edge_that_does_not_exist_and_raises_nothing_when_nothing_moves()
    {
        var (group, _, list, _) = Dashboard(listSize: 40, detailSize: 60);
        group.UpdateWidth(1200);
        var events = 0;
        group.SizesChanged += (_, _) => events++;

        group.ResizeBy(1, 10);
        group.ResizeBy(-1, 10);
        list.MaxSize = 40;
        group.ResizeBy(0, 10);

        Assert.Equal(0, events);
    }

    [Fact]
    public void Dashboard_group_sizes_are_two_way_bindable()
    {
        var vm = new Vm { Sizes = new[] { 70d, 30d } };
        var (group, _, _, _) = Dashboard();
        group.BindingContext = vm;
        group.SetBinding(KlyDashboardGroup.SizesProperty, new Binding(nameof(Vm.Sizes), BindingMode.TwoWay));
        group.UpdateWidth(1200);
        Assert.Equal([70d, 30d], Stars(group));

        vm.Sizes = new[] { 20d, 80d };
        Assert.Equal([20d, 80d], Stars(group));

        group.ResizeBy(0, 10);
        Assert.Equal([30d, 70d], vm.Sizes);
    }

    [Fact]
    public void Dashboard_group_ignores_bound_sizes_that_do_not_fit_its_panels()
    {
        var (group, _, _, _) = Dashboard(listSize: 10);
        group.Sizes = new[] { 100d };
        group.UpdateWidth(1200);

        Assert.Equal([10d, 90d], Stars(group));
    }

    [Fact]
    public void Dashboard_group_resizes_from_the_handle_of_a_panel_by_the_share_of_the_width_it_moved()
    {
        var (group, _, list, _) = Dashboard(listSize: 50, detailSize: 50);
        list.Resizable = true;
        group.UpdateWidth(1200);
        // The panels have no rendered width in a test: the dashboard falls back on its own.
        var handle = Assert.Single(LayoutTree.Descendants<KlyDashboardResizeHandle>(list));
        Assert.True(handle.IsVisible);

        // 60 units of a 1200 wide dashboard are 5 percent.
        handle.Drag(60);

        Assert.Equal([55d, 45d], group.Sizes);
        Assert.Equal(55, handle.Value);
    }

    [Fact]
    public void Dashboard_group_panel_handle_is_hidden_when_narrow()
    {
        var (group, _, list, _) = Dashboard();
        list.Resizable = true;
        var handle = Assert.Single(LayoutTree.Descendants<KlyDashboardResizeHandle>(list));

        group.UpdateWidth(1200);
        Assert.True(handle.IsVisible);

        group.UpdateWidth(500);
        Assert.False(handle.IsVisible);
    }

    [Fact]
    public void Dashboard_group_places_a_handle_between_panels_in_a_column_of_no_width()
    {
        var left = new KlyDashboardPanel();
        var handle = new KlyDashboardResizeHandle();
        var right = new KlyDashboardPanel();
        var group = new KlyDashboardGroup { Children = { left, handle, right } };

        group.UpdateWidth(1200);

        Assert.Equal(3, group.ColumnDefinitions.Count);
        Assert.Equal(0, group.ColumnDefinitions[1].Width.Value);
        Assert.Equal(1, Grid.GetColumn((BindableObject)handle));
        Assert.True(handle.IsVisible);

        group.UpdateWidth(500);
        Assert.False(handle.IsVisible);
    }

    [Fact]
    public void Dashboard_group_resizes_from_a_handle_between_panels()
    {
        var left = new KlyDashboardPanel { Size = 50 };
        var handle = new KlyDashboardResizeHandle();
        var right = new KlyDashboardPanel { Size = 50 };
        var group = new KlyDashboardGroup { Children = { left, handle, right } };
        group.UpdateWidth(1000);

        // Nothing is rendered, so the dashboard measures a drag against its own width.
        handle.Drag(100);

        Assert.Equal([60d, 40d], group.Sizes);
    }

    [Fact]
    public void Dashboard_group_stops_listening_to_a_panel_that_leaves()
    {
        var (group, _, list, _) = Dashboard(listSize: 30);
        group.UpdateWidth(1200);
        group.Children.Remove(list);

        list.Size = 80;

        Assert.Equal([100d], Stars(group));
    }

    [Fact]
    public void Dashboard_group_finds_a_sidebar_by_its_name_or_takes_the_first()
    {
        var other = new KlyDashboardSidebar { SidebarId = "other" };
        var (group, sidebar, _, _) = Dashboard();
        group.Children.Add(other);

        Assert.Same(sidebar, group.FindSidebar(null));
        Assert.Same(sidebar, group.FindSidebar("main"));
        Assert.Same(other, group.FindSidebar("other"));
        Assert.Null(group.FindSidebar("missing"));
    }

    // KlyDashboardSidebarToggle and KlyDashboardSidebarCollapse

    [Fact]
    public void Sidebar_toggle_shows_below_1024_wide_and_opens_the_panel_of_the_sidebar()
    {
        var toggle = new KlyDashboardSidebarToggle();
        var navbar = new KlyDashboardNavbar { Toggle = false, Left = toggle };
        var (group, sidebar, list, _) = Dashboard();
        list.Header = navbar;
        var clicked = 0;
        toggle.Clicked += (_, _) => clicked++;

        group.UpdateWidth(500);
        Assert.True(toggle.IsVisible);

        toggle.Activate();
        Assert.True(sidebar.IsOpen);
        Assert.Equal(1, clicked);

        group.UpdateWidth(1200);
        Assert.False(toggle.IsVisible);
        Assert.False(sidebar.IsOpen);
    }

    [Fact]
    public void Sidebar_collapse_shows_from_1024_wide_and_collapses_and_expands_the_sidebar()
    {
        var collapse = new KlyDashboardSidebarCollapse();
        var (group, sidebar, list, _) = Dashboard();
        list.Header = new KlyDashboardNavbar { Left = collapse };
        var clicked = 0;
        collapse.Clicked += (_, _) => clicked++;

        group.UpdateWidth(1200);
        Assert.True(collapse.IsVisible);

        collapse.Activate();
        Assert.True(sidebar.Collapsed);

        collapse.Activate();
        Assert.False(sidebar.Collapsed);
        Assert.Equal(2, clicked);

        group.UpdateWidth(500);
        Assert.False(collapse.IsVisible);
    }

    [Fact]
    public void Sidebar_toggle_and_collapse_target_the_sidebar_named_by_for()
    {
        var other = new KlyDashboardSidebar { SidebarId = "other" };
        var toggle = new KlyDashboardSidebarToggle { For = "other" };
        var collapse = new KlyDashboardSidebarCollapse { For = "other" };
        var (group, sidebar, list, _) = Dashboard();
        group.Children.Add(other);
        list.Body = new VerticalStackLayout { Children = { toggle, collapse } };
        group.UpdateWidth(500);

        toggle.Activate();
        collapse.Activate();

        Assert.True(other.IsOpen);
        Assert.True(other.Collapsed);
        Assert.False(sidebar.IsOpen);
        Assert.False(sidebar.Collapsed);
    }

    [Fact]
    public void Sidebar_toggle_and_collapse_do_nothing_but_raise_their_event_outside_a_dashboard()
    {
        var toggle = new KlyDashboardSidebarToggle();
        var collapse = new KlyDashboardSidebarCollapse();
        var events = 0;
        toggle.Clicked += (_, _) => events++;
        collapse.Clicked += (_, _) => events++;

        toggle.Activate();
        collapse.Activate();

        Assert.Equal(2, events);
        Assert.True(toggle.IsVisible);
        Assert.True(collapse.IsVisible);
    }

    [Fact]
    public void Sidebar_toggle_and_collapse_forward_their_look_and_name_to_the_button()
    {
        var toggle = new KlyDashboardSidebarToggle { Label = "Menu", Variant = ButtonVariant.Soft, Color = KlyColor.Primary, Size = KlySize.Lg };
        var collapse = new KlyDashboardSidebarCollapse { Label = "Replier", Variant = ButtonVariant.Outline, Color = KlyColor.Secondary, Size = KlySize.Sm };

        var a = (LayoutIconButton)toggle.Content!;
        var b = (LayoutIconButton)collapse.Content!;
        Assert.Equal(("Menu", ButtonVariant.Soft, KlyColor.Primary, KlySize.Lg), (a.Label, a.Variant, a.Color, a.Size));
        Assert.Equal(("Replier", ButtonVariant.Outline, KlyColor.Secondary, KlySize.Sm), (b.Label, b.Variant, b.Color, b.Size));
        Assert.Equal(44, a.WidthRequest);
        Assert.Equal(32, b.HeightRequest);
    }

    [Fact]
    public void Sidebar_toggle_and_collapse_default_to_a_ghost_neutral_forty_unit_button_with_a_name()
    {
        var toggle = new KlyDashboardSidebarToggle();
        var collapse = new KlyDashboardSidebarCollapse();

        Assert.Equal((ButtonVariant.Ghost, KlyColor.Neutral, KlySize.Md, "Open sidebar"), (toggle.Variant, toggle.Color, toggle.Size, toggle.Label));
        Assert.Equal((ButtonVariant.Ghost, KlyColor.Neutral, KlySize.Md, "Collapse sidebar"), (collapse.Variant, collapse.Color, collapse.Size, collapse.Label));
        var button = (LayoutIconButton)toggle.Content!;
        Assert.Equal(40, button.WidthRequest);
        Assert.Equal(KlyIconNames.Menu, button.IconName);
        Assert.Equal(KlyIconNames.PanelClose, ((LayoutIconButton)collapse.Content!).IconName);
    }

    [Fact]
    public void Disabled_toggle_disables_and_dims_its_button()
    {
        var toggle = new KlyDashboardSidebarToggle { IsEnabled = false };
        var button = (LayoutIconButton)toggle.Content!;

        Assert.False(button.IsEnabled);
        Assert.Equal(0.6, button.Opacity);
    }

    [Theory]
    [InlineData(KlySize.Xs, 28)]
    [InlineData(KlySize.Sm, 32)]
    [InlineData(KlySize.Md, 40)]
    [InlineData(KlySize.Lg, 44)]
    [InlineData(KlySize.Xl, 48)]
    public void Icon_button_side_follows_the_size_of_the_buttons_of_the_library(KlySize size, double side) =>
        Assert.Equal(side, LayoutIconButton.SideOf(size));

    // KlyDashboardPanel

    [Fact]
    public void Panel_defaults()
    {
        var panel = new KlyDashboardPanel();

        Assert.Null(panel.Size);
        Assert.Equal(0, panel.MinSize);
        Assert.Equal(100, panel.MaxSize);
        Assert.False(panel.Resizable);
        Assert.Equal("Resize panels", panel.ResizeLabel);
    }

    [Fact]
    public void Panel_hosts_header_body_and_footer_and_child_content_replaces_them()
    {
        var header = new Label();
        var body = new Label();
        var footer = new Label();
        var panel = new KlyDashboardPanel { Header = header, Body = body, Footer = footer };

        Assert.True(((ContentView)header.Parent).IsVisible);
        Assert.Same(body, ((ContentView)body.Parent).Content);
        Assert.True(((ContentView)footer.Parent).IsVisible);
        var areas = (Grid)((ContentView)header.Parent).Parent;
        Assert.True(areas.IsVisible);

        var whole = new Label();
        panel.ChildContent = whole;
        Assert.False(areas.IsVisible);
        Assert.True(((ContentView)whole.Parent).IsVisible);

        panel.ChildContent = null;
        Assert.True(areas.IsVisible);
    }

    [Fact]
    public void Panel_body_scrolls_vertically()
    {
        var panel = new KlyDashboardPanel { Body = new Label() };

        var scroll = LayoutTree.Descendants<ScrollView>(panel).Single();
        Assert.Equal(ScrollOrientation.Vertical, scroll.Orientation);
    }

    [Fact]
    public void Panel_handle_label_is_translatable_and_the_handle_shows_the_size_to_a_screen_reader()
    {
        var panel = new KlyDashboardPanel { Resizable = true, ResizeLabel = "Redimensionner" };
        var handle = Assert.Single(LayoutTree.Descendants<KlyDashboardResizeHandle>(panel));

        panel.ShowSize(37.4);

        Assert.Equal("Redimensionner", handle.Label);
        Assert.Equal("Redimensionner", SemanticProperties.GetDescription(handle));
        Assert.Equal("37%", SemanticProperties.GetHint(handle));
    }

    // KlyDashboardResizeHandle

    [Fact]
    public void Resize_handle_defaults_and_describes_itself()
    {
        var handle = new KlyDashboardResizeHandle();

        Assert.Equal(50, handle.Value);
        Assert.Equal("Resize panels", handle.Label);
        Assert.Equal("Resize panels", SemanticProperties.GetDescription(handle));
        Assert.Equal("50%", SemanticProperties.GetHint(handle));
        Assert.True(handle.WidthRequest >= 20);
        Assert.False(handle.IsDragging);
    }

    [Fact]
    public void Resize_handle_raises_the_distance_of_each_drag_step()
    {
        var handle = new KlyDashboardResizeHandle();
        var steps = new List<double>();
        handle.Dragged += (_, d) => steps.Add(d);

        handle.Drag(5);
        handle.Drag(-2);

        Assert.Equal([5d, -2d], steps);
    }

    [Fact]
    public void Resize_handle_thickens_its_line_while_dragged()
    {
        var handle = new KlyDashboardResizeHandle();
        var line = (Border)handle.Content!;
        Assert.Equal(2, line.WidthRequest);

        handle.SetDragging(true);
        Assert.True(handle.IsDragging);
        Assert.Equal(4, line.WidthRequest);

        handle.SetDragging(false);
        Assert.Equal(2, line.WidthRequest);
    }

    [Fact]
    public void Resize_handle_pan_starts_and_ends_a_drag_and_reports_the_distance_between_updates()
    {
        var handle = new KlyDashboardResizeHandle();
        var steps = new List<double>();
        handle.Dragged += (_, d) => steps.Add(d);
        Assert.IsType<PanGestureRecognizer>(Assert.Single(handle.GestureRecognizers));

        handle.HandlePan(GestureStatus.Started, 0);
        Assert.True(handle.IsDragging);
        handle.HandlePan(GestureStatus.Running, 10);
        handle.HandlePan(GestureStatus.Running, 25);
        handle.HandlePan(GestureStatus.Running, 25);
        handle.HandlePan(GestureStatus.Completed, 25);

        Assert.False(handle.IsDragging);
        Assert.Equal([10d, 15d], steps);
    }

    // KlyDashboardNavbar and KlyDashboardToolbar

    [Fact]
    public void Navbar_shows_the_title_as_a_heading_of_its_level()
    {
        var navbar = new KlyDashboardNavbar { Title = "Inbox" };
        var title = LayoutTree.Descendants<TextLabel>(navbar).Single();

        Assert.Equal("Inbox", title.Text);
        Assert.True(title.IsVisible);
        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(title));

        navbar.HeadingLevel = 2;
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(title));

        navbar.Title = null;
        Assert.False(title.IsVisible);
    }

    [Fact]
    public void Navbar_hosts_its_areas_and_shows_a_sidebar_toggle_unless_turned_off()
    {
        var leading = new Label();
        var trailing = new Label();
        var left = new Label();
        var center = new Label();
        var right = new Label();
        var navbar = new KlyDashboardNavbar { Leading = leading, Trailing = trailing, Left = left, ChildContent = center, Right = right };

        Assert.All(new[] { leading, trailing, left, center, right }, v => Assert.True(((ContentView)v.Parent).IsVisible));
        Assert.Single(LayoutTree.Descendants<KlyDashboardSidebarToggle>(navbar));

        navbar.Toggle = false;
        Assert.Empty(LayoutTree.Descendants<KlyDashboardSidebarToggle>(navbar));

        navbar.Toggle = true;
        Assert.Single(LayoutTree.Descendants<KlyDashboardSidebarToggle>(navbar));
    }

    [Fact]
    public void Navbar_is_sixteen_rem_high_and_pads_more_from_640_wide()
    {
        var navbar = new KlyDashboardNavbar();
        var row = (Grid)navbar.Children[0];

        Assert.Equal(64, row.HeightRequest);
        Assert.Equal(new Thickness(16, 0), row.Padding);

        navbar.UpdateWidth(700);
        Assert.Equal(new Thickness(24, 0), row.Padding);
    }

    [Fact]
    public void Toolbar_hosts_its_areas_and_is_forty_eight_high()
    {
        var left = new Label();
        var center = new Label();
        var toolbar = new KlyDashboardToolbar { Left = left, ChildContent = center };
        var row = (Grid)toolbar.Children[0];

        Assert.Equal(48, row.HeightRequest);
        Assert.True(((ContentView)left.Parent).IsVisible);
        Assert.True(((ContentView)center.Parent).IsVisible);
        Assert.False(((ContentView)toolbar.Children.OfType<Grid>().First().Children.OfType<ContentView>().Last()).IsVisible);

        toolbar.Right = new Label();
        Assert.True(((ContentView)toolbar.Children.OfType<Grid>().First().Children.OfType<ContentView>().Last()).IsVisible);

        toolbar.UpdateWidth(800);
        Assert.Equal(new Thickness(24, 0), row.Padding);
    }

    // KlyDashboardSearch

    private static IReadOnlyList<KlyDashboardSearchGroup> Commands() =>
    [
        new KlyDashboardSearchGroup("Pages",
        [
            new KlyDashboardSearchItem { Label = "Inbox", Icon = KlyIconNames.Search, Count = 12 },
            new KlyDashboardSearchItem { Label = "Settings", Description = "Account and billing", Keywords = "preferences" },
        ]),
        new KlyDashboardSearchGroup(null,
        [
            new KlyDashboardSearchItem { Label = "Sign out", Disabled = true, Shortcut = "Ctrl+Q" },
        ]),
    ];

    [Fact]
    public void Search_defaults()
    {
        var search = new KlyDashboardSearch();

        Assert.False(search.Open);
        Assert.Equal("", search.Text);
        Assert.Equal("Type a command or search...", search.Placeholder);
        Assert.Equal("Search", search.Label);
        Assert.Equal("No results", search.EmptyText);
        Assert.Null(search.Groups);
    }

    [Fact]
    public void Search_forwards_its_placeholder_and_accessible_name_to_the_search_bar()
    {
        var search = new KlyDashboardSearch { Placeholder = "Find...", Label = "Find a page" };
        var bar = LayoutTree.Descendants<SearchBar>(search).Single();

        Assert.Equal("Find...", bar.Placeholder);
        Assert.Equal("Find a page", SemanticProperties.GetDescription(bar));
    }

    [Theory]
    [InlineData("", 3)]
    [InlineData("inb", 1)]
    [InlineData("INBOX", 1)]
    [InlineData("account billing", 1)]
    [InlineData("preferences", 1)]
    [InlineData("pages", 2)]
    [InlineData("pages inbox", 1)]
    [InlineData("nothing", 0)]
    public void Search_matches_every_word_against_label_description_keywords_and_group(string text, int expected)
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Text = text };

        Assert.Equal(expected, search.Matches().Count);
    }

    [Fact]
    public void Search_text_goes_both_ways()
    {
        var vm = new Vm();
        var search = new KlyDashboardSearch { BindingContext = vm };
        search.SetBinding(KlyDashboardSearch.TextProperty, new Binding(nameof(Vm.Text), BindingMode.TwoWay));
        var bar = LayoutTree.Descendants<SearchBar>(search).Single();

        vm.Text = "inbox";
        Assert.Equal("inbox", search.Text);
        Assert.Equal("inbox", bar.Text);

        bar.Text = "settings";
        Assert.Equal("settings", search.Text);
        Assert.Equal("settings", vm.Text);
    }

    [Fact]
    public void Search_shows_its_results_only_while_open_and_raises_one_event_per_change()
    {
        var search = new KlyDashboardSearch { Groups = Commands() };
        var list = LayoutTree.Descendants<ScrollView>(search).Single();
        var close = IconButtons(search).Single();
        var events = new List<bool>();
        search.OpenChanged += (_, e) => events.Add(e.Value);

        Assert.False(list.IsVisible);
        Assert.False(close.IsVisible);

        search.Open = true;
        Assert.True(list.IsVisible);
        Assert.True(close.IsVisible);
        Assert.Equal(320, list.MaximumHeightRequest);

        close.SendClicked();
        Assert.False(search.Open);
        Assert.False(list.IsVisible);
        Assert.Equal([true, false], events);
    }

    [Fact]
    public void Search_lists_the_group_headings_and_the_commands_that_match()
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Open = true };

        var texts = LayoutTree.Descendants<TextLabel>(search).Select(l => l.Text).ToList();
        Assert.Contains("Pages", texts);
        Assert.Contains("Inbox", texts);
        Assert.Contains("Settings", texts);
        Assert.Contains("Account and billing", texts);
        Assert.Contains("12", texts);
        Assert.Contains("Ctrl+Q", texts);

        search.Text = "inbox";
        texts = LayoutTree.Descendants<TextLabel>(search).Select(l => l.Text).ToList();
        Assert.Contains("Inbox", texts);
        Assert.DoesNotContain("Settings", texts);
    }

    [Fact]
    public void Search_says_so_when_nothing_matches_and_the_text_can_be_translated()
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Open = true, Text = "zzz", EmptyText = "Aucun resultat" };

        var texts = LayoutTree.Descendants<TextLabel>(search).Select(l => l.Text).ToList();

        Assert.Contains("Aucun resultat", texts);
        Assert.DoesNotContain("Inbox", texts);
    }

    [Fact]
    public void Search_selection_raises_one_event_then_closes_and_clears()
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Open = true, Text = "inb" };
        var selected = new List<KlyDashboardSearchItem>();
        search.ItemSelected += (_, item) => selected.Add(item);

        search.Select(Commands()[0].Items[0]);

        Assert.Equal(["Inbox"], selected.Select(i => i.Label));
        Assert.False(search.Open);
        Assert.Equal("", search.Text);
    }

    [Fact]
    public void Search_does_not_select_a_disabled_command()
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Open = true };
        var selected = 0;
        search.ItemSelected += (_, _) => selected++;

        search.Select(Commands()[1].Items[0]);

        Assert.Equal(0, selected);
        Assert.True(search.Open);
    }

    [Fact]
    public void Search_disabled_row_is_dimmed_and_cannot_be_tapped()
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Open = true };

        var rows = LayoutTree.Descendants<Grid>(search).Where(g => g.MinimumHeightRequest == 40).ToList();
        Assert.Equal(3, rows.Count);
        var disabled = rows.Single(r => !r.IsEnabled);
        Assert.Equal(0.6, disabled.Opacity);
        Assert.Empty(disabled.GestureRecognizers);
        Assert.All(rows.Where(r => r.IsEnabled), r => Assert.Single(r.GestureRecognizers));
    }

    [Fact]
    public void Search_row_tap_selects_the_command()
    {
        var search = new KlyDashboardSearch { Groups = Commands(), Open = true };
        var selected = new List<string>();
        search.ItemSelected += (_, item) => selected.Add(item.Label);

        var row = LayoutTree.Descendants<Grid>(search).First(g => g.MinimumHeightRequest == 40 && g.IsEnabled);
        ((TapGestureRecognizer)row.GestureRecognizers[0]).Command!.Execute(null);

        Assert.Equal(["Inbox"], selected);
    }

    [Fact]
    public void Search_close_button_has_a_name_that_can_be_translated()
    {
        var search = new KlyDashboardSearch { CloseLabel = "Fermer" };

        Assert.Equal("Fermer", IconButtons(search).Single().Label);
    }

    // KlyDashboardSearchButton

    [Fact]
    public void Search_button_defaults_and_forwards_its_label_to_the_text_and_the_screen_reader()
    {
        var button = new KlyDashboardSearchButton();
        var text = LayoutTree.Descendants<TextLabel>(button).Single();
        var native = LayoutTree.Descendants<Button>(button).Single();

        Assert.Equal("Search", button.Label);
        Assert.Equal("Search", text.Text);
        Assert.Equal("Search", SemanticProperties.GetDescription(native));
        Assert.Equal(KlyIconNames.Search, LayoutTree.Descendants<KlyIcon>(button).Single().Name);
        Assert.True(button.MinimumHeightRequest >= 40);

        button.Label = "Rechercher";
        Assert.Equal("Rechercher", text.Text);
        Assert.Equal("Rechercher", SemanticProperties.GetDescription(native));
    }

    [Fact]
    public void Search_button_keeps_only_its_icon_in_a_collapsed_sidebar_and_still_names_itself()
    {
        var button = new KlyDashboardSearchButton();
        var sidebar = new KlySidebar { ChildContent = button };
        sidebar.UpdateAvailableWidth(1200);
        var text = LayoutTree.Descendants<TextLabel>(button).Single();
        Assert.False(button.IsIconOnly);
        Assert.True(text.IsVisible);

        sidebar.Collapsed = true;
        Assert.True(button.IsIconOnly);
        Assert.False(text.IsVisible);
        Assert.Equal("Search", SemanticProperties.GetDescription(LayoutTree.Descendants<Button>(button).Single()));

        sidebar.Collapsed = false;
        Assert.False(button.IsIconOnly);
        Assert.True(text.IsVisible);
    }

    [Fact]
    public void Search_button_opens_the_search_of_its_dashboard_and_raises_one_event()
    {
        var button = new KlyDashboardSearchButton();
        var search = new KlyDashboardSearch();
        var (group, _, list, detail) = Dashboard();
        list.Header = button;
        detail.Header = search;
        group.UpdateWidth(1200);
        var clicked = 0;
        button.Clicked += (_, _) => clicked++;

        button.Activate();

        Assert.True(search.Open);
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Search_button_outside_a_dashboard_only_raises_its_event()
    {
        var button = new KlyDashboardSearchButton();
        var clicked = 0;
        button.Clicked += (_, _) => clicked++;

        button.Activate();

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Disabled_search_button_dims()
    {
        var button = new KlyDashboardSearchButton { IsEnabled = false };

        Assert.Equal(0.6, button.Opacity);
        button.IsEnabled = true;
        Assert.Equal(1, button.Opacity);
    }

    // KlyError

    [Theory]
    [InlineData(404, "Page not found", "The page you are looking for does not exist or has moved.")]
    [InlineData(401, "Sign in required", "You need to sign in to view this page.")]
    [InlineData(403, "Access denied", "You do not have permission to view this page.")]
    [InlineData(500, "Something went wrong", "An unexpected error occurred. Please try again later.")]
    [InlineData(418, "Something went wrong", "An unexpected error occurred. Please try again later.")]
    public void Error_has_a_built_in_text_for_each_status(int code, string title, string message)
    {
        var error = new KlyError { StatusCode = code };
        var texts = LayoutTree.Descendants<TextLabel>(error).Select(l => l.Text).ToList();

        Assert.Equal([code.ToString(), title, message], texts);
    }

    [Fact]
    public void Error_without_a_status_shows_no_code_and_the_generic_text()
    {
        var error = new KlyError();
        var texts = LayoutTree.Descendants<TextLabel>(error).Select(l => l.Text).ToList();

        Assert.Equal(["Something went wrong", "An unexpected error occurred. Please try again later."], texts);
    }

    [Fact]
    public void Error_status_message_and_message_override_the_texts()
    {
        var error = new KlyError { StatusCode = 404, StatusMessage = "Introuvable", Message = "Cette page n'existe pas." };
        var texts = LayoutTree.Descendants<TextLabel>(error).Select(l => l.Text).ToList();

        Assert.Equal(["404", "Introuvable", "Cette page n'existe pas."], texts);
    }

    [Fact]
    public void Error_with_an_exception_is_a_500_and_shows_its_message_only_when_asked()
    {
        var error = new KlyError { Exception = new InvalidOperationException("secret detail") };
        var texts = LayoutTree.Descendants<TextLabel>(error).Select(l => l.Text).ToList();
        Assert.Equal("500", texts[0]);
        Assert.DoesNotContain("secret detail", texts);

        error.ShowDetails = true;
        texts = LayoutTree.Descendants<TextLabel>(error).Select(l => l.Text).ToList();
        Assert.Contains("secret detail", texts);
    }

    [Fact]
    public void Error_title_is_a_heading_of_its_level()
    {
        var error = new KlyError { StatusCode = 404 };
        var title = LayoutTree.Descendants<TextLabel>(error).Single(l => l.Text == "Page not found");

        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(title));

        error.HeadingLevel = 2;
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(title));
    }

    [Fact]
    public void Error_back_button_raises_one_event_and_can_be_hidden_or_renamed()
    {
        var error = new KlyError { StatusCode = 404 };
        var clicks = 0;
        error.ClearClicked += (_, _) => clicks++;
        var button = LayoutTree.Descendants<KlyButton>(error).Single();

        Assert.Equal("Back to home", button.Text);
        button.SendClicked();
        Assert.Equal(1, clicks);

        error.ClearLabel = "Retour";
        Assert.Equal("Retour", LayoutTree.Descendants<KlyButton>(error).Single().Text);

        error.Clear = false;
        Assert.Empty(LayoutTree.Descendants<KlyButton>(error));
    }

    [Fact]
    public void Error_actions_replace_the_back_button_and_content_sits_between_the_message_and_the_actions()
    {
        var content = new Label { Text = "Extra" };
        var actions = new Label { Text = "Actions" };
        var error = new KlyError { StatusCode = 404, ChildContent = content, Actions = actions };

        Assert.Empty(LayoutTree.Descendants<KlyButton>(error));
        var stack = (VerticalStackLayout)error.Content!;
        Assert.Equal(["404", "Page not found", "The page you are looking for does not exist or has moved.", "Extra", "Actions"],
            stack.Children.OfType<TextLabel>().Select(l => l.Text));

        error.Actions = null;
        Assert.Single(LayoutTree.Descendants<KlyButton>(error));
        Assert.Same(content, ((VerticalStackLayout)error.Content!).Children.OfType<TextLabel>().Last());
    }

    // KlyThemeStyle

    [Fact]
    public void Theme_style_registers_its_colors_when_applied()
    {
        var original = KlyTheme.Resolve(KlyColor.Primary);
        var set = new KlyColorSet(KlyThemeColor.FromHex("#112233", "#445566"), KlyThemeColor.FromHex("#111111", "#222222"), KlyThemeColor.FromHex("#ffffff", "#000000"));
        var style = new KlyThemeStyle { Colors = { ["layout-brand"] = set } };
        try
        {
            Assert.Throws<InvalidOperationException>(() => KlyTheme.Resolve("layout-brand"));

            var result = style.Apply();

            Assert.Same(style, result);
            Assert.Equal(set, KlyTheme.Resolve("layout-brand"));

            style.Colors[KlyColor.Primary] = set;
            style.Apply();
            Assert.Equal(set, KlyTheme.Resolve(KlyColor.Primary));
        }
        finally
        {
            KlyTheme.Register(KlyColor.Primary, original);
        }
    }

    [Fact]
    public void Theme_style_without_colors_changes_nothing()
    {
        var before = KlyTheme.Resolve(KlyColor.Success);

        new KlyThemeStyle().Apply();

        Assert.Equal(before, KlyTheme.Resolve(KlyColor.Success));
    }
}
