using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildForm() => new(
        "form", "Lux.Form", "#f59e0b",
        [
            new("", "Introduction", "Getting Started",
                "Declarative, NativeAOT schema-driven form engine with 31 field kinds, reactive visibility conditions, dirty path diffing, output transforms, multi-step wizards with SkipWhen, and modal FormService overlays.",
                [
                    new("overview", "What is Lux.Form?",
                        Paragraphs:
                        [
                            "Lux.Form eliminates repetitive form markup and ad-hoc validation glue. You describe fields, conditions, validation rules, output transforms, and multi-step flows in a plain C# FormSchema. FormController manages every value, error, touched flag, dirty diff, and step transition in 100% headless NativeAOT C#.",
                            "Pair FormController with BxFormEngine, BxFormPage, or BxFormProvider to render accessible Blazor Web and .NET MAUI Blazor Hybrid forms automatically."
                        ],
                        LiveDemoKey: "form-reactive",
                        LiveDemoTitle: "Lux.Form · Live Reactive Condition, Password Rules & Dirty Tracking"),
                    new("layers", "Two-Layer Architecture",
                        TableHeaders: ["Package", "Contains", "Targets"],
                        TableRows:
                        [
                            new("Lux.Form.Core", "FormSchema, FormFieldSchema, FormStepSchema, FormController, FormService", "Headless C# · NativeAOT"),
                            new("Lux.Form", "BxFormEngine, BxFormPage, BxFormProvider, AddLuxForm()", "Blazor Web & .NET MAUI Hybrid")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Form, register FormService, and optionally mount BxFormProvider for modal/drawer overlays.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Form.Core   # Headless NativeAOT schema & controller
                                dotnet add package Lux.Form        # Blazor Web & .NET MAUI form components
                                """)
                        ]),
                    new("di", "2. Register Services & Overlay Host",
                        Paragraphs: ["AddLuxForm() registers FormService as scoped. Mount <BxFormProvider /> once in your root layout if you plan to launch programmatic modal or drawer forms."],
                        CodeSamples:
                        [
                            new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxForm();"),
                            new("MainLayout.razor", "<BxFormProvider />")
                        ])
                ]),
            new("field-kinds", "FormSchema & 31 Field Kinds", "Schema & State",
                "Complete catalog of all 31 declarative FormFieldKind values supported by FormSchema.",
                [
                    new("all-kinds", "All 31 FormFieldKind Values",
                        TableHeaders: ["Category", "Exact FormFieldKind Values", "Capabilities"],
                        TableRows:
                        [
                            new("Text & Credentials (5)", "Text, Password, Textarea, PhoneNumber, OneTimeCode", "Required, MinLength/MaxLength, PasswordRequirements checklist, OTP input"),
                            new("Numeric & Visual (4)", "Number, Slider, Rating, ColorPicker", "Min/Max bounds, Step precision, visual slider/star/color pickers"),
                            new("Boolean & Choice (6)", "Checkbox, Switch, Radio, CheckboxGroup, Select, AutoComplete", "Static or dynamic FormOptionItem lists with labels and disabled states"),
                            new("Temporal & Tags (5)", "Date, DateTime, DateRange, Time, Tag", "Calendar/time inputs, ranges, and multi-tag chip editors"),
                            new("Layout & Presentation (6)", "Hidden, Info, Divider, Section, Button, Group", "Stateless layout blocks (skipped from validation and BuildOutput)"),
                            new("Composite & Nested (5)", "Card, Tabs, Object, ArrayList, ArrayTable", "Tabbed fieldsets, nested objects, and repeatable add/remove array rows")
                        ]),
                    new("shell-modes", "FormShellMode & Section Statuses",
                        TableHeaders: ["Enum", "Values", "Purpose"],
                        TableRows:
                        [
                            new("FormShellMode", "Inline, Modal, Drawer, Fullscreen", "Presentation mode for BxFormEngine and FormService overlays"),
                            new("FormPageSectionStatus", "Complete, Pending, Invalid, Optional", "Live completion indicator per section in BxFormPage")
                        ])
                ]),
            new("conditions", "Reactive Conditions & Transforms", "Schema & State",
                "Show, hide, disable, and normalize fields reactively as the user types.",
                [
                    new("reactive", "Condition, Disabled & TransformOutput",
                        Paragraphs:
                        [
                            "Condition = form => ... is re-evaluated automatically whenever any value changes. When Condition returns false, the field is hidden in the UI, bypassed during Validate() / SubmitAsync(), and omitted completely from BuildOutput().",
                            "TransformOutput runs when constructing the final submission dictionary (e.g., trimming strings, uppercasing VAT numbers, or parsing slugs)."
                        ],
                        LiveDemoKey: "form-reactive",
                        LiveDemoTitle: "Try setting Age below 21 to watch Company hide live",
                        CodeSamples:
                        [
                            new("ReactiveFields.cs", """
                                var schema = new FormSchema
                                {
                                    FormKey = "profile",
                                    Title = "User Profile",
                                    Fields =
                                    [
                                        new FormFieldSchema
                                        {
                                            Key = "name",
                                            Label = "Full Name",
                                            Required = true,
                                            DirtyCheck = true,
                                            TransformOutput = v => v?.ToString()?.Trim()
                                        },
                                        new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, Min = 18, Max = 120, DefaultValue = 25.0 },
                                        new FormFieldSchema { Key = "company", Label = "Company", Required = true, Condition = f => f.GetValue<double>("age") >= 21 }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("validation", "Validation & Password Rules", "Schema & State",
                "Control validation modes, triggers, custom rules, and real-time password strength requirements.",
                [
                    new("modes", "Validation Modes & Triggers",
                        TableHeaders: ["Enum", "Values", "Behavior"],
                        TableRows:
                        [
                            new("FormValidationMode", "All, RequiredOnly, ValidatorsOnly, None", "Selects which rule categories execute during Validate()"),
                            new("FormValidationTrigger", "Blur, Input, Submit", "Controls when individual field errors surface to the user")
                        ]),
                    new("password", "Live Password Requirements",
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
                                        new FormPasswordRequirement("len", "At least 8 characters", s => s.Length >= 8),
                                        new FormPasswordRequirement("upper", "Contains an uppercase letter", s => s.Any(char.IsUpper)),
                                        new FormPasswordRequirement("digit", "Contains a number", s => s.Any(char.IsDigit))
                                    ]
                                }
                                """)
                        ])
                ]),
            new("dirty-tracking", "Dirty Tracking & Diffing", "Schema & State",
                "Detect exact changed field paths against the baseline snapshot and re-baseline automatically on submit.",
                [
                    new("dirty", "IsDirty, DirtyPaths & Re-Baselining",
                        Paragraphs:
                        [
                            "FormController captures a baseline snapshot of initial values. Calling SetValue() updates IsDirty and populates DirtyPaths with only the keys whose current values differ from the baseline.",
                            "When SubmitAsync() succeeds, FormController re-baselines the snapshot so IsDirty returns to false until the next edit."
                        ],
                        CodeSamples:
                        [
                            new("DirtyTracking.cs", """
                                var controller = new FormController(schema, new Dictionary<string, object?> { ["name"] = "Alice" });
                                Assert.False(controller.IsDirty);

                                controller.SetValue("age", 19.0, markTouched: true);
                                Assert.True(controller.IsDirty);
                                Assert.Contains("age", controller.DirtyPaths);

                                Assert.True(await controller.SubmitAsync());
                                Assert.False(controller.IsDirty);
                                """)
                        ])
                ]),
            new("wizards", "Multi-Step Wizards (SkipWhen)", "Schema & State",
                "Split complex workflows into validated steps with dynamic SkipWhen branching.",
                [
                    new("steps", "Stepped FormSchema & Navigation",
                        Paragraphs: ["Populate Steps on FormSchema to enable wizard mode (IsStepped == true). NextStepAsync() validates only the active step's visible fields and automatically skips any step whose SkipWhen predicate returns true."],
                        CodeSamples:
                        [
                            new("OnboardingWizard.cs", """
                                var wizard = new FormSchema
                                {
                                    FormKey = "onboarding",
                                    Title = "Workspace Onboarding",
                                    Steps =
                                    [
                                        new FormStepSchema
                                        {
                                            Key = "account",
                                            Title = "Account",
                                            Fields = [new FormFieldSchema { Key = "plan", Label = "Plan", Required = true, DefaultValue = "free" }]
                                        },
                                        new FormStepSchema
                                        {
                                            Key = "billing",
                                            Title = "Billing",
                                            SkipWhen = f => f.GetValue<string>("plan") == "free",
                                            Fields = [new FormFieldSchema { Key = "vat", Label = "VAT Number", Required = true }]
                                        },
                                        new FormStepSchema
                                        {
                                            Key = "confirm",
                                            Title = "Confirm",
                                            Fields = [new FormFieldSchema { Key = "accept", Label = "Accept Terms", Kind = FormFieldKind.Checkbox, Required = true }]
                                        }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("form-engine", "BxFormEngine", "UI Components",
                "Declarative Blazor & .NET MAUI renderer for FormController and FormSchema.",
                [
                    new("engine", "Usage & Events",
                        LiveDemoKey: "form-reactive",
                        LiveDemoTitle: "BxFormEngine Live Demo",
                        CodeSamples:
                        [
                            new("ProfilePage.razor", """
                                <BxFormEngine Form="@_controller"
                                              SubmitLabel="Save profile"
                                              ShowReset="true"
                                              OnSubmit="@HandleSubmit"
                                              OnCancel="@HandleCancel" />
                                """)
                        ])
                ]),
            new("form-page-and-service", "BxFormPage & FormService Overlays", "UI Components",
                "Build multi-section settings pages or open forms programmatically in modals, drawers, and fullscreen dialogs.",
                [
                    new("service", "Programmatic FormService",
                        Paragraphs: ["Inject FormService into any component or action handler, open a FormSchema in Modal, Drawer, or Fullscreen shell mode, and await the submitted payload directly in C#."],
                        CodeSamples:
                        [
                            new("OpenModalForm.cs", """
                                var outcome = await formService.OpenAsync(inviteSchema, initialValues, FormShellMode.Modal);
                                if (outcome.Submitted)
                                {
                                    await api.SendInviteAsync(outcome.Output);
                                }
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Form", "Lux.Form.Core", "Lux.Form", "builder.Services.AddLuxForm();",
                "var form = new FormController(schema); if (await form.SubmitAsync()) Save(form.BuildOutput());"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Form.Core and Lux.Form.",
                [],
                ApiTypes: [typeof(BxFormEngine), typeof(BxFormPage), typeof(BxFormProvider), typeof(FormController), typeof(FormSchema), typeof(FormFieldSchema), typeof(FormService)])
        ]);

    private static LibraryDocTree BuildDashboard() => new(
        "dashboard", "Lux.Dashboard", "#8b5cf6",
        [
            new("", "Introduction", "Getting Started",
                "Analytical dashboard orchestrator with 3-wave staged queries (Essential -> Background -> Deferred), global filters, leap-year comparison ranges, UTF-8 BOM CSV export, and 13 visualization blocks.",
                [
                    new("overview", "What is Lux.Dashboard?",
                        Paragraphs:
                        [
                            "Analytical dashboards often suffer from waterfalls or all-or-nothing spinners. Lux.Dashboard solves this with a 100% headless NativeAOT orchestrator (DashboardController) that runs queries in 3 priority waves, coordinates shared filters and views, formats metrics consistently, and feeds 13 ready-to-use visualization blocks."
                        ],
                        LiveDemoKey: "dashboard-kpi",
                        LiveDemoTitle: "Lux.Dashboard · Live Stats, SVG Gauge & Bars"),
                    new("layers", "Two-Layer Package Split",
                        TableHeaders: ["Package", "Contains", "Targets"],
                        TableRows:
                        [
                            new("Lux.Dashboard.Core", "DashboardController, DashboardResource<T>, DashboardFilterControl, DashboardFormat, models", "Headless C# · NativeAOT"),
                            new("Lux.Dashboard", "13 analytical Razor blocks (Page, Grid, Card, Stat, Stats, Gauge, Details, Bars, List, Table, Alerts, Feed, Chart)", "Blazor Web & .NET MAUI Hybrid")
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
                "Load executive KPIs immediately, stream secondary charts second, and defer heavy queries until requested.",
                [
                    new("stages", "Stages, Resource States & Block Phases",
                        TableHeaders: ["Enum", "Values", "Role"],
                        TableRows:
                        [
                            new("DashboardQueryStage", "Essential, Background, Deferred", "Controls execution wave during DashboardController.RefreshAsync()"),
                            new("DashboardResourceState", "Idle, Loading, Ready, Error, Disabled", "Lifecycle state of each DashboardResource<TData>"),
                            new("DashboardBlockPhase", "Idle, Loading, Error, Empty, Content", "Derived visual rendering phase for every dashboard card")
                        ],
                        CodeSamples:
                        [
                            new("StagedWaves.cs", """
                                var controller = new DashboardController("analytics");
                                var kpis = controller.RegisterResource(new DashboardResource<int>(
                                    "summary", LoadSummaryAsync, DashboardQueryStage.Essential));
                                var chart = controller.RegisterResource(new DashboardResource<double[]>(
                                    "trend", LoadTrendAsync, DashboardQueryStage.Background));
                                var audit = controller.RegisterResource(new DashboardResource<string>(
                                    "audit", LoadAuditAsync, DashboardQueryStage.Deferred));

                                // Runs Essential first, then Background; Deferred remains Idle until LoadAsync()
                                await controller.RefreshAsync();
                                """)
                        ])
                ]),
            new("filters-and-views", "Global Filters, Presets & Views", "Orchestration",
                "Synchronize 10 filter kinds, quick presets, automatic Reset state detection, and badged views.",
                [
                    new("filter-kinds", "All 10 DashboardFilterKind Values",
                        TableHeaders: ["Category", "DashboardFilterKind Values", "Usage"],
                        TableRows:
                        [
                            new("Scalar & Toggle", "String, Number, Boolean", "Search inputs, thresholds, and include/exclude toggles"),
                            new("Temporal", "Date, DateRange, Comparison", "Period pickers paired with Previous/Year comparison mode"),
                            new("Selections", "Enum, Options, Remote, Custom", "Single/multi-select dimensions, remote option lookups & custom controls")
                        ],
                        CodeSamples:
                        [
                            new("FiltersAndViews.cs", """
                                controller.AddView("overview", "Overview", badge: "3");
                                controller.AddView("details", "Details");

                                var yearFilter = controller.AddFilter(new DashboardFilterControl(
                                    "year", "Year",
                                    items: [new("2025", "2025"), new("2026", "2026")],
                                    defaultValues: ["2026"]));

                                yearFilter.Toggle("2025");
                                // controller.Filtered == true; controller.ResetFilters() restores ["2026"]
                                """)
                        ])
                ]),
            new("formatting-and-csv", "Formatting, Comparison Ranges & CSV", "Orchestration",
                "Null/NaN-safe formatting presets, leap-year-safe date comparisons, and localized UTF-8 BOM CSV export.",
                [
                    new("presets", "All 9 DashboardFormatPreset Values & 3 Comparison Modes",
                        TableHeaders: ["Preset", "Sample Input", "Output (InvariantCulture)"],
                        TableRows:
                        [
                            new("Number / Integer / Decimal", "12600 / 12.345", "12,600 / 12 / 12.35"),
                            new("Compact", "12600 / 2400000", "12.6K / 2.4M"),
                            new("Percent / Ratio", "86 / 0.57", "86% / 57%"),
                            new("Delta / Points / Signed", "14.2 / 2.1 / -5", "+14.2% / +2.1 pts / -5")
                        ],
                        CodeSamples:
                        [
                            new("FormatAndCsv.cs", """
                                // Leap-year safe comparison (Feb 1-29, 2024 -> Feb 1-28, 2023)
                                var yearRange = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);
                                var prevRange = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Previous);

                                // Safe slugified export filename + UTF-8 BOM CSV
                                var name = DashboardFormat.ResolveFileName("Monthly Revenue"); // "monthly-revenue.csv"
                                var csv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
                                """)
                        ])
                ]),
            new("layout-blocks", "Page, Grid & Card", "Visualization Blocks",
                "Assemble responsive analytical pages with built-in action menus, freshness badges, and 13 skeleton kinds.",
                [
                    new("shell", "BxDashboardPage, BxDashboardGrid & BxDashboardCard",
                        Paragraphs: ["DashboardSkeletonKind provides 13 tailored skeleton shapes (Rows, Stat, Stats, Gauge, Details, Bars, Paired, Funnel, Stack, Table, Feed, Lines, Donut) so every card displays an accurate placeholder while its wave loads."],
                        CodeSamples:
                        [
                            new("Shell.razor", """
                                <BxDashboardPage Dashboard="@_dashboard" Title="Revenue Analytics">
                                    <BxDashboardGrid Menu="true" Freshness="true">
                                        <BxDashboardCard Title="Custom Insights">...</BxDashboardCard>
                                    </BxDashboardGrid>
                                </BxDashboardPage>
                                """)
                        ])
                ]),
            new("kpi-and-gauge", "Stat, Stats & SVG Gauge", "Visualization Blocks",
                "Present executive KPIs with deltas, trend sparklines, goal bars, and circular SVG gauges.",
                [
                    new("kpi", "Live KPI & Gauge Examples",
                        LiveDemoKey: "dashboard-kpi",
                        LiveDemoTitle: "BxDashboardStats, BxDashboardGauge & BxDashboardBars",
                        CodeSamples:
                        [
                            new("Kpis.razor", """
                                <BxDashboardStat Source="@_summary" Label="Units" Value="120" Compare="100" Goal="150" Trend="@([80.0, 95.0, 110.0, 120.0])" />
                                <BxDashboardStats Title="Executive KPIs" Items="@_stats" />
                                <BxDashboardGauge Title="Q4 ARR Goal" Value="86" Max="100" Target="90" Format="DashboardFormatPreset.Percent" Caption="86% reached" />
                                """)
                        ])
                ]),
            new("distributions", "Bars, List & Details", "Visualization Blocks",
                "Compare category distributions, ranked items, and key/value detail groups.",
                [
                    new("bars", "BxDashboardBars, BxDashboardList & BxDashboardDetails",
                        CodeSamples:
                        [
                            new("Distributions.razor", """
                                <BxDashboardBars Title="Channels" Items="@_bars" Format="DashboardFormatPreset.Compact" />
                                <BxDashboardList Title="Top Accounts" Items="@_accounts" />
                                <BxDashboardDetails Title="Region Breakdown" Items="@_details" />
                                """)
                        ])
                ]),
            new("tables-and-feeds", "Table, Chart, Alerts & Feed", "Visualization Blocks",
                "Sortable analytical tables with inline bars/deltas, severity alerts, activity feeds, and chart blocks.",
                [
                    new("table", "BxDashboardTable, Alerts & Feed",
                        Paragraphs: ["DashboardAlertSeverity supports Error, Warning, Info, and Success badges with optional drill-down actions."],
                        CodeSamples:
                        [
                            new("TableAndFeeds.razor", """
                                <BxDashboardTable Title="Top Regions" Menu="true" Columns="@_cols" Rows="@_rows" />
                                <BxDashboardAlerts Title="Anomalies" Items="@_alerts" />
                                <BxDashboardFeed Title="Recent Events" Items="@_feed" />
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Dashboard", "Lux.Dashboard.Core", "Lux.Dashboard", "builder.Services.AddLuxDashboard();",
                "await dashboardController.RefreshAsync(); var csv = DashboardFormat.ExportCsv(table);"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Dashboard.Core and Lux.Dashboard.",
                [],
                ApiTypes: [typeof(BxDashboardPage), typeof(BxDashboardStats), typeof(BxDashboardStat), typeof(BxDashboardGauge), typeof(BxDashboardBars), typeof(BxDashboardTable), typeof(DashboardController)])
        ]);

    private static LibraryDocTree BuildDisplay() => new(
        "display", "Lux.Display", "#ec4899",
        [
            new("", "Introduction", "Getting Started",
                "19 read-only display blocks (BxDisplay), constrained Audio/Video media players with clip bounds and play quotas, and a multi-format modal file previewer.",
                [
                    new("overview", "What is Lux.Display?",
                        Paragraphs:
                        [
                            "Lux.Display standardizes read-only data presentation, assessment-grade audio/video playback, and modal document previews across Blazor Web and .NET MAUI Blazor Hybrid.",
                            "All MIME/extension detection, RFC 4180 CSV parsing, and media rule models live in 100% NativeAOT C# (Lux.Display.Core)."
                        ],
                        LiveDemoKey: "display-blocks",
                        LiveDemoTitle: "Lux.Display · Live Stat, Threshold Meter, Secret Mask & Diff"),
                    new("all-kinds", "All 19 DisplayKind Values",
                        TableHeaders: ["Group", "Exact DisplayKind Values", "Highlights"],
                        TableRows:
                        [
                            new("Editorial (4)", "Heading, Text, Alert, Empty", "Section titles with counts, expandable Alert details & empty states"),
                            new("Metrics (5)", "Facts, Stat, Breakdown, Meter, Chart", "Copyable facts, sparklines, multi-part meters with warning/error thresholds"),
                            new("Structured Data (6)", "Table, Diff, Record, Person, List, Accordion", "Before/after diffs, incremental Reveal More lists, collapsible accordions"),
                            new("Rich & Technical (4)", "Timeline, Date, Media, Code", "Timeline states (Done/Current/Upcoming), relative dates & Variant=\"secret\" masking")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Display, register FilePreviewService, and mount BxFilePreviewProvider.",
                [
                    new("nuget", "1. Install Packages",
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
                            new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxDisplay();"),
                            new("MainLayout.razor", "<BxFilePreviewProvider />")
                        ])
                ]),
            new("content-blocks", "Typography, Alerts & Facts", "Display Blocks",
                "Render headings with count pills, expandable status alerts, and copyable key/value fact grids.",
                [
                    new("surface", "DisplaySurface & Content Blocks",
                        Paragraphs: ["Every BxDisplay block supports DisplaySurface (Plain, Muted, Bordered, Elevated), semantic Tone, and Loading skeleton state."],
                        CodeSamples:
                        [
                            new("ContentBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Heading" Title="Active Subscriptions" Count="42" Description="Updated 5m ago" />
                                <BxDisplay Kind="DisplayKind.Alert" Tone="BxColor.Warning" Title="Quota warning" Details="85% of monthly volume used" />
                                <BxDisplay Kind="DisplayKind.Facts" Facts="@([
                                    new DisplayFact { Label = "Environment", Value = "Production", Copy = true },
                                    new DisplayFact { Label = "Region", Value = null }
                                ])" />
                                """)
                        ])
                ]),
            new("metrics-and-diff", "Stats, Breakdown, Meter & Diff", "Display Blocks",
                "Visualize KPIs with sparklines, threshold meters that change tone automatically, and changed-field diffs.",
                [
                    new("metrics", "Stat, Meter & Diff",
                        LiveDemoKey: "display-blocks",
                        LiveDemoTitle: "Live Stat, Meter, Secret Code & Diff",
                        CodeSamples:
                        [
                            new("Metrics.razor", """
                                <BxDisplay Kind="DisplayKind.Stat" Title="MRR" Value="€148,900" Delta="+14.2%" Trend="@([102.0, 118.0, 134.0, 148.9])" />
                                <BxDisplay Kind="DisplayKind.Meter" Title="Storage" MeterValue="92" MeterMax="100" WarningThreshold="75" ErrorThreshold="90" />
                                <BxDisplay Kind="DisplayKind.Diff" DiffItems="@([
                                    new DisplayDiffItem("Plan", "Starter", "Enterprise", Changed: true),
                                    new DisplayDiffItem("Currency", "EUR", "EUR", Changed: false)
                                ])" />
                                """)
                        ])
                ]),
            new("code-and-lists", "Code, Secret Masking & Lists", "Display Blocks",
                "Mask sensitive secrets with reveal toggles and paginate lists progressively with Max/Step.",
                [
                    new("secret", "Secret Masking & Progressive Lists",
                        Paragraphs: ["Setting Kind=\"DisplayKind.Code\" with Variant=\"secret\" masks all characters except the trailing VisibleChars (default 4) until the user clicks Reveal."],
                        CodeSamples:
                        [
                            new("CodeAndList.razor", """
                                <BxDisplay Kind="DisplayKind.Code" Variant="secret" Title="API Secret" Value="sk_live_9876543210" VisibleChars="4" Copy="true" />
                                <BxDisplay Kind="DisplayKind.List" Max="2" Step="2" ListItems="@_steps" />
                                """)
                        ])
                ]),
            new("structured-blocks", "Timeline, People, Contacts & Accordion", "Display Blocks",
                "Display status timelines, team rosters, contact cards, and accordions.",
                [
                    new("timeline", "DisplayTimelineState & Structured Blocks",
                        Paragraphs: ["DisplayTimelineState supports Done, Current, and Upcoming states for deployment, onboarding, or order tracking timelines."])
                ]),
            new("audio-player", "Constrained Audio Player", "Media & Files",
                "Play bounded audio clips with play-count quotas, speed control, and seek locking.",
                [
                    new("audio", "BxAudioPlayer Constraints & Controls",
                        Paragraphs: ["Hide individual UI controls using MediaPlayerControl (Time, Volume, Rate, Captions, PictureInPicture, Fullscreen, Download). When Plays reaches MaxPlays, playback locks and OnLimitReached fires."],
                        LiveDemoKey: "display-audio",
                        LiveDemoTitle: "BxAudioPlayer · Bounded Clip & Quota Demo",
                        CodeSamples:
                        [
                            new("Audio.razor", """
                                <BxAudioPlayer Src="/media/listening-test.mp3"
                                               Title="Listening Comprehension · Extract 1"
                                               Start="15" End="45"
                                               MaxPlays="2" Seekable="false"
                                               OnLimitReached="@HandleQuotaReached" />
                                """)
                        ])
                ]),
            new("video-player", "Constrained Video Player", "Media & Files",
                "Play bounded video segments with WebVTT subtitle tracks (VideoPlayerTrack) and play quotas.",
                [
                    new("video", "BxVideoPlayer",
                        CodeSamples:
                        [
                            new("Video.razor", """
                                <BxVideoPlayer Src="/media/briefing.mp4"
                                               Title="Safety Briefing"
                                               Start="0" End="90"
                                               MaxPlays="3" Seekable="false" />
                                """)
                        ])
                ]),
            new("file-preview", "FilePreviewService & Lightbox", "Media & Files",
                "Multi-format file previewer with automatic MIME/extension detection and built-in RFC 4180 CSV parser.",
                [
                    new("kinds", "All 10 FilePreviewKind & 3 Container Modes",
                        TableHeaders: ["Enum", "Values", "Notes"],
                        TableRows:
                        [
                            new("FilePreviewKind", "Pdf, Image, Video, Audio, Csv, Markdown, Text, Office, Archive, Other", "Auto-resolved from MIME type or filename extension in Lux.Display.Core"),
                            new("FilePreviewContainer", "Modal, Drawer, Fullscreen", "Shell style for BxFilePreviewProvider")
                        ],
                        CodeSamples:
                        [
                            new("Preview.cs", """
                                filePreview.Open(
                                    new FilePreviewItem("spec.pdf", "/files/spec.pdf"),
                                    new FilePreviewItem("rates.csv", "/files/rates.csv"));

                                ParsedCsvPreview csv = FilePreviewService.ParseCsv("Region,MRR\nEU,148900\nUS,192400");
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Display", "Lux.Display.Core", "Lux.Display", "builder.Services.AddLuxDisplay();",
                "var csv = FilePreviewService.ParseCsv(rawCsvText);"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Display.Core and Lux.Display.",
                [],
                ApiTypes: [typeof(BxDisplay), typeof(BxAudioPlayer), typeof(BxVideoPlayer), typeof(BxFilePreviewProvider), typeof(FilePreviewService)])
        ]);

    private static LibraryDocTree BuildAgent() => new(
        "agent", "Lux.Agent", "#6366f1",
        [
            new("", "Introduction", "Getting Started",
                "Semantic AI Agent Bridge (snapshot, query, act, observe), composable browser behaviors, WebMCP tool registration, automatic [redacted] secret masking, permission policies, and bounded UndoStack<T> — inspired by ui-sdk.dev.",
                [
                    new("overview", "What is Lux.Agent?",
                        Paragraphs:
                        [
                            "DOM scraping and fragile CSS selectors break whenever layouts change, cannot inspect closed dropdowns without clicking popups, and risk leaking password fields into LLM contexts.",
                            "Lux.Agent implements a semantic bridge modeled after ui-sdk.dev: controllers register their fields, choices, and actions once, exposing three compact tools (snapshot, query, act) plus a unified Observe stream where sensitive inputs are always redacted as \"[redacted]\"."
                        ],
                        LiveDemoKey: "agent-bridge",
                        LiveDemoTitle: "Lux.Agent · Live Semantic Snapshot, ActAsync & UndoStack<T>"),
                    new("layers", "Two-Layer Package Split",
                        TableHeaders: ["Package", "Contains", "Targets"],
                        TableRows:
                        [
                            new("Lux.Agent.Core", "AgentComponentSnapshot, AgentQueryRequest, AgentActStep, AgentActResult, ObservedChange, AgentToolDefinition, UndoStack<T>", "Zero dependencies · NativeAOT"),
                            new("Lux.Agent", "AgentBridgeService, AddLuxAgent(), _content/Lux.Agent/lux-agent.js & lux-behaviors.js", "Blazor Web & .NET MAUI Hybrid")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Agent, register AgentBridgeService, and optionally load the WebMCP browser script.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Agent.Core   # Zero-dependency NativeAOT models & UndoStack<T>
                                dotnet add package Lux.Agent        # AgentBridgeService + WebMCP & Behaviors JS
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxAgent();")])
                ]),
            new("semantic-tools", "Snapshot, Query & Act", "Semantic Bridge",
                "Expose three deterministic tools via AgentBridgeService.GetToolDefinitions() for LLM tool-calling and WebMCP.",
                [
                    new("tools", "Tool Contracts & Hints",
                        TableHeaders: ["Tool", "Safety Hints", "Behavior"],
                        TableRows:
                        [
                            new("snapshot", "ReadOnlyHint=true, ConsequentialHint=false", "Lists up to 100 controls with stable ref (c1, c2…), state, Disabled/OffPage/ConfirmRequired flags, and allowed actions"),
                            new("query", "ReadOnlyHint=true, ConsequentialHint=false", "Finds controls accent-insensitively by Within/Text/Kind and lists up to 50 choices without opening popups"),
                            new("act", "ReadOnlyHint=false, ConsequentialHint=true", "Executes AgentActStep[] sequentially with ambiguity checks, policy validation, and structured outcomes")
                        ],
                        LiveDemoKey: "agent-bridge",
                        LiveDemoTitle: "Try running ActAsync & inspecting redacted Snapshot",
                        CodeSamples:
                        [
                            new("SemanticLoop.cs", """
                                // 1. Inspect current state
                                var snap = agent.Snapshot(includeOffPage: false);

                                // 2. Query select options accent-insensitively ("region" matches "Région")
                                var choices = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });

                                // 3. Execute semantic actions in sequence
                                var result = await agent.ActAsync([
                                    new AgentActStep("project name", "setValue", ["Atlas"]),
                                    new AgentActStep("region", "setValue", ["eu-west"])
                                ]);
                                """)
                        ])
                ]),
            new("registrations", "Registering Controllers", "Semantic Bridge",
                "Exact semantic actions exposed when binding Forms, Dashboards, and Spreadsheets.",
                [
                    new("actions-matrix", "Supported Actions per Registered Target",
                        TableHeaders: ["Registration Method", "Exposed Components", "Supported Actions"],
                        TableRows:
                        [
                            new("RegisterForm(form, confirmSubmit)", "input, select, checkbox, number, form", "setValue, clear, toggle, reset, submit, nextStep, previousStep"),
                            new("RegisterDashboard(dashboard)", "select (filters), dashboard", "setValue, toggle, clear, reset, setView, resetFilters, refresh"),
                            new("RegisterSpreadsheet(importer)", "spreadsheet", "editCell(row, field, text), nextStep, previousStep, submit, clearFile")
                        ],
                        CodeSamples:
                        [
                            new("RegisterAll.cs", """
                                agent.RegisterForm(form, confirmSubmit: true);
                                agent.RegisterDashboard(dashboard);
                                agent.RegisterSpreadsheet(importer);
                                """)
                        ])
                ]),
            new("policies", "Permission Policies (AllowPolicy)", "Semantic Bridge",
                "Guard consequential actions with global or per-call asynchronous permission policies.",
                [
                    new("gate", "AllowPolicy & ConfirmRequired",
                        Paragraphs: ["Every step in ActAsync() is checked against both the global agent.AllowPolicy and any per-call allow delegate. Targets marked ConfirmRequired (such as form submit when confirmSubmit: true) pass confirmRequired = true to the policy callback so you can prompt the user before proceeding."],
                        CodeSamples:
                        [
                            new("PermissionGate.cs", """
                                agent.AllowPolicy = async (step, confirmRequired) =>
                                {
                                    if (!confirmRequired) return true;
                                    return await confirmDialog.AskAsync($"Allow agent to run '{step.Action}' on '{step.Ref}'?");
                                };
                                """)
                        ])
                ]),
            new("observe-and-redaction", "Observe Stream & Secret Redaction", "Semantic Bridge",
                "Subscribe to a unified change stream with guaranteed [redacted] protection for sensitive fields.",
                [
                    new("sources", "All 7 AgentChangeSource Values",
                        TableHeaders: ["Source", "Origin", "Notes"],
                        TableRows:
                        [
                            new("Keyboard / Mouse / Touch / Pen", "Direct human interaction", "Distinguished from automated updates"),
                            new("Virtual", "Assistive technology / screen reader", "Preserves accessibility provenance"),
                            new("Programmatic", "Application C# / JS code", "Distinguishes app logic from agent steps"),
                            new("Agent", "AgentBridgeService.ActAsync()", "Tagged automatically on every agent-driven mutation")
                        ],
                        CodeSamples:
                        [
                            new("ObserveStream.cs", """
                                using var sub = agent.Observe(change =>
                                    logger.LogInformation("[{Source}] {Label}: {Prev} -> {Next}",
                                        change.Source, change.Label, change.Previous, change.Value));
                                """)
                        ],
                        Callout: "Password and OneTimeCode fields always report AgentBridgeService.RedactedValue (\"[redacted]\") in Snapshot(), ActResult.Values, and ObservedChange — plaintext secrets never leave the controller.")
                ]),
            new("undo-stack", "UndoStack<T>", "State & Browser",
                "Zero-dependency bounded undo/redo stack in Lux.Agent.Core (NativeAOT compatible).",
                [
                    new("undo", "Record, Undo, Redo & Clear",
                        Paragraphs: ["UndoStack<T> tracks immutable state snapshots with a configurable max depth (default 50). Recording a new state that equals Current is a no-op; recording a distinct state clears the redo branch."],
                        CodeSamples:
                        [
                            new("UndoStackExample.cs", """
                                var history = new UndoStack<string>("Initial", limit: 50);
                                history.Record("Draft v2");
                                history.Record("Published v3");

                                history.Undo(); // Current == "Draft v2", CanRedo == true
                                history.Redo(); // Current == "Published v3"
                                history.Clear();
                                """)
                        ])
                ]),
            new("webmcp-and-behaviors", "WebMCP & Composable Behaviors", "State & Browser",
                "Browser-side semantic bridge with WebMCP tool registration and standalone headless DOM behaviors.",
                [
                    new("webmcp", "Static Assets in _content/Lux.Agent/",
                        Bullets:
                        [
                            "_content/Lux.Agent/lux-agent.js — Exposes window.Blux.agent (snapshot, query, act, observe) and registers tools with navigator.modelContext (WebMCP) when available.",
                            "_content/Lux.Agent/lux-behaviors.js — Standalone DOM behavior primitives: dialog(), popup(), rovingTabindex(), formAssociated(), and dismissable()."
                        ],
                        CodeSamples:
                        [
                            new("WebMcpBrowser.js", """
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
            new("recipes", "Testing & Recipes", "Platforms",
                "Verify redaction, accent-insensitive queries, ambiguity rejection, and policy refusal in plain xUnit.",
                [
                    new("xunit", "xUnit Verification Pattern",
                        CodeSamples:
                        [
                            new("AgentUiSdkTests.cs", """
                                var snap = agent.Snapshot();
                                Assert.Equal(AgentBridgeService.RedactedValue, snap.First(s => s.Label == "API Secret").Value);

                                agent.AllowPolicy = (_, confirmRequired) => Task.FromResult(!confirmRequired);
                                var refused = await agent.ActAsync([new AgentActStep("Project Settings", "submit")]);
                                Assert.False(refused.Ok);
                                Assert.Contains("refused by policy", refused.Error);
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Agent.Core and Lux.Agent.",
                [],
                ApiTypes: [typeof(AgentBridgeService), typeof(AgentComponentSnapshot), typeof(AgentQueryRequest), typeof(AgentActStep), typeof(AgentActResult), typeof(ObservedChange), typeof(UndoStack<object>)])
        ]);
}
