namespace Klyroo.UI.Maui;

/// <summary>What the shared code cannot say: details of the native views, applied when a control gets its handler.</summary>
internal static class PlatformTweaks
{
    /// <summary>The frame of a field draws the border and the padding, so the native text view must draw neither.</summary>
    public static void RemoveNativeFieldChrome(View control)
    {
#if ANDROID
        if (control.Handler?.PlatformView is Android.Widget.EditText editText)
        {
            // An EditText draws an underline from its background tint, and carries a padding of its own.
            editText.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            editText.SetPadding(0, 0, 0, 0);
        }
#endif
    }
}
