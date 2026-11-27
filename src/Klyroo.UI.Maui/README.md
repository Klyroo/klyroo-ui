# Klyroo.UI.Maui

Native .NET MAUI controls (no WebView, no Blazor) that follow the Klyroo.UI design tokens: the same colors, sizes and radius as the web components, in light and dark, following the theme of the app.

Klyroo.UI itself runs in a `BlazorWebView` on MAUI. This package is for apps that want real native views.

## Controls

Every component of Klyroo.UI has a native control of the same name, with the same parameters where they make sense (a `Disabled` becomes `IsEnabled`, an `EventCallback` an event, a `@bind` a two-way bindable property).
`node build/maui-coverage.mjs` compares the Blazor components of all the Klyroo repositories with the native ones and lists the web-only components that have no native counterpart, with the reason
(`build/maui-coverage.json`).

- **Elements:** `KlyButton`, `KlyBadge`, `KlyCard`, `KlyAlert`, `KlyAvatar`, `KlySeparator`, `KlyIcon` (the Lucide icons built in, `KlyIcons.Register` for your own).
- **Display:** `KlyAvatarGroup`, `KlyBanner`, `KlyChip`, `KlyEmpty`, `KlyFieldGroup`, `KlyImage`, `KlyInfiniteScroll`, `KlyKbd`, `KlyLink`, `KlyProgress`, `KlySkeleton`, `KlyUser`, `KlyHeading`, `KlyText`, `KlyList`, `KlyMeter`, `KlyStat`, `KlyStatGroup`, `KlyFacts`, `KlyBreakdown`, `KlyDiff`, `KlyRecordList`, `KlyDateTile`, `KlyImageGallery`.
- **Form fields:** `KlyInput`, `KlyTextarea`, `KlySelect`, `KlyCheckbox`, `KlySwitch`, `KlyInputNumber`, `KlyInputDate`, `KlyInputTime`, `KlyInputRating`, `KlyPinInput`, `KlyInputTags`, `KlyInputMenu`, `KlySelectMenu`, `KlyListbox`, `KlySlider`, `KlyRangeSlider`, `KlyToggle`, `KlyToggleGroup`, `KlyRadioGroup`, `KlyCheckboxGroup`, `KlyCalendar`, `KlyColorPicker`, `KlyFileUpload`, `KlyForm`, `KlyFormField`.
- **Navigation:** `KlyTabs`, `KlyAccordion`, `KlyCollapsible`, `KlyBreadcrumb`, `KlyPagination`, `KlyStepper`, `KlyTimeline`, `KlyNavigationMenu`, `KlyTree`, `KlyMarquee`, `KlyScrollArea`, `KlySplitter`, `KlyCarousel`, `KlyLocaleSelect`, and the color mode controls (`KlyColorMode`, `KlyColorModeButton`, `KlyColorModeSwitch`, `KlyColorModeSelect`, `KlyColorModeAvatar`, `KlyColorModeImage`).
- **Overlays:** wrap the page content in `KlyOverlayHost`, then `KlyModal`, `KlySlideover`, `KlyDrawer`, `KlyPopover`, `KlyTooltip`, `KlyDropdownMenu`, `KlyContextMenu`, `KlyCommandPalette` and `KlyToaster` with `KlyToastService`.
- **Layout and dashboard shell:** `KlyApp`, `KlyContainer`, `KlyMain`, `KlyHeader`, `KlyFooter`, `KlyFooterColumns`, `KlySidebar`, `KlyError`, `KlyThemeStyle`, `KlyDashboardGroup` and the dashboard sidebar, panel, resize handle, navbar, toolbar and search controls.
- **Pages:** `KlyPage` and its sections (`KlyPageHeader`, `KlyPageHero`, `KlyPageSection`, `KlyPageFeature`, `KlyPageGrid`, ...), `KlyAuthForm`, the pricing, blog and changelog blocks, and the content navigation, search and table of contents.
- **Chat:** `KlyChatMessages`, `KlyChatMessage`, `KlyChatPrompt`, `KlyChatPromptSubmit`, `KlyChatAttachment`, `KlyChatReasoning`, `KlyChatTool`, `KlyChatShimmer`, `KlyChatPalette`.
- **Data:** `KlyChart` (line, area, bar, donut, funnel), `KlyTable`, `KlyColumn`, `KlyTemplateColumn`.
- **Editor:** `KlyEditor` edits Markdown with a toolbar (`KlyEditorToolbar`), a block drag handle and the suggestion, mention and emoji menus. MAUI has no rich text control, so the formatting marks stay visible in the text.

Not applicable on a device (they act on the HTML page): `KlyHead`, `KlyFonts`, `KlyCultureMeta`, `KlySkipLink` and `KlyColorModeScript` (its native counterpart is `KlyColorMode.ApplyStored()` at app start).

## Use

```csharp
using Klyroo.UI.Maui;

var save = new KlyButton { Text = "Save", Variant = ButtonVariant.Solid, Color = KlyColor.Primary };
var email = new KlyInput { Placeholder = "Email", Keyboard = Keyboard.Email, AccessibleName = "Email" };
```

Rebrand at startup, before the first control is created:

```csharp
KlyTheme.Register(KlyColor.Primary, new KlyColorSet(
    Fill: KlyThemeColor.FromHex("#2563eb", "#60a5fa"),
    Text: KlyThemeColor.FromHex("#1d4ed8", "#93c5fd"),
    Contrast: KlyThemeColor.FromHex("#ffffff", "#0b0f14")));
```

The colors are bound to the app theme: `Application.Current.UserAppTheme` switches every control live.

## Build

This project is not part of `Klyroo.UI.slnx`: it needs the MAUI workload. It targets `net10.0` (the unit tests run on it) and `net10.0-android`, plus iOS and Mac Catalyst outside Linux and Windows on Windows. A machine whose MAUI workload is the .NET 11 one builds with `-p:KlyrooMauiTfm=net11.0`.

```bash
dotnet workload install maui-android
dotnet test --project tests/Klyroo.UI.Maui.Tests
dotnet build samples/Klyroo.Maui.Native -t:Run   # an emulator or a device must be running
```

## Status

Checked on an Android emulator: the basic controls (light and dark, typing, toggles, focus ring), and tabs, accordion, progress, stat, number input, slider, charts, a modal and a toast. The other controls are covered by unit tests of their state and styling and compile for Android, but were not looked at on a device. Not run on iOS, Mac Catalyst or Windows.
