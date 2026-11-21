# Blux.UI.Tools.EntityFrameworkCore

The Entity Framework Core adapter of [Blux.UI.Tools](https://www.nuget.org/packages/Blux.UI.Tools). A data list with a `Queryable` source turns each request into
queries (`schema.Apply(...)` of Blux.UI.Tools.Query) and runs them through an `IDataListQueryExecutor`. The default executor calls `Count()` and `ToList()` and **blocks the
thread** while the database answers; this package registers one that calls `CountAsync` and `ToListAsync` with the cancellation token of the request, so a newer request
cancels the query of the stale one.

```csharp
// Server Program.cs. It registers Blux.UI.Tools too.
builder.Services.AddBluxUiToolsEntityFrameworkAdapter();
```

```razor
@inject IDbContextFactory<ShopContext> Factory

<BxDataList TItem="Product" Schema="ProductSchema" Queryable="_products" UrlKey="products">
    <BxDataListResultCount />
    <BxDataListTable Caption="Products">
        <BxDataListColumn Field="name" />
        <BxDataListColumn Field="price" Align="DataListAlign.End" Format="C2" />
    </BxDataListTable>
    <BxDataListPagination />
</BxDataList>

@code {
    private ShopContext _db = default!;
    private IQueryable<Product> _products = default!;

    // A DbContext runs one query at a time and a list asks for the count and the page one after the other: give each list its own context.
    protected override void OnInitialized()
    {
        _db = Factory.CreateDbContext();
        _products = _db.Products.AsNoTracking();
    }
}
```

Notes:

- The schema is built with `DataListSchema.For<T>(...)` and decides what a request may ask. Text case and accents follow the database collation, and SQLite needs value
  converters for `decimal` and `DateTimeOffset` (see the README of Blux.UI.Tools.Query).
- Filters that are not the user's (a tenant, soft delete) go on the query before you give it to the list: `_db.Products.Where(p => !p.Deleted)`.
- A `Queryable` over a list (`AsQueryable()`) is rejected: give the list `Items`, which runs in memory with the same semantics.
- The package depends on exactly the version of Blux.UI.Tools it was built with, and on Microsoft.EntityFrameworkCore 10.
