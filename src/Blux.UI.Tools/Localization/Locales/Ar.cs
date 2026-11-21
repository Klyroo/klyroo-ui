namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>العربية (ar). Needs review.</summary>
    public static BxToolsMessages Ar { get; } = new BxToolsMessages
        {
            NoResults = "لا توجد نتائج",
            NoResultsDescription = "جرّب تغيير البحث أو عوامل التصفية.",
            SortAscending = "ترتيب حسب {0} تصاعديًا",
            SortDescending = "ترتيب حسب {0} تنازليًا",
            SortClear = "إلغاء الترتيب حسب {0}",
            ResultRange = "الصفوف {0}–{1}",
            ResultCount = "{0}–{1} من {2}",
            Loading = "جارٍ التحميل…",
            PageSize = "الصفوف في كل صفحة",
            ApplyPageSize = "تطبيق",
            Refresh = "تحديث",
            ErrorTitle = "تعذّر تحميل البيانات",
            ErrorDescription = "حدث خطأ أثناء تحميل البيانات. حاول مرة أخرى.",
            Retry = "إعادة المحاولة",
            Results = "النتائج",
            PaginationFor = "ترقيم الصفحات، {0}",
        };
}
