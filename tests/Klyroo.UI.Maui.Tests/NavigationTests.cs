using System.Globalization;
using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui.Tests;

// The navigation controls run with no handler: the tests read the state and the styling properties, and press the tap targets in code.
// The color mode and the culture are process-wide, so everything that touches them lives in this one class, which xunit runs in sequence,
// and every test starts from a clean state and puts the culture back.
public class NavigationTests : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    public NavigationTests()
    {
        KlyColorMode.Reset();
        KlyCulture.ForgetStored();
    }

    public void Dispose()
    {
        KlyColorMode.Reset();
        KlyCulture.ForgetStored();
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        Application.Current = null;
    }

    private static List<T> All<T>(Element root) where T : Element
    {
        var found = new List<T>();
        void Walk(IVisualTreeElement element)
        {
            if (element is T match) found.Add(match);
            foreach (var child in element.GetVisualChildren()) Walk(child);
        }
        Walk(root);
        return found;
    }

    private static string? DescriptionOf(BindableObject view) => SemanticProperties.GetDescription(view);

    // ----- KlyTabs -----

    private static KlyTabs ThreeTabs(out View[] panels)
    {
        panels = [new Label { Text = "one" }, new Label { Text = "two" }, new Label { Text = "three" }];
        return new KlyTabs
        {
            Items = [new KlyTabItem("One") { Content = panels[0] }, new KlyTabItem("Two") { Value = "two", Content = panels[1] }, new KlyTabItem("Three") { Disabled = true, Content = panels[2] }],
        };
    }

    [Fact]
    public void Tabs_have_the_defaults_of_the_web_component()
    {
        var tabs = new KlyTabs();

        Assert.Equal(TabsVariant.Pill, tabs.Variant);
        Assert.Equal(StackOrientation.Horizontal, tabs.Orientation);
        Assert.Equal(KlyColor.Primary, tabs.Color);
        Assert.Null(tabs.Value);
        Assert.Empty(tabs.Items);
    }

    [Fact]
    public void Tabs_select_the_first_enabled_tab_and_show_its_panel()
    {
        var tabs = ThreeTabs(out var panels);

        Assert.Equal("0", tabs.Current);
        Assert.Same(panels[0], tabs.Panel.Content);

        var skipped = new KlyTabs { Items = [new KlyTabItem("A") { Disabled = true }, new KlyTabItem("B") { Value = "b" }] };
        Assert.Equal("b", skipped.Current);
    }

    [Fact]
    public void Pressing_a_tab_selects_it_and_raises_one_event()
    {
        var tabs = ThreeTabs(out var panels);
        var events = new List<(string? New, string? Old)>();
        tabs.ValueChanged += (_, e) => events.Add((e.Value, e.OldValue));

        tabs.TabButtons[1].Press();
        tabs.TabButtons[1].Press();

        Assert.Equal("two", tabs.Value);
        Assert.Same(panels[1], tabs.Panel.Content);
        Assert.Equal([("two", null)], events);
    }

    [Fact]
    public void A_disabled_tab_is_dimmed_and_cannot_be_selected()
    {
        var tabs = ThreeTabs(out _);

        var disabled = tabs.TabButtons[2];
        Assert.False(disabled.IsEnabled);
        Assert.Equal(0.6, disabled.Opacity);
        disabled.Press();
        Assert.Null(tabs.Value);
    }

    [Fact]
    public void Tab_value_goes_both_ways()
    {
        var tabs = ThreeTabs(out var panels);
        var events = new List<string?>();
        tabs.ValueChanged += (_, e) => events.Add(e.Value);

        tabs.Value = "two";

        Assert.Same(panels[1], tabs.Panel.Content);
        Assert.Equal(["two"], events);
    }

    [Fact]
    public void Tabs_have_a_touch_target_and_say_their_label()
    {
        var tabs = ThreeTabs(out _);

        Assert.All(tabs.TabButtons, button => Assert.True(button.MinimumHeightRequest >= 40 && button.MinimumWidthRequest >= 40));
        Assert.Equal("Two", DescriptionOf(tabs.TabButtons[1]));

        tabs.AccessibleName = "Sections";
        Assert.Equal("Sections", DescriptionOf(tabs.ListHost));
    }

    [Fact]
    public void The_pill_variant_rings_the_selected_tab_and_the_link_variant_underlines_it()
    {
        var tabs = ThreeTabs(out _);
        Assert.Equal(1, tabs.TabButtons[0].Face.StrokeThickness);
        Assert.Equal(0, tabs.TabButtons[1].Face.StrokeThickness);
        Assert.Equal(new Thickness(12, 6), tabs.TabButtons[0].Face.Padding);

        tabs.Variant = TabsVariant.Link;
        Assert.All(tabs.TabButtons, button => Assert.Equal(new Thickness(12, 8, 12, 0), button.Face.Padding));
        Assert.All(tabs.TabButtons, button => Assert.IsType<Grid>(button.Face.Content));
        Assert.Equal(2, ((BoxView)((Grid)tabs.TabButtons[0].Face.Content!).Children[1]).HeightRequest);
        // Only the selected tab has an underline in the color; the others have a transparent one.
        Assert.Equal(1f, ((BoxView)((Grid)tabs.TabButtons[0].Face.Content!).Children[1]).Color.Alpha, 3);
        Assert.Equal(0f, ((BoxView)((Grid)tabs.TabButtons[1].Face.Content!).Children[1]).Color.Alpha, 3);
    }

    [Fact]
    public void Vertical_tabs_sit_beside_their_panel_and_horizontal_ones_above_it()
    {
        var tabs = ThreeTabs(out _);
        Assert.Equal(2, tabs.RowDefinitions.Count);
        Assert.Empty(tabs.ColumnDefinitions);

        tabs.Orientation = StackOrientation.Vertical;
        Assert.Equal(2, tabs.ColumnDefinitions.Count);
        Assert.Empty(tabs.RowDefinitions);
    }

    [Fact]
    public void A_tab_icon_comes_before_its_label()
    {
        var tabs = new KlyTabs { Items = [new KlyTabItem("Search") { Icon = KlyIconNames.Search }] };

        var row = Assert.IsType<HorizontalStackLayout>(tabs.TabButtons[0].Face.Content);
        Assert.Equal(KlyIconNames.Search, Assert.IsType<KlyIcon>(row.Children[0]).Name);
        Assert.Equal("Search", Assert.IsType<Label>(row.Children[1]).Text);
    }

    // ----- KlyAccordion -----

    private static KlyAccordion ThreeItems() => new()
    {
        Items = [new KlyAccordionItem("A") { Text = "text a" }, new KlyAccordionItem("B") { Value = "b", Content = new Label { Text = "view b" } }, new KlyAccordionItem("C") { Disabled = true }],
    };

    [Fact]
    public void Accordion_has_the_defaults_of_the_web_component()
    {
        var accordion = new KlyAccordion();

        Assert.False(accordion.Multiple);
        Assert.True(accordion.Collapsible);
        Assert.Equal(3, accordion.HeadingLevel);
        Assert.Null(accordion.Value);
    }

    [Fact]
    public void Only_the_first_default_open_item_starts_open_unless_several_can_be_open()
    {
        var items = new[] { new KlyAccordionItem("A") { DefaultOpen = true }, new KlyAccordionItem("B") { DefaultOpen = true } };

        var single = new KlyAccordion { Items = items };
        Assert.Equal([true, false], single.Bodies.Select(b => b.IsVisible));

        var multiple = new KlyAccordion { Items = items, Multiple = true };
        Assert.Equal([true, true], multiple.Bodies.Select(b => b.IsVisible));
    }

    [Fact]
    public void Opening_an_item_closes_the_others_and_raises_one_event()
    {
        var accordion = ThreeItems();
        var events = new List<string[]>();
        accordion.ValueChanged += (_, e) => events.Add(e.Value?.ToArray() ?? []);

        accordion.Headers[0].Press();
        accordion.Headers[1].Press();

        Assert.Equal(["b"], accordion.Value);
        Assert.Equal([false, true, false], accordion.Bodies.Select(b => b.IsVisible));
        Assert.Equal(2, events.Count);
        Assert.Equal(["0"], events[0]);
        Assert.Equal(["b"], events[1]);
    }

    [Fact]
    public void A_single_item_closes_when_collapsible_and_stays_open_when_not()
    {
        var accordion = ThreeItems();
        accordion.Headers[0].Press();
        accordion.Headers[0].Press();
        Assert.Empty(accordion.Value!);

        var fixedOpen = ThreeItems();
        fixedOpen.Collapsible = false;
        fixedOpen.Headers[0].Press();
        fixedOpen.Headers[0].Press();
        Assert.Equal(["0"], fixedOpen.Value);
    }

    [Fact]
    public void Several_items_stay_open_with_multiple()
    {
        var accordion = ThreeItems();
        accordion.Multiple = true;

        accordion.Headers[1].Press();
        accordion.Headers[0].Press();

        Assert.Equal(["0", "b"], accordion.Value);
        accordion.Headers[1].Press();
        Assert.Equal(["0"], accordion.Value);
    }

    [Fact]
    public void A_disabled_item_is_dimmed_and_cannot_be_opened()
    {
        var accordion = ThreeItems();

        Assert.False(accordion.Headers[2].IsEnabled);
        Assert.Equal(0.6, accordion.Headers[2].Opacity);
        accordion.Headers[2].Press();
        Assert.Null(accordion.Value);
    }

    [Fact]
    public void The_open_values_go_both_ways_and_the_chevron_turns()
    {
        var accordion = ThreeItems();

        accordion.Value = ["b"];

        Assert.True(accordion.Bodies[1].IsVisible);
        Assert.Equal(180, accordion.Chevrons[1].Rotation);
        Assert.Equal(0, accordion.Chevrons[0].Rotation);
    }

    [Fact]
    public void The_body_shows_the_view_or_else_the_text()
    {
        var accordion = ThreeItems();

        Assert.Equal("text a", Assert.IsType<Label>(((ContentView)accordion.Bodies[0]).Content).Text);
        Assert.Equal("view b", Assert.IsType<Label>(((ContentView)accordion.Bodies[1]).Content).Text);
    }

    [Fact]
    public void Accordion_headers_are_headings_of_the_chosen_level_with_a_touch_target()
    {
        var accordion = ThreeItems();

        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(accordion.Headers[0]));
        accordion.HeadingLevel = 2;
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(accordion.Headers[0]));
        Assert.Equal("A", DescriptionOf(accordion.Headers[0]));
        Assert.True(accordion.Headers[0].MinimumHeightRequest >= 40);
    }

    // ----- KlyCollapsible -----

    [Fact]
    public void Collapsible_toggles_from_its_trigger_with_one_event_per_change()
    {
        var content = new Label { Text = "more" };
        var collapsible = new KlyCollapsible { Label = "Details", Content = content };
        var events = new List<bool>();
        collapsible.OpenChanged += (_, e) => events.Add(e.Value);
        Assert.False(collapsible.BodyView.IsVisible);

        collapsible.TriggerButton.Press();
        Assert.True(collapsible.Open);
        Assert.True(collapsible.BodyView.IsVisible);
        Assert.Equal(180, collapsible.Chevron.Rotation);

        collapsible.TriggerButton.Press();
        Assert.False(collapsible.Open);
        Assert.Equal(0, collapsible.Chevron.Rotation);
        Assert.Equal([true, false], events);
        Assert.Same(content, ((ContentView)collapsible.BodyView).Content);
    }

    [Fact]
    public void Collapsible_open_state_goes_both_ways()
    {
        var collapsible = new KlyCollapsible { Label = "Details", Content = new Label() };

        collapsible.Open = true;

        Assert.True(collapsible.BodyView.IsVisible);
    }

    [Fact]
    public void A_disabled_collapsible_cannot_be_toggled_by_the_user()
    {
        var collapsible = new KlyCollapsible { Label = "Details", Content = new Label(), IsEnabled = false };

        Assert.False(collapsible.TriggerButton.IsEnabled);
        Assert.Equal(0.6, collapsible.TriggerButton.Opacity);
        collapsible.TriggerButton.Press();
        Assert.False(collapsible.Open);
    }

    [Fact]
    public void A_custom_trigger_replaces_the_label_and_the_chevron_can_be_hidden()
    {
        var custom = new Label { Text = "custom" };
        var collapsible = new KlyCollapsible { Label = "Details", Trigger = custom, ShowIcon = false };

        Assert.Contains(custom, All<Label>(collapsible.TriggerButton));
        Assert.DoesNotContain(All<Label>(collapsible.TriggerButton), l => l.Text == "Details" && l != custom);
        Assert.False(collapsible.Chevron.IsVisible);
        Assert.Null(DescriptionOf(collapsible.TriggerButton));

        collapsible.Trigger = null;
        Assert.Equal("Details", DescriptionOf(collapsible.TriggerButton));
        Assert.True(collapsible.TriggerButton.MinimumHeightRequest >= 40);
    }

    // ----- KlyBreadcrumb -----

    [Fact]
    public void Breadcrumb_puts_a_separator_between_items_and_none_after_the_last()
    {
        var crumbs = new KlyBreadcrumb { Items = [new KlyBreadcrumbItem("Home") { Href = "/" }, new KlyBreadcrumbItem("Docs") { Href = "/docs" }, new KlyBreadcrumbItem("Page")] };

        var cells = crumbs.Children.Cast<HorizontalStackLayout>().ToList();
        Assert.Equal([2, 2, 1], cells.Select(c => c.Children.Count));
        Assert.Equal(KlyIconNames.ChevronRight, Assert.IsType<KlyIcon>(cells[0].Children[1]).Name);
        Assert.Equal(FlexWrap.Wrap, crumbs.Wrap);
    }

    [Fact]
    public void Only_items_with_an_address_before_the_last_one_can_be_tapped()
    {
        var home = new KlyBreadcrumbItem("Home") { Href = "/" };
        var plain = new KlyBreadcrumbItem("Plain");
        var last = new KlyBreadcrumbItem("Page") { Href = "/page" };
        var crumbs = new KlyBreadcrumb { Items = [home, plain, last] };
        var clicked = new List<KlyBreadcrumbItem>();
        crumbs.ItemClicked += (_, item) => clicked.Add(item);

        var cells = crumbs.Children.Cast<HorizontalStackLayout>().ToList();
        var button = Assert.IsType<NavigationButton>(cells[0].Children[0]);
        Assert.IsType<HorizontalStackLayout>(cells[1].Children[0]);
        Assert.IsType<HorizontalStackLayout>(cells[2].Children[0]);

        button.Press();
        Assert.Equal([home], clicked);
        Assert.Equal("Home", DescriptionOf(button));
        Assert.True(button.MinimumHeightRequest >= 40);
    }

    [Fact]
    public void A_separator_text_replaces_the_icon_and_the_trail_has_an_accessible_name()
    {
        var crumbs = new KlyBreadcrumb { Items = [new KlyBreadcrumbItem("A"), new KlyBreadcrumbItem("B")], SeparatorText = "/" };

        var separator = Assert.IsType<Label>(((HorizontalStackLayout)crumbs.Children[0]).Children[1]);
        Assert.Equal("/", separator.Text);
        Assert.Equal("Breadcrumb", DescriptionOf(crumbs));

        crumbs.AccessibleName = "Trail";
        Assert.Equal("Trail", DescriptionOf(crumbs));
    }

    [Fact]
    public void A_breadcrumb_icon_comes_before_the_label()
    {
        var crumbs = new KlyBreadcrumb { Items = [new KlyBreadcrumbItem("Home") { Icon = KlyIconNames.Folder }] };

        var entry = Assert.IsType<HorizontalStackLayout>(((HorizontalStackLayout)crumbs.Children[0]).Children[0]);
        Assert.Equal(KlyIconNames.Folder, Assert.IsType<KlyIcon>(entry.Children[0]).Name);
    }

    // ----- KlyPagination -----

    [Theory]
    [InlineData(1, 10, 1, new[] { 1, 2, 3, 4, 5, 0, 10 })]
    [InlineData(5, 10, 1, new[] { 1, 0, 4, 5, 6, 0, 10 })]
    [InlineData(10, 10, 1, new[] { 1, 0, 6, 7, 8, 9, 10 })]
    [InlineData(3, 5, 1, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(1, 1, 1, new[] { 1 })]
    public void Page_entries_keep_a_constant_width_while_paging(int page, int count, int sibling, int[] expected) =>
        Assert.Equal(expected, KlyPagination.Entries(page, count, sibling));

    [Fact]
    public void The_page_count_is_at_least_one_and_the_current_page_stays_in_range()
    {
        var pagination = new KlyPagination { Total = 95, PageSize = 10, Page = 99 };
        Assert.Equal(10, pagination.PageCount);
        Assert.Equal(10, pagination.EffectivePage);

        pagination.Total = 0;
        Assert.Equal(1, pagination.PageCount);
        Assert.Equal(1, pagination.EffectivePage);
    }

    private static string Describe(NavigationButton button) => DescriptionOf(button) ?? "";

    [Fact]
    public void Pagination_shows_previous_and_next_and_disables_them_at_the_ends()
    {
        var pagination = new KlyPagination { Total = 100 };

        var names = pagination.Buttons.Select(Describe).ToList();
        Assert.Equal("Previous page", names[0]);
        Assert.Equal("Next page", names[^1]);
        Assert.False(pagination.Buttons[0].IsEnabled);
        Assert.True(pagination.Buttons[^1].IsEnabled);

        pagination.Page = 10;
        Assert.True(pagination.Buttons[0].IsEnabled);
        Assert.False(pagination.Buttons[^1].IsEnabled);
    }

    [Fact]
    public void Pressing_next_moves_to_the_next_page_with_one_event()
    {
        var pagination = new KlyPagination { Total = 100 };
        var pages = new List<int>();
        pagination.PageChanged += (_, page) => pages.Add(page);

        pagination.Buttons.Single(b => Describe(b) == "Next page").Press();
        pagination.Buttons.Single(b => Describe(b) == "Page 5").Press();

        Assert.Equal(5, pagination.Page);
        Assert.Equal([2, 5], pages);
    }

    [Fact]
    public void Pressing_the_current_page_changes_nothing()
    {
        var pagination = new KlyPagination { Total = 100 };
        var pages = new List<int>();
        pagination.PageChanged += (_, page) => pages.Add(page);

        pagination.Buttons.Single(b => Describe(b) == "Page 1").Press();

        Assert.Empty(pages);
    }

    [Fact]
    public void First_and_last_buttons_jump_to_the_ends_and_controls_can_be_hidden()
    {
        var pagination = new KlyPagination { Total = 100, Page = 5, ShowFirstLast = true };
        var names = pagination.Buttons.Select(Describe).ToList();
        Assert.Equal("First page", names[0]);
        Assert.Equal("Last page", names[^1]);

        pagination.Buttons[^1].Press();
        Assert.Equal(10, pagination.Page);

        pagination.ShowControls = false;
        pagination.ShowFirstLast = false;
        Assert.DoesNotContain(pagination.Buttons.Select(Describe), n => n.Contains("page", StringComparison.OrdinalIgnoreCase) && !n.StartsWith("Page ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(KlySize.Xs, 24, 12)]
    [InlineData(KlySize.Sm, 28, 12)]
    [InlineData(KlySize.Md, 32, 14)]
    [InlineData(KlySize.Lg, 36, 14)]
    [InlineData(KlySize.Xl, 40, 16)]
    public void Pagination_size_sets_the_height_and_font_but_keeps_a_touch_target(KlySize size, double height, double font)
    {
        var pagination = new KlyPagination { Total = 30, Size = size };

        var (h, _, _, f) = KlyPagination.MetricsOf(size);
        Assert.Equal((height, font), (h, f));
        var page = pagination.Buttons.Single(b => Describe(b) == "Page 1");
        Assert.Equal(height, page.Face.HeightRequest);
        Assert.True(page.MinimumHeightRequest >= 40 && page.MinimumWidthRequest >= 40);
    }

    [Fact]
    public void The_current_page_is_ringed_and_the_labels_are_the_ones_given()
    {
        var pagination = new KlyPagination { Total = 30, Page = 2, PageLabel = "Seite" };

        Assert.Equal(1, pagination.Buttons.Single(b => Describe(b) == "Seite 2").Face.StrokeThickness);
        Assert.Equal(0, pagination.Buttons.Single(b => Describe(b) == "Seite 1").Face.StrokeThickness);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Primary).Fill.Light, pagination.Buttons.Single(b => Describe(b) == "Seite 2").Face.BackgroundColor);
        Assert.Equal(0f, pagination.Buttons.Single(b => Describe(b) == "Seite 1").Face.BackgroundColor.Alpha, 3);
        Assert.Equal("Pagination", DescriptionOf(pagination));
        pagination.AccessibleName = "Pages";
        Assert.Equal("Pages", DescriptionOf(pagination));
    }

    // ----- KlyStepper -----

    private static KlyStepper ThreeSteps() => new()
    {
        Items = [new KlyStepperItem("Account") { Description = "Create it" }, new KlyStepperItem("Profile") { Content = new Label { Text = "profile form" } }, new KlyStepperItem("Done") { Disabled = true }],
        Current = 1,
    };

    [Theory]
    [InlineData(0, 2, "completed")]
    [InlineData(2, 2, "active")]
    [InlineData(3, 2, "inactive")]
    public void A_step_is_completed_before_the_current_one_and_inactive_after_it(int index, int current, string expected) =>
        Assert.Equal(expected, KlyStepper.StateOf(index, current));

    [Theory]
    [InlineData(KlySize.Xs, 24, 12)]
    [InlineData(KlySize.Sm, 28, 12)]
    [InlineData(KlySize.Md, 32, 14)]
    [InlineData(KlySize.Lg, 36, 14)]
    [InlineData(KlySize.Xl, 40, 16)]
    public void Stepper_size_sets_the_marker_and_its_font(KlySize size, double diameter, double font) =>
        Assert.Equal((diameter, font), KlyStepper.MetricsOf(size));

    [Fact]
    public void Stepper_has_the_defaults_of_the_web_component()
    {
        var stepper = new KlyStepper();

        Assert.Equal(0, stepper.Current);
        Assert.False(stepper.Interactive);
        Assert.Equal(StackOrientation.Horizontal, stepper.Orientation);
        Assert.Equal(KlySize.Md, stepper.Size);
        Assert.Equal(-1, stepper.EffectiveCurrent);
    }

    [Fact]
    public void The_current_step_stays_inside_the_list()
    {
        var stepper = ThreeSteps();
        stepper.Current = 9;
        Assert.Equal(2, stepper.EffectiveCurrent);
        stepper.Current = -4;
        Assert.Equal(0, stepper.EffectiveCurrent);
    }

    [Fact]
    public void A_completed_step_shows_a_check_the_others_their_number_and_an_icon_replaces_both()
    {
        var stepper = new KlyStepper { Items = [new KlyStepperItem("A"), new KlyStepperItem("B"), new KlyStepperItem("C") { Icon = KlyIconNames.Star }], Current = 1 };

        string? Mark(int index)
        {
            var ring = (Border)stepper.Indicators[index];
            var disc = (Border)ring.Content!;
            return disc.Content switch { KlyIcon icon => icon.Name, Label label => label.Text, _ => null };
        }

        Assert.Equal(KlyIconNames.Check, Mark(0));
        Assert.Equal("2", Mark(1));
        Assert.Equal(KlyIconNames.Star, Mark(2));
    }

    [Fact]
    public void The_stepper_only_shows_progress_until_it_is_interactive()
    {
        var stepper = ThreeSteps();

        Assert.All(stepper.Indicators, view => Assert.IsNotType<NavigationButton>(view));

        stepper.Interactive = true;
        Assert.IsType<NavigationButton>(stepper.Indicators[0]);
        Assert.IsType<NavigationButton>(stepper.Indicators[1]);
        Assert.IsNotType<NavigationButton>(stepper.Indicators[2]);
    }

    [Fact]
    public void Tapping_a_step_makes_it_current_with_one_event()
    {
        var stepper = ThreeSteps();
        stepper.Interactive = true;
        var events = new List<int>();
        stepper.CurrentChanged += (_, index) => events.Add(index);

        ((NavigationButton)stepper.Indicators[0]).Press();

        Assert.Equal(0, stepper.Current);
        Assert.Equal([0], events);
        Assert.True(((NavigationButton)stepper.Indicators[0]).MinimumHeightRequest >= 40);
    }

    [Fact]
    public void The_content_of_the_current_step_shows_below_the_list()
    {
        var stepper = ThreeSteps();
        Assert.Equal("profile form", Assert.IsType<Label>(stepper.ContentHost.Content).Text);

        stepper.Current = 0;
        Assert.Null(stepper.ContentHost.Content);
    }

    [Fact]
    public void A_finished_step_is_said_in_words_to_a_screen_reader()
    {
        var stepper = ThreeSteps();

        var finished = All<Label>(stepper).Single(l => l.Text == "Account");
        Assert.Equal("Account (completed)", DescriptionOf(finished));
        var pending = All<Label>(stepper).Single(l => l.Text == "Done");
        Assert.Null(DescriptionOf(pending));

        stepper.CompletedLabel = "terminé";
        Assert.Equal("Account (terminé)", DescriptionOf(All<Label>(stepper).Single(l => l.Text == "Account")));
        Assert.Equal("1. Account", DescriptionOf(stepper.Indicators[0]));
    }

    [Fact]
    public void A_horizontal_stepper_has_a_column_per_step_and_a_vertical_one_a_row()
    {
        var stepper = ThreeSteps();
        var horizontal = Assert.IsType<Grid>(stepper.Children.OfType<ContentView>().First().Content);
        Assert.Equal(3, horizontal.ColumnDefinitions.Count);
        Assert.Equal(GridLength.Auto, horizontal.ColumnDefinitions[2].Width);

        stepper.Orientation = StackOrientation.Vertical;
        var vertical = Assert.IsType<VerticalStackLayout>(stepper.Children.OfType<ContentView>().First().Content);
        Assert.Equal(3, vertical.Children.Count);
    }

    // ----- KlyTimeline -----

    private static readonly DateTimeOffset Noon = new(2026, 3, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Timeline_has_the_defaults_of_the_web_component()
    {
        var timeline = new KlyTimeline();

        Assert.Equal(KlyTimelineLayout.Default, timeline.Layout);
        Assert.Equal(StackOrientation.Vertical, timeline.Orientation);
        Assert.Equal(KlySize.Md, timeline.Size);
        Assert.Equal(4, timeline.DayHeadingLevel);
        Assert.Null(timeline.Active);
        Assert.False(timeline.ShowNumbers);
    }

    [Theory]
    [InlineData(KlySize.Xs, 20, 12)]
    [InlineData(KlySize.Sm, 24, 12)]
    [InlineData(KlySize.Md, 28, 14)]
    [InlineData(KlySize.Lg, 32, 14)]
    [InlineData(KlySize.Xl, 36, 16)]
    public void Timeline_size_sets_the_marker_and_its_font(KlySize size, double diameter, double font) =>
        Assert.Equal((diameter, font), KlyTimeline.MetricsOf(size));

    [Fact]
    public void Events_up_to_the_active_one_are_active_and_an_own_state_wins()
    {
        var items = new[] { new KlyTimelineItem("A"), new KlyTimelineItem("B"), new KlyTimelineItem("C"), new KlyTimelineItem("D") { State = KlyTimelineState.Current } };
        var timeline = new KlyTimeline { Items = items, Active = 1 };

        Assert.Equal(["active", "active", "inactive", "current"], items.Select((item, i) => timeline.StateOf(item, i)));
        timeline.Active = null;
        Assert.Equal("inactive", timeline.StateOf(items[0], 0));
    }

    [Fact]
    public void A_vertical_timeline_stacks_the_events_and_a_horizontal_one_has_a_column_each()
    {
        var timeline = new KlyTimeline { Items = [new KlyTimelineItem("A"), new KlyTimelineItem("B")] };
        Assert.IsType<VerticalStackLayout>(timeline.Children[0]);

        timeline.Orientation = StackOrientation.Horizontal;
        Assert.Equal(2, Assert.IsType<Grid>(timeline.Children[0]).ColumnDefinitions.Count);
    }

    [Fact]
    public void Markers_show_an_icon_a_number_or_nothing()
    {
        var timeline = new KlyTimeline { Items = [new KlyTimelineItem("A") { Icon = KlyIconNames.Star }, new KlyTimelineItem("B")], ShowNumbers = true };

        Assert.Contains(All<KlyIcon>(timeline), i => i.Name == KlyIconNames.Star);
        Assert.Contains(All<Label>(timeline), l => l.Text == "2" && l.FontSize == 11);

        timeline.ShowNumbers = false;
        Assert.DoesNotContain(All<Label>(timeline), l => l.Text == "2");
    }

    [Fact]
    public void The_pipeline_layout_wraps_the_events_and_the_log_layout_has_a_row_each()
    {
        var items = new[] { new KlyTimelineItem("A") { At = Noon, Description = "d", Actor = "Ada" }, new KlyTimelineItem("B") { Date = "Yesterday" } };
        var timeline = new KlyTimeline { Items = items, Layout = KlyTimelineLayout.Pipeline };

        var flex = Assert.IsType<FlexLayout>(timeline.Children[0]);
        Assert.Equal(FlexWrap.Wrap, flex.Wrap);
        Assert.Equal(2, flex.Children.Count);

        timeline.Layout = KlyTimelineLayout.Log;
        Assert.Equal(2, All<Grid>(timeline).Count(g => g.ColumnDefinitions.Count == 3));
        Assert.Contains(All<Label>(timeline), l => l.Text == "Yesterday" || l.Text.EndsWith("Yesterday", StringComparison.Ordinal));
        Assert.Contains(All<Label>(timeline), l => l.Text == "Ada");
    }

    [Fact]
    public void The_log_line_starts_with_the_date_and_time_and_a_free_text_replaces_the_time()
    {
        var items = new[] { new KlyTimelineItem("A") { At = new DateTimeOffset(2026, 3, 12, 9, 30, 0, TimeSpan.Zero) }, new KlyTimelineItem("B") { At = Noon, Date = "noon" } };
        var timeline = new KlyTimeline { Items = items, Layout = KlyTimelineLayout.Log, Culture = CultureInfo.InvariantCulture };

        var labels = All<Label>(timeline).Select(l => l.Text).ToList();
        Assert.Contains("03/12/2026 09:30", labels);
        Assert.Contains("03/12/2026 noon", labels);
    }

    [Fact]
    public void Show_more_reveals_the_events_by_steps_and_goes_away_with_the_last_one()
    {
        var items = Enumerable.Range(1, 5).Select(i => new KlyTimelineItem($"E{i}")).ToList();
        var timeline = new KlyTimeline { Items = items, Max = 2, Step = 2 };

        Assert.Equal(["E1", "E2"], All<Label>(timeline).Select(l => l.Text).Where(t => t.StartsWith('E')));
        Assert.NotNull(timeline.More);
        Assert.Contains(All<Label>(timeline), l => l.Text == "Show more · 2");
        Assert.True(timeline.More!.MinimumHeightRequest >= 40);

        timeline.More!.Press();
        Assert.Equal(4, All<Label>(timeline).Count(l => l.Text.StartsWith('E')));
        Assert.Contains(All<Label>(timeline), l => l.Text == "Show more · 1");

        timeline.More!.Press();
        Assert.Equal(5, All<Label>(timeline).Count(l => l.Text.StartsWith('E')));
        Assert.Null(timeline.More);
    }

    [Fact]
    public void Show_more_without_a_step_reveals_everything()
    {
        var items = Enumerable.Range(1, 5).Select(i => new KlyTimelineItem($"E{i}")).ToList();
        var timeline = new KlyTimeline { Items = items, Max = 2, ShowMoreLabel = "More ({0})" };

        Assert.Contains(All<Label>(timeline), l => l.Text == "More (3)");
        timeline.More!.Press();
        Assert.Equal(5, All<Label>(timeline).Count(l => l.Text.StartsWith('E')));
        Assert.Null(timeline.More);
    }

    private static KlyTimeline Schedule(DateTimeOffset? now = null, params KlyTimelineItem[] items) => new()
    {
        Layout = KlyTimelineLayout.Schedule,
        Items = items,
        Now = now ?? Noon,
        Culture = CultureInfo.InvariantCulture,
    };

    [Fact]
    public void The_schedule_groups_events_by_day_and_names_today_and_tomorrow()
    {
        var timeline = Schedule(Noon,
            new KlyTimelineItem("Today meeting") { At = new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero) },
            new KlyTimelineItem("Tomorrow call") { At = new DateTimeOffset(2026, 3, 13, 15, 0, 0, TimeSpan.Zero) },
            new KlyTimelineItem("Later") { At = new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.Zero) },
            new KlyTimelineItem("Someday"));

        var texts = All<Label>(timeline).Select(l => l.Text).ToList();
        Assert.Contains("Today · Thursday, 12 March 2026", texts);
        Assert.Contains("Tomorrow · Friday, 13 March 2026", texts);
        Assert.Contains("Friday, 20 March 2026", texts);
        Assert.Contains("Unscheduled", texts);
    }

    [Fact]
    public void Schedule_day_titles_are_headings_of_the_chosen_level_or_plain_text()
    {
        var timeline = Schedule(Noon, new KlyTimelineItem("A") { At = Noon });
        var title = All<Label>(timeline).Single(l => l.Text.StartsWith("Today", StringComparison.Ordinal));
        Assert.Equal(SemanticHeadingLevel.Level4, SemanticProperties.GetHeadingLevel(title));

        timeline.DayHeadingLevel = 3;
        title = All<Label>(timeline).Single(l => l.Text.StartsWith("Today", StringComparison.Ordinal));
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(title));

        timeline.DayHeadingLevel = null;
        title = All<Label>(timeline).Single(l => l.Text.StartsWith("Today", StringComparison.Ordinal));
        Assert.Equal(SemanticHeadingLevel.None, SemanticProperties.GetHeadingLevel(title));
    }

    [Fact]
    public void A_schedule_event_shows_its_time_range_and_its_length()
    {
        var at = new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero);
        var timeline = Schedule(Noon, new KlyTimelineItem("Workshop") { At = at, EndAt = at.AddMinutes(90) }, new KlyTimelineItem("Trip") { At = new DateTimeOffset(2026, 3, 14, 0, 0, 0, TimeSpan.Zero), EndAt = new DateTimeOffset(2026, 3, 17, 0, 0, 0, TimeSpan.Zero) });

        var texts = All<Label>(timeline).Select(l => l.Text).ToList();
        Assert.Contains("10:00 – 11:30", texts);
        Assert.Contains("1 h 30 min", texts);
        Assert.Contains("All day", texts);
        Assert.Contains("3 days", texts);
    }

    [Fact]
    public void Schedule_labels_can_be_replaced()
    {
        var timeline = Schedule(Noon, new KlyTimelineItem("A") { At = Noon }, new KlyTimelineItem("B"));
        timeline.TodayLabel = "Aujourd'hui";
        timeline.UnscheduledLabel = "Sans date";

        var texts = All<Label>(timeline).Select(l => l.Text).ToList();
        Assert.Contains("Aujourd'hui · Thursday, 12 March 2026", texts);
        Assert.Contains("Sans date", texts);
    }

    [Fact]
    public void A_current_schedule_event_is_bold_and_a_done_one_is_dimmed_when_it_has_a_color()
    {
        var timeline = Schedule(Noon,
            new KlyTimelineItem("Now") { At = Noon, State = KlyTimelineState.Current },
            new KlyTimelineItem("Past") { At = Noon.AddHours(1), State = KlyTimelineState.Done, Color = KlyColor.Success });

        Assert.Equal(FontAttributes.Bold, All<Label>(timeline).Single(l => l.Text == "Now").FontAttributes);
        Assert.Equal(FontAttributes.None, All<Label>(timeline).Single(l => l.Text == "Past").FontAttributes);
        Assert.Contains(All<Grid>(timeline), g => g.Opacity == 0.7);
    }

    [Fact]
    public void Schedule_sorting_puts_all_day_events_first_and_undated_ones_last()
    {
        var day = new DateTimeOffset(2026, 3, 12, 0, 0, 0, TimeSpan.Zero);
        var items = new[]
        {
            new KlyTimelineItem("undated"),
            new KlyTimelineItem("late") { At = day.AddHours(15) },
            new KlyTimelineItem("early") { At = day.AddHours(8) },
            new KlyTimelineItem("all day") { At = day },
        };

        Assert.Equal(["all day", "early", "late", "undated"], NavigationTimelineSchedule.Sort(items).Select(e => e.Item.Title));
        var days = NavigationTimelineSchedule.Group(NavigationTimelineSchedule.Sort(items));
        Assert.Equal([3, 1], days.Select(d => d.Events.Count));
        Assert.Null(days[1].Day);
    }

    [Theory]
    [InlineData(0, 0, 0, 13, 0, true, 1, null)]
    [InlineData(0, 0, 14, 0, 0, true, 2, null)]
    [InlineData(0, 0, 14, 23, 59, true, 3, null)]
    [InlineData(10, 0, 12, 11, 30, false, 1, 90)]
    public void An_event_is_all_day_when_it_starts_at_midnight_and_has_no_end_or_ends_at_a_day_boundary(int hour, int minute, int endDay, int endHour, int endMinute, bool allDay, int days, int? minutes)
    {
        var start = new DateTimeOffset(2026, 3, 12, hour, minute, 0, TimeSpan.Zero);
        var item = new KlyTimelineItem("x") { At = start, EndAt = endDay == 0 ? null : new DateTimeOffset(2026, 3, endDay, endHour, endMinute, 0, TimeSpan.Zero) };

        var described = NavigationTimelineSchedule.Describe(item);

        Assert.Equal(allDay, described.AllDay);
        Assert.Equal(days, described.Days);
        Assert.Equal(minutes, described.Minutes);
    }

    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(120, "2 h")]
    [InlineData(90, "1 h 30 min")]
    [InlineData(-5, "0 min")]
    public void A_length_reads_in_hours_and_minutes(int minutes, string expected) =>
        Assert.Equal(expected, NavigationTimelineSchedule.Duration(minutes, "h", "min"));

    // ----- KlyNavigationMenu -----

    [Theory]
    [InlineData("/docs", "/docs", false, true)]
    [InlineData("/docs/", "/docs", false, true)]
    [InlineData("/docs/intro", "/docs", false, true)]
    [InlineData("/docs/intro", "/docs", true, false)]
    [InlineData("/docs?tab=2#top", "/docs", true, true)]
    [InlineData("/documents", "/docs", false, false)]
    [InlineData("/docs", "/", false, false)]
    [InlineData("/", "/", false, true)]
    [InlineData("/DOCS", "/docs", false, true)]
    [InlineData(null, "/docs", false, false)]
    public void An_item_matches_its_address_and_the_addresses_below_it_unless_exact(string? current, string href, bool exact, bool expected) =>
        Assert.Equal(expected, KlyNavigationMenu.Matches(current, href, exact));

    private static KlyNavigationMenu Menu(StackOrientation orientation = StackOrientation.Horizontal, string? current = null) => new()
    {
        Orientation = orientation,
        Current = current,
        Items =
        [
            new KlyNavigationItem("Home") { Href = "/", Exact = true },
            new KlyNavigationItem("Docs") { Children = [new KlyNavigationItem("Intro") { Href = "/docs/intro" }, new KlyNavigationItem("Guide") { Href = "/docs/guide" }] },
            new KlyNavigationItem("Blog") { Children = [new KlyNavigationItem("Latest") { Href = "/blog/latest" }] },
            new KlyNavigationItem("Off") { Href = "/off", Disabled = true },
            new KlyNavigationItem("Label only"),
        ],
    };

    [Fact]
    public void The_menu_marks_the_item_of_the_current_page_and_the_groups_that_hold_it()
    {
        var menu = Menu(current: "/docs/intro");

        Assert.False(menu.IsActive(menu.Items[0]));
        Assert.True(menu.IsActive(menu.Items[1]));
        Assert.True(menu.IsActive(menu.Items[1].Children![0]));
        Assert.False(menu.IsActive(menu.Items[1].Children![1]));
        Assert.False(menu.IsActive(menu.Items[4]));

        var forced = new KlyNavigationMenu { Items = [new KlyNavigationItem("X") { Active = true }, new KlyNavigationItem("Y") { Href = "/y", Active = false }], Current = "/y" };
        Assert.True(forced.IsActive(forced.Items[0]));
        Assert.False(forced.IsActive(forced.Items[1]));
    }

    [Fact]
    public void An_active_link_is_tinted_and_ringed()
    {
        var menu = Menu(current: "/");

        Assert.Equal(1, menu.Links[0].Face.StrokeThickness);
        Assert.Equal(0, menu.Links[1].Face.StrokeThickness);
        Assert.Equal(0.1f, menu.Links[0].Face.BackgroundColor.Alpha, 3);
        Assert.Equal(0f, menu.Links[1].Face.BackgroundColor.Alpha, 3);
    }

    [Fact]
    public void Tapping_a_link_raises_the_event_once_and_a_disabled_or_plain_item_raises_none()
    {
        var menu = Menu();
        var invoked = new List<string>();
        menu.ItemInvoked += (_, item) => invoked.Add(item.Label);

        var home = menu.Links.Single(l => DescriptionOf(l) == "Home");
        home.Press();
        menu.Links.Single(l => DescriptionOf(l) == "Off").Press();
        menu.Links.Single(l => DescriptionOf(l) == "Label only").Press();

        Assert.Equal(["Home"], invoked);
        Assert.False(menu.Links.Single(l => DescriptionOf(l) == "Off").IsEnabled);
        Assert.Equal(0.6, menu.Links.Single(l => DescriptionOf(l) == "Off").Opacity);
        Assert.True(home.MinimumHeightRequest >= 40);
    }

    [Fact]
    public void A_horizontal_menu_opens_the_sub_items_of_one_item_in_a_panel_under_the_row()
    {
        var menu = Menu();
        Assert.Null(menu.Panel);
        Assert.Equal(2, menu.GroupTriggers.Count);

        menu.GroupTriggers[0].Press();
        Assert.NotNull(menu.Panel);
        Assert.Equal(["Intro", "Guide"], All<NavigationButton>(menu.Panel!).Select(DescriptionOf));

        menu.GroupTriggers[1].Press();
        Assert.Equal(["Latest"], All<NavigationButton>(menu.Panel!).Select(DescriptionOf));

        menu.GroupTriggers[1].Press();
        Assert.Null(menu.Panel);
    }

    [Fact]
    public void A_sub_item_of_the_panel_raises_the_event()
    {
        var menu = Menu();
        var invoked = new List<string>();
        menu.ItemInvoked += (_, item) => invoked.Add(item.Label);

        menu.GroupTriggers[0].Press();
        All<NavigationButton>(menu.Panel!).Single(b => DescriptionOf(b) == "Guide").Press();

        Assert.Equal(["Guide"], invoked);
    }

    [Fact]
    public void A_vertical_menu_opens_the_group_of_the_current_page_and_each_group_toggles()
    {
        var menu = Menu(StackOrientation.Vertical, "/docs/guide");

        Assert.Equal([true, false], menu.Groups.Select(g => g.IsVisible));

        menu.GroupTriggers[1].Press();
        Assert.Equal([true, true], menu.Groups.Select(g => g.IsVisible));

        menu.GroupTriggers[0].Press();
        Assert.Equal([false, true], menu.Groups.Select(g => g.IsVisible));
    }

    [Fact]
    public void The_menu_has_an_accessible_name_and_a_row_or_a_column_of_items()
    {
        var menu = Menu();
        menu.AccessibleName = "Main";
        Assert.Equal("Main", DescriptionOf(menu));
        Assert.IsType<HorizontalStackLayout>(menu.Children[0]);

        menu.Orientation = StackOrientation.Vertical;
        Assert.Equal(5, menu.Children.Count);
    }

    // ----- KlyTree -----

    private static KlyTree Tree() => new()
    {
        Items =
        [
            new KlyTreeItem("src") { Expanded = true, Children = [new KlyTreeItem("app.cs") { Value = "src/app.cs" }, new KlyTreeItem("lib") { Children = [new KlyTreeItem("deep.cs")] }] },
            new KlyTreeItem("readme.md"),
            new KlyTreeItem("locked") { Disabled = true },
        ],
    };

    [Fact]
    public void A_tree_shows_the_children_of_expanded_nodes_only_and_indents_them()
    {
        var tree = Tree();

        Assert.Equal(["src", "app.cs", "lib", "readme.md", "locked"], tree.VisibleNodes().Select(n => n.Item.Label));
        Assert.Equal([0, 1, 1, 0, 0], tree.VisibleNodes().Select(n => n.Depth));
        Assert.Equal([0d, KlyTree.Indent, KlyTree.Indent, 0, 0], tree.Rows.Select(r => r.Margin.Left));
    }

    [Fact]
    public void Tapping_a_parent_selects_it_and_expands_it_with_one_event()
    {
        var tree = Tree();
        var events = new List<string?>();
        tree.ValueChanged += (_, e) => events.Add(e.Value);

        tree.Rows[2].Press();

        Assert.Equal("lib", tree.Value);
        Assert.Equal(["lib"], events);
        Assert.Equal(["src", "app.cs", "lib", "deep.cs", "readme.md", "locked"], tree.VisibleNodes().Select(n => n.Item.Label));
    }

    [Fact]
    public void Tapping_a_selected_parent_again_collapses_it_without_a_new_event()
    {
        var tree = Tree();
        var events = new List<string?>();
        tree.ValueChanged += (_, e) => events.Add(e.Value);

        tree.Rows[0].Press();
        Assert.Equal(["src", "readme.md", "locked"], tree.VisibleNodes().Select(n => n.Item.Label));
        tree.Rows[0].Press();

        Assert.Equal(["src"], events);
        Assert.Equal(["src", "app.cs", "lib", "readme.md", "locked"], tree.VisibleNodes().Select(n => n.Item.Label));
    }

    [Fact]
    public void A_node_is_selected_by_its_value_or_else_its_label_and_a_disabled_one_cannot_be_selected()
    {
        var tree = Tree();

        tree.Rows[1].Press();
        Assert.Equal("src/app.cs", tree.Value);

        tree.Rows[4].Press();
        Assert.Equal("src/app.cs", tree.Value);
        Assert.False(tree.Rows[4].IsEnabled);
        Assert.Equal(0.6, tree.Rows[4].Opacity);
    }

    [Fact]
    public void The_selection_goes_both_ways_and_marks_the_row()
    {
        var tree = Tree();
        var events = new List<string?>();
        tree.ValueChanged += (_, e) => events.Add(e.Value);

        tree.Value = "readme.md";

        Assert.Equal(["readme.md"], events);
        // The selected row has a tint of the color (a tenth of its fill); the others have none.
        Assert.Equal(0.1f, tree.Rows[3].Face.BackgroundColor.Alpha, 3);
        Assert.Equal(0f, tree.Rows[0].Face.BackgroundColor.Alpha, 3);
    }

    [Fact]
    public void A_parent_row_has_a_chevron_that_turns_when_expanded_and_a_leaf_has_room_for_one()
    {
        var tree = Tree();

        var parent = (HorizontalStackLayout)tree.Rows[0].Face.Content!;
        Assert.Equal(90, Assert.IsType<KlyIcon>(parent.Children[0]).Rotation);
        var collapsed = (HorizontalStackLayout)tree.Rows[2].Face.Content!;
        Assert.Equal(0, Assert.IsType<KlyIcon>(collapsed.Children[0]).Rotation);
        var leaf = (HorizontalStackLayout)tree.Rows[1].Face.Content!;
        Assert.IsType<BoxView>(leaf.Children[0]);
        Assert.True(tree.Rows[1].MinimumHeightRequest >= 40);
    }

    [Fact]
    public void A_tree_icon_and_the_accessible_names_are_set()
    {
        var tree = new KlyTree { Items = [new KlyTreeItem("file") { Icon = KlyIconNames.File }], AccessibleName = "Files" };

        Assert.Equal("Files", DescriptionOf(tree));
        Assert.Equal("file", DescriptionOf(tree.Rows[0]));
        Assert.Contains(All<KlyIcon>(tree.Rows[0]), i => i.Name == KlyIconNames.File);
    }

    // ----- KlyMarquee -----

    [Fact]
    public void Marquee_has_the_defaults_of_the_web_component()
    {
        var marquee = new KlyMarquee();

        Assert.Equal(StackOrientation.Horizontal, marquee.Orientation);
        Assert.False(marquee.Reverse);
        Assert.True(marquee.PauseOnHover);
        Assert.True(marquee.Overlay);
        Assert.Equal(30, marquee.Duration);
        Assert.Equal(1, marquee.Repeat);
        Assert.False(marquee.IsPaused);
        Assert.True(marquee.IsClippedToBounds);
    }

    [Theory]
    [InlineData(0, 100, 50, 10, true, false, 0)]
    [InlineData(0.5, 100, 50, 10, true, false, -30)]
    [InlineData(0, 100, 50, 10, true, true, -60)]
    [InlineData(1, 100, 50, 10, true, true, 0)]
    [InlineData(0, 200, 80, 10, false, false, 200)]
    [InlineData(1, 200, 80, 10, false, false, -80)]
    [InlineData(0, 200, 80, 10, false, true, -80)]
    public void The_track_moves_by_one_row_in_a_seamless_loop_and_across_the_control_for_a_single_view(double progress, double container, double group, double gap, bool seamless, bool reverse, double expected) =>
        Assert.Equal(expected, KlyMarquee.Position(progress, container, group, gap, seamless, reverse), 6);

    [Fact]
    public void A_single_view_is_one_group_and_a_template_builds_a_copy_for_each_repeat()
    {
        var single = new KlyMarquee { Content = new Label { Text = "x" } };
        Assert.Single(single.Groups);
        Assert.False(single.Seamless);

        var seamless = new KlyMarquee { ContentTemplate = new DataTemplate(() => new Label { Text = "logo" }), Repeat = 2 };
        Assert.Equal(3, seamless.Groups.Count);
        Assert.True(seamless.Seamless);
        Assert.True(AutomationProperties.GetIsInAccessibleTree(seamless.Groups[0]) is null or true);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(seamless.Groups[1]));
        Assert.False(AutomationProperties.GetIsInAccessibleTree(seamless.Groups[2]));
        Assert.Equal(16, ((HorizontalStackLayout)seamless.Track).Spacing);
    }

    [Fact]
    public void A_vertical_marquee_uses_a_vertical_track_and_the_gap_is_the_spacing()
    {
        var marquee = new KlyMarquee { Orientation = StackOrientation.Vertical, Gap = 8, ContentTemplate = new DataTemplate(() => new Label()) };

        var track = Assert.IsType<VerticalStackLayout>(marquee.Track);
        Assert.Equal(8, track.Spacing);
        Assert.All(marquee.Fades, fade => Assert.Equal(KlyMarquee.FadeLength, fade.HeightRequest));
    }

    [Fact]
    public void The_fades_follow_overlay_and_sit_at_the_two_edges()
    {
        var marquee = new KlyMarquee();

        Assert.All(marquee.Fades, fade => Assert.True(fade.IsVisible));
        Assert.Equal([LayoutOptions.Start, LayoutOptions.End], marquee.Fades.Select(f => f.HorizontalOptions));
        Assert.All(marquee.Fades, fade => Assert.Equal(KlyMarquee.FadeLength, fade.WidthRequest));
        Assert.All(marquee.Fades, fade => Assert.True(fade.InputTransparent));

        marquee.Overlay = false;
        Assert.All(marquee.Fades, fade => Assert.False(fade.IsVisible));
    }

    [Fact]
    public void The_marquee_moves_with_time_and_stops_while_paused_or_hovered()
    {
        var marquee = new KlyMarquee { ContentTemplate = new DataTemplate(() => new Label()), Duration = 10, Gap = 0 };
        marquee.Measured = (100, 50);

        marquee.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(-25, marquee.Track.TranslationX, 6);

        marquee.IsPaused = true;
        marquee.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(-25, marquee.Track.TranslationX, 6);

        marquee.IsPaused = false;
        marquee.IsHovered = true;
        marquee.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(-25, marquee.Track.TranslationX, 6);

        marquee.PauseOnHover = false;
        marquee.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(-30, marquee.Track.TranslationX, 6);
    }

    [Fact]
    public void A_vertical_marquee_moves_the_track_up_and_reverse_goes_the_other_way()
    {
        var marquee = new KlyMarquee { Orientation = StackOrientation.Vertical, ContentTemplate = new DataTemplate(() => new Label()), Duration = 10, Gap = 0, Reverse = true };
        marquee.Measured = (100, 50);

        marquee.Advance(TimeSpan.FromSeconds(2.5));

        Assert.Equal(-37.5, marquee.Track.TranslationY, 6);
        Assert.Equal(0, marquee.Track.TranslationX);
    }

    // ----- KlyScrollArea -----

    [Fact]
    public void Scroll_area_scrolls_vertically_by_default_and_has_an_accessible_name()
    {
        var area = new KlyScrollArea { AccessibleName = "Log" };

        Assert.IsAssignableFrom<ScrollView>(area);
        Assert.Equal(ScrollOrientation.Vertical, area.Orientation);
        Assert.Equal("Log", DescriptionOf(area));

        area.Orientation = ScrollOrientation.Both;
        Assert.Equal(ScrollOrientation.Both, area.Orientation);
    }

    // ----- KlySplitter -----

    private static KlySplitter TwoPanels() => new() { Panels = [new KlySplitterPanel { Content = new Label() }, new KlySplitterPanel { Content = new Label() }] };

    [Fact]
    public void Panels_without_a_size_share_what_the_others_leave()
    {
        Assert.Equal([30, 35, 35], KlySplitter.InitialSizes([new KlySplitterPanel { Size = 30 }, new KlySplitterPanel(), new KlySplitterPanel()]));
        Assert.Equal([50, 50], KlySplitter.InitialSizes([new KlySplitterPanel(), new KlySplitterPanel()]));
        Assert.Equal([60, 40], KlySplitter.InitialSizes([new KlySplitterPanel { Size = 60 }, new KlySplitterPanel { Size = 40 }]));
    }

    [Fact]
    public void The_range_of_a_panel_follows_its_own_limits_and_those_of_its_neighbor()
    {
        KlySplitterPanel[] panels = [new() { MinSize = 20, MaxSize = 80 }, new() { MinSize = 30, MaxSize = 100 }];

        Assert.Equal((20d, 70d), KlySplitter.Range(panels, [50, 50], 0));
    }

    [Fact]
    public void A_horizontal_splitter_has_a_column_per_panel_and_handle_and_a_vertical_one_a_row()
    {
        var splitter = TwoPanels();

        Assert.Equal(3, splitter.ColumnDefinitions.Count);
        Assert.Equal(new GridLength(50, GridUnitType.Star), splitter.ColumnDefinitions[0].Width);
        Assert.Equal(new GridLength(KlySplitter.HandleThickness), splitter.ColumnDefinitions[1].Width);
        Assert.Equal(40, KlySplitter.HandleThickness);
        Assert.Empty(splitter.RowDefinitions);

        splitter.Orientation = StackOrientation.Vertical;
        Assert.Equal(3, splitter.RowDefinitions.Count);
        Assert.Empty(splitter.ColumnDefinitions);
    }

    [Fact]
    public void A_drag_moves_the_border_and_reports_the_sizes_once_when_it_ends()
    {
        var splitter = TwoPanels();
        var events = new List<double[]>();
        splitter.SizesChanged += (_, e) => events.Add(e.Value!.ToArray());

        splitter.BeginDrag();
        splitter.Drag(0, 40, 440);
        splitter.Drag(0, 80, 440);
        Assert.Empty(events);
        Assert.Equal([70d, 30d], splitter.CurrentSizes);
        Assert.Equal(new GridLength(70, GridUnitType.Star), splitter.ColumnDefinitions[0].Width);

        splitter.EndDrag();
        Assert.Single(events);
        Assert.Equal([70d, 30d], events[0]);
        Assert.Equal([70d, 30d], splitter.Sizes);
    }

    [Fact]
    public void A_drag_stops_at_the_limits_of_the_panels()
    {
        var splitter = new KlySplitter { Panels = [new KlySplitterPanel { MinSize = 40, MaxSize = 60 }, new KlySplitterPanel { MaxSize = 55 }] };

        splitter.BeginDrag();
        splitter.Drag(0, -400, 440);
        Assert.Equal([45d, 55d], splitter.CurrentSizes);
        splitter.Drag(0, 400, 440);
        Assert.Equal([60d, 40d], splitter.CurrentSizes);
    }

    [Fact]
    public void Resize_and_nudge_move_the_border_by_a_distance_or_one_step_and_report_it()
    {
        var splitter = TwoPanels();
        var events = new List<double[]>();
        splitter.SizesChanged += (_, e) => events.Add(e.Value!.ToArray());

        splitter.Resize(0, 10);
        splitter.Nudge(0, -1);

        Assert.Equal([55d, 45d], splitter.CurrentSizes);
        Assert.Equal(2, events.Count);
        Assert.Equal([60d, 40d], events[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => splitter.Resize(1, 5));
    }

    [Fact]
    public void Sizes_given_by_the_app_replace_the_initial_ones_when_they_match_the_panels()
    {
        var splitter = TwoPanels();

        splitter.Sizes = [25, 75];
        Assert.Equal([25d, 75d], splitter.CurrentSizes);

        splitter.Sizes = [10, 20, 70];
        Assert.Equal([50d, 50d], splitter.CurrentSizes);
    }

    [Fact]
    public void A_handle_has_a_name_that_says_which_one_it_is_and_a_grip()
    {
        var two = TwoPanels();
        Assert.Equal("Resize panels", DescriptionOf(two.Handles[0]));

        var three = new KlySplitter { Panels = [new KlySplitterPanel(), new KlySplitterPanel(), new KlySplitterPanel()], ResizeLabel = "Redimensionner" };
        Assert.Equal(["Redimensionner 1", "Redimensionner 2"], three.Handles.Select(DescriptionOf));
        Assert.Contains(All<KlyIcon>(three.Handles[0]), i => i.Name == KlyIconNames.Drag);
    }

    // ----- KlyCarousel -----

    private static KlyCarousel ThreeSlides() => new()
    {
        Items = [new KlyCarouselItem(new Label { Text = "a" }), new KlyCarouselItem(new Label { Text = "b" }) { Label = "Second" }, new KlyCarouselItem(new Label { Text = "c" })],
    };

    [Theory]
    [InlineData(0, 1, 3, false, 1)]
    [InlineData(2, 1, 3, false, 2)]
    [InlineData(0, -1, 3, false, 0)]
    [InlineData(2, 1, 3, true, 0)]
    [InlineData(0, -1, 3, true, 2)]
    [InlineData(0, 1, 0, true, 0)]
    public void A_step_stops_at_the_ends_unless_the_carousel_loops(int current, int delta, int count, bool loop, int expected) =>
        Assert.Equal(expected, KlyCarousel.Step(current, delta, count, loop));

    [Fact]
    public void Carousel_has_the_defaults_of_the_web_component_with_a_slide_and_a_dot_per_item()
    {
        var carousel = ThreeSlides();

        Assert.Equal(0, carousel.Index);
        Assert.False(carousel.Loop);
        Assert.Equal(0, carousel.AutoPlay);
        Assert.True(carousel.Dots);
        Assert.True(carousel.Arrows);
        Assert.Equal(3, carousel.Slides.Count);
        Assert.Equal(3, carousel.DotButtons.Count);
        Assert.NotNull(carousel.PreviousButton);
        Assert.NotNull(carousel.NextButton);
        Assert.Null(carousel.ToggleButton);
    }

    [Fact]
    public void The_dots_and_the_arrows_can_be_left_out_and_autoplay_adds_a_pause_button()
    {
        var carousel = ThreeSlides();

        carousel.Dots = false;
        carousel.Arrows = false;
        Assert.Empty(carousel.DotButtons);
        Assert.Null(carousel.PreviousButton);

        carousel.AutoPlay = 3000;
        Assert.NotNull(carousel.ToggleButton);
        Assert.Equal("Pause automatic slide show", DescriptionOf(carousel.ToggleButton!));
    }

    [Fact]
    public void The_arrows_move_one_slide_with_one_event_and_the_ends_disable_them_unless_looping()
    {
        var carousel = ThreeSlides();
        var events = new List<int>();
        carousel.IndexChanged += (_, index) => events.Add(index);
        Assert.False(carousel.PreviousButton!.IsEnabled);

        carousel.NextButton!.Press();
        carousel.NextButton.Press();
        carousel.NextButton.Press();

        Assert.Equal(2, carousel.Index);
        Assert.Equal([1, 2], events);
        Assert.False(carousel.NextButton.IsEnabled);

        carousel.Loop = true;
        Assert.True(carousel.NextButton!.IsEnabled);
        carousel.NextButton.Press();
        Assert.Equal(0, carousel.Index);
    }

    [Fact]
    public void A_dot_goes_to_its_slide_and_the_index_goes_both_ways()
    {
        var carousel = ThreeSlides();
        var events = new List<int>();
        carousel.IndexChanged += (_, index) => events.Add(index);

        carousel.DotButtons[2].Press();
        Assert.Equal(2, carousel.Index);

        carousel.Index = 1;
        Assert.Equal([2, 1], events);
        Assert.Equal(1, carousel.Current);
    }

    [Fact]
    public void The_current_dot_is_ringed_and_the_labels_say_where_each_control_goes()
    {
        var carousel = ThreeSlides();
        carousel.Index = 1;

        Assert.Equal([1d, 1d, 1d], carousel.DotButtons.Select(d => d.Face.StrokeThickness));
        Assert.Equal("Go to slide 2", DescriptionOf(carousel.DotButtons[1]));
        Assert.Equal("Previous slide", DescriptionOf(carousel.PreviousButton!));
        Assert.Equal("Next slide", DescriptionOf(carousel.NextButton!));
        Assert.Equal("1 of 3", DescriptionOf(carousel.Slides[0]));
        Assert.Equal("Second", DescriptionOf(carousel.Slides[1]));
        Assert.True(carousel.DotButtons[0].MinimumHeightRequest >= 40 && carousel.NextButton!.MinimumHeightRequest >= 40);

        carousel.AccessibleName = "Photos";
        Assert.Equal("Photos", DescriptionOf(carousel));
    }

    [Fact]
    public void The_slides_take_the_width_of_the_viewport_and_the_track_follows_the_index()
    {
        var carousel = ThreeSlides();

        carousel.ApplyWidth(300);
        Assert.All(carousel.Slides, slide => Assert.Equal(300, slide.WidthRequest));
        Assert.Equal(0, carousel.Track.TranslationX);

        carousel.Index = 2;
        Assert.Equal(-600, carousel.Track.TranslationX);
    }

    [Fact]
    public void A_pull_of_a_quarter_of_the_width_moves_to_the_next_slide_and_less_settles_back()
    {
        var carousel = ThreeSlides();
        carousel.ApplyWidth(300);

        carousel.BeginPan();
        carousel.Drag(-100);
        Assert.Equal(-100, carousel.Track.TranslationX);
        carousel.Release(-100);
        Assert.Equal(1, carousel.Index);
        Assert.Equal(-300, carousel.Track.TranslationX);

        carousel.BeginPan();
        carousel.Drag(40);
        carousel.Release(40);
        Assert.Equal(1, carousel.Index);
        Assert.Equal(-300, carousel.Track.TranslationX);
    }

    [Fact]
    public void The_first_slide_resists_a_pull_without_loop_and_a_loop_goes_round()
    {
        var carousel = ThreeSlides();
        carousel.ApplyWidth(300);

        carousel.BeginPan();
        carousel.Drag(90);
        Assert.Equal(30, carousel.Track.TranslationX);
        carousel.Release(90);
        Assert.Equal(0, carousel.Index);

        carousel.Loop = true;
        carousel.BeginPan();
        carousel.Drag(200);
        carousel.Release(200);
        Assert.Equal(2, carousel.Index);
    }

    [Fact]
    public void An_automatic_step_stops_on_the_last_slide_without_loop_and_wraps_with_it()
    {
        var carousel = ThreeSlides();
        carousel.AutoPlay = 1000;

        carousel.Tick();
        carousel.Tick();
        carousel.Tick();
        Assert.Equal(2, carousel.Index);

        carousel.Loop = true;
        carousel.Tick();
        Assert.Equal(0, carousel.Index);
    }

    [Fact]
    public void The_pause_button_stops_autoplay_and_turns_into_a_play_button()
    {
        var carousel = ThreeSlides();
        carousel.AutoPlay = 1000;

        carousel.ToggleButton!.Press();
        carousel.Tick();

        Assert.Equal(0, carousel.Index);
        Assert.False(carousel.AutoPlayRunning);
        Assert.Equal("Start automatic slide show", DescriptionOf(carousel.ToggleButton));
        Assert.Equal(KlyIconNames.Play, ((KlyIcon)carousel.ToggleButton.Face.Content!).Name);

        carousel.ToggleButton.Press();
        carousel.Tick();
        Assert.Equal(1, carousel.Index);
        Assert.Equal(KlyIconNames.Pause, ((KlyIcon)carousel.ToggleButton.Face.Content!).Name);
    }

    // ----- KlyCulture and KlyLocaleSelect -----

    [Fact]
    public void Setting_a_culture_applies_it_to_the_app_and_stores_its_name()
    {
        KlyCulture.Set("fr");

        Assert.Equal("fr", CultureInfo.DefaultThreadCurrentCulture!.Name);
        Assert.Equal("fr", CultureInfo.DefaultThreadCurrentUICulture!.Name);
        Assert.Equal("fr", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("fr", KlyCulture.Stored);
    }

    [Fact]
    public void The_stored_culture_is_applied_at_startup_and_an_unknown_one_is_refused_or_ignored()
    {
        KlyCulture.Set("de");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        KlyCulture.ApplyStored();
        Assert.Equal("de", CultureInfo.CurrentCulture.Name);

        Assert.Throws<ArgumentException>(() => KlyCulture.Set("not a culture!"));
        Assert.Equal("de", KlyCulture.Stored);
    }

    [Fact]
    public void Applying_with_nothing_stored_changes_nothing()
    {
        var before = CultureInfo.CurrentCulture;

        KlyCulture.ApplyStored();

        Assert.Equal(before, CultureInfo.CurrentCulture);
    }

    [Fact]
    public void The_locale_select_lists_the_locales_of_klyroo_by_name()
    {
        var select = new KlyLocaleSelect();

        var options = Assert.IsAssignableFrom<IList<KlyLocaleOption>>(select.ItemsSource);
        Assert.Equal(["en", "fr", "de", "es", "it", "pt", "pt-BR", "nl", "ja", "zh-Hans", "ar", "ru"], options.Select(o => o.Code));
        Assert.Equal("Français", options[1].ToString());
        Assert.Equal("Language", DescriptionOf(select.Content!));
        Assert.Equal(160, select.MinimumWidthRequest);
    }

    [Theory]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-PT", "pt")]
    [InlineData("fr-CA", "fr")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("FR", "fr")]
    [InlineData("xx", null)]
    public void A_culture_resolves_to_the_same_code_or_else_the_first_locale_of_its_language(string code, string? expected) =>
        Assert.Equal(expected, KlyLocaleSelect.Resolve(KlyLocaleOption.All, code)?.Code);

    [Fact]
    public void The_locale_select_starts_on_the_locale_of_the_current_culture_without_storing_anything()
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-CA");

        var select = new KlyLocaleSelect();

        Assert.Equal("fr", select.Value);
        Assert.Equal("Français", select.SelectedItem?.ToString());
        Assert.Null(KlyCulture.Stored);
    }

    [Fact]
    public void Choosing_a_locale_applies_it_stores_it_and_raises_one_event()
    {
        var select = new KlyLocaleSelect();
        var picker = (Picker)select.Content!;
        var codes = new List<string>();
        select.LocaleChanged += (_, code) => codes.Add(code);

        picker.SelectedIndex = 3;

        Assert.Equal("es", select.Value);
        Assert.Equal("es", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("es", KlyCulture.Stored);
        Assert.Equal(["es"], codes);
    }

    [Fact]
    public void The_locale_goes_both_ways()
    {
        var select = new KlyLocaleSelect();
        var codes = new List<string>();
        select.LocaleChanged += (_, code) => codes.Add(code);

        select.Value = "ja";

        Assert.Equal("日本語", select.SelectedItem?.ToString());
        Assert.Equal("ja", CultureInfo.DefaultThreadCurrentCulture!.Name);
        Assert.Equal(["ja"], codes);
    }

    [Fact]
    public void A_custom_list_of_locales_and_a_flag_are_what_the_select_offers()
    {
        var select = new KlyLocaleSelect
        {
            Locales = [new KlyLocaleOption("en", "English") { Flag = "EN" }, new KlyLocaleOption("fr", "Français")],
        };

        var options = Assert.IsAssignableFrom<IList<KlyLocaleOption>>(select.ItemsSource);
        Assert.Equal(["EN English", "Français"], options.Select(o => o.ToString()));
        Assert.Equal(2, options.Count);
    }

    // ----- KlyColorMode -----

    [Fact]
    public void The_color_mode_follows_the_system_until_the_user_chooses()
    {
        Assert.Equal(AppTheme.Unspecified, KlyColorMode.Current);
        Assert.Equal("klyroo-color-mode", KlyColorMode.PreferenceKey);
    }

    [Fact]
    public void Choosing_a_mode_stores_it_and_raises_one_event_per_change()
    {
        var events = 0;
        KlyColorMode.Changed += Count;
        try
        {
            KlyColorMode.Set(AppTheme.Dark);
            KlyColorMode.Set(AppTheme.Dark);
            Assert.Equal(AppTheme.Dark, KlyColorMode.Current);
            Assert.Equal("dark", NavigationPreferences.Get(KlyColorMode.PreferenceKey));
            Assert.Equal(1, events);

            KlyColorMode.Set(AppTheme.Unspecified);
            Assert.Equal("system", NavigationPreferences.Get(KlyColorMode.PreferenceKey));
            Assert.Equal(2, events);
        }
        finally
        {
            KlyColorMode.Changed -= Count;
        }

        void Count(object? sender, EventArgs e) => events++;
    }

    [Theory]
    [InlineData("light", AppTheme.Light)]
    [InlineData("dark", AppTheme.Dark)]
    [InlineData("system", AppTheme.Unspecified)]
    [InlineData("whatever", AppTheme.Unspecified)]
    public void The_stored_mode_is_read_from_the_preferences(string stored, AppTheme expected)
    {
        NavigationPreferences.Set(KlyColorMode.PreferenceKey, stored);
        KlyColorMode.Reset();
        NavigationPreferences.Set(KlyColorMode.PreferenceKey, stored);

        Assert.Equal(expected, KlyColorMode.Current);
    }

    [Fact]
    public void Choosing_a_mode_sets_the_theme_of_the_app_and_the_stored_one_is_applied_at_startup()
    {
        var app = new Application();
        Application.Current = app;

        KlyColorMode.Set(AppTheme.Dark);
        Assert.Equal(AppTheme.Dark, app.UserAppTheme);
        Assert.True(KlyColorMode.IsDark);

        app.UserAppTheme = AppTheme.Unspecified;
        KlyColorMode.ApplyStored();
        Assert.Equal(AppTheme.Dark, app.UserAppTheme);

        KlyColorMode.Toggle();
        Assert.Equal(AppTheme.Light, KlyColorMode.Current);
        Assert.False(KlyColorMode.IsDark);
    }

    // ----- KlyColorModeButton -----

    [Fact]
    public void The_color_mode_button_is_a_neutral_ghost_icon_button()
    {
        var button = new KlyColorModeButton();

        Assert.Equal(KlyColor.Neutral, button.Color);
        Assert.Equal(ButtonVariant.Ghost, button.Variant);
        Assert.Equal(KlySize.Md, button.Size);
        Assert.Equal(40, button.Button.Face.WidthRequest);
        Assert.Equal(40, button.Button.Face.HeightRequest);
        Assert.Equal("Toggle dark mode", DescriptionOf(button.Button));
        Assert.Equal(KlyIconNames.Dark, button.Moon.Name);
        Assert.Equal(KlyIconNames.Light, button.Sun.Name);
        Assert.False(button.TextLabel.IsVisible);
    }

    [Theory]
    [InlineData(KlySize.Xs, 28)]
    [InlineData(KlySize.Sm, 32)]
    [InlineData(KlySize.Md, 40)]
    [InlineData(KlySize.Lg, 44)]
    [InlineData(KlySize.Xl, 48)]
    public void The_icon_only_button_is_square_at_the_height_of_the_button_size_and_keeps_a_touch_target(KlySize size, double height)
    {
        var button = new KlyColorModeButton { Size = size };

        Assert.Equal(height, button.Button.Face.WidthRequest);
        Assert.Equal(height, button.Button.Face.HeightRequest);
        Assert.Equal(size, button.Moon.Size);
        Assert.True(button.Button.MinimumHeightRequest >= 40 && button.Button.MinimumWidthRequest >= 40);
    }

    [Fact]
    public void A_label_makes_the_button_as_wide_as_its_content_and_variant_changes_the_border()
    {
        var button = new KlyColorModeButton { Label = "Theme" };

        Assert.Equal(-1, button.Button.Face.WidthRequest);
        Assert.True(button.TextLabel.IsVisible);
        Assert.Equal("Theme", button.TextLabel.Text);
        Assert.Null(DescriptionOf(button.Button));
        Assert.Equal(0, button.Button.Face.StrokeThickness);

        button.Variant = ButtonVariant.Outline;
        Assert.Equal(1, button.Button.Face.StrokeThickness);
    }

    [Fact]
    public void The_color_mode_button_dims_while_disabled_and_toggles_the_mode_when_pressed()
    {
        var button = new KlyColorModeButton();

        button.Button.Press();
        Assert.Equal(AppTheme.Dark, KlyColorMode.Current);

        button.IsEnabled = false;
        Assert.False(button.Button.IsEnabled);
        Assert.Equal(0.6, button.Button.Opacity);
        KlyColorMode.Set(AppTheme.Light);
        button.Button.Press();
        Assert.Equal(AppTheme.Light, KlyColorMode.Current);
    }

    [Theory]
    [InlineData(AppTheme.Light, true)]
    [InlineData(AppTheme.Dark, false)]
    public void The_moon_shows_in_the_light_theme_and_the_sun_in_the_dark_one(AppTheme theme, bool moon)
    {
        Application.Current = new Application { UserAppTheme = theme };

        var button = new KlyColorModeButton();

        Assert.Equal(moon, button.Moon.IsVisible);
        Assert.Equal(!moon, button.Sun.IsVisible);
    }

    // ----- KlyColorModeSwitch -----

    [Fact]
    public void The_color_mode_switch_sets_the_mode_when_it_is_toggled()
    {
        var toggle = new KlyColorModeSwitch();
        Assert.False(toggle.IsToggled);

        toggle.IsToggled = true;
        Assert.Equal(AppTheme.Dark, KlyColorMode.Current);

        toggle.IsToggled = false;
        Assert.Equal(AppTheme.Light, KlyColorMode.Current);
    }

    [Fact]
    public void The_color_mode_switch_follows_the_theme_of_the_app_without_changing_the_choice()
    {
        var app = new Application { UserAppTheme = AppTheme.Dark };
        Application.Current = app;
        var toggle = new KlyColorModeSwitch();

        Assert.True(toggle.IsToggled);
        Assert.Equal(AppTheme.Unspecified, KlyColorMode.Current);

        app.UserAppTheme = AppTheme.Light;
        toggle.Sync();
        Assert.False(toggle.IsToggled);
        Assert.Equal(AppTheme.Unspecified, KlyColorMode.Current);
    }

    [Fact]
    public void The_color_mode_switch_label_is_its_text_and_the_accessible_name_covers_for_it()
    {
        var toggle = new KlyColorModeSwitch();
        Assert.Equal("Toggle dark mode", DescriptionOf(toggle));

        toggle.Label = "Dark mode";
        Assert.Equal("Dark mode", toggle.Text);
        Assert.Null(DescriptionOf(toggle));

        toggle.IsEnabled = false;
        Assert.Equal(0.6, toggle.Opacity);
    }

    // ----- KlyColorModeSelect -----

    [Fact]
    public void The_color_mode_select_offers_system_light_and_dark_and_starts_on_the_stored_mode()
    {
        KlyColorMode.Set(AppTheme.Light);

        var select = new KlyColorModeSelect();

        var picker = (Picker)select.Content!;
        Assert.Equal(["System", "Light", "Dark"], picker.ItemsSource.Cast<object>().Select(o => o.ToString()));
        Assert.Equal("Light", select.SelectedItem?.ToString());
        Assert.Equal(AppTheme.Light, select.SelectedMode);
        Assert.Equal("Color mode", DescriptionOf(picker));
    }

    [Fact]
    public void Choosing_in_the_color_mode_select_sets_the_mode_once()
    {
        var select = new KlyColorModeSelect();
        var changes = 0;
        KlyColorMode.Changed += Count;
        try
        {
            ((Picker)select.Content!).SelectedIndex = 2;
        }
        finally
        {
            KlyColorMode.Changed -= Count;
        }

        Assert.Equal(AppTheme.Dark, KlyColorMode.Current);
        Assert.Equal(1, changes);

        void Count(object? sender, EventArgs e) => changes++;
    }

    [Fact]
    public void The_color_mode_select_follows_a_change_made_elsewhere_and_the_labels_can_be_replaced()
    {
        var select = new KlyColorModeSelect();
        Assert.Equal(AppTheme.Unspecified, select.SelectedMode);

        KlyColorMode.Set(AppTheme.Dark);
        select.Sync();
        Assert.Equal(AppTheme.Dark, select.SelectedMode);

        select.SystemLabel = "Système";
        select.DarkLabel = "Sombre";
        Assert.Equal(["Système", "Light", "Sombre"], ((Picker)select.Content!).ItemsSource.Cast<object>().Select(o => o.ToString()));
        Assert.Equal("Sombre", select.SelectedItem?.ToString());
    }

    // ----- KlyColorModeAvatar and KlyColorModeImage -----

    [Theory]
    [InlineData(AppTheme.Light, "light.png")]
    [InlineData(AppTheme.Dark, "dark.png")]
    public void The_color_mode_avatar_shows_the_picture_of_the_theme(AppTheme theme, string expected)
    {
        Application.Current = new Application { UserAppTheme = theme };

        var avatar = new KlyColorModeAvatar { Light = ImageSource.FromFile("light.png"), Dark = ImageSource.FromFile("dark.png"), Text = "Ada Lovelace" };

        Assert.Equal(expected, Assert.IsType<FileImageSource>(avatar.Source).File);
        Assert.IsAssignableFrom<KlyAvatar>(avatar);
    }

    [Fact]
    public void The_dark_picture_of_the_avatar_defaults_to_the_light_one_and_no_picture_clears_it()
    {
        var app = new Application { UserAppTheme = AppTheme.Dark };
        Application.Current = app;
        var light = ImageSource.FromFile("light.png");
        var avatar = new KlyColorModeAvatar { Light = light };

        Assert.Same(light, avatar.Source);

        avatar.Light = null;
        Assert.Null(avatar.Source);
    }

    [Theory]
    [InlineData(AppTheme.Light, "logo-light.png")]
    [InlineData(AppTheme.Dark, "logo-dark.png")]
    public void The_color_mode_image_shows_the_picture_of_the_theme(AppTheme theme, string expected)
    {
        Application.Current = new Application { UserAppTheme = theme };

        var image = new KlyColorModeImage { Light = ImageSource.FromFile("logo-light.png"), Dark = ImageSource.FromFile("logo-dark.png") };

        Assert.Equal(expected, Assert.IsType<FileImageSource>(image.Source).File);
    }

    [Fact]
    public void The_color_mode_image_is_decorative_until_it_has_an_alt()
    {
        var image = new KlyColorModeImage { Light = ImageSource.FromFile("logo.png") };
        Assert.False(AutomationProperties.GetIsInAccessibleTree(image));

        image.Alt = "Klyroo";
        Assert.True(AutomationProperties.GetIsInAccessibleTree(image));
        Assert.Equal("Klyroo", DescriptionOf(image));
    }

    [Fact]
    public void The_dark_image_defaults_to_the_light_one()
    {
        var light = ImageSource.FromFile("logo.png");
        var image = new KlyColorModeImage { Light = light };

        Assert.Same(light, image.Source);
        Assert.Same(light, image.Light);
        Assert.Null(image.Dark);
    }
}
