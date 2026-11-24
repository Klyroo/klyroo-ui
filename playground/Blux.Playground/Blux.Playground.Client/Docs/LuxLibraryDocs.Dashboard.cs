using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildDashboard() => new(
        "dashboard", "Lux.Dashboard", "#8b5cf6",
        [
            new("", "Introduction", "Getting Started",
                "Analytical dashboard orchestrator with 3-wave staged queries (Essential -> Background -> Deferred), global filters & presets, leap-year comparison ranges, localized UTF-8 BOM CSV export, and 13 visualization blocks.",
                [
                    new("overview", "What is Lux.Dashboard?",
                        Paragraphs:
                        [
                            "Analytical dashboards often suffer from slow waterfalls or blank full-page spinners. Lux.Dashboard puts a 100% NativeAOT orchestrator (DashboardController) in Lux.Dashboard.Core that executes data resources in 3 priority waves, synchronizes global filters and views, formats numbers/deltas consistently, and feeds 13 ready-to-use Razor visualization blocks."
                        ],
                        LiveDemoKey: "dashboard-kpi",
                        LiveDemoTitle: "Lux.Dashboard · Live Executive Stats, SVG Gauge & Bars"),
                    new("layers", "Two-Layer Package Split",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Dashboard.Core", "DashboardController, DashboardResource<TData>, DashboardFilterControl, DashboardViewMeta, DashboardFormat, all analytical record models", "Headless C# · IsAotCompatible=true"),
                            new("Lux.Dashboard", "13 analytical blocks: Page, Grid, Card, Stat, Stats, Gauge, Details, Bars, List, Table, Alerts, Feed, Chart", "browser, ios, android, maccatalyst, windows")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Dashboard and register services.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Dashboard.Core   # Headless NativeAOT orchestrator & formatters
                                dotnet add package Lux.Dashboard        # 13 Blazor Web & .NET MAUI visualization blocks
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxDashboard();")])
                ]),
            new("staged-queries", "Staged 3-Wave Queries", "Orchestration",
                "Prioritize critical KPIs, stream secondary charts second, and keep heavy resources deferred until explicitly activated.",
                [
                    new("stages", "Stages, Resource Lifecycle & Block Phases",
                        TableHeaders: ["Enum", "Exact Values", "Behavior"],
                        TableRows:
                        [
                            new("DashboardQueryStage", "Essential, Background, Deferred", "RefreshAsync() awaits Essential first, then Background; Deferred stays Idle until ActivateAsync()"),
                            new("DashboardResourceState", "Idle, Loading, Ready, Error, Disabled", "Tracked per DashboardResource<TData> and aggregated via DashboardController.CombineStates()"),
                            new("DashboardBlockPhase", "Idle, Loading, Error, Empty, Content", "Determines whether each card renders its skeleton, error state, empty state, or data")
                        ]),
                    new("example", "Orchestrating Waves & Auto-Refresh",
                        Paragraphs: ["DashboardResource<TData> tracks Data, Error, Refreshing, Fetching, UpdatedAt, and Active. Set controller.AutoRefresh (seconds) to enable periodic background refreshes while preserving displayed data during reload."],
                        CodeSamples:
                        [
                            new("StagedOrchestration.cs", """
                                var dashboard = new DashboardController("revenue") { AutoRefresh = 60 };

                                var kpis = dashboard.RegisterResource(new DashboardResource<KpiSummary>(
                                    "kpis", LoadKpisAsync, DashboardQueryStage.Essential));
                                var channels = dashboard.RegisterResource(new DashboardResource<IReadOnlyList<DashboardBarItem>>(
                                    "channels", LoadChannelsAsync, DashboardQueryStage.Background));
                                var audit = dashboard.RegisterResource(new DashboardResource<DashboardDataTable>(
                                    "audit", LoadHeavyAuditAsync, DashboardQueryStage.Deferred));

                                // Wave 1: kpis -> Wave 2: channels; audit remains Idle
                                await dashboard.RefreshAsync();

                                // Later when user opens the Audit tab:
                                await audit.ActivateAsync();
                                """)
                        ])
                ]),
            new("filters-and-views", "Global Filters, Presets & Views", "Orchestration",
                "Coordinate 10 filter kinds, quick presets, automatic Reset state detection, and badged dashboard views.",
                [
                    new("kinds", "All 10 DashboardFilterKind Values",
                        TableHeaders: ["Category", "Exact DashboardFilterKind Values", "Usage"],
                        TableRows:
                        [
                            new("Scalar & Toggle", "String, Number, Boolean", "Free-text search, numeric thresholds, include/exclude flags"),
                            new("Temporal", "Date, DateRange, Comparison", "Period selectors paired with Previous / Year comparison mode"),
                            new("Dimensions", "Enum, Options, Remote, Custom", "Single/multi-select dimensions, searchable remote lists & custom controls")
                        ]),
                    new("controls", "DashboardFilterControl Features",
                        Bullets:
                        [
                            "Multiple, Searchable, and Max selection limits with live FilteredItems matching.",
                            "Presets (DashboardFilterPreset) applied in one click via filter.ApplyPreset(preset).",
                            "Headless = true hides a filter from the visible toolbar while still participating in state & resets.",
                            "controller.Filtered flips to true whenever any filter differs from its defaultValues, exposing the Reset toolbar button."
                        ],
                        CodeSamples:
                        [
                            new("FiltersAndViews.cs", """
                                dashboard.AddView("overview", "Overview", badge: "3");
                                dashboard.AddView("cohorts", "Cohorts");

                                var region = dashboard.AddFilter(new DashboardFilterControl(
                                    "region", "Region",
                                    items: [new("eu", "Europe"), new("us", "North America"), new("ap", "Asia-Pacific")],
                                    defaultValues: ["eu"])
                                {
                                    Multiple = true,
                                    Searchable = true
                                });

                                region.Toggle("us");
                                Assert.True(dashboard.Filtered);
                                dashboard.ResetFilters();
                                Assert.False(dashboard.Filtered);
                                """)
                        ])
                ]),
            new("formatting-and-csv", "Formatting, Comparison Ranges & CSV", "Orchestration",
                "Consistent null/NaN-safe formatting, currency & relative time helpers, leap-year comparison ranges, and UTF-8 BOM CSV exports.",
                [
                    new("presets", "All 9 DashboardFormatPreset Values & Helpers",
                        TableHeaders: ["Preset / Method", "Sample Input", "Output (InvariantCulture)"],
                        TableRows:
                        [
                            new("Number / Integer / Decimal", "12600 / 12.345", "12,600 / 12 / 12.35"),
                            new("Compact", "12600 / 2400000", "12.6K / 2.4M"),
                            new("Percent / Ratio", "86 / 0.57", "86% / 57%"),
                            new("Delta / Points / Signed", "14.2 / 2.1 / 1.0", "+14.2% / +2.1 pts / +1 pt"),
                            new("FormatCurrency / FormatRelativeTime", "148900, \"€\" / 5m ago", "€148,900 / 5m ago"),
                            new("Null / NaN Guard", "null or double.NaN", "\"—\" (em-dash placeholder)")
                        ]),
                    new("ranges-csv", "Leap-Year Comparison Ranges & RFC 4180 CSV Export",
                        Paragraphs:
                        [
                            "DashboardFormat.ResolveComparisonRange() handles DashboardComparisonMode.Previous (equal-length preceding window) and DashboardComparisonMode.Year (exact year-over-year shift clamping Feb 29 leap years safely to Feb 28).",
                            "DashboardFormat.ExportCsv() emits a UTF-8 BOM (\uFEFF) so Excel opens accented characters properly, uses semicolon delimiters for European cultures (e.g. fr-FR) and comma for en-US/Invariant, quotes strings safely, and leaves numeric columns unquoted."
                        ],
                        CodeSamples:
                        [
                            new("FormatAndCsv.cs", """
                                var leapStart = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
                                var leapEnd   = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero);
                                var yearAgo   = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);
                                // yearAgo.Value.End == 2023-02-28

                                var fileName = DashboardFormat.ResolveFileName("Consommation mensuelle"); // "consommation-mensuelle.csv"
                                var csv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
                                """)
                        ])
                ]),
            new("layout-blocks", "Page, Grid & Card", "Visualization Blocks",
                "Build responsive 12-column analytical shells with freshness timestamps, card action menus, and 13 tailored skeleton shapes.",
                [
                    new("skeletons", "All 13 DashboardSkeletonKind Shapes",
                        Paragraphs: ["Every dashboard block automatically selects a matching DashboardSkeletonKind (Rows, Stat, Stats, Gauge, Details, Bars, Paired, Funnel, Stack, Table, Feed, Lines, Donut) while its linked IDashboardSource is in the Loading phase."],
                        CodeSamples:
                        [
                            new("LayoutShell.razor", """
                                <BxDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
                                    <BxDashboardGrid Menu="true" Freshness="true">
                                        <BxDashboardCard Title="Executive Summary" Span="12" Source="@_kpis">
                                            <p>Custom card content rendered when _kpis is Ready.</p>
                                        </BxDashboardCard>
                                    </BxDashboardGrid>
                                </BxDashboardPage>
                                """)
                        ])
                ]),
            new("kpi-and-gauge", "Stat, Stats & SVG Gauge", "Visualization Blocks",
                "Display KPIs with comparison deltas, inverted-delta coloring (e.g. lower churn = green), goal bars, sparklines, and circular SVG gauges.",
                [
                    new("kpi", "Live KPIs & Circular Gauge",
                        LiveDemoKey: "dashboard-kpi",
                        LiveDemoTitle: "BxDashboardStats, BxDashboardGauge & BxDashboardBars",
                        CodeSamples:
                        [
                            new("KpiBlocks.razor", """
                                <BxDashboardStat Source="@_summary"
                                                 Label="Active Units"
                                                 Value="120" Compare="100" Goal="150"
                                                 Trend="@([80.0, 95.0, 110.0, 120.0])" />

                                <BxDashboardStats Title="Executive KPIs" Items="@([
                                    new DashboardStatsItem("mrr", "MRR", 148900, DashboardFormatPreset.Compact, Delta: 14.2, Progress: 0.86),
                                    new DashboardStatsItem("churn", "Churn", 1.8, DashboardFormatPreset.Percent, Delta: -0.4, InvertDelta: true)
                                ])" />

                                <BxDashboardGauge Title="Q4 ARR Target" Value="86" Min="0" Max="100" Target="90"
                                                  Format="DashboardFormatPreset.Percent" Caption="86% reached" />
                                """)
                        ])
                ]),
            new("distributions", "Bars, List & Details", "Visualization Blocks",
                "Compare category shares, top-N rankings, and structured key/value detail grids.",
                [
                    new("bars-list", "BxDashboardBars, BxDashboardList & BxDashboardDetails",
                        CodeSamples:
                        [
                            new("Distributions.razor", """
                                <BxDashboardBars Title="Traffic by Platform"
                                                 Items="@([
                                                     new DashboardBarItem("Blazor Web", 14200, Tag: "Web", Meta: "57%"),
                                                     new DashboardBarItem(".NET MAUI Mobile", 7850, Tag: "iOS/Android", Meta: "32%")
                                                 ])"
                                                 Format="DashboardFormatPreset.Compact" />

                                <BxDashboardList Title="Top Accounts" Items="@_topAccounts" />
                                <BxDashboardDetails Title="Infrastructure Breakdown" Items="@_infraDetails" />
                                """)
                        ])
                ]),
            new("tables-and-feeds", "Table, Chart, Alerts & Feed", "Visualization Blocks",
                "Sortable analytical tables with inline bar & delta columns, severity-coded alerts, activity feeds, and multi-series charts.",
                [
                    new("table-alerts", "BxDashboardTable, Alerts, Feed & Chart",
                        Paragraphs:
                        [
                            "BxDashboardTable supports interactive column sorting (DashboardTableSort), typed columns (Type: \"text\" | \"number\" | \"bar\" | \"delta\"), and built-in CSV export via its card menu.",
                            "BxDashboardAlerts renders Error, Warning, Info, and Success severity items (DashboardAlertSeverity)."
                        ],
                        CodeSamples:
                        [
                            new("TablesAndFeeds.razor", """
                                <BxDashboardTable Title="Top Regions"
                                                  Menu="true"
                                                  Columns="@([
                                                      new DashboardTableColumn("region", "Region", Type: "text"),
                                                      new DashboardTableColumn("revenue", "Revenue", Type: "bar"),
                                                      new DashboardTableColumn("delta", "Growth", Type: "delta")
                                                  ])"
                                                  Rows="@_regionRows" />

                                <BxDashboardAlerts Title="Operational Alerts" Items="@_alerts" />
                                <BxDashboardFeed Title="Recent Activity" Items="@_feed" />
                                <BxDashboardChart Title="ARR Trend" Series="@_series" />
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Dashboard", "Lux.Dashboard.Core", "Lux.Dashboard", "builder.Services.AddLuxDashboard();",
                "await dashboardController.RefreshAsync(); var csv = DashboardFormat.ExportCsv(table);"),
            new("examples", "Live Examples", "Examples",
                "Interactive analytics dashboards combining KPI stat strips, gauges, progress bars, wave charts, sortable tables, and severity alerts.",
                [
                    new("kpi-strip", "1. Responsive KPI Stat Strip & Timeframe Switching",
                        Paragraphs: ["Switch timeframes (7d, 30d, 90d) to observe live KPI recalculations across BxDashboardStats with signed deltas, tone coloring, and sparkline trends."],
                        LiveDemoKey: "dashboard-kpi",
                        LiveDemoTitle: "BxDashboardStats · Interactive KPI Strip",
                        CodeSamples:
                        [
                            new("KpiStrip.razor", """
                                <BxDashboardStats Columns="3" Items="@([
                                    new DashboardStatItem("mrr", "Monthly Recurring Revenue", "€148.9K", Delta: "+12.4%", Tone: "success"),
                                    new DashboardStatItem("conv", "Trial Conversion", "34.2%", Delta: "+2.1%", Tone: "primary"),
                                    new DashboardStatItem("churn", "Net Churn", "1.1%", Delta: "-0.3%", Tone: "success")
                                ])" />
                                """)
                        ]),
                    new("gauges-bars", "2. 3-Wave Staged Loading (Essential · Background · Deferred)",
                        Paragraphs: ["Orchestrate DashboardResource<T> loading in three deterministic waves with DashboardController: Essential blocks load first, Background loads concurrently without blocking, and Deferred stays idle until explicitly activated."],
                        LiveDemoKey: "dashboard-waves",
                        LiveDemoTitle: "DashboardController · 3-Wave Staged Loading",
                        CodeSamples:
                        [
                            new("StagedWaves.cs", """
                                var controller = new DashboardController("sales");
                                var essential  = controller.Register("kpi", LoadKpiAsync, DashboardPriority.Essential);
                                var background = controller.Register("regions", LoadRegionsAsync, DashboardPriority.Background);
                                var deferred   = controller.Register("audit", LoadAuditAsync, DashboardPriority.Deferred);

                                await controller.RefreshAsync(); // Runs Wave 1 (Essential), then Wave 2 (Background)
                                await deferred.ActivateAsync();  // Wave 3 runs on demand
                                """)
                        ]),
                    new("tables-alerts", "3. Analytical Table, Operational Alerts & CSV Export",
                        Paragraphs: ["Combine BxDashboardTable (sortable columns with inline bar and delta cells) with BxDashboardAlerts and culture-aware DashboardFormat.ExportCsv."],
                        LiveDemoKey: "dashboard-table-alerts",
                        LiveDemoTitle: "BxDashboardTable, BxDashboardAlerts & CSV Export",
                        CodeSamples:
                        [
                            new("OpsOverview.razor", """
                                <BxDashboardTable Title="Top Regions" Columns="@_columns" Rows="@_rows" Menu="true" />
                                <BxDashboardAlerts Title="Operational Alerts" Items="@([
                                    new DashboardAlertItem(DashboardAlertSeverity.Success, "EU cluster latency within SLA", "P95 at 24ms"),
                                    new DashboardAlertItem(DashboardAlertSeverity.Warning, "Cache eviction spike in us-east", "Raise memory tier")
                                ])" />
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Dashboard.Core and Lux.Dashboard.",
                [],
                ApiTypes: [typeof(BxDashboardPage), typeof(BxDashboardStats), typeof(BxDashboardStat), typeof(BxDashboardGauge), typeof(BxDashboardBars), typeof(BxDashboardTable), typeof(DashboardController), typeof(DashboardFilterControl)])
        ]);
}
