using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Activity;
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
    /// Back is not a key event here: see OnCreate.
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

        return base.DispatchKeyEvent(e);
    }

    /// <summary>
    /// Back, on a phone (button or gesture) and on a TV remote. Since Android 13 it reaches the app through the back dispatcher, not as a
    /// key event, and with no callback the system leaves the app. The page is asked first (Blux.tvBack closes the topmost overlay,
    /// else goes back in the history); the app only goes to the background when the page had nothing left to close.
    /// </summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        OnBackPressedDispatcher.AddCallback(this, new BackCallback(this));
    }

    private sealed class BackCallback(MainActivity activity) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            if (MainPage.Current?.WebView.Handler?.PlatformView is Android.Webkit.WebView webView)
            {
                webView.EvaluateJavascript("window.Blux&&Blux.tvBack?Blux.tvBack():false", new BackResult(activity));
            }
            else
            {
                activity.MoveTaskToBack(true);
            }
        }
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
