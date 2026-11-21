# Blux.UI.Tools.Query

The Blazor-free half of [Blux.UI.Tools](https://www.nuget.org/packages/Blux.UI.Tools): the request, filter and result
records that a data list sends to a provider, the schema that says what a row type allows, the URL form of a list's
state, and an in-memory executor. It has no ASP.NET Core and no Blux.UI dependency, so an API server can reference it to
answer the same requests the components make.

## The schema

A schema is written in C# with expressions (no strings to mistype, trim-safe):

```csharp
var schema = DataListSchema.For<Employee>(s => s
    .RowKey(e => e.Id)
    .RowLabel(e => e.Name)
    .Search(e => e.Name, e => e.Email)
    .Field(e => e.Age, f => f.Sortable().Filter(DataListFilter.Number(min: 0, step: 1)))
    .Field(e => e.Department.Name, f => f.Filter(DataListFilter.Option()))      // key: "department.name"
    .Field(e => e.Status, f => f.Sortable().Filter(DataListFilter.Option()))
    .QueryFilter("archived", DataListFilter.Boolean(false))                     // for the provider only
    .DefaultSort(e => e.Name)
    .MaxPageSize(500));
```

Collection-valued fields use `.Collection(e => e.Tags, t => t.Name, f => f.Filter(DataListFilter.Option()))` (key
`tags.name`, `.SearchAny(e => e.Tags, t => t.Name)` for search). A row matches when at least one element does and the
predicate is `collection.Any(x => ...)`. `isNot` means no element is any of the values (a row without elements matches),
`isEmpty` means no non-empty element (an empty or null collection included), facets count a row once per distinct value, and a
collection cannot be sorted.

Keys are the member path in camelCase unless `f.Key("...")` says otherwise. Each filter kind allows a set of operators
(text: `contains` `is` `isNot`; option: `isAnyOf` `is` `isNot`; boolean: `is` `isNot`; number: `is` `isNot` `gt` `gte`
`lt` `lte` `between`; date: `is` `isNot` `before` `after` `between`) and has a default operator. `WithOperators` narrows
the set, or adds `isEmpty` and `isNotEmpty`. `schema.Sanitize(request)` is what a server should call on a request it
received: it drops unknown keys, disallowed operators and malformed values, and clamps the page size.

## The request

`DataListRequest(Paging, Sort, Filter, Search, Facets)` with `DataListPaging.Offset`, `.Cursor` or `.None`; a filter tree
of `FilterRule(Key, Operator, Value, IncludeEmpty)` and `DataListFilterGroup`; values are `FilterValue.Text`, `.Number`,
`.Bool`, `.Date`, `.Moment`, `.List` and `.Range`. `IncludeEmpty` also matches rows where the field is empty. The answer
is a `DataListResult<T>(Items, TotalCount, NextCursor, Facets)`. `DataListJsonContext` is the System.Text.Json
source-generated context of these records; declare `DataListResult<YourRow>` in a context of your own.

## The URL form

`DataListStateQuery.ToQuery(state, schema, prefix)` and `FromQuery(pairs, schema, prefix)` convert a `DataListState` to and
from query keys: `q`, `p`, `ps`, `s=name|-age`, `f.<key>`, `f.<key>.<operator>`, `c`, `l`, under an optional prefix
(`users.q`). A comma-separated value, `%2C` `%7E` `%25` for the characters `,` `~` `%`, a bare `~` for "include empty",
`from..to` for a range. Defaults are omitted, and bad input is dropped, never thrown.

## In memory

```csharp
DataListResult<Employee> page = schema.ExecuteInMemory(employees, request);
DataListProvider<Employee> provider = schema.InMemoryProvider(employees);
```

Search, the filter tree, a stable multi-key sort (nulls last, natural order for text), offset or keyset cursor paging,
the total and facet counts. Text comparisons ignore case and use the invariant culture. Filters are built as
`Expression<Func<T, bool>>` so a later `IQueryable` applier shares their definition.
