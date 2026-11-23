using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// Compact summary-row presentation for <see cref="BxDataListTable{TItem}"/>: one scannable row per record with an optional leading visual,
/// a primary title (optionally a link or button), a status badge/pill, secondary details joined with <c> · </c>, selection checkbox and row actions.
/// Shown automatically below the <c>md</c> breakpoint when <see cref="AutoPhone"/> is <see langword="true"/>, when <see cref="DefaultActive"/> is <see langword="true"/>,
/// or when toggled with <see cref="BxDataListViewToggle{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed record DataListRowSummary<TItem>
{
    /// <summary>Primary one-line title of the row. When <see langword="null"/>, uses <c>Schema.GetRowLabel(item)</c> or the first visible column's formatted value.</summary>
    public Func<TItem, string?>? Title { get; init; }

    /// <summary>Optional leading content (a 32 px tile, icon or avatar) shown before the title.</summary>
    public RenderFragment<TItem>? Leading { get; init; }

    /// <summary>Optional status content (a badge or pill) shown at the start of the second line.</summary>
    public RenderFragment<TItem>? Status { get; init; }

    /// <summary>Secondary detail texts joined with <c> · </c> on the second line (blank entries are skipped). When <see langword="null"/>, uses the remaining visible columns.</summary>
    public Func<TItem, IEnumerable<string?>>? Details { get; init; }

    /// <summary>Optional link destination (<c>href</c>) for the title of the row.</summary>
    public Func<TItem, string?>? Href { get; init; }

    /// <summary>Optional click callback when the summary row's title button is activated (interactive only, when <see cref="Href"/> is <see langword="null"/>).</summary>
    public EventCallback<TItem> OnSelect { get; init; }

    /// <summary>Optional custom template for the body of the summary row, replacing <see cref="Title"/>, <see cref="Leading"/>, <see cref="Status"/> and <see cref="Details"/>.</summary>
    public RenderFragment<TItem>? Template { get; init; }

    /// <summary>Starts in summary-row view on every screen size until the user toggles <see cref="BxDataListViewToggle{TItem}"/> (<see langword="false"/> by default).</summary>
    public bool DefaultActive { get; init; }

    /// <summary>Automatically switches to summary-row view below the <c>md</c> breakpoint (768 px) when the user has not explicitly chosen a view (<see langword="true"/> by default).</summary>
    public bool AutoPhone { get; init; } = true;
}
