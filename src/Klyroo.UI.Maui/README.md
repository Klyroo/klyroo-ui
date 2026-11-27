# Klyroo.UI.Maui

Native .NET MAUI controls (no WebView, no Blazor) that follow the Klyroo.UI design tokens: the same colors, sizes and radius as the web components, in light and dark, following the theme of the app.

Klyroo.UI itself runs in a `BlazorWebView` on MAUI. This package is for apps that want real native views.

## Controls

| Control | Built on | Notes |
| :--- | :--- | :--- |
| `KlyButton` | `Button` | `Color`, `Variant` (Solid, Outline, Soft, Subtle, Ghost, Link), `Size`, `Block`. Ripple, focus and accessibility of the platform. No spinner and no underline for `Link`. |
| `KlyBadge` | `Border` + `Label` | `Text`, `Color`, `Variant`, `Size`. |
| `KlyCard` | `Border` | Any content, `Variant` (Outline, Soft, Subtle). |
| `KlyAlert` | `Border` | `Title`, `Description`, `Color`, `Variant`, `Closable`, `Closed`. |
| `KlyAvatar` | `Border` + `Image` | `Source`, or the initials of `Text`; `Alt` for a screen reader. |
| `KlySeparator` | `Border` | `Orientation`, `Size`, `Color`. |
| `KlyInput`, `KlyTextarea`, `KlySelect` | `Entry`, `Editor`, `Picker` | A Klyroo frame (`Variant`, `Size`, `Color`, `Invalid`, `AccessibleName`) around the native control; `Text` and `SelectedItem` are two-way bindable. |
| `KlyCheckbox`, `KlySwitch` | `CheckBox`, `Switch` | With a label and a description; tapping the text toggles. |

## Use

```csharp
using Klyroo.UI.Maui;

var save = new KlyButton { Text = "Save", Variant = ButtonVariant.Solid, Color = KlyColor.Primary };
var email = new KlyInput { Placeholder = "Email", Keyboard = Keyboard.Email, AccessibleName = "Email" };
```

Rebrand at startup, before the first control is created:

```csharp
KlyTheme.Register(KlyColor.Primary, new KlyColorSet(
    Fill: KlyThemeColor.FromHex("#2563eb", "#60a5fa"),
    Text: KlyThemeColor.FromHex("#1d4ed8", "#93c5fd"),
    Contrast: KlyThemeColor.FromHex("#ffffff", "#0b0f14")));
```

The colors are bound to the app theme: `Application.Current.UserAppTheme` switches every control live.

## Build

This project is not part of `Klyroo.UI.slnx`: it needs the MAUI workload. It targets `net10.0` (the unit tests run on it) and `net10.0-android`, plus iOS and Mac Catalyst outside Linux and Windows on Windows. A machine whose MAUI workload is the .NET 11 one builds with `-p:KlyrooMauiTfm=net11.0`.

```bash
dotnet workload install maui-android
dotnet test --project tests/Klyroo.UI.Maui.Tests
dotnet build samples/Klyroo.Maui.Native -t:Run   # an emulator or a device must be running
```

## Status

Verified on an Android 16 emulator (light and dark, typing, toggles, focus ring). Not run on iOS, Mac Catalyst or Windows.
