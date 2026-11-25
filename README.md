# klyroo-ui

*Part of [Klyroo](https://klyroo.dev): Build better. Compose freely.*

[![CI](https://github.com/klyroo/klyroo-ui/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-ui/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/badge/NuGet-0.1.0--preview.1-004880.svg)](https://www.nuget.org/packages/Klyroo.UI) [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/) [![NativeAOT](https://img.shields.io/badge/NativeAOT-Compatible-10b981.svg)](https://klyroo.dev/why) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

Accessible, themable Blazor components styled with Tailwind CSS v4.
Status: early development. 133 accessible Blazor components across core UI families:

- **Elements**: Button, Badge, Avatar, AvatarGroup, Card, Alert, Banner, Chip, Container, Empty, FieldGroup, Icon, Kbd, Link,
  Progress, Separator, Skeleton, User
- **Form**: Form, FormField, Input, InputNumber, InputTags, InputMenu, InputDate, InputTime, InputRating, PinInput, Textarea,
  Checkbox, CheckboxGroup, Switch, RadioGroup, Select, SelectMenu, Listbox, Slider, Toggle, ToggleGroup, Calendar, ColorPicker,
  FileUpload
- **Overlay**: Modal, Slideover, Drawer, Popover, Tooltip, DropdownMenu, ContextMenu, CommandPalette, Toast, and the color mode
  controls (ColorModeButton, ColorModeSwitch, ColorModeSelect, ColorModeAvatar, ColorModeImage)
- **Navigation**: Tabs, Accordion, Collapsible, Breadcrumb, Pagination, Stepper, Timeline, NavigationMenu, Tree, Marquee,
  ScrollArea, Splitter, and the content components (ContentNavigation, ContentSearch, ContentSearchButton, ContentSurround, ContentToc)
- **Data**: Table, Chart, Carousel
- **Layout**: App, Header, Main, Footer, FooterColumns, Sidebar, Error, LocaleSelect, Theme
- **Page**: Page, PageHeader, PageHero, PageSection, PageFeature, PageGrid, PageColumns, PageCard, PageCta, PageLinks, PageList,
  PageLogos, PageAnchors, PageAside, PageBody, AuthForm, PricingPlan, PricingPlans, PricingTable, BlogPost, BlogPosts,
  ChangelogVersion, ChangelogVersions
- **Dashboard**: DashboardGroup, DashboardSidebar, DashboardSidebarCollapse, DashboardSidebarToggle, DashboardPanel,
  DashboardResizeHandle, DashboardNavbar, DashboardToolbar, DashboardSearch, DashboardSearchButton
- **Chat**: ChatMessages, ChatMessage, ChatPrompt, ChatPromptSubmit, ChatPalette, ChatAttachment, ChatReasoning, ChatShimmer, ChatTool
- **Editor**: Editor, EditorToolbar, EditorDragHandle, EditorSuggestionMenu, EditorMentionMenu, EditorEmojiMenu

Each one has a documentation page with live examples in the four render modes.

## Platforms

The same components run on the web and in .NET MAUI Blazor Hybrid apps (a `BlazorWebView`), so one set of pages can ship as a web app and as a native app.

| Target | Status |
| --- | --- |
| Web: static SSR, Server, WebAssembly, Auto | Tested in all four render modes, same markup |
| Android (phone) | Built and tested on the emulator |
| iOS 17 or later (the native popover is required) | CI build job set up but not run yet; not run on a device |
| Windows | CI build job set up but not run yet; not run on a device |
| macOS (Mac Catalyst) | CI build job set up but not run yet; not run on a device |
| Android TV | Experimental: D-pad navigation, editing mode for text fields, Back closes overlays. Tested on the emulator; the soft keyboard has not been seen yet (the TV emulator has none) |
| Tizen (Samsung TV) | Not supported: there is no BlazorWebView |

See [`samples/Klyroo.Maui`](samples/Klyroo.Maui) and the "MAUI and Blazor Hybrid" guide of the docs site.

## Install

Requires .NET 10.

```bash
dotnet add package Klyroo.UI
```

### Packages

| Package | What it is |
|---|---|
| `Klyroo.UI` | The components, themes, 12 locales and the build-time features (fonts, icons, Tailwind). |
| `Klyroo.UI.Images` | Image CDN providers (Cloudinary, imgix, ImageKit...) for `KlyImage`. |
| `Klyroo.UI.Images.Local` | A local image optimizer endpoint (NetVips). |
| `Klyroo.UI.Server` | Server-side request access (`services.AddKlyrooServer()`): lets components read cookies, such as the time zone of a data list, in static rendering and prerendering. ASP.NET Core only; call it in the server project. |
| `Klyroo.UI.Tools` | Data-heavy components built on Klyroo.UI (the data list and its parts, under construction). `services.AddKlyrooUiTools()` registers it, and Klyroo.UI with it. **Requires Tailwind compiled by your app**: no precompiled stylesheet, because a second one would override the variants of `klyroo.css`. With `<KlyrooTailwind>true</KlyrooTailwind>` the build imports its classes for you; with your own Tailwind, import `obj/klyroo/klyroo-tools.theme.css` after `klyroo.theme.css` and set `<KlyrooToolsOwnTailwind>true</KlyrooToolsOwnTailwind>`. Depends on exactly the same version of `Klyroo.UI`. |
| `Klyroo.UI.Tools.Query` | The Blazor-free request and filter models of the data list, for API servers (no ASP.NET Core, no Klyroo.UI). |
| `Klyroo.UI.Mcp` | The MCP server for agents (a dotnet tool). |

## Use it

```csharp
// Program.cs of the server AND of the .Client project (share the call in a method of the client project)
builder.Services.AddKlyrooUi();
```

Styles come in two modes. Without Tailwind in your app, load the precompiled stylesheets:

```razor
@* App.razor, in <head> *@
<KlyColorModeScript />
<KlyThemeStyle /> @* only needed when you configure palettes *@
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
```

```razor
@using Klyroo.UI

<KlyButton Label="Save" OnClick="Save" />
<KlySwitch @bind-Value="notify" Label="Email notifications" />
<KlyPopover @bind-Open="open">
    <ChildContent><KlyButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="p-3">Panel</p></Content>
</KlyPopover>
```

### Render modes

Components work in Interactive Server, WebAssembly and Auto, and in static server rendering (Switch posts forms,
Popover opens and closes in the browser). A button with `OnClick` renders disabled until the page is interactive,
because its handler cannot run before that. A Popover trigger must contain a `<button>`; bind `@bind-Open` when .NET needs
to know the state, otherwise opening and closing never leave the browser. If your app still loads Bootstrap from the Blazor template, remove it or import it
in a layer (`@import url(bootstrap.css) layer(bootstrap);`): unlayered CSS overrides Tailwind v4's layered CSS.

### Styles with Tailwind v4 compiled by your app

Every build copies the Klyroo theme (tokens, dark variant, the classes components use) to `obj/klyroo/klyroo.theme.css`.
Import it after Tailwind, and load only your compiled stylesheet (not `klyroo.css` nor `klyroo.preflight.css`):

```css
@import "tailwindcss";
@import "../obj/klyroo/klyroo.theme.css"; /* path relative to this file */
```

`Class` and `Ui` then accept any utility, since your Tailwind build sees them in your Razor files.

## Languages

Twelve locales are built in (English, French, German, Spanish, Italian, Portuguese, Brazilian Portuguese, Dutch, Japanese, Simplified
Chinese, Arabic and Russian). Components follow the culture of the user on their own: with ASP.NET Core request localization on the
server, or the culture of the browser in WebAssembly, no code is needed. A `KlyApp` writes `lang` and `dir` (Arabic is right to left).

```razor
<KlyApp Locale="KlyLocales.Fr"> @Body </KlyApp>
```

```csharp
builder.Services.AddKlyrooUi(o => o.Locale = KlyLocales.Extend(KlyLocales.En, code: "en-AU", messages: m => m with { Close = "Dismiss" }));
```

See the Internationalization guide of the documentation site.

## Customize

- **Palettes**: map the semantic colors to Tailwind palettes or your own. Klyroo computes the
  fill, text and on-fill tokens so the contrast rules still hold, and `KlyThemeStyle` (in `<head>`, next to
  `KlyColorModeScript`) delivers them; it renders nothing while the colors are the defaults.

  ```csharp
  builder.Services.AddKlyrooUi(o =>
  {
      o.Colors.Primary = KlyPalette.Blue;                 // 26 Tailwind v4 palettes, or KlyPalette.Custom("brand", shades)
      o.Colors.Neutral = KlyPalette.Zinc;                 // also drives the text, background and border tokens
      o.Colors.Add("tertiary", KlyPalette.Indigo);        // then Color="@("tertiary")", no CSS to write
      o.Defaults.Color = KlyColor.Secondary;              // used when a component's Color is not set (Size likewise)
  });
  ```

- **Tokens** (works everywhere): override `--klyroo-primary`, `--klyroo-radius`, `--klyroo-bg`… in your CSS, in both
  `:root` and `.dark`. Your unlayered `:root` rule beats the library's dark values, so a color overridden only in
  `:root` keeps its light value in dark mode while its paired `-contrast` token still flips. The full list: text
  (`--klyroo-text`, `-text-toned`, `-text-muted`, `-text-dimmed`, `-text-highlighted`, `-text-inverted`), backgrounds
  (`--klyroo-bg`, `-bg-muted`, `-bg-elevated`, `-bg-accented`, `-bg-overlay`, `-bg-inverted`), borders (`--klyroo-border`,
  `-border-muted`, `-border-accented`, `-border-inverted`), `--klyroo-radius` (the `rounded-*` scale is computed from it),
  `--klyroo-container`, `--klyroo-header-height` and `--klyroo-chart-1` to `-6` (unset by default, they fall back to the semantic colors).
- **Fonts**: name them in the project file, and the build downloads and self-hosts them (no request to a font provider
  when the page runs), with `@font-face`, a metrics-adjusted fallback face and a preload. Put `<KlyFonts />` in `<head>`.
  They set `--klyroo-font-sans` and `--klyroo-font-mono`, which the page font, `font-sans` and `font-mono` follow.

  ```xml
  <ItemGroup>
    <KlyrooFont Include="Public Sans" Role="sans" Weights="400;700" Subsets="latin;latin-ext" />  <!-- google, bunny, fontsource or local -->
  </ItemGroup>
  ```

- **Tailwind utilities** (Tailwind compiled by your app): `text-default`, `text-muted`, `text-dimmed`, `text-toned`,
  `text-highlighted`, `text-inverted`, `bg-default`, `bg-muted`, `bg-elevated`, `bg-accented`, `bg-inverted`,
  `border-default`, `border-muted`, `border-accented`, `border-inverted` (also `ring-`, `divide-`, `outline-`), the semantic
  colors (`bg-primary`, `text-error`…) and `rounded-*`, which follows `--klyroo-radius`.
- **Custom color by hand**: `Color="@("brand")"` plus `[data-color='brand'] { --kly-color: …; --kly-color-text: …; --kly-color-contrast: …; }`.
- **Global theme**: `AddKlyrooUi(o => o.Button.Slot("base", "font-semibold"))`.
- **Per instance**: `Class="…"` on the root and `Ui="@(new ButtonUi { Label = "…" })"` per slot. These take Tailwind
  utilities, so they need Tailwind compiled by your app; without it, only classes already in `klyroo.css` or your own CSS apply.

## Images

`KlyImage` renders an `img` with the `width`, `height`, `srcset`, `sizes` and `loading` an image provider can fill in;
the avatar, blog post, changelog, logos and chat attachment components use it and take an `ImageOptions` parameter.
Without a provider images are rendered as they are. Pick one in `AddKlyrooUi`:

```csharp
o.Images.AddCloudinary("my-cloud", makeDefault: true);   // Klyroo.UI.Images: also imgix, ImageKit, Cloudflare, Netlify, Vercel, Bunny
// or resize on your own server with Klyroo.UI.Images.Local:  services.AddKlyrooImagesLocal();  app.MapKlyrooImages();
```

```razor
<KlyImage Src="/img/team.jpg" Alt="The team" Width="640" Height="360" Sizes="100vw md:640px" Format="webp" />
```

See the "Images" guide of the docs site.

## MAUI and Blazor Hybrid

Klyroo runs in a `BlazorWebView` (.NET MAUI), so the same pages can ship as a web app and as an Android, Windows, iOS (17 or later) and Mac Catalyst app, for example from the
`maui-blazor-web` template. Server features degrade by themselves in a web view (storage in `localStorage`, a pluggable culture store, head content through `KlyHead` and a
`HeadOutlet` root component); the local image optimizer is web only. Tooltips follow touch, targets are 44 px on coarse pointers, safe areas are respected, and an experimental
TV mode (`<KlyApp SpatialNavigation="true">`) serves remote controls. See the Platforms table above for the status of each target, `samples/Klyroo.Maui` and the "MAUI and Blazor Hybrid" guide.

## AI tooling

Agents can read the real API instead of guessing it. All of it is generated from the docs catalog and the library, so it follows every change.

- **MCP server**: `dotnet tool install --global Klyroo.UI.Mcp --prerelease`, then `claude mcp add --transport stdio klyroo-ui -- klyroo-ui-mcp`. It searches components,
  returns parameters, events, slots and theme, example sources and guides. The docs site serves the same server at `/mcp`.
- **llms.txt**: `/llms.txt` (overview with links) and `/llms-full.txt` (everything) on the docs site.
- **Skill**: `skills/klyroo-ui` holds conventions, patterns and a routing table for coding agents (`npx skills add <repo>`).

See the "AI" guides of the docs site.

## Contributing

Issues and pull requests are welcome. Read [CONTRIBUTING.md](./CONTRIBUTING.md) for the workflow (Conventional
Commits, tests, documentation), follow the [Code of Conduct](./CODE_OF_CONDUCT.md), and report vulnerabilities
privately as described in [SECURITY.md](./SECURITY.md).

## Migrating from Lux

This library was previously named `lux-ui`. [MIGRATION.md](./MIGRATION.md) lists every renamed package, namespace,
component, CSS token and build setting.

## License

MIT
