using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using MauiGrid = Microsoft.Maui.Controls.Grid;
using MauiLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui;

/// <summary>
/// A line, area, bar, donut or funnel chart drawn natively, with no charting library. Series differ by dash, marker and fill pattern as well as by color,
/// a touch shows the tooltip of a row and raises <see cref="Selected"/>, and a generated description serves a screen reader. The series colors are the chart tokens, which follow the semantic colors.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <typeparam name="TIndex">The type of the x value of a row: a date, a number, a label.</typeparam>
public class KlyChart<TItem, TIndex> : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Data"/>.</summary>
    public static readonly BindableProperty DataProperty = Prop<IReadOnlyList<TItem>?>(nameof(Data), null);
    /// <summary>Identifies <see cref="Index"/>.</summary>
    public static readonly BindableProperty IndexProperty = Prop<Func<TItem, TIndex>?>(nameof(Index), null);
    /// <summary>Identifies <see cref="Categories"/>.</summary>
    public static readonly BindableProperty CategoriesProperty = Prop<IReadOnlyList<KlyChartCategory<TItem>>?>(nameof(Categories), null);
    /// <summary>Identifies <see cref="Type"/>.</summary>
    public static readonly BindableProperty TypeProperty = Prop(nameof(Type), KlyChartType.Line);
    /// <summary>Identifies <see cref="Horizontal"/>.</summary>
    public static readonly BindableProperty HorizontalProperty = Prop(nameof(Horizontal), false);
    /// <summary>Identifies <see cref="Stacked"/>.</summary>
    public static readonly BindableProperty StackedProperty = Prop(nameof(Stacked), false);
    /// <summary>Identifies <see cref="Curve"/>.</summary>
    public static readonly BindableProperty CurveProperty = Prop(nameof(Curve), KlyChartCurve.Linear);
    /// <summary>Identifies <see cref="Points"/>.</summary>
    public static readonly BindableProperty PointsProperty = Prop(nameof(Points), false);
    /// <summary>Identifies <see cref="Grid"/>.</summary>
    public static readonly BindableProperty GridProperty = Prop(nameof(Grid), true);
    /// <summary>Identifies <see cref="XAxis"/>.</summary>
    public static readonly BindableProperty XAxisProperty = Prop(nameof(XAxis), true);
    /// <summary>Identifies <see cref="YAxis"/>.</summary>
    public static readonly BindableProperty YAxisProperty = Prop(nameof(YAxis), true);
    /// <summary>Identifies <see cref="XLabel"/>.</summary>
    public static readonly BindableProperty XLabelProperty = Prop<string?>(nameof(XLabel), null);
    /// <summary>Identifies <see cref="YLabel"/>.</summary>
    public static readonly BindableProperty YLabelProperty = Prop<string?>(nameof(YLabel), null);
    /// <summary>Identifies <see cref="Legend"/>.</summary>
    public static readonly BindableProperty LegendProperty = Prop<bool?>(nameof(Legend), null);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = Prop<Func<double, string>?>(nameof(Format), null);
    /// <summary>Identifies <see cref="IndexFormat"/>.</summary>
    public static readonly BindableProperty IndexFormatProperty = Prop<string?>(nameof(IndexFormat), null);
    /// <summary>Identifies <see cref="Tooltip"/>.</summary>
    public static readonly BindableProperty TooltipProperty = Prop(nameof(Tooltip), true);
    /// <summary>Identifies <see cref="Thickness"/>.</summary>
    public static readonly BindableProperty ThicknessProperty = Prop(nameof(Thickness), 24);
    /// <summary>Identifies <see cref="PlotHeight"/>.</summary>
    public static readonly BindableProperty PlotHeightProperty = Prop(nameof(PlotHeight), 300);
    /// <summary>Identifies <see cref="PlotWidth"/>.</summary>
    public static readonly BindableProperty PlotWidthProperty = Prop(nameof(PlotWidth), 640);
    /// <summary>Identifies <see cref="Colors"/>.</summary>
    public static readonly BindableProperty ColorsProperty = Prop<IReadOnlyList<KlyColor>?>(nameof(Colors), null);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = Prop<string?>(nameof(AccessibleName), null);
    /// <summary>Identifies <see cref="AccessibleDescription"/>.</summary>
    public static readonly BindableProperty AccessibleDescriptionProperty = Prop<string?>(nameof(AccessibleDescription), null);
    /// <summary>Identifies <see cref="Table"/>.</summary>
    public static readonly BindableProperty TableProperty = Prop(nameof(Table), KlyChartTable.Hidden);
    /// <summary>Identifies <see cref="Center"/>.</summary>
    public static readonly BindableProperty CenterProperty = Prop<View?>(nameof(Center), null);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = Prop<CultureInfo?>(nameof(Culture), null);

    private readonly MauiGrid _plot = new();
    private readonly ChartDataCanvas _canvas = new();
    private readonly ContentView _center = new() { InputTransparent = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly VerticalStackLayout _funnel = new() { Spacing = 8, IsVisible = false };
    private readonly FlexLayout _legend = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Center, IsVisible = false, Margin = new Thickness(0, 12, 0, 0) };
    private readonly VerticalStackLayout _tableHost = new() { IsVisible = false };
    private ChartDataModel _model = new();
    private List<(string Label, string Text, double Width, KlyColor Color)> _steps = [];
    private bool _tableOpen;
    private bool _donut;

    /// <summary>Creates an empty chart: set <see cref="Data"/>, <see cref="Index"/> and <see cref="Categories"/>.</summary>
    public KlyChart()
    {
        _plot.Children.Add(_canvas);
        _plot.Children.Add(_center);
        _canvas.Picked += Pick;
        SemanticProperties.SetDescription(_canvas, string.Empty);
        Children.Add(_plot);
        Children.Add(_funnel);
        Children.Add(_legend);
        Children.Add(_tableHost);
        Rebuild();
    }

    /// <summary>Raised when the user taps a bar, a point or a slice. A donut, or a chart with one series, names that series; with several, the nearest one to the touch.</summary>
    public event EventHandler<KlyChartSelectEventArgs<TItem, TIndex>>? Selected;

    /// <summary>The rows to plot: one per x value, or per slice with a donut.</summary>
    public IReadOnlyList<TItem>? Data { get => (IReadOnlyList<TItem>?)GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The value of a row on the x axis (the label of a slice with a donut). Dates and numbers are formatted with <see cref="Culture"/>.</summary>
    public Func<TItem, TIndex>? Index { get => (Func<TItem, TIndex>?)GetValue(IndexProperty); set => SetValue(IndexProperty, value); }

    /// <summary>The series, in drawing order. With a donut the first one gives the size of each slice.</summary>
    public IReadOnlyList<KlyChartCategory<TItem>>? Categories { get => (IReadOnlyList<KlyChartCategory<TItem>>?)GetValue(CategoriesProperty); set => SetValue(CategoriesProperty, value); }

    /// <summary>Kind of chart to draw: line, area, bar, donut or funnel.</summary>
    public KlyChartType Type { get => (KlyChartType)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Draws bars along the x axis, with the index on the y axis. Only applies to <see cref="KlyChartType.Bar"/>.</summary>
    public bool Horizontal { get => (bool)GetValue(HorizontalProperty); set => SetValue(HorizontalProperty, value); }

    /// <summary>Stacks the series instead of drawing them side by side or over one another. Only applies to area and bar.</summary>
    public bool Stacked { get => (bool)GetValue(StackedProperty); set => SetValue(StackedProperty, value); }

    /// <summary>The shape of the line between values. Only applies to line and area.</summary>
    public KlyChartCurve Curve { get => (KlyChartCurve)GetValue(CurveProperty); set => SetValue(CurveProperty, value); }

    /// <summary>Always shows a marker at each value of a line chart. Without it the marker shows on the row under the finger.</summary>
    public bool Points { get => (bool)GetValue(PointsProperty); set => SetValue(PointsProperty, value); }

    /// <summary>Draws the grid lines at the ticks of the value axis.</summary>
    public bool Grid { get => (bool)GetValue(GridProperty); set => SetValue(GridProperty, value); }

    /// <summary>Shows the labels of the index axis (the x axis, or the y axis of horizontal bars).</summary>
    public bool XAxis { get => (bool)GetValue(XAxisProperty); set => SetValue(XAxisProperty, value); }

    /// <summary>Shows the ticks and labels of the value axis.</summary>
    public bool YAxis { get => (bool)GetValue(YAxisProperty); set => SetValue(YAxisProperty, value); }

    /// <summary>A title along the index axis.</summary>
    public string? XLabel { get => (string?)GetValue(XLabelProperty); set => SetValue(XLabelProperty, value); }

    /// <summary>A title along the value axis.</summary>
    public string? YLabel { get => (string?)GetValue(YLabelProperty); set => SetValue(YLabelProperty, value); }

    /// <summary>Shows the legend. By default it shows when there is more than one series, or more than one slice with a donut.</summary>
    public bool? Legend { get => (bool?)GetValue(LegendProperty); set => SetValue(LegendProperty, value); }

    /// <summary>Formats the values on the value axis, in the tooltip and in the table. Defaults to the number format of <see cref="Culture"/>.</summary>
    public Func<double, string>? Format { get => (Func<double, string>?)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The format string of an index that is formattable (a date, a number).</summary>
    public string? IndexFormat { get => (string?)GetValue(IndexFormatProperty); set => SetValue(IndexFormatProperty, value); }

    /// <summary>Shows the tooltip of the row under the finger.</summary>
    public bool Tooltip { get => (bool)GetValue(TooltipProperty); set => SetValue(TooltipProperty, value); }

    /// <summary>The width of the ring in device units. Only applies to <see cref="KlyChartType.Donut"/>.</summary>
    public int Thickness { get => (int)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>The height of the drawing in device units. The width is the width of the container, up to <see cref="PlotWidth"/>.</summary>
    public int PlotHeight { get => (int)GetValue(PlotHeightProperty); set => SetValue(PlotHeightProperty, value); }

    /// <summary>The widest the drawing gets.</summary>
    public int PlotWidth { get => (int)GetValue(PlotWidthProperty); set => SetValue(PlotWidthProperty, value); }

    /// <summary>The colors of the series in order, in place of the default palette. A color set on a category wins.</summary>
    public IReadOnlyList<KlyColor>? Colors { get => (IReadOnlyList<KlyColor>?)GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }

    /// <summary>The accessible name of the chart. Without it the chart is described by a generated summary of its values, in English.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>A longer description for a screen reader, in addition to the name.</summary>
    public string? AccessibleDescription { get => (string?)GetValue(AccessibleDescriptionProperty); set => SetValue(AccessibleDescriptionProperty, value); }

    /// <summary>Where the table with the chart's data goes: <c>Hidden</c> (none), <c>Visible</c> under the chart, or behind a <c>Toggle</c>.</summary>
    public KlyChartTable Table { get => (KlyChartTable)GetValue(TableProperty); set => SetValue(TableProperty, value); }

    /// <summary>Content over the hole of a donut: a total, a caption.</summary>
    public View? Center { get => (View?)GetValue(CenterProperty); set => SetValue(CenterProperty, value); }

    /// <summary>The culture of the numbers and dates. Defaults to the current culture.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    internal ChartDataModel Model => _model;
    internal ChartDataCanvas Canvas => _canvas;
    internal FlexLayout LegendHost => _legend;
    internal VerticalStackLayout FunnelHost => _funnel;
    internal VerticalStackLayout TableHost => _tableHost;
    internal MauiGrid PlotHost => _plot;
    internal ContentView CenterHost => _center;
    internal IReadOnlyList<(string Label, string Text, double Width, KlyColor Color)> FunnelSteps => _steps;
    internal string Summary { get; private set; } = string.Empty;
    private CultureInfo Cult => Culture ?? CultureInfo.CurrentCulture;

    private static BindableProperty Prop<T>(string name, T? defaultValue) =>
        BindableProperty.Create(name, typeof(T), typeof(KlyChart<TItem, TIndex>), defaultValue, propertyChanged: (b, _, _) => ((KlyChart<TItem, TIndex>)b).Rebuild());

    /// <summary>The text of a value: <see cref="Format"/>, else the number format of the culture.</summary>
    internal string FormatValue(double value) => Format is not null ? Format(value) : (value == 0 ? 0 : value).ToString("#,0.##", Cult);

    internal string IndexText(TIndex index) => (object?)index switch
    {
        null => string.Empty,
        DateTime date => date.ToString(IndexFormat ?? "d", Cult),
        DateTimeOffset date => date.ToString(IndexFormat ?? "d", Cult),
        DateOnly date => date.ToString(IndexFormat ?? "d", Cult),
        IFormattable formattable => formattable.ToString(IndexFormat, Cult),
        _ => index?.ToString() ?? string.Empty,
    };

    /// <summary>Reports a pick of the canvas: a row and the series nearest to the touch.</summary>
    internal void Pick(int row, int series)
    {
        var data = Data;
        if (data is null || Index is not { } index || row < 0 || row >= data.Count || _model.Values.Count == 0) return;
        var item = data[row];
        // A donut has one category (the size of the slices), and so does a chart with one series. With several, the touch names the nearest.
        var s = _model.Donut || _model.Values.Count == 1 ? 0 : series;
        var categories = Categories;
        if (s < 0 || s >= _model.Values.Count)
        {
            Selected?.Invoke(this, new KlyChartSelectEventArgs<TItem, TIndex>(row, index(item), null, -1, null, item));
            return;
        }
        var category = categories is not null && s < categories.Count ? categories[s] : null;
        Selected?.Invoke(this, new KlyChartSelectEventArgs<TItem, TIndex>(row, index(item), category?.Key, category is null ? -1 : s, _model.Values[s][row], item));
    }

    private static double? Sanitize(double? value) => value is { } v && double.IsFinite(v) ? v : null;

    private KlyColor ColorOf(int position, KlyColor? own) =>
        own ?? (Colors is { Count: > 0 } colors ? colors[position % colors.Count] : ChartDataColors.Palette[position % ChartDataColors.Palette.Length]);

    private void Rebuild()
    {
        var data = Data ?? [];
        var categories = (Categories ?? []).ToList();
        var index = Index;
        _donut = Type == KlyChartType.Donut;

        _plot.IsVisible = Type != KlyChartType.Funnel;
        _funnel.IsVisible = Type == KlyChartType.Funnel;
        _plot.HeightRequest = PlotHeight;
        _plot.MaximumWidthRequest = _donut ? PlotHeight : PlotWidth;
        _plot.HorizontalOptions = _donut ? LayoutOptions.Center : LayoutOptions.Fill;
        if (_donut) _plot.WidthRequest = PlotHeight;
        else _plot.WidthRequest = -1;

        _center.Content = Center;
        _center.IsVisible = _donut && Center is not null;

        if (Type == KlyChartType.Funnel)
        {
            BuildFunnel(data, categories, index);
            // The plot is hidden: the canvas has nothing to draw.
            _model = new ChartDataModel { Type = KlyChartType.Funnel };
            _canvas.Model = _model;
            _legend.IsVisible = false;
            _tableHost.IsVisible = false;
            Summary = string.Empty;
            return;
        }

        var indexes = index is null ? new List<string>() : data.Select(item => IndexText(index(item))).ToList();
        // A donut colors the slices, so it has one "series" per row; the others one per category.
        var used = _donut ? categories.Take(1).ToList() : categories;
        var values = used.Select(c => data.Select(item => Sanitize(c.Value(item))).ToArray()).ToList();
        // A donut without a category has slices of no size, not a missing column.
        if (_donut && values.Count == 0) values.Add(new double?[data.Count]);
        var series = new List<ChartDataSeries>();
        if (_donut)
        {
            for (var i = 0; i < data.Count; i++) series.Add(new ChartDataSeries(indexes.Count > i ? indexes[i] : string.Empty, ChartDataColors.Resolve(ColorOf(i, null))));
        }
        else
        {
            for (var i = 0; i < used.Count; i++) series.Add(new ChartDataSeries(used[i].Name, ChartDataColors.Resolve(ColorOf(i, used[i].Color))));
        }

        _model = new ChartDataModel
        {
            Type = Type,
            Horizontal = Horizontal,
            Stacked = Stacked,
            Curve = Curve,
            Points = Points,
            Grid = Grid,
            XAxis = XAxis,
            YAxis = YAxis,
            XLabel = XLabel,
            YLabel = YLabel,
            Thickness = Thickness,
            Index = indexes,
            Values = values,
            Series = series,
            ValueName = categories.Count > 0 ? categories[0].Name : string.Empty,
            Format = FormatValue,
            Percent = fraction => fraction.ToString("P0", Cult),
        };
        _canvas.TooltipEnabled = Tooltip;
        _canvas.Model = _model;

        Summary = BuildSummary(data.Count == 0 || categories.Count == 0);
        SemanticProperties.SetDescription(_canvas, AccessibleName ?? Summary);
        SemanticProperties.SetHint(_canvas, AccessibleDescription);

        BuildLegend();
        BuildTable();
    }

    private string Kind => Type switch
    {
        KlyChartType.Line => "Line chart",
        KlyChartType.Area => "Area chart",
        KlyChartType.Bar => Horizontal ? "Horizontal bar chart" : "Bar chart",
        KlyChartType.Donut => "Donut chart",
        _ => "Funnel chart",
    };

    /// <summary>The description of the chart: its kind, extent and the range of each series, or the slices of a donut.</summary>
    private string BuildSummary(bool empty)
    {
        if (empty) return $"{Kind}. No data.";
        var index = _model.Index;
        if (_donut)
        {
            var values = _model.Values[0];
            var total = values.Where(v => v is > 0).Sum(v => v!.Value);
            var slices = Enumerable.Range(0, index.Count).Where(i => values[i] is > 0).Select(i => $"{index[i]} {FormatValue(values[i]!.Value)} ({(values[i]!.Value / total).ToString("P0", Cult)})");
            return $"{Kind}. Slices: {string.Join(", ", slices)}.";
        }
        var ranges = _model.Values.Select((v, s) =>
        {
            var present = v.Where(x => x is not null).Select(x => x!.Value).ToList();
            return present.Count == 0 ? _model.Series[s].Name : $"{_model.Series[s].Name} ({FormatValue(present.Min())} to {FormatValue(present.Max())})";
        });
        return index.Count == 0
            ? $"{Kind}. No data."
            : $"{Kind} with {index.Count} data points from {index[0]} to {index[^1]}. {string.Join(", ", ranges)}.";
    }

    private void BuildLegend()
    {
        _legend.Children.Clear();
        var count = _model.Series.Count;
        var show = Legend ?? (_donut ? count > 1 : count > 1);
        _legend.IsVisible = show && count > 0;
        if (!_legend.IsVisible) return;
        SemanticProperties.SetDescription(_legend, "Legend");
        for (var i = 0; i < count; i++)
        {
            var series = _model.Series[i];
            var item = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(8, 3), VerticalOptions = LayoutOptions.Center };
            View swatch;
            if (Type == KlyChartType.Line)
            {
                var line = new BoxView { WidthRequest = 20, HeightRequest = 2, VerticalOptions = LayoutOptions.Center };
                line.SetThemeColor(BoxView.ColorProperty, series.Colors.Text);
                swatch = line;
            }
            else
            {
                var box = new Border { WidthRequest = 20, HeightRequest = 12, VerticalOptions = LayoutOptions.Center, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2) } };
                box.SetStroke(series.Colors.Text);
                box.SetThemeColor(BackgroundColorProperty, series.Colors.Text.WithAlpha(i % 4 == 0 && Type != KlyChartType.Area ? 1f : 0.2f));
                swatch = box;
            }
            item.Children.Add(swatch);
            var name = new MauiLabel { Text = series.Name, FontSize = 12, VerticalOptions = LayoutOptions.Center };
            name.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Text);
            item.Children.Add(name);
            if (_donut)
            {
                var value = new MauiLabel { Text = FormatValue(_model.Values[0][i] ?? 0), FontSize = 12, VerticalOptions = LayoutOptions.Center };
                value.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
                item.Children.Add(value);
            }
            _legend.Children.Add(item);
        }
    }

    // The table of the numbers: a visible one, or one behind a toggle. Hidden has none: the description of the chart tells a screen reader its range.
    private void BuildTable()
    {
        _tableHost.Children.Clear();
        _tableHost.IsVisible = Table != KlyChartTable.Hidden && _model.Index.Count > 0;
        if (!_tableHost.IsVisible) return;
        var table = BuildTableGrid();
        if (Table == KlyChartTable.Toggle)
        {
            var row = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            var title = new MauiLabel { Text = "Data table", FontSize = 14, FontAttributes = FontAttributes.Bold };
            title.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Text);
            var chevron = new KlyIcon { Name = _tableOpen ? KlyIconNames.ChevronUp : KlyIconNames.ChevronDown, Size = KlySize.Sm };
            row.Children.Add(title);
            row.Children.Add(chevron);
            var tap = new ChatDataTapArea(row) { Description = "Data table", HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(0, 8, 0, 0) };
            tap.Tapped += (_, _) =>
            {
                _tableOpen = !_tableOpen;
                BuildTable();
            };
            _tableHost.Children.Add(tap);
            table.IsVisible = _tableOpen;
        }
        _tableHost.Children.Add(table);
    }

    private View BuildTableGrid()
    {
        var grid = new MauiGrid { ColumnSpacing = 0, RowSpacing = 0 };
        var columns = _donut ? 2 : _model.Series.Count + 1;
        for (var c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var names = _donut ? new List<string> { ValueHeader() } : _model.Series.Select(s => s.Name).ToList();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddCell(grid, 0, 0, XLabel ?? "Category", true, false);
        for (var c = 0; c < names.Count; c++) AddCell(grid, 0, c + 1, names[c], true, true);
        for (var r = 0; r < _model.Index.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddCell(grid, r + 1, 0, _model.Index[r], true, false);
            for (var c = 0; c < names.Count; c++) AddCell(grid, r + 1, c + 1, _model.Values[_donut ? 0 : c][r] is { } v ? FormatValue(v) : string.Empty, false, true);
        }
        var border = new Border { StrokeThickness = 1, Margin = new Thickness(0, 8, 0, 0), Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = grid } };
        border.SetRadius(KlyTheme.RadiusOf(1));
        border.SetStroke(KlyTheme.Border);
        SemanticProperties.SetDescription(border, AccessibleName ?? Kind);
        return border;
    }

    private string ValueHeader() => Categories is { Count: > 0 } categories ? categories[0].Name : string.Empty;

    private static void AddCell(MauiGrid grid, int row, int column, string text, bool header, bool end)
    {
        var label = new MauiLabel
        {
            Text = text,
            FontSize = 14,
            FontAttributes = header ? FontAttributes.Bold : FontAttributes.None,
            Padding = new Thickness(12, 8),
            HorizontalTextAlignment = end ? TextAlignment.End : TextAlignment.Start,
        };
        label.SetThemeColor(MauiLabel.TextColorProperty, header ? KlyTheme.TextHighlighted : KlyTheme.Text);
        grid.Add(label, column, row);
    }

    // The funnel is a list of steps, not a plot: each has its label, its value with the share of the previous step, and a bar as wide as its share of the largest step.
    private void BuildFunnel(IReadOnlyList<TItem> data, List<KlyChartCategory<TItem>> categories, Func<TItem, TIndex>? index)
    {
        var category = categories.Count > 0 ? categories[0] : null;
        var values = data.Select(item => category is null ? null : Sanitize(category.Value(item))).ToList();
        // Widths are relative to the largest positive step; a zero, negative or missing step has no bar, a positive one never vanishes.
        var positives = values.Where(v => v is > 0).Select(v => v!.Value).ToList();
        var largest = positives.Count > 0 ? positives.Max() : 1.0;
        _steps = [];
        _funnel.Children.Clear();
        SemanticProperties.SetDescription(_funnel, AccessibleName);
        if (data.Count == 0)
        {
            var empty = new MauiLabel { Text = "No data", FontSize = 12 };
            empty.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
            _funnel.Children.Add(empty);
            return;
        }
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            var width = value is > 0 ? Math.Clamp(value.Value / largest * 100.0, 2, 100) : 0.0;
            var previous = i > 0 ? values[i - 1] : null;
            var rate = previous is > 0 ? $"{Math.Round((value ?? 0) / previous.Value * 100).ToString(CultureInfo.InvariantCulture)}%" : null;
            var shown = value is { } v ? FormatValue(v) : "—";
            var color = category?.Color ?? (Colors is { Count: > 0 } colors ? colors[i % colors.Count] : ChartDataColors.Palette[0]);
            var label = index is null ? string.Empty : IndexText(index(data[i]));
            var text = rate is null ? shown : $"{shown} ({rate})";
            _steps.Add((label, text, width, color));
            _funnel.Children.Add(FunnelStep(label, text, width, color));
        }
    }

    private static View FunnelStep(string label, string text, double width, KlyColor color)
    {
        var set = ChartDataColors.Resolve(color);
        var header = new MauiGrid();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var name = new MauiLabel { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation };
        name.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Text);
        var value = new MauiLabel { Text = text, FontSize = 12, HorizontalTextAlignment = TextAlignment.End, Margin = new Thickness(8, 0, 0, 0) };
        value.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
        header.Add(name, 0);
        header.Add(value, 1);

        // The bar takes its share of the width with star columns: no need to know the width of the container.
        var track = new MauiGrid();
        track.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(width, 0.0001), GridUnitType.Star)));
        track.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(100 - width, 0.0001), GridUnitType.Star)));
        var bar = new Border { HeightRequest = 8, StrokeThickness = 1, IsVisible = width > 0, StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) } };
        bar.SetThemeColor(BackgroundColorProperty, set.Fill);
        bar.SetStroke(set.Text);
        track.Add(bar, 0);

        var step = new VerticalStackLayout { Spacing = 4 };
        step.Children.Add(header);
        step.Children.Add(track);
        SemanticProperties.SetDescription(step, $"{label}: {text}");
        return step;
    }
}
