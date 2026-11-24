using Blux.UI.Tools;
using Blux.UI.Tools.Query;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildQuery() => new(
        "query", "Lux.Query", "#ff4154",
        [
            new("", "Introduction", "Getting Started",
                "Strongly typed, NativeAOT-compatible query AST, reversible URL search parameter codecs, InMemory & IQueryable evaluators, Entity Framework Core async SQL execution, and reactive Blazor / .NET MAUI QueryState.",
                [
                    new("overview", "What is Lux.Query?",
                        Paragraphs:
                        [
                            "Lux.Query decouples data querying from both UI frameworks and persistence engines. You declare the queryable surface of a model once using DataListSchema.For<T>(), then evaluate immutable DataListRequest instances consistently against in-memory arrays, LINQ IQueryable sources, or SQL databases through Entity Framework Core.",
                            "Every filter node, sort key, search term, and pagination cursor is modeled as a closed, polymorphic, NativeAOT-safe C# hierarchy with deterministic URL codecs. Bookmarking or sharing a URL reproduces the exact query state across Static SSR, Interactive Server, WebAssembly, and .NET MAUI Blazor Hybrid."
                        ],
                        Bullets:
                        [
                            "Lux.Query.Core — 100% headless C#, zero UI dependencies, <IsAotCompatible>true</IsAotCompatible>, <IsTrimmable>true</IsTrimmable>. Contains DataListSchema, DataListRequest, DataListFilterGroup, FilterRule, FilterValue, QueryCodecs, InMemoryDataList, and QueryableDataList.",
                            "Lux.Query — Multiplatform Razor Class Library (browser, ios, android, maccatalyst, windows). Provides QueryState, QueryGroup, QueryValue<T>, QueryClaim, and AddLuxQuery().",
                            "Lux.Query.EntityFrameworkCore — Optional server-only adapter translating DataListRequest into async SQL via EfAsyncQueryExecutor."
                        ],
                        LiveDemoKey: "query-inmemory",
                        LiveDemoTitle: "Lux.Query · Live DataListSchema.ExecuteInMemory"),
                    new("packages", "Package & Target Matrix",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Query.Core", "DataListSchema, DataListRequest, FilterRule, FilterValue, QueryCodecs, InMemoryDataList, QueryableDataList, DataListJsonContext", "NativeAOT · Any .NET 10 host"),
                            new("Lux.Query", "QueryState, QueryGroup, QueryValue<T>, QueryClaim, QueryHistory, AddLuxQuery()", "browser, ios, android, maccatalyst, windows"),
                            new("Lux.Query.EntityFrameworkCore", "EfAsyncQueryExecutor", "EF Core 10 (SQLite, PostgreSQL, SQL Server)")
                        ]),
                    new("guards", "Built-In Safety Guardrails",
                        Paragraphs: ["Schemas enforce defensive limits at query evaluation time so untrusted URL parameters or API payloads cannot cause excessive memory usage or unbounded SQL IN clauses."],
                        TableHeaders: ["Schema Limit", "Default", "Protects Against"],
                        TableRows:
                        [
                            new("MaxPageSize", "500", "Oversized page requests (Take is clamped safely)"),
                            new("MaxFilterRules", "100", "Deeply nested or abusive filter ASTs"),
                            new("MaxListValues", "500", "Unbounded IsAnyOf value lists in SQL IN (...) clauses"),
                            new("MaxFacetBuckets", "1000", "High-cardinality facet explosions on free-text fields")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Add the headless core package, the reactive URL state package, and optionally the Entity Framework Core async executor.",
                [
                    new("nuget", "1. Install NuGet Packages",
                        Paragraphs: ["Keep Lux.Query.EntityFrameworkCore strictly on your server or API project so WebAssembly and .NET MAUI client binaries remain lightweight."],
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Query.Core                # Headless NativeAOT schema, AST & codecs
                                dotnet add package Lux.Query                     # Blazor Web & .NET MAUI QueryState
                                dotnet add package Lux.Query.EntityFrameworkCore # Server-only EF Core async SQL executor
                                """)
                        ]),
                    new("di", "2. Register Services",
                        Paragraphs: ["Call AddLuxQuery() in Program.cs (both Server and Client projects when using Blazor Interactive Auto) or MauiProgram.cs."],
                        CodeSamples:
                        [
                            new("Program.cs (Client / MAUI)", """
                                builder.Services.AddLuxQuery();
                                """),
                            new("Program.cs (Server with EF Core)", """
                                builder.Services.AddLuxQuery();
                                builder.Services.AddSingleton<IDataListQueryExecutor, EfAsyncQueryExecutor>();
                                """)
                        ]),
                    new("usings", "3. Import Namespaces",
                        CodeSamples:
                        [
                            new("_Imports.razor / GlobalUsings.cs", """
                                using Blux.UI.Tools;
                                using Blux.UI.Tools.Query;
                                """)
                        ]),
                    new("json", "4. NativeAOT JSON Serialization",
                        Paragraphs: ["Lux.Query.Core ships a source-generated System.Text.Json context (DataListJsonContext) covering DataListRequest, DataListFilterNode, FilterRule, DataListFilterGroup, and every FilterValue subtype for zero-reflection HTTP APIs."],
                        CodeSamples:
                        [
                            new("JsonAot.cs", """
                                var json = JsonSerializer.Serialize(request, DataListJsonContext.Default.DataListRequest);
                                var parsed = JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListRequest);
                                """)
                        ])
                ]),
            new("schema", "Typed Schema (DataListSchema)", "Core Engine",
                "Declare row keys, labels, search fields, scalar fields, nested paths, collection predicates, query-only filters, and limits with compile-time safety.",
                [
                    new("builder", "Fluent DataListSchemaBuilder<T>",
                        Paragraphs:
                        [
                            "DataListSchema.For<T>() compiles fast property accessors (FieldAccessor) and expression trees once at startup. Every field key is derived automatically in camelCase from the member path (e.g., e => e.Department.Name becomes \"department.name\") unless overridden via .Key(\"...\").",
                            "Store the resulting DataListSchema<T> in a static readonly field — it is immutable and completely thread-safe."
                        ],
                        CodeSamples:
                        [
                            new("EmployeeSchema.cs", """
                                using Blux.UI.Tools.Query;

                                public sealed record Tag(string Name);
                                public sealed record Department(string Name);
                                public sealed record Employee(
                                    int Id,
                                    string Name,
                                    string Email,
                                    int Age,
                                    decimal Salary,
                                    DateTimeOffset HiredAt,
                                    bool Active,
                                    Department Department,
                                    IReadOnlyList<Tag> Tags);

                                public static class EmployeeSchemas
                                {
                                    public static readonly DataListSchema<Employee> Directory = DataListSchema.For<Employee>(s => s
                                        .RowKey(e => e.Id)
                                        .RowLabel(e => e.Name)
                                        .Search(e => e.Name, e => e.Email)
                                        .Field(e => e.Age, f => f.Sortable().Filter(DataListFilter.Number(min: 18, max: 120, step: 1)))
                                        .Field(e => e.Salary, f => f.Key("salary").Sortable().Filter(DataListFilter.Number(min: 0)))
                                        .Field(e => e.HiredAt, f => f.Key("hired").Sortable().Filter(DataListFilter.Date()))
                                        .Field(e => e.Active, f => f.Key("active").Filter(DataListFilter.Boolean()))
                                        .Field(e => e.Department.Name, f => f.Key("dept").Sortable().Filter(DataListFilter.Option()))
                                        .Collection(e => e.Tags, t => t.Name, f => f.Key("tag").Filter(DataListFilter.Option()))
                                        .QueryFilter("archived", DataListFilter.Boolean())
                                        .DefaultSort(e => e.Name)
                                        .DefaultPageSize(25)
                                        .MaxPageSize(200));
                                }
                                """)
                        ]),
                    new("builder-methods", "Complete Schema Builder Methods",
                        TableHeaders: ["Method", "Purpose", "Notes"],
                        TableRows:
                        [
                            new("RowKey(expr)", "Stable unique identity of each row", "Used for selection keys and deterministic cursor tie-breaking"),
                            new("RowLabel(func)", "Human-readable row label", "Used for accessible row labels (e.g. 'Select Alice')"),
                            new("Search(params expr[])", "Full-text search string fields", "Case-insensitive substring match across any listed field (OR semantics)"),
                            new("Field(expr, cfg)", "Scalar or nested navigation property", "Supports .Key(), .Sortable(), and .Filter(DataListFilter.*)"),
                            new("Collection(col, elem, cfg)", "One-to-many collection predicate", "Translates to collection.Any(...) in LINQ/SQL; collection fields cannot be sorted"),
                            new("QueryFilter(key, filter)", "Virtual query-only filter", "Validated and URL-encoded without mapping directly to one entity property"),
                            new("DefaultSort / MaxPageSize", "Default ordering and guardrail caps", "Configures DefaultPageSize, MaxPageSize, MaxFilterRules, MaxListValues, MaxFacetBuckets")
                        ]),
                    new("facets", "Filter Kinds, Facet Modes & Display Hints",
                        TableHeaders: ["Enum", "Exact Values", "Behavior"],
                        TableRows:
                        [
                            new("FilterKind", "Text, Option, Boolean, Number, Date", "Controls valid FilterOperator set, value parsing, and URL codec formatting"),
                            new("FacetMode", "ExcludeSelf, IncludeSelf", "ExcludeSelf computes facet counts ignoring the field's own active selection (multi-select UX)"),
                            new("DataListOptionSort", "Count, Alpha", "Orders facet option buckets by match count descending or alphabetically"),
                            new("DataListNumberDisplay", "Input, Slider, InputAndSlider", "Rendering hint for numeric range controls")
                        ])
                ]),
            new("filters", "Filter AST & Operators", "Core Engine",
                "Build arbitrarily nested AND/OR filter trees using DataListFilterGroup, FilterRule, and all 7 FilterValue subtypes across 13 operators.",
                [
                    new("operators", "All 13 FilterOperator Values",
                        TableHeaders: ["Operator", "Supported Kinds", "Semantics"],
                        TableRows:
                        [
                            new("Contains", "Text", "Case-insensitive substring match"),
                            new("Is", "Text, Option, Boolean, Number, Date", "Exact equality (or full calendar day/hour/minute/second window for dates)"),
                            new("IsAnyOf", "Option, Text, Number", "Matches when the field equals any item in FilterValue.List"),
                            new("IsNot", "Text, Option, Boolean, Number, Date", "Negated equality (for collections: no element matches)"),
                            new("Gt / Gte", "Number, Date", "Strictly greater than / greater than or equal"),
                            new("Lt / Lte", "Number, Date", "Strictly less than / less than or equal"),
                            new("Between", "Number, Date", "Inclusive range via FilterValue.Range(From, To); either bound may be null"),
                            new("Before / After", "Date", "Temporal comparison aligned to DateGranularity"),
                            new("IsEmpty / IsNotEmpty", "All nullable / text / collection", "Matches null, empty string, or empty collection (Value is null)")
                        ]),
                    new("filter-values", "All 7 FilterValue Record Subtypes",
                        TableHeaders: ["FilterValue Subtype", "Constructor", "Semantics"],
                        TableRows:
                        [
                            new("FilterValue.Text", "new FilterValue.Text(string)", "Text operand for Contains / Is / IsNot"),
                            new("FilterValue.Number", "new FilterValue.Number(decimal)", "Exact decimal representation so int, long, double, and decimal compare without float drift"),
                            new("FilterValue.Boolean", "new FilterValue.Boolean(bool)", "True / False boolean operand"),
                            new("FilterValue.Date", "new FilterValue.Date(DateOnly)", "Calendar day cut in Request.TimeZone (half-open [start, nextDayStart) for DateTimeOffset/DateTime)"),
                            new("FilterValue.Moment", "new FilterValue.Moment(DateTimeOffset)", "Instant matched at DateGranularity (Day, Hour, Minute, Second)"),
                            new("FilterValue.List", "new FilterValue.List(IReadOnlyList<string>)", "Option list coerced to the target field type (enum, int, Guid, string, DateOnly)"),
                            new("FilterValue.Range", "new FilterValue.Range(FilterValue? From, FilterValue? To)", "Inclusive range whose bounds are Number, Date, or Moment")
                        ]),
                    new("composition", "Composing Nested AND/OR Groups & IncludeEmpty",
                        Paragraphs:
                        [
                            "Use DataListFilterGroup.And(...) and DataListFilterGroup.Or(...) to combine FilterRule and nested DataListFilterGroup nodes.",
                            "Set IncludeEmpty: true on any FilterRule to match rows matching the rule OR rows where the field is null/empty (e.g., 'Department is Engineering OR unassigned')."
                        ],
                        CodeSamples:
                        [
                            new("NestedFilterTree.cs", """
                                var filter = DataListFilterGroup.And(
                                    new FilterRule("active", FilterOperator.Is, new FilterValue.Boolean(true)),
                                    new FilterRule("salary", FilterOperator.Between,
                                        new FilterValue.Range(new FilterValue.Number(80000m), new FilterValue.Number(150000m))),
                                    DataListFilterGroup.Or(
                                        new FilterRule("dept", FilterOperator.IsAnyOf, new FilterValue.List(["Engineering", "Product"]), IncludeEmpty: true),
                                        new FilterRule("hired", FilterOperator.After, new FilterValue.Date(new DateOnly(2025, 1, 1)))));
                                """)
                        ],
                        Callout: "Unknown filter keys or operators incompatible with the declared FilterKind are safely ignored during evaluation instead of throwing runtime reflection exceptions.")
                ]),
            new("paging-and-sorting", "Sorting, Paging & Cursors", "Core Engine",
                "Combine offset pagination, keyset cursor pagination, multi-column sorting, and time-zone aware requests.",
                [
                    new("paging", "Offset vs Keyset Cursor Paging",
                        Paragraphs:
                        [
                            "DataListPaging supports two immutable strategies: DataListPaging.Offset(Skip, Take) for numbered page bars, and DataListPaging.Cursor(After, Take) for infinite scroll and high-volume SQL tables where OFFSET slows down linearly.",
                            "DataListCursor automatically encodes the active sort column values plus the RowKey tie-breaker into a compact URL-safe cursor token so concurrent writes never skip or duplicate rows between pages."
                        ],
                        TableHeaders: ["Paging Type", "Signature", "Best For"],
                        TableRows:
                        [
                            new("DataListPaging.Offset", "new DataListPaging.Offset(int Skip, int Take)", "Admin tables with known TotalCount and random page navigation"),
                            new("DataListPaging.Cursor", "new DataListPaging.Cursor(string? After, int Take)", "Infinite scroll, mobile feeds, and large SQL tables")
                        ]),
                    new("request", "Multi-Column Sorting & TimeZone",
                        Paragraphs: ["Pass an IANA TimeZone identifier (e.g. \"Europe/Paris\" or \"America/New_York\") on DataListRequest so FilterValue.Date calendar days cut at the user's local midnight instead of UTC."],
                        CodeSamples:
                        [
                            new("RequestExample.cs", """
                                var request = new DataListRequest(
                                    Paging: new DataListPaging.Offset(Skip: 0, Take: 25),
                                    Sorts:
                                    [
                                        new DataListSort("dept", Descending: false),
                                        new DataListSort("salary", Descending: true)
                                    ],
                                    Filter: filter,
                                    Search: new DataListSearch("alice"))
                                {
                                    TimeZone = "Europe/Paris"
                                };
                                """)
                        ])
                ]),
            new("codecs", "URL Codecs", "Core Engine",
                "Round-trip complete DataListState objects and complex filter trees through clean, deterministic URL search parameters.",
                [
                    new("query-codecs", "IQueryCodec & Built-In QueryCodecs",
                        Paragraphs:
                        [
                            "IQueryCodec<T> defines deterministic string encoding/decoding for query parameters. QueryCodecs provides NativeAOT-compatible codecs for strings, integers, decimals, booleans, DateOnly, DateTimeOffset, enums, and comma-delimited lists.",
                            "DataListStateQuery encodes and decodes search text, multi-column sorts, offset/cursor paging, active layout, and FilterRuleText expressions in a single reversible format."
                        ],
                        CodeSamples:
                        [
                            new("CodecsExample.cs", """
                                // Serialize DataListState into URL query pairs
                                var pairs = DataListStateQuery.Write(state, EmployeeSchemas.Directory);

                                // Reconstruct exact DataListState from incoming query pairs
                                DataListState restored = DataListStateQuery.Read(pairs, EmployeeSchemas.Directory);
                                """)
                        ]),
                    new("custom-codec", "Implementing a Custom IQueryCodec<T>",
                        CodeSamples:
                        [
                            new("SlugCodec.cs", """
                                public sealed class LowercaseSlugCodec : IQueryCodec<string>
                                {
                                    public string? Encode(string value) =>
                                        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

                                    public bool TryDecode(string? raw, out string value)
                                    {
                                        value = raw?.Trim().ToLowerInvariant() ?? "";
                                        return value.Length > 0;
                                    }
                                }
                                """)
                        ])
                ]),
            new("executors", "InMemory & IQueryable", "Executors & State",
                "Evaluate DataListRequest identically in memory or over any LINQ IQueryable provider.",
                [
                    new("inmemory", "InMemoryDataList Evaluation",
                        Paragraphs: ["schema.ExecuteInMemory(items, request) evaluates full-text search, nested FilterRule trees, multi-column ValueComparers, TotalCount, and Offset/Cursor slicing in one deterministic pass — ideal for local collections, WASM caches, and unit tests."],
                        LiveDemoKey: "query-inmemory",
                        LiveDemoTitle: "Interactive ExecuteInMemory Playground",
                        CodeSamples:
                        [
                            new("InMemory.cs", """
                                DataListResult<Employee> page = EmployeeSchemas.Directory.ExecuteInMemory(employees, request);
                                // page.Items, page.TotalCount, page.NextCursor, page.HasMore
                                """)
                        ]),
                    new("iqueryable", "QueryableDataList Expression Builder",
                        Paragraphs: ["schema.Apply(queryable, request) builds pure System.Linq.Expressions trees (FilterExpressions + OrderBy/ThenBy + Skip/Take or keyset Where) so database providers translate the entire query into SQL without client evaluation."],
                        CodeSamples:
                        [
                            new("Queryable.cs", """
                                IQueryable<Employee> query = EmployeeSchemas.Directory.Apply(db.Employees.AsNoTracking(), request);
                                """)
                        ])
                ]),
            new("ef-core", "Entity Framework Core", "Executors & State",
                "Execute DataListRequest asynchronously against SQLite, PostgreSQL, or SQL Server with EfAsyncQueryExecutor.",
                [
                    new("ef-setup", "Async SQL Translation & Index Friendliness",
                        Paragraphs:
                        [
                            "Lux.Query.EntityFrameworkCore provides EfAsyncQueryExecutor, executing filtered counts (CountAsync) and paged queries (ToListAsync) asynchronously with CancellationToken support.",
                            "Calendar day filters on DateTime / DateTimeOffset compile to half-open range predicates (x.CreatedAt >= utcStart && x.CreatedAt < utcEnd) rather than calling DatePart SQL functions, preserving B-Tree index seeks."
                        ],
                        CodeSamples:
                        [
                            new("ServerProgram.cs", """
                                builder.Services.AddLuxQuery();
                                builder.Services.AddSingleton<IDataListQueryExecutor, EfAsyncQueryExecutor>();
                                """),
                            new("Endpoint.cs", """
                                app.MapPost("/api/employees/query", async (
                                    DataListRequest req,
                                    AppDbContext db,
                                    IDataListQueryExecutor executor,
                                    CancellationToken ct) =>
                                {
                                    var result = await EmployeeSchemas.Directory.ExecuteAsync(
                                        db.Employees.AsNoTracking(), req, executor, ct);
                                    return Results.Ok(result);
                                });
                                """)
                        ],
                        Callout: "Always call .AsNoTracking() on read-only EF Core queries to avoid change-tracker allocation overhead.")
                ]),
            new("query-state", "Reactive QueryState", "Executors & State",
                "Synchronize multiple components with the URL query string using prefix claims, batching, and history mode control.",
                [
                    new("methods", "Complete QueryState API",
                        TableHeaders: ["Member", "Signature", "Behavior"],
                        TableRows:
                        [
                            new("Get", "string? Get(string key)", "Reads current decoded value for a single query parameter"),
                            new("Set", "bool Set(string key, string? value, QueryHistory history = Replace)", "Updates or removes (null) a parameter; returns false if unchanged"),
                            new("SetMany", "bool SetMany(IEnumerable<KeyValuePair<string, string?>>, QueryHistory)", "Applies multiple parameter updates in one atomic navigation"),
                            new("Subscribe", "IDisposable Subscribe(string prefix, Action<IReadOnlyList<string>>)", "Notifies only when keys matching the given prefix change"),
                            new("Claim", "QueryClaim Claim(string prefix, string owner)", "Reserves a prefix so two lists on the same page detect key collisions via ThrowIfConflicted()"),
                            new("HrefWith", "string HrefWith(params (string Key, string? Value)[])", "Builds a relative URL with proposed changes for plain <a> links (SSR-safe)"),
                            new("FlushAsync", "Task FlushAsync()", "Flushes pending batched URL updates immediately")
                        ]),
                    new("usage", "Multi-Parameter & Prefix-Isolated Usage",
                        CodeSamples:
                        [
                            new("CatalogPage.razor", """
                                @inject QueryState Query
                                @implements IDisposable

                                <a href="@Query.HrefWith(("page", "2"))">Page 2 (works in Static SSR too)</a>

                                @code {
                                    private IDisposable? _sub;
                                    private QueryClaim? _claim;

                                    protected override void OnInitialized()
                                    {
                                        _claim = Query.Claim("cat_", "CatalogList");
                                        _claim.ThrowIfConflicted();
                                        _sub = Query.Subscribe("cat_", _ => InvokeAsync(StateHasChanged));
                                    }

                                    private void ApplyFilter(string search) =>
                                        Query.SetMany([new("cat_q", search), new("cat_page", null)], QueryHistory.Replace);

                                    public void Dispose()
                                    {
                                        _sub?.Dispose();
                                        _claim?.Dispose();
                                    }
                                }
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Query", "Lux.Query.Core", "Lux.Query", "builder.Services.AddLuxQuery();",
                "var page = EmployeeSchemas.Directory.ExecuteInMemory(localEmployees, request);"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Query.Core and Lux.Query.",
                [],
                ApiTypes: [typeof(DataListRequest), typeof(DataListResult<object>), typeof(DataListSort), typeof(DataListSearch), typeof(DataListState), typeof(FilterRule), typeof(DataListFilterGroup), typeof(QueryState)])
        ]);
}
