namespace Klyroo.UI.Maui;

/// <summary>
/// A card of a page, with an icon, a title, a description, a header, a body and a footer. With <see cref="Href"/> the whole card is tappable and raises <see cref="Clicked"/>.
/// The <see cref="PageCardVariant.Spotlight"/> glow shows while a pointer is over the card, so it appears with a mouse or a trackpad and never on a touch screen.
/// From 1024 units wide a <see cref="StackOrientation.Horizontal"/> card puts the body beside the text.
/// </summary>
[ContentProperty(nameof(Body))]
public class KlyPageCard : Border
{
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(KlyPageCard), null, propertyChanged: (b, _, v) => ((KlyPageCard)b).ShowIcon((string?)v));
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageCard), null, propertyChanged: (b, _, v) => ((KlyPageCard)b).ShowTitle((string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPageCard), null, propertyChanged: (b, _, v) => ((KlyPageCard)b).ShowDescription((string?)v));
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyPageCard), null);
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(KlyPageCard), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageCard)b)._header, (View?)v));
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageCard), null, propertyChanged: (b, _, v) => ((KlyPageCard)b).ShowBody((View?)v));
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyPageCard), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageCard)b)._footer, (View?)v));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(PageCardVariant), typeof(KlyPageCard), PageCardVariant.Outline, propertyChanged: (b, _, _) => ((KlyPageCard)b).ApplyStyle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPageCard), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyPageCard)b).ApplyStyle());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPageCard), StackOrientation.Vertical, propertyChanged: (b, _, _) => ((KlyPageCard)b).Relayout(((KlyPageCard)b)._width));
    /// <summary>Identifies <see cref="Reverse"/>.</summary>
    public static readonly BindableProperty ReverseProperty = BindableProperty.Create(nameof(Reverse), typeof(bool), typeof(KlyPageCard), false, propertyChanged: (b, _, _) => ((KlyPageCard)b).Relayout(((KlyPageCard)b)._width));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int?), typeof(KlyPageCard), 3, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageCard)b)._title, (int?)v));

    private readonly Border _leading = new() { IsVisible = false, WidthRequest = 40, HeightRequest = 40, StrokeThickness = 0, HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(0, 0, 0, 12) };
    private readonly KlyIcon _icon = new() { Size = KlySize.Md, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly Label _title = PagesUi.Text(16, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly ContentView _header = PagesUi.Slot();
    private readonly ContentView _body = PagesUi.Slot();
    private readonly ContentView _footer = PagesUi.Slot();
    private readonly VerticalStackLayout _wrapper;
    private readonly Grid _container = new();
    private readonly Border _spotlight = new() { Opacity = 0, InputTransparent = true, StrokeThickness = 0 };
    private double _width;

    /// <summary>Creates a page card.</summary>
    public KlyPageCard()
    {
        this.SetRadius(KlyTheme.RadiusOf(2));
        _leading.SetRadius(KlyTheme.RadiusOf(1));
        _leading.Content = _icon;
        AutomationProperties.SetIsInAccessibleTree(_leading, false);
        _description.Margin = new Thickness(0, 4, 0, 0);
        _footer.Padding = new Thickness(16, 0, 16, 16);
        _wrapper = new VerticalStackLayout { IsVisible = false, Children = { _leading, _title, _description } };
        PagesUi.Heading(_title, HeadingLevel);

        var stack = new VerticalStackLayout { Children = { _header, _container, _footer } };
        Content = new Grid { Children = { _spotlight, stack } };

        PagesUi.Tappable(this, Tap);
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => SetSpotlight(true);
        pointer.PointerExited += (_, _) => SetSpotlight(false);
        GestureRecognizers.Add(pointer);

        SizeChanged += (_, _) => Relayout(Width);
        ApplyStyle();
        Relayout(0);
    }

    /// <summary>Raised when the user taps a card that has an <see cref="Href"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? Clicked;

    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>).</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Title of the page card.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Secondary text of the page card, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Makes the whole card tappable: it raises <see cref="Clicked"/> with this address.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>Content above the text, an image for example.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Content beside (horizontal) or under (vertical) the text.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Content at the bottom of the card.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>Look of the card: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c> or <c>Spotlight</c>.</summary>
    public PageCardVariant Variant { get => (PageCardVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color that tints the icon and the spotlight.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Horizontal puts the text and the body side by side from 1024 units wide; vertical stacks them.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Shows the body before the text in a horizontal card.</summary>
    public bool Reverse { get => (bool)GetValue(ReverseProperty); set => SetValue(ReverseProperty, value); }

    /// <summary>Gives the title a heading level for a screen reader, 1 to 6; it is plain text when <c>null</c>. 3 by default.</summary>
    public int? HeadingLevel { get => (int?)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal Border Leading => _leading;

    internal Label TitleLabel => _title;

    internal VerticalStackLayout Wrapper => _wrapper;

    internal Grid Container => _container;

    internal View BodySlot => _body;

    internal double SpotlightOpacity => _spotlight.Opacity;

    internal void Tap()
    {
        if (IsEnabled && Href is not null) PagesLinks.Follow(this, Clicked, Href, null);
    }

    internal void SetSpotlight(bool on) => _spotlight.Opacity = on && Variant == PageCardVariant.Spotlight ? 1 : 0;

    internal void Relayout(double width)
    {
        _width = width;
        var side = Orientation == StackOrientation.Horizontal && width >= PagesUi.Lg;
        var padding = width >= PagesUi.Sm ? 24d : 16d;
        _container.Padding = new Thickness(padding);
        _footer.Padding = new Thickness(padding, 0, padding, padding);

        _container.Children.Clear();
        _container.ColumnDefinitions = new ColumnDefinitionCollection();
        _container.RowDefinitions = new RowDefinitionCollection();
        if (side)
        {
            _container.RowSpacing = 0;
            _container.ColumnSpacing = 32;
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _wrapper.VerticalOptions = LayoutOptions.Center;
            _body.VerticalOptions = LayoutOptions.Center;
            _container.Add(_wrapper, Reverse ? 1 : 0, 0);
            _container.Add(_body, Reverse ? 0 : 1, 0);
        }
        else
        {
            _container.ColumnSpacing = 0;
            _container.RowSpacing = 16;
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _wrapper.VerticalOptions = LayoutOptions.Start;
            _body.VerticalOptions = LayoutOptions.Start;
            _container.Add(_wrapper, 0, 0);
            _container.Add(_body, 0, 1);
        }
    }

    private void ShowIcon(string? name)
    {
        _icon.Name = name ?? "";
        _leading.IsVisible = !string.IsNullOrEmpty(name);
        ShowWrapper();
    }

    private void ShowTitle(string? text)
    {
        PagesUi.Show(_title, text);
        SemanticProperties.SetDescription(this, text);
        ShowWrapper();
    }

    private void ShowDescription(string? text)
    {
        PagesUi.Show(_description, text);
        ShowWrapper();
    }

    private void ShowBody(View? body) => PagesUi.Put(_body, body);

    private void ShowWrapper() => _wrapper.IsVisible = _leading.IsVisible || _title.IsVisible || _description.IsVisible;

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        var tint = KlyTheme.BackgroundElevated.WithAlpha(0.5f);
        this.Apply(Variant switch
        {
            PageCardVariant.Outline => new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text),
            PageCardVariant.Soft => new Surface(tint, KlyThemeColor.Transparent, 0, KlyTheme.Text),
            PageCardVariant.Subtle or PageCardVariant.Spotlight => new Surface(tint, KlyTheme.Border, 1, KlyTheme.Text),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        });
        _leading.SetThemeColor(BackgroundColorProperty, set.Fill.WithAlpha(0.10f));
        _icon.IconColor = set.Text;
        _spotlight.SetAppTheme<Brush>(BackgroundProperty, Glow(set.Fill.Light), Glow(set.Fill.Dark));
        _spotlight.IsVisible = Variant == PageCardVariant.Spotlight;
        if (Variant != PageCardVariant.Spotlight) _spotlight.Opacity = 0;
    }

    // The web glow: a radial gradient from the top edge, the semantic color at 16% fading to nothing.
    private static Brush Glow(Color color) => new RadialGradientBrush
    {
        Center = new Point(0.5, 0),
        Radius = 1,
        GradientStops = [new GradientStop(color.WithAlpha(0.16f), 0), new GradientStop(color.WithAlpha(0), 0.6f)],
    };
}
