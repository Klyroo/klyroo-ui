namespace Klyroo.UI.Maui;

/// <summary>A secondary bar under a <see cref="KlyDashboardNavbar"/>: filters and bulk actions at the start, view options at the end, over a hairline.</summary>
[ContentProperty(nameof(ChildContent))]
public class KlyDashboardToolbar : Grid
{
    /// <summary>Identifies <see cref="Left"/>.</summary>
    public static readonly BindableProperty LeftProperty = BindableProperty.Create(nameof(Left), typeof(View), typeof(KlyDashboardToolbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardToolbar)b)._left, (View?)v));
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyDashboardToolbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardToolbar)b)._center, (View?)v));
    /// <summary>Identifies <see cref="Right"/>.</summary>
    public static readonly BindableProperty RightProperty = BindableProperty.Create(nameof(Right), typeof(View), typeof(KlyDashboardToolbar), null, propertyChanged: (b, _, v) => LayoutParts.Fill(((KlyDashboardToolbar)b)._right, (View?)v));

    private readonly Grid _row = new() { HeightRequest = 48, ColumnSpacing = 16, Padding = new Thickness(16, 0) };
    private readonly ContentView _left = LayoutParts.Slot();
    private readonly ContentView _center = LayoutParts.Slot();
    private readonly ContentView _right = LayoutParts.Slot();

    /// <summary>Creates a toolbar.</summary>
    public KlyDashboardToolbar()
    {
        RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
        _left.VerticalOptions = LayoutOptions.Center;
        _center.VerticalOptions = LayoutOptions.Center;
        _right.VerticalOptions = LayoutOptions.Center;
        _row.ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)];
        _row.Add(_left, 0);
        _row.Add(_center, 1);
        _row.Add(_right, 2);
        this.Add(_row, 0, 0);
        this.Add(LayoutParts.Line(), 0, 1);
        SizeChanged += (_, _) => UpdateWidth(Width);
    }

    /// <summary>Filters and bulk actions, at the start of the toolbar.</summary>
    public View? Left { get => (View?)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>The center area, between the left and the right ones.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>View options, at the end of the toolbar.</summary>
    public View? Right { get => (View?)GetValue(RightProperty); set => SetValue(RightProperty, value); }

    internal void UpdateWidth(double width) => _row.Padding = new Thickness(width >= LayoutBreakpoints.Sm ? 24 : 16, 0);
}
