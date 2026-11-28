namespace Klyroo.UI.Maui;

/// <summary>A page <see cref="KlyContentSearch"/> can find, and optionally the headings inside it.</summary>
/// <param name="Title">Title of the page.</param>
/// <param name="Href">Address of the page.</param>
public sealed record KlyContentSearchPage(string Title, string Href)
{
    /// <summary>A line of context shown under the title.</summary>
    public string? Description { get; init; }

    /// <summary>The section the page is listed under in the results. Pages without one have no heading.</summary>
    public string? Group { get; init; }

    /// <summary>A built-in Lucide icon name, shown before the page. Defaults to a file; an empty string shows none.</summary>
    public string? Icon { get; init; }

    /// <summary>Extra words the search matches, that are not shown.</summary>
    public string? Keywords { get; init; }

    /// <summary>Headings of the page, searchable on their own and linking to <c>Href#Id</c>.</summary>
    public IReadOnlyList<KlyContentSearchHeading>? Headings { get; init; }
}
