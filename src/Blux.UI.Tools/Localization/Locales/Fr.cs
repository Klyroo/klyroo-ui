namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    /// <summary>Français. Reviewed.</summary>
    public static BxToolsMessages Fr { get; } = new BxToolsMessages
        {
            NoResults = "Aucun résultat",
            NoResultsDescription = "Essayez de modifier votre recherche ou vos filtres.",
            SortAscending = "Trier par {0}, croissant",
            SortDescending = "Trier par {0}, décroissant",
            SortClear = "Annuler le tri par {0}",
            ResultRange = "Lignes {0}–{1}",
            ResultCount = "{0}–{1} sur {2}",
            Loading = "Chargement…",
            PageSize = "Lignes par page",
            ApplyPageSize = "Appliquer",
            Refresh = "Actualiser",
            ErrorTitle = "Impossible de charger les données",
            ErrorDescription = "Une erreur est survenue pendant le chargement des données. Réessayez.",
            Retry = "Réessayer",
            Results = "Résultats",
            PaginationFor = "Navigation par page, {0}",
        };
}
