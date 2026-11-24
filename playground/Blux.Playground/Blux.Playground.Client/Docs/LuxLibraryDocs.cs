using Blux.UI;
using Blux.UI.Tools;
using Blux.UI.Tools.Query;

namespace Blux.Playground.Client.Docs;

public sealed record DocCodeSample(string Title, string Code);

public sealed record DocTableRow(string Col1, string Col2, string Col3);

public sealed record DocSection(
    string Id,
    string Heading,
    string[]? Paragraphs = null,
    string[]? Bullets = null,
    DocCodeSample[]? CodeSamples = null,
    string[]? TableHeaders = null,
    DocTableRow[]? TableRows = null);

public sealed record DocPageSpec(
    string Slug,
    string Title,
    string Group,
    string Lead,
    DocSection[] Sections,
    Type[]? ApiTypes = null);

public sealed record LibraryDocTree(
    string LibrarySlug,
    string LibraryName,
    string AccentHex,
    DocPageSpec[] Pages);

/// <summary>
/// Exhaustive, independent English multi-page documentation for each LuxStack library
/// (6 pages per library: Introduction, Installation, Core Concepts, MAUI &amp; NativeAOT, Recipes &amp; Testing, API Reference).
/// </summary>
public static class LuxLibraryDocs
{
    public static IReadOnlyList<LibraryDocTree> All { get; } =
    [
        BuildQuery(),
        BuildTable(),
        BuildForm(),
        BuildDashboard(),
        BuildDisplay(),
        BuildSpreadsheet(),
        BuildAgent(),
    ];

    public static LibraryDocTree? FindLibrary(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : All.FirstOrDefault(l => string.Equals(l.LibrarySlug, slug.Trim(), StringComparison.OrdinalIgnoreCase));

    public static DocPageSpec? FindPage(LibraryDocTree lib, string? pageSlug)
    {
        var key = string.IsNullOrWhiteSpace(pageSlug) ? "" : pageSlug.Trim().ToLowerInvariant();
        return lib.Pages.FirstOrDefault(p => string.Equals(p.Slug, key, StringComparison.OrdinalIgnoreCase));
    }

    public static string PageHref(string librarySlug, string pageSlug) =>
        string.IsNullOrEmpty(pageSlug) ? $"/{librarySlug}/docs" : $"/{librarySlug}/docs/{pageSlug}";

    public static (BxContentSurroundLink? Previous, BxContentSurroundLink? Next) Surround(LibraryDocTree lib, string pageSlug)
    {
        var idx = Array.FindIndex(lib.Pages, p => string.Equals(p.Slug, pageSlug, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return (null, null);
        BxContentSurroundLink ToLink(DocPageSpec p) => new(p.Title, PageHref(lib.LibrarySlug, p.Slug)) { Description = p.Lead };
        return (
            idx > 0 ? ToLink(lib.Pages[idx - 1]) : null,
            idx < lib.Pages.Length - 1 ? ToLink(lib.Pages[idx + 1]) : null);
    }

    private static DocPageSpec MauiAndAotPage(string name, string corePkg, string uiPkg, string diCall, string coreUsage) =>
        new(
            Slug: "maui-and-aot",
            Title: "MAUI & NativeAOT",
            Group: "Guides",
            Lead: $"Run {name} across Blazor Web, .NET MAUI Blazor Hybrid (iOS, Android, macOS Catalyst, Windows), native C# MAUI, and NativeAOT binaries.",
            Sections:
            [
                new("maui-hybrid", ".NET MAUI Blazor Hybrid",
                    Paragraphs:
                    [
                        $"{uiPkg} declares explicit <SupportedPlatform> items for browser, ios, android, maccatalyst, and windows so it compiles cleanly in multi-targeted .NET MAUI apps with zero platform analyzer warnings."
                    ],
                    CodeSamples:
                    [
                        new("MauiProgram.cs", $"""
                            var builder = MauiApp.CreateBuilder();
                            builder.UseMauiApp<App>();
                            builder.Services.AddMauiBlazorWebView();

                            // Register {name}
                            {diCall}
                            """),
                        new("wwwroot/index.html", """
                            <link rel="stylesheet" href="_content/Blux.UI/blux.preflight.css" />
                            <link rel="stylesheet" href="_content/Blux.UI/blux.css" />
                            """)
                    ]),
                new("native-maui", "Native C# .NET MAUI (Without BlazorWebView)",
                    Paragraphs:
                    [
                        $"If your MAUI application uses XAML or C# Markup instead of BlazorWebView, install only {corePkg}. It has zero dependency on ASP.NET Core or Razor and integrates directly into MVVM ViewModels."
                    ],
                    CodeSamples:
                    [
                        new("Terminal", $"dotnet add package {corePkg}"),
                        new("ViewModel.cs", coreUsage)
                    ]),
                new("native-aot", "NativeAOT & Trimming",
                    Paragraphs:
                    [
                        $"{corePkg} is compiled with <IsAotCompatible>true</IsAotCompatible>, <IsTrimmable>true</IsTrimmable>, and <TreatWarningsAsErrors>true</TreatWarningsAsErrors>."
                    ],
                    CodeSamples:
                    [
                        new("Publish NativeAOT", "dotnet publish -c Release -p:PublishAot=true")
                    ])
            ]);

    private static LibraryDocTree BuildQuery() => new(
        "query", "Lux.Query", "#ff4154",
        [
            new("", "Introduction", "Getting Started",
                "Strongly typed, NativeAOT-compatible query AST, reversible URL search parameter codecs, InMemory & IQueryable evaluators, Entity Framework Core async SQL execution, and reactive Blazor / .NET MAUI QueryState.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Query.Core — 100% headless C#, zero UI dependency, NativeAOT compatible. Houses DataListSchema, DataListRequest, DataListFilterGroup, QueryCodecs, InMemoryDataList, and QueryableDataList.",
                            "Lux.Query — Multiplatform Razor Class Library (browser, ios, android, maccatalyst, windows). Provides reactive URL QueryState, QueryGroup, QueryValue<T>, and AddLuxQuery().",
                            "Lux.Query.EntityFrameworkCore — Optional adapter translating DataListRequest into async SQL via EfAsyncQueryExecutor."
                        ]),
                    new("platforms", "Supported Targets",
                        TableHeaders: ["Package", "Blazor Web & MAUI Hybrid", "Headless NativeAOT / MAUI C#"],
                        TableRows:
                        [
                            new("Lux.Query.Core", "Yes", "Yes (IsAotCompatible)"),
                            new("Lux.Query", "Yes (browser, iOS, Android, Mac, Win)", "—"),
                            new("Lux.Query.EntityFrameworkCore", "Server / API", "EF Core 10")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Add the core headless package, the Blazor/MAUI URL state package, and optionally the EF Core executor.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Query.Core                # Headless NativeAOT schema & query AST
                                dotnet add package Lux.Query                     # Blazor Web & .NET MAUI QueryState
                                dotnet add package Lux.Query.EntityFrameworkCore # Optional async SQL executor
                                """)
                        ]),
                    new("di", "2. Register Services",
                        Paragraphs: ["Call AddLuxQuery() in Program.cs (Server and Client for Blazor Auto) or MauiProgram.cs."],
                        CodeSamples:
                        [
                            new("Program.cs", """
                                builder.Services.AddLuxQuery();
                                // Server project with EF Core:
                                builder.Services.AddSingleton<IDataListQueryExecutor, EfAsyncQueryExecutor>();
                                """)
                        ]),
                    new("imports", "3. Namespace Imports",
                        CodeSamples:
                        [
                            new("_Imports.razor", """
                                @using Blux.UI.Tools
                                @using Blux.UI.Tools.Query
                                """)
                        ])
                ]),
            new("concepts", "Core Concepts", "Guides",
                "Define schemas once, evaluate them in memory or in SQL, and round-trip complex filter trees through URL query parameters.",
                [
                    new("schema", "Typed DataListSchema<T>",
                        Paragraphs: ["Declare the row key, searchable columns, sortable fields, and filterable predicates with full compile-time type safety."],
                        CodeSamples:
                        [
                            new("ProductSchema.cs", """
                                using Blux.UI.Tools.Query;

                                public sealed record Product(int Id, string Name, string Category, decimal Price, bool Active);

                                var schema = DataListSchema.For<Product>(s => s
                                    .RowKey(p => p.Id)
                                    .Search(p => p.Name, p => p.Category)
                                    .Field(p => p.Price, f => f.Key("price").Sortable().Filterable())
                                    .Field(p => p.Category, f => f.Key("category").Sortable().Filterable())
                                    .Field(p => p.Active, f => f.Key("active").Filterable()));
                                """)
                        ]),
                    new("request", "DataListRequest & Evaluation",
                        Paragraphs: ["DataListRequest combines offset or cursor paging, multi-column sorting, nested AND/OR filter groups, and full-text search."],
                        CodeSamples:
                        [
                            new("Evaluate.cs", """
                                var request = new DataListRequest(
                                    new DataListPaging.Offset(Skip: 0, Take: 25),
                                    [new DataListSort("price", Descending: true)],
                                    DataListFilterGroup.Empty,
                                    new DataListSearch("Monitor"));

                                // In-memory execution
                                var page = schema.ExecuteInMemory(products, request);

                                // IQueryable / EF Core translation
                                IQueryable<Product> sqlQuery = schema.Apply(db.Products.AsNoTracking(), request);
                                """)
                        ]),
                    new("url-state", "Reactive URL QueryState",
                        Paragraphs: ["Inject QueryState in Blazor or MAUI Hybrid components to keep filters, sort order, and page number synchronized with the URL."],
                        CodeSamples:
                        [
                            new("Catalog.razor", """
                                @inject QueryState Query

                                <BxInput Value="@(Query.Get("q") ?? "")"
                                         ValueChanged="@(v => Query.Set("q", string.IsNullOrWhiteSpace(v) ? null : v))"
                                         Placeholder="Search catalog..." />
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Query", "Lux.Query.Core", "Lux.Query", "builder.Services.AddLuxQuery();",
                "var page = schema.ExecuteInMemory(localProducts, request);"),
            new("recipes", "Recipes & Testing", "Guides",
                "Practical patterns for EF Core async queries and deterministic xUnit / bUnit tests.",
                [
                    new("testing-core", "Headless Unit Testing with xUnit",
                        CodeSamples:
                        [
                            new("QueryCoreTests.cs", """
                                var res = schema.ExecuteInMemory(items, new DataListRequest(
                                    new DataListPaging.Offset(0, 10),
                                    [new DataListSort("price", Descending: true)],
                                    DataListFilterGroup.Empty,
                                    new DataListSearch("M")));

                                Assert.Equal(2, res.TotalCount);
                                Assert.Equal("Monitor", res.Items[0].Name);
                                """)
                        ]),
                    new("testing-qs", "Testing QueryState with bUnit",
                        CodeSamples:
                        [
                            new("QueryStateTests.cs", """
                                Services.AddLuxQuery();
                                var qs = Services.GetRequiredService<QueryState>();
                                qs.UseRenderer(new RendererInfo("Server", isInteractive: true));
                                Assert.True(qs.Set("page", "3"));
                                Assert.Equal("3", qs.Get("page"));
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Query.Core and Lux.Query.",
                [],
                ApiTypes: [typeof(DataListRequest), typeof(DataListResult<object>), typeof(DataListSort), typeof(DataListSearch), typeof(QueryState)])
        ]);

    private static LibraryDocTree BuildTable() => new(
        "table", "Lux.Table", "#00d8ff",
        [
            new("", "Introduction", "Getting Started",
                "Enterprise DataList and NativeAOT headless tree hierarchy engine for Blazor Web and .NET MAUI Blazor Hybrid.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Table.Core — 100% headless C#, zero UI dependency, NativeAOT compatible. Provides DataListTree<TItem>, DataListTreeNode<TItem>, and DataListTreeSelectionMode.",
                            "Lux.Table — Multiplatform Razor Class Library shipping BxDataList, BxDataListTable, BxDataListGrid, BxDataListColumn, BxDataListToolbar, BxDataListFilterBar, BxDataListPagination, DataListGroupBy, DataListSummaries, DataListRowSummary, and Lux.Table.lib.module.js."
                        ]),
                    new("polish", "Built-in UX Polish",
                        Bullets:
                        [
                            "HeaderMenu=\"true\" — Automatic Sort Asc / Sort Desc / Clear / Hide Column header dropdown.",
                            "ShowProgressLine=\"true\" — Subtle 2px top progress bar during background reloads.",
                            "SkeletonKind — Adaptive loading skeletons (Table, Cards, Matrix).",
                            "DataListEmptyState — Differentiated first-use onboarding CTA vs filtered-empty reset state.",
                            "DataListRowSummary — Automatic phone-friendly summary rows below 768px."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Table and register services in Program.cs or MauiProgram.cs.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Table.Core   # Headless NativeAOT tree hierarchy models
                                dotnet add package Lux.Table        # Full Blazor Web & .NET MAUI DataList
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
            new("concepts", "Core Concepts", "Guides",
                "Combine headless tree hierarchy, visual row grouping, footer aggregations, and responsive mobile summary rows.",
                [
                    new("tree", "Headless Tree Hierarchy (Lux.Table.Core)",
                        Paragraphs: ["Build flattened tree models with nesting depth, parent links, sibling end flags, and rail continuations with zero UI dependency."],
                        CodeSamples:
                        [
                            new("TreeHierarchy.cs", """
                                var tree = new DataListTree<Employee>
                                {
                                    Children = e => e.Reports,
                                    DefaultExpandedDepth = 1,
                                    Selectable = DataListTreeSelectionMode.All
                                };

                                var (roots, allNodes) = tree.BuildTree(employees, schema);
                                """)
                        ]),
                    new("datatable", "Table with HeaderMenu & Footer Summaries",
                        CodeSamples:
                        [
                            new("EmployeesTable.razor", """
                                <BxDataList TItem="Employee" Schema="@_schema" Items="@_employees" UrlSync="true">
                                    <BxDataListToolbar TItem="Employee" />
                                    <BxDataListTable TItem="Employee"
                                                     HeaderMenu="true"
                                                     ShowProgressLine="true"
                                                     Summaries="@(new DataListSummaries<Employee>())">
                                        <BxDataListColumn TItem="Employee" Field="name" Title="Employee" />
                                        <BxDataListColumn TItem="Employee" Field="department" Title="Department" />
                                        <BxDataListColumn TItem="Employee" Field="salary" Title="Salary"
                                                          Summary="DataListSummaryKind.Sum" />
                                    </BxDataListTable>
                                    <BxDataListPagination TItem="Employee" />
                                </BxDataList>
                                """)
                        ]),
                    new("mobile", "Responsive Mobile Summary Rows",
                        Paragraphs: ["Below 768px, AutoPhone switches wide tables into compact scannable rows while keeping checkboxes and row actions accessible."],
                        CodeSamples:
                        [
                            new("SummaryRow.cs", """
                                private static readonly DataListRowSummary<Employee> PhoneSummary = new()
                                {
                                    Title = e => e.Name,
                                    Details = e => [e.Department, $"€{e.Salary:N0}"],
                                    AutoPhone = true
                                };
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Table", "Lux.Table.Core", "Lux.Table", "builder.Services.AddLuxTable();",
                "var (roots, allNodes) = new DataListTree<Employee> { Children = e => e.Reports }.BuildTree(items);"),
            new("recipes", "Recipes & Testing", "Guides",
                "Visual row grouping and bUnit testing patterns for BxDataList.",
                [
                    new("grouping", "Grouping Rows by Department",
                        CodeSamples:
                        [
                            new("GroupBy.cs", """
                                var groupBy = new DataListGroupBy<Employee> { Key = e => e.Department };
                                """)
                        ]),
                    new("bunit", "Testing BxDataList with bUnit",
                        Paragraphs: ["Always call Services.AddLuxTable() before SetRendererInfo(...) in bUnit tests."],
                        CodeSamples:
                        [
                            new("TableTests.cs", """
                                Services.AddLuxTable();
                                JSInterop.Mode = JSRuntimeMode.Loose;
                                SetRendererInfo(new RendererInfo("Server", true));
                                var cut = Render<BxDataList<Employee>>(...);
                                Assert.Equal(2, cut.FindAll("tbody tr").Count);
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Table.Core and Lux.Table.",
                [],
                ApiTypes: [typeof(BxDataList<object>), typeof(BxDataListTable<object>), typeof(BxDataListColumn<object>), typeof(DataListTree<object>), typeof(DataListRowSummary<object>)])
        ]);

    private static LibraryDocTree BuildForm() => new(
        "form", "Lux.Form", "#f59e0b",
        [
            new("", "Introduction", "Getting Started",
                "Declarative, NativeAOT schema-driven form engine with 31 field kinds, reactive visibility rules, dirty diffing, multi-step wizards with SkipWhen, and modal FormService overlays.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Form.Core — 100% headless C#, zero UI dependency, NativeAOT compatible. Houses FormSchema, FormFieldSchema, FormStepSchema, FormController, and FormService.",
                            "Lux.Form — Multiplatform Razor Class Library providing BxFormEngine, BxFormPage, BxFormProvider, and AddLuxForm()."
                        ]),
                    new("kinds", "31 Supported Field Kinds",
                        Paragraphs: ["FormFieldKind supports text, email, password (with live requirement checklist), number, textarea, select, multi-select, autocomplete, checkbox, switch, radio, slider, date, time, datetime, color, tags, file, rating, one-time code, repeatable arrays, tabs, headings, dividers, and alerts."])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Form, register FormService, and mount BxFormProvider once for programmatic modal/drawer forms.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Form.Core   # Headless NativeAOT schema & FormController
                                dotnet add package Lux.Form        # BxFormEngine, BxFormPage & BxFormProvider
                                """)
                        ]),
                    new("di", "2. Register Services & Overlay Provider",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxForm();"),
                            new("MainLayout.razor", "<BxFormProvider />")
                        ])
                ]),
            new("concepts", "Core Concepts", "Guides",
                "Drive reactive forms, conditional validation, output transforms, dirty tracking, and multi-step wizards from one FormSchema.",
                [
                    new("schema", "Schema, Conditions & Output Transforms",
                        Paragraphs: ["Hidden fields (where Condition returns false) are automatically skipped during validation and omitted from BuildOutput()."],
                        CodeSamples:
                        [
                            new("ProfileSchema.cs", """
                                var schema = new FormSchema
                                {
                                    FormKey = "profile",
                                    Title = "User Profile",
                                    Fields =
                                    [
                                        new FormFieldSchema { Key = "name", Label = "Full Name", Required = true, DirtyCheck = true, TransformOutput = v => v?.ToString()?.Trim() },
                                        new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, Min = 18, Max = 120, DefaultValue = 25.0 },
                                        new FormFieldSchema { Key = "company", Label = "Company", Required = true, Condition = f => f.GetValue<double>("age") >= 21 }
                                    ]
                                };
                                var controller = new FormController(schema, new Dictionary<string, object?> { ["name"] = "Alice" });
                                """)
                        ]),
                    new("wizard", "Multi-Step Wizards with SkipWhen",
                        CodeSamples:
                        [
                            new("OnboardingWizard.cs", """
                                var wizard = new FormSchema
                                {
                                    FormKey = "onboarding",
                                    Title = "Onboarding",
                                    Steps =
                                    [
                                        new FormStepSchema { Key = "account", Title = "Account", Fields = [new() { Key = "plan", DefaultValue = "free" }] },
                                        new FormStepSchema { Key = "billing", Title = "Billing", SkipWhen = f => f.GetValue<string>("plan") == "free", Fields = [new() { Key = "vat", Required = true }] }
                                    ]
                                };
                                """)
                        ]),
                    new("render", "Rendering with BxFormEngine & BxFormPage",
                        CodeSamples:
                        [
                            new("ProfilePage.razor", """
                                <BxFormEngine Form="@_controller"
                                              SubmitLabel="Save profile"
                                              ShowReset="true"
                                              OnSubmit="@HandleSubmit" />
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Form", "Lux.Form.Core", "Lux.Form", "builder.Services.AddLuxForm();",
                "var controller = new FormController(schema); await controller.SubmitAsync();"),
            new("recipes", "Recipes & Testing", "Guides",
                "Password requirement rules, dirty re-baselining, and headless unit testing.",
                [
                    new("password", "Password Strength Checklist",
                        CodeSamples:
                        [
                            new("PasswordField.cs", """
                                new FormFieldSchema
                                {
                                    Key = "password",
                                    Label = "Password",
                                    Kind = FormFieldKind.Password,
                                    PasswordRequirements =
                                    [
                                        new FormPasswordRequirement("len", "At least 8 characters", s => s.Length >= 8)
                                    ]
                                }
                                """)
                        ]),
                    new("testing", "Testing FormController & BxFormEngine",
                        CodeSamples:
                        [
                            new("FormTests.cs", """
                                controller.SetValue("age", 19.0, markTouched: true);
                                Assert.True(controller.IsDirty);
                                Assert.Contains("age", controller.DirtyPaths);
                                Assert.True(await controller.SubmitAsync());
                                Assert.False(controller.IsDirty); // Re-baselined after submit
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Form.Core and Lux.Form.",
                [],
                ApiTypes: [typeof(BxFormEngine), typeof(BxFormPage), typeof(BxFormProvider), typeof(FormController), typeof(FormSchema), typeof(FormFieldSchema)])
        ]);

    private static LibraryDocTree BuildDashboard() => new(
        "dashboard", "Lux.Dashboard", "#8b5cf6",
        [
            new("", "Introduction", "Getting Started",
                "Analytical dashboard orchestrator with 3-wave staged queries (Essential -> Background -> Deferred), global filters, leap-year comparison ranges, UTF-8 BOM CSV export, and 13 KPI blocks.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Dashboard.Core — 100% headless C#, NativeAOT compatible. Contains DashboardController, DashboardResource<T>, DashboardFilterControl, DashboardFormat, and all data models.",
                            "Lux.Dashboard — Multiplatform Razor Class Library shipping 13 visualization blocks and AddLuxDashboard()."
                        ]),
                    new("blocks", "13 Built-In Analytical Blocks",
                        TableHeaders: ["Category", "Components", "Highlights"],
                        TableRows:
                        [
                            new("Shell & Layout", "BxDashboardPage, BxDashboardGrid, BxDashboardCard", "View tabs with badges, filter toolbar, freshness & action menus"),
                            new("KPIs & Gauges", "BxDashboardStat, BxDashboardStats, BxDashboardGauge", "Sparklines, comparison deltas, goal progress bars & SVG arc gauge"),
                            new("Distributions", "BxDashboardBars, BxDashboardList, BxDashboardDetails", "Horizontal bars, ranked lists & key/value breakdowns"),
                            new("Tables & Feeds", "BxDashboardTable, BxDashboardChart, BxDashboardAlerts, BxDashboardFeed", "Sortable table with inline bars/deltas, CSV export, alerts & feeds")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Dashboard and register services.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Dashboard.Core   # Headless NativeAOT controller, filters & formatters
                                dotnet add package Lux.Dashboard        # 13 Blazor Web & .NET MAUI visualization blocks
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxDashboard();")
                        ])
                ]),
            new("concepts", "Core Concepts", "Guides",
                "Coordinate staged query loading, global filter reset states, smart number formatting, and CSV exports.",
                [
                    new("waves", "3-Wave Staged Query Orchestration",
                        Bullets:
                        [
                            "Essential — Runs first so top-line KPIs appear immediately.",
                            "Background — Runs after Essential completes for secondary charts and tables.",
                            "Deferred — Remains Idle until explicitly loaded (e.g., secondary tab)."
                        ],
                        CodeSamples:
                        [
                            new("DashboardSetup.cs", """
                                var controller = new DashboardController("analytics");
                                controller.AddView("overview", "Overview", badge: "3");
                                controller.AddFilter(new DashboardFilterControl("year", "Year",
                                    items: [new("2025", "2025"), new("2026", "2026")], defaultValues: ["2026"]));

                                var kpis = controller.RegisterResource(new DashboardResource<int>(
                                    "summary", LoadSummaryAsync, DashboardQueryStage.Essential));
                                var chart = controller.RegisterResource(new DashboardResource<double[]>(
                                    "trend", LoadTrendAsync, DashboardQueryStage.Background));
                                var audit = controller.RegisterResource(new DashboardResource<string>(
                                    "audit", LoadAuditAsync, DashboardQueryStage.Deferred));

                                await controller.RefreshAsync(); // Runs Essential -> Background; audit stays Idle
                                """)
                        ]),
                    new("layout", "Composing BxDashboardPage",
                        CodeSamples:
                        [
                            new("RevenuePage.razor", """
                                <BxDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
                                    <BxDashboardGrid Menu="true" Freshness="true">
                                        <BxDashboardStats Title="Executive KPIs" Items="@_stats" />
                                        <BxDashboardGauge Title="Q4 ARR Goal" Value="86" Max="100" Target="90"
                                                          Format="DashboardFormatPreset.Percent" />
                                        <BxDashboardBars Title="Channels" Items="@_bars"
                                                         Format="DashboardFormatPreset.Compact" />
                                    </BxDashboardGrid>
                                </BxDashboardPage>
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Dashboard", "Lux.Dashboard.Core", "Lux.Dashboard", "builder.Services.AddLuxDashboard();",
                "await dashboardController.RefreshAsync(); var csv = DashboardFormat.ExportCsv(table);"),
            new("recipes", "Recipes & Testing", "Guides",
                "Formatting presets, leap-year comparison windows, and localized CSV exports.",
                [
                    new("format", "DashboardFormat & RFC 4180 CSV Export",
                        CodeSamples:
                        [
                            new("Formatting.cs", """
                                DashboardFormat.Format(12600, DashboardFormatPreset.Compact, CultureInfo.InvariantCulture); // "12.6K"
                                DashboardFormat.Format(14.2, DashboardFormatPreset.Delta, CultureInfo.InvariantCulture);    // "+14.2%"
                                DashboardFormat.Format(0.57, DashboardFormatPreset.Ratio, CultureInfo.InvariantCulture);    // "57%"

                                // Leap-year safe comparison (Feb 29, 2024 -> Feb 28, 2023)
                                var prevYear = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);

                                // Locale-aware UTF-8 BOM CSV
                                var csv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Dashboard.Core and Lux.Dashboard.",
                [],
                ApiTypes: [typeof(BxDashboardPage), typeof(BxDashboardStats), typeof(BxDashboardStat), typeof(BxDashboardGauge), typeof(BxDashboardBars), typeof(BxDashboardTable), typeof(DashboardController)])
        ]);

    private static LibraryDocTree BuildDisplay() => new(
        "display", "Lux.Display", "#ec4899",
        [
            new("", "Introduction", "Getting Started",
                "19 read-only display blocks (BxDisplay), constrained Audio/Video players with clip bounds and play quotas, and a multi-format modal file previewer.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Display.Core — 100% headless C#, NativeAOT compatible. Houses FilePreviewService, MIME/extension detection, RFC 4180 CSV parser, and media progress/track models.",
                            "Lux.Display — Multiplatform Razor Class Library shipping BxDisplay, BxAudioPlayer, BxVideoPlayer, BxFilePreviewProvider, and AddLuxDisplay()."
                        ]),
                    new("kinds", "19 Semantic Display Kinds",
                        Paragraphs: ["Heading, Text, Alert (expandable details), Callout, Facts (with copy button & empty states), Stat (with delta & sparkline), Stats, Breakdown, Meter (with warning/error thresholds), Chart, Table, Diff (before/after), Records, People, Contacts, List (incremental Reveal More), Timeline, Date, Code (with Variant=\"secret\" masking), and Accordion."])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Display, register FilePreviewService, and mount BxFilePreviewProvider in your layout.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Display.Core   # Headless NativeAOT MIME detector & CSV parser
                                dotnet add package Lux.Display        # BxDisplay, BxAudioPlayer, BxVideoPlayer & Lightbox
                                """)
                        ]),
                    new("di", "2. Register Services & Lightbox Host",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxDisplay();"),
                            new("MainLayout.razor", "<BxFilePreviewProvider />")
                        ])
                ]),
            new("concepts", "Core Concepts", "Guides",
                "Display blocks, secret masking, assessment-grade media constraints, and programmatic file preview lightboxes.",
                [
                    new("display", "BxDisplay Blocks & Secret Masking",
                        CodeSamples:
                        [
                            new("DisplayBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Stat" Title="MRR" Value="€148,900"
                                           Delta="+14.2%" Trend="@([102.0, 118.0, 134.0, 148.9])" />

                                <BxDisplay Kind="DisplayKind.Code" Variant="secret"
                                           Title="API Secret" Value="sk_live_9876543210"
                                           VisibleChars="4" Copy="true" />

                                <BxDisplay Kind="DisplayKind.Meter" Title="Storage"
                                           MeterValue="92" MeterMax="100"
                                           WarningThreshold="75" ErrorThreshold="90" />
                                """)
                        ]),
                    new("media", "Constrained Audio & Video Players",
                        Paragraphs: ["Enforce Start/End clip slicing, MaxPlays play limits (firing OnLimitReached and locking controls), and Seekable=\"false\" guards."],
                        CodeSamples:
                        [
                            new("ExamAudio.razor", """
                                <BxAudioPlayer Src="/media/listening-test.mp3"
                                               Title="Listening Extract #1"
                                               Start="15" End="45"
                                               MaxPlays="2" Seekable="false"
                                               OnLimitReached="@HandleQuotaReached" />
                                """)
                        ]),
                    new("preview", "FilePreviewService & CSV Parser",
                        CodeSamples:
                        [
                            new("Preview.cs", """
                                filePreview.Open(
                                    new FilePreviewItem("architecture.png", "/img/sample-grid.png"),
                                    new FilePreviewItem("rates.csv", "/files/rates.csv"));

                                var parsedCsv = FilePreviewService.ParseCsv("Region,MRR\nEU,148900\nUS,192400");
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Display", "Lux.Display.Core", "Lux.Display", "builder.Services.AddLuxDisplay();",
                "var parsed = FilePreviewService.ParseCsv(rawCsvText);"),
            new("recipes", "Recipes & Testing", "Guides",
                "Testing secret toggle, threshold tones, and media play quotas with bUnit.",
                [
                    new("bunit", "Testing Secret Masking & Diff Blocks",
                        CodeSamples:
                        [
                            new("DisplayTests.cs", """
                                Services.AddLuxDisplay();
                                var cut = Render<BxDisplay>(p => p
                                    .Add(x => x.Kind, DisplayKind.Code)
                                    .Add(x => x.Variant, "secret")
                                    .Add(x => x.Value, "sk_live_9876543210")
                                    .Add(x => x.VisibleChars, 4));

                                Assert.Equal("••••••••3210", cut.Find("[data-slot=code-value]").TextContent);
                                cut.Find("[data-slot=code-secret-toggle]").Click();
                                Assert.Equal("sk_live_9876543210", cut.Find("[data-slot=code-value]").TextContent);
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Display.Core and Lux.Display.",
                [],
                ApiTypes: [typeof(BxDisplay), typeof(BxAudioPlayer), typeof(BxVideoPlayer), typeof(BxFilePreviewProvider), typeof(FilePreviewService)])
        ]);

    private static LibraryDocTree BuildSpreadsheet() => new(
        "spreadsheet", "Lux.Spreadsheet", "#14b8a6",
        [
            new("", "Introduction", "Getting Started",
                "Headless NativeAOT spreadsheet importer with 20-row header layout detection, NFD normalization, >=80% pass-rate column suggestions, unknown value questions, duplicate/upsert handling, inline edits, and batch retry.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Spreadsheet.Core — 100% headless C#, NativeAOT compatible. Houses SpreadsheetSchema, SpreadsheetColumnSchema, SpreadsheetRule, and SpreadsheetImporter.",
                            "Lux.Spreadsheet — Multiplatform Razor Class Library providing the 5-step BxSpreadsheetImport wizard and AddLuxSpreadsheet()."
                        ]),
                    new("pipeline", "The 5-Step Import Pipeline",
                        Bullets:
                        [
                            "1. Source & Layout Detection — CSV, TSV, or Excel clipboard paste; scans up to 20 rows to skip title banners and detect delimiter + header row.",
                            "2. NFD Normalization & >=80% Suggestions — Accent/marker-insensitive header matching plus sample-validated suggestions for unmapped columns.",
                            "3. Unknown Value Questions — Prompts users to map, keep, or discard unknown select values globally.",
                            "4. Reconciliation & Inline Review — Intra-file duplicate policies, ExistingRecords upsert detection, and inline EditCell.",
                            "5. Batch Submit & Retry Rejected — Submits valid rows in batches and isolates rejected rows for targeted retry."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Spreadsheet and register services.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Spreadsheet.Core   # Headless NativeAOT importer engine
                                dotnet add package Lux.Spreadsheet        # 5-step Blazor Web & .NET MAUI wizard
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxSpreadsheet();")
                        ])
                ]),
            new("concepts", "Core Concepts", "Guides",
                "Define column rules, aliases, duplicate/upsert policies, and drive the wizard.",
                [
                    new("schema", "Defining SpreadsheetSchema & Loading CSV/TSV",
                        CodeSamples:
                        [
                            new("RatesImporter.cs", """
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

                                var importer = new SpreadsheetImporter(schema);
                                importer.LoadText("Export 2026;;\nCode;Amount;Currency\nA1;12,50;€", "rates.csv");
                                """)
                        ]),
                    new("wizard", "Rendering BxSpreadsheetImport",
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
                "var importer = new SpreadsheetImporter(schema); importer.LoadText(csvText, \"data.csv\");"),
            new("recipes", "Recipes & Testing", "Guides",
                "Resolving unknown values, editing cells inline, and retrying rejected server batches.",
                [
                    new("reconcile", "Unknown Value Resolution & Batch Retry",
                        CodeSamples:
                        [
                            new("Reconcile.cs", """
                                importer.AnswerValue("level", "C1-Advanced",
                                    new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.Map, "B2", "B2"));
                                importer.EditCell(rowIndex: 1, fieldKey: "email", rawText: "fixed@example.com");

                                await importer.SubmitAsync();
                                if (importer.RejectedRows.Count > 0)
                                {
                                    await importer.RetryRejectedAsync();
                                }
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Spreadsheet.Core and Lux.Spreadsheet.",
                [],
                ApiTypes: [typeof(BxSpreadsheetImport), typeof(SpreadsheetImporter), typeof(SpreadsheetSchema), typeof(SpreadsheetColumnSchema)])
        ]);

    private static LibraryDocTree BuildAgent() => new(
        "agent", "Lux.Agent", "#6366f1",
        [
            new("", "Introduction", "Getting Started",
                "Semantic AI Agent Bridge (snapshot, query, act, observe), composable browser behaviors, WebMCP tool registration, automatic [redacted] secret masking, permission policies, and bounded UndoStack<T> — inspired by ui-sdk.dev.",
                [
                    new("architecture", "Two-Layer Architecture",
                        Bullets:
                        [
                            "Lux.Agent.Core — Zero-dependency, NativeAOT compatible. Houses AgentComponentSnapshot, AgentQueryRequest, AgentQueryMatch, AgentActStep, AgentActResult, ObservedChange, AgentToolDefinition, and UndoStack<T>.",
                            "Lux.Agent — Multiplatform Razor Class Library providing AgentBridgeService, AddLuxAgent(), and static web assets _content/Lux.Agent/lux-agent.js & lux-behaviors.js."
                        ]),
                    new("tools", "Three Standard Semantic Tools",
                        TableHeaders: ["Tool", "Mode", "Purpose"],
                        TableRows:
                        [
                            new("snapshot", "Read-only", "Lists up to 100 registered components, state, disabled/off-page flags, and actions (secrets auto-redacted)."),
                            new("query", "Read-only", "Finds controls accent-insensitively and lists select/filter choices without opening popups."),
                            new("act", "Consequential", "Runs semantic actions (setValue, toggle, clear, submit, setView, editCell) guarded by AllowPolicy.")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Agent, register AgentBridgeService, and optionally load the WebMCP browser script.",
                [
                    new("packages", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Agent.Core   # Zero-dependency NativeAOT models & UndoStack<T>
                                dotnet add package Lux.Agent        # AgentBridgeService + WebMCP & Behaviors JS
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxAgent();")
                        ])
                ]),
            new("concepts", "Core Concepts", "Guides",
                "Bridge forms, dashboards, and spreadsheets to AI agents with permission gates, privacy-first redaction, and WebMCP.",
                [
                    new("bridge", "Registering Controllers & Running Snapshot / Query / Act",
                        CodeSamples:
                        [
                            new("AgentBridgeUsage.cs", """
                                agent.RegisterForm(form, confirmSubmit: true);
                                agent.RegisterDashboard(dashboard);
                                agent.RegisterSpreadsheet(importer);

                                // Global permission gate for consequential actions
                                agent.AllowPolicy = (step, confirmRequired) =>
                                    Task.FromResult(!confirmRequired || userApproved);

                                // 1. Snapshot (Password & OneTimeCode fields return "[redacted]")
                                var snapshot = agent.Snapshot();

                                // 2. Query select options accent-insensitively without opening popups
                                var choices = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });

                                // 3. Execute semantic actions in sequence
                                var res = await agent.ActAsync([
                                    new AgentActStep("project name", "setValue", ["Atlas"]),
                                    new AgentActStep("region", "setValue", ["eu-west"])
                                ]);
                                """)
                        ]),
                    new("observe", "Unified Observe Stream & UndoStack<T>",
                        CodeSamples:
                        [
                            new("ObserveAndUndo.cs", """
                                using var sub = agent.Observe(change =>
                                    Console.WriteLine($"{change.Source}: {change.Label} {change.Previous} -> {change.Value}"));

                                var history = new UndoStack<string>("Initial", limit: 50);
                                history.Record("Draft v2");
                                history.Undo(); // Current == "Initial"
                                history.Redo(); // Current == "Draft v2"
                                """)
                        ]),
                    new("webmcp", "Browser WebMCP & Composable Behaviors",
                        Paragraphs: ["_content/Lux.Agent/lux-agent.js exposes window.Blux.agent and registers snapshot, query, and act on navigator.modelContext when supported by the browser."],
                        CodeSamples:
                        [
                            new("Browser.js", """
                                const snap = window.Blux.agent.snapshot();
                                const res  = await window.Blux.agent.act([
                                  { ref: 'Project name', action: 'setValue', args: ['Atlas'] }
                                ]);
                                const stop = window.Blux.agent.observe(evt => console.log(evt));
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Agent", "Lux.Agent.Core", "Lux.Agent", "builder.Services.AddLuxAgent();",
                "var history = new UndoStack<DocumentState>(initial, limit: 50);"),
            new("recipes", "Recipes & Testing", "Guides",
                "Verifying redaction, policy refusal, and semantic actions in xUnit.",
                [
                    new("testing", "Testing Secret Redaction & Policy Refusal",
                        CodeSamples:
                        [
                            new("AgentTests.cs", """
                                var snap = agent.Snapshot();
                                Assert.Equal(AgentBridgeService.RedactedValue, snap.First(s => s.Label == "API Secret").Value);

                                agent.AllowPolicy = (_, confirmRequired) => Task.FromResult(!confirmRequired);
                                var refused = await agent.ActAsync([new AgentActStep("Project Settings", "submit")]);
                                Assert.False(refused.Ok);
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API for Lux.Agent.Core and Lux.Agent.",
                [],
                ApiTypes: [typeof(AgentBridgeService), typeof(AgentComponentSnapshot), typeof(AgentQueryRequest), typeof(AgentActStep), typeof(AgentActResult), typeof(ObservedChange), typeof(UndoStack<object>)])
        ]);
}
