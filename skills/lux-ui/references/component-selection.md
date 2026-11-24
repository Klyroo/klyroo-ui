# Choosing a component

Start with `search-components` (it understands alternate names: dialog finds Modal, snackbar finds Toast). When two components look alike, decide with the questions below, then read the metadata of the winner.

## Overlays

| Need | Use | Why |
|---|---|---|
| A decision or a form that blocks the page | `LxModal` | Native modal `<dialog>`: the page becomes inert and focus stays inside. Set `Dismissible="false"` for a confirmation. |
| A form or details beside the page, full height | `LxSlideover` | Slides in from any edge, for details and forms. |
| A sheet docked to an edge, touch-friendly | `LxDrawer` | A sheet on the native dialog, bottom, top, left or right. |
| A small panel next to a button, not blocking | `LxPopover` | Non-modal, anchored to its trigger, which must contain a `<button>`. |
| A one-line hint on hover or focus | `LxTooltip` | Not for anything interactive or essential. |
| A list of actions or links | `LxDropdownMenu` | Keyboard navigable. Items are `LxMenuItem` records. |
| Actions on right click or long press | `LxContextMenu` | Opens at the pointer, also with Shift+F10. |
| Search commands or pages | `LxCommandPalette` | Filtered in the browser as you type. |
| A short message after an action | `LxToast` through `IToastService` | Raised from .NET: `await Toasts.Add(new LxToast { Title = ... })`. |

## Feedback

| Need | Use |
|---|---|
| A message that stays in the page | `LxAlert` |
| A site-wide announcement the user can close | `LxBanner` (dismissal is remembered per `Id`, also in static pages) |
| Nothing to show yet | `LxEmpty` |
| Content is loading | `LxSkeleton` inside a region marked `aria-busy`, or `LxProgress` for a measurable task |
| A status, a count, a category | `LxBadge`; a dot or count on a corner of another element is `LxChip` |

## Choosing a value

| Need | Use |
|---|---|
| One value from a short list, posts in a form | `LxSelect` |
| One or several values from a long list, with filtering | `LxSelectMenu` |
| Free text with suggestions | `LxInputMenu` |
| An always visible list of options | `LxListbox` |
| Two to five options, all visible | `LxRadioGroup` (one) or `LxCheckboxGroup` (several) |
| A yes/no that takes effect or posts | `LxSwitch` for a setting, `LxCheckbox` for agreement or a list |
| One or several pressed buttons | `LxToggleGroup`; a single pressed button is `LxToggle` |
| Typed tags | `LxInputTags` |
| A number, a rating, a code | `LxInputNumber`, `LxInputRating`, `LxPinInput` |
| A range or a slider | `LxSlider` |
| A date | `LxInputDate` (typed segments plus popover) or `LxCalendar` (always visible) |
| A time | `LxInputTime` |
| A color | `LxColorPicker` |
| Files | `LxFileUpload` |
| Rich text | `LxEditor` |

## Navigation and structure

| Need | Use |
|---|---|
| Switch between views of the same page | `LxTabs`; with `Href` items it is a set of links |
| Links of a site, with the current page marked | `LxNavigationMenu` (horizontal or vertical) |
| The documentation tree | `LxContentNavigation`; table of contents `LxContentToc`; previous and next `LxContentSurround`; search `LxContentSearch` |
| Show or hide a region | `LxCollapsible` for one, `LxAccordion` for a stack |
| Where you are | `LxBreadcrumb` |
| Page through results | `LxPagination` (links or buttons) |
| Steps of a process | `LxStepper`; a history is `LxTimeline` |
| A hierarchy | `LxTree` |
| Resizable panels | `LxSplitter`, or `LxDashboardPanel` with `Resizable` in a dashboard |
| A scroll area with keyboard access | `LxScrollArea` |

## Page building blocks

| Need | Use |
|---|---|
| Landing hero, section, features, call to action | `LxPageHero`, `LxPageSection`, `LxPageFeature`, `LxPageCta`, in a `LxPageGrid` or `LxPageColumns` |
| Cards, link lists, logo strips | `LxPageCard`, `LxPageLinks`, `LxPageList`, `LxPageLogos` |
| Pricing | `LxPricingPlans` with `LxPricingPlan`, or `LxPricingTable` |
| Blog and changelog | `LxBlogPosts` with `LxBlogPost`, `LxChangelogVersions` with `LxChangelogVersion` |
| Sign-in, sign-up, verification | `LxAuthForm` |
| Site chrome | `LxApp`, `LxHeader`, `LxMain`, `LxFooter`, `LxFooterColumns`, `LxSidebar` |
| Application shell | `LxDashboardGroup` with `LxDashboardSidebar` and `LxDashboardPanel` |
| Data | `LxTable`, `LxChart`, `LxCarousel` |
| Chat | `LxChatMessages`, `LxChatMessage`, `LxChatPrompt`, or `LxChatPalette` for a whole panel |

## Rules of thumb

- If a native element does the job (`<button>`, `<a>`, `<details>`), the Lux component is built on it: prefer the component for styling and accessibility, not a `div` with handlers.
- Interactive overlays work without being bound. Add `@bind-Open` only when your code must open the overlay or know its state.
- Data-driven components take records (`LxSelectItem`, `LxMenuItem`, `LxTabItem`, `LxNavigationItem`, `LxToggleItem`, `LxBreadcrumbItem`): build them once in a static field, not on every render.
