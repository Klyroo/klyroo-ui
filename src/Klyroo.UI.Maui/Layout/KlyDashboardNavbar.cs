namespace Klyroo.UI.Maui;

/// <summary>
/// The top bar of a <see cref="KlyDashboardPanel"/>: the sidebar toggle (below 1024 wide), a title, and left, center and right areas, over a hairline.
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlyDashboardNavbar : Grid
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyDashboardNavbar), null, propertyChanged: (b, _, v) => ((KlyDashboardNavbar)b).ShowTitle((string?)v));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyDashboardNavbar), 1, propertyChanged: (b, _, v) => SemanticProperties.SetHeadingLevel(((KlyDashboardNavbar)b)._title, LayoutParts.Heading((int)v)));
    /// <summary>Identifies <see cref="Toggle"/>.</summary>
    public static readonly BindableProperty ToggleProperty = BindableProperty.Create(nameof(Toggle), typeof(bool), typeof(KlyDashboardNavbar), true, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardNavbar)b)._toggleHost, (bool)v ? ((KlyDashboardNavbar)b)._toggle : null));
    /// <summary>Identifies <see cref="Leading"/>.</summary>
    public static readonly BindableProperty LeadingProperty = BindableProperty.Create(nameof(Leading), typeof(View), typeof(KlyDashboardNavbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardNavbar)b)._leading, (View?)v));
    /// <summary>Identifies <see cref="Trailing"/>.</summary>
    public static readonly BindableProperty TrailingProperty = BindableProperty.Create(nameof(Trailing), typeof(View), typeof(KlyDashboardNavbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardNavbar)b)._trailing, (View?)v));
    /// <summary>Identifies <see cref="Left"/>.</summary>
    public static readonly BindableProperty LeftProperty = BindableProperty.Create(nameof(Left), typeof(View), typeof(KlyDashboardNavbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardNavbar)b)._left, (View?)v));
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyDashboardNavbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardNavbar)b)._center, (View?)v));
    /// <summary>Identifies <see cref="Right"/>.</summary>
    public static readonly BindableProperty RightProperty = BindableProperty.Create(nameof(Right), typeof(View), typeof(KlyDashboardNavbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardNavbar)b)._right, (View?)v));

    private readonly Grid _row = new() { HeightRequest = 64, ColumnSpacing = 16, Padding = new Thickness(16, 0) };
    private readonly KlyDashboardSidebarToggle _toggle = new();
    private readonly ContentView _toggleHost = LayoutParts.Slot();
    private readonly ContentView _leading = LayoutParts.Slot();
    private readonly Label _title = new() { FontSize = 16, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly ContentView _trailing = LayoutParts.Slot();
    private readonly ContentView _left = LayoutParts.Slot();
    private readonly ContentView _center = LayoutParts.Slot();
    private readonly ContentView _right = LayoutParts.Slot();

    /// <summary>Creates a navbar.</summary>
    public KlyDashboardNavbar()
    {
        RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
        _title.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level1);

        var start = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center, Children = { _toggleHost, _leading, _title, _trailing, _left } };
        _center.VerticalOptions = LayoutOptions.Center;
        _right.VerticalOptions = LayoutOptions.Center;
        _row.ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        _row.Add(start, 0);
        _row.Add(_center, 1);
        _row.Add(_right, 2);
        this.Add(_row, 0, 0);
        this.Add(LayoutParts.Line(), 0, 1);

        LayoutParts.Fill(_toggleHost, _toggle);
        SizeChanged += (_, _) => UpdateWidth(Width);
    }

    /// <summary>The title, a heading of <see cref="HeadingLevel"/>.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Level of the title heading for a screen reader, 1 to 6. A page has one level 1: give the navbars after the first one a lower level, or no <see cref="Title"/>.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Shows a <see cref="KlyDashboardSidebarToggle"/> at the start, below 1024 wide (default).</summary>
    public bool Toggle { get => (bool)GetValue(ToggleProperty); set => SetValue(ToggleProperty, value); }

    /// <summary>Before the title.</summary>
    public View? Leading { get => (View?)GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }

    /// <summary>After the title.</summary>
    public View? Trailing { get => (View?)GetValue(TrailingProperty); set => SetValue(TrailingProperty, value); }

    /// <summary>Left area, after the title.</summary>
    public View? Left { get => (View?)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>The center area, between the left and the right ones.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>Right area: actions, the user menu.</summary>
    public View? Right { get => (View?)GetValue(RightProperty); set => SetValue(RightProperty, value); }

    internal void UpdateWidth(double width) => _row.Padding = new Thickness(width >= LayoutBreakpoints.Sm ? 24 : 16, 0);

    private void ShowTitle(string? text)
    {
        _title.Text = text;
        _title.IsVisible = !string.IsNullOrEmpty(text);
    }
}
