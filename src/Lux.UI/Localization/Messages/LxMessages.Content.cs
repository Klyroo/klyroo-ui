namespace Lux.UI;

public sealed partial record LxMessages
{
    /// <summary>Accessible name of a <c>LxContentNavigation</c> that has no <c>AriaLabel</c>.</summary>
    public string ContentNavigation { get; init; } = "Documentation";

    /// <summary>Title of a <c>LxContentToc</c> that has no <c>Title</c>.</summary>
    public string ContentToc { get; init; } = "On this page";

    /// <summary>Accessible name of the previous and next page links.</summary>
    public string ContentSurround { get; init; } = "Previous and next pages";

    /// <summary>Caption of the link to the previous page.</summary>
    public string ContentPrevious { get; init; } = "Previous";

    /// <summary>Caption of the link to the next page.</summary>
    public string ContentNext { get; init; } = "Next";

    /// <summary>Accessible name of the documentation search dialog and its search field.</summary>
    public string ContentSearch { get; init; } = "Search documentation";

    /// <summary>Placeholder of the documentation search field.</summary>
    public string ContentSearchPlaceholder { get; init; } = "Search the documentation...";

    /// <summary>Text of the button that opens the documentation search.</summary>
    public string ContentSearchButton { get; init; } = "Search";

    /// <summary>Shown while the search index of a <c>LxContentSearch</c> with an <c>IndexUrl</c> is being fetched.</summary>
    public string ContentSearchLoading { get; init; } = "Loading the search index...";

    /// <summary>Shown when the search index could not be fetched.</summary>
    public string ContentSearchError { get; init; } = "The search could not be loaded.";

    /// <summary>Text of the button that fetches the search index again after an error.</summary>
    public string ContentSearchRetry { get; init; } = "Try again";
}
