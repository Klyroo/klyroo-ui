namespace Klyroo.UI.Maui;

/// <summary>
/// The card of one pricing plan, with its price, features and call to action. From 1024 units wide a <see cref="StackOrientation.Horizontal"/> plan lays out in a row:
/// the price on the left, the features, then the button.
/// </summary>
public class KlyPricingPlan : Border
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, v) => ((KlyPricingPlan)b).ShowTitleRow((string?)v));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyPricingPlan), 3, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPricingPlan)b)._title, (int)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, v) => ((KlyPricingPlan)b).ShowHeader((string?)v));
    /// <summary>Identifies <see cref="Badge"/>.</summary>
    public static readonly BindableProperty BadgeProperty = BindableProperty.Create(nameof(Badge), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowBadge());
    /// <summary>Identifies <see cref="BadgeColor"/>.</summary>
    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(nameof(BadgeColor), typeof(KlyColor?), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowBadge());
    /// <summary>Identifies <see cref="Tagline"/>.</summary>
    public static readonly BindableProperty TaglineProperty = BindableProperty.Create(nameof(Tagline), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPricingPlan)b)._tagline, (string?)v));
    /// <summary>Identifies <see cref="Price"/>.</summary>
    public static readonly BindableProperty PriceProperty = BindableProperty.Create(nameof(Price), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowPrice());
    /// <summary>Identifies <see cref="Discount"/>.</summary>
    public static readonly BindableProperty DiscountProperty = BindableProperty.Create(nameof(Discount), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowPrice());
    /// <summary>Identifies <see cref="BillingCycle"/>.</summary>
    public static readonly BindableProperty BillingCycleProperty = BindableProperty.Create(nameof(BillingCycle), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPricingPlan)b)._cycle, (string?)v));
    /// <summary>Identifies <see cref="Terms"/>.</summary>
    public static readonly BindableProperty TermsProperty = BindableProperty.Create(nameof(Terms), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPricingPlan)b)._terms, (string?)v));
    /// <summary>Identifies <see cref="OriginalPriceLabel"/>.</summary>
    public static readonly BindableProperty OriginalPriceLabelProperty = BindableProperty.Create(nameof(OriginalPriceLabel), typeof(string), typeof(KlyPricingPlan), "Original price", propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowPrice());
    /// <summary>Identifies <see cref="DiscountedPriceLabel"/>.</summary>
    public static readonly BindableProperty DiscountedPriceLabelProperty = BindableProperty.Create(nameof(DiscountedPriceLabel), typeof(string), typeof(KlyPricingPlan), "Now", propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowPrice());
    /// <summary>Identifies <see cref="Features"/>.</summary>
    public static readonly BindableProperty FeaturesProperty = BindableProperty.Create(nameof(Features), typeof(IReadOnlyList<KlyPricingFeature>), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowFeatures());
    /// <summary>Identifies <see cref="ButtonLabel"/>.</summary>
    public static readonly BindableProperty ButtonLabelProperty = BindableProperty.Create(nameof(ButtonLabel), typeof(string), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowFooter());
    /// <summary>Identifies <see cref="ButtonHref"/>.</summary>
    public static readonly BindableProperty ButtonHrefProperty = BindableProperty.Create(nameof(ButtonHref), typeof(string), typeof(KlyPricingPlan), null);
    /// <summary>Identifies <see cref="ButtonColor"/>.</summary>
    public static readonly BindableProperty ButtonColorProperty = BindableProperty.Create(nameof(ButtonColor), typeof(KlyColor?), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ApplyStyle());
    /// <summary>Identifies <see cref="ButtonVariant"/>.</summary>
    public static readonly BindableProperty ButtonVariantProperty = BindableProperty.Create(nameof(ButtonVariant), typeof(ButtonVariant?), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ApplyStyle());
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyPricingPlan), null, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ShowFooter());
    /// <summary>Identifies <see cref="Highlight"/>.</summary>
    public static readonly BindableProperty HighlightProperty = BindableProperty.Create(nameof(Highlight), typeof(bool), typeof(KlyPricingPlan), false, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ApplyStyle());
    /// <summary>Identifies <see cref="Enlarge"/>.</summary>
    public static readonly BindableProperty EnlargeProperty = BindableProperty.Create(nameof(Enlarge), typeof(bool), typeof(KlyPricingPlan), false, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).Relayout(((KlyPricingPlan)b)._width));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyVariant), typeof(KlyPricingPlan), KlyVariant.Outline, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ApplyStyle());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPricingPlan), StackOrientation.Vertical, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).Relayout(((KlyPricingPlan)b)._width));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPricingPlan), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyPricingPlan)b).ApplyStyle());

    private readonly Label _title = PagesUi.Text(20, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly KlyBadge _badge = new() { Variant = KlyVariant.Subtle, Size = KlySize.Sm, IsVisible = false, VerticalOptions = LayoutOptions.Center };
    private readonly FlexLayout _titleRow = PagesUi.Row();
    private readonly Label _description = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly VerticalStackLayout _header = new() { Spacing = 8, IsVisible = false };
    private readonly Label _tagline = PagesUi.Text(14, KlyTheme.TextMuted, FontAttributes.Bold);
    private readonly Label _discount = PagesUi.Text(20, KlyTheme.TextMuted);
    private readonly Label _price = PagesUi.Text(36, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _cycle = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly Label _terms = PagesUi.Text(12, KlyTheme.TextMuted);
    private readonly HorizontalStackLayout _priceRow = new() { Spacing = 8, IsVisible = false };
    private readonly VerticalStackLayout _pricing = new() { Spacing = 4, IsVisible = false };
    private readonly VerticalStackLayout _intro = new() { Spacing = 24 };
    private readonly VerticalStackLayout _features = new() { Spacing = 8, IsVisible = false };
    private readonly ContentView _footer = PagesUi.Slot();
    private readonly KlyButton _button = new() { Block = true, Size = KlySize.Lg };
    private readonly Grid _layout = new();
    private double _width;

    /// <summary>Creates a pricing plan.</summary>
    public KlyPricingPlan()
    {
        this.SetRadius(KlyTheme.RadiusOf(2));
        _discount.TextDecorations = TextDecorations.Strikethrough;
        _titleRow.Children.Add(_title);
        _titleRow.Children.Add(_badge);
        _title.Margin = new Thickness(0, 0, 8, 0);
        _header.Children.Add(_titleRow);
        _header.Children.Add(_description);
        foreach (var label in new[] { _discount, _price, _cycle }) _priceRow.Children.Add(label);
        _price.VerticalOptions = LayoutOptions.End;
        _discount.VerticalOptions = LayoutOptions.End;
        _cycle.VerticalOptions = LayoutOptions.End;
        _cycle.Margin = new Thickness(0, 0, 0, 6);
        _discount.Margin = new Thickness(0, 0, 0, 4);
        _pricing.Children.Add(_tagline);
        _pricing.Children.Add(_priceRow);
        _pricing.Children.Add(_terms);
        _intro.Children.Add(_header);
        _intro.Children.Add(_pricing);
        _button.Clicked += (_, _) => PagesLinks.Follow(this, ButtonClicked, ButtonHref, null);
        PagesUi.Heading(_title, HeadingLevel);
        Content = _layout;

        SizeChanged += (_, _) => Relayout(Width);
        ApplyStyle();
        Relayout(0);
    }

    /// <summary>Raised when the user presses the call to action button.</summary>
    public event EventHandler<PageLinkEventArgs>? ButtonClicked;

    /// <summary>The plan name.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Level of the title heading for a screen reader: 3 by default, for plans under a level 2 heading.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Secondary text of the pricing plan, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Text of a badge next to the title, such as "Popular".</summary>
    public string? Badge { get => (string?)GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }

    /// <summary>Color of the badge; the plan's color by default, neutral on a solid plan.</summary>
    public KlyColor? BadgeColor { get => (KlyColor?)GetValue(BadgeColorProperty); set => SetValue(BadgeColorProperty, value); }

    /// <summary>A value statement above the price.</summary>
    public string? Tagline { get => (string?)GetValue(TaglineProperty); set => SetValue(TaglineProperty, value); }

    /// <summary>The price, formatted by the caller ("$29"). With <see cref="Discount"/> it is the original price, struck through.</summary>
    public string? Price { get => (string?)GetValue(PriceProperty); set => SetValue(PriceProperty, value); }

    /// <summary>The discounted price, shown instead of <see cref="Price"/>.</summary>
    public string? Discount { get => (string?)GetValue(DiscountProperty); set => SetValue(DiscountProperty, value); }

    /// <summary>Text after the price: "/month".</summary>
    public string? BillingCycle { get => (string?)GetValue(BillingCycleProperty); set => SetValue(BillingCycleProperty, value); }

    /// <summary>Fine print under the price.</summary>
    public string? Terms { get => (string?)GetValue(TermsProperty); set => SetValue(TermsProperty, value); }

    /// <summary>What a screen reader says before the struck-through price, in the language of the app. The strike is only visual.</summary>
    public string OriginalPriceLabel { get => (string)GetValue(OriginalPriceLabelProperty); set => SetValue(OriginalPriceLabelProperty, value); }

    /// <summary>What a screen reader says before the discounted price, in the language of the app.</summary>
    public string DiscountedPriceLabel { get => (string)GetValue(DiscountedPriceLabelProperty); set => SetValue(DiscountedPriceLabelProperty, value); }

    /// <summary>The included features. A string converts to a feature.</summary>
    public IReadOnlyList<KlyPricingFeature>? Features { get => (IReadOnlyList<KlyPricingFeature>?)GetValue(FeaturesProperty); set => SetValue(FeaturesProperty, value); }

    /// <summary>Label of the button at the bottom.</summary>
    public string? ButtonLabel { get => (string?)GetValue(ButtonLabelProperty); set => SetValue(ButtonLabelProperty, value); }

    /// <summary>The address <see cref="ButtonClicked"/> reports.</summary>
    public string? ButtonHref { get => (string?)GetValue(ButtonHrefProperty); set => SetValue(ButtonHrefProperty, value); }

    /// <summary>Color of the call to action button. Defaults to neutral for a solid plan, else to the color of the plan.</summary>
    public KlyColor? ButtonColor { get => (KlyColor?)GetValue(ButtonColorProperty); set => SetValue(ButtonColorProperty, value); }

    /// <summary>Look of the call to action button. Defaults to solid for a highlighted or solid plan, else to outline.</summary>
    public ButtonVariant? ButtonVariant { get => (ButtonVariant?)GetValue(ButtonVariantProperty); set => SetValue(ButtonVariantProperty, value); }

    /// <summary>Replaces the button.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>Adds a 2 unit ring around the plan.</summary>
    public bool Highlight { get => (bool)GetValue(HighlightProperty); set => SetValue(HighlightProperty, value); }

    /// <summary>Enlarges the plan by 5% from 1024 units wide so it stands out from the others.</summary>
    public bool Enlarge { get => (bool)GetValue(EnlargeProperty); set => SetValue(EnlargeProperty, value); }

    /// <summary>Look of the plan card: <c>Solid</c>, <c>Outline</c>, <c>Soft</c> or <c>Subtle</c>.</summary>
    public KlyVariant Variant { get => (KlyVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Horizontal lays the plan out in a row from 1024 units wide: the price on the left, the features, then the button.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the plan: the ring of a highlight, the tagline, the check marks and the solid fill.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    internal Label TitleLabel => _title;

    internal Label PriceLabel => _price;

    internal Label DiscountLabel => _discount;

    internal KlyBadge BadgeView => _badge;

    internal KlyButton Button => _button;

    internal VerticalStackLayout FeatureList => _features;

    internal ContentView FooterSlot => _footer;

    internal Grid LayoutGrid => _layout;

    internal void Relayout(double width)
    {
        _width = width;
        var row = Orientation == StackOrientation.Horizontal && width >= PagesUi.Lg;
        var padding = width >= PagesUi.Sm ? 32d : 24d;
        Padding = new Thickness(padding);
        Scale = Enlarge && width >= PagesUi.Lg ? 1.05 : 1;
        ZIndex = Enlarge && width >= PagesUi.Lg ? 10 : 0;

        _layout.Children.Clear();
        _layout.ColumnDefinitions = new ColumnDefinitionCollection();
        _layout.RowDefinitions = new RowDefinitionCollection();
        if (row)
        {
            _layout.RowSpacing = 0;
            _layout.ColumnSpacing = 40;
            _layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(192)));
            _intro.VerticalOptions = LayoutOptions.Center;
            _features.VerticalOptions = LayoutOptions.Center;
            _footer.VerticalOptions = LayoutOptions.Center;
            _layout.Add(_intro, 0, 0);
            _layout.Add(_features, 1, 0);
            _layout.Add(_footer, 2, 0);
        }
        else
        {
            _layout.ColumnSpacing = 0;
            _layout.RowSpacing = 24;
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _intro.VerticalOptions = LayoutOptions.Start;
            _features.VerticalOptions = LayoutOptions.Start;
            // The button sticks to the bottom of the card, so the buttons of plans of one row line up.
            _footer.VerticalOptions = LayoutOptions.End;
            _layout.Add(_intro, 0, 0);
            _layout.Add(_features, 0, 1);
            _layout.Add(_footer, 0, 2);
        }
    }

    private void ShowTitleRow(string? text)
    {
        PagesUi.Show(_title, text);
        ShowHeader(Description);
    }

    private void ShowHeader(string? description)
    {
        PagesUi.Show(_description, description);
        _titleRow.IsVisible = _title.IsVisible || _badge.IsVisible;
        _header.IsVisible = _titleRow.IsVisible || _description.IsVisible;
    }

    private void ShowBadge()
    {
        _badge.Text = Badge;
        _badge.IsVisible = !string.IsNullOrEmpty(Badge);
        ApplyStyle();
        ShowHeader(Description);
    }

    private void ShowPrice()
    {
        var discounted = Discount is not null;
        PagesUi.Show(_discount, discounted ? Price : null);
        PagesUi.Show(_price, discounted ? Discount : Price);
        // The strike is visual only: spell out which price is which.
        SemanticProperties.SetDescription(_discount, discounted ? $"{OriginalPriceLabel} {Price}" : null);
        SemanticProperties.SetDescription(_price, discounted ? $"{DiscountedPriceLabel} {Discount}" : null);
        _priceRow.IsVisible = Price is not null;
        _pricing.IsVisible = Price is not null;
    }

    private void ShowFeatures()
    {
        _features.Children.Clear();
        _features.IsVisible = Features is { Count: > 0 };
        if (Features is null) return;
        var textColor = Variant == KlyVariant.Solid ? KlyTheme.Resolve(Color).Contrast : KlyTheme.Text;
        var iconColor = Variant == KlyVariant.Solid ? KlyTheme.Resolve(Color).Contrast : KlyTheme.Resolve(Color).Text;
        foreach (var feature in Features)
        {
            var label = PagesUi.Text(14, textColor);
            label.Text = feature.Title;
            label.IsVisible = true;
            var icon = PagesUi.Icon(feature.Icon ?? KlyIconNames.Check, KlySize.Xs, iconColor);
            icon.VerticalOptions = LayoutOptions.Start;
            icon.Margin = new Thickness(0, 2, 0, 0);
            var row = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
            row.Add(icon, 0, 0);
            row.Add(label, 1, 0);
            _features.Children.Add(row);
        }
    }

    private void ShowFooter()
    {
        _button.Text = ButtonLabel;
        PagesUi.Put(_footer, Footer ?? (string.IsNullOrEmpty(ButtonLabel) ? null : _button));
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        var solid = Variant == KlyVariant.Solid;
        var tint = KlyTheme.BackgroundElevated.WithAlpha(0.5f);
        var surface = Variant switch
        {
            KlyVariant.Outline => new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text),
            KlyVariant.Soft => new Surface(tint, KlyThemeColor.Transparent, 0, KlyTheme.Text),
            KlyVariant.Subtle => new Surface(tint, KlyTheme.Border, 1, KlyTheme.Text),
            KlyVariant.Solid => new Surface(set.Fill, set.Text, 1, set.Contrast),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        };
        this.Apply(surface);
        if (Highlight)
        {
            this.SetStroke(solid ? KlyTheme.TextHighlighted : set.Text);
            StrokeThickness = 2;
        }

        // On a solid plan every text takes the color that reads on the fill.
        KlyThemeColor Pick(KlyThemeColor normal) => solid ? set.Contrast : normal;
        _title.SetThemeColor(Label.TextColorProperty, Pick(KlyTheme.TextHighlighted));
        _description.SetThemeColor(Label.TextColorProperty, Pick(KlyTheme.TextMuted));
        _tagline.SetThemeColor(Label.TextColorProperty, Pick(set.Text));
        _price.SetThemeColor(Label.TextColorProperty, Pick(KlyTheme.TextHighlighted));
        _discount.SetThemeColor(Label.TextColorProperty, Pick(KlyTheme.TextMuted));
        _cycle.SetThemeColor(Label.TextColorProperty, Pick(KlyTheme.TextMuted));
        _terms.SetThemeColor(Label.TextColorProperty, Pick(KlyTheme.TextMuted));
        _badge.Color = solid ? KlyColor.Neutral : BadgeColor ?? Color;

        _button.Color = ButtonColor ?? (solid ? KlyColor.Neutral : Color);
        _button.Variant = ButtonVariant ?? (Highlight || solid ? Klyroo.UI.Maui.ButtonVariant.Solid : Klyroo.UI.Maui.ButtonVariant.Outline);
        ShowFeatures();
    }
}
