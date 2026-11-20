using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
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
            // One page key per press: the page handles the keydown, auto repeat included. Enter can enter or leave the editing
            // mode of a field, so it also asks the page whether a text field is being edited, to show or hide the soft keyboard.
            if (e.Action == KeyEventActions.Down)
            {
                if (key == "Enter")
                {
                    webView.EvaluateJavascript("(function(){window.Blux&&Blux.tvKey&&Blux.tvKey('Enter');return window.Blux&&Blux.tvTextEditing&&Blux.tvTextEditing()?1:0;})()", new KeyboardSync(this, webView, 0));
                }
                else
                {
                    webView.EvaluateJavascript($"window.Blux&&Blux.tvKey&&Blux.tvKey('{key}')", null);
                }
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
                // The page answers 1 when it used Back (it left editing, closed an overlay or went back), plus 2 while a text field is still edited.
                webView.EvaluateJavascript("(function(){if(!window.Blux||!Blux.tvBack)return 0;var used=Blux.tvBack();return (used?1:0)+(Blux.tvTextEditing&&Blux.tvTextEditing()?2:0);})()", new KeyboardSync(activity, webView, 1));
            }
            else
            {
                activity.MoveTaskToBack(true);
            }
        }
    }

    /// <summary>
    /// Shows the soft keyboard while the page edits a text field and hides it when editing ends: a script focus alone does not reliably
    /// open it in a web view on a TV. After Back the answer also says whether the page used the key; when it did not (nothing left to
    /// close or go back to) the app moves to the background. A non-dismissible modal counts as used, so it swallows Back.
    /// </summary>
    private sealed class KeyboardSync(MainActivity activity, Android.Webkit.WebView webView, int mode) : Java.Lang.Object, IValueCallback
    {
        public void OnReceiveValue(Java.Lang.Object? value)
        {
            _ = int.TryParse(value?.ToString(), out var answer);
            var editing = mode == 0 ? answer == 1 : (answer & 2) != 0;
            activity.SetKeyboard(webView, editing);
            if (mode == 1 && (answer & 1) == 0)
            {
                activity.MoveTaskToBack(true);
            }
        }
    }

    private bool _keyboardShown;

    private void SetKeyboard(Android.Webkit.WebView webView, bool show)
    {
        if (GetSystemService(InputMethodService) is not InputMethodManager manager)
        {
            return;
        }

        if (show)
        {
            _keyboardShown = true;
            webView.RequestFocus();
            // After the page focused the field: only then has the web view an input connection to type into.
            webView.PostDelayed(() => manager.ShowSoftInput(webView, ShowFlags.Implicit), 100);
        }
        else if (_keyboardShown)
        {
            _keyboardShown = false;
            manager.HideSoftInputFromWindow(webView.WindowToken, HideSoftInputFlags.None);
        }
    }
}
