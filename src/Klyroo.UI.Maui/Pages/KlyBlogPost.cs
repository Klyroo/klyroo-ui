using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A card for one blog post: image, date, title, description, badge and authors. With <see cref="Href"/> the whole card is tappable and raises <see cref="Clicked"/>.
/// From 768 units wide a <see cref="StackOrientation.Horizontal"/> card puts the image beside the text.
/// </summary>
[ContentProperty(nameof(Body))]
public class KlyBlogPost : Border
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyBlogPost), null, propertyChanged: (b, _, v) => ((KlyBlogPost)b).ShowTitle((string?)v));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyBlogPost), 3, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyBlogPost)b)._title, (int)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyBlogPost), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyBlogPost)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Date"/>.</summary>
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTimeOffset?), typeof(KlyBlogPost), null, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ShowMeta());
    /// <summary>Identifies <see cref="DateFormat"/>.</summary>
    public static readonly BindableProperty DateFormatProperty = BindableProperty.Create(nameof(DateFormat), typeof(string), typeof(KlyBlogPost), null, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ShowMeta());
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyBlogPost), null, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ShowMeta());
    /// <summary>Identifies <see cref="Badge"/>.</summary>
    public static readonly BindableProperty BadgeProperty = BindableProperty.Create(nameof(Badge), typeof(string), typeof(KlyBlogPost), null, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ShowMeta());
    /// <summary>Identifies <see cref="BadgeColor"/>.</summary>
    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(nameof(BadgeColor), typeof(KlyColor), typeof(KlyBlogPost), KlyColor.Primary, propertyChanged: (b, _, v) => ((KlyBlogPost)b)._badge.Color = (KlyColor)v);
    /// <summary>Identifies <see cref="Image"/>.</summary>
    public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(ImageSource), typeof(KlyBlogPost), null, propertyChanged: (b, _, v) => ((KlyBlogPost)b).ShowImage((ImageSource?)v));
    /// <summary>Identifies <see cref="ImageAlt"/>.</summary>
    public static readonly BindableProperty ImageAltProperty = BindableProperty.Create(nameof(ImageAlt), typeof(string), typeof(KlyBlogPost), null, propertyChanged: (b, _, v) => ((KlyBlogPost)b).ShowImageAlt((string?)v));
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(KlyBlogPost), null, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ShowHeader());
    /// <summary>Identifies <see cref="Authors"/>.</summary>
    public static readonly BindableProperty AuthorsProperty = BindableProperty.Create(nameof(Authors), typeof(IReadOnlyList<KlyPostAuthor>), typeof(KlyBlogPost), null, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ShowAuthors());
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyBlogPost), null);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyBlogPost), StackOrientation.Vertical, propertyChanged: (b, _, _) => ((KlyBlogPost)b).Relayout(((KlyBlogPost)b)._width));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(BlogPostVariant), typeof(KlyBlogPost), BlogPostVariant.Outline, propertyChanged: (b, _, _) => ((KlyBlogPost)b).ApplyStyle());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyBlogPost), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyBlogPost)b)._content, (View?)v));
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyBlogPost), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyBlogPost)b)._footer, (View?)v));

    private readonly Border _header = new() { StrokeThickness = 0, IsVisible = false };
    private readonly Image _image = new() { Aspect = Aspect.AspectFill };
    private readonly Label _date = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly KlyBadge _badge = new() { Variant = KlyVariant.Subtle, Size = KlySize.Sm, IsVisible = false };
    private readonly FlexLayout _meta = PagesUi.Row();
    private readonly Label _title = PagesUi.Text(18, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly ContentView _content = PagesUi.Slot();
    private readonly ContentView _authors = PagesUi.Slot();
    private readonly ContentView _footer = PagesUi.Slot();
    private readonly VerticalStackLayout _body;
    private readonly Grid _layout = new();
    private double _width;

    /// <summary>Creates a blog post card.</summary>
    public KlyBlogPost()
    {
        this.SetRadius(KlyTheme.RadiusOf(2));
        _header.Content = _image;
        _header.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
        _date.Margin = new Thickness(0, 0, 12, 4);
        _badge.Margin = new Thickness(0, 0, 0, 4);
        _meta.Children.Add(_date);
        _meta.Children.Add(_badge);
        _authors.Padding = new Thickness(0, 8, 0, 0);
        _body = new VerticalStackLayout { Spacing = 8, Children = { _meta, _title, _description, _content, _authors, _footer } };
        PagesUi.Heading(_title, HeadingLevel);
        Content = _layout;

        PagesUi.Tappable(this, Tap);
        SizeChanged += (_, _) => Relayout(Width);
        ApplyStyle();
        Relayout(0);
    }

    /// <summary>Raised when the user taps a card that has an <see cref="Href"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? Clicked;

    /// <summary>Title of the blog post.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Level of the title heading for a screen reader: 3 by default, for a list of posts under a level 2 heading.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Secondary text of the blog post, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Publication date, shown with <see cref="Culture"/> (the current culture by default) and <see cref="DateFormat"/>.</summary>
    public DateTimeOffset? Date { get => (DateTimeOffset?)GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <summary>A .NET date format. Without one, the long date of the culture without the day name.</summary>
    public string? DateFormat { get => (string?)GetValue(DateFormatProperty); set => SetValue(DateFormatProperty, value); }

    /// <summary>The culture of the date. Defaults to the current culture.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>Text of a badge next to the date, such as a category.</summary>
    public string? Badge { get => (string?)GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }

    /// <summary>Semantic color of the badge.</summary>
    public KlyColor BadgeColor { get => (KlyColor)GetValue(BadgeColorProperty); set => SetValue(BadgeColorProperty, value); }

    /// <summary>Cover image. Decorative unless <see cref="ImageAlt"/> says what it shows.</summary>
    public ImageSource? Image { get => (ImageSource?)GetValue(ImageProperty); set => SetValue(ImageProperty, value); }

    /// <summary>What a screen reader says for the image. Leave it empty when the image is decorative.</summary>
    public string? ImageAlt { get => (string?)GetValue(ImageAltProperty); set => SetValue(ImageAltProperty, value); }

    /// <summary>Replaces the cover image.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>One author shows an avatar with the name, several show overlapping avatars named together for a screen reader.</summary>
    public IReadOnlyList<KlyPostAuthor>? Authors { get => (IReadOnlyList<KlyPostAuthor>?)GetValue(AuthorsProperty); set => SetValue(AuthorsProperty, value); }

    /// <summary>Makes the whole card tappable: it raises <see cref="Clicked"/> with this address.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>Whether the image sits above the text (<c>Vertical</c>) or, from 768 units wide, beside it (<c>Horizontal</c>).</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Look of the card: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>Naked</c>.</summary>
    public BlogPostVariant Variant { get => (BlogPostVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Extra content between the description and the authors.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Content at the bottom of the card, under the authors.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    internal Border HeaderBox => _header;

    internal Image CoverImage => _image;

    internal Label TitleLabel => _title;

    internal Label DateLabel => _date;

    internal KlyBadge BadgeView => _badge;

    internal VerticalStackLayout BodyStack => _body;

    internal Grid LayoutGrid => _layout;

    internal ContentView AuthorSlot => _authors;

    internal void Tap()
    {
        if (IsEnabled && Href is not null) PagesLinks.Follow(this, Clicked, Href, null);
    }

    internal void Relayout(double width)
    {
        _width = width;
        var side = Orientation == StackOrientation.Horizontal && width >= PagesUi.Md;
        var naked = Variant == BlogPostVariant.Naked;
        var padding = width >= PagesUi.Sm ? 24d : 16d;

        _layout.Children.Clear();
        _layout.ColumnDefinitions = new ColumnDefinitionCollection();
        _layout.RowDefinitions = new RowDefinitionCollection();
        if (side)
        {
            _layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
            _header.HeightRequest = -1;
            _header.VerticalOptions = LayoutOptions.Fill;
            _body.VerticalOptions = LayoutOptions.Center;
            _body.Padding = naked ? new Thickness(padding, 0, 0, 0) : new Thickness(padding);
            _layout.Add(_header, 0, 0);
            _layout.Add(_body, 1, 0);
        }
        else
        {
            _layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            // aspect-video: the cover is as high as 9/16 of its width.
            _header.HeightRequest = width > 0 ? width * 9 / 16 : -1;
            _header.VerticalOptions = LayoutOptions.Start;
            _body.VerticalOptions = LayoutOptions.Start;
            _body.Padding = naked ? new Thickness(0, padding, 0, 0) : new Thickness(padding);
            _layout.Add(_header, 0, 0);
            _layout.Add(_body, 0, 1);
        }
    }

    private void ShowTitle(string? text)
    {
        PagesUi.Show(_title, text);
        SemanticProperties.SetDescription(this, Href is null ? null : text);
    }

    private void ShowMeta()
    {
        _date.Text = Date is { } date ? PagesPost.Date(date, DateFormat, Culture) : null;
        _date.IsVisible = Date is not null;
        _badge.Text = Badge;
        _badge.IsVisible = !string.IsNullOrEmpty(Badge);
        _meta.IsVisible = _date.IsVisible || _badge.IsVisible;
    }

    private void ShowImage(ImageSource? source)
    {
        _image.Source = source;
        ShowHeader();
    }

    private void ShowImageAlt(string? alt)
    {
        SemanticProperties.SetDescription(_image, alt);
        AutomationProperties.SetIsInAccessibleTree(_image, !string.IsNullOrEmpty(alt));
    }

    private void ShowHeader()
    {
        _header.Content = Header ?? (Image is not null ? _image : null);
        _header.IsVisible = _header.Content is not null;
    }

    private void ShowAuthors()
    {
        var authors = Authors;
        PagesUi.Put(_authors, authors is null or { Count: 0 } ? null : authors.Count == 1 ? PagesPost.Author(authors[0], KlySize.Md) : PagesPost.Group(authors));
    }

    private void ApplyStyle()
    {
        var tint = KlyTheme.BackgroundElevated.WithAlpha(0.5f);
        this.Apply(Variant switch
        {
            BlogPostVariant.Outline => new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text),
            BlogPostVariant.Soft => new Surface(tint, KlyThemeColor.Transparent, 0, KlyTheme.Text),
            BlogPostVariant.Subtle => new Surface(tint, KlyTheme.Border, 1, KlyTheme.Text),
            BlogPostVariant.Ghost or BlogPostVariant.Naked => new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.Text),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        });
        // A naked card has no frame to clip the cover: the cover rounds its own corners.
        _header.SetRadius(Variant == BlogPostVariant.Naked ? KlyTheme.RadiusOf(2) : 0);
        Relayout(_width);
    }
}
