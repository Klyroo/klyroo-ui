namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Nederlands (nl). Needs review.</summary>
    public static BxToolsMessages Nl { get; } = new BxToolsMessages
        {
            NoResults = "Geen resultaten",
            NoResultsDescription = "Pas je zoekopdracht of filters aan.",
            SortAscending = "Sorteren op {0}, oplopend",
            SortDescending = "Sorteren op {0}, aflopend",
            SortClear = "Sortering op {0} wissen",
            ResultRange = "Rijen {0}–{1}",
            ResultCount = "{0}–{1} van {2}",
            Loading = "Laden…",
            PageSize = "Rijen per pagina",
            ApplyPageSize = "Toepassen",
            Refresh = "Vernieuwen",
            ErrorTitle = "De gegevens konden niet worden geladen",
            ErrorDescription = "Er is iets misgegaan bij het laden van de gegevens. Probeer het opnieuw.",
            Retry = "Opnieuw proberen",
            Results = "Resultaten",
            PaginationFor = "Paginering, {0}",
        };
}
