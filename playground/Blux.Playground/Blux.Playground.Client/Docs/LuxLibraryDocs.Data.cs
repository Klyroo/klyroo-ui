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
                            "Lux.Query separates query modeling from both UI frameworks and database engines. You declare the queryable surface of a record or entity once with DataListSchema.For<T>(), then execute immutable DataListRequest instances identically against in-memory arrays, LINQ IQueryable sources, or SQL databases via Entity Framework Core.",
                            "Because every filter tree, sort descriptor, and paging token is represented as a strongly typed immutable AST with built-in URL codecs, query state can round-trip cleanly between URLs, REST/Minimal APIs, WebAssembly clients, and native .NET MAUI apps."
                        ],
                        Bullets:
                        [
                            "Lux.Query.Core — 100% headless C#, zero UI dependency, <IsAotCompatible>true</IsAotCompatible>. Houses DataListSchema, DataListRequest, DataListFilterGroup, FilterOperator, QueryCodecs, InMemoryDataList, and QueryableDataList.",
                            "Lux.Query — Multiplatform Razor Class Library (browser, ios, android, maccatalyst, windows). Provides reactive URL QueryState, QueryGroup, QueryValue<T>, and AddLuxQuery().",
                            "Lux.Query.EntityFrameworkCore — Optional server adapter translating DataListRequest into async SQL via EfAsyncQueryExecutor."
                        ],
                        LiveDemoKey: "query-inmemory",
                        LiveDemoTitle: "Lux.Query · Live DataListSchema.ExecuteInMemory"),
                    new("matrix", "Package & Target Matrix",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Query.Core", "DataListSchema, DataListRequest, DataListFilterGroup, QueryCodecs, InMemoryDataList, QueryableDataList", "NativeAOT · Any .NET 10 host"),
                            new("Lux.Query", "QueryState, QueryGroup, QueryValue<T>, QueryHistory, AddLuxQuery()", "browser, ios, android, maccatalyst, windows"),
                            new("Lux.Query.EntityFrameworkCore", "EfAsyncQueryExecutor", "EF Core 10 (SQLite, Postgres, SQL Server)")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install the headless Core engine, the reactive QueryState package, and optionally the EF Core executor.",
                [
                    new("nuget", "1. Install NuGet Packages",
                        Paragraphs: ["Pick only the layers your project needs. WebAssembly clients and native C# MAUI apps should never reference Lux.Query.EntityFrameworkCore."],
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Query.Core                # Headless NativeAOT schema, AST & codecs
                                dotnet add package Lux.Query                     # Blazor Web & .NET MAUI QueryState
                                dotnet add package Lux.Query.EntityFrameworkCore # Server-only EF Core async SQL executor
                                """)
                        ]),
                    new("di", "2. Register Services",
                        Paragraphs: ["Call AddLuxQuery() in Program.cs (Server and Client when using Blazor Interactive Auto) or MauiProgram.cs."],
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
                        ])
                ]),
            new("schema", "Typed Schema (DataListSchema)", "Core Engine",
                "Declare row keys, full-text search columns, sortable fields, filterable fields, and collection accessors with compile-time safety.",
                [
                    new("builder", "Fluent Schema Builder",
                        Paragraphs:
                        [
                            "DataListSchema.For<T>() compiles strongly typed FieldAccessor delegates and Expression trees once at startup. Every field key is registered explicitly so user-supplied URL parameters or sort keys can never probe arbitrary private properties.",
                            "Keep your schema as a static readonly field — it is completely immutable and thread-safe across all requests."
                        ],
                        CodeSamples:
                        [
                            new("ProductSchema.cs", """
                                using Blux.UI.Tools.Query;

                                public sealed record Product(
                                    int Id,
                                    string Name,
                                    string Category,
                                    decimal Price,
                                    DateTimeOffset CreatedAt,
                                    bool Active,
                                    IReadOnlyList<string> Tags);

                                public static class ProductSchemas
                                {
                                    public static readonly DataListSchema<Product> Catalog = DataListSchema.For<Product>(s => s
                                        .RowKey(p => p.Id)
                                        .Search(p => p.Name, p => p.Category)
                                        .Field(p => p.Price, f => f.Key("price").Sortable().Filter(DataListFilter.Number(min: 0)))
                                        .Field(p => p.Category, f => f.Key("category").Sortable().Filter(DataListFilter.Option()))
                                        .Field(p => p.CreatedAt, f => f.Key("created").Sortable().Filter(DataListFilter.Date()))
                                        .Field(p => p.Active, f => f.Key("active").Filter(DataListFilter.Boolean())));
                                }
                                """)
                        ]),
                    new("facets", "Facet Modes & Option Sorting",
                        TableHeaders: ["Enum", "Values", "Behavior"],
                        TableRows:
                        [
                            new("FilterKind", "Text, Option, Boolean, Number, Date", "Controls valid operators, value coercion, and codec formatting"),
                            new("FacetMode", "ExcludeSelf, IncludeSelf", "ExcludeSelf computes counts ignoring the field's own active selection (multi-select UX)"),
                            new("DataListOptionSort", "Count, Alpha", "Sorts facet option counts by highest match count or alphabetically"),
                            new("DataListNumberDisplay", "Input, Slider, InputAndSlider", "Hint for numeric range filter presentation")
                        ])
                ]),
            new("filters", "Filter AST & Operators", "Core Engine",
                "Compose nested AND/OR filter trees using DataListFilterGroup, DataListFilterRule, and all 13 FilterOperator kinds.",
                [
                    new("operators", "All 13 FilterOperator Values",
                        TableHeaders: ["Operator", "Applicable Kinds", "Semantics"],
                        TableRows:
                        [
                            new("Contains", "Text", "Case-insensitive substring match"),
                            new("Is", "Text, Option, Boolean, Number, Date", "Exact equality (or calendar unit match for dates)"),
                            new("IsAnyOf", "Option, Text, Number", "Matches when the field equals any item in a multi-value set"),
                            new("IsNot", "Text, Option, Boolean, Number, Date", "Negated equality"),
                            new("Gt / Gte", "Number, Date", "Strictly greater than / greater than or equal"),
                            new("Lt / Lte", "Number, Date", "Strictly less than / less than or equal"),
                            new("Between", "Number, Date", "Inclusive lower/upper range match"),
                            new("Before / After", "Date", "Temporal comparison aligned to DateGranularity"),
                            new("IsEmpty / IsNotEmpty", "All nullable/text/collection", "Matches null, blank string, or empty collection")
                        ]),
                    new("combinators", "Nested AND / OR Trees & Date Granularity",
                        Paragraphs:
                        [
                            "Combine rules with FilterCombinator.And or FilterCombinator.Or, and nest child DataListFilterGroup instances to any depth.",
                            "For temporal fields, DateGranularity (Day, Hour, Minute, Second) and LocalInstants convert calendar dates into UTC intervals respecting the caller's IANA time zone."
                        ],
                        CodeSamples:
                        [
                            new("FilterTree.cs", """
                                var filter = DataListFilterGroup.And(
                                    new FilterRule("active", FilterOperator.Is, new FilterValue.Boolean(true)),
                                    new FilterRule("price", FilterOperator.Between, new FilterValue.Range(new FilterValue.Number(50m), new FilterValue.Number(400m))),
                                    new FilterRule("category", FilterOperator.IsAnyOf, new FilterValue.List(["Audio", "Displays"])));
                                """)
                        ],
                        Callout: "Every rule is validated against the field's declared FilterKind before execution; unknown field keys are ignored safely instead of throwing reflection errors.")
                ]),
            new("paging-and-sorting", "Sorting, Paging & Cursors", "Core Engine",
                "Support offset pagination, keyset cursor pagination, and multi-column sorting in one immutable DataListRequest.",
                [
                    new("paging", "Offset vs Keyset Cursor Paging",
                        Paragraphs:
                        [
                            "DataListPaging is a closed hierarchy with two strategies: DataListPaging.Offset(Skip, Take) for classic page numbers and DataListPaging.Cursor(After, Take) for infinite lists and high-throughput tables where SQL OFFSET degrades.",
                            "CursorValue encodes the active sort keys plus the row key into an opaque URL-safe token so concurrent inserts never cause duplicate or skipped rows."
                        ],
                        TableHeaders: ["Mode", "Constructor", "Recommended For"],
                        TableRows:
                        [
                            new("Offset", "new DataListPaging.Offset(Skip: 0, Take: 25)", "Admin tables with total count and random page jumps"),
                            new("Cursor", "new DataListPaging.Cursor(After: cursorToken, Take: 25)", "Infinite scroll, mobile feeds, and million-row tables")
                        ],
                        CodeSamples:
                        [
                            new("SortingAndPaging.cs", """
                                var request = new DataListRequest(
                                    Paging: new DataListPaging.Offset(Skip: 25, Take: 25),
                                    Sorts:
                                    [
                                        new DataListSort("category", Descending: false),
                                        new DataListSort("price", Descending: true)
                                    ],
                                    Filter: DataListFilterGroup.Empty,
                                    Search: new DataListSearch("studio"));
                                """)
                        ])
                ]),
            new("codecs", "URL Codecs", "Core Engine",
                "Serialize and deserialize complete DataListState and filter trees into clean, deterministic URL search parameters.",
                [
                    new("query-codecs", "IQueryCodec, QueryCodecs & DataListStateQuery",
                        Paragraphs:
                        [
                            "QueryCodecs provides built-in NativeAOT codecs for strings, integers, decimals, booleans, dates, enums, and sequences. DataListStateQuery builds on top of them to encode search terms, multi-sorts, page/cursor tokens, and nested filter rules into compact query pairs.",
                            "Because encoding and decoding are completely pure and deterministic, sharing a URL reproduces the exact filtered, sorted view on Server SSR, WebAssembly, and MAUI Hybrid."
                        ],
                        CodeSamples:
                        [
                            new("StateCodecs.cs", """
                                // Encode state to URL key/value pairs
                                var pairs = DataListStateQuery.Write(state, schema);

                                // Decode URL key/value pairs back to strongly typed DataListState
                                var restored = DataListStateQuery.Read(pairs, schema);
                                """)
                        ])
                ]),
            new("executors", "InMemory & IQueryable", "Executors & State",
                "Run the exact same DataListRequest against in-memory arrays or LINQ IQueryable providers.",
                [
                    new("inmemory", "InMemoryDataList Execution",
                        Paragraphs: ["schema.ExecuteInMemory(items, request) evaluates search, nested filter trees, multi-column ValueComparers, total count, and paging/cursors in a single pass."],
                        LiveDemoKey: "query-inmemory",
                        LiveDemoTitle: "Interactive ExecuteInMemory Playground",
                        CodeSamples:
                        [
                            new("InMemory.cs", """
                                DataListResult<Product> result = schema.ExecuteInMemory(products, request);
                                Console.WriteLine($"Showing {result.Items.Count} of {result.TotalCount}");
                                """)
                        ]),
                    new("iqueryable", "QueryableDataList Applier",
                        Paragraphs: ["schema.Apply(queryable, request) translates search, filters, and sorts into standard LINQ expression trees without invoking client-side evaluation."],
                        CodeSamples:
                        [
                            new("Queryable.cs", """
                                IQueryable<Product> filtered = schema.Apply(db.Products.AsNoTracking(), request);
                                """)
                        ])
                ]),
            new("ef-core", "Entity Framework Core", "Executors & State",
                "Translate DataListRequest into asynchronous SQL queries and counts with EfAsyncQueryExecutor.",
                [
                    new("ef-setup", "Server Registration & SQL Translation",
                        Paragraphs:
                        [
                            "Lux.Query.EntityFrameworkCore implements IDataListQueryExecutor using Entity Framework Core's ToListAsync and CountAsync extensions. Keeping it in a separate assembly ensures WebAssembly and MAUI binaries never pull in EF Core.",
                            "Date filters compile to range comparisons (>= start && < end) so database indexes on DateTime / DateTimeOffset columns remain usable."
                        ],
                        CodeSamples:
                        [
                            new("Program.cs (Server)", """
                                builder.Services.AddLuxQuery();
                                builder.Services.AddSingleton<IDataListQueryExecutor, EfAsyncQueryExecutor>();
                                """)
                        ],
                        Callout: "Always call .AsNoTracking() on read-only DbSet queries passed into QueryableDataList for minimal memory overhead.")
                ]),
            new("query-state", "Reactive QueryState", "Executors & State",
                "Synchronize typed UI state with the browser URL or .NET MAUI BlazorWebView navigation stack.",
                [
                    new("qs", "Reading, Writing & QueryHistory",
                        Paragraphs:
                        [
                            "QueryState wraps NavigationManager with debounced batching, value equality checks (QueryEquality), and multi-key ownership claims (QueryClaim) so independent components on the same page never overwrite each other's query parameters."
                        ],
                        TableHeaders: ["History Mode", "Effect", "Typical Use"],
                        TableRows:
                        [
                            new("QueryHistory.Replace", "Replaces current history entry (default)", "Typing in a search box or toggling filters"),
                            new("QueryHistory.Push", "Pushes a new browser Back/Forward entry", "Changing pages, tabs, or switching saved views")
                        ],
                        CodeSamples:
                        [
                            new("SearchPage.razor", """
                                @inject QueryState Query

                                @code {
                                    private string Search => Query.Get("q") ?? "";

                                    private void UpdateSearch(string value) =>
                                        Query.Set("q", string.IsNullOrWhiteSpace(value) ? null : value, QueryHistory.Replace);

                                    private void GoToPage(int page) =>
                                        Query.Set("page", page <= 1 ? null : page.ToString(), QueryHistory.Push);
                                }
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Query", "Lux.Query.Core", "Lux.Query", "builder.Services.AddLuxQuery();",
                "var page = ProductSchemas.Catalog.ExecuteInMemory(localProducts, request);"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Query.Core and Lux.Query.",
                [],
                ApiTypes: [typeof(DataListRequest), typeof(DataListResult<object>), typeof(DataListSort), typeof(DataListSearch), typeof(DataListState), typeof(QueryState)])
        ]);

    private static LibraryDocTree BuildTable() => new(
        "table", "Lux.Table", "#00d8ff",
        [
            new("", "Introduction", "Getting Started",
                "Enterprise DataList and NativeAOT headless tree hierarchy engine for Blazor Web and .NET MAUI Blazor Hybrid.",
                [
                    new("overview", "What is Lux.Table?",
                        Paragraphs:
                        [
                            "Lux.Table combines a pure headless, NativeAOT tree-hierarchy engine (Lux.Table.Core) with enterprise Blazor & .NET MAUI Blazor Hybrid table and card-grid components.",
                            "Out of the box it supports declarative columns, automatic Sort/Hide HeaderMenu, sticky column pinning, resizing, visual row grouping, hierarchical tree expansion with indentation rails, footer aggregations (Sum, Average, Min, Max, Count), bulk selection bars, saved views, 2px loading progress lines, 9 skeleton kinds, and responsive phone summary rows."
                        ],
                        LiveDemoKey: "table-basic",
                        LiveDemoTitle: "Lux.Table · Live BxDataList with HeaderMenu & Footer Summaries"),
                    new("layers", "Two-Layer Package Split",
                        TableHeaders: ["Package", "Contains", "Targets"],
                        TableRows:
                        [
                            new("Lux.Table.Core", "DataListTree<TItem>, DataListTreeNode<TItem>, DataListTreeSelectionMode", "Headless C# · NativeAOT"),
                            new("Lux.Table", "BxDataList, BxDataListTable, BxDataListGrid, BxDataListColumn, Toolbar, FilterBar, Pagination, GroupBy, Summaries, RowSummary", "Blazor Web & .NET MAUI Hybrid")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Table, configure default options, and import the namespace.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Table.Core   # Headless NativeAOT tree hierarchy models
                                dotnet add package Lux.Table        # Full Blazor Web & .NET MAUI DataList
                                """)
                        ]),
                    new("di", "2. Register Services",
                        Paragraphs: ["AddLuxTable() registers the query executor, column preference helpers, and optional BluxToolsOptions defaults."],
                        CodeSamples:
                        [
                            new("Program.cs / MauiProgram.cs", """
                                builder.Services.AddLuxTable(options =>
                                {
                                    options.DefaultPageSize = 25;
                                });
                                """)
                        ]),
                    new("static-assets", "3. Automatic JS Initializer",
                        Paragraphs: ["Lux.Table ships Lux.Table.lib.module.js automatically as a Blazor JS initializer handling column resize handles, focus preservation across virtualized refreshes, actions-column width observation, and phone viewport detection."])
                ]),
            new("data-list", "BxDataList & Layouts", "Components",
                "Compose tables, card grids, toolbars, filter bars, selection bars, and pagination inside one BxDataList<TItem> coordinator.",
                [
                    new("compose", "Declarative Composition",
                        Paragraphs: ["BxDataList<TItem> owns the active request, loading state, selection set, and optional URL synchronization. Child blocks subscribe to its cascading state so you can arrange Toolbar, FilterBar, Table/Grid, and Pagination in any layout."],
                        LiveDemoKey: "table-basic",
                        LiveDemoTitle: "BxDataList with HeaderMenu & Footer Sum",
                        CodeSamples:
                        [
                            new("EmployeesPage.razor", """
                                <BxDataList TItem="Employee" Schema="@_schema" Items="@_employees" UrlSync="true">
                                    <BxDataListToolbar TItem="Employee" />
                                    <BxDataListFilterBar TItem="Employee" />
                                    <BxDataListTable TItem="Employee"
                                                     HeaderMenu="true"
                                                     ShowProgressLine="true"
                                                     Density="DataListDensity.Default"
                                                     Summaries="@(new DataListSummaries<Employee>())">
                                        <BxDataListColumn TItem="Employee" Field="name" Title="Employee" />
                                        <BxDataListColumn TItem="Employee" Field="department" Title="Department" />
                                        <BxDataListColumn TItem="Employee" Field="salary" Title="Salary"
                                                          Align="DataListAlign.End"
                                                          Summary="DataListSummaryKind.Sum" />
                                    </BxDataListTable>
                                    <BxDataListPagination TItem="Employee" />
                                </BxDataList>
                                """)
                        ]),
                    new("options", "Density, Alignment & Selection Enums",
                        TableHeaders: ["Enum", "Values", "Purpose"],
                        TableRows:
                        [
                            new("DataListDensity", "Default, Compact, Comfortable", "Controls row padding and vertical information density"),
                            new("DataListAlign", "Start, Center, End", "Horizontal alignment for numeric, badge, and text cells"),
                            new("DataListPin", "None, Start, End", "Pins columns sticky to the leading or trailing edge"),
                            new("DataListSelectionScope", "Auto, Page, All", "Defines whether Select-All selects the current page or entire filtered query"),
                            new("DataListSelectionClear", "Never, Trigger, Success", "Controls when bulk selection clears after executing a bulk action"),
                            new("DataListSelectionBarPosition", "Sticky, Fixed", "Floating bottom bar vs sticky table header selection bar")
                        ])
                ]),
            new("columns", "Columns & HeaderMenu", "Components",
                "Configure sortable, hideable, resizable, and pinned columns with automatic HeaderMenu actions.",
                [
                    new("header-menu", "Automatic HeaderMenu",
                        Paragraphs:
                        [
                            "Enabling HeaderMenu=\"true\" on BxDataListTable turns every sortable or hideable column header into an accessible menu trigger offering Sort Ascending, Sort Descending, Clear Sort, and Hide Column.",
                            "Column widths and visibility preferences persist automatically in cookies (web) or localStorage (MAUI WebView) via Lux.Table.lib.module.js."
                        ],
                        CodeSamples:
                        [
                            new("PinnedColumns.razor", """
                                <BxDataListTable TItem="Employee" HeaderMenu="true">
                                    <BxDataListColumn TItem="Employee" Field="name" Title="Employee" Pin="DataListPin.Start" />
                                    <BxDataListColumn TItem="Employee" Field="department" Title="Department" />
                                    <BxDataListColumn TItem="Employee" Field="salary" Title="Salary" Align="DataListAlign.End" Summary="DataListSummaryKind.Sum" />
                                </BxDataListTable>
                                """)
                        ])
                ]),
            new("tree", "Tree Hierarchy (DataListTree)", "Components",
                "Render nested parent-child trees with continuation rails, keyboard navigation, and configurable selection modes.",
                [
                    new("headless-tree", "DataListTree<TItem> in Lux.Table.Core (NativeAOT)",
                        Paragraphs:
                        [
                            "DataListTree<TItem> lives in Lux.Table.Core with zero UI dependencies. BuildTree() filters null items and duplicate keys safely, computes Depth, Parent, RootIndex, IsLast, and AncestorContinuations (boolean rails for drawing crisp vertical tree connectors), and exposes FlattenVisible() for expanded rows.",
                            "In BxDataListTable, users expand/collapse branch nodes via click, Enter/Space, or ArrowRight/ArrowLeft."
                        ],
                        TableHeaders: ["Selection Mode", "Behavior", "Use Case"],
                        TableRows:
                        [
                            new("DataListTreeSelectionMode.All", "Every root and descendant row renders an independent checkbox", "File trees, permission matrices"),
                            new("DataListTreeSelectionMode.Roots", "Only top-level root rows (depth 0) display checkboxes; children reserve alignment width", "Orders with nested line items")
                        ],
                        CodeSamples:
                        [
                            new("OrgTree.cs", """
                                var tree = new DataListTree<Employee>
                                {
                                    Children = e => e.Reports,
                                    DefaultExpandedDepth = 1,
                                    Selectable = DataListTreeSelectionMode.All
                                };

                                var (roots, allNodes) = tree.BuildTree(employees, schema);
                                """)
                        ],
                        Callout: "When a tree is active on BxDataListTable, visual row grouping and summary-row phone mode are disabled automatically so hierarchy rails stay intact.")
                ]),
            new("grouping-and-summaries", "Grouping & Footer Summaries", "Components",
                "Group rows visually under collapsible headers and compute automatic footer aggregations.",
                [
                    new("grouping", "Visual Grouping (DataListGroupBy<TItem>)",
                        Paragraphs: ["DataListGroupBy<TItem> partitions loaded rows by any key selector, renders collapsible group headers with counts, and supports custom group title formatters."],
                        CodeSamples:
                        [
                            new("GroupBy.cs", """
                                var byDept = new DataListGroupBy<Employee>
                                {
                                    Key = e => e.Department
                                };
                                """)
                        ]),
                    new("summaries", "Footer Aggregations (All 6 DataListSummaryKind Values)",
                        TableHeaders: ["Kind", "Computation", "Typical Column"],
                        TableRows:
                        [
                            new("DataListSummaryKind.Sum", "Arithmetic sum of numeric values", "Revenue, Salary, Quantity"),
                            new("DataListSummaryKind.Average", "Mean across non-null numeric rows", "Score, Latency, Margin %"),
                            new("DataListSummaryKind.Count", "Count of non-empty values", "Active items, IDs"),
                            new("DataListSummaryKind.Min", "Minimum value in column", "Lowest price, earliest date"),
                            new("DataListSummaryKind.Max", "Maximum value in column", "Peak load, latest date"),
                            new("DataListSummaryKind.None", "No footer cell computation (default)", "Text or action columns")
                        ])
                ]),
            new("mobile-summary", "Mobile Summary Rows", "Components",
                "Automatically convert dense desktop tables into scannable two-line mobile rows below 768px.",
                [
                    new("row-summary", "DataListRowSummary<TItem>",
                        Paragraphs:
                        [
                            "On small screens, horizontal table scrolling hurts usability. Provide DataListRowSummary<TItem> with AutoPhone = true (default) to render each record as a compact card-like row with optional leading avatar/icon, primary title (link or button), status pill, secondary details joined with ' · ', selection checkbox, and row actions."
                        ],
                        CodeSamples:
                        [
                            new("PhoneSummary.cs", """
                                private static readonly DataListRowSummary<Employee> PhoneSummary = new()
                                {
                                    Title = e => e.Name,
                                    Details = e => [e.Department, $"€{e.Salary:N0}"],
                                    Href = e => $"/employees/{e.Id}",
                                    AutoPhone = true
                                };
                                """)
                        ])
                ]),
            new("ux-polish", "Empty States, Skeletons & Progress", "Components",
                "Deliver crisp perceived performance during first load, background refreshes, and empty results.",
                [
                    new("skeletons", "All 9 DataListSkeletonKind Cell Shapes",
                        Paragraphs: ["Every column can declare its SkeletonKind so loading placeholders match the real content geometry and prevent layout shift."],
                        TableHeaders: ["SkeletonKind", "Rendered Shape", "Best For"],
                        TableRows:
                        [
                            new("Text / Number", "Horizontal pill bar (leading or trailing aligned)", "Names, descriptions, currency amounts"),
                            new("Avatar / Icon / Dot", "Circle or square media placeholder", "User avatars, leading icons, status dots"),
                            new("Badge / Check / Progress", "Pill badge, checkbox square, or thin bar", "Status badges, booleans, completion %"),
                            new("None", "Empty placeholder cell", "Secondary actions or spacer columns")
                        ]),
                    new("empty-progress", "Progress Line & Differentiated Empty States",
                        Bullets:
                        [
                            "ShowProgressLine=\"true\" renders a 2px top progress bar during background reloads while keeping existing rows interactive.",
                            "DataListEmptyState distinguishes first-use empty datasets (showing a primary creation CTA) from filtered-empty states (showing a Reset Filters action)."
                        ])
                ]),
            MauiAndAotPage("Lux.Table", "Lux.Table.Core", "Lux.Table", "builder.Services.AddLuxTable();",
                "var (roots, allNodes) = new DataListTree<Employee> { Children = e => e.Reports }.BuildTree(employees);"),
            new("recipes", "Testing & Recipes", "Platforms",
                "Testing BxDataList and DataListTree reliably in xUnit and bUnit.",
                [
                    new("bunit", "bUnit Service Registration Order",
                        CodeSamples:
                        [
                            new("TableRepoTests.cs", """
                                Services.AddLuxTable();
                                JSInterop.Mode = JSRuntimeMode.Loose;
                                SetRendererInfo(new RendererInfo("Server", true));

                                var cut = Render<BxDataList<Employee>>(...);
                                Assert.Equal(2, cut.FindAll("tbody tr").Count);
                                Assert.NotNull(cut.Find("tfoot"));
                                """)
                        ],
                        Callout: "Always call Services.AddLuxTable() BEFORE SetRendererInfo(...) in bUnit tests; SetRendererInfo locks the service collection.")
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Table.Core and Lux.Table.",
                [],
                ApiTypes: [typeof(BxDataList<object>), typeof(BxDataListTable<object>), typeof(BxDataListColumn<object>), typeof(DataListTree<object>), typeof(DataListRowSummary<object>)])
        ]);

    private static LibraryDocTree BuildSpreadsheet() => new(
        "spreadsheet", "Lux.Spreadsheet", "#14b8a6",
        [
            new("", "Introduction", "Getting Started",
                "Headless NativeAOT CSV/TSV importer with 20-row header layout detection, NFD normalization, >=80% pass-rate column suggestions, unknown value questions, duplicate/upsert handling, inline cell edits, and batched submission with rejected-row retry.",
                [
                    new("overview", "What is Lux.Spreadsheet?",
                        Paragraphs:
                        [
                            "Real-world CSV exports from ERPs, Excel, and Google Sheets are messy: title banners sit above headers, column names contain accents or asterisks, decimal separators vary by locale, and select values don't match internal codes.",
                            "Lux.Spreadsheet puts the entire parsing, layout detection, scoring, reconciliation, and batch submission engine into 100% headless NativeAOT C# (Lux.Spreadsheet.Core), paired with a drop-in 5-step wizard (BxSpreadsheetImport) for Blazor Web and .NET MAUI Blazor Hybrid."
                        ],
                        LiveDemoKey: "spreadsheet-wizard",
                        LiveDemoTitle: "Lux.Spreadsheet · Live 5-Step Import Wizard"),
                    new("steps", "5-Step Pipeline Summary",
                        TableHeaders: ["Step", "Core Capability", "Key Properties"],
                        TableRows:
                        [
                            new("1. Source & Layout", "Delimiter & 20-row header auto-detection", "LoadText, Paste, HeaderRowNumber"),
                            new("2. Column Mapping", "NFD normalization + >=80% sample suggestions", "MissingFields, Suggestion, AssignColumn"),
                            new("3. Unknown Values", "Interactive resolution of unrecognized select values", "OpenQuestions, AnswerValue"),
                            new("4. Review & Reconcile", "Duplicates, ExistingRecords upsert & inline EditCell", "AllRows, ImportableRows, EditCell"),
                            new("5. Batched Submit", "Chunked async submission & retry rejected rows", "SubmitAsync, RejectedRows, RetryRejectedAsync")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Spreadsheet and register services.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Spreadsheet.Core   # Headless NativeAOT import engine
                                dotnet add package Lux.Spreadsheet        # 5-step Blazor & .NET MAUI wizard
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxSpreadsheet();")])
                ]),
            new("schema", "Schema, Columns & Rules", "Import Pipeline",
                "Define typed columns, locale decimal separators, built-in rules, option aliases, and reconciliation policies.",
                [
                    new("kinds", "All 5 SpreadsheetColumnKind Values",
                        TableHeaders: ["Kind", "Parsing Behavior", "Built-In Rules"],
                        TableRows:
                        [
                            new("Text", "Trimmed string value", "SpreadsheetRule.Email(), MinLength, MaxLength, Pattern"),
                            new("Number", "Respects custom DecimalSeparator ('.' or ',')", "SpreadsheetRule.Min(n), Max(n)"),
                            new("Date", "ISO & locale date parsing", "Date bounds & required checks"),
                            new("Boolean", "Parses true/false, yes/no, 1/0", "Required checks"),
                            new("Select", "Matches Value, Label, or any Alias case/accent-insensitively", "SpreadsheetOption + UnknownPolicy")
                        ]),
                    new("example", "Complete SpreadsheetSchema Example",
                        CodeSamples:
                        [
                            new("RatesSchema.cs", """
                                var schema = new SpreadsheetSchema
                                {
                                    Key = "rates",
                                    RowKeyField = "code",
                                    Duplicates = SpreadsheetDuplicatesPolicy.Error,
                                    ExistingAction = SpreadsheetExistingAction.Update,
                                    Columns =
                                    [
                                        new SpreadsheetColumnSchema { Key = "code", Label = "Code", Required = true },
                                        new SpreadsheetColumnSchema
                                        {
                                            Key = "price",
                                            Label = "Unit Price",
                                            Kind = SpreadsheetColumnKind.Number,
                                            DecimalSeparator = ',',
                                            Required = true,
                                            Rules = [SpreadsheetRule.Min(0)]
                                        },
                                        new SpreadsheetColumnSchema
                                        {
                                            Key = "currency",
                                            Label = "Currency",
                                            Kind = SpreadsheetColumnKind.Select,
                                            Required = true,
                                            UnknownPolicy = SpreadsheetUnknownValuePolicy.Ask,
                                            Options =
                                            [
                                                new SpreadsheetOption("EUR", "Euro", ["€", "EUR"]),
                                                new SpreadsheetOption("USD", "US Dollar", ["$", "USD"])
                                            ]
                                        }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("layout-detection", "20-Row Header & Delimiter Detection", "Import Pipeline",
                "Automatically skip export banners and detect CSV/TSV delimiters across the first 20 rows.",
                [
                    new("detect", "How Layout Detection Works",
                        Paragraphs:
                        [
                            "LoadText(csv, fileName) and Paste(clipboardText) inspect the raw content for comma, semicolon, or tab delimiters (matching Excel and Google Sheets copy-paste natively).",
                            "The scanner scores the first 20 rows against your schema's column keys, labels, and aliases to locate the true header line — automatically skipping title banners like 'Export Report 2026;;'."
                        ],
                        CodeSamples:
                        [
                            new("LayoutDetection.cs", """
                                var importer = new SpreadsheetImporter(schema);
                                importer.LoadText("Export Report 2026;;\nCode;Amount;Currency\nA1;12,50;€\nA2;45,00;USD", "rates.csv");

                                Assert.True(importer.FileLoaded);
                                Assert.Equal(2, importer.HeaderRowNumber);
                                """)
                        ])
                ]),
            new("column-mapping", "NFD Normalization & 80% Suggestions", "Import Pipeline",
                "Match messy headers reliably and suggest missing mappings only when sample validation passes >= 80%.",
                [
                    new("nfd", "Normalization & Column Statuses",
                        Paragraphs:
                        [
                            "SpreadsheetImporter.NormalizeText() applies Unicode FormD decomposition, strips diacritics, lowercases text, and removes trailing markers like '*' and '(required)' (e.g. 'Prénom * (required)' -> 'prenom').",
                            "When a required schema column has no header name match (e.g. file says 'Amount' instead of 'Unit Price'), the engine tests each unmapped source column's sample values against the field's parser and rules. If >= 80% pass, it surfaces a Suggestion without silently auto-applying it."
                        ],
                        TableHeaders: ["SpreadsheetColumnStatus", "Meaning", "Action"],
                        TableRows:
                        [
                            new("Matched", "Bound to a source column (exact, alias, or user-assigned)", "Ready"),
                            new("Default", "Unmapped but has a static default value", "Uses default"),
                            new("Missing", "Required column not yet bound (may carry a Suggestion)", "Confirm suggestion or pick column"),
                            new("Unmatched", "Source column not bound to any schema field", "Ignored during import")
                        ],
                        CodeSamples:
                        [
                            new("Suggestions.cs", """
                                var suggestion = importer.MissingFields[0].Suggestion; // "Amount" (100% >= 80%)
                                importer.AssignColumn("price", suggestion!.HeaderIndex);
                                Assert.Empty(importer.MissingFields);
                                """)
                        ])
                ]),
            new("unknown-values", "Unknown Value Questions", "Import Pipeline",
                "Resolve unrecognized enum/select values globally before running validation or submission.",
                [
                    new("policies", "All 5 SpreadsheetUnknownValuePolicy Options",
                        TableHeaders: ["Policy", "Behavior", "When to Use"],
                        TableRows:
                        [
                            new("Ask", "Collects distinct unknown strings into OpenQuestions for user resolution", "Interactive onboarding wizards"),
                            new("Error", "Marks every row with an unknown value as Blocking", "Strict compliance imports"),
                            new("SkipRows", "Automatically discards rows with unknown values", "Best-effort migrations"),
                            new("LeaveEmpty", "Coerces unknown values to null", "Optional lookup fields"),
                            new("Create", "Accepts unknown values as new items to create on submit", "Tag / category vocabularies")
                        ],
                        CodeSamples:
                        [
                            new("AnswerUnknown.cs", """
                                // Resolution actions: Map, LeaveEmpty, SkipRows, Create
                                importer.AnswerValue("level", "C1-Advanced",
                                    new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.Map, "B2", "B2"));
                                """)
                        ])
                ]),
            new("reconciliation", "Duplicates, Upsert & Inline Edits", "Import Pipeline",
                "Detect intra-file duplicates, reconcile against ExistingRecords, and fix rows inline.",
                [
                    new("policies", "Duplicates, Existing Records & Row Modes",
                        TableHeaders: ["Enum", "Values", "Purpose"],
                        TableRows:
                        [
                            new("SpreadsheetDuplicatesPolicy", "Error, KeepFirst, KeepLast", "Controls handling of duplicate RowKeyField keys inside the same file"),
                            new("SpreadsheetExistingAction", "Update, Skip, Error", "Controls behavior when a row key already exists in Schema.ExistingRecords"),
                            new("SpreadsheetRowStatus", "Valid, Warning, Blocking, Discarded", "Computed validation status per row"),
                            new("SpreadsheetRowMode", "Create, Update, Skip", "Target persistence operation for each importable row"),
                            new("SpreadsheetDiscardReason", "Manual, Value, Limit, Duplicate, Existing", "Why a row was excluded from ImportableRows")
                        ],
                        CodeSamples:
                        [
                            new("InlineEdit.cs", """
                                // Fix a blocking cell inline; row status recalculates immediately
                                importer.EditCell(rowIndex: 1, fieldKey: "email", rawText: "alice.alt@example.com");
                                Assert.True(importer.CanSubmit);
                                """)
                        ])
                ]),
            new("batch-submit", "Batch Submit & Retry Rejected", "Import Pipeline",
                "Stream validated rows in configurable batches and recover cleanly from partial server rejections.",
                [
                    new("submit", "SubmitAsync & RetryRejectedAsync",
                        Paragraphs:
                        [
                            "Configure BatchSize and SubmitHandler on SpreadsheetSchema. During SubmitAsync(), SubmitStatus transitions Idle -> Running -> Done (or Error if any rows were rejected by the server).",
                            "Server rejections return SpreadsheetRejectedRow(RowIndex, Message, Field) records; users edit only those rows and call RetryRejectedAsync() without re-submitting already-succeeded batches."
                        ],
                        CodeSamples:
                        [
                            new("BatchSubmit.cs", """
                                await importer.SubmitAsync();
                                if (importer.SubmitStatus == SpreadsheetSubmitStatus.Error)
                                {
                                    foreach (var rej in importer.RejectedRows)
                                    {
                                        importer.EditCell(rej.RowIndex, rej.Field ?? "code", "FIXED-CODE");
                                    }
                                    await importer.RetryRejectedAsync();
                                }
                                """)
                        ])
                ]),
            new("wizard", "BxSpreadsheetImport Wizard", "Import Pipeline",
                "Drop-in 5-step interactive Blazor & .NET MAUI Blazor Hybrid wizard.",
                [
                    new("component", "Interactive Wizard",
                        LiveDemoKey: "spreadsheet-wizard",
                        LiveDemoTitle: "BxSpreadsheetImport Live Demo",
                        CodeSamples:
                        [
                            new("ImportPage.razor", """
                                <BxSpreadsheetImport Importer="@_importer"
                                                     Title="Import Product Rates"
                                                     Wizard="true" />
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Spreadsheet", "Lux.Spreadsheet.Core", "Lux.Spreadsheet", "builder.Services.AddLuxSpreadsheet();",
                "var importer = new SpreadsheetImporter(schema); importer.LoadText(csvText, \"rates.csv\");"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Spreadsheet.Core and Lux.Spreadsheet.",
                [],
                ApiTypes: [typeof(BxSpreadsheetImport), typeof(SpreadsheetImporter), typeof(SpreadsheetSchema), typeof(SpreadsheetColumnSchema)])
        ]);
}
