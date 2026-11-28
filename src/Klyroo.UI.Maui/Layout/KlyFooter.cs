namespace Klyroo.UI.Maui;

/// <summary>
/// The footer of a site or an app: optional <see cref="Top"/> and <see cref="Bottom"/> areas, and <see cref="Left"/>, <see cref="ChildContent"/> and <see cref="Right"/> between them.
/// From 1024 wide the three line up in a row; below they stack, centered.
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlyFooter : Grid
{
    /// <summary>Identifies <see cref="Left"/>.</summary>
    public static readonly BindableProperty LeftProperty = BindableProperty.Create(nameof(Left), typeof(View), typeof(KlyFooter), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooter)b)._left, (View?)v));
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyFooter), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooter)b)._center, (View?)v));
    /// <summary>Identifies <see cref="Right"/>.</summary>
    public static readonly BindableProperty RightProperty = BindableProperty.Create(nameof(Right), typeof(View), typeof(KlyFooter), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooter)b)._right, (View?)v));
    /// <summary>Identifies <see cref="Top"/>.</summary>
    public static readonly BindableProperty TopProperty = BindableProperty.Create(nameof(Top), typeof(View), typeof(KlyFooter), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooter)b)._top, (View?)v));
    /// <summary>Identifies <see cref="Bottom"/>.</summary>
    public static readonly BindableProperty BottomProperty = BindableProperty.Create(nameof(Bottom), typeof(View), typeof(KlyFooter), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooter)b)._bottom, (View?)v));

    private readonly ContentView _left = LayoutParts.Slot();
    private readonly ContentView _center = LayoutParts.Slot();
    private readonly ContentView _right = LayoutParts.Slot();
    private readonly ContentView _top = LayoutParts.Slot();
    private readonly ContentView _bottom = LayoutParts.Slot();
    private readonly Grid _row = new() { RowSpacing = 12 };
    private readonly Grid _topArea = new() { IsVisible = false };
    private readonly KlyContainer _topContainer = new() { Padding = new Thickness(0, 32) };
    private readonly KlyContainer _rowContainer = new() { Padding = new Thickness(0, 32) };
    private readonly Grid _bottomArea = new() { IsVisible = false };
    private bool _compact = true;

    /// <summary>Creates a footer.</summary>
    public KlyFooter()
    {
        RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);

        _topContainer.Add(_top);
        _topArea.Add(_topContainer);
        _topArea.Add(LayoutParts.Line());

        _row.Add(_left);
        _row.Add(_center);
        _row.Add(_right);
        _rowContainer.Add(_row);

        var bottomContainer = new KlyContainer { Padding = new Thickness(0, 32) };
        bottomContainer.Add(_bottom);
        _bottomArea.Add(bottomContainer);
        var topLine = LayoutParts.Line();
        topLine.VerticalOptions = LayoutOptions.Start;
        _bottomArea.Add(topLine);

        // The hairline above the footer, then the areas.
        var edge = LayoutParts.Line();
        edge.VerticalOptions = LayoutOptions.Start;
        this.Add(edge, 0, 0);
        this.Add(_topArea, 0, 1);
        this.Add(_rowContainer, 0, 2);
        this.Add(_bottomArea, 0, 3);

        SizeChanged += (_, _) => UpdateWidth(Width);
        ApplyMode();
    }

    /// <summary>Start side: a copyright line, a logo.</summary>
    public View? Left { get => (View?)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>The middle of the footer: links, a newsletter form.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>End side: social links, a language select.</summary>
    public View? Right { get => (View?)GetValue(RightProperty); set => SetValue(RightProperty, value); }

    /// <summary>A full-width area above, typically a <see cref="KlyFooterColumns"/>.</summary>
    public View? Top { get => (View?)GetValue(TopProperty); set => SetValue(TopProperty, value); }

    /// <summary>A full-width area below.</summary>
    public View? Bottom { get => (View?)GetValue(BottomProperty); set => SetValue(BottomProperty, value); }

    /// <summary>True below 1024 wide, where <see cref="Left"/>, <see cref="ChildContent"/> and <see cref="Right"/> stack instead of lining up. The footer starts compact, as the mobile first styles of the web.</summary>
    public bool IsCompact => _compact;

    internal void UpdateWidth(double width)
    {
        var compact = !LayoutBreakpoints.IsWide(width);
        if (compact == _compact) return;
        _compact = compact;
        ApplyMode();
    }

    private static void Place(BindableObject owner, ContentView slot, View? view)
    {
        LayoutParts.Fill(slot, view);
        ((KlyFooter)owner).ApplyMode();
    }

    private void ApplyMode()
    {
        _topArea.IsVisible = Top is not null;
        _bottomArea.IsVisible = Bottom is not null;
        _rowContainer.IsVisible = Left is not null || ChildContent is not null || Right is not null;
        var vertical = _compact ? 32d : 48d;
        _topContainer.Padding = new Thickness(0, vertical);

        if (_compact)
        {
            _row.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            _row.RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Arrange(_left, 0, 0, LayoutOptions.Center);
            Arrange(_center, 1, 0, LayoutOptions.Center);
            Arrange(_right, 2, 0, LayoutOptions.Center);
        }
        else
        {
            _row.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)];
            _row.RowDefinitions = [new RowDefinition(GridLength.Auto)];
            Arrange(_left, 0, 0, LayoutOptions.Start);
            Arrange(_center, 0, 1, LayoutOptions.Center);
            Arrange(_right, 0, 2, LayoutOptions.End);
        }
    }

    private static void Arrange(View slot, int row, int column, LayoutOptions alignment)
    {
        LayoutParts.Row(slot, row);
        LayoutParts.Column(slot, column);
        slot.HorizontalOptions = alignment;
        slot.VerticalOptions = LayoutOptions.Center;
    }
}
