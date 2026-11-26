# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `IKlyOverlayService`, a scoped service registered by `AddKlyrooUi`, to open dialogs from code. `ShowAsync<TComponent>(parameters, options)` shows a component in a `KlyModal` or `KlySlideover` and returns a `KlyOverlayResult` that the component completes through the cascading `KlyOverlayContext` (`Close(data)` or `Cancel()`); `ConfirmAsync(title, message, confirmLabel, cancelLabel, color)` returns a `bool`. Escape, the backdrop and the close button are a cancel result, dialogs stack, and a `CancellationToken` closes them. `KlyApp` renders the new `KlyOverlayHost` that shows them (`KlyApp.Overlays` turns it off). New messages `Cancel` and `Confirm` in the twelve locales.
- `KlyPersistedStateBudget`, a scoped service registered by `AddKlyrooUi`, and `KlyrooOptions.PersistedStateMaxBytes` (28 KB): the budget of persistent component state that the Klyroo packages of one page share when they hand prerendered data to the interactive render, counted as sent (`SentBytes` adds the JSON escaping and the two base64 encodings Blazor applies). Klyroo.Table and Klyroo.Dashboard draw on it, so together they stay under SignalR's 32 KB limit on the message that starts an Interactive Server circuit.

### Changed

- The release workflow has a required `verify-package` job that runs `build/verify-package.mjs --packages` on the packed `.nupkg` files before `publish`, and `verify-package.mjs` no longer expects the `Klyroo.UI.Tools` packages that this repository no longer contains. `Directory.Build.props` imports `build/Version.props` once instead of repeating the default version.
- `Klyroo.UI`, `Klyroo.UI.Images` and `Klyroo.UI.Server` declare `IsAotCompatible` (it implies trimmable and turns on the trimming, AOT and single-file analyzers, which report no warning).

## [0.1.0-preview.1] - 2026-11-24

### Added

- 143 accessible Blazor components in thirteen families (elements, form controls, overlays, navigation, data, layout, page sections, dashboards, chat and a rich text editor) that work in static server rendering, Interactive Server, WebAssembly and Auto.
- A design system of CSS variables with semantic colors, palettes mapped in C# with contrast checks, and per-component themes, slot overrides and class overrides at the app, subtree and instance level.
- Two stylesheet modes: a precompiled `klyroo.css`, or a build that compiles your Tailwind CSS v4 stylesheet with the Klyroo theme through the standalone Tailwind CLI, without Node.
- Build-time icons and fonts: any Iconify icon by name, custom SVG collections, and self-hosted fonts with a metrics-adjusted fallback face, with no request to a third party when the page runs.
- `KlyImage` with image providers for Cloudinary, imgix, ImageKit, Cloudflare Images, Netlify, Vercel, Bunny and URL templates (`Klyroo.UI.Images`), and a libvips image endpoint for your own server (`Klyroo.UI.Images.Local`).
- Twelve locales with right-to-left support, culture-aware formatting, a locale select and a way to extend or add a locale.
- .NET MAUI Blazor Hybrid support with a sample project, and `Klyroo.UI.Server` for components that read cookies in static rendering and prerendering.

[Unreleased]: https://github.com/klyroo/klyroo-ui/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/klyroo/klyroo-ui/releases/tag/v0.1.0-preview.1
