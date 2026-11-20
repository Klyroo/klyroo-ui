# Adding components to blux-ui

blux-ui follows Nuxt UI's catalog and API where it makes sense, adapted to Blazor and to its render modes.
Reference for a component's features: `https://ui.nuxt.com/raw/docs/components/<slug>.md`.
Read `PLAN.md` for the design decisions; this file is the checklist.

## Rules that are not negotiable

1. **Every render mode.** Each component works in Interactive Server, WebAssembly, Auto and static server rendering.
   What can only work with interactivity must say so in its XML docs and degrade honestly (a `BxButton` with
   `OnClick` renders disabled until the page is interactive).
2. **UI state lives in the browser.** Opening, closing, focus, keyboard navigation, highlighting and selection run in
   a custom element (`src/Blux.UI/js/<name>.js`). .NET only hears about state that is bound
   (`ValueChanged.HasDelegate` / `OpenChanged.HasDelegate`); unbound, no handler is attached at all.
3. **The JS/.NET protocol.** A custom element reports changes with `bx-toggle` (`open`) or `bx-change` (`value`),
   through `Reporter` from `js/shared.js`, which adds a sequence number. The component:
   - renders `data-bx-seq="@(Bound ? _sequence : null)"` as the **first** attribute of the custom element;
   - stores `e.Sequence` in its handler before anything else;
   - adds `@onbx...:stopPropagation="true"` so nested components don't trigger each other;
   - puts `[DynamicDependency(PublicConstructors | PublicProperties, typeof(BxChangeEventArgs))]` (or the toggle args)
     on the handler, or trimming removes the event args.
   The element observes `data-bx-seq` and its state attribute, ignores renders older than its last report
   (`reporter.isStale()`), and re-applies the rendered state in a `queueMicrotask` when `data-bx-seq` changes.
   Reuse `bx-change` / `bx-toggle`; a new event type needs registering in `js/index.js` and `EventHandlers.cs`.
   Read the rendered value with `reporter.take(attr)`: it returns it only when the attribute changed or a bound render
   arrived, so an **unbound** element never undoes the user when Blazor re-renders or children change.
   Give the element a `refresh()` that calls `reporter.adopt()` and re-applies what an enhanced-navigation merge resets
   (attributes it set, tabindex, hidden inputs); every `bx-*` element with `refresh()` is called after `enhancedload`.
4. **Never fight Blazor over the DOM.** The element may set attributes Blazor does not render, and may change
   attributes Blazor rendered only when Blazor would render the same value afterwards (bound) or never re-render them
   (unbound). It never replaces text nodes or elements Blazor rendered: put a label in an attribute and show it with
   `before:content-[attr(data-label)]`, or append into an element Blazor renders empty.
   Custom elements wire themselves with `observeChildren` (Blazor inserts an element before its children).
5. **Accessibility.** Follow the WAI-ARIA Authoring Practices pattern of the component; native elements first
   (`<dialog>`, `popover`, `<input>`, `<details>` only for disclosure). Text at least 4.5:1, control boundaries and
   state indicators at least 3:1, in light and dark. Every page must pass axe.
6. **Theme through tokens only.** Colors come from `--blux-*` tokens and the `data-color` variables
   (`--bx-color`, `--bx-color-text`, `--bx-color-contrast`); no Tailwind palette colors in themes. Focus outlines use
   `outline-(--bx-color-text)` or `outline-(--blux-primary-text)`. Sizes are `BxSize` (xs to xl) where they apply.
7. **Static ids and loops.** Ids come from `NewId(...)`. In a Razor `for` loop, copy the index into a local before any
   child component content or lambda uses it.
8. **Interop is primitive.** Calls into JS pass strings and numbers, never anonymous objects (trimming).

## Where things go (one family = one set of files)

| What | Where |
|---|---|
| Component | `src/Blux.UI/Components/<Name>/Bx<Name>.razor` (+ `.razor.cs`), item records next to it |
| Slot overrides | `<Name>Ui` record implementing `ISlotOverrides`, in the component folder |
| Theme | `src/Blux.UI/Theming/Themes.<Family>.cs`: `internal static partial class Themes` |
| Options | `src/Blux.UI/Theming/BluxOptions.<Family>.cs`: `public sealed partial class BluxOptions` (theme properties) and `public sealed partial class BluxStrings` (texts) |
| Browser behavior | `src/Blux.UI/js/<name>.js`, imported in `js/index.js`; use `js/shared.js` helpers |
| Docs examples | `playground/Blux.Playground/Blux.Playground.Client/Examples/<Name><What>Example.razor` (unique names) |
| Docs catalog | `Docs/Catalog.<Family>.cs`: `public static partial class Catalog { private static readonly ComponentDoc[] <Family> = [...]; }`, then add `.. <Family>` to `Components` in `Catalog.cs` |
| Unit tests | `tests/Blux.UI.Tests/<Family>Tests.cs`, deriving from `BluxTestContext` |
| E2E tests | `tests/Blux.UI.E2E/<Family>Tests.cs`: an axe theory over the family's slugs in all four modes, plus behavior tests, **bound and unbound**, and after an enhanced navigation for elements that set attributes |

Form controls derive from `BxFieldBase<T>` (EditForm integration, `FormFieldContext`); custom markup that must post
uses `EditBridge` and a hidden input, like `BxSelect`. Overlays build on `<bx-popover>` / `<bx-modal>` patterns.

## Documentation site

The documentation site lives in `playground/`. Run it with
`ASPNETCORE_ENVIRONMENT=Development dotnet run --project playground/Blux.Playground/Blux.Playground`, then open `/`.
Component pages are at `/components/{name}` (Auto mode) and `/{ssr|server|wasm|auto}/components/{name}`.
The site compiles its own Tailwind (`npm run site:css`), so it exercises the "Tailwind in the app" mode.
Generated files (`src/Blux.UI/wwwroot`, `src/Blux.UI/styles/generated`, the site's `wwwroot/site.css`) are committed;
CI fails if `npm run build` changes them.

## Commands

```bash
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
npm ci                     # or symlink node_modules from the main checkout
npm run build              # theme classes, blux.css, JS bundle, site.css: commit the results
dotnet build Blux.UI.slnx  # warnings are errors
dotnet test --project tests/Blux.UI.Tests
dotnet build tests/Blux.UI.E2E && dotnet test --project tests/Blux.UI.E2E --no-build -- --filter-class Blux.UI.E2E.<Family>Tests
```

`GeneratedAssetsTests` fails when a theme class is missing from `blux.css` (typo, invalid Tailwind syntax) or
`npm run build` was not run.

## Done means

- The component renders and behaves in all four modes, with a docs entry, live examples and an API table.
- `dotnet build` has no warnings, all unit tests pass, the family's E2E tests pass (Chromium at least).
- Generated assets are rebuilt and committed.

## Cutting a release

Versions follow [Semantic Versioning](https://semver.org). While below 1.0 a minor version (0.2.0) may contain breaking
changes and a patch (0.1.1) only fixes; prereleases carry a suffix (`0.2.0-beta.1`). The version lives in one place,
`VersionPrefix` and `VersionSuffix` in `src/Blux.UI/Blux.UI.csproj` (an empty suffix is a stable release).

1. Make sure `main` is green: build, unit tests, and `npm run build` leaves the generated assets unchanged.
2. In `CHANGELOG.md`, move the `[Unreleased]` content into a `## [X.Y.Z] - YYYY-MM-DD` section (Added, Changed, Fixed,
   Removed) and leave a fresh empty `## [Unreleased]` above it.
3. Set `VersionPrefix` / `VersionSuffix` in `Blux.UI.csproj` to the release version.
4. Run the packaging check locally: `node build/verify-package.mjs`. It packs in Release, checks the `.nupkg` and `.snupkg`
   contents, and builds a throwaway Blazor Web App against the package in both CSS modes. It restores from the local NuGet
   cache; add `--online` if framework packages are missing from it.
5. Commit (`Release X.Y.Z`), then tag `vX.Y.Z` on that commit and push the branch and the tag.
6. The `Release` workflow (`.github/workflows/release.yml`, triggered by `v*` tags only) checks that the tagged commit is
   on `main`, that the tag equals the package version and that the changelog section is dated, rebuilds, tests and runs
   the packaging check. A separate `publish` job then pushes the `.nupkg` and `.snupkg` to nuget.org with the
   `NUGET_API_KEY` secret of the `nuget` environment. Create that environment in the repository settings, with required
   reviewers if every release should be approved by hand.

Before the first release, fill in `RepositoryUrl` and `PackageProjectUrl` (marked TODO) in `Blux.UI.csproj` once the
repository is hosted, so nuget.org shows the repository link and SourceLink can map sources. Until then the package
carries no repository URL, even when built in CI.
