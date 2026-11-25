---
name: klyroo-ui
description: Build Blazor user interfaces with Klyroo.UI (Kly components, Tailwind CSS v4, inspired by Nuxt UI). Use when a task adds or changes Blazor markup that uses or should use Klyroo.UI, picks a component, styles or themes it, builds forms, dashboards, chat, editor or landing pages, sets up the library, or when a project references the Klyroo.UI package.
---

# Klyroo.UI

Klyroo.UI is a Blazor component library of 133 accessible components, styled with Tailwind CSS v4 and modeled on Nuxt UI. Components are prefixed `Kly` (`<KlyButton>`), work in Interactive Server, WebAssembly, Auto and static server rendering, and keep UI state (open, focus, selection, keyboard navigation) in the browser. They also run in .NET MAUI Blazor Hybrid apps (a `BlazorWebView`): Android is tested on an emulator, iOS 17 or later, Windows and macOS (Mac Catalyst) have CI build jobs that have not run yet and are untested on devices, Android TV is experimental, Tizen is not supported (see `/docs/maui`).

## Use the MCP server for API details

Never write a parameter from memory. This skill holds the conventions and patterns; the exact API comes from the Klyroo.UI MCP server.

- `search-components` finds a component by name, family or intent (dialog finds Modal).
- `get-component-metadata` returns the exact parameters, events, slots and `Ui` slots as JSON. Call it before writing markup for a component you have not used in this session.
- `get-component` returns usage, examples, api and theme (`sections`), `get-example` the source of one example.
- `get-installation-guide`, `search-documentation` and `get-documentation-page` cover setup and guides.

If the server is not available, add it (local, offline): `dotnet tool install --global Klyroo.UI.Mcp --prerelease`, then for Claude Code `claude mcp add --transport stdio klyroo-ui -- klyroo-ui-mcp`. For other agents and for the hosted HTTP form see `/docs/mcp` of the docs site. Without any server, read `references/components.md` (every component with its parameter names) and the site's `/llms.txt`.

## Core rules

1. **Register once, in every project that renders.** `builder.Services.AddKlyrooUi();` in the server `Program.cs`, and in the `.Client` project too for WebAssembly or Auto, with the same options (share a method).
2. **Imports and styles.** `@using Klyroo.UI` in `_Imports.razor`. Put `<KlyColorModeScript />` in `<head>` before the stylesheets. Load `klyroo.preflight.css` and `klyroo.css` from `_content/Klyroo.UI/`, or compile Tailwind in the app and import `obj/klyroo/klyroo.theme.css`. Remove Bootstrap from the Blazor template or put it in a CSS layer.
3. **Bind only what .NET needs.** `@bind-Value` and `@bind-Open` exist where the component has `Value`/`ValueChanged` or `Open`/`OpenChanged`. An unbound overlay opens and closes without ever calling .NET.
4. **Static rendering is a supported mode.** A `KlyButton` with `OnClick` renders disabled until the page is interactive. For a link use `Href`; for a form use `Type="submit"` inside `KlyForm` or `EditForm` with a `FormName`.
5. **Style through tokens, `Class`, `Ui` and `AddKlyrooUi`**, never by editing component markup. Colors are `KlyColor` values (`KlyColor.Primary`, or any name that has a `[data-color='name']` rule), sizes are `KlySize` (`Xs` to `Xl`), looks are `ButtonVariant` for buttons and `KlyVariant` for badges and alerts.
6. **Use the component, not a re-implementation.** Modals, popovers, menus, tabs and selects follow WAI-ARIA patterns that a hand-made `div` would lose. Pick through `references/component-selection.md`.
7. **Loops capture variables.** In a Razor `for` loop copy the index into a local before a child component's content or a lambda uses it.

## Which reference to read

| Task | Read |
|---|---|
| Colors, tokens, dark mode, `Class`, `Ui`, `AddKlyrooUi`, `KlyTheme` | `references/design-system.md` |
| Choosing between components (modal or drawer, select or listbox, tabs or navigation) | `references/component-selection.md` |
| Render modes, MAUI Blazor Hybrid hosts, `Kly` prefix, `@bind`, static rendering, inert buttons, events | `references/conventions.md` |
| Forms, validation, `EditForm`, `KlyForm`, `KlyFormField`, static posts | `references/forms.md` |
| Dashboard, docs site, chat, editor or landing page layouts | `references/layouts.md` |
| Data table, sign-in, overlays, navigation | `references/recipes.md` |
| Every component and its parameter names | `references/components.md` (generated) |

## Workflow

1. Find the component (`search-components`, or the selection guide).
2. Read its metadata and one example (`get-component-metadata`, `get-example`).
3. Write the markup with those parameters, then run the app in the render mode the project uses and check static, interactive and dark mode.
4. If a parameter you want does not exist, say so and use `Class` or `Ui` for styling, or a different component; do not invent it.
