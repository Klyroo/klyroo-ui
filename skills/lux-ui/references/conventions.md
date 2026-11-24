# Conventions

## Naming

- Components are `Lx` plus the name: `LxButton`, `LxDropdownMenu`, `LxPageHero`. The docs slug is the kebab-case name (`dropdown-menu`).
- Per-instance style records end in `Ui` (`ButtonUi`, `CardUi`) and have one property per slot.
- Data records are `Lx` plus what they describe: `LxSelectItem`, `LxMenuItem`, `LxTabItem`, `LxNavigationItem`, `LxToggleItem`, `LxBreadcrumbItem`, `LxCommandGroup`, `LxAuthField`.
- Enums keep short names: `ButtonVariant` (buttons), `LxVariant` (badges, alerts), `LxSize` (`Xs`, `Sm`, `Md`, `Lg`, `Xl`), `LxOrientation`.
- Any attribute that is not a parameter (`data-testid`, `aria-label`, `class`) is forwarded to the main element. `Class` is a parameter that merges after the theme.

## Render modes

Every component works in Interactive Server, WebAssembly, Auto and static server rendering, with the same markup.

- **In the browser** (custom elements, no round trip): opening and closing overlays, focus, keyboard navigation, positioning, tab and accordion selection, select options, toast display.
- **In .NET**: the values you bind (`@bind-Value`, `@bind-Open`), click handlers, validation.
- Register `AddLuxUi` with the same options in the server project and, for WebAssembly or Auto, in the `.Client` project. A shared extension method keeps both in sync.
- Use the standard Blazor render mode attributes; nothing Lux-specific is needed.
- **MAUI Blazor Hybrid** (a `BlazorWebView`) has no render modes: it is always interactive, with no prerendering and no `HttpContext`. Call `AddLuxUi` from `MauiProgram.cs`, link the stylesheets from `wwwroot/index.html`, and add a `HeadOutlet` root component (`head::after`) so `LxHead` can write the theme and fonts into the head. `LuxHost.IsHybrid(RendererInfo)` tells the host; server-only features (`Lux.UI.Images.Local`, `Lux.UI.Server`, the culture cookie) do nothing there. Coarse pointers get 44 px hit areas. TV mode (`<LxApp SpatialNavigation="true">`) is experimental. Read `/docs/maui` before writing code for a MAUI host.

## Binding

`@bind-Value` and `@bind-Open` exist where a component has the pair `Value`/`ValueChanged` or `Open`/`OpenChanged`; `get-component-metadata` marks them `[bind]`.

```razor
<LxTabs Items="_items" @bind-Value="_tab" AriaLabel="Settings" />
<LxPopover @bind-Open="_open">
    <ChildContent><LxButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="p-3">Panel</p></Content>
</LxPopover>
```

- **Unbound is the cheap default.** With no `@bind-Open` the overlay opens and closes entirely in the browser, and a Server-mode app pays no round trip per click or key. Bind only when your code reads or sets the state.
- **Lists bind as lists.** Multiple selection components (`LxSelectMenu` and `LxToggleGroup` with `Multiple`) bind an `IReadOnlyList<string>`; `LxCheckboxGroup<TValue>` binds an `IReadOnlyList<TValue>`.
- **Dates are `DateOnly?`** (`LxCalendar`, `LxInputDate`) and post as `yyyy-MM-dd`.

## Static server rendering

- Forms post: `LxSwitch`, `LxCheckbox`, `LxRadioGroup`, `LxInput`, `LxSelect` and the other controls render real form fields (custom ones post through a hidden input). Give the form a `FormName` and read the model with `[SupplyParameterFromForm(FormName = "...")]`.
- Overlays, tabs, accordions, popovers and tooltips keep working because their behavior is in the browser.
- Anything that changes the page from a .NET handler needs interactivity. In static pages such a control must be absent, disabled, or replaced by a link or a form post.

## Inert buttons

A `LxButton` with `OnClick` renders disabled until the page is interactive, because its handler cannot run before that. The rules:

- A link (`Href`) and a submit button (`Type="submit"`) work without interactivity and are never inert.
- Do not put `OnClick` on a button that must work on a static page. Use a `LxForm` or `EditForm` post, or `Href`.
- Do not "fix" a disabled-looking button by removing `Disabled`; it enables itself once the circuit or the WebAssembly runtime is ready.

```razor
<LxButton Label="Save" OnClick="SaveAsync" />            @* disabled until interactive *@
<LxButton Label="Docs" Href="/docs" />                    @* a link, always works *@
<LxButton Type="submit" Label="Create" />                 @* posts the enclosing form *@
```

## Events

Event parameters are `EventCallback<T>`: `OnClick` (`MouseEventArgs`), `OnSelect` on menus and the command palette (the selected item), `OnValidSubmit` on forms. Handlers only attach when you pass one, so an unused event costs nothing.

## Icons

`<LxIcon Name="search" />` draws an icon by name, and `LxIcons.Named("home", "size-5")` does the same where a component takes a `RenderFragment` for an icon. Any Iconify icon works by name: `i-{collection}-{name}` (`i-ph-house`, `i-simple-icons-github`) or `{collection}:{name}` (`ph:house`, `mdi:home`), and a bare name is a Lucide icon. The Lucide icons that components use are built in (see `search-icons`); every other icon written as a literal in the app's `.razor`/`.cs` files is downloaded at build time and compiled into the app, so nothing is requested when the page runs. A name built at runtime is invisible to that scan: list it in the project file as `<LuxIcon Include="ph:cube;ph:cube-fill" />`. A folder of SVG files is a collection: `<LuxIconCollection Include="Assets/icons" Prefix="custom" />` gives `custom:file-name`. The icons components draw are replaced by name in `AddLuxUi`: `o.Icons.Close = "ph:x"` (keys such as `Close`, `Check`, `ChevronDown`, `Loading`, `Search`, as in Nuxt UI), and `o.Icons.Register("custom:logo", svgBody, 24, 24)` adds an icon from code. An icon that cannot be found renders an empty icon marked `data-lx-missing-icon`. See the Icons guide (`/docs/icons`).

## Loops and ids

In a Razor `for` loop copy the index into a local before a child component's content or a lambda uses it; the content is evaluated after the loop and would see the last index.

```razor
@for (var i = 0; i < _items.Count; i++)
{
    var index = i;
    <LxButton Label="@_items[index]" OnClick="() => Remove(index)" />
}
```

## Accessibility

Provide an accessible name wherever the component has no visible label: `AriaLabel` on `LxTabs`, `LxNavigationMenu`, `LxToggleGroup`, `LxBreadcrumb`, `LxCalendar`; `Label` or `aria-label` on icon-only buttons (`Square`). Landmarks: `LxMain` is the page's `<main>` and a page has one, so inside a page that already has one pass `As="div"`.
