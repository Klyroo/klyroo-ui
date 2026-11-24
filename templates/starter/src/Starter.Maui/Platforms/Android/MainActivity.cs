using Android.App;
using Android.Content.PM;

namespace Starter.Maui;

// Phones and tablets. The Back button and gesture use the default of MAUI: they leave the app. The Blux sample (samples/Lux.Maui) shows how to
// forward Back to the page (close an overlay, go back in the history) and how to support the remote control of an Android TV.
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : MauiAppCompatActivity
{
}
