# FAQ

Troubleshooting first, then known limitations, then frequent questions. Every entry is explained by the code of this
repository.

## Troubleshooting

### A button looks disabled and nothing happens when I click it

**Cause.** A `KlyButton` with `OnClick` renders disabled until the page is interactive, because its handler cannot run
in a static render or a prerender. It enables itself once the circuit or the WebAssembly runtime is ready.

**Fix.** Do not remove `Disabled` to work around it. If the button must work on a static page, give it `Href`, or give it
`Type="submit"` inside a form that posts. A link and a submit button never wait for interactivity.

### The components are unstyled, or the classes I pass in `Class` and `Ui` do nothing

**Cause.** Either no stylesheet is loaded, or you are in the precompiled mode. `klyroo.css` only contains the classes
the library itself uses, so a utility you write yourself has no CSS.

**Fix.** Load `klyroo.preflight.css` and `klyroo.css`, or compile Tailwind in your app and import
`obj/klyroo/klyroo.theme.css` after it (see [Compile Tailwind in your app](guides.md#compile-tailwind-in-your-app)). Do not load
both modes.

### Bootstrap overrides the styles

**Cause.** The Blazor template links Bootstrap. CSS outside a cascade layer beats the layered CSS that Tailwind v4
generates.

**Fix.** Remove the Bootstrap link and its files, or import it in a layer: `@import url(bootstrap.css) layer(bootstrap);`.

### A dark page flashes light on load

**Cause.** `KlyColorModeScript` is missing, or it comes after the stylesheets.

**Fix.** Put `<KlyColorModeScript />` in `<head>`, before the stylesheets. It applies the stored mode before the first paint.

### The app looks different after WebAssembly takes over

**Cause.** `AddKlyrooUi` was configured differently in the server project and in the client project, so the two renders
use different themes, palettes or locales.

**Fix.** Call one shared method from both `Program.cs` files. Do the same for `o.Images` providers.

### `InvalidOperationException`: the theme, the colors or the locale cannot change

**Cause.** A theme becomes read-only the first time a component renders with it, because the resolved classes are
cached. The palettes, the defaults, the locale options and the icon options freeze the same way.

**Fix.** Configure everything inside `AddKlyrooUi`, which runs before the first render and may be called several times.

### `InvalidOperationException` about contrast when I set a palette

**Cause.** The palette cannot meet the contrast rules (4.5:1 for text, 3:1 for fills), for example because its shades are
too close to each other. The exception names the color, the palette and the rule.

**Fix.** Choose another palette, or set `o.Colors.AllowLowContrast = true` to keep the best combination found, which may
be below the ratios.

### Overrides from `KlyApp` or `KlyTheme` are missing inside one component

**Cause.** They are cascading values, and a component with its own `@rendermode` starts a new tree that does not receive them.

**Fix.** Put a `KlyApp` or a `KlyTheme` inside that component, or use `AddKlyrooUi`, which every render mode shares.

### A popover does not open

**Cause.** The trigger is the first button inside the `ChildContent` of `KlyPopover`. Without a `<button>` there is nothing for the
browser to attach to.

**Fix.** Put a `<button>` or a `KlyButton` in `ChildContent`. Bind `@bind-Open` only when .NET needs the state.

### The form posts but the model is empty, or nothing validates

**Cause.** In static rendering the model comes from `[SupplyParameterFromForm]`, and it is matched by the form name.

**Fix.** Give the form a `FormName` and use the same name in `[SupplyParameterFromForm(FormName = "...")]`. Create the model
with `Model ??= new()` in `OnInitialized`, and use `Type="submit"` on the button.

### Items in a `for` loop all use the last index

**Cause.** The content of a child component is evaluated after the loop has moved on.

**Fix.** Copy the index into a local inside the loop and use the local.

### A toast does not appear

**Cause.** `IToastService` calls into the browser, so it needs an interactive render mode, and a `KlyToaster` must be on the page.

**Fix.** Call it from an interactive component. `KlyApp` renders a toaster; do not also place your own `KlyToaster`.

### The page has the wrong language, or dates differ between the prerender and the client

**Cause.** A WebAssembly client has no request and starts from the language of the browser. Also, there is no "same
language" step in the locale choice: `zh-TW` gets English unless you offer a `zh-TW` or a `zh` locale, which is what
`UseRequestLocalization` does too.

**Fix.** Render `KlyCultureMeta` in `<head>` and call `ApplyServerCultureAsync` in the client `Program.cs`. Add the
regional cultures you want to `AddSupportedCultures`.

### An icon renders empty

**Cause.** The name could not be resolved. An icon built at runtime is invisible to the build-time scan, and an icon
that is not a built-in Lucide icon is bundled only if its name is written as a literal.

**Fix.** Find it in the page with `[data-kly-missing-icon]`, then write the name as a literal or declare it with
`<KlyrooIcon Include="ph:cube" />`. At build time a missing icon is a warning in Debug and an error in Release (`KLYICON001`).

### An image has no `srcset`

**Cause.** No provider is registered (the default `none` provider keeps the source), or the source is remote and its
host is not in `o.Images.Domains`.

**Fix.** Register a provider in `AddKlyrooUi` on the server and the client, and add the remote host to `o.Images.Domains`.
`Alt` is required: pass an empty string for a decorative image.

### The image endpoint answers 400, 403, 429 or 503

**Cause.** 400 is a rejected source (dot segments, backslashes, an absolute local path, an unsupported extension). 403 is a
remote host that is not in `AllowedRemoteHosts`. 429 is the rate limit of a client, 600 computations per minute by default.
503 is a request that waited longer than `QueueTimeout` for a processing slot.

**Fix.** Fix the source, add the host, raise `RateLimitPermits` or `MaxConcurrency`. Behind a reverse proxy call
`UseForwardedHeaders` first, otherwise every visitor shares one address and one allowance.

### The build fails with a `KLYTW`, `KLYFONT` or `KLYICON` code

| Code | Cause | Fix |
| :--- | :--- | :--- |
| `KLYTW001`, `KLYTW002` | The Tailwind binary could not be found, downloaded or verified. | Fill the cache with one online build and set `KlyrooOffline`, or set `KlyrooTailwindExecutable`. |
| `KLYTW003` | `KlyrooTailwind` is on and the stylesheet does not exist. | Create `Styles/app.css`, or set `KlyrooTailwindInput`. |
| `KLYTW004` | The project already has `wwwroot/app.css`, the route of the generated sheet. | Delete it or set `KlyrooTailwindAssetPath`. |
| `KLYFONT010` | `KlyrooFont` items are declared in a Blazor WebAssembly project. | Declare them in the server project. |
| `KLYFONT001` to `KLYFONT004` | A font item is invalid, a provider answer was rejected, or the font could not be resolved (an error in Release, a warning in Debug). | Read the message, or set `KlyrooFontsFailOnError`. |
| `KLYICON001` | An icon name does not exist in a known collection (an error in Release, a warning in Debug). | Fix the name, or set `KlyrooIconsFailOnError`. |

### A MAUI page has no theme or fonts

**Cause.** A web view has no server that renders `<head>`. `KlyHead` sends the theme variables and the fonts to the head
through `HeadContent`, which needs a `HeadOutlet` root component.

**Fix.** Add `HeadOutlet` with the selector `head::after` to the `BlazorWebView` and put `<KlyHead />` in the layout. See
[Platforms](platforms.md#net-maui-blazor-hybrid).

### Popovers and menus do not work on an old iOS or Android web view

**Cause.** Klyroo.UI uses the native popover API. The sample project sets iOS 17 and Mac Catalyst 17 as minimums, and Android needs a
current System WebView (Chrome 114 or later).

**Fix.** Raise the minimum platform version, or update the WebView.

### `dotnet watch` does not pick up Tailwind changes

**Cause.** `dotnet watch` does not follow files under `obj` and does not refresh the page for a CSS-only edit.

**Fix.** Run `dotnet build -t:KlyrooTailwindWatch` in a second terminal and reload the page by hand.

## Known limitations

- Only English and French have been reviewed by native speakers (`KlyLocales.ReviewedCodes`). The other ten locales are drafts.
- `Klyroo.UI.Images.Local` is for ASP.NET Core servers. It is not available in a MAUI web view or in WebAssembly, where
  only its URL builder (`AddLocal`, in `Klyroo.UI.Images`) is.
- The first Release build of a project that bundles icons, fonts or the Tailwind binary needs the network, or a cache
  that an earlier build filled.
- NativeAOT is not declared or tested for any package. See [Platforms](platforms.md#trimming-and-nativeaot).
- The chat components render no Markdown: give them rendered markup or plain text.
- A unit test cannot exercise the in-browser behavior, because bUnit runs no JavaScript.
- TV mode (`SpatialNavigation`) is experimental.

## Frequent questions

**Do I need Node or npm?** No. The precompiled stylesheets need nothing, and `KlyrooTailwind` runs the Tailwind
standalone CLI that the build downloads. Node is only needed to work on the library itself.

**Can I use my own Tailwind setup?** Yes. Import `obj/klyroo/klyroo.theme.css` after `@import "tailwindcss"` and load only
your compiled stylesheet.

**Which .NET version does it need?** Every package targets `net10.0`.

**Do I need a package per component?** No. `Klyroo.UI` holds every component. The other packages add image providers, an
image endpoint and server cookie access.

**Does it work without interactivity?** Yes. Form controls post, and overlays, tabs and accordions run in the browser. Only
what needs a .NET handler is disabled until the page is interactive.

**Where are the data tables, forms and dashboards of the rest of the family?** In the other Klyroo libraries. They depend on
this one and are listed in the [README](../README.md#klyroo-ecosystem).
