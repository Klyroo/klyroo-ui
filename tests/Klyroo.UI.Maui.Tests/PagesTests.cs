using System.Globalization;

namespace Klyroo.UI.Maui.Tests;

public class PagesTests
{
    // A link that the test handles itself: nothing opens, and every followed link is recorded.
    private static List<PageLinkEventArgs> Record(Action<EventHandler<PageLinkEventArgs>> subscribe)
    {
        var raised = new List<PageLinkEventArgs>();
        subscribe((_, e) =>
        {
            raised.Add(e);
            e.Handled = true;
        });
        return raised;
    }

    private static IEnumerable<T> Descendants<T>(Element root) where T : Element
    {
        foreach (var child in ((IVisualTreeElement)root).GetVisualChildren().OfType<Element>())
        {
            if (child is T match) yield return match;
            foreach (var inner in Descendants<T>(child)) yield return inner;
        }
    }

    private static void Tap(View view)
    {
        var tap = view.GestureRecognizers.OfType<TapGestureRecognizer>().First();
        typeof(TapGestureRecognizer).GetMethod("SendTapped", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(tap, [view, null]);
    }

    // ---- KlyPage

    [Fact]
    public void Page_stacks_the_columns_below_1024_and_spans_a_ten_column_grid_from_1024()
    {
        var left = new BoxView();
        var center = new BoxView();
        var right = new BoxView();
        var page = new KlyPage { Left = left, Center = center, Right = right };

        page.Relayout(800);
        Assert.False(page.IsWide);
        Assert.Equal([0, 1, 2], new[] { left, center, right }.Select(Grid.GetRow));

        page.Relayout(1200);
        Assert.Equal(10, page.ColumnDefinitions.Count);
        Assert.Equal((0, 2), (Grid.GetColumn(left), Grid.GetColumnSpan(left)));
        Assert.Equal((2, 6), (Grid.GetColumn(center), Grid.GetColumnSpan(center)));
        Assert.Equal((8, 2), (Grid.GetColumn(right), Grid.GetColumnSpan(right)));
    }

    [Theory]
    [InlineData(true, true, 6)]
    [InlineData(true, false, 8)]
    [InlineData(false, true, 8)]
    [InlineData(false, false, 10)]
    public void Page_center_takes_the_columns_the_sides_leave(bool hasLeft, bool hasRight, int span)
    {
        var center = new BoxView();
        var page = new KlyPage { Center = center, Left = hasLeft ? new BoxView() : null, Right = hasRight ? new BoxView() : null };
        page.Relayout(1100);

        Assert.Equal(span, page.CenterSpan);
        Assert.Equal(span, Grid.GetColumnSpan(center));
    }

    // ---- KlyPageHeader

    [Fact]
    public void PageHeader_shows_only_the_texts_it_has_and_sets_the_heading_level()
    {
        var header = new KlyPageHeader { Title = "Intro", Headline = "Docs" };
        var labels = ((VerticalStackLayout)((Grid)header.Content!).Children[0]).Children.OfType<Label>().ToList();

        Assert.True(labels[0].IsVisible);
        Assert.Equal("DOCS", labels[0].Text.ToUpperInvariant());
        Assert.Equal(TextTransform.Uppercase, labels[0].TextTransform);
        Assert.True(labels[1].IsVisible);
        Assert.False(labels[2].IsVisible);
        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(header.TitleLabel));

        header.HeadingLevel = 2;
        header.Description = "Text";
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(header.TitleLabel));
        Assert.True(labels[2].IsVisible);
    }

    [Fact]
    public void PageHeader_title_grows_from_640_and_the_links_are_buttons_that_report_themselves()
    {
        var header = new KlyPageHeader { Title = "Intro", Links = [new PageLinkItem("Start", "/start") { Color = KlyColor.Secondary, Variant = ButtonVariant.Soft }, new PageLinkItem("Off") { Disabled = true }] };
        var raised = Record(h => header.LinkClicked += h);

        header.Relayout(400);
        Assert.Equal(30, header.TitleLabel.FontSize);
        header.Relayout(700);
        Assert.Equal(36, header.TitleLabel.FontSize);

        var buttons = header.LinkRow.Children.OfType<KlyButton>().ToList();
        Assert.Equal(2, buttons.Count);
        Assert.Equal(KlyColor.Secondary, buttons[0].Color);
        Assert.Equal(ButtonVariant.Soft, buttons[0].Variant);
        Assert.Equal(KlySize.Md, buttons[0].Size);
        Assert.False(buttons[1].IsEnabled);

        buttons[0].SendClicked();
        var link = Assert.Single(raised);
        Assert.Equal("/start", link.Href);
        Assert.Equal("Start", Assert.IsType<PageLinkItem>(link.Item).Label);
    }

    // ---- KlyPageHero

    [Theory]
    [InlineData(400, 48, 1)]
    [InlineData(700, 60, 1)]
    [InlineData(1200, 72, 2)]
    public void PageHero_title_and_columns_follow_the_width(double width, double titleSize, int columns)
    {
        var hero = new KlyPageHero { Title = "Build", Media = new BoxView() };
        hero.Relayout(width);

        Assert.Equal(titleSize, hero.TitleLabel.FontSize);
        Assert.Equal(columns, hero.Container.ColumnDefinitions.Count);
    }

    [Fact]
    public void PageHero_puts_the_media_beside_the_text_and_reverse_swaps_them()
    {
        var media = new BoxView();
        var hero = new KlyPageHero { Title = "Build", Media = media };
        hero.Relayout(1200);
        Assert.Equal(0, Grid.GetColumn(hero.TextColumn));
        Assert.Equal(1, Grid.GetColumn(media.Parent!));

        hero.Reverse = true;
        Assert.Equal(1, Grid.GetColumn(hero.TextColumn));
        Assert.Equal(0, Grid.GetColumn(media.Parent!));

        hero.Orientation = StackOrientation.Vertical;
        Assert.Single(hero.Container.ColumnDefinitions);
        Assert.Equal(TextAlignment.Center, hero.TitleLabel.HorizontalTextAlignment);
    }

    [Fact]
    public void PageHero_stacks_the_media_under_the_text_below_1024_and_without_media_there_is_one_column()
    {
        var hero = new KlyPageHero { Title = "Build", Description = "Fast" };
        hero.Relayout(1200);
        Assert.Single(hero.Container.ColumnDefinitions);

        hero.Media = new BoxView();
        hero.Relayout(800);
        Assert.Single(hero.Container.ColumnDefinitions);
        Assert.Equal(1, Grid.GetRow(hero.MediaSlot));
    }

    [Fact]
    public void PageHero_links_are_xl_buttons_and_report_through_the_event()
    {
        var hero = new KlyPageHero { Links = [new PageLinkItem("Go", "https://example.com")] };
        var raised = Record(h => hero.LinkClicked += h);

        var button = Assert.Single(hero.LinkRow.Children.OfType<KlyButton>());
        Assert.Equal(KlySize.Xl, button.Size);
        button.SendClicked();
        Assert.Equal("https://example.com", Assert.Single(raised).Href);
        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(hero.TitleLabel));
    }

    // ---- KlyPageSection

    [Fact]
    public void PageSection_shows_its_icon_and_hides_it_without_a_name()
    {
        var section = new KlyPageSection { Title = "Why" };
        Assert.False(section.LeadingBox.IsVisible);

        section.Icon = KlyIconNames.Check;
        Assert.True(section.LeadingBox.IsVisible);
        Assert.Equal(KlyIconNames.Check, ((KlyIcon)section.LeadingBox.Content!).Name);
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(section.TitleLabel));
    }

    [Theory]
    [InlineData(StackOrientation.Horizontal, 400, 1)]
    [InlineData(StackOrientation.Horizontal, 700, 2)]
    [InlineData(StackOrientation.Horizontal, 1200, 1)]
    [InlineData(StackOrientation.Vertical, 700, 2)]
    [InlineData(StackOrientation.Vertical, 1200, 3)]
    public void PageSection_spreads_the_features_over_the_columns_of_the_width(StackOrientation orientation, double width, int columns)
    {
        var section = new KlyPageSection { Orientation = orientation, Features = [new PageFeatureItem("A"), new PageFeatureItem("B"), new PageFeatureItem("C")] };
        section.Relayout(width);

        Assert.Equal(columns, section.FeatureColumnCount);
        Assert.Equal(3, section.FeatureGrid.Children.Count);
    }

    [Fact]
    public void PageSection_features_carry_their_texts_heading_level_and_report_a_tap_once()
    {
        var section = new KlyPageSection { FeatureHeadingLevel = 3, Features = [new PageFeatureItem("Fast", "Very") { Icon = KlyIconNames.Check, Href = "/fast" }] };
        var raised = Record(h => section.LinkClicked += h);

        var feature = Assert.IsType<KlyPageFeature>(Assert.Single(section.FeatureGrid.Children));
        Assert.Equal("Fast", feature.Title);
        Assert.Equal("Very", feature.Description);
        Assert.Equal(3, feature.HeadingLevel);

        feature.Tap();
        var link = Assert.Single(raised);
        Assert.Equal("/fast", link.Href);
        Assert.Equal("Fast", Assert.IsType<PageFeatureItem>(link.Item).Title);
    }

    [Fact]
    public void PageSection_media_goes_beside_the_text_from_1024_and_reverse_swaps()
    {
        var section = new KlyPageSection { Media = new BoxView() };
        section.Relayout(1200);
        Assert.Equal(2, section.Container.ColumnDefinitions.Count);
        Assert.Equal(1, Grid.GetColumn(section.MediaSlot));

        section.Reverse = true;
        Assert.Equal(0, Grid.GetColumn(section.MediaSlot));

        section.Relayout(600);
        Assert.Single(section.Container.ColumnDefinitions);
    }

    // ---- KlyPageFeature

    [Fact]
    public void PageFeature_lays_out_the_icon_beside_or_above_the_text()
    {
        var feature = new KlyPageFeature { Icon = KlyIconNames.Check, Title = "Fast", Description = "Very" };
        Assert.True(feature.Leading.IsVisible);
        Assert.Equal(2, feature.LayoutGrid.ColumnDefinitions.Count);

        feature.Orientation = StackOrientation.Vertical;
        Assert.Single(feature.LayoutGrid.ColumnDefinitions);
        Assert.Equal(2, feature.LayoutGrid.RowDefinitions.Count);

        feature.Icon = null;
        Assert.False(feature.Leading.IsVisible);
    }

    [Fact]
    public void PageFeature_title_is_a_heading_only_when_asked_and_a_tap_needs_an_href()
    {
        var feature = new KlyPageFeature { Title = "Fast" };
        var raised = Record(h => feature.Clicked += h);
        Assert.Equal(SemanticHeadingLevel.None, SemanticProperties.GetHeadingLevel(feature.TitleLabel));

        feature.Tap();
        Assert.Empty(raised);

        feature.HeadingLevel = 4;
        feature.Href = "/fast";
        feature.Tap();
        Assert.Equal(SemanticHeadingLevel.Level4, SemanticProperties.GetHeadingLevel(feature.TitleLabel));
        Assert.Equal("/fast", Assert.Single(raised).Href);
    }

    // ---- KlyPageGrid, KlyPageColumns, KlyPageList

    [Theory]
    [InlineData(300, 1)]
    [InlineData(700, 2)]
    [InlineData(1100, 3)]
    public void PageGrid_has_one_column_on_a_phone_two_from_640_and_columns_from_1024(double width, int columns)
    {
        var grid = new KlyPageGrid();
        grid.Relayout(width);

        Assert.Equal(columns, grid.ColumnCount);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(4, 4)]
    [InlineData(9, 4)]
    public void PageGrid_clamps_columns_to_2_through_4(int requested, int columns)
    {
        var grid = new KlyPageGrid { Columns = requested };
        grid.Relayout(1200);

        Assert.Equal(columns, grid.ColumnCount);
    }

    [Fact]
    public void PageGrid_places_the_children_row_by_row()
    {
        var items = Enumerable.Range(0, 5).Select(_ => new BoxView()).ToList();
        var grid = new KlyPageGrid { Columns = 3 };
        foreach (var item in items) grid.Children.Add(item);
        grid.Relayout(1200);

        Assert.Equal([0, 0, 0, 1, 1], items.Select(Grid.GetRow));
        Assert.Equal([0, 1, 2, 0, 1], items.Select(Grid.GetColumn));
        Assert.Equal(2, grid.RowDefinitions.Count);

        grid.Relayout(300);
        Assert.Equal([0, 1, 2, 3, 4], items.Select(Grid.GetRow));

        // A child added later is placed at once.
        var late = new BoxView();
        grid.Children.Add(late);
        Assert.Equal(5, Grid.GetRow(late));
    }

    [Fact]
    public void PageColumns_fills_each_column_in_reading_order()
    {
        var items = Enumerable.Range(0, 5).Select(_ => new BoxView()).ToList();
        var columns = new KlyPageColumns { Columns = 2 };
        foreach (var item in items) columns.Items.Add(item);
        columns.Relayout(700);

        Assert.Equal(2, columns.ColumnCount);
        Assert.Equal([3, 2], columns.Stacks.Select(s => s.Children.Count));
        Assert.Same(items[0], columns.Stacks[0].Children[0]);
        Assert.Same(items[3], columns.Stacks[1].Children[0]);

        columns.Relayout(300);
        Assert.Equal(1, columns.ColumnCount);
        Assert.Equal(5, columns.Stacks[0].Children.Count);
    }

    [Fact]
    public void PageList_draws_a_line_between_the_items_only_with_divide()
    {
        var list = new KlyPageList();
        list.Items.Add(new Label());
        list.Items.Add(new Label());
        list.Items.Add(new Label());

        Assert.Equal(5, list.Stack.Children.Count);
        Assert.Equal(2, list.Stack.Children.OfType<BoxView>().Count());
        Assert.Equal(0, list.Stack.Spacing);

        list.Divide = false;
        Assert.Equal(3, list.Stack.Children.Count);
        Assert.Equal(32, list.Stack.Spacing);
    }

    // ---- KlyPageCard

    [Theory]
    [InlineData(PageCardVariant.Outline, 1)]
    [InlineData(PageCardVariant.Soft, 0)]
    [InlineData(PageCardVariant.Subtle, 1)]
    [InlineData(PageCardVariant.Spotlight, 1)]
    public void PageCard_variant_sets_the_border(PageCardVariant variant, double stroke)
    {
        var card = new KlyPageCard { Variant = variant };

        Assert.Equal(stroke, card.StrokeThickness);
        Assert.Equal(KlyTheme.Radius * 2, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)card.StrokeShape!).CornerRadius.TopLeft);
    }

    [Fact]
    public void PageCard_spotlight_glows_only_for_the_spotlight_variant()
    {
        var card = new KlyPageCard();
        card.SetSpotlight(true);
        Assert.Equal(0, card.SpotlightOpacity);

        card.Variant = PageCardVariant.Spotlight;
        card.SetSpotlight(true);
        Assert.Equal(1, card.SpotlightOpacity);
        card.SetSpotlight(false);
        Assert.Equal(0, card.SpotlightOpacity);
    }

    [Fact]
    public void PageCard_shows_the_text_block_only_when_it_has_text_and_defaults_to_a_level_3_heading()
    {
        var card = new KlyPageCard();
        Assert.False(card.Wrapper.IsVisible);

        card.Title = "Docs";
        Assert.True(card.Wrapper.IsVisible);
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(card.TitleLabel));

        card.HeadingLevel = null;
        Assert.Equal(SemanticHeadingLevel.None, SemanticProperties.GetHeadingLevel(card.TitleLabel));

        card.Title = null;
        card.Icon = KlyIconNames.Check;
        Assert.True(card.Leading.IsVisible);
        Assert.True(card.Wrapper.IsVisible);
    }

    [Fact]
    public void PageCard_puts_the_body_beside_the_text_from_1024_and_the_padding_grows_from_640()
    {
        var card = new KlyPageCard { Title = "Docs", Body = new BoxView(), Orientation = StackOrientation.Horizontal };
        card.Relayout(500);
        Assert.Single(card.Container.ColumnDefinitions);
        Assert.Equal(new Thickness(16), card.Container.Padding);

        card.Relayout(1100);
        Assert.Equal(2, card.Container.ColumnDefinitions.Count);
        Assert.Equal(new Thickness(24), card.Container.Padding);
        Assert.Equal(0, Grid.GetColumn(card.Wrapper));

        card.Reverse = true;
        Assert.Equal(1, Grid.GetColumn(card.Wrapper));
    }

    [Fact]
    public void PageCard_tap_reports_its_href_once()
    {
        var card = new KlyPageCard { Title = "Docs" };
        var raised = Record(h => card.Clicked += h);
        card.Tap();
        Assert.Empty(raised);

        card.Href = "/docs";
        card.Tap();
        Assert.Equal("/docs", Assert.Single(raised).Href);

        card.IsEnabled = false;
        card.Tap();
        Assert.Single(raised);
    }

    // ---- KlyPageCta

    [Fact]
    public void PageCta_solid_takes_the_contrast_color_for_the_texts_and_the_others_the_page_colors()
    {
        var cta = new KlyPageCta { Title = "Go", Description = "Now", Variant = KlyVariant.Solid };
        Assert.Equal(1, cta.Box.StrokeThickness);
        Assert.Equal(KlyTheme.Radius * 3, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)cta.Box.StrokeShape!).CornerRadius.TopLeft);

        cta.Variant = KlyVariant.Soft;
        Assert.Equal(0, cta.Box.StrokeThickness);
        cta.Variant = KlyVariant.Subtle;
        Assert.Equal(1, cta.Box.StrokeThickness);
        Assert.Equal(KlyVariant.Outline, new KlyPageCta().Variant);
    }

    [Fact]
    public void PageCta_puts_the_links_beside_the_text_from_1024_and_centers_everything_when_vertical()
    {
        var cta = new KlyPageCta { Title = "Go", Links = [new PageLinkItem("Start", "/s")] };
        cta.Relayout(1200);
        Assert.Equal(2, cta.LayoutGrid.ColumnDefinitions.Count);
        Assert.Equal(1, Grid.GetColumn(cta.LinkRow));

        cta.Reverse = true;
        Assert.Equal(0, Grid.GetColumn(cta.LinkRow));

        cta.Relayout(500);
        Assert.Single(cta.LayoutGrid.ColumnDefinitions);
        Assert.Equal(1, Grid.GetRow(cta.LinkRow));

        cta.Orientation = StackOrientation.Vertical;
        Assert.Equal(TextAlignment.Center, cta.TitleLabel.HorizontalTextAlignment);
        Assert.Equal(KlySize.Lg, cta.LinkRow.Children.OfType<KlyButton>().Single().Size);
    }

    [Fact]
    public void PageCta_links_report_through_the_event()
    {
        var cta = new KlyPageCta { Links = [new PageLinkItem("Start", "/s")] };
        var raised = Record(h => cta.LinkClicked += h);
        cta.LinkRow.Children.OfType<KlyButton>().Single().SendClicked();

        Assert.Equal("/s", Assert.Single(raised).Href);
    }

    // ---- KlyPageLinks, KlyPageAnchors

    [Fact]
    public void PageLinks_draws_a_row_per_link_with_the_external_icon_for_an_absolute_address()
    {
        var links = new KlyPageLinks
        {
            Title = "Resources",
            HeadingLevel = 3,
            Links = [new PageLinkItem("Guide", "/guide") { Icon = KlyIconNames.File }, new PageLinkItem("Site", "https://example.com"), new PageLinkItem("Soon") { Disabled = true }],
        };

        Assert.Equal(3, links.List.Children.Count);
        Assert.Equal(2, Descendants<KlyIcon>(links).Count(i => i.Name == KlyIconNames.File || i.Name == KlyIconNames.External));
        Assert.Equal(0.6, ((View)links.List.Children[2]).Opacity);
        Assert.False(((View)links.List.Children[2]).IsEnabled);
        Assert.All(links.List.Children.Cast<View>(), row => Assert.True(row.MinimumHeightRequest >= 40));
        Assert.Equal("Guide", SemanticProperties.GetDescription((View)links.List.Children[0]));
    }

    [Fact]
    public void PageLinks_marks_the_current_page_and_reports_a_followed_link()
    {
        var links = new KlyPageLinks { Links = [new PageLinkItem("Home", "/") { Active = true }, new PageLinkItem("Docs", "/docs")] };
        var raised = Record(h => links.LinkClicked += h);

        var labels = Descendants<Label>(links).Where(l => l.Text is "Home" or "Docs").ToList();
        Assert.Equal(FontAttributes.Bold, labels[0].FontAttributes);
        Assert.Equal(FontAttributes.None, labels[1].FontAttributes);

        links.Activate(links.Links![1]);
        Assert.Equal("/docs", Assert.Single(raised).Href);
    }

    [Fact]
    public void PageAnchors_finds_the_target_by_its_automation_id_and_reports_the_link()
    {
        var target = new Label { AutomationId = "installation" };
        var scroll = new ScrollView { Content = new VerticalStackLayout { Children = { new Label(), new Border { Content = target } } } };
        var anchors = new KlyPageAnchors { ScrollView = scroll, Links = [new PageLinkItem("Install", "#installation") { Icon = KlyIconNames.Hash }, new PageLinkItem("Site", "https://example.com")] };
        var raised = Record(h => anchors.LinkClicked += h);

        Assert.Equal(2, anchors.List.Children.Count);
        Assert.Same(target, PagesScroll.Resolve(scroll, "#installation"));
        Assert.Same(target, PagesScroll.Resolve(scroll, "installation"));
        Assert.Null(PagesScroll.Resolve(scroll, "#missing"));
        Assert.Null(PagesScroll.Resolve(null, "#installation"));

        anchors.Activate(anchors.Links![0]);
        Assert.Equal("#installation", Assert.Single(raised).Href);
    }

    // ---- KlyPageLogos

    [Fact]
    public void PageLogos_lists_icons_and_images_with_their_accessible_names()
    {
        var logos = new KlyPageLogos
        {
            Title = "Trusted by",
            Items = [new PageLogoItem("Acme") { Icon = KlyIconNames.Check }, new PageLogoItem("Globex") { Source = ImageSource.FromFile("globex.png") }, new PageLogoItem("Empty")],
        };

        Assert.Equal(2, logos.Wrap.Children.Count);
        var icon = Assert.IsType<KlyIcon>(logos.Wrap.Children[0]);
        Assert.Equal("Acme", icon.Label);
        var image = Assert.IsType<Image>(logos.Wrap.Children[1]);
        Assert.Equal("Globex", SemanticProperties.GetDescription(image));
        Assert.Equal(40, image.HeightRequest);
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(Descendants<Label>(logos).First()));
    }

    [Fact]
    public void PageLogos_custom_logos_follow_the_items_and_the_marquee_repeats_the_items_hidden_from_assistive_technology()
    {
        var custom = new Label { Text = "Custom" };
        var logos = new KlyPageLogos { Items = [new PageLogoItem("Acme") { Icon = KlyIconNames.Check }] };
        logos.Logos.Add(custom);
        Assert.Equal(2, logos.Wrap.Children.Count);
        Assert.Same(custom, logos.Wrap.Children[1]);

        logos.Marquee = true;
        Assert.False(logos.Wrap.IsVisible);
        Assert.Equal(2, logos.Strip.Children.Count);
        var copy = Assert.IsType<HorizontalStackLayout>(logos.Strip.Children[1]);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(copy));
        Assert.Single(copy.Children);
        Assert.False(logos.IsMarqueeRunning);

        logos.Marquee = false;
        Assert.True(logos.Wrap.IsVisible);
    }

    // ---- KlyPageAside, KlyPageBody

    [Fact]
    public void PageAside_hides_itself_in_a_page_narrower_than_1024_unless_always_visible()
    {
        var aside = new KlyPageAside { Body = new Label() };
        aside.Relayout(800);
        Assert.False(aside.IsVisible);

        aside.Relayout(1100);
        Assert.True(aside.IsVisible);

        aside.Relayout(800);
        aside.AlwaysVisible = true;
        Assert.True(aside.IsVisible);
    }

    [Fact]
    public void PageAside_sets_the_top_and_the_bottom_apart_by_a_line()
    {
        var aside = new KlyPageAside();
        var stack = (VerticalStackLayout)aside.Content!;
        var lines = stack.Children.OfType<BoxView>().ToList();
        Assert.All(lines, l => Assert.False(l.IsVisible));

        aside.Top = new Label();
        aside.Bottom = new Label();
        Assert.All(lines, l => Assert.True(l.IsVisible));
    }

    [Fact]
    public void PageBody_gives_the_labels_below_it_prose_styles()
    {
        var body = new KlyPageBody();

        Assert.Equal(KlyTheme.Text.Light, body.Resources["KlyBodyText"]);
        Assert.Equal(KlyTheme.TextHighlighted.Light, body.Resources["KlyBodyHeading"]);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Primary).Text.Light, body.Resources["KlyBodyLink"]);
        Assert.Equal(KlyTheme.TextMuted.Light, body.Resources["KlyBodyMuted"]);
        Assert.Equal(KlyTheme.BackgroundElevated.Light, body.Resources["KlyBodyCode"]);
        Assert.True(body.Resources.TryGetValue(typeof(Label).FullName!, out _));
        Assert.Equal(new Thickness(0, 48, 0, 0), body.Margin);
        Assert.Equal(new Thickness(0, 0, 0, 96), body.Padding);
    }

    // ---- KlyAuthForm

    private static KlyAuthForm SignIn() => new()
    {
        Title = "Sign in",
        Fields =
        [
            new KlyAuthField("email", KlyAuthFieldType.Email) { Label = "Email", Required = true, Placeholder = "you@example.com" },
            new KlyAuthField("password", KlyAuthFieldType.Password) { Label = "Password", Required = true },
            new KlyAuthField("remember", KlyAuthFieldType.Checkbox) { Label = "Remember me" },
        ],
    };

    [Fact]
    public void AuthForm_builds_a_native_control_for_each_field_type()
    {
        var form = new KlyAuthForm
        {
            Fields =
            [
                new KlyAuthField("name"),
                new KlyAuthField("mail", KlyAuthFieldType.Email),
                new KlyAuthField("phone", KlyAuthFieldType.Tel),
                new KlyAuthField("site", KlyAuthFieldType.Url),
                new KlyAuthField("age", KlyAuthFieldType.Number),
                new KlyAuthField("code", KlyAuthFieldType.Otp) { Length = 4 },
                new KlyAuthField("secret", KlyAuthFieldType.Password),
                new KlyAuthField("agree", KlyAuthFieldType.Checkbox),
                new KlyAuthField("plan", KlyAuthFieldType.Select) { Items = ["Free", "Pro"] },
                new KlyAuthField("day", KlyAuthFieldType.Date) { Value = "2026-03-04" },
                new KlyAuthField("at", KlyAuthFieldType.Time) { Value = "09:30" },
            ],
        };

        Assert.Equal(Keyboard.Email, ((KlyInput)form.ControlOf("mail")!).Keyboard);
        Assert.Equal(Keyboard.Telephone, ((KlyInput)form.ControlOf("phone")!).Keyboard);
        Assert.Equal(Keyboard.Url, ((KlyInput)form.ControlOf("site")!).Keyboard);
        Assert.Equal(Keyboard.Numeric, ((KlyInput)form.ControlOf("age")!).Keyboard);
        var code = (KlyInput)form.ControlOf("code")!;
        Assert.Equal(4, code.MaxLength);
        Assert.Equal(Keyboard.Numeric, code.Keyboard);
        var password = ((Grid)form.ControlOf("secret")!).Children.OfType<KlyInput>().Single();
        Assert.True(password.IsPassword);
        Assert.IsType<KlyCheckbox>(form.ControlOf("agree"));
        Assert.Equal(new[] { "Free", "Pro" }, ((System.Collections.IEnumerable)((KlySelect)form.ControlOf("plan")!).ItemsSource!).Cast<string>());
        Assert.IsType<PagesPickerField>(form.ControlOf("day"));
        Assert.Equal("2026-03-04", form.Values["day"]);
        Assert.Equal("09:30", form.Values["at"]);
    }

    [Fact]
    public void AuthForm_keeps_the_values_by_field_name_in_both_directions()
    {
        var form = SignIn();
        ((KlyInput)form.ControlOf("email")!).Text = "ada@example.com";
        ((KlyCheckbox)form.ControlOf("remember")!).IsChecked = true;
        ((Grid)form.ControlOf("password")!).Children.OfType<KlyInput>().Single().Text = "secret";

        Assert.Equal("ada@example.com", form.Values["email"]);
        Assert.Equal("true", form.Values["remember"]);
        Assert.Equal("secret", form.Values["password"]);

        var unchecked_ = new KlyAuthForm { Fields = [new KlyAuthField("agree", KlyAuthFieldType.Checkbox)] };
        Assert.Equal("false", unchecked_.Values["agree"]);
    }

    [Fact]
    public void AuthForm_marks_required_fields_in_the_label_and_describes_the_field_for_a_screen_reader()
    {
        var form = SignIn();
        var labels = Descendants<Label>(form.FieldStack).Select(l => l.Text).ToList();

        Assert.Contains("Email *", labels);
        Assert.Contains("Password *", labels);
        Assert.Equal("Email", ((KlyInput)form.ControlOf("email")!).AccessibleName);
    }

    [Fact]
    public void AuthForm_reports_empty_required_fields_and_a_bad_email_without_submitting()
    {
        var form = SignIn();
        var valid = 0;
        var invalid = new List<IReadOnlyDictionary<string, string?>>();
        form.ValidSubmit += (_, _) => valid++;
        form.InvalidSubmit += (_, e) => invalid.Add(e.Values);

        form.SubmitButton.SendClicked();
        Assert.Equal(0, valid);
        Assert.Single(invalid);
        Assert.Equal(["Email is required.", "Password is required.", ""], form.ErrorTexts);
        Assert.True(((KlyInput)form.ControlOf("email")!).Invalid);

        ((KlyInput)form.ControlOf("email")!).Text = "not-an-email";
        ((Grid)form.ControlOf("password")!).Children.OfType<KlyInput>().Single().Text = "secret";
        form.SubmitButton.SendClicked();
        Assert.Equal(["Enter a valid email address.", "", ""], form.ErrorTexts);
        Assert.Equal(2, invalid.Count);
        Assert.Equal(0, valid);
    }

    [Fact]
    public void AuthForm_raises_ValidSubmit_once_with_the_values_when_everything_is_valid()
    {
        var form = SignIn();
        var submitted = new List<IReadOnlyDictionary<string, string?>>();
        form.ValidSubmit += (_, e) => submitted.Add(e.Values);
        ((KlyInput)form.ControlOf("email")!).Text = "ada@example.com";
        ((Grid)form.ControlOf("password")!).Children.OfType<KlyInput>().Single().Text = "secret";

        form.SubmitButton.SendClicked();

        var values = Assert.Single(submitted);
        Assert.Equal("ada@example.com", values["email"]);
        Assert.Equal("secret", values["password"]);
        Assert.Equal("false", values["remember"]);
        Assert.All(form.ErrorTexts, text => Assert.Equal("", text));
    }

    [Fact]
    public void AuthForm_checks_the_length_of_a_one_time_code_and_a_required_checkbox()
    {
        var form = new KlyAuthForm { Fields = [new KlyAuthField("code", KlyAuthFieldType.Otp) { Length = 6 }, new KlyAuthField("terms", KlyAuthFieldType.Checkbox) { Label = "Terms", Required = true }] };
        ((KlyInput)form.ControlOf("code")!).Text = "123";
        form.SubmitButton.SendClicked();
        Assert.Equal(["Enter all 6 characters.", "Terms is required."], form.ErrorTexts);

        ((KlyInput)form.ControlOf("code")!).Text = "123456";
        ((KlyCheckbox)form.ControlOf("terms")!).IsChecked = true;
        var valid = 0;
        form.ValidSubmit += (_, _) => valid++;
        form.SubmitButton.SendClicked();
        Assert.Equal(1, valid);
    }

    [Fact]
    public void AuthForm_runs_the_custom_validation_after_the_built_in_checks_and_shows_server_messages()
    {
        var form = new KlyAuthForm { Fields = [new KlyAuthField("email", KlyAuthFieldType.Email) { Label = "Email" }] };
        form.Validate = values => values["email"] == "taken@example.com" ? new Dictionary<string, string> { ["email"] = "Already used." } : null;
        ((KlyInput)form.ControlOf("email")!).Text = "taken@example.com";
        var invalid = 0;
        form.InvalidSubmit += (_, _) => invalid++;

        form.SubmitButton.SendClicked();
        Assert.Equal(["Already used."], form.ErrorTexts);
        Assert.Equal(1, invalid);

        form.FieldErrors = new Dictionary<string, string> { ["email"] = "From the server." };
        Assert.Equal(["Already used."], form.ErrorTexts);

        form.Validate = null;
        form.SubmitButton.SendClicked();
        Assert.Equal(["From the server."], form.ErrorTexts);
    }

    [Fact]
    public void AuthForm_shows_the_form_error_and_dims_the_fields_and_the_button_while_loading()
    {
        var form = SignIn();
        Assert.False(form.Alert.IsVisible);
        form.Error = "Wrong password.";
        Assert.True(form.Alert.IsVisible);
        Assert.Equal("Wrong password.", form.Alert.Title);
        Assert.Equal(KlyColor.Error, form.Alert.Color);

        var submitted = 0;
        form.ValidSubmit += (_, _) => submitted++;
        form.Loading = true;
        Assert.False(form.FieldStack.IsEnabled);
        Assert.False(form.SubmitButton.IsEnabled);
        form.SubmitForm();
        Assert.Equal(0, submitted);

        form.Loading = false;
        Assert.True(form.SubmitButton.IsEnabled);
        Assert.True(form.FieldStack.IsEnabled);
    }

    [Fact]
    public void AuthForm_header_providers_and_separator_follow_their_content()
    {
        var form = new KlyAuthForm { Title = "Welcome", Description = "Sign in", Icon = KlyIconNames.Check, Color = KlyColor.Secondary, SubmitLabel = "Go", Fields = [new KlyAuthField("email")] };
        Assert.True(form.Leading.IsVisible);
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(form.TitleLabel));
        Assert.Equal(KlyColor.Secondary, form.SubmitButton.Color);
        Assert.Equal("Go", form.SubmitButton.Text);
        Assert.False(form.SeparatorRow.IsVisible);

        var raised = Record(h => form.ProviderClicked += h);
        form.Providers = [new KlyAuthProvider("Continue with GitHub", "https://github.com/login")];
        var button = Assert.Single(form.ProviderStack.Children.OfType<KlyButton>());
        Assert.Equal(ButtonVariant.Outline, button.Variant);
        Assert.Equal(KlyColor.Neutral, button.Color);
        Assert.True(form.SeparatorRow.IsVisible);
        Assert.Equal("or", form.SeparatorLabel.Text);

        form.Separator = "";
        Assert.False(form.SeparatorRow.IsVisible);

        button.SendClicked();
        Assert.Equal("https://github.com/login", Assert.Single(raised).Href);
    }

    [Fact]
    public void AuthForm_return_key_goes_to_the_next_field_and_submits_from_the_last_one()
    {
        var form = new KlyAuthForm { Fields = [new KlyAuthField("a"), new KlyAuthField("b", KlyAuthFieldType.Search), new KlyAuthField("c")] };
        var valid = 0;
        form.ValidSubmit += (_, _) => valid++;

        Assert.Equal(ReturnType.Next, ((KlyInput)form.ControlOf("a")!).ReturnType);
        Assert.Equal(ReturnType.Search, ((KlyInput)form.ControlOf("b")!).ReturnType);
        var last = (KlyInput)form.ControlOf("c")!;
        Assert.Equal(ReturnType.Done, last.ReturnType);

        ((IEntryController)last.Content!).SendCompleted();
        Assert.Equal(1, valid);
    }

    [Fact]
    public void AuthForm_password_button_shows_and_hides_the_characters()
    {
        var form = new KlyAuthForm { Fields = [new KlyAuthField("secret", KlyAuthFieldType.Password) { Label = "Secret" }] };
        var grid = (Grid)form.ControlOf("secret")!;
        var input = grid.Children.OfType<KlyInput>().Single();
        var toggle = grid.Children.OfType<ContentView>().Single();
        Assert.Equal("Show password", SemanticProperties.GetDescription(toggle));
        Assert.True(toggle.WidthRequest >= 40);

        Tap(toggle);
        Assert.False(input.IsPassword);
        Assert.Equal(KlyIconNames.EyeOff, ((KlyIcon)toggle.Content!).Name);
        Tap(toggle);
        Assert.True(input.IsPassword);
    }

    // ---- KlyPricingPlan, KlyPricingPlans

    [Theory]
    [InlineData(KlyVariant.Outline, 1)]
    [InlineData(KlyVariant.Soft, 0)]
    [InlineData(KlyVariant.Subtle, 1)]
    [InlineData(KlyVariant.Solid, 1)]
    public void PricingPlan_variant_sets_the_border_and_the_highlight_a_ring_of_two(KlyVariant variant, double stroke)
    {
        var plan = new KlyPricingPlan { Variant = variant };
        Assert.Equal(stroke, plan.StrokeThickness);

        plan.Highlight = true;
        Assert.Equal(2, plan.StrokeThickness);
    }

    [Fact]
    public void PricingPlan_button_follows_the_variant_and_the_highlight()
    {
        var plan = new KlyPricingPlan { Color = KlyColor.Secondary, ButtonLabel = "Buy" };
        Assert.Equal(KlyColor.Secondary, plan.Button.Color);
        Assert.Equal(ButtonVariant.Outline, plan.Button.Variant);
        Assert.Equal(KlySize.Lg, plan.Button.Size);
        Assert.True(plan.Button.Block);

        plan.Highlight = true;
        Assert.Equal(ButtonVariant.Solid, plan.Button.Variant);

        plan.Variant = KlyVariant.Solid;
        Assert.Equal(KlyColor.Neutral, plan.Button.Color);

        plan.ButtonColor = KlyColor.Error;
        plan.ButtonVariant = ButtonVariant.Soft;
        Assert.Equal(KlyColor.Error, plan.Button.Color);
        Assert.Equal(ButtonVariant.Soft, plan.Button.Variant);
    }

    [Fact]
    public void PricingPlan_button_reports_the_href_and_a_footer_replaces_it()
    {
        var plan = new KlyPricingPlan { ButtonLabel = "Buy", ButtonHref = "/buy" };
        var raised = Record(h => plan.ButtonClicked += h);
        Assert.Same(plan.Button, plan.FooterSlot.Content);

        plan.Button.SendClicked();
        Assert.Equal("/buy", Assert.Single(raised).Href);

        var footer = new Label();
        plan.Footer = footer;
        Assert.Same(footer, plan.FooterSlot.Content);

        plan.Footer = null;
        plan.ButtonLabel = null;
        Assert.False(plan.FooterSlot.IsVisible);
    }

    [Fact]
    public void PricingPlan_shows_the_price_the_discount_and_names_each_for_a_screen_reader()
    {
        var plan = new KlyPricingPlan { Price = "$29", BillingCycle = "/month", Tagline = "For teams" };
        Assert.Equal("$29", plan.PriceLabel.Text);
        Assert.False(plan.DiscountLabel.IsVisible);

        plan.Discount = "$19";
        Assert.Equal("$19", plan.PriceLabel.Text);
        Assert.Equal("$29", plan.DiscountLabel.Text);
        Assert.Equal(TextDecorations.Strikethrough, plan.DiscountLabel.TextDecorations);
        Assert.Equal("Original price $29", SemanticProperties.GetDescription(plan.DiscountLabel));
        Assert.Equal("Now $19", SemanticProperties.GetDescription(plan.PriceLabel));

        plan.OriginalPriceLabel = "Avant";
        Assert.Equal("Avant $29", SemanticProperties.GetDescription(plan.DiscountLabel));
    }

    [Fact]
    public void PricingPlan_lists_features_with_a_check_mark_by_default_and_converts_strings()
    {
        KlyPricingFeature plain = "Unlimited projects";
        var plan = new KlyPricingPlan { Features = [plain, new KlyPricingFeature("Support") { Icon = KlyIconNames.Hash }] };

        Assert.Equal(2, plan.FeatureList.Children.Count);
        var icons = Descendants<KlyIcon>(plan.FeatureList).ToList();
        Assert.Equal([KlyIconNames.Check, KlyIconNames.Hash], icons.Select(i => i.Name));
        Assert.Equal("Unlimited projects", Descendants<Label>(plan.FeatureList).First().Text);
    }

    [Fact]
    public void PricingPlan_badge_is_subtle_neutral_on_a_solid_plan_and_in_the_plan_color_otherwise()
    {
        var plan = new KlyPricingPlan { Badge = "Popular", Color = KlyColor.Secondary };
        Assert.Equal("Popular", plan.BadgeView.Text);
        Assert.Equal(KlyVariant.Subtle, plan.BadgeView.Variant);
        Assert.Equal(KlyColor.Secondary, plan.BadgeView.Color);

        plan.BadgeColor = KlyColor.Success;
        Assert.Equal(KlyColor.Success, plan.BadgeView.Color);

        plan.Variant = KlyVariant.Solid;
        Assert.Equal(KlyColor.Neutral, plan.BadgeView.Color);
    }

    [Fact]
    public void PricingPlan_lays_out_in_a_row_from_1024_and_enlarges_when_asked()
    {
        var plan = new KlyPricingPlan { Orientation = StackOrientation.Horizontal, Enlarge = true, ButtonLabel = "Buy", Price = "$9" };
        plan.Relayout(500);
        Assert.Single(plan.LayoutGrid.ColumnDefinitions);
        Assert.Equal(1, plan.Scale);
        Assert.Equal(new Thickness(24), plan.Padding);

        plan.Relayout(1100);
        Assert.Equal(3, plan.LayoutGrid.ColumnDefinitions.Count);
        Assert.Equal(1.05, plan.Scale);
        Assert.Equal(new Thickness(32), plan.Padding);
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(plan.TitleLabel));
    }

    [Theory]
    [InlineData(StackOrientation.Horizontal, 300, 1)]
    [InlineData(StackOrientation.Horizontal, 600, 2)]
    [InlineData(StackOrientation.Horizontal, 900, 3)]
    [InlineData(StackOrientation.Vertical, 900, 1)]
    public void PricingPlans_fits_as_many_plans_per_row_as_272_units_allow(StackOrientation orientation, double width, int columns)
    {
        var plans = new KlyPricingPlans { Orientation = orientation };
        plans.Children.Add(new KlyPricingPlan());
        plans.Children.Add(new KlyPricingPlan());
        plans.Children.Add(new KlyPricingPlan());
        plans.Relayout(width);

        Assert.Equal(columns, plans.ColumnCount);
    }

    // ---- KlyPricingTable

    private static KlyPricingTable Table() => new()
    {
        Tiers = [new KlyPricingTier("free", "Free") { Price = "$0", ButtonLabel = "Start", ButtonHref = "/free" }, new KlyPricingTier("pro", "Pro") { Highlight = true, Badge = "Popular", Price = "$20", Discount = "$15", BillingCycle = "/mo" }],
        Sections = [new KlyPricingSection("Limits", [new KlyPricingTableFeature("Projects", 3, "Unlimited"), new KlyPricingTableFeature("SSO", false, true), new KlyPricingTableFeature("Support") { ByTier = new Dictionary<string, KlyPricingTableValue> { ["pro"] = "24/7" } }])],
    };

    [Fact]
    public void PricingTable_is_a_table_from_768_and_cards_below()
    {
        var table = Table();
        table.Relayout(900);
        Assert.True(table.IsTable);
        var grid = Assert.IsType<Grid>(table.Content);
        Assert.Equal(3, grid.ColumnDefinitions.Count);

        table.Relayout(400);
        Assert.False(table.IsTable);
        Assert.IsType<VerticalStackLayout>(table.Content);
    }

    [Fact]
    public void PricingTable_cells_show_check_marks_minus_signs_texts_and_numbers_with_accessible_names()
    {
        var table = Table();
        table.Relayout(900);
        var icons = Descendants<KlyIcon>(table).ToList();

        var check = Assert.Single(icons, i => i.Name == KlyIconNames.Check);
        var minus = icons.First(i => i.Name == KlyIconNames.Minus);
        Assert.Equal("Included", check.Label);
        Assert.Equal("Not included", minus.Label);
        var texts = Descendants<Label>(table).Select(l => l.Text).ToList();
        Assert.Contains("3", texts);
        Assert.Contains("Unlimited", texts);
        Assert.Contains("24/7", texts);
    }

    [Fact]
    public void PricingTable_by_tier_values_win_over_positions_and_a_missing_cell_is_a_minus_sign()
    {
        var table = Table();
        table.Relayout(900);

        // Support has a value for "pro" only: the free column of that row is a minus sign.
        Assert.Equal(2, Descendants<KlyIcon>(table).Count(i => i.Name == KlyIconNames.Minus));
    }

    [Fact]
    public void PricingTable_tier_header_has_the_price_the_discount_the_badge_and_a_button_that_reports()
    {
        var table = Table();
        var raised = Record(h => table.TierButtonClicked += h);
        table.Relayout(900);

        var labels = Descendants<Label>(table).Select(l => l.Text).ToList();
        Assert.Contains("$0", labels);
        Assert.Contains("$20", labels);
        Assert.Contains("$15", labels);
        Assert.Single(Descendants<KlyBadge>(table), b => b.Text == "Popular");
        var button = Assert.Single(Descendants<KlyButton>(table));
        Assert.Equal(ButtonVariant.Outline, button.Variant);

        button.SendClicked();
        var link = Assert.Single(raised);
        Assert.Equal("/free", link.Href);
        Assert.Equal("free", Assert.IsType<KlyPricingTier>(link.Item).Id);
    }

    [Fact]
    public void PricingTable_caption_names_the_table_for_a_screen_reader()
    {
        var table = new KlyPricingTable { Caption = "Plans compared" };

        Assert.Equal("Plans compared", SemanticProperties.GetDescription(table));
    }

    [Fact]
    public void PricingTableValue_converts_from_bool_text_and_numbers()
    {
        KlyPricingTableValue yes = true;
        KlyPricingTableValue no = false;
        KlyPricingTableValue none = (string?)null;
        KlyPricingTableValue text = "10 GB";
        KlyPricingTableValue count = 5;
        KlyPricingTableValue price = 2.5;

        Assert.True(yes.IsIncluded);
        Assert.True(no.IsNotIncluded);
        Assert.True(none.IsNotIncluded);
        Assert.Equal("10 GB", text.ToString());
        Assert.Equal("5", count.ToString());
        Assert.Equal("2.5", price.ToString());
        Assert.Equal("2,5", price.TextOf(new CultureInfo("fr-FR")));
        Assert.Equal("true", yes.ToString());
        Assert.Equal("", no.ToString());
    }

    // ---- KlyBlogPost, KlyBlogPosts

    [Fact]
    public void BlogPost_formats_the_date_with_the_long_pattern_without_the_day_name_or_a_given_format()
    {
        var post = new KlyBlogPost { Title = "Hello", Date = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero), Culture = new CultureInfo("en-US") };
        Assert.Equal("March 4, 2026", post.DateLabel.Text);

        post.Culture = new CultureInfo("fr-FR");
        Assert.Equal("4 mars 2026", post.DateLabel.Text);

        post.DateFormat = "yyyy-MM-dd";
        Assert.Equal("2026-03-04", post.DateLabel.Text);

        post.Date = null;
        Assert.False(post.DateLabel.IsVisible);
    }

    [Fact]
    public void BlogPost_shows_the_badge_the_cover_and_a_heading_level_3_title()
    {
        var post = new KlyBlogPost { Title = "Hello", Badge = "News", BadgeColor = KlyColor.Info, Image = ImageSource.FromFile("cover.png"), ImageAlt = "A cover" };

        Assert.True(post.BadgeView.IsVisible);
        Assert.Equal("News", post.BadgeView.Text);
        Assert.Equal(KlyColor.Info, post.BadgeView.Color);
        Assert.Equal(KlyVariant.Subtle, post.BadgeView.Variant);
        Assert.True(post.HeaderBox.IsVisible);
        Assert.Equal("A cover", SemanticProperties.GetDescription(post.CoverImage));
        Assert.True(AutomationProperties.GetIsInAccessibleTree(post.CoverImage));
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(post.TitleLabel));

        post.ImageAlt = null;
        Assert.False(AutomationProperties.GetIsInAccessibleTree(post.CoverImage));

        post.Image = null;
        Assert.False(post.HeaderBox.IsVisible);
        var custom = new BoxView();
        post.Header = custom;
        Assert.Same(custom, post.HeaderBox.Content);
    }

    [Fact]
    public void BlogPost_cover_is_sixteen_by_nine_above_the_text_and_beside_it_from_768_when_horizontal()
    {
        var post = new KlyBlogPost { Title = "Hello", Image = ImageSource.FromFile("cover.png") };
        post.Relayout(320);
        Assert.Equal(180, post.HeaderBox.HeightRequest);
        Assert.Equal(new Thickness(16), post.BodyStack.Padding);

        post.Orientation = StackOrientation.Horizontal;
        post.Relayout(500);
        Assert.Single(post.LayoutGrid.ColumnDefinitions);

        post.Relayout(900);
        Assert.Equal(2, post.LayoutGrid.ColumnDefinitions.Count);
        Assert.Equal(new Thickness(24), post.BodyStack.Padding);
        Assert.Equal(-1, post.HeaderBox.HeightRequest);
    }

    [Theory]
    [InlineData(BlogPostVariant.Outline, 1)]
    [InlineData(BlogPostVariant.Soft, 0)]
    [InlineData(BlogPostVariant.Subtle, 1)]
    [InlineData(BlogPostVariant.Ghost, 0)]
    [InlineData(BlogPostVariant.Naked, 0)]
    public void BlogPost_variant_sets_the_border(BlogPostVariant variant, double stroke)
    {
        var post = new KlyBlogPost { Variant = variant };

        Assert.Equal(stroke, post.StrokeThickness);
    }

    [Fact]
    public void BlogPost_naked_rounds_the_cover_itself_and_has_the_padding_on_one_side_only()
    {
        var post = new KlyBlogPost { Variant = BlogPostVariant.Naked, Title = "Hello" };
        post.Relayout(320);

        Assert.Equal(KlyTheme.Radius * 2, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)post.HeaderBox.StrokeShape!).CornerRadius.TopLeft);
        Assert.Equal(new Thickness(0, 16, 0, 0), post.BodyStack.Padding);
    }

    [Fact]
    public void BlogPost_one_author_is_a_named_avatar_and_several_are_a_group_named_together()
    {
        var post = new KlyBlogPost { Authors = [new KlyPostAuthor("Ada Lovelace") { Description = "Engineer" }] };
        var avatar = Assert.Single(Descendants<KlyAvatar>(post.AuthorSlot));
        Assert.Equal("Ada Lovelace", avatar.Text);
        Assert.Contains(Descendants<Label>(post.AuthorSlot), l => l.Text == "Engineer");

        post.Authors = [new KlyPostAuthor("Ada Lovelace"), new KlyPostAuthor("Grace Hopper")];
        Assert.Equal(2, Descendants<KlyAvatar>(post.AuthorSlot).Count());
        Assert.Equal("Ada Lovelace, Grace Hopper", SemanticProperties.GetDescription((View)post.AuthorSlot.Content!));

        post.Authors = [];
        Assert.False(post.AuthorSlot.IsVisible);
    }

    [Fact]
    public void BlogPost_tap_reports_the_href_once()
    {
        var post = new KlyBlogPost { Title = "Hello" };
        var raised = Record(h => post.Clicked += h);
        post.Tap();
        Assert.Empty(raised);

        post.Href = "/blog/hello";
        post.Tap();
        Assert.Equal("/blog/hello", Assert.Single(raised).Href);
    }

    [Theory]
    [InlineData(StackOrientation.Horizontal, 280, 1)]
    [InlineData(StackOrientation.Horizontal, 700, 2)]
    [InlineData(StackOrientation.Horizontal, 1000, 3)]
    [InlineData(StackOrientation.Vertical, 1000, 1)]
    public void BlogPosts_fits_as_many_posts_per_row_as_288_units_allow(StackOrientation orientation, double width, int columns)
    {
        var posts = new KlyBlogPosts { Orientation = orientation };
        for (var i = 0; i < 4; i++) posts.Children.Add(new KlyBlogPost());
        posts.Relayout(width);

        Assert.Equal(columns, posts.ColumnCount);
    }

    // ---- KlyChangelogVersion, KlyChangelogVersions

    [Fact]
    public void ChangelogVersion_draws_the_dot_and_the_line_and_the_last_entry_has_no_line()
    {
        var version = new KlyChangelogVersion { Title = "1.0" };
        Assert.True(version.Dot.IsVisible);
        Assert.False(version.Line.IsVisible);
        Assert.Equal(new Thickness(32, 0, 0, 0), version.Container.Margin);

        version.IsLast = false;
        Assert.True(version.Line.IsVisible);
        Assert.Equal(new Thickness(32, 0, 0, 48), version.Container.Margin);

        version.Indicator = false;
        Assert.False(version.Dot.IsVisible);
        Assert.False(version.Line.IsVisible);
        Assert.Equal(new Thickness(0), version.Container.Margin);
    }

    [Fact]
    public void ChangelogVersions_tells_the_last_entry_to_end_the_line()
    {
        var versions = new KlyChangelogVersions();
        var first = new KlyChangelogVersion { Title = "2.0" };
        var second = new KlyChangelogVersion { Title = "1.0" };
        versions.Children.Add(first);
        versions.Children.Add(second);

        Assert.True(first.Line.IsVisible);
        Assert.False(second.Line.IsVisible);

        versions.Children.Remove(second);
        Assert.False(first.Line.IsVisible);
    }

    [Fact]
    public void ChangelogVersion_shows_the_date_the_badge_the_authors_and_the_image()
    {
        var version = new KlyChangelogVersion
        {
            Title = "1.2",
            Date = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero),
            Culture = new CultureInfo("en-US"),
            Badge = "Latest",
            BadgeColor = KlyColor.Success,
            Authors = [new KlyPostAuthor("Ada Lovelace"), new KlyPostAuthor("Grace Hopper")],
            Image = ImageSource.FromFile("shot.png"),
            ImageAlt = "A screenshot",
        };

        Assert.Equal("March 4, 2026", version.DateLabel.Text);
        Assert.True(version.BadgeView.IsVisible);
        Assert.Equal(KlyColor.Success, version.BadgeView.Color);
        Assert.Equal(2, version.AuthorRow.Children.Count);
        Assert.True(version.ImageFrame.IsVisible);
        Assert.Equal("A screenshot", SemanticProperties.GetDescription(version.Picture));
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(version.TitleLabel));

        version.Image = null;
        Assert.False(version.ImageFrame.IsVisible);
    }

    // ---- KlyContentNavigation

    [Theory]
    [InlineData("/docs/intro", "/docs/intro", false, true)]
    [InlineData("/docs/intro/", "/docs/intro", false, true)]
    [InlineData("/docs/intro?tab=1#top", "/docs/intro", true, true)]
    [InlineData("/DOCS/Intro", "/docs/intro", false, true)]
    [InlineData("/docs/intro/install", "/docs/intro", false, true)]
    [InlineData("/docs/intro/install", "/docs/intro", true, false)]
    [InlineData("/docs/other", "/docs/intro", false, false)]
    [InlineData("/docs/introduction", "/docs/intro", false, false)]
    [InlineData("/anything", "/", false, false)]
    [InlineData("/", "/", true, true)]
    [InlineData(null, "/docs", false, false)]
    public void ContentNavigation_matches_the_current_address_like_the_web_matcher(string? current, string href, bool exact, bool matches) =>
        Assert.Equal(matches, KlyContentNavigation.Matches(current, href, exact));

    private static KlyContentNavigation Navigation() => new()
    {
        CurrentHref = "/docs/install",
        Items =
        [
            new KlyContentNavigationItem("Start") { Children = [new KlyContentNavigationItem("Intro") { Href = "/docs/intro", Icon = KlyIconNames.File }, new KlyContentNavigationItem("Install") { Href = "/docs/install", Badge = "New" }] },
            new KlyContentNavigationItem("Guides") { Children = [new KlyContentNavigationItem("Theming") { Href = "/docs/theming" }] },
            new KlyContentNavigationItem("Off") { Href = "/docs/off", Disabled = true },
        ],
    };

    [Fact]
    public void ContentNavigation_marks_the_current_page_and_opens_the_group_that_holds_it()
    {
        var nav = Navigation();
        var start = nav.Items![0];
        var guides = nav.Items[1];

        Assert.True(nav.IsActive(start));
        Assert.True(nav.IsActive(start.Children![1]));
        Assert.False(nav.IsActive(start.Children[0]));
        Assert.True(nav.IsOpen(start));
        Assert.False(nav.IsOpen(guides));
        Assert.Equal(3, nav.Root.Children.Count);
    }

    [Fact]
    public void ContentNavigation_toggle_opens_and_closes_a_group_and_a_group_can_start_open()
    {
        var nav = Navigation();
        var guides = nav.Items![1];

        nav.Toggle(guides);
        Assert.True(nav.IsOpen(guides));
        nav.Toggle(guides);
        Assert.False(nav.IsOpen(guides));

        var opened = new KlyContentNavigationItem("Closed") { Open = true, Children = [new KlyContentNavigationItem("Page") { Href = "/x" }] };
        nav.Items = [opened];
        Assert.True(nav.IsOpen(opened));
    }

    [Fact]
    public void ContentNavigation_forced_active_wins_over_the_address()
    {
        var nav = new KlyContentNavigation { CurrentHref = "/a", Items = [new KlyContentNavigationItem("A") { Href = "/a", Active = false }, new KlyContentNavigationItem("B") { Href = "/b", Active = true }] };

        Assert.False(nav.IsActive(nav.Items![0]));
        Assert.True(nav.IsActive(nav.Items[1]));
    }

    [Fact]
    public void ContentNavigation_a_followed_entry_is_reported_and_a_disabled_one_is_not()
    {
        var nav = Navigation();
        var raised = Record(h => nav.LinkClicked += h);

        nav.Activate(nav.Items![0].Children![0]);
        nav.Activate(nav.Items[2]);

        var link = Assert.Single(raised);
        Assert.Equal("/docs/intro", link.Href);
        Assert.Equal("Intro", Assert.IsType<KlyContentNavigationItem>(link.Item).Title);
    }

    [Fact]
    public void ContentNavigation_rows_are_touch_sized_the_badge_is_neutral_and_disabled_rows_dim()
    {
        var nav = Navigation();
        var rows = Descendants<Border>(nav).Where(b => b.MinimumHeightRequest > 0).ToList();

        Assert.All(rows, r => Assert.True(r.MinimumHeightRequest >= 40));
        var badge = Assert.Single(Descendants<KlyBadge>(nav));
        Assert.Equal(KlyColor.Neutral, badge.Color);
        Assert.Equal(KlySize.Xs, badge.Size);
        var off = nav.Root.Children.Cast<View>().Last();
        Assert.Equal(0.6, off.Opacity);
        Assert.False(off.IsEnabled);
        Assert.Equal("Documentation", SemanticProperties.GetDescription(nav));
    }

    // ---- KlyContentSearch, KlyContentSearchButton

    private static KlyContentSearch Search() => new()
    {
        Pages =
        [
            new KlyContentSearchPage("Installation", "/docs/install") { Group = "Guides", Description = "Get started", Keywords = "setup nuget", Headings = [new KlyContentSearchHeading("Requirements", "requirements")] },
            new KlyContentSearchPage("Theming", "/docs/theming") { Group = "Guides", Icon = "" },
            new KlyContentSearchPage("About", "/about"),
        ],
    };

    [Fact]
    public void ContentSearch_lists_pages_and_their_headings_in_order_without_a_query()
    {
        var search = Search();
        var all = search.Search(null);

        Assert.Equal(["Installation", "Requirements", "Theming", "About"], all.Select(r => r.Label));
        Assert.Equal("/docs/install#requirements", all[1].Href);
        Assert.Equal("Installation", all[1].Description);
        Assert.Equal([KlyIconNames.File, KlyIconNames.Hash, null, KlyIconNames.File], all.Select(r => r.Icon));
    }

    [Theory]
    [InlineData("install", new[] { "Installation", "Requirements" })]
    [InlineData("NUGET", new[] { "Installation" })]
    [InlineData("started install", new[] { "Installation" })]
    [InlineData("requirements", new[] { "Requirements" })]
    [InlineData("theming", new[] { "Theming" })]
    [InlineData("zzz", new string[0])]
    public void ContentSearch_every_word_of_the_query_must_be_found(string query, string[] labels) =>
        Assert.Equal(labels, Search().Search(query).Select(r => r.Label));

    [Fact]
    public void ContentSearch_shows_groups_with_the_ungrouped_results_first_and_the_empty_text_without_results()
    {
        var search = Search();

        var children = search.ResultList.Children;
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel((Label)children[1]));
        Assert.Equal("About", Descendants<Label>(search.ResultList).First().Text);
        Assert.Contains(Descendants<Label>(search.ResultList), l => l.Text == "Guides");

        search.Text = "zzz";
        var empty = Assert.IsType<Label>(Assert.Single(search.ResultList.Children));
        Assert.Equal("No results", empty.Text);

        search.EmptyText = "Rien";
        Assert.Equal("Rien", empty.Text);
    }

    [Fact]
    public void ContentSearch_caps_the_number_of_results_it_builds()
    {
        var search = Search();
        search.MaxResults = 2;

        Assert.Equal(2, search.Search(null).Count);
        Assert.Equal(2, Descendants<Border>(search.ResultList).Count(b => b.MinimumHeightRequest >= 40));
    }

    [Fact]
    public void ContentSearch_text_goes_both_ways_and_typing_opens_the_results()
    {
        var search = Search();
        var changes = new List<string?>();
        search.TextChanged += (_, e) => changes.Add(e.NewTextValue);
        Assert.False(search.IsOpen);
        Assert.False(search.Panel.IsVisible);

        search.Bar.Text = "theming";
        Assert.Equal("theming", search.Text);
        Assert.True(search.IsOpen);
        Assert.True(search.Panel.IsVisible);

        search.Text = "install";
        Assert.Equal("install", search.Bar.Text);
        Assert.Equal(["theming", "install"], changes);
    }

    [Fact]
    public void ContentSearch_choosing_a_result_reports_it_clears_the_text_and_closes()
    {
        var search = Search();
        var raised = Record(h => search.ResultSelected += h);
        var opened = 0;
        search.OpenChanged += (_, _) => opened++;
        search.Text = "requirements";

        search.Select(search.Search("requirements").Single());

        var result = Assert.Single(raised);
        Assert.Equal("/docs/install#requirements", result.Href);
        Assert.Equal("requirements", Assert.IsType<KlyContentSearchHeading>(result.Item).Id);
        Assert.Null(search.Text);
        Assert.False(search.IsOpen);
        Assert.Equal(2, opened);
    }

    [Fact]
    public void ContentSearch_names_the_field_and_takes_a_placeholder()
    {
        var search = new KlyContentSearch { Placeholder = "Chercher", AccessibleName = "Recherche" };

        Assert.Equal("Chercher", search.Bar.Placeholder);
        Assert.Equal("Recherche", SemanticProperties.GetDescription(search.Bar));
        Assert.Equal("Search the documentation...", new KlyContentSearch().Placeholder);
    }

    [Fact]
    public void ContentSearchButton_is_a_square_icon_button_until_it_is_expanded()
    {
        var button = new KlyContentSearchButton();
        Assert.True(button.Collapsed);
        Assert.False(button.LabelView.IsVisible);
        Assert.False(button.KeyCaps.IsVisible);
        Assert.Equal(button.Button.MinimumHeightRequest, button.Button.MinimumWidthRequest);
        Assert.Equal("Search", SemanticProperties.GetDescription(button.Button));
        Assert.Equal(ButtonVariant.Outline, button.Variant);
        Assert.Equal(KlyColor.Neutral, button.Color);

        button.Collapsed = false;
        Assert.True(button.LabelView.IsVisible);
        Assert.Equal("Search", button.LabelView.Text);
        Assert.Equal(2, button.KeyCaps.Children.Count);
        Assert.Equal(-1, button.Button.MinimumWidthRequest);
    }

    [Fact]
    public void ContentSearchButton_shows_the_given_keys_or_none_and_forwards_color_variant_and_size()
    {
        var button = new KlyContentSearchButton { Collapsed = false, Keys = ["Alt", "S"], Color = KlyColor.Primary, Variant = ButtonVariant.Ghost, Size = KlySize.Lg };

        Assert.Equal(["Alt", "S"], button.KeyTexts);
        Assert.Equal(2, button.KeyCaps.Children.Count);
        Assert.Equal(KlyColor.Primary, button.Button.Color);
        Assert.Equal(ButtonVariant.Ghost, button.Button.Variant);
        Assert.Equal(KlySize.Lg, button.Button.Size);

        button.Keys = [];
        Assert.False(button.KeyCaps.IsVisible);
        Assert.Equal(["Ctrl", "K"], new KlyContentSearchButton { Keys = null }.KeyTexts);
    }

    [Fact]
    public void ContentSearchButton_opens_its_search_and_raises_Clicked_once()
    {
        var search = new KlyContentSearch();
        var button = new KlyContentSearchButton { Search = search };
        var clicks = 0;
        button.Clicked += (_, _) => clicks++;

        button.Button.SendClicked();

        Assert.Equal(1, clicks);
        Assert.True(search.IsOpen);
    }

    [Fact]
    public void ContentSearchButton_disabled_dims_like_the_other_buttons()
    {
        var button = new KlyContentSearchButton { IsEnabled = false };

        Assert.False(button.Button.IsEnabled);
        Assert.Equal(0.6, button.Opacity);
        button.IsEnabled = true;
        Assert.Equal(1, button.Opacity);
    }

    // ---- KlyContentSurround

    [Fact]
    public void ContentSurround_is_hidden_without_a_link_and_draws_two_columns_from_640()
    {
        var surround = new KlyContentSurround();
        Assert.False(surround.IsVisible);

        surround.Next = new KlyContentSurroundLink("Install", "/docs/install") { Description = "Get started" };
        surround.Relayout(700);
        Assert.True(surround.IsVisible);
        Assert.Equal(2, surround.LayoutGrid.ColumnDefinitions.Count);
        // Without a previous page the next one stays in the second column.
        Assert.Equal(1, Grid.GetColumn((BindableObject)surround.LayoutGrid.Children[0]));

        surround.Previous = new KlyContentSurroundLink("Intro", "/docs/intro");
        Assert.Equal(2, surround.LayoutGrid.Children.Count);
        Assert.Equal(0, Grid.GetColumn((BindableObject)surround.LayoutGrid.Children[0]));

        surround.Relayout(400);
        Assert.Single(surround.LayoutGrid.ColumnDefinitions);
        Assert.Equal([0, 1], surround.LayoutGrid.Children.Select(c => Grid.GetRow((BindableObject)c)));
    }

    [Fact]
    public void ContentSurround_names_the_cards_and_reports_a_tap()
    {
        var surround = new KlyContentSurround { Previous = new KlyContentSurroundLink("Intro", "/docs/intro"), Next = new KlyContentSurroundLink("Install", "/docs/install"), PreviousLabel = "Précédent" };
        var raised = Record(h => surround.LinkClicked += h);

        var cards = surround.LayoutGrid.Children.Cast<View>().ToList();
        Assert.Equal("Précédent: Intro", SemanticProperties.GetDescription(cards[0]));
        Assert.Equal("Next: Install", SemanticProperties.GetDescription(cards[1]));
        Assert.Equal("Previous and next pages", SemanticProperties.GetDescription(surround));

        surround.Activate(surround.Next!);
        Assert.Equal("/docs/install", Assert.Single(raised).Href);
    }

    // ---- KlyContentToc

    private static KlyContentToc Toc() => new()
    {
        Links = [new KlyContentTocLink("intro", "Intro"), new KlyContentTocLink("install", "Install") { Level = 3 }, new KlyContentTocLink("deep", "Deep") { Level = 9 }],
    };

    [Fact]
    public void ContentToc_folds_behind_its_title_below_the_breakpoint_and_opens_by_tap()
    {
        var toc = Toc();
        toc.Relayout(800);
        Assert.True(toc.IsFolded);
        Assert.True(toc.TriggerRow.IsVisible);
        Assert.False(toc.TitleLabel.IsVisible);
        Assert.False(toc.ListHost.IsVisible);
        Assert.Equal("On this page", toc.TriggerLabel.Text);

        toc.IsOpen = true;
        Assert.True(toc.ListHost.IsVisible);

        toc.Relayout(1200);
        Assert.False(toc.IsFolded);
        Assert.False(toc.TriggerRow.IsVisible);
        Assert.True(toc.TitleLabel.IsVisible);
        Assert.True(toc.ListHost.IsVisible);
    }

    [Theory]
    [InlineData(KlyContentTocBreakpoint.Lg, 1100, false)]
    [InlineData(KlyContentTocBreakpoint.Xl, 1100, true)]
    [InlineData(KlyContentTocBreakpoint.Xl, 1300, false)]
    [InlineData(KlyContentTocBreakpoint.Xxl, 1300, true)]
    [InlineData(KlyContentTocBreakpoint.Xxl, 1600, false)]
    public void ContentToc_breakpoint_is_1024_1280_or_1536(KlyContentTocBreakpoint breakpoint, double width, bool folded)
    {
        var toc = Toc();
        toc.Breakpoint = breakpoint;
        toc.Relayout(width);

        Assert.Equal(folded, toc.IsFolded);
    }

    [Fact]
    public void ContentToc_without_collapsible_is_always_shown()
    {
        var toc = Toc();
        toc.Collapsible = false;
        toc.Relayout(300);

        Assert.False(toc.IsFolded);
        Assert.False(toc.TriggerRow.IsVisible);
        Assert.True(toc.ListHost.IsVisible);
    }

    [Fact]
    public void ContentToc_indents_deeper_headings_up_to_level_5_and_every_row_is_touch_sized()
    {
        var toc = Toc();

        Assert.Equal([12d, 24d, 48d], toc.Rows.Select(r => r.Label.Margin.Left));
        Assert.All(toc.Rows, r => Assert.True(r.Row.MinimumHeightRequest >= 40));
        Assert.Equal(3, toc.List.Children.Count);
    }

    [Fact]
    public void ContentToc_tap_reports_the_heading_marks_it_active_and_can_be_handled()
    {
        var toc = Toc();
        var raised = new List<PageLinkEventArgs>();
        toc.LinkClicked += (_, e) => raised.Add(e);

        toc.Activate(toc.Links![1]);

        Assert.Equal("#install", Assert.Single(raised).Href);
        Assert.Equal("install", toc.ActiveId);
        Assert.Equal(FontAttributes.Bold, toc.Rows[1].Label.FontAttributes);
        Assert.Equal(FontAttributes.None, toc.Rows[0].Label.FontAttributes);

        toc.LinkClicked += (_, e) => e.Handled = true;
        toc.Activate(toc.Links[2]);
        Assert.Equal("install", toc.ActiveId);
    }

    [Fact]
    public void ContentToc_scroll_spy_picks_the_last_heading_above_the_top_quarter_or_the_first_one()
    {
        var toc = Toc();
        var offsets = new Dictionary<string, double> { ["intro"] = 100, ["install"] = 600, ["deep"] = 1400 };

        toc.Spy(0, 800, link => offsets[link.Id]);
        Assert.Equal("intro", toc.ActiveId);

        // The line sits a quarter of the viewport (200) under the top of the scrolled content.
        toc.Spy(450, 800, link => offsets[link.Id]);
        Assert.Equal("install", toc.ActiveId);

        toc.Spy(1300, 800, link => offsets[link.Id]);
        Assert.Equal("deep", toc.ActiveId);

        toc.Spy(0, 800, link => link.Id == "deep" ? null : offsets[link.Id]);
        Assert.Equal("intro", toc.ActiveId);
    }

    [Theory]
    [InlineData(new double[0], 100, -1)]
    [InlineData(new[] { 300d, 600d }, 100, 0)]
    [InlineData(new[] { 300d, 600d }, 300, 0)]
    [InlineData(new[] { 300d, 600d }, 700, 1)]
    public void Scroll_spy_rule_is_the_last_heading_at_or_above_the_line_else_the_first(double[] offsets, double line, int expected) =>
        Assert.Equal(expected, PagesScroll.ActiveIndex(offsets, line));

    [Fact]
    public void ContentToc_title_names_the_list_and_the_color_marks_the_active_heading()
    {
        var toc = Toc();
        toc.Title = "Sur cette page";
        toc.Color = KlyColor.Secondary;
        toc.Activate(toc.Links![0]);

        Assert.Equal("Sur cette page", toc.TriggerLabel.Text);
        Assert.Equal("Sur cette page", toc.TitleLabel.Text);
        Assert.Equal("Sur cette page", SemanticProperties.GetDescription(toc.List));
        Assert.Equal("Intro", SemanticProperties.GetDescription(toc.Rows[0].Row));
    }

    // ---- links

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com/a", true)]
    [InlineData("mailto:ada@example.com", true)]
    [InlineData("tel:+33123456789", true)]
    [InlineData("/docs/intro", false)]
    [InlineData("#section", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData(null, false)]
    public void Only_absolute_web_mail_and_phone_addresses_leave_the_app(string? href, bool external) =>
        Assert.Equal(external, PagesLinks.IsExternal(href, out _));
}
