namespace Klyroo.UI.Maui;

/// <summary>
/// A section of a landing page: a headline, a title, a description, links, a list of features and a media area.
/// From 1024 units a <see cref="StackOrientation.Horizontal"/> section puts the media beside the text; below, the media sits under it.
/// </summary>
[ContentProperty(nameof(Body))]
public class KlyPageSection : ContentView
{
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(KlyPageSection), null, propertyChanged: (b, _, v) => ((KlyPageSection)b).ShowIcon((string?)v));
    /// <summary>Identifies <see cref="Headline"/>.</summary>
    public static readonly BindableProperty HeadlineProperty = BindableProperty.Create(nameof(Headline), typeof(string), typeof(KlyPageSection), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageSection)b)._headline, (string?)v));
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageSection), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageSection)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPageSection), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageSection)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<PageLinkItem>), typeof(KlyPageSection), null, propertyChanged: (b, _, _) => ((KlyPageSection)b).ShowLinks());
    /// <summary>Identifies <see cref="Features"/>.</summary>
    public static readonly BindableProperty FeaturesProperty = BindableProperty.Create(nameof(Features), typeof(IReadOnlyList<PageFeatureItem>), typeof(KlyPageSection), null, propertyChanged: (b, _, _) => ((KlyPageSection)b).ShowFeatures());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageSection), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageSection)b)._body, (View?)v));
    /// <summary>Identifies <see cref="Media"/>.</summary>
    public static readonly BindableProperty MediaProperty = BindableProperty.Create(nameof(Media), typeof(View), typeof(KlyPageSection), null, propertyChanged: (b, _, v) => ((KlyPageSection)b).ShowMedia((View?)v));
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPageSection), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyPageSection)b).Relayout(((KlyPageSection)b)._width));
    /// <summary>Identifies <see cref="Reverse"/>.</summary>
    public static readonly BindableProperty ReverseProperty = BindableProperty.Create(nameof(Reverse), typeof(bool), typeof(KlyPageSection), false, propertyChanged: (b, _, _) => ((KlyPageSection)b).Relayout(((KlyPageSection)b)._width));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyPageSection), 2, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageSection)b)._title, (int)v));
    /// <summary>Identifies <see cref="FeatureHeadingLevel"/>.</summary>
    public static readonly BindableProperty FeatureHeadingLevelProperty = BindableProperty.Create(nameof(FeatureHeadingLevel), typeof(int?), typeof(KlyPageSection), null, propertyChanged: (b, _, _) => ((KlyPageSection)b).ShowFeatures());

    private readonly Border _leading = new() { IsVisible = false, WidthRequest = 40, HeightRequest = 40, HorizontalOptions = LayoutOptions.Start, StrokeThickness = 0, Margin = new Thickness(0, 0, 0, 24) };
    private readonly KlyIcon _leadingIcon = new() { Size = KlySize.Xl, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly Label _headline = PagesUi.Eyebrow();
    private readonly Label _title = PagesUi.Text(30, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(16, KlyTheme.TextMuted);
    private readonly FlexLayout _links = PagesUi.Row();
    private readonly Grid _features = new() { IsVisible = false, Margin = new Thickness(0, 40, 0, 0) };
    private readonly ContentView _body = PagesUi.Slot();
    private readonly ContentView _media = PagesUi.Slot();
    private readonly VerticalStackLayout _text;
    private readonly Grid _container = new();
    private readonly PagesColumnFlow _featureFlow;
    private double _width;

    /// <summary>Creates a page section.</summary>
    public KlyPageSection()
    {
        _leading.SetRadius(KlyTheme.RadiusOf(1));
        _leading.SetThemeColor(BackgroundColorProperty, KlyTheme.Resolve(KlyColor.Primary).Fill.WithAlpha(0.10f));
        _leadingIcon.IconColor = KlyTheme.Resolve(KlyColor.Primary).Text;
        _leading.Content = _leadingIcon;
        AutomationProperties.SetIsInAccessibleTree(_leading, false);

        _headline.Margin = new Thickness(0, 0, 0, 12);
        _title.LineHeight = 1.15;
        _description.Margin = new Thickness(0, 16, 0, 0);
        _description.MaximumWidthRequest = 672;
        _description.HorizontalOptions = LayoutOptions.Start;
        _links.Margin = new Thickness(0, 32, 0, 0);
        _featureFlow = new PagesColumnFlow(_features, FeatureColumns, 32);
        PagesUi.Heading(_title, HeadingLevel);

        _text = new VerticalStackLayout { Children = { _leading, _headline, _title, _description, _links, _features, _body } };
        MaximumWidthRequest = 1280;
        Content = _container;

        SizeChanged += (_, _) => Relayout(Width);
        Relayout(0);
    }

    /// <summary>Raised when the user follows one of the <see cref="Links"/>, or taps a feature that has an address.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>Name of a built-in icon shown above the headline.</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Short text above the title, such as a category.</summary>
    public string? Headline { get => (string?)GetValue(HeadlineProperty); set => SetValue(HeadlineProperty, value); }

    /// <summary>Title of the page section.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Secondary text of the page section, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Links drawn as buttons under the description.</summary>
    public IReadOnlyList<PageLinkItem>? Links { get => (IReadOnlyList<PageLinkItem>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>Features listed under the links, each with an icon, a title and a description.</summary>
    public IReadOnlyList<PageFeatureItem>? Features { get => (IReadOnlyList<PageFeatureItem>?)GetValue(FeaturesProperty); set => SetValue(FeaturesProperty, value); }

    /// <summary>Extra content after the features.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>An illustration or a screenshot shown beside (horizontal, from 1024 units) or under the text.</summary>
    public View? Media { get => (View?)GetValue(MediaProperty); set => SetValue(MediaProperty, value); }

    /// <summary>Horizontal puts the text and the media side by side on a wide screen; vertical stacks them and spreads the features on three columns.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Shows the media before the text in a horizontal section.</summary>
    public bool Reverse { get => (bool)GetValue(ReverseProperty); set => SetValue(ReverseProperty, value); }

    /// <summary>Level of the title heading for a screen reader, 1 to 6.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Gives the feature titles a heading level for a screen reader; they are plain text by default.</summary>
    public int? FeatureHeadingLevel { get => (int?)GetValue(FeatureHeadingLevelProperty); set => SetValue(FeatureHeadingLevelProperty, value); }

    internal Label TitleLabel => _title;

    internal Border LeadingBox => _leading;

    internal Grid Container => _container;

    internal Grid FeatureGrid => _features;

    internal int FeatureColumnCount => _featureFlow.Columns;

    internal FlexLayout LinkRow => _links;

    internal View TextColumn => _text;

    internal View MediaSlot => _media;

    internal void Relayout(double width)
    {
        _width = width;
        var side = Orientation == StackOrientation.Horizontal && width >= PagesUi.Lg && Media is not null;

        _container.Padding = PagesUi.Pick(width, new Thickness(16, 64), new Thickness(24, 96), new Thickness(32, 128));
        _title.FontSize = width >= PagesUi.Sm ? 36 : 30;
        _description.FontSize = width >= PagesUi.Sm ? 18 : 16;
        _featureFlow.Refresh(width);

        _container.Children.Clear();
        _container.ColumnDefinitions = new ColumnDefinitionCollection();
        _container.RowDefinitions = new RowDefinitionCollection();
        if (side)
        {
            _container.RowSpacing = 0;
            _container.ColumnSpacing = 64;
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _media.VerticalOptions = LayoutOptions.Center;
            _text.VerticalOptions = LayoutOptions.Center;
            _container.Add(_text, Reverse ? 1 : 0, 0);
            _container.Add(_media, Reverse ? 0 : 1, 0);
        }
        else
        {
            _container.ColumnSpacing = 0;
            _container.RowSpacing = width >= PagesUi.Sm ? 64 : 32;
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _media.VerticalOptions = LayoutOptions.Start;
            _text.VerticalOptions = LayoutOptions.Start;
            _container.Add(_text, 0, 0);
            _container.Add(_media, 0, 1);
        }
    }

    // One column on a phone, two from sm, then one beside the media (horizontal) or three (vertical) from lg.
    private int FeatureColumns(double width) => width >= PagesUi.Lg ? (Orientation == StackOrientation.Vertical ? 3 : 1) : width >= PagesUi.Sm ? 2 : 1;

    private void ShowIcon(string? name)
    {
        _leadingIcon.Name = name ?? "";
        _leading.IsVisible = !string.IsNullOrEmpty(name);
    }

    private void ShowLinks() => PagesUi.FillButtons(_links, Links, KlySize.Lg, 16, 12, link => PagesLinks.Follow(this, LinkClicked, link.Href, link));

    private void ShowMedia(View? media)
    {
        PagesUi.Put(_media, media);
        Relayout(_width);
    }

    private void ShowFeatures()
    {
        _features.Children.Clear();
        _features.IsVisible = Features is { Count: > 0 };
        if (Features is null) return;
        foreach (var item in Features)
        {
            var feature = new KlyPageFeature { Icon = item.Icon, Title = item.Title, Description = item.Description, Href = item.Href, HeadingLevel = FeatureHeadingLevel };
            feature.Clicked += (_, e) =>
            {
                // The section reports the tap with its own item and opens the address itself: the feature must not open it again.
                PagesLinks.Follow(this, LinkClicked, e.Href, item);
                e.Handled = true;
            };
            _features.Children.Add(feature);
        }
    }
}
