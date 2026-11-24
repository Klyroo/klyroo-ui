using Blux.UI.Tools;
using Blux.UI.Tools.Query;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildQuery() => new(
        "query", "Lux.Query", "#ff4154",
        [
            new("", "Introduction", "Getting Started",
                "Strongly typed, NativeAOT-compatible query AST, reversible URL search parameter codecs, InMemory & IQueryable evaluators, EF Core async SQL execution, and reactive QueryState.",
                [
                    new("overview", "What is Lux.Query?",
                        Paragraphs:
                        [
                            "Lux.Query separates query modeling from UI rendering. You describe a data shape once with DataListSchema.For<T>(), then run immutable DataListRequest queries against in-memory collections, LINQ IQueryable sources, or SQL databases via Entity Framework Core."
                        ],
                        Bullets:
                        [
                            "Lux.Query.Core — 100% headless C#, zero UI dependency, NativeAOT & trimming verified.",
                            "Lux.Query — Blazor Web & .NET MAUI Blazor Hybrid URL state manager (QueryState, QueryGroup, QueryValue<T>).",
                            "Lux.Query.EntityFrameworkCore — Async SQL execution adapter (EfAsyncQueryExecutor)."
                        ]),
                    new("matrix", "Package & Target Matrix",
                        TableHeaders: ["Package", "Purpose", "AOT / Platforms"],
                        TableRows:
                        [
                            new("Lux.Query.Core", "Schema, Filter AST, Codecs, InMemory & IQueryable", "IsAotCompatible · Any .NET 10 host"),
                            new("Lux.Query", "Reactive URL QueryState & QueryGroup", "browser, ios, android, maccatalyst, windows"),
                            new("Lux.Query.EntityFrameworkCore", "EfAsyncQueryExecutor for async SQL", "EF Core 10")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install the headless Core engine, the reactive QueryState package, and optionally the EF Core executor.",
                [
                    new("nuget", "1. Add Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Query.Core
                                dotnet add package Lux.Query
                                dotnet add package Lux.Query.EntityFrameworkCore # Optional: EF Core async SQL
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples:
                        [
                            new("Program.cs", """
                                builder.Services.AddLuxQuery();
                                // Server with EF Core:
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
                    new("builder", "Building a Schema",
                        Paragraphs: ["DataListSchema.For<T>() compiles fast property accessors and validates field keys at startup so queries never rely on untyped reflection strings."],
                        CodeSamples:
                        [
                            new("ProductSchema.cs", """
                                public sealed record Product(int Id, string Name, string Category, decimal Price, DateOnly Released, bool Active);

                                var schema = DataListSchema.For<Product>(s => s
                                    .RowKey(p => p.Id)
                                    .Search(p => p.Name, p => p.Category)
                                    .Field(p => p.Price, f => f.Key("price").Sortable().Filterable())
                                    .Field(p => p.Category, f => f.Key("category").Sortable().Filterable())
                                    .Field(p => p.Released, f => f.Key("released").Sortable().Filterable())
                                    .Field(p => p.Active, f => f.Key("active").Filterable()));
                                """)
                        ]),
                    new("collections", "Collection & Nested Fields",
                        Paragraphs: ["Schemas support nested property paths and collection fields (e.g. tags or roles) with consistent InMemory and IQueryable semantics."])
                ]),
            new("filters", "Filter AST & Operators", "Core Engine",
                "Compose nested AND/OR filter trees using DataListFilterGroup and strongly typed FilterValue nodes.",
                [
                    new("ast", "Filter Tree Model",
                        Bullets:
                        [
                            "DataListFilterGroup combines child rules and sub-groups using LogicalOperator.And or LogicalOperator.Or.",
                            "FilterExpressions compiles the AST into expression trees for IQueryable (translatable to SQL) and delegates for InMemory execution.",
                            "DateUnit and LocalInstants cut calendar days accurately according to the caller's IANA time zone."
                        ],
                        CodeSamples:
                        [
                            new("FilterExample.cs", """
                                var filter = new DataListFilterGroup(
                                    DataListLogic.And,
                                    [
                                        new DataListFilterRule("active", DataListOperator.Equals, FilterValue.From(true)),
                                        new DataListFilterRule("price", DataListOperator.GreaterThanOrEqual, FilterValue.From(100m))
                                    ]);
                                """)
                        ])
                ]),
            new("paging-and-sorting", "Sorting, Paging & Cursors", "Core Engine",
                "Support offset pagination, keyset cursor pagination, and multi-column sorting in a single immutable request.",
                [
                    new("offset-cursor", "Offset vs Cursor Paging",
                        TableHeaders: ["Paging Mode", "Type", "Best For"],
                        TableRows:
                        [
                            new("Offset", "DataListPaging.Offset(Skip, Take)", "Admin pages with jump-to-page controls and known TotalCount"),
                            new("Cursor", "DataListPaging.Cursor(After, Take)", "Infinite scroll feeds and large tables without OFFSET drift")
                        ],
                        CodeSamples:
                        [
                            new("PagingAndSorting.cs", """
                                var request = new DataListRequest(
                                    Paging: new DataListPaging.Offset(Skip: 25, Take: 25),
                                    Sorts: [new DataListSort("category", Descending: false), new DataListSort("price", Descending: true)],
                                    Filter: DataListFilterGroup.Empty,
                                    Search: new DataListSearch("studio"));
                                """)
                        ])
                ]),
            new("codecs", "URL Codecs", "Core Engine",
                "Serialize and deserialize complete DataListState and filter trees into clean, shareable URL query parameters.",
                [
                    new("query-codecs", "IQueryCodec & QueryCodecs",
                        Paragraphs: ["QueryCodecs provides built-in reversible codecs for primitives, dates, enums, lists, and DataListStateQuery so bookmarked URLs reconstruct the exact query state."],
                        CodeSamples:
                        [
                            new("CodecUsage.cs", """
                                var pairs = DataListStateQuery.Write(state, schema);
                                var restored = DataListStateQuery.Read(pairs, schema);
                                """)
                        ])
                ]),
            new("executors", "InMemory & IQueryable", "Executors & State",
                "Run the same DataListRequest against local arrays or LINQ IQueryable providers.",
                [
                    new("inmemory", "InMemory Execution",
                        CodeSamples:
                        [
                            new("InMemory.cs", """
                                DataListResult<Product> result = schema.ExecuteInMemory(products, request);
                                // result.Items, result.TotalCount, result.NextCursor
                                """)
                        ]),
                    new("iqueryable", "IQueryable Applier",
                        CodeSamples:
                        [
                            new("Queryable.cs", """
                                IQueryable<Product> query = schema.Apply(db.Products.AsNoTracking(), request);
                                """)
                        ])
                ]),
            new("ef-core", "Entity Framework Core", "Executors & State",
                "Execute DataListRequest asynchronously against SQLite, PostgreSQL, or SQL Server via EfAsyncQueryExecutor.",
                [
                    new("ef-setup", "Registering EfAsyncQueryExecutor",
                        Paragraphs: ["Lux.Query.EntityFrameworkCore keeps EF Core out of Lux.Query.Core so WebAssembly and MAUI clients never download database drivers."],
                        CodeSamples:
                        [
                            new("ServerProgram.cs", """
                                builder.Services.AddSingleton<IDataListQueryExecutor, EfAsyncQueryExecutor>();
                                """)
                        ])
                ]),
            new("query-state", "Reactive QueryState", "Executors & State",
                "Two-way URL search parameter state for Blazor Web and .NET MAUI BlazorWebView.",
                [
                    new("qs", "Reading, Writing & History Mode",
                        Bullets:
                        [
                            "QueryHistory.Replace (default) updates the address bar without polluting Back history while typing filters.",
                            "QueryHistory.Push creates a browser history entry for page changes or view switches.",
                            "QueryGroup coordinates multiple parameters atomically in a single navigation update."
                        ],
                        CodeSamples:
                        [
                            new("SearchPage.razor", """
                                @inject QueryState Query

                                @code {
                                    private string Search => Query.Get("q") ?? "";
                                    private void SetSearch(string value) => Query.Set("q", string.IsNullOrWhiteSpace(value) ? null : value);
                                    private void GoToPage(int page) => Query.Set("page", page.ToString(), QueryHistory.Push);
                                }
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Query", "Lux.Query.Core", "Lux.Query", "builder.Services.AddLuxQuery();",
                "var result = schema.ExecuteInMemory(localItems, request);"),
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
                        Paragraphs: ["Lux.Table pairs a zero-UI NativeAOT tree engine (Lux.Table.Core) with high-density Blazor & MAUI table and card-grid components featuring sorting, HeaderMenu, visual grouping, footer aggregations, progress lines, skeletons, and responsive phone summary rows."],
                        Bullets:
                        [
                            "Lux.Table.Core — 100% headless NativeAOT DataListTree<TItem> and DataListTreeNode<TItem>.",
                            "Lux.Table — Multiplatform RCL containing BxDataList, BxDataListTable, BxDataListGrid, BxDataListColumn, BxDataListToolbar, BxDataListFilterBar, BxDataListPagination, DataListGroupBy, DataListSummaries, and DataListRowSummary."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Table and register services in Program.cs or MauiProgram.cs.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Table.Core   # Headless NativeAOT tree models
                                dotnet add package Lux.Table        # Full Blazor & MAUI DataList
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples:
                        [
                            new("Program.cs", """
                                builder.Services.AddLuxTable(options =>
                                {
                                    options.DefaultPageSize = 25;
                                });
                                """)
                        ])
                ]),
            new("data-list", "BxDataList & Layouts", "Components",
                "Compose tables, card grids, toolbars, filter bars, and pagination inside a single BxDataList<TItem> container.",
                [
                    new("compose", "Declarative Composition",
                        CodeSamples:
                        [
                            new("Directory.razor", """
                                <BxDataList TItem="Employee" Schema="@_schema" Items="@_employees" UrlSync="true">
                                    <BxDataListToolbar TItem="Employee" />
                                    <BxDataListFilterBar TItem="Employee" />
                                    <BxDataListTable TItem="Employee" HeaderMenu="true" ShowProgressLine="true">
                                        <BxDataListColumn TItem="Employee" Field="name" Title="Employee" />
                                        <BxDataListColumn TItem="Employee" Field="department" Title="Department" />
                                        <BxDataListColumn TItem="Employee" Field="salary" Title="Salary" />
                                    </BxDataListTable>
                                    <BxDataListPagination TItem="Employee" />
                                </BxDataList>
                                """)
                        ])
                ]),
            new("columns", "Columns & HeaderMenu", "Components",
                "Configure sortable, hideable, resizable, and pinnable columns with an automatic column header dropdown.",
                [
                    new("header-menu", "Automatic HeaderMenu",
                        Paragraphs: ["Set HeaderMenu=\"true\" on BxDataListTable so clicking any column header opens Sort Ascending, Sort Descending, Clear Sort, and Hide Column actions without writing custom header templates."],
                        CodeSamples:
                        [
                            new("Columns.razor", """
                                <BxDataListTable TItem="Employee" HeaderMenu="true">
                                    <BxDataListColumn TItem="Employee" Field="name" Title="Name" />
                                    <BxDataListColumn TItem="Employee" Field="salary" Title="Salary" Summary="DataListSummaryKind.Sum" />
                                </BxDataListTable>
                                """)
                        ])
                ]),
            new("tree", "Tree Hierarchy (DataListTree)", "Components",
                "Render nested parent-child hierarchies with indentation rails, keyboard expand/collapse, and configurable selection modes.",
                [
                    new("headless-tree", "Headless Tree Engine (Lux.Table.Core)",
                        Paragraphs: ["DataListTree<TItem>.BuildTree() produces flattened DataListTreeNode<TItem> records carrying Depth, Parent, IsLast, and AncestorContinuations — usable in NativeAOT code or passed directly to BxDataListTable."],
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
                        ])
                ]),
            new("grouping-and-summaries", "Grouping & Footer Summaries", "Components",
                "Group loaded rows visually under collapsible headers and compute automatic footer aggregations.",
                [
                    new("grouping", "Visual Row Grouping",
                        CodeSamples:
                        [
                            new("Grouping.cs", """
                                var byDept = new DataListGroupBy<Employee> { Key = e => e.Department };
                                """)
                        ]),
                    new("summaries", "Footer Aggregations",
                        Paragraphs: ["Attach Summaries=\"@(new DataListSummaries<Employee>())\" on BxDataListTable and specify Summary=\"DataListSummaryKind.Sum | Average | Min | Max | Count\" on numeric columns."])
                ]),
            new("mobile-summary", "Mobile Summary Rows", "Components",
                "Automatically adapt wide multi-column tables into thumb-friendly summary cards on iOS and Android phones.",
                [
                    new("row-summary", "DataListRowSummary<TItem>",
                        Paragraphs: ["When AutoPhone is true (default), BxDataListTable switches below the 768px breakpoint into a scannable two-line layout preserving leading visuals, status badges, checkboxes, and row actions."],
                        CodeSamples:
                        [
                            new("MobileSummary.cs", """
                                private static readonly DataListRowSummary<Employee> PhoneSummary = new()
                                {
                                    Title = e => e.Name,
                                    Details = e => [e.Department, $"€{e.Salary:N0}"],
                                    AutoPhone = true
                                };
                                """)
                        ])
                ]),
            new("ux-polish", "Empty States, Skeletons & Progress", "Components",
                "Give users clear loading, refreshing, and zero-data feedback.",
                [
                    new("polish", "Built-in States",
                        TableHeaders: ["Feature", "Property / Type", "Behavior"],
                        TableRows:
                        [
                            new("Progress Line", "ShowProgressLine=\"true\"", "2px top indeterminate bar during background refreshes while keeping rows visible"),
                            new("Adaptive Skeletons", "DataListSkeletonKind", "Table, Cards, or Matrix loading placeholders matching the active layout"),
                            new("Empty State", "DataListEmptyState", "First-use onboarding CTA when dataset is empty vs Reset Filters button when filtered")
                        ])
                ]),
            MauiAndAotPage("Lux.Table", "Lux.Table.Core", "Lux.Table", "builder.Services.AddLuxTable();",
                "var (roots, allNodes) = new DataListTree<Employee> { Children = e => e.Reports }.BuildTree(employees);"),
            new("recipes", "Testing & Recipes", "Platforms",
                "Testing BxDataList and DataListTree with xUnit and bUnit.",
                [
                    new("bunit", "bUnit Setup Order",
                        Paragraphs: ["Always register Services.AddLuxTable() before calling SetRendererInfo(...) in bUnit tests."],
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
                        ])
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
                "Headless NativeAOT CSV/TSV importer with 20-row header detection, NFD normalization, >=80% pass-rate column suggestions, unknown value resolution, duplicate/upsert handling, inline cell edits, and batched submission.",
                [
                    new("overview", "What is Lux.Spreadsheet?",
                        Bullets:
                        [
                            "Lux.Spreadsheet.Core — 100% headless C#, NativeAOT compatible. Contains SpreadsheetSchema, SpreadsheetColumnSchema, SpreadsheetRule, and SpreadsheetImporter.",
                            "Lux.Spreadsheet — Multiplatform Razor Class Library shipping the 5-step BxSpreadsheetImport wizard and AddLuxSpreadsheet()."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Spreadsheet and register services.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Spreadsheet.Core
                                dotnet add package Lux.Spreadsheet
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs", "builder.Services.AddLuxSpreadsheet();")])
                ]),
            new("schema", "Schema, Columns & Rules", "Import Pipeline",
                "Declare typed columns, decimal separators, validation rules, aliases, and select options.",
                [
                    new("define", "Defining SpreadsheetSchema",
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
                                        new SpreadsheetColumnSchema { Key = "price", Label = "Unit Price", Kind = SpreadsheetColumnKind.Number, DecimalSeparator = ',', Required = true, Rules = [SpreadsheetRule.Min(0)] },
                                        new SpreadsheetColumnSchema
                                        {
                                            Key = "currency",
                                            Label = "Currency",
                                            Kind = SpreadsheetColumnKind.Select,
                                            Required = true,
                                            UnknownPolicy = SpreadsheetUnknownValuePolicy.Ask,
                                            Options = [new("EUR", "Euro", ["€", "EUR"]), new("USD", "US Dollar", ["$", "USD"])]
                                        }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("layout-detection", "20-Row Header & Delimiter Detection", "Import Pipeline",
                "Skip report banners and detect CSV/TSV delimiters automatically across the first 20 rows.",
                [
                    new("detect", "Heuristic Layout Scanner",
                        Paragraphs: ["LoadText() and Paste() automatically detect comma, semicolon, or tab delimiters and inspect up to 20 leading rows to pinpoint the true header line (exposed via importer.HeaderRowNumber)."],
                        CodeSamples:
                        [
                            new("LoadText.cs", """
                                var csv = "Export Report 2026;;\nCode;Amount;Currency\nA1;12,50;€\nA2;45,00;USD";
                                importer.LoadText(csv, "rates.csv");
                                // importer.HeaderRowNumber == 2
                                """)
                        ])
                ]),
            new("column-mapping", "NFD Normalization & 80% Suggestions", "Import Pipeline",
                "Match headers accent-insensitively and propose high-confidence column mappings without silent mistakes.",
                [
                    new("nfd", "Normalization & Sample Scoring",
                        Bullets:
                        [
                            "SpreadsheetImporter.NormalizeText(\"Prénom * (required)\") produces \"prenom\".",
                            "Unmapped fields score remaining source columns against sample rows and emit a Suggestion only when >= 80% of samples parse and pass validation rules.",
                            "Suggestions are never auto-applied silently; call importer.AssignColumn(fieldKey, headerIndex) or let the user confirm in the UI."
                        ],
                        CodeSamples:
                        [
                            new("ApplySuggestion.cs", """
                                var suggestion = importer.MissingFields[0].Suggestion;
                                if (suggestion is not null)
                                {
                                    importer.AssignColumn("price", suggestion.HeaderIndex);
                                }
                                """)
                        ])
                ]),
            new("unknown-values", "Unknown Value Questions", "Import Pipeline",
                "Surface unrecognized select values as explicit mapping questions before submission.",
                [
                    new("questions", "SpreadsheetUnknownValuePolicy.Ask",
                        CodeSamples:
                        [
                            new("AnswerValue.cs", """
                                // When "C1-Advanced" is not in allowed options ["B1", "B2"]:
                                Assert.Single(importer.OpenQuestions);
                                importer.AnswerValue("level", "C1-Advanced",
                                    new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.Map, "B2", "B2"));
                                """)
                        ])
                ]),
            new("reconciliation", "Duplicates, Upsert & Inline Edits", "Import Pipeline",
                "Handle intra-file duplicate keys, updates against ExistingRecords, and row-level inline corrections.",
                [
                    new("policies", "Reconciliation Policies",
                        TableHeaders: ["Policy", "Enum", "Options"],
                        TableRows:
                        [
                            new("Intra-file duplicates", "SpreadsheetDuplicatesPolicy", "Error, KeepFirst, KeepLast"),
                            new("Existing DB records", "SpreadsheetExistingAction", "Update, Skip, Error"),
                            new("Inline correction", "EditCell(rowIndex, key, text)", "Re-validates the row immediately and clears resolved errors")
                        ])
                ]),
            new("batch-submit", "Batch Submit & Retry Rejected", "Import Pipeline",
                "Send validated rows in configurable batches and retry only rows rejected by your backend.",
                [
                    new("batch", "Batched Submission",
                        CodeSamples:
                        [
                            new("SubmitAndRetry.cs", """
                                await importer.SubmitAsync();
                                if (importer.RejectedRows.Count > 0)
                                {
                                    importer.EditCell(importer.RejectedRows[0].RowIndex, "code", "FIXED-01");
                                    await importer.RetryRejectedAsync();
                                }
                                """)
                        ])
                ]),
            new("wizard", "BxSpreadsheetImport Wizard", "Import Pipeline",
                "Drop-in 5-step interactive Blazor & MAUI Hybrid import wizard.",
                [
                    new("component", "Using BxSpreadsheetImport",
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
