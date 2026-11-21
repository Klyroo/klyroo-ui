namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Italiano (it). Needs review.</summary>
    public static BxToolsMessages It { get; } = new BxToolsMessages
        {
            NoResults = "Nessun risultato",
            NoResultsDescription = "Prova a modificare la ricerca o i filtri.",
            SortAscending = "Ordina per {0}, crescente",
            SortDescending = "Ordina per {0}, decrescente",
            SortClear = "Rimuovi l'ordinamento per {0}",
            ResultRange = "Righe {0}–{1}",
            ResultCount = "{0}–{1} di {2}",
            Loading = "Caricamento…",
            PageSize = "Righe per pagina",
            ApplyPageSize = "Applica",
            Refresh = "Aggiorna",
            ErrorTitle = "Impossibile caricare i dati",
            ErrorDescription = "Si è verificato un errore durante il caricamento dei dati. Riprova.",
            Retry = "Riprova",
            Results = "Risultati",
            PaginationFor = "Paginazione, {0}",
        };
}
