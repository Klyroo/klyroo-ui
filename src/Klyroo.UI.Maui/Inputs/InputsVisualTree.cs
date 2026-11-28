namespace Klyroo.UI.Maui;

/// <summary>Walks the views under an element: used by the form to find its form fields, and by a form field to find the controls it labels.</summary>
internal static class InputsVisualTree
{
    /// <summary>Every element under <paramref name="root"/> in document order. <paramref name="enter"/> says whether to look inside an element.</summary>
    public static IEnumerable<Element> Descendants(Element root, Func<Element, bool>? enter = null)
    {
        if (root is not IVisualTreeElement tree) yield break;
        foreach (var child in tree.GetVisualChildren())
        {
            if (child is not Element element) continue;
            yield return element;
            if (enter is null || enter(element))
            {
                foreach (var nested in Descendants(element, enter)) yield return nested;
            }
        }
    }

    /// <summary>The nearest ancestor of a given type, or <c>null</c>.</summary>
    public static T? Ancestor<T>(Element element) where T : class
    {
        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is T found) return found;
        }
        return null;
    }
}
