# Design system

Lux.UI is themed at four levels, from the broadest to the most local: design tokens (CSS variables), the global theme in C#, scoped overrides, and per instance. Pick the broadest level that does the job.

## Tokens

Colors, radius and surfaces are CSS variables prefixed `--lux-`. They work with and without Tailwind. Override them in both `:root` and `.dark`: an unlayered `:root` rule beats the library's dark values, so a color overridden only in `:root` keeps its light value in dark mode.

| Token | Role |
|---|---|
| `--lux-radius` | Base corner radius (0.5rem) |
| `--lux-primary`, `-secondary`, `-success`, `-info`, `-warning`, `-error`, `-neutral` | Solid fills of the semantic colors |
| `--lux-<color>-text` | The same color as text or border on the page background (meets contrast) |
| `--lux-<color>-contrast` | Text that sits on a solid `--lux-<color>` fill |
| `--lux-text`, `--lux-text-highlighted`, `--lux-text-muted`, `--lux-text-dimmed` | Text, from strongest to weakest |
| `--lux-bg`, `--lux-bg-muted`, `--lux-bg-elevated`, `--lux-bg-accented`, `--lux-bg-overlay` | Surfaces, page to overlay |
| `--lux-border`, `--lux-border-accented`, `--lux-control` | Decorative borders, stronger borders, control boundaries |

The default primary is yellow (`#ffd23f`), so text on it is dark (`--lux-primary-contrast`). Do not assume white text on primary.

```css
:root {
  --lux-radius: 0.5rem;
  --lux-primary: #0f766e;
  --lux-primary-text: #115e59;
  --lux-primary-contrast: #ffffff;
}
.dark {
  --lux-primary: #2dd4bf;
  --lux-primary-text: #5eead4;
  --lux-primary-contrast: #042f2e;
}
```

Keep the contrast: text against its background at least 4.5:1, and fills or borders that are a UI cue (a control boundary, a state indicator) at least 3:1, in light and dark. Use `--lux-<color>-text` for colored text, never `--lux-<color>`.

## Colors

A component takes a `Color` (`LxColor`). Built-in: `LxColor.Primary`, `Secondary`, `Success`, `Info`, `Warning`, `Error`, `Neutral`. Each one is a `data-color` attribute that sets three variables, `--lx-color`, `--lx-color-text` and `--lx-color-contrast`, which themes read. A custom color is a name plus those variables, in plain CSS:

```css
[data-color='brand'] { --lx-color: #0f766e; --lx-color-text: #115e59; --lx-color-contrast: #fff; }
```

```razor
<LxButton Label="Brand" Color="@("brand")" />
<LxBadge Label="Brand" Color="@("brand")" Variant="LxVariant.Subtle" />
```

## Dark mode

The `dark` class on `<html>` switches the dark tokens. `<LxColorModeScript />` in `<head>` (before the stylesheets) applies the stored choice, or the system one, before first paint. Ready-made controls, all in the Overlay family: `LxColorModeButton`, `LxColorModeSwitch`, `LxColorModeSelect`, `LxColorModeAvatar`, `LxColorModeImage`. From script: `Lux.setColorMode('light' | 'dark' | 'system')`.

```razor
<LxColorModeButton />
```

## Global theme

Every styled component has a theme in C#: slots (the parts), variants, compound variants and defaults. Configure it in `AddLuxUi`. Classes you add are merged after the defaults with tailwind-merge, so the last conflicting utility wins.

```csharp
builder.Services.AddLuxUi(o =>
{
    o.Button.Slot("base", "rounded-full");
    o.Card.Slot("root", "shadow-sm");
    o.Button.Variant("size", "md", "base", "px-5");
    o.Button.Default("variant", "soft");
});
```

- The property name on `LuxOptions` is the theme (`o.Button`, `o.Card`, `o.Modal`). `get-component` with `sections: "theme"` shows it for each component, with its slot names, variants and defaults.
- `Slot(slot, classes)` adds to a slot, `Variant(variant, value, slot, classes)` to a variant value, `Compound(when, slot, classes)` to a combination, `Default(variant, value)` changes the default.
- A theme becomes read-only the first time a component renders with it: configure it in `AddLuxUi`, not later. `AddLuxUi` may be called several times (a library, then the app) and configures the same options.
- Call it with the same configuration in the server and in the `.Client` project, or components change appearance when Auto mode switches runtime.
- Every class must be a literal so the Tailwind build can see it.

## Scoped and per instance

Order of precedence, last wins: theme defaults, `AddLuxUi`, enclosing `LxTheme` scopes, `Ui`, then `Class` on the root.

```razor
@using Lux.UI.Theming

@* One component *@
<LxButton Label="Save" Class="px-6" Ui="@(new ButtonUi { Label = "uppercase tracking-wide" })" />

@* A subtree: every LxButton inside gets rounded-full *@
<LxTheme For="typeof(LxButton)" Ui='new ButtonUi { Base = "rounded-full" }'>
    <LxButton Label="Rounded" />
</LxTheme>
```

- `Class` adds classes to the root element.
- `Ui` takes the component's record (`ButtonUi`, `CardUi`, ...), one property per slot. Its type and slot names are in `get-component-metadata`.
- `LxTheme` takes `For` and `Ui` for one component type, or `Overrides` (a dictionary of type to `ISlotOverrides`) for several. Themes nest and the innermost wins. `LxApp` also takes `Overrides` for the whole app.

## Tailwind or not

- Precompiled (no Tailwind in the app): `Class` and `Ui` can only use classes that already exist in `lux.css` or in your own CSS. Prefer tokens and `AddLuxUi`.
- Tailwind v4 compiled by the app: import `obj/lux/lux.theme.css` after `@import "tailwindcss";` and load only your stylesheet. `Class` and `Ui` then accept any utility.
