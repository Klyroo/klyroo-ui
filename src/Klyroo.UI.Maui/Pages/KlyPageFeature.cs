namespace Klyroo.UI.Maui;

/// <summary>A feature block with an icon, a title and a description. With <see cref="Href"/> the whole block is tappable and raises <see cref="Clicked"/>.</summary>
[ContentProperty(nameof(Body))]
public class KlyPageFeature : ContentView
{
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(KlyPageFeature), null, propertyChanged: (b, _, v) => ((KlyPageFeature)b).ShowIcon((string?)v));
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageFeature), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageFeature)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyPageFeature), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageFeature)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyPageFeature), null);
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageFeature), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageFeature)b)._body, (View?)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPageFeature), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyPageFeature)b).ApplyStyle());
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyPageFeature), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyPageFeature)b).Arrange());
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int?), typeof(KlyPageFeature), null, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageFeature)b)._title, (int?)v));

    private readonly Border _leading = new() { IsVisible = false, WidthRequest = 36, HeightRequest = 36, StrokeThickness = 0, VerticalOptions = LayoutOptions.Start };
    private readonly KlyIcon _icon = new() { Size = KlySize.Md, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly Label _title = PagesUi.Text(16, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly ContentView _body = PagesUi.Slot();
    private readonly Grid _grid = new();
    private readonly VerticalStackLayout _text = new();

    /// <summary>Creates a page feature.</summary>
    public KlyPageFeature()
    {
        _text.Children.Add(_title);
        _text.Children.Add(_description);
        _text.Children.Add(_body);
        _leading.SetRadius(KlyTheme.RadiusOf(1));
        _leading.Content = _icon;
        AutomationProperties.SetIsInAccessibleTree(_leading, false);
        _description.Margin = new Thickness(0, 4, 0, 0);
        Content = _grid;
        PagesUi.Tappable(this, Tap);
        ApplyStyle();
        Arrange();
    }

    /// <summary>Raised when the user taps a feature that has an <see cref="Href"/>.</summary>
    public event EventHandler<PageLinkEventArgs>? Clicked;

    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>).</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Title of the page feature.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Secondary text of the page feature, shown under its title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Makes the whole feature tappable: it raises <see cref="Clicked"/> with this address.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>Extra content under the description.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Semantic color that tints the icon.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Horizontal puts the icon before the text; vertical puts it above.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Gives the title a heading level for a screen reader, 1 to 6; it is plain text when <c>null</c>.</summary>
    public int? HeadingLevel { get => (int?)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal Border Leading => _leading;

    internal Label TitleLabel => _title;

    internal Grid LayoutGrid => _grid;

    internal void Tap()
    {
        if (IsEnabled && Href is not null) PagesLinks.Follow(this, Clicked, Href, null);
    }

    private void ShowIcon(string? name)
    {
        _icon.Name = name ?? "";
        _leading.IsVisible = !string.IsNullOrEmpty(name);
        Arrange();
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        _leading.SetThemeColor(BackgroundColorProperty, set.Fill.WithAlpha(0.10f));
        _icon.IconColor = set.Text;
    }

    private void Arrange()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions = new ColumnDefinitionCollection();
        _grid.RowDefinitions = new RowDefinitionCollection();
        var text = _text;
        if (Orientation == StackOrientation.Horizontal)
        {
            _grid.ColumnSpacing = 12;
            _grid.RowSpacing = 0;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.Add(_leading, 0, 0);
            _grid.Add(text, 1, 0);
        }
        else
        {
            _grid.ColumnSpacing = 0;
            _grid.RowSpacing = 12;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.Add(_leading, 0, 0);
            _grid.Add(text, 0, 1);
        }
    }
}
