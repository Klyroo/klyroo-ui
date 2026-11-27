namespace Klyroo.UI.Maui;

/// <summary>
/// A responsive grid of page items, such as <see cref="KlyPageCard"/> or <see cref="KlyPageFeature"/>: one column on a phone, two from 640 units,
/// <see cref="Columns"/> from 1024. Add the items to <c>Children</c>.
/// </summary>
public class KlyPageGrid : Grid
{
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(int), typeof(KlyPageGrid), 3, propertyChanged: (b, _, _) => ((KlyPageGrid)b).Refresh());

    private readonly PagesColumnFlow _flow;
    private double _width;

    /// <summary>Creates a page grid.</summary>
    public KlyPageGrid()
    {
        _flow = new PagesColumnFlow(this, ColumnsFor, 32);
        SizeChanged += (_, _) => _width = Width;
    }

    /// <summary>Number of columns from 1024 units, 2 to 4. One column on a phone, two from 640 units.</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    internal int ColumnCount => _flow.Columns;

    internal void Relayout(double width)
    {
        _width = width;
        _flow.Apply(width);
    }

    private int ColumnsFor(double width) => width >= PagesUi.Lg ? Math.Clamp(Columns, 2, 4) : width >= PagesUi.Sm ? 2 : 1;

    private void Refresh() => _flow.Refresh(_width);
}
