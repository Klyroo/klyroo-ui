namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Русский (ru). Needs review.</summary>
    public static BxToolsMessages Ru { get; } = new BxToolsMessages
        {
            NoResults = "Нет результатов",
            NoResultsDescription = "Попробуйте изменить поиск или фильтры.",
            SortAscending = "Сортировать по {0} по возрастанию",
            SortDescending = "Сортировать по {0} по убыванию",
            SortClear = "Сбросить сортировку по {0}",
            ResultRange = "Строки {0}–{1}",
            ResultCount = "{0}–{1} из {2}",
            Loading = "Загрузка…",
            PageSize = "Строк на странице",
            ApplyPageSize = "Применить",
            Refresh = "Обновить",
            ErrorTitle = "Не удалось загрузить данные",
            ErrorDescription = "При загрузке данных произошла ошибка. Повторите попытку.",
            Retry = "Повторить",
        };
}
