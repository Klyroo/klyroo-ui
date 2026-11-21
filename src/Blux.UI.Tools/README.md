# Blux.UI.Tools

Data-heavy components for Blazor, built on [Blux.UI](https://www.nuget.org/packages/Blux.UI): the data list with URL
state, and more to come. It follows the same rules as Blux.UI: every component works in static server rendering,
Interactive Server, WebAssembly and Auto, in 12 languages, and is themed through a `ComponentTheme` you can override.

```csharp
// Server Program.cs and the client Program.cs, with the same configuration. It registers Blux.UI too.
builder.Services.AddBluxUiTools(t => t.DataListEmpty.Slot("root", "p-8"));
```

```razor
<BxDataList TItem="Invoice" Schema="InvoiceSchema" Provider="LoadAsync" UrlKey="invoices" ResultJson="AppJson.Default.DataListResultInvoice">
    <BxDataListSearch />
    <BxDataListFilterTags ShowAdd="true" ShowClear="true" />
    <BxDataListResultCount />
    <BxDataListErrorState />
    <BxDataListTable Caption="Invoices">
        <BxDataListColumn Field="customer" />
        <BxDataListColumn Field="amount" Align="DataListAlign.End" Format="C2" />
    </BxDataListTable>
    <BxDataListPagination AriaLabel="Invoices pagination" />
</BxDataList>
```

`BxDataList` sorts and pages in memory (`Items`) or asks a provider; its state (search, sort, page, page size) lives in the URL, as links and GET forms in static rendering and as buttons otherwise. `BxDataListSearch` needs a schema with `Search(...)` fields; typing replaces the history entry instead of adding one.

A list takes one source: `Items` (in memory), a `Provider`, or a `Queryable` (an EF Core query, with a schema). A queryable runs through an `IDataListQueryExecutor`; the default one blocks the thread,
so add [Blux.UI.Tools.EntityFrameworkCore](https://www.nuget.org/packages/Blux.UI.Tools.EntityFrameworkCore) and `services.AddBluxUiToolsEntityFrameworkAdapter()` for `CountAsync` and `ToListAsync`.

A static render or a prerender cuts the days of the date filters in the time zone the browser left in a cookie. Reading it needs the request, which only a server has, so add
[Blux.UI.Server](https://www.nuget.org/packages/Blux.UI.Server) to the server project and call `services.AddBluxServer()`. Without it those renders use UTC (and Development logs one warning).

Styles: this package has **no precompiled stylesheet** (a second sheet loaded after `blux.css` would repeat base utilities in its own layer and override the
responsive and dark variants of Blux.UI), so the app must compile Tailwind.

- With `<BluxTailwind>true</BluxTailwind>` (Tailwind at build time, no Node) there is nothing to add: the build imports the classes of this package.
- With your own Tailwind, import `obj/blux/blux-tools.theme.css` (copied by the package) after `obj/blux/blux.theme.css`, and set
  `<BluxToolsOwnTailwind>true</BluxToolsOwnTailwind>` in the project. Without either, the build warns with `BLUXTOOLS001`.

The package depends on exactly the version of Blux.UI (and of Blux.UI.Tools.Query) it was built with: keep them at the same version.

Texts are in `BxToolsMessages`, one table per locale of Blux.UI (`BluxToolsOptions.Messages` overrides a table).
`Blux.UI.Tools.Query` holds the Blazor-free request and filter models, for API servers.
