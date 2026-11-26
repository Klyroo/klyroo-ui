# Platforms

Where Klyroo.UI runs, what each environment needs, and what does not work there. Everything below is read from the
project files and the code of this repository.

## Render modes

Every component works in static server rendering, Interactive Server, WebAssembly and Auto, with the same markup. Use
the standard Blazor render mode attributes: nothing Klyroo-specific is needed.

| Mode | What works | What does not |
| :--- | :--- | :--- |
| Static server rendering | Form controls render real fields and post. Overlays, tabs, accordions, popovers, tooltips, the color mode controls and `KlyLocaleSelect` work, because their behavior is in the browser. | Anything that changes the page from a .NET handler. A `KlyButton` with `OnClick` and the sort buttons of `KlyTable` are disabled until the page is interactive. `IToastService` needs JavaScript interop. |
| Interactive Server | Everything. Bound values and handlers run on the server. | Cookies cannot be read after the prerender: there is no HTTP request in a circuit. |
| WebAssembly | Everything. Handlers run in the browser. | `Klyroo.UI.Server` and `Klyroo.UI.Images.Local` are server packages and do not belong in a client project. |
| Auto | Everything. The first render is on the server and later ones on WebAssembly. | Configuration that differs between the two projects changes the appearance when the runtime switches. |

What decides the behavior is who owns the state. Opening, closing, focus, keyboard navigation and positioning run in
custom elements in the browser, so an overlay without `@bind-Open` never calls .NET. A value you bind with
`@bind-Value` or `@bind-Open` is reported to .NET with a custom event. See [Concepts](concepts.md#what-runs-in-the-browser-and-what-runs-in-net).

Requirements for every mode:

- Call `AddKlyrooUi` with the same options in the server project and in the `.Client` project of a WebAssembly or Auto
  app. A shared extension method in the client project keeps both in sync.
- Put the head components in `App.razor`: `KlyColorModeScript`, `KlyThemeStyle`, `KlyCultureMeta` and `KlyFonts` as you
  need them. They render the same in static rendering, prerendering and every interactive mode.
- Declare `KlyrooFont` items in the server project, never in a WebAssembly project (`KLYFONT010`).
- A `KlyApp` or `KlyTheme` does not reach a component that has its own `@rendermode`, because cascading values do not
  cross a render mode boundary. Place one inside that island, or use `AddKlyrooUi`.
- Call `ApplyServerCultureAsync` in the client `Program.cs` so the WebAssembly runtime formats with the culture the
  server used. See [Localize the app](guides.md#localize-the-app).

Features that read the request, such as the time zone cookie of a data list in a sister package, need
`Klyroo.UI.Server` in the server project and `builder.Services.AddKlyrooServer()`. Without it the feature falls back to a
default and does not fail. The package depends on exactly the version of `Klyroo.UI` it was built with.

Sister packages can hand data from the prerender to the interactive render (the results of a Klyroo.Table list, the
resources of a Klyroo.Dashboard). The browser sends all of it back to the server in the message that starts an
Interactive Server circuit, which SignalR refuses past 32 KB by default, and the circuit then fails to start. So the
packages of a page share one budget, `KlyrooOptions.PersistedStateMaxBytes` (28 KB by default, counted as sent, with the
escaping and the two base64 encodings Blazor adds), through the scoped `KlyPersistedStateBudget`. What does not fit is
not handed over: that list or resource loads again once in the interactive render. Raise the budget only together with
the `MaximumReceiveMessageSize` of the hub.

## .NET MAUI Blazor Hybrid

The same components run in a `BlazorWebView`, so one set of pages can ship as a web app and as a native app. A web
view is always interactive, has no prerendering, no `HttpContext` and no server to read cookies or to write the page
head. The components that depend on a server degrade to what a web view can do.

The sample in [`samples/Klyroo.Maui`](../samples/Klyroo.Maui) has a shared Razor class library with the pages, a MAUI
head and a web head that run the same pages. Its MAUI project targets Android on every host, iOS and Mac Catalyst
except on Linux, and Windows on a Windows host.

To use Klyroo.UI in a MAUI app:

1. Reference `Klyroo.UI` from the shared project and call `AddKlyrooUi` from `MauiProgram.cs`, after
   `AddMauiBlazorWebView`:

```csharp
using Klyroo.UI;

// MauiProgram.cs, after builder.Services.AddMauiBlazorWebView();
builder.Services.AddKlyrooUi(o => o.Colors.Primary = KlyPalette.Blue);
```

2. Link the stylesheets from `wwwroot/index.html`. Give the viewport `viewport-fit=cover` so the page uses the whole screen;
   Klyroo keeps its content inside the safe area:

```html
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
<link rel="stylesheet" href="_content/Klyroo.UI/klyroo.preflight.css" />
<link rel="stylesheet" href="_content/Klyroo.UI/klyroo.css" />
```

3. Add a `HeadOutlet` root component with the selector `head::after` next to the root component of the app, so the
   head content has somewhere to go:

```xml
<blazor:BlazorWebView x:Name="blazorWebView" HostPage="wwwroot/index.html">
    <blazor:BlazorWebView.RootComponents>
        <blazor:RootComponent Selector="#app" ComponentType="{x:Type shared:Routes}" />
        <blazor:RootComponent Selector="head::after" ComponentType="{x:Type web:HeadOutlet}" />
    </blazor:BlazorWebView.RootComponents>
</blazor:BlazorWebView>
```

4. Put `KlyHead` once in the layout, next to `KlyApp`. It sends the theme variables and the self-hosted fonts to the
   head through `HeadContent`, which a web view cannot receive from a server. It renders nothing in a browser.

```razor
@inherits LayoutComponentBase

<KlyHead />
<KlyApp>
    <KlyMain>@Body</KlyMain>
</KlyApp>
```

Platform minimums, from the sample project: Klyroo.UI needs the native popover API in the web view, which means
iOS 17 and Mac Catalyst 17 and, on Android, a current System WebView (Chrome 114 or later; the app itself runs from
Android 7.0, API 24). The Windows target is set to 10.0.17763.0.

What changes in a web view:

| Feature | In a web view |
| :--- | :--- |
| Culture | `KlyLocaleSelect` stores the choice through `IKlyCultureStore`, by default in `localStorage` (`KlyLocalStorageCultureStore`). Register your own store, such as one over `Preferences`, and call `ApplyStoredCultureAsync` before the first render to start in the right language. |
| Cookies | There is no server to read them. `Klyroo.UI.Server` does nothing here. |
| `KlyCultureMeta` | Renders nothing, and `ApplyServerCultureAsync` keeps the culture of the device. |
| Images | `Klyroo.UI.Images.Local` is an ASP.NET Core endpoint and is not available. Use a CDN provider or local files. |
| Head | `KlyHead` and a `HeadOutlet` root component replace the head of `App.razor`. |
| Touch | Targets reach 44 px on coarse pointers. |
| Host detection | `KlyrooHost.IsHybrid(RendererInfo)` tells a web view from a browser; the page also gets `data-kly-host="webview"` on `<html>`. |

`<KlyApp SpatialNavigation="true">` turns on an experimental mode for TV screens that serves remote controls.

## Trimming and NativeAOT

`Klyroo.UI`, `Klyroo.UI.Images` and `Klyroo.UI.Server` set `IsTrimmable`. The components avoid reflection in their
interop: calls into JavaScript pass strings and numbers, event handlers carry `DynamicDependency` attributes so their
event arguments survive trimming, and the generic radio group carries the annotation that `InputRadioGroup<T>`
requires. `Klyroo.UI.Images.Local` does not set `IsTrimmable`, because it depends on NetVips and its native binaries.

None of the packages sets `IsAotCompatible` or is tested here as a NativeAOT target. Treat NativeAOT as unverified: publish
your app with the setting you intend to use and test it.

## Supported platform

`Klyroo.UI` declares `SupportedPlatform` `browser`: it targets code that runs in a browser or in a web view. The
stylesheets and the JavaScript are static web assets, so any host that serves static web assets can serve them.
The target framework of every package is `net10.0`.
