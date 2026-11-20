# Changelog

All notable changes to Blux.UI are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html) (while below 1.0, a minor version may contain breaking changes).

## [Unreleased]

### Added

- **Internationalization**, equivalent to the locale support of Nuxt UI. A `BxLocale` (code, name, direction, messages) carries every text a component renders. Twelve built-in locales (`BxLocales.En`, `Fr`, `De`, `Es`, `It`, `Pt`, `PtBr`, `Nl`, `Ja`, `ZhCn`, `Ar` (right to left), `Ru`, and `BxLocales.All`); `BxLocales.Extend` derives one, `new BxLocale(...)` creates one.
- Automatic locale: components follow `CultureInfo.CurrentUICulture` (exact code, parents, language, then English), cached per culture, the same in every render mode. `BxApp Locale="..."` and `AddBluxUi(o => o.Locale = ...)` override it, `o.Locales` limits and extends the choice. `BxApp` writes `lang` and `dir` from the locale; `BxLocales.Resolve(culture)` does the same for `App.razor`.
- `BluxCulture.ApplyCultureCookieAsync` gives a WebAssembly client the culture the server read from the cookie of `BxLocaleSelect`.
- Components with a `Culture` parameter format with the culture of their locale by default. File sizes, AM/PM, the modifier key of shortcuts and the slash menu of the editor are localized.
- The sort buttons of Table (tooltip) and the summary of Form (accessible name) use their messages.

### Changed

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

