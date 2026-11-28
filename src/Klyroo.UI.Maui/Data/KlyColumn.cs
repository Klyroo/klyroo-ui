using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A column of <see cref="KlyTable{TItem}"/> that shows a value of each item. It is also the sort key when <see cref="Sortable"/>, compared with the default comparer of <typeparamref name="TValue"/>.
/// Give a <see cref="Template"/> to show a view in the cells while <see cref="Field"/> still sorts them; for a column that has no value to sort by use <see cref="KlyTemplateColumn{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">The row type of the table.</typeparam>
/// <typeparam name="TValue">The type of the value shown.</typeparam>
public sealed class KlyColumn<TItem, TValue> : KlyColumnBase<TItem>
{
    /// <summary>Identifies <see cref="Field"/>.</summary>
    public static readonly BindableProperty FieldProperty = BindableProperty.Create(nameof(Field), typeof(Func<TItem, TValue>), typeof(KlyColumn<TItem, TValue>), null);
    /// <summary>Identifies <see cref="Template"/>.</summary>
    public static readonly BindableProperty TemplateProperty = BindableProperty.Create(nameof(Template), typeof(Func<TItem, View>), typeof(KlyColumn<TItem, TValue>), null);
    /// <summary>Identifies <see cref="Sortable"/>.</summary>
    public static readonly BindableProperty SortableProperty = BindableProperty.Create(nameof(Sortable), typeof(bool), typeof(KlyColumn<TItem, TValue>), false);

    /// <summary>Value shown in the cell, and the sort key when <see cref="Sortable"/>. Dates and numbers are shown with the current culture (<c>null</c> shows nothing).</summary>
    public Func<TItem, TValue>? Field { get => (Func<TItem, TValue>?)GetValue(FieldProperty); set => SetValue(FieldProperty, value); }

    /// <summary>Builds the cell from the item, shown instead of the text of <see cref="Field"/>, which still orders the column.</summary>
    public Func<TItem, View>? Template { get => (Func<TItem, View>?)GetValue(TemplateProperty); set => SetValue(TemplateProperty, value); }

    /// <summary>Sorts the table by <see cref="Field"/> when the header is tapped.</summary>
    public bool Sortable { get => (bool)GetValue(SortableProperty); set => SetValue(SortableProperty, value); }

    internal override bool CanSort => Sortable;

    internal override View Cell(TItem item)
    {
        if (Template is { } template) return template(item);
        var value = Field!(item);
        var text = value is IFormattable formattable ? formattable.ToString(null, CultureInfo.CurrentCulture) : value?.ToString();
        return TextCell(text ?? string.Empty);
    }

    internal override IEnumerable<TItem> OrderItems(IEnumerable<TItem> items, bool ascending) =>
        ascending ? items.OrderBy(Field!, Comparer<TValue>.Default) : items.OrderByDescending(Field!, Comparer<TValue>.Default);

    internal override void Validate()
    {
        if (Field is null) throw new InvalidOperationException($"{nameof(KlyColumn<TItem, TValue>)} '{Title}' needs a {nameof(Field)}.");
    }
}
