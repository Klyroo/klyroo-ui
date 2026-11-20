namespace Blux.UI;

public sealed partial record BxMessages
{
    /// <summary>Accessible name of a <c>BxContentNavigation</c> that has no <c>AriaLabel</c>.</summary>
    public string ContentNavigation { get; init; } = "Documentation";

    /// <summary>Title of a <c>BxContentToc</c> that has no <c>Title</c>.</summary>
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
}
