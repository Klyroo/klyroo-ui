using Blux.UI.Tools;
using Blux.UI.Tools.Query;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildTable() => new(
        "table", "Lux.Table", "#00d8ff",
        [
            new("", "Introduction", "Getting Started",
                "Enterprise DataList and NativeAOT headless tree hierarchy engine for Blazor Web and .NET MAUI Blazor Hybrid.",
                [
                    new("overview", "What is Lux.Table?",
                        Paragraphs:
                        [
                            "Lux.Table pairs a zero-UI, NativeAOT tree-hierarchy engine (Lux.Table.Core) with high-density Blazor & .NET MAUI Blazor Hybrid table and card-grid components.",
                            "It supports declarative columns, automatic Sort/Hide HeaderMenu dropdowns, left/right sticky column pinning, interactive column resizing with cookie/localStorage persistence, hierarchical tree expansion with continuation rails, visual row grouping, footer aggregations, bulk selection bars, saved views, 2px loading progress lines, 9 typed cell skeleton shapes, and automatic phone summary rows."
                        ],
                        LiveDemoKey: "table-basic",
                        LiveDemoTitle: "Lux.Table · Live BxDataList with HeaderMenu & Footer Sum"),
                    new("layers", "Two-Layer Architecture",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Table.Core", "DataListTree<TItem>, DataListTreeNode<TItem>, DataListTreeSelectionMode", "Headless C# · IsAotCompatible=true"),
                            new("Lux.Table", "BxDataList, BxDataListTable, BxDataListGrid, BxDataListColumn, Toolbar, FilterBar, Pagination, GroupBy, Summaries, RowSummary, Lux.Table.lib.module.js", "browser, ios, android, maccatalyst, windows")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Table, register services, and understand the bundled JS initializer.",
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
                        Paragraphs: ["AddLuxTable() registers the default synchronous query executor (overrideable with EF Core on servers), URL state services, and optional BluxToolsOptions defaults."],
                        CodeSamples:
                        [
                            new("Program.cs / MauiProgram.cs", """
                                builder.Services.AddLuxTable(options =>
                                {
                                    options.DefaultPageSize = 25;
                                });
                                """)
                        ]),
                    new("js", "3. Automatic JS Initializer (Lux.Table.lib.module.js)",
                        Paragraphs: ["Lux.Table automatically registers Lux.Table.lib.module.js as a Blazor JS module initializer. Zero manual <script> tags are required. It provides column resize handles, persisted column width/visibility cookies (or localStorage inside MAUI WebView), virtualized focus preservation across re-renders, sticky actions-column width measurement, and 768px phone viewport detection."])
                ]),
            new("data-list", "BxDataList & Layouts", "Components",
                "Coordinate Table and Grid views, Toolbars, FilterBars, Bulk Selection Bars, and Pagination inside one BxDataList<TItem> root.",
                [
                    new("composition", "Full Enterprise Composition",
                        Paragraphs: ["BxDataList<TItem> cascades its controller state to child blocks so you can place Toolbars, FilterBars, SelectionBars, Tables, Grids, and Paginators in any layout order."],
                        LiveDemoKey: "table-basic",
                        LiveDemoTitle: "BxDataList with HeaderMenu & Footer Sum",
                        CodeSamples:
                        [
                            new("DirectoryPage.razor", """
                                <BxDataList TItem="Employee"
                                            Schema="@_schema"
                                            Items="@_employees"
                                            UrlSync="true"
                                            ApplyMode="DataListApplyMode.Live">
                                    <BxDataListToolbar TItem="Employee" />
                                    <BxDataListFilterBar TItem="Employee" />
                                    <BxDataListTable TItem="Employee"
                                                     HeaderMenu="true"
                                                     ShowProgressLine="true"
                                                     Density="DataListDensity.Default"
                                                     Summaries="@(new DataListSummaries<Employee>())">
                                        <BxDataListColumn TItem="Employee" Field="name" Title="Employee" Pin="DataListPin.Start" />
                                        <BxDataListColumn TItem="Employee" Field="department" Title="Department" />
                                        <BxDataListColumn TItem="Employee" Field="salary" Title="Salary"
                                                          Align="DataListAlign.End"
                                                          Summary="DataListSummaryKind.Sum" />
                                    </BxDataListTable>
                                    <BxDataListPagination TItem="Employee" />
                                </BxDataList>
                                """)
                        ]),
                    new("enums", "Layout, Density & Selection Enums",
                        TableHeaders: ["Enum", "Exact Values", "Behavior"],
                        TableRows:
                        [
                            new("DataListApplyMode", "Live, Submit", "Live re-queries on every filter change; Submit waits for an explicit Apply button"),
                            new("DataListDensity", "Default, Compact, Comfortable", "Controls vertical cell padding and information density"),
                            new("DataListAlign", "Start, Center, End", "Horizontal alignment for header, body, and summary cells"),
                            new("DataListSelectionScope", "Auto, Page, All", "Defines whether Select-All selects visible page rows or the entire filtered result set"),
                            new("DataListSelectionClear", "Never, Trigger, Success", "Controls when selected keys clear after invoking a bulk action"),
                            new("DataListSelectionBarPosition", "Sticky, Fixed", "Sticky bar above table vs floating fixed bar at viewport bottom")
                        ])
                ]),
            new("columns", "Columns & HeaderMenu", "Components",
                "Configure sortable, hideable, resizable, and pinned columns with automatic HeaderMenu actions and keyboard resizing.",
                [
                    new("header-menu", "HeaderMenu & Column Persistence",
                        Paragraphs:
                        [
                            "Set HeaderMenu=\"true\" on BxDataListTable so clicking any sortable or hideable column header opens an accessible menu offering Sort Ascending, Sort Descending, Clear Sort, and Hide Column.",
                            "When column resizing is enabled, resize handles support both pointer drag and keyboard navigation (ArrowRight/ArrowLeft in 16px steps, Shift+Arrow in 64px steps, Home/End for min/max width, Delete/Backspace to reset to default width)."
                        ],
                        TableHeaders: ["DataListPin", "Behavior", "Typical Column"],
                        TableRows:
                        [
                            new("DataListPin.None", "Scrolls horizontally with table body (default)", "Standard data columns"),
                            new("DataListPin.Start", "Sticky to the leading edge (RTL-aware)", "Selection checkbox, Name/ID column"),
                            new("DataListPin.End", "Sticky to the trailing edge (RTL-aware)", "Row actions menu, Total column")
                        ],
                        CodeSamples:
                        [
                            new("PinnedColumns.razor", """
                                <BxDataListTable TItem="Employee" HeaderMenu="true">
                                    <BxDataListColumn TItem="Employee" Field="name" Title="Employee" Pin="DataListPin.Start" SkeletonKind="DataListSkeletonKind.Avatar" />
                                    <BxDataListColumn TItem="Employee" Field="department" Title="Department" SkeletonKind="DataListSkeletonKind.Badge" />
                                    <BxDataListColumn TItem="Employee" Field="salary" Title="Salary" Align="DataListAlign.End" SkeletonKind="DataListSkeletonKind.Number" Summary="DataListSummaryKind.Sum" />
                                </BxDataListTable>
                                """)
                        ])
                ]),
            new("tree", "Tree Hierarchy (DataListTree)", "Components",
                "Render arbitrarily nested parent-child trees with indentation rails, keyboard expand/collapse, and cycle/duplicate key protection.",
                [
                    new("headless-tree", "DataListTree<TItem> in Lux.Table.Core (NativeAOT)",
                        Paragraphs:
                        [
                            "DataListTree<TItem> is 100% headless in Lux.Table.Core. BuildTree(rootItems, schema) skips null entries and duplicate keys (preventing infinite cycles), builds DataListTreeNode<TItem> objects carrying Key, Item, Depth, Parent, RootIndex, Children, HasChildren, IsLast, and AncestorContinuations (boolean rail array for drawing vertical tree lines).",
                            "DefaultExpandedDepth (e.g., 1 expands roots only, 2 expands roots and children) overrides DefaultExpanded when specified."
                        ],
                        TableHeaders: ["Selection Mode", "Behavior", "Best For"],
                        TableRows:
                        [
                            new("DataListTreeSelectionMode.All", "Every root and descendant row renders its own checkbox", "Folder trees, org charts, permissions"),
                            new("DataListTreeSelectionMode.Roots", "Only depth-0 root rows show checkboxes; descendants reserve alignment width", "Invoices with nested line items")
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
                                """),
                            new("OrgTreeTable.razor", """
                                <BxDataListTable TItem="Employee" Tree="@_orgTree">
                                    <BxDataListColumn TItem="Employee" Field="name" Title="Name" />
                                    <BxDataListColumn TItem="Employee" Field="department" Title="Department" />
                                </BxDataListTable>
                                """)
                        ],
                        Callout: "Keyboard support: press Enter/Space or ArrowRight to expand a focused branch node, and ArrowLeft to collapse it. When Tree is set, visual grouping and phone summary rows are disabled automatically.")
                ]),
            new("grouping-and-summaries", "Grouping & Footer Summaries", "Components",
                "Group rows under collapsible headers and calculate automatic footer totals across numeric columns.",
                [
                    new("grouping", "Visual Row Grouping (DataListGroupBy<TItem>)",
                        Paragraphs: ["DataListGroupBy<TItem> groups loaded rows by any key selector, renders collapsible group headers with item counts, and preserves stable group ordering."],
                        CodeSamples:
                        [
                            new("Grouping.cs", """
                                var groupBy = new DataListGroupBy<Employee>
                                {
                                    Key = e => e.Department
                                };
                                """)
                        ]),
                    new("summaries", "All 6 DataListSummaryKind Aggregations",
                        TableHeaders: ["Kind", "Computation", "Example Use"],
                        TableRows:
                        [
                            new("DataListSummaryKind.Sum", "Arithmetic sum of numeric values", "Total Payroll, Revenue, Quantity"),
                            new("DataListSummaryKind.Average", "Mean across non-null numeric rows", "Average Rating, Margin %, Latency"),
                            new("DataListSummaryKind.Count", "Count of non-empty values", "Active Records, Assigned Tasks"),
                            new("DataListSummaryKind.Min", "Minimum value in column", "Lowest Cost, Earliest Due Date"),
                            new("DataListSummaryKind.Max", "Maximum value in column", "Peak Throughput, Latest Update"),
                            new("DataListSummaryKind.None", "No summary calculation (default)", "Free-text or action columns")
                        ],
                        CodeSamples:
                        [
                            new("FooterSummaries.razor", """
                                <BxDataListTable TItem="Employee" Summaries="@(new DataListSummaries<Employee>())">
                                    <BxDataListColumn TItem="Employee" Field="name" Title="Employee" Summary="DataListSummaryKind.Count" />
                                    <BxDataListColumn TItem="Employee" Field="salary" Title="Salary" Summary="DataListSummaryKind.Sum" />
                                </BxDataListTable>
                                """)
                        ])
                ]),
            new("mobile-summary", "Mobile Summary Rows", "Components",
                "Switch wide tables automatically into scannable two-line mobile rows below 768px.",
                [
                    new("row-summary", "DataListRowSummary<TItem> Properties",
                        Paragraphs:
                        [
                            "DataListRowSummary<TItem> eliminates awkward horizontal scrolling on smartphones. Below the md breakpoint (768px), AutoPhone = true automatically displays each item with a leading visual, primary title (plain text, Href link, or OnSelect button), status badge, secondary details joined with ' · ', selection checkbox, and row actions."
                        ],
                        TableHeaders: ["Property", "Type", "Purpose"],
                        TableRows:
                        [
                            new("Title", "Func<TItem, string?>?", "Primary one-line title (falls back to Schema.GetRowLabel or first column)"),
                            new("Leading / Status", "RenderFragment<TItem>?", "Optional 32px avatar/icon before title and status pill on line 2"),
                            new("Details", "Func<TItem, IEnumerable<string?>>?", "Secondary metadata items joined with ' · ' (blank entries skipped)"),
                            new("Href / OnSelect", "Func<TItem, string?>? / EventCallback<TItem>", "Navigates or triggers row selection on title tap"),
                            new("Template", "RenderFragment<TItem>?", "Replaces the entire body layout when custom markup is needed"),
                            new("AutoPhone / DefaultActive", "bool (true / false)", "AutoPhone activates under 768px; DefaultActive forces summary view on all widths")
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
                "Keep tables responsive and informative during initial load, background refreshes, and zero-row states.",
                [
                    new("skeletons", "All 9 DataListSkeletonKind Cell Shapes",
                        TableHeaders: ["SkeletonKind", "Rendered Placeholder", "Recommended Column"],
                        TableRows:
                        [
                            new("Text / Number", "Horizontal pill (start-aligned for Text, end-aligned for Number)", "Titles, names, prices, quantities"),
                            new("Avatar / Icon / Dot", "32px circle, 20px icon square, or 10px status dot", "User profiles, file type icons, online indicators"),
                            new("Badge / Check / Progress", "Status pill, checkbox square, or thin progress bar", "Statuses, booleans, goal completion %"),
                            new("None", "Leaves cell empty while loading", "Row action columns or spacers")
                        ]),
                    new("progress-empty", "Progress Line & Differentiated Empty States",
                        Bullets:
                        [
                            "ShowProgressLine=\"true\" renders an unobtrusive 2px indeterminate progress bar along the top border during background reloads without hiding existing data.",
                            "DataListEmptyState differentiates an empty dataset (first-use onboarding copy + primary CTA) from a filtered-empty result (helpful message + one-click Clear Filters button)."
                        ])
                ]),
            MauiAndAotPage("Lux.Table", "Lux.Table.Core", "Lux.Table", "builder.Services.AddLuxTable();",
                "var (roots, allNodes) = new DataListTree<Employee> { Children = e => e.Reports }.BuildTree(employees);"),
            new("recipes", "Testing & Recipes", "Platforms",
                "Unit testing DataListTree in xUnit and rendering BxDataList in bUnit.",
                [
                    new("xunit", "Headless Tree & GroupBy Tests",
                        CodeSamples:
                        [
                            new("TableCoreTests.cs", """
                                var tree = new DataListTree<Employee> { Children = e => e.Reports, DefaultExpanded = true };
                                var (_, allNodes) = tree.BuildTree([new Employee(1, "Alice", "Eng", 120000m, [new(2, "Bob", "Eng", 95000m)])]);

                                Assert.Equal(2, allNodes.Count);
                                Assert.Equal(0, allNodes[0].Depth);
                                Assert.Equal(1, allNodes[1].Depth);
                                """)
                        ]),
                    new("bunit", "bUnit Service Registration Order",
                        CodeSamples:
                        [
                            new("TableBunitTests.cs", """
                                Services.AddLuxTable();
                                JSInterop.Mode = JSRuntimeMode.Loose;
                                SetRendererInfo(new RendererInfo("Server", true));

                                var cut = Render<BxDataList<Employee>>(...);
                                Assert.Equal(2, cut.FindAll("tbody tr").Count);
                                Assert.NotNull(cut.Find("tfoot"));
                                """)
                        ],
                        Callout: "Always invoke Services.AddLuxTable() BEFORE SetRendererInfo(...) in bUnit tests; SetRendererInfo resolves the renderer and locks DI.")
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Table.Core and Lux.Table.",
                [],
                ApiTypes: [typeof(BxDataList<object>), typeof(BxDataListTable<object>), typeof(BxDataListColumn<object>), typeof(DataListTree<object>), typeof(DataListRowSummary<object>)])
        ]);
}
