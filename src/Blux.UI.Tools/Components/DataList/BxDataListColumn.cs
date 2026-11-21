using System.Globalization;
using System.Linq.Expressions;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// A column of <see cref="BxDataListTable{TItem}"/>. Renders nothing itself: it registers with its table, in the order the columns first rendered.
/// What a cell shows comes from one of three places: <see cref="Template"/>, <see cref="Property"/> or <see cref="Field"/>.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class BxDataListColumn<TItem> : ComponentBase, IDisposable
{
    private Expression<Func<TItem, object?>>? _compiledFrom;
    private Func<TItem, object?>? _compiled;

    [CascadingParameter] private BxDataListTable<TItem>? Table { get; set; }

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

    protected override void OnInitialized()
    {
        if (Table is null)
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
        Table.AddColumn(this);
    }

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
    public void Dispose() => Table?.RemoveColumn(this);
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
