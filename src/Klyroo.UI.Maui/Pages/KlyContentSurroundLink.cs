namespace Klyroo.UI.Maui;

/// <summary>A page linked by <see cref="KlyContentSurround"/>.</summary>
/// <param name="Title">Title of the page.</param>
/// <param name="Href">Address of the page.</param>
public sealed record KlyContentSurroundLink(string Title, string Href)
{
    /// <summary>A line of context shown under the title.</summary>
    public string? Description { get; init; }
}
