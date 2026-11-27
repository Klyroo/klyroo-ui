namespace Klyroo.UI.Maui;

/// <summary>
/// Lets the back button of Android dismiss the top layer of a host. It registers a callback on the back dispatcher of the activity
/// (the same mechanism MAUI uses), which is only enabled while the host has a layer that takes the back button, so navigation works as usual otherwise.
/// Other platforms have no back button: use <see cref="KlyOverlayHost.DismissTop"/> from your own back handling.
/// </summary>
internal sealed class OverlaysBackButton : IDisposable
{
#if ANDROID
    private sealed class Callback(Func<bool> handle) : AndroidX.Activity.OnBackPressedCallback(false)
    {
        public override void HandleOnBackPressed()
        {
            // Not handled (the layers went away meanwhile): give the press back to the next callback.
            if (handle()) return;
            Enabled = false;
            (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.Activity.ComponentActivity)?.OnBackPressedDispatcher.OnBackPressed();
        }
    }

    private Callback? _callback;
#endif

    private OverlaysBackButton()
    {
    }

    /// <summary>Registers the callback for the current activity. Returns null where there is nothing to register (not Android, or no activity yet).</summary>
    /// <param name="handle">Dismisses the top layer and tells whether it took the press.</param>
    public static OverlaysBackButton? Register(Func<bool> handle)
    {
#if ANDROID
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is AndroidX.Activity.ComponentActivity activity)
        {
            var button = new OverlaysBackButton { _callback = new Callback(handle) };
            activity.OnBackPressedDispatcher.AddCallback(button._callback);
            return button;
        }
#endif
        _ = handle;
        return null;
    }

    /// <summary>Takes the back button while <paramref name="active"/> is true.</summary>
    public void SetActive(bool active)
    {
#if ANDROID
        if (_callback is not null) _callback.Enabled = active;
#endif
        _ = active;
    }

    /// <summary>Removes the callback.</summary>
    public void Dispose()
    {
#if ANDROID
        _callback?.Remove();
        _callback = null;
#endif
    }
}
