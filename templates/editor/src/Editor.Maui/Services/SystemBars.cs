using Editor.Shared.Services;
#if ANDROID
using Microsoft.Maui.Platform;
#endif

namespace Editor.Maui.Services;

/// <summary>
/// Dresses the system bars in the color of the page. On Android 15 and later the app is drawn edge to edge: the bars are transparent and the
/// page behind them is the MAUI page, so its background color is what shows there, and the bar icons must contrast with it.
/// </summary>
public sealed class SystemBars : ISystemBars
{
    // --blux-bg of Blux: the page background in dark and light mode.
    private static readonly Color Dark = Color.FromArgb("#0a0b0c");
    private static readonly Color Light = Color.FromArgb("#f5f5f2");

    public void Apply(bool dark) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (MainPage.Current is not { } page) return;
        page.BackgroundColor = dark ? Dark : Light;
#if ANDROID
        if (Platform.CurrentActivity?.Window is { } window)
        {
            window.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable((dark ? Dark : Light).ToPlatform()));
            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (controller is not null)
            {
                // Light icons on a dark page, dark icons on a light one.
                controller.AppearanceLightStatusBars = !dark;
                controller.AppearanceLightNavigationBars = !dark;
            }
        }
#endif
    });
}
