namespace Klyroo.UI.Maui;

/// <summary>A column of <see cref="KlyTable{TItem}"/> whose cells are views built from the item. It cannot be sorted: use <see cref="KlyColumn{TItem, TValue}"/> for that.</summary>
/// <typeparam name="TItem">The row type of the table.</typeparam>
public sealed class KlyTemplateColumn<TItem> : KlyColumnBase<TItem>
{
    /// <summary>Identifies <see cref="Template"/>.</summary>
    public static readonly BindableProperty TemplateProperty = BindableProperty.Create(nameof(Template), typeof(Func<TItem, View>), typeof(KlyTemplateColumn<TItem>), null);

    /// <summary>Builds the cell of an item.</summary>
    public Func<TItem, View>? Template { get => (Func<TItem, View>?)GetValue(TemplateProperty); set => SetValue(TemplateProperty, value); }

    internal override bool CanSort => false;

    internal override View Cell(TItem item) => Template!(item);

    internal override IEnumerable<TItem> OrderItems(IEnumerable<TItem> items, bool ascending) => items;

    internal override void Validate()
    {
        if (Template is null) throw new InvalidOperationException($"{nameof(KlyTemplateColumn<TItem>)} '{Title}' needs a {nameof(Template)}.");
    }
}
