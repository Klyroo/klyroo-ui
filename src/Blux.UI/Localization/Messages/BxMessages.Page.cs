namespace Blux.UI;

public sealed partial record BxMessages
{
    /// <summary>Accessible name of a <c>BxPageLinks</c> list that has neither a title nor an <c>AriaLabel</c>.</summary>
    public string PageLinks { get; init; } = "Links";

    /// <summary>Accessible name of a <c>BxPageAnchors</c> list that has no <c>AriaLabel</c>.</summary>
    public string PageAnchors { get; init; } = "On this page";

    /// <summary>Hidden text added to the name of a page link that opens in a new tab.</summary>
    public string OpensInNewTab { get; init; } = "(opens in a new tab)";
}
