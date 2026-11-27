namespace Klyroo.UI.Maui;

/// <summary>A feature of a <see cref="KlyPageSection"/>.</summary>
/// <param name="Title">Title of the feature.</param>
/// <param name="Description">Text under the title.</param>
public sealed record PageFeatureItem(string Title, string? Description = null)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) shown beside the title.</summary>
    public string? Icon { get; init; }

    /// <summary>Address the feature links to: reported when the feature is tapped.</summary>
    public string? Href { get; init; }
}
