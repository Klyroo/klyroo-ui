namespace Blux.UI.Tools;

public sealed partial record BxToolsMessages
{
    /// <summary>Accessible name of the list of filter tags. {0} is the number of active filters, so a screen reader announces the count.</summary>
    public string FilterTagsLabel { get; init; } = "Filters, {0} active";

    /// <summary>Label of the button that opens the menu of filters to add.</summary>
    public string AddFilter { get; init; } = "Add filter";

    /// <summary>Label and placeholder of the field that narrows the menu of filters to add (shown when the list has many).</summary>
    public string AddFilterSearch { get; init; } = "Search filters";

    /// <summary>Said in the menu of filters to add when every filter is already in use.</summary>
    public string AddFilterNone { get; init; } = "Every filter is in use";

    /// <summary>Label of the button of an editor opened from the add filter menu that goes back to the list of filters.</summary>
    public string AddFilterBack { get; init; } = "Back to the filters";

    /// <summary>Label of the button that takes every filter off.</summary>
    public string ClearFilters { get; init; } = "Clear filters";

    /// <summary>Accessible name of the button of a filter tag that removes the filter. {0} is the name of the filter.</summary>
    public string RemoveFilter { get; init; } = "Remove filter {0}";

    /// <summary>Accessible name of the editor panel of a filter. {0} is the name of the filter.</summary>
    public string EditFilter { get; init; } = "Edit filter {0}";

    /// <summary>Accessible name of the operator picker of a filter editor. {0} is the name of the filter.</summary>
    public string FilterOperatorFor { get; init; } = "Operator for {0}";

    /// <summary>Accessible name of the value field of a filter editor. {0} is the name of the filter.</summary>
    public string FilterValueFor { get; init; } = "Value for {0}";

    /// <summary>Placeholder of the value field of a text filter.</summary>
    public string FilterTextPlaceholder { get; init; } = "Type a value…";

    /// <summary>Label and placeholder of the field that narrows the options of an option filter.</summary>
    public string FilterOptionsSearch { get; init; } = "Search options";

    /// <summary>Said in the editor of an option filter that has no option to list.</summary>
    public string FilterNoOptions { get; init; } = "No options";

    /// <summary>Said in the editor of an option filter when the search matches no option.</summary>
    public string FilterNoOptionsMatch { get; init; } = "No option matches your search";

    /// <summary>Label of the switch that also matches the rows where the field is empty.</summary>
    public string FilterIncludeEmpty { get; init; } = "Include empty values";

    /// <summary>The value of a filter tag that matches only empty rows, after the operator: "Status is empty".</summary>
    public string FilterEmpty { get; init; } = "empty";

    /// <summary>Added to the values of a filter tag that also matches empty rows: "Status is Active or empty".</summary>
    public string FilterOrEmpty { get; init; } = "or empty";

    /// <summary>The "true" choice of a boolean filter.</summary>
    public string FilterYes { get; init; } = "Yes";

    /// <summary>The "false" choice of a boolean filter.</summary>
    public string FilterNo { get; init; } = "No";

    /// <summary>The choice of a boolean filter that takes the filter off.</summary>
    public string FilterAny { get; init; } = "Any";

    /// <summary>Button of a filter editor that applies the changes (the Submit apply mode).</summary>
    public string FilterApply { get; init; } = "Apply";

    /// <summary>Button of a filter editor that drops the changes (the Submit apply mode).</summary>
    public string FilterCancel { get; init; } = "Cancel";

    /// <summary>Said after the values a filter tag shows when it has more. {0} is how many more.</summary>
    public string FilterMore { get; init; } = "+{0}";

    /// <summary>The <c>contains</c> operator of a text filter: "Name contains ann".</summary>
    public string OperatorContains { get; init; } = "contains";

    /// <summary>The <c>is</c> operator. Also what a filter tag says for <c>isAnyOf</c>: "Status is Active, Paused".</summary>
    public string OperatorIs { get; init; } = "is";

    /// <summary>The <c>isAnyOf</c> operator in the operator picker of an option filter.</summary>
    public string OperatorIsAnyOf { get; init; } = "is any of";

    /// <summary>The <c>isNot</c> operator.</summary>
    public string OperatorIsNot { get; init; } = "is not";

    /// <summary>The <c>gt</c> operator of a number filter.</summary>
    public string OperatorGt { get; init; } = "is greater than";

    /// <summary>The <c>gte</c> operator of a number filter.</summary>
    public string OperatorGte { get; init; } = "is at least";

    /// <summary>The <c>lt</c> operator of a number filter.</summary>
    public string OperatorLt { get; init; } = "is less than";

    /// <summary>The <c>lte</c> operator of a number filter.</summary>
    public string OperatorLte { get; init; } = "is at most";

    /// <summary>The <c>between</c> operator of a number or date filter.</summary>
    public string OperatorBetween { get; init; } = "is between";

    /// <summary>The <c>before</c> operator of a date filter.</summary>
    public string OperatorBefore { get; init; } = "is before";

    /// <summary>The <c>after</c> operator of a date filter.</summary>
    public string OperatorAfter { get; init; } = "is after";

    /// <summary>The <c>isEmpty</c> operator.</summary>
    public string OperatorIsEmpty { get; init; } = "is empty";

    /// <summary>The <c>isNotEmpty</c> operator.</summary>
    public string OperatorIsNotEmpty { get; init; } = "is not empty";

    /// <summary>The text of an operator for a filter tag and an operator picker.</summary>
    /// <param name="op">The operator.</param>
    /// <param name="forTag">Whether it is for a tag, where <c>isAnyOf</c> reads "is".</param>
    public string OperatorLabel(Query.FilterOperator op, bool forTag = false) => op switch
    {
        Query.FilterOperator.Contains => OperatorContains,
        Query.FilterOperator.Is => OperatorIs,
        Query.FilterOperator.IsAnyOf => forTag ? OperatorIs : OperatorIsAnyOf,
        Query.FilterOperator.IsNot => OperatorIsNot,
        Query.FilterOperator.Gt => OperatorGt,
        Query.FilterOperator.Gte => OperatorGte,
        Query.FilterOperator.Lt => OperatorLt,
        Query.FilterOperator.Lte => OperatorLte,
        Query.FilterOperator.Between => OperatorBetween,
        Query.FilterOperator.Before => OperatorBefore,
        Query.FilterOperator.After => OperatorAfter,
        Query.FilterOperator.IsEmpty => OperatorIsEmpty,
        Query.FilterOperator.IsNotEmpty => OperatorIsNotEmpty,
        _ => op.ToString(),
    };
}
