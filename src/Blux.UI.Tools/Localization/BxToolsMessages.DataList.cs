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

    /// <summary>Rows on a cursor page out of the total, when the start offset is not known (a static link). {0} is the row count of the page, {1} the total.</summary>
    public string ResultCountLoaded { get; init; } = "{0} of {1}";

    /// <summary>Rows on a cursor page when neither the start offset nor the total is known. {0} is the row count of the page.</summary>
    public string ResultCountShown { get; init; } = "{0} shown";

    /// <summary>Error shown inline by the infinite loader when fetching the next cursor page failed.</summary>
    public string LoadMoreError { get; init; } = "Could not load more results.";

    /// <summary>Link shown by a statically rendered cursor-paged list to go back to the first page.</summary>
    public string BackToStart { get; init; } = "Back to start";

    /// <summary>Label of the column configurator button and heading.</summary>
    public string Columns { get; init; } = "Columns";

    /// <summary>Visible column count in the column panel. {0} is visible count, {1} is total count.</summary>
    public string ColumnsVisible { get; init; } = "{0} of {1} visible";

    /// <summary>Placeholder and accessible name of the search field inside the column panel.</summary>
    public string ColumnsSearch { get; init; } = "Search columns…";

    /// <summary>Button in the column panel that restores default column visibility, order, pinning and widths.</summary>
    public string ColumnsReset { get; init; } = "Reset columns";

    /// <summary>Accessible name of the button moving a column earlier in the order. {0} is the column title.</summary>
    public string ColumnMoveUp { get; init; } = "Move {0} up";

    /// <summary>Accessible name of the button moving a column later in the order. {0} is the column title.</summary>
    public string ColumnMoveDown { get; init; } = "Move {0} down";

    /// <summary>Accessible name of the button pinning a column to the start edge. {0} is the column title.</summary>
    public string ColumnPinStart { get; init; } = "Pin {0} to start";

    /// <summary>Accessible name of the button pinning a column to the end edge. {0} is the column title.</summary>
    public string ColumnPinEnd { get; init; } = "Pin {0} to end";

    /// <summary>Accessible name of the button unpinning a column. {0} is the column title.</summary>
    public string ColumnUnpin { get; init; } = "Unpin {0}";

    /// <summary>Accessible name of the column resize handle. {0} is the column title.</summary>
    public string ColumnResize { get; init; } = "Resize {0}";

    /// <summary>Button of the column panel that shows every column.</summary>
    public string ColumnsShowAll { get; init; } = "Show all";

    /// <summary>Button of the column panel that hides every column that is not required.</summary>
    public string ColumnsHideAll { get; init; } = "Hide all";

    /// <summary>Announced after Show all.</summary>
    public string ColumnsAllShown { get; init; } = "All columns shown";

    /// <summary>Announced after Hide all.</summary>
    public string ColumnsAllHidden { get; init; } = "Optional columns hidden";

    /// <summary>Hint of the column panel shown when only one column is left, which cannot be hidden.</summary>
    public string ColumnsKeepOne { get; init; } = "At least one column stays visible.";

    /// <summary>Announced after a column was shown. {0} is the column title.</summary>
    public string ColumnShown { get; init; } = "{0} shown";

    /// <summary>Announced after a column was hidden. {0} is the column title.</summary>
    public string ColumnHidden { get; init; } = "{0} hidden";

    /// <summary>Announced after a column moved. {0} is the column title, {1} its new position and {2} the number of columns.</summary>
    public string ColumnMoved { get; init; } = "{0} moved to position {1} of {2}";

    /// <summary>Announced after a column was pinned to the start edge. {0} is the column title.</summary>
    public string ColumnPinnedToStart { get; init; } = "{0} pinned to the start";

    /// <summary>Announced after a column was pinned to the end edge. {0} is the column title.</summary>
    public string ColumnPinnedToEnd { get; init; } = "{0} pinned to the end";

    /// <summary>Announced after a column was unpinned. {0} is the column title.</summary>
    public string ColumnUnpinned { get; init; } = "{0} unpinned";

    /// <summary>Announced after the columns went back to their defaults.</summary>
    public string ColumnsWereReset { get; init; } = "Columns reset";

    /// <summary>Value text of a column resize handle. {0} is the width in pixels.</summary>
    public string ColumnWidthPixels { get; init; } = "{0} pixels";

    /// <summary>Announced after a column was resized. {0} is the column title and {1} the width in pixels.</summary>
    public string ColumnWidthChanged { get; init; } = "{0}: {1} pixels wide";

    /// <summary>Announced after a column went back to its default width. {0} is the column title.</summary>
    public string ColumnWidthReset { get; init; } = "{0}: default width";

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
