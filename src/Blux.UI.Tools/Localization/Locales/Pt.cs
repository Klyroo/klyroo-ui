namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Português (pt). Needs review.</summary>
    public static BxToolsMessages Pt { get; } = new BxToolsMessages
        {
            NoResults = "Sem resultados",
            NoResultsDescription = "Experimente alterar a pesquisa ou os filtros.",
            SortAscending = "Ordenar por {0}, ascendente",
            SortDescending = "Ordenar por {0}, descendente",
            SortClear = "Remover a ordenação por {0}",
            ResultRange = "Linhas {0}–{1}",
            ResultCount = "{0}–{1} de {2}",
            Loading = "A carregar…",
            PageSize = "Linhas por página",
            ApplyPageSize = "Aplicar",
            Refresh = "Atualizar",
            ErrorTitle = "Não foi possível carregar os dados",
            ErrorDescription = "Ocorreu um erro ao carregar os dados. Tente novamente.",
            Retry = "Tentar novamente",
            Results = "Resultados",
            PaginationFor = "Paginação, {0}",
            SearchPlaceholder = "Pesquisar…",
            Search = "Pesquisar",
            SearchFor = "Pesquisar em {0}",
            ClearSearch = "Limpar pesquisa",
            NoResultsFor = "Sem resultados para “{0}”",
            NoResultsForDescription = "Verifique a ortografia ou tente outra pesquisa.",
            NoResultsFiltered = "Nenhum resultado corresponde aos filtros",
            ResetFilters = "Repor filtros",
        };
}
