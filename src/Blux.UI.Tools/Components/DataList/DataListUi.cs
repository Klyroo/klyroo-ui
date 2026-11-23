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
    public string? FilterPanel { get; init; }
    public string? FilterPanelHeader { get; init; }
    public string? FilterPanelTitle { get; init; }
    public string? FilterPanelCount { get; init; }
    public string? FilterPanelDescription { get; init; }
    public string? FilterPanelSection { get; init; }
    public string? FilterPanelTrigger { get; init; }
    public string? FilterPanelContent { get; init; }
    public string? FilterPanelEditor { get; init; }
    public string? FilterPanelFooter { get; init; }
    public string? FilterPanelEmpty { get; init; }
    public string? FilterNativeInput { get; init; }
    public string? FilterNativeLabel { get; init; }
    public string? FilterPanelResults { get; init; }
    public string? SelectCell { get; init; }
    public string? RowSelected { get; init; }
    public string? SelectionActions { get; init; }
    public string? SelectionBar { get; init; }
    public string? SelectionCount { get; init; }
    public string? SelectionLive { get; init; }
    public string? SelectionScope { get; init; }
    public string? SelectionGroup { get; init; }
    public string? ActionsDropdown { get; init; }
    public string? RowActionsCell { get; init; }
    public string? RowActions { get; init; }
    public string? RowActionsInline { get; init; }
    public string? RowActionsMenu { get; init; }
    public string? RowActionsPhone { get; init; }
    public string? SheetList { get; init; }
    public string? SheetDivider { get; init; }
    public string? SheetEntry { get; init; }
    public string? SheetHeading { get; init; }
    public string? SheetItem { get; init; }
    public string? SheetReason { get; init; }
    public string? SheetIcon { get; init; }
    public string? InfiniteLoader { get; init; }
    public string? LoadMoreError { get; init; }
    public string? LoadMoreErrorText { get; init; }
    public string? CursorReset { get; init; }
    public string? InfiniteEnd { get; init; }
    public string? InfiniteStatus { get; init; }
    public string? ThPinnedStart { get; init; }
    public string? ThPinnedEnd { get; init; }
    public string? TdPinnedStart { get; init; }
    public string? TdPinnedEnd { get; init; }
    public string? ColResize { get; init; }
    public string? ColumnsPanel { get; init; }
    public string? ColumnsPopover { get; init; }
    public string? ColumnsContent { get; init; }
    public string? ColumnsHeader { get; init; }
    public string? ColumnsTitle { get; init; }
    public string? ColumnsCount { get; init; }
    public string? ColumnsList { get; init; }
    public string? ColumnsItem { get; init; }
    public string? ColumnsLabel { get; init; }
    public string? ColumnsActions { get; init; }
    public string? ColumnsIconBtn { get; init; }
    public string? ColumnsIconBtnActive { get; init; }
    public string? ColSized { get; init; }
    public string? ColumnsAll { get; init; }
    public string? ColumnsHint { get; init; }
    public string? ColumnsGrip { get; init; }
    public string? ColumnsLive { get; init; }
    public string? ColumnsPinGroup { get; init; }
    public string? ColumnsSearch { get; init; }

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
        "filterPanel" => FilterPanel,
        "filterPanelHeader" => FilterPanelHeader,
        "filterPanelTitle" => FilterPanelTitle,
        "filterPanelCount" => FilterPanelCount,
        "filterPanelDescription" => FilterPanelDescription,
        "filterPanelSection" => FilterPanelSection,
        "filterPanelTrigger" => FilterPanelTrigger,
        "filterPanelContent" => FilterPanelContent,
        "filterPanelEditor" => FilterPanelEditor,
        "filterPanelFooter" => FilterPanelFooter,
        "filterPanelEmpty" => FilterPanelEmpty,
        "filterNativeInput" => FilterNativeInput,
        "filterNativeLabel" => FilterNativeLabel,
        "filterPanelResults" => FilterPanelResults,
        "selectCell" => SelectCell,
        "rowSelected" => RowSelected,
        "selectionActions" => SelectionActions,
        "selectionBar" => SelectionBar,
        "selectionCount" => SelectionCount,
        "selectionLive" => SelectionLive,
        "selectionScope" => SelectionScope,
        "selectionGroup" => SelectionGroup,
        "actionsDropdown" => ActionsDropdown,
        "rowActionsCell" => RowActionsCell,
        "rowActions" => RowActions,
        "rowActionsInline" => RowActionsInline,
        "rowActionsMenu" => RowActionsMenu,
        "rowActionsPhone" => RowActionsPhone,
        "sheetList" => SheetList,
        "sheetDivider" => SheetDivider,
        "sheetEntry" => SheetEntry,
        "sheetHeading" => SheetHeading,
        "sheetItem" => SheetItem,
        "sheetReason" => SheetReason,
        "sheetIcon" => SheetIcon,
        "infiniteLoader" => InfiniteLoader,
        "loadMoreError" => LoadMoreError,
        "loadMoreErrorText" => LoadMoreErrorText,
        "cursorReset" => CursorReset,
        "infiniteEnd" => InfiniteEnd,
        "infiniteStatus" => InfiniteStatus,
        "thPinnedStart" => ThPinnedStart,
        "thPinnedEnd" => ThPinnedEnd,
        "tdPinnedStart" => TdPinnedStart,
        "tdPinnedEnd" => TdPinnedEnd,
        "colResize" => ColResize,
        "columnsPanel" => ColumnsPanel,
        "columnsPopover" => ColumnsPopover,
        "columnsContent" => ColumnsContent,
        "columnsHeader" => ColumnsHeader,
        "columnsTitle" => ColumnsTitle,
        "columnsCount" => ColumnsCount,
        "columnsList" => ColumnsList,
        "columnsItem" => ColumnsItem,
        "columnsLabel" => ColumnsLabel,
        "columnsActions" => ColumnsActions,
        "columnsIconBtn" => ColumnsIconBtn,
        "columnsIconBtnActive" => ColumnsIconBtnActive,
        "colSized" => ColSized,
        "columnsAll" => ColumnsAll,
        "columnsHint" => ColumnsHint,
        "columnsGrip" => ColumnsGrip,
        "columnsLive" => ColumnsLive,
        "columnsPinGroup" => ColumnsPinGroup,
        "columnsSearch" => ColumnsSearch,
        _ => null,
    };
}
