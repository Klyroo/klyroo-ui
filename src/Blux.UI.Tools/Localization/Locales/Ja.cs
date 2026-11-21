namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>日本語 (ja). Needs review.</summary>
    public static BxToolsMessages Ja { get; } = new BxToolsMessages
        {
            NoResults = "結果がありません",
            NoResultsDescription = "検索条件またはフィルターを変更してみてください。",
            SortAscending = "{0}で並べ替え（昇順）",
            SortDescending = "{0}で並べ替え（降順）",
            SortClear = "{0}の並べ替えを解除",
            ResultRange = "{0}–{1}行目",
            ResultCount = "{2}件中 {0}–{1}件",
            Loading = "読み込み中…",
            PageSize = "1ページあたりの行数",
            ApplyPageSize = "適用",
            Refresh = "更新",
            ErrorTitle = "データを読み込めませんでした",
            ErrorDescription = "データの読み込み中にエラーが発生しました。もう一度お試しください。",
            Retry = "再試行",
            Results = "結果",
            PaginationFor = "ページ送り、{0}",
        };
}
