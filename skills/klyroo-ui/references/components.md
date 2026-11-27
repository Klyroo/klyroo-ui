# Components quick reference

<!-- Generated from the Klyroo.UI docs bundle by build/Klyroo.Stack.DocsBundle. Do not edit: run
     dotnet run --project build/Klyroo.Stack.DocsBundle -- --skill-reference  to refresh it. -->

158 components of Klyroo.UI 0.1.0-preview.1, by family. Every parameter listed exists in the library; `Class` and `Ui` are accepted by every styled component. Parameters marked `@bind` take `@bind-Name`. For types, defaults, descriptions and examples, call the `get-component` and `get-component-metadata` tools of the MCP server or open `/components/{slug}` on the docs site.

## Elements

### Button

`/components/button`. A button, or a link styled as one, in every color and variant.

- `<KlyButton>`
  - Parameters: `Block`, `Class`, `Color`, `Disabled`, `Href`, `Label`, `Loading`, `Size`, `Square`, `Target`, `Type`, `Ui`, `Variant`
  - Events: `OnClick`
  - Slots: `LeadingIcon`, `TrailingIcon`, `ChildContent`
  - `Ui` (`ButtonUi`): `Base`, `Label`, `LeadingIcon`, `TrailingIcon`

### Badge

`/components/badge`. A short label for a status, a count or a category.

- `<KlyBadge>`
  - Parameters: `Class`, `Color`, `Label`, `Size`, `Ui`, `Variant`
  - Slots: `LeadingIcon`, `TrailingIcon`, `ChildContent`
  - `Ui` (`BadgeUi`): `Base`, `Label`, `LeadingIcon`, `TrailingIcon`

### Avatar

`/components/avatar`. An image of a person, falling back to initials when there is none or it fails to load.

- `<KlyAvatar>`
  - Parameters: `Alt`, `Class`, `Color`, `ImageOptions`, `Size`, `Src`, `Text`, `Ui`
  - Slots: `Icon`
  - `Ui` (`AvatarUi`): `Root`, `Image`, `Fallback`

### Card

`/components/card`. A surface grouping a header, a body and a footer.

- `<KlyCard>`
  - Parameters: `Class`, `Ui`, `Variant`
  - Slots: `Footer`, `Header`, `ChildContent`
  - `Ui` (`CardUi`): `Root`, `Header`, `Body`, `Footer`

### Alert

`/components/alert`. A callout that draws attention to a message.

- `<KlyAlert>`
  - Parameters: `ActionItems`, `Class`, `Close`, `Color`, `DefaultOpen`, `Description`, `Details`, `Live`, `StatusIcon`, `Title`, `Ui`, `Variant`
  - Events: `OnClose`
  - Slots: `Actions`, `Icon`, `ChildContent`
  - `Ui` (`AlertUi`): `Root`, `Icon`, `Wrapper`, `Title`, `Description`, `Actions`, `Close`, `Toggle`, `ToggleIcon`, `DetailsPanel`

### Kbd

`/components/kbd`. A keyboard key or shortcut, in four variants and five sizes.

- `<KlyKbd>`
  - Parameters: `Class`, `Color`, `Size`, `Ui`, `Value`, `Variant`
  - Slots: `ChildContent`
  - `Ui` (`KbdUi`): `Base`

### Separator

`/components/separator`. A line between sections, optionally with a label or an icon, horizontal or vertical.

- `<KlySeparator>`
  - Parameters: `Class`, `Color`, `Label`, `Orientation`, `Size`, `Type`, `Ui`
  - Slots: `Icon`, `ChildContent`
  - `Ui` (`SeparatorUi`): `Root`, `Border`, `Container`, `Label`, `Icon`

### Skeleton

`/components/skeleton`. A pulsing placeholder for content that is loading. Mark the loading region with aria-busy.

- `<KlySkeleton>`
  - Parameters: `Class`, `Ui`
  - `Ui` (`SkeletonUi`): `Base`

### InfiniteScroll

`/components/infinite-scroll`. A sentinel that asks for the next items when it comes into view, with a button and a link for keyboards, screen readers and pages without .NET.

- `<KlyInfiniteScroll>`
  - Parameters: `Class`, `HasMore`, `Href`, `Label`, `Loading`, `Root`, `RootMargin`, `ShowEnd`, `Ui`
  - Events: `OnLoadMore`
  - `Ui` (`InfiniteScrollUi`): `Root`, `Sentinel`, `Trigger`, `Spinner`, `Status`, `End`, `Marker`

### Chip

`/components/chip`. A small dot or count placed on a corner of another element.

- `<KlyChip>`
  - Parameters: `Class`, `Color`, `Inset`, `Label`, `Position`, `Show`, `Size`, `Standalone`, `Text`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`ChipUi`): `Root`, `Base`

### Link

`/components/link`. An anchor that knows whether it points at the current page and says so with aria-current.

- `<KlyLink>`
  - Parameters: `Active`, `ActiveClass`, `Class`, `Disabled`, `Exact`, `Href`, `InactiveClass`, `Label`, `Raw`, `Target`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`LinkUi`): `Base`

### Icon

`/components/icon`. Any Iconify icon by name (i-ph-house, mdi:home, a bare Lucide name), bundled into the app at build time, with the Lucide icons that components use built in.

- `<KlyIcon>`
  - Parameters: `Class`, `Label`, `Name` (required), `Size`, `Ui`
  - `Ui` (`IconUi`): `Base`

### Container

`/components/container`. A centered wrapper with a maximum width and responsive padding.

- `<KlyContainer>`
  - Parameters: `As`, `Class`, `Size`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`ContainerUi`): `Base`

### Progress

`/components/progress`. A determinate or indeterminate progress bar, with optional step labels.

- `<KlyProgress>`
  - Parameters: `Animation`, `AriaLabel`, `Class`, `Color`, `Max`, `Size`, `Status`, `Steps`, `Ui`, `Value`
  - `Ui` (`ProgressUi`): `Root`, `Status`, `Base`, `Indicator`, `Steps`, `Step`

### Banner

`/components/banner`. An announcement bar. Its close button remembers the dismissal per Id in the browser, static pages included.

- `<KlyBanner>`
  - Parameters: `Class`, `Close`, `Color`, `Description`, `Href`, `Id`, `Target`, `Title`, `Ui`
  - Slots: `Actions`, `Icon`, `ChildContent`
  - `Ui` (`BannerUi`): `Root`, `Container`, `Icon`, `Content`, `Title`, `Description`, `Actions`, `Close`

### Empty

`/components/empty`. An empty state with an icon, a title, a description and actions.

- `<KlyEmpty>`
  - Parameters: `ActionItems`, `Class`, `Description`, `HeadingLevel`, `Size`, `Title`, `Ui`, `Variant`
  - Slots: `Actions`, `Icon`, `ChildContent`
  - `Ui` (`EmptyUi`): `Root`, `Header`, `Icon`, `Title`, `Description`, `Actions`

### User

`/components/user`. An avatar with a name and a description, optionally a link.

- `<KlyUser>`
  - Parameters: `AvatarColor`, `AvatarSrc`, `Badge`, `Class`, `Contacts`, `Description`, `Href`, `Name`, `Orientation`, `ShowAvatar`, `Size`, `Target`, `Ui`
  - Slots: `AvatarIcon`
  - `Ui` (`UserUi`): `Root`, `Avatar`, `Wrapper`, `Name`, `Description`, `Stack`, `Badge`, `Contacts`, `Contact`, `ContactBody`, `ContactIcon`, `ContactLink`, `ContactText`, `Copy`, `CopyIcon`

### AvatarGroup

`/components/avatar-group`. A stack of avatars with a +N avatar for the overflow.

- `<KlyAvatarGroup>`
  - Parameters: `AriaLabel`, `Class`, `Items` (required), `Max`, `Size`, `Ui`
  - `Ui` (`AvatarGroupUi`): `Root`, `Avatar`

### Image

`/components/image`. An img with the width, height, srcset, sizes and loading a provider fills in.

- `<KlyImage>`
  - Parameters: `Alt` (required), `Caption`, `Class`, `Densities`, `FetchPriority`, `Fit`, `Format`, `Height`, `ImageOptions`, `Loading`, `Modifiers`, `Placeholder`, `Preload`, `Preset`, `Provider`, `Quality`, `Sizes`, `Src` (required), `Ui`, `Width`
  - `Ui` (`ImageUi`): `Figure`, `Caption`

### FieldGroup

`/components/field-group`. Attaches buttons and fields to each other: the children lose the inner radii, with CSS only.

- `<KlyFieldGroup>`
  - Parameters: `AriaLabel`, `Class`, `Orientation`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`FieldGroupUi`): `Base`

## Form

### Input

`/components/input`. A single-line text field.

- `<KlyInput>`
  - Parameters: `Class`, `ClearOnEscape`, `Color`, `Debounce`, `Disabled`, `Id`, `Immediate`, `Name`, `Placeholder`, `Required`, `Size`, `Type`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - Slots: `LeadingIcon`, `TrailingIcon`
  - `Ui` (`InputUi`): `Root`, `Base`, `Leading`, `LeadingIcon`, `Trailing`, `TrailingIcon`

### Textarea

`/components/textarea`. A multi-line text field that can grow with its content.

- `<KlyTextarea>`
  - Parameters: `AutoResize`, `Class`, `Color`, `Disabled`, `Id`, `Immediate`, `Name`, `Placeholder`, `Required`, `Rows`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`TextareaUi`): `Base`

### Checkbox

`/components/checkbox`. A native checkbox with a label and a description.

- `<KlyCheckbox>`
  - Parameters: `Class`, `Color`, `Description`, `Disabled`, `Id`, `Indeterminate`, `Label`, `Name`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`CheckboxUi`): `Root`, `Container`, `Base`, `Icon`, `IndeterminateIcon`, `Wrapper`, `Label`, `Description`

### Switch

`/components/switch`. An on/off toggle built on a native checkbox, correct even without interactivity.

- `<KlySwitch>`
  - Parameters: `Class`, `Color`, `Description`, `Disabled`, `Id`, `Label`, `Name`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`SwitchUi`): `Root`, `Container`, `Input`, `Thumb`, `Wrapper`, `Label`, `Description`

### RadioGroup

`/components/radio-group`. A set of native radio buttons in a fieldset.

- `<KlyRadioGroup>`
  - Parameters: `Class`, `Color`, `Disabled`, `Id`, `Items` (required), `Legend`, `Name`, `Orientation`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`RadioGroupUi`): `Root`, `Legend`, `Fieldset`, `Item`, `Container`, `Base`, `Indicator`, `Wrapper`, `Label`, `Description`

### Select

`/components/select`. A select-only combobox with keyboard navigation and typeahead; it posts through a hidden input.

- `<KlySelect>`
  - Parameters: `Class`, `Color`, `Disabled`, `Id`, `Items` (required), `Name`, `Placeholder`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`SelectUi`): `Root`, `Base`, `Label`, `TrailingIcon`, `Content`, `Listbox`, `Item`, `ItemLabel`, `ItemIcon`, `ItemLeadingIcon`, `ItemCount`, `Group`, `GroupLabel`, `LeadingIcon`

### FormField

`/components/form-field`. Label, description, hint and validation message wired to the control inside.

- `<KlyFormField>`
  - Parameters: `Class`, `Description`, `Error`, `Field`, `Help`, `Hint`, `Id`, `Label`, `Required`, `Size`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`FormFieldUi`): `Root`, `LabelWrapper`, `Label`, `Hint`, `Description`, `Container`, `Error`, `Help`

### Calendar

`/components/calendar`. A month grid following the WAI-ARIA date picker pattern: arrow keys, Page Up/Down, Home and End, in single, multiple or range mode.

- `<KlyCalendar>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Culture`, `Disabled`, `DisabledDates`, `Id`, `Max`, `Min`, `Mode`, `Name`, `Range` (@bind), `RangeExpression`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Values` (@bind), `ValuesExpression`, `WeekStart`
  - `Ui` (`CalendarUi`): `Root`, `Header`, `Heading`, `Nav`, `Grid`, `Weekday`, `Cell`, `Day`
- `<KlyDateRange>`
  - Parameters: `End`, `Start`

### InputDate

`/components/input-date`. A date field typed in segments with a calendar popover. The value is always posted as yyyy-MM-dd.

- `<KlyInputDate>`
  - Parameters: `Class`, `Color`, `Culture`, `Disabled`, `Id`, `Max`, `Min`, `Name`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`, `WeekStart`
  - `Ui` (`InputDateUi`): `Root`, `Base`, `Trigger`, `TriggerButton`, `TriggerIcon`, `Content`

### InputTime

`/components/input-time`. A time field made of spinbutton segments, with a 12 or 24 hour clock taken from the culture. Posts HH:mm.

- `<KlyInputTime>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Hour12`, `Id`, `Name`, `Required`, `Size`, `Step`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`InputTimeUi`): `Root`, `Segment`, `Separator`

### ColorPicker

`/components/color-picker`. A saturation and brightness area, a hue slider and a hex field. The value is a #rrggbb string.

- `<KlyColorPicker>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Name`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`ColorPickerUi`): `Root`, `Area`, `AreaThumb`, `Hue`, `Preview`, `Swatch`, `Hex`

### FileUpload

`/components/file-upload`. A drop zone around a native file input, with a file list, remove buttons and size, count and type limits.

- `<KlyFileUpload>`
  - Parameters: `Accept`, `Class`, `Color`, `Disabled`, `Hint`, `Id`, `Label`, `MaxFiles`, `MaxSize`, `Multiple`, `Name`, `Required`, `Size`, `Ui`
  - Events: `OnChange`
  - `Ui` (`FileUploadUi`): `Root`, `Dropzone`, `Icon`, `Label`, `Description`, `Files`, `File`, `FileName`, `FileSize`, `Remove`, `Error`

### SelectMenu

`/components/select-menu`. A select with a filter field inside its list, for one or several values. Each value posts through a hidden input.

- `<KlySelectMenu>`
  - Parameters: `Class`, `Color`, `Create`, `Debounce`, `Disabled`, `Id`, `Items`, `MinLength`, `Multiple`, `Name`, `OptionsKey`, `OptionsProvider`, `PageSize`, `Placeholder`, `Required`, `ResolveSelected`, `SearchLabel`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`SelectMenuUi`): `Root`, `Base`, `Label`, `TrailingIcon`, `Content`, `SearchWrapper`, `Search`, `Listbox`, `Item`, `ItemLabel`, `ItemIcon`, `ItemLeadingIcon`, `ItemCount`, `Group`, `GroupLabel`, `LeadingIcon`, `Empty`, `Create`, `Scroller`, `Loading`, `Spinner`, `Error`, `Announcer`

### InputMenu

`/components/input-menu`. A text field with a list of suggestions: free text, or restricted to the suggestions.

- `<KlyInputMenu>`
  - Parameters: `Class`, `Color`, `Debounce`, `Disabled`, `Id`, `Items`, `MinLength`, `Name`, `OptionsKey`, `OptionsProvider`, `PageSize`, `Placeholder`, `Required`, `ResolveSelected`, `Size`, `Strict`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`InputMenuUi`): `Root`, `Base`, `Trailing`, `TrailingIcon`, `Content`, `Listbox`, `Item`, `ItemLabel`, `ItemIcon`, `ItemLeadingIcon`, `ItemCount`, `Group`, `GroupLabel`, `Empty`, `Scroller`, `Loading`, `Spinner`, `Error`, `Announcer`

### InputNumber

`/components/input-number`. A numeric field with increment and decrement buttons, formatted for a culture.

- `<KlyInputNumber>`
  - Parameters: `Class`, `Color`, `Culture`, `Disabled`, `Id`, `Max`, `Min`, `Name`, `Orientation`, `Placeholder`, `Required`, `Size`, `Step`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`InputNumberUi`): `Root`, `Base`, `Decrement`, `Increment`, `Icon`

### InputTags

`/components/input-tags`. A field that turns typed values into removable tags; each tag posts as its own entry.

- `<KlyInputTags>`
  - Parameters: `Class`, `Color`, `Disabled`, `Id`, `Max`, `Name`, `Placeholder`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`InputTagsUi`): `Root`, `Tags`, `Tag`, `Remove`, `Input`

### PinInput

`/components/pin-input`. One box per character, with auto-advance and paste support.

- `<KlyPinInput>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Length`, `Mask`, `Name`, `Required`, `Size`, `Type`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`PinInputUi`): `Root`, `Cell`

### Slider

`/components/slider`. A native range input for one value, or two thumbs for a range.

- `<KlySlider>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Immediate`, `Max`, `Min`, `Name`, `Orientation`, `Required`, `Size`, `Step`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`SliderUi`): `Root`, `Track`, `Range`, `Thumb`
- `<KlyRangeSlider>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Immediate`, `Max`, `Min`, `Name`, `Orientation`, `Required`, `Size`, `Step`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`SliderUi`): `Root`, `Track`, `Range`, `Thumb`

### CheckboxGroup

`/components/checkbox-group`. A fieldset of native checkboxes bound to a list of values.

- `<KlyCheckboxGroup>`
  - Parameters: `Class`, `Color`, `Disabled`, `Id`, `Items` (required), `Legend`, `Name`, `Orientation`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`CheckboxGroupUi`): `Root`, `Legend`, `Fieldset`, `Item`, `Container`, `Base`, `Icon`, `Wrapper`, `Label`, `Description`

### Toggle

`/components/toggle`. A button that stays pressed. Works in the browser without being bound.

- `<KlyToggle>`
  - Parameters: `Class`, `Color`, `Disabled`, `Id`, `Label`, `Name`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - Slots: `Icon`, `ChildContent`
  - `Ui` (`ToggleUi`): `Base`, `Label`, `Icon`

### ToggleGroup

`/components/toggle-group`. Toggle buttons where one or several can be pressed, with arrow key navigation.

- `<KlyToggleGroup>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Items` (required), `Multiple`, `Name`, `Orientation`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - `Ui` (`ToggleGroupUi`): `Root`, `Item`, `Icon`

### Listbox

`/components/listbox`. An always visible list of options for one or several values, with typeahead.

- `<KlyListbox>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Items` (required), `Multiple`, `Name`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`ListboxUi`): `Root`, `Base`, `Item`, `ItemLabel`, `ItemIcon`, `ItemLeadingIcon`, `ItemCount`, `Group`, `GroupLabel`

### InputRating

`/components/input-rating`. A star rating built on radio buttons, with an optional half step and a read-only display.

- `<KlyInputRating>`
  - Parameters: `AllowHalf`, `AriaLabel`, `Class`, `Color`, `Disabled`, `Id`, `Max`, `Name`, `ReadOnly`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`
  - `Ui` (`InputRatingUi`): `Root`, `Input`, `Star`, `Icon`, `Fill`, `FilledIcon`

### Form

`/components/form`. A thin wrapper over EditForm: validation summary and a busy state while submitting.

- `<KlyForm>`
  - Parameters: `Class`, `Disabled`, `EditContext`, `Enhance`, `FormName`, `Model`, `ShowSummary`, `Ui`, `Validate`
  - Events: `OnInvalidSubmit`, `OnValidSubmit`
  - Slots: `ChildContent`
  - `Ui` (`FormUi`): `Root`, `Fieldset`, `Summary`, `SummaryList`

## Overlay

### Modal

`/components/modal`. A native modal dialog: the page becomes inert and focus stays inside.

- `<KlyModal>`
  - Parameters: `AriaLabel`, `Class`, `Close`, `Description`, `Dismissible`, `Open` (@bind), `Size`, `Title`, `Ui`
  - Slots: `Body`, `Footer`, `ChildContent`
  - `Ui` (`ModalUi`): `Content`, `Header`, `Wrapper`, `Title`, `Description`, `Close`, `Body`, `Footer`

### Popover

`/components/popover`. A non-modal panel anchored to a button.

- `<KlyPopover>`
  - Parameters: `AriaLabel`, `Class`, `Offset`, `Open` (@bind), `Placement`, `Role`, `Ui`
  - Slots: `Content`, `ChildContent`
  - `Ui` (`PopoverUi`): `Content`

### Tooltip

`/components/tooltip`. A short hint shown on hover and keyboard focus.

- `<KlyTooltip>`
  - Parameters: `Class`, `Delay`, `Placement`, `Text`, `Ui`
  - Slots: `Content`, `ChildContent`
  - `Ui` (`TooltipUi`): `Content`

### DropdownMenu

`/components/dropdown-menu`. A menu of actions and links, navigable with the keyboard.

- `<KlyDropdownMenu>`
  - Parameters: `Class`, `Items` (required), `Open` (@bind), `Placement`, `Ui`
  - Events: `OnSelect`
  - Slots: `ChildContent`
  - `Ui` (`DropdownMenuUi`): `Content`, `Group`, `Label`, `Separator`, `Item`, `ItemIcon`, `ItemLabel`, `ItemShortcut`, `ItemChevron`

### Toast

`/components/toast`. Brief notifications, raised from .NET through IToastService.

- `<KlyToaster>`
  - Parameters: `Class`, `Duration`, `Ui`
  - `Ui` (`ToastUi`): `Viewport`, `Root`, `Wrapper`, `Title`, `Description`, `Close`

### Drawer

`/components/drawer`. A sheet docked to the bottom, top, left or right edge, on the native modal dialog.

- `<KlyDrawer>`
  - Parameters: `AriaLabel`, `Class`, `Close`, `Description`, `Direction`, `Dismissible`, `Handle`, `Inset`, `Open` (@bind), `Title`, `Ui`
  - Slots: `Body`, `Footer`, `ChildContent`
  - `Ui` (`DrawerUi`): `Content`, `Handle`, `Header`, `Wrapper`, `Title`, `Description`, `Close`, `Body`, `Footer`

### Slideover

`/components/slideover`. A full-height panel that slides in from any edge, for details and forms.

- `<KlySlideover>`
  - Parameters: `AriaLabel`, `Class`, `Close`, `Description`, `Dismissible`, `Inset`, `Open` (@bind), `Side`, `Title`, `Ui`
  - Slots: `Body`, `Footer`, `ChildContent`
  - `Ui` (`SlideoverUi`): `Content`, `Header`, `Wrapper`, `Title`, `Description`, `Close`, `Body`, `Footer`

### ContextMenu

`/components/context-menu`. A menu opened at the pointer by right click, long press, Shift+F10 or the ContextMenu key.

- `<KlyContextMenu>`
  - Parameters: `Class`, `Disabled`, `Focusable`, `Items` (required), `Label`, `Open` (@bind), `Ui`
  - Events: `OnSelect`
  - Slots: `ChildContent`
  - `Ui` (`ContextMenuUi`): `Root`, `Trigger`, `Content`, `Group`, `Label`, `Separator`, `Item`, `ItemIcon`, `ItemLabel`, `ItemShortcut`

### CommandPalette

`/components/command-palette`. A searchable list of commands, filtered in the browser as you type.

- `<KlyCommandPalette>`
  - Parameters: `Class`, `CloseOnSelect`, `Groups` (required), `Label`, `Placeholder`, `Ui`
  - Events: `OnSelect`
  - `Ui` (`CommandPaletteUi`): `Root`, `InputWrapper`, `InputIcon`, `Input`, `Listbox`, `Group`, `GroupLabel`, `Item`, `ItemIcon`, `ItemWrapper`, `ItemLabel`, `ItemDescription`, `ItemShortcut`, `ItemCount`, `Empty`, `Status`

### ColorModeButton

`/components/color-mode-button`. A button that switches between light and dark.

- `<KlyColorModeButton>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Label`, `Size`, `Variant`

### ColorModeSwitch

`/components/color-mode-switch`. A switch that is on in dark mode.

- `<KlyColorModeSwitch>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Label`, `Size`, `Ui`
  - `Ui` (`ColorModeSwitchUi`): `Root`, `Container`, `Input`, `Thumb`, `Moon`, `Sun`, `Wrapper`, `Label`

### ColorModeSelect

`/components/color-mode-select`. A select of System, Light and Dark.

- `<KlyColorModeSelect>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Size`, `Ui`, `Variant`
  - `Ui` (`ColorModeSelectUi`): `Root`, `Base`, `TrailingIcon`

### ColorModeAvatar

`/components/color-mode-avatar`. An avatar with one image per color mode.

- `<KlyColorModeAvatar>`
  - Parameters: `Alt`, `Class`, `Dark`, `ImageOptions`, `Light`, `Size`, `Text`, `Ui`
  - Slots: `Icon`
  - `Ui` (`AvatarUi`): `Root`, `Image`, `Fallback`

### ColorModeImage

`/components/color-mode-image`. An image with one source per color mode.

- `<KlyColorModeImage>`
  - Parameters: `Alt`, `Class`, `Dark`, `Height`, `ImageOptions`, `Light` (required), `Width`

## Navigation

### Tabs

`/components/tabs`. WAI-ARIA tabs with automatic activation.

- `<KlyTabs>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Items` (required), `Orientation`, `Ui`, `Value` (@bind), `Variant`
  - `Ui` (`TabsUi`): `Root`, `List`, `Trigger`, `Icon`, `Label`, `Content`

### Accordion

`/components/accordion`. Stacked headings that reveal their section.

- `<KlyAccordion>`
  - Parameters: `Class`, `Collapsible`, `HeadingLevel`, `Items` (required), `Multiple`, `Ui`, `Value` (@bind)
  - `Ui` (`AccordionUi`): `Root`, `Item`, `Header`, `Trigger`, `LeadingIcon`, `Label`, `TrailingIcon`, `Content`, `Body`

### Breadcrumb

`/components/breadcrumb`. The path to the current page, as a nav landmark with an ordered list.

- `<KlyBreadcrumb>`
  - Parameters: `AriaLabel`, `Class`, `Items` (required), `Ui`
  - Slots: `Separator`
  - `Ui` (`BreadcrumbUi`): `Root`, `List`, `Item`, `Link`, `Icon`, `Label`, `Separator`, `SeparatorIcon`

### Pagination

`/components/pagination`. Page links or buttons with ellipses, siblings and first/last controls.

- `<KlyPagination>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `HrefTemplate`, `Page` (@bind), `PageSize`, `ShowControls`, `ShowFirstLast`, `SiblingCount`, `Size`, `Total`, `Ui`
  - `Ui` (`PaginationUi`): `Root`, `List`, `Item`, `Page`, `Control`, `Ellipsis`, `Icon`

### Stepper

`/components/stepper`. Steps of a process with a current and completed state.

- `<KlyStepper>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Current` (@bind), `Items` (required), `Orientation`, `Size`, `Ui`
  - `Ui` (`StepperUi`): `Root`, `List`, `Item`, `Track`, `Indicator`, `Separator`, `Icon`, `Text`, `Title`, `Description`, `Status`, `Content`

### Timeline

`/components/timeline`. A chronological list of events.

- `<KlyTimeline>`
  - Parameters: `Active`, `AriaLabel`, `Class`, `Color`, `Items` (required), `Layout`, `Max`, `Now`, `Orientation`, `ShowNumbers`, `Size`, `Step`, `Ui`
  - `Ui` (`TimelineUi`): `Root`, `Item`, `Track`, `Indicator`, `Icon`, `Separator`, `Text`, `Date`, `Title`, `Description`, `Actor`, `Number`, `PipelineList`, `PipelineItem`, `LogList`, `LogItem`, `LogTimestamp`, `LogMarker`, `LogDot`, `LogIcon`, `LogBody`, `LogTitle`, `LogDescription`, `LogActor`, `Schedule`, `ScheduleDay`, `ScheduleDayTitle`, `ScheduleUnscheduled`, `ScheduleList`, `ScheduleCurrent`, `ScheduleCurrentTone`, `ScheduleDone`, `ScheduleDoneTone`, `ScheduleUpcoming`, `ScheduleUpcomingTone`, `ScheduleTimes`, `ScheduleTime`, `ScheduleDuration`, `ScheduleBody`, `ScheduleHeader`, `ScheduleIcon`, `ScheduleTitle`, `ScheduleDescription`, `ScheduleActor`, `More`, `MoreIcon`, `ScheduleEvent`

### NavigationMenu

`/components/navigation-menu`. Links with an active state taken from the address; sub-items open in a panel or a collapsible group.

- `<KlyNavigationMenu>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Items` (required), `Orientation`, `Ui`
  - `Ui` (`NavigationMenuUi`): `Root`, `List`, `Item`, `Link`, `Trigger`, `Icon`, `Label`, `Chevron`, `Panel`, `PanelList`, `Group`, `GroupList`

### Collapsible

`/components/collapsible`. A button that shows or hides a region, handled in the browser.

- `<KlyCollapsible>`
  - Parameters: `Class`, `Disabled`, `Label`, `Open` (@bind), `ShowIcon`, `Ui`
  - Slots: `Trigger`, `ChildContent`
  - `Ui` (`CollapsibleUi`): `Root`, `Trigger`, `Label`, `Icon`, `Content`

### Tree

`/components/tree`. A WAI-ARIA tree view with arrow-key navigation and optional selection.

- `<KlyTree>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Items` (required), `Ui`, `Value` (@bind)
  - `Ui` (`TreeUi`): `Root`, `List`, `Item`, `Row`, `Chevron`, `Spacer`, `Icon`, `Label`, `Group`

### Marquee

`/components/marquee`. Content that scrolls endlessly; pure CSS, paused on hover and by reduced-motion preferences.

- `<KlyMarquee>`
  - Parameters: `Class`, `Duration`, `Orientation`, `Overlay`, `PauseOnHover`, `Repeat`, `Reverse`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`MarqueeUi`): `Root`, `Group`

### ScrollArea

`/components/scroll-area`. A styled scroll container that stays reachable with the keyboard.

- `<KlyScrollArea>`
  - Parameters: `AriaLabel`, `Class`, `Orientation`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`ScrollAreaUi`): `Root`, `Viewport`

### Splitter

`/components/splitter`. Resizable panels, with the mouse or the arrow keys.

- `<KlySplitter>`
  - Parameters: `Class`, `Color`, `Orientation`, `Panels` (required), `Sizes` (@bind), `Step`, `Ui`
  - `Ui` (`SplitterUi`): `Root`, `Panel`, `Handle`

### ContentNavigation

`/components/content-navigation`. The navigation of a documentation site: a tree of pages with collapsible groups, icons and badges.

- `<KlyContentNavigation>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `Items` (required), `Ui`
  - `Ui` (`ContentNavigationUi`): `Root`, `List`, `Item`, `Link`, `Trigger`, `Section`, `SectionList`, `Icon`, `Label`, `Badge`, `Chevron`, `Group`, `GroupList`

### ContentSearch

`/components/content-search`. A documentation search: a command palette in a modal over a list of pages and their headings, opened by a shortcut.

- `<KlyContentSearch>`
  - Parameters: `Class`, `IndexUrl`, `Label`, `Name`, `Open` (@bind), `Pages`, `Placeholder`, `Shortcut`, `Ui`
  - Events: `OnSelect`
  - `Ui` (`CommandPaletteUi`): `Root`, `InputWrapper`, `InputIcon`, `Input`, `Listbox`, `Group`, `GroupLabel`, `Item`, `ItemIcon`, `ItemWrapper`, `ItemLabel`, `ItemDescription`, `ItemShortcut`, `ItemCount`, `Empty`, `Status`

### ContentSearchButton

`/components/content-search-button`. A button that opens the search and shows its shortcut.

- `<KlyContentSearchButton>`
  - Parameters: `Class`, `Collapsed`, `Color`, `For`, `Keys`, `Label`, `Size`, `Ui`, `Variant`
  - `Ui` (`ContentSearchButtonUi`): `Root`, `Label`, `Kbds`

### ContentSurround

`/components/content-surround`. Links to the previous and the next page, with their titles and descriptions.

- `<KlyContentSurround>`
  - Parameters: `AriaLabel`, `Class`, `Next`, `Previous`, `Ui`
  - `Ui` (`ContentSurroundUi`): `Root`, `Link`, `Caption`, `Icon`, `Title`, `Description`

### ContentToc

`/components/content-toc`. A table of contents that follows the heading being read, and collapses behind its title on small screens.

- `<KlyContentToc>`
  - Parameters: `Breakpoint`, `Class`, `Collapsible`, `Color`, `Links` (required), `Title`, `Ui`
  - `Ui` (`ContentTocUi`): `Root`, `Trigger`, `Title`, `Chevron`, `Content`, `List`, `Item`, `Link`

## Data

### Table

`/components/table`. A data table with sortable columns.

- `<KlyTable>`
  - Parameters: `Caption`, `Class`, `Items`, `Max`, `Step`, `Sticky`, `Ui`
  - Slots: `Empty`, `ChildContent`
  - `Ui` (`TableUi`): `Root`, `Base`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Tfoot`, `Tf`, `CellEnd`, `CellNumeric`, `CellMono`, `More`, `MoreIcon`
- `<KlyColumn>`
  - Parameters: `Align`, `Class`, `Field` (required), `Footer`, `Mono`, `Numeric`, `Sortable`, `Title` (required)
  - Slots: `FooterTemplate`, `Template`
- `<KlyTemplateColumn>`
  - Parameters: `Align`, `Class`, `Footer`, `Mono`, `Numeric`, `Title` (required)
  - Slots: `FooterTemplate`, `Template`

### DataList

`/components/data-list`. A table of rows loaded in memory, from a provider or from a database query (a Queryable, run asynchronously by the Klyroo.Table.EntityFrameworkCore adapter), with search, sortable columns, pagination, a result count, loading, empty and error states, and its state in the URL. Filters, counts, the filter panel and selection have their own pages. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataList>`
  - Parameters: `Class`, `Density`, `Items`, `Label`, `PagingMode`, `PersistKey`, `PreferencesKey`, `Provider`, `Queryable`, `ResultJson`, `Schema`, `Selectable`, `SelectedKeys` (@bind), `SelectionScope`, `State` (@bind), `Ui`, `UrlKey`, `UrlSync`
  - Slots: `ChildContent`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListTable>`
  - Parameters: `AriaLabel`, `Caption`, `Class`, `EmptyState`, `GroupBy`, `HeaderMenu`, `ItemSize`, `OverscanCount`, `Resizable`, `RowActions`, `RowActionsMaxVisible`, `RowAttributes`, `RowClass`, `RowHref`, `RowSummary`, `ShowProgressLine`, `Summaries`, `Tree`, `Ui`, `Virtualize`
  - Events: `OnRowActionError`, `OnRowClick`
  - Slots: `DetailTemplate`, `Empty`, `ChildContent`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListColumn>`
  - Parameters: `Align`, `CellClass`, `Class`, `Format`, `HeaderMenu`, `Key`, `MaxWidth`, `MinWidth`, `Order`, `Pinned`, `Property` (required), `Required`, `Resizable`, `Skeleton`, `Sortable`, `Summary`, `SummaryFormat`, `SummaryValue`, `Title`, `Visible`, `Width`
  - Slots: `HeaderTemplate`, `SummaryTemplate`, `Template`
- `<KlyDataListFieldColumn>`
  - Parameters: `Align`, `CellClass`, `Class`, `Field` (required), `Format`, `HeaderMenu`, `Key`, `MaxWidth`, `MinWidth`, `Order`, `Pinned`, `Required`, `Resizable`, `Skeleton`, `Sortable`, `Summary`, `SummaryFormat`, `Title`, `Visible`, `Width`
  - Slots: `HeaderTemplate`, `SummaryTemplate`
- `<KlyDataListTemplateColumn>`
  - Parameters: `Align`, `CellClass`, `Class`, `Format`, `HeaderMenu`, `Key`, `MaxWidth`, `MinWidth`, `Order`, `Pinned`, `Required`, `Resizable`, `Skeleton`, `Sortable`, `Summary`, `SummaryFormat`, `Title`, `Visible`, `Width`
  - Slots: `HeaderTemplate`, `SummaryTemplate`, `Template`
- `<KlyDataListPagination>`
  - Parameters: `AriaLabel`, `Class`, `PageSizes`, `ShowFirstLast`, `ShowPageSize`, `SiblingCount`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListResultCount>`
  - Parameters: `Class`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListRefresh>`
  - Parameters: `Class`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListErrorState>`
  - Parameters: `Class`, `Description`, `Title`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListSearch>`
  - Parameters: `AriaLabel`, `Class`, `Debounce`, `MinLength`, `Placeholder`, `Size`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListFilters

`/components/data-list-filters`. Filters of a data list: filter tags with an editor in a popover, the Add filter menu, Clear filters, text, option, yes / no, number and date filters, in Live or Submit mode. Statically rendered the tags are links and the editors GET forms. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListFilterTags>`
  - Parameters: `ApplyMode`, `Class`, `ShowAdd`, `ShowClear`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListAddFilter>`
  - Parameters: `ApplyMode`, `Class`, `SearchFrom`, `Size`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListClearFilters>`
  - Parameters: `Class`, `Label`, `Size`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListFilterPanel

`/components/data-list-filter-panel`. Every filter of a data list as a collapsible section, as a sidebar or in a slideover on a phone, with the same editors, limits and history as the filter tags. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListFilterPanel>`
  - Parameters: `ApplyMode`, `Class`, `CloseOnApply`, `Description`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListFacets

`/components/data-list-facets`. Counts next to the options of a filter (facets) and option filters that load their options from a remote source. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListFilterEditor>`
  - Parameters: `ApplyMode`, `Class`, `Key` (required), `ResetToken`, `ShowTitle`, `Ui`
  - Events: `OnDone`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListSelection

`/components/data-list-selection`. Row selection, a bar of bulk actions with an every-result scope and a toolbar menu of actions, for a list in memory and for a provider. Interactive only. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListSelectionActions>`
  - Parameters: `Actions`, `Class`, `MaxVisible`, `Position`, `Scope`, `Ui`
  - Events: `OnError`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListActionsDropdown>`
  - Parameters: `Actions`, `Class`, `Icon`, `Label`, `Ui`
  - Events: `OnError`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListRowActions>`
  - Parameters: `Actions`, `Class`, `Item` (required), `MaxVisible`, `Position`, `Size`, `Ui`
  - Events: `OnError`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListCursor

`/components/data-list-cursor`. Keyset cursor paging with an infinite loader (automatic scroll sentinel or manual Load more, inline retry on error, link fallback in static SSR) and opt-in row virtualization. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListInfiniteLoader>`
  - Parameters: `Auto`, `Class`, `Label`, `Root`, `RootMargin`, `ShowEnd`, `Ui`
  - Slots: `ErrorTemplate`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListColumns

`/components/data-list-columns`. The column panel of a data list: show or hide columns, show or hide them all, reorder by dragging or with buttons, pin to either edge, resize by pointer or keyboard, and remember it all in a cookie that the server reads at the first paint. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListColumnPanel>`
  - Parameters: `AllowPinning`, `AllowReorder`, `Class`, `Label`, `SearchThreshold`, `Size`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListGrid

`/components/data-list-grid`. Grid layout of cards (KlyDataListGrid), layout switch between Table and Grid views (KlyDataListLayoutSwitch, synced with l=table|grid in the URL), compact phone summary rows (DataListRowSummary) with KlyDataListViewToggle, visual row grouping (DataListGroupBy and KlyDataListGroupHeader), hierarchical tree rows (DataListTree), footer totals and column aggregations (DataListSummaries), and a toolbar sort menu (KlyDataListSortMenu). Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListGrid>`
  - Parameters: `AriaLabel`, `Class`, `Columns`, `EmptyState`, `GroupBy`, `RowActions`, `RowActionsMaxVisible`, `ShowProgressLine`, `ShowSelectionControl`, `Ui`
  - Events: `OnRowActionError`
  - Slots: `Empty`, `ItemTemplate`, `SkeletonTemplate`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListLayoutSwitch>`
  - Parameters: `AriaLabel`, `Class`, `GridIcon`, `Order`, `ShowLabels`, `TableIcon`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListSortMenu>`
  - Parameters: `Class`, `Items`, `Label`, `Layouts`, `Multiple`, `Size`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListViewToggle>`
  - Parameters: `Class`, `Icon`, `Label`, `ShowLabel`, `Ui`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`
- `<KlyDataListGroupHeader>`
  - Parameters: `Class`, `Collapsed`, `Group` (required), `GroupBy`, `Selectable`, `ToggleLabel`, `Ui`
  - Events: `OnToggle`
  - `Ui` (`DataListUi`): `Root`, `Toolbar`, `Search`, `SearchField`, `SearchInput`, `SearchClear`, `SearchClearIcon`, `SearchSubmit`, `ResultCount`, `Refresh`, `RefreshIcon`, `TableRoot`, `Table`, `Caption`, `Thead`, `Tr`, `Th`, `SortButton`, `SortIcon`, `Tbody`, `Td`, `Empty`, `Skeleton`, `Pagination`, `PageSize`, `PageSizeLabel`, `PageSizeSelect`, `PageSizeForm`, `Error`, `Filters`, `FilterTags`, `FilterTag`, `FilterTagButton`, `FilterTagLabel`, `FilterTagOperator`, `FilterTagValue`, `FilterTagRemove`, `FilterTagRemoveIcon`, `AddFilter`, `ClearFilters`, `FilterPopover`, `FilterEditor`, `FilterEditorTitle`, `FilterField`, `FilterFieldLabel`, `FilterOperator`, `FilterInput`, `FilterOptions`, `FilterOption`, `FilterOptionLink`, `FilterOptionCount`, `FilterOptionCheck`, `FilterEmpty`, `FilterActions`, `FilterChoices`, `FilterMenu`, `FilterMenuItem`, `FilterMenuHeader`, `FilterBack`, `FilterSection`, `FilterRange`, `FilterRangeSlider`, `FilterPresets`, `FilterHint`, `FilterPopoverWide`, `FilterPanel`, `FilterPanelHeader`, `FilterPanelTitle`, `FilterPanelCount`, `FilterPanelDescription`, `FilterPanelSection`, `FilterPanelTrigger`, `FilterPanelContent`, `FilterPanelEditor`, `FilterPanelFooter`, `FilterPanelEmpty`, `FilterNativeInput`, `FilterNativeLabel`, `FilterPanelResults`, `SelectCell`, `RowSelected`, `SelectionActions`, `SelectionBar`, `SelectionCount`, `SelectionLive`, `SelectionScope`, `SelectionGroup`, `ActionsDropdown`, `RowActionsCell`, `RowActions`, `RowActionsInline`, `RowActionsMenu`, `RowActionsPhone`, `SheetList`, `SheetDivider`, `SheetEntry`, `SheetHeading`, `SheetItem`, `SheetReason`, `SheetIcon`, `InfiniteLoader`, `LoadMoreError`, `LoadMoreErrorText`, `CursorReset`, `InfiniteEnd`, `InfiniteStatus`, `ThPinnedStart`, `ThPinnedEnd`, `TdPinnedStart`, `TdPinnedEnd`, `ColResize`, `ColumnsPanel`, `ColumnsPopover`, `ColumnsContent`, `ColumnsHeader`, `ColumnsTitle`, `ColumnsCount`, `ColumnsList`, `ColumnsItem`, `ColumnsLabel`, `ColumnsActions`, `ColumnsIconBtn`, `ColumnsIconBtnActive`, `ColSized`, `ColumnsAll`, `ColumnsHint`, `ColumnsGrip`, `ColumnsLive`, `ColumnsPinGroup`, `ColumnsSearch`, `SortMenu`, `SortMenuPopover`, `SortMenuContent`, `SortMenuHeader`, `SortMenuTitle`, `SortMenuDirections`, `SortMenuDirection`, `SortMenuList`, `SortMenuItem`, `LayoutSwitch`, `LayoutOption`, `GridRoot`, `GridHeader`, `Grid`, `GridCard`, `GridCardTop`, `GridCardBody`, `GridCardFields`, `GridCardField`, `GridCardFieldLabel`, `GridCardFieldValue`, `GridEmpty`, `ViewToggle`, `SummaryHeader`, `SummaryList`, `SummaryRow`, `SummaryLeading`, `SummaryBody`, `SummaryTitle`, `SummaryTitleLink`, `SummaryTitleButton`, `SummaryMeta`, `SummaryStatus`, `SummaryDetails`, `SummaryActions`, `GroupRow`, `GroupCell`, `SummaryGroupRow`, `GridGroup`, `GridGroupHeader`, `GroupHeader`, `GroupToggle`, `GroupChevron`, `GroupChevronExpanded`, `GroupLabel`, `GroupSummary`, `TreeCell`, `TreeRailBlank`, `TreeRailPass`, `TreeRailBranch`, `TreeRailLast`, `TreeToggle`, `TreeChevron`, `TreeChevronExpanded`, `TreeSpacer`, `TreeContent`, `Tfoot`, `TfootRow`, `TfootCell`, `TfootCellPinnedStart`, `TfootCellPinnedEnd`, `TfootLabel`, `ProgressLine`, `ProgressLineBar`, `HeaderCell`, `HeaderMenu`, `HeaderMenuTrigger`

### DataListEmpty

`/components/data-list-empty`. The empty state of a data list: a localized title and hint, an icon and room for actions. Package Klyroo.Table, not part of Klyroo.UI: `dotnet add package Klyroo.Table --prerelease`.

- `<KlyDataListEmpty>`
  - Parameters: `Class`, `Description`, `Icon`, `Title`, `Ui`
  - Slots: `Actions`, `ChildContent`
  - `Ui` (`DataListEmptyUi`): `Root`, `Icon`, `Title`, `Description`, `Actions`

### Carousel

`/components/carousel`. Slides in a scroll-snap viewport with previous and next buttons, a slide picker and optional autoplay that can be paused.

- `<KlyCarousel>`
  - Parameters: `AriaLabel` (required), `Arrows`, `AutoPlay`, `Class`, `Color`, `Dots`, `Index` (@bind), `Items` (required), `Loop`, `Ui`
  - `Ui` (`CarouselUi`): `Root`, `Viewport`, `Slide`, `Controls`, `Control`, `Dots`, `Dot`
- `<KlyCarouselItem>`
  - Parameters: `Label`
  - Slots: `Content`

### Chart

`/components/chart`. A line, area, bar or donut chart drawn as SVG by the server, with a tooltip, a legend and a data table.

- `<KlyChart>`
  - Parameters: `AriaDescription`, `AriaLabel`, `Categories` (required), `Class`, `Colors`, `Culture`, `Curve`, `Data` (required), `Format`, `Grid`, `Height`, `Horizontal`, `Index` (required), `IndexFormat`, `Legend`, `Points`, `Stacked`, `Table`, `Thickness`, `Tooltip`, `Type`, `Ui`, `Width`, `XAxis`, `XLabel`, `YAxis`, `YLabel`
  - Events: `OnSelect`
  - Slots: `Center`
  - `Ui` (`ChartUi`): `Root`, `Plot`, `Svg`, `Grid`, `AxisLine`, `Tick`, `AxisTitle`, `Line`, `Bar`, `Marker`, `Hit`, `HitArea`, `Slice`, `Tooltip`, `TooltipTitle`, `TooltipRow`, `TooltipIndicator`, `TooltipLabel`, `TooltipValue`, `Center`, `Legend`, `LegendItem`, `LegendSwatch`, `LegendLabel`, `LegendValue`, `Details`, `Summary`, `TableWrapper`, `Table`, `Th`, `Td`, `Funnel`, `FunnelStep`, `FunnelHeader`, `FunnelLabel`, `FunnelValue`, `FunnelBar`, `FunnelEmpty`

## Blocks

### Heading

`/components/heading`. A section heading with a count.

- `<KlyHeading>`
  - Parameters: `Action`, `Class`, `Count`, `Description`, `HeadingLevel`, `Loading`, `Surface`, `Title`, `Ui`
  - Slots: `Icon`
  - `Ui` (`HeadingUi`): `Root`, `Lead`, `Icon`, `Text`, `TitleRow`, `Title`, `Count`, `Description`, `Skeleton`

### Text

`/components/text`. Paragraphs, a quote, clamped text.

- `<KlyText>`
  - Parameters: `Author`, `Class`, `Loading`, `MaxLines`, `Meta`, `Paragraphs`, `Surface`, `Title`, `Ui`, `Value`, `Variant`
  - `Ui` (`TextUi`): `Root`, `Title`, `Paragraph`, `Body`, `Toggle`, `Footer`, `Skeleton`

### Stat

`/components/stat`. A number with a delta and a sparkline.

- `<KlyStat>`
  - Parameters: `Caption`, `Class`, `Culture`, `Delta`, `DeltaDirection`, `DeltaFormat`, `DeltaUnit`, `Format`, `Loading`, `Surface`, `Title`, `Trend`, `Ui`, `Unit`, `Value`
  - `Ui` (`StatUi`): `Root`, `Label`, `Row`, `ValueGroup`, `Value`, `Unit`, `Delta`, `Sparkline`, `Caption`, `Skeleton`

### StatGroup

`/components/stat-group`. Small metrics side by side.

- `<KlyStatGroup>`
  - Parameters: `Class`, `Culture`, `Format`, `Items`, `Loading`, `Surface`, `Title`, `Ui`
  - `Ui` (`StatGroupUi`): `Root`, `Title`, `Grid`, `Item`, `Label`, `Value`, `Caption`, `Skeleton`

### Facts

`/components/facts`. Label and value rows.

- `<KlyFacts>`
  - Parameters: `Class`, `Groups`, `Items`, `Loading`, `Max`, `Step`, `Surface`, `Ui`
  - `Ui` (`FactsUi`): `Root`, `Group`, `GroupHeader`, `GroupTitle`, `GroupAside`, `List`, `Item`, `Label`, `LabelRow`, `LabelIcon`, `Hint`, `Value`, `ValueMono`, `ValueEmphasis`, `ValueSkeleton`, `Empty`, `Link`, `Copy`, `CopyIcon`, `More`, `MoreIcon`, `None`, `Skeleton`

### Breakdown

`/components/breakdown`. Amounts with subtotals and a total.

- `<KlyBreakdown>`
  - Parameters: `Class`, `Collapsible`, `CompareColumns`, `Culture`, `DefaultOpen`, `Footnote`, `Format`, `Lines`, `Loading`, `Surface`, `Title`, `Total`, `Ui`
  - `Ui` (`BreakdownUi`): `Root`, `Title`, `Toggle`, `ToggleIcon`, `Columns`, `ColumnHeader`, `Lines`, `Row`, `Subtotal`, `Muted`, `Total`, `Label`, `LabelRow`, `Detail`, `Amount`, `CompareAmount`, `Footnote`, `Skeleton`

### Diff

`/components/diff`. Before and after values.

- `<KlyDiff>`
  - Parameters: `Class`, `Culture`, `Items`, `Loading`, `ShowUnchanged`, `Surface`, `Title`, `Ui`
  - `Ui` (`DiffUi`): `Root`, `Title`, `List`, `Item`, `Label`, `Values`, `Before`, `Arrow`, `After`, `Unchanged`, `Skeleton`

### RecordList

`/components/record-list`. Entity rows with a trailing value.

- `<KlyRecordList>`
  - Parameters: `Class`, `Count`, `ImageOptions`, `Items`, `Loading`, `Max`, `Step`, `Surface`, `Title`, `Ui`
  - `Ui` (`RecordListUi`): `Root`, `Header`, `Title`, `Count`, `Item`, `Lead`, `Image`, `Avatar`, `AvatarIcon`, `Body`, `Reference`, `Name`, `Link`, `Meta`, `MetaIcon`, `MetaText`, `Trail`, `Trailing`, `TrailingBlock`, `TrailingCaption`, `More`, `MoreIcon`, `Skeleton`

### List

`/components/list`. Plain, stepped or checked items.

- `<KlyList>`
  - Parameters: `Class`, `Items`, `Loading`, `Max`, `Step`, `Surface`, `Title`, `Ui`, `Variant`
  - `Ui` (`ListUi`): `Root`, `Title`, `List`, `Item`, `Lead`, `Marker`, `MarkerIcon`, `Icon`, `Bullet`, `Body`, `Text`, `Link`, `Description`, `Meta`, `More`, `MoreIcon`, `Skeleton`

### DateTile

`/components/date-tile`. A calendar tile.

- `<KlyDateTile>`
  - Parameters: `Class`, `Description`, `EndValue`, `Loading`, `Mode`, `Now`, `Surface`, `Title`, `Ui`, `Value`
  - `Ui` (`DateTileUi`): `Root`, `Tile`, `Month`, `Day`, `Text`, `Title`, `Caption`, `Description`, `Skeleton`

### Meter

`/components/meter`. A gauge: bar, ring or scale.

- `<KlyMeter>`
  - Parameters: `Class`, `CurrentLevel`, `Description`, `ErrorThreshold`, `Layout`, `Levels`, `Loading`, `Max`, `Parts`, `Surface`, `Title`, `Ui`, `Value`, `ValueText`, `WarningThreshold`
  - `Ui` (`MeterUi`): `Root`, `Header`, `Label`, `Value`, `Track`, `Bar`, `Part`, `Ring`, `RingSvg`, `RingDrawing`, `RingTrack`, `RingArc`, `RingValue`, `RingText`, `Caption`, `Scale`, `Level`, `Legend`, `LegendItem`, `LegendIcon`, `LegendDot`, `LegendLabel`, `LegendValue`, `Skeleton`

### ImageGallery

`/components/image-gallery`. A grid of images.

- `<KlyImageGallery>`
  - Parameters: `Caption`, `Class`, `Columns`, `ImageOptions`, `Images`, `Loading`, `Max`, `Surface`, `Ui`
  - `Ui` (`ImageGalleryUi`): `Root`, `Grid`, `Tile`, `Image`, `Overflow`, `Caption`, `Skeleton`

## Layout

### App

`/components/app`. The root wrapper of an app: renders the toaster, resolves the locale and sets language and direction, and cascades global class overrides.

- `<KlyApp>`
  - Parameters: `ApplyStoredCulture`, `Class`, `Dir`, `Lang`, `Locale`, `Overlays`, `Overrides`, `SpatialNavigation`, `ToastDuration`, `Toaster`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`AppUi`): `Root`

### Header

`/components/header`. A site header with a title, links, actions and a mobile menu that opens in a slideover.

- `<KlyHeader>`
  - Parameters: `AriaLabel`, `Class`, `Href`, `Items`, `Menu`, `MenuSide`, `MenuTitle`, `Sticky`, `Title`, `Ui`
  - Slots: `Body`, `Left`, `Right`, `ChildContent`
  - `Ui` (`HeaderUi`): `Root`, `Container`, `Left`, `Title`, `Center`, `Right`, `Menu`

### Footer

`/components/footer`. A site footer with top and bottom areas and left, center and right slots.

- `<KlyFooter>`
  - Parameters: `Class`, `Ui`
  - Slots: `Bottom`, `Left`, `Right`, `Top`, `ChildContent`
  - `Ui` (`FooterUi`): `Root`, `Top`, `Container`, `Left`, `Center`, `Right`, `Bottom`

### FooterColumns

`/components/footer-columns`. Columns of links under headings, for the top of a footer.

- `<KlyFooterColumns>`
  - Parameters: `AriaLabel`, `Class`, `Columns` (required), `HeadingLevel`, `Ui`
  - Slots: `Left`, `Right`
  - `Ui` (`FooterColumnsUi`): `Root`, `Left`, `Nav`, `Column`, `Label`, `List`, `Item`, `Link`, `Icon`, `Right`
- `<KlyFooterColumn>`
  - Parameters: `Label`, `Links`

### Main

`/components/main`. The main landmark, tall enough to keep the footer at the bottom of the screen.

- `<KlyMain>`
  - Parameters: `As`, `Class`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`MainUi`): `Base`

### SkipLink

`/components/skip-link`. The first link of a page: hidden until it has the focus, it moves the focus to the main content.

- `<KlySkipLink>`
  - Parameters: `Class`, `Label`, `Target`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`SkipLinkUi`): `Root`

### Sidebar

`/components/sidebar`. An app sidebar that collapses to icons, can be resized, and becomes an off-canvas panel on small screens.

- `<KlySidebar>`
  - Parameters: `AriaLabel`, `As`, `Class`, `Collapsed` (@bind), `Collapsible`, `Id`, `MaxWidth`, `MinWidth`, `MobileTitle`, `MobileTrigger`, `Resizable`, `ScriptNonce`, `Ui`, `Width`
  - Slots: `Footer`, `Header`, `ChildContent`
  - `Ui` (`SidebarUi`): `Root`, `Host`, `Panel`, `Header`, `HeaderContent`, `Toggle`, `ToggleIcon`, `Content`, `Footer`, `Handle`, `Mobile`, `MobileHeader`, `MobileTitle`, `MobileClose`

### Error

`/components/error`. The content of an error page: status code, message and a way back.

- `<KlyError>`
  - Parameters: `Class`, `Clear`, `ClearLabel`, `Exception`, `HeadingLevel`, `Href`, `Message`, `ShowDetails`, `StatusCode`, `StatusMessage`, `Ui`
  - Slots: `Actions`, `ChildContent`
  - `Ui` (`ErrorUi`): `Root`, `StatusCode`, `StatusMessage`, `Message`, `Actions`

### LocaleSelect

`/components/locale-select`. A select of locales that writes the ASP.NET Core culture cookie and reloads the page.

- `<KlyLocaleSelect>`
  - Parameters: `AriaLabel`, `Class`, `Color`, `CookieName`, `CookiePath`, `Locales`, `Reload`, `Size`, `Ui`, `Value`, `Variant`
  - Events: `OnLocaleChange`
  - `Ui` (`LocaleSelectUi`): `Root`
- `<KlyLocale>`
  - Parameters: `Code`, `Culture`, `Dir`, `DirAttribute`, `Flag`, `Messages`, `Name`
- `<KlyMessages>`
  - Parameters: `AllDay`, `AuthFormIncompleteCode`, `AuthFormInvalidEmail`, `AuthFormRequired`, `AuthFormSeparator`, `AuthFormShowPassword`, `AuthFormSubmit`, `Breadcrumb`, `Brightness`, `Cancel`, `CarouselRoleDescription`, `CarouselSlideRoleDescription`, `ChartArea`, `ChartBar`, `ChartBarHorizontal`, `ChartDonut`, `ChartHint`, `ChartHintSelect`, `ChartIndex`, `ChartLegend`, `ChartLine`, `ChartRange`, `ChartSlice`, `ChartSummary`, `ChartSummaryDonut`, `ChartTable`, `ChartValue`, `ChatAssistant`, `ChatAttachmentFailed`, `ChatAttachmentUploading`, `ChatConversation`, `ChatError`, `ChatJumpToLatest`, `ChatPromptLabel`, `ChatPromptPlaceholder`, `ChatRemoveAttachment`, `ChatResponding`, `ChatRetry`, `ChatSend`, `ChatStop`, `ChatSystem`, `ChatThinking`, `ChatThought`, `ChatThoughtFor`, `ChatToolFailed`, `ChatToolRunning`, `ChatUser`, `ChooseDate`, `Close`, `CollapseSidebar`, `ColorMode`, `ColorModeDark`, `ColorModeLight`, `ColorModeSystem`, `CommandPaletteClose`, `CommandResults`, `Confirm`, `ContentNavigation`, `ContentNext`, `ContentPrevious`, `ContentSearch`, `ContentSearchButton`, `ContentSearchError`, `ContentSearchLoading`, `ContentSearchPlaceholder`, `ContentSearchRetry`, `ContentSurround`, `ContentToc`, `ContextMenu`, `CopyValue`, `DashboardSearch`, `DashboardSearchButton`, `DashboardSearchPlaceholder`, `DayCount`, `DayPeriod`, `DaysOverdue`, `Details`, `DropFiles`, `Duration`, `EditorAddressRefused`, `EditorBlockquote`, `EditorBold`, `EditorBulletList`, `EditorClearFormatting`, `EditorCode`, `EditorCodeBlock`, `EditorDescriptionBlockquote`, `EditorDescriptionBulletList`, `EditorDescriptionCodeBlock`, `EditorDescriptionHeading1`, `EditorDescriptionHeading2`, `EditorDescriptionHeading3`, `EditorDescriptionHorizontalRule`, `EditorDescriptionOrderedList`, `EditorDescriptionParagraph`, `EditorDragHandle`, `EditorEmojiMenu`, `EditorHeading`, `EditorHorizontalRule`, `EditorImage`, `EditorImageUrl`, `EditorItalic`, `EditorLabel`, `EditorLink`, `EditorLinkApply`, `EditorLinkRemove`, `EditorLinkUrl`, `EditorMentionMenu`, `EditorMoveDown`, `EditorMoveUp`, `EditorNoResults`, `EditorOrderedList`, `EditorParagraph`, `EditorRedo`, `EditorSchemesOnly`, `EditorStrike`, `EditorSuggestionMenu`, `EditorSuggestionText`, `EditorTaskItem`, `EditorTaskList`, `EditorToolbar`, `EditorUnderline`, `EditorUndo`, `ErrorBackHome`, `ErrorForbidden`, `ErrorForbiddenMessage`, `ErrorNotFound`, `ErrorNotFoundMessage`, `ErrorServer`, `ErrorServerMessage`, `ErrorUnauthorized`, `ErrorUnauthorizedMessage`, `FileNotAccepted`, `FileSizeBytes`, `FileSizeGigabytes`, `FileSizeKilobytes`, `FileSizeMegabytes`, `FileTooLarge`, `FirstPage`, `FooterLinks`, `FormSummaryTitle`, `GoToSlide`, `HeaderMenu`, `HeaderNavigation`, `HexColor`, `HideDetails`, `Hours`, `Hue`, `ImageAlt`, `InDays`, `InputMenuNoResults`, `InputMenuSuggestions`, `InputNumberDecrement`, `InputNumberIncrement`, `InputRatingValue`, `InputTagsRemove`, `LastPage`, `LoadMore`, `LoadingMore`, `LocaleSelectLabel`, `MaximumSize`, `Meter`, `Minutes`, `ModifierKey`, `MoreAvatars`, `NextMonth`, `NextPage`, `NextSlide`, `NoCommandResults`, `NoData`, `NoMoreItems`, `Notifications`, `OpenHeaderMenu`, `OpenSidebar`, `OpensInNewTab`, `Page`, `PageAnchors`, `PageLinks`, `Pagination`, `PauseSlides`, `PinInputCharacter`, `PlaySlides`, `PreviousMonth`, `PreviousPage`, `PreviousSlide`, `PricingDiscountedPrice`, `PricingFeatures`, `PricingIncluded`, `PricingNotIncluded`, `PricingOriginalPrice`, `Progress`, `ReadMore`, `RemoteOptionsError`, `RemoteOptionsLoading`, `RemoteOptionsMinLength`, `RemoteOptionsRetry`, `RemoveFile`, `ResizePanels`, `ResizeSidebar`, `Saturation`, `SearchCommands`, `SelectMenuCreate`, `SelectMenuNoResults`, `SelectMenuSearch`, `ShowDetails`, `ShowLess`, `ShowMore`, `SidebarLabel`, `SkipToContent`, `SlideControls`, `SlideOf`, `SliderMaximum`, `SliderMinimum`, `SortAscending`, `SortDescending`, `StepCompleted`, `TimeAm`, `TimePm`, `Today`, `ToggleColorMode`, `Tomorrow`, `TooManyFiles`, `Unscheduled`

### Theme

`/components/theme`. Cascades Ui overrides to every component of a type below it, without touching each instance.

- `<KlyTheme>`
  - Parameters: `For`, `Overrides`, `Ui`
  - Slots: `ChildContent`

### ThemeStyle

`/components/theme-style`. Delivers the palettes configured in AddKlyrooUi to the page: the color variables and the rule of every extra semantic color.

- `<KlyThemeStyle>`
  - Parameters: `Colors`, `Nonce`, `Scope`

## Page

### Page

`/components/page`. A grid of up to three columns: optional left and right asides around the main content.

- `<KlyPage>`
  - Parameters: `Class`, `Ui`
  - Slots: `Left`, `Right`, `ChildContent`
  - `Ui` (`PageUi`): `Root`, `Left`, `Center`, `Right`

### PageHeader

`/components/page-header`. The top of a page: a headline, a title, a description and links.

- `<KlyPageHeader>`
  - Parameters: `Class`, `Description`, `HeadingLevel`, `Headline`, `Links`, `Title`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageHeaderUi`): `Root`, `Wrapper`, `Headline`, `Title`, `Description`, `Links`, `Body`

### PageHero

`/components/page-hero`. A landing hero with a headline, a title, a description, links and an optional media slot.

- `<KlyPageHero>`
  - Parameters: `Class`, `Description`, `HeadingLevel`, `Headline`, `Links`, `Orientation`, `Reverse`, `Title`, `Ui`
  - Slots: `Media`, `ChildContent`
  - `Ui` (`PageHeroUi`): `Root`, `Container`, `Wrapper`, `Headline`, `Title`, `Description`, `Links`, `Media`

### PageSection

`/components/page-section`. A marketing section with a headline, a title, a description, features, links and media.

- `<KlyPageSection>`
  - Parameters: `Class`, `Description`, `FeatureHeadingLevel`, `Features`, `HeadingLevel`, `Headline`, `Icon`, `Links`, `Orientation`, `Reverse`, `Title`, `Ui`
  - Slots: `Media`, `ChildContent`
  - `Ui` (`PageSectionUi`): `Root`, `Container`, `Wrapper`, `Leading`, `LeadingIcon`, `Headline`, `Title`, `Description`, `Links`, `Features`, `Feature`, `Media`
- `<PageFeatureItem>`
  - Parameters: `Description`, `Href`, `Icon`, `Target`, `Title`
- `<PageLinkItem>`
  - Parameters: `Active`, `Color`, `Disabled`, `Exact`, `Href`, `Icon`, `Label`, `Target`, `TrailingIcon`, `Variant`

### PageFeature

`/components/page-feature`. An icon, a title and a description, optionally one link over the whole feature.

- `<KlyPageFeature>`
  - Parameters: `Class`, `Color`, `Description`, `HeadingLevel`, `Href`, `Icon`, `Orientation`, `Target`, `Title`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageFeatureUi`): `Root`, `Leading`, `LeadingIcon`, `Wrapper`, `Title`, `Description`, `Link`

### PageGrid

`/components/page-grid`. A responsive grid of two to four columns.

- `<KlyPageGrid>`
  - Parameters: `Class`, `Columns`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageGridUi`): `Root`

### PageColumns

`/components/page-columns`. Masonry-style CSS columns: items of different heights pack without gaps.

- `<KlyPageColumns>`
  - Parameters: `Class`, `Columns`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageColumnsUi`): `Root`

### PageCard

`/components/page-card`. A card with an icon, a title and a description, in four variants, optionally one link over the whole card.

- `<KlyPageCard>`
  - Parameters: `Class`, `Color`, `Description`, `HeadingLevel`, `Href`, `Icon`, `Orientation`, `Reverse`, `Target`, `Title`, `Ui`, `Variant`
  - Slots: `Footer`, `Header`, `ChildContent`
  - `Ui` (`PageCardUi`): `Root`, `Spotlight`, `Header`, `Container`, `Wrapper`, `Body`, `Leading`, `LeadingIcon`, `Title`, `Description`, `Footer`, `Link`

### PageCta

`/components/page-cta`. A call to action band with a title, a description and links, in four variants.

- `<KlyPageCta>`
  - Parameters: `Class`, `Color`, `Description`, `HeadingLevel`, `Links`, `Orientation`, `Reverse`, `Title`, `Ui`, `Variant`
  - Slots: `ChildContent`
  - `Ui` (`PageCtaUi`): `Root`, `Container`, `Wrapper`, `Title`, `Description`, `Links`, `Body`

### PageLinks

`/components/page-links`. A titled list of links that marks the current page with aria-current.

- `<KlyPageLinks>`
  - Parameters: `Class`, `HeadingLevel`, `Links`, `Title`, `Ui`
  - `Ui` (`PageLinksUi`): `Root`, `Title`, `List`, `Item`, `Link`, `Icon`, `Label`, `ExternalIcon`
- `<PageLinkItem>`
  - Parameters: `Active`, `Color`, `Disabled`, `Exact`, `Href`, `Icon`, `Label`, `Target`, `TrailingIcon`, `Variant`

### PageList

`/components/page-list`. A vertical list with optional dividers, for stacks of users, posts or cards.

- `<KlyPageList>`
  - Parameters: `Class`, `Divide`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageListUi`): `Root`

### PageLogos

`/components/page-logos`. A strip of logos, optionally scrolling as a marquee.

- `<KlyPageLogos>`
  - Parameters: `Class`, `Duration`, `HeadingLevel`, `ImageOptions`, `Items`, `Marquee`, `Title`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageLogosUi`): `Root`, `Title`, `Items`, `Item`, `Icon`, `Image`, `Marquee`
- `<PageLogoItem>`
  - Parameters: `Alt`, `Icon`, `Src`

### PageAnchors

`/components/page-anchors`. A list of in-page anchors, typically the table of contents in a right column.

- `<KlyPageAnchors>`
  - Parameters: `Class`, `Links`, `Ui`
  - `Ui` (`PageAnchorsUi`): `Root`, `List`, `Item`, `Link`, `Leading`, `LeadingIcon`, `Label`, `ExternalIcon`

### PageAside

`/components/page-aside`. A sticky aside for page navigation, shown from the lg breakpoint.

- `<KlyPageAside>`
  - Parameters: `As`, `Class`, `Ui`
  - Slots: `Bottom`, `Top`, `ChildContent`
  - `Ui` (`PageAsideUi`): `Root`, `Top`, `Body`, `Bottom`

### PageBody

`/components/page-body`. A wrapper that styles plain HTML content: headings, paragraphs, lists, code and quotes.

- `<KlyPageBody>`
  - Parameters: `Class`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PageBodyUi`): `Root`

### AuthForm

`/components/auth-form`. A sign-in or sign-up form built from a description of its fields, with OAuth provider buttons, validation and a loading state. It posts in static rendering too.

- `<KlyAuthForm>`
  - Parameters: `Class`, `Color`, `Description`, `Disabled`, `Enhance`, `Error`, `FieldErrors`, `Fields` (required), `FormName`, `HeadingLevel`, `Loading`, `Providers`, `Separator`, `SubmitLabel`, `Title`, `Ui`, `Validate`
  - Events: `OnInvalidSubmit`, `OnValidSubmit`
  - Slots: `Footer`, `Header`, `Icon`, `Submit`, `Validation`
  - `Ui` (`AuthFormUi`): `Root`, `Header`, `Leading`, `Title`, `Description`, `Providers`, `Form`, `Fieldset`, `Error`, `Footer`, `PasswordRoot`, `PasswordButton`

### PricingPlan

`/components/pricing-plan`. A card for one plan: title, badge, price with an optional discount, feature list and a call to action.

- `<KlyPricingPlan>`
  - Parameters: `Badge`, `BadgeColor`, `BillingCycle`, `ButtonColor`, `ButtonHref`, `ButtonLabel`, `ButtonVariant`, `Class`, `Color`, `Description`, `Discount`, `Features`, `HeadingLevel`, `Highlight`, `Orientation`, `Price`, `Scale`, `Tagline`, `Terms`, `Title`, `Ui`, `Variant`
  - Events: `OnButtonClick`
  - Slots: `Footer`
  - `Ui` (`PricingPlanUi`): `Root`, `Intro`, `Header`, `TitleRow`, `Title`, `Badge`, `Description`, `Tagline`, `Pricing`, `PriceRow`, `Price`, `Discount`, `BillingCycle`, `Terms`, `Features`, `Feature`, `FeatureIcon`, `Footer`

### PricingPlans

`/components/pricing-plans`. A responsive group of plans, side by side or stacked.

- `<KlyPricingPlans>`
  - Parameters: `Class`, `Orientation`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`PricingPlansUi`): `Root`

### PricingTable

`/components/pricing-table`. A feature comparison across tiers: sticky header, check and minus icons with hidden text, tiers stacked on small screens.

- `<KlyPricingTable>`
  - Parameters: `Caption`, `Class`, `Color`, `Sections` (required), `Sticky`, `Tiers` (required), `Ui`
  - `Ui` (`PricingTableUi`): `Root`, `Table`, `Caption`, `Thead`, `HeadRow`, `Corner`, `Tier`, `TierTitle`, `TierBadge`, `TierDescription`, `TierPrice`, `TierDiscount`, `TierCycle`, `TierButton`, `Tbody`, `SectionRow`, `Section`, `Row`, `Feature`, `Cell`, `ValueIcon`, `Included`, `NotIncluded`

### BlogPost

`/components/blog-post`. A post card with a cover image, a culture formatted date, a badge and authors. The title is the only link and covers the card.

- `<KlyBlogPost>`
  - Parameters: `Authors`, `Badge`, `BadgeColor`, `Class`, `Color`, `Culture`, `Date`, `DateFormat`, `Description`, `HeadingLevel`, `Href`, `Image`, `ImageAlt`, `ImageOptions`, `Orientation`, `Target`, `Title`, `Ui`, `Variant`
  - Slots: `Footer`, `Header`, `ChildContent`
  - `Ui` (`BlogPostUi`): `Root`, `Header`, `Image`, `Body`, `Meta`, `Date`, `Badge`, `Title`, `Link`, `Description`, `Authors`, `Footer`

### BlogPosts

`/components/blog-posts`. A responsive grid or a stacked list of blog posts.

- `<KlyBlogPosts>`
  - Parameters: `Class`, `Orientation`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`BlogPostsUi`): `Root`

### ChangelogVersion

`/components/changelog-version`. One release: version, date, badge, rich description and authors, with a timeline indicator.

- `<KlyChangelogVersion>`
  - Parameters: `Authors`, `Badge`, `BadgeColor`, `Class`, `Color`, `Culture`, `Date`, `DateFormat`, `Description`, `HeadingLevel`, `Href`, `Image`, `ImageAlt`, `ImageOptions`, `Indicator`, `Target`, `Title`, `Ui`
  - Slots: `Actions`, `Footer`, `ChildContent`
  - `Ui` (`ChangelogVersionUi`): `Root`, `Indicator`, `Container`, `Header`, `Meta`, `Date`, `Badge`, `Title`, `Link`, `Description`, `Image`, `ImageElement`, `Body`, `Authors`, `Actions`, `Footer`

### ChangelogVersions

`/components/changelog-versions`. The timeline that joins changelog versions with a line.

- `<KlyChangelogVersions>`
  - Parameters: `Class`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`ChangelogVersionsUi`): `Root`

## Dashboard

### DashboardGroup

`/components/dashboard-group`. The root of a dashboard: a sidebar and resizable panels in a row, with their sizes and collapsed state remembered under a storage key.

- `<KlyDashboardGroup>`
  - Parameters: `Class`, `Sizes` (@bind), `StorageKey`, `Ui`
  - Slots: `ChildContent`
  - `Ui` (`DashboardGroupUi`): `Root`

### DashboardSidebar

`/components/dashboard-sidebar`. A resizable sidebar that collapses to icons and becomes an off-canvas panel below the lg breakpoint. It builds on Sidebar.

- `<KlyDashboardSidebar>`
  - Parameters: `AriaLabel`, `As`, `Class`, `Collapsed` (@bind), `Collapsible`, `Id`, `MaxWidth`, `MinWidth`, `MobileTitle`, `Resizable`, `Ui`, `Width`
  - Slots: `Footer`, `Header`, `ChildContent`
  - `Ui` (`SidebarUi`): `Root`, `Host`, `Panel`, `Header`, `HeaderContent`, `Toggle`, `ToggleIcon`, `Content`, `Footer`, `Handle`, `Mobile`, `MobileHeader`, `MobileTitle`, `MobileClose`

### DashboardSidebarCollapse

`/components/dashboard-sidebar-collapse`. A button that collapses the sidebar to icons from the lg breakpoint up.

- `<KlyDashboardSidebarCollapse>`
  - Parameters: `Class`, `Color`, `For`, `Label`, `Size`, `Variant`

### DashboardSidebarToggle

`/components/dashboard-sidebar-toggle`. A button that opens the sidebar as an off-canvas panel below the lg breakpoint.

- `<KlyDashboardSidebarToggle>`
  - Parameters: `Class`, `Color`, `For`, `Label`, `Size`, `Variant`

### DashboardPanel

`/components/dashboard-panel`. A column with a header, a scrollable body and a footer, optionally resizable between limits.

- `<KlyDashboardPanel>`
  - Parameters: `Class`, `MaxSize`, `MinSize`, `Resizable`, `ResizeLabel`, `Size`, `Ui`
  - Slots: `Body`, `Footer`, `Header`, `ChildContent`
  - `Ui` (`DashboardPanelUi`): `Root`, `Header`, `Body`, `Footer`

### DashboardResizeHandle

`/components/dashboard-resize-handle`. A separator that resizes the panel before it with the mouse or the keyboard.

- `<KlyDashboardResizeHandle>`
  - Parameters: `Class`, `Controls`, `Label`, `Step`, `Ui`, `Value`
  - `Ui` (`DashboardResizeHandleUi`): `Root`

### DashboardNavbar

`/components/dashboard-navbar`. The top bar of a panel: sidebar toggle, title, and left, center and right areas.

- `<KlyDashboardNavbar>`
  - Parameters: `As`, `Class`, `HeadingLevel`, `Title`, `Toggle`, `Ui`
  - Slots: `Leading`, `Left`, `Right`, `Trailing`, `ChildContent`
  - `Ui` (`DashboardNavbarUi`): `Root`, `Left`, `Title`, `Center`, `Right`

### DashboardToolbar

`/components/dashboard-toolbar`. A secondary bar under a navbar, for filters and bulk actions.

- `<KlyDashboardToolbar>`
  - Parameters: `Class`, `Ui`
  - Slots: `Left`, `Right`, `ChildContent`
  - `Ui` (`DashboardToolbarUi`): `Root`, `Left`, `Center`, `Right`

### DashboardSearch

`/components/dashboard-search`. A command palette in a modal, opened by a shortcut (Ctrl or Cmd plus K by default).

- `<KlyDashboardSearch>`
  - Parameters: `Class`, `Groups` (required), `Label`, `Open` (@bind), `Placeholder`, `Shortcut`, `Ui`
  - Events: `OnSelect`
  - `Ui` (`CommandPaletteUi`): `Root`, `InputWrapper`, `InputIcon`, `Input`, `Listbox`, `Group`, `GroupLabel`, `Item`, `ItemIcon`, `ItemWrapper`, `ItemLabel`, `ItemDescription`, `ItemShortcut`, `ItemCount`, `Empty`, `Status`

### DashboardSearchButton

`/components/dashboard-search-button`. A button that opens the search and shows its shortcut.

- `<KlyDashboardSearchButton>`
  - Parameters: `Class`, `Keys`, `Label`, `Ui`
  - `Ui` (`DashboardSearchButtonUi`): `Root`, `Icon`, `Label`, `Kbds`

## Chat

### ChatMessage

`/components/chat-message`. One message of a conversation, with an avatar, a surface, a header, attachments and actions. It renders the markup or text you give it: markdown is the app's job (render it with Markdig and pass the HTML as content).

- `<KlyChatMessage>`
  - Parameters: `AvatarAlt`, `AvatarSrc`, `AvatarText`, `Class`, `Color`, `Compact`, `Role`, `Side`, `Streaming`, `Text`, `Ui`, `Variant`
  - Slots: `Actions`, `Files`, `Header`, `Icon`, `Leading`, `ChildContent`
  - `Ui` (`ChatMessageUi`): `Root`, `Container`, `Leading`, `Wrapper`, `Header`, `Files`, `Content`, `Text`, `Actions`

### ChatMessages

`/components/chat-messages`. The log of a conversation. It follows new messages while you are at the bottom, stops when you scroll up and offers a button to jump back, shows a typing indicator while an answer is pending, and announces a finished answer once instead of token by token.

- `<KlyChatMessages>`
  - Parameters: `AutoScroll`, `Class`, `Label`, `Status`, `Ui`
  - Slots: `Error`, `Indicator`, `ChildContent`
  - `Ui` (`ChatMessagesUi`): `Root`, `Viewport`, `Content`, `Indicator`, `Dot`, `Error`, `Jump`, `JumpIcon`, `Status`

### ChatPrompt

`/components/chat-prompt`. A message form with a textarea that grows with its text. Enter sends, Shift+Enter adds a line, an IME composition never sends. It works in static server rendering as a form post.

- `<KlyChatPrompt>`
  - Parameters: `Autofocus`, `Class`, `Color`, `Disabled`, `Enhance`, `FormName`, `Label`, `MaxRows`, `Placeholder`, `Rows`, `Status`, `SubmitOnEnter`, `Ui`, `Value`, `Variant`
  - Events: `OnReload`, `OnStop`, `OnSubmit`
  - Slots: `Attachments`, `Footer`, `Leading`, `Trailing`
  - `Ui` (`ChatPromptUi`): `Root`, `Form`, `Attachments`, `Row`, `Textarea`, `Trailing`, `Footer`

### ChatPromptSubmit

`/components/chat-prompt-submit`. The prompt's button: send, stop while the answer is on its way, retry after an error.

- `<KlyChatPromptSubmit>`
  - Parameters: `Class`, `Color`, `Disabled`, `Label`, `Size`, `Status`, `Ui`
  - Events: `OnReload`, `OnStop`
  - `Ui` (`ChatPromptSubmitUi`): `Base`, `Icon`, `StopIcon`

### ChatPalette

`/components/chat-palette`. A full chat panel: messages that take the room that is left, and a prompt pinned under them. Put it in a modal, a slideover or any box with a height.

- `<KlyChatPalette>`
  - Parameters: `Class`, `Ui`
  - Slots: `Prompt`, `ChildContent`
  - `Ui` (`ChatPaletteUi`): `Root`, `Content`, `Prompt`

### ChatAttachment

`/components/chat-attachment`. A file as a chip: an icon or an image preview, its name and size, and a remove button.

- `<KlyChatAttachment>`
  - Parameters: `Class`, `Color`, `Description`, `FileSize`, `ImageOptions`, `Name` (required), `Orientation`, `Removable`, `Size`, `Status`, `Type`, `Ui`, `Url`, `Variant`
  - Events: `OnRemove`
  - Slots: `Icon`
  - `Ui` (`ChatAttachmentUi`): `Root`, `Leading`, `LeadingIcon`, `Image`, `Wrapper`, `Name`, `Description`, `Remove`, `RemoveIcon`

### ChatReasoning

`/components/chat-reasoning`. The model's thinking as a collapsible block. It opens while streaming, closes shortly after, and tells how long it took.

- `<KlyChatReasoning>`
  - Parameters: `AutoCloseDelay`, `AutoOpen`, `Chevron`, `Class`, `Disabled`, `Duration`, `Open` (@bind), `Streaming`, `Text`, `Ui`
  - Slots: `Icon`, `ChildContent`
  - `Ui` (`ChatReasoningUi`): `Root`, `Trigger`, `Leading`, `LeadingIcon`, `ChevronIcon`, `Label`, `Content`, `Body`

### ChatShimmer

`/components/chat-shimmer`. Text with a sweeping highlight, for loading states. With reduced motion it is static, muted text.

- `<KlyChatShimmer>`
  - Parameters: `Class`, `Duration`, `Spread`, `Text` (required), `Ui`
  - `Ui` (`ChatShimmerUi`): `Base`

### ChatTool

`/components/chat-tool`. A tool call of the assistant: its name, whether it runs or failed, and optionally its input and output in a collapsible part.

- `<KlyChatTool>`
  - Parameters: `Chevron`, `Class`, `Disabled`, `Error`, `Loading`, `Open` (@bind), `Streaming`, `Suffix`, `Text`, `Ui`, `Variant`
  - Slots: `Actions`, `Icon`, `ChildContent`
  - `Ui` (`ChatToolUi`): `Root`, `Trigger`, `Leading`, `LeadingIcon`, `ChevronIcon`, `Label`, `Suffix`, `Content`, `Body`, `Actions`

## Editor

### Editor

`/components/editor`. A rich text editor built on TipTap. The value is an HTML string; it loads from a separate file only on pages that have an editor, and static pages show the HTML read-only until it is ready.

- `<KlyEditor>`
  - Parameters: `Class`, `Color`, `Disabled`, `Id`, `ImageSchemes`, `Label`, `LinkSchemes`, `MaxLength`, `Name`, `Placeholder`, `ReadOnly`, `Required`, `Size`, `Ui`, `Value` (@bind), `ValueExpression`, `Variant`
  - Events: `OnLimit`
  - Slots: `ChildContent`
  - `Ui` (`EditorUi`): `Root`, `Content`, `Base`, `Fallback`

### EditorToolbar

`/components/editor-toolbar`. Formatting buttons for the editor, fixed above the text or floating over a selection, with pressed states that follow the cursor.

- `<KlyEditorToolbar>`
  - Parameters: `Class`, `Color`, `Items`, `Label`, `Layout`, `Size`, `Ui`
  - `Ui` (`EditorToolbarUi`): `Root`, `Button`, `Icon`, `Separator`, `LinkForm`, `LinkInput`, `LinkButton`, `LinkMessage`
- `<EditorToolbarItem>`
  - Parameters: `Action`, `IsSeparator`, `Label`, `Level`
- `<EditorAction>`
- `<EditorToolbarLayout>`

### EditorDragHandle

`/components/editor-drag-handle`. A handle beside the block under the pointer that drags it to a new place. Alt and the arrow keys move a block from the keyboard.

- `<KlyEditorDragHandle>`
  - Parameters: `Class`, `Color`, `Label`, `Ui`
  - `Ui` (`EditorDragHandleUi`): `Root`, `Handle`, `Icon`

### EditorSuggestionMenu

`/components/editor-suggestion-menu`. A slash menu that inserts blocks and formats. Items come from .NET; filtering and keys run in the browser.

- `<KlyEditorSuggestionMenu>`
  - Parameters: `Char`, `Class`, `Items`, `Label`, `Limit`, `Size`, `Ui`
  - `Ui` (`EditorMenuUi`): `Root`, `Content`, `Listbox`, `Item`, `Icon`, `Body`, `ItemLabel`, `ItemDescription`, `Empty`
- `<EditorSuggestionItem>`
  - Parameters: `Action`, `Description`, `Disabled`, `Keywords`, `Label`, `Level`

### EditorMentionMenu

`/components/editor-mention-menu`. An @ menu that inserts a mention from a list of items.

- `<KlyEditorMentionMenu>`
  - Parameters: `Char`, `Class`, `Items` (required), `Label`, `Limit`, `Size`, `Ui`
  - `Ui` (`EditorMenuUi`): `Root`, `Content`, `Listbox`, `Item`, `Icon`, `Body`, `ItemLabel`, `ItemDescription`, `Empty`
- `<EditorMentionItem>`
  - Parameters: `Description`, `Disabled`, `Id`, `Label`

### EditorEmojiMenu

`/components/editor-emoji-menu`. A colon menu that inserts an emoji.

- `<KlyEditorEmojiMenu>`
  - Parameters: `Char`, `Class`, `Items`, `Label`, `Limit`, `Size`, `Ui`
  - `Ui` (`EditorMenuUi`): `Root`, `Content`, `Listbox`, `Item`, `Icon`, `Body`, `ItemLabel`, `ItemDescription`, `Empty`
- `<EditorEmojiItem>`
  - Parameters: `Emoji`, `Keywords`, `Name`
