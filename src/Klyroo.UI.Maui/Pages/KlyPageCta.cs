using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// A call to action band with a title, a description and links. From 1024 units wide a <see cref="StackOrientation.Horizontal"/> band puts the links beside the text;
/// below, or when vertical, everything is stacked and centered. In the <c>Solid</c> variant give the links a neutral color: a primary button would vanish.
/// </summary>
[ContentProperty(nameof(Body))]
public class KlyPageCta : ContentView
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageCta), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageCta)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPageCta), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageCta)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<PageLinkItem>), typeof(KlyPageCta), null, propertyChanged: (b, _, _) => ((KlyPageCta)b).ShowLinks());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageCta), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageCta)b)._body, (View?)v));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyVariant), typeof(KlyPageCta), KlyVariant.Outline, propertyChanged: (b, _, _) => ((KlyPageCta)b).ApplyStyle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPageCta), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyPageCta)b).ApplyStyle());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPageCta), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyPageCta)b).Relayout(((KlyPageCta)b)._width));
    /// <summary>Identifies <see cref="Reverse"/>.</summary>
    public static readonly BindableProperty ReverseProperty = BindableProperty.Create(nameof(Reverse), typeof(bool), typeof(KlyPageCta), false, propertyChanged: (b, _, _) => ((KlyPageCta)b).Relayout(((KlyPageCta)b)._width));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyPageCta), 2, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageCta)b)._title, (int)v));

    private readonly Label _title = PagesUi.Text(30, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(16, KlyTheme.TextMuted);
    private readonly FlexLayout _links = PagesUi.Row();
    private readonly ContentView _body = PagesUi.Slot();
    private readonly VerticalStackLayout _text;
    private readonly Grid _layout = new();
    private readonly Border _box = new();
    private double _width;

    /// <summary>Creates a call to action.</summary>
    public KlyPageCta()
    {
        _title.LineHeight = 1.15;
        _description.Margin = new Thickness(0, 16, 0, 0);
        _text = new VerticalStackLayout { MaximumWidthRequest = 672, Children = { _title, _description, _body } };
        _box.SetRadius(KlyTheme.RadiusOf(3));
        _box.Content = _layout;
        PagesUi.Heading(_title, HeadingLevel);
        MaximumWidthRequest = 1280;
        Content = _box;

        SizeChanged += (_, _) => Relayout(Width);
        ApplyStyle();
        Relayout(0);
    }

    /// <summary>Raised when the user follows one of the <see cref="Links"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>Title of the page cta.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Secondary text of the page cta, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Links drawn as buttons beside (horizontal, from 1024 units) or under the text.</summary>
    public IReadOnlyList<PageLinkItem>? Links { get => (IReadOnlyList<PageLinkItem>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>Extra content under the description.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Look of the band: <c>Solid</c>, <c>Outline</c>, <c>Soft</c> or <c>Subtle</c>.</summary>
    public KlyVariant Variant { get => (KlyVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the band.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Horizontal puts the links beside the text from 1024 units wide; vertical centers everything.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Shows the links before the text in a horizontal band.</summary>
    public bool Reverse { get => (bool)GetValue(ReverseProperty); set => SetValue(ReverseProperty, value); }

    /// <summary>Level of the title heading for a screen reader, 1 to 6.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal Border Box => _box;

    internal Label TitleLabel => _title;

    internal Label DescriptionLabel => _description;

    internal Grid LayoutGrid => _layout;

    internal FlexLayout LinkRow => _links;

    internal View TextColumn => _text;

    internal void Relayout(double width)
    {
        _width = width;
        var centered = Orientation == StackOrientation.Vertical;
        var side = !centered && width >= PagesUi.Lg;

        Padding = PagesUi.Pick(width, new Thickness(16, 48), new Thickness(24, 48), new Thickness(32, 48));
        _box.Padding = width >= PagesUi.Sm ? new Thickness(48, 64) : new Thickness(24, 48);
        _title.FontSize = width >= PagesUi.Sm ? 36 : 30;
        _description.FontSize = width >= PagesUi.Sm ? 18 : 16;

        var align = centered ? TextAlignment.Center : TextAlignment.Start;
        _title.HorizontalTextAlignment = align;
        _description.HorizontalTextAlignment = align;
        _text.HorizontalOptions = centered ? LayoutOptions.Center : LayoutOptions.Fill;
        _links.JustifyContent = centered ? FlexJustify.Center : FlexJustify.Start;
        _links.HorizontalOptions = centered ? LayoutOptions.Center : LayoutOptions.Fill;

        _layout.Children.Clear();
        _layout.ColumnDefinitions = new ColumnDefinitionCollection();
        _layout.RowDefinitions = new RowDefinitionCollection();
        if (side)
        {
            _layout.RowSpacing = 0;
            _layout.ColumnSpacing = 48;
            _layout.ColumnDefinitions.Add(new ColumnDefinition(Reverse ? GridLength.Auto : GridLength.Star));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(Reverse ? GridLength.Star : GridLength.Auto));
            _text.VerticalOptions = LayoutOptions.Center;
            _links.VerticalOptions = LayoutOptions.Center;
            _layout.Add(_text, Reverse ? 1 : 0, 0);
            _layout.Add(_links, Reverse ? 0 : 1, 0);
        }
        else
        {
            _layout.ColumnSpacing = 0;
            _layout.RowSpacing = 32;
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _text.VerticalOptions = LayoutOptions.Start;
            _links.VerticalOptions = LayoutOptions.Start;
            _layout.Add(_text, 0, 0);
            _layout.Add(_links, 0, 1);
        }
    }

    private void ShowLinks() => PagesUi.FillButtons(_links, Links, KlySize.Lg, 12, 12, link => PagesLinks.Follow(this, LinkClicked, link.Href, link));

    private void ApplyStyle()
    {
        var surface = Surface.For(Variant, KlyTheme.Resolve(Color));
        _box.Apply(surface);
        var solid = Variant == KlyVariant.Solid;
        _title.SetThemeColor(Label.TextColorProperty, solid ? surface.Text : KlyTheme.TextHighlighted);
        _description.SetThemeColor(Label.TextColorProperty, solid ? surface.Text : KlyTheme.TextMuted);
    }
}
