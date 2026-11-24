# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Primary**: .NET 10 and Blazor developers and tech leads building accessible, responsive web applications (Static SSR, Interactive Server, WebAssembly, Auto) and `.NET MAUI Blazor Hybrid` apps.
- **Secondary**: Authors of higher-level LuxStack libraries (`Lux.Table`, `Lux.Form`, `Lux.Dashboard`, `Lux.Display`, `Lux.Spreadsheet`, `Lux.Agent`) who build directly on `Lux.UI` primitives and design tokens.

## Product Purpose

`lux-ui` is the foundational component library, DOM behavior layer, font/image pipeline, and application template suite for LuxStack. It provides 133 accessible Blazor components (`Lx*`), zero-roundtrip native Web Component behaviors, self-hosted fonts, on-demand local image optimization, and 9 complete starter templates.

Success means interactive primitives (menus, dialogs, popovers, tabs, roving focus, form controls) respond instantaneously at 60fps in the browser, degrade cleanly in Static SSR, and integrate seamlessly with Tailwind CSS v4 or precompiled CSS.

## Positioning

Unlike Blazor suites that round-trip every menu open, tab switch, or tooltip hover over SignalR or lock teams into Material/proprietary SCSS, `Lux.UI` pairs native browser primitives and composable DOM behaviors (`dialog`, `popup`, `rovingTabindex`, `formAssociated`) with Tailwind CSS v4 tokens (`--lux-*`), self-hosted build-time fonts (`Lux.UI.Build.Tasks`), and full Static SSR + Server + WASM + MAUI Hybrid compatibility under MIT.

## Operating Context

- **Packages**: `Lux.UI`, `Lux.UI.Build.Tasks`, `Lux.UI.Server`, `Lux.UI.Images`, and `Lux.UI.Images.Local`.
- **Component families**: Elements, Form, Overlay, Navigation, Data, Layout, Page, Dashboard, Chat, and Editor (`Lx*` components).
- **Starter templates**: 9 runnable Blazor app templates under `templates/` (`starter`, `landing`, `saas`, `dashboard`, `docs`, `changelog`, `editor`, `chat`, `portfolio`).
- **Consumption modes**: Tailwind-in-the-app compilation or precompiled CSS, with automatic culture/RTL (`LxLocales`, `dir`) and light/dark color-mode plumbing (`LxColorModeScript`, `LxThemeStyle`).

## Capabilities and Constraints

- Targets `.NET 10` across Static SSR, Interactive Server, WebAssembly, Interactive Auto, and `.NET MAUI Blazor Hybrid`.
- Zero-roundtrip browser behaviors must remain lightweight, framework-agnostic, and free of heavy JS runtime frameworks.
- Components expose semantic slots (`<Body>`, `<Footer>`, etc.) and strict naming (`Lx*` components, `--lux-*` CSS custom properties, `lx-*` classes/attributes, `@onlx*` events).
- Built-in localization and RTL support across all bundled locales (`LxLocales`).
- 100% MIT open-source license.

## Brand Commitments

- **Name**: `Lux.UI` (`lux-ui`), part of the **LuxStack** ecosystem.
- **Voice**: Technical, precise, and developer-focused — emphasizing web standards, accessibility, and composability.
- **Naming invariant**: Exclusively `Lux` / `Lx*` / `--lux-*` / `lx-*` across public APIs, CSS, JS, and docs.

## Evidence on Hand

- 133 implemented Blazor components across 10 component families in `src/Lux.UI`.
- Extensive .NET unit/bUnit test suites and JS runtime tests (`tests/`) passing in CI, plus NativeAOT/trimming verification.
- 9 runnable reference templates in `templates/` verified live via `Lux.Stack.Site`.
- **Explicit absences (do not fabricate)**: No NuGet download stats, customer logos, or third-party testimonials exist yet.

## Product Principles

1. **Zero-roundtrip interactions**: Browser-local UI state (popovers, dialogs, menus, roving focus) runs immediately in the browser without waiting on a server circuit.
2. **Web standards over custom reimplementations**: Prefer native `<dialog>`, Popover API, form-associated semantics, and standard ARIA patterns.
3. **Static SSR resilience**: Core navigation, theming, localization, and layout primitives work even without an active interactive runtime.
4. **Composable tokens and slots**: Every component is customizable via `--lux-*` semantic tokens and Tailwind utilities without fighting specificity.
5. **Accessible by default**: Keyboard navigation, focus trapping/restoration, RTL, and screen-reader semantics are built into every primitive.

## Accessibility & Inclusion

- Durable standard: **WCAG 2.1 AA** across all components and templates.
- Required behaviors: WAI-ARIA APG keyboard interactions, roving tabindex, focus restoration on overlays, visible `:focus-visible` rings, skip links, semantic landmarks, and full RTL/locale support.
