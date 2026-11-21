namespace Blux.UI.Tools;

public static partial class BxToolsLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Português (Brasil) (pt-BR). Needs review.</summary>
    public static BxToolsMessages PtBr { get; } = new BxToolsMessages
        {
            NoResults = "Nenhum resultado",
            NoResultsDescription = "Tente alterar a busca ou os filtros.",
            SortAscending = "Ordenar por {0}, crescente",
            SortDescending = "Ordenar por {0}, decrescente",
            SortClear = "Remover a ordenação por {0}",
            ResultRange = "Linhas {0}–{1}",
            ResultCount = "{0}–{1} de {2}",
            Loading = "Carregando…",
            PageSize = "Linhas por página",
            ApplyPageSize = "Aplicar",
            Refresh = "Atualizar",
            ErrorTitle = "Não foi possível carregar os dados",
            ErrorDescription = "Ocorreu um erro ao carregar os dados. Tente novamente.",
            Retry = "Tentar novamente",
        };
}
