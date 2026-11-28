namespace Klyroo.UI.Maui;

/// <summary>
/// Groups of links under a heading, for the <see cref="KlyFooter.Top"/> area. The columns flow two per row below 768 wide and three per row above; from 1280 wide
/// <see cref="Left"/> sits beside them, below it comes first, and <see cref="Right"/> follows.
/// </summary>
public class KlyFooterColumns : Grid
{
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(IReadOnlyList<KlyFooterColumn>), typeof(KlyFooterColumns), null, propertyChanged: (b, _, _) => ((KlyFooterColumns)b).Rebuild());
    /// <summary>Identifies <see cref="Left"/>.</summary>
    public static readonly BindableProperty LeftProperty = BindableProperty.Create(nameof(Left), typeof(View), typeof(KlyFooterColumns), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooterColumns)b)._left, (View?)v));
    /// <summary>Identifies <see cref="Right"/>.</summary>
    public static readonly BindableProperty RightProperty = BindableProperty.Create(nameof(Right), typeof(View), typeof(KlyFooterColumns), null, propertyChanged: (b, _, v) => Place(b, ((KlyFooterColumns)b)._right, (View?)v));
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyFooterColumns), 3, propertyChanged: (b, _, _) => ((KlyFooterColumns)b).Rebuild());

    private readonly ContentView _left = LayoutParts.Slot();
    private readonly ContentView _right = LayoutParts.Slot();
    private readonly Grid _nav = new() { ColumnSpacing = 32, RowSpacing = 32 };
    private int _navColumns = 2;
    private bool _wide;

    /// <summary>Creates footer columns.</summary>
    public KlyFooterColumns()
    {
        RowSpacing = 40;
        _left.VerticalOptions = LayoutOptions.Start;
        _right.VerticalOptions = LayoutOptions.Start;
        this.Add(_left);
        this.Add(_nav);
        this.Add(_right);
        SizeChanged += (_, _) => UpdateWidth(Width);
        Arrange();
    }

    /// <summary>Raised when the user taps a link that is not disabled. The app follows <see cref="KlyFooterLink.Href"/>.</summary>
    public event EventHandler<KlyFooterLink>? LinkTapped;

    /// <summary>The columns, each a heading and a list of links. Assign a new list to change them.</summary>
    public IReadOnlyList<KlyFooterColumn>? Columns { get => (IReadOnlyList<KlyFooterColumn>?)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Content before the columns on wide screens: a logo and a short description.</summary>
    public View? Left { get => (View?)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>Content after the columns: a newsletter form.</summary>
    public View? Right { get => (View?)GetValue(RightProperty); set => SetValue(RightProperty, value); }

    /// <summary>Level of the column headings for a screen reader: 3 by default.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>How many columns a row of links holds now: 2, or 3 from 768 wide.</summary>
    internal int NavColumnCount => _navColumns;

    internal void UpdateWidth(double width)
    {
        var navColumns = width >= LayoutBreakpoints.Md ? 3 : 2;
        var wide = width >= LayoutBreakpoints.Xl;
        if (navColumns == _navColumns && wide == _wide) return;
        _navColumns = navColumns;
        _wide = wide;
        Rebuild();
    }

    private static void Place(BindableObject owner, ContentView slot, View? view)
    {
        LayoutParts.Fill(slot, view);
        ((KlyFooterColumns)owner).Arrange();
    }

    private void Rebuild()
    {
        _nav.Children.Clear();
        _nav.RowDefinitions.Clear();
        _nav.ColumnDefinitions.Clear();
        for (var c = 0; c < _navColumns; c++) _nav.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var columns = Columns ?? [];
        for (var i = 0; i < columns.Count; i++)
        {
            if (i % _navColumns == 0) _nav.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _nav.Add(BuildColumn(columns[i]), i % _navColumns, i / _navColumns);
        }
        Arrange();
    }

    private void Arrange()
    {
        ColumnSpacing = 32;
        if (_wide)
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Place(_left, 0, 0, 1);
            Place(_nav, 0, 1, 2);
            Place(_right, 1, 0, 1);
        }
        else
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Place(_left, 0, 0, 1);
            Place(_nav, 1, 0, 1);
            Place(_right, 2, 0, 1);
        }
    }

    private static void Place(View view, int row, int column, int columnSpan)
    {
        LayoutParts.Row(view, row);
        LayoutParts.Column(view, column);
        LayoutParts.ColumnSpan(view, columnSpan);
    }

    private View BuildColumn(KlyFooterColumn column)
    {
        var heading = new Label { Text = column.Label, FontSize = 14, FontAttributes = FontAttributes.Bold };
        heading.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        SemanticProperties.SetHeadingLevel(heading, LayoutParts.Heading(HeadingLevel));

        // Each link is a 40 unit touch target, which also spaces them (the web has 16 between lines of text).
        var links = new VerticalStackLayout { Spacing = 0, Margin = new Thickness(0, 14, 0, 0) };
        foreach (var link in column.Links) links.Add(BuildLink(link));
        return new VerticalStackLayout { Children = { heading, links } };
    }

    private View BuildLink(KlyFooterLink link)
    {
        var text = new Label { Text = link.Label, FontSize = 14, VerticalOptions = LayoutOptions.Center };
        text.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
        var row = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Start, MinimumHeightRequest = 40 };
        if (!string.IsNullOrEmpty(link.Icon)) row.Add(new KlyIcon { Name = link.Icon, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted });
        row.Add(text);
        row.IsEnabled = !link.Disabled;
        if (!link.Disabled) row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Tap(link)) });
        return row;
    }

    /// <summary>What a tap on a link does.</summary>
    internal void Tap(KlyFooterLink link)
    {
        if (!link.Disabled) LinkTapped?.Invoke(this, link);
    }
}
