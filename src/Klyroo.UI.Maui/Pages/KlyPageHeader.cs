namespace Klyroo.UI.Maui;

/// <summary>The header of a page: a headline, a title, a description and links, with a line under it.</summary>
[ContentProperty(nameof(Body))]
public class KlyPageHeader : ContentView
{
    /// <summary>Identifies <see cref="Headline"/>.</summary>
    public static readonly BindableProperty HeadlineProperty = BindableProperty.Create(nameof(Headline), typeof(string), typeof(KlyPageHeader), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageHeader)b)._headline, (string?)v));
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageHeader), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageHeader)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPageHeader), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageHeader)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Links"/>.</summary>
    public static readonly BindableProperty LinksProperty = BindableProperty.Create(nameof(Links), typeof(IReadOnlyList<PageLinkItem>), typeof(KlyPageHeader), null, propertyChanged: (b, _, _) => ((KlyPageHeader)b).ShowLinks());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageHeader), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageHeader)b)._body, (View?)v));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyPageHeader), 1, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageHeader)b)._title, (int)v));

    private readonly Label _headline = PagesUi.Eyebrow();
    private readonly Label _title = PagesUi.Text(30, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(16, KlyTheme.TextMuted);
    private readonly FlexLayout _links = PagesUi.Row();
    private readonly ContentView _body = PagesUi.Slot();

    /// <summary>Creates a page header.</summary>
    public KlyPageHeader()
    {
        _headline.Margin = new Thickness(0, 0, 0, 10);
        _title.LineHeight = 1.15;
        _description.Margin = new Thickness(0, 12, 0, 0);
        _description.MaximumWidthRequest = 672;
        _description.HorizontalOptions = LayoutOptions.Start;
        _links.Margin = new Thickness(0, 16, 0, 0);
        _body.Margin = new Thickness(0, 24, 0, 0);
        PagesUi.Heading(_title, HeadingLevel);

        var root = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(1) } };
        root.Add(new VerticalStackLayout { Padding = new Thickness(0, 32), Children = { _headline, _title, _description, _links, _body } }, 0, 0);
        root.Add(PagesUi.Line(KlyTheme.Border), 0, 1);
        Content = root;

        SizeChanged += (_, _) => Relayout(Width);
        Relayout(0);
    }

    /// <summary>Raised when the user follows one of the <see cref="Links"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? LinkClicked;

    /// <summary>Small label above the title.</summary>
    public string? Headline { get => (string?)GetValue(HeadlineProperty); set => SetValue(HeadlineProperty, value); }

    /// <summary>Title of the page header.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Secondary text of the page header, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Links drawn as buttons under the description.</summary>
    public IReadOnlyList<PageLinkItem>? Links { get => (IReadOnlyList<PageLinkItem>?)GetValue(LinksProperty); set => SetValue(LinksProperty, value); }

    /// <summary>Extra content under the links.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Level of the title heading for a screen reader, 1 to 6: 1 by default, the main heading of the page. Set 2 on the secondary one when a header and a <see cref="KlyPageHero"/> share a page.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal Label TitleLabel => _title;

    internal FlexLayout LinkRow => _links;

    internal void Relayout(double width) => _title.FontSize = width >= PagesUi.Sm ? 36 : 30;

    private void ShowLinks() => PagesUi.FillButtons(_links, Links, KlySize.Md, 6, 6, link => PagesLinks.Follow(this, LinkClicked, link.Href, link));
}
