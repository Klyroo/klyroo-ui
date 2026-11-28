using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using Klyroo.UI.Maui;

namespace Klyroo.UI.Maui.Tests;

public class DisplayTests
{
    // --- helpers ---------------------------------------------------------------------------------------------------------------------------

    private static IEnumerable<object> Walk(object root)
    {
        yield return root;
        IEnumerable<object?> kids = root switch
        {
            Layout layout => layout.Children.Cast<object?>(),
            Border border => [border.Content],
            ContentView content => [content.Content],
            ScrollView scroll => [scroll.Content],
            _ => [],
        };
        foreach (var kid in kids)
        {
            if (kid is null) continue;
            foreach (var descendant in Walk(kid)) yield return descendant;
        }
    }

    private static List<T> All<T>(object root) => Walk(root).OfType<T>().ToList();

    private static List<string> Texts(object root) => All<Label>(root).Where(l => l.IsVisible && !string.IsNullOrEmpty(l.Text)).Select(l => l.Text!).ToList();

    private static Label LabelWith(object root, string text) => All<Label>(root).First(l => l.Text == text);

    private static double Radius(Border border) => ((RoundRectangle)border.StrokeShape!).CornerRadius.TopLeft;

    private static CornerRadius Corners(Border border) => ((RoundRectangle)border.StrokeShape!).CornerRadius;

    private static (View Body, View Skeleton) Frame(KlyBlockBase block)
    {
        var grid = Assert.IsType<Grid>(block.Content);
        return ((View)grid.Children[0], (View)grid.Children[1]);
    }

    private static CultureInfo Invariant()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        return CultureInfo.InvariantCulture;
    }

    // --- KlySkeleton and the block frame ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Skeleton_is_a_pulsing_placeholder_hidden_from_assistive_technology()
    {
        var skeleton = new KlySkeleton();

        Assert.Equal(16, skeleton.HeightRequest);
        Assert.Equal(KlyTheme.RadiusOf(1), Radius(skeleton));
        Assert.False(AutomationProperties.GetIsInAccessibleTree(skeleton));
        Assert.False(skeleton.IsPulsing);
        Assert.Equal(1, skeleton.Opacity);

        skeleton.Circle = true;
        Assert.Equal(9999, Radius(skeleton));
    }

    [Fact]
    public void Block_surface_sets_the_padding_the_border_and_the_background_of_each_treatment()
    {
        var block = new KlyFacts();
        Assert.Equal(KlySurface.Bordered, block.EffectiveSurface);
        Assert.Equal(new Thickness(16), block.Padding);
        Assert.Equal(1, block.StrokeThickness);
        Assert.Null(block.Shadow);

        block.Surface = KlySurface.Plain;
        Assert.Equal(new Thickness(0), block.Padding);
        Assert.Equal(0, block.StrokeThickness);

        block.Surface = KlySurface.Muted;
        Assert.Equal(new Thickness(16), block.Padding);
        Assert.Equal(0, block.StrokeThickness);

        block.Surface = KlySurface.Elevated;
        Assert.Equal(1, block.StrokeThickness);
        Assert.NotNull(block.Shadow);

        block.Surface = null;
        Assert.Equal(KlySurface.Bordered, block.EffectiveSurface);
    }

    [Fact]
    public void Blocks_have_their_own_default_surface()
    {
        Assert.Equal(KlySurface.Plain, new KlyHeading().EffectiveSurface);
        Assert.Equal(KlySurface.Plain, new KlyText().EffectiveSurface);
        Assert.Equal(KlySurface.Plain, new KlyList().EffectiveSurface);
        Assert.Equal(KlySurface.Plain, new KlyDiff().EffectiveSurface);
        Assert.Equal(KlySurface.Plain, new KlyDateTile().EffectiveSurface);
        Assert.Equal(KlySurface.Plain, new KlyMeter().EffectiveSurface);
        Assert.Equal(KlySurface.Plain, new KlyImageGallery().EffectiveSurface);
        Assert.Equal(KlySurface.Muted, new KlyStat<int>().EffectiveSurface);
        Assert.Equal(KlySurface.Bordered, new KlyStatGroup<int>().EffectiveSurface);
        Assert.Equal(KlySurface.Bordered, new KlyFacts().EffectiveSurface);
        Assert.Equal(KlySurface.Bordered, new KlyBreakdown().EffectiveSurface);
        Assert.Equal(KlySurface.Bordered, new KlyRecordList().EffectiveSurface);
    }

    [Fact]
    public void Loading_shows_a_skeleton_instead_of_the_content()
    {
        var facts = new KlyFacts { Items = [new KlyFact("Plan") { Value = "Pro" }] };
        var (body, skeleton) = Frame(facts);
        Assert.True(body.IsVisible);
        Assert.False(skeleton.IsVisible);

        facts.Loading = true;
        Assert.False(body.IsVisible);
        Assert.True(skeleton.IsVisible);
        Assert.Equal(3, All<KlySkeleton>(skeleton).Count);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(skeleton));

        facts.Loading = false;
        Assert.True(body.IsVisible);
    }

    // --- KlyAvatarGroup --------------------------------------------------------------------------------------------------------------------

    private static List<KlyAvatarItem> People(int count) =>
        Enumerable.Range(1, count).Select(i => new KlyAvatarItem(Alt: $"Person {i}")).ToList();

    [Fact]
    public void AvatarGroup_shows_every_avatar_without_a_max()
    {
        var group = new KlyAvatarGroup { Items = People(5) };

        Assert.Equal(5, group.Children.Count);
        Assert.Equal(-12, group.Spacing);
        Assert.All(group.Children, c => Assert.IsType<KlyAvatar>(Assert.IsType<Border>(c).Content));
    }

    [Fact]
    public void AvatarGroup_collapses_the_avatars_over_max_into_a_counter()
    {
        var group = new KlyAvatarGroup { Items = People(5), Max = 3, MoreFormat = "{0} others" };

        Assert.Equal(4, group.Children.Count);
        var counter = Assert.IsType<Border>(((Border)group.Children[3]).Content);
        Assert.Equal("+2", Assert.IsType<Label>(counter.Content).Text);
        Assert.Equal("2 others", SemanticProperties.GetDescription(counter));

        group.Max = 5;
        Assert.Equal(5, group.Children.Count);
        group.Max = 0;
        Assert.Single(group.Children);
    }

    [Fact]
    public void AvatarGroup_names_each_avatar_with_its_description_and_badge_and_takes_the_initials_from_the_name()
    {
        var group = new KlyAvatarGroup
        {
            Items = [new KlyAvatarItem(Alt: "Ada Lovelace") { Description = "Engineer", Badge = new KlyBadgeItem("Admin") }, new KlyAvatarItem(Alt: "Grace", Text: "GH")],
            AccessibleName = "Project members",
        };

        var first = Assert.IsType<KlyAvatar>(((Border)group.Children[0]).Content);
        var second = Assert.IsType<KlyAvatar>(((Border)group.Children[1]).Content);
        Assert.Equal("Ada Lovelace · Engineer · Admin", first.Alt);
        Assert.Equal("Grace", second.Alt);
        Assert.Equal("Project members", SemanticProperties.GetDescription(group));
        Assert.Equal("AL", KlyAvatar.Initials(first.Text));
        Assert.Equal("GH", KlyAvatar.Initials(second.Text));
    }

    [Theory]
    [InlineData(KlySize.Xs, 24)]
    [InlineData(KlySize.Sm, 28)]
    [InlineData(KlySize.Md, 32)]
    [InlineData(KlySize.Lg, 40)]
    [InlineData(KlySize.Xl, 48)]
    public void AvatarGroup_size_is_the_size_of_every_avatar_and_the_ring_wraps_it(KlySize size, double diameter)
    {
        var group = new KlyAvatarGroup { Items = People(2), Size = size };

        var ring = (Border)group.Children[0];
        Assert.Equal(2, ring.StrokeThickness);
        Assert.Equal(diameter / 2 + 2, Radius(ring));
        Assert.Equal(diameter, ((KlyAvatar)ring.Content!).WidthRequest);
        Assert.Equal(size, ((KlyAvatar)ring.Content!).Size);
    }

    // --- KlyBanner -------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Banner_shows_its_texts_and_a_rich_body_wins_over_the_description()
    {
        var banner = new KlyBanner { Title = "Sale", Description = "20% off" };
        Assert.Contains("Sale", Texts(banner));
        Assert.Contains("20% off", Texts(banner));

        var body = new Label { Text = "Rich" };
        banner.Body = body;
        Assert.Contains("Rich", All<Label>(banner).Select(l => l.Text));
        Assert.DoesNotContain("20% off", Texts(banner));
        Assert.Equal(KlyColor.Primary, banner.Color);
    }

    [Fact]
    public void Banner_close_button_hides_the_banner_and_raises_closed_once()
    {
        var banner = new KlyBanner { Closable = true };
        var closed = 0;
        banner.Closed += (_, _) => closed++;
        var close = All<DisplayIconButton>(banner).Single();
        Assert.True(close.IsVisible);

        close.SendClicked();

        Assert.False(banner.IsVisible);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void Banner_close_button_is_hidden_until_it_is_closable()
    {
        var banner = new KlyBanner();

        Assert.False(All<DisplayIconButton>(banner).Single().IsVisible);
        Assert.Equal("Close", SemanticProperties.GetDescription(All<DisplayIconButton>(banner).Single()));

        banner.CloseLabel = "Fermer";
        Assert.Equal("Fermer", SemanticProperties.GetDescription(All<DisplayIconButton>(banner).Single()));
    }

    [Fact]
    public void Banner_with_an_id_stays_hidden_once_it_was_dismissed()
    {
        var id = "banner-" + Guid.NewGuid();
        var first = new KlyBanner { Id = id, Closable = true };
        Assert.True(first.IsVisible);
        All<DisplayIconButton>(first).Single().SendClicked();

        var later = new KlyBanner { Id = id };
        Assert.False(later.IsVisible);

        var other = new KlyBanner { Id = "banner-" + Guid.NewGuid() };
        Assert.True(other.IsVisible);
    }

    [Fact]
    public void Banner_without_an_id_is_visible_again_when_it_is_created_again()
    {
        var first = new KlyBanner { Closable = true };
        All<DisplayIconButton>(first).Single().SendClicked();

        Assert.True(new KlyBanner().IsVisible);
    }

    [Fact]
    public void Banner_title_is_underlined_when_it_links_somewhere()
    {
        var banner = new KlyBanner { Title = "Read more", Href = "https://example.com", Target = KlyLinkTarget.Blank };

        Assert.Equal(TextDecorations.Underline, LabelWith(banner, "Read more").TextDecorations);
        Assert.Equal(KlyLinkTarget.Blank, banner.Target);
    }

    // --- KlyChip ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Chip_without_a_text_is_a_dot_hidden_from_assistive_technology()
    {
        var chip = new KlyChip { Content = new Label { Text = "Inbox" } };
        var ring = (Border)chip.Children[1];
        var indicator = (Border)ring.Content!;

        Assert.Same(chip.Content, chip.Children[0]);
        Assert.Null(indicator.Content);
        Assert.Equal(10, indicator.WidthRequest);
        Assert.Equal(10, indicator.HeightRequest);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(ring));
    }

    [Theory]
    [InlineData(KlySize.Xs, 6)]
    [InlineData(KlySize.Sm, 8)]
    [InlineData(KlySize.Md, 10)]
    [InlineData(KlySize.Lg, 12)]
    [InlineData(KlySize.Xl, 14)]
    public void Chip_dot_size_follows_the_size(KlySize size, double dot)
    {
        var chip = new KlyChip { Size = size };

        var indicator = (Border)((Border)chip.Children[0]).Content!;
        Assert.Equal(dot, indicator.WidthRequest);
        Assert.Equal(dot, indicator.HeightRequest);
    }

    [Theory]
    [InlineData(KlySize.Xs, 14, 10)]
    [InlineData(KlySize.Sm, 16, 10)]
    [InlineData(KlySize.Md, 16, 10)]
    [InlineData(KlySize.Lg, 20, 12)]
    [InlineData(KlySize.Xl, 24, 14)]
    public void Chip_count_has_a_minimum_size_and_a_font_of_its_size(KlySize size, double height, double font)
    {
        var chip = new KlyChip { Text = "3", Size = size };

        var indicator = (Border)((Border)chip.Children[0]).Content!;
        var label = Assert.IsType<Label>(indicator.Content);
        Assert.Equal("3", label.Text);
        Assert.Equal(font, label.FontSize);
        Assert.Equal(height, indicator.HeightRequest);
        Assert.Equal(height, indicator.MinimumWidthRequest);
    }

    [Fact]
    public void Chip_text_is_read_as_it_is_and_an_accessible_name_replaces_it()
    {
        var chip = new KlyChip { Text = "3" };
        var ring = (Border)chip.Children[0];
        Assert.True(AutomationProperties.GetIsInAccessibleTree(ring));

        chip.AccessibleName = "3 unread messages";
        Assert.Equal("3 unread messages", SemanticProperties.GetDescription(ring));

        var dot = new KlyChip { AccessibleName = "Online" };
        Assert.True(AutomationProperties.GetIsInAccessibleTree((Border)dot.Children[0]));
    }

    [Theory]
    [InlineData(KlyChipPosition.TopRight, LayoutAlignment.End, LayoutAlignment.Start, 1, -1)]
    [InlineData(KlyChipPosition.TopLeft, LayoutAlignment.Start, LayoutAlignment.Start, -1, -1)]
    [InlineData(KlyChipPosition.BottomRight, LayoutAlignment.End, LayoutAlignment.End, 1, 1)]
    [InlineData(KlyChipPosition.BottomLeft, LayoutAlignment.Start, LayoutAlignment.End, -1, 1)]
    public void Chip_straddles_its_corner_by_half_its_size(KlyChipPosition position, LayoutAlignment horizontal, LayoutAlignment vertical, int signX, int signY)
    {
        var chip = new KlyChip { Position = position };
        var ring = (Border)chip.Children[0];

        chip.Reposition(20, 12);

        Assert.Equal(horizontal, ring.HorizontalOptions.Alignment);
        Assert.Equal(vertical, ring.VerticalOptions.Alignment);
        Assert.Equal(signX * 10, ring.TranslationX);
        Assert.Equal(signY * 6, ring.TranslationY);
    }

    [Fact]
    public void Chip_inset_and_standalone_stay_in_place()
    {
        var chip = new KlyChip { Inset = true };
        var ring = (Border)chip.Children[0];
        chip.Reposition(20, 12);
        Assert.Equal(0, ring.TranslationX);
        Assert.Equal(0, ring.TranslationY);

        chip.Inset = false;
        chip.Standalone = true;
        chip.Reposition(20, 12);
        Assert.Equal(0, ring.TranslationX);
        Assert.Equal(LayoutAlignment.Center, ring.HorizontalOptions.Alignment);
    }

    [Fact]
    public void Chip_show_hides_the_indicator_and_keeps_the_content()
    {
        var content = new Label { Text = "Inbox" };
        var chip = new KlyChip { Content = content };
        var ring = (Border)chip.Children[1];
        Assert.True(ring.IsVisible);

        chip.Show = false;

        Assert.False(ring.IsVisible);
        Assert.Same(content, chip.Children[0]);

        chip.Content = null;
        Assert.Single(chip.Children);
    }

    // --- KlyKbd ----------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(KlySize.Xs, 16, 10)]
    [InlineData(KlySize.Sm, 16, 11)]
    [InlineData(KlySize.Md, 20, 12)]
    [InlineData(KlySize.Lg, 24, 14)]
    [InlineData(KlySize.Xl, 28, 16)]
    public void Kbd_size_sets_the_height_the_minimum_width_and_the_font(KlySize size, double height, double font)
    {
        var kbd = new KlyKbd { Value = "K", Size = size };

        Assert.Equal(height, kbd.HeightRequest);
        Assert.Equal(height, kbd.MinimumWidthRequest);
        var label = Assert.IsType<Label>(kbd.Content);
        Assert.Equal("K", label.Text);
        Assert.Equal(font, label.FontSize);
    }

    [Fact]
    public void Kbd_is_an_outline_neutral_key_with_the_small_radius_and_variants_change_the_border()
    {
        var kbd = new KlyKbd { Value = "Ctrl" };

        Assert.Equal(KlyVariant.Outline, kbd.Variant);
        Assert.Equal(KlyColor.Neutral, kbd.Color);
        Assert.Equal(KlySize.Md, kbd.Size);
        Assert.Equal(KlyTheme.RadiusOf(0.5), Radius(kbd));
        Assert.Equal(1, kbd.StrokeThickness);

        kbd.Variant = KlyVariant.Soft;
        Assert.Equal(0, kbd.StrokeThickness);
        kbd.Variant = KlyVariant.Solid;
        Assert.Equal(1, kbd.StrokeThickness);
    }

    // --- KlyLink ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Link_opens_its_address_with_its_target_and_raises_clicked_first()
    {
        var order = new List<string>();
        var link = new KlyLink { Text = "Docs", Href = "https://example.com/docs", Target = KlyLinkTarget.Blank };
        (Uri, KlyLinkTarget?)? opened = null;
        link.Opener = (uri, target) =>
        {
            order.Add("open");
            opened = (uri, target);
            return Task.CompletedTask;
        };
        link.Clicked += (_, _) => order.Add("clicked");

        link.SendTapped();

        Assert.Equal(["clicked", "open"], order);
        Assert.Equal(new Uri("https://example.com/docs"), opened!.Value.Item1);
        Assert.Equal(KlyLinkTarget.Blank, opened.Value.Item2);
    }

    [Fact]
    public void Link_without_an_absolute_address_only_raises_clicked()
    {
        var link = new KlyLink { Text = "Docs", Href = "/docs" };
        var opened = 0;
        link.Opener = (_, _) =>
        {
            opened++;
            return Task.CompletedTask;
        };
        var clicks = 0;
        link.Clicked += (_, _) => clicks++;

        link.SendTapped();

        Assert.Equal(1, clicks);
        Assert.Equal(0, opened);
    }

    [Fact]
    public void Link_that_is_disabled_does_nothing_and_dims()
    {
        var link = new KlyLink { Href = "https://example.com", IsEnabled = false };
        var clicks = 0;
        link.Clicked += (_, _) => clicks++;

        link.SendTapped();

        Assert.Equal(0, clicks);
        Assert.Equal(0.6, link.Opacity);
        link.IsEnabled = true;
        Assert.Equal(1, link.Opacity);
    }

    [Fact]
    public void Link_uri_is_the_href_as_a_uri_and_a_touch_target_of_forty()
    {
        var link = new KlyLink { Uri = new Uri("mailto:ada@example.com") };

        Assert.Equal("mailto:ada@example.com", link.Href);
        Assert.Equal(new Uri("mailto:ada@example.com"), link.Uri);
        Assert.Equal(40, link.MinimumHeightRequest);
        Assert.False(link.Active);

        link.Href = null;
        Assert.Null(link.Uri);
    }

    [Fact]
    public void Link_that_opens_outside_the_app_says_so_to_a_screen_reader()
    {
        var link = new KlyLink { Href = "https://example.com" };
        Assert.Null(SemanticProperties.GetHint(link));

        link.Target = KlyLinkTarget.Blank;
        Assert.Equal("Opens in a new tab", SemanticProperties.GetHint(link));

        link.OpensInNewTabLabel = "S'ouvre dans un nouvel onglet";
        Assert.Equal("S'ouvre dans un nouvel onglet", SemanticProperties.GetHint(link));
    }

    [Fact]
    public void LinkTarget_keeps_the_names_of_the_web_targets()
    {
        Assert.Equal("_self", default(KlyLinkTarget).Value);
        Assert.True(KlyLinkTarget.Blank.IsBlank);
        Assert.False(KlyLinkTarget.Top.IsBlank);
        Assert.Equal("_parent", KlyLinkTarget.Parent.ToString());
        Assert.Equal("frame", KlyLinkTarget.Named("frame").Value);
        Assert.Throws<ArgumentException>(() => KlyLinkTarget.Named("_blank"));
        Assert.True(KlyLinkTarget.Blank == KlyLinkTarget.Blank);
        Assert.True(KlyLinkTarget.Blank != KlyLinkTarget.Self);
    }

    // --- KlyProgress -----------------------------------------------------------------------------------------------------------------------

    private static Border Track(KlyProgress progress) => (Border)progress.Children[1];

    private static Border Indicator(KlyProgress progress) => (Border)((AbsoluteLayout)Track(progress).Content!).Children[0];

    [Fact]
    public void Progress_without_a_value_is_indeterminate_with_a_bar_of_forty_percent()
    {
        var progress = new KlyProgress();

        Assert.True(progress.IsIndeterminate);
        Assert.Null(progress.ValueText);
        Assert.Equal(0.4, AbsoluteLayout.GetLayoutBounds(Indicator(progress)).Width);
        Assert.False(((View)progress.Children[0]).IsVisible);
        Assert.Equal("Progress", SemanticProperties.GetDescription(progress));
    }

    [Fact]
    public void Progress_value_fills_the_bar_and_is_clamped_to_the_range()
    {
        var progress = new KlyProgress { Value = 40 };
        Assert.Equal(40, progress.Percent);
        Assert.Equal("40%", progress.ValueText);
        Assert.Equal(0.4, AbsoluteLayout.GetLayoutBounds(Indicator(progress)).Width);
        Assert.Equal("40%", SemanticProperties.GetHint(progress));

        progress.Value = 250;
        Assert.Equal(100, progress.Percent);
        progress.Value = -5;
        Assert.Equal(0, progress.Percent);

        progress.Max = 20;
        progress.Value = 5;
        Assert.Equal(25, progress.Percent);
    }

    [Fact]
    public void Progress_status_shows_the_percentage_unless_it_is_indeterminate()
    {
        var progress = new KlyProgress { Value = 62.5, Status = true };

        var status = Assert.IsType<Label>(progress.Children[0]);
        Assert.True(status.IsVisible);
        Assert.Equal("62.5%", status.Text);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(status));

        progress.Value = null;
        Assert.False(status.IsVisible);
    }

    [Theory]
    [InlineData(KlySize.Xs, 4)]
    [InlineData(KlySize.Sm, 6)]
    [InlineData(KlySize.Md, 8)]
    [InlineData(KlySize.Lg, 12)]
    [InlineData(KlySize.Xl, 16)]
    public void Progress_size_sets_the_height_of_the_track(KlySize size, double height)
    {
        var progress = new KlyProgress { Value = 10, Size = size };

        Assert.Equal(height, Track(progress).HeightRequest);
    }

    [Fact]
    public void Progress_steps_count_from_zero_to_the_last_step_and_mark_the_state_of_each()
    {
        var progress = new KlyProgress { Steps = ["Cart", "Address", "Payment", "Done"], Value = 1 };

        Assert.Equal(3, progress.MaxValue);
        Assert.Equal(33.3, progress.Percent);
        Assert.Equal("Address", progress.ValueText);
        Assert.Equal(["completed", "active", "inactive", "inactive"], Enumerable.Range(0, 4).Select(progress.StepState).ToArray());
        var steps = Assert.IsType<Grid>(progress.Children[2]);
        Assert.Equal(["Cart", "Address", "Payment", "Done"], steps.Children.Cast<Label>().Select(l => l.Text).ToArray());
        Assert.Equal(FontAttributes.Bold, ((Label)steps.Children[1]).FontAttributes);
        Assert.Equal(LayoutAlignment.Start, ((Label)steps.Children[0]).HorizontalOptions.Alignment);
        Assert.Equal(LayoutAlignment.End, ((Label)steps.Children[3]).HorizontalOptions.Alignment);

        progress.Value = null;
        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal("inactive", progress.StepState(i)));
    }

    [Fact]
    public void Progress_has_an_accessible_name_and_no_animation_without_a_native_view()
    {
        var progress = new KlyProgress { Value = 10, AccessibleName = "Upload", Animation = KlyProgressAnimation.Swing };

        Assert.Equal("Upload", SemanticProperties.GetDescription(progress));
        Assert.Equal(KlyProgressAnimation.Swing, progress.Animation);
        Assert.Equal(0, Indicator(progress).TranslationX);
        Assert.Equal(0.1, progress.ShownFraction);
    }

    // --- KlyMeter and the ring -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Meter_value_is_clamped_and_the_tone_follows_the_thresholds()
    {
        var meter = new KlyMeter { Value = 50, WarningThreshold = 70, ErrorThreshold = 90 };
        Assert.Equal(50, meter.Percent);
        Assert.Equal(KlyColor.Primary, meter.Tone);

        meter.Value = 70;
        Assert.Equal(KlyColor.Warning, meter.Tone);
        meter.Value = 95;
        Assert.Equal(KlyColor.Error, meter.Tone);

        meter.Value = 500;
        Assert.Equal(100, meter.EffectiveValue);
        meter.Value = -4;
        Assert.Equal(0, meter.EffectiveValue);
    }

    [Fact]
    public void Meter_writes_the_value_out_of_the_maximum_unless_it_has_a_text()
    {
        Invariant();
        var meter = new KlyMeter { Value = 3.2, Max = 10, Title = "Disk" };
        Assert.Equal("3.2 / 10", meter.ValueLabel);
        Assert.Contains("3.2 / 10", Texts(meter));
        Assert.Contains("Disk", Texts(meter));

        meter.ValueText = "3.2 GB";
        Assert.Equal("3.2 GB", meter.ValueLabel);
        Assert.Contains("3.2 GB", Texts(meter));
    }

    [Fact]
    public void Meter_bar_gives_the_bar_its_share_of_the_track_and_the_rest_to_the_end()
    {
        var meter = new KlyMeter { Value = 30, Title = "Quota" };

        var track = All<Border>(meter).First(b => b.HeightRequest == 10);
        var cells = Assert.IsType<Grid>(track.Content);
        Assert.Equal([30d, 70d], cells.ColumnDefinitions.Select(c => c.Width.Value).ToArray());
        Assert.Equal("Quota", SemanticProperties.GetDescription(track));
        Assert.Equal("30 / 100", SemanticProperties.GetHint(track));
    }

    [Fact]
    public void Meter_without_a_title_is_named_meter()
    {
        var meter = new KlyMeter { Value = 5 };
        var track = All<Border>(meter).First(b => b.HeightRequest == 10);

        Assert.Equal("Meter", SemanticProperties.GetDescription(track));
        meter.DefaultTitle = "Jauge";
        track = All<Border>(meter).First(b => b.HeightRequest == 10);
        Assert.Equal("Jauge", SemanticProperties.GetDescription(track));
    }

    [Fact]
    public void Meter_parts_stack_in_the_bar_and_a_maximum_of_zero_is_their_sum()
    {
        var parts = new[] { new KlyMeterPart("Apps", 20) { Color = KlyColor.Info }, new KlyMeterPart("Media", 30) { Color = KlyColor.Warning }, new KlyMeterPart("Capped", 90) { Max = 10 } };
        var meter = new KlyMeter { Parts = parts, Max = 0 };

        Assert.Equal(60, meter.EffectiveMax);
        var cells = (Grid)All<Border>(meter).First(b => b.HeightRequest == 10).Content!;
        Assert.Equal(4, cells.ColumnDefinitions.Count);
        Assert.Equal(20 / 60d * 100, cells.ColumnDefinitions[0].Width.Value, 6);
        Assert.Equal(30 / 60d * 100, cells.ColumnDefinitions[1].Width.Value, 6);
        Assert.Equal(10 / 60d * 100, cells.ColumnDefinitions[2].Width.Value, 6);
        Assert.Equal(0, cells.ColumnDefinitions[3].Width.Value, 6);
    }

    [Fact]
    public void Meter_legend_shows_only_when_a_part_has_a_value_label_an_icon_or_a_maximum()
    {
        var meter = new KlyMeter { Parts = [new KlyMeterPart("Apps", 20), new KlyMeterPart("Media", 30)] };
        Assert.DoesNotContain("Apps", Texts(meter));

        meter.Parts = [new KlyMeterPart("Apps", 20) { ValueLabel = "2 GB" }, new KlyMeterPart("Media", 30) { Max = 40 }];
        var texts = Texts(meter);
        Assert.Contains("Apps", texts);
        Assert.Contains("2 GB", texts);
        Assert.Contains("30 / 40", texts);
    }

    [Fact]
    public void Meter_scale_highlights_the_levels_up_to_the_current_one()
    {
        var meter = new KlyMeter { Layout = KlyMeterLayout.Scale, Levels = ["Low", "Medium", "High"], CurrentLevel = 1 };

        Assert.True(meter.Reached(0));
        Assert.True(meter.Reached(1));
        Assert.False(meter.Reached(2));
        var cells = All<Border>(meter).Where(b => b.Content is Label).ToList();
        Assert.Equal(["Low", "Medium", "High"], cells.Select(c => ((Label)c.Content!).Text).ToArray());
        Assert.Equal("Medium, Current", SemanticProperties.GetDescription(cells[1]));
        Assert.Null(SemanticProperties.GetDescription(cells[0]));

        meter.CurrentLevel = null;
        Assert.False(meter.Reached(0));
    }

    [Fact]
    public void Meter_scale_without_levels_is_a_bar()
    {
        var meter = new KlyMeter { Layout = KlyMeterLayout.Scale, Value = 40 };

        Assert.Contains(All<Border>(meter), b => b.HeightRequest == 10);
    }

    [Fact]
    public void Meter_ring_shows_the_rounded_percentage_in_the_center_and_one_rounded_arc()
    {
        var meter = new KlyMeter { Layout = KlyMeterLayout.Ring, Value = 62.4, Title = "Storage", Description = "of 128 GB" };

        var ring = All<DisplayMeterRing>(meter).Single();
        var arc = Assert.Single(ring.Arcs);
        Assert.Equal(62.4, arc.Percent);
        Assert.Equal(0, arc.Offset);
        Assert.True(arc.RoundCap);
        var texts = Texts(meter);
        Assert.Contains("62%", texts);
        Assert.Contains("Storage", texts);
        Assert.Contains("of 128 GB", texts);

        meter.ValueText = "80 GB";
        Assert.Contains("80 GB", Texts(meter));
    }

    [Fact]
    public void Meter_ring_draws_one_arc_per_part_stacked_and_none_for_an_empty_value()
    {
        var meter = new KlyMeter { Layout = KlyMeterLayout.Ring, Parts = [new KlyMeterPart("A", 25), new KlyMeterPart("B", 50)] };

        var arcs = meter.RingArcs();
        Assert.Equal([25d, 50d], arcs.Select(a => a.Percent).ToArray());
        Assert.Equal([0d, 25d], arcs.Select(a => a.Offset).ToArray());
        Assert.All(arcs, a => Assert.False(a.RoundCap));

        Assert.Empty(new KlyMeter { Layout = KlyMeterLayout.Ring }.RingArcs());
    }

    [Fact]
    public void Ring_arc_starts_at_the_top_and_runs_clockwise()
    {
        var quarter = DisplayMeterRing.ArcPath(50, 50, 40, 0, 25);

        Assert.Equal(50, quarter.FirstPoint.X, 3);
        Assert.Equal(10, quarter.FirstPoint.Y, 3);
        Assert.Equal(90, quarter.LastPoint.X, 3);
        Assert.Equal(50, quarter.LastPoint.Y, 3);

        var second = DisplayMeterRing.ArcPath(50, 50, 40, 25, 25);
        Assert.Equal(90, second.FirstPoint.X, 3);
        Assert.Equal(50, second.LastPoint.X, 3);
        Assert.Equal(90, second.LastPoint.Y, 3);
    }

    // --- KlyStat, KlyStatGroup and the sparkline --------------------------------------------------------------------------------------------

    [Fact]
    public void Stat_formats_the_value_with_its_format_and_culture_and_shows_the_unit()
    {
        var stat = new KlyStat<int> { Title = "Users", Value = 12345, Unit = "k", Culture = CultureInfo.InvariantCulture };

        Assert.Equal("12,345", stat.ValueText);
        Assert.Contains("Users", Texts(stat));
        Assert.Contains("12,345", Texts(stat));
        Assert.Contains("k", Texts(stat));

        stat.Format = "C0";
        stat.Value = 7;
        Assert.StartsWith("¤", stat.ValueText);
    }

    [Fact]
    public void Stat_of_a_nullable_number_shows_a_dash_and_no_delta_without_a_value()
    {
        var stat = new KlyStat<decimal?> { Culture = CultureInfo.InvariantCulture };

        Assert.Equal("—", stat.ValueText);
        Assert.Null(stat.DeltaText);
        Assert.False(stat.DeltaBadge.IsVisible);

        stat.Value = 9.5m;
        stat.Delta = 1.25m;
        Assert.Equal("9.5", stat.ValueText);
        Assert.Equal("+1.25", stat.DeltaText);
        Assert.True(stat.DeltaBadge.IsVisible);
    }

    [Fact]
    public void Stat_of_a_float_that_is_not_finite_shows_a_dash()
    {
        var stat = new KlyStat<double> { Value = double.NaN, Delta = double.PositiveInfinity };

        Assert.Equal("—", stat.ValueText);
        Assert.Null(stat.DeltaText);
    }

    [Fact]
    public void Stat_delta_colors_its_badge_from_the_sign_unless_the_direction_is_set()
    {
        var stat = new KlyStat<int> { Value = 10, Delta = 5, DeltaUnit = "%", Culture = CultureInfo.InvariantCulture };
        Assert.Equal("+5%", stat.DeltaText);
        Assert.Equal(KlyColor.Success, stat.DeltaBadge.Color);
        Assert.Equal("+5%", stat.DeltaBadge.Text);
        Assert.Equal(KlyVariant.Subtle, stat.DeltaBadge.Variant);
        Assert.Equal(KlySize.Sm, stat.DeltaBadge.Size);

        stat.Delta = -3;
        Assert.Equal("-3%", stat.DeltaText);
        Assert.Equal(KlyColor.Error, stat.DeltaBadge.Color);

        stat.Delta = 0;
        Assert.Equal("0%", stat.DeltaText);
        Assert.Equal(KlyColor.Neutral, stat.DeltaBadge.Color);

        stat.Delta = -3;
        stat.DeltaDirection = KlyDeltaDirection.Up;
        Assert.Equal(KlyColor.Success, stat.DeltaBadge.Color);
        stat.DeltaDirection = KlyDeltaDirection.Flat;
        Assert.Equal(KlyColor.Neutral, stat.DeltaBadge.Color);
    }

    [Fact]
    public void Stat_draws_a_sparkline_from_two_points_and_a_caption()
    {
        var stat = new KlyStat<int> { Value = 1, Caption = "Last 7 days" };
        Assert.False(stat.Sparkline.IsVisible);
        Assert.Contains("Last 7 days", Texts(stat));

        stat.Trend = [1];
        Assert.False(stat.Sparkline.IsVisible);
        stat.Trend = [1, 3, 2];
        Assert.True(stat.Sparkline.IsVisible);
        Assert.Equal(3, stat.Sparkline.Points.Count);
        Assert.Equal(32, stat.Sparkline.HeightRequest);
    }

    [Fact]
    public void Stat_refuses_a_type_that_is_not_a_number()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new KlyStat<string>());

        Assert.Contains("String", error.Message);
        Assert.Contains("decimal", error.Message);
        Assert.NotNull(new KlyStat<byte?>());
        Assert.NotNull(new KlyStat<ulong>());
    }

    [Fact]
    public void Sparkline_spreads_the_points_over_the_width_and_puts_the_maximum_on_top()
    {
        var line = DisplaySparkline.Project([0, 10, 5], 100, 30);

        Assert.Equal([0f, 50f, 100f], line.Select(p => p.X).ToArray());
        Assert.Equal(28f, line[0].Y, 3);
        Assert.Equal(4f, line[1].Y, 3);
        Assert.Equal(16f, line[2].Y, 3);
        Assert.Empty(DisplaySparkline.Project([1], 100, 30));
    }

    [Fact]
    public void Sparkline_of_a_flat_series_draws_a_flat_line()
    {
        var line = DisplaySparkline.Project([3, 3, 3], 100, 30);

        Assert.Single(line.Select(p => p.Y).Distinct());
    }

    [Fact]
    public void StatGroup_shows_its_metrics_with_a_dash_for_a_missing_value_and_stacks_them_on_a_narrow_control()
    {
        var group = new KlyStatGroup<int?>
        {
            Title = "Today",
            Culture = CultureInfo.InvariantCulture,
            Items = [new KlyStatItem<int?>("Visits", 1200) { Caption = "+4%" }, new KlyStatItem<int?>("Errors", null) { Color = KlyColor.Error }, new KlyStatItem<int?>("Orders", 7)],
        };

        var texts = Texts(group);
        Assert.Contains("Today", texts);
        Assert.Contains("1,200", texts);
        Assert.Contains("+4%", texts);
        Assert.Contains("—", texts);
        Assert.Equal(1, group.Columns);
        Assert.Equal(3, group.Cells.RowDefinitions.Count);

        group.Arrange(3);
        Assert.Equal(3, group.Columns);
        Assert.Equal(3, group.Cells.ColumnDefinitions.Count);
        Assert.Single(group.Cells.RowDefinitions);
        Assert.Equal([0, 1, 2], group.Cells.Children.Select(c => Grid.GetColumn((BindableObject)c)).ToArray());
    }

    [Fact]
    public void StatGroup_refuses_a_type_that_is_not_a_number()
    {
        Assert.Throws<InvalidOperationException>(() => new KlyStatGroup<DateTime>());
    }

    // --- KlyHeading ------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Heading_shows_the_title_the_description_and_the_count()
    {
        Invariant();
        var heading = new KlyHeading { Title = "Orders", Description = "Last 30 days", Count = 1234 };

        var texts = Texts(heading);
        Assert.Contains("Orders", texts);
        Assert.Contains("Last 30 days", texts);
        Assert.Contains("1,234", texts);

        heading.Count = null;
        Assert.DoesNotContain("1,234", Texts(heading));
    }

    [Fact]
    public void Heading_title_is_a_heading_of_the_level_asked_and_level_three_by_default()
    {
        var heading = new KlyHeading { Title = "Orders" };
        var title = LabelWith(heading, "Orders");
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(title));

        heading.HeadingLevel = 1;
        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(title));
        heading.HeadingLevel = 99;
        Assert.Equal(SemanticHeadingLevel.Level6, SemanticProperties.GetHeadingLevel(title));
        heading.HeadingLevel = 0;
        Assert.Equal(SemanticHeadingLevel.Level1, SemanticProperties.GetHeadingLevel(title));
    }

    [Fact]
    public void Heading_icon_goes_in_a_tile_hidden_from_assistive_technology_and_the_action_is_a_small_button()
    {
        var clicks = 0;
        var heading = new KlyHeading { Title = "Orders", Icon = new KlyIcon { Name = "inbox" }, Action = new KlyActionItem("View all") { OnClick = () => clicks++ } };

        var tile = All<Border>(heading).First(b => b.Content is KlyIcon);
        Assert.Equal(32, tile.WidthRequest);
        Assert.True(tile.IsVisible);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(tile));

        var button = All<KlyButton>(heading).Single();
        Assert.Equal("View all", button.Text);
        Assert.Equal(KlySize.Sm, button.Size);
        Assert.Equal(ButtonVariant.Outline, button.Variant);
        Assert.Equal(KlyColor.Neutral, button.Color);
        button.SendClicked();
        Assert.Equal(1, clicks);

        heading.Icon = null;
        heading.Action = null;
        Assert.Empty(All<KlyButton>(heading));
        Assert.False(tile.IsVisible);
    }

    [Fact]
    public void Heading_keeps_an_icon_while_other_properties_change()
    {
        var icon = new KlyIcon { Name = "inbox" };
        var heading = new KlyHeading { Icon = icon, Title = "A" };

        heading.Title = "B";
        heading.Description = "C";

        Assert.Single(All<KlyIcon>(heading));
        Assert.Same(icon, All<KlyIcon>(heading).Single());
    }

    // --- KlyText ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Text_shows_a_title_a_value_and_a_footer()
    {
        var text = new KlyText { Title = "Note", Value = "Hello", Author = "Ada", Meta = "2026" };

        var texts = Texts(text);
        Assert.Equal(["Note", "Hello", "Ada · 2026"], texts);
    }

    [Fact]
    public void Text_paragraphs_replace_the_value_and_are_not_clamped()
    {
        var text = new KlyText { Value = "ignored", Paragraphs = ["One", "Two"], MaxLines = 1 };

        Assert.Equal(["One", "Two"], Texts(text));
        Assert.All(All<Label>(text), l => Assert.Equal(-1, l.MaxLines));
        Assert.NotNull(text.ToggleButton);
    }

    [Fact]
    public void Text_max_lines_clamps_the_value_and_the_button_expands_it()
    {
        var text = new KlyText { Value = "A very long text", MaxLines = 2 };

        Assert.Equal(2, text.BodyLabel!.MaxLines);
        Assert.Equal("Read more", text.ToggleButton!.Text);
        Assert.False(text.IsExpanded);

        text.ToggleButton.SendClicked();

        Assert.True(text.IsExpanded);
        Assert.Equal(-1, text.BodyLabel!.MaxLines);
        Assert.Equal("Show less", text.ToggleButton!.Text);

        text.ToggleButton.SendClicked();
        Assert.False(text.IsExpanded);
        Assert.Equal(2, text.BodyLabel!.MaxLines);
    }

    [Fact]
    public void Text_without_max_lines_has_no_button_and_a_button_for_every_clamped_text()
    {
        var plain = new KlyText { Value = "Short" };
        Assert.Null(plain.ToggleButton);

        var clamped = new KlyText { Value = "Short", MaxLines = 3, ReadMoreLabel = "Lire la suite", ShowLessLabel = "Réduire" };
        Assert.Equal("Lire la suite", clamped.ToggleButton!.Text);
        clamped.IsExpanded = true;
        Assert.Equal("Réduire", clamped.ToggleButton!.Text);
    }

    [Fact]
    public void Text_quote_has_a_start_border_and_italic_text()
    {
        var text = new KlyText { Variant = KlyTextVariant.Quote, Value = "Be kind", Author = "Anon" };

        var row = Assert.IsType<Grid>(Frame(text).Body is VerticalStackLayout body ? body.Children[0] : null);
        Assert.Equal(2, ((Border)row.Children[0]).WidthRequest);
        Assert.Equal(FontAttributes.Italic, LabelWith(text, "Be kind").FontAttributes);

        text.Variant = KlyTextVariant.Default;
        Assert.DoesNotContain(All<Grid>(text), g => g.Children.OfType<Border>().Any(b => b.WidthRequest == 2));
        Assert.Equal(FontAttributes.None, LabelWith(text, "Be kind").FontAttributes);
    }

    // --- KlyList ---------------------------------------------------------------------------------------------------------------------------

    private static List<KlyListItem> Items(int count) => Enumerable.Range(1, count).Select(i => new KlyListItem($"Item {i}")).ToList();

    [Fact]
    public void List_shows_a_title_and_one_row_per_item_with_its_description_and_meta()
    {
        var list = new KlyList
        {
            Title = "Todo",
            Items = [new KlyListItem("First") { Description = "Soon", Meta = "today" }, new KlyListItem("Second")],
        };

        Assert.Equal(["Todo", "First", "Soon", "today", "Second"], Texts(list));
        Assert.Equal(2, list.Rows.Count);
    }

    [Fact]
    public void List_plain_uses_a_bullet_or_the_icon_of_each_item()
    {
        var list = new KlyList { Items = [new KlyListItem("Plain"), new KlyListItem("Iconic") { Icon = "check", Color = KlyColor.Success }] };

        Assert.Single(All<KlyIcon>(list));
        Assert.Equal("check", All<KlyIcon>(list).Single().Name);
        Assert.Contains(All<Border>(list), b => b.WidthRequest == 6);
    }

    [Fact]
    public void List_steps_number_the_items_and_mark_done_and_current_ones_for_a_screen_reader()
    {
        var list = new KlyList
        {
            Variant = KlyListVariant.Steps,
            Items = [new KlyListItem("Cart") { Done = true }, new KlyListItem("Address") { Current = true }, new KlyListItem("Pay")],
        };

        var markers = All<Border>(list).Where(b => b.WidthRequest == 20).ToList();
        Assert.Equal(3, markers.Count);
        Assert.IsType<KlyIcon>(markers[0].Content);
        Assert.Equal("2", ((Label)markers[1].Content!).Text);
        Assert.Equal("3", ((Label)markers[2].Content!).Text);
        Assert.Equal("Cart, Step completed", SemanticProperties.GetDescription(list.Rows[0]));
        Assert.Equal("Address, Current step", SemanticProperties.GetDescription(list.Rows[1]));
        Assert.Null(SemanticProperties.GetDescription(list.Rows[2]));

        list.Variant = KlyListVariant.Checks;
        Assert.Equal(3, All<Border>(list).Count(b => b.WidthRequest == 20));
    }

    [Fact]
    public void List_max_reveals_the_rest_by_step_and_the_button_says_how_many()
    {
        var list = new KlyList { Items = Items(10), Max = 3, Step = 4 };

        Assert.Equal(3, list.Rows.Count);
        Assert.Equal("Show more · 4", list.MoreButton!.Text);

        list.MoreButton.SendClicked();
        Assert.Equal(7, list.Rows.Count);
        Assert.Equal("Show more · 3", list.MoreButton!.Text);

        list.MoreButton.SendClicked();
        Assert.Equal(10, list.Rows.Count);
        Assert.Null(list.MoreButton);
    }

    [Fact]
    public void List_without_a_step_reveals_everything_and_the_format_is_in_the_language_of_the_app()
    {
        var list = new KlyList { Items = Items(5), Max = 2, ShowMoreFormat = "Voir {0} de plus" };

        Assert.Equal("Voir 3 de plus", list.MoreButton!.Text);
        list.MoreButton.SendClicked();
        Assert.Equal(5, list.Rows.Count);
    }

    [Fact]
    public void List_with_a_link_shows_the_text_as_a_link()
    {
        var list = new KlyList { Items = [new KlyListItem("Docs") { Href = "https://example.com" }] };

        Assert.Equal(TextDecorations.Underline, LabelWith(list, "Docs").TextDecorations);
        Assert.NotEmpty(LabelWith(list, "Docs").GestureRecognizers);
    }

    // --- KlyFacts --------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Facts_show_a_label_and_a_value_per_row_with_the_hint_and_the_icon()
    {
        var facts = new KlyFacts
        {
            Items = [new KlyFact("Plan") { Value = "Pro", Hint = "Billed yearly", Icon = "star", Emphasis = true }, new KlyFact("Seats") { Value = "5" }],
        };

        Assert.Equal(["Plan", "Billed yearly", "Pro", "Seats", "5"], Texts(facts));
        Assert.Equal(FontAttributes.Bold, LabelWith(facts, "Pro").FontAttributes);
        Assert.Single(All<KlyIcon>(facts));
        Assert.False(AutomationProperties.GetIsInAccessibleTree(All<KlyIcon>(facts).Single()));
    }

    [Fact]
    public void Facts_show_a_dash_for_an_empty_value_and_nothing_to_copy_there()
    {
        var facts = new KlyFacts { Items = [new KlyFact("Phone") { Value = " ", Copy = true }] };

        Assert.Contains("—", Texts(facts));
        Assert.Empty(All<DisplayIconButton>(facts));

        Assert.Equal(["—"], Texts(new KlyFacts()));
    }

    [Fact]
    public void Facts_value_can_be_a_badge_a_link_a_placeholder_or_monospace()
    {
        var facts = new KlyFacts
        {
            Items =
            [
                new KlyFact("Status") { Badge = new KlyBadgeItem("Active") { Color = KlyColor.Success } },
                new KlyFact("Site") { Value = "example.com", Href = "https://example.com" },
                new KlyFact("Loading") { Loading = true },
                new KlyFact("Key") { Value = "abc123", Mono = true },
            ],
        };

        var badge = All<KlyBadge>(facts).Single();
        Assert.Equal("Active", badge.Text);
        Assert.Equal(KlyColor.Success, badge.Color);
        Assert.Equal(TextDecorations.Underline, LabelWith(facts, "example.com").TextDecorations);
        Assert.Single(All<KlySkeleton>(facts.Stack));
        Assert.False(string.IsNullOrEmpty(LabelWith(facts, "abc123").FontFamily));
    }

    [Fact]
    public void Facts_copy_button_copies_the_value_or_the_copy_text_and_is_named_after_the_row()
    {
        var facts = new KlyFacts { Items = [new KlyFact("Email") { Value = "ada@example.com", Copy = true }, new KlyFact("Token") { Value = "••••", CopyText = "secret-token" }] };
        var copied = new List<string>();
        facts.Copier = text =>
        {
            copied.Add(text);
            return Task.CompletedTask;
        };

        var buttons = All<DisplayIconButton>(facts);
        Assert.Equal(2, buttons.Count);
        Assert.Equal("Copy Email", SemanticProperties.GetDescription(buttons[0]));
        Assert.Equal(40, buttons[0].WidthRequest);
        buttons[0].SendClicked();
        buttons[1].SendClicked();

        Assert.Equal(["ada@example.com", "secret-token"], copied);
    }

    [Fact]
    public void Facts_groups_replace_the_items_with_titled_sections()
    {
        var facts = new KlyFacts
        {
            Items = [new KlyFact("Ignored") { Value = "x" }],
            Groups = [new KlyFactGroup("Account", [new KlyFact("Plan") { Value = "Pro" }]) { Aside = "since 2024" }, new KlyFactGroup(null, [new KlyFact("Seats") { Value = "5" }])],
        };

        var texts = Texts(facts);
        Assert.Equal(["Account", "since 2024", "Plan", "Pro", "Seats", "5"], texts);
        Assert.DoesNotContain("Ignored", texts);
        Assert.Equal(2, facts.Stack.Children.Count);
    }

    [Fact]
    public void Facts_max_hides_the_rows_behind_a_show_more_button()
    {
        var facts = new KlyFacts { Items = Enumerable.Range(1, 5).Select(i => new KlyFact($"Row {i}") { Value = "v" }).ToList(), Max = 2 };

        var more = All<Button>(facts).Single();
        Assert.Equal("Show more · 3", more.Text);
        Assert.Equal(2, Texts(facts).Count(t => t.StartsWith("Row", StringComparison.Ordinal)));

        more.SendClicked();
        Assert.Equal(5, Texts(facts).Count(t => t.StartsWith("Row", StringComparison.Ordinal)));
        Assert.Empty(All<Button>(facts));
    }

    // --- KlyBreakdown ----------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Breakdown_formats_the_amounts_with_its_format_and_culture_and_a_dash_for_none()
    {
        var breakdown = new KlyBreakdown { Culture = CultureInfo.InvariantCulture, Lines = [new KlyBreakdownLine("Subscription") { Amount = 1234.5m }, new KlyBreakdownLine("Tax")], Total = new KlyBreakdownLine("Total") { Amount = 1500m } };

        Assert.Equal("1,234.50", breakdown.Amount(1234.5m));
        Assert.Equal("—", breakdown.Amount(null));
        Assert.Equal(["Subscription", "1,234.50", "Tax", "—", "Total", "1,500.00"], Texts(breakdown));

        breakdown.Format = "N0";
        Assert.Equal("1,235", breakdown.Amount(1234.5m));
    }

    [Fact]
    public void Breakdown_shows_the_title_the_detail_the_badge_and_the_footnote()
    {
        var breakdown = new KlyBreakdown
        {
            Culture = CultureInfo.InvariantCulture,
            Title = "Invoice",
            Footnote = "Taxes included",
            Lines = [new KlyBreakdownLine("Plan") { Amount = 10m, Detail = "Pro, yearly", Badge = new KlyBadgeItem("-20%") }],
        };

        var texts = Texts(breakdown);
        Assert.Contains("Invoice", texts);
        Assert.Contains("Pro, yearly", texts);
        Assert.Contains("Taxes included", texts);
        Assert.Equal("-20%", All<KlyBadge>(breakdown).Single().Text);
    }

    [Fact]
    public void Breakdown_subtotal_and_total_have_a_line_above_and_bold_text()
    {
        var breakdown = new KlyBreakdown
        {
            Culture = CultureInfo.InvariantCulture,
            Lines = [new KlyBreakdownLine("Item") { Amount = 1m }, new KlyBreakdownLine("Subtotal") { Amount = 1m, IsSubtotal = true }, new KlyBreakdownLine("Gift") { Amount = 0m, IsMuted = true }],
            Total = new KlyBreakdownLine("Total") { Amount = 1m },
        };

        Assert.Equal(FontAttributes.Bold, LabelWith(breakdown, "Subtotal").FontAttributes);
        Assert.Equal(FontAttributes.Bold, LabelWith(breakdown, "Total").FontAttributes);
        Assert.Equal(FontAttributes.None, LabelWith(breakdown, "Item").FontAttributes);
        Assert.Equal(2, All<Border>(breakdown).Count(b => b.HeightRequest == 1));
    }

    [Fact]
    public void Breakdown_two_columns_come_from_the_headers_or_from_a_second_amount()
    {
        var plain = new KlyBreakdown { Lines = [new KlyBreakdownLine("A") { Amount = 1m }] };
        Assert.False(plain.Compare);

        var headers = new KlyBreakdown { CompareColumns = new KlyBreakdownColumns("2026", "2025"), Lines = [new KlyBreakdownLine("A") { Amount = 2m }] };
        Assert.True(headers.Compare);
        Assert.Contains("2026", Texts(headers));
        Assert.Contains("2025", Texts(headers));
        Assert.Contains("—", Texts(headers));

        var second = new KlyBreakdown { Lines = [new KlyBreakdownLine("A") { Amount = 2m, CompareAmount = 1m }] };
        Assert.True(second.Compare);
        Assert.DoesNotContain("2026", Texts(second));

        var onTotal = new KlyBreakdown { Total = new KlyBreakdownLine("T") { Amount = 2m, CompareAmount = 1m } };
        Assert.True(onTotal.Compare);
    }

    [Fact]
    public void Breakdown_collapsible_hides_the_lines_behind_a_toggle_but_not_the_total()
    {
        var breakdown = new KlyBreakdown { Culture = CultureInfo.InvariantCulture, Collapsible = true, Lines = [new KlyBreakdownLine("Item") { Amount = 1m }], Total = new KlyBreakdownLine("Total") { Amount = 1m } };
        var changes = 0;
        breakdown.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KlyBreakdown.IsOpen)) changes++;
        };

        Assert.False(breakdown.IsOpen);
        Assert.DoesNotContain("Item", Texts(breakdown));
        Assert.Contains("Total", Texts(breakdown));
        var toggle = All<Button>(breakdown).Single();
        Assert.Equal("Details", toggle.Text);

        toggle.SendClicked();

        Assert.True(breakdown.IsOpen);
        Assert.Equal(1, changes);
        Assert.Contains("Item", Texts(breakdown));
        Assert.Equal("Hide details", All<Button>(breakdown).Single().Text);
    }

    [Fact]
    public void Breakdown_default_open_starts_it_open_until_the_user_has_toggled_it()
    {
        var breakdown = new KlyBreakdown { Collapsible = true, DefaultOpen = true, Lines = [new KlyBreakdownLine("Item") { Amount = 1m }] };
        Assert.True(breakdown.IsOpen);
        Assert.Contains("Item", Texts(breakdown));

        All<Button>(breakdown).Single().SendClicked();
        Assert.False(breakdown.IsOpen);

        breakdown.DefaultOpen = false;
        breakdown.DefaultOpen = true;
        Assert.False(breakdown.IsOpen);
    }

    [Fact]
    public void Breakdown_collapsed_still_shows_the_column_headers_when_there_is_a_total()
    {
        var breakdown = new KlyBreakdown { Collapsible = true, CompareColumns = new KlyBreakdownColumns("2026", "2025"), Lines = [new KlyBreakdownLine("Item") { Amount = 1m }], Total = new KlyBreakdownLine("Total") { Amount = 1m } };
        Assert.Contains("2026", Texts(breakdown));

        var noTotal = new KlyBreakdown { Collapsible = true, CompareColumns = new KlyBreakdownColumns("2026", "2025"), Lines = [new KlyBreakdownLine("Item") { Amount = 1m }] };
        Assert.DoesNotContain("2026", Texts(noTotal));
        noTotal.IsOpen = true;
        Assert.Contains("2026", Texts(noTotal));
    }

    // --- KlyDiff ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Diff_shows_the_old_value_struck_through_an_arrow_and_the_new_value()
    {
        var diff = new KlyDiff { Title = "Changes", Items = [new KlyDiffItem("Plan", "Free", "Pro")] };

        Assert.Equal(["Changes", "Plan", "Free", "→", "Pro"], Texts(diff));
        Assert.Equal(TextDecorations.Strikethrough, LabelWith(diff, "Free").TextDecorations);
        Assert.Equal(FontAttributes.Bold, LabelWith(diff, "Pro").FontAttributes);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(LabelWith(diff, "→")));
    }

    [Fact]
    public void Diff_changed_row_says_what_changed_to_a_screen_reader()
    {
        var diff = new KlyDiff { Items = [new KlyDiffItem("Plan", "Free", "Pro"), new KlyDiffItem("Seats", null, "5")] };

        var rows = All<Grid>(diff).Where(g => SemanticProperties.GetDescription(g) is not null).ToList();
        Assert.Equal("Plan: was Free, now Pro", SemanticProperties.GetDescription(rows[0]));
        Assert.Equal("Seats: was —, now 5", SemanticProperties.GetDescription(rows[1]));

        diff.ChangedFormat = "{0} : {1} puis {2}";
        rows = All<Grid>(diff).Where(g => SemanticProperties.GetDescription(g) is not null).ToList();
        Assert.Equal("Plan : Free puis Pro", SemanticProperties.GetDescription(rows[0]));
    }

    [Fact]
    public void Diff_without_an_old_value_shows_only_the_new_one_and_a_note_goes_in_a_badge()
    {
        var diff = new KlyDiff { Items = [new KlyDiffItem("Seats", null, "5") { Note = new KlyBadgeItem("Requires approval") }] };

        Assert.DoesNotContain("→", Texts(diff));
        Assert.Contains("5", Texts(diff));
        Assert.Equal("Requires approval", All<KlyBadge>(diff).Single().Text);
    }

    [Fact]
    public void Diff_hides_the_unchanged_fields_unless_asked()
    {
        var items = new[] { new KlyDiffItem("Plan", "Free", "Pro"), new KlyDiffItem("Name", "Ada", "Ada") { Changed = false } };
        var diff = new KlyDiff { Items = items };
        Assert.DoesNotContain("Name", Texts(diff));

        diff.ShowUnchanged = true;
        Assert.Contains("Name", Texts(diff));
        Assert.Single(Texts(diff), "Ada");
        Assert.Equal(TextDecorations.None, LabelWith(diff, "Ada").TextDecorations);
    }

    [Fact]
    public void DiffItem_compare_is_changed_when_the_values_differ_and_formats_them_with_the_culture_of_the_block()
    {
        var changed = KlyDiffItem.Compare("Price", 1234.5m, 99m, "N1");
        var same = KlyDiffItem.Compare("Seats", 5, 5);
        Assert.True(changed.Changed);
        Assert.False(same.Changed);

        var diff = new KlyDiff { Culture = CultureInfo.InvariantCulture, Items = [changed] };
        Assert.Equal(["Price", "1,234.5", "→", "99.0"], Texts(diff));

        var custom = KlyDiffItem.Compare("Day", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3), (d, c) => d.ToString("dd/MM", c));
        diff.Items = [custom];
        Assert.Equal(["Day", "02/01", "→", "03/01"], Texts(diff));

        Assert.False(KlyDiffItem.Compare<string>("Note", null, null).Changed);
        Assert.True(KlyDiffItem.Compare("Note", "a", "A", comparer: StringComparer.Ordinal).Changed);
        Assert.False(KlyDiffItem.Compare("Note", "a", "A", comparer: StringComparer.OrdinalIgnoreCase).Changed);
    }

    // --- KlyRecordList ---------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RecordList_shows_the_title_the_count_and_the_parts_of_each_row()
    {
        Invariant();
        var list = new KlyRecordList
        {
            Title = "Orders",
            Count = 1234,
            Items =
            [
                new KlyRecordItem("Acme Ltd")
                {
                    Reference = "#A-1042",
                    Description = "Paid today",
                    DescriptionIcon = "clock",
                    AvatarText = "AL",
                    Badge = new KlyBadgeItem("Paid") { Color = KlyColor.Success },
                    Badges = [new KlyBadgeItem("Priority")],
                    Trailing = "$120.00",
                    TrailingCaption = "Nov 27",
                },
            ],
        };

        var texts = Texts(list);
        Assert.Equal(["Orders", "1,234", "AL", "#A-1042", "Acme Ltd", "Paid today", "Priority", "Paid", "$120.00", "Nov 27"], texts);
        Assert.Equal(["Priority", "Paid"], All<KlyBadge>(list).Select(b => b.Text).ToArray());
        Assert.Single(All<KlyIcon>(list));
    }

    [Fact]
    public void RecordList_leading_tile_is_an_image_before_an_icon_before_the_text_and_is_hidden_from_a_screen_reader()
    {
        var list = new KlyRecordList
        {
            Items =
            [
                new KlyRecordItem("Image") { ImageSrc = "photo.png", Icon = "star", AvatarText = "IM" },
                new KlyRecordItem("Icon") { Icon = "star", AvatarText = "IC" },
                new KlyRecordItem("Text") { AvatarText = "TX" },
                new KlyRecordItem("None"),
            ],
        };

        var tiles = All<Border>(list).Where(b => b.WidthRequest == 32).ToList();
        Assert.Equal(3, tiles.Count);
        Assert.IsType<Image>(tiles[0].Content);
        Assert.IsType<KlyIcon>(tiles[1].Content);
        Assert.Equal("TX", ((Label)tiles[2].Content!).Text);
        Assert.All(tiles, t => Assert.False(AutomationProperties.GetIsInAccessibleTree(t)));
        Assert.DoesNotContain("IM", Texts(list));
        Assert.DoesNotContain("IC", Texts(list));
    }

    [Fact]
    public void RecordList_title_is_tappable_with_an_address_or_a_handler()
    {
        var list = new KlyRecordList
        {
            Items = [new KlyRecordItem("Linked") { Href = "https://example.com" }, new KlyRecordItem("Handled") { OnClick = () => { } }, new KlyRecordItem("Plain")],
        };

        Assert.NotEmpty(LabelWith(list, "Linked").GestureRecognizers);
        Assert.NotEmpty(LabelWith(list, "Handled").GestureRecognizers);
        Assert.Empty(LabelWith(list, "Plain").GestureRecognizers);
    }

    [Fact]
    public void RecordList_max_reveals_the_rest_by_step()
    {
        var list = new KlyRecordList { Items = Enumerable.Range(1, 6).Select(i => new KlyRecordItem($"Row {i}")).ToList(), Max = 2, Step = 3 };

        var more = All<Button>(list).Single();
        Assert.Equal("Show more · 3", more.Text);
        Assert.Equal(2, Texts(list).Count(t => t.StartsWith("Row", StringComparison.Ordinal)));

        more.SendClicked();
        Assert.Equal(5, Texts(list).Count(t => t.StartsWith("Row", StringComparison.Ordinal)));
        Assert.Equal("Show more · 1", All<Button>(list).Single().Text);
    }

    // --- KlyDateTile -----------------------------------------------------------------------------------------------------------------------

    private static readonly DateTimeOffset Day = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DateTile_date_mode_shows_the_calendar_and_the_long_date_or_the_title()
    {
        var culture = Invariant();
        var tile = new KlyDateTile { Value = Day };

        Assert.Equal("Nov", tile.MonthText);
        Assert.Equal("27", tile.DayText);
        Assert.Equal(Day.ToString("D", culture), tile.TitleText);

        tile.Title = "Launch";
        tile.Description = "Berlin";
        Assert.Equal("Launch", tile.TitleText);
        Assert.Contains("Berlin", Texts(tile));
        Assert.DoesNotContain("Launch", Texts(tile).Skip(2).Where(t => t != "Launch"));
    }

    [Theory]
    [InlineData(0, "Today")]
    [InlineData(1, "Tomorrow")]
    [InlineData(5, "In 5 days")]
    [InlineData(-1, "1 day overdue")]
    [InlineData(-3, "3 days overdue")]
    public void DateTile_countdown_says_how_many_days_are_left_and_the_title_becomes_a_caption(int offset, string expected)
    {
        Invariant();
        var tile = new KlyDateTile { Value = Day.AddDays(offset), Now = Day, Mode = KlyDateTileMode.Countdown, Title = "Release" };

        Assert.Equal(expected, tile.TitleText);
        Assert.Contains("Release", Texts(tile));
    }

    [Fact]
    public void DateTile_countdown_texts_are_in_the_language_of_the_app()
    {
        Invariant();
        var tile = new KlyDateTile { Value = Day.AddDays(4), Now = Day, Mode = KlyDateTileMode.Countdown, InDays = n => $"Dans {n} jours", TodayText = "Aujourd'hui", TomorrowText = "Demain" };
        Assert.Equal("Dans 4 jours", tile.TitleText);

        tile.Value = Day;
        Assert.Equal("Aujourd'hui", tile.TitleText);
        tile.Value = Day.AddDays(1);
        Assert.Equal("Demain", tile.TitleText);
        tile.Value = Day.AddDays(-2);
        tile.DaysOverdue = n => $"{n} jours de retard";
        Assert.Equal("2 jours de retard", tile.TitleText);
    }

    [Fact]
    public void DateTile_range_shows_both_dates_and_the_inclusive_number_of_days()
    {
        Invariant();
        var tile = new KlyDateTile { Value = Day, EndValue = Day.AddDays(2), Mode = KlyDateTileMode.Range, Title = "Trip" };
        Assert.Equal("2026-11-27 → 2026-11-29 (3 days)", tile.TitleText);
        Assert.Contains("Trip", Texts(tile));

        tile.EndValue = Day;
        Assert.Equal("2026-11-27 → 2026-11-27 (1 day)", tile.TitleText);

        tile.DayCount = n => $"{n} j";
        Assert.Equal("2026-11-27 → 2026-11-27 (1 j)", tile.TitleText);

        tile.EndValue = null;
        Assert.Equal("Trip", tile.TitleText);
    }

    [Fact]
    public void DateTile_calendar_is_hidden_from_assistive_technology()
    {
        var tile = new KlyDateTile { Value = Day };

        var calendar = All<Border>(tile).First(b => b.WidthRequest == 48);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(calendar));
        Assert.Equal(TextTransform.Uppercase, All<Label>(calendar).First().TextTransform);
    }

    // --- KlyImageGallery -------------------------------------------------------------------------------------------------------------------

    private static List<KlyGalleryImage> Pictures(int count) => Enumerable.Range(1, count).Select(i => new KlyGalleryImage($"p{i}.png")).ToList();

    [Fact]
    public void Gallery_shows_at_most_max_tiles_and_the_last_one_counts_the_hidden_images()
    {
        var gallery = new KlyImageGallery { Images = Pictures(5) };

        Assert.Equal(3, gallery.Tiles!.Children.Count);
        Assert.Equal(3, gallery.Tiles.ColumnDefinitions.Count);
        Assert.Contains("+2", Texts(gallery));

        gallery.Max = 5;
        Assert.Equal(5, gallery.Tiles!.Children.Count);
        Assert.DoesNotContain("+2", Texts(gallery));

        gallery.Max = 0;
        Assert.Single(gallery.Tiles!.Children);
        Assert.Contains("+4", Texts(gallery));
    }

    [Fact]
    public void Gallery_columns_are_clamped_between_one_and_six_and_fill_the_rows_in_order()
    {
        var gallery = new KlyImageGallery { Images = Pictures(6), Max = 6, Columns = 9 };
        Assert.Equal(6, gallery.Tiles!.ColumnDefinitions.Count);

        gallery.Columns = 0;
        Assert.Single(gallery.Tiles!.ColumnDefinitions);
        Assert.Equal(6, gallery.Tiles.RowDefinitions.Count);

        gallery.Columns = 2;
        Assert.Equal(3, gallery.Tiles!.RowDefinitions.Count);
        Assert.Equal([0, 1, 0, 1, 0, 1], gallery.Tiles.Children.Select(c => Grid.GetColumn((BindableObject)c)).ToArray());
        Assert.Equal([0, 0, 1, 1, 2, 2], gallery.Tiles.Children.Select(c => Grid.GetRow((BindableObject)c)).ToArray());
    }

    [Fact]
    public void Gallery_names_each_image_with_its_alt_or_its_position_and_shows_the_caption()
    {
        var gallery = new KlyImageGallery { Images = [new KlyGalleryImage("a.png") { Alt = "The team" }, new KlyGalleryImage("b.png")], Caption = "Offsite 2026" };

        var tiles = gallery.Tiles!.Children.Cast<Border>().ToList();
        Assert.Equal("The team", SemanticProperties.GetDescription(tiles[0]));
        Assert.Equal("Image 2", SemanticProperties.GetDescription(tiles[1]));
        Assert.Contains("Offsite 2026", Texts(gallery));

        gallery.ImageAltFormat = "Photo {0}";
        tiles = gallery.Tiles!.Children.Cast<Border>().ToList();
        Assert.Equal("Photo 2", SemanticProperties.GetDescription(tiles[1]));
        Assert.Equal(96, All<Image>(gallery).First().HeightRequest);
    }

    // --- KlyImage --------------------------------------------------------------------------------------------------------------------------

    private static (Image Fallback, Image Picture) Pair(KlyImage image)
    {
        var frame = (Grid)image.Children[0];
        return ((Image)frame.Children[0], (Image)frame.Children[1]);
    }

    [Fact]
    public void Image_forwards_the_source_the_fallback_and_the_aspect()
    {
        var image = new KlyImage { Source = "photo.png", Fallback = "placeholder.png", Aspect = Aspect.AspectFill };

        var (fallback, picture) = Pair(image);
        Assert.NotNull(picture.Source);
        Assert.NotNull(fallback.Source);
        Assert.Equal(Aspect.AspectFill, picture.Aspect);
        Assert.Equal(Aspect.AspectFill, fallback.Aspect);
        Assert.Equal(Aspect.AspectFit, new KlyImage().Aspect);

        image.Source = null;
        Assert.Null(Pair(image).Picture.Source);
    }

    [Fact]
    public void Image_without_an_alt_is_decorative_and_with_one_it_is_named()
    {
        var image = new KlyImage { Source = "photo.png" };
        var frame = (Grid)image.Children[0];
        Assert.False(AutomationProperties.GetIsInAccessibleTree(frame));

        image.Alt = "The team";
        Assert.True(AutomationProperties.GetIsInAccessibleTree(frame));
        Assert.Equal("The team", SemanticProperties.GetDescription(frame));

        image.Alt = "";
        Assert.False(AutomationProperties.GetIsInAccessibleTree(frame));
    }

    [Fact]
    public void Image_caption_shows_under_the_picture_and_the_size_reserves_the_space()
    {
        var image = new KlyImage { Source = "photo.png", ImageWidth = 120, ImageHeight = 80 };
        var caption = (Label)image.Children[1];
        var frame = (Grid)image.Children[0];

        Assert.False(caption.IsVisible);
        Assert.Equal(120, frame.WidthRequest);
        Assert.Equal(80, frame.HeightRequest);

        image.Caption = "Our team";
        Assert.True(caption.IsVisible);
        Assert.Equal("Our team", caption.Text);

        image.Caption = null;
        Assert.False(caption.IsVisible);
    }

    // --- KlyInfiniteScroll -----------------------------------------------------------------------------------------------------------------

    [Fact]
    public void InfiniteScroll_asks_once_near_the_end_and_again_only_after_the_user_scrolled_away()
    {
        var scroll = new KlyInfiniteScroll { Content = new ScrollView { Content = new Label() }, Threshold = 200 };
        var loads = 0;
        scroll.LoadMore += (_, _) => loads++;

        scroll.Evaluate(1000);
        Assert.Equal(0, loads);

        scroll.Evaluate(150);
        scroll.Evaluate(100);
        Assert.Equal(1, loads);
        Assert.False(scroll.IsArmed);

        scroll.Evaluate(900);
        Assert.True(scroll.IsArmed);
        scroll.Evaluate(50);
        Assert.Equal(2, loads);
    }

    [Fact]
    public void InfiniteScroll_does_not_ask_while_loading_or_with_nothing_left_and_asks_again_when_a_load_ends()
    {
        var scroll = new KlyInfiniteScroll { Content = new ScrollView { Content = new Label() } };
        var loads = 0;
        scroll.LoadMore += (_, _) => loads++;

        scroll.Loading = true;
        scroll.Evaluate(0);
        Assert.Equal(0, loads);
        Assert.True(scroll.Spinner.IsRunning);
        Assert.False(scroll.MoreButton.IsEnabled);

        // The end is still in view when the load is over (a scroll view with no layout has nothing past its viewport): ask again, once.
        scroll.Loading = false;
        Assert.Equal(1, loads);
        Assert.False(scroll.Spinner.IsRunning);

        scroll.HasMore = false;
        scroll.Evaluate(0);
        scroll.SendRemainingItemsThresholdReached();
        Assert.Equal(1, loads);
    }

    [Fact]
    public void InfiniteScroll_button_loads_by_hand_and_is_disabled_while_loading()
    {
        var scroll = new KlyInfiniteScroll { Content = new ScrollView(), Label = "More" };
        var loads = 0;
        scroll.LoadMore += (_, _) => loads++;
        Assert.Equal("More", scroll.MoreButton.Text);

        scroll.MoreButton.SendClicked();
        scroll.MoreButton.SendClicked();
        Assert.Equal(2, loads);

        scroll.Loading = true;
        scroll.MoreButton.SendClicked();
        Assert.Equal(2, loads);
        Assert.False(scroll.MoreButton.IsEnabled);
    }

    [Fact]
    public void InfiniteScroll_end_of_the_list_hides_the_button_and_shows_the_end_text_when_asked()
    {
        var scroll = new KlyInfiniteScroll { Content = new ScrollView() };
        Assert.True(scroll.MoreButton.IsVisible);
        Assert.False(scroll.EndText.IsVisible);

        scroll.HasMore = false;
        Assert.False(scroll.MoreButton.IsVisible);
        Assert.False(scroll.EndText.IsVisible);

        scroll.ShowEnd = true;
        scroll.EndLabel = "Fin";
        Assert.True(scroll.EndText.IsVisible);
        Assert.Equal("Fin", scroll.EndText.Text);

        scroll.HasMore = true;
        Assert.True(scroll.MoreButton.IsVisible);
        Assert.False(scroll.EndText.IsVisible);
    }

    [Fact]
    public void InfiniteScroll_collection_view_reports_through_its_threshold()
    {
        var collection = new CollectionView();
        var scroll = new KlyInfiniteScroll { Content = collection, RemainingItemsThreshold = 7 };
        var loads = 0;
        scroll.LoadMore += (_, _) => loads++;

        Assert.Equal(7, collection.RemainingItemsThreshold);
        scroll.RemainingItemsThreshold = 4;
        Assert.Equal(4, collection.RemainingItemsThreshold);

        scroll.SendRemainingItemsThresholdReached();
        scroll.SendRemainingItemsThresholdReached();
        Assert.Equal(1, loads);
        Assert.Null(scroll.Scroller);
    }

    [Fact]
    public void InfiniteScroll_wraps_a_view_that_does_not_scroll_and_runs_the_command()
    {
        var content = new VerticalStackLayout();
        var scroll = new KlyInfiniteScroll { Content = content };
        var executed = 0;
        scroll.LoadMoreCommand = new Command(() => executed++);

        Assert.NotNull(scroll.Scroller);
        Assert.Same(content, scroll.Scroller.Content);

        scroll.MoreButton.SendClicked();
        Assert.Equal(1, executed);

        var replacement = new Label();
        scroll.Content = replacement;
        Assert.Same(replacement, scroll.Scroller!.Content);
        Assert.Null(content.Parent);
    }

    // --- KlyEmpty --------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(KlySize.Xs, 16, 8)]
    [InlineData(KlySize.Sm, 16, 12)]
    [InlineData(KlySize.Md, 24, 16)]
    [InlineData(KlySize.Lg, 32, 16)]
    [InlineData(KlySize.Xl, 40, 20)]
    public void Empty_size_sets_the_padding_and_the_space_between_the_parts(KlySize size, double padding, double gap)
    {
        var empty = new KlyEmpty { Title = "Nothing", Size = size };

        Assert.Equal(new Thickness(padding), empty.Padding);
        Assert.Equal(gap, ((VerticalStackLayout)empty.Content!).Spacing);
    }

    [Theory]
    [InlineData(KlySize.Xs, 12, 12)]
    [InlineData(KlySize.Sm, 14, 12)]
    [InlineData(KlySize.Md, 16, 14)]
    [InlineData(KlySize.Lg, 18, 16)]
    [InlineData(KlySize.Xl, 20, 16)]
    public void Empty_size_sets_the_fonts_of_the_title_and_the_description(KlySize size, double title, double description)
    {
        var empty = new KlyEmpty { Title = "Nothing", Description = "Add one", Size = size };

        Assert.Equal(title, LabelWith(empty, "Nothing").FontSize);
        Assert.Equal(description, LabelWith(empty, "Add one").FontSize);
    }

    [Fact]
    public void Empty_variants_change_the_border_and_only_the_outline_is_dashed()
    {
        var empty = new KlyEmpty();
        Assert.Equal(EmptyVariant.Outline, empty.Variant);
        Assert.Equal(1, empty.StrokeThickness);
        Assert.Equal([4d, 4d], empty.StrokeDashArray!.ToArray());

        empty.Variant = EmptyVariant.Soft;
        Assert.Equal(0, empty.StrokeThickness);
        Assert.Empty(empty.StrokeDashArray!);

        empty.Variant = EmptyVariant.Subtle;
        Assert.Equal(1, empty.StrokeThickness);
        Assert.Empty(empty.StrokeDashArray!);

        empty.Variant = EmptyVariant.Naked;
        Assert.Equal(0, empty.StrokeThickness);
    }

    [Fact]
    public void Empty_shows_the_icon_the_texts_the_body_and_the_actions_in_order()
    {
        var clicks = 0;
        var empty = new KlyEmpty
        {
            Icon = new KlyIcon { Name = "inbox" },
            Title = "No messages",
            Description = "Messages show here",
            Body = new Label { Text = "Body" },
            Actions = new Button { Text = "Raw action" },
            ActionItems = [new KlyActionItem("Compose") { OnClick = () => clicks++, Variant = ButtonVariant.Solid, Color = KlyColor.Primary }],
        };

        var root = Assert.IsType<VerticalStackLayout>(empty.Content);
        Assert.Equal(3, root.Children.Count);
        Assert.Equal(["No messages", "Messages show here", "Body"], Texts(empty));
        Assert.Single(All<KlyIcon>(empty));
        Assert.False(AutomationProperties.GetIsInAccessibleTree(All<KlyIcon>(empty).Single()));
        var buttons = All<Button>(empty);
        Assert.Equal(["Raw action", "Compose"], buttons.Select(b => b.Text).ToArray());
        Assert.Equal(ButtonVariant.Solid, ((KlyButton)buttons[1]).Variant);
        ((KlyButton)buttons[1]).SendClicked();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Empty_title_is_a_heading_only_when_a_level_is_asked()
    {
        var empty = new KlyEmpty { Title = "Nothing" };
        var title = LabelWith(empty, "Nothing");
        Assert.Equal(SemanticHeadingLevel.None, SemanticProperties.GetHeadingLevel(title));

        empty.HeadingLevel = 2;
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(title));

        empty.HeadingLevel = null;
        Assert.Equal(SemanticHeadingLevel.None, SemanticProperties.GetHeadingLevel(title));
    }

    [Fact]
    public void Empty_without_anything_has_no_parts_and_keeps_its_views_when_rebuilt()
    {
        var icon = new KlyIcon { Name = "inbox" };
        var empty = new KlyEmpty();
        Assert.Empty(((VerticalStackLayout)empty.Content!).Children);

        empty.Icon = icon;
        empty.Title = "A";
        empty.Title = "B";
        empty.Description = "C";

        Assert.Same(icon, All<KlyIcon>(empty).Single());
    }

    // --- KlyFieldGroup ---------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FieldGroup_keeps_the_radius_on_the_outer_corners_of_a_row()
    {
        var group = new KlyFieldGroup { Items = { new KlyInput(), new KlyInput(), new KlyInput() } };

        var corners = group.Items.Cast<Border>().Select(Corners).ToList();
        Assert.Equal(new CornerRadius(8, 0, 8, 0), corners[0]);
        Assert.Equal(new CornerRadius(0), corners[1]);
        Assert.Equal(new CornerRadius(0, 8, 0, 8), corners[2]);
    }

    [Fact]
    public void FieldGroup_keeps_the_radius_on_the_outer_corners_of_a_column()
    {
        var group = new KlyFieldGroup { Orientation = KlyOrientation.Vertical, Items = { new KlyInput(), new KlyInput() } };

        var corners = group.Items.Cast<Border>().Select(Corners).ToList();
        Assert.Equal(new CornerRadius(8, 8, 0, 0), corners[0]);
        Assert.Equal(new CornerRadius(0, 0, 8, 8), corners[1]);

        group.Items.Add(new KlySelect());
        corners = group.Items.Cast<Border>().Select(Corners).ToList();
        Assert.Equal(new CornerRadius(0), corners[1]);
        Assert.Equal(new CornerRadius(0, 0, 8, 8), corners[2]);
    }

    [Fact]
    public void FieldGroup_a_single_field_keeps_every_corner()
    {
        var group = new KlyFieldGroup { Items = { new KlyInput() } };

        Assert.Equal(new CornerRadius(8), Corners((Border)group.Items[0]));
    }

    [Fact]
    public void FieldGroup_lets_fields_fill_the_row_and_buttons_keep_their_width_and_overlap_by_one()
    {
        var group = new KlyFieldGroup { Items = { new KlyInput(), new KlyButton { Text = "Go" } }, AccessibleName = "Search" };

        var grid = Assert.IsType<Grid>(group.Content);
        Assert.Equal(GridUnitType.Star, grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(GridUnitType.Auto, grid.ColumnDefinitions[1].Width.GridUnitType);
        Assert.Equal(-1, grid.ColumnSpacing);
        Assert.Equal("Search", SemanticProperties.GetDescription(group));
        Assert.Equal(KlyOrientation.Horizontal, group.Orientation);
        Assert.Equal(2, grid.Children.Count);
    }

    [Fact]
    public void FieldGroup_vertical_stacks_the_fields_in_rows()
    {
        var group = new KlyFieldGroup { Items = { new KlyInput(), new KlyInput() } };
        group.Orientation = KlyOrientation.Vertical;

        var grid = Assert.IsType<Grid>(group.Content);
        Assert.Equal(2, grid.RowDefinitions.Count);
        Assert.Empty(grid.ColumnDefinitions);
        Assert.Equal(-1, grid.RowSpacing);

        group.Items.RemoveAt(1);
        Assert.Single(grid.Children);
    }

    // --- KlyUser ---------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void User_shows_an_avatar_with_the_initials_the_name_and_the_description()
    {
        var user = new KlyUser { Name = "Ada Lovelace", Description = "Engineer" };

        var avatar = All<KlyAvatar>(user).Single();
        Assert.Equal("Ada Lovelace", avatar.Text);
        Assert.Equal(KlySize.Md, avatar.Size);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(avatar));
        Assert.Equal(["AL", "Ada Lovelace", "Engineer"], Texts(user));
        Assert.Equal(14, LabelWith(user, "Ada Lovelace").FontSize);
        Assert.Equal(12, LabelWith(user, "Engineer").FontSize);
    }

    [Theory]
    [InlineData(KlySize.Xs, 12, 11)]
    [InlineData(KlySize.Sm, 12, 12)]
    [InlineData(KlySize.Md, 14, 12)]
    [InlineData(KlySize.Lg, 14, 14)]
    [InlineData(KlySize.Xl, 16, 14)]
    public void User_size_sets_the_avatar_and_the_fonts(KlySize size, double name, double description)
    {
        var user = new KlyUser { Name = "Ada", Description = "Engineer", Size = size };

        Assert.Equal(size, All<KlyAvatar>(user).Single().Size);
        Assert.Equal(name, LabelWith(user, "Ada").FontSize);
        Assert.Equal(description, LabelWith(user, "Engineer").FontSize);
    }

    [Fact]
    public void User_without_a_name_and_a_picture_shows_a_person_icon_and_an_avatar_icon_replaces_it()
    {
        var user = new KlyUser();
        Assert.Empty(All<KlyAvatar>(user));
        Assert.Equal("user", All<KlyIcon>(user).Single().Name);

        var custom = new KlyIcon { Name = "star" };
        user.AvatarIcon = custom;
        Assert.Same(custom, All<KlyIcon>(user).Single());

        user.Name = "Ada";
        user.AvatarIcon = null;
        Assert.Single(All<KlyAvatar>(user));
        Assert.Empty(All<KlyIcon>(user));
    }

    [Fact]
    public void User_can_hide_the_avatar_stack_the_name_under_it_and_show_a_badge()
    {
        var user = new KlyUser { Name = "Ada", ShowAvatar = false, Badge = new KlyBadgeItem("Admin") { Color = KlyColor.Info } };
        Assert.Empty(All<KlyAvatar>(user));
        Assert.Equal("Admin", All<KlyBadge>(user).Single().Text);
        Assert.Equal(KlyColor.Info, All<KlyBadge>(user).Single().Color);

        user.ShowAvatar = true;
        user.Orientation = KlyOrientation.Vertical;
        Assert.IsType<VerticalStackLayout>(((Border)user.Person).Content);
        Assert.Equal(TextAlignment.Center, LabelWith(user, "Ada").HorizontalTextAlignment);

        user.Orientation = KlyOrientation.Horizontal;
        Assert.IsType<Grid>(((Border)user.Person).Content);
    }

    [Fact]
    public void User_contacts_sit_under_the_user_with_a_link_and_a_copy_button_each()
    {
        var user = new KlyUser
        {
            Name = "Ada",
            Contacts = [new KlyContactItem("ada@example.com") { Icon = "mail", Href = "mailto:ada@example.com" }, new KlyContactItem("+33 1 23") { Copy = false }],
        };

        Assert.Equal(["A", "Ada", "ada@example.com", "+33 1 23"], Texts(user));
        Assert.Equal(TextDecorations.Underline, LabelWith(user, "ada@example.com").TextDecorations);
        Assert.Equal(TextDecorations.None, LabelWith(user, "+33 1 23").TextDecorations);
        var copy = All<DisplayIconButton>(user).Single();
        Assert.Equal("Copy ada@example.com", SemanticProperties.GetDescription(copy));
        Assert.Equal("mail", All<KlyIcon>(user).Single(i => i.Name == "mail").Name);

        user.CopyLabelFormat = "Copier {0}";
        Assert.Equal("Copier ada@example.com", SemanticProperties.GetDescription(All<DisplayIconButton>(user).Single()));
    }

    [Fact]
    public void User_is_tappable_when_it_has_an_address_or_a_handler_and_one_thing_for_a_screen_reader()
    {
        var user = new KlyUser { Name = "Ada", Description = "Engineer" };
        var clicks = 0;
        user.SendTapped();
        user.Clicked += (_, _) => clicks++;

        user.SendTapped();
        Assert.Equal(1, clicks);

        Assert.Null(SemanticProperties.GetDescription(user.Person));
        user.Href = "https://example.com/ada";
        user.Target = KlyLinkTarget.Blank;
        Assert.Equal("Ada, Engineer", SemanticProperties.GetDescription(user.Person));
        Assert.Equal(KlyLinkTarget.Blank, user.Target);
    }

    // --- Two-way binding ---------------------------------------------------------------------------------------------------------------------

    // A binding needs a dispatcher, which the plain framework target has not: the test checks what makes a property two-way, and that the toggles write the property.
    [Fact]
    public void The_state_the_user_changes_is_a_two_way_bindable_property_and_the_toggles_write_it()
    {
        Assert.Equal(BindingMode.TwoWay, KlyBreakdown.IsOpenProperty.DefaultBindingMode);
        Assert.Equal(BindingMode.TwoWay, KlyText.IsExpandedProperty.DefaultBindingMode);

        var breakdown = new KlyBreakdown { Collapsible = true, Lines = [new KlyBreakdownLine("Item") { Amount = 1m }] };
        var written = new List<string?>();
        breakdown.PropertyChanged += (_, e) => written.Add(e.PropertyName);
        All<Button>(breakdown).Single().SendClicked();
        Assert.Contains(nameof(KlyBreakdown.IsOpen), written);

        var text = new KlyText { Value = "Long text", MaxLines = 2 };
        written.Clear();
        text.PropertyChanged += (_, e) => written.Add(e.PropertyName);
        text.ToggleButton!.SendClicked();
        Assert.Contains(nameof(KlyText.IsExpanded), written);
    }

    // --- Shared ----------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ActionItem_defaults_to_a_neutral_outline_button()
    {
        var item = new KlyActionItem("Go");

        Assert.Equal(KlyColor.Neutral, item.Color);
        Assert.Equal(ButtonVariant.Outline, item.Variant);
        Assert.Null(item.Href);
        Assert.Null(item.OnClick);
    }

    [Fact]
    public void Contact_and_badge_items_have_their_defaults()
    {
        Assert.True(new KlyContactItem("a").Copy);
        Assert.Null(new KlyBadgeItem("a").Color);
        Assert.True(new KlyDiffItem("a", "b", "c").Changed);
    }

    [Fact]
    public void DisplayReveal_reveals_by_step_and_never_hides_what_was_shown()
    {
        var reveal = new DisplayReveal();
        var source = Enumerable.Range(1, 10).ToList();

        reveal.Sync(3);
        var (visible, remaining) = reveal.Apply(source, 3, 4);
        Assert.Equal(3, visible.Count);
        Assert.Equal(4, remaining);

        reveal.More(source.Count, 3, 4);
        (visible, remaining) = reveal.Apply(source, 3, 4);
        Assert.Equal(7, visible.Count);
        Assert.Equal(3, remaining);

        reveal.Sync(2);
        Assert.Equal(7, reveal.Apply(source, 2, 4).Visible.Count);

        reveal.More(source.Count, 3, null);
        Assert.Equal(0, reveal.Apply(source, 3, 4).Remaining);
        Assert.Equal(10, reveal.Apply(source, 3, 4).Visible.Count);

        Assert.Equal(10, new DisplayReveal().Apply(source, null, null).Visible.Count);
        Assert.Empty(new DisplayReveal().Apply<int>(null, 3, null).Visible);
    }
}
