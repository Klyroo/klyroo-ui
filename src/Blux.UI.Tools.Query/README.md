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

Search, the filter tree, a stable multi-key sort (nulls last, natural order for text, the row key as the last tie-break compared by its own type so 2 comes before 10),
offset or keyset cursor paging, the total and facet counts. Text comparisons ignore case and use the invariant culture. The in-memory executor and the
`IQueryable` applier (next section) share the definition of every operator.

## Using the schema with EF Core

`schema.Apply(source, request)` turns a request into two `IQueryable<T>`s and `schema.FacetQueries(source, request)` into one group-by query per facet.
The package references no EF Core: it builds plain `Queryable` expressions, and you run them with `CountAsync` and `ToListAsync`. A Minimal API endpoint
that answers the same requests the data list components send:

```csharp
app.MapPost("/api/employees/query", async (DataListRequest request, AppDb db, CancellationToken ct) =>
{
    var clean = schema.Sanitize(request);                         // never trust a request: unknown keys, operators and sizes are dropped
    IQueryable<EmployeeRow> rows = db.Employees.AsNoTracking()
        .Where(e => !e.Deleted)                                    // authorization and query-only filters go here, before Apply
        .Select(e => new EmployeeRow { Id = e.Id, Name = e.Name, Age = e.Age, DepartmentName = e.Department!.Name });

    var query = schema.Apply(rows, clean);
    var page = await query.Page.ToListAsync(ct);
    int? total = query.WantsTotal ? await query.Matching.CountAsync(ct) : null;

    var facets = new List<DataListFacetResult>();
    foreach (var facet in schema.FacetQueries(rows, clean))        // one GROUP BY per facet
    {
        var buckets = await facet.Buckets.ToListAsync(ct);
        int? missing = facet.Missing is null ? null : await facet.Missing.CountAsync(ct);
        facets.Add(facet.ToResult(buckets, missing));
    }
    return schema.ToResult(clean, page, total, facets.Count == 0 ? null : facets);   // trims the cursor's extra row, makes NextCursor
});
```

What `Apply` builds: the filter tree and the search as one `Where` (values are parameters, option lists are `IN`, collection fields are `Any` subqueries, a null
along a path such as `e.Department.Name` is SQL null), then the sort with nulls last in both directions (a leading `CASE WHEN x IS NULL THEN 1 ELSE 0 END`), then the row key
(so pages never repeat or skip a row), then `Skip`/`Take` as parameters. Cursor paging adds a keyset `WHERE` over the sort values and the row key and takes one row more than the page, which
`ToResult` removes after using it to know that a next page exists. `Matching` is unordered and unpaged, for the count. A facet counts the rows that match the search and the filters
(not the facet's own filter, unless `FacetMode.IncludeSelf`), once per distinct value even when several elements of a collection carry it (`COUNT(DISTINCT key)`), so a collection facet needs a
`RowKey`. `ToResult` of a facet orders, searches and windows the buckets exactly like the in-memory executor, so every distinct value is transferred: do not offer a facet on a column with
millions of values.

Results equal those of `ExecuteInMemory` over the same rows (the EF tests run both on SQLite and compare), except where the database decides:

- **Text case and accents follow the database.** The expressions contain no `ToLower`: `is`, `isNot`, option lists, facet values and the sort follow the collation of the column
  (a case-insensitive collation merges `a` and `A` into one facet option), and `Contains` follows whatever the provider translates it to, which you must check for yours.
  **EF Core's SQLite provider translates `Contains` to `instr`, which is case sensitive whatever the collation** (the EF tests assert it), so a search for `ALICE` finds nothing there where
  the in-memory executor finds `Alice`. For a case- and accent-insensitive search on a provider whose `Contains` is case sensitive, use a column type or collation that it honors, or filter in memory.
- **Text sorts by the database order**, not the natural order of memory (`item2` before `item10`, accents ignored).
- **Enums** compare as stored: an enum stored as a number sorts by number, like in memory; one stored as text through a converter sorts by that text.
- **Ties** are broken by the row key by its type (an integer key by number, a string key by the collation). Cursor paging refuses a `Guid` key or sort field (databases order Guids their own way).

Providers must be able to compare the types of your fields: with EF Core's SQLite provider a `DateTimeOffset` cannot be compared or ordered (EF throws `could not be translated`), so declare a converter
(`configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>()`, `v => v.UtcTicks`); `decimal` needs nothing on EF Core 10 (it orders and compares it with its own collation and functions). Other providers: check the types of your fields against what the provider can compare. Nothing
is evaluated on the client: a query the provider cannot translate throws when it runs, and `Apply` and `FacetQueries` refuse an in-memory `AsQueryable()` (use `ExecuteInMemory`).

Two rules for cursor paging. The next cursor is made from the sort values of the last loaded row, so that row must carry them: sorting by `e.Department.Name` on an entity needs the
`Department` loaded (`Include`), and **the simplest and safest source is a query projected to a flat row type** (`Select(e => new EmployeeRow ...)` as above), which also keeps the SQL narrow.
And a schema without `RowKey` cannot page by cursor (`InvalidOperationException`).

## Semantics worth knowing

- **Text.** `contains` and search ignore case and accents in memory (invariant culture, `CompareInfo` with `IgnoreCase | IgnoreNonSpace`). On a
  database the `IQueryable` applier emits plain `Contains` and the database decides, by design (see above). `is` and `isNot` are exact.
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
- **Raw expressions** (for a database) are parameterized through closure-like holders, use `array.Contains(x)` for lists, and contain no `ToLower`. They are
  proven against EF Core and SQLite in `tests/Blux.UI.Tools.Query.EfTests`.
- **NativeAOT.** The in-memory executor runs in a NativeAOT image (`tests/Blux.UI.Tools.Query.AotSmoke` publishes and runs it in CI): expression trees are interpreted there, a list of
  option values is a chain of equalities, and the operators of date and decimal types are kept for the interpreter. The `IQueryable` applier builds generic `Queryable` calls over the
  types of your fields at runtime, which needs a runtime that can create generic instantiations (CoreCLR, Mono): that is where EF Core runs anyway, and the few reflection points carry
  justified suppressions. `DataListJsonContext` (the JSON wire model) is not covered by the smoke check: the ILCompiler of .NET 10.0.15 and 11.0.0 crashes on a source-generated
  polymorphic hierarchy with a nested record named `Bool` (the cursor has a context of its own so that running a request never reaches it).
