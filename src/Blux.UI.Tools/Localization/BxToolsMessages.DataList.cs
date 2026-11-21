namespace Blux.UI.Tools;

public sealed partial record BxToolsMessages
{
    /// <summary>Title of the empty state of a data list.</summary>
    public string NoResults { get; init; } = "No results";

    /// <summary>Hint under the title of the empty state of a data list.</summary>
    public string NoResultsDescription { get; init; } = "Try changing your search or your filters.";

    /// <summary>Accessible name of a sort button that sorts a column from the smallest to the largest. {0} is the title of the column.</summary>
    public string SortAscending { get; init; } = "Sort by {0}, ascending";

    /// <summary>Accessible name of a sort button that sorts a column from the largest to the smallest. {0} is the title of the column.</summary>
    public string SortDescending { get; init; } = "Sort by {0}, descending";

    /// <summary>Accessible name of a sort button that removes the sort of a column. {0} is the title of the column.</summary>
    public string SortClear { get; init; } = "Clear the sort by {0}";

    /// <summary>The rows shown when the total is not known. {0} is the first row number, {1} the last.</summary>
    public string ResultRange { get; init; } = "Rows {0}–{1}";

    /// <summary>The rows shown out of the total, announced when they change. {0} is the first row number, {1} the last, {2} the total.</summary>
    public string ResultCount { get; init; } = "{0}–{1} of {2}";

    /// <summary>Said while the first rows of a data list load.</summary>
    public string Loading { get; init; } = "Loading…";

    /// <summary>Label of the page size picker.</summary>
    public string PageSize { get; init; } = "Rows per page";

    /// <summary>Submit button of the page size form of a statically rendered data list.</summary>
    public string ApplyPageSize { get; init; } = "Apply";

    /// <summary>Button that loads the rows again.</summary>
    public string Refresh { get; init; } = "Refresh";

    /// <summary>Title of the error state of a data list.</summary>
    public string ErrorTitle { get; init; } = "Could not load the data";

    /// <summary>Text of the error state of a data list.</summary>
    public string ErrorDescription { get; init; } = "Something went wrong while loading the data. Try again.";

    /// <summary>Button of the error state that sends the request again.</summary>
    public string Retry { get; init; } = "Try again";

    /// <summary>The default accessible name of the table of a data list that has no label of its own.</summary>
    public string Results { get; init; } = "Results";

    /// <summary>The accessible name of the pagination of a labelled data list. {0} is the label of the list.</summary>
    public string PaginationFor { get; init; } = "Pagination, {0}";

    /// <summary>Placeholder of the search field of a data list.</summary>
    public string SearchPlaceholder { get; init; } = "Search…";

    /// <summary>Accessible name of the search field of a data list that has no label. It also labels the button that submits the search of a statically rendered list.</summary>
    public string Search { get; init; } = "Search";

    /// <summary>Accessible name of the search field of a labelled data list. {0} is the label of the list.</summary>
    public string SearchFor { get; init; } = "Search {0}";

    /// <summary>Accessible name of the button that empties the search field, and label of the action of the empty state that clears the search.</summary>
    public string ClearSearch { get; init; } = "Clear search";

    /// <summary>Title of the empty state of a data list when a search matches nothing, and the text its result count announces. {0} is the search text.</summary>
    public string NoResultsFor { get; init; } = "No results for “{0}”";

    /// <summary>Hint under the title of the empty state of a data list when a search matches nothing.</summary>
    public string NoResultsForDescription { get; init; } = "Check the spelling or try a different search.";

    /// <summary>Title of the empty state of a data list when its filters match nothing.</summary>
    public string NoResultsFiltered { get; init; } = "No results match the filters";

    /// <summary>Action of the empty state of a data list that removes its search and its filters.</summary>
    public string ResetFilters { get; init; } = "Reset filters";
}
