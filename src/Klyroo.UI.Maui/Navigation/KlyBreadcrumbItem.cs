namespace Klyroo.UI.Maui;

/// <summary>A page of the trail of a <see cref="KlyBreadcrumb"/>.</summary>
/// <param name="Label">Text of the item.</param>
public sealed record KlyBreadcrumbItem(string Label)
{
    /// <summary>
    /// Makes the item tappable: <see cref="KlyBreadcrumb.ItemClicked"/> is raised for it, and the app navigates to this address or route.
    /// The last item is the current page whether it has one or not, and cannot be tapped.
    /// </summary>
    public string? Href { get; init; }

    /// <summary>Icon shown before the label: a Lucide name such as <see cref="KlyIconNames.Folder"/>.</summary>
    public string? Icon { get; init; }
}
