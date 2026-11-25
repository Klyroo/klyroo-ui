# Design system

Klyroo.UI is themed at four levels, from the broadest to the most local: design tokens (CSS variables), the global theme in C#, scoped overrides, and per instance. Pick the broadest level that does the job.

## Tokens

Colors, radius and surfaces are CSS variables prefixed `--klyroo-`. They work with and without Tailwind. Override them in both `:root` and `.dark`: an unlayered `:root` rule beats the library's dark values, so a color overridden only in `:root` keeps its light value in dark mode.

| Token | Role |
|---|---|
| `--klyroo-radius` | Base corner radius (0.5rem) |
| `--klyroo-primary`, `-secondary`, `-success`, `-info`, `-warning`, `-error`, `-neutral` | Solid fills of the semantic colors |
| `--klyroo-<color>-text` | The same color as text or border on the page background (meets contrast) |
| `--klyroo-<color>-contrast` | Text that sits on a solid `--klyroo-<color>` fill |
| `--klyroo-text`, `--klyroo-text-highlighted`, `--klyroo-text-muted`, `--klyroo-text-dimmed` | Text, from strongest to weakest |
| `--klyroo-bg`, `--klyroo-bg-muted`, `--klyroo-bg-elevated`, `--klyroo-bg-accented`, `--klyroo-bg-overlay` | Surfaces, page to overlay |
| `--klyroo-border`, `--klyroo-border-accented`, `--klyroo-control` | Decorative borders, stronger borders, control boundaries |

The default primary is yellow (`#ffd23f`), so text on it is dark (`--klyroo-primary-contrast`). Do not assume white text on primary.

```css
:root {
  --klyroo-radius: 0.5rem;
  --klyroo-primary: #0f766e;
  --klyroo-primary-text: #115e59;
  --klyroo-primary-contrast: #ffffff;
}
.dark {
  --klyroo-primary: #2dd4bf;
  --klyroo-primary-text: #5eead4;
  --klyroo-primary-contrast: #042f2e;
}
```

Keep the contrast: text against its background at least 4.5:1, and fills or borders that are a UI cue (a control boundary, a state indicator) at least 3:1, in light and dark. Use `--klyroo-<color>-text` for colored text, never `--klyroo-<color>`.

## Colors

A component takes a `Color` (`KlyColor`). Built-in: `KlyColor.Primary`, `Secondary`, `Success`, `Info`, `Warning`, `Error`, `Neutral`. Each one is a `data-color` attribute that sets three variables, `--kly-color`, `--kly-color-text` and `--kly-color-contrast`, which themes read. A custom color is a name plus those variables, in plain CSS:

```css
[data-color='brand'] { --kly-color: #0f766e; --kly-color-text: #115e59; --kly-color-contrast: #fff; }
```

```razor
<KlyButton Label="Brand" Color="@("brand")" />
<KlyBadge Label="Brand" Color="@("brand")" Variant="KlyVariant.Subtle" />
```

## Dark mode

The `dark` class on `<html>` switches the dark tokens. `<KlyColorModeScript />` in `<head>` (before the stylesheets) applies the stored choice, or the system one, before first paint. Ready-made controls, all in the Overlay family: `KlyColorModeButton`, `KlyColorModeSwitch`, `KlyColorModeSelect`, `KlyColorModeAvatar`, `KlyColorModeImage`. From script: `Klyroo.setColorMode('light' | 'dark' | 'system')`.

```razor
<KlyColorModeButton />
```

## Global theme

Every styled component has a theme in C#: slots (the parts), variants, compound variants and defaults. Configure it in `AddKlyrooUi`. Classes you add are merged after the defaults with tailwind-merge, so the last conflicting utility wins.

```csharp
builder.Services.AddKlyrooUi(o =>
{
    o.Button.Slot("base", "rounded-full");
    o.Card.Slot("root", "shadow-sm");
    o.Button.Variant("size", "md", "base", "px-5");
    o.Button.Default("variant", "soft");
});
```

- The property name on `KlyrooOptions` is the theme (`o.Button`, `o.Card`, `o.Modal`). `get-component` with `sections: "theme"` shows it for each component, with its slot names, variants and defaults.
- `Slot(slot, classes)` adds to a slot, `Variant(variant, value, slot, classes)` to a variant value, `Compound(when, slot, classes)` to a combination, `Default(variant, value)` changes the default.
- A theme becomes read-only the first time a component renders with it: configure it in `AddKlyrooUi`, not later. `AddKlyrooUi` may be called several times (a library, then the app) and configures the same options.
- Call it with the same configuration in the server and in the `.Client` project, or components change appearance when Auto mode switches runtime.
- Every class must be a literal so the Tailwind build can see it.

## Scoped and per instance

Order of precedence, last wins: theme defaults, `AddKlyrooUi`, enclosing `KlyTheme` scopes, `Ui`, then `Class` on the root.

```razor
@using Klyroo.UI.Theming

@* One component *@
<KlyButton Label="Save" Class="px-6" Ui="@(new ButtonUi { Label = "uppercase tracking-wide" })" />

@* A subtree: every KlyButton inside gets rounded-full *@
<KlyTheme For="typeof(KlyButton)" Ui='new ButtonUi { Base = "rounded-full" }'>
    <KlyButton Label="Rounded" />
</KlyTheme>
```

- `Class` adds classes to the root element.
- `Ui` takes the component's record (`ButtonUi`, `CardUi`, ...), one property per slot. Its type and slot names are in `get-component-metadata`.
- `KlyTheme` takes `For` and `Ui` for one component type, or `Overrides` (a dictionary of type to `ISlotOverrides`) for several. Themes nest and the innermost wins. `KlyApp` also takes `Overrides` for the whole app.

## Tailwind or not

- Precompiled (no Tailwind in the app): `Class` and `Ui` can only use classes that already exist in `klyroo.css` or in your own CSS. Prefer tokens and `AddKlyrooUi`.
- Tailwind v4 compiled by the app: import `obj/klyroo/klyroo.theme.css` after `@import "tailwindcss";` and load only your stylesheet. `Class` and `Ui` then accept any utility.
