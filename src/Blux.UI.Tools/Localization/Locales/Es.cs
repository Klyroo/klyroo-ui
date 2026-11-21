namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Español (es). Needs review.</summary>
    public static BxToolsMessages Es { get; } = new BxToolsMessages
        {
            NoResults = "Sin resultados",
            NoResultsDescription = "Prueba a cambiar la búsqueda o los filtros.",
            SortAscending = "Ordenar por {0}, ascendente",
            SortDescending = "Ordenar por {0}, descendente",
            SortClear = "Quitar la ordenación por {0}",
            ResultRange = "Filas {0}–{1}",
            ResultCount = "{0}–{1} de {2}",
            Loading = "Cargando…",
            PageSize = "Filas por página",
            ApplyPageSize = "Aplicar",
            Refresh = "Actualizar",
            ErrorTitle = "No se pudieron cargar los datos",
            ErrorDescription = "Se produjo un error al cargar los datos. Inténtelo de nuevo.",
            Retry = "Reintentar",
            Results = "Resultados",
            PaginationFor = "Paginación, {0}",
            SearchPlaceholder = "Buscar…",
            Search = "Buscar",
            SearchFor = "Buscar en {0}",
            ClearSearch = "Borrar búsqueda",
            NoResultsFor = "Sin resultados para «{0}»",
            NoResultsForDescription = "Revisa la ortografía o prueba con otra búsqueda.",
            NoResultsFiltered = "Ningún resultado coincide con los filtros",
            ResetFilters = "Restablecer filtros",
        };
}
