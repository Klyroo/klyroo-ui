namespace Klyroo.UI.Maui;

/// <summary>Walks the element tree: how a control finds the dashboard, the sidebar or the page it sits in, with no handler needed.</summary>
internal static class LayoutTree
{
    public static IEnumerable<T> Descendants<T>(Element root)
        where T : class
    {
        foreach (var child in ((IVisualTreeElement)root).GetVisualChildren())
        {
            if (child is T match) yield return match;
            if (child is Element element)
            {
                foreach (var deeper in Descendants<T>(element)) yield return deeper;
            }
        }
    }

    public static T? Ancestor<T>(Element? element)
        where T : class
    {
        for (var parent = element?.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is T match) return match;
        }
        return null;
    }
}
