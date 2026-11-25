using Klyroo.UI;

namespace Dashboard.Shared.Components;

/// <summary>
/// A toast needs JavaScript, which a page that is still being rendered by the server (a form posted before the page became interactive) does not have yet. The
/// forms of the app keep their message in a <c>role="status"</c> paragraph that is always there, and add the toast only once the page is interactive.
/// </summary>
public static class Feedback
{
    public static async Task ToastAsync(this IToastService toasts, bool interactive, KlyToast toast)
    {
        if (interactive) await toasts.Add(toast);
    }
}
