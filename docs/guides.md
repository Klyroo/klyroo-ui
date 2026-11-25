# Guides

Each section solves one task. The code is complete for the task it shows. For the reasoning behind it, read
[Concepts](concepts.md).

- [Change the palette and the design tokens](#change-the-palette-and-the-design-tokens)
- [Restyle components](#restyle-components)
- [Compile Tailwind in your app](#compile-tailwind-in-your-app)
- [Self-host fonts](#self-host-fonts)
- [Use and replace icons](#use-and-replace-icons)
- [Serve responsive images](#serve-responsive-images)
- [Localize the app](#localize-the-app)
- [Validate a form in every render mode](#validate-a-form-in-every-render-mode)
- [Control the color mode](#control-the-color-mode)
- [Test components with bUnit](#test-components-with-bunit)

## Change the palette and the design tokens

Goal: make the primary color blue, use a gray that suits it, and add a color of your own.

Map the semantic colors to Tailwind palettes in `AddKlyrooUi`. Klyroo.UI computes the fill, the text color and the
color used on top of the fill for light and dark mode, and it keeps the contrast ratios (4.5:1 for text, 3:1 for
fills). Put `KlyThemeStyle` in `<head>` so the page receives the result.

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi(o =>
{
    o.Colors.Primary = KlyPalette.Blue;
    o.Colors.Neutral = KlyPalette.Zinc;
    o.Colors.Add("tertiary", KlyPalette.Indigo);
    o.Defaults.Color = KlyColor.Secondary;
    o.Defaults.Size = KlySize.Sm;
});
```

```razor
<KlyButton Label="Publish" Color="@("tertiary")" />
```

`Neutral` also drives the text, background and border tokens. `o.Defaults` applies to the components whose `Color`
or `Size` is not set; an explicit parameter always wins. A palette that cannot meet the contrast rules throws an
`InvalidOperationException` that names the color and the rule, unless you set `o.Colors.AllowLowContrast`. For a palette
of your own, use `KlyPalette.Custom("brand", shades)` with all eleven shades from 50 to 950.

Everything is also a CSS variable, so you can rebrand without C#, with or without Tailwind. Override the variables in
both `:root` and `.dark`:

```css
:root {
  --klyroo-radius: 0.5rem;
  --klyroo-primary: var(--color-teal-700);
  --klyroo-primary-text: var(--color-teal-800);
}
.dark {
  --klyroo-primary: var(--color-teal-400);
  --klyroo-primary-text: var(--color-teal-300);
}
```

The variables are the colors (`--klyroo-primary`, `-secondary`, `-success`, `-info`, `-warning`, `-error`,
`-neutral`, each with a `-text` and a `-contrast` variant), the text tokens (`--klyroo-text`, `-text-toned`,
`-text-muted`, `-text-dimmed`, `-text-highlighted`, `-text-inverted`), the surfaces (`--klyroo-bg`, `-bg-muted`,
`-bg-elevated`, `-bg-accented`, `-bg-overlay`, `-bg-inverted`), the borders (`--klyroo-border`, `-border-muted`,
`-border-accented`, `-border-inverted`), `--klyroo-radius`, `--klyroo-container`, `--klyroo-header-height`,
`--klyroo-control` and the two font stacks. Your unlayered `:root` rule beats the library's dark values, which is why a
color overridden only in `:root` keeps its light value in dark mode.

A color you define by hand needs three variables on its `data-color` selector:

```css
[data-color='brand'] {
  --kly-color: #0f766e;
  --kly-color-text: #115e59;
  --kly-color-contrast: #fff;
}
```

## Restyle components

Goal: change how a component looks everywhere, in one subtree, or in one place.

There are four levels, from the broadest to the narrowest. Each is merged after the previous one with tailwind-merge,
so a conflicting class from a narrower level wins.

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi(o =>
{
    o.Button.Slot("base", "rounded-full");
    o.Card.Slot("root", "shadow-sm");
    o.Button.Variant("size", "md", "base", "min-w-24");
});
```

```razor
<KlyTheme For="typeof(KlyButton)" Ui="@(new ButtonUi { Base = "uppercase tracking-wide" })">
    <KlyButton Label="Save" />
    <KlyButton Label="Cancel" Variant="ButtonVariant.Ghost" />
</KlyTheme>

<KlyButton Label="Save" Class="px-6" Ui="@(new ButtonUi { Label = "font-bold" })" />
```

`AddKlyrooUi` configures the theme of every instance. `KlyTheme` applies overrides to every component of a type below
it. `Ui` targets one slot of one instance, with one property per slot (`ButtonUi.Base`, `CardUi.Header`, and so on).
`Class` adds classes to the root slot of one instance.

Two limits are worth knowing. A theme becomes read-only the first time a component renders with it, because the
resolved classes are cached: change it in `AddKlyrooUi`, which runs before the first render, and expect an
`InvalidOperationException` otherwise. And `KlyTheme` is a cascading value, so it does not cross a render mode
boundary: put one inside each interactive island, or use `AddKlyrooUi` for what must apply everywhere.

Utilities you write in `Class` and `Ui` need Tailwind compiled by your app, or a class that already exists in
`klyroo.css`, because the precompiled stylesheet only contains the classes the library itself uses.

## Compile Tailwind in your app

Goal: use Tailwind utilities in your markup, together with the components, without installing Node.

Set one property in the project that owns the stylesheet. The build downloads the official Tailwind standalone CLI
once, checks it against a SHA-256 that ships in the package, and compiles your stylesheet on every build.

```xml
<PropertyGroup>
  <KlyrooTailwind>true</KlyrooTailwind>
</PropertyGroup>
```

Create `Styles/app.css`. Every build copies the Klyroo theme to `obj/klyroo/klyroo.theme.css`; importing it after
Tailwind gives you the tokens, the dark variant, the utilities that follow them and the list of classes the
components use:

```css
@import "tailwindcss";
@import "../obj/klyroo/klyroo.theme.css";
```

Link the result instead of `klyroo.css` and `klyroo.preflight.css`:

```razor
<link rel="stylesheet" href="@Assets["app.css"]" />
```

The compiled file is generated under `obj` and added to the static web assets, so it is served at `app.css` in a web
project and at `_content/<Project>/app.css` in a Razor class library. The properties are:

| Property | Default | Meaning |
| :--- | :--- | :--- |
| `KlyrooTailwind` | `false` | Turns the feature on. |
| `KlyrooTailwindInput` | `Styles/app.css` | The stylesheet, relative to the project. |
| `KlyrooTailwindAssetPath` | `app.css` | Where it is served, below the base path of the project. |
| `KlyrooTailwindMinify` | `true` in Release | Minifies the output. |
| `KlyrooTailwindExecutable` | none | A Tailwind binary to use instead of the download. |
| `KlyrooTailwindVersion` | `4.3.5` | The release to download. Only the default has an embedded checksum, so use `KlyrooTailwindExecutable` for another one. |
| `KlyrooTailwindCacheDir` | `~/.klyroo/tailwind` (Linux, macOS), `%LOCALAPPDATA%\Klyroo\tailwind` (Windows) | Where the binaries are kept, shared by every project of the user. |
| `KlyrooTailwindAdditionalInput` | none | Files outside the project that the stylesheet reads, so editing them rebuilds the CSS. |
| `KlyrooOffline` | `false` | Never touches the network. |

The CLI finds your `.razor`, `.cs` and `.html` files from the project folder. For a shared project, add an `@source`
line to the stylesheet and list the same files as additional inputs:

```xml
<ItemGroup>
  <KlyrooTailwindAdditionalInput Include="../Shared/**/*.razor;../Shared/**/*.cs" />
</ItemGroup>
```

```css
@source "../../Shared";
```

If the Blazor template left a `wwwroot/app.css`, delete it or change `KlyrooTailwindAssetPath`: two assets at one
route is an error (`KLYTW004`). When the binary cannot be downloaded the build fails with `KLYTW001`; fill the cache
with one online build and set `KlyrooOffline`, or point `KlyrooTailwindExecutable` at a binary you installed.

Under `dotnet watch`, run `dotnet build -t:KlyrooTailwindWatch` in a second terminal. It runs the CLI with its own
file watcher on the same output. You still reload the page by hand.

If you already run Tailwind yourself, skip `KlyrooTailwind`, import `obj/klyroo/klyroo.theme.css` after Tailwind in
your own pipeline, and load your compiled sheet only.

## Self-host fonts

Goal: use a web font without a request to a font provider when the page runs.

Name the font in the project that renders the page (the server project of an Auto app) and put `KlyFonts` in `<head>`.
The build downloads only the weights and subsets you list, writes `@font-face` rules with a metrics-adjusted fallback
face, and preloads the main face.

```xml
<ItemGroup>
  <KlyrooFont Include="Public Sans" Role="sans" Weights="400;700" Subsets="latin;latin-ext" />
  <KlyrooFont Include="Geist Mono" Role="mono" Weights="400..600" Subsets="latin" />
</ItemGroup>
```

```razor
<head>
    <KlyFonts />
    <link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
</head>
```

A font with `Role="sans"` or `Role="mono"` becomes the font of the page through `--klyroo-font-sans` or
`--klyroo-font-mono`, in both stylesheet modes. The metadata of an item are `Role`, `Provider` (`google`, `bunny`,
`fontsource` or `local`), `Weights`, `Styles`, `Subsets`, `Display`, `Preload`, `Src` (a folder of your own files, which
selects the `local` provider) and `Category` (`sans`, `serif` or `mono`, for the fallback face). Without `Provider`, the
providers of `KlyrooFontsProviderPriority` (`google;bunny;fontsource`) are tried in order.

Declaring the items in a Blazor WebAssembly project is an error (`KLYFONT010`), because the server renders the head and
`KlyFonts` reads the manifest of the entry assembly. A font that cannot be resolved is a warning in Debug and an error
in Release (`KlyrooFontsFailOnError`). Answers are cached in `obj/klyroo/fonts-cache`; `KlyrooOffline` never touches the
network. `KlyrooFonts=false` turns the feature off.

## Use and replace icons

Goal: draw any icon by name, and change the icons the components use.

`KlyIcon` takes a Lucide name (`search`), the Iconify form (`ph:house`) or the prefixed form (`i-ph-house`). The build
scans your `.razor`, `.cshtml` and `.cs` files for icon names written as literals, downloads what it lacks from the
Iconify API, and compiles the icons into your assembly. Nothing is requested when the page runs.

```razor
<KlyIcon Name="ph:house" Size="KlySize.Lg" />
<KlyButton Label="Open" LeadingIcon="@KlyIcons.Named("i-lucide-folder-open")" />
```

A name built at runtime is invisible to the scan: declare it in the project file. A folder of SVG files is a
collection of your own, named `prefix:file-name`:

```xml
<ItemGroup>
  <KlyrooIcon Include="ph:cube;ph:cube-fill" />
  <KlyrooIconCollection Include="Assets/icons" Prefix="custom" />
  <KlyrooIconScanDirectory Include="../Shared" />
</ItemGroup>
```

The icons that components draw come from `KlyrooOptions.Icons`, each with a Lucide default. Assign a name to replace
one everywhere, or register markup in code:

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi(o =>
{
    o.Icons.Close = "ph:x";
    o.Icons.Loading = "i-mdi-loading";
    o.Icons.Register("brand:mark", "<path d=\"M3 3h18v18H3z\" />", 24, 24);
});
```

`Register` takes the body of an `svg` element and `RegisterSvg` takes a whole SVG file, which it sanitizes. Both are for
markup you own: never register content from end users. Every icon the build bundles is read as XML and rewritten from
an allowlist of SVG elements and attributes, so scripts, event handlers and external references never reach the page.
An icon that cannot be found renders an empty icon marked `data-kly-missing-icon`. At build time a missing icon is a
warning in Debug and an error in Release (`KlyrooIconsFailOnError`). `KlyrooIcons=false` turns the scan off, and
`KlyrooIconsScan=false` keeps the items but stops reading your sources.

## Serve responsive images

Goal: render images with a `srcset` that an image service or your own server produces.

Components that show a picture render a `KlyImage`. Without a provider it keeps the source and only adds `width`,
`height` and `loading`. Pick a provider in `AddKlyrooUi`, in the server project and in the WebAssembly client alike:

```csharp
using Klyroo.UI;
using Klyroo.UI.Images;

builder.Services.AddKlyrooUi(o =>
{
    o.Images.Domains.Add("images.example.com");
    o.Images.AddCloudinary("my-cloud", makeDefault: true);
    o.Images.Presets["thumb"] = new KlyImageOptions { Format = "webp", Quality = 60 };
});
```

```razor
<KlyImage Src="/img/team.jpg" Alt="The team on stage" Width="640" Height="360"
          Sizes="100vw sm:50vw md:640px" Densities="x1 x2" Format="webp" />
<KlyAvatar Src="/img/ada.jpg" Alt="Ada Lovelace" ImageOptions="@(new KlyImageOptions { Preset = "thumb" })" />
```

`Klyroo.UI.Images` holds Cloudinary, imgix, ImageKit, Cloudflare Images, Netlify, Vercel, Bunny and a URL template
provider. `Alt` is required: pass an empty string for a decorative image. A remote `src` goes only to the providers
that fetch remote images (Cloudflare, Netlify, Vercel and the local one) when its host is in `o.Images.Domains`.

To resize on your own server, add `Klyroo.UI.Images.Local` to the server project. It maps an endpoint that resizes and
converts with libvips:

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi();
builder.Services.AddKlyrooImagesLocal(o =>
{
    o.CacheDirectory = "/var/cache/my-app/images";
    o.Version = "2026-11-20";
    o.AllowedRemoteHosts.Add("images.example.com");
});

var app = builder.Build();
app.MapKlyrooImages();
```

In a WebAssembly client register only the URL builder with `o.Images.AddLocal(makeDefault: true)`; it has no server
dependency. The endpoint is `_klyroo/img`, relative to `<base href>`, so an app served below a path base reaches its
own optimizer in every render mode. Call `app.UseRouting()` right after `UsePathBase`. Behind a reverse proxy, call
`UseForwardedHeaders` first, because the rate limit tells clients apart by address.

### Limits of the local endpoint

The endpoint is meant to face the internet, so every request is bounded. The options are on `KlyLocalImagesOptions`.

- **Sources.** Local sources are files below the web root (or `FileProvider`), with image extensions only. Dot segments,
  backslashes and absolute paths are rejected with 400. Remote images are fetched only from `AllowedRemoteHosts` (403
  otherwise), on the default port of the scheme unless the entry names one, with a size limit, a timeout, redirects only to
  the same scheme, host and port, no credentials in the URL and a content type check. Loopback, private, link-local and
  other special-use addresses are refused unless `AllowPrivateNetworkAddresses` is on.
- **Sizes.** Width and height are clamped to `MaxWidth` and `MaxHeight`, widths are rounded up to a fixed list and
  qualities to multiples of 5, so a client cannot ask for every combination. A request without a size is scaled down to
  the maximum. The formats produced are `webp`, `avif`, `jpeg`, `png` and `gif` (one frame).
- **Cost.** Identical requests share one computation. Processing is limited to `MaxConcurrency` images at a time, and a
  request that waits longer than `QueueTimeout` for a slot gets 503. Each client may start `RateLimitPermits`
  computations per `RateLimitWindow` (600 per minute by default, 429 with `Retry-After` beyond; cached variants are free).
- **Cache.** A source keeps at most `MaxVariantsPerSource` variants and a new one replaces the least recently used. The
  disk cache is limited to `MaxCacheBytes` and trimmed by last access. Responses carry an `ETag` and answer 304. With
  `Version` set, every URL carries it and is cached as immutable for a year, so change it when your images change.
- **SVG.** An SVG is recognized by its root element, served as it is with a sandboxing `Content-Security-Policy`, and
  rasterized only when `RasterizeSvg` is on (off by default) and a raster format is requested. libvips loads only JPEG, PNG,
  WebP, AVIF or HEIC, and GIF here, and animated GIF and WebP files are served untouched.

## Localize the app

Goal: show the app in the language of the user, change a few texts, and add a language.

Twelve locales are built in: English, French, German, Spanish, Italian, Portuguese, Brazilian Portuguese, Dutch,
Japanese, Simplified Chinese, Arabic and Russian. Only English and French have been reviewed by native speakers
(`KlyLocales.ReviewedCodes`); the others are drafts. A component picks its locale from `CultureInfo.CurrentUICulture`:
the exact code, then its parent cultures, then English. On the server, request localization sets the culture of every
request, so no component code is needed:

```csharp
using Klyroo.UI;

var uiCultures = KlyLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures([.. uiCultures, "en-GB"])
    .AddSupportedUICultures(uiCultures));
```

Render `KlyCultureMeta` in `<head>` and, in a WebAssembly client, give the runtime the culture the server used before
it starts. Without this the client starts from the language of the browser, which can differ.

```csharp
using Klyroo.UI;

var host = builder.Build();
await host.Services.ApplyServerCultureAsync(KlyLocales.All.Select(locale => locale.Code));
await host.RunAsync();
```

Change a few messages, or add a locale, in `AddKlyrooUi`. A message with placeholders keeps them in a translation.

```csharp
using Klyroo.UI;

var australian = KlyLocales.Extend(KlyLocales.En, code: "en-AU", name: "English (Australia)",
    messages: m => m with { SearchCommands = "Find a command..." });

builder.Services.AddKlyrooUi(o => o.Locales = [.. KlyLocales.All, australian]);
```

`KlyLocaleSelect` lists `o.Locales`, writes the culture cookie and reloads the page. `o.Locale` fixes one locale for
the whole app, and `KlyApp` takes a `Locale` parameter for a subtree. `Locale` and `Locales` become read-only when the
first component resolves a locale. An Arabic locale is right to left: `KlyApp` writes `lang` and `dir`, and the
components use logical utilities and mirror their arrows. Dates and numbers use the regional culture of the user when
it speaks the language of the locale, so an `en-GB` user with the English locale keeps `20/11/2026`.

## Validate a form in every render mode

Goal: a form that validates with data annotations and works in static rendering, Server, WebAssembly and Auto.

[Getting started](getting-started.md#7-build-the-form) builds one. The rules that make it work everywhere:

- Give the form a `FormName` and match it in `[SupplyParameterFromForm(FormName = "...")]`.
- Create the model with `Model ??= new()` in `OnInitialized`: on a post the framework has filled it.
- Use `Type="submit"` on the button. A button with `OnClick` is disabled until the page is interactive.
- Put the message in the annotation. `KlyFormField` shows the message of its `For` expression and ties the label, the
  description and the error to the control with the right ids. `ShowSummary` lists every message above the fields.
- Validation runs on the server on a post. Never rely on the browser alone.

Use a plain `EditForm` when you want Blazor's own parts with Klyroo controls:

```razor
@using System.ComponentModel.DataAnnotations

<EditForm Model="Model" FormName="signup" OnValidSubmit="Submit" class="flex max-w-sm flex-col gap-4" novalidate>
    <DataAnnotationsValidator />
    <KlyFormField Label="Email" Required For="() => Model!.Email">
        <KlyInput Type="email" @bind-Value="Model!.Email" />
    </KlyFormField>
    <KlyFormField Label="About you" Help="Markdown is supported." For="() => Model!.Bio">
        <KlyTextarea @bind-Value="Model!.Bio" Rows="3" />
    </KlyFormField>
    <KlySwitch @bind-Value="Model!.Newsletter" Label="Send me the newsletter" />
    <KlyButton Type="submit" Label="Create account" Block />
</EditForm>

@code {
    [SupplyParameterFromForm(FormName = "signup")]
    private SignUp? Model { get; set; }

    protected override void OnInitialized() => Model ??= new();

    private void Submit() { }

    private sealed class SignUp
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        public string? Bio { get; set; }

        public bool Newsletter { get; set; }
    }
}
```

Inside an `EditForm`, a control posts under the name of the bound property; outside one, give it `Name`. `KlyForm
Enhance` turns on Blazor's enhanced form handling, so the post is a fetch and the page is patched. To show an error that
does not come from validation, such as a server error, pass `Error` to `KlyFormField`.

## Control the color mode

Goal: let the user pick light, dark or system, and set it from code.

`KlyColorModeScript` in `<head>` applies the stored mode before the first paint. Any of the color mode controls
changes it and they all stay in sync: `KlyColorModeButton`, `KlyColorModeSwitch`, `KlyColorModeSelect`. The mode is
stored in `localStorage` under `klyroo-color-mode` as `light`, `dark` or `system`, and the page gets the `dark` class
on `<html>`.

```razor
<KlyColorModeButton />
<KlyColorModeSelect />
<KlyColorModeImage Light="/img/logo-light.svg" Dark="/img/logo-dark.svg" Alt="Acme" />
```

From JavaScript, call `Klyroo.setColorMode('dark')` or `Klyroo.getColorMode()`. From .NET, call
`IJSRuntime.InvokeVoidAsync("Klyroo.setColorMode", "dark")`. The images and avatars of `KlyColorModeImage` and
`KlyColorModeAvatar` render both versions and let CSS pick one, so there is no flash and no script.

## Test components with bUnit

Goal: render Klyroo components in a unit test and check their markup and behavior.

A test project needs xUnit, bUnit and a reference to the project under test. With the Razor SDK it can also hold test
components written in Razor. These are the versions the tests of this repository use:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.3" />
    <PackageReference Include="bunit" Version="2.15.2" />
    <PackageReference Include="Klyroo.UI" Version="0.1.0-preview.1" />
  </ItemGroup>
</Project>
```

Register Klyroo in the context and tell bUnit how the page renders. A component reads `RendererInfo`, so the same test
can check the interactive and the static behavior:

```csharp
using Bunit;
using Klyroo.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public abstract class KlyrooTestContext : BunitContext
{
    protected KlyrooTestContext(Action<KlyrooOptions>? configure = null, bool interactive = true)
    {
        Services.AddKlyrooUi(configure);
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
    }
}

public class ButtonTests : KlyrooTestContext
{
    [Fact]
    public void Renders_a_typed_button_with_a_label()
    {
        var cut = Render<KlyButton>(p => p.Add(b => b.Label, "Save"));

        Assert.Equal("button", cut.Find("button").GetAttribute("type"));
        Assert.Equal("Save", cut.Find("[data-slot=label]").TextContent);
    }

    [Fact]
    public void Click_invokes_the_callback()
    {
        var clicks = 0;
        var cut = Render<KlyButton>(p => p.Add(b => b.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }
}

public class StaticRenderTests : KlyrooTestContext
{
    public StaticRenderTests() : base(interactive: false) { }

    [Fact]
    public void A_button_with_a_click_handler_is_disabled_until_the_page_is_interactive()
    {
        var cut = Render<KlyButton>(p => p.Add(b => b.Label, "Save").Add(b => b.OnClick, (MouseEventArgs _) => { }));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void A_link_never_waits_for_interactivity()
    {
        var cut = Render<KlyButton>(p => p.Add(b => b.Href, "/docs").Add(b => b.Label, "Docs"));

        Assert.Equal("/docs", cut.Find("a").GetAttribute("href"));
    }
}
```

bUnit runs no JavaScript. The custom elements that handle opening, focus and keyboard navigation are never connected
in a unit test, so test what .NET renders and the callbacks it raises, and cover the in-browser behavior with a browser
test. In an end-to-end test, wait until the page is interactive before clicking a button that has an `OnClick`.
