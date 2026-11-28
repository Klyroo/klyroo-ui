using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A list of labelled amounts with subtotal rows and a total row, optionally in two columns or behind a toggle.</summary>
public class KlyBreakdown : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyBreakdown), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Lines"/>.</summary>
    public static readonly BindableProperty LinesProperty = BindableProperty.Create(nameof(Lines), typeof(IReadOnlyList<KlyBreakdownLine>), typeof(KlyBreakdown), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Total"/>.</summary>
    public static readonly BindableProperty TotalProperty = BindableProperty.Create(nameof(Total), typeof(KlyBreakdownLine), typeof(KlyBreakdown), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="CompareColumns"/>.</summary>
    public static readonly BindableProperty CompareColumnsProperty = BindableProperty.Create(nameof(CompareColumns), typeof(KlyBreakdownColumns), typeof(KlyBreakdown), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Collapsible"/>.</summary>
    public static readonly BindableProperty CollapsibleProperty = BindableProperty.Create(nameof(Collapsible), typeof(bool), typeof(KlyBreakdown), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="DefaultOpen"/>.</summary>
    public static readonly BindableProperty DefaultOpenProperty = BindableProperty.Create(nameof(DefaultOpen), typeof(bool), typeof(KlyBreakdown), false, propertyChanged: (b, _, v) => ((KlyBreakdown)b).ApplyDefaultOpen((bool)v));
    /// <summary>Identifies <see cref="IsOpen"/>.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(KlyBreakdown), false, BindingMode.TwoWay, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Footnote"/>.</summary>
    public static readonly BindableProperty FootnoteProperty = BindableProperty.Create(nameof(Footnote), typeof(string), typeof(KlyBreakdown), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = BindableProperty.Create(nameof(Format), typeof(string), typeof(KlyBreakdown), "N2", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyBreakdown), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="DetailsLabel"/>.</summary>
    public static readonly BindableProperty DetailsLabelProperty = BindableProperty.Create(nameof(DetailsLabel), typeof(string), typeof(KlyBreakdown), "Details", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="HideDetailsLabel"/>.</summary>
    public static readonly BindableProperty HideDetailsLabelProperty = BindableProperty.Create(nameof(HideDetailsLabel), typeof(string), typeof(KlyBreakdown), "Hide details", propertyChanged: Rebuild);

    private const double AmountWidth = 96;

    private readonly VerticalStackLayout _stack = new() { Spacing = 8 };
    private bool _touched;

    /// <summary>Creates a breakdown.</summary>
    public KlyBreakdown() : base(KlySurface.Bordered, 8)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The title of the block.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The lines, as <see cref="KlyBreakdownLine"/>.</summary>
    public IReadOnlyList<KlyBreakdownLine>? Lines { get => (IReadOnlyList<KlyBreakdownLine>?)GetValue(LinesProperty); set => SetValue(LinesProperty, value); }

    /// <summary>The total row, shown below the lines.</summary>
    public KlyBreakdownLine? Total { get => (KlyBreakdownLine?)GetValue(TotalProperty); set => SetValue(TotalProperty, value); }

    /// <summary>The headers of the two amount columns, for example <c>new KlyBreakdownColumns("2026", "2025")</c>: <see cref="KlyBreakdownLine.Amount"/> goes under <c>First</c> and <see cref="KlyBreakdownLine.CompareAmount"/> under <c>Second</c>, for the lines and the total. A line without a second amount shows a dash. Setting a <see cref="KlyBreakdownLine.CompareAmount"/> on any line also switches to two columns, without headers.</summary>
    public KlyBreakdownColumns? CompareColumns { get => (KlyBreakdownColumns?)GetValue(CompareColumnsProperty); set => SetValue(CompareColumnsProperty, value); }

    /// <summary>Hides the lines behind a toggle. The total, the headers of the columns and the footnote stay visible.</summary>
    public bool Collapsible { get => (bool)GetValue(CollapsibleProperty); set => SetValue(CollapsibleProperty, value); }

    /// <summary>Whether a collapsible block starts open. It sets <see cref="IsOpen"/> until the user has pressed the toggle once.</summary>
    public bool DefaultOpen { get => (bool)GetValue(DefaultOpenProperty); set => SetValue(DefaultOpenProperty, value); }

    /// <summary>Whether the lines of a collapsible block show. Two-way bindable; the toggle changes it.</summary>
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>A line of smaller text under the block.</summary>
    public string? Footnote { get => (string?)GetValue(FootnoteProperty); set => SetValue(FootnoteProperty, value); }

    /// <summary>The .NET numeric format string of the amounts (<c>"N2"</c> by default; <c>"C2"</c> shows the currency of <see cref="Culture"/>).</summary>
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The culture that formats the amounts. Defaults to the culture of the current thread.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>The text of the toggle that opens the lines, in the language of the app.</summary>
    public string DetailsLabel { get => (string)GetValue(DetailsLabelProperty); set => SetValue(DetailsLabelProperty, value); }

    /// <summary>The text of the toggle that closes the lines, in the language of the app.</summary>
    public string HideDetailsLabel { get => (string)GetValue(HideDetailsLabelProperty); set => SetValue(HideDetailsLabelProperty, value); }

    internal VerticalStackLayout Stack => _stack;

    /// <summary>The two-column layout: set by the headers, by a second amount on a line, or by a second amount on the total.</summary>
    internal bool Compare => CompareColumns is not null || Lines?.Any(l => l.CompareAmount is not null) == true || Total?.CompareAmount is not null;

    internal string Amount(decimal? amount) => amount is { } value ? value.ToString(Format, Culture ?? CultureInfo.CurrentCulture) : "—";

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyBreakdown)bindable).Rebuild();

    private void ApplyDefaultOpen(bool open)
    {
        if (!_touched) IsOpen = open;
    }

    private void Rebuild()
    {
        _stack.Children.Clear();
        var compare = Compare;

        if (!string.IsNullOrEmpty(Title))
        {
            var title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
            title.Text = Title;
            _stack.Children.Add(title);
        }

        if (Collapsible)
        {
            _stack.Children.Add(DisplayParts.TextButton(IsOpen ? HideDetailsLabel : DetailsLabel, DisplayParts.PrimaryText, 12, () =>
            {
                _touched = true;
                IsOpen = !IsOpen;
            }));
        }

        if (CompareColumns is { } columns && (!Collapsible || IsOpen || Total is not null))
        {
            var header = new Grid { ColumnSpacing = 8 };
            Columns(header, compare);
            var first = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            first.Text = columns.First;
            first.HorizontalTextAlignment = TextAlignment.End;
            var second = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            second.Text = columns.Second;
            second.HorizontalTextAlignment = TextAlignment.End;
            header.Add(first, 1);
            header.Add(second, 2);
            _stack.Children.Add(header);
        }

        if ((!Collapsible || IsOpen) && Lines is { Count: > 0 } lines)
        {
            var list = new VerticalStackLayout { Spacing = 6 };
            foreach (var line in lines) list.Children.Add(Row(line, line.IsSubtotal ? RowKind.Subtotal : line.IsMuted ? RowKind.Muted : RowKind.Line, compare));
            _stack.Children.Add(list);
        }

        if (Total is { } total) _stack.Children.Add(Row(total, RowKind.Total, compare));

        if (!string.IsNullOrEmpty(Footnote))
        {
            var footnote = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            footnote.Text = Footnote;
            _stack.Children.Add(footnote);
        }
    }

    private static void Columns(Grid grid, bool compare)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(compare ? new GridLength(AmountWidth) : GridLength.Auto));
        if (compare) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(AmountWidth)));
    }

    private enum RowKind { Line, Subtotal, Muted, Total }

    private View Row(KlyBreakdownLine line, RowKind kind, bool compare)
    {
        var emphasis = kind is RowKind.Subtotal or RowKind.Total;
        var textColor = kind == RowKind.Muted ? KlyTheme.TextDimmed : emphasis ? KlyTheme.TextHighlighted : KlyTheme.Text;
        var attributes = emphasis ? FontAttributes.Bold : FontAttributes.None;

        var row = new Grid { ColumnSpacing = 8 };
        Columns(row, compare);

        var labelRow = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center };
        var label = DisplayParts.NewLabel(14, textColor, attributes);
        label.Text = line.Label;
        label.Margin = new Thickness(0, 0, 6, 0);
        labelRow.Children.Add(label);
        if (line.Badge is { } badge) labelRow.Children.Add(DisplayParts.Badge(badge));
        var term = new VerticalStackLayout { Children = { labelRow } };
        if (!string.IsNullOrEmpty(line.Detail))
        {
            var detail = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            detail.Text = line.Detail;
            term.Children.Add(detail);
        }
        row.Add(term, 0);

        var amountColor = line.Color is { } color ? KlyTheme.Resolve(color).Text : textColor;
        var amount = DisplayParts.NewLabel(14, amountColor, attributes, LineBreakMode.NoWrap);
        amount.Text = Amount(line.Amount);
        amount.HorizontalTextAlignment = TextAlignment.End;
        amount.VerticalOptions = LayoutOptions.End;
        row.Add(amount, 1);
        if (compare)
        {
            var other = DisplayParts.NewLabel(14, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            other.Text = Amount(line.CompareAmount);
            other.HorizontalTextAlignment = TextAlignment.End;
            other.VerticalOptions = LayoutOptions.End;
            row.Add(other, 2);
        }

        if (kind is RowKind.Subtotal or RowKind.Total)
        {
            // A subtotal has a thin line above it, the total a firmer one (border-t, and border-t with the accented border).
            var box = new VerticalStackLayout { Spacing = kind == RowKind.Total ? 8 : 6 };
            box.Children.Add(DisplayParts.Divider(kind == RowKind.Total ? KlyTheme.BorderAccented : KlyTheme.Border));
            box.Children.Add(row);
            return box;
        }
        return row;
    }
}
