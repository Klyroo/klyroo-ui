namespace Klyroo.UI.Maui;

/// <summary>
/// The native counterpart of an in-page anchor: a heading is the view whose <c>AutomationId</c> is its id, and following the anchor scrolls a
/// <see cref="ScrollView"/> to that view.
/// </summary>
internal static class PagesScroll
{
    /// <summary>The view with this automation id below <paramref name="root"/>, or <c>null</c>.</summary>
    public static View? Find(Element? root, string id)
    {
        if (root is null) return null;
        if (root is View view && view.AutomationId == id) return view;
        foreach (var child in ((IVisualTreeElement)root).GetVisualChildren())
        {
            if (child is Element element && Find(element, id) is { } found) return found;
        }
        return null;
    }

    /// <summary>The view an address points at: <c>#id</c> or a bare id.</summary>
    public static View? Resolve(ScrollView? scrollView, string? href)
    {
        if (scrollView is null || string.IsNullOrEmpty(href)) return null;
        var id = href[0] == '#' ? href[1..] : href;
        return id.Length == 0 ? null : Find(scrollView.Content, id);
    }

    /// <summary>Scrolls to the view. Without a handler (no device) there is nothing to scroll and the call returns at once.</summary>
    public static void ScrollTo(ScrollView scrollView, View target)
    {
        if (scrollView.Handler is null) return;
        _ = ScrollAsync(scrollView, target);
    }

    /// <summary>Distance from the top of the scrolled content to the top of a view, from the layout positions of the view and its parents.</summary>
    public static double OffsetOf(View target, ScrollView scrollView)
    {
        var y = 0d;
        for (Element? element = target; element is VisualElement visual && element != scrollView; element = element.Parent) y += visual.Y;
        return y;
    }

    /// <summary>The heading being read: the last one at or above <paramref name="line"/>, or the first before any is above it (the rule of the web scroll-spy).</summary>
    public static int ActiveIndex(IReadOnlyList<double> offsets, double line)
    {
        var active = offsets.Count > 0 ? 0 : -1;
        for (var i = 0; i < offsets.Count; i++)
        {
            if (offsets[i] <= line) active = i;
        }
        return active;
    }

    private static async Task ScrollAsync(ScrollView scrollView, View target)
    {
        try
        {
            await scrollView.ScrollToAsync(target, ScrollToPosition.Start, true);
        }
        catch (Exception)
        {
            // The target left the tree while the scroll was starting: there is nowhere to scroll to.
        }
    }
}
