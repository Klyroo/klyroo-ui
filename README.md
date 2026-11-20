# blux-ui

Accessible, themable Blazor components styled with Tailwind CSS v4, inspired by Nuxt UI.
Status: early development, see [PLAN.md](PLAN.md). 20 components:

- **Elements**: Button, Badge, Avatar, Card, Alert
- **Form**: Input, Textarea, Checkbox, Switch, RadioGroup, Select, FormField
- **Overlay**: Modal, Popover, Tooltip, DropdownMenu, Toast
- **Navigation & data**: Tabs, Accordion, Table

## Install

Requires .NET 10.

```bash
dotnet add package Blux.UI
```

## Use it

```csharp
// Program.cs of the server AND of the .Client project (share the call in a method of the client project)
builder.Services.AddBluxUi();
```

Styles come in two modes. Without Tailwind in your app, load the precompiled stylesheets:

```razor
@* App.razor, in <head> *@
<BxColorModeScript />
<link rel="stylesheet" href="@Assets["_content/Blux.UI/blux.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Blux.UI/blux.css"]" />
```

```razor
@using Blux.UI

<BxButton Label="Save" OnClick="Save" />
<BxSwitch @bind-Value="notify" Label="Email notifications" />
<BxPopover @bind-Open="open">
    <ChildContent><BxButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="p-3">Panel</p></Content>
</BxPopover>
```

### Render modes

Components work in Interactive Server, WebAssembly and Auto, and in static server rendering (Switch posts forms,
Popover opens and closes in the browser). A button with `OnClick` renders disabled until the page is interactive,
because its handler cannot run before that. A Popover trigger must contain a `<button>`; bind `@bind-Open` when .NET needs
to know the state, otherwise opening and closing never leave the browser. If your app still loads Bootstrap from the Blazor template, remove it or import it
in a layer (`@import url(bootstrap.css) layer(bootstrap);`): unlayered CSS overrides Tailwind v4's layered CSS.

### Styles with Tailwind v4 compiled by your app

Every build copies the Blux theme (tokens, dark variant, the classes components use) to `obj/blux/blux.theme.css`.
Import it after Tailwind, and load only your compiled stylesheet (not `blux.css` nor `blux.preflight.css`):

```css
@import "tailwindcss";
@import "../obj/blux/blux.theme.css"; /* path relative to this file */
```

`Class` and `Ui` then accept any utility, since your Tailwind build sees them in your Razor files.

## Customize

- **Tokens** (works everywhere): override `--blux-primary`, `--blux-radius`, `--blux-bg`… in your CSS, in both
  `:root` and `.dark`. Your unlayered `:root` rule beats the library's dark values, so a color overridden only in
  `:root` keeps its light value in dark mode while its paired `-contrast` token still flips.
- **Custom color**: `Color="@("brand")"` plus `[data-color='brand'] { --bx-color: …; --bx-color-text: …; --bx-color-contrast: …; }`.
- **Global theme**: `AddBluxUi(o => o.Button.Slot("base", "font-semibold"))`.
- **Per instance**: `Class="…"` on the root and `Ui="@(new ButtonUi { Label = "…" })"` per slot. These take Tailwind
  utilities, so they need Tailwind compiled by your app; without it, only classes already in `blux.css` or your own CSS apply.

## License

MIT
