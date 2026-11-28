using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>Several small metrics side by side under an optional title. <typeparamref name="TValue"/> is the number type of every value (see <see cref="KlyStat{TValue}"/>).</summary>
/// <typeparam name="TValue">The number type of the values.</typeparam>
public class KlyStatGroup<TValue> : KlyBlockBase
{
    /// <summary>The width from which the metrics sit three in a row: the <c>sm</c> breakpoint of the web grid. Below, they stack.</summary>
    internal const double WideWidth = 640;

    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyStatGroup<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyStatItem<TValue>>), typeof(KlyStatGroup<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = BindableProperty.Create(nameof(Format), typeof(string), typeof(KlyStatGroup<TValue>), "#,0.##", propertyChanged: Update);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyStatGroup<TValue>), null, propertyChanged: Update);

    private readonly Label _title = DisplayParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Grid _grid = new() { ColumnSpacing = 16, RowSpacing = 16 };
    private int _columns = 1;

    /// <summary>Creates a group of metrics.</summary>
    public KlyStatGroup() : base(KlySurface.Bordered, 8)
    {
        DisplayNumber.EnsureSupported(typeof(TValue));
        _title.IsVisible = false;
        Body.Add(_title);
        Body.Add(_grid);
        SizeChanged += (_, _) => Arrange(Width >= WideWidth ? 3 : 1);
        Refresh();
    }

    /// <summary>The line above the metrics.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The metrics, as <see cref="KlyStatItem{TValue}"/>.</summary>
    public IReadOnlyList<KlyStatItem<TValue>>? Items { get => (IReadOnlyList<KlyStatItem<TValue>>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The .NET numeric format string of the values (<c>"#,0.##"</c> by default).</summary>
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The culture that formats the values. Defaults to the culture of the current thread.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>The number of metrics per row now: 3 on a wide control, 1 on a narrow one (a phone held upright).</summary>
    internal int Columns => _columns;

    internal Grid Cells => _grid;

    internal string Text(TValue value) => DisplayNumber.Display(value, Format, Culture ?? CultureInfo.CurrentCulture) ?? "—";

    private static void Update(BindableObject bindable, object oldValue, object newValue) => ((KlyStatGroup<TValue>)bindable).Refresh();

    /// <summary>Lays the metrics out in <paramref name="columns"/> columns, in the order they are in.</summary>
    internal void Arrange(int columns)
    {
        if (columns == _columns && _grid.Children.Count == (Items?.Count ?? 0)) return;
        _columns = columns;
        Refresh();
    }

    private void Refresh()
    {
        DisplayParts.Show(_title, Title);
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();
        var items = Items ?? [];
        for (var c = 0; c < _columns; c++) _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (var i = 0; i < items.Count; i++)
        {
            if (i % _columns == 0) _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.Add(Cell(items[i]), i % _columns, i / _columns);
        }
    }

    private View Cell(KlyStatItem<TValue> item)
    {
        var cell = new VerticalStackLayout { Spacing = 2 };
        var label = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
        label.Text = item.Label;
        var value = DisplayParts.NewLabel(20, item.Color is { } color ? KlyTheme.Resolve(color).Text : KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = Text(item.Value);
        cell.Children.Add(label);
        cell.Children.Add(value);
        if (!string.IsNullOrEmpty(item.Caption))
        {
            var caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = item.Caption;
            cell.Children.Add(caption);
        }
        return cell;
    }
}
