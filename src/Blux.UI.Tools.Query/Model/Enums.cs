using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>The 13 comparison operators of a filter rule. Each filter kind allows a subset (see <see cref="DataListFilter"/>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterOperator>))]
public enum FilterOperator
{
    /// <summary>Text contains the value, ignoring case.</summary>
    Contains,
    /// <summary>The field equals the value.</summary>
    Is,
    /// <summary>The field equals any value of a list.</summary>
    IsAnyOf,
    /// <summary>Negation of <see cref="IsAnyOf"/>: the field equals none of the values.</summary>
    IsNot,
    /// <summary>Greater than.</summary>
    Gt,
    /// <summary>Greater than or equal.</summary>
    Gte,
    /// <summary>Less than.</summary>
    Lt,
    /// <summary>Less than or equal.</summary>
    Lte,
    /// <summary>Between two bounds, both inclusive; either bound may be missing.</summary>
    Between,
    /// <summary>Strictly before a date (a day value means before the start of that day).</summary>
    Before,
    /// <summary>Strictly after a date (a day value means after the end of that day).</summary>
    After,
    /// <summary>The field is null or an empty string.</summary>
    IsEmpty,
    /// <summary>The field is neither null nor an empty string.</summary>
    IsNotEmpty,
}

/// <summary>The five kinds of filter a field can have.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterKind>))]
public enum FilterKind
{
    /// <summary>Free text on a string field.</summary>
    Text,
    /// <summary>A pick from a set of values (strings, enums, numbers, booleans, ids).</summary>
    Option,
    /// <summary>True or false.</summary>
    Boolean,
    /// <summary>A number or a range of numbers.</summary>
    Number,
    /// <summary>A date or a range of dates.</summary>
    Date,
}

/// <summary>How the children of a filter group combine.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterCombinator>))]
public enum FilterCombinator
{
    /// <summary>Every child must match.</summary>
    And,
    /// <summary>At least one child must match.</summary>
    Or,
}

/// <summary>The precision a date filter edits; a day is carried as a <see cref="FilterValue.Date"/>, anything finer as a <see cref="FilterValue.Moment"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DateGranularity>))]
public enum DateGranularity
{
    /// <summary>A calendar day.</summary>
    Day,
    /// <summary>An hour.</summary>
    Hour,
    /// <summary>A minute.</summary>
    Minute,
    /// <summary>A second.</summary>
    Second,
}

/// <summary>How a data list shows its rows.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DataListLayout>))]
public enum DataListLayout
{
    /// <summary>One row per record (URL value <c>table</c>).</summary>
    Table,
    /// <summary>A grid of cards (URL value <c>grid</c>).</summary>
    Grid,
}

/// <summary>Whether a facet counts with its own filter applied.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FacetMode>))]
public enum FacetMode
{
    /// <summary>The facet's own filter is removed before counting, so the other options stay visible (default).</summary>
    ExcludeSelf,
    /// <summary>The facet's own filter stays applied.</summary>
    IncludeSelf,
}
