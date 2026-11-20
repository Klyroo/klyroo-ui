# Changelog

All notable changes to Blux.UI are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html) (while below 1.0, a minor version may contain breaking changes).

## [Unreleased]

### Added

- **AI tooling**, generated from the docs catalog and the library so it cannot go stale: the `Blux.UI.Mcp` dotnet tool (`blux-ui-mcp`, an MCP server over stdio),
  the same server over HTTP at `/mcp` on the docs site (`X-MCP-Tools` restricts the tools), `/llms.txt` and `/llms-full.txt`, and the `blux-ui` agent skill in `skills/blux-ui`.
  An "AI" section in the docs explains how to install each.
- Design system like Nuxt UI's: `BxPalette` (the 26 Tailwind v4 palettes, `BxPalette.Custom`, and `BxPalette.Blux` for the hand-tuned defaults), `o.Colors.Primary` to `o.Colors.Neutral`, `o.Colors.Add("tertiary", palette)` and `BxThemeStyle`, which renders the computed `:root` and `.dark` variables and a `[data-color]` rule for every extra color. The fill, text and on-fill tokens are picked from the shades so the contrast rules hold.
- `o.Defaults.Color` and `o.Defaults.Size`, used by components when the parameter is not set.
- Tokens `--blux-text-toned`, `--blux-text-inverted`, `--blux-bg-inverted`, `--blux-border-muted`, `--blux-border-inverted`, `--blux-container`, `--blux-header-height`, and the chart series colors `--blux-chart-1` to `--blux-chart-6` (with `-text` and `-contrast`), which are unset by default and fall back to the semantic colors at the element.
- `o.Colors.AllowLowContrast`: by default a palette that cannot meet the contrast rules throws an `InvalidOperationException` naming the color, the palette and the rule when the theme is first resolved; set it to keep the best combination found.
- Tailwind mode utilities: `text-default`, `text-muted`, `bg-elevated`, `border-accented`, `bg-primary`… that follow the runtime variables, and `rounded-*`, computed from `--blux-radius` at the element, so changing it (globally or on a subtree) resizes every `rounded-*` utility in Tailwind mode.
- Docs: Design system and CSS variables guides, and the ThemeStyle page.
- **Self-hosted fonts**, like `@nuxt/fonts` in Nuxt UI. Declare `<BluxFont Include="Public Sans" Role="sans" Weights="400;700" />` items in the app project: the build downloads the woff2 files from Google Fonts, Bunny Fonts or Fontsource (or reads local files), hosts them as static web assets under `/_blux/fonts/` with content-hashed names, and generates `blux.fonts.css` (`@font-face` with `unicode-range` per subset, a metrics-adjusted fallback face per family, `--blux-font-sans` and `--blux-font-mono`). `BxFonts` links it and preloads the main faces. Cached in `obj/blux/fonts-cache`, offline builds work from the cache, a font that cannot be resolved is a warning in Debug and an error in Release, `BluxFonts=false` turns it off. The MSBuild task lives in `Blux.UI.Build.Tasks.dll` (netstandard2.0), shipped in the package.
- Tokens `--blux-font-sans` and `--blux-font-mono`, followed by the preflight, `font-sans` and `font-mono` in both CSS modes.
- **Internationalization**, equivalent to the locale support of Nuxt UI. A `BxLocale` (code, name, direction, messages) carries every text a component renders. Twelve built-in locales (`BxLocales.En`, `Fr`, `De`, `Es`, `It`, `Pt`, `PtBr`, `Nl`, `Ja`, `ZhCn`, `Ar` (right to left), `Ru`, and `BxLocales.All`); `BxLocales.Extend` derives one, `new BxLocale(...)` creates one.
- Automatic locale: components follow `CultureInfo.CurrentUICulture` (exact code, then parent cultures, then English: the same steps as `UseRequestLocalization`, so `zh-TW` gets English), cached per culture, the same in every render mode. `BxApp Locale="..."` and `AddBluxUi(o => o.Locale = ...)` override it, `o.Locales` limits and extends the choice. `BxApp` writes `lang` and `dir` from the locale; `BxLocales.Resolve(culture)` does the same for `App.razor`.
- `BluxCulture.ApplyServerCultureAsync` gives a WebAssembly client the culture the server rendered with (the `<BxCultureMeta />` element, then the culture cookie, then the `lang` attribute of `html`; the cookie and the attribute are checked against an optional list of supported cultures), instead of the language of the browser. `ApplyCultureCookieAsync` is obsolete. The cookie is parsed completely: `CurrentCulture` comes from `c` and `CurrentUICulture` from `uic`.
- Dates, numbers and 12 or 24 hour clocks follow the regional `CultureInfo.CurrentCulture` when its language is the one of the locale (an `en-GB` user with the English locale keeps `20/11/2026`, Monday first, 24 hours), otherwise the culture of the locale. An explicit `Culture` parameter wins. File sizes, AM/PM, the modifier key of shortcuts and the slash menu of the editor are localized, and the calendar heading and the carousel role descriptions too.
- `BxApp` cascades its locale as a fixed value: a navigation no longer re-renders every component below it. Changing `Locale` re-creates the subtree. `BluxOptions.Locales` is copied when assigned, and `Locale` and `Locales` become read-only when the first locale is resolved (like the themes).
- The sort buttons of Table (tooltip) and the summary of Form (accessible name) use their messages.

### Changed

- The light secondary, success, info and error fills and their text colors are slightly darker, so white text on them stays at 4.5:1 on the hover and pressed states (the fills at 90% and 80% over the page).
- `BxChart` series default to `data-color="chart-1"` to `chart-6`, which resolve to the semantic colors at the element (same look, and a `--blux-primary` set on a subtree reaches the charts inside it).
- The header height is `--blux-header-height` (4rem on `:root`), and `BxHeader`, `BxMain` and the page aside read `var(--bx-header-height, var(--blux-header-height))` at the element: either variable works on `:root` or on a subtree, and `--bx-header-height` wins when both are set. The container width reads `--blux-container`.
- Dark borders follow the surfaces: with a configured neutral palette, `--blux-border` and `--blux-border-muted` are picked (800 and 700 in dark mode, 200 in light, moved toward more contrast) so they differ by at least 1.15:1 from the page, overlay, muted and elevated surfaces. The Blux default borders are `#d8d8d3` (light) and `#272b2f` (dark), at least 1.17:1 and 1.19:1.
- `BxThemeStyle` with a `Scope` writes every token in both modes, so a preview is exact inside a configured page. A token that changes in one mode is written in the other too, so a light-only change no longer leaks into dark mode.
- `BxFileUpload`, the send button of `BxChatPrompt`, the badge of `BxBlogPost` and `BxChangelogVersion`, the buttons of `BxPricingTable` and `PageLinkItem.Color` (now nullable) follow `o.Defaults`. The Design system guide lists which components do.
- `oklch()` palette colors follow CSS Color 4: a percentage chroma is scaled (100% is 0.4), the hue accepts `deg`, `grad`, `rad` and `turn`, and `NaN`, `Infinity` and trailing characters are rejected with an `ArgumentException` naming the palette and the value.
- **Breaking**: `BluxOptions.Strings` and the mutable `BluxStrings` are replaced by the immutable `BxMessages` record carried by a locale: `o.Locale = BxLocales.Extend(BxLocales.En, messages: m => m with { Close = "Dismiss" })`. `BxLocale` is now the locale type (`BxLocale(Code, Name, Dir, Messages)`); `BxLocaleSelect.Locales` takes them and defaults to `BluxOptions.Locales`.
- **Breaking**: `BxInputNumber.Locale` (a culture name) is now `Culture` (a `CultureInfo`), like the other components.
- `BxApp` writes `lang` and `dir` from the locale when `Lang` and `Dir` are not set.
- Right to left: Slideover and Drawer slide in from their logical side, the switch thumbs, the calendar and carousel arrows and the chat jump button mirror, and the marquee runs the other way.

### Removed

- The unused `Loading` message.

## [0.1.0] - Unreleased

First public release. Components work in Interactive Server, WebAssembly, Auto and static server rendering; UI state
(open, close, focus, keyboard, selection) runs in the browser through custom elements, so overlays work without a .NET circuit.

### Added

- **Elements**: Button, Badge, Avatar, AvatarGroup, Card, Alert, Banner, Chip, Container, Empty, FieldGroup, Icon, Kbd, Link, Progress, Separator, Skeleton, User.
- **Form**: Input, Textarea, Checkbox, CheckboxGroup, Switch, RadioGroup, Select, SelectMenu, InputMenu, InputNumber, InputTags, InputRating, InputDate, InputTime, PinInput, Slider, Toggle, ToggleGroup, Listbox, Calendar, ColorPicker, FileUpload, Form and FormField (EditForm integration, `aria-invalid`, posted names).
- **Overlay**: Modal, Drawer, Slideover, Popover, Tooltip, DropdownMenu, ContextMenu, CommandPalette, Toast with `IToastService`, and the ColorMode components (Button, Switch, Select, Avatar, Image).
- **Navigation**: Tabs, Accordion, Breadcrumb, Pagination, Stepper, Timeline, NavigationMenu, Collapsible, Tree, Marquee, ScrollArea, Splitter, and the Content components (ContentNavigation, ContentSearch, ContentSearchButton, ContentSurround, ContentToc).
- **Data**: Table, Chart, Carousel.
- **Layout**: App, Header, Footer, FooterColumns, Main, Sidebar, Error, LocaleSelect, Theme.
- **Page**: Page, PageHeader, PageHero, PageSection, PageFeature, PageGrid, PageColumns, PageCard, PageCta, PageLinks, PageList, PageLogos, PageAnchors, PageAside, PageBody, PricingPlan, PricingPlans, PricingTable, BlogPost, BlogPosts, ChangelogVersion, ChangelogVersions, AuthForm.
- **Dashboard**: DashboardGroup, DashboardSidebar (with collapse and toggle), DashboardPanel, DashboardResizeHandle, DashboardNavbar, DashboardToolbar, DashboardSearch, DashboardSearchButton.
- **Chat**: ChatMessage, ChatMessages, ChatPrompt, ChatPromptSubmit, ChatPalette, ChatAttachment, ChatReasoning, ChatShimmer, ChatTool.
- **Editor**: Editor, EditorToolbar, EditorDragHandle, and the suggestion, mention and emoji menus (rich text engine in `blux-editor.js`).
- Theme engine with `AddBluxUi(options => ...)`, per-slot `Ui` overrides, `Class` merging, `--blux-*` tokens, seven built-in colors plus custom colors, light and dark mode (`BxColorModeScript`).
- Two styling modes: precompiled `blux.css` with `blux.preflight.css`, or Tailwind v4 compiled by the app with `obj/blux/blux.theme.css` copied by the package's `buildTransitive` targets.
- Package: XML documentation, SourceLink, embedded untracked sources and a symbol package (`.snupkg`), MIT license.

