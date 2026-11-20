# Choosing a component

Start with `search-components` (it understands alternate names: dialog finds Modal, snackbar finds Toast). When two components look alike, decide with the questions below, then read the metadata of the winner.

## Overlays

| Need | Use | Why |
|---|---|---|
| A decision or a form that blocks the page | `BxModal` | Native modal `<dialog>`: the page becomes inert and focus stays inside. Set `Dismissible="false"` for a confirmation. |
| A form or details beside the page, full height | `BxSlideover` | Slides in from any edge, for details and forms. |
| A sheet docked to an edge, touch-friendly | `BxDrawer` | A sheet on the native dialog, bottom, top, left or right. |
| A small panel next to a button, not blocking | `BxPopover` | Non-modal, anchored to its trigger, which must contain a `<button>`. |
| A one-line hint on hover or focus | `BxTooltip` | Not for anything interactive or essential. |
| A list of actions or links | `BxDropdownMenu` | Keyboard navigable. Items are `BxMenuItem` records. |
| Actions on right click or long press | `BxContextMenu` | Opens at the pointer, also with Shift+F10. |
| Search commands or pages | `BxCommandPalette` | Filtered in the browser as you type. |
| A short message after an action | `BxToast` through `IToastService` | Raised from .NET: `await Toasts.Add(new BxToast { Title = ... })`. |

## Feedback

| Need | Use |
|---|---|
| A message that stays in the page | `BxAlert` |
| A site-wide announcement the user can close | `BxBanner` (dismissal is remembered per `Id`, also in static pages) |
| Nothing to show yet | `BxEmpty` |
| Content is loading | `BxSkeleton` inside a region marked `aria-busy`, or `BxProgress` for a measurable task |
| A status, a count, a category | `BxBadge`; a dot or count on a corner of another element is `BxChip` |

## Choosing a value

| Need | Use |
|---|---|
| One value from a short list, posts in a form | `BxSelect` |
| One or several values from a long list, with filtering | `BxSelectMenu` |
| Free text with suggestions | `BxInputMenu` |
| An always visible list of options | `BxListbox` |
| Two to five options, all visible | `BxRadioGroup` (one) or `BxCheckboxGroup` (several) |
| A yes/no that takes effect or posts | `BxSwitch` for a setting, `BxCheckbox` for agreement or a list |
| One or several pressed buttons | `BxToggleGroup`; a single pressed button is `BxToggle` |
| Typed tags | `BxInputTags` |
| A number, a rating, a code | `BxInputNumber`, `BxInputRating`, `BxPinInput` |
| A range or a slider | `BxSlider` |
| A date | `BxInputDate` (typed segments plus popover) or `BxCalendar` (always visible) |
| A time | `BxInputTime` |
| A color | `BxColorPicker` |
| Files | `BxFileUpload` |
| Rich text | `BxEditor` |

## Navigation and structure

| Need | Use |
|---|---|
| Switch between views of the same page | `BxTabs`; with `Href` items it is a set of links |
| Links of a site, with the current page marked | `BxNavigationMenu` (horizontal or vertical) |
| The documentation tree | `BxContentNavigation`; table of contents `BxContentToc`; previous and next `BxContentSurround`; search `BxContentSearch` |
| Show or hide a region | `BxCollapsible` for one, `BxAccordion` for a stack |
| Where you are | `BxBreadcrumb` |
| Page through results | `BxPagination` (links or buttons) |
| Steps of a process | `BxStepper`; a history is `BxTimeline` |
| A hierarchy | `BxTree` |
| Resizable panels | `BxSplitter`, or `BxDashboardPanel` with `Resizable` in a dashboard |
| A scroll area with keyboard access | `BxScrollArea` |

## Page building blocks

| Need | Use |
|---|---|
| Landing hero, section, features, call to action | `BxPageHero`, `BxPageSection`, `BxPageFeature`, `BxPageCta`, in a `BxPageGrid` or `BxPageColumns` |
| Cards, link lists, logo strips | `BxPageCard`, `BxPageLinks`, `BxPageList`, `BxPageLogos` |
| Pricing | `BxPricingPlans` with `BxPricingPlan`, or `BxPricingTable` |
| Blog and changelog | `BxBlogPosts` with `BxBlogPost`, `BxChangelogVersions` with `BxChangelogVersion` |
| Sign-in, sign-up, verification | `BxAuthForm` |
| Site chrome | `BxApp`, `BxHeader`, `BxMain`, `BxFooter`, `BxFooterColumns`, `BxSidebar` |
| Application shell | `BxDashboardGroup` with `BxDashboardSidebar` and `BxDashboardPanel` |
| Data | `BxTable`, `BxChart`, `BxCarousel` |
| Chat | `BxChatMessages`, `BxChatMessage`, `BxChatPrompt`, or `BxChatPalette` for a whole panel |

## Rules of thumb

- If a native element does the job (`<button>`, `<a>`, `<details>`), the Blux component is built on it: prefer the component for styling and accessibility, not a `div` with handlers.
- Interactive overlays work without being bound. Add `@bind-Open` only when your code must open the overlay or know its state.
- Data-driven components take records (`BxSelectItem`, `BxMenuItem`, `BxTabItem`, `BxNavigationItem`, `BxToggleItem`, `BxBreadcrumbItem`): build them once in a static field, not on every render.
