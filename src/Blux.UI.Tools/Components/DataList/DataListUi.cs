using Blux.UI.Theming;

namespace Blux.UI.Tools;

/// <summary>Per-instance class overrides of the slots of the data list parts (<see cref="BxDataList{TItem}"/>, <see cref="BxDataListTable{TItem}"/>, <see cref="BxDataListPagination{TItem}"/>...).</summary>
public sealed record DataListUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Toolbar { get; init; }
    public string? Search { get; init; }
    public string? SearchField { get; init; }
    public string? SearchInput { get; init; }
    public string? SearchClear { get; init; }
    public string? SearchClearIcon { get; init; }
    public string? SearchSubmit { get; init; }
    public string? ResultCount { get; init; }
    public string? Refresh { get; init; }
    public string? RefreshIcon { get; init; }
    public string? TableRoot { get; init; }
    public string? Table { get; init; }
    public string? Caption { get; init; }
    public string? Thead { get; init; }
    public string? Tr { get; init; }
    public string? Th { get; init; }
    public string? SortButton { get; init; }
    public string? SortIcon { get; init; }
    public string? Tbody { get; init; }
    public string? Td { get; init; }
    public string? Empty { get; init; }
    public string? Skeleton { get; init; }
    public string? Pagination { get; init; }
    public string? PageSize { get; init; }
    public string? PageSizeLabel { get; init; }
    public string? PageSizeSelect { get; init; }
    public string? PageSizeForm { get; init; }
    public string? Error { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "toolbar" => Toolbar,
        "search" => Search,
        "searchField" => SearchField,
        "searchInput" => SearchInput,
        "searchClear" => SearchClear,
        "searchClearIcon" => SearchClearIcon,
        "searchSubmit" => SearchSubmit,
        "resultCount" => ResultCount,
        "refresh" => Refresh,
        "refreshIcon" => RefreshIcon,
        "tableRoot" => TableRoot,
        "table" => Table,
        "caption" => Caption,
        "thead" => Thead,
        "tr" => Tr,
        "th" => Th,
        "sortButton" => SortButton,
        "sortIcon" => SortIcon,
        "tbody" => Tbody,
        "td" => Td,
        "empty" => Empty,
        "skeleton" => Skeleton,
        "pagination" => Pagination,
        "pageSize" => PageSize,
        "pageSizeLabel" => PageSizeLabel,
        "pageSizeSelect" => PageSizeSelect,
        "pageSizeForm" => PageSizeForm,
        "error" => Error,
        _ => null,
    };
}
