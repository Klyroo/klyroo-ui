using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A column of <see cref="KlyTable{TItem}"/>. Renders nothing itself: it registers with its table.</summary>
public sealed class KlyColumn<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] private KlyTable<TItem>? Table { get; set; }

    /// <summary>Text of the column header.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>Value shown in the cell, and the sort key when <see cref="Sortable"/>.</summary>
    [Parameter] public Func<TItem, object?>? Field { get; set; }

    /// <summary>Cell content, instead of <see cref="Field"/>'s value.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    /// <summary>Sorts by <see cref="Field"/>; ignored without one.</summary>
    [Parameter] public bool Sortable { get; set; }

    /// <summary>Classes added to the cells of this column.</summary>
    [Parameter] public string? Class { get; set; }

    // Runs each time the table re-renders its columns, in markup order.
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Table is null)
        {
            throw new InvalidOperationException($"{nameof(KlyColumn<TItem>)} must be placed inside a {nameof(KlyTable<TItem>)}.");
        }
        if (Field is null && Template is null)
        {
            throw new InvalidOperationException($"{nameof(KlyColumn<TItem>)} '{Title}' needs a {nameof(Field)} or a {nameof(Template)}.");
        }
        Table.AddColumn(this);
    }

    /// <inheritdoc />
    protected override bool ShouldRender() => false;

    /// <summary>Removes the column from its table.</summary>
    public void Dispose() => Table?.RemoveColumn(this);
}
