namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Deutsch (de). Needs review.</summary>
    public static BxToolsMessages De { get; } = new BxToolsMessages
        {
            NoResults = "Keine Ergebnisse",
            NoResultsDescription = "Ändern Sie Ihre Suche oder Ihre Filter.",
            SortAscending = "Nach {0} sortieren, aufsteigend",
            SortDescending = "Nach {0} sortieren, absteigend",
            SortClear = "Sortierung nach {0} aufheben",
            ResultRange = "Zeilen {0}–{1}",
            ResultCount = "{0}–{1} von {2}",
            Loading = "Wird geladen…",
            PageSize = "Zeilen pro Seite",
            ApplyPageSize = "Übernehmen",
            Refresh = "Aktualisieren",
            ErrorTitle = "Die Daten konnten nicht geladen werden",
            ErrorDescription = "Beim Laden der Daten ist ein Fehler aufgetreten. Versuchen Sie es erneut.",
            Retry = "Erneut versuchen",
        };
}
