using Microsoft.AspNetCore.Components;

namespace Blux.UI;

/// <summary>Decides whether an address is the current page, shared by every component that marks an active link.</summary>
internal static class LinkMatcher
{
    /// <summary>
    /// True when <paramref name="href"/> resolves to the current path, or, unless <paramref name="exact"/>, to one of its
    /// ancestors. Comparison ignores case, the query string and the fragment, resolves relative addresses against the
    /// app base path, and never treats the root of the app as an ancestor.
    /// </summary>
    public static bool Matches(NavigationManager navigation, string href, bool exact)
    {
        var baseUri = new Uri(navigation.BaseUri);
        if (!Uri.TryCreate(baseUri, href, out var target) || !Uri.TryCreate(navigation.Uri, UriKind.Absolute, out var current))
        {
            return false;
        }
        if (!string.Equals(target.GetLeftPart(UriPartial.Authority), current.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var targetPath = Normalize(target.AbsolutePath);
        var currentPath = Normalize(current.AbsolutePath);
        if (string.Equals(targetPath, currentPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        // A link to the root of the app would otherwise be active everywhere.
        return !exact
            && !string.Equals(targetPath, Normalize(baseUri.AbsolutePath), StringComparison.OrdinalIgnoreCase)
            && currentPath.StartsWith(targetPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) => Uri.UnescapeDataString(path).TrimEnd('/');
}
