using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>The alignment of a column of a <see cref="KlyTable{TItem}"/>.</summary>
public enum KlyColumnAlign
{
    /// <summary>At the start of the cell.</summary>
    Start,
    /// <summary>At the end of the cell.</summary>
    End,
}

/// <summary>
/// What every column of a <see cref="KlyTable{TItem}"/> has: a title, cell classes, and the registration with its table.
/// Columns render nothing themselves. Use <see cref="KlyColumn{TItem, TValue}"/> to show a value of the item or <see cref="KlyTemplateColumn{TItem}"/> for custom cells.
/// </summary>
/// <typeparam name="TItem">The row type of the table.</typeparam>
public abstract class KlyColumnBase<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] private KlyTable<TItem>? Table { get; set; }

    // Only this assembly derives from it: the table reads the members below.
    internal KlyColumnBase()
    {
    }

    /// <summary>Text of the column header.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>Classes added to the cells of this column.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The alignment of the header, the cells and the footer of the column: <see cref="KlyColumnAlign.Start"/> (the default) or <see cref="KlyColumnAlign.End"/>.</summary>
    [Parameter] public KlyColumnAlign Align { get; set; }

    /// <summary>Aligns the column to the end and uses tabular figures, so digits line up.</summary>
    [Parameter] public bool Numeric { get; set; }

    /// <summary>Renders the cells in a monospace font.</summary>
    [Parameter] public bool Mono { get; set; }

    /// <summary>Text of the cell of this column in the footer row. The footer row shows when any column has a <see cref="Footer"/> or a <see cref="FooterTemplate"/>.</summary>
    [Parameter] public string? Footer { get; set; }

    /// <summary>Markup of the cell of this column in the footer row, instead of <see cref="Footer"/>.</summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    internal bool HasFooter => Footer is not null || FooterTemplate is not null;

    /// <summary>True when a click on the header sorts the table by this column.</summary>
    internal abstract bool CanSort { get; }

    /// <summary>The cell of <paramref name="item"/>.</summary>
    internal abstract RenderFragment Cell(TItem item);

    /// <summary>The items ordered by this column.</summary>
    internal abstract IEnumerable<TItem> OrderItems(IEnumerable<TItem> items, bool ascending);

    /// <summary>Throws when the column cannot render, with the reason.</summary>
    private protected abstract void Validate();

    // Runs each time the table re-renders its columns, in markup order.
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Table is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside a {nameof(KlyTable<TItem>)}.");
        }
        Validate();
        Table.AddColumn(this);
    }

    /// <inheritdoc />
    protected override bool ShouldRender() => false;

    /// <summary>Removes the column from its table.</summary>
    public void Dispose() => Table?.RemoveColumn(this);
}

/// <summary>
/// A column of <see cref="KlyTable{TItem}"/> that shows a value of each item. It is also the sort key when <see cref="Sortable"/>, compared with the default comparer of <typeparamref name="TValue"/>.
/// Give a <see cref="Template"/> to show markup in the cells while <see cref="Field"/> still sorts them; for a column that has no value to sort by use <see cref="KlyTemplateColumn{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">The row type of the table.</typeparam>
/// <typeparam name="TValue">The type of the value shown: Blazor infers it from <see cref="Field"/>.</typeparam>
public sealed class KlyColumn<TItem, TValue> : KlyColumnBase<TItem>
{
    /// <summary>Value shown in the cell, and the sort key when <see cref="Sortable"/>. Dates and numbers are shown with the culture of the page (<c>null</c> shows nothing).</summary>
    [Parameter, EditorRequired] public Func<TItem, TValue> Field { get; set; } = default!;

    /// <summary>Cell content built from the item, shown instead of the text of <see cref="Field"/>, which still orders the column.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }

    /// <summary>Sorts the table by <see cref="Field"/> when the header is clicked.</summary>
    [Parameter] public bool Sortable { get; set; }

    internal override bool CanSort => Sortable;

    internal override RenderFragment Cell(TItem item)
    {
        if (Template is not null) return Template(item);
        var value = Field(item);
        return builder => builder.AddContent(0, value);
    }

    internal override IEnumerable<TItem> OrderItems(IEnumerable<TItem> items, bool ascending) =>
        ascending ? items.OrderBy(Field, Comparer<TValue>.Default) : items.OrderByDescending(Field, Comparer<TValue>.Default);

    private protected override void Validate()
    {
        if (Field is null)
        {
            throw new InvalidOperationException($"{nameof(KlyColumn<TItem, TValue>)} '{Title}' needs a {nameof(Field)}.");
        }
    }
}

/// <summary>A column of <see cref="KlyTable{TItem}"/> whose cells are markup built from the item. It cannot be sorted: use <see cref="KlyColumn{TItem, TValue}"/> for that.</summary>
/// <typeparam name="TItem">The row type of the table.</typeparam>
public sealed class KlyTemplateColumn<TItem> : KlyColumnBase<TItem>
{
    /// <summary>The content of the cell of an item.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> Template { get; set; } = default!;

    internal override bool CanSort => false;

    internal override RenderFragment Cell(TItem item) => Template(item);

    internal override IEnumerable<TItem> OrderItems(IEnumerable<TItem> items, bool ascending) => items;

    private protected override void Validate()
    {
        if (Template is null)
        {
            throw new InvalidOperationException($"{nameof(KlyTemplateColumn<TItem>)} '{Title}' needs a {nameof(Template)}.");
        }
    }
}
