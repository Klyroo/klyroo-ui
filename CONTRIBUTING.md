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
   `outline-(--bx-color-text)` or `outline-(--blux-primary-text)`. Sizes are `BxSize` (xs to xl) where they apply. A color token added to `blux.base.css` also goes into the default
   tables of `Theming/BxThemeTokens.cs` (a test compares them), and a solid fill must keep 4.5:1 for its `-contrast` text at
   the `/90` and `/80` hover and pressed states.
7. **Static ids and loops.** Ids come from `NewId(...)`. In a Razor `for` loop, copy the index into a local before any
   child component content or lambda uses it.
8. **No text in components.** Every text a person reads or hears (labels, placeholders, announcements, units, key names) is a
   `BxMessages` property read as `Messages.X` (and `CurrentLocale` for the culture), never a literal. Browser code gets texts through
   `data-*` attributes rendered by .NET; a JS default is a last resort. Date and number formatting defaults to
   `CurrentLocale.Culture`, an explicit `Culture` parameter wins. Use logical utilities (`ms-`, `pe-`, `start-`, `rounded-s`) and give
   arrows and chevrons an `rtl:` variant. The tests fail when a locale lacks a message or changes its placeholders.
9. **Interop is primitive.** Calls into JS pass strings and numbers, never anonymous objects (trimming).

## Where things go (one family = one set of files)

| What | Where |
|---|---|
| Component | `src/Blux.UI/Components/<Name>/Bx<Name>.razor` (+ `.razor.cs`), item records next to it |
| Slot overrides | `<Name>Ui` record implementing `ISlotOverrides`, in the component folder |
| Theme | `src/Blux.UI/Theming/Themes.<Family>.cs`: `internal static partial class Themes` |
| Options | `src/Blux.UI/Theming/BluxOptions.<Family>.cs`: `public sealed partial class BluxOptions` (theme properties) |
| Texts | `src/Blux.UI/Localization/Messages/BxMessages.<Family>.cs`: `public sealed partial record BxMessages` (init-only strings, English defaults) |
| Translations | one file per language in `src/Blux.UI/Localization/Locales/<Code>.cs`: add every new message to all twelve |
| Browser behavior | `src/Blux.UI/js/<name>.js`, imported in `js/index.js`; use `js/shared.js` helpers |
| Docs examples | `playground/Blux.Playground/Blux.Playground.Client/Examples/<Name><What>Example.razor` (unique names) |
| Docs catalog | `Docs/Catalog.<Family>.cs`: `public static partial class Catalog { private static readonly ComponentDoc[] <Family> = [...]; }`, then add `.. <Family>` to `Components` in `Catalog.cs` |
| Unit tests | `tests/Blux.UI.Tests/<Family>Tests.cs`, deriving from `BluxTestContext` |
| E2E tests | `tests/Blux.UI.E2E/<Family>Tests.cs`: an axe theory over the family's slugs in all four modes, plus behavior tests, **bound and unbound**, and after an enhanced navigation for elements that set attributes |

Form controls derive from `BxFieldBase<T>` (EditForm integration, `FormFieldContext`); custom markup that must post
uses `EditBridge` and a hidden input, like `BxSelect`. Overlays build on `<bx-popover>` / `<bx-modal>` patterns.

## Adding a component to Blux.UI.Tools

`src/Blux.UI.Tools` is the home of data-heavy components (the data list and its parts). It follows the rules above, with these differences:

| What | Where |
|---|---|
| Component | `src/Blux.UI.Tools/Components/<Family>/Bx<Name>.razor`, deriving from `BxToolsComponentBase` (it adds `ToolsOptions` and `ToolsMessages` to `BxComponentBase`) |
| Theme | `Theming/Themes.<Family>.cs` (`internal static partial class ToolsThemes`) and a `ComponentTheme` property in `Theming/BluxToolsOptions.<Family>.cs`. The property is named after the component without `Bx` (`BxDataListEmpty` is `DataListEmpty`), which is how the docs bundle finds it. |
| Resolving a theme | `ResolveSlots(ToolsOptions.<Name>, variants, Ui, "root")`: the same call as a core component, so `Class`, `Ui`, `BxTheme` scopes and the freeze of themes behave the same |
| Texts | `Localization/BxToolsMessages.<Family>.cs`, and the value in each of the twelve `Localization/Locales/<Code>.cs` files (the codes are those of `BxLocales.All`; a test fails when one is missing or a placeholder changes). English and French are reviewed; the other ten are drafts, marked with a comment and absent from `BxToolsLocales.ReviewedCodes`, until a native speaker reviews them. |
| Icons | add the Lucide name to `build/tools-icons.json`; `npm run icons` writes `Icons/ToolsIcons.g.cs`, registered through `BluxIconBundle` (an icon Blux.UI already has needs no entry) |
| Browser behavior | `js/index.js` (the second JS initializer, bundled to `wwwroot/Blux.UI.Tools.lib.module.js`). Report through the `bx-change` and `bx-toggle` events of Blux.UI; register no event type of your own. Helpers of `src/Blux.UI/js/shared.js` can be imported by relative path. |
| CSS | `npm run build` extracts the classes of the Tools themes (`classes:tools`) into `styles/generated/classes.txt` and `wwwroot/blux-tools.theme.css`, and compiles `wwwroot/blux-tools.css` (`css:tools`). Both are committed. |
| Docs | `Docs/Catalog.DataList.cs` (or a new `Catalog.<Family>.cs`), examples as usual. The bundle gives the component a `Package` of `Blux.UI.Tools`, and the tools, `llms.txt` and the skill reference say `dotnet add package Blux.UI.Tools`. |
| Tests | `tests/Blux.UI.Tools.Tests` (bUnit, deriving from `BluxToolsTestContext`), and E2E in `tests/Blux.UI.E2E` as for core |

`BxDataListEmpty` is the smallest example of all of this. Two cautions:

- `blux-tools.css` is a second stylesheet loaded after `blux.css`, each with its own `@layer utilities`. Within one element the class merger drops conflicting
  utilities, but a longhand in one sheet and a shorthand in the other (`px-2` in `blux.css`, `p-6` in `blux-tools.css`) are ordered by sheet, and the
  later sheet wins. In a Tools theme, do not rely on a core utility to override a shorthand written in the Tools theme of the same element: use the same property group.
- `styles/blux-tools.tokens.css` is a verbatim copy of the token mapping at the top of `styles/blux.base.css`; a test fails when they differ.

## Documentation site

The documentation site lives in `playground/`. Run it with
`ASPNETCORE_ENVIRONMENT=Development dotnet run --project playground/Blux.Playground/Blux.Playground`, then open `/`.
Component pages are at `/components/{name}` (Auto mode) and `/{ssr|server|wasm|auto}/components/{name}`.
The site compiles its own Tailwind (`npm run site:css`), so it exercises the "Tailwind in the app" mode.
Generated files (`src/Blux.UI/wwwroot`, `src/Blux.UI/styles/generated`, the same two folders of `src/Blux.UI.Tools`, `src/Blux.UI.Tools/Icons`, the site's `wwwroot/site.css`) are committed;
CI fails if `npm run build` changes them.

## Images

`BxImage` and the provider abstraction (`IBxImageProvider`, `BxImageSettings`, in `src/Blux.UI/Images`) live in the library. Components that show a
picture render a `BxImage` and expose `ImageOptions`; never write an `img` by hand. `src/Blux.UI.Images` holds the CDN providers
(read the vendor's documentation, cite its page in the XML comment, and test against the examples of that page) and `src/Blux.UI.Images.Local` the
endpoint, whose limits are part of its security: a new parameter must be clamped, snapped or rejected in `BxImageRequestParser` with a test. Both
packages share the version of `build/Version.props` and are packed by CI and the release workflow.

## AI tooling

The MCP server (`src/Blux.UI.Mcp`, shared code in `src/Blux.UI.Mcp.Core`), `llms.txt` and the skill reference read one docs bundle that
`build/Blux.UI.DocsBundle` generates from the docs catalog, the XML documentation and themes of the library, the example sources and the
guide pages. Nothing generated is committed, except the skill's component reference. So:

- A new component needs nothing extra beyond its catalog entry and examples; the bundle, the tools and `llms.txt` pick it up on the next build.
- A guide page needs a line in `Catalog.Guides` and a Razor page with the same route, whose prose sits in `<div class="pg-prose">` (headings, paragraphs, lists, `CodeBlock`).
- After changing components or the catalog, refresh `skills/blux-ui/references/components.md` with
  `dotnet run --project build/Blux.UI.DocsBundle -- --skill-reference`; a test fails while it is stale.
- Every Razor sample in `skills/blux-ui` is checked against the library by `tests/Blux.UI.Mcp.Tests`, so a renamed parameter shows up there.
- The docs site serves `/mcp`, `/llms.txt` and `/llms-full.txt` (`playground/.../AiEndpoints.cs`). In production set `Blux:SiteUrl` (env `Blux__SiteUrl`)
  to the public address: the links then never come from the request's `Host` header and the text is generated once. Behind a TLS proxy the
  forwarded headers (`X-Forwarded-Proto/Host/For`) are honoured from loopback and from the IPs in `Blux:KnownProxies`. `/mcp` is limited per client IP
  (`Blux:Ai:RequestsPerMinute`, default 120) and times out after `Blux:Ai:RequestTimeoutSeconds` (default 30). Input to the search is capped
  (`DocsService.MaxQueryLength`, `MaxQueryTokens`) so a long query cannot cost more CPU than a short one.
- Document parameters with XML comments: they are the descriptions the tools return.

## Commands

```bash
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
npm ci                     # or symlink node_modules from the main checkout
npm run build              # theme classes, blux.css, JS bundle, site.css: commit the results
dotnet build Blux.UI.slnx  # warnings are errors
dotnet test --project tests/Blux.UI.Tests
dotnet test --project tests/Blux.UI.Mcp.Tests   # bundle, MCP tools, llms.txt, skill
dotnet test --project tests/Blux.UI.Tools.Tests  # Blux.UI.Tools: components, messages, generated assets
dotnet test --project tests/Blux.UI.Images.Tests         # provider URLs
dotnet test --project tests/Blux.UI.Images.Local.Tests   # optimizer parameters, security, resizing
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
`VersionPrefix` and `VersionSuffix` in `build/Version.props`, imported by the library and the dotnet tool (an empty suffix is a stable release).

1. Make sure `main` is green: build, unit tests, and `npm run build` leaves the generated assets unchanged.
2. In `CHANGELOG.md`, move the `[Unreleased]` content into a `## [X.Y.Z] - YYYY-MM-DD` section (Added, Changed, Fixed,
   Removed) and leave a fresh empty `## [Unreleased]` above it.
3. Set `VersionPrefix` / `VersionSuffix` in `build/Version.props` to the release version.
4. Run the packaging check locally: `node build/verify-package.mjs`. It packs in Release, checks the `.nupkg` and `.snupkg`
   contents, and builds a throwaway Blazor Web App against the package in both CSS modes. It restores from the local NuGet
   cache; add `--online` if framework packages are missing from it.
5. Commit (`Release X.Y.Z`), then tag `vX.Y.Z` on that commit and push the branch and the tag.
6. The `Release` workflow (`.github/workflows/release.yml`, triggered by `v*` tags only) checks that the tagged commit is
   on `main`, that the tag equals the package version and that the changelog section is dated, rebuilds, tests and runs
   the packaging check. A separate `publish` job then pushes the `.nupkg` and `.snupkg` of Blux.UI and of the
   `blux-ui-mcp` tool (Blux.UI.Mcp) and of the image packages (Blux.UI.Images, Blux.UI.Images.Local), all with the same version, to nuget.org with the
   `NUGET_API_KEY` secret of the `nuget` environment. Create that environment in the repository settings, with required
   reviewers if every release should be approved by hand.

Before the first release, fill in `RepositoryUrl` and `PackageProjectUrl` (marked TODO) in `Blux.UI.csproj` once the
repository is hosted, so nuget.org shows the repository link and SourceLink can map sources. Until then the package
carries no repository URL, even when built in CI.
