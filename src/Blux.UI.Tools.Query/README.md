# Blux.UI.Tools.Query

The Blazor-free half of [Blux.UI.Tools](https://www.nuget.org/packages/Blux.UI.Tools): the request, filter and result
records that a data list sends to a provider, the schema that says what a row type allows, the URL form of a list's
state, and an in-memory executor. It has no ASP.NET Core and no Blux.UI dependency, so an API server can reference it to
answer the same requests the components make.

It also holds the URL codecs of the URL state of Blux.UI.Tools (`IQueryCodec<T>` and `QueryCodecs`: numbers, dates, enums, lists, ranges), so an API server can parse the query strings the components write.

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

## Semantics worth knowing

- **Text.** `contains` and search ignore case and accents in memory (invariant culture, `CompareInfo` with `IgnoreCase | IgnoreNonSpace`). On a
  database the future `IQueryable` applier emits plain `Contains` and the collation of the column decides, by design. `is` and `isNot` are exact.
- **Dates are half-open ranges** in the user's time zone. The client sends its IANA id with the request (`DataListRequest.TimeZone`,
  `DataListState.ToRequest(timeZone: ...)`); it is never in the URL. A day on a `DateTimeOffset` or `DateTime` field is that local day (23 or 25
  hours on a daylight saving change); `DateOnly` fields compare the day. `Hour`, `Minute` and `Second` granularity match the whole unit: `is` covers
  `[start, start + unit)`, `before` and `after` use the unit bounds, `between` includes both end units. `DateTime` fields are read as UTC whatever
  their `Kind`. An unknown zone id is cleared by `Sanitize` and means UTC. IANA ids need ICU or tz data: they work with `TimeZoneInfo` on Linux and
  macOS and on Windows with ICU (.NET 6 and later); Blazor WebAssembly ships tz data unless `BlazorEnableTimeZoneSupport` is false.
- **Cursor paging** is keyset (the last row's sort values and key). A cursor that does not decode, or that was made for another sort, returns an
  empty page and no next cursor, so an infinite scroll stops instead of looping on the first page.
- **Hostile input.** `Sanitize` drops rules without a key and null nodes, keeps at most `MaxFilterRules` rules (100), `MaxListValues` values
  per list (500) and 16 levels of groups; set the first two with `.MaxFilterRules(n)` and `.MaxListValues(n)`.
- **JSON.** Enums are camelCase strings like the URL names; integers are rejected.
- **Raw expressions** (for a database) are parameterized through closure-like holders, use `array.Contains(x)` for lists, and contain no `ToLower`.
  They are not run against EF Core in this package. Building them needs a runtime that can create generic instantiations (CoreCLR, Mono); the
  library is marked AOT compatible and the few unavoidable reflection points carry justified suppressions.
