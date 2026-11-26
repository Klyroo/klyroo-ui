# Klyroo.UI API reference API reference

> Generated from the compiled assemblies and their XML documentation. Do not edit by hand.

## Klyroo.UI

Components (158) · Static classes and extensions (11) · Classes (23) · Records (175) · Structs (3) · Interfaces (8) · Enums (39) · Delegates (2)

### Components

#### `KlyAccordion`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Accordion of collapsible items. One or several items can be open at once, and the state lives in the browser, so it works in every render mode.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Collapsible` | `bool` | With a single open item, it can be closed too. |
| `HeadingLevel` | `int` | Heading level of the item headers, to fit the page outline. |
| `Items` | `IReadOnlyList<KlyAccordionItem>` | The items of the accordion (`KlyAccordionItem`): a label and a content each, with an optional icon and value. |
| `Multiple` | `bool` | Several items can be open at once. |
| `Ui` | `AccordionUi?` | Per-instance class overrides of the slots of the accordion, see `AccordionUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `IReadOnlyList<string>?` | Values of the open items. |
| `ValueChanged` | `EventCallback<IReadOnlyList<string>>` | Raised with the values of the open items when the user opens or closes one. Without a handler the state stays in the browser and .NET is never called. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyAlert`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Inline message with an optional icon, title, description, actions and close button, for feedback that stays in the page flow.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Buttons or links under the description of the alert. |
| `ChildContent` | `RenderFragment?` | Rich description, instead of `KlyAlert.Description`. |
| `Close` | `bool` | Shows a close button that removes the alert. |
| `Color` | `KlyColor` | Semantic color of the alert: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Description` | `string?` | Text of the alert. Use `ChildContent` for markup. |
| `Icon` | `RenderFragment?` | Icon of the alert, as markup such as a `KlyIcon`. |
| `OnClose` | `EventCallback` | Raised when the user closes the alert with its close button. |
| `Title` | `string?` | Title of the alert, shown above its description. |
| `Ui` | `AlertUi?` | Per-instance class overrides of the slots of the alert, see `AlertUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `KlyVariant` | Look of the alert: `Solid`, `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyApp`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Root wrapper of an app, placed once in the layout around the body. It renders the toaster, resolves the locale, writes the `lang` and `dir` attributes and cascades global class overrides to every component below it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ApplyStoredCulture` | `bool` | In a Blazor Hybrid web view, applies the culture saved by `KlyLocaleSelect` (`IKlyCultureStore`) before the content renders, unless `KlyApp.Locale` is set. Turn it off when the app applies its own culture in `MauiProgram`. It does nothing in a browser. |
| `ChildContent` | `RenderFragment?` | The app: usually the body of the layout. |
| `Dir` | `string?` | Text direction (the `dir` attribute): "ltr", "rtl" or "auto". Defaults to the direction of the locale. |
| `Lang` | `string?` | Language of the content (the `lang` attribute), for example "fr". Defaults to the code of the locale. |
| `Locale` | `KlyLocale?` | The locale of every component below: texts, writing direction and the culture used to format dates and numbers. Defaults to the locale of an enclosing `KlyApp`, then `KlyrooOptions.Locale`, then the one matching the current UI culture among `KlyrooOptions.Locales`. |
| `Overlays` | `bool` | Renders the `KlyOverlayHost` after the content, which shows the dialogs opened with `IKlyOverlayService`. Turn it off when the app places its own. |
| `Overrides` | `IReadOnlyDictionary<Type, ISlotOverrides>?` | Class overrides for every component below, by component type. See `KlyTheme`. |
| `SpatialNavigation` | `bool` | Turns on the remote control mode for TV screens (experimental, opt in): the arrow keys move focus to the nearest focusable element in that direction, the focus ring is thick and the sizes are larger (`data-kly-tv` on the `html` element). See docs/platforms.md in the repository. |
| `ToastDuration` | `TimeSpan` | How long a toast stays on screen. |
| `Toaster` | `bool` | Renders the `KlyToaster` after the content. Turn it off when the app places its own. |
| `Ui` | `AppUi?` | Per-instance class overrides of the slots of the app, see `AppUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyAuthForm`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Sign-in, sign-up or password reset form built from a list of fields, with optional provider buttons, validation and a submit button.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the auth form: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Description` | `string?` | Text under the title of the form. |
| `Disabled` | `bool` | Disables every field and the submit button. |
| `Enhance` | `bool` | Enhanced form handling in static server rendering: the post updates the page without a reload. |
| `Error` | `string?` | A message about the whole form (wrong password): put it here after `KlyAuthForm.OnValidSubmit` failed. |
| `FieldErrors` | `IReadOnlyDictionary<string, string>?` | Messages by field name, for what only the server can tell (the email is taken). Shown after the built-in ones. |
| `Fields` | `IReadOnlyList<KlyAuthField>` | The fields, in order. |
| `Footer` | `RenderFragment?` | Content under the submit button, such as a link to another form. |
| `FormName` | `string?` | Name of the form. Required to post in static server rendering, where it identifies the posted form. Several auth forms can share a page when their names differ. |
| `Header` | `RenderFragment?` | Replaces the title block. |
| `HeadingLevel` | `int` | Level of the title heading: 2 by default. |
| `Icon` | `RenderFragment?` | Icon above the title. |
| `Loading` | `bool` | Shows the submit button as loading and disables the fields, as it does itself while `KlyAuthForm.OnValidSubmit` runs. |
| `OnInvalidSubmit` | `EventCallback<IReadOnlyDictionary<string, string?>>` | Raised when the form is submitted and validation fails, with the current values of the fields by name. |
| `OnValidSubmit` | `EventCallback<IReadOnlyDictionary<string, string?>>` | Called with the values by field name once they are valid. The form is busy until the task completes. |
| `Providers` | `IReadOnlyList<KlyAuthProvider>` | Buttons that leave for another address (OAuth), above the fields. |
| `Separator` | `string?` | Text between the providers and the fields: `KlyMessages.AuthFormSeparator` by default. An empty string hides the line. |
| `Submit` | `RenderFragment?` | Replaces the submit button. |
| `SubmitLabel` | `string?` | Label of the submit button. |
| `Title` | `string?` | Title of the form, such as "Sign in". |
| `Ui` | `AuthFormUi?` | Per-instance class overrides of the slots of the auth form, see `AuthFormUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Validate` | `Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>?>?` | Extra validation after the required, email and code checks, with the values by field name. Returns messages by field name. |
| `Validation` | `RenderFragment?` | Content between the fields and the submit button (a "forgot password" link). |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyAvatar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Round picture of a person or entity, with an icon or initials fallback when the image is missing or fails to load.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Alt` | `string?` | Text alternative; its initials are shown when there is no image or it fails to load. |
| `Icon` | `RenderFragment?` | Icon shown when there is no picture and no text, as markup such as a `KlyIcon`. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to `KlyImage`: `new KlyImageOptions { Format = "webp", Quality = 80 }`. The avatar passes its size in pixels as the width and the height, so a provider can serve a picture of that size. |
| `Size` | `KlySize` | Size of the avatar, from `Xs` to `Xl`; it also sets the pixel size asked of the image provider. |
| `Src` | `string?` | Address of the picture. When it is missing or fails to load, the avatar shows its text or icon. |
| `Text` | `string?` | Fallback text instead of the initials of `KlyAvatar.Alt`. |
| `Ui` | `AvatarUi?` | Per-instance class overrides of the slots of the avatar, see `AvatarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyAvatarGroup`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Row of overlapping avatars with an optional "+N" counter for the ones that do not fit.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the group ("Project members"). |
| `Items` | `IReadOnlyList<KlyAvatarItem>` | The avatars to show, as `KlyAvatarItem`. |
| `Max` | `int?` | Most avatars to show; the others collapse into a "+N" avatar. Null shows them all. |
| `Size` | `KlySize` | Size of the avatar group, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `AvatarGroupUi?` | Per-instance class overrides of the slots of the avatar group, see `AvatarGroupUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyBadge`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Small label for a status, a count or a category, in the colors and variants of the theme.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Content of the badge. |
| `Color` | `KlyColor` | Semantic color of the badge: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Label` | `string?` | Text of the badge. Use `ChildContent` for markup. |
| `LeadingIcon` | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Size` | `KlySize` | Size of the badge, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `TrailingIcon` | `RenderFragment?` | Icon shown after the label, as markup such as a `KlyIcon`. |
| `Ui` | `BadgeUi?` | Per-instance class overrides of the slots of the badge, see `BadgeUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `KlyVariant` | Look of the badge: `Solid`, `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyBanner`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Dismissible announcement bar at the top of a page. Dismissal is remembered in the browser by `Id`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Buttons or links shown at the end of the banner. |
| `ChildContent` | `RenderFragment?` | Rich description, instead of `KlyBanner.Description`. |
| `Close` | `bool` | Shows a close button. It needs the Klyroo browser module, which every render mode loads. |
| `Color` | `KlyColor` | Semantic color of the banner: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Description` | `string?` | Text of the banner. |
| `Href` | `string?` | Turns the title into a link that covers the whole banner. |
| `Icon` | `RenderFragment?` | Icon of the banner, as markup such as a `KlyIcon`. |
| `Id` | `string?` | Identifies the banner in the browser's localStorage: a dismissed banner stays hidden on later visits. Without an Id a dismissal only lasts until the page is reloaded. |
| `Target` | `string?` | The `target` attribute of the banner link, such as `_blank`. It applies when `Href` is set. |
| `Title` | `string?` | Title of the banner. |
| `Ui` | `BannerUi?` | Per-instance class overrides of the slots of the banner, see `BannerUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyBlogPost`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Card for one blog post: image, date, title, description, badge and authors, with a single link covering the card.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Authors` | `IReadOnlyList<KlyPostAuthor>` | One author shows a `KlyUser`, several show a `KlyAvatarGroup` named after them. |
| `Badge` | `string?` | Text of a badge over the image, such as a category. |
| `BadgeColor` | `KlyColor` | Semantic color of the badge. |
| `ChildContent` | `RenderFragment?` | Extra content between the description and the authors. |
| `Color` | `KlyColor` | Semantic color of the blog post: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Culture` | `CultureInfo?` | The culture of the date. By default the format culture of the locale in effect (the regional current culture when it has the same language), so the prerender and a WebAssembly render agree as long as they pick the same locale. |
| `Date` | `DateTimeOffset?` | Publication date, shown in the format culture of the locale in effect (the regional current culture when it has the same language), or in `KlyBlogPost.Culture` (see `KlyBlogPost.DateFormat`) in a `time` element. |
| `DateFormat` | `string?` | A .NET date format. Without one, the long date of the culture without the day name. |
| `Description` | `string?` | Secondary text of the blog post, shown under or next to its title. |
| `Footer` | `RenderFragment?` | Content at the bottom of the card, under the description. |
| `Header` | `RenderFragment?` | Replaces the cover image. |
| `HeadingLevel` | `int` | Level of the title heading: 3 by default, for a list of posts under a level 2 heading. |
| `Href` | `string?` | Makes the whole card a link to this address, through the title. |
| `Image` | `string?` | Cover image. Decorative unless `KlyBlogPost.ImageAlt` says what it shows. |
| `ImageAlt` | `string?` | Alternative text of the image. Leave it empty when the image is decorative. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to `KlyImage` (format, quality, sizes, provider, preset…). |
| `Orientation` | `KlyOrientation` | Whether the image sits above the text (`Vertical`) or beside it (`Horizontal`). |
| `Target` | `string?` | The `target` attribute of the link of the card, such as `_blank`. It applies when `Href` is set. |
| `Title` | `string?` | Title of the blog post. |
| `Ui` | `BlogPostUi?` | Per-instance class overrides of the slots of the blog post, see `BlogPostUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `BlogPostVariant` | Look of the card: `Outline`, `Soft`, `Subtle`, `Ghost` or `Naked`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyBlogPosts`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Responsive grid of `KlyBlogPost` cards.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The posts, usually `KlyBlogPost` items. |
| `Orientation` | `KlyOrientation` | Horizontal places the posts next to each other in a responsive grid, vertical stacks them in one column. |
| `Ui` | `BlogPostsUi?` | Per-instance class overrides of the slots of the blog posts, see `BlogPostsUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyBreadcrumb`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Navigation trail of the current page and its ancestors. It is plain markup and works in every render mode.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the landmark. Defaults to `KlyMessages.Breadcrumb`. |
| `Items` | `IReadOnlyList<KlyBreadcrumbItem>` | The pages of the trail, from the root to the current page, as `KlyBreadcrumbItem`. The last one is the current page. |
| `Separator` | `RenderFragment?` | Replaces the chevron between items. Decorative: it is hidden from assistive technology. |
| `Ui` | `BreadcrumbUi?` | Per-instance class overrides of the slots of the breadcrumb, see `BreadcrumbUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyButton`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button, rendered as a `<button>`, or as a link when `KlyButton.Href` is set. Works in static server rendering: a submit button or a link needs no interactivity.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Block` | `bool` | Full width. |
| `ChildContent` | `RenderFragment?` | Content of the button, instead of `KlyButton.Label`. |
| `Color` | `KlyColor` | Semantic color of the button: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else primary. |
| `Disabled` | `bool` | Disables the button. |
| `Href` | `string?` | Address to link to. When set the button renders as a link. |
| `Label` | `string?` | Text of the button. Use `ChildContent` for markup. |
| `LeadingIcon` | `RenderFragment?` | Icon before the label, as markup such as a `KlyIcon`. Replaced by a spinner while `KlyButton.Loading`. |
| `Loading` | `bool` | Shows a spinner in place of the leading icon and disables the button. |
| `OnClick` | `EventCallback<MouseEventArgs>` | Raised when the button is clicked. The button stays disabled until the page is interactive when this is set. |
| `Size` | `KlySize` | Size of the button, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else medium. |
| `Square` | `bool` | Equal padding on every side, for icon-only buttons (give them an `aria-label`). |
| `Target` | `string?` | The `target` attribute of the link, such as `_blank`. It applies when `KlyButton.Href` is set. |
| `TrailingIcon` | `RenderFragment?` | Icon after the label, as markup such as a `KlyIcon`. |
| `Type` | `string` | The `type` attribute of the button: button, submit or reset. |
| `Ui` | `ButtonUi?` | Per-instance class overrides of the slots of the button, see `ButtonUi`. |
| `Variant` | `ButtonVariant` | Look of the button: `Solid`, `Outline`, `Soft`, `Subtle`, `Ghost` or `Link`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyCalendar`

`Klyroo.UI` · component · inherits `KlyFieldBase<DateOnly?>`

Month calendar to pick a single date, several dates or a range, with keyboard navigation and localized month and day names.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the calendar. |
| `Culture` | `CultureInfo?` | Source of month and day names and of the first day of the week. Defaults to the format culture of the locale in effect (the regional current culture when it has the same language). |
| `DisabledDates` | `IEnumerable<DateOnly>?` | Dates that cannot be selected, besides those outside `KlyCalendar.Min` and `KlyCalendar.Max`. |
| `Max` | `DateOnly?` | Latest selectable date. |
| `Min` | `DateOnly?` | Earliest selectable date. |
| `Mode` | `KlyCalendarMode` | What the calendar selects. `Single` binds `Value`, `Multiple` binds `KlyCalendar.Values`, `Range` binds `KlyCalendar.Range`. |
| `Range` | `KlyDateRange?` | The selected range in `Range` mode. |
| `RangeChanged` | `EventCallback<KlyDateRange>` | Raised with the selected range when the mode is `Range`. |
| `RangeExpression` | `Expression<Func<KlyDateRange>>?` | Lets an `EditForm` name the posted entries and validate `KlyCalendar.Range`. Set by `@bind-Range`. The range posts as `name.Start` and `name.End`. |
| `Ui` | `CalendarUi?` | Per-instance class overrides of the slots of the calendar, see `CalendarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Values` | `IReadOnlyList<DateOnly>?` | The selected dates in `Multiple` mode. |
| `ValuesChanged` | `EventCallback<IReadOnlyList<DateOnly>>` | Raised with the selected dates when the mode is `Multiple`. |
| `ValuesExpression` | `Expression<Func<IReadOnlyList<DateOnly>>>?` | Lets an `EditForm` name the posted entries and validate `KlyCalendar.Values`. Set by `@bind-Values`. Each date posts as its own entry. |
| `WeekStart` | `DayOfWeek?` | First column of the grid. Defaults to the culture's first day of the week. |

Also accepts the parameters of `KlyFieldBase<DateOnly?>`.

#### `KlyCard`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Container with optional header and footer areas, in outline, soft or subtle variants.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The body. |
| `Footer` | `RenderFragment?` | Content of the footer of the card. |
| `Header` | `RenderFragment?` | Content of the header of the card. |
| `Ui` | `CardUi?` | Per-instance class overrides of the slots of the card, see `CardUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `CardVariant` | Look of the card: `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyCarousel`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Scroll-snap carousel with previous and next buttons, slide dots and optional autoplay, following the WAI-ARIA carousel pattern.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the carousel. |
| `Arrows` | `bool` | Show the previous and next buttons. |
| `AutoPlay` | `int` | Milliseconds between automatic slide changes. Zero (the default) turns autoplay off. Autoplay stops while the pointer or focus is inside and has a pause button; it does not start for visitors who prefer reduced motion. |
| `Color` | `KlyColor` | Semantic color of the carousel: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Dots` | `bool` | Show a button for each slide. |
| `Index` | `int` | Zero-based index of the current slide. Use `@bind-Index` to follow swipes and button presses. |
| `IndexChanged` | `EventCallback<int>` | Raised with the index of the visible slide when it changes. |
| `Items` | `IReadOnlyList<KlyCarouselItem>` | The slides, as `KlyCarouselItem`. |
| `Loop` | `bool` | Wrap from the last slide to the first and back with the buttons and autoplay. |
| `Ui` | `CarouselUi?` | Per-instance class overrides of the slots of the carousel, see `CarouselUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChangelogVersion`

`Klyroo.UI` · component · inherits `KlyComponentBase`

One entry of a changelog: version, date, description, authors and image, on a timeline.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Buttons or links shown under the content of the version. |
| `Authors` | `IReadOnlyList<KlyPostAuthor>` | The people behind the version, as `KlyPostAuthor` items. |
| `Badge` | `string?` | Text of a badge next to the title, such as "Latest". |
| `BadgeColor` | `KlyColor` | Semantic color of the badge. |
| `ChildContent` | `RenderFragment?` | Rich description: paragraphs, lists, links. |
| `Color` | `KlyColor` | Semantic color of the changelog version: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Culture` | `CultureInfo?` | The culture of the date. By default the format culture of the locale in effect (the regional current culture when it has the same language), so the prerender and a WebAssembly render agree as long as they pick the same locale. |
| `Date` | `DateTimeOffset?` | Release date, shown in the format culture of the locale in effect (the regional current culture when it has the same language), or in `KlyChangelogVersion.Culture` in a `time` element. |
| `DateFormat` | `string?` | A .NET date format. Without one, the long date of the culture without the day name. |
| `Description` | `string?` | Text of the version, shown under its title. |
| `Footer` | `RenderFragment?` | Content shown at the bottom of the version. |
| `HeadingLevel` | `int` | Level of the title heading: 3 by default, for versions under a level 2 heading. |
| `Href` | `string?` | Links the title. Unlike a blog post the card is not stretched, so the rich content keeps its own links. |
| `Image` | `string?` | Address of an image shown under the description. |
| `ImageAlt` | `string?` | Alternative text of the image. Leave it empty when the image is decorative. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to `KlyImage` (format, quality, sizes, provider, preset…). |
| `Indicator` | `bool` | Shows the dot and the line that join the versions of a `KlyChangelogVersions`. |
| `Target` | `string?` | The `target` attribute of the link of the version, such as `_blank`. It applies when `Href` is set. |
| `Title` | `string?` | Version number or release name. |
| `Ui` | `ChangelogVersionUi?` | Per-instance class overrides of the slots of the changelog version, see `ChangelogVersionUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChangelogVersions`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Timeline of `KlyChangelogVersion` entries.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The `KlyChangelogVersion` entries, newest first. |
| `Ui` | `ChangelogVersionsUi?` | Per-instance class overrides of the slots of the timeline, see `ChangelogVersionsUi`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChart<TItem, TIndex>`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Line, area, bar or donut chart drawn as SVG on the server, with a generated text description and a data table for accessibility. No charting library or JavaScript is involved.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaDescription` | `string?` | A longer description for assistive technology, in addition to the name. |
| `AriaLabel` | `string?` | The accessible name of the chart. Without it the chart is described by a generated summary of its values. |
| `Categories` | `IReadOnlyList<KlyChartCategory<TItem>>` | The series, in drawing order. With a donut the first one gives the size of each slice. |
| `Center` | `RenderFragment?` | Content over the hole of a donut: a total, a caption. |
| `Colors` | `IReadOnlyList<KlyColor>?` | The colors of the series in order, in place of the default palette. A color set on a category wins. |
| `Culture` | `CultureInfo?` | The culture of every number, date and percentage the chart writes (axis ticks, tooltip, table, summary). Defaults to the culture of the locale in effect (the same on the server and in WebAssembly for one culture). |
| `Curve` | `KlyChartCurve` | The shape of the line between values. Only applies to line and area. |
| `Data` | `IReadOnlyList<TItem>` | The rows to plot: one per x value, or per slice with `KlyChartType.Donut`. |
| `Format` | `Func<double, string>?` | Formats the values on the value axis, in the tooltip and in the table. Defaults to the format culture of the locale in effect (the regional current culture when it has the same language). |
| `Grid` | `bool` | Draws the grid lines at the ticks of the value axis. |
| `Height` | `int` | The height of the drawing in pixels, at `KlyChart.Width`. The chart scales down with its container. |
| `Horizontal` | `bool` | Draws bars along the x axis, with the index on the y axis. Only applies to `KlyChartType.Bar`. |
| `Index` | `Func<TItem, TIndex>` | The value of a row on the x axis (the label of a slice with a donut). Dates and numbers are formatted with the format culture of the locale in effect (the regional current culture when it has the same language). |
| `IndexFormat` | `string?` | The format string of an index that is formattable (a date, a number). |
| `Legend` | `bool?` | Shows the legend. By default it shows when there is more than one series, or more than one slice with a donut. |
| `OnSelect` | `EventCallback<KlyChartSelectEventArgs<TItem, TIndex>>` | Raised when a bar, point or donut slice is clicked, or chosen with Enter or Space on the row the arrow keys are on. The arguments carry the row, its index, the series that was picked, its value and the row item. Only a chart with a handler reacts: it gets a pointer cursor on its bars, points and slices, and its keyboard hint says Enter selects. A bar is the one under the pointer, a point on a line or area the series nearest the pointer; from the keyboard, with several series, no series is named (`SeriesIndex` -1, `SeriesKey` and `Value` null) and the handler chooses from the row item. It needs the tooltip (the keyboard row is the highlighted one) and an interactive render, since the pick is reported by the browser. |
| `Points` | `bool` | Always shows a marker at each value of a line chart. Without it the marker shows on the value under the pointer or the keyboard. |
| `Stacked` | `bool` | Stacks the series instead of drawing them side by side or over one another. Only applies to area and bar. |
| `Table` | `KlyChartTable` | Where the table with the chart's data goes. It is always rendered; by default it is visually hidden. |
| `Thickness` | `int` | The width of the ring in pixels. Only applies to `KlyChartType.Donut`. |
| `Tooltip` | `bool` | Shows the value under the pointer, and under the focus when the chart is focused: the arrow keys move between points. It runs in the browser, so it works in static rendering too. |
| `Type` | `KlyChartType` | Kind of chart to draw: line, area, bar or donut (`KlyChartType`). |
| `Ui` | `ChartUi?` | Per-instance class overrides of the slots of the chart, see `ChartUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Width` | `int` | The width of the drawing in pixels, and the widest the chart gets. |
| `XAxis` | `bool` | Shows the labels of the index axis (the x axis, or the y axis of horizontal bars). |
| `XLabel` | `string?` | A title along the index axis. |
| `YAxis` | `bool` | Shows the ticks and labels of the value axis. |
| `YLabel` | `string?` | A title along the value axis. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatAttachment`

`Klyroo.UI` · component · inherits `KlyComponentBase`

File shown as a chip or a thumbnail in a chat, with its name, size, upload status and a remove button.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the chat attachment: a built-in `KlyColor` or the name of a color you defined. |
| `Description` | `string?` | Replaces the default description: the extension, then the size. For an error, put the reason here. |
| `FileSize` | `long?` | Size in bytes, shown in the description. |
| `Icon` | `RenderFragment?` | Replaces the file icon. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to the preview of an image, see `KlyImage`. A horizontal card passes the size of its square preview as the width and the height; a thumbnail or a vertical card takes the size of its container, so give `Sizes` to get a `srcset`. |
| `Name` | `string` | File name. |
| `OnRemove` | `EventCallback<MouseEventArgs>` | Raised when the user clicks the remove button. Take the attachment out of your list. |
| `Orientation` | `KlyOrientation` | Card only: a vertical card gives an image a large preview. |
| `Removable` | `bool` | Shows a remove button. Raise `KlyChatAttachment.OnRemove` and take the attachment out of your list. |
| `Size` | `KlySize` | Size of the chat attachment, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Status` | `ChatAttachmentStatus` | Whether the file is idle, uploading or failed (`ChatAttachmentStatus`). |
| `Type` | `string?` | Media type ("image/png"): images with a `KlyChatAttachment.Url` show a preview. |
| `Ui` | `ChatAttachmentUi?` | Per-instance class overrides of the slots of the chat attachment, see `ChatAttachmentUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Url` | `string?` | Address of the file. With an image media type, the attachment shows a preview of it. |
| `Variant` | `ChatAttachmentVariant` | Look of the attachment: a `Card` or a `Thumbnail`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatMessage`

`Klyroo.UI` · component · inherits `KlyComponentBase`

One message of a conversation, aligned by role, with an avatar, actions and a streaming state.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Below the message: copy, retry, thumbs. |
| `AvatarAlt` | `string?` | Name of the author for the avatar. Defaults to the name of the role. |
| `AvatarSrc` | `string?` | Image of the author; the initials of `KlyChatMessage.AvatarAlt` show when there is none. |
| `AvatarText` | `string?` | Initials of the avatar when there is no picture. |
| `ChildContent` | `RenderFragment?` | The message, as markup. Rendering markdown is the app's job; nothing here parses it. |
| `Color` | `KlyColor` | Semantic color of the chat message: a built-in `KlyColor` or the name of a color you defined. |
| `Compact` | `bool` | Tighter spacing, for narrow panels such as `KlyChatPalette`. |
| `Files` | `RenderFragment?` | Attachments of the message, above the text: `KlyChatAttachment` items. |
| `Header` | `RenderFragment?` | Above the message: a name, a time. |
| `Icon` | `RenderFragment?` | An icon in place of the avatar picture. |
| `Leading` | `RenderFragment?` | Replaces the avatar altogether. Without any of the avatar parameters nothing is shown. |
| `Role` | `ChatRole` | Who wrote it: tints nothing, but sets the default side and look, and the accessible name of the message. |
| `Side` | `ChatSide?` | The side of the conversation. A user message sits on the right, the others on the left. |
| `Streaming` | `bool` | Marks the message as still arriving. Pair it with the changing content. |
| `Text` | `string?` | The message as plain text, line breaks kept. Ignored when `KlyChatMessage.ChildContent` is set. |
| `Ui` | `ChatMessageUi?` | Per-instance class overrides of the slots of the chat message, see `ChatMessageUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `ChatMessageVariant?` | The surface of the message. A user message is soft, the others have none. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatMessages`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Scrolling list of chat messages that keeps the latest one in view, announces new messages to assistive technology and offers a "jump to latest" button.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AutoScroll` | `bool` | Follows the latest message while the user is at the bottom, and shows the "jump to latest" button once they scroll up. Without it the list never scrolls by itself. |
| `ChildContent` | `RenderFragment?` | The messages: `KlyChatMessage` items, in order. |
| `Error` | `RenderFragment?` | Replaces the message shown when `KlyChatMessages.Status` is Error. |
| `Indicator` | `RenderFragment?` | Replaces the typing dots. |
| `Label` | `string?` | Accessible name of the log. Defaults to the built-in text. |
| `Status` | `ChatStatus` | Where the conversation is. Submitted and Streaming show the typing indicator, Error shows `KlyChatMessages.Error`. Update it from .NET as the request progresses. |
| `Ui` | `ChatMessagesUi?` | Per-instance class overrides of the slots of the chat messages, see `ChatMessagesUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatPalette`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Complete chat panel: the messages fill the available height and the prompt stays pinned below them.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The conversation: a `KlyChatMessages`. |
| `Prompt` | `RenderFragment?` | The message form, pinned to the bottom: a `KlyChatPrompt`. |
| `Ui` | `ChatPaletteUi?` | Per-instance class overrides of the slots of the chat palette, see `ChatPaletteUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatPrompt`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Chat message form with a growing textarea that submits on Enter, plus slots for actions and a submit button.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Attachments` | `RenderFragment?` | Above the field: `KlyChatAttachment` items. A message may be sent with no text while this is set. |
| `Autofocus` | `bool` | Focuses the field when the page loads. |
| `Color` | `KlyColor` | Semantic color of the chat prompt: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Disabled` | `bool` | Disables the text area and the submit button. |
| `Enhance` | `bool` | Enhanced form handling in static server rendering: the post updates the page without a reload. |
| `Footer` | `RenderFragment?` | Below the field: buttons to attach files, a model picker. |
| `FormName` | `string?` | Name of the form. Required to send in static server rendering, where the page receives the form post and calls `KlyChatPrompt.OnSubmit`; one prompt per page and name. Posting is for static rendering only: under an interactive render mode (prerendering included) the form never posts, and Enter does nothing until the page is interactive. |
| `Label` | `string?` | Accessible name of the form and the field. Defaults to the built-in text. |
| `Leading` | `RenderFragment?` | Before the field, in its row. |
| `MaxRows` | `int` | The field grows with its text up to this many rows, then scrolls. |
| `OnReload` | `EventCallback` | Called when the retry button of the default submit button is pressed. |
| `OnStop` | `EventCallback` | Called when the stop button of the default submit button is pressed. |
| `OnSubmit` | `EventCallback<string>` | Called with the text of the message, when the user sends it. Without a handler, nothing leaves the browser. |
| `Placeholder` | `string?` | Hint shown in the text area while it is empty. Defaults to the built-in text. |
| `Rows` | `int` | Rows of an empty field. |
| `Status` | `ChatStatus` | Where the conversation is. While Submitted or Streaming a message cannot be sent (the submit button becomes a stop button); the field stays editable so the next message can be drafted. |
| `SubmitOnEnter` | `bool` | Enter sends and Shift+Enter adds a line. Set it to false and Ctrl or Cmd plus Enter sends instead. |
| `Trailing` | `RenderFragment?` | Replaces the submit button, after the field. An empty fragment hides it. |
| `Ui` | `ChatPromptUi?` | Per-instance class overrides of the slots of the chat prompt, see `ChatPromptUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `string?` | Text the field starts with: a draft, or the message to show again after a post. |
| `Variant` | `FieldVariant` | Look of the prompt, as a `FieldVariant`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatPromptSubmit`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button of a chat prompt that sends, stops or retries according to the chat status.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor?` | The color of the send button. The stop and retry buttons keep their own. |
| `Disabled` | `bool` | Disables the button. |
| `Label` | `string?` | Accessible name. Defaults to the built-in text for the status. |
| `OnReload` | `EventCallback` | Called when the button is pressed after an error. |
| `OnStop` | `EventCallback` | Called when the button is pressed while the answer is submitted or streaming. |
| `Size` | `KlySize` | Size of the button, from `Xs` to `Xl`. |
| `Status` | `ChatStatus?` | The status to show. Defaults to the one of the enclosing `KlyChatPrompt`. |
| `Ui` | `ChatPromptSubmitUi?` | Per-instance class overrides of the slots of the chat prompt submit, see `ChatPromptSubmitUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatReasoning`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Collapsible block that shows the thinking of a model. It opens while streaming and closes shortly after.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AutoCloseDelay` | `int` | Milliseconds after streaming ends before it closes by itself. 0 keeps it open. |
| `AutoOpen` | `bool` | Opens when streaming starts. The user's own toggle in the meantime wins over the automatic close. |
| `Chevron` | `ChatChevron` | Side of the chevron that shows the state: `Trailing` or `Leading`. |
| `ChildContent` | `RenderFragment?` | The reasoning text. Updates as it streams. |
| `Disabled` | `bool` | Prevents the user from opening or closing the block. |
| `Duration` | `int?` | Seconds the thinking took, for the finished label. Without it the time between the start and the end of streaming is measured here (interactive rendering). |
| `Icon` | `RenderFragment?` | An icon before the label. |
| `Open` | `bool` | Open state. Bind it with `@bind-Open` to hear about changes; unbound, nothing leaves the browser. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the block opens or closes, by the user or by the streaming state. |
| `Streaming` | `bool` | The block is still receiving its content: it shimmers and, with `KlyChatReasoning.AutoOpen`, opens. |
| `Text` | `string?` | Label while it streams. Defaults to the built-in "Thinking...". Afterwards it is the label too, unless a duration is known. |
| `Ui` | `ChatReasoningUi?` | Per-instance class overrides of the slots of the chat reasoning, see `ChatReasoningUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatShimmer`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Text with a sweeping highlight, to show that something is in progress. It is plain and muted when the user prefers reduced motion.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Duration` | `double` | Seconds one sweep takes. |
| `Spread` | `double` | Width of the highlight, relative to the text: 2 by default, bigger is a wider band. |
| `Text` | `string` | The text to shimmer. |
| `Ui` | `ChatShimmerUi?` | Per-instance class overrides of the slots of the chat shimmer, see `ChatShimmerUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyChatTool`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Tool call of an assistant: its name, running or failed state and, optionally, its input and output in a collapsible area.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Below the name, outside the collapsible part: approve and deny buttons. |
| `Chevron` | `ChatChevron` | Side of the chevron that shows the state: `Trailing` or `Leading`. |
| `ChildContent` | `RenderFragment?` | What the tool was given and returned. Setting it makes the block collapsible. |
| `Disabled` | `bool` | Prevents the user from opening or closing the details. |
| `Error` | `bool` | The call failed: error colors, and "failed" is read out after the name. |
| `Icon` | `RenderFragment?` | An icon before the name, replaced by a spinner while loading and by an alert on error. |
| `Loading` | `bool` | The call is running: a spinner replaces the icon. |
| `Open` | `bool` | Open state. Bind it with `@bind-Open` to hear about changes; unbound, nothing leaves the browser. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the details open or close. |
| `Streaming` | `bool` | The name shimmers, as the arguments or the output still stream in. |
| `Suffix` | `string?` | Quieter text after the name: the query, the file. |
| `Text` | `string?` | The name of the tool call: "Searching the web". |
| `Ui` | `ChatToolUi?` | Per-instance class overrides of the slots of the chat tool, see `ChatToolUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `ChatToolVariant` | Look of the tool call: `Inline` or `Card`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyCheckbox`

`Klyroo.UI` · component · inherits `KlyFieldBase<bool>`

Checkbox with a label and a description, bindable to a `bool` and usable in an `EditForm`. It supports the indeterminate state.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Description` | `string?` | Text under the label of the checkbox. |
| `Indeterminate` | `bool` | Shows the mixed state (a minus), for a "select all" box when only some items are selected. `indeterminate` is a DOM property that HTML cannot render, so the component renders `data-indeterminate` and the browser script of Klyroo sets the property (also in static rendering, and again after an enhanced navigation). The user's click clears the mixed state and toggles the box, as in any browser. The parameter is controlled: the parent decides what the box shows next, normally by setting it to `false` in its `ValueChanged` handler. While it stays `true`, every render of the parent puts the mixed state back. Static rendering has no .NET to render again, so a click there clears it until the page is rendered by the server again. |
| `Label` | `string?` | Text next to the checkbox. A `KlyFormField` label also names it. |
| `Ui` | `CheckboxUi?` | Per-instance class overrides of the slots of the checkbox, see `CheckboxUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyFieldBase<bool>`.

#### `KlyCheckboxGroup<TValue>`

`Klyroo.UI` · component · inherits `KlyFieldBase<IReadOnlyList<TValue>>`

Group of checkboxes that binds to the list of the selected values.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Items` | `IReadOnlyList<KlyCheckboxItem<TValue>>` | The checkboxes of the group, as `KlyCheckboxItem`. |
| `Legend` | `string?` | Caption of the group, rendered as the legend of its fieldset. |
| `Orientation` | `KlyOrientation` | Whether the checkboxes are stacked (`Vertical`) or side by side (`Horizontal`). |
| `Ui` | `CheckboxGroupUi?` | Per-instance class overrides of the slots of the checkbox group, see `CheckboxGroupUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyFieldBase<IReadOnlyList<TValue>>`.

#### `KlyChip`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Small badge placed on a corner of its content, as a dot, a count or a status.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The element the indicator sits on, an avatar or a button for example. |
| `Color` | `KlyColor` | Semantic color of the chip: a built-in `KlyColor` or the name of a color you defined. |
| `Inset` | `bool` | Keeps the indicator inside the corner of the element instead of straddling it. |
| `Label` | `string?` | Accessible text of the indicator ("3 unread messages"). Without it a dot is hidden from assistive technology and a text is read as it is: never rely on the color alone to convey a meaning. |
| `Position` | `KlyChipPosition` | Corner of the content where the chip sits (`KlyChipPosition`). Defaults to the top right. |
| `Show` | `bool` | Shows the indicator. Set it to false to hide it without removing the element it sits on. |
| `Size` | `KlySize` | Size of the chip, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Standalone` | `bool` | Renders the indicator on its own, in the flow, without an element to sit on. |
| `Text` | `string?` | Text inside the indicator, a count for example. Without it the indicator is a dot. |
| `Ui` | `ChipUi?` | Per-instance class overrides of the slots of the chip, see `ChipUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyCollapsible`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Disclosure made of a trigger and a region that opens and closes in the browser.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The revealed content. |
| `Disabled` | `bool` | Prevents the user from opening or closing the region. |
| `Label` | `string?` | Text of the trigger button. |
| `Open` | `bool` | Open state. Bind it with `@bind-Open` to hear about changes; unbound, nothing leaves the browser. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the region opens or closes. |
| `ShowIcon` | `bool` | Shows the chevron that turns when the region opens. |
| `Trigger` | `RenderFragment?` | Custom content of the trigger button; replaces `KlyCollapsible.Label`. It must not contain interactive elements. |
| `Ui` | `CollapsibleUi?` | Per-instance class overrides of the slots of the collapsible, see `CollapsibleUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyColorModeAvatar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Avatar with one picture for light mode and one for dark mode. CSS shows the matching one, so there is no flash.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Alt` | `string?` | Alternative text of the picture. Both pictures carry it. |
| `Dark` | `string?` | Image in dark mode. Defaults to `KlyColorModeAvatar.Light`. |
| `Icon` | `RenderFragment?` | Icon shown when there is no picture and no text, as markup such as a `KlyIcon`. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to both avatars, see `KlyAvatar.ImageOptions`. |
| `Light` | `string?` | Image in light mode. |
| `Size` | `KlySize` | Size of the color mode avatar, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Text` | `string?` | Initials shown when there is no picture. |
| `Ui` | `AvatarUi?` | Per-instance class overrides of the slots of the color mode avatar, see `AvatarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyColorModeButton`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button that switches between light and dark mode and stays in sync with the other color mode controls.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of an icon-only button. Defaults to the built-in text. |
| `Color` | `KlyColor` | Semantic color of the button: a built-in `KlyColor` or the name of a color you defined. Defaults to neutral. |
| `Label` | `string?` | Visible text next to the icon. Without it the button is icon-only and needs `KlyColorModeButton.AriaLabel`. |
| `Size` | `KlySize` | Size of the color mode button, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Variant` | `ButtonVariant` | Look of the button: `Solid`, `Outline`, `Soft`, `Subtle`, `Ghost` or `Link`. Defaults to `Ghost`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyColorModeImage`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Image with one source for light mode and one for dark mode. CSS shows the matching one, so there is no flash.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Alt` | `string?` | Text alternative. Leave empty for a decorative image. |
| `Dark` | `string?` | Image in dark mode. Defaults to `KlyColorModeImage.Light`. |
| `Height` | `int?` | Height of both images in CSS pixels, see `KlyImage.Height`. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to both light and dark images. |
| `Light` | `string?` | Image in light mode. |
| `Width` | `int?` | Width of both images in CSS pixels, see `KlyImage.Width`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyColorModeScript`

`Klyroo.UI` · component · inherits `ComponentBase`

Place in `<head>`, before the stylesheets. Applies the stored color mode (or the system one) before the first paint, so a dark page never flashes light. Toggle at runtime with `Klyroo.setColorMode('light' | 'dark' | 'system')`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Nonce` | `string?` | Nonce for a Content-Security-Policy that forbids inline scripts without one. |

#### `KlyColorModeSelect`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Select that offers system, light and dark color modes.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the select. Defaults to the built-in text. |
| `Color` | `KlyColor` | Semantic color of the color mode select: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Size` | `KlySize` | Size of the color mode select, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `ColorModeSelectUi?` | Per-instance class overrides of the slots of the color mode select, see `ColorModeSelectUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyColorModeSwitch`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Switch that is on in dark mode.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name when there is no visible label. Defaults to the built-in text. |
| `Color` | `KlyColor` | Semantic color of the color mode switch: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Label` | `string?` | Visible label. Without it the switch needs `KlyColorModeSwitch.AriaLabel`. |
| `Size` | `KlySize` | Size of the color mode switch, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `ColorModeSwitchUi?` | Per-instance class overrides of the slots of the color mode switch, see `ColorModeSwitchUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyColorPicker`

`Klyroo.UI` · component · inherits `KlyFieldBase<string>`

Color picker with a saturation and brightness area, a hue slider and a hex field. It binds to a hex string.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the picker when it is not inside a `KlyFormField` with a label. |
| `Ui` | `ColorPickerUi?` | Per-instance class overrides of the slots of the color picker, see `ColorPickerUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the hex field, as a `FieldVariant`. |

Also accepts the parameters of `KlyFieldBase<string>`.

#### `KlyColumn<TItem, TValue>`

`Klyroo.UI` · component · inherits `KlyColumnBase<TItem>`

A column of `KlyTable` that shows a value of each item. It is also the sort key when `KlyColumn.Sortable`, compared with the default comparer of `TValue`. Give a `KlyColumn.Template` to show markup in the cells while `KlyColumn.Field` still sorts them; for a column that has no value to sort by use `KlyTemplateColumn`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Field` | `Func<TItem, TValue>` | Value shown in the cell, and the sort key when `KlyColumn.Sortable`. Dates and numbers are shown with the culture of the page (`null` shows nothing). |
| `Sortable` | `bool` | Sorts the table by `KlyColumn.Field` when the header is clicked. |
| `Template` | `RenderFragment<TItem>?` | Cell content built from the item, shown instead of the text of `KlyColumn.Field`, which still orders the column. |

Also accepts the parameters of `KlyColumnBase<TItem>`.

#### `KlyColumnBase<TItem>`

`Klyroo.UI` · component · inherits `ComponentBase`

What every column of a `KlyTable` has: a title, cell classes, and the registration with its table. Columns render nothing themselves. Use `KlyColumn` to show a value of the item or `KlyTemplateColumn` for custom cells.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Class` | `string?` | Classes added to the cells of this column. |
| `Title` | `string` | Text of the column header. |

#### `KlyCommandPalette`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Searchable list of commands in a dialog, filtered in the browser as the user types and operated with the keyboard.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `CloseOnSelect` | `bool` | Makes activating a command close the enclosing `KlyModal`, `KlyDrawer` or `KlySlideover` without a round trip. |
| `Groups` | `IReadOnlyList<KlyCommandGroup>` | The commands, in groups. A group without a label has no heading. |
| `Label` | `string?` | Accessible name of the search field and the list. Defaults to the built-in text. |
| `OnSelect` | `EventCallback<KlyCommandItem>` | Called with the activated command. Link commands navigate as well. |
| `Placeholder` | `string?` | Hint of the search field. Defaults to the built-in text. |
| `Ui` | `CommandPaletteUi?` | Per-instance class overrides of the slots of the command palette, see `CommandPaletteUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContainer`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Centered wrapper with a maximum width and responsive horizontal padding.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `As` | `string` | The element to render, "div" by default: "main", "section", "header"… |
| `ChildContent` | `RenderFragment?` | Content of the container. |
| `Size` | `KlySize` | Maximum width: Xs 42rem, Sm 56rem, Md 80rem (default), Lg the 2xl breakpoint, Xl none. |
| `Ui` | `ContainerUi?` | Per-instance class overrides of the slots of the container, see `ContainerUi`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContentNavigation`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Navigation tree of a documentation site, with collapsible groups and the current page highlighted.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the navigation landmark. Needed when the page has several. |
| `Color` | `KlyColor` | The color of the mark on the current page. |
| `Items` | `IReadOnlyList<KlyContentNavigationItem>` | The entries. A top-level entry with children is a section heading with its pages below it. |
| `Ui` | `ContentNavigationUi?` | Per-instance class overrides of the slots of the content navigation, see `ContentNavigationUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContentSearch`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Documentation search: a command palette in a modal over a list of pages and headings, opened by a keyboard shortcut or a `KlyContentSearchButton`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `IndexUrl` | `string?` | The address of a JSON file with the pages to search, instead of (or in addition to) `KlyContentSearch.Pages`: an array of `{"title", "href", "description"?, "group"?, "keywords"?, "headings"?: [{"text", "id"}]}`, the fields of `KlyContentSearchPage`. The browser fetches it the first time the search opens, shows a loading message, keeps the result for the page and, when the fetch fails, says so in a live region with a button to try again. It is resolved against `<base href>`, so a relative address works below a path base, and it must be on the same origin as the page. Works in every render mode, because the browser does it. |
| `Label` | `string?` | Accessible name of the dialog and the search field. Defaults to the built-in text. |
| `Name` | `string` | Names this search so that a `KlyContentSearchButton` with the same `For` opens it. Needed only when a page has more than one search; the unnamed one is the default. |
| `OnSelect` | `EventCallback<KlyCommandItem>` | Called with the activated command. Pages and headings navigate as well. The search closes in either case. |
| `Open` | `bool` | Open state. Use `@bind-Open`; the browser opens and closes the search on its own. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the search dialog opens or closes. |
| `Pages` | `IReadOnlyList<KlyContentSearchPage>` | The pages to search, written into the page. A page is listed with its headings, under the heading of its `Group`. For a long list use `KlyContentSearch.IndexUrl` instead. |
| `Placeholder` | `string?` | Hint of the search field. Defaults to the built-in text. |
| `Shortcut` | `string` | The keyboard shortcut that opens the search: "mod+k" by default, where mod is Cmd on Apple platforms and Ctrl elsewhere. Ctrl, meta, alt and shift are accepted too. |
| `Ui` | `CommandPaletteUi?` | Slot overrides of the command palette. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContentSearchButton`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button that opens the `KlyContentSearch` of the page and shows its keyboard shortcut.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Collapsed` | `bool` | Shows only the icon, as a square button. Without it the button shows its label and its shortcut. |
| `Color` | `KlyColor` | Semantic color of the content search button: a built-in `KlyColor` or the name of a color you defined. |
| `For` | `string` | The `Name` of the search to open. Without it the button opens the unnamed one. |
| `Keys` | `IReadOnlyList<string>?` | The keys of the shortcut to show, one `KlyKbd` each. Match the `Shortcut` of the search. Left out, the hint follows the platform in the browser (the command key and K on Apple devices, Ctrl and K elsewhere), matching the default `mod+k` shortcut, and the button announces `aria-keyshortcuts`. Keys you give are shown as given. |
| `Label` | `string?` | The text of the button. Defaults to the built-in text; with `KlyContentSearchButton.Collapsed` it is the accessible name. |
| `Size` | `KlySize` | Size of the content search button, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `ContentSearchButtonUi?` | Per-instance class overrides of the slots of the content search button, see `ContentSearchButtonUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `ButtonVariant` | Look of the button: `Solid`, `Outline`, `Soft`, `Subtle`, `Ghost` or `Link`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContentSurround`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Links to the previous and next page of a documentation site.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the navigation landmark. Defaults to the built-in text. |
| `Next` | `KlyContentSurroundLink?` | The page after this one. |
| `Previous` | `KlyContentSurroundLink?` | The page before this one. Without it the next page stays in the second column. |
| `Ui` | `ContentSurroundUi?` | Per-instance class overrides of the slots of the content surround, see `ContentSurroundUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContentToc`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Table of contents of a page that highlights the heading being read.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Breakpoint` | `KlyContentTocBreakpoint` | Screen width from which the table of contents is a sticky list instead of a collapsible block (`KlyContentTocBreakpoint`). |
| `Collapsible` | `bool` | Collapses the list behind its title below `KlyContentToc.Breakpoint`, as a disclosure button. Above it, and without this, the list is always shown. |
| `Color` | `KlyColor` | The color of the link of the heading being read. |
| `Links` | `IReadOnlyList<KlyContentTocLink>` | The headings of the page, in document order. Each `Id` must be the id of an element on the page. |
| `Title` | `string?` | The title, and the accessible name of the navigation. Defaults to the built-in text. |
| `Ui` | `ContentTocUi?` | Per-instance class overrides of the slots of the content toc, see `ContentTocUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyContextMenu`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Menu that opens at the pointer on right click, long press or the keyboard, over the content it wraps.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The area that reacts to the context menu gesture. |
| `Disabled` | `bool` | Lets the browser show its own context menu in the area. |
| `Focusable` | `bool` | Makes the area reachable with Tab, so that Shift+F10 and the ContextMenu key work from the keyboard. |
| `Items` | `IReadOnlyList<KlyMenuItem>` | The entries of the menu, as `KlyMenuItem`. |
| `Label` | `string?` | Accessible name of the menu. |
| `OnSelect` | `EventCallback<KlyMenuItem>` | Called with the activated item. Link items navigate instead. |
| `Open` | `bool` | Open state, reported with `@bind-Open`. Setting it to true opens the menu at the center of the area, since there is no pointer to open it at. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the menu opens or closes. |
| `Ui` | `ContextMenuUi?` | Per-instance class overrides of the slots of the context menu, see `ContextMenuUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyCultureMeta`

`Klyroo.UI` · component · inherits `ComponentBase`

Place in `<head>` of `App.razor`. It writes the culture and the UI culture of the request (`<meta name="klyroo-culture" content="c=en-GB|uic=en">`) so that a WebAssembly client, which has no request, formats dates and numbers exactly like the prerender did: see `KlyrooCulture.ApplyServerCultureAsync`. In a Blazor Hybrid web view there is no server request: it renders nothing (and `KlyrooCulture.ApplyServerCultureAsync` keeps the culture of the device).

#### `KlyDashboardGroup`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Root of a dashboard layout: a sidebar and resizable panels, with the panel sizes remembered in the browser.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The sidebar, the panels and anything else of the dashboard, laid out in a row. |
| `Sizes` | `IReadOnlyList<double>?` | Sizes of the panels in percent, one per panel in document order. Bind it to follow resizing from .NET; unbound, resizing stays in the browser. Bound sizes are not restored from storage: your state wins. |
| `SizesChanged` | `EventCallback<IReadOnlyList<double>>` | Raised with the size of each panel, as weights in percent, when the user resizes them. |
| `StorageKey` | `string?` | Key under which the dashboard remembers its state (sidebar in cookies, panel sizes in localStorage): the collapsed state and the width of its sidebar (cookies "klyroo-sidebar-" followed by the key, see KlySidebar.Id and KlyPathScope.SidebarCookieName for an app below a path base) and the sizes of its panels ("klyroo:dashboard:" key ":panels"). Without a key nothing is remembered. Use a different key for each dashboard of the app. |
| `Ui` | `DashboardGroupUi?` | Per-instance class overrides of the slots of the dashboard group, see `DashboardGroupUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardNavbar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Top bar of a dashboard panel with the sidebar toggle, a title and left, center and right areas.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `As` | `string` | The element to render: "header" (default) or "div", in any case; anything else throws an `ArgumentException`. A header outside a sectioning element is the banner landmark and a page has one: use "div" for the second navbar of a page, such as the one of a detail panel next to a list. |
| `ChildContent` | `RenderFragment?` | The center area, between the left and the right ones. |
| `HeadingLevel` | `int` | Level of the title heading, 1 to 6. A page has one h1: give the navbars after the first one a lower level, or no `KlyDashboardNavbar.Title`. |
| `Leading` | `RenderFragment?` | Before the title. |
| `Left` | `RenderFragment?` | Left area, after the title. |
| `Right` | `RenderFragment?` | Right area: actions, the user menu. |
| `Title` | `string?` | The title, a heading of `KlyDashboardNavbar.HeadingLevel` (1 by default). |
| `Toggle` | `bool` | Shows the `KlyDashboardSidebarToggle` at the start, below the lg breakpoint. |
| `Trailing` | `RenderFragment?` | After the title. |
| `Ui` | `DashboardNavbarUi?` | Per-instance class overrides of the slots of the dashboard navbar, see `DashboardNavbarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardPanel`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Column of a dashboard with a header, a scrollable body and a footer, optionally resizable.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Body` | `RenderFragment?` | The scrollable middle. |
| `ChildContent` | `RenderFragment?` | Replaces the header, the body and the footer. |
| `Footer` | `RenderFragment?` | Content of the footer of the panel, below its scrollable body. |
| `Header` | `RenderFragment?` | The top of the panel: usually a `KlyDashboardNavbar` and a `KlyDashboardToolbar`. |
| `MaxSize` | `double` | Largest share of the group the panel can take, in percent. Defaults to 100. |
| `MinSize` | `double` | Smallest share of the group the panel can take, in percent. |
| `Resizable` | `bool` | Adds a `KlyDashboardResizeHandle` on the trailing edge, to resize the panel against the next one. |
| `ResizeLabel` | `string?` | Accessible name of the resize handle. Defaults to the built-in text. |
| `Size` | `double?` | Size in percent when nothing is remembered. Panels without one share what the others leave. |
| `Ui` | `DashboardPanelUi?` | Per-instance class overrides of the slots of the dashboard panel, see `DashboardPanelUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardResizeHandle`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Draggable separator between two dashboard panels, operable with the keyboard.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Controls` | `string?` | The id of the panel before the handle. |
| `Label` | `string?` | Accessible name. Defaults to the built-in text. |
| `Step` | `double` | Size change of an arrow key press, in percent. |
| `Ui` | `DashboardResizeHandleUi?` | Per-instance class overrides of the slots of the dashboard resize handle, see `DashboardResizeHandleUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `double` | The size in percent of the panel before the handle, as shown until the browser takes over. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardSearch`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Command palette in a modal for a dashboard, opened by a keyboard shortcut.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Groups` | `IReadOnlyList<KlyCommandGroup>` | The commands, in groups: links navigate, other commands go to `KlyDashboardSearch.OnSelect`. |
| `Label` | `string?` | Accessible name of the dialog and the search field. Defaults to the built-in text. |
| `OnSelect` | `EventCallback<KlyCommandItem>` | Called with the activated command. Link commands navigate as well. The search closes in either case. |
| `Open` | `bool` | Open state. Use `@bind-Open`; the browser opens and closes the search on its own. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the search dialog opens or closes. |
| `Placeholder` | `string?` | Hint of the search field. Defaults to the built-in text. |
| `Shortcut` | `string` | The keyboard shortcut that opens the search: "mod+k" by default, where mod is Cmd on Apple platforms and Ctrl elsewhere. Ctrl, meta, alt and shift are accepted too. |
| `Ui` | `CommandPaletteUi?` | Slot overrides of the command palette. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardSearchButton`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button that opens the `KlyDashboardSearch` and shows its keyboard shortcut.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Keys` | `IReadOnlyList<string>?` | The keys of the shortcut to show, one `KlyKbd` each. Match the `Shortcut` of the search. Left out, the hint follows the platform in the browser (the command key and K on Apple devices, Ctrl and K elsewhere), matching the default `mod+k` shortcut, and the button announces `aria-keyshortcuts`. Keys you give are shown as given. |
| `Label` | `string?` | The text of the button. Defaults to the built-in text. |
| `Ui` | `DashboardSearchButtonUi?` | Per-instance class overrides of the slots of the dashboard search button, see `DashboardSearchButtonUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardSidebar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Sidebar for a dashboard: a column that collapses to icons and resizes from the lg breakpoint up, and an off-canvas panel below it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the sidebar landmark. Defaults to the built-in text. |
| `As` | `string` | The element to render, "aside" by default; use "div" inside another landmark. |
| `ChildContent` | `RenderFragment?` | The scrollable middle: navigation, search button. |
| `Collapsed` | `bool` | The collapsed state when nothing is remembered. `@bind-Collapsed` follows and sets it from .NET. |
| `CollapsedChanged` | `EventCallback<bool>` | Raised with the new state when the sidebar collapses to icons or expands. |
| `Collapsible` | `bool` | Also shows a collapse button in the header of the sidebar, besides `KlyDashboardSidebarCollapse`. |
| `Footer` | `RenderFragment?` | Bottom of the sidebar: the signed-in user. |
| `Header` | `RenderFragment?` | Top of the sidebar: a logo and a title. Hidden while the sidebar is collapsed. |
| `Id` | `string?` | Overrides the storage id taken from the group. The first sidebar of a group uses the key of the group, the next ones that key followed by "-2", "-3"; set an Id to name one yourself, and give it to the controls with `For`. A sidebar with an Id takes no number, so the first sidebar without one still gets the key of the group. Don't give it the key of the group itself, or it shares its saved state and the controls without a target with that first sidebar. |
| `MaxWidth` | `int` | Largest width the user can resize the sidebar to, in pixels. Defaults to 448. |
| `MinWidth` | `int` | Smallest width the user can resize the sidebar to, in pixels. Defaults to 192. |
| `MobileTitle` | `string?` | Title of the off-canvas panel shown on small screens. Defaults to the built-in text. |
| `Resizable` | `bool` | Adds a handle on the edge to resize the sidebar with the mouse or the keyboard. Sizes are in pixels, unlike the panels. |
| `Ui` | `SidebarUi?` | Slot overrides, the same as `KlySidebar`. |
| `Width` | `int` | Initial width of the sidebar column, in pixels. Defaults to 256. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardSidebarCollapse`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button that collapses the dashboard sidebar to icons and expands it again.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the dashboard sidebar collapse: a built-in `KlyColor` or the name of a color you defined. |
| `For` | `string?` | The storage id of the sidebar to collapse. Defaults to the one of the enclosing group, or the sidebar of the same dashboard. |
| `Label` | `string?` | Accessible name. Defaults to the built-in text. |
| `Size` | `KlySize` | Size of the dashboard sidebar collapse, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Variant` | `ButtonVariant` | Look of the button: `Solid`, `Outline`, `Soft`, `Subtle`, `Ghost` or `Link`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardSidebarToggle`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button that opens the off-canvas dashboard sidebar on small screens.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the dashboard sidebar toggle: a built-in `KlyColor` or the name of a color you defined. |
| `For` | `string?` | The storage id of the sidebar to open. Defaults to the one of the enclosing group, or the sidebar of the same dashboard. |
| `Label` | `string?` | Accessible name. Defaults to the built-in text. |
| `Size` | `KlySize` | Size of the dashboard sidebar toggle, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Variant` | `ButtonVariant` | Look of the button: `Solid`, `Outline`, `Soft`, `Subtle`, `Ghost` or `Link`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDashboardToolbar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Secondary bar under a navbar, with filters and bulk actions on the left and view options on the right.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The center area, between the left and the right ones. |
| `Left` | `RenderFragment?` | Filters and bulk actions, at the start of the toolbar. |
| `Right` | `RenderFragment?` | View options, at the end of the toolbar. |
| `Ui` | `DashboardToolbarUi?` | Per-instance class overrides of the slots of the dashboard toolbar, see `DashboardToolbarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDrawer`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Panel docked to one edge of the screen in a modal dialog, with a title, a description, a body and a footer.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name when there is no `KlyDrawer.Title`. |
| `Body` | `RenderFragment?` | Content of the body of the drawer. |
| `ChildContent` | `RenderFragment?` | The trigger: its first button opens the drawer. |
| `Close` | `bool` | Shows a close button in the header (a title or a description also shows the header). |
| `Description` | `string?` | Text under the title of the drawer. |
| `Direction` | `DrawerDirection` | The edge the drawer is docked to. |
| `Dismissible` | `bool` | Escape and a click on the backdrop close the drawer. |
| `Footer` | `RenderFragment?` | Footer actions. Add `data-kly-close` to a button to make it close the drawer. |
| `Handle` | `bool` | The grab handle shown on top and bottom drawers. It is decorative: the drawer closes with Escape, the backdrop or a close button. |
| `Inset` | `bool` | Floats the drawer away from the edges of the screen. |
| `Open` | `bool` | Open state. Use `@bind-Open`: the browser closes the drawer on its own (Escape, backdrop, close button). |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the drawer opens or closes, including Escape and a click outside. |
| `Title` | `string?` | Title of the drawer, which also names the dialog. |
| `Ui` | `DrawerUi?` | Per-instance class overrides of the slots of the drawer, see `DrawerUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDropdownMenu`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Button that opens a menu of items, with submenus, keyboard navigation and typeahead.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The trigger; it must contain a button. |
| `Items` | `IReadOnlyList<KlyMenuItem>` | The entries. An item with `KlyMenuItem.Children` opens a submenu; a heading followed by items and a separator makes a labelled group. |
| `OnSelect` | `EventCallback<KlyMenuItem>` | Called with the activated item. Link items navigate instead, and items with children open their submenu. |
| `Open` | `bool` | Whether the menu is open. Bind it with `@bind-Open` when .NET needs the state. |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the menu opens or closes. |
| `Placement` | `string` | Floating UI placement of the menu: top, right, bottom or left, optionally with `-start` or `-end`. Defaults to `bottom-start`. |
| `Ui` | `DropdownMenuUi?` | Per-instance class overrides of the slots of the dropdown menu, see `DropdownMenuUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyEditor`

`Klyroo.UI` · component · inherits `KlyFieldBase<string>`

Rich text editor that binds to an HTML string, with formatting commands, links, images, task lists and slash, mention and emoji menus.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Toolbars, drag handle and suggestion menus: components of the editor family placed inside it. They are wired to the editor by the browser. |
| `ImageSchemes` | `IReadOnlyList<string>?` | The schemes an image address may have: `http` and `https` (the default, `null`) or one of them. An address with another scheme, no host, a user name or longer than 2,048 characters is refused when it is entered, with a message that names the schemes, and is not an image when it is pasted or loaded. List what the server keeps: a server that keeps only https images should say so here. |
| `Label` | `string?` | Accessible name of the editable area when there is no enclosing `KlyFormField` label. Defaults to `KlyMessages.EditorLabel`. |
| `LinkSchemes` | `IReadOnlyList<string>?` | The schemes a link may have, as absolute addresses (for example `https`, `http` and `mailto`). Without a list, the editor takes what TipTap takes: those schemes and a few more (tel:, ftp:), and relative addresses. With one, an address of another scheme or a relative one is refused when it is entered, with a message that names the schemes, and is not a link when it is pasted, typed or loaded. List what the server keeps. |
| `MaxLength` | `int?` | The most characters of HTML the editor reports (`KlyFieldBase.ValueChanged`), or null for no limit. A value longer than that is never sent to .NET: the editor keeps it on the page, marks the text invalid (`aria-invalid`, and `data-over-limit` on the element) and raises `KlyEditor.OnLimit`, so the bound value stays the last one that fit. Measured on the HTML the editor writes, in UTF-16 characters; a server that cleans or rewrites the HTML should measure again. |
| `OnLimit` | `EventCallback<KlyLimitEventArgs>` | Raised when the HTML goes past `KlyEditor.MaxLength`, while it keeps changing past it, and once more when it is back within it. Settles after 400 ms without typing, like the value. |
| `Placeholder` | `string?` | Shown while the editor is empty. |
| `ReadOnly` | `bool` | The text can be selected and copied but not changed. The value still posts with the form. |
| `Ui` | `EditorUi?` | Per-instance class overrides of the slots of the editor, see `EditorUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the editor frame, as a `FieldVariant`. |

Also accepts the parameters of `KlyFieldBase<string>`.

#### `KlyEditorDragHandle`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Handle that drags a block of the editor, or moves it with Alt and the arrow keys.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the handle: a built-in `KlyColor` or the name of a color you defined. |
| `Label` | `string?` | Accessible name of the handle. Defaults to `KlyMessages.EditorDragHandle`. The handle shows beside the block under the pointer or holding the cursor, and drags it. The keyboard alternative is Alt and the up or down arrow in the text (or the arrows on the focused handle), and the `EditorAction.MoveUp` and `EditorAction.MoveDown` toolbar buttons. |
| `Ui` | `EditorDragHandleUi?` | Per-instance class overrides of the slots of the editor drag handle, see `EditorDragHandleUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyEditorEmojiMenu`

`Klyroo.UI` · component · inherits `KlyEditorMenuBase`

The emoji menu: opens when ":" is typed followed by at least one character, and inserts the emoji as text.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Items` | `IReadOnlyList<EditorEmojiItem>?` | Emoji of the menu. Defaults to `EditorEmojiItem.Defaults`. |

Also accepts the parameters of `KlyEditorMenuBase`.

#### `KlyEditorMentionMenu`

`Klyroo.UI` · component · inherits `KlyEditorMenuBase`

The mention menu: opens when "@" is typed and inserts a mention, a span with `data-type="mention"`, `data-id` and `data-label`. Items are supplied from .NET and filtered in the browser.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Items` | `IReadOnlyList<EditorMentionItem>` | Who or what can be mentioned. |

Also accepts the parameters of `KlyEditorMenuBase`.

#### `KlyEditorMenuBase`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Base of the menus that open when a trigger character is typed in the editor. The options are rendered as a listbox; the browser filters them as the user types and takes the keys (up, down, Enter, Tab, Escape) while the menu is open, so typing does not make a round trip to .NET.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Char` | `string?` | Character that opens the menu. |
| `Label` | `string?` | Accessible name of the list. |
| `Limit` | `int` | Most options shown at a time. |
| `Size` | `KlySize` | Size of the menu, from `Xs` to `Xl`. |
| `Ui` | `EditorMenuUi?` | Per-instance class overrides of the slots of the menu, see `EditorMenuUi`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyEditorSuggestionMenu`

`Klyroo.UI` · component · inherits `KlyEditorMenuBase`

The slash menu: opens when "/" is typed (after a space or at the start of a block) and inserts a block or a format. Items run in the browser: pass them as a parameter, no round trip is made while the user types.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Items` | `IReadOnlyList<EditorSuggestionItem>?` | Entries of the menu. Defaults to `EditorSuggestionItem.For` of the messages of the locale in effect. |

Also accepts the parameters of `KlyEditorMenuBase`.

#### `KlyEditorToolbar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Formatting toolbar of a `KlyEditor`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Color of pressed buttons. |
| `Items` | `IReadOnlyList<EditorToolbarItem>?` | The buttons, in order. Defaults to `EditorToolbarItem.Defaults`, or `EditorToolbarItem.BubbleDefaults` for a bubble toolbar. |
| `Label` | `string?` | Accessible name of the toolbar. Defaults to `KlyMessages.EditorToolbar`. |
| `Layout` | `EditorToolbarLayout` | Fixed (always visible, above the text) or Bubble (floats above a selection). Buttons are enabled once the editor is ready; before that, in static pages, they are disabled. |
| `Size` | `KlySize` | Size of the toolbar buttons, from `Xs` to `Xl`. |
| `Ui` | `EditorToolbarUi?` | Per-instance class overrides of the slots of the editor toolbar, see `EditorToolbarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyEmpty`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Placeholder for an area without content, with an icon, a title, a description and actions.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Buttons or links under the text. |
| `ChildContent` | `RenderFragment?` | Extra content between the description and the actions. |
| `Description` | `string?` | Text under the title. |
| `HeadingLevel` | `int?` | Renders the title as a heading of this level (1 to 6) instead of a paragraph. |
| `Icon` | `RenderFragment?` | Illustration above the title, 24x24 icons such as `KlyIcons.Named("inbox")` fit. |
| `Size` | `KlySize` | Size of the empty, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Title` | `string?` | Title of the placeholder, rendered with the level of `KlyEmpty.HeadingLevel`. |
| `Ui` | `EmptyUi?` | Per-instance class overrides of the slots of the empty, see `EmptyUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `EmptyVariant` | Look of the placeholder: `Outline`, `Soft`, `Subtle` or `Naked`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyError`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Content of an error page: status code, title, message and a way back. Use it as the body of a not found page or inside an error boundary.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Actions` | `RenderFragment?` | Replaces the back button. |
| `ChildContent` | `RenderFragment?` | Extra content between the message and the actions. |
| `Clear` | `bool` | Shows the button that leads back to `KlyError.Href`. Ignored when `KlyError.Actions` is set. |
| `ClearLabel` | `string?` | Text of the back button. Defaults to the built-in text. |
| `Exception` | `Exception?` | The exception of an `ErrorBoundary`. Sets the status code to 500 when none is given. |
| `HeadingAs` | `string` | The element of the title: "h1" by default, "h2" when the error replaces a part of a page. |
| `Href` | `string` | Where the back button goes. The default "./" is the home of the app: relative to `<base href>`, so it is right below a path base too. |
| `Message` | `string?` | The explanation under the title. Defaults to a text for `KlyError.StatusCode`. |
| `ShowDetails` | `bool` | Shows the message of `KlyError.Exception`. Leave it off in production: exception messages can leak internals. |
| `StatusCode` | `int?` | The HTTP status code, shown above the title. 404, 403 and 500 have built-in texts. |
| `StatusMessage` | `string?` | The title. Defaults to a text for `KlyError.StatusCode`. |
| `Ui` | `ErrorUi?` | Per-instance class overrides of the slots of the error, see `ErrorUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFieldGroup`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Joins buttons, inputs and selects into one control by removing their inner radii. It is pure CSS.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the group, when it needs one. |
| `ChildContent` | `RenderFragment?` | The controls to attach to each other: `KlyButton`, `KlyInput`, `KlySelect`… |
| `Orientation` | `KlyOrientation` | Whether the controls sit side by side (`Horizontal`) or stacked (`Vertical`). |
| `Ui` | `FieldGroupUi?` | Per-instance class overrides of the slots of the field group, see `FieldGroupUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFileUpload`

`Klyroo.UI` · component · inherits `KlyComponentBase`

File input with a drop zone, a list of the chosen files with remove buttons, and limits on size, count and type.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Accept` | `string?` | The native `accept` attribute: comma separated extensions (`.png`) and MIME types (`image/*`). Also enforced on dropped files. |
| `Color` | `KlyColor` | Semantic color of the drop zone: a built-in `KlyColor` or the name of a color you defined. |
| `Disabled` | `bool` | Disables the input and the drop zone. |
| `Hint` | `string?` | Secondary text of the drop zone, such as the accepted formats. |
| `Id` | `string?` | The `id` of the file input, which a `label` can point at. |
| `Label` | `string?` | Main text of the drop zone. Defaults to `KlyMessages.DropFiles`. |
| `MaxFiles` | `int?` | Most files kept at once. Defaults to 1 unless `KlyFileUpload.Multiple`. |
| `MaxSize` | `long?` | Largest file, in bytes. Larger files are refused in the browser with a message, before anything is sent. |
| `Multiple` | `bool` | Allows choosing several files. |
| `Name` | `string?` | Form field name. A static form posts the chosen files under it. |
| `OnChange` | `EventCallback<InputFileChangeEventArgs>` | Raised with the chosen files when the page is interactive. Setting it renders Blazor's `InputFile`; read the files with `e.GetMultipleFiles()` or `e.File`. Without it, the input is a plain one, which is what static forms need. |
| `Required` | `bool` | Marks the input as required for the browser and for assistive technology. |
| `Size` | `KlySize?` | Defaults to the size of the enclosing `KlyFormField`, or medium. |
| `Ui` | `FileUploadUi?` | Per-instance class overrides of the slots of the file upload, see `FileUploadUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFonts`

`Klyroo.UI` · component · inherits `ComponentBase`

Place in `<head>`, before your own stylesheet. Links the stylesheet generated at build time from the `KlyrooFont` items of the project (the `@font-face` rules, the fallback faces and `--klyroo-font-sans` / `--klyroo-font-mono`) and preloads the main face of every font that asks for it, so the text that needs it is not held up by a stylesheet that is read first and a font that is requested after. Everything comes from the app's own origin: nothing is requested from a font provider while the page runs.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ManifestJson` | `string?` | A manifest to render instead of the one generated at build, for tests and previews. Same shape as the generated `klyroo.fonts.json`. |

#### `KlyFooter`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Site footer with optional top and bottom areas and left, center and right slots.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Bottom` | `RenderFragment?` | A full-width area below. |
| `ChildContent` | `RenderFragment?` | The middle of the footer: links, a newsletter form. |
| `Left` | `RenderFragment?` | Start side: a copyright line, a logo. |
| `Right` | `RenderFragment?` | End side: social links, a language select. |
| `Top` | `RenderFragment?` | A full-width area above, typically a `KlyFooterColumns`. |
| `Ui` | `FooterUi?` | Per-instance class overrides of the slots of the footer, see `FooterUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFooterColumns`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Columns of links under a heading, as a navigation landmark. Meant for the top area of a `KlyFooter`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the links navigation. Needed when the page has several. |
| `Columns` | `IReadOnlyList<KlyFooterColumn>` | The columns, each a title and a list of links (`KlyFooterColumn`). |
| `HeadingLevel` | `int` | Level of the column headings: 3 by default. |
| `Left` | `RenderFragment?` | Content before the columns on wide screens: a logo and a short description. |
| `Right` | `RenderFragment?` | Content after the columns: a newsletter form. |
| `Ui` | `FooterColumnsUi?` | Per-instance class overrides of the slots of the footer columns, see `FooterColumnsUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyForm`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Form that wraps an `EditForm`, gives its fields the edit context and can show a summary of the validation errors.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment<EditContext>?` | The fields of the form, with the edit context as the context value. |
| `Disabled` | `bool` | Disables every control inside. |
| `EditContext` | `EditContext?` | The edit context to use. Give either this or `KlyForm.Model`. |
| `Enhance` | `bool` | Enhanced form handling in static server rendering: the post updates the page without a reload. |
| `FormName` | `string?` | Name of the form in static server rendering, where it identifies the posted form. |
| `Model` | `object?` | The object edited. Give either this or `KlyForm.EditContext`. |
| `OnInvalidSubmit` | `EventCallback<EditContext>` | Raised with the edit context when the form is submitted and validation fails. |
| `OnValidSubmit` | `EventCallback<EditContext>` | Called with a valid form. While its task runs the form is busy and its controls are disabled. |
| `ShowSummary` | `bool` | Shows the list of every validation message above the fields. |
| `Ui` | `FormUi?` | Per-instance class overrides of the slots of the form, see `FormUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Validate` | `bool` | Runs data annotation validation on submit and on field changes. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormField`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Wrapper that gives a control its label, description, hint and error, and ties them together with the right ids for accessibility.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The control, such as a `KlyInput`. |
| `Description` | `string?` | Text shown under the label and tied to the control with `aria-describedby`. |
| `Error` | `string?` | Error text. Without it, the first validation message of `KlyFormField.Field` is shown. |
| `Field` | `FieldIdentifier?` | The bound field, for validation messages inside an `EditForm`: `Field="FieldIdentifier.Create(() => Model.Email)"` (or `new FieldIdentifier(Model, nameof(Model.Email))`). It names the same field the control inside is bound to, so the message under the control is the one the control's own error styling follows. |
| `Help` | `string?` | Help text shown under the control. |
| `Hint` | `string?` | Short text next to the label, such as "Optional". |
| `Id` | `string?` | Id of the control inside. Defaults to a generated one. |
| `Label` | `string?` | Label of the control, tied to it with `for`. |
| `Required` | `bool` | Marks the label and the control as required. |
| `Size` | `KlySize` | Size passed to the controls inside, from `Xs` to `Xl`. |
| `Ui` | `FormFieldUi?` | Per-instance class overrides of the slots of the form field, see `FormFieldUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyHead`

`Klyroo.UI` · component · inherits `ComponentBase`

The head content of Klyroo for a Blazor Hybrid web view: the theme variables (`KlyThemeStyle`) and the self-hosted fonts (`KlyFonts`), sent to the head through `HeadContent`. A web view has no server that renders `<head>`, so place this once in the layout of the app and add a `HeadOutlet` root component with the selector `head::after` to the `BlazorWebView`. It renders nothing in a browser, where `App.razor` places the head components itself, unless `KlyHead.Always` is set.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Always` | `bool` | Renders the head content in a browser too (an app with a `HeadOutlet` that does not place the components in `App.razor`). |
| `Fonts` | `bool` | Renders the self-hosted fonts. Turn it off when the app declares none. |
| `Nonce` | `string?` | Nonce for a Content-Security-Policy that forbids inline styles without one. |

#### `KlyHeader`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Site header with a title or logo, links and actions. On small screens the links move into a slideover menu.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the `KlyHeader.Items` navigation. Needed when the page has several. |
| `Body` | `RenderFragment?` | Content of the mobile menu. Replaces the vertical `KlyHeader.Items` navigation. |
| `ChildContent` | `RenderFragment?` | The middle of the header, shown from the lg breakpoint up. Replaces the `KlyHeader.Items` navigation. |
| `Href` | `string` | Where the title link goes. The default "./" is the home of the app: relative to `<base href>`, so it is right below a path base too. |
| `Items` | `IReadOnlyList<KlyNavigationItem>` | Site links: a horizontal navigation in the middle, and a vertical one in the mobile menu. Set `KlyHeader.ChildContent` or `KlyHeader.Body` to replace either. |
| `Left` | `RenderFragment?` | Replaces the title link: a logo, or a logo with the title. |
| `Menu` | `bool` | Shows the mobile menu button below the lg breakpoint, when there is something to put in the menu. |
| `MenuSide` | `SlideoverSide` | The edge the mobile menu slides in from. |
| `MenuTitle` | `string?` | Title of the mobile menu panel. Defaults to the built-in text. |
| `Right` | `RenderFragment?` | The right side: actions, color mode button, language select. Always visible. |
| `Sticky` | `bool` | Keeps the header at the top of the screen while the page scrolls. |
| `Title` | `string?` | The title link on the left, used when `KlyHeader.Left` is not set. |
| `Ui` | `HeaderUi?` | Per-instance class overrides of the slots of the header, see `HeaderUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyIcon`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Inline SVG icon by name, from the built-in Lucide set, any Iconify collection bundled at build time, or icons you register.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Label` | `string?` | Accessible name. Without it the icon is decorative and hidden from assistive technology. |
| `Name` | `string` | The icon, by name, in any supported form: `i-lucide-search`, `lucide:search`, `ph:house`, `i-simple-icons-github`, or a bare Lucide name (`search`). The Lucide icons components use are built in; every other Iconify icon written as a literal in the app's sources is bundled at build time, and icons added to `KlyrooIconOptions` or from a `KlyrooIconCollection` folder work too. An icon that is not found renders an empty icon marked `data-kly-missing-icon`. |
| `Size` | `KlySize` | Size of the icon, from `Xs` to `Xl`. |
| `Ui` | `IconUi?` | Per-instance class overrides of the slots of the icon, see `IconUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyImage`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Image element that fills in the width, height, srcset, sizes and loading attributes with the help of an image provider.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Alt` | `string?` | Text alternative. Required: pass an empty string for a decorative image. |
| `Densities` | `string?` | Densities of the `srcset`, like `"x1 x2"`. Defaults to `KlyImageSettings.Densities`. |
| `FetchPriority` | `KlyFetchPriority?` | `high` speeds up the image that matters most (the hero); preloading sets it to high unless you choose. |
| `Fit` | `KlyImageFit?` | How the image fills `KlyImage.Width` by `KlyImage.Height`. Cover when unset. |
| `Format` | `string?` | Output format: "webp", "avif", "jpeg", "png", "gif" or "svg". Only a provider that converts honors it. |
| `Height` | `int?` | Height of the image in CSS pixels, with the same rules as `KlyImage.Width`. |
| `ImageOptions` | `KlyImageOptions?` | Settings shared with the other image parameters: the parameters below override the ones set here. |
| `Loading` | `KlyImageLoading?` | Lazy by default, eager when `KlyImage.Preload` is set. Use eager for the image visible at first paint. |
| `Modifiers` | `IReadOnlyDictionary<string, string>?` | Provider specific modifiers (for example `gravity`), merged over the preset's. |
| `Placeholder` | `KlyImagePlaceholder?` | A placeholder shown behind the image while it loads: `KlyImagePlaceholder.Url` for a small image of yours, or `KlyImagePlaceholder.Auto` to ask the provider for a tiny (and, when it can blur, blurred) version of the image. It is painted as the background of the element. |
| `Preload` | `bool?` | Adds `<link rel="preload" as="image" imagesrcset imagesizes>` to the document head, and loads eagerly. It goes through `HeadContent`, so the app needs a `HeadOutlet` (the Blazor Web App template has one). In a Blazor Hybrid web view no link is added (the image only loads eagerly with a high priority). |
| `Preset` | `string?` | Name of an entry of `KlyImageSettings.Presets`. |
| `Provider` | `string?` | Name of a registered provider; `KlyImageSettings.DefaultProvider` when unset. |
| `Quality` | `int?` | Quality from 1 to 100. |
| `Sizes` | `string?` | Responsive sizes: `"100vw sm:50vw md:400px"` (an unprefixed entry is the default; each breakpoint applies from its width upward). Breakpoints come from `KlyImageSettings.Screens`. It becomes the `sizes` attribute and decides the widths of the `srcset`. |
| `Src` | `string?` | Address of the image: a root-relative path ("/img/team.jpg") or an absolute URL. |
| `Width` | `int?` | Width of the image in CSS pixels. It is also the base of the 1x, 2x… candidates, and it reserves the space before the image loads. A value that is not positive is written as given and takes no part in the resolution. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyInfiniteScroll`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Load-more point for a growing list: it reports each time the user scrolls near it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `HasMore` | `bool` | Whether there is anything left to load. When false the sentinel and the button are removed and the end is announced. |
| `Href` | `string?` | Where the next items are, for a page without .NET (static rendering, or before the page is interactive): the button becomes a link to it, like `?page=2` or the next cursor URL. Once the page is interactive the button loads in place instead. |
| `Label` | `string?` | Text of the button. Defaults to the translated "Load more". |
| `Loading` | `bool` | A load is running. While it is true nothing is requested again; when it turns false the sentinel is checked again, so a sentinel still in view asks for the next items. A load started through `KlyInfiniteScroll.OnLoadMore` counts as running until the callback returns, so an async handler does not have to set this. The component does not set `aria-busy` (it would hide its own announcement): put `aria-busy` on your list while it loads. |
| `OnLoadMore` | `EventCallback` | Asked for the next items: each time the sentinel comes into view and when the button is used, never twice for one visibility until the load is over. Only runs when the page is interactive. After a load that added nothing (or threw), nothing is asked again until the sentinel has left the view and come back, or the button is used: a handler that has nothing more to give, or failed, should set `KlyInfiniteScroll.HasMore` to false, or show a retry. |
| `Root` | `string?` | A CSS selector of the element that scrolls (`"#feed"`). By default the closest scrollable ancestor is used (`overflow-y` auto or scroll), or the viewport when there is none. |
| `RootMargin` | `string` | How far before the sentinel is really in view the load starts, as the margin of an `IntersectionObserver` (pixels or percent, CSS order top right bottom left). The default loads 200px ahead of the end of a list; for a list that loads upward put the component first and use `"200px 0px 0px 0px"`. |
| `ShowEnd` | `bool` | Shows the translated "No more items" when `KlyInfiniteScroll.HasMore` is false. It is announced either way. |
| `Ui` | `InfiniteScrollUi?` | Per-instance class overrides of the slots of the infinite scroll, see `InfiniteScrollUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyInput`

`Klyroo.UI` · component · inherits `KlyFieldBase<string>`

Single-line text input with a variant, a size and optional leading and trailing icons. It binds to a string and takes part in `EditForm` validation.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ClearOnEscape` | `bool` | Escape empties the field and reports the empty value (immediately, whatever `KlyInput.Debounce` says). On an already empty field Escape is left alone, so it still closes a modal or popover around the field. The browser does the work, in every render mode. The Escape that clears text is consumed: no other handler of the page, Blazor's `@onkeydown` included, sees it. A read-only field is not cleared. |
| `Debounce` | `int?` | Milliseconds of quiet after the last keystroke before the value is reported, in the browser: typing a word raises one `ValueChanged` per pause instead of one per key (a round trip each in Server mode). Enter and leaving the field report at once, and nothing is reported in the middle of an IME composition. A render that arrives with an older value does not overwrite what was typed after it. `null` (the default) keeps the plain behavior. Debounce wins over `KlyInput.Immediate` when both are set. Inside an `EditForm` the field is validated each time a value is reported, so after each pause (or Enter, or leaving the field), not on every key. Static rendering has no .NET to report to, so there the attribute only does what the browser can do alone. While text is waiting to be reported, a value written into the field by the page's own script, or by a render that started before the last report, is ignored: typed text wins. To replace the text on purpose (a "reset" button), call `KlyInput.SetValueAsync`. |
| `Immediate` | `bool` | Update the bound value on every keystroke instead of on change. Costs a round trip per key in Server mode. |
| `LeadingIcon` | `RenderFragment?` | Icon at the start of the field, as markup such as a `KlyIcon`. |
| `Placeholder` | `string?` | Hint shown while the field is empty. |
| `TrailingIcon` | `RenderFragment?` | Icon at the end of the field, as markup such as a `KlyIcon`. |
| `Type` | `string` | The `type` attribute: text, email, password, search, tel, url, number… |
| `Ui` | `InputUi?` | Per-instance class overrides of the slots of the input, see `InputUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<string>`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `SetValueAsync(string? value)` | `ValueTask` | Replaces the text of the field on purpose, in the browser, even text still waiting for its pause: the pending text and the debounce timer are dropped, and reports sent before the call but not yet received are ignored. Call it through `@ref`, for example from a "reset" button (`@onmousedown:preventDefault` keeps the focus in the field). The change is reported once, as a user change would be: `ValueChanged` is raised (when `value` differs from `Value`) and the edit context is notified, so a bound model stays consistent. It needs a live component, so it works in Server, WebAssembly and Auto once the page is interactive. In static server rendering there is no component to call and it does nothing. Without `KlyInput.Debounce` or `KlyInput.ClearOnEscape` the input is bound the plain way and the call only sets the value. |

#### `KlyInputDate<TValue>`

`Klyroo.UI` · component · inherits `KlyFieldBase<TValue>`

Date field with a calendar popup, with localized formatting. It binds to a `DateOnly`, a `DateTime` or a `DateTimeOffset`, or to the nullable form of one of them. Picking a day keeps the time of day of the current value (and the kind of a `DateTime`, the offset of a `DateTimeOffset`); a value that had none (null before, or the default) starts at midnight, a `DateTimeOffset` in the offset of the server's local time zone on that day.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Culture` | `CultureInfo?` | Source of the calendar's month and day names. Defaults to the format culture of the locale in effect (the regional current culture when it has the same language). |
| `Max` | `DateOnly?` | Latest date, for the input and the calendar. |
| `Min` | `DateOnly?` | Earliest date, for the input and the calendar. A day (`DateOnly`) whatever the bound type: use `DateOnly.FromDateTime(...)` to limit a `DateTime` field. |
| `Ui` | `InputDateUi?` | Per-instance class overrides of the slots of the input date, see `InputDateUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |
| `WeekStart` | `DayOfWeek?` | First column of the calendar. Defaults to the culture's first day of the week. |

Also accepts the parameters of `KlyFieldBase<TValue>`.

#### `KlyInputMenu<TValue>`

`Klyroo.UI` · component · inherits `KlyFieldBase<TValue>`

Text input with a list of suggestions that filters as the user types, bound to the typed text (a string) or, with `Strict` or any other type, to the chosen option's value (a number, an enum, a `Guid`...). The options can be static or fetched from .NET.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Debounce` | `int` | With `KlyInputMenu.OptionsProvider`: the quiet time in milliseconds after typing before the source is searched. |
| `Items` | `IReadOnlyList<IKlySelectItem<TValue>>` | The suggestions, filtered by what is typed. Not needed when `KlyInputMenu.OptionsProvider` loads them; given together with it, these items are not listed but their labels are known (the label of a `KlyInputMenu.Strict` value the provider has not returned yet). The parameter must be passed when there is no provider, and a null value (`Items="@_list"` while the list is still loading) is an empty list, before and after it loads. |
| `MinLength` | `int` | With `KlyInputMenu.OptionsProvider`: the shortest text that is searched. A shorter, non-empty text shows a hint and lists nothing. |
| `OptionsKey` | `string?` | With `KlyInputMenu.OptionsProvider`: what the suggestions depend on besides the typed text (the country whose cities are suggested). When it changes (compared ordinally), the loaded pages, the search text, the cursor and the resolved label are forgotten, a `KlyInputMenu.Strict` value is resolved again, and the list loads again the next time it opens. A new delegate with the same key keeps what was loaded. Without a key the source is assumed not to change. |
| `OptionsProvider` | `KlyOptionsProvider?` | Loads the suggestions from a remote source, one page at a time, instead of listing `KlyInputMenu.Items`. The browser stops filtering and the field searches the source with what is typed: the text is trimmed, sent after `KlyInputMenu.Debounce` milliseconds of quiet and only when it has at least `KlyInputMenu.MinLength` characters (an empty text lists the unfiltered suggestions; a `KlyInputMenu.Strict` field searches the empty text when it opens, as it always lists everything); an older request is cancelled and an older answer never replaces a newer one. The next page loads as the list scrolls, a bit before its end, and by a "Load more" button. While loading the list is `aria-busy` and a polite status says so; when the provider throws, the list shows a translated error with a Retry button (Enter in the field also retries). Interactive only: a statically rendered page (and the prerender before the page becomes interactive) never calls the provider. A free text field then works as a plain text field with no suggestions; a `KlyInputMenu.Strict` field shows its value as it was given (read only, since no suggestion can be chosen) and still posts it. A parent that renders again with an equal or a new delegate keeps the loaded state; give the component a `@key` to start from scratch. |
| `PageSize` | `int` | With `KlyInputMenu.OptionsProvider`: the number of suggestions requested per page. |
| `Placeholder` | `string?` | Hint shown while the field is empty. |
| `ResolveSelected` | `KlySelectedOptionsResolver?` | With `KlyInputMenu.OptionsProvider` and `KlyInputMenu.Strict`: returns the option of the value when the loaded pages do not contain it (a value restored from the address or a saved form), so the field shows its label. Called once for that value, as soon as the component is interactive. Without it the field shows the raw value. |
| `Strict` | `bool` | Only a suggestion is a valid value: `Value` is the chosen item's value and the field shows its label. Otherwise (the default) the typed text is the value and suggestions only complete it. Only a string can be typed: when `TValue` is not `String` the field is always strict, whatever this parameter says. |
| `Ui` | `InputMenuUi?` | Per-instance class overrides of the slots of the input menu, see `InputMenuUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<TValue>`.

#### `KlyInputNumber<TNumber>`

`Klyroo.UI` · component · inherits `KlyFieldBase<TNumber>`

Number field with increment and decrement buttons, limits and localized formatting. It binds to any built-in numeric type (`int`, `long`, `decimal`, `double`, `byte`...) and to its nullable form; only a nullable type can be empty, clearing the field of a non-nullable one sets zero.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Culture` | `CultureInfo?` | Culture used to show and read the number, such as `fr-FR`. Defaults to the format culture of the locale in effect (the regional current culture when it has the same language). The posted value is always invariant. |
| `Max` | `decimal?` | Largest value accepted. There is no upper limit when it is null. |
| `Min` | `decimal?` | Smallest value accepted. There is no lower limit when it is null. A `decimal` whatever the bound type, so `Min="0"` works for an `int`, a `float` or a `decimal?`. |
| `Orientation` | `KlyOrientation` | Horizontal puts the buttons on both sides of the field, vertical stacks them at its end. |
| `Placeholder` | `string?` | Hint shown while the field is empty. |
| `Step` | `decimal` | Amount added or removed by the buttons and the arrow keys; Page Up and Page Down use ten steps. |
| `Ui` | `InputNumberUi?` | Per-instance class overrides of the slots of the input number, see `InputNumberUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<TNumber>`.

#### `KlyInputRating`

`Klyroo.UI` · component · inherits `KlyFieldBase<double>`

Star rating input, bindable to a `double`, with half steps and a read-only mode.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AllowHalf` | `bool` | Lets a star be chosen by halves (0.5, 1, 1.5…). |
| `AriaLabel` | `string?` | Accessible name of the group when no `KlyFormField` label is around. |
| `Max` | `int` | Number of stars. The value goes from 0 (nothing chosen) to this. |
| `ReadOnly` | `bool` | Shows the value without letting it change. Rendered as an image with a text alternative, still posting the value. |
| `Ui` | `InputRatingUi?` | Per-instance class overrides of the slots of the input rating, see `InputRatingUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyFieldBase<double>`.

#### `KlyInputTags`

`Klyroo.UI` · component · inherits `KlyListFieldBase`

Input that turns each entered text into a removable tag and binds to the list of tags.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Max` | `int` | Most tags allowed. Past it, typed text is ignored. Zero means no limit. |
| `Placeholder` | `string?` | Hint shown while no text is being typed. |
| `Ui` | `InputTagsUi?` | Per-instance class overrides of the slots of the input tags, see `InputTagsUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyListFieldBase`.

#### `KlyInputTime`

`Klyroo.UI` · component · inherits `KlyFieldBase<TimeOnly?>`

Time field with hour and minute segments, bindable to a `TimeOnly?`, in 12 or 24 hour format.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the field when it is not inside a `KlyFormField` with a label. |
| `Hour12` | `bool?` | Show an AM/PM segment. Defaults to what the culture's short time pattern uses. |
| `Step` | `int` | Minutes added or removed by the arrow keys on the minute segment. |
| `Ui` | `InputTimeUi?` | Per-instance class overrides of the slots of the input time, see `InputTimeUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<TimeOnly?>`.

#### `KlyKbd`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Keyboard key cap, to show a shortcut.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The key, when `Value` is not enough. |
| `Color` | `KlyColor` | Semantic color of the kbd: a built-in `KlyColor` or the name of a color you defined. |
| `Size` | `KlySize` | Size of the kbd, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `KbdUi?` | Per-instance class overrides of the slots of the kbd, see `KbdUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `string?` | The key to show, "Ctrl" or "K" for example. `KlyKbd.ChildContent` wins when both are set. |
| `Variant` | `KlyVariant` | Look of the key cap: `Solid`, `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyLink`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Anchor that marks itself active when its address matches the current page, with an optional disabled state.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Active` | `bool?` | Forces the active state instead of comparing `KlyLink.Href` with the current address. |
| `ActiveClass` | `string?` | Classes added while the link is active, merged after the theme. |
| `ChildContent` | `RenderFragment?` | Content of the link. |
| `Disabled` | `bool` | Renders the link without an `href`, so it cannot be followed or focused. |
| `Exact` | `bool` | Requires the whole path to match to be active. Without it the link is also active on the pages below its path ("/docs" is active on "/docs/theming"), except a link to the root of the app, which is always exact. The query string and the fragment never take part in the comparison. |
| `Href` | `string?` | Address the link points to. A link without it is not followable. |
| `InactiveClass` | `string?` | Classes added while the link is not active, merged after the theme. |
| `Label` | `string?` | Text of the link. Use `ChildContent` for markup. |
| `Raw` | `bool` | Drops the theme classes, keeping only `KlyComponentBase.Class`, `KlyLink.ActiveClass` and `KlyLink.InactiveClass`. |
| `Target` | `string?` | The `target` attribute, such as `_blank`. |
| `Ui` | `LinkUi?` | Per-instance class overrides of the slots of the link, see `LinkUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyListbox<TValue>`

`Klyroo.UI` · component · inherits `KlyListFieldBase<TValue>`

Always visible list of options with single or multiple selection, bound to a list of strings, numbers, enums, `Guid`s or any value that identifies an option.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name when no `KlyFormField` label is around. |
| `Items` | `IReadOnlyList<IKlySelectItem<TValue>>` | The options of the list, as `KlySelectItem` (`KlySelectItem` for strings). |
| `Multiple` | `bool` | Several values can be selected, each posting under `Name`. |
| `Ui` | `ListboxUi?` | Per-instance class overrides of the slots of the listbox, see `ListboxUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyListFieldBase<TValue>`.

#### `KlyLocaleSelect`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Select of the available locales. Choosing one writes the culture cookie and reloads the page.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the select. Defaults to the built-in text. |
| `Color` | `KlyColor` | Semantic color of the locale select: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `CookieName` | `string` | The cookie to write. The default is the one the ASP.NET Core request localization middleware reads. |
| `CookiePath` | `string` | The path of the cookie. It must cover every page that is localized. The default "/" makes the culture one for the whole origin: a language chosen in an app applies to the other apps of that origin, such as the ones served below a path base. It has to stay "/" for that to work, because a server keeps the last of two cookies of the same name and the cookie of another path could win. |
| `Locales` | `IReadOnlyList<KlyLocale>?` | The locales to offer, shown by `KlyLocale.Name` (with `KlyLocale.Flag` when set). Defaults to `KlyrooOptions.Locales`. Each code must be a culture the server supports. |
| `OnLocaleChange` | `EventCallback<string>` | Raised with the code of the chosen locale in a Blazor Hybrid web view, after it was saved. A browser has no use for it: the page reloads. |
| `Reload` | `bool?` | Reloads the page after the choice is saved. In a browser it is on by default (the server renders the page in the new culture). In a Blazor Hybrid web view it is off by default: the culture of a .NET process is read at start, so set it to true to apply the choice right away (the culture of the process is changed and the web view reloads, losing the state of the page), or leave it off and let the choice apply at the next start. |
| `Size` | `KlySize` | Size of the select, from `Xs` to `Xl`. |
| `Ui` | `LocaleSelectUi?` | Per-instance class overrides of the slots of the locale select, see `LocaleSelectUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `string?` | The selected locale code. Defaults to the code of the locale in effect (the enclosing `KlyApp`, or the one matching the current UI culture). In WebAssembly the client has its own culture: pass the value when it can differ from the server's. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyMain`

`Klyroo.UI` · component · inherits `KlyComponentBase`

The main landmark of a page. Its minimum height is the viewport minus the header (`--klyroo-header-height`, 4rem by default), so a short page still pushes the footer to the bottom of the screen.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `As` | `string` | The element to render, "main" by default. A page has only one main landmark: use "div" when the layout already has one. |
| `ChildContent` | `RenderFragment?` | Content of the main area. |
| `Ui` | `MainUi?` | Per-instance class overrides of the slots of the main area, see `MainUi`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyMarquee`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Endless horizontal or vertical scrolling strip of content, in pure CSS. It pauses on hover and stops when the user prefers reduced motion.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The items to scroll. Avoid ids and focusable elements: the content is repeated. |
| `Duration` | `double` | Seconds for one full loop. |
| `Orientation` | `KlyOrientation` | Direction of the scrolling: `Horizontal` or `Vertical`. |
| `Overlay` | `bool` | Fades the edges. Vertical marquees need a height from their parent. |
| `PauseOnHover` | `bool` | Pauses while hovered or while something inside has focus. |
| `Repeat` | `int` | Number of aria-hidden copies of the content. One is enough unless the content is narrower than the container. |
| `Reverse` | `bool` | Scrolls the other way. |
| `Ui` | `MarqueeUi?` | Per-instance class overrides of the slots of the marquee, see `MarqueeUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyModal`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Modal dialog on the native dialog element, with a title, a description, a body and a footer. Opening and closing happen in the browser.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name when there is no `KlyModal.Title`. |
| `Body` | `RenderFragment?` | Content of the body of the modal. |
| `ChildContent` | `RenderFragment?` | The trigger: its first button opens the modal. |
| `Close` | `bool` | Shows the close button in the header. |
| `Description` | `string?` | Text under the title of the modal. |
| `Dismissible` | `bool` | Escape and a click on the backdrop close the modal. |
| `Footer` | `RenderFragment?` | Footer actions. Add `data-kly-close` to a button to make it close the modal. |
| `Open` | `bool` | Open state. Use `@bind-Open`: the browser closes the modal on its own (Escape, close button). |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the modal opens or closes, including Escape and a click outside. |
| `Size` | `KlySize` | Size of the modal, from `Xs` to `Xl`; it changes the width of the dialog. |
| `Title` | `string?` | Title of the modal, which also names the dialog. |
| `Ui` | `ModalUi?` | Per-instance class overrides of the slots of the modal, see `ModalUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyNavigationMenu`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Site navigation of links with optional sub-items, horizontal or vertical, with the current page marked.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the navigation landmark. Needed when the page has several. |
| `Color` | `KlyColor` | Semantic color of the navigation menu: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Items` | `IReadOnlyList<KlyNavigationItem>` | The links, with optional sub-items, as `KlyNavigationItem`. |
| `Orientation` | `KlyOrientation` | `Horizontal` opens sub-items in a popover; `Vertical` shows them as collapsible groups. |
| `Ui` | `NavigationMenuUi?` | Per-instance class overrides of the slots of the navigation menu, see `NavigationMenuUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyOverlayHost`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Shows the dialogs opened with `IKlyOverlayService`, each on a `KlyModal` or a `KlySlideover`. `KlyApp` renders one for you.

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPage`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Page layout of up to three columns, with optional left and right columns that stack below the lg breakpoint.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The main column. |
| `Left` | `RenderFragment?` | The left column, typically a `KlyPageAside` holding navigation. |
| `Right` | `RenderFragment?` | The right column, typically a `KlyPageAside` holding a table of contents. |
| `Ui` | `PageUi?` | Per-instance class overrides of the slots of the page, see `PageUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageAnchors`

`Klyroo.UI` · component · inherits `KlyComponentBase`

List of in-page anchor links.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Links` | `IReadOnlyList<PageLinkItem>?` | The anchors: `Href` is typically a fragment such as `#installation`. A fragment is rendered with the path and query of the current page: in a document with a `base` element a bare fragment would resolve against the base address and leave the page. Pass `aria-label` to rename the navigation. |
| `Ui` | `PageAnchorsUi?` | Per-instance class overrides of the slots of the page anchors, see `PageAnchorsUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageAside`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Sticky side column of a page, shown from the lg breakpoint up.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `As` | `string` | The element to render: "aside" (default) or "div", in any case; anything else throws an `ArgumentException`. |
| `Bottom` | `RenderFragment?` | Content at the bottom of the column. |
| `ChildContent` | `RenderFragment?` | Content of the page aside. |
| `Top` | `RenderFragment?` | Content at the top of the column. |
| `Ui` | `PageAsideUi?` | Per-instance class overrides of the slots of the page aside, see `PageAsideUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageBody`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Styles the plain HTML inside it, such as rendered Markdown.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The HTML or Razor content to style, such as rendered Markdown. |
| `Ui` | `PageBodyUi?` | Per-instance class overrides of the slots of the page body, see `PageBodyUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageCard`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Card of a page, with a title, a description, an optional link that covers the whole card, and a spotlight effect.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Content beside (horizontal) or under (vertical) the text. Interactive content would sit under the stretched link: give it `relative z-10`. |
| `Color` | `KlyColor` | Tints the icon, the spotlight and the hover ring. |
| `Description` | `string?` | Secondary text of the page card, shown under or next to its title. |
| `Footer` | `RenderFragment?` | Content at the bottom of the card. |
| `Header` | `RenderFragment?` | Content above the text, an image for example. |
| `HeadingLevel` | `int?` | Level of the title heading, 1 to 6; null renders a paragraph. |
| `Href` | `string?` | Makes the whole card a link, carried by the title. |
| `Icon` | `string?` | Name of a built-in icon (see `KlyIcon`). |
| `Orientation` | `KlyOrientation` | Horizontal puts the text and the content side by side from the lg breakpoint. |
| `Reverse` | `bool` | Shows the content before the text in a horizontal card. |
| `Target` | `string?` | The `target` attribute of the link of the card, such as `_blank`. It applies when `Href` is set. |
| `Title` | `string?` | Title of the page card. |
| `Ui` | `PageCardUi?` | Per-instance class overrides of the slots of the page card, see `PageCardUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `PageCardVariant` | Look of the card: `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageColumns`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Multi-column layout where items of different heights pack without gaps.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The items. Each direct child is kept whole: it is never split across two columns. |
| `Columns` | `int` | Number of columns from the lg breakpoint, 2 to 4: one column on phones, two from sm. |
| `Ui` | `PageColumnsUi?` | Per-instance class overrides of the slots of the page columns, see `PageColumnsUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageCta`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Call to action band with a title, a description and links.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Extra content under the description. |
| `Color` | `KlyColor` | Semantic color of the page cta: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Description` | `string?` | Secondary text of the page cta, shown under or next to its title. |
| `HeadingLevel` | `int` | Level of the title heading, 1 to 6. |
| `Links` | `IReadOnlyList<PageLinkItem>?` | Links rendered as buttons beside (horizontal) or under (vertical) the text. |
| `Orientation` | `KlyOrientation` | Horizontal puts the links beside the text from the lg breakpoint; vertical centers everything. |
| `Reverse` | `bool` | Shows the links before the text in a horizontal band. |
| `Title` | `string?` | Title of the page cta. |
| `Ui` | `PageCtaUi?` | Per-instance class overrides of the slots of the page cta, see `PageCtaUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `KlyVariant` | Look of the band: `Solid`, `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageFeature`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Feature block with an icon, a title and a description, optionally linked.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Extra content under the description. Interactive content would sit under the stretched link: give it `relative z-10`. |
| `Color` | `KlyColor` | Semantic color of the page feature: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Description` | `string?` | Secondary text of the page feature, shown under or next to its title. |
| `HeadingLevel` | `int?` | Renders the title as a heading of this level (1 to 6) instead of a paragraph. |
| `Href` | `string?` | Makes the whole feature a link, carried by the title. |
| `Icon` | `string?` | Name of a built-in icon (see `KlyIcon`). |
| `Orientation` | `KlyOrientation` | Horizontal puts the icon before the text; vertical puts it above. |
| `Target` | `string?` | The `target` attribute of the link of the feature, such as `_blank`. It applies when `Href` is set. |
| `Title` | `string?` | Title of the page feature. |
| `Ui` | `PageFeatureUi?` | Per-instance class overrides of the slots of the page feature, see `PageFeatureUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageGrid`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Responsive grid of page items.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The items of the grid, such as `KlyPageCard` or `KlyPageFeature`. |
| `Columns` | `int` | Number of columns from the lg breakpoint, 2 to 4: one column on phones, two from sm. |
| `Ui` | `PageGridUi?` | Per-instance class overrides of the slots of the page grid, see `PageGridUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageHeader`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Header of a page with a headline, a title, a description and links.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Extra content under the links. |
| `Description` | `string?` | Secondary text of the page header, shown under or next to its title. |
| `HeadingLevel` | `int` | Level of the title heading, 1 to 6: 1 by default, the main heading of the page. A page has one h1: when a `KlyPageHeader` and a `KlyPageHero` share a page, or either sits under another h1, set this to 2 on the secondary one. |
| `Headline` | `string?` | Small label above the title. |
| `Links` | `IReadOnlyList<PageLinkItem>?` | Links rendered as buttons under the description. |
| `Title` | `string?` | Title of the page header. |
| `Ui` | `PageHeaderUi?` | Per-instance class overrides of the slots of the page header, see `PageHeaderUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageHero`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Hero section of a landing page, with a headline, a title, a description, links and an optional media area.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Extra content under the links. |
| `Description` | `string?` | Secondary text of the page hero, shown under or next to its title. |
| `HeadingLevel` | `int` | Level of the title heading, 1 to 6: 1 by default, the main heading of the page. A page has one h1: when a `KlyPageHeader` and a `KlyPageHero` share a page, or either sits under another h1, set this to 2 on the secondary one. |
| `Headline` | `string?` | Short text above the title, such as an announcement or a category. |
| `Links` | `IReadOnlyList<PageLinkItem>?` | Links rendered as buttons under the description. |
| `Media` | `RenderFragment?` | An illustration, a screenshot or a demo shown beside (horizontal) or under (vertical) the text. |
| `Orientation` | `KlyOrientation` | Horizontal puts the text and the media side by side; vertical centers the text above the media. |
| `Reverse` | `bool` | Shows the media before the text in a horizontal hero. |
| `Title` | `string?` | Title of the page hero. |
| `Ui` | `PageHeroUi?` | Per-instance class overrides of the slots of the page hero, see `PageHeroUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageLinks`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Titled list of links, as a navigation landmark.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `HeadingLevel` | `int?` | Renders the title as a heading of this level (1 to 6) instead of a paragraph. |
| `Links` | `IReadOnlyList<PageLinkItem>?` | The links. A link to the current page (or to a page above it, unless `PageLinkItem.Exact`) is marked active. |
| `Title` | `string?` | Heading above the list; it also names the navigation. |
| `Ui` | `PageLinksUi?` | Per-instance class overrides of the slots of the page links, see `PageLinksUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageList`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Styled list whose children are list items.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The items, as `li` elements. |
| `Divide` | `bool` | Draws a line between the items; otherwise they are only spaced. |
| `Ui` | `PageListUi?` | Per-instance class overrides of the slots of the page list, see `PageListUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageLogos`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Logos of customers or partners, as a wrapped list or a scrolling strip.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Custom logos, shown after `KlyPageLogos.Items`. Give each one an accessible name. |
| `Duration` | `double` | Seconds for one loop of the marquee. |
| `HeadingLevel` | `int?` | Level of the title heading, 1 to 6; null renders a paragraph. |
| `ImageOptions` | `KlyImageOptions?` | Image settings forwarded to `KlyImage` for every image logo. |
| `Items` | `IReadOnlyList<PageLogoItem>?` | The logos, each an icon or an image with an accessible name. |
| `Marquee` | `bool` | Scrolls the logos in a loop, see `KlyMarquee`. The scrolling copies are hidden from assistive technology. |
| `Title` | `string?` | Heading above the logos. |
| `Ui` | `PageLogosUi?` | Per-instance class overrides of the slots of the page logos, see `PageLogosUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPageSection`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Section of a landing page, with a headline, a title, a description, a list of features and a media area.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Extra content after the features. |
| `Description` | `string?` | Secondary text of the page section, shown under or next to its title. |
| `FeatureHeadingLevel` | `int?` | Renders the feature titles as headings of this level; they are plain text by default. |
| `Features` | `IReadOnlyList<PageFeatureItem>?` | Features listed under the links, each with an icon, a title and a description. |
| `HeadingLevel` | `int` | Level of the title heading, 1 to 6. |
| `Headline` | `string?` | Short text above the title, such as a category. |
| `Icon` | `string?` | Name of a built-in icon shown above the headline. |
| `Links` | `IReadOnlyList<PageLinkItem>?` | Links rendered as buttons under the description. |
| `Media` | `RenderFragment?` | An illustration or a screenshot shown beside (horizontal) or under (vertical) the text. |
| `Orientation` | `KlyOrientation` | Horizontal puts the text and the media side by side; vertical stacks them and spreads the features on three columns. |
| `Reverse` | `bool` | Shows the media before the text in a horizontal section. |
| `Title` | `string?` | Title of the page section. |
| `Ui` | `PageSectionUi?` | Per-instance class overrides of the slots of the page section, see `PageSectionUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPagination`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Page navigation. With an address template every page is a link, so it works in static server rendering.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the landmark. Defaults to `KlyMessages.Pagination`. |
| `Color` | `KlyColor` | Semantic color of the pagination: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `HrefTemplate` | `string?` | Turns pages into links. `{page}` is replaced by the page number, for example `/items?page={page}`. The owner of the page reads the number from the URL and passes it as `KlyPagination.Page`. |
| `Page` | `int` | Current page, starting at 1. |
| `PageChanged` | `EventCallback<int>` | Raised with the new page number when the user picks a page. Not used when `HrefTemplate` turns the pages into links. |
| `PageSize` | `int` | Number of items on a page. With `Total` it gives the number of pages. Defaults to 10. |
| `ShowControls` | `bool` | Previous and next buttons. |
| `ShowFirstLast` | `bool` | Buttons jumping to the first and the last page. |
| `SiblingCount` | `int` | Pages shown on each side of the current one. |
| `Size` | `KlySize` | Size of the pagination, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Total` | `int` | Total number of items. The page count is `ceil(Total / PageSize)`, at least 1. |
| `Ui` | `PaginationUi?` | Per-instance class overrides of the slots of the pagination, see `PaginationUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPinInput`

`Klyroo.UI` · component · inherits `KlyFieldBase<string>`

One-time code input made of separate cells, with paste support, numeric or alphanumeric.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the group when no `KlyFormField` label is around. |
| `Length` | `int` | Number of boxes. |
| `Mask` | `bool` | Hides the characters, as in a password field. |
| `Type` | `PinInputType` | Characters the cells accept: `Numeric` or `Alphanumeric`. Defaults to numeric. |
| `Ui` | `PinInputUi?` | Per-instance class overrides of the slots of the pin input, see `PinInputUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<string>`.

#### `KlyPopover`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Non-modal floating panel anchored to a trigger. `KlyPopover.ChildContent` is the trigger: its first button (or focusable element) opens and closes the panel. Opening, closing, Escape, light dismiss, positioning and focus run in the browser, so they cost no round trip in Server mode and work in static server rendering once `blazor.web.js` has loaded.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the panel. Defaults to the trigger's text. |
| `ChildContent` | `RenderFragment?` | The trigger. |
| `Content` | `RenderFragment?` | The panel content. |
| `Offset` | `int` | Distance between trigger and panel, in pixels. |
| `Open` | `bool` | Open state. Use `@bind-Open`: the browser can close the panel on its own (Escape, click outside). |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state whenever the popover opens or closes in the browser, Escape and light dismiss included. |
| `Placement` | `string` | Floating UI placement: top, right, bottom, left, optionally suffixed with -start or -end. |
| `Role` | `string?` | Role of the panel. Defaults to `dialog`. Null renders a plain panel, a disclosure as in the WAI-ARIA navigation pattern: the trigger then only reports `aria-expanded` and `aria-controls`, and `KlyPopover.AriaLabel` is not rendered because a role-less element cannot carry a name. |
| `Ui` | `PopoverUi?` | Per-instance class overrides of the slots of the popover, see `PopoverUi`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPricingPlan`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Card of one pricing plan, with its price, features and call to action.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Badge` | `string?` | Text of a badge next to the title, such as "Popular". |
| `BadgeColor` | `KlyColor?` | Color of the badge; the plan's color by default. |
| `BillingCycle` | `string?` | Text after the price: "/month". |
| `ButtonColor` | `KlyColor?` | Color of the call to action button. Defaults to neutral for a solid plan, else to the color of the plan. |
| `ButtonHref` | `string?` | Makes the button a link. |
| `ButtonLabel` | `string?` | Label of the button at the bottom. |
| `ButtonVariant` | `ButtonVariant?` | Look of the call to action button. Defaults to solid for a highlighted or solid plan, else to outline. |
| `Color` | `KlyColor` | Semantic color of the pricing plan: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Description` | `string?` | Secondary text of the pricing plan, shown under or next to its title. |
| `Discount` | `string?` | The discounted price, shown instead of `KlyPricingPlan.Price`. |
| `Features` | `IReadOnlyList<KlyPricingFeature>` | The included features. A string converts to a feature. |
| `Footer` | `RenderFragment?` | Replaces the button. |
| `HeadingLevel` | `int` | Level of the title heading: 3 by default, for plans under a level 2 heading. |
| `Highlight` | `bool` | Adds a ring around the plan. |
| `OnButtonClick` | `EventCallback<MouseEventArgs>` | Click of the button. As for `KlyButton`, it stays disabled until the page is interactive. |
| `Orientation` | `KlyOrientation` | Horizontal lays the plan out in a row on wide screens: the price on the left, the features, then the button. |
| `Price` | `string?` | The price, formatted by the caller ("$29"). With `KlyPricingPlan.Discount` it is the original price, struck through. |
| `Scale` | `bool` | Enlarges the plan on wide screens so it stands out from the others. |
| `Tagline` | `string?` | A value statement above the price. |
| `Terms` | `string?` | Fine print under the price. |
| `Title` | `string?` | The plan name, in a heading. |
| `Ui` | `PricingPlanUi?` | Per-instance class overrides of the slots of the pricing plan, see `PricingPlanUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `KlyVariant` | Look of the plan card: `Solid`, `Outline`, `Soft` or `Subtle`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPricingPlans`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Responsive grid of `KlyPricingPlan` cards.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The `KlyPricingPlan` cards. |
| `Orientation` | `KlyOrientation` | Horizontal places the plans next to each other in a responsive grid, vertical stacks them in one column. |
| `Ui` | `PricingPlansUi?` | Per-instance class overrides of the slots of the pricing plans, see `PricingPlansUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyPricingTable`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Comparison table of pricing tiers and their features. On small screens each tier becomes a card.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Caption` | `string?` | Name of the table for assistive technology. It is not shown. |
| `Color` | `KlyColor` | Semantic color of the accents of the table: a built-in `KlyColor` or the name of a color you defined. |
| `Sections` | `IReadOnlyList<KlyPricingSection>` | The groups of rows. |
| `Sticky` | `bool` | Keeps the tier headers at the top of the viewport while the table scrolls, from the md breakpoint up. The distance from the top is the `--kly-sticky-offset` CSS variable (0 by default), for pages with a fixed header. |
| `Tiers` | `IReadOnlyList<KlyPricingTier>` | The columns. |
| `Ui` | `PricingTableUi?` | Per-instance class overrides of the slots of the pricing table, see `PricingTableUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyProgress`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Progress bar, determinate or indeterminate, with optional steps and a status text.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Animation` | `KlyProgressAnimation` | Motion of the indeterminate bar. It stops, leaving a full faded bar, when the user prefers reduced motion. |
| `AriaLabel` | `string?` | Accessible name of the bar. Defaults to `KlyMessages.Progress`. |
| `Color` | `KlyColor` | Semantic color of the progress: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Max` | `double` | Largest value. Ignored when `KlyProgress.Steps` is set: the maximum is then the index of the last step. |
| `Size` | `KlySize` | Size of the progress, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Status` | `bool` | Shows the percentage above the bar. |
| `Steps` | `IReadOnlyList<string>?` | Labels of the steps shown under the progress bar. `KlyProgress.Value` is the current step's index. |
| `Ui` | `ProgressUi?` | Per-instance class overrides of the slots of the progress, see `ProgressUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `double?` | Current value between 0 and the maximum. Null makes the bar indeterminate (no `aria-valuenow`), for work of unknown duration. With `KlyProgress.Steps` it is the index of the current step. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyRadioGroup<TValue>`

`Klyroo.UI` · component · inherits `KlyFieldBase<TValue>`

Group of radio buttons bound to one value.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Items` | `IReadOnlyList<KlyRadioItem<TValue>>` | The radio buttons, as `KlyRadioItem`. |
| `Legend` | `string?` | Caption of the group, rendered as the legend of its fieldset. |
| `Orientation` | `KlyOrientation` | Whether the radio buttons are stacked (`Vertical`) or side by side (`Horizontal`). |
| `Ui` | `RadioGroupUi?` | Per-instance class overrides of the slots of the radio group, see `RadioGroupUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyFieldBase<TValue>`.

#### `KlyRangeSlider`

`Klyroo.UI` · component · inherits `KlyFieldBase<IReadOnlyList<double>>`

Slider with two thumbs that binds to a list of two numbers.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the group when no `KlyFormField` label is around. |
| `Immediate` | `bool` | Report every move to .NET instead of the final values. Costs a round trip per step in Server mode. |
| `Max` | `double` | Largest value of the track. Defaults to 100. |
| `Min` | `double` | Smallest value of the track. Defaults to 0. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Step` | `double` | Distance between two values the thumbs can take. Defaults to 1. |
| `Ui` | `SliderUi?` | Per-instance class overrides of the slots of the range slider, see `SliderUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyFieldBase<IReadOnlyList<double>>`.

#### `KlyScrollArea`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Styled scroll container that stays reachable with the keyboard while its content overflows.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the scrollable region. |
| `ChildContent` | `RenderFragment?` | Content of the scroll area. |
| `Orientation` | `KlyOrientation?` | Scrolling direction. Null scrolls both ways. |
| `Ui` | `ScrollAreaUi?` | Per-instance class overrides of the slots of the scroll area, see `ScrollAreaUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySelect<TValue>`

`Klyroo.UI` · component · inherits `KlyFieldBase<TValue>`

Select that opens a list of items in a popup, bound to a string, a number, an enum, a `Guid`, a date or any value that identifies an option, with optional icons and sections.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Items` | `IReadOnlyList<IKlySelectItem<TValue>>` | The options, as `KlySelectItem` (`KlySelectItem` for strings). Items with a group are shown in sections. |
| `Placeholder` | `string?` | Hint shown while the field is empty. |
| `Ui` | `SelectUi?` | Per-instance class overrides of the slots of the select, see `SelectUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<TValue>`.

#### `KlySelectMenu<TValue>`

`Klyroo.UI` · component · inherits `KlyListFieldBase<TValue>`

Select with a searchable popup list, single or multiple selection, bound to a list of strings, numbers, enums, `Guid`s or any value that identifies an option, and options that can be fetched from .NET.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Create` | `bool` | Offers the typed text as a new value when no item has that label. A created value is kept in the value list but is not added to `KlySelectMenu.Items`. |
| `Debounce` | `int` | With `KlySelectMenu.OptionsProvider`: the quiet time in milliseconds after typing before the source is searched. |
| `Items` | `IReadOnlyList<IKlySelectItem<TValue>>` | The options. Not needed when `KlySelectMenu.OptionsProvider` loads them; given together with it, these items are not listed but their labels are known (the label of a value the provider has not returned yet). The parameter must be passed when there is no provider, and a null value (`Items="@_list"` while the list is still loading) is an empty list, before and after it loads. |
| `MinLength` | `int` | With `KlySelectMenu.OptionsProvider`: the shortest text that is searched. A shorter, non-empty text shows a hint and lists nothing. |
| `Multiple` | `bool` | Several values can be selected; each one posts under `Name`. Otherwise the list holds at most one value. |
| `OptionsKey` | `string?` | With `KlySelectMenu.OptionsProvider`: what the options depend on besides the search text (the country whose cities are listed). When it changes (compared ordinally), the loaded pages, the search text, the cursor and the resolved labels are forgotten, the selected values are resolved again, and the list loads again the next time it opens. A new delegate with the same key keeps what was loaded: the provider is only asked for what was not loaded yet. Without a key the source is assumed not to change. |
| `OptionsProvider` | `KlyOptionsProvider?` | Loads the options from a remote source, one page at a time, instead of listing `KlySelectMenu.Items`. The filter field searches the source (the browser does not filter): the text is trimmed, sent after `KlySelectMenu.Debounce` milliseconds and only when it has at least `KlySelectMenu.MinLength` characters (an empty text lists the unfiltered options); an older request is cancelled and an old answer never replaces a newer one. The next page loads as the list scrolls, a bit before its end, and by the "Load more" button. While loading the list is `aria-busy` and a polite status says so; when the provider throws, the list shows a translated error with a Retry button (also reached with Enter in the filter field). Interactive only: a statically rendered page (and the prerender before the page becomes interactive) has nobody to call the provider, so the field renders its current values (as their raw values, unless `KlySelectMenu.Items` names them) with a disabled trigger, and still posts them with the form. The provider is first called when the list opens. A parent that renders again with an equal or a new delegate keeps the loaded state; give the component a `@key` to start from scratch. |
| `PageSize` | `int` | With `KlySelectMenu.OptionsProvider`: the number of options requested per page. |
| `Placeholder` | `string?` | Hint shown while the field is empty. |
| `ResolveSelected` | `KlySelectedOptionsResolver?` | With `KlySelectMenu.OptionsProvider`: returns the options of selected values the loaded pages do not contain (values restored from the address or a saved form), so the field shows their labels. Called once for each such value, as soon as the component is interactive. Without it, such values show their raw value. |
| `SearchLabel` | `string?` | Label of the filter field. Defaults to `KlyMessages.SelectMenuSearch`. |
| `Ui` | `SelectMenuUi?` | Per-instance class overrides of the slots of the select menu, see `SelectMenuUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyListFieldBase<TValue>`.

#### `KlySeparator`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Horizontal or vertical divider, with an optional label or icon.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Extra decorative content in the middle, next to the icon and label. |
| `Color` | `KlyColor` | Semantic color of the line: a built-in `KlyColor` or the name of a color you defined. |
| `Icon` | `RenderFragment?` | Icon of the separator, as markup such as a `KlyIcon`. |
| `Label` | `string?` | Text in the middle of the line. It also names the separator for assistive technology. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Size` | `KlySize` | Thickness of the line. |
| `Type` | `KlySeparatorType` | Line style: `Solid`, `Dashed` or `Dotted`. |
| `Ui` | `SeparatorUi?` | Per-instance class overrides of the slots of the separator, see `SeparatorUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySidebar`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Application sidebar that collapses to icons and resizes from the lg breakpoint up, and becomes an off-canvas panel below it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the sidebar landmark. Defaults to the built-in text. |
| `As` | `string` | The element to render, "aside" by default. A complementary landmark must not sit inside another landmark: use "div" when the sidebar is placed inside a main element. |
| `ChildContent` | `RenderFragment?` | The scrollable middle: navigation, search. It is rendered once and serves both the column and the off-canvas panel. |
| `Collapsed` | `bool` | The collapsed state when nothing is remembered. Use `@bind-Collapsed` to follow and set it from .NET; the browser still toggles it without a round trip, and in static rendering a bound state is not reported. |
| `CollapsedChanged` | `EventCallback<bool>` | Raised with the new state when the sidebar collapses to icons or expands. |
| `Collapsible` | `bool` | Shows the button that collapses the sidebar to icons. |
| `Footer` | `RenderFragment?` | Bottom of the sidebar: the signed-in user, settings. |
| `Header` | `RenderFragment?` | Top of the sidebar: a logo and a title. Hidden while the sidebar is collapsed, next to the collapse button. |
| `Id` | `string?` | Name under which the collapsed state and the width are remembered, in the cookies `klyroo-sidebar-{id}` ("1" when collapsed) and `klyroo-sidebar-{id}-width`, written by the browser. Without an id nothing is remembered. An app served below a path base (`UsePathBase("/x")`) shares its origin with other apps, so its cookies are scoped to the base path and named `klyroo-sidebar.{slug}~~{id}` (see `KlyPathScope.SidebarCookieName`): a server keeps the last of two cookies of the same name, and another app's would otherwise win. At the root nothing changes. With a bound `KlySidebar.Collapsed` the collapsed state is not restored: your state wins. A small inline script placed right after the sidebar applies the remembered state while the page is parsed, before the first paint, so a reload neither shifts nor animates (give it `KlySidebar.ScriptNonce` under a strict Content-Security-Policy). Reading the cookies on the server and passing `KlySidebar.Collapsed` also works: get the cookie name from `KlyPathScope.SidebarCookieName` with `Request.PathBase`, so the same code is right at the root and below a path base. |
| `MaxWidth` | `int` | Largest width the user can resize the sidebar to, in pixels. Defaults to 448. |
| `MinWidth` | `int` | Smallest width the user can resize the sidebar to, in pixels. Defaults to 192. |
| `MobileTitle` | `string?` | Title of the mobile panel. Defaults to the built-in text. |
| `MobileTrigger` | `bool` | Shows the button that opens the off-canvas panel. Turn it off when another button opens the panel instead, such as `KlyDashboardSidebarToggle`, which finds the sidebar by itself. |
| `Resizable` | `bool` | Adds a handle on the edge to resize the sidebar with the mouse or the arrow keys, Home and End. |
| `ScriptNonce` | `string?` | Nonce for the small inline script that applies the remembered state before the first paint, when a Content-Security-Policy forbids inline scripts without one. |
| `Ui` | `SidebarUi?` | Per-instance class overrides of the slots of the sidebar, see `SidebarUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Width` | `int` | Width in pixels when expanded and not resized yet. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySkeleton`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Placeholder that shows where content is loading. It is hidden from assistive technology.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Ui` | `SkeletonUi?` | Per-instance class overrides of the slots of the skeleton, see `SkeletonUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySkipLink`

`Klyroo.UI` · component · inherits `KlyComponentBase`

First stop of the page for keyboard users: a link to the main content, visible when focused.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Replaces `KlySkipLink.Label`. |
| `Label` | `string?` | The text of the link. Defaults to the translated "Skip to content". |
| `Target` | `string` | The id of the element that receives the focus, "main" by default. It gets `tabindex="-1"` when it has none. |
| `Ui` | `SkipLinkUi?` | Per-instance class overrides of the slots of the skip link, see `SkipLinkUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySlideover`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Full-height side panel in a modal dialog, with a title, a description, a body and a footer.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name when there is no `KlySlideover.Title`. |
| `Body` | `RenderFragment?` | Content of the body of the slideover. |
| `ChildContent` | `RenderFragment?` | The trigger: its first button opens the panel. |
| `Close` | `bool` | Shows the close button in the header. |
| `Description` | `string?` | Text under the title of the slideover. |
| `Dismissible` | `bool` | Escape and a click on the backdrop close the panel. |
| `Footer` | `RenderFragment?` | Footer actions. Add `data-kly-close` to a button to make it close the panel. |
| `Inset` | `bool` | Floats the panel away from the edges of the screen. |
| `Open` | `bool` | Open state. Use `@bind-Open`: the browser closes the panel on its own (Escape, backdrop, close button). |
| `OpenChanged` | `EventCallback<bool>` | Raised with the new state when the slideover opens or closes, including Escape and a click outside. |
| `Side` | `SlideoverSide` | The edge the panel slides in from. |
| `Title` | `string?` | Title of the slideover, which also names the dialog. |
| `Ui` | `SlideoverUi?` | Per-instance class overrides of the slots of the slideover, see `SlideoverUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySlider<TNumber>`

`Klyroo.UI` · component · inherits `KlyFieldBase<TNumber>`

Slider with one thumb, bound to a number of any built-in numeric type (`double`, `int`, `decimal`, `float`...) or its nullable form. A nullable slider with no value rests on its minimum; moving it sets a value.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name when no `KlyFormField` label is around. |
| `Immediate` | `bool` | Report every move to .NET instead of the final value. Costs a round trip per step in Server mode. |
| `Max` | `double` | Largest value of the track. Defaults to 100. |
| `Min` | `double` | Smallest value of the track. Defaults to 0. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Step` | `double` | Distance between two values the thumb can take. Defaults to 1. |
| `Ui` | `SliderUi?` | Per-instance class overrides of the slots of the slider, see `SliderUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyFieldBase<TNumber>`.

#### `KlySplitter`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Resizable panels separated by focusable handles.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the splitter: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Orientation` | `KlyOrientation` | Horizontal places the panels side by side (the handles are vertical); Vertical stacks them. |
| `Panels` | `IReadOnlyList<KlySplitterPanel>` | The panels, in order, as `KlySplitterPanel`. A handle sits between two neighbors. |
| `Sizes` | `IReadOnlyList<double>?` | Sizes in percent, one per panel. Bind it to follow resizing; unbound, resizing stays in the browser. |
| `SizesChanged` | `EventCallback<IReadOnlyList<double>>` | Raised with the size of each panel, as weights in percent, once a resize gesture ends. |
| `Step` | `double` | Size change of an arrow key press, in percent. |
| `Ui` | `SplitterUi?` | Per-instance class overrides of the slots of the splitter, see `SplitterUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyStepper`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Sequence of steps with the current one marked, which can be static or interactive.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the list of steps. |
| `Color` | `KlyColor` | Semantic color of the stepper: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Current` | `int` | Index of the current step, starting at 0. Earlier steps are completed. |
| `CurrentChanged` | `EventCallback<int>` | Bind it to let users click a step; without it the stepper only displays progress. |
| `Items` | `IReadOnlyList<KlyStepperItem>` | The steps, as `KlyStepperItem`. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Size` | `KlySize` | Size of the stepper, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `StepperUi?` | Per-instance class overrides of the slots of the stepper, see `StepperUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySwitch`

`Klyroo.UI` · component · inherits `KlyFieldBase<bool>`

On/off switch, a native checkbox with `role="switch"`. Its look follows the native checked state, so it is correct in static server rendering and after a form post.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Description` | `string?` | Text under the label of the switch. |
| `Label` | `string?` | Text next to the switch. A `KlyFormField` label also names it. |
| `Ui` | `SwitchUi?` | Per-instance class overrides of the slots of the switch, see `SwitchUi`. |

Also accepts the parameters of `KlyFieldBase<bool>`.

#### `KlyTable<TItem>`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Table of items whose columns are declared with `KlyColumn` and `KlyTemplateColumn` children, with optional sorting and custom cells.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Caption` | `string?` | Describes the table for assistive technologies (visually hidden). |
| `ChildContent` | `RenderFragment?` | The columns: `KlyColumn` and `KlyTemplateColumn` components. |
| `Empty` | `RenderFragment?` | Shown when there are no items. |
| `Items` | `IEnumerable<TItem>?` | The rows to show. The columns come from the `KlyColumn` and `KlyTemplateColumn` children. |
| `Sticky` | `bool` | Keeps the header visible while the table scrolls (give the table a max height through Class). |
| `Ui` | `TableUi?` | Per-instance class overrides of the slots of the table, see `TableUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyTabs`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Tabs that switch between content panels, following the WAI-ARIA tabs pattern. Selection happens in the browser.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the tab list. |
| `Color` | `KlyColor` | Semantic color of the tabs: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Items` | `IReadOnlyList<KlyTabItem>` | The tabs, as `KlyTabItem`. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Ui` | `TabsUi?` | Per-instance class overrides of the slots of the tabs, see `TabsUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `string?` | Value of the selected tab. Defaults to the first enabled one. |
| `ValueChanged` | `EventCallback<string?>` | Raised with the value of the selected tab when the user picks another one. |
| `Variant` | `TabsVariant` | Look of the tabs: `Pill` or `Link`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyTemplateColumn<TItem>`

`Klyroo.UI` · component · inherits `KlyColumnBase<TItem>`

A column of `KlyTable` whose cells are markup built from the item. It cannot be sorted: use `KlyColumn` for that.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Template` | `RenderFragment<TItem>` | The content of the cell of an item. |

Also accepts the parameters of `KlyColumnBase<TItem>`.

#### `KlyTextarea`

`Klyroo.UI` · component · inherits `KlyFieldBase<string>`

Multi-line text input that binds to a string and can resize itself to its content.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AutoResize` | `bool` | Grow with the content (CSS field-sizing, where the browser supports it). |
| `Immediate` | `bool` | Update the bound value on every keystroke instead of on change. Costs a round trip per key in Server mode. |
| `Placeholder` | `string?` | Hint shown while the field is empty. |
| `Rows` | `int` | Number of visible lines when the field is empty. Defaults to 3. |
| `Ui` | `TextareaUi?` | Per-instance class overrides of the slots of the textarea, see `TextareaUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `FieldVariant` | Look of the field: `Outline`, `Soft`, `Subtle`, `Ghost` or `None`. |

Also accepts the parameters of `KlyFieldBase<string>`.

#### `KlyTheme`

`Klyroo.UI` · component · inherits `ComponentBase`

Cascades class overrides to every component of a given type below it, merged after the theme and before each instance.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The content whose components receive the overrides. |
| `For` | `Type?` | The component type the `KlyTheme.Ui` applies to: `typeof(KlyButton)`, or `typeof(KlyTable<>)` for a generic one. |
| `Overrides` | `IReadOnlyDictionary<Type, ISlotOverrides>?` | Overrides for several component types at once. They win over `KlyTheme.For` and `KlyTheme.Ui` on a clash. |
| `Ui` | `ISlotOverrides?` | The overrides for `KlyTheme.For`: any `*Ui` record, such as `new ButtonUi { Base = "font-bold" }`. Slots the component does not have are ignored. |

#### `KlyThemeStyle`

`Klyroo.UI` · component · inherits `ComponentBase`

Place in `<head>`, next to `KlyColorModeScript` and before your own stylesheet. Renders one `<style>` with the `:root` and `.dark` variables of the palettes configured in `AddKlyrooUi(o => o.Colors...)`, and a `[data-color]` rule for every extra semantic color, so `Color="@("tertiary")"` works without hand-written CSS. It renders nothing when the colors are the defaults, which `klyroo.css` already contains.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Colors` | `KlyColorOptions?` | Colors to render instead of the ones of `AddKlyrooUi`, for a preview. They become read-only. Components only follow them under `KlyThemeStyle.Scope`. |
| `Nonce` | `string?` | Nonce for a Content-Security-Policy that forbids inline styles without one. |
| `Scope` | `string?` | A simple selector (`#preview`, `.demo`) that receives the variables instead of `:root`: the elements inside it get these colors, in light and dark mode. Every token is written for the scope, even with the default colors, so the preview looks the same inside a page that configured other colors. |

#### `KlyTimeline`

`Klyroo.UI` · component · inherits `KlyComponentBase`

List of events along a line, with the current one highlighted.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Active` | `int?` | Index of the last highlighted item. Null highlights none. |
| `AriaLabel` | `string?` | Accessible name of the list. |
| `Color` | `KlyColor` | Semantic color of the timeline: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Items` | `IReadOnlyList<KlyTimelineItem>` | The events, as `KlyTimelineItem`. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Size` | `KlySize` | Size of the timeline, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Ui` | `TimelineUi?` | Per-instance class overrides of the slots of the timeline, see `TimelineUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyToaster`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Container that shows the toasts added through `IToastService`. `KlyApp` renders one for you.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Duration` | `TimeSpan` | How long a toast stays on screen when it does not set its own duration. Defaults to 5 seconds. |
| `Ui` | `ToastUi?` | Per-instance class overrides of the slots of the toaster, see `ToastUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyToggle`

`Klyroo.UI` · component · inherits `KlyFieldBase<bool>`

Button that toggles between pressed and not pressed, bound to a `bool`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Content of the button. |
| `Icon` | `RenderFragment?` | Icon of the button, as markup such as a `KlyIcon`. |
| `Label` | `string?` | Text of the button. `KlyToggle.ChildContent` wins when both are given. |
| `Ui` | `ToggleUi?` | Per-instance class overrides of the slots of the toggle, see `ToggleUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `ToggleVariant` | Look of the toggle: `Outline`, `Soft` or `Ghost`. |

Also accepts the parameters of `KlyFieldBase<bool>`.

#### `KlyToggleGroup`

`Klyroo.UI` · component · inherits `KlyListFieldBase`

Group of toggle buttons with single or multiple selection.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the group when no `KlyFormField` label is around. |
| `Items` | `IReadOnlyList<KlyToggleItem>` | The buttons, as `KlyToggleItem`. |
| `Multiple` | `bool` | Several items can be pressed at once; otherwise pressing an item releases the others. |
| `Orientation` | `KlyOrientation` | Direction of the layout: `Horizontal` or `Vertical`. |
| `Ui` | `ToggleGroupUi?` | Per-instance class overrides of the slots of the toggle group, see `ToggleGroupUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Variant` | `ToggleVariant` | Look of the toggle: `Outline`, `Soft` or `Ghost`. |

Also accepts the parameters of `KlyListFieldBase`.

#### `KlyTooltip`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Text label that appears when its trigger is hovered or focused. It is handled in the browser.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The trigger, a focusable element. |
| `Content` | `RenderFragment?` | Content of the tooltip, instead of `Text`. |
| `Delay` | `int` | Hover delay before showing, in milliseconds. Keyboard focus shows it at once. |
| `Placement` | `string` | Floating UI placement of the tooltip: top, right, bottom or left, optionally with `-start` or `-end`. Defaults to `top`. |
| `Text` | `string?` | Text of the tooltip. Use `Content` for markup. |
| `Ui` | `TooltipUi?` | Per-instance class overrides of the slots of the tooltip, see `TooltipUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyTree`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Tree view with expandable nodes, selection and arrow key navigation.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` | `string?` | Accessible name of the tree. |
| `Color` | `KlyColor` | Semantic color of the tree: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else the default of the component. |
| `Items` | `IReadOnlyList<KlyTreeItem>` | The nodes of the tree, as `KlyTreeItem`. |
| `Ui` | `TreeUi?` | Per-instance class overrides of the slots of the tree, see `TreeUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |
| `Value` | `string?` | Value of the selected item. Selection also works unbound: the browser keeps it. |
| `ValueChanged` | `EventCallback<string?>` | Raised with the value of the selected node when the user selects one. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyUser`

`Klyroo.UI` · component · inherits `KlyComponentBase`

Avatar with a name and a description, optionally linked.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AvatarIcon` | `RenderFragment?` | Icon of the avatar when there is no picture, as markup such as a `KlyIcon`. |
| `AvatarSrc` | `string?` | Address of the avatar picture. |
| `Description` | `string?` | Text under the name, such as a role or an email address. |
| `Href` | `string?` | Makes the whole user a link. |
| `Name` | `string?` | Name of the person or entity. |
| `Orientation` | `KlyOrientation` | Whether the name sits beside the avatar (`Horizontal`) or under it (`Vertical`). |
| `ShowAvatar` | `bool` | Shows the avatar: the image, else the initials of `KlyUser.Name`. |
| `Size` | `KlySize` | Size of the user, from `Xs` to `Xl`. When not set, `KlyDefaults.Size` applies, else the default of the component. |
| `Target` | `string?` | The `target` attribute of the link, such as `_blank`. It applies when `Href` is set. |
| `Ui` | `UserUi?` | Per-instance class overrides of the slots of the user, see `UserUi`. They are merged after the theme and the `Class` parameter wins on the root slot. |

Also accepts the parameters of `KlyComponentBase`.

#### `Defer`

`Klyroo.UI.Internal` · component · inherits `ComponentBase`

Renders its content after its earlier siblings, so child components can register first (table columns).

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The content to render. |

#### `EditBridge`

`Klyroo.UI.Internal` · component · inherits `InputBase<string>`

Infrastructure of Klyroo form controls; not meant to be used directly. Lets a control with custom markup take part in an `EditForm` like a built-in input: `InputBase` computes the posted field name and re-renders on validation changes; the control renders its own markup.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment<EditBinding>` | The content, given the binding of the field. |

Also accepts the parameters of `InputBase<string>`.

#### `FieldBridge<TValue>`

`Klyroo.UI.Internal` · component · inherits `InputBase<TValue>`

Infrastructure of Klyroo form controls; not meant to be used directly. Same as `EditBridge` for controls whose bound value is not a string (dates, times).

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment<EditBinding>` | The content, given the binding of the field. |

Also accepts the parameters of `InputBase<TValue>`.

#### `Heading`

`Klyroo.UI.Internal` · component · inherits `ComponentBase`

Renders h1…h6 from a number.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Content of the heading. |
| `Class` | `string?` | Classes of the heading element. |
| `Level` | `int` | Heading level, 1 to 6. Defaults to 2. |

#### `MessageSummary`

`Klyroo.UI.Internal` · component · inherits `ComponentBase`

Infrastructure of `KlyForm`; not meant to be used directly. Lists every validation message of an edit context and re-renders when validation changes.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Class` | `string?` | Classes of the list. |
| `For` | `EditContext` | The edit context whose validation messages are listed. |

#### `SectionHeading`

`Klyroo.UI.Internal` · component · inherits `ComponentBase`

Internal building block, not part of the supported API. A heading of a given level (1 to 6) that can carry an id, so a region can be named by it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Content of the heading. |
| `Class` | `string?` | Classes of the heading element. |
| `Id` | `string?` | The `id` of the heading element. |
| `Level` | `int` | Heading level, 1 to 6. Defaults to 2. |

#### `ValueBridge<TValue>`

`Klyroo.UI.Internal` · component · inherits `InputBase<TValue>`

Infrastructure of Klyroo form controls; not meant to be used directly. `EditBridge` for values that are not strings (lists, numbers, booleans).

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment<EditBinding>` | The content, given the binding of the field. |

Also accepts the parameters of `InputBase<TValue>`.

#### `KlyComponentBase`

`Klyroo.UI.Theming` · component · inherits `ComponentBase`

Base of styled components: injects the theme services and resolves slot classes in the documented order (theme defaults, `AddKlyrooUi` overrides, enclosing `KlyTheme` scopes, `Ui`, then `Class` on the root slot).

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | Unmatched attributes, forwarded to the main element of the component. |
| `Class` | `string?` | Classes added to the root element, merged last so they win over the theme. |

#### `KlyFieldBase<TValue>`

`Klyroo.UI.Theming` · component · inherits `KlyComponentBase`

Base of form controls. Inside an `EditForm` with `@bind-Value`, a control takes part in validation and posts under the bound field's name; elsewhere it is a plain bindable input named by `KlyFieldBase.Name`. Inside a `KlyFormField`, it takes the field's id and description.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Color` | `KlyColor` | Semantic color of the control: a built-in `KlyColor` or the name of a color you defined. When not set, `KlyDefaults.Color` applies, else primary. |
| `Disabled` | `bool` | Disables the control: it cannot be focused or changed and is not submitted. |
| `Id` | `string?` | The `id` of the native control. Defaults to the one of the enclosing `KlyFormField`, else a generated one. |
| `Name` | `string?` | Form field name outside an `EditForm`. Inside one, the name comes from the bound expression. |
| `Required` | `bool` | Marks the control as required, for the browser and for assistive technology. |
| `Size` | `KlySize?` | Defaults to the size of the enclosing `KlyFormField`, or medium. |
| `Value` | `TValue` | The value of the control. Bind it with `@bind-Value`. |
| `ValueChanged` | `EventCallback<TValue>` | Raised with the new value when the user changes it. |
| `ValueExpression` | `Expression<Func<TValue>>?` | The expression of the bound field. The compiler sets it with `@bind-Value`, and validation uses it to find the field. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyListFieldBase`

`Klyroo.UI.Theming` · component · inherits `KlyListFieldBase<string>`

Base of form controls whose value is a list of strings kept by a custom element: the element reports changes with `kly-change` (the values joined by a separator), the component renders the list back in `data-values`. Unbound, no handler is attached and the browser keeps the state.

Also accepts the parameters of `KlyListFieldBase<string>`.

#### `KlyListFieldBase<TValue>`

`Klyroo.UI.Theming` · component · inherits `KlyFieldBase<IReadOnlyList<TValue>>`

Base of form controls whose value is a list kept by a custom element: the element reports changes with `kly-change` (the invariant text of the values joined by a separator), the component renders the list back in `data-values`. Unbound, no handler is attached and the browser keeps the state.

Also accepts the parameters of `KlyFieldBase<IReadOnlyList<TValue>>`.

### Static classes and extensions

#### `EventHandlers`

`Klyroo.UI` · service

Custom events, registered on the browser side by `Klyroo.UI.lib.module.js`.

#### `KlyCultureStoreExtensions`

`Klyroo.UI` · service

Extension methods for `IKlyCultureStore`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static ApplyStoredCultureAsync(IServiceProvider services, IEnumerable<string>? supportedCultures = null)` | `Task<CultureInfo?>` | Applies the culture of `IKlyCultureStore` to the process (`CultureInfo.DefaultThreadCurrentCulture` and the current thread), for a Blazor Hybrid app that wants it before the first render. `KlyApp` does it by itself in a web view, so this is only for an app that reads the store from its own startup code. Returns the culture applied, or null when the store is empty or holds a culture that is not supported. |

#### `KlyDates`

`Klyroo.UI` · service

Dates written the way the content components of Klyroo write them (`KlyBlogPost`, `KlyChangelogVersion`), for pages that sit next to them and must agree with them: the long date of the culture without the day name, and the machine readable value of a `time` element.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Format(DateTimeOffset date, string? format = null, CultureInfo? culture = null)` | `string` | Formats `date` with `format`, or, without one, with the long date pattern of the culture minus the day name ("March 4, 2026" in en-US, "4 mars 2026" in fr-FR, "4 марта 2026 г." in ru-RU: the order of the culture, not the English one). Without `culture` it uses `KlyDates.FormatCulture`, which knows only the built-in locales: inside a component that follows `KlyrooOptions.Locale` or a cascading `KlyLocale`, pass the culture from `KlyDates.FormatCulture` or use `KlyDates.FormatFor`. |
| `static FormatCulture()` | `CultureInfo` | The same as `KlyDates.FormatCulture`, for the built-in locale that matches the current UI culture. It cannot see the options of the app or a cascading locale (a static member has no services): where those are set, use the overload that takes them, or the dates differ from the ones of the components. |
| `static FormatCulture(KlyLocale locale)` | `CultureInfo` | The culture that formats dates for `locale`: the regional `CultureInfo.CurrentCulture` when its language is the one of the locale (an "en-GB" visitor with the English locale keeps 4 March 2026), otherwise the culture of the locale. This is the rule of every component; they pass the locale in effect (the cascading one, then `KlyrooOptions.Locale`, then the one of the UI culture). |
| `static FormatCulture(KlyrooOptions options)` | `CultureInfo` | The same as `KlyDates.FormatCulture`, for the locale `options` resolve from the current UI culture (`KlyrooOptions.Locale` and `Locales` included). |
| `static FormatFor(DateTimeOffset date, KlyLocale locale, string? format = null)` | `string` | Formats `date` as the components do for `locale`: with `KlyDates.FormatCulture`. Named apart from `Format` so no call is ambiguous. |
| `static Iso(DateTimeOffset date)` | `string` | The value of the `datetime` attribute of a `time` element: `yyyy-MM-dd`, the same in every culture. |

#### `KlyIcons`

`Klyroo.UI` · service

Icons as render fragments, for the parameters that take one (`LeadingIcon`, `Icon`...). Size them with classes.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Named(string name, string? class = null)` | `RenderFragment` | An icon by name, in any form `KlyIcon` accepts, drawn in `currentColor`. It finds the Lucide icons built into the library and the icons bundled by the app's build; icons registered on `KlyrooOptions.Icons` are only found by `KlyIcon`. An unknown name renders an empty icon. |

#### `KlyLocales`

`Klyroo.UI` · service

The built-in locales (one property each, defined in `Localization/Locales`), `KlyLocales.All`, and the helpers to derive and resolve locales.

| Property | Type | Description |
| :--- | :--- | :--- |
| `static All` | `IReadOnlyList<KlyLocale>` | Every built-in locale: English, French, German, Spanish, Italian, Portuguese, Brazilian Portuguese, Dutch, Japanese, Simplified Chinese, Arabic (right to left) and Russian. It is the default of `KlyrooOptions.Locales`. |
| `static Ar` | `KlyLocale` | العربية (ar). |
| `static De` | `KlyLocale` | Deutsch (de). |
| `static En` | `KlyLocale` | English. |
| `static Es` | `KlyLocale` | Español (es). |
| `static Fr` | `KlyLocale` | Français (fr). |
| `static It` | `KlyLocale` | Italiano (it). |
| `static Ja` | `KlyLocale` | 日本語 (ja). |
| `static Nl` | `KlyLocale` | Nederlands (nl). |
| `static Pt` | `KlyLocale` | Português (pt). |
| `static PtBr` | `KlyLocale` | Português (Brasil) (pt-BR). |
| `static ReviewedCodes` | `IReadOnlyList<string>` | The codes whose texts were reviewed by a native speaker. The other locales hold drafts, at least the messages that a comment in their file marks as "Draft translations" (the messages added since the first review: the `RemoteOptions` ones), until a native speaker reviews them. |
| `static Ru` | `KlyLocale` | Русский (ru). |
| `static ZhCn` | `KlyLocale` | 简体中文, code zh-Hans: zh-CN, zh-SG and zh-Hans-* reach it through their parent culture, like on the server. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Extend(KlyLocale source, string? code = null, string? name = null, KlyDirection? dir = null, Func<KlyMessages, KlyMessages>? messages = null)` | `KlyLocale` | Derives a locale from another one: everything you do not override is kept. |
| `static Resolve(CultureInfo? culture = null, IReadOnlyList<KlyLocale>? locales = null)` | `KlyLocale` | Picks the locale for a culture the way `UseRequestLocalization` picks a supported culture, so the server and the WebAssembly client agree: the exact code ("pt-BR"), then its parent cultures ("pt-PT" gets "pt", "zh-Hans-CN" tries "zh-Hans" then "zh"), then English. There is no "same language" step: "zh-TW" gets English unless the list holds "zh-TW" or "zh". |
| `static ResolveCode(string code, IEnumerable<string> available)` | `string` | Picks one of `available` for a locale code with the same rules as `KlyLocales.Resolve`, so a package with its own message tables (keyed by locale code) follows the locale of the app: pass `KlyLocale.Code`. The exact code ("pt-BR"), then its parent cultures ("pt-PT" gets "pt", "zh-CN" and "zh-Hans-CN" get "zh-Hans" then "zh"), then "en", then the first available code. Codes compare case-insensitively and the result is the spelling found in `available`. |

#### `KlyPathScope`

`Klyroo.UI` · service

What an app remembers in the browser, scoped to the path base it is served under (`UsePathBase("/x")` with `<base href="/x/">`). Apps on one origin share cookies and localStorage: a cookie is scoped by its path and by its name (a server keeps the last of two cookies of the same name, and the one of another app can come after), a storage key by a prefix. An app at the root keeps the plain names, so nothing changes for it. The browser side of the same rules is `js/base-path.js`. The path base is compared as written (case-sensitive), so use the same casing in `UsePathBase` and in `<base href>`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static CookieName(string name, string? pathBase = null)` | `string` | The name of a cookie that an app keeps under `name` (`klyroo-timezone`): the name itself at the root and `{name}.{slug}~~` below a path base, the same rule as `KlyPathScope.SidebarCookieName` without an id. The cookie path is the path base, "/" at the root. |
| `static SidebarCookieName(string id, string? pathBase = null)` | `string` | The name of the cookie that `KlySidebar` writes for `id`: `klyroo-sidebar-{id}` at the root and `klyroo-sidebar.{slug}~~{id}` below a path base. The width is kept under the same name followed by `-width`. Read the cookie on the server with it: `Request.Cookies[KlyPathScope.SidebarCookieName("app", Request.PathBase)]`. |

#### `KlyrooCulture`

`Klyroo.UI` · service

Helpers to give the WebAssembly runtime the culture the server chose.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static ApplyCultureCookieAsync(IServiceProvider services, string cookieName = ".AspNetCore.Culture")` | `Task` | Reads only the culture cookie and ignores the `lang` attribute and the culture meta element. Obsolete: use `KlyrooCulture.ApplyServerCultureAsync`. |
| `static ApplyServerCultureAsync(IServiceProvider services, IEnumerable<string>? supportedCultures = null, string cookieName = ".AspNetCore.Culture")` | `Task` | Gives a WebAssembly client the culture the server rendered with, instead of the language of the browser, so the prerender and the client pick the same `KlyLocale` (and format dates and numbers the same way) for the same user. The client has no request, so it reads what the server left in the page, in this order: 1. the `klyroo-culture` meta element rendered by `KlyCultureMeta` in `<head>`: the exact culture and UI culture of the request, whatever provider chose them (query string, cookie or `Accept-Language`); 2. the ASP.NET Core culture cookie (`c=fr\|uic=fr`, written by `KlyLocaleSelect` and read by `UseRequestLocalization`); 3. the `lang` attribute of the `html` element, which the server wrote from the locale it resolved (`<html lang="@KlyLocales.Resolve().Code">`), so a culture the server picked from `Accept-Language` reaches the client even without the meta element. Render `KlyCultureMeta` to get the regional culture too ("en-GB" for dates and clocks); the cookie and the attribute only carry what they hold. Call it in the `Program.cs` of the client, after `Build()` and before `RunAsync()`. Nothing changes when none of them is present. |

#### `KlyrooHost`

`Klyroo.UI` · service

Where the components run. A Blazor Hybrid app (`BlazorWebView` in .NET MAUI, WPF or Windows Forms) renders in a local web view: it is always interactive, has no prerendering, no `HttpContext` and no server to read cookies or to render the page head. The components that depend on a server degrade to what a web view can do (browser storage instead of cookies, no culture meta element, no inline script in the head); see docs/platforms.md in the repository.

| Field | Type | Description |
| :--- | :--- | :--- |
| `const WebViewRendererName = "WebView"` | `string` | The `RendererInfo.Name` of a `BlazorWebView`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static IsHybrid(RendererInfo renderer)` | `bool` | True when `renderer` is a Blazor Hybrid web view. In a component, read `RendererInfo`. |

#### `KlyrooIconBundle`

`Klyroo.UI` · service

The icons an app bundles at build time. The code the Klyroo.UI build targets generate calls `KlyrooIconBundle.Add` from a module initializer; apps do not use it directly (see `KlyrooOptions.Icons.Register` to add an icon in code).

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Add(string prefix, string name, string body, string viewBox)` | `void` | Adds an icon: `body` is the markup inside the `<svg>` element, `viewBox` its drawing area. |

#### `KlyrooServiceCollectionExtensions`

`Klyroo.UI` · service

Registers Klyroo.UI in an `IServiceCollection`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooUi(IServiceCollection services, Action<KlyrooOptions>? configure = null)` | `IServiceCollection` | Registers Klyroo.UI. Call it in the server `Program.cs` and in the client one with the same configuration, ideally through a shared method in the client project. |

#### `KlySelectItems`

`Klyroo.UI` · service

Builds the options of `KlySelect`, `KlySelectMenu`, `KlyInputMenu` and `KlyListbox` from an enum or from the rows of a list (the entities of a foreign key). None of it uses reflection beyond `Enum.GetValues``, so it is safe with trimming and native AOT.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AsNullable<TValue>(IEnumerable<KlySelectItem<TValue>> items)` | `IReadOnlyList<KlySelectItem<TValue?>>` | The same options for a nullable property: a control bound to an `int?` takes `KlySelectItem<int?>`, and a list of `KlySelectItem<int>` is not one (a generic class is not covariant for value types). Disabled, count, icon and group are kept. |
| `static From<TSource, TValue>(IEnumerable<TSource> source, Func<TSource, TValue> value, Func<TSource, string> label)` | `IReadOnlyList<KlySelectItem<TValue>>` | One option per row of a list, such as the rows of a lookup table: the key of the row is the value, a column is the label. |
| `static FromEnum<TEnum>(Func<TEnum, string>? label = null)` | `IReadOnlyList<KlySelectItem<TEnum>>` | One option per distinct value of an enum, in the order of its values, so `@bind-Value` binds to the enum property itself. Members that share a value (aliases) are one option, labeled by the first of their names. |

### Classes

#### `KlyChangeEventArgs`

`Klyroo.UI` · class · inherits `EventArgs`

Payload of the `kly-change` browser event raised by Klyroo custom elements when their value changes.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Sequence` (get, set) | `int` | Increments with every toggle of one element. Rendered back so the element can ignore a render that was computed before .NET saw its latest toggle. |
| `Value` (get, set) | `string?` | The new value of the element, as text. |

#### `KlyColorOptions`

`Klyroo.UI` · class

Maps the semantic colors to palettes: `AddKlyrooUi(o => { o.Colors.Primary = KlyPalette.Blue; o.Colors.Neutral = KlyPalette.Zinc; o.Colors.Add("tertiary", KlyPalette.Indigo); })`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `AllowLowContrast` (get, set) | `bool` | What happens when a palette cannot meet the contrast rules (a flat palette, a neutral whose shades are too close to each other…). False, the default, throws an `InvalidOperationException` naming the color, the palette and the rule the first time the stylesheet is resolved; true keeps the best combination found, which may be below the ratios. |
| `static BuiltInNames` | `IReadOnlyList<string>` | The names of the built-in semantic colors. |
| `Error` (get, set) | `KlyPalette` | Palette of the error color. |
| `Extra` | `IReadOnlyDictionary<string, KlyPalette>` | Extra semantic colors added with `KlyColorOptions.Add`, usable as `Color="@("tertiary")"`. |
| `Info` (get, set) | `KlyPalette` | Palette of the info color. |
| `Neutral` (get, set) | `KlyPalette` | Drives the text, background and border tokens as well. |
| `Primary` (get, set) | `KlyPalette` | Palette of the primary color. |
| `Secondary` (get, set) | `KlyPalette` | Palette of the secondary color. |
| `Success` (get, set) | `KlyPalette` | Palette of the success color. |
| `Warning` (get, set) | `KlyPalette` | Palette of the warning color. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Add(string name, KlyPalette palette)` | `void` | Registers an extra semantic color role (e.g. 'tertiary'). |

#### `KlyDefaults`

`Klyroo.UI` · class

Default `Color` and `Size` applied across components.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Color` (get, set) | `KlyColor?` | Color of the components whose `Color` parameter is not set. Null keeps the default of each component. |
| `Size` (get, set) | `KlySize?` | Size of the components whose `Size` parameter is not set and whose built-in size is medium. Null keeps the default of each component. |

#### `KlyImagePlaceholder`

`Klyroo.UI` · class

What `KlyImage` paints behind the image while it loads: `KlyImagePlaceholder.Auto` asks the image provider for a tiny (and, when it can blur, blurred) version of the image, `KlyImagePlaceholder.Url` uses a small image you already have.

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Auto` | `KlyImagePlaceholder` | A tiny version of the image itself, made by the image provider (blurred when it can blur). |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Url(string address)` | `KlyImagePlaceholder` | A small image at `address`, used as is: a root-relative path or an absolute URL, or a `data:` URL. An empty address shows no placeholder. |

#### `KlyImageSettings`

`Klyroo.UI` · class

Global image configuration (`KlyrooOptions.Images`): `AddKlyrooUi(o => { o.Images.Domains.Add("images.example.com"); o.Images.AddProvider(new MyProvider()); o.Images.DefaultProvider = "my"; })`.

| Constructor | Description |
| :--- | :--- |
| `KlyImageSettings()` | Creates the settings with the `none` provider, the Tailwind breakpoints and the densities 1 and 2. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `DefaultProvider` (get, set) | `string` | The name of the provider used when none is asked for. Defaults to `none`. |
| `Densities` (get, set) | `IReadOnlyList<int>` | Pixel densities of the generated `srcset`. Defaults to 1 and 2 (like `image.densities`). Never empty. |
| `Domains` | `ICollection<string>` | Hosts of the remote images a provider with `IKlyImageProvider.ValidateDomains` may process (`"images.example.com"`; a full URL is reduced to its host). Only the default port of the scheme matches unless the entry names a port (`"images.example.com:8443"`). A remote `src` from any other host is passed through untouched. |
| `Presets` | `IDictionary<string, KlyImageOptions>` | Named bundles of image options, selected with the `Preset` parameter (like `image.presets`). |
| `Providers` | `IReadOnlyCollection<IKlyImageProvider>` | The registered providers, `none` included. |
| `Screens` | `IDictionary<string, int>` | Breakpoints of the `Sizes` syntax, in pixels. Defaults to Tailwind's: sm 640, md 768, lg 1024, xl 1280, 2xl 1536. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddDefaultProvider(IKlyImageProvider provider)` | `KlyImageSettings` | Registers a provider and makes it the default one. |
| `AddProvider(IKlyImageProvider provider)` | `KlyImageSettings` | Registers a provider under its `IKlyImageProvider.Name`, replacing a provider of the same name. |
| `Resolve(string? src, KlyImageOptions? options = null, int? width = null, int? height = null)` | `KlyResolvedImage` | The resolved `src`, `srcset` and `sizes` of an image. Use it to render your own `img` or `picture`; `KlyImage` calls it. |
| `ResolvePlaceholder(string? src, KlyImageOptions? options, int width, int? height)` | `string?` | The URL of a tiny version of the image (quality 50, blurred when the provider can blur), for `Placeholder="true"`. Null when the source is not resolvable (empty, `data:`, `blob:`), the provider cannot resize or the image is remote and not allowed. Unknown providers and presets throw like `KlyImageSettings.Resolve`. |

#### `KlyLimitEventArgs`

`Klyroo.UI` · class · inherits `EventArgs`

Payload of the `kly-limit` browser event raised by `KlyEditor` when its text goes past `KlyEditor.MaxLength` or comes back within it.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Exceeded` (get, set) | `bool` | True while the text is longer than `KlyLimitEventArgs.Max`. |
| `Length` (get, set) | `int` | The characters of HTML the editor holds now. |
| `Max` (get, set) | `int` | The limit, `KlyEditor.MaxLength`. |

#### `KlyLocalStorageCultureStore`

`Klyroo.UI` · class

The default `IKlyCultureStore`: the `localStorage` of the web view, under the key `klyroo-culture`.

| Constructor | Description |
| :--- | :--- |
| `KlyLocalStorageCultureStore(IJSRuntime js)` | The default `IKlyCultureStore`: the `localStorage` of the web view, under the key `klyroo-culture`. |

| Field | Type | Description |
| :--- | :--- | :--- |
| `const Key = "klyroo-culture"` | `string` | The `localStorage` key. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetAsync()` | `ValueTask<string?>` | The stored culture name (`fr`, `pt-BR`), or null when none was chosen. |
| `SetAsync(string culture)` | `ValueTask` | Remembers the culture name the user chose. |

#### `KlyOverlayContext<TResult>`

`Klyroo.UI` · class

Handed to the component shown by `IKlyOverlayService` as a cascading parameter, so it can end its own dialog: `[CascadingParameter] public KlyOverlayContext<Person> Overlay { get; set; } = default!;` with the same `TResult` the caller of `ShowAsync` gave.

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsClosed` | `bool` | True once the dialog has been completed or canceled. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Cancel()` | `void` | Dismisses the dialog: closes it and gives the caller a canceled result. Does nothing once the dialog has ended. |
| `Close(TResult value)` | `void` | Completes the dialog: closes it and hands `value` to the caller of `ShowAsync`. Does nothing once the dialog has ended. |

#### `KlyPalette`

`Klyroo.UI` · class

A palette of 11 shades (50, 100, 200 … 900, 950) that a semantic color is mapped to, like the palettes of Tailwind CSS Use the Tailwind v4 defaults (`KlyPalette.Blue`, `KlyPalette.Zinc` …), `KlyPalette.Custom` for your own, or `KlyPalette.Klyroo`, the hand-tuned colors Klyroo.UI ships with.

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Amber` | `KlyPalette` | Tailwind CSS v4 `amber` palette. |
| `static Blue` | `KlyPalette` | Tailwind CSS v4 `blue` palette. |
| `static Cyan` | `KlyPalette` | Tailwind CSS v4 `cyan` palette. |
| `static Emerald` | `KlyPalette` | Tailwind CSS v4 `emerald` palette. |
| `static Fuchsia` | `KlyPalette` | Tailwind CSS v4 `fuchsia` palette. |
| `static Gray` | `KlyPalette` | Tailwind CSS v4 `gray` palette. |
| `static Green` | `KlyPalette` | Tailwind CSS v4 `green` palette. |
| `static Indigo` | `KlyPalette` | Tailwind CSS v4 `indigo` palette. |
| `IsKlyroo` | `bool` | True for `KlyPalette.Klyroo`, which carries no shades. |
| `static Klyroo` | `KlyPalette` | The colors Klyroo.UI ships with, tuned by hand for contrast (a lime primary, #b6ff00, and a teal secondary). Assigned to a semantic color it keeps that color's default; it has no shades. |
| `static Lime` | `KlyPalette` | Tailwind CSS v4 `lime` palette. |
| `static Mauve` | `KlyPalette` | Tailwind CSS v4 `mauve` palette. |
| `static Mist` | `KlyPalette` | Tailwind CSS v4 `mist` palette. |
| `Name` | `string` | The name of the palette, such as `blue`. |
| `static Neutral` | `KlyPalette` | Tailwind CSS v4 `neutral` palette. |
| `static Olive` | `KlyPalette` | Tailwind CSS v4 `olive` palette. |
| `static Orange` | `KlyPalette` | Tailwind CSS v4 `orange` palette. |
| `static Pink` | `KlyPalette` | Tailwind CSS v4 `pink` palette. |
| `static Purple` | `KlyPalette` | Tailwind CSS v4 `purple` palette. |
| `static Red` | `KlyPalette` | Tailwind CSS v4 `red` palette. |
| `static Rose` | `KlyPalette` | Tailwind CSS v4 `rose` palette. |
| `static ShadeNumbers` | `IReadOnlyList<int>` | The shade numbers of a palette, lightest first. |
| `Shades` | `IReadOnlyList<string>` | The shades as given, lightest first (empty for `KlyPalette.Klyroo`). |
| `static Sky` | `KlyPalette` | Tailwind CSS v4 `sky` palette. |
| `static Slate` | `KlyPalette` | Tailwind CSS v4 `slate` palette. |
| `static Stone` | `KlyPalette` | Tailwind CSS v4 `stone` palette. |
| `static TailwindPalettes` | `IReadOnlyList<KlyPalette>` | Every Tailwind CSS v4 default palette, in Tailwind's order. |
| `static Taupe` | `KlyPalette` | Tailwind CSS v4 `taupe` palette. |
| `static Teal` | `KlyPalette` | Tailwind CSS v4 `teal` palette. |
| `static Violet` | `KlyPalette` | Tailwind CSS v4 `violet` palette. |
| `static Yellow` | `KlyPalette` | Tailwind CSS v4 `yellow` palette. |
| `static Zinc` | `KlyPalette` | Tailwind CSS v4 `zinc` palette. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Custom(string name, IReadOnlyDictionary<int, string> shades)` | `KlyPalette` | A palette from all 11 shades, keyed by shade number (50 to 950). |
| `static Custom(string name, params string[] shades)` | `KlyPalette` | A palette from its 11 shades, lightest (50) first. |
| `ToString()` | `string` | Returns the name of the palette. |

#### `KlyPersistedStateBudget`

`Klyroo.UI` · class

The share of persistent component state that the Klyroo libraries of one page may hand from the prerender to the interactive render, shared by all of them (Klyroo.Table lists, Klyroo.Dashboard resources). The browser sends everything a page persisted back to the server in the single SignalR message that starts an Interactive Server circuit, and SignalR refuses that message past 32 KB by default, which fails the circuit: each library measures what it writes with `KlyPersistedStateBudget.SentBytes` and reserves it here, and skips (then loads again) what does not fit.

| Constructor | Description |
| :--- | :--- |
| `KlyPersistedStateBudget(KlyrooOptions options)` | Creates the budget of a scope. Use the instance `AddKlyrooUi` registers rather than creating one. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `MaxBytes` | `long` | The bytes, as sent, the libraries of the page may persist together. |
| `UsedBytes` | `long` | The bytes, as sent, already reserved in this scope. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static SentBytes(string key, string text)` | `long` | What a value persisted as text under `key` weighs in the message that starts a Server circuit. Blazor writes the text as a JSON string, which escapes quotes, `+`, `<` and non-ASCII characters, keeps it as bytes in a dictionary that is serialized with base64, then protects and base64-encodes the whole state again: about the escaped length plus the key, times 16/9. |
| `TryReserve(long bytes)` | `bool` | Takes `bytes` from the budget when they fit, and returns whether they did. Nothing is taken when they do not. |

#### `KlyPickEventArgs`

`Klyroo.UI` · class · inherits `EventArgs`

Payload of the `kly-pick` browser event raised by `KlyChart` when a bar, point or slice is clicked or chosen with Enter or Space.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Row` (get, set) | `int` | The position of the row in the data of the chart. |
| `Series` (get, set) | `int` | The position of the series the pick landed on, or -1 when the browser could not tell (the chart then takes the first series that has a value in the row). |

#### `KlyRemoteOptions`

`Klyroo.UI` · class

Loads options from a `KlyOptionsProvider` one page at a time: the pages listed so far, the search term, the cancellation of stale requests and the error state. It is the engine behind `KlySelectMenu` and `KlyInputMenu` when they have an `OptionsProvider`, public so that components of other Klyroo libraries (such as filter editors) list remote options the same way. A plain class with no rendering: `changed` is called after every transition, possibly on another thread, so a component calls `InvokeAsync(StateHasChanged)`. Dispose it with the component: it cancels the request in flight.

| Constructor | Description |
| :--- | :--- |
| `KlyRemoteOptions(KlyOptionsProvider provider, Action changed)` | Creates the engine. |

| Field | Type | Description |
| :--- | :--- | :--- |
| `const DefaultPageSize = 25` | `int` | The page size when none is set: 25 options. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Busy` | `bool` | A page is on its way: `KlyRemoteOptions.Status` is `KlyRemoteStatus.Loading` or `KlyRemoteStatus.LoadingMore`. |
| `HasMore` | `bool` | Whether the source has another page after the last one listed (`KlyRemoteOptions.LoadMoreAsync` asks for it). |
| `Items` | `IReadOnlyList<KlySelectItem>` | The options of the pages loaded so far, in the order of the source. The value of each is the text of the value the provider gave (a string as it is, a number or a date in the invariant culture, an enum by name), which is how the engine tells options apart. The list is live: read it on the thread that renders, and copy it to keep it. |
| `MinLength` (get, set) | `int` | Terms shorter than this (and not empty) are not searched. An empty term always lists the unfiltered options. |
| `PageSize` (get, set) | `int` | The number of options asked per page (at least 1). |
| `Provider` (get, set) | `KlyOptionsProvider` | The provider used for the next request. A parent that renders again with an equal one (a lambda that captures) keeps the state of the list. |
| `Status` | `KlyRemoteStatus` | Where the engine stands. |
| `Term` | `string` | The trimmed term of the current (or last) search. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Dispose()` | `void` | Cancels the request in flight and the resolutions, and ignores any answer that still arrives. The engine does nothing afterwards. |
| `LoadMoreAsync()` | `Task` | Loads the next page. Nothing happens while a request runs, after an error (use `KlyRemoteOptions.RetryAsync`) or at the end. |
| `RetryAsync()` | `Task` | Repeats the request that failed. |
| `SearchAsync(string? raw)` | `Task` | Searches for a term (what the field reports). The same term again changes nothing, except after an error, where it asks again. |

#### `KlyrooFontsManifestAttribute`

`Klyroo.UI` · class · inherits `Attribute`

Written into the app's assembly at build time by the `KlyrooFont` targets: the manifest `KlyFonts` reads. Not meant to be used by hand.

| Constructor | Description |
| :--- | :--- |
| `KlyrooFontsManifestAttribute(string json)` | Written into the app's assembly at build time by the `KlyrooFont` targets: the manifest `KlyFonts` reads. Not meant to be used by hand. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Json` | `string` | The manifest, as JSON: the route of the generated stylesheet and the faces to preload. |

#### `KlyrooIconOptions`

`Klyroo.UI` · class

The icons components draw: every property is an icon name (`i-lucide-x`, `ph:x`, `mdi:close`, or a bare Lucide name) with a Lucide default. Set them in `AddKlyrooUi` to replace an icon everywhere components use it: `o.Icons.Close = "ph:x"`. A name that is not one of the Lucide icons built into the library is bundled at build time when the assignment is a literal (see docs/guides.md in the repository); a name written another way goes in a `KlyrooIcon` item. The values become read-only the first time a component draws an icon.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ArrowDown` (get, set) | `string` | Arrow pointing down. Default `i-lucide-arrow-down`. |
| `ArrowLeft` (get, set) | `string` | Arrow pointing left (previous page of a content surround). Default `i-lucide-arrow-left`. |
| `ArrowRight` (get, set) | `string` | Arrow pointing right. Default `i-lucide-arrow-right`. |
| `ArrowUp` (get, set) | `string` | Arrow pointing up (sort ascending, send a chat message). Default `i-lucide-arrow-up`. |
| `Calendar` (get, set) | `string` | Opens the calendar of a date input. Default `i-lucide-calendar`. |
| `Caution` (get, set) | `string` | Something needs attention (chat attachment error). Default `i-lucide-circle-alert`. |
| `Check` (get, set) | `string` | A check mark: checkboxes, selected items, steps. Default `i-lucide-check`. |
| `ChevronDoubleLeft` (get, set) | `string` | First page. Default `i-lucide-chevrons-left`. |
| `ChevronDoubleRight` (get, set) | `string` | Last page. Default `i-lucide-chevrons-right`. |
| `ChevronDown` (get, set) | `string` | Disclosure chevrons, carousel arrows and select triggers. Default `i-lucide-chevron-down`. |
| `ChevronLeft` (get, set) | `string` | Previous page, back. Default `i-lucide-chevron-left`. |
| `ChevronRight` (get, set) | `string` | Next page, breadcrumb separator, tree chevron. Default `i-lucide-chevron-right`. |
| `ChevronUp` (get, set) | `string` | Chevron pointing up. Default `i-lucide-chevron-up`. |
| `ChevronUpDown` (get, set) | `string` | Trigger of selects and sortable columns. Default `i-lucide-chevrons-up-down`. |
| `Close` (get, set) | `string` | Dismiss buttons of alerts, banners, modals, drawers and slideovers, tag removal. Default `i-lucide-x`. |
| `Copy` (get, set) | `string` | Copy to the clipboard. Default `i-lucide-copy`. |
| `CopyCheck` (get, set) | `string` | Copied. Default `i-lucide-copy-check`. |
| `Dark` (get, set) | `string` | Dark color mode. Default `i-lucide-moon`. |
| `Drag` (get, set) | `string` | Drag handle. Default `i-lucide-grip-vertical`. |
| `Edit` (get, set) | `string` | Edit. Default `i-lucide-pencil`. |
| `Ellipsis` (get, set) | `string` | More actions. Default `i-lucide-ellipsis`. |
| `Error` (get, set) | `string` | Error. Default `i-lucide-circle-x`. |
| `External` (get, set) | `string` | Link to another site. Default `i-lucide-arrow-up-right`. |
| `Eye` (get, set) | `string` | Show a password. Default `i-lucide-eye`. |
| `EyeOff` (get, set) | `string` | Hide a password. Default `i-lucide-eye-off`. |
| `File` (get, set) | `string` | A file. Default `i-lucide-file`. |
| `Folder` (get, set) | `string` | A closed folder. Default `i-lucide-folder`. |
| `FolderOpen` (get, set) | `string` | An open folder. Default `i-lucide-folder-open`. |
| `Hash` (get, set) | `string` | Hash, anchor. Default `i-lucide-hash`. |
| `Info` (get, set) | `string` | Information. Default `i-lucide-info`. |
| `Light` (get, set) | `string` | Light color mode. Default `i-lucide-sun`. |
| `Loading` (get, set) | `string` | Spinner of loading buttons and busy states (spun by the component). Default `i-lucide-loader-circle`. |
| `Menu` (get, set) | `string` | Opens a navigation menu. Default `i-lucide-menu`. |
| `Minus` (get, set) | `string` | Decrement, not included. Default `i-lucide-minus`. |
| `PanelClose` (get, set) | `string` | Collapse a side panel. Default `i-lucide-panel-left-close`. |
| `PanelOpen` (get, set) | `string` | Expand a side panel. Default `i-lucide-panel-left-open`. |
| `Pause` (get, set) | `string` | Pause a carousel autoplay. Default `i-lucide-pause`. |
| `Play` (get, set) | `string` | Resume a carousel autoplay. Default `i-lucide-play`. |
| `Plus` (get, set) | `string` | Increment, add. Default `i-lucide-plus`. |
| `Reload` (get, set) | `string` | Retry, reload. Default `i-lucide-rotate-ccw`. |
| `Search` (get, set) | `string` | Search. Default `i-lucide-search`. |
| `Star` (get, set) | `string` | A rating star. Default `i-lucide-star`. |
| `Stop` (get, set) | `string` | Stop a running action. Default `i-lucide-square`. |
| `Success` (get, set) | `string` | Success. Default `i-lucide-circle-check`. |
| `System` (get, set) | `string` | System color mode. Default `i-lucide-monitor`. |
| `Tip` (get, set) | `string` | A tip. Default `i-lucide-lightbulb`. |
| `Trash` (get, set) | `string` | Delete. Default `i-lucide-trash`. |
| `Upload` (get, set) | `string` | Upload a file. Default `i-lucide-upload`. |
| `Warning` (get, set) | `string` | Warning. Default `i-lucide-triangle-alert`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Register(string name, string body, double width = 24, double height = 24)` | `void` | Registers an icon under a name, to use it like any other: `o.Icons.Register("custom:logo", "<path d=\"...\" />", 24, 24)`. `body` is the markup inside the `<svg>` element (what Iconify calls an icon's body): give the paths their own `fill` or `stroke` (`currentColor` follows the text color). A bare name replaces a built-in Lucide icon. It is markup written by the app, rendered as it is, so never register content from end users (`KlyrooIconOptions.RegisterSvg` sanitizes its input, this method does not). |
| `RegisterSvg(string name, string svg)` | `void` | Registers an icon from the text of an SVG file: its content becomes the body and its `viewBox` the drawing area. The file is sanitized like the icons the build bundles (scripts, handlers, images, external references and styles are removed), but it is still markup your app owns: never register content from end users. |

#### `KlyrooIconRegistrarAttribute`

`Klyroo.UI` · class · inherits `Attribute`

Written into an assembly by the Klyroo.UI build when the project bundles icons: `KlyrooIconRegistrarAttribute.Registrar` is the generated class whose module initializer registers them. `KlyrooIconBundle` uses it to register the icons of every assembly of the app before the first icon is drawn, so an icon declared in a client (WebAssembly) project is there when the server renders a component of that project.

| Constructor | Description |
| :--- | :--- |
| `KlyrooIconRegistrarAttribute(Type registrar)` | Declares the generated class that registers the icons of the assembly. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Registrar` | `Type` | The generated class whose module initializer registers the icons of the assembly. |

#### `KlyrooOptions`

`Klyroo.UI` · class

Global configuration of Klyroo.UI. Register it from code shared by the server and the WebAssembly client, otherwise components change appearance when an Auto render mode switches runtime.

| Constructor | Description |
| :--- | :--- |
| `KlyrooOptions()` | Creates the options with the default theme of every component. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Accordion` | `ComponentTheme` | Theme of `KlyAccordion`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Alert` | `ComponentTheme` | Theme of `KlyAlert`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `AllThemes` | `IEnumerable<ComponentTheme>` | Every theme: the `ComponentTheme` properties of this class, whichever file declares them, then those of each registered `KlyrooOptions.Extension``. Feeds the Tailwind build and the generated-assets tests. |
| `App` | `ComponentTheme` | Theme of `KlyApp`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `AuthForm` | `ComponentTheme` | Theme of `KlyAuthForm`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Avatar` | `ComponentTheme` | Theme of `KlyAvatar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `AvatarGroup` | `ComponentTheme` | Theme of `KlyAvatarGroup`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Badge` | `ComponentTheme` | Theme of `KlyBadge`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Banner` | `ComponentTheme` | Theme of `KlyBanner`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `BlogPost` | `ComponentTheme` | Theme of `KlyBlogPost`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `BlogPosts` | `ComponentTheme` | Theme of `KlyBlogPosts`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Breadcrumb` | `ComponentTheme` | Theme of `KlyBreadcrumb`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Button` | `ComponentTheme` | Theme of `KlyButton`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Calendar` | `ComponentTheme` | Theme of `KlyCalendar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Card` | `ComponentTheme` | Theme of `KlyCard`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Carousel` | `ComponentTheme` | Theme of `KlyCarousel`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChangelogVersion` | `ComponentTheme` | Theme of `KlyChangelogVersion`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChangelogVersions` | `ComponentTheme` | Theme of `KlyChangelogVersions`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Chart` | `ComponentTheme` | Theme of `KlyChart`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatAttachment` | `ComponentTheme` | Theme of `KlyChatAttachment`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatMessage` | `ComponentTheme` | Theme of `KlyChatMessage`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatMessages` | `ComponentTheme` | Theme of `KlyChatMessages`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatPalette` | `ComponentTheme` | Theme of `KlyChatPalette`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatPrompt` | `ComponentTheme` | Theme of `KlyChatPrompt`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatPromptSubmit` | `ComponentTheme` | Theme of `KlyChatPromptSubmit`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatReasoning` | `ComponentTheme` | Theme of `KlyChatReasoning`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatShimmer` | `ComponentTheme` | Theme of `KlyChatShimmer`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ChatTool` | `ComponentTheme` | Theme of `KlyChatTool`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Checkbox` | `ComponentTheme` | Theme of `KlyCheckbox`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `CheckboxGroup` | `ComponentTheme` | Theme of `KlyCheckboxGroup`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Chip` | `ComponentTheme` | Theme of `KlyChip`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Collapsible` | `ComponentTheme` | Theme of `KlyCollapsible`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ColorModeAvatar` | `ComponentTheme` | Theme of `KlyColorModeAvatar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ColorModeButton` | `ComponentTheme` | Theme of `KlyColorModeButton`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ColorModeImage` | `ComponentTheme` | Theme of `KlyColorModeImage`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ColorModeSelect` | `ComponentTheme` | Theme of `KlyColorModeSelect`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ColorModeSwitch` | `ComponentTheme` | Theme of `KlyColorModeSwitch`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ColorPicker` | `ComponentTheme` | Theme of `KlyColorPicker`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Colors` | `KlyColorOptions` | Palettes of the semantic colors (`Primary`, `Neutral`…) and extra semantic colors. Render `KlyThemeStyle` in `<head>` to deliver them. |
| `CommandPalette` | `ComponentTheme` | Theme of `KlyCommandPalette`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Container` | `ComponentTheme` | Theme of `KlyContainer`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ContentNavigation` | `ComponentTheme` | Theme of `KlyContentNavigation`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ContentSearchButton` | `ComponentTheme` | Theme of `KlyContentSearchButton`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ContentSurround` | `ComponentTheme` | Theme of `KlyContentSurround`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ContentToc` | `ComponentTheme` | Theme of `KlyContentToc`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ContextMenu` | `ComponentTheme` | Theme of `KlyContextMenu`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardGroup` | `ComponentTheme` | Theme of `KlyDashboardGroup`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardNavbar` | `ComponentTheme` | Theme of `KlyDashboardNavbar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardPanel` | `ComponentTheme` | Theme of `KlyDashboardPanel`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardResizeHandle` | `ComponentTheme` | Theme of `KlyDashboardResizeHandle`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardSearchButton` | `ComponentTheme` | Theme of `KlyDashboardSearchButton`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardSidebarCollapse` | `ComponentTheme` | Theme of `KlyDashboardSidebarCollapse`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardSidebarToggle` | `ComponentTheme` | Theme of `KlyDashboardSidebarToggle`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DashboardToolbar` | `ComponentTheme` | Theme of `KlyDashboardToolbar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Defaults` | `KlyDefaults` | Default `Color` and `Size` of the components, when a parameter is not set. |
| `Drawer` | `ComponentTheme` | Theme of `KlyDrawer`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `DropdownMenu` | `ComponentTheme` | Theme of `KlyDropdownMenu`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Editor` | `ComponentTheme` | Theme of `KlyEditor`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `EditorDragHandle` | `ComponentTheme` | Theme of `KlyEditorDragHandle`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `EditorMenu` | `ComponentTheme` | Theme shared by `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `EditorToolbar` | `ComponentTheme` | Theme of `KlyEditorToolbar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Empty` | `ComponentTheme` | Theme of `KlyEmpty`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Error` | `ComponentTheme` | Theme of `KlyError`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `FieldGroup` | `ComponentTheme` | Theme of `KlyFieldGroup`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `FileUpload` | `ComponentTheme` | Theme of `KlyFileUpload`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Footer` | `ComponentTheme` | Theme of `KlyFooter`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `FooterColumns` | `ComponentTheme` | Theme of `KlyFooterColumns`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Form` | `ComponentTheme` | Theme of `KlyForm`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `FormField` | `ComponentTheme` | Theme of `KlyFormField`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Header` | `ComponentTheme` | Theme of `KlyHeader`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Icon` | `ComponentTheme` | Theme of `KlyIcon`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Icons` | `KlyrooIconOptions` | The icons components draw (`Close`, `Check`, `ChevronDown`...), replaceable by name, and `Register` to add icons in code. Any Iconify icon works by name, see `KlyIcon`. |
| `Images` | `KlyImageSettings` | How images are resized and delivered: providers, breakpoints, densities, allowed remote hosts and presets, global image provider and preset configuration. Read-only once an image was resolved. |
| `InfiniteScroll` | `ComponentTheme` | Theme of `KlyInfiniteScroll`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Input` | `ComponentTheme` | Theme of `KlyInput`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `InputDate` | `ComponentTheme` | Theme of `KlyInputDate`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `InputMenu` | `ComponentTheme` | Theme of `KlyInputMenu`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `InputNumber` | `ComponentTheme` | Theme of `KlyInputNumber`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `InputRating` | `ComponentTheme` | Theme of `KlyInputRating`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `InputTags` | `ComponentTheme` | Theme of `KlyInputTags`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `InputTime` | `ComponentTheme` | Theme of `KlyInputTime`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Kbd` | `ComponentTheme` | Theme of `KlyKbd`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Link` | `ComponentTheme` | Theme of `KlyLink`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Listbox` | `ComponentTheme` | Theme of `KlyListbox`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Locale` (get, set) | `KlyLocale?` | The locale every component uses unless a `KlyApp` (or a `CascadingValue` of `KlyLocale`) above it sets one. Leave it unset to follow the culture of the user: the locale is then picked from `KlyrooOptions.Locales` by `CultureInfo.CurrentUICulture`. Like the themes, it becomes read-only the first time a locale is resolved: set it in `AddKlyrooUi`. |
| `Locales` (get, set) | `IReadOnlyList<KlyLocale>` | The locales the automatic selection chooses from, and that `KlyLocaleSelect` offers by default. Defaults to `KlyLocales.All`; assign a new list to choose fewer or to add your own, for example `o.Locales = [.. KlyLocales.All, australian]`. The list is copied when assigned, so changing the original afterwards has no effect, and it becomes read-only the first time a locale is resolved (a later assignment throws `InvalidOperationException`). English (code "en"), or the first locale when there is none, is the fallback of an unmatched culture. |
| `LocaleSelect` | `ComponentTheme` | Theme of `KlyLocaleSelect`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Main` | `ComponentTheme` | Theme of `KlyMain`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Marquee` | `ComponentTheme` | Theme of `KlyMarquee`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Modal` | `ComponentTheme` | Theme of `KlyModal`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `NavigationMenu` | `ComponentTheme` | Theme of `KlyNavigationMenu`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Page` | `ComponentTheme` | Theme of `KlyPage`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageAnchors` | `ComponentTheme` | Theme of `KlyPageAnchors`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageAside` | `ComponentTheme` | Theme of `KlyPageAside`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageBody` | `ComponentTheme` | Theme of `KlyPageBody`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageCard` | `ComponentTheme` | Theme of `KlyPageCard`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageColumns` | `ComponentTheme` | Theme of `KlyPageColumns`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageCta` | `ComponentTheme` | Theme of `KlyPageCta`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageFeature` | `ComponentTheme` | Theme of `KlyPageFeature`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageGrid` | `ComponentTheme` | Theme of `KlyPageGrid`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageHeader` | `ComponentTheme` | Theme of `KlyPageHeader`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageHero` | `ComponentTheme` | Theme of `KlyPageHero`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageLinks` | `ComponentTheme` | Theme of `KlyPageLinks`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageList` | `ComponentTheme` | Theme of `KlyPageList`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageLogos` | `ComponentTheme` | Theme of `KlyPageLogos`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PageSection` | `ComponentTheme` | Theme of `KlyPageSection`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Pagination` | `ComponentTheme` | Theme of `KlyPagination`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PersistedStateMaxBytes` (get, set) | `int` | The most bytes, as sent, that the Klyroo libraries of one page hand from the prerender to the interactive render together (28 KB by default), shared through `KlyPersistedStateBudget`. The browser sends the persisted state of a page back to the server in the message that starts an Interactive Server circuit, which SignalR refuses past 32 KB by default, so the default leaves room for the state of the app itself. Raise it only with `MaximumReceiveMessageSize` of the hub. |
| `PinInput` | `ComponentTheme` | Theme of `KlyPinInput`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Popover` | `ComponentTheme` | Theme of `KlyPopover`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PricingPlan` | `ComponentTheme` | Theme of `KlyPricingPlan`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PricingPlans` | `ComponentTheme` | Theme of `KlyPricingPlans`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `PricingTable` | `ComponentTheme` | Theme of `KlyPricingTable`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Progress` | `ComponentTheme` | Theme of `KlyProgress`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `RadioGroup` | `ComponentTheme` | Theme of `KlyRadioGroup`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `SanitizeEditorHtml` (get, set) | `Func<string, string>?` | Sanitizes editor HTML before the server shows it as markup (the read-only view displayed until the editor script loads, and on static pages). Without one, that view shows the text only, never raw markup: the value is user input and rendering it as HTML would let stored script run. Plug an allow-list sanitizer here, for example `o.SanitizeEditorHtml = html => new Ganss.Xss.HtmlSanitizer().Sanitize(html);` |
| `ScrollArea` | `ComponentTheme` | Theme of `KlyScrollArea`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Select` | `ComponentTheme` | Theme of `KlySelect`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `SelectMenu` | `ComponentTheme` | Theme of `KlySelectMenu`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Separator` | `ComponentTheme` | Theme of `KlySeparator`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Sidebar` | `ComponentTheme` | Theme of `KlySidebar`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Skeleton` | `ComponentTheme` | Theme of `KlySkeleton`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `SkipLink` | `ComponentTheme` | Theme of `KlySkipLink`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Slideover` | `ComponentTheme` | Theme of `KlySlideover`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Slider` | `ComponentTheme` | Theme of `KlySlider`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Splitter` | `ComponentTheme` | Theme of `KlySplitter`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Stepper` | `ComponentTheme` | Theme of `KlyStepper`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Switch` | `ComponentTheme` | Theme of `KlySwitch`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Table` | `ComponentTheme` | Theme of `KlyTable`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Tabs` | `ComponentTheme` | Theme of `KlyTabs`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Textarea` | `ComponentTheme` | Theme of `KlyTextarea`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Timeline` | `ComponentTheme` | Theme of `KlyTimeline`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Toast` | `ComponentTheme` | Theme of `KlyToaster` and of the toasts it shows: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Toggle` | `ComponentTheme` | Theme of `KlyToggle`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `ToggleGroup` | `ComponentTheme` | Theme of `KlyToggleGroup`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Tooltip` | `ComponentTheme` | Theme of `KlyTooltip`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `Tree` | `ComponentTheme` | Theme of `KlyTree`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |
| `User` | `ComponentTheme` | Theme of `KlyUser`: the default classes of its slots, its variants and its defaults. Configure it in `AddKlyrooUi`; it becomes read-only the first time a component renders with it. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Extension<T>()` | `T` | The options of a sister package, created on the first call and shared afterwards: one instance per `T` and per `KlyrooOptions`, whichever thread asks first. Its public `ComponentTheme` properties are themes like those of this class: `KlyrooOptions.AllThemes` lists them, the Tailwind class extractor and the docs bundle read them, and a theme becomes read-only when a component resolves it, with the same `InvalidOperationException` as the themes of this class. |
| `ResolveLocale(CultureInfo culture)` | `KlyLocale` | The locale for a culture: `KlyrooOptions.Locale` when set, otherwise the match in `KlyrooOptions.Locales` (see `KlyLocales.Resolve`). The result of each culture is cached, and the first call makes `KlyrooOptions.Locale` and `KlyrooOptions.Locales` read-only. |

#### `KlySearchEventArgs`

`Klyroo.UI` · class · inherits `EventArgs`

Payload of the `kly-search` browser event raised by menus whose options come from .NET, when the text typed in their filter field settles.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Sequence` (get, set) | `int` | Increments with every report of one element. .NET orders answers by what it asked last, so it does not read it. |
| `Value` (get, set) | `string?` | The text of the filter field, as typed (not trimmed). |

#### `KlyToggleEventArgs`

`Klyroo.UI` · class · inherits `EventArgs`

Payload of the `kly-toggle` browser event raised by Klyroo custom elements when they open or close.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Open` (get, set) | `bool` | True when the element is open after the toggle. |
| `Sequence` (get, set) | `int` | Increments with every toggle of one element. Rendered back so the element can ignore a render that was computed before .NET saw its latest toggle. |

#### `ComponentTheme`

`Klyroo.UI.Theming` · class

Declarative theme of a component, the C# counterpart of a tailwind-variants definition: base classes per slot, classes per variant value, compound variants and default variant values. Every class must be written as a literal so the Tailwind build can see it. Overriding a theme through `AddKlyrooUi` appends classes; conflicts are resolved by tailwind-merge, so the last class wins.

| Constructor | Description |
| :--- | :--- |
| `ComponentTheme(string name, params string[] slots)` | Creates a theme with the given slots, the first of which is the root element. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CompoundVariants` | `IReadOnlyList<CompoundVariant>` | Classes applied when several variant values match at once. |
| `DefaultVariants` | `IReadOnlyDictionary<string, string>` | Variant value used when a component passes none. |
| `Name` | `string` | Name of the theme, which is the name of its component. |
| `Slots` | `IReadOnlyDictionary<string, string>` | Base classes of each slot. The first slot is the root element. |
| `Variants` | `IReadOnlyDictionary<string, IReadOnlyDictionary<string, SlotClasses>>` | Variant name → variant value → slot classes. Boolean variants use "true" and "false". |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AllClassTokens()` | `IEnumerable<string>` | Every class token used anywhere in the theme. Feeds the Tailwind build. |
| `Compound(Dictionary<string, string> when, string slot, string classes)` | `ComponentTheme` | Appends classes applied to `slot` when every entry of `when` matches. The dictionary is copied. |
| `Default(string variant, string value)` | `ComponentTheme` | Sets the value of a variant that applies when a component does not choose one. |
| `Slot(string slot, string classes)` | `ComponentTheme` | Appends classes to a slot's base classes. |
| `Variant(string variant, string value, string slot, string classes)` | `ComponentTheme` | Appends classes applied to `slot` when variant `variant` equals `value`. |

#### `CompoundVariant`

`Klyroo.UI.Theming` · class

Classes applied when every condition in `CompoundVariant.When` matches the selected variants.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Classes` | `SlotClasses` | The classes added to each slot when every condition of `CompoundVariant.When` matches. |
| `When` | `IReadOnlyDictionary<string, string>` | The variant values that must all match, by variant name. |

#### `FormFieldContext`

`Klyroo.UI.Theming` · class

Cascaded by `KlyFormField` to the control inside it, which takes the field's input id and adds the field's description, hint and error to its `aria-describedby`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `DescribedBy` (init) | `string?` | The ids of the description, hint and error elements, to add to `aria-describedby`. |
| `InputId` (init) | `string` | The `id` the control inside the field must use. |
| `Invalid` (init) | `bool` | Whether the field shows an error. |
| `LabelId` (init) | `string?` | The `id` of the label of the field, if it has one. |
| `Required` (init) | `bool` | Whether the field is required. |
| `Size` (init) | `KlySize` | Size the control inside the field takes when it has none of its own. |

#### `SlotClasses`

`Klyroo.UI.Theming` · class

Classes to add to one or more slots, keyed by slot name. Read-only: a `ComponentTheme` builds them through its fluent methods.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Count` | `int` | The number of slots that have classes. |
| `Keys` | `IEnumerable<string>` | The names of the slots that have classes. |
| `Values` | `IEnumerable<string>` | The classes of each slot. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `ContainsKey(string key)` | `bool` | Whether the slot has classes. |
| `GetEnumerator()` | `IEnumerator<KeyValuePair<string, string>>` | Enumerates the slots and their classes. |
| `TryGetValue(string key, out string value)` | `bool` | Gets the classes of a slot, if it has any. |

#### `SlotSet`

`Klyroo.UI.Theming` · class

The resolved classes of each slot of a component, returned by `ResolveSlots` of `KlyComponentBase`.

### Records

#### `AccordionUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyAccordion`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyAccordion`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyAccordion`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyAccordion`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyAccordion`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyAccordion`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyAccordion`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyAccordion`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlyAccordion`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyAccordion`, after the theme and the app configuration. |

#### `AlertUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyAlert`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyAlert`, after the theme and the app configuration. |
| `Close` (init) | `string?` | Classes merged into the `close` slot of `KlyAlert`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyAlert`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyAlert`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyAlert`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyAlert`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyAlert`, after the theme and the app configuration. |

#### `AppUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyApp` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyApp`, after the theme and the app configuration. |

#### `AuthFormUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyAuthForm`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Error` (init) | `string?` | Classes merged into the `error` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Fieldset` (init) | `string?` | Classes merged into the `fieldset` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Form` (init) | `string?` | Classes merged into the `form` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `PasswordButton` (init) | `string?` | Classes merged into the `passwordButton` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `PasswordRoot` (init) | `string?` | Classes merged into the `passwordRoot` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Providers` (init) | `string?` | Classes merged into the `providers` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyAuthForm`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyAuthForm`, after the theme and the app configuration. |

#### `AvatarGroupUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyAvatarGroup` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Avatar` (init) | `string?` | Classes merged into the `avatar` slot of `KlyAvatarGroup`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyAvatarGroup`, after the theme and the app configuration. |

#### `AvatarUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyAvatar` and `KlyColorModeAvatar`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Fallback` (init) | `string?` | Classes merged into the `fallback` slot of `KlyAvatar` and `KlyColorModeAvatar`, after the theme and the app configuration. |
| `Image` (init) | `string?` | Classes merged into the `image` slot of `KlyAvatar` and `KlyColorModeAvatar`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyAvatar` and `KlyColorModeAvatar`, after the theme and the app configuration. |

#### `BadgeUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyBadge`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyBadge`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyBadge`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyBadge`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlyBadge`, after the theme and the app configuration. |

#### `BannerUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyBanner` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyBanner`, after the theme and the app configuration. |
| `Close` (init) | `string?` | Classes merged into the `close` slot of `KlyBanner`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyBanner`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyBanner`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyBanner`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyBanner`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyBanner`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyBanner`, after the theme and the app configuration. |

#### `BlogPostsUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyBlogPosts`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyBlogPosts`, after the theme and the app configuration. |

#### `BlogPostUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyBlogPost`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Authors` (init) | `string?` | Classes merged into the `authors` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Badge` (init) | `string?` | Classes merged into the `badge` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Date` (init) | `string?` | Classes merged into the `date` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Image` (init) | `string?` | Classes merged into the `image` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Meta` (init) | `string?` | Classes merged into the `meta` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyBlogPost`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyBlogPost`, after the theme and the app configuration. |

#### `BreadcrumbUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyBreadcrumb`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyBreadcrumb`, after the theme and the app configuration. |
| `SeparatorIcon` (init) | `string?` | Classes merged into the `separatorIcon` slot of `KlyBreadcrumb`, after the theme and the app configuration. |

#### `ButtonUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyButton` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyButton`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyButton`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyButton`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlyButton`, after the theme and the app configuration. |

#### `CalendarUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyCalendar`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Cell` (init) | `string?` | Classes merged into the `cell` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Day` (init) | `string?` | Classes merged into the `day` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Grid` (init) | `string?` | Classes merged into the `grid` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Heading` (init) | `string?` | Classes merged into the `heading` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Nav` (init) | `string?` | Classes merged into the `nav` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCalendar`, after the theme and the app configuration. |
| `Weekday` (init) | `string?` | Classes merged into the `weekday` slot of `KlyCalendar`, after the theme and the app configuration. |

#### `CardUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyCard`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyCard`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyCard`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyCard`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCard`, after the theme and the app configuration. |

#### `CarouselUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyCarousel`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Control` (init) | `string?` | Classes merged into the `control` slot of `KlyCarousel`, after the theme and the app configuration. |
| `Controls` (init) | `string?` | Classes merged into the `controls` slot of `KlyCarousel`, after the theme and the app configuration. |
| `Dot` (init) | `string?` | Classes merged into the `dot` slot of `KlyCarousel`, after the theme and the app configuration. |
| `Dots` (init) | `string?` | Classes merged into the `dots` slot of `KlyCarousel`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCarousel`, after the theme and the app configuration. |
| `Slide` (init) | `string?` | Classes merged into the `slide` slot of `KlyCarousel`, after the theme and the app configuration. |
| `Viewport` (init) | `string?` | Classes merged into the `viewport` slot of `KlyCarousel`, after the theme and the app configuration. |

#### `ChangelogVersionsUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyChangelogVersions`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChangelogVersions`, after the theme and the app configuration. |

#### `ChangelogVersionUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyChangelogVersion`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Authors` (init) | `string?` | Classes merged into the `authors` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Badge` (init) | `string?` | Classes merged into the `badge` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Date` (init) | `string?` | Classes merged into the `date` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Image` (init) | `string?` | Classes merged into the `image` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `ImageElement` (init) | `string?` | Classes merged into the `imageElement` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Indicator` (init) | `string?` | Classes merged into the `indicator` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Meta` (init) | `string?` | Classes merged into the `meta` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChangelogVersion`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyChangelogVersion`, after the theme and the app configuration. |

#### `ChartUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChart` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `AxisLine` (init) | `string?` | Classes merged into the `axisLine` slot of `KlyChart`, after the theme and the app configuration. |
| `AxisTitle` (init) | `string?` | Classes merged into the `axisTitle` slot of `KlyChart`, after the theme and the app configuration. |
| `Bar` (init) | `string?` | Classes merged into the `bar` slot of `KlyChart`, after the theme and the app configuration. |
| `Center` (init) | `string?` | Classes merged into the `center` slot of `KlyChart`, after the theme and the app configuration. |
| `Details` (init) | `string?` | Classes merged into the `details` slot of `KlyChart`, after the theme and the app configuration. |
| `Grid` (init) | `string?` | Classes merged into the `grid` slot of `KlyChart`, after the theme and the app configuration. |
| `Hit` (init) | `string?` | Classes merged into the `hit` slot of `KlyChart`, after the theme and the app configuration. |
| `HitArea` (init) | `string?` | Classes merged into the `hitArea` slot of `KlyChart`, after the theme and the app configuration. |
| `Legend` (init) | `string?` | Classes merged into the `legend` slot of `KlyChart`, after the theme and the app configuration. |
| `LegendItem` (init) | `string?` | Classes merged into the `legendItem` slot of `KlyChart`, after the theme and the app configuration. |
| `LegendLabel` (init) | `string?` | Classes merged into the `legendLabel` slot of `KlyChart`, after the theme and the app configuration. |
| `LegendSwatch` (init) | `string?` | Classes merged into the `legendSwatch` slot of `KlyChart`, after the theme and the app configuration. |
| `LegendValue` (init) | `string?` | Classes merged into the `legendValue` slot of `KlyChart`, after the theme and the app configuration. |
| `Line` (init) | `string?` | Classes merged into the `line` slot of `KlyChart`, after the theme and the app configuration. |
| `Marker` (init) | `string?` | Classes merged into the `marker` slot of `KlyChart`, after the theme and the app configuration. |
| `Plot` (init) | `string?` | Classes merged into the `plot` slot of `KlyChart`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChart`, after the theme and the app configuration. |
| `Slice` (init) | `string?` | Classes merged into the `slice` slot of `KlyChart`, after the theme and the app configuration. |
| `Summary` (init) | `string?` | Classes merged into the `summary` slot of `KlyChart`, after the theme and the app configuration. |
| `Svg` (init) | `string?` | Classes merged into the `svg` slot of `KlyChart`, after the theme and the app configuration. |
| `Table` (init) | `string?` | Classes merged into the `table` slot of `KlyChart`, after the theme and the app configuration. |
| `TableWrapper` (init) | `string?` | Classes merged into the `tableWrapper` slot of `KlyChart`, after the theme and the app configuration. |
| `Td` (init) | `string?` | Classes merged into the `td` slot of `KlyChart`, after the theme and the app configuration. |
| `Th` (init) | `string?` | Classes merged into the `th` slot of `KlyChart`, after the theme and the app configuration. |
| `Tick` (init) | `string?` | Classes merged into the `tick` slot of `KlyChart`, after the theme and the app configuration. |
| `Tooltip` (init) | `string?` | Classes merged into the `tooltip` slot of `KlyChart`, after the theme and the app configuration. |
| `TooltipIndicator` (init) | `string?` | Classes merged into the `tooltipIndicator` slot of `KlyChart`, after the theme and the app configuration. |
| `TooltipLabel` (init) | `string?` | Classes merged into the `tooltipLabel` slot of `KlyChart`, after the theme and the app configuration. |
| `TooltipRow` (init) | `string?` | Classes merged into the `tooltipRow` slot of `KlyChart`, after the theme and the app configuration. |
| `TooltipTitle` (init) | `string?` | Classes merged into the `tooltipTitle` slot of `KlyChart`, after the theme and the app configuration. |
| `TooltipValue` (init) | `string?` | Classes merged into the `tooltipValue` slot of `KlyChart`, after the theme and the app configuration. |

#### `ChatAttachmentUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatAttachment` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `Image` (init) | `string?` | Classes merged into the `image` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `Name` (init) | `string?` | Classes merged into the `name` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `Remove` (init) | `string?` | Classes merged into the `remove` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `RemoveIcon` (init) | `string?` | Classes merged into the `removeIcon` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatAttachment`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyChatAttachment`, after the theme and the app configuration. |

#### `ChatMessagesUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatMessages` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Dot` (init) | `string?` | Classes merged into the `dot` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Error` (init) | `string?` | Classes merged into the `error` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Indicator` (init) | `string?` | Classes merged into the `indicator` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Jump` (init) | `string?` | Classes merged into the `jump` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `JumpIcon` (init) | `string?` | Classes merged into the `jumpIcon` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Status` (init) | `string?` | Classes merged into the `status` slot of `KlyChatMessages`, after the theme and the app configuration. |
| `Viewport` (init) | `string?` | Classes merged into the `viewport` slot of `KlyChatMessages`, after the theme and the app configuration. |

#### `ChatMessageUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatMessage` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Files` (init) | `string?` | Classes merged into the `files` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Text` (init) | `string?` | Classes merged into the `text` slot of `KlyChatMessage`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyChatMessage`, after the theme and the app configuration. |

#### `ChatPaletteUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatPalette` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyChatPalette`, after the theme and the app configuration. |
| `Prompt` (init) | `string?` | Classes merged into the `prompt` slot of `KlyChatPalette`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatPalette`, after the theme and the app configuration. |

#### `ChatPromptSubmitUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatPromptSubmit` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyChatPromptSubmit`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyChatPromptSubmit`, after the theme and the app configuration. |
| `StopIcon` (init) | `string?` | Classes merged into the `stopIcon` slot of `KlyChatPromptSubmit`, after the theme and the app configuration. |

#### `ChatPromptUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatPrompt` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Attachments` (init) | `string?` | Classes merged into the `attachments` slot of `KlyChatPrompt`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyChatPrompt`, after the theme and the app configuration. |
| `Form` (init) | `string?` | Classes merged into the `form` slot of `KlyChatPrompt`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatPrompt`, after the theme and the app configuration. |
| `Row` (init) | `string?` | Classes merged into the `row` slot of `KlyChatPrompt`, after the theme and the app configuration. |
| `Textarea` (init) | `string?` | Classes merged into the `textarea` slot of `KlyChatPrompt`, after the theme and the app configuration. |
| `Trailing` (init) | `string?` | Classes merged into the `trailing` slot of `KlyChatPrompt`, after the theme and the app configuration. |

#### `ChatReasoningUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatReasoning` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `ChevronIcon` (init) | `string?` | Classes merged into the `chevronIcon` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatReasoning`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyChatReasoning`, after the theme and the app configuration. |

#### `ChatShimmerUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatShimmer` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyChatShimmer`, after the theme and the app configuration. |

#### `ChatToolUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChatTool` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyChatTool`, after the theme and the app configuration. |
| `ChevronIcon` (init) | `string?` | Classes merged into the `chevronIcon` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyChatTool`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Suffix` (init) | `string?` | Classes merged into the `suffix` slot of `KlyChatTool`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyChatTool`, after the theme and the app configuration. |

#### `CheckboxGroupUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyCheckboxGroup`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Fieldset` (init) | `string?` | Classes merged into the `fieldset` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Legend` (init) | `string?` | Classes merged into the `legend` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyCheckboxGroup`, after the theme and the app configuration. |

#### `CheckboxUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyCheckbox`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `IndeterminateIcon` (init) | `string?` | Classes merged into the `indeterminateIcon` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCheckbox`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyCheckbox`, after the theme and the app configuration. |

#### `ChipUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyChip` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyChip`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyChip`, after the theme and the app configuration. |

#### `CollapsibleUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyCollapsible`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyCollapsible`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyCollapsible`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyCollapsible`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCollapsible`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyCollapsible`, after the theme and the app configuration. |

#### `ColorModeSelectUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyColorModeSelect` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyColorModeSelect`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyColorModeSelect`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlyColorModeSelect`, after the theme and the app configuration. |

#### `ColorModeSwitchUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyColorModeSwitch` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Input` (init) | `string?` | Classes merged into the `input` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Moon` (init) | `string?` | Classes merged into the `moon` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Sun` (init) | `string?` | Classes merged into the `sun` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Thumb` (init) | `string?` | Classes merged into the `thumb` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyColorModeSwitch`, after the theme and the app configuration. |

#### `ColorPickerUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyColorPicker`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Area` (init) | `string?` | Classes merged into the `area` slot of `KlyColorPicker`, after the theme and the app configuration. |
| `AreaThumb` (init) | `string?` | Classes merged into the `areaThumb` slot of `KlyColorPicker`, after the theme and the app configuration. |
| `Hex` (init) | `string?` | Classes merged into the `hex` slot of `KlyColorPicker`, after the theme and the app configuration. |
| `Hue` (init) | `string?` | Classes merged into the `hue` slot of `KlyColorPicker`, after the theme and the app configuration. |
| `Preview` (init) | `string?` | Classes merged into the `preview` slot of `KlyColorPicker`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyColorPicker`, after the theme and the app configuration. |
| `Swatch` (init) | `string?` | Classes merged into the `swatch` slot of `KlyColorPicker`, after the theme and the app configuration. |

#### `CommandPaletteUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyCommandPalette` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Empty` (init) | `string?` | Classes merged into the `empty` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `GroupLabel` (init) | `string?` | Classes merged into the `groupLabel` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `Input` (init) | `string?` | Classes merged into the `input` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `InputIcon` (init) | `string?` | Classes merged into the `inputIcon` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `InputWrapper` (init) | `string?` | Classes merged into the `inputWrapper` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `ItemCount` (init) | `string?` | Classes merged into the `itemCount` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `ItemDescription` (init) | `string?` | Classes merged into the `itemDescription` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `ItemShortcut` (init) | `string?` | Classes merged into the `itemShortcut` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `ItemWrapper` (init) | `string?` | Classes merged into the `itemWrapper` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `Listbox` (init) | `string?` | Classes merged into the `listbox` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |
| `Status` (init) | `string?` | Classes merged into the `status` slot of `KlyCommandPalette`, `KlyContentSearch` and `KlyDashboardSearch`, after the theme and the app configuration. |

#### `ContainerUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyContainer` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyContainer`, after the theme and the app configuration. |

#### `ContentNavigationUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyContentNavigation` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Badge` (init) | `string?` | Classes merged into the `badge` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Chevron` (init) | `string?` | Classes merged into the `chevron` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `GroupList` (init) | `string?` | Classes merged into the `groupList` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Section` (init) | `string?` | Classes merged into the `section` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `SectionList` (init) | `string?` | Classes merged into the `sectionList` slot of `KlyContentNavigation`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyContentNavigation`, after the theme and the app configuration. |

#### `ContentSearchButtonUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyContentSearchButton` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Kbds` (init) | `string?` | Classes merged into the `kbds` slot of `KlyContentSearchButton`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyContentSearchButton`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyContentSearchButton`, after the theme and the app configuration. |

#### `ContentSurroundUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyContentSurround` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Caption` (init) | `string?` | Classes merged into the `caption` slot of `KlyContentSurround`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyContentSurround`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyContentSurround`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyContentSurround`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyContentSurround`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyContentSurround`, after the theme and the app configuration. |

#### `ContentTocUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyContentToc` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Chevron` (init) | `string?` | Classes merged into the `chevron` slot of `KlyContentToc`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyContentToc`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyContentToc`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyContentToc`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyContentToc`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyContentToc`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyContentToc`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyContentToc`, after the theme and the app configuration. |

#### `ContextMenuUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyContextMenu` slots. `Trigger` is the area around the content.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `ItemShortcut` (init) | `string?` | Classes merged into the `itemShortcut` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyContextMenu`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyContextMenu`, after the theme and the app configuration. |

#### `DashboardGroupUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDashboardGroup` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardGroup`, after the theme and the app configuration. |

#### `DashboardNavbarUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDashboardNavbar` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Center` (init) | `string?` | Classes merged into the `center` slot of `KlyDashboardNavbar`, after the theme and the app configuration. |
| `Left` (init) | `string?` | Classes merged into the `left` slot of `KlyDashboardNavbar`, after the theme and the app configuration. |
| `Right` (init) | `string?` | Classes merged into the `right` slot of `KlyDashboardNavbar`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardNavbar`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyDashboardNavbar`, after the theme and the app configuration. |

#### `DashboardPanelUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDashboardPanel` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyDashboardPanel`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyDashboardPanel`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyDashboardPanel`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardPanel`, after the theme and the app configuration. |

#### `DashboardResizeHandleUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDashboardResizeHandle` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardResizeHandle`, after the theme and the app configuration. |

#### `DashboardSearchButtonUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDashboardSearchButton` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyDashboardSearchButton`, after the theme and the app configuration. |
| `Kbds` (init) | `string?` | Classes merged into the `kbds` slot of `KlyDashboardSearchButton`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyDashboardSearchButton`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardSearchButton`, after the theme and the app configuration. |

#### `DashboardToolbarUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDashboardToolbar` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Center` (init) | `string?` | Classes merged into the `center` slot of `KlyDashboardToolbar`, after the theme and the app configuration. |
| `Left` (init) | `string?` | Classes merged into the `left` slot of `KlyDashboardToolbar`, after the theme and the app configuration. |
| `Right` (init) | `string?` | Classes merged into the `right` slot of `KlyDashboardToolbar`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardToolbar`, after the theme and the app configuration. |

#### `DrawerUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyDrawer` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Close` (init) | `string?` | Classes merged into the `close` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Handle` (init) | `string?` | Classes merged into the `handle` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyDrawer`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyDrawer`, after the theme and the app configuration. |

#### `DropdownMenuUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyDropdownMenu`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `ItemChevron` (init) | `string?` | The arrow at the end of an item that opens a submenu. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `ItemShortcut` (init) | `string?` | Classes merged into the `itemShortcut` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyDropdownMenu`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyDropdownMenu`, after the theme and the app configuration. |

#### `EditorDragHandleUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyEditorDragHandle` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Handle` (init) | `string?` | Classes merged into the `handle` slot of `KlyEditorDragHandle`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyEditorDragHandle`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyEditorDragHandle`, after the theme and the app configuration. |

#### `EditorEmojiItem`

`Klyroo.UI` · record

An entry of `KlyEditorEmojiMenu`.

| Constructor | Description |
| :--- | :--- |
| `EditorEmojiItem(string emoji, string name)` | Creates an emoji entry. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Defaults` | `IReadOnlyList<EditorEmojiItem>` | A few common emoji: what the emoji menu offers by default. |
| `Emoji` | `string` | The emoji inserted into the text. |
| `Keywords` (init) | `string?` | Extra words the typed text is matched against. |
| `Name` | `string` | What the user types after the colon to find it: `:smile`. |

#### `EditorMentionItem`

`Klyroo.UI` · record

An entry of `KlyEditorMentionMenu`: someone or something that can be mentioned.

| Constructor | Description |
| :--- | :--- |
| `EditorMentionItem(string id, string label)` | Creates an entry for something that can be mentioned. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Text shown under the label. |
| `Disabled` (init) | `bool` | Disables the entry. |
| `Id` | `string` | Stored in the mention as `data-id`. |
| `Label` | `string` | Shown in the menu and in the text after the @. |

#### `EditorMenuUi`

`Klyroo.UI` · record

Per-instance class overrides of the editor suggestion menus.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `Empty` (init) | `string?` | Classes merged into the `empty` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `ItemDescription` (init) | `string?` | Classes merged into the `itemDescription` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `Listbox` (init) | `string?` | Classes merged into the `listbox` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyEditorSuggestionMenu`, `KlyEditorMentionMenu` and `KlyEditorEmojiMenu`, after the theme and the app configuration. |

#### `EditorSuggestionItem`

`Klyroo.UI` · record

An entry of `KlyEditorSuggestionMenu`: a block or a format the user can insert.

| Constructor | Description |
| :--- | :--- |
| `EditorSuggestionItem(EditorAction action, string label)` | Creates an entry for an action. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Action` | `EditorAction` | What the entry inserts or applies. |
| `static Defaults` | `IReadOnlyList<EditorSuggestionItem>` | The default entries in English. The menu itself uses `EditorSuggestionItem.For` with the messages of the locale in effect. |
| `Description` (init) | `string?` | Text shown under the label. |
| `Disabled` (init) | `bool` | Disables the entry. |
| `Keywords` (init) | `string?` | Extra words the typed text is matched against. |
| `Label` | `string` | Text of the entry. |
| `Level` (init) | `int` | Heading level for `EditorAction.Heading`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static For(KlyMessages messages)` | `IReadOnlyList<EditorSuggestionItem>` | Text, headings, lists, quote, code block and divider, named and described by `messages`: what the slash menu offers by default. The keywords are markup shorthands (h1, ul, hr) and stay the same in every language. |

#### `EditorToolbarItem`

`Klyroo.UI` · record

A button of `KlyEditorToolbar`, or a separator between groups of buttons.

| Constructor | Description |
| :--- | :--- |
| `EditorToolbarItem(EditorAction action)` | Creates a button for an action. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Action` | `EditorAction` | What the button does. |
| `static BubbleDefaults` | `IReadOnlyList<EditorToolbarItem>` | Inline formatting: what a bubble toolbar shows by default. |
| `static Defaults` | `IReadOnlyList<EditorToolbarItem>` | Formatting buttons and lists, grouped: what a fixed toolbar shows by default. |
| `IsSeparator` | `bool` | True for `EditorToolbarItem.Separator`, which is a line between groups and not a button. |
| `Label` (init) | `string?` | Accessible name and tooltip. Defaults to the text of `KlyMessages` for the action. |
| `Level` (init) | `int` | Heading level for `EditorAction.Heading`. |
| `static Separator` | `EditorToolbarItem` | A thin line between two groups of buttons. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Heading(int level)` | `EditorToolbarItem` | Creates a button for a heading of the given level, 1 to 6. |

#### `EditorToolbarUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyEditorToolbar` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Button` (init) | `string?` | Classes merged into the `button` slot of `KlyEditorToolbar`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyEditorToolbar`, after the theme and the app configuration. |
| `LinkButton` (init) | `string?` | Classes merged into the `linkButton` slot of `KlyEditorToolbar`, after the theme and the app configuration. |
| `LinkForm` (init) | `string?` | Classes merged into the `linkForm` slot of `KlyEditorToolbar`, after the theme and the app configuration. |
| `LinkInput` (init) | `string?` | Classes merged into the `linkInput` slot of `KlyEditorToolbar`, after the theme and the app configuration. |
| `LinkMessage` (init) | `string?` | The explanation shown under the address field when the editor does not accept the address. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyEditorToolbar`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyEditorToolbar`, after the theme and the app configuration. |

#### `EditorUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyEditor` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | The element the editable text is drawn in. |
| `Content` (init) | `string?` | The area around the text. |
| `Fallback` (init) | `string?` | The HTML shown before the editor is ready. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyEditor`, after the theme and the app configuration. |

#### `EmptyUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyEmpty` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyEmpty`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyEmpty`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyEmpty`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyEmpty`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyEmpty`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyEmpty`, after the theme and the app configuration. |

#### `ErrorUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyError` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Actions` (init) | `string?` | Classes merged into the `actions` slot of `KlyError`, after the theme and the app configuration. |
| `Message` (init) | `string?` | Classes merged into the `message` slot of `KlyError`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyError`, after the theme and the app configuration. |
| `StatusCode` (init) | `string?` | Classes merged into the `statusCode` slot of `KlyError`, after the theme and the app configuration. |
| `StatusMessage` (init) | `string?` | Classes merged into the `statusMessage` slot of `KlyError`, after the theme and the app configuration. |

#### `FieldGroupUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyFieldGroup` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyFieldGroup`, after the theme and the app configuration. |

#### `FileUploadUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyFileUpload`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Dropzone` (init) | `string?` | Classes merged into the `dropzone` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Error` (init) | `string?` | Classes merged into the `error` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `File` (init) | `string?` | Classes merged into the `file` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `FileName` (init) | `string?` | Classes merged into the `fileName` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Files` (init) | `string?` | Classes merged into the `files` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `FileSize` (init) | `string?` | Classes merged into the `fileSize` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Remove` (init) | `string?` | Classes merged into the `remove` slot of `KlyFileUpload`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyFileUpload`, after the theme and the app configuration. |

#### `FooterColumnsUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyFooterColumns` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Column` (init) | `string?` | Classes merged into the `column` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Left` (init) | `string?` | Classes merged into the `left` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Nav` (init) | `string?` | Classes merged into the `nav` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Right` (init) | `string?` | Classes merged into the `right` slot of `KlyFooterColumns`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyFooterColumns`, after the theme and the app configuration. |

#### `FooterUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyFooter` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Bottom` (init) | `string?` | Classes merged into the `bottom` slot of `KlyFooter`, after the theme and the app configuration. |
| `Center` (init) | `string?` | Classes merged into the `center` slot of `KlyFooter`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyFooter`, after the theme and the app configuration. |
| `Left` (init) | `string?` | Classes merged into the `left` slot of `KlyFooter`, after the theme and the app configuration. |
| `Right` (init) | `string?` | Classes merged into the `right` slot of `KlyFooter`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyFooter`, after the theme and the app configuration. |
| `Top` (init) | `string?` | Classes merged into the `top` slot of `KlyFooter`, after the theme and the app configuration. |

#### `FormFieldUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyFormField`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyFormField`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyFormField`, after the theme and the app configuration. |
| `Error` (init) | `string?` | Classes merged into the `error` slot of `KlyFormField`, after the theme and the app configuration. |
| `Help` (init) | `string?` | Classes merged into the `help` slot of `KlyFormField`, after the theme and the app configuration. |
| `Hint` (init) | `string?` | Classes merged into the `hint` slot of `KlyFormField`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyFormField`, after the theme and the app configuration. |
| `LabelWrapper` (init) | `string?` | Classes merged into the `labelWrapper` slot of `KlyFormField`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyFormField`, after the theme and the app configuration. |

#### `FormUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyForm`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Fieldset` (init) | `string?` | Classes merged into the `fieldset` slot of `KlyForm`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyForm`, after the theme and the app configuration. |
| `Summary` (init) | `string?` | Classes merged into the `summary` slot of `KlyForm`, after the theme and the app configuration. |
| `SummaryList` (init) | `string?` | Classes merged into the `summaryList` slot of `KlyForm`, after the theme and the app configuration. |

#### `HeaderUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyHeader` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Center` (init) | `string?` | Classes merged into the `center` slot of `KlyHeader`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyHeader`, after the theme and the app configuration. |
| `Left` (init) | `string?` | Classes merged into the `left` slot of `KlyHeader`, after the theme and the app configuration. |
| `Menu` (init) | `string?` | Classes merged into the `menu` slot of `KlyHeader`, after the theme and the app configuration. |
| `Right` (init) | `string?` | Classes merged into the `right` slot of `KlyHeader`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyHeader`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyHeader`, after the theme and the app configuration. |

#### `IconUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyIcon` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyIcon`, after the theme and the app configuration. |

#### `InfiniteScrollUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyInfiniteScroll` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `End` (init) | `string?` | Classes merged into the `end` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |
| `Marker` (init) | `string?` | Classes merged into the `marker` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |
| `Sentinel` (init) | `string?` | Classes merged into the `sentinel` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |
| `Spinner` (init) | `string?` | Classes merged into the `spinner` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |
| `Status` (init) | `string?` | Classes merged into the `status` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyInfiniteScroll`, after the theme and the app configuration. |

#### `InputDateUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInputDate`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyInputDate`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyInputDate`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInputDate`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyInputDate`, after the theme and the app configuration. |
| `TriggerButton` (init) | `string?` | Classes merged into the `triggerButton` slot of `KlyInputDate`, after the theme and the app configuration. |
| `TriggerIcon` (init) | `string?` | Classes merged into the `triggerIcon` slot of `KlyInputDate`, after the theme and the app configuration. |

#### `InputMenuUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInputMenu`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Announcer` (init) | `string?` | Classes merged into the `announcer` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Empty` (init) | `string?` | Classes merged into the `empty` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Error` (init) | `string?` | Classes merged into the `error` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `GroupLabel` (init) | `string?` | Classes merged into the `groupLabel` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `ItemCount` (init) | `string?` | Classes merged into the `itemCount` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `ItemLeadingIcon` (init) | `string?` | Classes merged into the `itemLeadingIcon` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Listbox` (init) | `string?` | Classes merged into the `listbox` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Loading` (init) | `string?` | Classes merged into the `loading` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Scroller` (init) | `string?` | Classes merged into the `scroller` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Spinner` (init) | `string?` | Classes merged into the `spinner` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `Trailing` (init) | `string?` | Classes merged into the `trailing` slot of `KlyInputMenu`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlyInputMenu`, after the theme and the app configuration. |

#### `InputNumberUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInputNumber`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyInputNumber`, after the theme and the app configuration. |
| `Decrement` (init) | `string?` | Classes merged into the `decrement` slot of `KlyInputNumber`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyInputNumber`, after the theme and the app configuration. |
| `Increment` (init) | `string?` | Classes merged into the `increment` slot of `KlyInputNumber`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInputNumber`, after the theme and the app configuration. |

#### `InputRatingUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInputRating`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Fill` (init) | `string?` | Classes merged into the `fill` slot of `KlyInputRating`, after the theme and the app configuration. |
| `FilledIcon` (init) | `string?` | Classes merged into the `filledIcon` slot of `KlyInputRating`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyInputRating`, after the theme and the app configuration. |
| `Input` (init) | `string?` | Classes merged into the `input` slot of `KlyInputRating`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInputRating`, after the theme and the app configuration. |
| `Star` (init) | `string?` | Classes merged into the `star` slot of `KlyInputRating`, after the theme and the app configuration. |

#### `InputTagsUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInputTags`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Input` (init) | `string?` | Classes merged into the `input` slot of `KlyInputTags`, after the theme and the app configuration. |
| `Remove` (init) | `string?` | Classes merged into the `remove` slot of `KlyInputTags`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInputTags`, after the theme and the app configuration. |
| `Tag` (init) | `string?` | Classes merged into the `tag` slot of `KlyInputTags`, after the theme and the app configuration. |
| `Tags` (init) | `string?` | Classes merged into the `tags` slot of `KlyInputTags`, after the theme and the app configuration. |

#### `InputTimeUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInputTime`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInputTime`, after the theme and the app configuration. |
| `Segment` (init) | `string?` | Classes merged into the `segment` slot of `KlyInputTime`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyInputTime`, after the theme and the app configuration. |

#### `InputUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyInput`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyInput`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyInput`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyInput`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyInput`, after the theme and the app configuration. |
| `Trailing` (init) | `string?` | Classes merged into the `trailing` slot of `KlyInput`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlyInput`, after the theme and the app configuration. |

#### `KbdUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyKbd` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyKbd`, after the theme and the app configuration. |

#### `KlyAccordionItem`

`Klyroo.UI` · record

An item of `KlyAccordion`: a header label and a content that opens and closes.

| Constructor | Description |
| :--- | :--- |
| `KlyAccordionItem(string Label)` | An item of `KlyAccordion`: a header label and a content that opens and closes. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `RenderFragment?` | Content of the panel that opens under the header. |
| `Disabled` (init) | `bool` | Prevents the user from opening or closing the item. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Label` (init) | `string` | Text of the header button of the item. |
| `Value` (init) | `string?` | Identifies the item in `KlyAccordion.Value`. Defaults to its index. |

#### `KlyAuthField`

`Klyroo.UI` · record

A field of a `KlyAuthForm`. `KlyAuthField.Type` is `checkbox`, `select` (give `KlyAuthField.Items`), `otp` (a one-time code, see `KlyAuthField.Length`), `password` (with a button that shows it) or any `type` of an `input`: `text`, `email`, `tel`...

| Constructor | Description |
| :--- | :--- |
| `KlyAuthField(string Name, string Type = "text")` | A field of a `KlyAuthForm`. `KlyAuthField.Type` is `checkbox`, `select` (give `KlyAuthField.Items`), `otp` (a one-time code, see `KlyAuthField.Length`), `password` (with a button that shows it) or any `type` of an `input`: `text`, `email`, `tel`... |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Autocomplete` (init) | `string?` | The `autocomplete` token of the control: `email`, `current-password`, `new-password`... |
| `Description` (init) | `string?` | Text under the label. |
| `Help` (init) | `string?` | Text under the control while there is no error. |
| `Items` (init) | `IReadOnlyList<KlySelectItem>` | The options of a `select`. |
| `Label` (init) | `string?` | Label of the field. Defaults to the name of the field. |
| `Length` (init) | `int` | Number of characters of an `otp`. |
| `Name` (init) | `string` | Name of the field: the `name` of the control, and the key of its value in the submitted values. |
| `Placeholder` (init) | `string?` | Hint shown while the field is empty. |
| `Required` (init) | `bool` | Marks the field as required. |
| `Type` (init) | `string` | Kind of the field: `text` by default, or `checkbox`, `select`, `otp`, `password`, or any `type` of an `input`. |
| `Value` (init) | `string?` | The initial value. A checkbox is checked when it is `"true"`. |

#### `KlyAuthProvider`

`Klyroo.UI` · record

A button of a `KlyAuthForm` that leaves for another address, such as an OAuth provider.

| Constructor | Description |
| :--- | :--- |
| `KlyAuthProvider(string Label, string Href)` | A button of a `KlyAuthForm` that leaves for another address, such as an OAuth provider. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Href` (init) | `string` | Address the button leaves for. |
| `Icon` (init) | `string?` | Name of a built-in icon (see `KlyIcon`). |
| `IconContent` (init) | `RenderFragment?` | A custom icon, instead of `KlyAuthProvider.Icon`. |
| `Label` (init) | `string` | Text of the button, such as "Continue with GitHub". |

#### `KlyAvatarItem`

`Klyroo.UI` · record

One avatar of a `KlyAvatarGroup`, with the parameters of `KlyAvatar`.

| Constructor | Description |
| :--- | :--- |
| `KlyAvatarItem(string? Src = null, string? Alt = null, string? Text = null)` | One avatar of a `KlyAvatarGroup`, with the parameters of `KlyAvatar`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Alt` (init) | `string?` | Alternative text of the picture, which also names the avatar. |
| `Src` (init) | `string?` | Address of the picture. |
| `Text` (init) | `string?` | Initials shown when there is no picture. |

#### `KlyBreadcrumbItem`

`Klyroo.UI` · record

A page of the trail of a `KlyBreadcrumb`.

| Constructor | Description |
| :--- | :--- |
| `KlyBreadcrumbItem(string Label)` | A page of the trail of a `KlyBreadcrumb`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Href` (init) | `string?` | Makes the item a link. The last item is the current page whether it has one or not. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Label` (init) | `string` | Text of the item. |

#### `KlyCarouselItem`

`Klyroo.UI` · record

A slide of a `KlyCarousel`.

| Constructor | Description |
| :--- | :--- |
| `KlyCarouselItem(RenderFragment? Content)` | A slide of a `KlyCarousel`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `RenderFragment?` | Content of the slide. |
| `Label` (init) | `string?` | Accessible name of the slide. Defaults to "1 of 3". |

#### `KlyChartCategory<TItem>`

`Klyroo.UI` · record

One series of a chart: where its value comes from in a row, and how it is named and colored.

| Constructor | Description |
| :--- | :--- |
| `KlyChartCategory<TItem>(string Key, Func<TItem, double?> Value)` | One series of a chart: where its value comes from in a row, and how it is named and colored. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Color` (init) | `KlyColor?` | The color of the series, through the `--kly-color` tokens. Defaults to the chart's palette, in order. |
| `Key` (init) | `string` | Identifies the series. Also its name when there is no `KlyChartCategory.Label`. |
| `Label` (init) | `string?` | The name shown in the legend, the tooltip and the table. Defaults to `KlyChartCategory.Key`. |
| `Value` (init) | `Func<TItem, double?>` | The value of the series in a row. `null` leaves a gap. |

#### `KlyChartSelectEventArgs<TItem, TIndex>`

`Klyroo.UI` · record

What `KlyChart.OnSelect` reports: the row of the bar, point or slice that was clicked or chosen with Enter or Space.

| Constructor | Description |
| :--- | :--- |
| `KlyChartSelectEventArgs<TItem, TIndex>(int Row, TIndex Index, string? SeriesKey, int SeriesIndex, double? Value, TItem Item)` | What `KlyChart.OnSelect` reports: the row of the bar, point or slice that was clicked or chosen with Enter or Space. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Index` (init) | `TIndex` | The value of the row on the x axis (`KlyChart.Index`): a date, a number, a label. The label of the slice with a donut. |
| `Item` (init) | `TItem` | The row of `KlyChart.Data`. |
| `Row` (init) | `int` | The position of the row in `KlyChart.Data`. |
| `SeriesIndex` (init) | `int` | The position of that series in `KlyChart.Categories`, or -1 when the chart has none or the pick named none. |
| `SeriesKey` (init) | `string?` | The `KlyChartCategory.Key` of the series that was picked (the only category of a donut or of a chart with one series), or null when the chart has none or the pick named none (the keyboard on a chart with several series: the handler chooses from `Item`). |
| `Value` (init) | `double?` | The value of that series in the row, or null for a gap or when no series was named. |

#### `KlyCheckboxItem<TValue>`

`Klyroo.UI` · record

A checkbox of a `KlyCheckboxGroup`.

| Constructor | Description |
| :--- | :--- |
| `KlyCheckboxItem<TValue>(TValue Value, string Label)` | A checkbox of a `KlyCheckboxGroup`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Text under the label. |
| `Disabled` (init) | `bool` | Disables the checkbox. |
| `Label` (init) | `string` | Text next to the checkbox. |
| `Value` (init) | `TValue` | Value added to the selection when the checkbox is checked. |

#### `KlyCommandGroup`

`Klyroo.UI` · record

A headed set of commands. Without a `KlyCommandGroup.Label` the commands are listed with no heading.

| Constructor | Description |
| :--- | :--- |
| `KlyCommandGroup(string? Label, IReadOnlyList<KlyCommandItem> Items)` | A headed set of commands. Without a `KlyCommandGroup.Label` the commands are listed with no heading. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Items` (init) | `IReadOnlyList<KlyCommandItem>` | The commands of the group. |
| `Label` (init) | `string?` | Heading of the group. Null lists the commands with no heading. |

#### `KlyCommandItem`

`Klyroo.UI` · record

A command of `KlyCommandPalette`: an action, or a link when `KlyCommandItem.Href` is set.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Count` (init) | `int?` | A number shown at the end of the row, before the shortcut (matches, unread, items): formatted for the locale and not searched. |
| `Description` (init) | `string?` | Text shown under the label. |
| `Disabled` (init) | `bool` | Disables the command: it is listed but cannot be activated. |
| `Href` (init) | `string?` | Address to go to. When set the command is a link. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Keywords` (init) | `string?` | Extra words the search matches, that are not shown. |
| `Label` (init) | `string` | Text of the command. |
| `Shortcut` (init) | `string?` | Keyboard shortcut shown at the end of the row, such as `Ctrl+K`. |
| `Value` (init) | `string?` | Identifies the command in `KlyCommandPalette.OnSelect`. Defaults to the label. |

#### `KlyContentNavigationItem`

`Klyroo.UI` · record

An entry of `KlyContentNavigation`: a page link, or a group of entries when it has children.

| Constructor | Description |
| :--- | :--- |
| `KlyContentNavigationItem(string Title)` | An entry of `KlyContentNavigation`: a page link, or a group of entries when it has children. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Active` (init) | `bool?` | Forces the current state instead of comparing `KlyContentNavigationItem.Href` with the current address. |
| `Badge` (init) | `string?` | A short label after the title: "New", a count. |
| `BadgeColor` (init) | `KlyColor?` | Semantic color of the badge. Defaults to the color of the navigation. |
| `Children` (init) | `IReadOnlyList<KlyContentNavigationItem>?` | The entries below this one. They form a collapsible group. |
| `Disabled` (init) | `bool` | Disables the entry: it is listed but cannot be followed. |
| `Exact` (init) | `bool` | The entry is current only on exactly its address; otherwise it is also current on the pages below it. |
| `Href` (init) | `string?` | The page the entry links to. An entry with children is a group and has no link of its own. |
| `Icon` (init) | `string?` | A built-in Lucide icon name ("book-open", "file"), shown before the title. |
| `Open` (init) | `bool?` | Initial state of a group. By default a group is open when it holds the current page. |
| `Target` (init) | `string?` | The `target` attribute of the link, such as `_blank`. |
| `Title` (init) | `string` | Text of the entry. |

#### `KlyContentSearchHeading`

`Klyroo.UI` · record

A heading of a `KlyContentSearchPage`.

| Constructor | Description |
| :--- | :--- |
| `KlyContentSearchHeading(string Text, string Id)` | A heading of a `KlyContentSearchPage`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` (init) | `string` | The `id` of the heading element, used as the fragment of the address. |
| `Text` (init) | `string` | Text of the heading. |

#### `KlyContentSearchPage`

`Klyroo.UI` · record

A page `KlyContentSearch` can find, and optionally the headings inside it.

| Constructor | Description |
| :--- | :--- |
| `KlyContentSearchPage(string Title, string Href)` | A page `KlyContentSearch` can find, and optionally the headings inside it. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | A line of context shown under the title. |
| `Group` (init) | `string?` | The section the page is listed under in the results. Pages without one have no heading. |
| `Headings` (init) | `IReadOnlyList<KlyContentSearchHeading>?` | Headings of the page, searchable on their own and linking to `Href#Id`. |
| `Href` (init) | `string` | Address of the page. |
| `Icon` (init) | `string?` | A built-in Lucide icon name, shown before the page. Defaults to a file; an empty string shows none. |
| `Keywords` (init) | `string?` | Extra words the search matches, that are not shown. |
| `Title` (init) | `string` | Title of the page. |

#### `KlyContentSurroundLink`

`Klyroo.UI` · record

A page linked by `KlyContentSurround`.

| Constructor | Description |
| :--- | :--- |
| `KlyContentSurroundLink(string Title, string Href)` | A page linked by `KlyContentSurround`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | A line of context shown under the title. |
| `Href` (init) | `string` | Address of the page. |
| `Title` (init) | `string` | Title of the page. |

#### `KlyContentTocLink`

`Klyroo.UI` · record

A heading of the page, listed by `KlyContentToc`.

| Constructor | Description |
| :--- | :--- |
| `KlyContentTocLink(string Id, string Text)` | A heading of the page, listed by `KlyContentToc`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` (init) | `string` | The `id` of the heading element. The link points at it and the scroll-spy follows it. |
| `Level` (init) | `int` | The heading level, 2 for an `h2`. Deeper levels are indented. |
| `Text` (init) | `string` | The text of the link. |

#### `KlyDateRange`

`Klyroo.UI` · record

A range of dates. `KlyDateRange.End` is null while only the start has been chosen.

| Constructor | Description |
| :--- | :--- |
| `KlyDateRange(DateOnly? Start, DateOnly? End)` | A range of dates. `KlyDateRange.End` is null while only the start has been chosen. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `End` (init) | `DateOnly?` | Last date of the range, or null while only the start has been chosen. |
| `Start` (init) | `DateOnly?` | First date of the range, or null when none was chosen. |

#### `KlyFooterColumn`

`Klyroo.UI` · record

A column of a `KlyFooterColumns`: a heading and its links. Only the label, href, target, icon and disabled state of a link are used.

| Constructor | Description |
| :--- | :--- |
| `KlyFooterColumn(string Label)` | A column of a `KlyFooterColumns`: a heading and its links. Only the label, href, target, icon and disabled state of a link are used. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Label` (init) | `string` | Heading of the column. |
| `Links` (init) | `IReadOnlyList<KlyNavigationItem>` | The links of the column. |

#### `KlyImageModifiers`

`Klyroo.UI` · record

What a provider is asked to produce for one URL: the source resized, converted and compressed as described.

| Constructor | Description |
| :--- | :--- |
| `KlyImageModifiers(int? Width = null, int? Height = null, int? Quality = null, string? Format = null, KlyImageFit? Fit = null, int? Blur = null, IReadOnlyDictionary<string, string>? Extra = null)` | What a provider is asked to produce for one URL: the source resized, converted and compressed as described. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Blur` (init) | `int?` | Blur radius for placeholders. Only asked from providers that report `IKlyImageProvider.SupportsBlur`. |
| `Extra` (init) | `IReadOnlyDictionary<string, string>?` | Provider specific modifiers, from the `Modifiers` parameter (for example `"gravity"` or `"sharpen"`). A provider ignores the ones it does not know. |
| `Fit` (init) | `KlyImageFit?` | How to fill `Width` by `Height`; null means the provider default (`KlyImageFit.Cover`). |
| `Format` (init) | `string?` | "webp", "avif", "jpeg", "png", "gif" or "svg"; null keeps the format of the source (or lets the provider negotiate). |
| `Height` (init) | `int?` | Pixels, or null. |
| `Quality` (init) | `int?` | 1 to 100, or null for the provider default. |
| `Width` (init) | `int?` | Pixels, or null to keep the width (the ratio is kept when only one side is given). |

#### `KlyImageOptions`

`Klyroo.UI` · record

Image settings that a component forwards to `KlyImage` (`:image="{ src, format: 'webp', quality: 80 }"`). The same shape describes the entries of `KlyImageSettings.Presets`. Every value left null falls through to the preset, then to the global defaults.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Densities` (init) | `string?` | Pixel densities of the `srcset`, like `"x1 x2"` (the default is `KlyImageSettings.Densities`). |
| `Fit` (init) | `KlyImageFit?` | How the image fills its width and height (cover when null). |
| `Format` (init) | `string?` | Output format: "webp", "avif", "jpeg", "png", "gif" or "svg". |
| `Loading` (init) | `KlyImageLoading?` | Lazy by default, eager when `KlyImageOptions.Preload` is set. |
| `Modifiers` (init) | `IReadOnlyDictionary<string, string>?` | Provider specific modifiers, merged key by key over the preset's. |
| `Preload` (init) | `bool?` | Adds a `<link rel="preload" as="image">` to the head. Needs a `HeadOutlet` in the app. |
| `Preset` (init) | `string?` | Name of an entry of `KlyImageSettings.Presets`, applied below the values set here. |
| `Provider` (init) | `string?` | Name of a registered `IKlyImageProvider`; the default provider when null. |
| `Quality` (init) | `int?` | Quality from 1 to 100. |
| `Sizes` (init) | `string?` | Responsive sizes: space-separated entries, an unprefixed one for the default and prefixed ones for the breakpoints of `KlyImageSettings.Screens`, each applying from its breakpoint upward: `"100vw sm:50vw md:400px"`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `OverDefaultsOf(KlyImageOptions? fallback)` | `KlyImageOptions` | The values of this instance, with the ones it leaves null taken from `fallback`. `KlyImageOptions.Modifiers` are merged key by key. |

#### `KlyLocale`

`Klyroo.UI` · record

A language of the UI: its code, its name, its writing direction and every text the components render. Locale definition for component labels and text direction. Use a built-in one from `KlyLocales`, derive one with `KlyLocales.Extend`, or create it with `new KlyLocale(...)`.

| Constructor | Description |
| :--- | :--- |
| `KlyLocale(string Code, string Name, KlyDirection Dir, KlyMessages Messages)` | A language of the UI: its code, its name, its writing direction and every text the components render. Locale definition for component labels and text direction. Use a built-in one from `KlyLocales`, derive one with `KlyLocales.Extend`, or create it with `new KlyLocale(...)`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` (init) | `string` | A culture name such as "fr" or "pt-BR". It is the value written to the culture cookie by `KlyLocaleSelect`, the `lang` attribute of the page, and the culture used to format dates and numbers. |
| `Culture` | `CultureInfo` | The culture of `KlyLocale.Code`, used by date, number and file size formatting when a component has no explicit `Culture`. A code the runtime does not know gives the invariant culture. |
| `Dir` (init) | `KlyDirection` | The writing direction. |
| `DirAttribute` | `string` | The value of the `dir` attribute: "ltr" or "rtl". |
| `Flag` (init) | `string?` | Shown before the name by `KlyLocaleSelect`: a flag emoji, or any short text. Some systems (Windows) draw flag emoji as letters. |
| `Messages` (init) | `KlyMessages` | The texts of the components. |
| `Name` (init) | `string` | The name of the language in that language ("Français"), shown by `KlyLocaleSelect`. |

#### `KlyMenuItem`

`Klyroo.UI` · record

An entry of `KlyDropdownMenu`: an action, a link (with `KlyMenuItem.Href`), a heading or a separator.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Children` (init) | `IReadOnlyList<KlyMenuItem>?` | Items of a submenu. An item with children opens a nested menu (on the right arrow key, Enter, a click or a pause of the pointer) instead of being selected, so `KlyMenuItem.Href` and `KlyDropdownMenu.OnSelect` do not apply to it. Only `KlyDropdownMenu` shows submenus: `KlyContextMenu` renders such an item as a plain item. |
| `Color` (init) | `KlyColor?` | "error" for destructive actions. |
| `Description` (init) | `string?` | A hint for the item, such as why it is disabled: it is the title (the tooltip) of the entry and its accessible description. Not shown otherwise. |
| `Disabled` (init) | `bool` | Disables the item: it is listed but cannot be activated. |
| `Href` (init) | `string?` | Address to go to. When set the item is a link. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Kind` (init) | `KlyMenuItemKind` | Whether the entry is an item, a separator or a heading. Defaults to an item. |
| `Label` (init) | `string` | Text of the item or of the heading. |
| `Shortcut` (init) | `string?` | Keyboard shortcut shown at the end of the row, such as `Ctrl+S`. |
| `Value` (init) | `string?` | Identifies the item in `KlyDropdownMenu.OnSelect`. Defaults to the label. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Heading(string label)` | `KlyMenuItem` | Creates a heading that names the group of items below it. |
| `static Separator()` | `KlyMenuItem` | Creates a separator between two groups of items. |

#### `KlyMessages`

`Klyroo.UI` · record

Every text the components render: accessible names, labels, placeholders, announcements. A `KlyLocale` carries one set; build a variant with a `with` expression (`KlyLocales.En.Messages with { Close = "Dismiss" }`).

| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthFormIncompleteCode` (init) | `string` | Error of an incomplete one-time code; `{0}` is the number of characters. |
| `AuthFormInvalidEmail` (init) | `string` | Error that `KlyAuthForm` shows when an email field holds an address that is not valid. |
| `AuthFormRequired` (init) | `string` | Error of an empty required field of an auth form; `{0}` is the field label. |
| `AuthFormSeparator` (init) | `string` | Text between the providers and the fields of an auth form, unless `Separator` is set. |
| `AuthFormShowPassword` (init) | `string` | Accessible name of the button of an auth form that shows or hides a password. |
| `AuthFormSubmit` (init) | `string` | Label of the submit button of an auth form that has none. |
| `Breadcrumb` (init) | `string` | Accessible name of the navigation landmark of `KlyBreadcrumb`. |
| `Brightness` (init) | `string` | Accessible name of the brightness control of `KlyColorPicker`. |
| `Cancel` (init) | `string` | Label of the cancel button of the dialog opened by `IKlyOverlayService.ConfirmAsync`. |
| `CarouselRoleDescription` (init) | `string` | Spoken by screen readers for the carousel itself (`aria-roledescription`). |
| `CarouselSlideRoleDescription` (init) | `string` | Spoken by screen readers for each slide (`aria-roledescription`). |
| `ChartArea` (init) | `string` | Accessible name of an area chart (`KlyChart`), read before its generated description. |
| `ChartBar` (init) | `string` | Accessible name of a bar chart (`KlyChart`), read before its generated description. |
| `ChartBarHorizontal` (init) | `string` | Accessible name of a horizontal bar chart (`KlyChart`), read before its generated description. |
| `ChartDonut` (init) | `string` | Accessible name of a donut chart (`KlyChart`), read before its generated description. |
| `ChartHint` (init) | `string` | Accessible name of a chart that can be explored with the keyboard. |
| `ChartHintSelect` (init) | `string` | Added to `KlyMessages.ChartHint` when the chart reports a selection (`OnSelect`): how to choose the point the arrow keys are on. |
| `ChartIndex` (init) | `string` | Header of the first column of the data table, when the chart has no x axis title. |
| `ChartLegend` (init) | `string` | Accessible name of the legend. |
| `ChartLine` (init) | `string` | Kind of chart, used in the generated description. |
| `ChartRange` (init) | `string` | One series in the description. {0} name, {1} lowest value, {2} highest value. |
| `ChartSlice` (init) | `string` | One slice in the description. {0} name, {1} value, {2} share. |
| `ChartSummary` (init) | `string` | Generated description of a line, area or bar chart. {0} kind, {1} number of points, {2} first, {3} last, {4} the series (each formatted with `KlyMessages.ChartRange`). |
| `ChartSummaryDonut` (init) | `string` | Generated description of a donut chart. {0} kind, {1} the slices (each formatted with `KlyMessages.ChartSlice`). |
| `ChartTable` (init) | `string` | Summary of the disclosure that holds the chart's data table. |
| `ChatAssistant` (init) | `string` | Name of the author of an assistant message in `KlyChatMessage`, used as the accessible name of its avatar. |
| `ChatAttachmentFailed` (init) | `string` | Word that `KlyChatAttachment` shows when the upload of a file failed. |
| `ChatAttachmentUploading` (init) | `string` | Read out after the name of an attachment that is uploading or failed. |
| `ChatConversation` (init) | `string` | Accessible name of the list of messages of a conversation. |
| `ChatError` (init) | `string` | Shown under the messages when the last request failed. |
| `ChatJumpToLatest` (init) | `string` | Text of the button that scrolls a conversation back to its latest message. |
| `ChatPromptLabel` (init) | `string` | Accessible name and placeholder of the message field of a chat prompt. |
| `ChatPromptPlaceholder` (init) | `string` | Placeholder of the text area of `KlyChatPrompt`. |
| `ChatRemoveAttachment` (init) | `string` | Accessible name of the button that removes an attachment: {0} is the file name. |
| `ChatResponding` (init) | `string` | Read out while an answer is pending or arriving. |
| `ChatRetry` (init) | `string` | Label of the retry button of `KlyChatPromptSubmit`, shown after an error. |
| `ChatSend` (init) | `string` | Accessible names of the submit button of a chat prompt: send, stop the answer, retry after an error. |
| `ChatStop` (init) | `string` | Label of the stop button of `KlyChatPromptSubmit`, shown while an answer is on its way. |
| `ChatSystem` (init) | `string` | Name of the author of a system message in `KlyChatMessage`, used as the accessible name of its avatar. |
| `ChatThinking` (init) | `string` | Label of a reasoning block while it streams, and when it is done without a known duration. |
| `ChatThought` (init) | `string` | Title of `KlyChatReasoning` once the reasoning has finished. |
| `ChatThoughtFor` (init) | `string` | Label of a finished reasoning block: {0} is the number of seconds. |
| `ChatToolFailed` (init) | `string` | Word that `KlyChatTool` adds to its name when the tool call failed. |
| `ChatToolRunning` (init) | `string` | Read out after the name of a tool call that is running or failed. |
| `ChatUser` (init) | `string` | Accessible names of the authors of a message. |
| `ChooseDate` (init) | `string` | Accessible name of the button of `KlyInputDate` that opens its calendar. |
| `Close` (init) | `string` | Accessible name of the close button of alerts, banners, modals, drawers, slideovers, toasts, tags, chat attachments and the other dismissible components. |
| `CollapseSidebar` (init) | `string` | Name of the button that collapses a sidebar to icons; it is pressed while the sidebar is collapsed. |
| `ColorMode` (init) | `string` | Accessible name of `KlyColorModeSelect`. |
| `ColorModeDark` (init) | `string` | Label of the dark option of `KlyColorModeSelect`. |
| `ColorModeLight` (init) | `string` | Label of the light option of `KlyColorModeSelect`. |
| `ColorModeSystem` (init) | `string` | Label of the option of `KlyColorModeSelect` that follows the system setting. |
| `CommandResults` (init) | `string` | Announced to screen readers after filtering; `{0}` is the number of results. |
| `Confirm` (init) | `string` | Label of the confirm button of the dialog opened by `IKlyOverlayService.ConfirmAsync`. |
| `ContentNavigation` (init) | `string` | Accessible name of a `KlyContentNavigation` that has no `AriaLabel`. |
| `ContentNext` (init) | `string` | Caption of the link to the next page. |
| `ContentPrevious` (init) | `string` | Caption of the link to the previous page. |
| `ContentSearch` (init) | `string` | Accessible name of the documentation search dialog and its search field. |
| `ContentSearchButton` (init) | `string` | Text of the button that opens the documentation search. |
| `ContentSearchError` (init) | `string` | Shown when the search index could not be fetched. |
| `ContentSearchLoading` (init) | `string` | Shown while the search index of a `KlyContentSearch` with an `IndexUrl` is being fetched. |
| `ContentSearchPlaceholder` (init) | `string` | Placeholder of the documentation search field. |
| `ContentSearchRetry` (init) | `string` | Text of the button that fetches the search index again after an error. |
| `ContentSurround` (init) | `string` | Accessible name of the previous and next page links. |
| `ContentToc` (init) | `string` | Title of a `KlyContentToc` that has no `Title`. |
| `ContextMenu` (init) | `string` | Accessible name of the menu of `KlyContextMenu`. |
| `DashboardSearch` (init) | `string` | Accessible name of the dashboard search dialog and its search field. |
| `DashboardSearchButton` (init) | `string` | Text of the button that opens the dashboard search. |
| `DashboardSearchPlaceholder` (init) | `string` | Placeholder of the dashboard search field. |
| `DayPeriod` (init) | `string` | Accessible name of the AM/PM segment of `KlyInputTime`. |
| `DropFiles` (init) | `string` | Text of the drop zone of `KlyFileUpload`. |
| `EditorAddressRefused` (init) | `string` | Explains why the editor refused an address when no list of schemes is known. |
| `EditorBlockquote` (init) | `string` | Label of the quote button of the editor toolbar and of its slash menu entry. |
| `EditorBold` (init) | `string` | Label of the bold button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorBulletList` (init) | `string` | Label of the bulleted list button of the editor toolbar and of its slash menu entry. |
| `EditorClearFormatting` (init) | `string` | Label of the button of the editor toolbar that removes the formatting of the selection. |
| `EditorCode` (init) | `string` | Label of the inline code button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorCodeBlock` (init) | `string` | Label of the code block button of the editor toolbar and of its slash menu entry. |
| `EditorDescriptionBlockquote` (init) | `string` | Description of the quote entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionBulletList` (init) | `string` | Description of the bulleted list entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionCodeBlock` (init) | `string` | Description of the code block entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionHeading1` (init) | `string` | Description of the level 1 heading entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionHeading2` (init) | `string` | Description of the level 2 heading entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionHeading3` (init) | `string` | Description of the level 3 heading entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionHorizontalRule` (init) | `string` | Description of the divider entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionOrderedList` (init) | `string` | Description of the numbered list entry of the slash menu of `KlyEditor`. |
| `EditorDescriptionParagraph` (init) | `string` | Description of the paragraph entry of the slash menu of `KlyEditor`. |
| `EditorDragHandle` (init) | `string` | Accessible name of the button that drags a block (it also moves with Alt and the arrow keys). |
| `EditorEmojiMenu` (init) | `string` | Accessible name of the list of the emoji menu. |
| `EditorHeading` (init) | `string` | Label of a heading button; `{0}` is the level. |
| `EditorHorizontalRule` (init) | `string` | Label of the divider button of the editor toolbar and of its slash menu entry. |
| `EditorImage` (init) | `string` | Label of the image button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorImageUrl` (init) | `string` | Accessible name of the field that takes the address of an image (the button is `KlyMessages.EditorLinkApply`). |
| `EditorItalic` (init) | `string` | Label of the italic button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorLabel` (init) | `string` | Accessible name of the editable area when no label is given. |
| `EditorLink` (init) | `string` | Label of the link button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorLinkApply` (init) | `string` | Label of the button of the link and image address form of `KlyEditorToolbar` that applies the address. |
| `EditorLinkRemove` (init) | `string` | Label of the button of the link form of `KlyEditorToolbar` that removes the link. |
| `EditorLinkUrl` (init) | `string` | Accessible name of the field that takes the address of a link. |
| `EditorMentionMenu` (init) | `string` | Accessible name of the list of the mention menu. |
| `EditorMoveDown` (init) | `string` | Accessible name of the action that moves the current block of the editor down. |
| `EditorMoveUp` (init) | `string` | Accessible name of the action that moves the current block of the editor up. |
| `EditorNoResults` (init) | `string` | Text shown by the slash, mention and emoji menus of `KlyEditor` when nothing matches what was typed. |
| `EditorOrderedList` (init) | `string` | Label of the numbered list button of the editor toolbar and of its slash menu entry. |
| `EditorParagraph` (init) | `string` | Label of the paragraph button of the editor toolbar and of its slash menu entry. |
| `EditorRedo` (init) | `string` | Label of the redo button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorSchemesOnly` (init) | `string` | Explains why the editor refused an address when it takes only some schemes; `{0}` lists them ("https, http, mailto"). |
| `EditorStrike` (init) | `string` | Label of the strikethrough button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorSuggestionMenu` (init) | `string` | Accessible name of the list of the slash menu. |
| `EditorSuggestionText` (init) | `string` | Name of the plain text entry of the slash menu. |
| `EditorTaskItem` (init) | `string` | Accessible name of the checkbox of a task item; `{0}` is the text of the item. |
| `EditorTaskList` (init) | `string` | Label of the task list button of the editor toolbar. |
| `EditorToolbar` (init) | `string` | Accessible name of the editor toolbar. |
| `EditorUnderline` (init) | `string` | Label of the underline button of the editor toolbar (`KlyEditorToolbar`). |
| `EditorUndo` (init) | `string` | Label of the undo button of the editor toolbar (`KlyEditorToolbar`). |
| `ErrorBackHome` (init) | `string` | Label of the link of `KlyError` that goes back to the home page. |
| `ErrorForbidden` (init) | `string` | Title of `KlyError` for status 403. |
| `ErrorForbiddenMessage` (init) | `string` | Message of `KlyError` for status 403. |
| `ErrorNotFound` (init) | `string` | Title of `KlyError` for status 404. |
| `ErrorNotFoundMessage` (init) | `string` | Message of `KlyError` for status 404. |
| `ErrorServer` (init) | `string` | Title of `KlyError` for status 500 and any other status without its own text. |
| `ErrorServerMessage` (init) | `string` | Message of `KlyError` for status 500 and any other status without its own text. |
| `ErrorUnauthorized` (init) | `string` | Title of `KlyError` for status 401. |
| `ErrorUnauthorizedMessage` (init) | `string` | Message of `KlyError` for status 401. |
| `FileNotAccepted` (init) | `string` | Error of `KlyFileUpload` for a file whose type is not accepted; `{0}` is the file name. |
| `FileSizeBytes` (init) | `string` | A size in bytes (`{0}` is the number, formatted for the culture of the locale). |
| `FileSizeGigabytes` (init) | `string` | A size in gigabytes. |
| `FileSizeKilobytes` (init) | `string` | A size in kilobytes. |
| `FileSizeMegabytes` (init) | `string` | A size in megabytes. |
| `FileTooLarge` (init) | `string` | Error of `KlyFileUpload` for a file over the size limit; `{0}` is the file name and `{1}` the limit. |
| `FirstPage` (init) | `string` | Accessible name of the first page button of `KlyPagination`. |
| `FooterLinks` (init) | `string` | Accessible name of the link columns of a footer. |
| `FormSummaryTitle` (init) | `string` | Title of the list of validation errors that `KlyForm` shows when `ShowSummary` is on. |
| `GoToSlide` (init) | `string` | Accessible name of a slide dot of `KlyCarousel`; `{0}` is the slide number. |
| `HeaderMenu` (init) | `string` | Title of the mobile menu panel of a header, and name of its navigation. |
| `HeaderNavigation` (init) | `string` | Accessible name of the site navigation of a header. |
| `HexColor` (init) | `string` | Accessible name of the hex field of `KlyColorPicker`. |
| `Hours` (init) | `string` | Accessible name of the hours segment of `KlyInputTime`. |
| `Hue` (init) | `string` | Accessible name of the hue slider of `KlyColorPicker`. |
| `InputMenuNoResults` (init) | `string` | Text shown by `KlyInputMenu` when no suggestion matches what was typed. |
| `InputMenuSuggestions` (init) | `string` | Accessible name of the list of suggestions of `KlyInputMenu`. |
| `InputNumberDecrement` (init) | `string` | Accessible name of the decrease button of `KlyInputNumber`. |
| `InputNumberIncrement` (init) | `string` | Accessible name of the increase button of `KlyInputNumber`. |
| `InputRatingValue` (init) | `string` | Label of one star of a rating; `{0}` is its value, `{1}` the maximum. |
| `InputTagsRemove` (init) | `string` | Label of the button that removes a tag; `{0}` is the tag. |
| `LastPage` (init) | `string` | Accessible name of the last page button of `KlyPagination`. |
| `LoadingMore` (init) | `string` | Announced politely while `KlyInfiniteScroll` loads the next items. |
| `LoadMore` (init) | `string` | Text of the button of `KlyInfiniteScroll` that loads the next items by hand. |
| `LocaleSelectLabel` (init) | `string` | Accessible name of a language select. |
| `MaximumSize` (init) | `string` | Hint of `KlyFileUpload` that states the size limit; `{0}` is the limit, formatted for the locale. |
| `Minutes` (init) | `string` | Accessible name of the minutes segment of `KlyInputTime`. |
| `ModifierKey` (init) | `string` | Name of the key that "mod" stands for in the hint of a shortcut (`mod+k`): "Ctrl", "Strg" in German. On Apple devices the browser shows the command symbol instead. |
| `MoreAvatars` (init) | `string` | Name of the "+N" avatar of an avatar group; `{0}` is the number of hidden avatars. |
| `NextMonth` (init) | `string` | Accessible name of the next month button of `KlyCalendar`. |
| `NextPage` (init) | `string` | Accessible name of the next page button of `KlyPagination`. |
| `NextSlide` (init) | `string` | Accessible name of the next button of `KlyCarousel`. |
| `NoCommandResults` (init) | `string` | Text shown by `KlyCommandPalette` when no command matches the search. |
| `NoData` (init) | `string` | Text shown by `KlyChart` and `KlyTable` when there is nothing to show. |
| `NoMoreItems` (init) | `string` | Announced politely when `KlyInfiniteScroll` has no more items to load, and shown by `ShowEnd`. |
| `Notifications` (init) | `string` | Accessible name of the list of toasts of `KlyToaster`. |
| `OpenHeaderMenu` (init) | `string` | Name of the button that opens the mobile menu of a header. |
| `OpenSidebar` (init) | `string` | Name of the button that opens the mobile panel of a sidebar. |
| `OpensInNewTab` (init) | `string` | Hidden text added to the name of a page link that opens in a new tab. |
| `Page` (init) | `string` | Prefix of the accessible name of a page button of `KlyPagination` ("Page 3"). |
| `PageAnchors` (init) | `string` | Accessible name of a `KlyPageAnchors` list that has no `AriaLabel`. |
| `PageLinks` (init) | `string` | Accessible name of a `KlyPageLinks` list that has neither a title nor an `AriaLabel`. |
| `Pagination` (init) | `string` | Accessible name of the navigation landmark of `KlyPagination` when no `AriaLabel` is given. |
| `PauseSlides` (init) | `string` | Accessible name of the button of `KlyCarousel` that pauses the automatic rotation. |
| `PinInputCharacter` (init) | `string` | Label of one box of a pin input; `{0}` is its position, `{1}` the number of boxes. |
| `PlaySlides` (init) | `string` | Accessible name of the button of `KlyCarousel` that starts the automatic rotation. |
| `PreviousMonth` (init) | `string` | Accessible name of the previous month button of `KlyCalendar`. |
| `PreviousPage` (init) | `string` | Accessible name of the previous page button of `KlyPagination`. |
| `PreviousSlide` (init) | `string` | Accessible name of the previous button of `KlyCarousel`. |
| `PricingDiscountedPrice` (init) | `string` | Hidden text before the discounted price of a pricing plan. |
| `PricingFeatures` (init) | `string` | Hidden name of the column of a pricing table that lists the features. |
| `PricingIncluded` (init) | `string` | Hidden text of a pricing table cell that has a check mark. |
| `PricingNotIncluded` (init) | `string` | Hidden text of a pricing table cell that has a minus sign. |
| `PricingOriginalPrice` (init) | `string` | Hidden text before the original price of a pricing plan that has a discount. |
| `Progress` (init) | `string` | Accessible name of a progress bar that has no `AriaLabel`. |
| `RemoteOptionsError` (init) | `string` | Shown (and announced) when the `OptionsProvider` of a `SelectMenu` or `InputMenu` fails. |
| `RemoteOptionsLoading` (init) | `string` | Announced politely while a `SelectMenu` or `InputMenu` with an `OptionsProvider` loads its options. |
| `RemoteOptionsMinLength` (init) | `string` | Shown when the typed text is shorter than `MinLength`; `{0}` is that length. |
| `RemoteOptionsRetry` (init) | `string` | Text of the button that asks the `OptionsProvider` again after a failure. |
| `RemoveFile` (init) | `string` | Accessible name of the remove button of a file in `KlyFileUpload`; `{0}` is the file name. |
| `ResizePanels` (init) | `string` | Accessible name of the resize handle of `KlySplitter` and `KlyDashboardResizeHandle`. |
| `ResizeSidebar` (init) | `string` | Name of the handle that resizes a sidebar. |
| `Saturation` (init) | `string` | Accessible name of the saturation control of `KlyColorPicker`. |
| `SearchCommands` (init) | `string` | Placeholder and accessible name of the search field of `KlyCommandPalette`. |
| `SelectMenuCreate` (init) | `string` | Label of the option that adds the typed text as a new value; `{0}` is the text. |
| `SelectMenuNoResults` (init) | `string` | Text shown by `KlySelectMenu` when no option matches the filter. |
| `SelectMenuSearch` (init) | `string` | Accessible name of the filter field of the popup of `KlySelectMenu`. |
| `SidebarLabel` (init) | `string` | Accessible name of a sidebar, and title of its mobile panel. |
| `SkipToContent` (init) | `string` | Text of the skip link (`KlySkipLink`): the first link of a page, to the main content. |
| `SlideControls` (init) | `string` | Accessible name of the group of slide dots of `KlyCarousel`. |
| `SlideOf` (init) | `string` | Default accessible name of a slide of `KlyCarousel`; `{0}` is its number and `{1}` the count. |
| `SliderMaximum` (init) | `string` | Accessible name of the thumb that sets the upper bound of `KlyRangeSlider`. |
| `SliderMinimum` (init) | `string` | Accessible name of the thumb that sets the lower bound of `KlyRangeSlider`. |
| `SortAscending` (init) | `string` | Tooltip of the button of a sortable column that sorts it in ascending order. |
| `SortDescending` (init) | `string` | Tooltip of the button of a sorted column that reverses the order. |
| `StepCompleted` (init) | `string` | Word read by assistive technology after the title of a finished step of `KlyStepper`. |
| `TimeAm` (init) | `string` | Label of the morning segment when the culture has no designator for it. |
| `TimePm` (init) | `string` | Label of the afternoon segment when the culture has no designator for it. |
| `ToggleColorMode` (init) | `string` | Accessible name of `KlyColorModeButton` and `KlyColorModeSwitch`. |
| `TooManyFiles` (init) | `string` | Error of `KlyFileUpload` when more files than allowed are chosen; `{0}` is the limit. |

#### `KlyNavigationItem`

`Klyroo.UI` · record

A link of a `KlyNavigationMenu`, with optional sub-items.

| Constructor | Description |
| :--- | :--- |
| `KlyNavigationItem(string Label)` | A link of a `KlyNavigationMenu`, with optional sub-items. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Active` (init) | `bool?` | Forces the active state instead of comparing `KlyNavigationItem.Href` with the current address. |
| `Children` (init) | `IReadOnlyList<KlyNavigationItem>?` | Sub-items. Horizontally they open in a panel under the item, vertically they form a collapsible group. An item with children has no link of its own. |
| `Disabled` (init) | `bool` | Disables the item: it is listed but cannot be followed. |
| `Exact` (init) | `bool` | The item is active only on exactly its address; otherwise it is also active on the addresses below it. |
| `Href` (init) | `string?` | Address the item links to. An item with children and no address is a group. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Label` (init) | `string` | Text of the link. |
| `Target` (init) | `string?` | The `target` attribute of the link, such as `_blank`. |

#### `KlyOptionsRequest`

`Klyroo.UI` · record

What `KlyOptionsProvider` is asked for: one page of options for a search term. Both kinds of paging are carried: a provider that pages by position uses `KlyOptionsRequest.Skip` and `KlyOptionsRequest.Take`, one that pages with an opaque token uses `KlyOptionsRequest.Cursor`. The component fills all of them, whichever the provider reads.

| Constructor | Description |
| :--- | :--- |
| `KlyOptionsRequest(string Search, int Skip, int Take, string? Cursor)` | What `KlyOptionsProvider` is asked for: one page of options for a search term. Both kinds of paging are carried: a provider that pages by position uses `KlyOptionsRequest.Skip` and `KlyOptionsRequest.Take`, one that pages with an opaque token uses `KlyOptionsRequest.Cursor`. The component fills all of them, whichever the provider reads. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Cursor` (init) | `string?` | The `KlyOptionsResult.NextCursor` of the previous page; null for the first page and for a provider that does not use cursors. |
| `Search` (init) | `string` | The text typed in the filter field, trimmed. Empty when nothing is typed: list the options unfiltered. |
| `Skip` (init) | `int` | How many options the earlier pages of this search returned (0 for the first page). |
| `Take` (init) | `int` | The number of options wanted (`PageSize` of the component). |

#### `KlyOptionsResult`

`Klyroo.UI` · record

One page of options returned by a `KlyOptionsProvider`.

| Constructor | Description |
| :--- | :--- |
| `KlyOptionsResult(IReadOnlyList<KlySelectItem> Items, bool HasMore = false, string? NextCursor = null)` | Creates a page of options whose values are strings. |
| `KlyOptionsResult(IReadOnlyList<IKlySelectItem> Items, bool HasMore = false, string? NextCursor = null)` | Creates a page of options of any value type: `KlySelectItem` of the type the menu is bound to (an `int`, an enum, a `Guid`...). A menu that is given an option of another type throws when it reads it. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `HasMore` (init) | `bool` | Whether another page exists. A non-null `KlyOptionsResult.NextCursor` also means there is one. |
| `Items` (init) | `IReadOnlyList<IKlySelectItem>` | The options of this page, in the order to list them. |
| `NextCursor` (init) | `string?` | The cursor that asks for the page after this one; null when the provider pages by `Skip` and `Take` or this is the last page. |

#### `KlyOverlayOptions`

`Klyroo.UI` · record

Presentation of a dialog opened with `IKlyOverlayService`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `AriaLabel` (init) | `string?` | Accessible name when there is no `KlyOverlayOptions.Title`. |
| `Close` (init) | `bool` | Shows the close button of the header, which cancels the dialog. |
| `Description` (init) | `string?` | Text under the title. |
| `Dismissible` (init) | `bool` | Escape and a click on the backdrop cancel the dialog. Turn it off to force an explicit answer. |
| `Kind` (init) | `KlyOverlayKind` | A centered modal (the default) or a slideover. |
| `Side` (init) | `SlideoverSide` | Edge a slideover comes from. Ignored for a modal. |
| `Size` (init) | `KlySize` | Size of a modal, from `Xs` to `Xl`. Ignored for a slideover. |
| `Title` (init) | `string?` | Title of the dialog, which also names it for assistive technology. |

#### `KlyOverlayResult<TResult>`

`Klyroo.UI` · record

How a dialog opened with `IKlyOverlayService` ended.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Canceled` | `bool` | True when the dialog was dismissed (Escape, backdrop, close button, `KlyOverlayContext.Cancel` or a cancellation token) instead of completed. |
| `Value` | `TResult` | The value passed to `KlyOverlayContext.Close`, or `default` when the dialog was canceled. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `TryGetValue(out TResult value)` | `bool` | Gets the value when the dialog completed with one (it was not canceled). |

#### `KlyPostAuthor`

`Klyroo.UI` · record

An author of a `KlyBlogPost` or a `KlyChangelogVersion`, with the parameters of `KlyUser`.

| Constructor | Description |
| :--- | :--- |
| `KlyPostAuthor(string Name)` | An author of a `KlyBlogPost` or a `KlyChangelogVersion`, with the parameters of `KlyUser`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `AvatarSrc` (init) | `string?` | Address of the avatar picture. |
| `Description` (init) | `string?` | Text under the name, such as a role. |
| `Href` (init) | `string?` | Makes the author a link. Ignored when the post itself is a link: a link cannot contain another one. |
| `Name` (init) | `string` | Name of the author. |

#### `KlyPricingFeature`

`Klyroo.UI` · record

A line of the feature list of a `KlyPricingPlan`. A string converts to one.

| Constructor | Description |
| :--- | :--- |
| `KlyPricingFeature(string Title)` | A line of the feature list of a `KlyPricingPlan`. A string converts to one. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Icon` (init) | `string?` | Name of a built-in icon (see `KlyIcon`) shown instead of the check mark. |
| `Title` (init) | `string` | Text of the feature. |

#### `KlyPricingSection`

`Klyroo.UI` · record

A group of rows of a `KlyPricingTable`.

| Constructor | Description |
| :--- | :--- |
| `KlyPricingSection(string Title, IReadOnlyList<KlyPricingTableFeature> Features)` | A group of rows of a `KlyPricingTable`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Features` (init) | `IReadOnlyList<KlyPricingTableFeature>` | The rows of the group. |
| `Title` (init) | `string` | Heading of the group of rows. |

#### `KlyPricingTableFeature`

`Klyroo.UI` · record

A row of a `KlyPricingTable`. `KlyPricingTableFeature.Values` are the cells in the order of the tiers (or give `KlyPricingTableFeature.ByTier`): `true` is a check mark, `false` or `null` a minus sign, a text or a number is shown as it is.

| Constructor | Description |
| :--- | :--- |
| `KlyPricingTableFeature(string Title, params KlyPricingTableValue[] Values)` | A row of a `KlyPricingTable`. `KlyPricingTableFeature.Values` are the cells in the order of the tiers (or give `KlyPricingTableFeature.ByTier`): `true` is a check mark, `false` or `null` a minus sign, a text or a number is shown as it is. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ByTier` (init) | `IReadOnlyDictionary<string, KlyPricingTableValue>?` | Cells by tier id, instead of by position. A tier missing from it falls back to its position in `KlyPricingTableFeature.Values`. |
| `Title` (init) | `string` | Name of the feature, shown in the first column. |
| `Values` (init) | `KlyPricingTableValue[]` | The cells, in the order of the tiers: `true` is a check mark, `false` or `null` a minus sign, a text or a number is shown as it is. |

#### `KlyPricingTier`

`Klyroo.UI` · record

A column of a `KlyPricingTable`.

| Constructor | Description |
| :--- | :--- |
| `KlyPricingTier(string Id, string Title)` | A column of a `KlyPricingTable`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Badge` (init) | `string?` | Text of a badge next to the title. |
| `BillingCycle` (init) | `string?` | Period the price covers, such as "/month". |
| `ButtonHref` (init) | `string?` | Address the call to action button links to. |
| `ButtonLabel` (init) | `string?` | Label of the call to action button. |
| `Description` (init) | `string?` | Text under the title of the tier. |
| `Discount` (init) | `string?` | The discounted price; `KlyPricingTier.Price` then is the original one, struck through. |
| `Highlight` (init) | `bool` | Emphasizes the recommended tier. |
| `Id` (init) | `string` | Identifies the tier, to give its cells in `KlyPricingTableFeature.ByTier`. |
| `Price` (init) | `string?` | Price of the tier, as the text to show. |
| `Title` (init) | `string` | Name of the tier. |

#### `KlyRadioItem<TValue>`

`Klyroo.UI` · record

A radio button of a `KlyRadioGroup`.

| Constructor | Description |
| :--- | :--- |
| `KlyRadioItem<TValue>(TValue Value, string Label)` | A radio button of a `KlyRadioGroup`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Text under the label. |
| `Disabled` (init) | `bool` | Disables the radio button. |
| `Label` (init) | `string` | Text next to the radio button. |
| `Value` (init) | `TValue` | Value the group takes when the radio button is chosen. |

#### `KlyResolvedImage`

`Klyroo.UI` · record

The attributes `KlyImageSettings.Resolve` computed for an image.

| Constructor | Description |
| :--- | :--- |
| `KlyResolvedImage(string Src, string? SrcSet, string? Sizes)` | The attributes `KlyImageSettings.Resolve` computed for an image. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Sizes` (init) | `string?` | The `sizes` attribute, or null when no sizes were asked for. |
| `Src` (init) | `string` | The `src` attribute: the largest variant when there is a `sizes` list, the 1x variant otherwise. |
| `SrcSet` (init) | `string?` | The `srcset` attribute, or null when the provider cannot resize or nothing varies. |

#### `KlySelectItem`

`Klyroo.UI` · record · inherits `KlySelectItem<string>`

An option whose value is a string, so `new KlySelectItem("fr", "France")` keeps working. It is a `KlySelectItem` of `String`: lists of it are accepted wherever a list of `KlySelectItem<string>` is.

| Constructor | Description |
| :--- | :--- |
| `KlySelectItem(string Value, string Label)` | An option whose value is a string, so `new KlySelectItem("fr", "France")` keeps working. It is a `KlySelectItem` of `String`: lists of it are accepted wherever a list of `KlySelectItem<string>` is. |

#### `KlySelectItem<TValue>`

`Klyroo.UI` · record

An option of `KlySelect`, `KlySelectMenu`, `KlyInputMenu` and `KlyListbox`.

| Constructor | Description |
| :--- | :--- |
| `KlySelectItem<TValue>(TValue Value, string Label)` | An option of `KlySelect`, `KlySelectMenu`, `KlyInputMenu` and `KlyListbox`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Count` (init) | `int?` | A number shown at the end of the row (matches, stock, unread): formatted for the locale, and not searched or typed ahead. |
| `Disabled` (init) | `bool` | The option is listed but cannot be chosen. |
| `Group` (init) | `string?` | Lists the item under a heading. The items without a group are listed first, in their order and with no heading, then each group in the order of its first item, with all its items, wherever they are in the list (so an item without a group never sits between two groups, where it would look like part of one). The heading is not an option: the arrow keys and the typeahead skip it, and a filter hides it when none of its items matches. The list has role group, named by the heading. |
| `Icon` (init) | `string?` | An icon drawn before the label, by name like every icon of Klyroo (a Lucide name, or any Iconify name the build bundles). |
| `Label` (init) | `string` | Text of the option. |
| `Value` (init) | `TValue` | Value the control takes when the option is chosen. |

#### `KlySplitterPanel`

`Klyroo.UI` · record

A panel of a `KlySplitter`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `RenderFragment?` | Content of the panel. |
| `MaxSize` (init) | `double` | Largest size in percent. |
| `MinSize` (init) | `double` | Smallest size in percent. |
| `Size` (init) | `double?` | Initial size in percent. Panels without one share what the others leave. |

#### `KlyStepperItem`

`Klyroo.UI` · record

A step of a `KlyStepper`.

| Constructor | Description |
| :--- | :--- |
| `KlyStepperItem(string Title)` | A step of a `KlyStepper`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `RenderFragment?` | Shown below the list while this step is the current one. |
| `Description` (init) | `string?` | Text under the title. |
| `Disabled` (init) | `bool` | Cannot be selected when the stepper is interactive. |
| `Icon` (init) | `RenderFragment?` | Replaces the step number (and the check of a completed step). |
| `Title` (init) | `string` | Title of the step. |

#### `KlyTabItem`

`Klyroo.UI` · record

A tab of `KlyTabs`.

| Constructor | Description |
| :--- | :--- |
| `KlyTabItem(string Label)` | A tab of `KlyTabs`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `RenderFragment?` | Content of the panel the tab shows. |
| `Disabled` (init) | `bool` | Disables the tab. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Label` (init) | `string` | Text of the tab button. |
| `Value` (init) | `string?` | Identifies the tab in `KlyTabs.Value`. Defaults to its index. |

#### `KlyTimelineItem`

`Klyroo.UI` · record

An event of a `KlyTimeline`.

| Constructor | Description |
| :--- | :--- |
| `KlyTimelineItem(string Title)` | An event of a `KlyTimeline`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Date` (init) | `string?` | Free text: "Mar 2026", "Yesterday". |
| `Description` (init) | `string?` | Text under the title. |
| `Icon` (init) | `RenderFragment?` | Icon of the event, as markup such as a `KlyIcon`. |
| `Title` (init) | `string` | Title of the event. |

#### `KlyToast`

`Klyroo.UI` · record

A notification shown by `KlyToaster`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Color` (init) | `KlyColor` | Semantic color of the toast. Defaults to neutral. |
| `Description` (init) | `string?` | Text under the title. |
| `Duration` (init) | `TimeSpan?` | Time before the toast closes itself; hovering or focusing it pauses the countdown. Zero keeps it open. |
| `Title` (init) | `string?` | Title of the toast. |

#### `KlyToggleItem`

`Klyroo.UI` · record

A button of a `KlyToggleGroup`.

| Constructor | Description |
| :--- | :--- |
| `KlyToggleItem(string Value, string Label)` | A button of a `KlyToggleGroup`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Disabled` (init) | `bool` | Disables the button. |
| `Icon` (init) | `RenderFragment?` | Icon of the button, as markup such as a `KlyIcon`. |
| `Label` (init) | `string` | Text of the button. |
| `Value` (init) | `string` | Value of the item in the selection of the group. |

#### `KlyTreeItem`

`Klyroo.UI` · record

A node of a `KlyTree`.

| Constructor | Description |
| :--- | :--- |
| `KlyTreeItem(string Label)` | A node of a `KlyTree`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Children` (init) | `IReadOnlyList<KlyTreeItem>?` | The nodes below this one. A node with children can be expanded. |
| `Disabled` (init) | `bool` | Disables the node. |
| `Expanded` (init) | `bool` | Initial state of an item with children. |
| `Href` (init) | `string?` | Makes the label a link; Enter on the item follows it. |
| `Icon` (init) | `RenderFragment?` | Icon shown before the label, as markup such as a `KlyIcon`. |
| `Label` (init) | `string` | Text of the node. |
| `Value` (init) | `string?` | Identifies the item in `KlyTree.Value`. Defaults to its label, so give one when labels repeat. |

#### `LinkUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyLink` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyLink`, after the theme and the app configuration. |

#### `ListboxUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyListbox`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyListbox`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyListbox`, after the theme and the app configuration. |
| `GroupLabel` (init) | `string?` | Classes merged into the `groupLabel` slot of `KlyListbox`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyListbox`, after the theme and the app configuration. |
| `ItemCount` (init) | `string?` | Classes merged into the `itemCount` slot of `KlyListbox`, after the theme and the app configuration. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlyListbox`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlyListbox`, after the theme and the app configuration. |
| `ItemLeadingIcon` (init) | `string?` | Classes merged into the `itemLeadingIcon` slot of `KlyListbox`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyListbox`, after the theme and the app configuration. |

#### `LocaleSelectUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyLocaleSelect` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyLocaleSelect`, after the theme and the app configuration. |

#### `MainUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyMain` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyMain`, after the theme and the app configuration. |

#### `MarqueeUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyMarquee`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyMarquee`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyMarquee`, after the theme and the app configuration. |

#### `ModalUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyModal`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyModal`, after the theme and the app configuration. |
| `Close` (init) | `string?` | Classes merged into the `close` slot of `KlyModal`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyModal`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyModal`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyModal`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyModal`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyModal`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyModal`, after the theme and the app configuration. |

#### `NavigationMenuUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyNavigationMenu`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Chevron` (init) | `string?` | Classes merged into the `chevron` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `GroupList` (init) | `string?` | Classes merged into the `groupList` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Panel` (init) | `string?` | Classes merged into the `panel` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `PanelList` (init) | `string?` | Classes merged into the `panelList` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyNavigationMenu`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyNavigationMenu`, after the theme and the app configuration. |

#### `PageAnchorsUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageAnchors` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ExternalIcon` (init) | `string?` | Classes merged into the `externalIcon` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyPageAnchors`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageAnchors`, after the theme and the app configuration. |

#### `PageAsideUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageAside` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyPageAside`, after the theme and the app configuration. |
| `Bottom` (init) | `string?` | Classes merged into the `bottom` slot of `KlyPageAside`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageAside`, after the theme and the app configuration. |
| `Top` (init) | `string?` | Classes merged into the `top` slot of `KlyPageAside`, after the theme and the app configuration. |

#### `PageBodyUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageBody` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageBody`, after the theme and the app configuration. |

#### `PageCardUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageCard` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyPageCard`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Spotlight` (init) | `string?` | Classes merged into the `spotlight` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageCard`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyPageCard`, after the theme and the app configuration. |

#### `PageColumnsUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageColumns` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageColumns`, after the theme and the app configuration. |

#### `PageCtaUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageCta` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyPageCta`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyPageCta`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPageCta`, after the theme and the app configuration. |
| `Links` (init) | `string?` | Classes merged into the `links` slot of `KlyPageCta`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageCta`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageCta`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyPageCta`, after the theme and the app configuration. |

#### `PageFeatureItem`

`Klyroo.UI` · record

A feature of a `KlyPageSection`.

| Constructor | Description |
| :--- | :--- |
| `PageFeatureItem(string Title, string? Description = null)` | A feature of a `KlyPageSection`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Text under the title. |
| `Href` (init) | `string?` | Address the feature links to. |
| `Icon` (init) | `string?` | Name of a built-in icon (see `KlyIcon`) shown above the title. |
| `Target` (init) | `string?` | The `target` attribute of the link, such as `_blank`. |
| `Title` (init) | `string` | Title of the feature. |

#### `PageFeatureUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageFeature` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPageFeature`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyPageFeature`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyPageFeature`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyPageFeature`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageFeature`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageFeature`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyPageFeature`, after the theme and the app configuration. |

#### `PageGridUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageGrid` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageGrid`, after the theme and the app configuration. |

#### `PageHeaderUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageHeader` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlyPageHeader`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPageHeader`, after the theme and the app configuration. |
| `Headline` (init) | `string?` | Classes merged into the `headline` slot of `KlyPageHeader`, after the theme and the app configuration. |
| `Links` (init) | `string?` | Classes merged into the `links` slot of `KlyPageHeader`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageHeader`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageHeader`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyPageHeader`, after the theme and the app configuration. |

#### `PageHeroUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageHero` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Headline` (init) | `string?` | Classes merged into the `headline` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Links` (init) | `string?` | Classes merged into the `links` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Media` (init) | `string?` | Classes merged into the `media` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageHero`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyPageHero`, after the theme and the app configuration. |

#### `PageLinkItem`

`Klyroo.UI` · record

A link of the page family. `KlyPageHeader`, `KlyPageHero`, `KlyPageSection` and `KlyPageCta` render it as a button (`PageLinkItem.Color` and `PageLinkItem.Variant` apply); `KlyPageLinks` and `KlyPageAnchors` render it as a text link.

| Constructor | Description |
| :--- | :--- |
| `PageLinkItem(string Label, string? Href = null)` | A link of the page family. `KlyPageHeader`, `KlyPageHero`, `KlyPageSection` and `KlyPageCta` render it as a button (`PageLinkItem.Color` and `PageLinkItem.Variant` apply); `KlyPageLinks` and `KlyPageAnchors` render it as a text link. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Active` (init) | `bool?` | Forces the active state of a text link; otherwise it follows the current address. |
| `Color` (init) | `KlyColor?` | Defaults to `KlyDefaults.Color`, or primary. |
| `Disabled` (init) | `bool` | Disables the link. |
| `Exact` (init) | `bool` | Requires the whole path to match for a text link to be active. |
| `Href` (init) | `string?` | Address the link points to. |
| `Icon` (init) | `string?` | Name of a built-in icon (see `KlyIcon`) shown before the label. |
| `Label` (init) | `string` | Text of the link. |
| `Target` (init) | `string?` | The `target` attribute of the link, such as `_blank`. |
| `TrailingIcon` (init) | `string?` | Name of a built-in icon shown after the label (buttons only). |
| `Variant` (init) | `ButtonVariant` | Look of the button when the link is rendered as one. |

#### `PageLinksUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageLinks` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ExternalIcon` (init) | `string?` | Classes merged into the `externalIcon` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `Link` (init) | `string?` | Classes merged into the `link` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageLinks`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageLinks`, after the theme and the app configuration. |

#### `PageListUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageList` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageList`, after the theme and the app configuration. |

#### `PageLogoItem`

`Klyroo.UI` · record

A logo of a `KlyPageLogos`: a built-in icon, or an image. `PageLogoItem.Alt` is its accessible name.

| Constructor | Description |
| :--- | :--- |
| `PageLogoItem(string Alt)` | A logo of a `KlyPageLogos`: a built-in icon, or an image. `PageLogoItem.Alt` is its accessible name. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Alt` (init) | `string` | Accessible name of the logo. |
| `Icon` (init) | `string?` | Name of a built-in icon (see `KlyIcon`). |
| `Src` (init) | `string?` | URL of an image, used when no `PageLogoItem.Icon` is given. |

#### `PageLogosUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageLogos` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyPageLogos`, after the theme and the app configuration. |
| `Image` (init) | `string?` | Classes merged into the `image` slot of `KlyPageLogos`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyPageLogos`, after the theme and the app configuration. |
| `Items` (init) | `string?` | Classes merged into the `items` slot of `KlyPageLogos`, after the theme and the app configuration. |
| `Marquee` (init) | `string?` | Classes merged into the `marquee` slot of `KlyPageLogos`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageLogos`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageLogos`, after the theme and the app configuration. |

#### `PageSectionUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPageSection` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Feature` (init) | `string?` | Classes merged into the `feature` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Features` (init) | `string?` | Classes merged into the `features` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Headline` (init) | `string?` | Classes merged into the `headline` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Leading` (init) | `string?` | Classes merged into the `leading` slot of `KlyPageSection`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Links` (init) | `string?` | Classes merged into the `links` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Media` (init) | `string?` | Classes merged into the `media` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPageSection`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyPageSection`, after the theme and the app configuration. |

#### `PageUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyPage` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Center` (init) | `string?` | Classes merged into the `center` slot of `KlyPage`, after the theme and the app configuration. |
| `Left` (init) | `string?` | Classes merged into the `left` slot of `KlyPage`, after the theme and the app configuration. |
| `Right` (init) | `string?` | Classes merged into the `right` slot of `KlyPage`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPage`, after the theme and the app configuration. |

#### `PaginationUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyPagination`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Control` (init) | `string?` | Classes merged into the `control` slot of `KlyPagination`, after the theme and the app configuration. |
| `Ellipsis` (init) | `string?` | Classes merged into the `ellipsis` slot of `KlyPagination`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyPagination`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyPagination`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyPagination`, after the theme and the app configuration. |
| `Page` (init) | `string?` | Classes merged into the `page` slot of `KlyPagination`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPagination`, after the theme and the app configuration. |

#### `PinInputUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyPinInput`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Cell` (init) | `string?` | Classes merged into the `cell` slot of `KlyPinInput`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPinInput`, after the theme and the app configuration. |

#### `PopoverUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyPopover`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyPopover`, after the theme and the app configuration. |

#### `PricingPlansUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyPricingPlans`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPricingPlans`, after the theme and the app configuration. |

#### `PricingPlanUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyPricingPlan`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Badge` (init) | `string?` | Classes merged into the `badge` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `BillingCycle` (init) | `string?` | Classes merged into the `billingCycle` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Discount` (init) | `string?` | Classes merged into the `discount` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Feature` (init) | `string?` | Classes merged into the `feature` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `FeatureIcon` (init) | `string?` | Classes merged into the `featureIcon` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Features` (init) | `string?` | Classes merged into the `features` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Intro` (init) | `string?` | Classes merged into the `intro` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Price` (init) | `string?` | Classes merged into the `price` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `PriceRow` (init) | `string?` | Classes merged into the `priceRow` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Pricing` (init) | `string?` | Classes merged into the `pricing` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Tagline` (init) | `string?` | Classes merged into the `tagline` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Terms` (init) | `string?` | Classes merged into the `terms` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyPricingPlan`, after the theme and the app configuration. |
| `TitleRow` (init) | `string?` | Classes merged into the `titleRow` slot of `KlyPricingPlan`, after the theme and the app configuration. |

#### `PricingTableUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyPricingTable`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Caption` (init) | `string?` | Classes merged into the `caption` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Cell` (init) | `string?` | Classes merged into the `cell` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Corner` (init) | `string?` | Classes merged into the `corner` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Feature` (init) | `string?` | Classes merged into the `feature` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `HeadRow` (init) | `string?` | Classes merged into the `headRow` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Included` (init) | `string?` | Classes merged into the `included` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `NotIncluded` (init) | `string?` | Classes merged into the `notIncluded` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Row` (init) | `string?` | Classes merged into the `row` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Section` (init) | `string?` | Classes merged into the `section` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `SectionRow` (init) | `string?` | Classes merged into the `sectionRow` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Table` (init) | `string?` | Classes merged into the `table` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Tbody` (init) | `string?` | Classes merged into the `tbody` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Thead` (init) | `string?` | Classes merged into the `thead` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `Tier` (init) | `string?` | Classes merged into the `tier` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierBadge` (init) | `string?` | Classes merged into the `tierBadge` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierButton` (init) | `string?` | Classes merged into the `tierButton` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierCycle` (init) | `string?` | Classes merged into the `tierCycle` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierDescription` (init) | `string?` | Classes merged into the `tierDescription` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierDiscount` (init) | `string?` | Classes merged into the `tierDiscount` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierPrice` (init) | `string?` | Classes merged into the `tierPrice` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `TierTitle` (init) | `string?` | Classes merged into the `tierTitle` slot of `KlyPricingTable`, after the theme and the app configuration. |
| `ValueIcon` (init) | `string?` | Classes merged into the `valueIcon` slot of `KlyPricingTable`, after the theme and the app configuration. |

#### `ProgressUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyProgress` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyProgress`, after the theme and the app configuration. |
| `Indicator` (init) | `string?` | Classes merged into the `indicator` slot of `KlyProgress`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyProgress`, after the theme and the app configuration. |
| `Status` (init) | `string?` | Classes merged into the `status` slot of `KlyProgress`, after the theme and the app configuration. |
| `Step` (init) | `string?` | Classes merged into the `step` slot of `KlyProgress`, after the theme and the app configuration. |
| `Steps` (init) | `string?` | Classes merged into the `steps` slot of `KlyProgress`, after the theme and the app configuration. |

#### `RadioGroupUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyRadioGroup`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Fieldset` (init) | `string?` | Classes merged into the `fieldset` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Indicator` (init) | `string?` | Classes merged into the `indicator` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Legend` (init) | `string?` | Classes merged into the `legend` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyRadioGroup`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyRadioGroup`, after the theme and the app configuration. |

#### `ScrollAreaUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyScrollArea`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyScrollArea`, after the theme and the app configuration. |
| `Viewport` (init) | `string?` | Classes merged into the `viewport` slot of `KlyScrollArea`, after the theme and the app configuration. |

#### `SelectMenuUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlySelectMenu`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Announcer` (init) | `string?` | Classes merged into the `announcer` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Create` (init) | `string?` | Classes merged into the `create` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Empty` (init) | `string?` | Classes merged into the `empty` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Error` (init) | `string?` | Classes merged into the `error` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `GroupLabel` (init) | `string?` | Classes merged into the `groupLabel` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `ItemCount` (init) | `string?` | Classes merged into the `itemCount` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `ItemLeadingIcon` (init) | `string?` | Classes merged into the `itemLeadingIcon` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Listbox` (init) | `string?` | Classes merged into the `listbox` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Loading` (init) | `string?` | Classes merged into the `loading` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Scroller` (init) | `string?` | Classes merged into the `scroller` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Search` (init) | `string?` | Classes merged into the `search` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `SearchWrapper` (init) | `string?` | Classes merged into the `searchWrapper` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `Spinner` (init) | `string?` | Classes merged into the `spinner` slot of `KlySelectMenu`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlySelectMenu`, after the theme and the app configuration. |

#### `SelectUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlySelect`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlySelect`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlySelect`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlySelect`, after the theme and the app configuration. |
| `GroupLabel` (init) | `string?` | Classes merged into the `groupLabel` slot of `KlySelect`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlySelect`, after the theme and the app configuration. |
| `ItemCount` (init) | `string?` | Classes merged into the `itemCount` slot of `KlySelect`, after the theme and the app configuration. |
| `ItemIcon` (init) | `string?` | Classes merged into the `itemIcon` slot of `KlySelect`, after the theme and the app configuration. |
| `ItemLabel` (init) | `string?` | Classes merged into the `itemLabel` slot of `KlySelect`, after the theme and the app configuration. |
| `ItemLeadingIcon` (init) | `string?` | Classes merged into the `itemLeadingIcon` slot of `KlySelect`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlySelect`, after the theme and the app configuration. |
| `LeadingIcon` (init) | `string?` | Classes merged into the `leadingIcon` slot of `KlySelect`, after the theme and the app configuration. |
| `Listbox` (init) | `string?` | Classes merged into the `listbox` slot of `KlySelect`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlySelect`, after the theme and the app configuration. |
| `TrailingIcon` (init) | `string?` | Classes merged into the `trailingIcon` slot of `KlySelect`, after the theme and the app configuration. |

#### `SeparatorUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlySeparator` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Border` (init) | `string?` | Classes merged into the `border` slot of `KlySeparator`, after the theme and the app configuration. |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlySeparator`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlySeparator`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlySeparator`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlySeparator`, after the theme and the app configuration. |

#### `SidebarUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlySidebar` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Handle` (init) | `string?` | Classes merged into the `handle` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `HeaderContent` (init) | `string?` | Classes merged into the `headerContent` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Host` (init) | `string?` | Classes merged into the `host` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Mobile` (init) | `string?` | The wrapper of the button that opens the panel (below lg). |
| `MobileClose` (init) | `string?` | Classes merged into the `mobileClose` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `MobileHeader` (init) | `string?` | The title row of the panel, below lg. |
| `MobileTitle` (init) | `string?` | Classes merged into the `mobileTitle` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Panel` (init) | `string?` | The wrapper of everything inside the aside but the resize handle; the panel takes its dialog role while it is open. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `Toggle` (init) | `string?` | Classes merged into the `toggle` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |
| `ToggleIcon` (init) | `string?` | Classes merged into the `toggleIcon` slot of `KlyDashboardSidebar` and `KlySidebar`, after the theme and the app configuration. |

#### `SkeletonUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlySkeleton` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlySkeleton`, after the theme and the app configuration. |

#### `SkipLinkUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlySkipLink` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlySkipLink`, after the theme and the app configuration. |

#### `SlideoverUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlySlideover` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Body` (init) | `string?` | Classes merged into the `body` slot of `KlySlideover`, after the theme and the app configuration. |
| `Close` (init) | `string?` | Classes merged into the `close` slot of `KlySlideover`, after the theme and the app configuration. |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlySlideover`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlySlideover`, after the theme and the app configuration. |
| `Footer` (init) | `string?` | Classes merged into the `footer` slot of `KlySlideover`, after the theme and the app configuration. |
| `Header` (init) | `string?` | Classes merged into the `header` slot of `KlySlideover`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlySlideover`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlySlideover`, after the theme and the app configuration. |

#### `SliderUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyRangeSlider` and `KlySlider`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Range` (init) | `string?` | Classes merged into the `range` slot of `KlyRangeSlider` and `KlySlider`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyRangeSlider` and `KlySlider`, after the theme and the app configuration. |
| `Thumb` (init) | `string?` | Classes merged into the `thumb` slot of `KlyRangeSlider` and `KlySlider`, after the theme and the app configuration. |
| `Track` (init) | `string?` | Classes merged into the `track` slot of `KlyRangeSlider` and `KlySlider`, after the theme and the app configuration. |

#### `SplitterUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlySplitter`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Handle` (init) | `string?` | Classes merged into the `handle` slot of `KlySplitter`, after the theme and the app configuration. |
| `Panel` (init) | `string?` | Classes merged into the `panel` slot of `KlySplitter`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlySplitter`, after the theme and the app configuration. |

#### `StepperUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyStepper`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyStepper`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyStepper`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyStepper`, after the theme and the app configuration. |
| `Indicator` (init) | `string?` | Classes merged into the `indicator` slot of `KlyStepper`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyStepper`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyStepper`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyStepper`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyStepper`, after the theme and the app configuration. |
| `Status` (init) | `string?` | Classes merged into the `status` slot of `KlyStepper`, after the theme and the app configuration. |
| `Text` (init) | `string?` | Classes merged into the `text` slot of `KlyStepper`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyStepper`, after the theme and the app configuration. |
| `Track` (init) | `string?` | Classes merged into the `track` slot of `KlyStepper`, after the theme and the app configuration. |

#### `SwitchUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlySwitch`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Container` (init) | `string?` | Classes merged into the `container` slot of `KlySwitch`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlySwitch`, after the theme and the app configuration. |
| `Input` (init) | `string?` | Classes merged into the `input` slot of `KlySwitch`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlySwitch`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlySwitch`, after the theme and the app configuration. |
| `Thumb` (init) | `string?` | Classes merged into the `thumb` slot of `KlySwitch`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlySwitch`, after the theme and the app configuration. |

#### `TableUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyTable`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyTable`, after the theme and the app configuration. |
| `Caption` (init) | `string?` | Classes merged into the `caption` slot of `KlyTable`, after the theme and the app configuration. |
| `Empty` (init) | `string?` | Classes merged into the `empty` slot of `KlyTable`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyTable`, after the theme and the app configuration. |
| `SortButton` (init) | `string?` | Classes merged into the `sortButton` slot of `KlyTable`, after the theme and the app configuration. |
| `SortIcon` (init) | `string?` | Classes merged into the `sortIcon` slot of `KlyTable`, after the theme and the app configuration. |
| `Tbody` (init) | `string?` | Classes merged into the `tbody` slot of `KlyTable`, after the theme and the app configuration. |
| `Td` (init) | `string?` | Classes merged into the `td` slot of `KlyTable`, after the theme and the app configuration. |
| `Th` (init) | `string?` | Classes merged into the `th` slot of `KlyTable`, after the theme and the app configuration. |
| `Thead` (init) | `string?` | Classes merged into the `thead` slot of `KlyTable`, after the theme and the app configuration. |
| `Tr` (init) | `string?` | Classes merged into the `tr` slot of `KlyTable`, after the theme and the app configuration. |

#### `TabsUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyTabs`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyTabs`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyTabs`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyTabs`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyTabs`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyTabs`, after the theme and the app configuration. |
| `Trigger` (init) | `string?` | Classes merged into the `trigger` slot of `KlyTabs`, after the theme and the app configuration. |

#### `TextareaUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyTextarea`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyTextarea`, after the theme and the app configuration. |

#### `TimelineUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyTimeline`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Date` (init) | `string?` | Classes merged into the `date` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Indicator` (init) | `string?` | Classes merged into the `indicator` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Separator` (init) | `string?` | Classes merged into the `separator` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Text` (init) | `string?` | Classes merged into the `text` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyTimeline`, after the theme and the app configuration. |
| `Track` (init) | `string?` | Classes merged into the `track` slot of `KlyTimeline`, after the theme and the app configuration. |

#### `ToastUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyToaster`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Close` (init) | `string?` | Classes merged into the `close` slot of `KlyToaster`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyToaster`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyToaster`, after the theme and the app configuration. |
| `Title` (init) | `string?` | Classes merged into the `title` slot of `KlyToaster`, after the theme and the app configuration. |
| `Viewport` (init) | `string?` | Classes merged into the `viewport` slot of `KlyToaster`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyToaster`, after the theme and the app configuration. |

#### `ToggleGroupUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyToggleGroup`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyToggleGroup`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyToggleGroup`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyToggleGroup`, after the theme and the app configuration. |

#### `ToggleUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyToggle`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Base` (init) | `string?` | Classes merged into the `base` slot of `KlyToggle`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyToggle`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyToggle`, after the theme and the app configuration. |

#### `TooltipUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyTooltip`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Content` (init) | `string?` | Classes merged into the `content` slot of `KlyTooltip`, after the theme and the app configuration. |

#### `TreeUi`

`Klyroo.UI` · record

Per-instance class overrides of the slots of `KlyTree`, passed to its `Ui` parameter. Each property holds the classes merged into one slot.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Chevron` (init) | `string?` | Classes merged into the `chevron` slot of `KlyTree`, after the theme and the app configuration. |
| `Group` (init) | `string?` | Classes merged into the `group` slot of `KlyTree`, after the theme and the app configuration. |
| `Icon` (init) | `string?` | Classes merged into the `icon` slot of `KlyTree`, after the theme and the app configuration. |
| `Item` (init) | `string?` | Classes merged into the `item` slot of `KlyTree`, after the theme and the app configuration. |
| `Label` (init) | `string?` | Classes merged into the `label` slot of `KlyTree`, after the theme and the app configuration. |
| `List` (init) | `string?` | Classes merged into the `list` slot of `KlyTree`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyTree`, after the theme and the app configuration. |
| `Row` (init) | `string?` | Classes merged into the `row` slot of `KlyTree`, after the theme and the app configuration. |
| `Spacer` (init) | `string?` | Classes merged into the `spacer` slot of `KlyTree`, after the theme and the app configuration. |

#### `UserUi`

`Klyroo.UI` · record

Per-instance class overrides of `KlyUser` slots.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Avatar` (init) | `string?` | Classes merged into the `avatar` slot of `KlyUser`, after the theme and the app configuration. |
| `Description` (init) | `string?` | Classes merged into the `description` slot of `KlyUser`, after the theme and the app configuration. |
| `Name` (init) | `string?` | Classes merged into the `name` slot of `KlyUser`, after the theme and the app configuration. |
| `Root` (init) | `string?` | Classes merged into the `root` slot of `KlyUser`, after the theme and the app configuration. |
| `Wrapper` (init) | `string?` | Classes merged into the `wrapper` slot of `KlyUser`, after the theme and the app configuration. |

### Structs

#### `KlyColor`

`Klyroo.UI` · struct

Semantic color of a component, rendered as `data-color`. Built-in colors map to the `--klyroo-*` tokens. A custom color only needs CSS rules for its name, for example `[data-color="brand"] { --kly-color: ...; --kly-color-contrast: ...; --kly-color-text: ...; }`, which works with and without Tailwind in the app.

| Constructor | Description |
| :--- | :--- |
| `KlyColor(string value)` | Creates a color from the name of a built-in or custom semantic color. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Error` | `KlyColor` | The color of errors. |
| `static Info` | `KlyColor` | The color of informational messages. |
| `static Neutral` | `KlyColor` | The neutral color, a gray that also drives text, background and border tokens. |
| `static Primary` | `KlyColor` | The primary color of the app. |
| `static Secondary` | `KlyColor` | The secondary color of the app. |
| `static Success` | `KlyColor` | The color of success messages. |
| `Value` | `string` | The name of the color, such as `primary`. |
| `static Warning` | `KlyColor` | The color of warnings. |

#### `KlyPricingTableValue`

`Klyroo.UI` · struct

The cell of a feature for one tier of a `KlyPricingTable`: a check mark (`true`), a minus sign (`false`, `null` or the default value), a text or a number. `bool`, `string`, `int`, `long` and `decimal` convert to it implicitly, so `[true, "10 GB", false, 5]` is a list of values. A number is shown with the format culture of the locale in effect.

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Included` | `KlyPricingTableValue` | A check mark: the tier includes the feature. |
| `static NotIncluded` | `KlyPricingTableValue` | A minus sign: the tier does not include the feature. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Number(decimal number)` | `KlyPricingTableValue` | A number, shown with the format culture of the locale in effect. |
| `static Text(string? text)` | `KlyPricingTableValue` | A text such as `"10 GB"`. `null` is a minus sign. |

#### `EditBinding`

`Klyroo.UI.Internal` · struct

Infrastructure of Klyroo form controls; not meant to be used directly. What a custom control needs from an `EditForm` field: its form name and its validation state.

| Constructor | Description |
| :--- | :--- |
| `EditBinding(string? Name, bool Invalid, string? CssClass = null)` | Infrastructure of Klyroo form controls; not meant to be used directly. What a custom control needs from an `EditForm` field: its form name and its validation state. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CssClass` (init) | `string?` | Classes from the `FieldCssClassProvider` of the edit context, or null. |
| `Invalid` (init) | `bool` | Whether the field is invalid and should be marked as such. |
| `Name` (init) | `string?` | Name under which the field is posted, or null. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `WithFieldClass(string? classes)` | `string?` | `classes` followed by the classes of the field's `FieldCssClassProvider` (valid, modified, invalid, or the app's own), as `InputText` adds them to its input. |

### Interfaces

#### `IKlyCultureStore`

`Klyroo.UI` · interface

Where a Blazor Hybrid app keeps the culture the user chose (`KlyLocaleSelect` writes it, `KlyApp` reads it). A browser app keeps it in the ASP.NET Core culture cookie instead and never uses this. Register your own implementation (`services.AddSingleton<IKlyCultureStore, PreferencesCultureStore>()`) to keep it in `Preferences`, a file or a server; the default is the web view's `localStorage`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetAsync()` | `ValueTask<string?>` | The stored culture name (`fr`, `pt-BR`), or null when none was chosen. |
| `SetAsync(string culture)` | `ValueTask` | Remembers the culture name the user chose. |

#### `IKlyImageProvider`

`Klyroo.UI` · interface

Turns a source image into the URL of a resized, converted version of it. Register providers with `KlyImageSettings.AddProvider` and pick one with `KlyImageSettings.DefaultProvider` or the `Provider` parameter of `KlyImage`. Implementations must be thread-safe and produce the same URL for the same input on the server and in WebAssembly (no secrets: the URL is public).

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `SupportsBlur` | `bool` | True when the provider can blur (see `KlyImageModifiers.Blur`), which makes `Placeholder` look better. Defaults to false. |
| `ValidateDomains` | `bool` | True when `KlyImageSettings.Domains` applies: a remote source (`https://host/...`) whose host is not listed is passed through untouched instead of being sent to the provider. Defaults to false, for providers whose sources are paths in their own storage. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetEffectiveWidth(int width)` | `int` | The width, in pixels, of the image the URL for `width` will really return, for the `w` descriptors of the `srcset`. A provider that rounds widths to a fixed set (`local`, `vercel`) returns the rounded one, so that a "400w" candidate is not a 512 pixel image; the others produce the width they are asked for. Defaults to `width`. |
| `GetUrl(string src, KlyImageModifiers modifiers)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `IKlyOverlayService`

`Klyroo.UI` · interface

Opens dialogs from code and awaits how they end. Needs an interactive render mode and a `KlyOverlayHost` (`KlyApp` renders one), like `IToastService` needs a toaster. Call it from an event handler or `OnAfterRenderAsync`, never from `OnInitialized` or while prerendering.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `ConfirmAsync(string title, string? message = null, string? confirmLabel = null, string? cancelLabel = null, KlyColor? color = null, CancellationToken cancellationToken = null)` | `Task<bool>` | Asks a yes or no question in a dialog and returns true when the user confirms, false when they cancel or dismiss it. |
| `ShowAsync<TComponent, TResult>(IReadOnlyDictionary<string, object?>? parameters = null, KlyOverlayOptions? options = null, CancellationToken cancellationToken = null)` | `Task<KlyOverlayResult<TResult>>` | Shows `TComponent` in a dialog and completes when the dialog ends: with the value the component passed to `KlyOverlayContext.Close`, or with a canceled result after Escape, a click on the backdrop, the close button, `KlyOverlayContext.Cancel` or `cancellationToken`. The component reads its context with `[CascadingParameter] KlyOverlayContext<TResult>`. A dialog with nothing to return uses `bool`. |
| `ShowAsync<TResult>(RenderFragment content, KlyOverlayOptions? options = null, CancellationToken cancellationToken = null)` | `Task<KlyOverlayResult<TResult>>` | Shows `content` in a dialog and completes when the dialog ends, like `IKlyOverlayService.ShowAsync``. A component placed in the content ends the dialog through a `KlyOverlayContext<TResult>` cascading parameter. |

#### `IKlyRequestCookies`

`Klyroo.UI` · interface

The HTTP request a component is rendering for, as far as Klyroo needs it: the cookies and the path base. It exists for the renders that have a request and no browser, a static render and a prerender, where a component can read what the browser left in a cookie (the time zone of a data list, and the columns a user chose). Klyroo.UI is also loaded by WebAssembly and MAUI apps, where there is no ASP.NET Core request, so the library only knows this abstraction: the package `Klyroo.UI.Server` implements it over `IHttpContextAccessor` and `AddKlyrooServer()` registers it.

| Property | Type | Description |
| :--- | :--- | :--- |
| `PathBase` | `string?` | The path base of the current request (`Request.PathBase`), null or empty at the root or without a request. Cookie names below a path base carry its slug, see `KlyPathScope.CookieName`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Get(string name)` | `string?` | The value of the cookie called `name` in the current request, or `null` when there is no request or no such cookie. The name is taken as it is: scope it with `KlyPathScope.CookieName`. |

#### `IKlySelectItem`

`Klyroo.UI` · interface

What a list control reads of an option, whatever the type of its value: `KlySelectItem` is the usual implementation. The remote options of a menu (see `KlyOptionsResult`) are given as this interface, so one provider serves a menu of any value type.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Count` | `int?` | A number shown at the end of the row (matches, stock, unread): formatted for the locale, and not searched or typed ahead. |
| `Disabled` | `bool` | The option is listed but cannot be chosen. |
| `Group` | `string?` | Lists the item under a heading; items without a group are listed first, with no heading. |
| `Icon` | `string?` | An icon drawn before the label, by name like every icon of Klyroo (a Lucide name, or any Iconify name the build bundles). |
| `Label` | `string` | Text of the option. |
| `Value` | `object?` | Value the control takes when the option is chosen, as an object: its type is the one the control is bound to. |

#### `IKlySelectItem<TValue>`

`Klyroo.UI` · interface

An option with a typed value. The interface is covariant in the value, so a list of `KlySelectItem` (strings) is accepted by a control bound to a `string?` property as well as one bound to a `string`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Value` | `TValue` | Value the control takes when the option is chosen. |

#### `IToastService`

`Klyroo.UI` · interface

Shows toasts in the page's `KlyToaster`. Needs an interactive render mode.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Add(KlyToast toast)` | `ValueTask` | Shows a toast in the `KlyToaster` of the page. |

#### `ISlotOverrides`

`Klyroo.UI.Theming` · interface

Per-instance class overrides of a component's slots.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `For(string slot)` | `string?` | Gets the classes to merge into a slot, or null when there are none. |

### Enums

#### `BlogPostVariant`

`Klyroo.UI` · enum

Look of a `KlyBlogPost` card.

| Value | Description |
| :--- | :--- |
| `Outline` | A border around the card. |
| `Soft` | A tinted background. |
| `Subtle` | A tinted background with a border. |
| `Ghost` | A transparent card with the usual padding. |
| `Naked` | A transparent card that does not clip its content. |

#### `ButtonVariant`

`Klyroo.UI` · enum

Look of a `KlyButton`.

| Value | Description |
| :--- | :--- |
| `Solid` | A filled button in the semantic color. |
| `Outline` | A border in the semantic color and no fill. |
| `Soft` | A light tint of the semantic color. |
| `Subtle` | A light tint with a border. |
| `Ghost` | No border and no fill until hovered. |
| `Link` | Looks like a text link. |

#### `CardVariant`

`Klyroo.UI` · enum

Look of a `KlyCard`.

| Value | Description |
| :--- | :--- |
| `Outline` | A border around the card. |
| `Soft` | A tinted background. |
| `Subtle` | A tinted background with a border. |

#### `ChatAttachmentStatus`

`Klyroo.UI` · enum

State of a `KlyChatAttachment`.

| Value | Description |
| :--- | :--- |
| `Idle` | The file is ready. |
| `Uploading` | The file is being uploaded. |
| `Error` | The upload failed. |

#### `ChatAttachmentVariant`

`Klyroo.UI` · enum

Look of a `KlyChatAttachment`.

| Value | Description |
| :--- | :--- |
| `Card` | A chip with an icon or a small preview, the name and the size. |
| `Thumbnail` | The picture alone. |

#### `ChatChevron`

`Klyroo.UI` · enum

Which side of a collapsible block carries the chevron.

| Value | Description |
| :--- | :--- |
| `Trailing` | The chevron is at the end of the header. |
| `Leading` | The chevron is at the start of the header. |

#### `ChatMessageVariant`

`Klyroo.UI` · enum

Look of a message bubble. `ChatMessageVariant.Naked` has no surface at all.

| Value | Description |
| :--- | :--- |
| `Solid` | A filled bubble in the semantic color. |
| `Outline` | A border and no fill. |
| `Soft` | A light tint of the color. |
| `Subtle` | A light tint with a border. |
| `Naked` | No surface. |

#### `ChatRole`

`Klyroo.UI` · enum

Who wrote a `KlyChatMessage`.

| Value | Description |
| :--- | :--- |
| `User` | Written by the person using the chat. |
| `Assistant` | Written by the model or the agent. |
| `System` | A notice from the app, such as an instruction or a status. |

#### `ChatSide`

`Klyroo.UI` · enum

Which side of the conversation a message sits on.

| Value | Description |
| :--- | :--- |
| `Left` | At the start of the line. |
| `Right` | At the end of the line. |

#### `ChatStatus`

`Klyroo.UI` · enum

Where a conversation is: nothing pending, the request sent and no answer yet, the answer arriving, or failed.

| Value | Description |
| :--- | :--- |
| `Idle` | Nothing is pending. |
| `Submitted` | The request is sent and no answer has started. |
| `Streaming` | The answer is arriving. |
| `Error` | The request failed. |

#### `ChatToolVariant`

`Klyroo.UI` · enum

Look of a `KlyChatTool`.

| Value | Description |
| :--- | :--- |
| `Inline` | One line of text. |
| `Card` | A bordered block. |

#### `DrawerDirection`

`Klyroo.UI` · enum

Edge of the screen a `KlyDrawer` is docked to.

| Value | Description |
| :--- | :--- |
| `Bottom` | The bottom edge. |
| `Top` | The top edge. |
| `Left` | The left edge. |
| `Right` | The right edge. |

#### `EditorAction`

`Klyroo.UI` · enum

What a toolbar button or a slash-menu item does to the editor.

| Value | Description |
| :--- | :--- |
| `Bold` | Bold text. |
| `Italic` | Italic text. |
| `Strike` | Struck-through text. |
| `Underline` | Underlined text. |
| `Code` | Inline code. |
| `Paragraph` | A plain paragraph. |
| `Heading` | A heading; the level comes from `Level` (1 to 6). |
| `BulletList` | A list with bullets. |
| `OrderedList` | A list with numbers. |
| `TaskList` | A list of items with a checkbox each. |
| `Blockquote` | A quotation block. |
| `CodeBlock` | A block of code. |
| `HorizontalRule` | A horizontal divider. |
| `Link` | Sets or removes the link of the selection; the toolbar asks for the address in a small field. |
| `Image` | Inserts an image from an http or https address; the toolbar asks for the address in a small field. |
| `ClearFormatting` | Removes the formatting of the selection. |
| `Undo` | Undoes the last change. |
| `Redo` | Redoes the change that was undone. |
| `MoveUp` | Moves the block holding the selection up. Alt and the up arrow does the same in the text. |
| `MoveDown` | Moves the block holding the selection down. Alt and the down arrow does the same in the text. |

#### `EditorToolbarLayout`

`Klyroo.UI` · enum

Where the toolbar shows.

| Value | Description |
| :--- | :--- |
| `Fixed` | Always visible, above the text. |
| `Bubble` | Floats above the selection while some text is selected. |

#### `EmptyVariant`

`Klyroo.UI` · enum

Look of a `KlyEmpty` placeholder.

| Value | Description |
| :--- | :--- |
| `Outline` | A border around the placeholder. |
| `Soft` | A tinted background. |
| `Subtle` | A tinted background with a border. |
| `Naked` | No surface. |

#### `FieldVariant`

`Klyroo.UI` · enum

Look of text fields: input, textarea, select.

| Value | Description |
| :--- | :--- |
| `Outline` | A border around the field. |
| `Soft` | A tinted background. |
| `Subtle` | A tinted background with a border. |
| `Ghost` | No border and no background until hovered or focused. |
| `None` | A transparent field with no border, for a field placed inside a surface of your own. |

#### `KlyCalendarMode`

`Klyroo.UI` · enum

What a `KlyCalendar` selects.

| Value | Description |
| :--- | :--- |
| `Single` | One date, bound with `Value`. |
| `Multiple` | Any number of dates, bound with `Values`. |
| `Range` | A start and an end date, bound with `Range`. |

#### `KlyChartCurve`

`Klyroo.UI` · enum

The shape of the line between values of a line or area chart.

| Value | Description |
| :--- | :--- |
| `Linear` | Straight segments between values. |
| `Monotone` | A smooth curve that never overshoots the values. |
| `Step` | The line holds each value until the next one, with right angles. |

#### `KlyChartTable`

`Klyroo.UI` · enum

Where the table with the chart's data goes.

| Value | Description |
| :--- | :--- |
| `Hidden` | Visually hidden, still read by assistive technology. |
| `Visible` | Shown under the chart. |
| `Toggle` | Behind a disclosure under the chart. |

#### `KlyChartType`

`Klyroo.UI` · enum

The kind of chart `KlyChart` draws.

| Value | Description |
| :--- | :--- |
| `Line` | Values joined by a line. |
| `Area` | Values joined by a line with the area under it filled. |
| `Bar` | One bar per value, vertical or, with `Horizontal`, horizontal. |
| `Donut` | One slice per row, sized by the first category. |

#### `KlyChipPosition`

`Klyroo.UI` · enum

Corner of its content where a `KlyChip` sits.

| Value | Description |
| :--- | :--- |
| `TopRight` | Top end corner. |
| `TopLeft` | Top start corner. |
| `BottomRight` | Bottom end corner. |
| `BottomLeft` | Bottom start corner. |

#### `KlyContentTocBreakpoint`

`Klyroo.UI` · enum

The width below which `KlyContentToc` collapses behind its title.

| Value | Description |
| :--- | :--- |
| `Lg` | 1024px. |
| `Xl` | 1280px. |
| `Xxl` | 1536px. |

#### `KlyDirection`

`Klyroo.UI` · enum

The writing direction of a language: the `dir` attribute of the page.

| Value | Description |
| :--- | :--- |
| `Ltr` | Left to right. |
| `Rtl` | Right to left (Arabic, Hebrew, Persian...). |

#### `KlyFetchPriority`

`Klyroo.UI` · enum

The `fetchpriority` attribute of an image and of its preload link.

| Value | Description |
| :--- | :--- |
| `Auto` | Lets the browser decide. |
| `High` | Loads the image before others. |
| `Low` | Loads the image after others. |

#### `KlyImageFit`

`Klyroo.UI` · enum

How the image fills the requested width and height. Providers map it to their own vocabulary.

| Value | Description |
| :--- | :--- |
| `Cover` | Fill the box and crop what overflows (default). The local optimizer enlarges a smaller picture to fill the box. |
| `Contain` | Fit inside the box without cropping and without distorting. The local optimizer then pads to exactly the box (transparent, or white for JPEG); other providers leave the free space out. |
| `Fill` | Stretch to the box, distorting the picture when the ratios differ. |
| `Inside` | Resize to stay inside the box, keeping the ratio, never enlarging. |
| `Outside` | Resize to cover the box, keeping the ratio, without cropping. |

#### `KlyImageLoading`

`Klyroo.UI` · enum

The `loading` attribute of an image.

| Value | Description |
| :--- | :--- |
| `Lazy` | Wait until the image is near the viewport (the default). |
| `Eager` | Load right away. Use it for the image that is visible at first paint. |

#### `KlyMenuItemKind`

`Klyroo.UI` · enum

What a `KlyMenuItem` is.

| Value | Description |
| :--- | :--- |
| `Item` | An action, or a link when `Href` is set. |
| `Separator` | A line between two groups of items. |
| `Heading` | A non-interactive title of a group of items. |

#### `KlyOrientation`

`Klyroo.UI` · enum

Direction of a layout.

| Value | Description |
| :--- | :--- |
| `Horizontal` | Along the line of text. |
| `Vertical` | Top to bottom. |

#### `KlyOverlayKind`

`Klyroo.UI` · enum

How `IKlyOverlayService` presents the component it opens.

| Value | Description |
| :--- | :--- |
| `Modal` | A centered modal dialog (`KlyModal`). |
| `Slideover` | A panel on an edge of the screen (`KlySlideover`). |

#### `KlyProgressAnimation`

`Klyroo.UI` · enum

Animation of an indeterminate `KlyProgress`.

| Value | Description |
| :--- | :--- |
| `Carousel` | The bar slides across the track and starts again. |
| `Swing` | The bar moves back and forth. |

#### `KlyRemoteStatus`

`Klyroo.UI` · enum

Where a `KlyRemoteOptions` stands: nothing asked yet, a request on its way, a page listed, or a failure.

| Value | Description |
| :--- | :--- |
| `Idle` | Nothing was asked yet (the list has never been opened). |
| `Loading` | The first page of a search is on its way; the options of the previous search, if any, are still listed. |
| `Loaded` | A page is listed. `KlyRemoteOptions.HasMore` says whether another one exists. |
| `LoadingMore` | The next page is on its way. |
| `Error` | The last request failed; `KlyRemoteOptions.RetryAsync` repeats it. |
| `TooShort` | The term is shorter than `KlyRemoteOptions.MinLength`: nothing is asked and nothing is listed. |

#### `KlySeparatorType`

`Klyroo.UI` · enum

Line style of a `KlySeparator`.

| Value | Description |
| :--- | :--- |
| `Solid` | A continuous line. |
| `Dashed` | A dashed line. |
| `Dotted` | A dotted line. |

#### `KlySize`

`Klyroo.UI` · enum

Size of a component, which scales its text, padding and icons.

| Value | Description |
| :--- | :--- |
| `Xs` | Extra small. |
| `Sm` | Small. |
| `Md` | Medium, the default of most components. |
| `Lg` | Large. |
| `Xl` | Extra large. |

#### `KlyVariant`

`Klyroo.UI` · enum

Look of colored surfaces: badges, alerts.

| Value | Description |
| :--- | :--- |
| `Solid` | A filled surface in the semantic color. |
| `Outline` | A border in the semantic color and no fill. |
| `Soft` | A light tint of the semantic color. |
| `Subtle` | A light tint with a border. |

#### `PageCardVariant`

`Klyroo.UI` · enum

Look of a `KlyPageCard`.

| Value | Description |
| :--- | :--- |
| `Outline` | A border around the card. |
| `Soft` | A tinted background. |
| `Subtle` | A tinted background with a border. |
| `Spotlight` | A subtle card with a glow that fades in on hover. |

#### `PinInputType`

`Klyroo.UI` · enum

Characters a `KlyPinInput` accepts.

| Value | Description |
| :--- | :--- |
| `Numeric` | Digits only. |
| `Alphanumeric` | Letters and digits. |

#### `SlideoverSide`

`Klyroo.UI` · enum

Edge of the screen a `KlySlideover` comes from.

| Value | Description |
| :--- | :--- |
| `Right` | The right edge. |
| `Left` | The left edge. |
| `Top` | The top edge. |
| `Bottom` | The bottom edge. |

#### `TabsVariant`

`Klyroo.UI` · enum

Look of `KlyTabs`.

| Value | Description |
| :--- | :--- |
| `Pill` | The selected tab is a filled pill. |
| `Link` | The selected tab is marked by an underline. |

#### `ToggleVariant`

`Klyroo.UI` · enum

Look of a `KlyToggle` and of the buttons of a `KlyToggleGroup`.

| Value | Description |
| :--- | :--- |
| `Outline` | A border around the button. |
| `Soft` | A tinted background when pressed. |
| `Ghost` | No border and no background until hovered or pressed. |

### Delegates

#### `KlyOptionsProvider`

`Klyroo.UI` · delegate

Loads the options of a `KlySelectMenu` or `KlyInputMenu` from a remote source, one page at a time. The token is cancelled when a newer search replaces the request (the component also ignores a late answer that the provider did not stop). An exception shows the translated error with a retry button; it is not logged by the component.

#### `KlySelectedOptionsResolver`

`Klyroo.UI` · delegate

Returns the options of values the component has not listed (the value was restored from the address or a saved form, and its page was never loaded), so the field can show their labels. Values it cannot find may be left out: the component then shows their raw value. The values come as text, whatever the type the control is bound to: strings as they are, numbers, dates and `Guid`s in the invariant culture, enums by name. Each item returned is matched by the text of its value.

## Klyroo.UI.Server

Static classes and extensions (1) · Classes (1)

### Static classes and extensions

#### `KlyServerExtensions`

`Klyroo.UI.Server` · service

Registration of the server side of Klyroo.UI.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooServer(IServiceCollection services)` | `IServiceCollection` | Lets Klyroo components read the request they render for in a static render and in a prerender: it registers `IHttpContextAccessor` (`AddHttpContextAccessor`) and an `IKlyRequestCookies` over it. Call it in the server `Program.cs` only: a WebAssembly client has no request, and Klyroo.UI then keeps its defaults (the data list cuts days in UTC until the browser is interactive). Safe to call more than once. |
| `static AddKlyrooServer(IServiceCollection services, Action<KlyrooServerOptions>? configure)` | `IServiceCollection` | Lets Klyroo components read the request they render for in a static render and in a prerender, and configures `KlyrooServerOptions`. |

### Classes

#### `KlyrooServerOptions`

`Klyroo.UI.Server` · class

Options of `KlyServerExtensions.AddKlyrooServer`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `SetCacheHeaders` (get, set) | `bool` | Marks the HTTP response `Cache-Control: private` (when no `private` or `no-store` directive is already set) and adds `Cookie` to `Vary` whenever a Klyroo component reads a request cookie during a static render or a prerender (`true` by default), so a shared cache or CDN does not serve one user's cookie-shaped HTML (time zone, column preferences) to another. Turn this off only when your own middleware sets the caching headers. |

## Klyroo.UI.Images

Static classes and extensions (1) · Classes (9)

### Static classes and extensions

#### `KlyImageProviders`

`Klyroo.UI.Images` · service

Registration helpers: `o.Images.AddCloudinary("demo")`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddBunny(KlyImageSettings images, string baseUrl, bool makeDefault = false)` | `KlyImageSettings` | Registers `BunnyImageProvider` for a Bunny Optimizer base URL. `makeDefault` also makes it the default provider. |
| `static AddCloudflareImages(KlyImageSettings images, string? zone = null, bool makeDefault = false)` | `KlyImageSettings` | Registers `CloudflareImagesProvider`, optionally with the address of the zone. Without it URLs are root-relative. `makeDefault` also makes it the default provider. |
| `static AddCloudinary(KlyImageSettings images, string cloudName, bool makeDefault = false)` | `KlyImageSettings` | Registers `CloudinaryImageProvider`. `makeDefault` also makes it the default provider. |
| `static AddImageKit(KlyImageSettings images, string urlEndpoint, bool makeDefault = false)` | `KlyImageSettings` | Registers `ImageKitImageProvider` for an ImageKit URL endpoint. `makeDefault` also makes it the default provider. |
| `static AddImgix(KlyImageSettings images, string domain, bool makeDefault = false)` | `KlyImageSettings` | Registers `ImgixImageProvider` for the host of an imgix source. `makeDefault` also makes it the default provider. |
| `static AddLocal(KlyImageSettings images, string endpoint = "_klyroo/img", string? version = null, bool makeDefault = false)` | `KlyImageSettings` | Registers the URL builder of the local optimizer under the name `local` (see `KlyLocalImageProvider`). |
| `static AddNetlify(KlyImageSettings images, string? baseUrl = null, bool makeDefault = false)` | `KlyImageSettings` | Registers `NetlifyImageProvider`, optionally with a site address for URLs that leave the site. `makeDefault` also makes it the default provider. |
| `static AddTemplate(KlyImageSettings images, string name, string template, bool makeDefault = false)` | `KlyImageSettings` | Registers a `TemplateImageProvider` under `name`, for any service whose URLs follow the `template`. `makeDefault` also makes it the default provider. |
| `static AddVercel(KlyImageSettings images, IReadOnlyList<int>? sizes = null, bool makeDefault = false)` | `KlyImageSettings` | Registers `VercelImageProvider`. `sizes` must list the widths of the `images.sizes` configuration of the Vercel project. `makeDefault` also makes it the default provider. |

### Classes

#### `BunnyImageProvider`

`Klyroo.UI.Images` · class

Bunny Optimizer dynamic images (https://bunny.net/docs/optimizer/dynamic-images/overview.md): query parameters on a Pull Zone URL, for example `https://yourzone.b-cdn.net/image.jpg?quality=75&format=webp`, `?width=500`, `?height=300`. The page documents no fit parameter, no accepted range for quality and no way to pass a remote source, so fit and blur are ignored and the source is a path in the zone. `baseUrl` is the Pull Zone address, for example `https://yourzone.b-cdn.net`.

| Constructor | Description |
| :--- | :--- |
| `BunnyImageProvider(string baseUrl, string name = "bunny")` | Bunny Optimizer dynamic images (https://bunny.net/docs/optimizer/dynamic-images/overview.md): query parameters on a Pull Zone URL, for example `https://yourzone.b-cdn.net/image.jpg?quality=75&format=webp`, `?width=500`, `?height=300`. The page documents no fit parameter, no accepted range for quality and no way to pass a remote source, so fit and blur are ignored and the source is a path in the zone. `baseUrl` is the Pull Zone address, for example `https://yourzone.b-cdn.net`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `CloudflareImagesProvider`

`Klyroo.UI.Images` · class

Cloudflare Images transformations via URL (https://developers.cloudflare.com/images/transform-images/transform-via-url/): `https://<ZONE>/cdn-cgi/image/<OPTIONS>/<SOURCE-IMAGE>`, options comma separated, the source an absolute path on the origin or an absolute URL. Options: `width`, `height`, `quality` (1 to 100), `format` (auto, avif, webp, jpeg), `fit` (scale-down, contain, cover, crop, pad, squeeze, scale-up), `blur` (0 to 250). Fit mapping: cover and outside are `cover`, contain `contain`, fill `squeeze`, inside `scale-down`. Formats the service does not list (png, gif, svg) are not requested. Remote sources must be listed in `KlyImageSettings.Domains` and allowed in your Cloudflare zone. `zone` is optional, for example `https://example.com`; without it URLs are root-relative.

| Constructor | Description |
| :--- | :--- |
| `CloudflareImagesProvider(string? zone = null, string name = "cloudflare")` | Cloudflare Images transformations via URL (https://developers.cloudflare.com/images/transform-images/transform-via-url/): `https://<ZONE>/cdn-cgi/image/<OPTIONS>/<SOURCE-IMAGE>`, options comma separated, the source an absolute path on the origin or an absolute URL. Options: `width`, `height`, `quality` (1 to 100), `format` (auto, avif, webp, jpeg), `fit` (scale-down, contain, cover, crop, pad, squeeze, scale-up), `blur` (0 to 250). Fit mapping: cover and outside are `cover`, contain `contain`, fill `squeeze`, inside `scale-down`. Formats the service does not list (png, gif, svg) are not requested. Remote sources must be listed in `KlyImageSettings.Domains` and allowed in your Cloudflare zone. `zone` is optional, for example `https://example.com`; without it URLs are root-relative. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `SupportsBlur` | `bool` | True when the provider can blur (see `KlyImageModifiers.Blur`), which makes `Placeholder` look better. Defaults to false. |
| `ValidateDomains` | `bool` | True when `KlyImageSettings.Domains` applies: a remote source (`https://host/...`) whose host is not listed is passed through untouched instead of being sent to the provider. Defaults to false, for providers whose sources are paths in their own storage. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `CloudinaryImageProvider`

`Klyroo.UI.Images` · class

Cloudinary. Delivery URL structure from https://cloudinary.com/documentation/image_transformations: `https://res.cloudinary.com/<cloud_name>/<asset_type>/<delivery_type>/<transformations>/<version>/<public_id>.<extension>`, parameters of one component separated by commas, for example `https://res.cloudinary.com/demo/image/upload/c_fill,h_400,w_250/q_auto/f_auto/docs/shoes.jpg`. Parameters: `w_`, `h_` (https://cloudinary.com/documentation/resizing_and_cropping), `c_` crop modes (`fill`, `fit`, `scale`, `limit`, https://cloudinary.com/documentation/transformation_reference), `q_1..100` (https://cloudinary.com/documentation/image_optimization), `f_<format>` / `f_auto`, `e_blur[:<strength>]` (transformation reference). An absolute source uses the fetch delivery type, `image/fetch/<transformations>/<remote url>` (https://cloudinary.com/documentation/fetch_remote_images); the `src` is a public id otherwise. `IKlyImageProvider.ValidateDomains` is off: fetching is limited by your Cloudinary account settings. Fit mapping: cover is `c_fill`, contain `c_fit`, fill `c_scale`, inside `c_limit`; outside has no equivalent and uses `c_fill`.

| Constructor | Description |
| :--- | :--- |
| `CloudinaryImageProvider(string cloudName, string name = "cloudinary")` | Cloudinary. Delivery URL structure from https://cloudinary.com/documentation/image_transformations: `https://res.cloudinary.com/<cloud_name>/<asset_type>/<delivery_type>/<transformations>/<version>/<public_id>.<extension>`, parameters of one component separated by commas, for example `https://res.cloudinary.com/demo/image/upload/c_fill,h_400,w_250/q_auto/f_auto/docs/shoes.jpg`. Parameters: `w_`, `h_` (https://cloudinary.com/documentation/resizing_and_cropping), `c_` crop modes (`fill`, `fit`, `scale`, `limit`, https://cloudinary.com/documentation/transformation_reference), `q_1..100` (https://cloudinary.com/documentation/image_optimization), `f_<format>` / `f_auto`, `e_blur[:<strength>]` (transformation reference). An absolute source uses the fetch delivery type, `image/fetch/<transformations>/<remote url>` (https://cloudinary.com/documentation/fetch_remote_images); the `src` is a public id otherwise. `IKlyImageProvider.ValidateDomains` is off: fetching is limited by your Cloudinary account settings. Fit mapping: cover is `c_fill`, contain `c_fit`, fill `c_scale`, inside `c_limit`; outside has no equivalent and uses `c_fill`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `SupportsBlur` | `bool` | True when the provider can blur (see `KlyImageModifiers.Blur`), which makes `Placeholder` look better. Defaults to false. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `ImageKitImageProvider`

`Klyroo.UI.Images` · class

ImageKit URL transformations, path form (https://imagekit.io/docs/transformations): `https://ik.imagekit.io/ikmedia/tr:h-300,w-400,f-jpg,cp-false/docs_images/examples/example_food_3.jpg`. `w-`, `h-` (https://imagekit.io/docs/image-transformation), `q-` and `f-` (https://imagekit.io/docs/image-optimization), `bl-` 1 to 100 (https://imagekit.io/docs/effects-and-enhancements), `c-` crop strategies (https://imagekit.io/docs/image-resize-and-crop). Fit mapping: cover is `c-maintain_ratio` (the default strategy, cropping to the exact size), contain and inside `c-at_max`, fill `c-force`, outside `c-at_least`. `urlEndpoint` is your endpoint, for example `https://ik.imagekit.io/ikmedia`.

| Constructor | Description |
| :--- | :--- |
| `ImageKitImageProvider(string urlEndpoint, string name = "imagekit")` | ImageKit URL transformations, path form (https://imagekit.io/docs/transformations): `https://ik.imagekit.io/ikmedia/tr:h-300,w-400,f-jpg,cp-false/docs_images/examples/example_food_3.jpg`. `w-`, `h-` (https://imagekit.io/docs/image-transformation), `q-` and `f-` (https://imagekit.io/docs/image-optimization), `bl-` 1 to 100 (https://imagekit.io/docs/effects-and-enhancements), `c-` crop strategies (https://imagekit.io/docs/image-resize-and-crop). Fit mapping: cover is `c-maintain_ratio` (the default strategy, cropping to the exact size), contain and inside `c-at_max`, fill `c-force`, outside `c-at_least`. `urlEndpoint` is your endpoint, for example `https://ik.imagekit.io/ikmedia`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `SupportsBlur` | `bool` | True when the provider can blur (see `KlyImageModifiers.Blur`), which makes `Placeholder` look better. Defaults to false. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `ImgixImageProvider`

`Klyroo.UI.Images` · class

imgix Rendering API. Query string form, for example `https://assets.imgix.net/docs/road-sm.jpg?w=300&h=100&fit=crop` (https://docs.imgix.com/apis/rendering/size/fit). `w`/`h`: https://docs.imgix.com/apis/rendering/size/w and .../size/h; `q` 0 to 100 (https://docs.imgix.com/apis/rendering/format/q); `fm` (https://docs.imgix.com/apis/rendering/format/fm); `blur` 0 to 2000 (https://docs.imgix.com/apis/rendering/stylize/blur); `auto=format` when the format is `auto` (https://docs.imgix.com/apis/rendering/auto/auto). Fit mapping: cover is `crop`, contain `clip`, fill `scale`, inside `max`; outside has no equivalent and uses `crop`. `domain` is the host of your source, for example `assets.imgix.net`. Absolute sources (imgix Web Proxy sources) are not supported: they need a signing key, which cannot be part of a public URL built on both the server and the client. Use a Web Folder or S3 source and give paths.

| Constructor | Description |
| :--- | :--- |
| `ImgixImageProvider(string domain, string name = "imgix")` | imgix Rendering API. Query string form, for example `https://assets.imgix.net/docs/road-sm.jpg?w=300&h=100&fit=crop` (https://docs.imgix.com/apis/rendering/size/fit). `w`/`h`: https://docs.imgix.com/apis/rendering/size/w and .../size/h; `q` 0 to 100 (https://docs.imgix.com/apis/rendering/format/q); `fm` (https://docs.imgix.com/apis/rendering/format/fm); `blur` 0 to 2000 (https://docs.imgix.com/apis/rendering/stylize/blur); `auto=format` when the format is `auto` (https://docs.imgix.com/apis/rendering/auto/auto). Fit mapping: cover is `crop`, contain `clip`, fill `scale`, inside `max`; outside has no equivalent and uses `crop`. `domain` is the host of your source, for example `assets.imgix.net`. Absolute sources (imgix Web Proxy sources) are not supported: they need a signing key, which cannot be part of a public URL built on both the server and the client. Use a Web Folder or S3 source and give paths. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `SupportsBlur` | `bool` | True when the provider can blur (see `KlyImageModifiers.Blur`), which makes `Placeholder` look better. Defaults to false. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `KlyLocalImageProvider`

`Klyroo.UI.Images` · class

URLs of the endpoint of `Klyroo.UI.Images.Local` (`app.MapKlyrooImages()`). This class only builds URLs, so it is safe in a WebAssembly client: register it on the server and the client with `KlyImageProviders.AddLocal`. Widths are rounded up to `KlyLocalImageProvider.Steps` and the quality to a multiple of 5, so a page can only ask for a bounded set of variants; a height is scaled with its width so the aspect ratio holds, and `KlyLocalImageProvider.GetEffectiveWidth` reports the rounded width for the `srcset`. `v` is a cache-busting token of yours (an application version): with it the server answers with `immutable` caching. The default endpoint is relative (`KlyLocalImageProvider.DefaultEndpoint`): the browser resolves it against `<base href>`, so an app deployed under a path base (`UsePathBase("/x")` with `<base href="/x/">`) reaches its own optimizer at `/x/_klyroo/img` in every render mode, and not the one of another app on the same origin. An endpoint you pass that starts with "/" is used as it is, which is right only for an app served at the root.

| Constructor | Description |
| :--- | :--- |
| `KlyLocalImageProvider(string endpoint = "_klyroo/img", string? version = null, string name = "local")` | URLs of the endpoint of `Klyroo.UI.Images.Local` (`app.MapKlyrooImages()`). This class only builds URLs, so it is safe in a WebAssembly client: register it on the server and the client with `KlyImageProviders.AddLocal`. Widths are rounded up to `KlyLocalImageProvider.Steps` and the quality to a multiple of 5, so a page can only ask for a bounded set of variants; a height is scaled with its width so the aspect ratio holds, and `KlyLocalImageProvider.GetEffectiveWidth` reports the rounded width for the `srcset`. `v` is a cache-busting token of yours (an application version): with it the server answers with `immutable` caching. The default endpoint is relative (`KlyLocalImageProvider.DefaultEndpoint`): the browser resolves it against `<base href>`, so an app deployed under a path base (`UsePathBase("/x")` with `<base href="/x/">`) reaches its own optimizer at `/x/_klyroo/img` in every render mode, and not the one of another app on the same origin. An endpoint you pass that starts with "/" is used as it is, which is right only for an app served at the root. |

| Field | Type | Description |
| :--- | :--- | :--- |
| `const DefaultEndpoint = "_klyroo/img"` | `string` | The endpoint when none is given, relative to the base of the app (`<base href>`), which `MapKlyrooImages` maps below the path base. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `static Steps` | `IReadOnlyList<int>` | The widths the local optimizer produces (the device and icon widths of Next.js, which suit most layouts). |
| `SupportsBlur` | `bool` | True when the provider can blur (see `KlyImageModifiers.Blur`), which makes `Placeholder` look better. Defaults to false. |
| `ValidateDomains` | `bool` | True when `KlyImageSettings.Domains` applies: a remote source (`https://host/...`) whose host is not listed is passed through untouched instead of being sent to the provider. Defaults to false, for providers whose sources are paths in their own storage. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetEffectiveWidth(int width)` | `int` | The width, in pixels, of the image the URL for `width` will really return, for the `w` descriptors of the `srcset`. A provider that rounds widths to a fixed set (`local`, `vercel`) returns the rounded one, so that a "400w" candidate is not a 512 pixel image; the others produce the width they are asked for. Defaults to `width`. |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |
| `static SnapQuality(int quality)` | `int` | The quality rounded up to a multiple of 5 within 5 to 100. |
| `static SnapWidth(int width)` | `int` | The smallest step that is at least `width`, the largest when none is. |

#### `NetlifyImageProvider`

`Klyroo.UI.Images` · class

Netlify Image CDN (https://docs.netlify.com/image-cdn/overview/): `/.netlify/images?url=/owl.jpeg&fit=cover&w=50&h=50`. `url`, `w`, `h`, `fit` (contain, cover, fill), `fm` (avif, jpg, png, webp, gif), `q` 1 to 100. The service has no blur option (only `fm=blurhash`), so `IKlyImageProvider.SupportsBlur` is false. Fit mapping: cover and outside are `cover`, contain and inside `contain`, fill `fill`. Remote sources need `remote_images` in `netlify.toml` and an entry in `KlyImageSettings.Domains` here. `baseUrl` is optional (a site address), for URLs that leave the site.

| Constructor | Description |
| :--- | :--- |
| `NetlifyImageProvider(string? baseUrl = null, string name = "netlify")` | Netlify Image CDN (https://docs.netlify.com/image-cdn/overview/): `/.netlify/images?url=/owl.jpeg&fit=cover&w=50&h=50`. `url`, `w`, `h`, `fit` (contain, cover, fill), `fm` (avif, jpg, png, webp, gif), `q` 1 to 100. The service has no blur option (only `fm=blurhash`), so `IKlyImageProvider.SupportsBlur` is false. Fit mapping: cover and outside are `cover`, contain and inside `contain`, fill `fill`. Remote sources need `remote_images` in `netlify.toml` and an entry in `KlyImageSettings.Domains` here. `baseUrl` is optional (a site address), for URLs that leave the site. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `ValidateDomains` | `bool` | True when `KlyImageSettings.Domains` applies: a remote source (`https://host/...`) whose host is not listed is passed through untouched instead of being sent to the provider. Defaults to false, for providers whose sources are paths in their own storage. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `TemplateImageProvider`

`Klyroo.UI.Images` · class

Any service whose URLs follow a pattern. Placeholders: `{src}` (the source as given), `{encodedSrc}` (percent-encoded), `{width}`, `{height}`, `{quality}`, `{format}`; a placeholder without a value becomes an empty string. Example: `new TemplateImageProvider("acme", "https://img.acme.dev/{width}x{height}/q{quality}.{format}?u={encodedSrc}")`.

| Constructor | Description |
| :--- | :--- |
| `TemplateImageProvider(string name, string template, bool validateDomains = false)` | Any service whose URLs follow a pattern. Placeholders: `{src}` (the source as given), `{encodedSrc}` (percent-encoded), `{width}`, `{height}`, `{quality}`, `{format}`; a placeholder without a value becomes an empty string. Example: `new TemplateImageProvider("acme", "https://img.acme.dev/{width}x{height}/q{quality}.{format}?u={encodedSrc}")`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `ValidateDomains` | `bool` | True when `KlyImageSettings.Domains` applies: a remote source (`https://host/...`) whose host is not listed is passed through untouched instead of being sent to the provider. Defaults to false, for providers whose sources are paths in their own storage. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

#### `VercelImageProvider`

`Klyroo.UI.Images` · class

Vercel Image Optimization (https://vercel.com/docs/image-optimization): `/_vercel/image?url={link/to/src/image}&w=3840&q=75`. `url` is a relative or absolute address, `w` the width ("No height is needed since the source image aspect ratio is preserved") and `q` 1 to 100. The allowed widths come from the `sizes` of the `images` configuration (https://vercel.com/docs/build-output-api/configuration), so `sizes` must list the same values: the width is rounded up to the next one (the largest when none fits), and when no width is asked for the largest is used because `w` is required. Height, fit and format are not parameters of the service and are ignored (the docs do not say how the format is chosen). Remote sources must be allowed by the project's `domains` or `remotePatterns` and listed in `KlyImageSettings.Domains`. The quality must be one of the project's `qualities` when it sets them; the default is 75, as in the docs.

| Constructor | Description |
| :--- | :--- |
| `VercelImageProvider(IReadOnlyList<int>? sizes = null, string name = "vercel")` | Vercel Image Optimization (https://vercel.com/docs/image-optimization): `/_vercel/image?url={link/to/src/image}&w=3840&q=75`. `url` is a relative or absolute address, `w` the width ("No height is needed since the source image aspect ratio is preserved") and `q` 1 to 100. The allowed widths come from the `sizes` of the `images` configuration (https://vercel.com/docs/build-output-api/configuration), so `sizes` must list the same values: the width is rounded up to the next one (the largest when none fits), and when no width is asked for the largest is used because `w` is required. Height, fit and format are not parameters of the service and are ignored (the docs do not say how the format is chosen). Remote sources must be allowed by the project's `domains` or `remotePatterns` and listed in `KlyImageSettings.Domains`. The quality must be one of the project's `qualities` when it sets them; the default is 75, as in the docs. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CanResize` | `bool` | False when the provider cannot resize (`none`): `KlyImage` then renders no `srcset` and no `sizes`. |
| `Name` | `string` | The name used by `Provider` parameters and `KlyImageSettings.DefaultProvider`. |
| `ValidateDomains` | `bool` | True when `KlyImageSettings.Domains` applies: a remote source (`https://host/...`) whose host is not listed is passed through untouched instead of being sent to the provider. Defaults to false, for providers whose sources are paths in their own storage. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `GetEffectiveWidth(int width)` | `int` | The width, in pixels, of the image the URL for `width` will really return, for the `w` descriptors of the `srcset`. A provider that rounds widths to a fixed set (`local`, `vercel`) returns the rounded one, so that a "400w" candidate is not a 512 pixel image; the others produce the width they are asked for. Defaults to `width`. |
| `GetUrl(string src, KlyImageModifiers m)` | `string` | The URL of `src` with `modifiers` applied. `src` has a leading slash when it is a local path. |

## Klyroo.UI.Images.Local

Static classes and extensions (2) · Classes (1)

### Static classes and extensions

#### `KlyImagesLocalEndpointExtensions`

`Microsoft.AspNetCore.Builder` · service

Maps the endpoint of the local image optimizer.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static MapKlyrooImages(IEndpointRouteBuilder endpoints, string? pattern = null)` | `IEndpointConventionBuilder` | Maps the image endpoint (`GET /_klyroo/img?src=/img/a.png&w=640&f=webp`). Defaults to the `KlyLocalImagesOptions.Endpoint` that `KlyImagesLocalServiceCollectionExtensions.AddKlyrooImagesLocal` was given. The pattern is relative to the path base: with `app.UsePathBase("/x")` the endpoint answers at `/x/_klyroo/img`, where the relative URLs of `KlyLocalImageProvider` lead. Call `app.UseRouting()` right after `UsePathBase`, because `WebApplication` otherwise adds routing before it and the prefix is never stripped. |

#### `KlyImagesLocalServiceCollectionExtensions`

`Microsoft.Extensions.DependencyInjection` · service

Registers the local image optimizer in an `IServiceCollection`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooImagesLocal(IServiceCollection services, Action<KlyLocalImagesOptions>? configure = null)` | `IServiceCollection` | Registers the local image optimizer and the `local` provider of `KlyImage` (the default provider unless `KlyLocalImagesOptions.MakeDefaultProvider` is false). Call it where `AddKlyrooUi` is called and map the endpoint with `app.MapKlyrooImages()`. In a WebAssembly client, register only the URL builder: `o.Images.AddLocal(endpoint)`. |

### Classes

#### `KlyLocalImagesOptions`

`Klyroo.UI.Images.Local` · class

Settings of the local image optimizer. Every limit has a safe default; the endpoint serves local images of the web root and, only for the hosts in `KlyLocalImagesOptions.AllowedRemoteHosts`, remote ones.

| Property | Type | Description |
| :--- | :--- | :--- |
| `AllowedRemoteHosts` | `List<string>` | Hosts whose images may be fetched and optimized (`"images.example.com"`). Empty by default: no remote image is ever fetched. Only the default port of the scheme (80, 443) is reachable; an entry that names a port (`"images.example.com:8443"`) allows that port and no other. They are also added to `KlyrooOptions.Images.Domains`, so `KlyImage` only sends these hosts here. |
| `AllowPrivateNetworkAddresses` (get, set) | `bool` | Allows fetching from loopback, private, link-local and other non-public addresses. Off by default: a listed host that resolves to an internal address (a DNS rebinding, a misconfiguration) is refused instead of reaching your network. |
| `CacheDirectory` (get, set) | `string` | Folder of the disk cache. Defaults to `klyroo-images` in the temporary folder; use a folder that survives restarts in production. |
| `CacheMaxAge` (get, set) | `TimeSpan` | `max-age` of responses to URLs without the `v` token. Default 1 hour. |
| `DefaultQuality` (get, set) | `int` | Quality when the request names none. Default 80. A requested quality is clamped to 1 to 100 and rounded up to a multiple of 5. |
| `Endpoint` (get, set) | `string` | Where `MapKlyrooImages` maps the endpoint and where the `local` provider points. Default `_klyroo/img`, relative to the base of the app (see `KlyLocalImageProvider.DefaultEndpoint`). |
| `FileProvider` (get, set) | `IFileProvider?` | The files local sources are read from, `src=/img/a.png` being resolved against its root. Defaults to the web root (`wwwroot`, through `IWebHostEnvironment.WebRootFileProvider`). Only files below the root are reachable, and a provider that excludes hidden files (the physical one does) keeps them out. |
| `MakeDefaultProvider` (get, set) | `bool` | Makes `local` the default provider of `KlyrooOptions.Images`. Default true. |
| `MaxCacheBytes` (get, set) | `long` | Size limit of the disk cache, counted in allocated disk space (every file rounds up to 4 KiB blocks and costs an entry, every folder a block). Once it is exceeded the variants that were used least recently are deleted, in the background. Default 512 MiB. |
| `MaxConcurrency` (get, set) | `int` | Images processed at the same time. Default: the number of processors. |
| `MaxHeight` (get, set) | `int` | Requested heights above this are clamped, and no output is taller. Default 4096. |
| `MaxInputPixels` (get, set) | `long` | Largest source in pixels (width times height), which bounds the memory of a decode. Default 100 million. A request without a size is downscaled to `KlyLocalImagesOptions.MaxWidth` by `KlyLocalImagesOptions.MaxHeight`. |
| `MaxRemoteBytes` (get, set) | `long` | Largest remote source, in bytes. Default 10 MiB. |
| `MaxRemoteFetches` (get, set) | `int` | Remote fetches running at the same time. They have their own limit so a slow origin cannot take processing slots away. Default 8. |
| `MaxSourceBytes` (get, set) | `long` | Largest local source, in bytes. Default 25 MiB. |
| `MaxVariantsPerSource` (get, set) | `int` | Variants kept per source image. A new variant beyond it replaces the least recently used one, so junk requests cannot lock real variants out. Default 32. |
| `MaxWidth` (get, set) | `int` | Requested widths above this are clamped, and no output is wider. Default 4096. |
| `QueueTimeout` (get, set) | `TimeSpan` | How long a request waits for a free processing slot before it answers 503. Default 5 seconds. |
| `RasterizeSvg` (get, set) | `bool` | Whether an SVG may be rasterized when the request names a raster format (`f=webp`). An SVG is otherwise served as it is (with a sandboxing `Content-Security-Policy`), and libvips never sees it. Rasterizing hands the file to an SVG renderer, so turn it on only for SVGs you trust. Default false. |
| `RateLimitPermits` (get, set) | `int` | Computations (every request that is not answered from the cache, and every remote fetch) one client may start per `KlyLocalImagesOptions.RateLimitWindow`. Beyond it the endpoint answers 429 with `Retry-After`; cached variants are always served. Clients are told apart by their address (the /64 prefix for IPv6), so behind a reverse proxy call `UseForwardedHeaders` first, otherwise all visitors share one allowance. 0 turns the limit off. Default 600. |
| `RateLimitWindow` (get, set) | `TimeSpan` | The window of `KlyLocalImagesOptions.RateLimitPermits`. Default 1 minute. |
| `RemoteCacheTtl` (get, set) | `TimeSpan` | How long a cached variant of a remote image is reused before it is fetched again. Default 24 hours. |
| `RemoteHandlerFactory` (get, set) | `Func<HttpMessageHandler>?` | Replaces the handler that fetches remote images (tests, a proxy). The default refuses redirects and non-public addresses. |
| `RemoteTimeout` (get, set) | `TimeSpan` | Time allowed to fetch a remote source. Default 10 seconds. |
| `Version` (get, set) | `string?` | A token of yours (an application version, a deploy id) added as `v` to every URL. A request that carries it is answered with `Cache-Control: public, max-age=31536000, immutable`; change it when the source images change. Without it responses are cached for `KlyLocalImagesOptions.CacheMaxAge` and revalidated with the `ETag`. |
| `Widths` (get, set) | `IReadOnlyList<int>` | The widths the endpoint produces: a requested width is rounded up to the next one (the largest when none fits). That bounds the variants one image can have, so a client cannot fill the cache or burn CPU by asking for every width. When the endpoint changes the width, a requested height is scaled by the same factor so the crop keeps its ratio. Defaults to `KlyLocalImageProvider.Steps`. |
