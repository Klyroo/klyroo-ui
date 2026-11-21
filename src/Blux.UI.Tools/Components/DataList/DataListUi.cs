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
    public string? Filters { get; init; }
    public string? FilterTags { get; init; }
    public string? FilterTag { get; init; }
    public string? FilterTagButton { get; init; }
    public string? FilterTagLabel { get; init; }
    public string? FilterTagOperator { get; init; }
    public string? FilterTagValue { get; init; }
    public string? FilterTagRemove { get; init; }
    public string? FilterTagRemoveIcon { get; init; }
    public string? AddFilter { get; init; }
    public string? ClearFilters { get; init; }
    public string? FilterPopover { get; init; }
    public string? FilterEditor { get; init; }
    public string? FilterEditorTitle { get; init; }
    public string? FilterField { get; init; }
    public string? FilterFieldLabel { get; init; }
    public string? FilterOperator { get; init; }
    public string? FilterInput { get; init; }
    public string? FilterOptions { get; init; }
    public string? FilterOption { get; init; }
    public string? FilterOptionLink { get; init; }
    public string? FilterOptionCount { get; init; }
    public string? FilterOptionCheck { get; init; }
    public string? FilterEmpty { get; init; }
    public string? FilterActions { get; init; }
    public string? FilterChoices { get; init; }
    public string? FilterMenu { get; init; }
    public string? FilterMenuItem { get; init; }
    public string? FilterMenuHeader { get; init; }
    public string? FilterBack { get; init; }
    public string? FilterSection { get; init; }
    public string? FilterRange { get; init; }
    public string? FilterRangeSlider { get; init; }
    public string? FilterPresets { get; init; }
    public string? FilterHint { get; init; }
    public string? FilterPopoverWide { get; init; }

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
        "filters" => Filters,
        "filterTags" => FilterTags,
        "filterTag" => FilterTag,
        "filterTagButton" => FilterTagButton,
        "filterTagLabel" => FilterTagLabel,
        "filterTagOperator" => FilterTagOperator,
        "filterTagValue" => FilterTagValue,
        "filterTagRemove" => FilterTagRemove,
        "filterTagRemoveIcon" => FilterTagRemoveIcon,
        "addFilter" => AddFilter,
        "clearFilters" => ClearFilters,
        "filterPopover" => FilterPopover,
        "filterEditor" => FilterEditor,
        "filterEditorTitle" => FilterEditorTitle,
        "filterField" => FilterField,
        "filterFieldLabel" => FilterFieldLabel,
        "filterOperator" => FilterOperator,
        "filterInput" => FilterInput,
        "filterOptions" => FilterOptions,
        "filterOption" => FilterOption,
        "filterOptionLink" => FilterOptionLink,
        "filterOptionCount" => FilterOptionCount,
        "filterOptionCheck" => FilterOptionCheck,
        "filterEmpty" => FilterEmpty,
        "filterActions" => FilterActions,
        "filterChoices" => FilterChoices,
        "filterMenu" => FilterMenu,
        "filterMenuItem" => FilterMenuItem,
        "filterMenuHeader" => FilterMenuHeader,
        "filterBack" => FilterBack,
        "filterSection" => FilterSection,
        "filterRange" => FilterRange,
        "filterRangeSlider" => FilterRangeSlider,
        "filterPresets" => FilterPresets,
        "filterHint" => FilterHint,
        "filterPopoverWide" => FilterPopoverWide,
        _ => null,
    };
}
