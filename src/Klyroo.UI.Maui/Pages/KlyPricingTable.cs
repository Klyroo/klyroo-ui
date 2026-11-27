namespace Klyroo.UI.Maui;

/// <summary>
/// A comparison table of pricing tiers and their features. From 768 units wide it is a table with a column per tier; below, each tier becomes a card and
/// each feature lists its value per tier under the tier name.
/// </summary>
public class KlyPricingTable : ContentView
{
    /// <summary>Identifies <see cref="Tiers"/>.</summary>
    public static readonly BindableProperty TiersProperty = BindableProperty.Create(nameof(Tiers), typeof(IReadOnlyList<KlyPricingTier>), typeof(KlyPricingTable), null, propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());
    /// <summary>Identifies <see cref="Sections"/>.</summary>
    public static readonly BindableProperty SectionsProperty = BindableProperty.Create(nameof(Sections), typeof(IReadOnlyList<KlyPricingSection>), typeof(KlyPricingTable), null, propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(nameof(Caption), typeof(string), typeof(KlyPricingTable), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyPricingTable)b, (string?)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPricingTable), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());
    /// <summary>Identifies <see cref="IncludedLabel"/>.</summary>
    public static readonly BindableProperty IncludedLabelProperty = BindableProperty.Create(nameof(IncludedLabel), typeof(string), typeof(KlyPricingTable), "Included", propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());
    /// <summary>Identifies <see cref="NotIncludedLabel"/>.</summary>
    public static readonly BindableProperty NotIncludedLabelProperty = BindableProperty.Create(nameof(NotIncludedLabel), typeof(string), typeof(KlyPricingTable), "Not included", propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());
    /// <summary>Identifies <see cref="OriginalPriceLabel"/>.</summary>
    public static readonly BindableProperty OriginalPriceLabelProperty = BindableProperty.Create(nameof(OriginalPriceLabel), typeof(string), typeof(KlyPricingTable), "Original price", propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());
    /// <summary>Identifies <see cref="DiscountedPriceLabel"/>.</summary>
    public static readonly BindableProperty DiscountedPriceLabelProperty = BindableProperty.Create(nameof(DiscountedPriceLabel), typeof(string), typeof(KlyPricingTable), "Now", propertyChanged: (b, _, _) => ((KlyPricingTable)b).Rebuild());

    private double _width;

    /// <summary>Creates a pricing table.</summary>
    public KlyPricingTable()
    {
        SizeChanged += (_, _) => Relayout(Width);
        Rebuild();
    }

    /// <summary>Raised when the user presses the call to action button of a tier. <see cref="PageLinkEventArgs.Item"/> is the <see cref="KlyPricingTier"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? TierButtonClicked;

    /// <summary>The columns.</summary>
    public IReadOnlyList<KlyPricingTier>? Tiers { get => (IReadOnlyList<KlyPricingTier>?)GetValue(TiersProperty); set => SetValue(TiersProperty, value); }

    /// <summary>The groups of rows.</summary>
    public IReadOnlyList<KlyPricingSection>? Sections { get => (IReadOnlyList<KlyPricingSection>?)GetValue(SectionsProperty); set => SetValue(SectionsProperty, value); }

    /// <summary>Name of the table for assistive technology. It is not shown.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Semantic color of the accent of the recommended tier.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for a check mark, in the language of the app.</summary>
    public string IncludedLabel { get => (string)GetValue(IncludedLabelProperty); set => SetValue(IncludedLabelProperty, value); }

    /// <summary>What a screen reader says for a minus sign, in the language of the app.</summary>
    public string NotIncludedLabel { get => (string)GetValue(NotIncludedLabelProperty); set => SetValue(NotIncludedLabelProperty, value); }

    /// <summary>What a screen reader says before a struck-through price, in the language of the app.</summary>
    public string OriginalPriceLabel { get => (string)GetValue(OriginalPriceLabelProperty); set => SetValue(OriginalPriceLabelProperty, value); }

    /// <summary>What a screen reader says before a discounted price, in the language of the app.</summary>
    public string DiscountedPriceLabel { get => (string)GetValue(DiscountedPriceLabelProperty); set => SetValue(DiscountedPriceLabelProperty, value); }

    internal bool IsTable => _width >= PagesUi.Md;

    internal void Relayout(double width)
    {
        var wasTable = IsTable;
        _width = width;
        if (wasTable != IsTable) Rebuild();
    }

    private static KlyPricingTableValue ValueOf(KlyPricingTableFeature feature, KlyPricingTier tier, int index) =>
        feature.ByTier is not null && feature.ByTier.TryGetValue(tier.Id, out var byId) ? byId
        : index < feature.Values.Length ? feature.Values[index]
        : default;

    private void Rebuild() => Content = IsTable ? BuildTable() : BuildCards();

    private Grid BuildTable()
    {
        var tiers = Tiers ?? [];
        var grid = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        foreach (var _ in tiers) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(tiers.Count == 0 ? 1 : 2d / tiers.Count, GridUnitType.Star)));
        var columns = tiers.Count + 1;

        var row = 0;
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.Add(Rule(KlyTheme.Border, 1), 0, 0);
        for (var i = 0; i < tiers.Count; i++)
        {
            var cell = Header(tiers[i]);
            cell.Padding = new Thickness(16, 20);
            grid.Add(cell, i + 1, 0);
            var rule = Rule(tiers[i].Highlight ? KlyTheme.Resolve(Color).Text : KlyTheme.Border, tiers[i].Highlight ? 2 : 1);
            grid.Add(rule, i + 1, 0);
        }

        foreach (var section in Sections ?? [])
        {
            row++;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var title = Title(section.Title, 14, FontAttributes.Bold);
            title.Margin = new Thickness(16, 32, 16, 8);
            SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level3);
            grid.Add(title, 0, row);
            Grid.SetColumnSpan((BindableObject)title, columns);
            var sectionRule = Rule(KlyTheme.Border, 1);
            grid.Add(sectionRule, 0, row);
            Grid.SetColumnSpan((BindableObject)sectionRule, columns);

            foreach (var feature in section.Features)
            {
                row++;
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var name = Title(feature.Title, 14, FontAttributes.None);
                name.Margin = new Thickness(16, 12);
                grid.Add(name, 0, row);
                for (var i = 0; i < tiers.Count; i++)
                {
                    var value = ValueView(ValueOf(feature, tiers[i], i));
                    value.Margin = new Thickness(16, 12);
                    value.HorizontalOptions = LayoutOptions.Start;
                    value.VerticalOptions = LayoutOptions.Center;
                    grid.Add(value, i + 1, row);
                }
                var rule = Rule(KlyTheme.Border, 1);
                grid.Add(rule, 0, row);
                Grid.SetColumnSpan((BindableObject)rule, columns);
            }
        }
        return grid;
    }

    private VerticalStackLayout BuildCards()
    {
        var tiers = Tiers ?? [];
        var stack = new VerticalStackLayout { Spacing = 16 };
        foreach (var tier in tiers)
        {
            var card = new Border { Padding = new Thickness(16), StrokeThickness = tier.Highlight ? 2 : 1, Content = Header(tier) };
            card.SetRadius(KlyTheme.RadiusOf(2));
            card.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
            card.SetStroke(tier.Highlight ? KlyTheme.Resolve(Color).Text : KlyTheme.Border);
            stack.Children.Add(card);
        }

        foreach (var section in Sections ?? [])
        {
            var title = Title(section.Title, 14, FontAttributes.Bold);
            title.Margin = new Thickness(0, 16, 0, 0);
            SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level3);
            stack.Children.Add(title);
            foreach (var feature in section.Features)
            {
                var item = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(0, 12) };
                item.Children.Add(Title(feature.Title, 14, FontAttributes.Bold));
                for (var i = 0; i < tiers.Count; i++)
                {
                    var line = new Grid { ColumnSpacing = 16, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
                    var tier = Title(tiers[i].Title, 14, FontAttributes.Bold);
                    tier.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
                    var value = ValueView(ValueOf(feature, tiers[i], i));
                    value.HorizontalOptions = LayoutOptions.End;
                    line.Add(tier, 0, 0);
                    line.Add(value, 1, 0);
                    item.Children.Add(line);
                }
                stack.Children.Add(new VerticalStackLayout { Children = { item, PagesUi.Line(KlyTheme.Border) } });
            }
        }
        return stack;
    }

    private static BoxView Rule(KlyThemeColor color, double height)
    {
        var line = PagesUi.Line(color);
        line.HeightRequest = height;
        line.VerticalOptions = LayoutOptions.End;
        return line;
    }

    private static Label Title(string text, double size, FontAttributes attributes)
    {
        var label = PagesUi.Text(size, KlyTheme.TextHighlighted, attributes);
        label.Text = text;
        label.IsVisible = true;
        return label;
    }

    private View ValueView(KlyPricingTableValue value)
    {
        if (value.IsIncluded)
        {
            var icon = PagesUi.Icon(KlyIconNames.Check, KlySize.Md, KlyTheme.Resolve(KlyColor.Success).Text);
            icon.Label = IncludedLabel;
            return icon;
        }
        if (value.IsNotIncluded)
        {
            var icon = PagesUi.Icon(KlyIconNames.Minus, KlySize.Md, KlyTheme.TextDimmed);
            icon.Label = NotIncludedLabel;
            return icon;
        }
        var label = PagesUi.Text(14, KlyTheme.Text);
        label.Text = value.TextOf(System.Globalization.CultureInfo.CurrentCulture);
        label.IsVisible = true;
        return label;
    }

    private VerticalStackLayout Header(KlyPricingTier tier)
    {
        var header = new VerticalStackLayout { Spacing = 4 };
        header.Children.Add(Title(tier.Title, 16, FontAttributes.Bold));
        if (!string.IsNullOrEmpty(tier.Badge))
        {
            header.Children.Add(new KlyBadge { Text = tier.Badge, Variant = KlyVariant.Subtle, Size = KlySize.Sm });
        }
        if (!string.IsNullOrEmpty(tier.Description))
        {
            var description = PagesUi.Text(14, KlyTheme.TextMuted);
            PagesUi.Show(description, tier.Description);
            header.Children.Add(description);
        }
        if (tier.Price is not null)
        {
            var price = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
            if (tier.Discount is not null)
            {
                var original = PagesUi.Text(16, KlyTheme.TextMuted);
                PagesUi.Show(original, tier.Price);
                original.TextDecorations = TextDecorations.Strikethrough;
                original.VerticalOptions = LayoutOptions.End;
                SemanticProperties.SetDescription(original, $"{OriginalPriceLabel} {tier.Price}");
                var now = Title(tier.Discount, 24, FontAttributes.Bold);
                now.VerticalOptions = LayoutOptions.End;
                SemanticProperties.SetDescription(now, $"{DiscountedPriceLabel} {tier.Discount}");
                price.Children.Add(original);
                price.Children.Add(now);
            }
            else
            {
                var now = Title(tier.Price, 24, FontAttributes.Bold);
                now.VerticalOptions = LayoutOptions.End;
                price.Children.Add(now);
            }
            if (tier.BillingCycle is not null)
            {
                var cycle = PagesUi.Text(14, KlyTheme.TextMuted);
                PagesUi.Show(cycle, tier.BillingCycle);
                cycle.VerticalOptions = LayoutOptions.End;
                price.Children.Add(cycle);
            }
            header.Children.Add(price);
        }
        if (tier.ButtonLabel is not null)
        {
            var button = new KlyButton { Text = tier.ButtonLabel, Block = true, Variant = tier.Highlight ? ButtonVariant.Solid : ButtonVariant.Outline, Margin = new Thickness(0, 12, 0, 0) };
            button.Clicked += (_, _) => PagesLinks.Follow(this, TierButtonClicked, tier.ButtonHref, tier);
            header.Children.Add(button);
        }
        return header;
    }
}
