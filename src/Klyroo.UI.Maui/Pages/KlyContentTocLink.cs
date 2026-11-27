namespace Klyroo.UI.Maui;

/// <summary>A heading of the page, listed by <see cref="KlyContentToc"/>.</summary>
/// <param name="Id">The <c>AutomationId</c> of the heading view. The link scrolls to it and the scroll-spy follows it.</param>
/// <param name="Text">The text of the link.</param>
public sealed record KlyContentTocLink(string Id, string Text)
{
    /// <summary>The heading level, 2 for an <c>h2</c>. Deeper levels (up to 5) are indented.</summary>
    public int Level { get; init; } = 2;
}
