namespace Klyroo.UI.Maui;

/// <summary>Shows toasts in the <see cref="KlyToaster"/> of the page.</summary>
public interface IToastService
{
    /// <summary>Shows a toast in the <see cref="KlyToaster"/> of the page.</summary>
    /// <param name="toast">The notification.</param>
    /// <exception cref="InvalidOperationException">No <see cref="KlyToaster"/> is in the tree of a <see cref="KlyOverlayHost"/>.</exception>
    ValueTask Add(KlyToast toast);
}
