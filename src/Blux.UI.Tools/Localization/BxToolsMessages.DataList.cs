namespace Blux.UI.Tools;

public sealed partial record BxToolsMessages
{
    /// <summary>Title of the empty state of a data list.</summary>
    public string NoResults { get; init; } = "No results";

    /// <summary>Hint under the title of the empty state of a data list.</summary>
    public string NoResultsDescription { get; init; } = "Try changing your search or your filters.";
}
