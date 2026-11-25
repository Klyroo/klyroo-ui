namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of a <c>KlyPageLinks</c> list that has neither a title nor an <c>AriaLabel</c>.</summary>
    public string PageLinks { get; init; } = "Links";

    /// <summary>Accessible name of a <c>KlyPageAnchors</c> list that has no <c>AriaLabel</c>.</summary>
    public string PageAnchors { get; init; } = "On this page";

    /// <summary>Hidden text added to the name of a page link that opens in a new tab.</summary>
    public string OpensInNewTab { get; init; } = "(opens in a new tab)";
}
