namespace Klyroo.UI.Maui;

/// <summary>
/// Places the children of a <see cref="Grid"/> on a number of columns that follows its width, row by row: the native version of a responsive CSS grid.
/// The grid reports its width through <c>SizeChanged</c>; <see cref="Apply"/> is the same step for code that has no layout pass.
/// </summary>
internal sealed class PagesColumnFlow
{
    private readonly Grid _grid;
    private readonly Func<double, int> _columnsFor;

    public PagesColumnFlow(Grid grid, Func<double, int> columnsFor, double gap)
    {
        _grid = grid;
        _columnsFor = columnsFor;
        grid.RowSpacing = gap;
        grid.ColumnSpacing = gap;
        grid.SizeChanged += (_, _) => Apply(grid.Width);
        grid.ChildAdded += (_, _) => Place();
        grid.ChildRemoved += (_, _) => Place();
        Apply(0);
    }

    /// <summary>Number of columns now in use.</summary>
    public int Columns { get; private set; }

    /// <summary>Picks the number of columns for a width and places the children when it changed.</summary>
    public void Apply(double width)
    {
        var columns = Math.Max(1, _columnsFor(width));
        if (columns == Columns) return;
        Columns = columns;
        Place();
    }

    /// <summary>Places the children again after a change of the column rule itself.</summary>
    public void Refresh(double width)
    {
        Columns = 0;
        Apply(width);
    }

    private void Place()
    {
        if (Columns == 0) return;
        _grid.ColumnDefinitions = new ColumnDefinitionCollection();
        for (var i = 0; i < Columns; i++) _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var rows = (_grid.Children.Count + Columns - 1) / Columns;
        _grid.RowDefinitions = new RowDefinitionCollection();
        for (var i = 0; i < rows; i++) _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var i = 0; i < _grid.Children.Count; i++)
        {
            if (_grid.Children[i] is not BindableObject child) continue;
            Grid.SetRow(child, i / Columns);
            Grid.SetColumn(child, i % Columns);
        }
    }
}
