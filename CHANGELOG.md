# Changelog

All notable changes to **klyroo-ui** are documented in this file.

## [0.1.0-preview.1] - 2026-11-24

### Changed
- **BREAKING:** the project joins the Klyroo ecosystem. Packages, namespaces, types, components (`Lx*` to `Kly*`),
  CSS tokens (`--lux-*` to `--klyroo-*`), MSBuild properties and diagnostics are renamed. See
  [MIGRATION.md](./MIGRATION.md).
- Klyroo visual identity: Klyroo Lime (`#b6ff00`) on Charcoal (`#0b0f14`) as the default primary palette.

### Added
- Initial preview release on `.NET 10`: Accessible, themable Tailwind CSS v4 Blazor & .NET MAUI UI component library (133 components, icons, fonts, images).
- Clean two-layer architecture (`*.Core` 100% Headless / NativeAOT + multiplatform Blazor Web & .NET MAUI Blazor Hybrid UI).
- Comprehensive xUnit / bUnit / EF Core SQLite test suite and GitHub Actions CI/Release NuGet pipelines.
- Full English documentation, cross-library integration guides, and interactive live examples at https://klyroo.dev.
- `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `.editorconfig`, issue and pull request templates,
  Dependabot configuration.
- Release workflow with version/tag check, NuGet metadata verification, Trusted Publishing (OIDC), an approval gate
  (`nuget` environment) and GitHub Release notes taken from this changelog.
