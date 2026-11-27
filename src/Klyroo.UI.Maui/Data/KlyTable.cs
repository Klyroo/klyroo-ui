using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using MauiGrid = Microsoft.Maui.Controls.Grid;
using MauiLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui;

/// <summary>
/// A table of a list you hold, whose columns are declared with <see cref="KlyColumn{TItem, TValue}"/> and <see cref="KlyTemplateColumn{TItem}"/>: a header row, one row per item with a line between rows,
/// optional sorting by a tap on the header, a footer row, and a "show more" button for long lists. A table wider than the screen scrolls sideways.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
[ContentProperty(nameof(Columns))]
public class KlyTable<TItem> : VerticalStackLayout
{
    /// <summary>Identifies <see cref="ItemsSource"/>.</summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<TItem>), typeof(KlyTable<TItem>), null, propertyChanged: (b, o, v) => ((KlyTable<TItem>)b).ShowItems(o as IEnumerable<TItem>, v as IEnumerable<TItem>));
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(nameof(Caption), typeof(string), typeof(KlyTable<TItem>), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyTable<TItem>)b)._body, (string?)v));
    /// <summary>Identifies <see cref="Empty"/>.</summary>
    public static readonly BindableProperty EmptyProperty = BindableProperty.Create(nameof(Empty), typeof(View), typeof(KlyTable<TItem>), null, propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).Rebuild());
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int?), typeof(KlyTable<TItem>), null, propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).ShowMax());
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(int?), typeof(KlyTable<TItem>), null, propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).Rebuild());
    /// <summary>Identifies <see cref="EmptyText"/>.</summary>
    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(nameof(EmptyText), typeof(string), typeof(KlyTable<TItem>), "No data", propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).Rebuild());
    /// <summary>Identifies <see cref="SortAscendingLabel"/>.</summary>
    public static readonly BindableProperty SortAscendingLabelProperty = BindableProperty.Create(nameof(SortAscendingLabel), typeof(string), typeof(KlyTable<TItem>), "Sort ascending", propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).Rebuild());
    /// <summary>Identifies <see cref="SortDescendingLabel"/>.</summary>
    public static readonly BindableProperty SortDescendingLabelProperty = BindableProperty.Create(nameof(SortDescendingLabel), typeof(string), typeof(KlyTable<TItem>), "Sort descending", propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).Rebuild());
    /// <summary>Identifies <see cref="ShowMoreFormat"/>.</summary>
    public static readonly BindableProperty ShowMoreFormatProperty = BindableProperty.Create(nameof(ShowMoreFormat), typeof(string), typeof(KlyTable<TItem>), "Show more · {0}", propertyChanged: (b, _, _) => ((KlyTable<TItem>)b).Rebuild());

    private readonly MauiGrid _body = new();
    private readonly ScrollView _scroll;
    private readonly MauiLabel _moreLabel = new() { FontSize = 12, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
    private readonly ChatDataTapArea _more;
    private KlyColumnBase<TItem>? _sortColumn;
    private bool _ascending = true;
    private int _revealed;
    private INotifyCollectionChanged? _watched;
    private readonly List<ContentView> _hosts = [];

    /// <summary>Creates a table: add columns to <see cref="Columns"/> and set <see cref="ItemsSource"/>.</summary>
    public KlyTable()
    {
        Columns = [];
        Columns.CollectionChanged += OnColumnsChanged;
        _scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = _body };

        var more = new HorizontalStackLayout { Spacing = 6, Padding = new Thickness(8, 0), VerticalOptions = LayoutOptions.Center };
        more.Children.Add(_moreLabel);
        more.Children.Add(new KlyIcon { Name = KlyIconNames.ChevronDown, Size = KlySize.Xs, IconColor = KlyTheme.TextMuted });
        _moreLabel.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
        _more = new ChatDataTapArea(more) { IsVisible = false, HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(4) };
        _more.Tapped += (_, _) => ShowMore();

        Children.Add(_scroll);
        Children.Add(_more);
        Rebuild();
    }

    /// <summary>The columns: <see cref="KlyColumn{TItem, TValue}"/> and <see cref="KlyTemplateColumn{TItem}"/> items, in order.</summary>
    public ObservableCollection<KlyColumnBase<TItem>> Columns { get; }

    /// <summary>The rows to show. The table shows them again when the list tells that it changed (<see cref="INotifyCollectionChanged"/>).</summary>
    public IEnumerable<TItem>? ItemsSource { get => (IEnumerable<TItem>?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Describes the table for a screen reader.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Shown when there are no items, instead of <see cref="EmptyText"/>.</summary>
    public View? Empty { get => (View?)GetValue(EmptyProperty); set => SetValue(EmptyProperty, value); }

    /// <summary>The number of rows shown before a Show more button (all rows when <see langword="null"/>). The rows are cut after sorting.</summary>
    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The number of rows each press on Show more reveals (all the remaining ones when <see langword="null"/>).</summary>
    public int? Step { get => (int?)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Text shown when there are no items and no <see cref="Empty"/> view, in the language of the app.</summary>
    public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>What a screen reader says for the header of a column a tap sorts ascending, in the language of the app.</summary>
    public string SortAscendingLabel { get => (string)GetValue(SortAscendingLabelProperty); set => SetValue(SortAscendingLabelProperty, value); }

    /// <summary>What a screen reader says for the header of a column a tap sorts descending, in the language of the app.</summary>
    public string SortDescendingLabel { get => (string)GetValue(SortDescendingLabelProperty); set => SetValue(SortDescendingLabelProperty, value); }

    /// <summary>Text of the Show more button: <c>{0}</c> is the number of rows the next press reveals, in the language of the app.</summary>
    public string ShowMoreFormat { get => (string)GetValue(ShowMoreFormatProperty); set => SetValue(ShowMoreFormatProperty, value); }

    internal MauiGrid Body => _body;
    internal ChatDataTapArea MoreButton => _more;
    internal KlyColumnBase<TItem>? SortColumn => _sortColumn;
    internal bool SortAscending => _ascending;

    /// <summary>The rows in order: sorted by the sort column, if any.</summary>
    internal List<TItem> SortedItems()
    {
        var items = ItemsSource ?? [];
        return _sortColumn is { CanSort: true } column ? column.OrderItems(items, _ascending).ToList() : items.ToList();
    }

    /// <summary>Sorts by a column, or reverses the order when it already is the sort column. The first press is ascending.</summary>
    internal void Sort(KlyColumnBase<TItem> column)
    {
        _ascending = column != _sortColumn || !_ascending;
        _sortColumn = column;
        Rebuild();
    }

    /// <summary>Reveals the next batch of rows.</summary>
    internal void ShowMore()
    {
        var total = SortedItems().Count;
        var current = Math.Max(Max ?? 0, _revealed);
        _revealed = Step is > 0 ? Math.Min(total, current + Step.Value) : total;
        Rebuild();
    }

    private void ShowMax()
    {
        if (Max is { } max && _revealed < max) _revealed = max;
        Rebuild();
    }

    private void ShowItems(IEnumerable<TItem>? old, IEnumerable<TItem>? items)
    {
        if (_watched is not null) _watched.CollectionChanged -= OnItemsChanged;
        _watched = items as INotifyCollectionChanged;
        if (_watched is not null) _watched.CollectionChanged += OnItemsChanged;
        Rebuild();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (KlyColumnBase<TItem> column in e.OldItems)
            {
                column.Changed -= OnColumnChanged;
                if (column == _sortColumn) _sortColumn = null;
            }
        }
        if (e.NewItems is not null)
        {
            foreach (KlyColumnBase<TItem> column in e.NewItems) column.Changed += OnColumnChanged;
        }
        Rebuild();
    }

    private void OnColumnChanged(object? sender, EventArgs e) => Rebuild();

    // The rows to show and the number the next press reveals (0 when everything shows).
    private (List<TItem> Visible, int Remaining) Cut(List<TItem> all)
    {
        if (all.Count == 0 || Max is not { } max || max <= 0) return (all, 0);
        var limit = Math.Max(max, _revealed);
        if (all.Count <= limit) return (all, 0);
        var next = Step is > 0 ? Math.Min(Step.Value, all.Count - limit) : all.Count - limit;
        return (all.Take(limit).ToList(), next);
    }

    private void Rebuild()
    {
        // The cells hold views: let go of them before the grid is filled again, so a footer view can go into the new hosts.
        foreach (var host in _hosts) host.Content = null;
        _hosts.Clear();
        _body.Children.Clear();
        _body.RowDefinitions.Clear();
        _body.ColumnDefinitions.Clear();
        foreach (var column in Columns) column.Validate();
        if (Columns.Count == 0)
        {
            _more.IsVisible = false;
            return;
        }
        foreach (var _ in Columns) _body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var row = 0;
        for (var c = 0; c < Columns.Count; c++) _body.Add(Header(Columns[c]), c, row);
        _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        row++;

        var all = SortedItems();
        var (visible, remaining) = Cut(all);
        if (visible.Count == 0)
        {
            var empty = Empty ?? new MauiLabel { Text = EmptyText, HorizontalTextAlignment = TextAlignment.Center, FontSize = 14 };
            if (Empty is null) ((MauiLabel)empty).SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
            empty.Margin = new Thickness(0, 24);
            _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _body.Add(empty, 0, row);
            Grid.SetColumnSpan((BindableObject)empty, Columns.Count);
            row++;
        }
        for (var i = 0; i < visible.Count; i++)
        {
            for (var c = 0; c < Columns.Count; c++) _body.Add(Wrap(Columns[c].Cell(visible[i]), 16), c, row);
            _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            row++;
            // A line under every row but the last.
            if (i < visible.Count - 1) row = Line(row, KlyTheme.Border);
        }

        if (Columns.Any(c => c.HasFooter))
        {
            row = Line(row, KlyTheme.BorderAccented);
            _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var c = 0; c < Columns.Count; c++) _body.Add(Footer(Columns[c]), c, row);
        }

        _more.IsVisible = remaining > 0;
        _moreLabel.Text = string.Format(CultureInfo.CurrentCulture, ShowMoreFormat, remaining);
        _more.Description = _moreLabel.Text;
    }

    // A one unit line across the row: a row of its own, as the border of a web row.
    private int Line(int row, KlyThemeColor color)
    {
        var line = new BoxView { HeightRequest = 1 };
        line.SetThemeColor(BoxView.ColorProperty, color);
        _body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _body.Add(line, 0, row);
        Grid.SetColumnSpan((BindableObject)line, Columns.Count);
        return row + 1;
    }

    // A cell in the padding of the web cell. A label takes it as its own; any other view gets a host.
    private View Wrap(View cell, double vertical)
    {
        if (cell is MauiLabel label)
        {
            label.Padding = new Thickness(16, vertical);
            return label;
        }
        var host = new ContentView { Padding = new Thickness(16, vertical), Content = cell };
        _hosts.Add(host);
        return host;
    }

    private View Header(KlyColumnBase<TItem> column)
    {
        var title = new MauiLabel
        {
            Text = column.Title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.NoWrap,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = column.AtEnd ? TextAlignment.End : TextAlignment.Start,
        };
        title.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Text);
        if (!column.CanSort) return Wrap(title, 14);

        // A sortable header is a button: the title and the sort icon, which shows the direction once the column is the sort column.
        var icon = new KlyIcon
        {
            Name = column != _sortColumn ? KlyIconNames.ChevronUpDown : _ascending ? KlyIconNames.ArrowUp : KlyIconNames.ArrowDown,
            Size = KlySize.Xs,
            IconColor = KlyTheme.TextDimmed,
            VerticalOptions = LayoutOptions.Center,
        };
        var row = new HorizontalStackLayout { Spacing = 4, HorizontalOptions = column.AtEnd ? LayoutOptions.End : LayoutOptions.Start };
        row.Children.Add(title);
        row.Children.Add(icon);
        var area = new ChatDataTapArea(row)
        {
            Description = column.Title,
            Hint = column == _sortColumn && _ascending ? SortDescendingLabel : SortAscendingLabel,
            Margin = new Thickness(16, 0),
        };
        area.Tapped += (_, _) => Sort(column);
        return area;
    }

    private View Footer(KlyColumnBase<TItem> column)
    {
        if (column.FooterTemplate is { } template) return Wrap(template, 14);
        var label = new MauiLabel
        {
            Text = column.Footer,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.NoWrap,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = column.AtEnd ? TextAlignment.End : TextAlignment.Start,
        };
        label.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextHighlighted);
        return Wrap(label, 14);
    }
}
