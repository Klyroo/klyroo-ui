using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme ContentNavigation { get; } = Themes.ContentNavigation();
    public ComponentTheme ContentToc { get; } = Themes.ContentToc();
    public ComponentTheme ContentSurround { get; } = Themes.ContentSurround();
    public ComponentTheme ContentSearchButton { get; } = Themes.ContentSearchButton();
}

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of a <c>BxContentNavigation</c> that has no <c>AriaLabel</c>.</summary>
    public string ContentNavigation { get; set; } = "Documentation";

    /// <summary>Title of a <c>BxContentToc</c> that has no <c>Title</c>.</summary>
    public string ContentToc { get; set; } = "On this page";

    /// <summary>Accessible name of the previous and next page links.</summary>
    public string ContentSurround { get; set; } = "Previous and next pages";

    /// <summary>Caption of the link to the previous page.</summary>
    public string ContentPrevious { get; set; } = "Previous";

    /// <summary>Caption of the link to the next page.</summary>
    public string ContentNext { get; set; } = "Next";

    /// <summary>Accessible name of the documentation search dialog and its search field.</summary>
    public string ContentSearch { get; set; } = "Search documentation";

    /// <summary>Placeholder of the documentation search field.</summary>
    public string ContentSearchPlaceholder { get; set; } = "Search the documentation...";

    /// <summary>Text of the button that opens the documentation search.</summary>
    public string ContentSearchButton { get; set; } = "Search";
}
