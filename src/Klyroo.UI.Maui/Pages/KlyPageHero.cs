using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// The hero section of a landing page: a headline, a title, a description, links and an optional media area.
/// From 1024 units a <see cref="StackOrientation.Horizontal"/> hero puts the media beside the text; below, the media sits under it.
/// </summary>
[ContentProperty(nameof(Body))]
public class KlyPageHero : ContentView
{
    /// <summary>Identifies <see cref="Headline"/>.</summary>
    public static readonly BindableProperty HeadlineProperty = BindableProperty.Create(nameof(Headline), typeof(string), typeof(KlyPageHero), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageHero)b)._headline, (string?)v));
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageHero), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageHero)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPageHero), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageHero)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<PageLinkItem>), typeof(KlyPageHero), null, propertyChanged: (b, _, _) => ((KlyPageHero)b).ShowLinks());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageHero), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageHero)b)._body, (View?)v));
    /// <summary>Identifies <see cref="Media"/>.</summary>
    public static readonly BindableProperty MediaProperty = BindableProperty.Create(nameof(Media), typeof(View), typeof(KlyPageHero), null, propertyChanged: (b, _, v) => ((KlyPageHero)b).ShowMedia((View?)v));
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPageHero), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyPageHero)b).Relayout(((KlyPageHero)b)._width));
    /// <summary>Identifies <see cref="Reverse"/>.</summary>
    public static readonly BindableProperty ReverseProperty = BindableProperty.Create(nameof(Reverse), typeof(bool), typeof(KlyPageHero), false, propertyChanged: (b, _, _) => ((KlyPageHero)b).Relayout(((KlyPageHero)b)._width));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyPageHero), 1, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageHero)b)._title, (int)v));

    private readonly Label _headline = PagesUi.Eyebrow();
    private readonly Label _title = PagesUi.Text(48, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(18, KlyTheme.TextMuted);
    private readonly FlexLayout _links = PagesUi.Row();
    private readonly ContentView _body = PagesUi.Slot();
    private readonly ContentView _media = PagesUi.Slot();
    private readonly VerticalStackLayout _text;
    private readonly Grid _container = new();
    private double _width;

    /// <summary>Creates a page hero.</summary>
    public KlyPageHero()
    {
        _headline.Margin = new Thickness(0, 0, 0, 20);
        _title.LineHeight = 1;
        _description.Margin = new Thickness(0, 24, 0, 0);
        _links.Margin = new Thickness(0, 32, 0, 0);
        _text = new VerticalStackLayout { Children = { _headline, _title, _description, _links, _body } };
        PagesUi.Heading(_title, HeadingLevel);
        MaximumWidthRequest = 1280;
        Content = _container;

        SizeChanged += (_, _) => Relayout(Width);
        Relayout(0);
    }

    /// <summary>Raised when the user follows one of the <see cref="Links"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>Short text above the title, such as an announcement or a category.</summary>
    public string? Headline { get => (string?)GetValue(HeadlineProperty); set => SetValue(HeadlineProperty, value); }

    /// <summary>Title of the page hero.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Secondary text of the page hero, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Links drawn as buttons under the description.</summary>
    public IReadOnlyList<PageLinkItem>? Links { get => (IReadOnlyList<PageLinkItem>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>Extra content under the links.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>An illustration, a screenshot or a demo shown beside (horizontal, from 1024 units) or under the text.</summary>
    public View? Media { get => (View?)GetValue(MediaProperty); set => SetValue(MediaProperty, value); }

    /// <summary>Horizontal puts the text and the media side by side on a wide screen; vertical centers the text above the media.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Shows the media before the text in a horizontal hero.</summary>
    public bool Reverse { get => (bool)GetValue(ReverseProperty); set => SetValue(ReverseProperty, value); }

    /// <summary>Level of the title heading for a screen reader, 1 to 6: 1 by default, the main heading of the page.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal Label TitleLabel => _title;

    internal Label DescriptionLabel => _description;

    internal FlexLayout LinkRow => _links;

    internal Grid Container => _container;

    internal View TextColumn => _text;

    internal View MediaSlot => _media;

    internal void Relayout(double width)
    {
        _width = width;
        var centered = Orientation == StackOrientation.Vertical;
        var side = Orientation == StackOrientation.Horizontal && width >= PagesUi.Lg && Media is not null;

        _container.Padding = PagesUi.Pick(width, new Thickness(16, 80), new Thickness(24, 96), new Thickness(32, 128));
        _title.FontSize = PagesUi.Pick(width, 48d, 60d, 72d);
        _description.FontSize = width >= PagesUi.Sm ? 20 : 18;

        var align = centered ? TextAlignment.Center : TextAlignment.Start;
        _headline.HorizontalTextAlignment = align;
        _title.HorizontalTextAlignment = align;
        _description.HorizontalTextAlignment = align;
        _description.MaximumWidthRequest = centered ? 672 : -1;
        _description.HorizontalOptions = centered ? LayoutOptions.Center : LayoutOptions.Fill;
        _links.JustifyContent = centered ? FlexJustify.Center : FlexJustify.Start;

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
            _container.RowSpacing = width >= PagesUi.Sm ? 96 : 64;
            _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _media.VerticalOptions = LayoutOptions.Start;
            _text.VerticalOptions = LayoutOptions.Start;
            _container.Add(_text, 0, 0);
            _container.Add(_media, 0, 1);
        }
    }

    private void ShowLinks() => PagesUi.FillButtons(_links, Links, KlySize.Xl, 16, 12, link => PagesLinks.Follow(this, LinkClicked, link.Href, link));

    private void ShowMedia(View? media)
    {
        PagesUi.Put(_media, media);
        Relayout(_width);
    }
}
