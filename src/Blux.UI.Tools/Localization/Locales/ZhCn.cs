namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>简体中文 (zh-Hans). Needs review.</summary>
    public static BxToolsMessages ZhCn { get; } = new BxToolsMessages
        {
            NoResults = "没有结果",
            NoResultsDescription = "请尝试更改搜索条件或筛选器。",
            SortAscending = "按{0}升序排序",
            SortDescending = "按{0}降序排序",
            SortClear = "取消按{0}排序",
            ResultRange = "第 {0}–{1} 行",
            ResultCount = "第 {0}–{1} 条，共 {2} 条",
            Loading = "正在加载…",
            PageSize = "每页行数",
            ApplyPageSize = "应用",
            Refresh = "刷新",
            ErrorTitle = "无法加载数据",
            ErrorDescription = "加载数据时出错，请重试。",
            Retry = "重试",
            Results = "结果",
            PaginationFor = "分页，{0}",
            SearchPlaceholder = "搜索…",
            Search = "搜索",
            SearchFor = "搜索{0}",
            ClearSearch = "清除搜索",
            NoResultsFor = "没有找到与“{0}”相关的结果",
            NoResultsForDescription = "请检查拼写或尝试其他搜索词。",
            NoResultsFiltered = "没有符合筛选条件的结果",
            ResetFilters = "重置筛选",
        };
}
