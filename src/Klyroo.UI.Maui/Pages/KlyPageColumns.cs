namespace Klyroo.UI.Maui;

/// <summary>
/// A multi-column layout where items of different heights pack without gaps: one column on a phone, two from 640 units, <see cref="Columns"/> from 1024.
/// The items fill the first column from top to bottom, then the next one, in reading order; each item is kept whole.
/// </summary>
[ContentProperty(nameof(Items))]
public class KlyPageColumns : ContentView
{
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(int), typeof(KlyPageColumns), 3, propertyChanged: (b, _, _) => ((KlyPageColumns)b).Relayout(((KlyPageColumns)b)._width));

    private readonly System.Collections.ObjectModel.ObservableCollection<View> _items = [];
    private readonly Grid _grid = new() { ColumnSpacing = 32 };
    private double _width;

    /// <summary>Creates the columns.</summary>
    public KlyPageColumns()
    {
        Content = _grid;
        _items.CollectionChanged += (_, _) => Relayout(_width);
        SizeChanged += (_, _) => Relayout(Width);
        Relayout(0);
    }

    /// <summary>The items. Each one is kept whole: it is never split across two columns.</summary>
    public IList<View> Items => _items;

    /// <summary>Number of columns from 1024 units, 2 to 4. One column on a phone, two from 640 units.</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    internal int ColumnCount => _grid.ColumnDefinitions.Count;

    internal IReadOnlyList<VerticalStackLayout> Stacks => _grid.Children.OfType<VerticalStackLayout>().ToList();

    internal void Relayout(double width)
    {
        _width = width;
        var columns = width >= PagesUi.Lg ? Math.Clamp(Columns, 2, 4) : width >= PagesUi.Sm ? 2 : 1;
        foreach (var stack in _grid.Children.OfType<VerticalStackLayout>()) stack.Children.Clear();
        _grid.Children.Clear();
        _grid.ColumnDefinitions = new ColumnDefinitionCollection();
        var perColumn = Math.Max(1, (_items.Count + columns - 1) / columns);
        for (var i = 0; i < columns; i++)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var stack = new VerticalStackLayout { Spacing = 32, VerticalOptions = LayoutOptions.Start };
            foreach (var item in _items.Skip(i * perColumn).Take(perColumn)) stack.Children.Add(item);
            _grid.Add(stack, i, 0);
        }
    }
}
