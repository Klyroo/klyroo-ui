# Conventions

## Naming

- Components are `Bx` plus the name: `BxButton`, `BxDropdownMenu`, `BxPageHero`. The docs slug is the kebab-case name (`dropdown-menu`).
- Per-instance style records end in `Ui` (`ButtonUi`, `CardUi`) and have one property per slot.
- Data records are `Bx` plus what they describe: `BxSelectItem`, `BxMenuItem`, `BxTabItem`, `BxNavigationItem`, `BxToggleItem`, `BxBreadcrumbItem`, `BxCommandGroup`, `BxAuthField`.
- Enums keep short names: `ButtonVariant` (buttons), `BxVariant` (badges, alerts), `BxSize` (`Xs`, `Sm`, `Md`, `Lg`, `Xl`), `BxOrientation`.
- Any attribute that is not a parameter (`data-testid`, `aria-label`, `class`) is forwarded to the main element. `Class` is a parameter that merges after the theme.

## Render modes

Every component works in Interactive Server, WebAssembly, Auto and static server rendering, with the same markup.

- **In the browser** (custom elements, no round trip): opening and closing overlays, focus, keyboard navigation, positioning, tab and accordion selection, select options, toast display.
- **In .NET**: the values you bind (`@bind-Value`, `@bind-Open`), click handlers, validation.
- Register `AddBluxUi` with the same options in the server project and, for WebAssembly or Auto, in the `.Client` project. A shared extension method keeps both in sync.
- Use the standard Blazor render mode attributes; nothing Blux-specific is needed.

## Binding

`@bind-Value` and `@bind-Open` exist where a component has the pair `Value`/`ValueChanged` or `Open`/`OpenChanged`; `get-component-metadata` marks them `[bind]`.

```razor
<BxTabs Items="_items" @bind-Value="_tab" AriaLabel="Settings" />
<BxPopover @bind-Open="_open">
    <ChildContent><BxButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="p-3">Panel</p></Content>
</BxPopover>
```

- **Unbound is the cheap default.** With no `@bind-Open` the overlay opens and closes entirely in the browser, and a Server-mode app pays no round trip per click or key. Bind only when your code reads or sets the state.
- **Lists bind as lists.** Multiple selection components (`BxSelectMenu` and `BxToggleGroup` with `Multiple`) bind an `IReadOnlyList<string>`; `BxCheckboxGroup<TValue>` binds an `IReadOnlyList<TValue>`.
- **Dates are `DateOnly?`** (`BxCalendar`, `BxInputDate`) and post as `yyyy-MM-dd`.

## Static server rendering

- Forms post: `BxSwitch`, `BxCheckbox`, `BxRadioGroup`, `BxInput`, `BxSelect` and the other controls render real form fields (custom ones post through a hidden input). Give the form a `FormName` and read the model with `[SupplyParameterFromForm(FormName = "...")]`.
- Overlays, tabs, accordions, popovers and tooltips keep working because their behavior is in the browser.
- Anything that changes the page from a .NET handler needs interactivity. In static pages such a control must be absent, disabled, or replaced by a link or a form post.

## Inert buttons

A `BxButton` with `OnClick` renders disabled until the page is interactive, because its handler cannot run before that. The rules:

- A link (`Href`) and a submit button (`Type="submit"`) work without interactivity and are never inert.
- Do not put `OnClick` on a button that must work on a static page. Use a `BxForm` or `EditForm` post, or `Href`.
- Do not "fix" a disabled-looking button by removing `Disabled`; it enables itself once the circuit or the WebAssembly runtime is ready.

```razor
<BxButton Label="Save" OnClick="SaveAsync" />            @* disabled until interactive *@
<BxButton Label="Docs" Href="/docs" />                    @* a link, always works *@
<BxButton Type="submit" Label="Create" />                 @* posts the enclosing form *@
```

## Events

Event parameters are `EventCallback<T>`: `OnClick` (`MouseEventArgs`), `OnSelect` on menus and the command palette (the selected item), `OnValidSubmit` on forms. Handlers only attach when you pass one, so an unused event costs nothing.

## Icons

`<BxIcon Name="search" />` draws an icon by name, and `BxIcons.Named("home", "size-5")` does the same where a component takes a `RenderFragment` for an icon. Any Iconify icon works by name: `i-{collection}-{name}` (`i-ph-house`, `i-simple-icons-github`) or `{collection}:{name}` (`ph:house`, `mdi:home`), and a bare name is a Lucide icon. The Lucide icons that components use are built in (see `search-icons`); every other icon written as a literal in the app's `.razor`/`.cs` files is downloaded at build time and compiled into the app, so nothing is requested when the page runs. A name built at runtime is invisible to that scan: list it in the project file as `<BluxIcon Include="ph:cube;ph:cube-fill" />`. A folder of SVG files is a collection: `<BluxIconCollection Include="Assets/icons" Prefix="custom" />` gives `custom:file-name`. The icons components draw are replaced by name in `AddBluxUi`: `o.Icons.Close = "ph:x"` (keys such as `Close`, `Check`, `ChevronDown`, `Loading`, `Search`, as in Nuxt UI), and `o.Icons.Register("custom:logo", svgBody, 24, 24)` adds an icon from code. An icon that cannot be found renders an empty icon marked `data-bx-missing-icon`. See the Icons guide (`/docs/icons`).

## Loops and ids

In a Razor `for` loop copy the index into a local before a child component's content or a lambda uses it; the content is evaluated after the loop and would see the last index.

```razor
@for (var i = 0; i < _items.Count; i++)
{
    var index = i;
    <BxButton Label="@_items[index]" OnClick="() => Remove(index)" />
}
```

## Accessibility

Provide an accessible name wherever the component has no visible label: `AriaLabel` on `BxTabs`, `BxNavigationMenu`, `BxToggleGroup`, `BxBreadcrumb`, `BxCalendar`; `Label` or `aria-label` on icon-only buttons (`Square`). Landmarks: `BxMain` is the page's `<main>` and a page has one, so inside a page that already has one pass `As="div"`.
