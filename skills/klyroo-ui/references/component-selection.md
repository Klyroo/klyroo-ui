# Choosing a component

Start with `search-components` (it understands alternate names: dialog finds Modal, snackbar finds Toast). When two components look alike, decide with the questions below, then read the metadata of the winner.

## Overlays

| Need | Use | Why |
|---|---|---|
| A decision or a form that blocks the page | `KlyModal` | Native modal `<dialog>`: the page becomes inert and focus stays inside. Set `Dismissible="false"` for a confirmation. |
| A form or details beside the page, full height | `KlySlideover` | Slides in from any edge, for details and forms. |
| A sheet docked to an edge, touch-friendly | `KlyDrawer` | A sheet on the native dialog, bottom, top, left or right. |
| A small panel next to a button, not blocking | `KlyPopover` | Non-modal, anchored to its trigger, which must contain a `<button>`. |
| A one-line hint on hover or focus | `KlyTooltip` | Not for anything interactive or essential. |
| A list of actions or links | `KlyDropdownMenu` | Keyboard navigable. Items are `KlyMenuItem` records. |
| Actions on right click or long press | `KlyContextMenu` | Opens at the pointer, also with Shift+F10. |
| Search commands or pages | `KlyCommandPalette` | Filtered in the browser as you type. |
| A short message after an action | `KlyToast` through `IToastService` | Raised from .NET: `await Toasts.Add(new KlyToast { Title = ... })`. |

## Feedback

| Need | Use |
|---|---|
| A message that stays in the page | `KlyAlert` (collapsible `Details`, `ActionItems`, `StatusIcon`, `Live` for a live region) |
| A site-wide announcement the user can close | `KlyBanner` (dismissal is remembered per `Id`, also in static pages) |
| Nothing to show yet | `KlyEmpty` (buttons from data with `ActionItems`) |
| Content is loading | `KlySkeleton` inside a region marked `aria-busy`, or `KlyProgress` for a measurable task |
| A status, a count, a category | `KlyBadge`; a dot or count on a corner of another element is `KlyChip` |

## Read-only information

| Need | Use |
|---|---|
| A section title with an icon, a count and an action | `KlyHeading` |
| Paragraphs, a quote, a text clamped to a few lines | `KlyText` |
| One highlighted number with its change and a sparkline | `KlyStat` (generic over the number type, `Format` and `Culture`; the delta sign picks its color); several small numbers in a row are `KlyStatGroup` |
| Label and value rows, with badges, links and copy buttons | `KlyFacts` (flat or in groups) |
| Labelled amounts with subtotals, a total, two columns or a toggle | `KlyBreakdown` (decimal amounts, formatted by culture) |
| What changed between two versions of a record | `KlyDiff`, with `KlyDiffItem.Compare` for typed values formatted by culture |
| Entity rows with an image, a reference, badges and a trailing value | `KlyRecordList` |
| Items, numbered steps or a checklist | `KlyList` with `Plain`, `Steps` or `Checks` |
| A date as a calendar tile, a countdown or a range | `KlyDateTile` |
| A measure in a known range (quota, disk), with thresholds or stacked parts | `KlyMeter` as a bar, a ring or a scale; a task that is running is `KlyProgress` |
| Several pictures with a +N tile | `KlyImageGallery`; one picture is `KlyImage`, with a `Caption` it is a figure |
| A conversion funnel | `KlyChart` with `Type="KlyChartType.Funnel"` |
| A history, a pipeline, a log or an agenda by day | `KlyTimeline` with `Layout` `Default`, `Pipeline`, `Log` or `Schedule` |
| A person with contacts, a badge, or a stack of people with tooltips | `KlyUser` with `Contacts` and `Badge`; `KlyAvatarGroup` |

## Choosing a value

| Need | Use |
|---|---|
| One value from a short list, posts in a form | `KlySelect` |
| One or several values from a long list, with filtering | `KlySelectMenu` |
| Free text with suggestions | `KlyInputMenu` |
| An always visible list of options | `KlyListbox` |
| Two to five options, all visible | `KlyRadioGroup` (one) or `KlyCheckboxGroup` (several) |
| A yes/no that takes effect or posts | `KlySwitch` for a setting, `KlyCheckbox` for agreement or a list |
| One or several pressed buttons | `KlyToggleGroup`; a single pressed button is `KlyToggle` |
| Typed tags | `KlyInputTags` |
| A number, a rating, a code | `KlyInputNumber`, `KlyInputRating`, `KlyPinInput` |
| A range or a slider | `KlySlider` |
| A date | `KlyInputDate` (typed segments plus popover) or `KlyCalendar` (always visible) |
| A time | `KlyInputTime` |
| A color | `KlyColorPicker` |
| Files | `KlyFileUpload` |
| Rich text | `KlyEditor` |

## Navigation and structure

| Need | Use |
|---|---|
| Switch between views of the same page | `KlyTabs`; with `Href` items it is a set of links |
| Links of a site, with the current page marked | `KlyNavigationMenu` (horizontal or vertical) |
| The documentation tree | `KlyContentNavigation`; table of contents `KlyContentToc`; previous and next `KlyContentSurround`; search `KlyContentSearch` |
| Show or hide a region | `KlyCollapsible` for one, `KlyAccordion` for a stack |
| Where you are | `KlyBreadcrumb` |
| Page through results | `KlyPagination` (links or buttons) |
| Steps of a process | `KlyStepper`; a history is `KlyTimeline`, a checklist `KlyList` |
| A hierarchy | `KlyTree` |
| Resizable panels | `KlySplitter`, or `KlyDashboardPanel` with `Resizable` in a dashboard |
| A scroll area with keyboard access | `KlyScrollArea` |

## Page building blocks

| Need | Use |
|---|---|
| Landing hero, section, features, call to action | `KlyPageHero`, `KlyPageSection`, `KlyPageFeature`, `KlyPageCta`, in a `KlyPageGrid` or `KlyPageColumns` |
| Cards, link lists, logo strips | `KlyPageCard`, `KlyPageLinks`, `KlyPageList`, `KlyPageLogos` |
| Pricing | `KlyPricingPlans` with `KlyPricingPlan`, or `KlyPricingTable` |
| Blog and changelog | `KlyBlogPosts` with `KlyBlogPost`, `KlyChangelogVersions` with `KlyChangelogVersion` |
| Sign-in, sign-up, verification | `KlyAuthForm` |
| Site chrome | `KlyApp`, `KlyHeader`, `KlyMain`, `KlyFooter`, `KlyFooterColumns`, `KlySidebar` |
| Application shell | `KlyDashboardGroup` with `KlyDashboardSidebar` and `KlyDashboardPanel` |
| Data | `KlyTable` (footer, alignment, Show more), `KlyChart`, `KlyCarousel`; read-only figures and facts are in Read-only information |
| Chat | `KlyChatMessages`, `KlyChatMessage`, `KlyChatPrompt`, or `KlyChatPalette` for a whole panel |

## Rules of thumb

- If a native element does the job (`<button>`, `<a>`, `<details>`), the Klyroo component is built on it: prefer the component for styling and accessibility, not a `div` with handlers.
- Interactive overlays work without being bound. Add `@bind-Open` only when your code must open the overlay or know its state.
- Data-driven components take records (`KlySelectItem`, `KlyMenuItem`, `KlyTabItem`, `KlyNavigationItem`, `KlyToggleItem`, `KlyBreadcrumbItem`): build them once in a static field, not on every render.
