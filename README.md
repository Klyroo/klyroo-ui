# Klyroo.UI

Accessible Blazor components styled with Tailwind CSS v4, for .NET developers who build web apps and .NET MAUI Blazor Hybrid apps.

[![CI](https://github.com/klyroo/klyroo-ui/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-ui/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Klyroo.UI.svg)](https://www.nuget.org/packages/Klyroo.UI)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Klyroo.UI gives a Blazor app a complete set of components, a design system built on CSS variables and a build that
bundles icons, fonts and Tailwind for you. Opening, closing, focus and keyboard handling run in the browser, so the
components work in static server rendering, Interactive Server, WebAssembly and Auto, and in a `BlazorWebView`.

## Features

- **143 components**: elements, form controls, overlays, navigation, data, layout, page sections, dashboards, chat and a rich text editor, listed [below](#components).
- **Every render mode**: the same markup in static server rendering, Interactive Server, WebAssembly and Auto, because UI state lives in custom elements in the browser unless you bind it.
- **Two stylesheet modes**: load the precompiled `klyroo.css`, or let the build compile your Tailwind v4 stylesheet with the Klyroo theme and no Node.
- **Design tokens and palettes**: map the semantic colors to any Tailwind palette in C#, with the contrast rules checked, or override `--klyroo-*` CSS variables.
- **Build-time icons and fonts**: any Iconify icon works by name and fonts are self-hosted, with no request to a third party when the page runs.
- **Responsive images**: `KlyImage` fills `srcset` and `sizes` through providers for Cloudinary, imgix, ImageKit, Cloudflare, Netlify, Vercel and Bunny, or through your own server with libvips.
- **Twelve locales**: every text comes from a locale, components follow the culture of the user, and Arabic renders right to left.
- **.NET MAUI Blazor Hybrid**: the same pages run in a `BlazorWebView`, with a sample project.

## Components

Every component name starts with `Kly`, so `Button` is `KlyButton`. The list counts the 143 public components that you place in a page.
The records they take (`KlyTabItem`, `KlySelectItem`, ...), the `*Ui` slot records, the base classes and the internal helper components
that the [API reference](docs/api-reference.md) also lists are not counted.

- **Elements** (20): Button, Badge, Avatar, AvatarGroup, Card, Alert, Banner, Chip, Container, Empty, FieldGroup, Icon, Image, InfiniteScroll, Kbd, Link, Progress, Separator, Skeleton, User
- **Form** (25): Form, FormField, Input, InputNumber, InputTags, InputMenu, InputDate, InputTime, InputRating, PinInput, Textarea, Checkbox, CheckboxGroup, Switch, RadioGroup, Select, SelectMenu, Listbox, Slider, RangeSlider, Toggle, ToggleGroup, Calendar, ColorPicker, FileUpload
- **Overlay** (9): Modal, Slideover, Drawer, Popover, Tooltip, DropdownMenu, ContextMenu, CommandPalette, Toaster
- **Color mode** (5): ColorModeButton, ColorModeSwitch, ColorModeSelect, ColorModeAvatar, ColorModeImage
- **Navigation** (12): Tabs, Accordion, Collapsible, Breadcrumb, Pagination, Stepper, Timeline, NavigationMenu, Tree, Marquee, ScrollArea, Splitter
- **Content** (5): ContentNavigation, ContentSearch, ContentSearchButton, ContentSurround, ContentToc
- **Data** (4): Table, Column, Chart, Carousel
- **Layout** (10): App, Header, Main, Footer, FooterColumns, Sidebar, SkipLink, Error, LocaleSelect, Theme
- **Page** (23): Page, PageHeader, PageHero, PageSection, PageFeature, PageGrid, PageColumns, PageCard, PageCta, PageLinks, PageList, PageLogos, PageAnchors, PageAside, PageBody, AuthForm, PricingPlan, PricingPlans, PricingTable, BlogPost, BlogPosts, ChangelogVersion, ChangelogVersions
- **Dashboard** (10): DashboardGroup, DashboardSidebar, DashboardSidebarCollapse, DashboardSidebarToggle, DashboardPanel, DashboardResizeHandle, DashboardNavbar, DashboardToolbar, DashboardSearch, DashboardSearchButton
- **Chat** (9): ChatMessages, ChatMessage, ChatPrompt, ChatPromptSubmit, ChatPalette, ChatAttachment, ChatReasoning, ChatShimmer, ChatTool
- **Editor** (6): Editor, EditorToolbar, EditorDragHandle, EditorSuggestionMenu, EditorMentionMenu, EditorEmojiMenu
- **Head** (5): ColorModeScript, CultureMeta, Fonts, Head, ThemeStyle (the components that go in `<head>`)

## Packages

| Package | Description |
| :--- | :--- |
| [`Klyroo.UI`](https://www.nuget.org/packages/Klyroo.UI) | The components, themes, twelve locales, stylesheets, JavaScript and the build targets for fonts, icons and Tailwind. |
| [`Klyroo.UI.Server`](https://www.nuget.org/packages/Klyroo.UI.Server) | Lets components read cookies in static rendering and prerendering over `HttpContext`. ASP.NET Core server projects only. |
| [`Klyroo.UI.Images`](https://www.nuget.org/packages/Klyroo.UI.Images) | Image providers for `KlyImage`: Cloudinary, imgix, ImageKit, Cloudflare Images, Netlify, Vercel, Bunny and URL templates. |
| [`Klyroo.UI.Images.Local`](https://www.nuget.org/packages/Klyroo.UI.Images.Local) | An ASP.NET Core endpoint that resizes and converts images with libvips, and the provider that uses it. |

The other three packages have the same version as `Klyroo.UI` and build on it. `Klyroo.UI.Server` depends on exactly that version.

## Installation

```bash
dotnet add package Klyroo.UI --prerelease
```

Register the services in the server `Program.cs` and, for a WebAssembly or Auto app, in the `.Client` project with the
same configuration:

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi();
```

Import the namespace in `_Imports.razor`:

```razor
@using Klyroo.UI
```

Load the styles in `<head>` of `App.razor`. Without Tailwind in your app, link the precompiled sheets:

```razor
<KlyColorModeScript />
<KlyThemeStyle />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
```

If your app compiles Tailwind v4, turn on `KlyrooTailwind` in the project file, import the Klyroo theme after Tailwind and
load your own compiled sheet instead:

```xml
<PropertyGroup>
  <KlyrooTailwind>true</KlyrooTailwind>
</PropertyGroup>
```

```css
@import "tailwindcss";
@import "../obj/klyroo/klyroo.theme.css";
```

Remove Bootstrap if the Blazor template added it: CSS outside a cascade layer overrides the layered CSS of Tailwind v4.

## Quick start

Wrap the layout in `KlyApp`, once, around the body. It renders the toaster, resolves the locale and writes `lang` and `dir`:

```razor
@inherits LayoutComponentBase

<KlyApp>
    <KlyMain>
        @Body
    </KlyMain>
</KlyApp>
```

Then use the components:

```razor
@page "/settings"

<KlyFormField Label="Display name">
    <KlyInput @bind-Value="_name" Placeholder="Ada Lovelace" />
</KlyFormField>
<KlySwitch @bind-Value="_notify" Label="Email notifications" />
<KlyPopover>
    <ChildContent><KlyButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="p-3">Opens and closes in the browser, with no round trip.</p></Content>
</KlyPopover>
<KlyButton Label="Save" OnClick="SaveAsync" />

@code {
    private string? _name;
    private bool _notify = true;

    private Task SaveAsync() => Task.CompletedTask;
}
```

The popover has no `@bind-Open`, so .NET is never called when it opens. The save button has an `OnClick`, so it renders disabled until the
page is interactive; use `Href` or `Type="submit"` on a button that must work in a static page.
[Getting started](docs/getting-started.md) builds a complete form step by step.

## Theming

Map the semantic colors to palettes in `AddKlyrooUi`. Klyroo.UI computes the fill, the text color and the on-fill color for
light and dark mode and keeps the contrast ratios.

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi(o =>
{
    o.Colors.Primary = KlyPalette.Blue;
    o.Colors.Neutral = KlyPalette.Zinc;
    o.Colors.Add("tertiary", KlyPalette.Indigo);
    o.Button.Slot("base", "font-semibold");
});
```

Four levels of customization, from the broadest to the narrowest: the palettes, the `--klyroo-*` CSS variables, the theme of
each component in `AddKlyrooUi`, and the `Class` and `Ui` parameters of one instance. See the
[guides](docs/guides.md#change-the-palette-and-the-design-tokens) and [Concepts](docs/concepts.md#components-slots-and-themes).

## Documentation

| | |
| :--- | :--- |
| [Getting started](docs/getting-started.md) | Step-by-step first integration |
| [Guides](docs/guides.md) | Task-oriented recipes |
| [Concepts](docs/concepts.md) | How the library works |
| [Platforms](docs/platforms.md) | Render modes, .NET MAUI, NativeAOT |
| [API reference](docs/api-reference.md) | Every public type and member |
| [FAQ](docs/faq.md) | Troubleshooting and limitations |

## Requirements

- The .NET 10 SDK. Every package targets `net10.0`.
- Static server rendering, Interactive Server, WebAssembly and Auto, and a .NET MAUI `BlazorWebView`. The components use the native popover API, so a web view needs iOS 17 or Mac Catalyst 17, or an Android System WebView of Chrome 114 or later.
- `Klyroo.UI`, `Klyroo.UI.Images` and `Klyroo.UI.Server` are marked trimmable. NativeAOT is not declared for any package.
- Fonts, icons and Tailwind are resolved by the build, which needs the network once (or a filled cache). Tailwind needs no Node.

## Klyroo ecosystem

Klyroo is a family of independent .NET libraries that compose. Each one has its own repository, versions and releases.

| Library | Description |
| :--- | :--- |
| **[Klyroo.UI](https://github.com/klyroo/klyroo-ui)** | Accessible Blazor components for the web and .NET MAUI Blazor Hybrid, styled with Tailwind CSS v4 |
| [Klyroo.Query](https://github.com/klyroo/klyroo-query) | Typed query model, URL state sync, and IQueryable / EF Core execution |
| [Klyroo.Table](https://github.com/klyroo/klyroo-table) | Data lists with table and card-grid layouts, filters, selection and actions |
| [Klyroo.Form](https://github.com/klyroo/klyroo-form) | Schema-driven forms, validation and wizards |
| [Klyroo.Dashboard](https://github.com/klyroo/klyroo-dashboard) | Dashboard controller, filters and Blazor blocks: stats, gauges, charts, tables and feeds |
| [Klyroo.Display](https://github.com/klyroo/klyroo-display) | Read-only display blocks, media player controls and file preview |
| [Klyroo.Spreadsheet](https://github.com/klyroo/klyroo-spreadsheet) | Import wizard for CSV, TSV and pasted data, with column and value mapping |
| [Klyroo.Agent](https://github.com/klyroo/klyroo-agent) | Bridge that lets AI agents read and act on Klyroo forms, dashboards and imports |
| [Klyroo.Stack](https://github.com/klyroo/klyroo-stack) | Meta-package, MCP documentation server and `dotnet new` templates |

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request, and follow the
[code of conduct](CODE_OF_CONDUCT.md). Report security issues privately as described in [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
