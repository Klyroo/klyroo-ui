using Blux.UI;

namespace Blux.Playground.Client.Docs;

public sealed record LuxProductComponentLink(
    string Name,
    string Description,
    string Href,
    string Layer);

public sealed record LuxProductInfo(
    string Slug,
    string Name,
    string ShortName,
    string Tagline,
    string HeroHeadline,
    string HeroAccent,
    string Description,
    string AccentHex,
    BxColor BadgeColor,
    string RepoName,
    string RepoPath,
    string CorePackage,
    string UiPackage,
    string DocsHref,
    IReadOnlyList<string> Platforms,
    IReadOnlyList<(string Title, string Description, string Tag)> Features,
    string CoreCodeTitle,
    string CoreCode,
    string UiCodeTitle,
    string UiCode,
    IReadOnlyList<LuxProductComponentLink> Components);

public static class LuxStackProducts
{
    public static IReadOnlyList<LuxProductInfo> All { get; } =
    [
        new(
            Slug: "ui",
            Name: "Lux.UI",
            ShortName: "UI",
            Tagline: "125+ zero-roundtrip primitives & composable DOM behaviors",
            HeroHeadline: "Accessible primitives",
            HeroAccent: "zero SignalR lag.",
            Description: "Complete design system for Blazor (Server, WebAssembly, Auto, Static SSR) and .NET MAUI Blazor Hybrid. Menus, dialogs, popovers, and tabs answer instantaneously at 60fps in the browser via native Web Components and composable behaviors (dialog, popup, rovingTabindex, formAssociated).",
            AccentHex: "#10b981",
            BadgeColor: BxColor.Success,
            RepoName: "lux-ui",
            RepoPath: "/home/gpoumarat/lux-ui",
            CorePackage: "Blux.UI.Build.Tasks",
            UiPackage: "Blux.UI",
            DocsHref: "/ui/docs",
            Platforms: ["Blazor Server", "Blazor WASM", "Blazor Auto / SSR", ".NET MAUI Hybrid (iOS, Android, Mac, Win)", "Android TV (D-Pad)"],
            Features:
            [
                ("Zero-Roundtrip Popovers & Dialogs", "Built on native HTML Popover and <dialog> APIs with light-dismiss, Escape handling, and top-layer stacking managed directly by the browser.", "DOM Native"),
                ("Composable Behaviors (ui-sdk.dev)", "Standalone JS primitives: dialog(), popup(), rovingTabindex(), formAssociated(), and dismissable() with zero external dependencies.", "Behaviors"),
                ("Tailwind v4 & Design Tokens", "A single stylesheet driven by semantic CSS variables (--blux-primary, --blux-bg, --blux-border), 12 built-in locales, and instant dark mode.", "Tailwind v4"),
                ("Mobile 44px Touch & TV D-Pad", "44px touch hit targets calibrated for iOS/Android, safe area insets, and directional D-pad focus rings for Android TV.", "Multiplatform")
            ],
            CoreCodeTitle: "js/behaviors.js · Composable DOM Behaviors",
            CoreCode: """
                import { dialog, rovingTabindex } from './behaviors.js';

                // Focus trap, Escape dismiss, aria-modal & return focus
                const modalCtrl = dialog(dialogElement, {
                  closeOnBackdrop: true,
                  onClose: () => dialogElement.dispatchEvent(new CustomEvent('bx-close'))
                });

                // Roving tabindex across toolbar or tabs (Arrow keys, Home/End)
                const tabsCtrl = rovingTabindex(tabListElement, {
                  selector: '[role="tab"]',
                  orientation: 'horizontal'
                });
                """,
            UiCodeTitle: "App.razor · Blazor Web & MAUI Hybrid",
            UiCode: """
                <BxModal Title="Deploy to production" Description="Zero round-trip open & close.">
                    <ChildContent>
                        <BxButton Label="Deploy now" Color="BxColor.Primary" />
                    </ChildContent>
                    <Body>
                        <BxFormField Label="Environment">
                            <BxSelect Items="@Environments" @bind-Value="_env" />
                        </BxFormField>
                    </Body>
                    <Footer>
                        <BxButton Label="Confirm" data-bx-close />
                    </Footer>
                </BxModal>
                """,
            Components:
            [
                new("BxButton", "Seven colors, six variants, loading and link states.", "/components/button", "UI"),
                new("BxModal", "Native <dialog> with focus trap, inert background and Escape.", "/components/modal", "UI"),
                new("BxSelect", "Keyboard typeahead and hidden input for native forms.", "/components/select", "UI"),
                new("BxTabs", "Arrow keys and automatic activation without SignalR round-trip.", "/components/tabs", "UI"),
                new("BxCommandPalette", "Full keyboard command launcher with groups and shortcuts.", "/components/command-palette", "UI"),
                new("BxChatMessages", "Streaming AI chat bubbles, reasoning and tool calls.", "/components/chat-messages", "UI")
            ]),

        new(
            Slug: "query",
            Name: "Lux.Query",
            ShortName: "Query",
            Tagline: "Headless NativeAOT Query AST, URL codecs, IQueryable applier, EF Core & QueryState",
            HeroHeadline: "Headless query engine",
            HeroAccent: "& reactive URL state.",
            Description: "Strongly typed, 100% NativeAOT query engine: composable filter AST (DataListSchema, DataListRequest, DataListFilterGroup), reversible URL query codecs, InMemory & IQueryable evaluators, Entity Framework Core async SQL executor (EfAsyncQueryExecutor), and reactive QueryState for Blazor and MAUI Hybrid.",
            AccentHex: "#ff4154",
            BadgeColor: BxColor.Error,
            RepoName: "lux-query",
            RepoPath: "/home/gpoumarat/lux-query",
            CorePackage: "Lux.Query.Core",
            UiPackage: "Lux.Query",
            DocsHref: "/query/docs",
            Platforms: ["Headless C# / NativeAOT", ".NET MAUI Native C#", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid", "Entity Framework Core"],
            Features:
            [
                ("Typed Schema & Filter AST", "Declare searchable, sortable, and filterable fields with DataListSchema.For<T>() with zero magic strings and full compile-time safety.", "Typed AST"),
                ("InMemory, IQueryable & EF Core", "Execute the exact same DataListRequest in memory (ExecuteInMemory), over LINQ IQueryable, or translated to async SQL via EfAsyncQueryExecutor.", "Any Backend"),
                ("URL Query Codecs & QueryState", "Reversible serialization of filters, sorts, and paging into the query string with browser history control and mobile WebView support.", "URL Sync"),
                ("100% Headless & NativeAOT", "Lux.Query.Core has zero UI dependencies: compile it in NativeAOT APIs, background workers, CLI tools, or native .NET MAUI ViewModels.", "NativeAOT")
            ],
            CoreCodeTitle: "Lux.Query.Core · Typed Schema & InMemory / IQueryable",
            CoreCode: """
                using Blux.UI.Tools.Query;

                var schema = DataListSchema.For<Product>(s => s
                    .RowKey(p => p.Id)
                    .Search(p => p.Name)
                    .Field(p => p.Price, f => f.Key("price").Sortable()));

                var request = new DataListRequest(
                    new DataListPaging.Offset(0, 25),
                    [new DataListSort("price", Descending: true)],
                    DataListFilterGroup.Empty,
                    new DataListSearch("Monitor"));

                var result = schema.ExecuteInMemory(products, request);
                """,
            UiCodeTitle: "Lux.Query · Reactive URL QueryState (Blazor & MAUI)",
            UiCode: """
                @inject QueryState Query

                @code {
                    // Two-way synchronized URL parameter (?page=3&sort=price:desc)
                    private void GoToPage(int page) => Query.Set("page", page.ToString());
                    private string CurrentPage => Query.Get("page") ?? "1";
                }
                """,
            Components:
            [
                new("DataListSchema", "Fluent typed schema builder for searchable, sortable and filterable fields.", "/query/docs", "Core"),
                new("DataListRequest", "Immutable paging (Offset/Cursor), sorting, filtering & search AST.", "/query/docs", "Core"),
                new("QueryCodecs", "Reversible URL search parameter serialization for complex filter trees.", "/query/docs", "Core"),
                new("EfAsyncQueryExecutor", "Translates DataListRequest directly to async SQL via Entity Framework Core.", "/query/docs", "Core"),
                new("QueryState", "Reactive URL state manager for Blazor Web and .NET MAUI BlazorWebView.", "/query/docs", "UI")
            ]),

        new(
            Slug: "table",
            Name: "Lux.Table",
            ShortName: "Table",
            Tagline: "Headless tree hierarchy engine & high-density enterprise BxDataList",
            HeroHeadline: "Supercharged tables",
            HeroAccent: "& headless datagrids.",
            Description: "The .NET counterpart to TanStack Table paired with Nuxt UI Tools polish: table and card grid layouts, NativeAOT tree hierarchy (DataListTree<TItem>), visual grouping, footer aggregations, 2px loading progress line, automatic Sort/Hide HeaderMenu, 60fps virtualization, and responsive mobile summary rows.",
            AccentHex: "#00d8ff",
            BadgeColor: BxColor.Info,
            RepoName: "lux-table",
            RepoPath: "/home/gpoumarat/lux-table",
            CorePackage: "Lux.Table.Core",
            UiPackage: "Lux.Table",
            DocsHref: "/table/docs",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid", "Entity Framework Core"],
            Features:
            [
                ("Headless Tree Hierarchy (NativeAOT)", "Lux.Table.Core builds and flattens hierarchical trees (DataListTree<TItem>) with depth tracking and continuation rails without any UI dependency.", "Headless Tree"),
                ("Nuxt UI Tools Polish Included", "2px indeterminate progress line during reloads, automatic Sort Asc/Desc/Hide HeaderMenu, adaptive skeletons, and first-use EmptyState CTAs.", "UX Polish"),
                ("Grouping, Summaries & Pinning", "Visual row grouping (DataListGroupBy), footer Sum/Avg/Min/Max aggregations, column resizing, and left/right sticky column pinning.", "Enterprise Grid"),
                ("Responsive Summary Rows on Phone", "Automatically switches to scannable summary rows (DataListRowSummary) below 768px on iOS and Android phones.", "Mobile Ready")
            ],
            CoreCodeTitle: "Lux.Table.Core · Headless DataListTree<T> (NativeAOT)",
            CoreCode: """
                using Blux.UI.Tools;

                var tree = new DataListTree<Employee>
                {
                    Children = e => e.Reports,
                    DefaultExpandedDepth = 1
                };

                var (roots, allNodes) = tree.BuildTree(employees, schema);
                // Every node carries Depth, Parent, IsLast & AncestorContinuations
                """,
            UiCodeTitle: "Lux.Table · BxDataList & BxDataListTable",
            UiCode: """
                <BxDataList TItem="Employee" Schema="@_schema" Items="@_employees" UrlSync="false">
                    <BxDataListTable TItem="Employee"
                                     HeaderMenu="true"
                                     Summaries="@(new DataListSummaries<Employee>())">
                        <BxDataListColumn TItem="Employee" Field="name" Title="Name" />
                        <BxDataListColumn TItem="Employee" Field="salary" Title="Salary"
                                          Summary="DataListSummaryKind.Sum" />
                    </BxDataListTable>
                </BxDataList>
                """,
            Components:
            [
                new("DataListTree<TItem>", "Pure headless NativeAOT hierarchy builder with rail continuations & expansion.", "/table/docs/tree", "Core"),
                new("BxDataList<TItem>", "Full-featured enterprise datagrid with table, grid, filter bar & pagination.", "/table/docs/data-list", "UI"),
                new("BxDataListTable<TItem>", "High-density table with HeaderMenu, summaries, tree view and column resizing.", "/table/docs/columns", "UI"),
                new("DataListRowSummary<TItem>", "Automatic mobile summary-row presentation below the md breakpoint.", "/table/docs/mobile-summary", "UI")
            ]),

        new(
            Slug: "form",
            Name: "Lux.Form",
            ShortName: "Form",
            Tagline: "31-kind schema-driven form engine, reactive conditions, dirty diffing & wizards",
            HeroHeadline: "Schema-driven forms",
            HeroAccent: "without boilerplate.",
            Description: "Declarative, 100% NativeAOT form engine in Lux.Form.Core: 31 field kinds (FormFieldKind), reactive conditional visibility, dirty/touched diffing, output transforms, multi-step wizards with SkipWhen, sectioned FormPage layouts, and programmatic modal/drawer overlays via FormService.",
            AccentHex: "#f59e0b",
            BadgeColor: BxColor.Warning,
            RepoName: "lux-form",
            RepoPath: "/home/gpoumarat/lux-form",
            CorePackage: "Lux.Form.Core",
            UiPackage: "Lux.Form",
            DocsHref: "/form/docs",
            Platforms: ["Headless C# / NativeAOT", ".NET MAUI Native C#", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid"],
            Features:
            [
                ("31 Declarative Field Kinds", "Text, Password (with requirement checklist), Number, Select, AutoComplete, Checkbox, Switch, Slider, Repeatable Arrays, and Tabs driven by FormSchema.", "31 Field Kinds"),
                ("Reactive Conditions & Transforms", "Evaluates Condition = f => ... live, automatically skips hidden fields during validation and output building, and applies TransformOutput on submit.", "Reactive Engine"),
                ("Multi-Step Wizards with SkipWhen", "Split complex flows into FormStepSchema with conditional SkipWhen branching, per-step validation, and automatic dirty re-baselining after submit.", "Wizard Flow"),
                ("FormPage & Overlay FormService", "Sectioned pages with completion indicators (BxFormPage) and programmatic modal/drawer form hosts via FormService + BxFormProvider.", "Overlay Host")
            ],
            CoreCodeTitle: "Lux.Form.Core · Headless FormSchema & FormController",
            CoreCode: """
                using Blux.UI.Tools;

                var schema = new FormSchema
                {
                    FormKey = "profile",
                    Title = "User Profile",
                    Fields =
                    [
                        new FormFieldSchema { Key = "name", Label = "Full Name", Required = true, TransformOutput = v => v?.ToString()?.Trim() },
                        new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, DefaultValue = 25.0 },
                        new FormFieldSchema { Key = "company", Label = "Company", Required = true, Condition = f => f.GetValue<double>("age") >= 21 }
                    ]
                };

                var form = new FormController(schema, new Dictionary<string, object?> { ["name"] = "Alice" });
                var ok = await form.SubmitAsync();
                """,
            UiCodeTitle: "Lux.Form · BxFormEngine & BxFormPage",
            UiCode: """
                <BxFormEngine Form="@_form"
                              SubmitLabel="Save profile"
                              ShowReset="true"
                              OnSubmit="@HandleSubmit" />
                """,
            Components:
            [
                new("FormSchema", "Declarative schema with 31 field kinds, steps, sections, rules & transforms.", "/form/docs", "Core"),
                new("FormController", "Headless NativeAOT state machine for values, errors, dirty paths & steps.", "/form/docs", "Core"),
                new("FormService", "Programmatic overlay service to open forms in modals or drawers.", "/form/docs", "Core"),
                new("BxFormEngine", "Automatic Blazor/MAUI renderer for FormSchema & multi-step wizards.", "/form/docs", "UI"),
                new("BxFormPage", "Sectioned form page layout with live completion & dirty status.", "/form/docs", "UI")
            ]),

        new(
            Slug: "dashboard",
            Name: "Lux.Dashboard",
            ShortName: "Dashboard",
            Tagline: "Staged 3-wave analytical queries, global filters, comparison ranges & 13 KPI blocks",
            HeroHeadline: "Analytical dashboards",
            HeroAccent: "staged for speed.",
            Description: "Analytical dashboard orchestrator with 3-wave staged query loading (Essential -> Background -> Deferred), reactive global filter toolbar and views, leap-year-safe comparison date ranges, RFC 4180 UTF-8 BOM CSV export, and 13 ready-to-use KPI & visualization blocks.",
            AccentHex: "#8b5cf6",
            BadgeColor: BxColor.Primary,
            RepoName: "lux-dashboard",
            RepoPath: "/home/gpoumarat/lux-dashboard",
            CorePackage: "Lux.Dashboard.Core",
            UiPackage: "Lux.Dashboard",
            DocsHref: "/dashboard/docs",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid (Tablet, Desktop, Mobile)"],
            Features:
            [
                ("3-Wave Staged Query Orchestration", "DashboardController runs Essential KPIs first, Background charts second, and keeps Deferred resources idle until explicitly requested.", "Staged Waves"),
                ("Global Filters, Presets & Views", "Synchronized filters (DashboardFilterControl) with one-click Reset detection and tabbed DashboardViewMeta views with live count badges.", "Reactive Filters"),
                ("13 Analytical Visualization Blocks", "BxDashboardPage, Grid, Card, Stat, Stats, SVG Gauge, Details, Bars, List, sortable Table with CSV export, Alerts, Feed, and Chart.", "13 Widgets"),
                ("Smart Formatters & CSV Export", "DashboardFormat formats Compact (12.6K), Delta (+14.2%), Points (+2.1 pts), Ratio (57%), leap-year date ranges, and locale-aware UTF-8 BOM CSV.", "Analytics Core")
            ],
            CoreCodeTitle: "Lux.Dashboard.Core · Staged DashboardController",
            CoreCode: """
                using Blux.UI.Tools;

                var controller = new DashboardController("analytics");
                controller.AddView("overview", "Overview", badge: "3");
                controller.AddFilter(new DashboardFilterControl("year", "Year",
                    items: [new("2025", "2025"), new("2026", "2026")], defaultValues: ["2026"]));

                var kpis = controller.RegisterResource(new DashboardResource<int>(
                    "summary", LoadSummaryAsync, DashboardQueryStage.Essential));
                var chart = controller.RegisterResource(new DashboardResource<double[]>(
                    "trend", LoadTrendAsync, DashboardQueryStage.Background));

                await controller.RefreshAsync(); // Runs Essential -> Background
                """,
            UiCodeTitle: "Lux.Dashboard · BxDashboardPage & KPI Widgets",
            UiCode: """
                <BxDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
                    <BxDashboardGrid Menu="true" Freshness="true">
                        <BxDashboardStats Title="Key Metrics" Items="@_statItems" />
                        <BxDashboardGauge Title="ARR Goal" Value="84" Max="100" Target="90"
                                          Format="DashboardFormatPreset.Percent" />
                        <BxDashboardBars Title="Channels" Items="@_barItems"
                                         Format="DashboardFormatPreset.Compact" />
                    </BxDashboardGrid>
                </BxDashboardPage>
                """,
            Components:
            [
                new("DashboardController", "Staged 3-wave query coordinator (Essential/Background/Deferred), filters & views.", "/dashboard/docs", "Core"),
                new("DashboardFormat", "Compact/Delta/Points/Ratio formatters, leap-year ranges & BOM CSV exporter.", "/dashboard/docs", "Core"),
                new("BxDashboardPage", "Full analytical dashboard shell with filter toolbar, views & refresh state.", "/dashboard/docs", "UI"),
                new("BxDashboardStats", "Multi-KPI summary strip with delta badges, progress and status pills.", "/dashboard/docs", "UI"),
                new("BxDashboardGauge", "SVG circular gauge with target marker and formatted center value.", "/dashboard/docs", "UI"),
                new("BxDashboardTable", "Sortable analytical table block with inline bars, deltas & CSV export.", "/dashboard/docs", "UI")
            ]),

        new(
            Slug: "display",
            Name: "Lux.Display",
            ShortName: "Display",
            Tagline: "19 read-only display blocks, constrained audio/video players & modal file preview",
            HeroHeadline: "Rich display blocks",
            HeroAccent: "& constrained media.",
            Description: "19 read-only presentation blocks inside <BxDisplay> (Heading, Alert, Facts, Stat with sparkline, Breakdown, Meter with thresholds, Diff, masked Code/Secret, Timeline, Accordion…), constrained Audio/Video players with clip Start/End bounds and MaxPlays quotas, plus a modal multi-format file previewer.",
            AccentHex: "#ec4899",
            BadgeColor: BxColor.Secondary,
            RepoName: "lux-display",
            RepoPath: "/home/gpoumarat/lux-display",
            CorePackage: "Lux.Display.Core",
            UiPackage: "Lux.Display",
            DocsHref: "/display/docs",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid"],
            Features:
            [
                ("19 Read-Only Display Blocks", "Heading, Text, expandable Alert, Callout, copyable Facts, Stat with sparkline, Breakdown, threshold Meter, Diff before/after, Timeline, and Secret Code.", "19 Blocks"),
                ("Constrained Audio & Video Players", "Supports Start/End clip slicing, MaxPlays play quotas (ideal for exams and language assessments), Seekable=false locking, and subtitle tracks.", "Media Quotas"),
                ("Multi-Format FilePreviewService", "Headless MIME/extension detection in .Core, built-in RFC 4180 CSV parser, and zoomable modal lightbox for images, PDFs, audio, video, and CSV.", "File Lightbox"),
                ("Secret Masking & One-Click Reveal", "DisplayKind.Code with Variant=\"secret\" automatically masks API keys (••••••••3210) with Reveal/Hide toggle and clipboard copy.", "Security UX")
            ],
            CoreCodeTitle: "Lux.Display.Core · FilePreviewService & CSV Parser",
            CoreCode: """
                using Blux.UI.Tools;

                // Headless MIME/extension kind detection & modal preview trigger
                filePreview.Open(
                    new FilePreviewItem("spec.pdf", "/files/spec.pdf"),
                    new FilePreviewItem("metrics.csv", "/files/metrics.csv"));

                // Built-in RFC 4180 CSV parser for instant preview tables
                var csv = FilePreviewService.ParseCsv("Region,MRR\nEurope,148900\nUS,192400");
                """,
            UiCodeTitle: "Lux.Display · BxDisplay & BxAudioPlayer",
            UiCode: """
                <BxDisplay Kind="DisplayKind.Stat" Title="MRR" Value="€148,900"
                           Delta="+14.2%" Trend="@([102, 118, 134, 149])" />

                <BxDisplay Kind="DisplayKind.Code" Variant="secret"
                           Value="sk_live_9876543210" VisibleChars="4" Copy="true" />

                <BxAudioPlayer Src="/media/listening.mp3" Title="Listening Extract #1"
                               Start="15" End="45" MaxPlays="2" Seekable="false" />
                """,
            Components:
            [
                new("FilePreviewService", "Headless MIME detector, RFC 4180 CSV parser and lightbox queue state.", "/display/docs", "Core"),
                new("BxDisplay", "19-kind read-only block component (Stat, Facts, Meter, Diff, Code/Secret, Timeline).", "/display/docs", "UI"),
                new("BxAudioPlayer", "Constrained audio player with Start/End clip bounds, MaxPlays & Seekable guard.", "/display/docs", "UI"),
                new("BxVideoPlayer", "Constrained video player with tracks, clip bounds and play-limit locking.", "/display/docs", "UI"),
                new("BxFilePreviewProvider", "Modal lightbox host for images, PDFs, audio, video and parsed CSV files.", "/display/docs", "UI")
            ]),

        new(
            Slug: "spreadsheet",
            Name: "Lux.Spreadsheet",
            ShortName: "Spreadsheet",
            Tagline: "Headless CSV/TSV importer, 20-row header detection, 80%-pass suggestions & batch retry",
            HeroHeadline: "Intelligent CSV import",
            HeroAccent: "& row reconciliation.",
            Description: "100% NativeAOT spreadsheet import engine in Lux.Spreadsheet.Core: 20-row heuristic header layout detection, NFD accent-insensitive normalization, ≥80% pass-rate column suggestions, unknown value resolution questions, duplicate & existing record upsert policies, inline cell edits, and batched submission with rejected-row retry.",
            AccentHex: "#14b8a6",
            BadgeColor: BxColor.Success,
            RepoName: "lux-spreadsheet",
            RepoPath: "/home/gpoumarat/lux-spreadsheet",
            CorePackage: "Lux.Spreadsheet.Core",
            UiPackage: "Lux.Spreadsheet",
            DocsHref: "/spreadsheet/docs",
            Platforms: ["Headless C# / NativeAOT", ".NET MAUI Native C#", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid"],
            Features:
            [
                ("20-Row Heuristic Header Detection", "Automatically skips export banners and title rows across the first 20 rows of a CSV/TSV or clipboard paste to locate the real header row.", "Auto Header"),
                ("NFD Normalization & 80%-Pass Matching", "Normalizes headers (accents, *, (required)) and suggests unmapped columns only when ≥80% of sample values pass type parsing and validation rules.", "Smart Mapping"),
                ("Unknown Value Questions & Upsert", "Surfaces unknown select values as explicit mapping questions and resolves intra-file duplicates or updates against existing database records.", "Reconciliation"),
                ("Batch Submit & Retry Rejected Rows", "Submits validated rows in configurable batches and isolates server-rejected rows for inline correction and targeted retry.", "Batch Retry")
            ],
            CoreCodeTitle: "Lux.Spreadsheet.Core · Headless SpreadsheetImporter",
            CoreCode: """
                using Blux.UI.Tools;

                var schema = new SpreadsheetSchema
                {
                    Key = "rates",
                    Columns =
                    [
                        new SpreadsheetColumnSchema { Key = "code", Label = "Code", Required = true },
                        new SpreadsheetColumnSchema { Key = "price", Label = "Unit Price", Kind = SpreadsheetColumnKind.Number, DecimalSeparator = ',', Rules = [SpreadsheetRule.Min(0)] },
                        new SpreadsheetColumnSchema { Key = "currency", Label = "Currency", Kind = SpreadsheetColumnKind.Select, Options = [new("EUR", "Euro", ["€", "EUR"]), new("USD", "US Dollar", ["$", "USD"])] }
                    ]
                };

                var importer = new SpreadsheetImporter(schema);
                importer.LoadText(csvText, "rates.csv"); // Auto-detects header row & 80%-pass suggestions
                """,
            UiCodeTitle: "Lux.Spreadsheet · BxSpreadsheetImport Wizard",
            UiCode: """
                <BxSpreadsheetImport Importer="@_importer"
                                     Title="Import Product Rates"
                                     Wizard="true" />
                """,
            Components:
            [
                new("SpreadsheetSchema", "Column definitions, rules, options, duplicate policies & existing record lookup.", "/spreadsheet/docs", "Core"),
                new("SpreadsheetImporter", "Headless NativeAOT parser, layout detector, column suggester & batch runner.", "/spreadsheet/docs", "Core"),
                new("BxSpreadsheetImport", "Interactive 5-step Blazor/MAUI import wizard with inline cell editing.", "/spreadsheet/docs", "UI")
            ]),

        new(
            Slug: "agent",
            Name: "Lux.Agent",
            ShortName: "Agent",
            Tagline: "Semantic AI Agent Bridge (snapshot/query/act), WebMCP tools, observe stream & UndoStack",
            HeroHeadline: "AI-ready interfaces",
            HeroAccent: "& semantic WebMCP.",
            Description: "Inspired by ui-sdk.dev: makes Lux.Form, Lux.Dashboard, and Lux.Spreadsheet immediately inspectable and operable by AI agents without brittle CSS selectors. Provides semantic Snapshot/Query/Act, global permission policies, automatic [redacted] secret masking, WebMCP browser assets, and UndoStack<T>.",
            AccentHex: "#6366f1",
            BadgeColor: BxColor.Info,
            RepoName: "lux-agent",
            RepoPath: "/home/gpoumarat/lux-agent",
            CorePackage: "Lux.Agent.Core",
            UiPackage: "Lux.Agent",
            DocsHref: "/agent/docs",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid", "Browser WebMCP (navigator.modelContext)"],
            Features:
            [
                ("Semantic Snapshot, Query & Act", "Exposes up to 100 controls by stable ref (c1, c2…), queries select/filter choices without opening popups, and runs semantic actions (setValue, toggle, submit).", "Semantic Bridge"),
                ("Permission Gate & Confirmation Policy", "Global AllowPolicy or per-call gate with ConfirmRequired flags to approve or block consequential actions before execution.", "Policy Guard"),
                ("Observe Stream & Secret Redaction", "Unified ObservedChange stream (Agent/User source) where Password and OneTimeCode fields are always redacted as [redacted].", "Privacy First"),
                ("WebMCP JS Assets & UndoStack<T>", "Ships _content/Lux.Agent/lux-agent.js + lux-behaviors.js for WebMCP and a bounded generic UndoStack<T> in Lux.Agent.Core (NativeAOT).", "WebMCP + Undo")
            ],
            CoreCodeTitle: "Lux.Agent · AgentBridgeService & UndoStack<T>",
            CoreCode: """
                using Blux.UI.Tools;

                agent.RegisterForm(form, confirmSubmit: true);
                agent.RegisterDashboard(dashboard);

                // Inspect choices accent-insensitively without opening any popup
                var choices = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });

                // Execute semantic steps with policy checks & automatic secret redaction
                var result = await agent.ActAsync([
                    new AgentActStep("project name", "setValue", ["Atlas"]),
                    new AgentActStep("region", "setValue", ["eu-west"])
                ]);
                """,
            UiCodeTitle: "wwwroot/lux-agent.js · Browser WebMCP Bridge",
            UiCode: """
                // Served automatically via _content/Lux.Agent/lux-agent.js
                const snap = window.Blux.agent.snapshot();
                const res  = await window.Blux.agent.act([
                  { ref: 'Project name', action: 'setValue', args: ['Atlas'] }
                ]);

                // Privacy-preserving observation stream (passwords auto-masked as "[redacted]")
                const stop = window.Blux.agent.observe(evt => console.log(evt));
                """,
            Components:
            [
                new("AgentComponentSnapshot", "Compact NativeAOT semantic snapshot model with state, actions & flags.", "/agent/docs", "Core"),
                new("UndoStack<T>", "Generic bounded Undo/Redo stack with labels and state notifications.", "/agent/docs", "Core"),
                new("AgentBridgeService", "Connects FormController, DashboardController & SpreadsheetImporter to AI agents.", "/agent/docs", "UI"),
                new("lux-agent.js & lux-behaviors.js", "Static web assets for WebMCP (navigator.modelContext) and composable DOM behaviors.", "/agent/docs", "UI")
            ])
    ];

    public static LuxProductInfo? Find(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : All.FirstOrDefault(p => string.Equals(p.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Resolves which LuxStack library owns the current route path, or null on the LuxStack Hub / Templates.</summary>
    public static LuxProductInfo? ResolveFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var clean = path.Split('?', '#')[0].Trim('/');
        if (clean.Length == 0 || clean is "stack") return null;
        if (clean == "templates") return Find("ui");

        var segs = clean.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length == 0) return null;

        if (segs[0].Equals("stack", StringComparison.OrdinalIgnoreCase) && segs.Length >= 2)
        {
            return Find(segs[1]);
        }
        if (segs[0].Equals("docs", StringComparison.OrdinalIgnoreCase))
        {
            if (segs.Length >= 2 && segs[1].StartsWith("lux-", StringComparison.OrdinalIgnoreCase))
            {
                return Find(segs[1]["lux-".Length..]);
            }
            return Find("ui");
        }
        if (segs[0].Equals("components", StringComparison.OrdinalIgnoreCase)
            || (segs.Length >= 2 && segs[1].Equals("components", StringComparison.OrdinalIgnoreCase)))
        {
            return Find("ui");
        }
        return Find(segs[0]);
    }
}
