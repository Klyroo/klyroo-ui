namespace Klyroo.UI.Maui;

/// <summary>
/// What every column of a <see cref="KlyTable{TItem}"/> has: a title, an alignment, a footer. A column draws nothing itself: the table asks it for the cell of each item.
/// Use <see cref="KlyColumn{TItem, TValue}"/> to show a value of the item or <see cref="KlyTemplateColumn{TItem}"/> for custom cells.
/// </summary>
/// <typeparam name="TItem">The row type of the table.</typeparam>
public abstract class KlyColumnBase<TItem> : BindableObject
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyColumnBase<TItem>), string.Empty);
    /// <summary>Identifies <see cref="Align"/>.</summary>
    public static readonly BindableProperty AlignProperty = BindableProperty.Create(nameof(Align), typeof(KlyColumnAlign), typeof(KlyColumnBase<TItem>), KlyColumnAlign.Start);
    /// <summary>Identifies <see cref="Numeric"/>.</summary>
    public static readonly BindableProperty NumericProperty = BindableProperty.Create(nameof(Numeric), typeof(bool), typeof(KlyColumnBase<TItem>), false);
    /// <summary>Identifies <see cref="Mono"/>.</summary>
    public static readonly BindableProperty MonoProperty = BindableProperty.Create(nameof(Mono), typeof(bool), typeof(KlyColumnBase<TItem>), false);
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(string), typeof(KlyColumnBase<TItem>), null);
    /// <summary>Identifies <see cref="FooterTemplate"/>.</summary>
    public static readonly BindableProperty FooterTemplateProperty = BindableProperty.Create(nameof(FooterTemplate), typeof(View), typeof(KlyColumnBase<TItem>), null);

    // Only this assembly derives from it: the table reads the members below.
    internal KlyColumnBase()
    {
    }

    /// <summary>Text of the column header.</summary>
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The alignment of the header, the cells and the footer of the column: <see cref="KlyColumnAlign.Start"/> (the default) or <see cref="KlyColumnAlign.End"/>.</summary>
    public KlyColumnAlign Align { get => (KlyColumnAlign)GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

    /// <summary>Aligns the column to the end, so digits line up on their right edge.</summary>
    public bool Numeric { get => (bool)GetValue(NumericProperty); set => SetValue(NumericProperty, value); }

    /// <summary>Draws the cells in a monospace font.</summary>
    public bool Mono { get => (bool)GetValue(MonoProperty); set => SetValue(MonoProperty, value); }

    /// <summary>Text of the cell of this column in the footer row. The footer row shows when any column has a <see cref="Footer"/> or a <see cref="FooterTemplate"/>.</summary>
    public string? Footer { get => (string?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>A view for the cell of this column in the footer row, instead of <see cref="Footer"/>.</summary>
    public View? FooterTemplate { get => (View?)GetValue(FooterTemplateProperty); set => SetValue(FooterTemplateProperty, value); }

    internal bool HasFooter => Footer is not null || FooterTemplate is not null;

    /// <summary>The header, the cells and the footer sit at the end of the column.</summary>
    internal bool AtEnd => Numeric || Align == KlyColumnAlign.End;

    /// <summary>True when a tap on the header sorts the table by this column.</summary>
    internal abstract bool CanSort { get; }

    /// <summary>The cell of <paramref name="item"/>.</summary>
    internal abstract View Cell(TItem item);

    /// <summary>The items ordered by this column.</summary>
    internal abstract IEnumerable<TItem> OrderItems(IEnumerable<TItem> items, bool ascending);

    /// <summary>Throws when the column cannot show cells, with the reason.</summary>
    internal abstract void Validate();

    /// <summary>Raised when a property of the column changes, so the table shows it again.</summary>
    internal event EventHandler? Changed;

    /// <summary>A cell of text, aligned and set in the font of the column.</summary>
    internal Label TextCell(string text)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 14,
            LineBreakMode = LineBreakMode.NoWrap,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = AtEnd ? TextAlignment.End : TextAlignment.Start,
        };
        if (Mono) label.FontFamily = ChatDataMotion.MonospaceFont;
        label.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
        return label;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
