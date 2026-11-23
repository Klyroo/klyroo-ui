using System.Globalization;
using System.Linq.Expressions;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// A column of <see cref="BxDataListTable{TItem}"/>. Renders nothing itself: it registers with its list (<see cref="DataListContext{TItem}.Columns"/>).
/// What a cell shows comes from one of three places: <see cref="Template"/>, <see cref="Property"/> or <see cref="Field"/>.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class BxDataListColumn<TItem> : ComponentBase, IDisposable
{
    private Expression<Func<TItem, object?>>? _compiledFrom;
    private Func<TItem, object?>? _compiled;

    // The list: columns register with it, not with a table, so a columns panel can see them.
    [CascadingParameter] private DataListContext<TItem>? Context { get; set; }

    /// <summary>The key of a field of the schema: the cell shows its value and the header sorts by it when the schema allows. With <see cref="Template"/> it names the field the header sorts by.</summary>
    [Parameter] public string? Field { get; set; }

    /// <summary>
    /// The value of a cell as an expression, <c>@(e =&gt; e.Name)</c>:the cell shows it and, without a schema on the list, the column describes the schema: its key is the member path in camel case (<c>name</c>, <c>department.name</c>)
    /// unless <see cref="Key"/> says otherwise, and it sorts (give <c>Sortable="false"</c> to turn that off). With a schema the key must be one of its fields to sort.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object?>>? Property { get; set; }

    /// <summary>The key of the column: of its field when it has a <see cref="Template"/> or a <see cref="Property"/> that is not a plain member path.</summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>The content of a cell, instead of the value of the field.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }

    /// <summary>The header. Without it, the key written as words (<c>firstName</c> becomes "First name").</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The content of the header cell, instead of <see cref="Title"/> and the sort control: a select-all checkbox, or an accessible name that is not shown (an actions column).
    /// It renders inside the list, so a part that derives from <see cref="DataListPartBase{TItem}"/> works in it. A column that sorts but has this template has no sort control.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Where the text of the column sits. Numbers read best at the end.</summary>
    [Parameter] public DataListAlign Align { get; set; }

    /// <summary>A format string for the value of a cell (<c>"C2"</c>, <c>"d"</c>), used with the culture of the page. Ignored with a <see cref="Template"/>.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>
    /// Whether the header sorts. By default the schema decides (a field marked sortable); <see langword="false"/> keeps a sortable field from sorting here. A column cannot sort by what the schema does not allow.
    /// </summary>
    [Parameter] public bool? Sortable { get; set; }

    /// <summary>Classes added to the header and the cells of this column.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Where the column sits among the others: columns are sorted by this number, then by the order in which they first rendered (the order of the markup for columns that
    /// exist from the start). A column that an <c>@if</c> adds later renders after the others unless it has an <c>Order</c>; <see cref="Visible"/> avoids the question.
    /// </summary>
    [Parameter] public int Order { get; set; }

    /// <summary>
    /// Whether the table shows the column (true by default). A hidden column stays in <see cref="DataListContext{TItem}.Columns"/> and keeps its place in the order, which
    /// is why this is the way to show and hide columns; an <c>@if</c> around a column works too, but the column then registers at its place in the markup only while it exists.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>When <see langword="true"/>, the column cannot be hidden: <see cref="BxDataListColumnPanel{TItem}"/> does not list it and <see cref="DataListContext{TItem}.SetColumnVisible"/> ignores it. It cannot be reordered or pinned from the panel either.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>
    /// Default horizontal pin edge (<see cref="DataListPin.Start"/> or <see cref="DataListPin.End"/>). Users can override it through the column panel. A pinned column sticks from the <c>sm</c> breakpoint up (below it
    /// pinned columns scroll with the table but keep their place in the order), after the selection column and before the row actions column, and needs a width: without <see cref="Width"/> it gets 150 px.
    /// </summary>
    [Parameter] public DataListPin Pinned { get; set; }

    /// <summary>
    /// Default width in pixels, clamped to [<see cref="MinWidth"/>, <see cref="MaxWidth"/>]. <see langword="null"/> lets the browser size the column to its content. A column with a width keeps it whatever its content
    /// (a longer text is cut with an ellipsis); when every column has one the table is as wide as their sum, otherwise the columns without a width share the room that is left. Rendered on the <c>&lt;col&gt;</c> and on every cell,
    /// so a static render has the width at the first paint. The user's width (the column resize, kept in the preferences) wins.
    /// </summary>
    [Parameter] public int? Width { get; set; }

    /// <summary>Minimum width in pixels when resized (64 by default).</summary>
    [Parameter] public int MinWidth { get; set; } = DataListColumnPreferences.MinAllowedWidth;

    /// <summary>Maximum width in pixels when resized (900 by default).</summary>
    [Parameter] public int MaxWidth { get; set; } = DataListColumnPreferences.MaxAllowedWidth;

    /// <summary>Whether the user can resize the column by dragging its header handle or using the keyboard (true by default). Only when <see cref="BxDataListTable{TItem}.Resizable"/> is on.</summary>
    [Parameter] public bool Resizable { get; set; } = true;

    /// <summary>Built-in aggregation shown for this column in the table's summary footer (<c>&lt;tfoot&gt;</c>): <see cref="DataListSummaryKind.Sum"/>, <see cref="DataListSummaryKind.Average"/>, <see cref="DataListSummaryKind.Count"/>, <see cref="DataListSummaryKind.Min"/> or <see cref="DataListSummaryKind.Max"/>.</summary>
    [Parameter] public DataListSummaryKind Summary { get; set; }

    /// <summary>Format string applied to the summary footer value of this column. When <see langword="null"/>, falls back to <see cref="Format"/> (or the localized row count for <see cref="DataListSummaryKind.Count"/>).</summary>
    [Parameter] public string? SummaryFormat { get; set; }

    /// <summary>Custom synchronous aggregation calculator for this column in the table's summary footer (<c>&lt;tfoot&gt;</c>), receiving the rows of the active <see cref="DataListSummaryScope"/>.</summary>
    [Parameter] public Func<IReadOnlyList<TItem>, object?>? SummaryValue { get; set; }

    /// <summary>Custom template for rendering the summary footer cell (<c>&lt;tfoot&gt;</c>) of this column.</summary>
    [Parameter] public RenderFragment<DataListSummaryCellContext<TItem>>? SummaryTemplate { get; set; }

    /// <summary>Placeholder shape rendered inside this column's cells while the initial rows load (<see cref="DataListSkeletonKind.Text"/> by default).</summary>
    [Parameter] public DataListSkeletonKind Skeleton { get; set; } = DataListSkeletonKind.Text;

    /// <summary>Overrides <see cref="BxDataListTable{TItem}.HeaderMenu"/> for this column: <see langword="true"/> shows the header actions menu (sort, pin, hide), <see langword="false"/> omits it.</summary>
    [Parameter] public bool? HeaderMenu { get; set; }

    internal bool HasSummary => Summary != DataListSummaryKind.None || SummaryValue is not null || SummaryTemplate is not null;

    /// <summary>The key of the column: <see cref="Key"/>, <see cref="Field"/> or the member path of <see cref="Property"/> in camel case.</summary>
    public string? EffectiveKey => ResolvedKey;

    /// <summary>The header of the column: <see cref="Title"/>, or its key written as words.</summary>
    public string EffectiveTitle => ResolvedTitle;

    internal string? ResolvedKey
    {
        get
        {
            if (!string.IsNullOrEmpty(Key)) return Key;
            if (!string.IsNullOrEmpty(Field)) return Field;
            return Property is null ? null : FieldAccessor.Create(Property).DefaultKey;
        }
    }

    internal string ResolvedTitle => Title ?? DataListText.Humanize(ResolvedKey ?? "");

    internal bool HasValue => Template is not null || Property is not null || Field is not null;

    protected override void OnParametersSet()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(BxDataListColumn<TItem>)} must be placed inside a {nameof(BxDataListTable<TItem>)}.");
        }
        if (Field is null && Property is null && Template is null)
        {
            throw new InvalidOperationException($"{nameof(BxDataListColumn<TItem>)} needs a {nameof(Field)}, a {nameof(Property)} or a {nameof(Template)}.");
        }
        if (Template is not null && ResolvedKey is null && (Sortable ?? false))
        {
            throw new InvalidOperationException($"{nameof(BxDataListColumn<TItem>)} '{Title}' sorts but has no {nameof(Field)} or {nameof(Key)}: a template has no value to sort by.");
        }
        _registeredWith = Context;
        _index ??= Context.NextColumnIndex();
        Context.AddColumn(this);
    }

    private DataListContext<TItem>? _registeredWith;
    private int? _index;

    internal int RegistrationIndex => _index ?? int.MaxValue;

    // The table reads the values of its columns when it renders, after them: a column never renders by itself.
    protected override bool ShouldRender() => false;

    /// <summary>The text of a cell for a value: the format of the column, with the culture of the page.</summary>
    internal string? FormatValue(object? value, CultureInfo culture) => value switch
    {
        null => null,
        IFormattable formattable when !string.IsNullOrEmpty(Format) => formattable.ToString(Format, culture),
        IFormattable formattable => formattable.ToString(null, culture),
        _ => value.ToString(),
    };

    /// <summary>The text of a summary footer cell for an aggregated value.</summary>
    internal string? FormatSummaryValue(object? value, CultureInfo culture, BxToolsMessages messages)
    {
        if (value is null) return null;
        var fmt = !string.IsNullOrEmpty(SummaryFormat) ? SummaryFormat : Format;
        if (Summary == DataListSummaryKind.Count && string.IsNullOrEmpty(fmt) && value is int count)
        {
            return string.Format(culture, count == 1 ? messages.SummaryRowOne : messages.SummaryRowOther, count);
        }
        return value switch
        {
            IFormattable formattable when !string.IsNullOrEmpty(fmt) => formattable.ToString(fmt, culture),
            IFormattable formattable => formattable.ToString(null, culture),
            _ => value.ToString(),
        };
    }

    /// <summary>The value of the column for a row, from its <see cref="Property"/> or from the field of the schema; <see langword="null"/> when it has neither.</summary>
    internal object? ReadValue(TItem item, DataListSchema<TItem>? schema)
    {
        if (Property is not null)
        {
            if (!ReferenceEquals(_compiledFrom, Property))
            {
                _compiled = Property.Compile();
                _compiledFrom = Property;
            }
            try
            {
                return _compiled!(item);
            }
            catch (NullReferenceException)
            {
                // A null along the path (e.Department.Name with no department): an empty cell, like the in-memory executor reads it.
                return null;
            }
        }
        return Field is not null && schema is not null ? schema.ReadField(item, Field) : null;
    }

    /// <summary>Takes the column out of its table.</summary>
    public void Dispose() => _registeredWith?.RemoveColumn(this);
}

/// <summary>Small text helpers of the data list.</summary>
internal static class DataListText
{
    /// <summary>A key written as words: <c>firstName</c> becomes "First name", <c>department.name</c> becomes "Name" (the last segment).</summary>
    public static string Humanize(string key)
    {
        if (key.Length == 0) return "";
        var segment = key[(key.LastIndexOf('.') + 1)..];
        var words = new System.Text.StringBuilder(segment.Length + 4);
        for (var i = 0; i < segment.Length; i++)
        {
            var c = segment[i];
            if (c is '_' or '-')
            {
                words.Append(' ');
                continue;
            }
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(segment[i - 1]) && segment[i - 1] is not ('_' or '-')) words.Append(' ');
            words.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }
        return words.ToString();
    }
}

/// <summary>Reading a field of the schema for a row.</summary>
internal static class DataListSchemaReader
{
    /// <summary>The value of a field of the schema for a row, null-safe along its path; the elements of a collection field joined by commas; <see langword="null"/> for an unknown key.</summary>
    public static object? ReadField<T>(this DataListSchema<T> schema, T item, string key)
    {
        if (!schema.TryGetField(key, out var field)) return null;
        return field.IsCollection
            ? string.Join(", ", field.Values(item).Where(v => v is not null))
            : field.Getter(item);
    }
}
