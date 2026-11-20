using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Android.Webkit;

namespace Blux.Maui;

// One APK for phones and TVs: the second category adds the app to the Android TV launcher (leanback), see AndroidManifest.xml.
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
[IntentFilter([Intent.ActionMain], Categories = ["android.intent.category.LEANBACK_LAUNCHER"])]
public class MainActivity : MauiAppCompatActivity
{
    private bool IsTv =>
#if BLUX_TV
        true;
#else
        DeviceInfo.Idiom == DeviceIdiom.TV;
#endif

    /// <summary>
    /// Android TV: a web view does not reliably get the D-pad as key events, and Chromium has no legacy focus navigation for it.
    /// The remote's keys are caught here, sent into the page as Blux.tvKey('ArrowLeft'), and Blux moves the focus (spatial navigation).
    /// Back asks the page to close its topmost overlay or go back first (Blux.tvBack) and leaves the app only when there is nothing left.
    /// </summary>
    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e is null || !IsTv || MainPage.Current?.WebView.Handler?.PlatformView is not Android.Webkit.WebView webView)
        {
            return base.DispatchKeyEvent(e);
        }

        var key = e.KeyCode switch
        {
            Keycode.DpadUp => "ArrowUp",
            Keycode.DpadDown => "ArrowDown",
            Keycode.DpadLeft => "ArrowLeft",
            Keycode.DpadRight => "ArrowRight",
            Keycode.DpadCenter or Keycode.Enter or Keycode.NumpadEnter => "Enter",
            _ => null,
        };

        if (key is not null)
        {
            // One page key per press: the page handles the keydown, auto repeat included.
            if (e.Action == KeyEventActions.Down)
            {
                webView.EvaluateJavascript($"window.Blux&&Blux.tvKey&&Blux.tvKey('{key}')", null);
            }
            return true;
        }

        if (e.KeyCode == Keycode.Back)
        {
            if (e.Action == KeyEventActions.Up)
            {
                webView.EvaluateJavascript("window.Blux&&Blux.tvBack?Blux.tvBack():false", new BackResult(this));
            }
            return true;
        }

        return base.DispatchKeyEvent(e);
    }

    /// <summary>The answer of Blux.tvBack(): false means nothing was left to close or go back to, so the app moves to the background.</summary>
    private sealed class BackResult(Activity activity) : Java.Lang.Object, IValueCallback
    {
        public void OnReceiveValue(Java.Lang.Object? value)
        {
            if (value?.ToString() != "true")
            {
                activity.MoveTaskToBack(true);
            }
        }
    }
}
