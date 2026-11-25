# Migrating from Lux to Klyroo

**Klyroo.UI** was developed as part of the LuxStack project, in the `lux-ui` repository. Klyroo is its new name.
No package was published to NuGet under the Lux name, so this guide only concerns code written against the Lux sources.
The rename is a breaking change for source code, markup, styles and build settings; the behavior of the library is
unchanged. There are no compatibility aliases: the project was in
preview, and keeping a second set of names would have doubled the public surface for every future release.

## Packages

| Before | After |
| :--- | :--- |
| `Lux.UI` | `Klyroo.UI` |
| `Lux.UI.Server` | `Klyroo.UI.Server` |
| `Lux.UI.Images` | `Klyroo.UI.Images` |
| `Lux.UI.Images.Local` | `Klyroo.UI.Images.Local` |

```bash
dotnet remove package Lux.UI
dotnet add package Klyroo.UI --prerelease
```

## Namespaces and types

Replace the root namespace `Lux` with `Klyroo` (`using Lux.UI;` becomes `using Klyroo.UI;`).
Types and members whose name carried the brand follow the same rule:

| Before | After |
| :--- | :--- |
| `Lux...` types (`LuxOptions`, `Lux...ServiceCollectionExtensions`) | `Klyroo...` (`KlyrooOptions`, `Klyroo...ServiceCollectionExtensions`) |
| `services.AddLux...()` | `services.AddKlyroo...()` |
| `dotnet new` templates `lux-*` | `klyroo-*` |

## Razor, CSS and JavaScript

| Before | After |
| :--- | :--- |
| Components `<LxButton>`, `<LxDataList>`, ... | `<KlyButton>`, `<KlyDataList>`, ... (`Lx` prefix becomes `Kly`) |
| Interfaces `ILx...`, records `Lx...` (`LxSelectItem`, `LxColor`) | `IKly...`, `Kly...` (`KlySelectItem`, `KlyColor`) |
| Options `LuxOptions`, `AddLux...()` | `KlyrooOptions`, `AddKlyroo...()` |
| Design tokens `--lux-*` (`--lux-primary`) | `--klyroo-*` (`--klyroo-primary`) |
| Internal tokens `--lx-*` | `--kly-*` |
| Attributes `data-lx-*` | `data-kly-*` |
| Custom elements `<lx-modal>`, `<lx-popover>`, ... | `<kly-modal>`, `<kly-popover>`, ... |
| Blazor events `@onlxchange`, `@onlxtoggle`, `@onlxlimit`, `@onlxsearch` | `@onklychange`, `@onklytoggle`, `@onklylimit`, `@onklysearch` |
| Generated ids `lx-<name>-...` | `kly-<name>-...` |
| Stylesheets `lux.css`, `lux.theme.css`, `lux-<library>.css` | `klyroo.css`, `klyroo.theme.css`, `klyroo-<library>.css` |
| JavaScript global `window.Lux` | `window.Klyroo` |
| Browser storage and cookies `lux-*` (color mode, sidebar, columns) | `klyroo-*` |

Saved browser state (color mode, collapsed sidebars, column choices) is stored under the new keys, so users see
the defaults once after the upgrade.

The default primary color moved from the Lux yellow (`#ffd23f`) to **Klyroo Lime** (`#b6ff00`) on Charcoal
(`#0b0f14`), with a darker lime (`#3b5900`) for primary text on light surfaces so it keeps WCAG AA contrast.
Apps that set their own palette are not affected.

## Build integration (Klyroo.UI)

| Before | After |
| :--- | :--- |
| MSBuild properties `Lux*` (`LuxTailwind`, `LuxCacheDir`, ...) | `Klyroo*` (`KlyrooTailwind`, `KlyrooCacheDir`, ...) |
| MSBuild items `<LuxFont>`, `<LuxIcon>` | `<KlyrooFont>`, `<KlyrooIcon>` |
| Diagnostics `LUXFONT###`, `LUXICON###`, `LUXTW###`, `LUXTOOLS###` | `KLYFONT###`, `KLYICON###`, `KLYTW###`, `KLYTOOLS###` |
| Generated files under `obj/lux/` | `obj/klyroo/` (the app stylesheet imports `../obj/klyroo/klyroo.theme.css`) |
| Static web assets under `_lux/` (fonts, images) | `_klyroo/` |
| Tailwind binary cache `~/.lux/tailwind` | `~/.klyroo/tailwind` |

Update `<NoWarn>` / `<WarningsAsErrors>` lists that name the old diagnostic codes, and the `@import` of the theme in
your Tailwind input stylesheet.

## Find what is left

From the root of your solution, list the old names that remain:

```bash
grep -rnE '\bLux\.|\bLx[A-Z]|--lux-|--lx-|data-lx-|<lx-|onlx|\bLux__|LUX(FONT|ICON|TW|TOOLS)' \
  --include='*.cs' --include='*.razor' --include='*.cshtml' --include='*.css' --include='*.js' \
  --include='*.csproj' --include='*.props' --include='*.targets' --include='*.json' .
```
