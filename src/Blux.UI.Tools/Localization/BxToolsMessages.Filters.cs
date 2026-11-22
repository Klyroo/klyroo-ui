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

    /// <summary>Label of the button that lists the options an option filter kept back. {0} is how many more there are.</summary>
    public string FilterOptionsMore { get; init; } = "Show {0} more";

    /// <summary>Said in a statically rendered editor of an option filter whose options come from a remote source: the options that are listed are the selected ones and the counted ones, searching the rest runs in the browser.</summary>
    public string FilterOptionsNeedScript { get; init; } = "Searching the other options needs JavaScript.";

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

    /// <summary>Title of the filter panel, and its accessible name.</summary>
    public string FilterPanelTitle { get; init; } = "Filters";

    /// <summary>Said to screen readers after the name of a filter panel section (or of the panel) that has filters on. {0} is how many are active.</summary>
    public string FilterPanelActive { get; init; } = "{0} active";

    /// <summary>The number of rows that match the filters, in the filter panel (a live region). {0} is the count. Written so that it reads right for one row too.</summary>
    public string FilterPanelResults { get; init; } = "Results: {0}";

    /// <summary>Said in the filter panel when the list has no filter to edit.</summary>
    public string FilterPanelNone { get; init; } = "No filters available";

    /// <summary>Button of the filter panel (the Submit apply mode) that drops the changes that were not applied.</summary>
    public string FilterReset { get; init; } = "Reset";

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

    /// <summary>Label of the first field of a range editor (the start of a number or date range).</summary>
    public string FilterFrom { get; init; } = "From";

    /// <summary>Label of the second field of a range editor (the end of a number or date range).</summary>
    public string FilterTo { get; init; } = "To";

    /// <summary>Accessible name of the start field of a range. {0} is the name of the filter.</summary>
    public string FilterFromFor { get; init; } = "{0}, from";

    /// <summary>Accessible name of the end field of a range. {0} is the name of the filter.</summary>
    public string FilterToFor { get; init; } = "{0}, to";

    /// <summary>Accessible name of the time field of a date filter that is exact to the hour or the minute. {0} is the name of the filter.</summary>
    public string FilterTimeFor { get; init; } = "{0}, time";

    /// <summary>Accessible name of the time field of the start of a range. {0} is the name of the filter.</summary>
    public string FilterFromTimeFor { get; init; } = "{0}, from time";

    /// <summary>Accessible name of the time field of the end of a range. {0} is the name of the filter.</summary>
    public string FilterToTimeFor { get; init; } = "{0}, to time";

    /// <summary>Accessible name of the group of preset buttons of a date filter (Today, Last 7 days...). {0} is the name of the filter.</summary>
    public string FilterPresetsFor { get; init; } = "Presets for {0}";

    /// <summary>Accessible name of the calendar of a date filter editor. {0} is the name of the filter.</summary>
    public string FilterCalendarFor { get; init; } = "Calendar for {0}";

    /// <summary>Accessible name of the slider of a number filter editor, which sits next to the number field. {0} is the name of the filter.</summary>
    public string FilterSliderFor { get; init; } = "{0}, slider";

    /// <summary>Hint under the range of a date filter that limits it. {0} is the number of days.</summary>
    public string FilterMaxRangeDays { get; init; } = "At most {0} days";

    /// <summary>Date preset: the current day.</summary>
    public string DatePresetToday { get; init; } = "Today";

    /// <summary>Date preset: the day before today.</summary>
    public string DatePresetYesterday { get; init; } = "Yesterday";

    /// <summary>Date preset: the first day of the current month.</summary>
    public string DatePresetStartOfMonth { get; init; } = "Start of month";

    /// <summary>Date preset: the day seven days before today.</summary>
    public string DatePreset7DaysAgo { get; init; } = "7 days ago";

    /// <summary>Date preset: the first day of the current year.</summary>
    public string DatePresetStartOfYear { get; init; } = "Start of year";

    /// <summary>Range preset: the last seven days, today included.</summary>
    public string DatePresetLast7Days { get; init; } = "Last 7 days";

    /// <summary>Range preset: the last thirty days, today included.</summary>
    public string DatePresetLast30Days { get; init; } = "Last 30 days";

    /// <summary>Range preset: the current month, from its first day to its last.</summary>
    public string DatePresetThisMonth { get; init; } = "This month";

    /// <summary>Range preset: the whole of the previous month.</summary>
    public string DatePresetLastMonth { get; init; } = "Last month";

    /// <summary>Range preset: from the first day of the year to today.</summary>
    public string DatePresetYearToDate { get; init; } = "Year to date";

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
