# Blux.UI.Tools

Data-heavy components for Blazor, built on [Blux.UI](https://www.nuget.org/packages/Blux.UI): the data list with URL
state, and more to come. It follows the same rules as Blux.UI: every component works in static server rendering,
Interactive Server, WebAssembly and Auto, in 12 languages, and is themed through a `ComponentTheme` you can override.

```csharp
// Server Program.cs and the client Program.cs, with the same configuration. It registers Blux.UI too.
builder.Services.AddBluxUiTools(t => t.DataListEmpty.Slot("root", "p-8"));
```

Stylesheet:

- Without Tailwind in the app, link `_content/Blux.UI.Tools/blux-tools.css` after `blux.css`.
- With Tailwind in the app, import `obj/blux/blux-tools.theme.css` (copied by the package) after `obj/blux/blux.theme.css`.

Texts are in `BxToolsMessages`, one table per locale of Blux.UI (`BluxToolsOptions.Messages` overrides a table).
`Blux.UI.Tools.Query` holds the Blazor-free request and filter models, for API servers.
