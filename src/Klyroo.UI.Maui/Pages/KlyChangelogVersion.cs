using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>One entry of a changelog: version, date, description, authors and image, with a dot and a line that join it to the next entry on a timeline.</summary>
[ContentProperty(nameof(Body))]
public class KlyChangelogVersion : ContentView
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyChangelogVersion)b)._title, (string?)v));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyChangelogVersion), 3, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyChangelogVersion)b)._title, (int)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyChangelogVersion)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Date"/>.</summary>
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTimeOffset?), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ShowMeta());
    /// <summary>Identifies <see cref="DateFormat"/>.</summary>
    public static readonly BindableProperty DateFormatProperty = BindableProperty.Create(nameof(DateFormat), typeof(string), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ShowMeta());
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ShowMeta());
    /// <summary>Identifies <see cref="Badge"/>.</summary>
    public static readonly BindableProperty BadgeProperty = BindableProperty.Create(nameof(Badge), typeof(string), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ShowMeta());
    /// <summary>Identifies <see cref="BadgeColor"/>.</summary>
    public static readonly BindableProperty BadgeColorProperty = BindableProperty.Create(nameof(BadgeColor), typeof(KlyColor), typeof(KlyChangelogVersion), KlyColor.Primary, propertyChanged: (b, _, v) => ((KlyChangelogVersion)b)._badge.Color = (KlyColor)v);
    /// <summary>Identifies <see cref="Image"/>.</summary>
    public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(ImageSource), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => ((KlyChangelogVersion)b).ShowImage((ImageSource?)v));
    /// <summary>Identifies <see cref="ImageAlt"/>.</summary>
    public static readonly BindableProperty ImageAltProperty = BindableProperty.Create(nameof(ImageAlt), typeof(string), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => ((KlyChangelogVersion)b).ShowImageAlt((string?)v));
    /// <summary>Identifies <see cref="Authors"/>.</summary>
    public static readonly BindableProperty AuthorsProperty = BindableProperty.Create(nameof(Authors), typeof(IReadOnlyList<KlyPostAuthor>), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ShowAuthors());
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyChangelogVersion), null);
    /// <summary>Identifies <see cref="Indicator"/>.</summary>
    public static readonly BindableProperty IndicatorProperty = BindableProperty.Create(nameof(Indicator), typeof(bool), typeof(KlyChangelogVersion), true, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ApplyTimeline());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyChangelogVersion), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyChangelogVersion)b).ApplyStyle());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyChangelogVersion)b)._content, (View?)v));
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(nameof(Actions), typeof(View), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyChangelogVersion)b)._actions, (View?)v));
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyChangelogVersion), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyChangelogVersion)b)._footer, (View?)v));

    private readonly Label _date = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly KlyBadge _badge = new() { Variant = KlyVariant.Subtle, Size = KlySize.Sm, IsVisible = false };
    private readonly FlexLayout _meta = PagesUi.Row();
    private readonly Label _title = PagesUi.Text(20, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(16, KlyTheme.TextMuted);
    private readonly Image _image = new() { Aspect = Aspect.AspectFit };
    private readonly Border _imageFrame = new() { IsVisible = false, StrokeThickness = 1 };
    private readonly ContentView _content = PagesUi.Slot();
    private readonly FlexLayout _authors = PagesUi.Row();
    private readonly ContentView _actions = PagesUi.Slot();
    private readonly ContentView _footer = PagesUi.Slot();
    private readonly Border _dot = new() { WidthRequest = 16, HeightRequest = 16, StrokeThickness = 2, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 6, 0, 0) };
    private readonly BoxView _line = PagesUi.Line(KlyTheme.Border, vertical: true);
    private readonly VerticalStackLayout _container;
    private readonly Grid _root = new();
    private bool _isLast = true;

    /// <summary>Creates a changelog version.</summary>
    public KlyChangelogVersion()
    {
        _date.Margin = new Thickness(0, 0, 12, 0);
        _meta.Children.Add(_date);
        _meta.Children.Add(_badge);
        _imageFrame.SetRadius(KlyTheme.RadiusOf(1));
        _imageFrame.Content = _image;
        _dot.SetRadius(8);
        _line.HorizontalOptions = LayoutOptions.Start;
        _line.Margin = new Thickness(7, 28, 0, 0);
        _container = new VerticalStackLayout { Spacing = 12, Children = { _meta, _title, _description, _imageFrame, _content, _authors, _actions, _footer } };
        PagesUi.Heading(_title, HeadingLevel);
        PagesUi.Tappable(_title, () =>
        {
            if (IsEnabled && Href is not null) PagesLinks.Follow(this, LinkClicked, Href, null);
        });

        _root.Children.Add(_line);
        _root.Children.Add(_dot);
        _root.Children.Add(_container);
        Content = _root;
        ApplyStyle();
        ApplyTimeline();
    }

    /// <summary>Raised when the user taps the title of a version that has an <see cref="Href"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>Version number or release name.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Level of the title heading for a screen reader: 3 by default, for versions under a level 2 heading.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Text of the version, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Release date, shown with <see cref="Culture"/> (the current culture by default) and <see cref="DateFormat"/>.</summary>
    public DateTimeOffset? Date { get => (DateTimeOffset?)GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <summary>A .NET date format. Without one, the long date of the culture without the day name.</summary>
    public string? DateFormat { get => (string?)GetValue(DateFormatProperty); set => SetValue(DateFormatProperty, value); }

    /// <summary>The culture of the date. Defaults to the current culture.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>Text of a badge next to the date, such as "Latest".</summary>
    public string? Badge { get => (string?)GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }

    /// <summary>Semantic color of the badge.</summary>
    public KlyColor BadgeColor { get => (KlyColor)GetValue(BadgeColorProperty); set => SetValue(BadgeColorProperty, value); }

    /// <summary>An image shown under the description.</summary>
    public ImageSource? Image { get => (ImageSource?)GetValue(ImageProperty); set => SetValue(ImageProperty, value); }

    /// <summary>What a screen reader says for the image. Leave it empty when the image is decorative.</summary>
    public string? ImageAlt { get => (string?)GetValue(ImageAltProperty); set => SetValue(ImageAltProperty, value); }

    /// <summary>The people behind the version, each an avatar with a name.</summary>
    public IReadOnlyList<KlyPostAuthor>? Authors { get => (IReadOnlyList<KlyPostAuthor>?)GetValue(AuthorsProperty); set => SetValue(AuthorsProperty, value); }

    /// <summary>Makes the title tappable: it raises <see cref="LinkClicked"/> with this address. Unlike a blog post the card is not tappable as a whole, so the rich content keeps its own links.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>Shows the dot and the line that join the versions of a <see cref="KlyChangelogVersions"/>.</summary>
    public bool Indicator { get => (bool)GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }

    /// <summary>Semantic color of the dot.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Rich description: paragraphs, lists, links.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Buttons or links shown under the content of the version.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Content shown at the bottom of the version.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>Whether this is the last entry of its timeline: it has no line down to a next entry and no space after it. A <see cref="KlyChangelogVersions"/> sets it.</summary>
    internal bool IsLast
    {
        get => _isLast;
        set
        {
            _isLast = value;
            ApplyTimeline();
        }
    }

    internal Border Dot => _dot;

    internal BoxView Line => _line;

    internal Label TitleLabel => _title;

    internal Label DateLabel => _date;

    internal KlyBadge BadgeView => _badge;

    internal FlexLayout AuthorRow => _authors;

    internal VerticalStackLayout Container => _container;

    internal Border ImageFrame => _imageFrame;

    internal Image Picture => _image;

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
        _imageFrame.IsVisible = source is not null;
    }

    private void ShowImageAlt(string? alt)
    {
        SemanticProperties.SetDescription(_image, alt);
        AutomationProperties.SetIsInAccessibleTree(_image, !string.IsNullOrEmpty(alt));
    }

    private void ShowAuthors()
    {
        _authors.Children.Clear();
        _authors.IsVisible = Authors is { Count: > 0 };
        if (Authors is null) return;
        foreach (var author in Authors)
        {
            var view = PagesPost.Author(author, KlySize.Sm);
            view.Margin = new Thickness(0, 0, 16, 0);
            _authors.Children.Add(view);
        }
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        _dot.SetStroke(set.Text);
        _dot.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _imageFrame.SetStroke(KlyTheme.Border);
    }

    // The dot and the line sit in a 32 unit gutter; the entry leaves 48 units before the next one, and none when it is the last.
    private void ApplyTimeline()
    {
        _dot.IsVisible = Indicator;
        _line.IsVisible = Indicator && !IsLast;
        _container.Margin = new Thickness(Indicator ? 32 : 0, 0, 0, Indicator && !IsLast ? 48 : 0);
    }
}
