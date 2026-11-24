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
                        Bullets:
                        [
                            "Lux.Form.Core — 100% headless C#, zero UI dependency, NativeAOT compatible. Contains FormSchema, FormFieldSchema, FormStepSchema, FormController, and FormService.",
                            "Lux.Form — Multiplatform Razor Class Library providing BxFormEngine, BxFormPage, BxFormProvider, and AddLuxForm()."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Form, register FormService, and mount BxFormProvider for programmatic modal/drawer forms.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Form.Core
                                dotnet add package Lux.Form
                                """)
                        ]),
                    new("di", "2. Register Services & Overlay Provider",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxForm();"),
                            new("MainLayout.razor", "<BxFormProvider />")
                        ])
                ]),
            new("field-kinds", "FormSchema & 31 Field Kinds", "Schema & State",
                "Model complex forms declaratively without hand-writing repetitive input bindings.",
                [
                    new("kinds", "Field Kind Categories",
                        TableHeaders: ["Category", "FormFieldKind Values", "Highlights"],
                        TableRows:
                        [
                            new("Text & Auth", "Text, Email, Password, Textarea, OneTimeCode", "Live password requirement checklist, masking & trim transforms"),
                            new("Numeric & Range", "Number, Slider, Rating", "Min/Max bounds, step precision & numeric parsing"),
                            new("Selection", "Select, MultiSelect, AutoComplete, Radio, Checkbox, Switch, Tags", "Static or dynamic FormOptionItem lists"),
                            new("Structure", "Array, Tabs, Heading, Divider, Alert", "Repeatable nested sub-forms and grouped sections")
                        ])
                ]),
            new("conditions", "Reactive Conditions & Transforms", "Schema & State",
                "Evaluate visibility, disabled states, and output normalization dynamically on every state change.",
                [
                    new("reactive", "Condition & TransformOutput",
                        Paragraphs: ["When Condition evaluates to false, the field is hidden from rendering, excluded from validation, and omitted from BuildOutput()."],
                        CodeSamples:
                        [
                            new("ReactiveFields.cs", """
                                new FormFieldSchema { Key = "name", Label = "Full Name", Required = true, TransformOutput = v => v?.ToString()?.Trim() },
                                new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, DefaultValue = 25.0 },
                                new FormFieldSchema { Key = "company", Label = "Company", Required = true, Condition = f => f.GetValue<double>("age") >= 21 }
                                """)
                        ])
                ]),
            new("validation", "Validation & Password Rules", "Schema & State",
                "Combine required/min/max constraints, custom rules, validation triggers, and live password requirement meters.",
                [
                    new("password", "Password Requirements",
                        CodeSamples:
                        [
                            new("PasswordRules.cs", """
                                new FormFieldSchema
                                {
                                    Key = "password",
                                    Label = "Password",
                                    Kind = FormFieldKind.Password,
                                    PasswordRequirements =
                                    [
                                        new FormPasswordRequirement("len", "At least 8 characters", s => s.Length >= 8),
                                        new FormPasswordRequirement("num", "Contains a digit", s => s.Any(char.IsDigit))
                                    ]
                                }
                                """)
                        ])
                ]),
            new("dirty-tracking", "Dirty Tracking & Diffing", "Schema & State",
                "Track exact modified field paths against the initial snapshot and reset cleanly after submission.",
                [
                    new("dirty", "IsDirty & DirtyPaths",
                        Paragraphs: ["FormController snapshots initial values on construction. Inspect IsDirty and DirtyPaths to guard navigation or build minimal PATCH payloads. Successful SubmitAsync() re-baselines the snapshot automatically."],
                        CodeSamples:
                        [
                            new("DirtyCheck.cs", """
                                controller.SetValue("age", 19.0, markTouched: true);
                                Assert.True(controller.IsDirty);
                                Assert.Contains("age", controller.DirtyPaths);

                                await controller.SubmitAsync();
                                Assert.False(controller.IsDirty);
                                """)
                        ])
                ]),
            new("wizards", "Multi-Step Wizards (SkipWhen)", "Schema & State",
                "Break long flows into validated steps with conditional branching via SkipWhen.",
                [
                    new("steps", "Stepped FormSchema",
                        CodeSamples:
                        [
                            new("Wizard.cs", """
                                var wizard = new FormSchema
                                {
                                    FormKey = "onboarding",
                                    Title = "Workspace Onboarding",
                                    Steps =
                                    [
                                        new FormStepSchema { Key = "account", Title = "Account", Fields = [new() { Key = "plan", DefaultValue = "free" }] },
                                        new FormStepSchema { Key = "billing", Title = "Billing", SkipWhen = f => f.GetValue<string>("plan") == "free", Fields = [new() { Key = "vat", Required = true }] }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("form-engine", "BxFormEngine", "UI Components",
                "Automatic Razor renderer for single-page and multi-step FormController instances.",
                [
                    new("engine", "Rendering BxFormEngine",
                        CodeSamples:
                        [
                            new("Profile.razor", """
                                <BxFormEngine Form="@_controller"
                                              SubmitLabel="Save profile"
                                              ShowReset="true"
                                              OnSubmit="@HandleSubmit" />
                                """)
                        ])
                ]),
            new("form-page-and-service", "BxFormPage & FormService Overlays", "UI Components",
                "Build sectioned settings pages with completion badges or open forms programmatically in modals and drawers.",
                [
                    new("service", "FormService & BxFormProvider",
                        Paragraphs: ["Inject FormService to open any FormSchema in a modal or slide-over drawer and await the typed FormSubmitOutcome directly in C#."],
                        CodeSamples:
                        [
                            new("OverlayForm.cs", """
                                var outcome = await formService.OpenAsync(schema, initialValues, FormShellMode.Modal);
                                if (outcome.Submitted) { /* use outcome.Output */ }
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
                        Bullets:
                        [
                            "Lux.Dashboard.Core — 100% headless C#, NativeAOT compatible. Contains DashboardController, DashboardResource<T>, DashboardFilterControl, DashboardFormat, and all analytical models.",
                            "Lux.Dashboard — Multiplatform RCL containing 13 analytical blocks and AddLuxDashboard()."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Dashboard and register services.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Dashboard.Core
                                dotnet add package Lux.Dashboard
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs", "builder.Services.AddLuxDashboard();")])
                ]),
            new("staged-queries", "Staged 3-Wave Queries", "Orchestration",
                "Prioritize above-the-fold KPIs while loading heavy charts in the background and keeping secondary tabs lazy.",
                [
                    new("waves", "Essential -> Background -> Deferred",
                        Bullets:
                        [
                            "DashboardQueryStage.Essential — Executes first on RefreshAsync() for top-line executive metrics.",
                            "DashboardQueryStage.Background — Executes after Essential settles for charts, bars, and lists.",
                            "DashboardQueryStage.Deferred — Stays Idle until explicitly loaded on demand."
                        ],
                        CodeSamples:
                        [
                            new("StagedController.cs", """
                                var controller = new DashboardController("analytics");
                                var kpis = controller.RegisterResource(new DashboardResource<int>("summary", LoadSummaryAsync, DashboardQueryStage.Essential));
                                var trend = controller.RegisterResource(new DashboardResource<double[]>("trend", LoadTrendAsync, DashboardQueryStage.Background));
                                var audit = controller.RegisterResource(new DashboardResource<string>("audit", LoadAuditAsync, DashboardQueryStage.Deferred));

                                await controller.RefreshAsync(); // Runs Essential -> Background; audit stays Idle
                                """)
                        ])
                ]),
            new("filters-and-views", "Global Filters, Presets & Views", "Orchestration",
                "Coordinate shared filter controls, automatic Reset state detection, and badged dashboard views.",
                [
                    new("filters", "DashboardFilterControl & Views",
                        CodeSamples:
                        [
                            new("FiltersAndViews.cs", """
                                controller.AddView("overview", "Overview", badge: "3");
                                controller.AddView("details", "Details");

                                var year = controller.AddFilter(new DashboardFilterControl(
                                    "year", "Year",
                                    items: [new("2025", "2025"), new("2026", "2026")],
                                    defaultValues: ["2026"]));

                                year.Toggle("2025");
                                // controller.Filtered == true -> Reset button appears automatically
                                """)
                        ])
                ]),
            new("formatting-and-csv", "Formatting, Comparison Ranges & CSV", "Orchestration",
                "Consistent number formatting, leap-year-safe date comparison windows, and localized UTF-8 BOM CSV export.",
                [
                    new("format", "DashboardFormat Utilities",
                        CodeSamples:
                        [
                            new("DashboardFormat.cs", """
                                DashboardFormat.Format(12600, DashboardFormatPreset.Compact, CultureInfo.InvariantCulture); // "12.6K"
                                DashboardFormat.Format(14.2, DashboardFormatPreset.Delta, CultureInfo.InvariantCulture);    // "+14.2%"
                                DashboardFormat.Format(2.1, DashboardFormatPreset.Points, CultureInfo.InvariantCulture);    // "+2.1 pts"
                                DashboardFormat.Format(0.57, DashboardFormatPreset.Ratio, CultureInfo.InvariantCulture);    // "57%"

                                // Leap year Feb 29, 2024 -> Feb 28, 2023
                                var prevYear = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);
                                var csv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
                                """)
                        ])
                ]),
            new("layout-blocks", "Page, Grid & Card", "Visualization Blocks",
                "Compose analytical dashboards with BxDashboardPage, BxDashboardGrid, and BxDashboardCard.",
                [
                    new("shell", "Analytical Shell",
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
                "Highlight KPIs with sparklines, comparison deltas, goal progress bars, and circular SVG gauges.",
                [
                    new("kpi", "BxDashboardStat, BxDashboardStats & BxDashboardGauge",
                        CodeSamples:
                        [
                            new("Kpis.razor", """
                                <BxDashboardStats Title="Executive Metrics" Items="@_stats" />
                                <BxDashboardGauge Title="Q4 ARR Goal" Value="86" Max="100" Target="90"
                                                  Format="DashboardFormatPreset.Percent" Caption="86% reached" />
                                """)
                        ])
                ]),
            new("distributions", "Bars, List & Details", "Visualization Blocks",
                "Display ranked breakdowns, horizontal progress bars, and structured metric details.",
                [
                    new("bars", "BxDashboardBars, BxDashboardList & BxDashboardDetails",
                        CodeSamples:
                        [
                            new("Distributions.razor", """
                                <BxDashboardBars Title="Channels" Items="@_bars" Format="DashboardFormatPreset.Compact" />
                                <BxDashboardList Title="Top Accounts" Items="@_accounts" />
                                """)
                        ])
                ]),
            new("tables-and-feeds", "Table, Chart, Alerts & Feed", "Visualization Blocks",
                "Interactive sortable analytical tables with inline bars/deltas, severity alerts, activity feeds, and chart cards.",
                [
                    new("table", "BxDashboardTable",
                        CodeSamples:
                        [
                            new("RegionsTable.razor", """
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
                        Bullets:
                        [
                            "Lux.Display.Core — 100% headless C#, NativeAOT compatible. Provides FilePreviewService, MIME/extension detection, RFC 4180 CSV parser, and media rule/progress models.",
                            "Lux.Display — Multiplatform RCL containing BxDisplay, BxAudioPlayer, BxVideoPlayer, BxFilePreviewProvider, and AddLuxDisplay()."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Display, register FilePreviewService, and mount BxFilePreviewProvider.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Display.Core
                                dotnet add package Lux.Display
                                """)
                        ]),
                    new("di", "2. Register Services & Lightbox Host",
                        CodeSamples:
                        [
                            new("Program.cs", "builder.Services.AddLuxDisplay();"),
                            new("MainLayout.razor", "<BxFilePreviewProvider />")
                        ])
                ]),
            new("content-blocks", "Typography, Alerts & Facts", "Display Blocks",
                "Render headings with counts, expandable alerts, callouts, and copyable key/value fact lists.",
                [
                    new("facts", "Heading, Alert & Facts",
                        CodeSamples:
                        [
                            new("ContentBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Heading" Title="Active Subscriptions" Count="42" Description="Updated 5m ago" />
                                <BxDisplay Kind="DisplayKind.Alert" Tone="BxColor.Warning" Title="Quota warning" Details="85% of monthly volume used" />
                                <BxDisplay Kind="DisplayKind.Facts" Facts="@([new DisplayFact { Label = "Environment", Value = "Production", Copy = true }])" />
                                """)
                        ])
                ]),
            new("metrics-and-diff", "Stats, Breakdown, Meter & Diff", "Display Blocks",
                "Present inline KPIs with sparklines, threshold meters, and before/after diffs.",
                [
                    new("metrics", "Stat, Meter & Diff",
                        CodeSamples:
                        [
                            new("Metrics.razor", """
                                <BxDisplay Kind="DisplayKind.Stat" Title="MRR" Value="€148,900" Delta="+14.2%" Trend="@([102.0, 118.0, 134.0, 148.9])" />
                                <BxDisplay Kind="DisplayKind.Meter" Title="Storage" MeterValue="92" MeterMax="100" WarningThreshold="75" ErrorThreshold="90" />
                                <BxDisplay Kind="DisplayKind.Diff" DiffItems="@([new DisplayDiffItem("Plan", "Starter", "Enterprise", Changed: true)])" />
                                """)
                        ])
                ]),
            new("code-and-lists", "Code, Secret Masking & Lists", "Display Blocks",
                "Mask sensitive tokens with reveal toggles and paginate long lists with incremental Reveal More.",
                [
                    new("secret", "Secret Masking & Copy",
                        CodeSamples:
                        [
                            new("Secret.razor", """
                                <BxDisplay Kind="DisplayKind.Code" Variant="secret" Title="API Secret" Value="sk_live_9876543210" VisibleChars="4" Copy="true" />
                                <BxDisplay Kind="DisplayKind.List" Max="2" Step="2" ListItems="@_steps" />
                                """)
                        ])
                ]),
            new("structured-blocks", "Timeline, People, Contacts & Accordion", "Display Blocks",
                "Display chronological timelines, team directories, contact cards, and collapsible accordions.",
                [
                    new("structured", "Structured Read-Only Blocks",
                        Paragraphs: ["Use DisplayKind.Timeline, DisplayKind.People, DisplayKind.Contacts, DisplayKind.Records, and DisplayKind.Accordion with consistent surfaces and loading skeletons."])
                ]),
            new("audio-player", "Constrained Audio Player", "Media & Files",
                "Play bounded audio segments with play-count quotas and seek locking for assessments and courses.",
                [
                    new("audio", "BxAudioPlayer",
                        CodeSamples:
                        [
                            new("Audio.razor", """
                                <BxAudioPlayer Src="/media/listening-test.mp3"
                                               Title="Listening Extract #1"
                                               Start="15" End="45"
                                               MaxPlays="2" Seekable="false"
                                               OnLimitReached="@HandleQuotaReached" />
                                """)
                        ])
                ]),
            new("video-player", "Constrained Video Player", "Media & Files",
                "Play video clips with subtitle tracks, Start/End clip bounds, and play-quota enforcement.",
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
                "Open multi-file modal previews for images, PDFs, audio, video, code, and RFC 4180 CSV tables.",
                [
                    new("lightbox", "Programmatic Preview & CSV Parser",
                        CodeSamples:
                        [
                            new("Preview.cs", """
                                filePreview.Open(
                                    new FilePreviewItem("spec.pdf", "/files/spec.pdf"),
                                    new FilePreviewItem("rates.csv", "/files/rates.csv"));

                                var parsed = FilePreviewService.ParseCsv("Region,MRR\nEU,148900\nUS,192400");
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Display", "Lux.Display.Core", "Lux.Display", "builder.Services.AddLuxDisplay();",
                "var csv = FilePreviewService.ParseCsv(rawText);"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Display.Core and Lux.Display.",
                [],
                ApiTypes: [typeof(BxDisplay), typeof(BxAudioPlayer), typeof(BxVideoPlayer), typeof(BxFilePreviewProvider), typeof(FilePreviewService)])
        ]);

    private static LibraryDocTree BuildAgent() => new(
        "agent", "Lux.Agent", "#6366f1",
        [
            new("", "Introduction", "Getting Started",
                "Semantic AI Agent Bridge (snapshot, query, act, observe), composable headless browser behaviors, WebMCP tool registration, automatic [redacted] secret masking, permission policies, and bounded UndoStack<T>.",
                [
                    new("overview", "What is Lux.Agent?",
                        Bullets:
                        [
                            "Lux.Agent.Core — Zero-dependency, NativeAOT compatible models (AgentComponentSnapshot, AgentQueryRequest, AgentActStep, ObservedChange, UndoStack<T>).",
                            "Lux.Agent — Multiplatform RCL shipping AgentBridgeService, AddLuxAgent(), and static web assets _content/Lux.Agent/lux-agent.js & lux-behaviors.js."
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Agent and register AgentBridgeService.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Agent.Core
                                dotnet add package Lux.Agent
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs", "builder.Services.AddLuxAgent();")])
                ]),
            new("semantic-tools", "Snapshot, Query & Act", "Semantic Bridge",
                "Expose deterministic, token-efficient UI tools instead of brittle CSS selectors or screenshots.",
                [
                    new("tools", "Three Standard Tools (GetToolDefinitions)",
                        TableHeaders: ["Tool", "Hints", "Behavior"],
                        TableRows:
                        [
                            new("snapshot", "ReadOnlyHint=true", "Lists up to 100 controls with stable refs (c1, c2…), state, flags, and actions"),
                            new("query", "ReadOnlyHint=true", "Matches controls accent-insensitively and lists choices without opening popups"),
                            new("act", "ConsequentialHint=true", "Executes semantic actions in sequence and returns outcomes + updated values")
                        ],
                        CodeSamples:
                        [
                            new("SemanticLoop.cs", """
                                var snap = agent.Snapshot();
                                var choices = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });
                                var result = await agent.ActAsync([
                                    new AgentActStep("project name", "setValue", ["Atlas"]),
                                    new AgentActStep("region", "setValue", ["eu-west"])
                                ]);
                                """)
                        ])
                ]),
            new("registrations", "Registering Controllers", "Semantic Bridge",
                "Bind headless state controllers to AgentBridgeService in one line each.",
                [
                    new("bind", "Supported Registrations",
                        CodeSamples:
                        [
                            new("Register.cs", """
                                agent.RegisterForm(form, confirmSubmit: true);
                                agent.RegisterDashboard(dashboard);
                                agent.RegisterSpreadsheet(importer);
                                """)
                        ])
                ]),
            new("policies", "Permission Policies (AllowPolicy)", "Semantic Bridge",
                "Approve, confirm, or refuse consequential agent actions before they mutate state.",
                [
                    new("gate", "Global & Per-Call Policies",
                        CodeSamples:
                        [
                            new("AllowPolicy.cs", """
                                agent.AllowPolicy = (step, confirmRequired) =>
                                    Task.FromResult(!confirmRequired || userApproved);
                                """)
                        ])
                ]),
            new("observe-and-redaction", "Observe Stream & Secret Redaction", "Semantic Bridge",
                "Capture every semantic state transition while keeping passwords and one-time codes redacted.",
                [
                    new("observe", "Unified Observe Stream",
                        Paragraphs: ["Password and OneTimeCode fields always emit AgentBridgeService.RedactedValue (\"[redacted]\") in both Snapshot() and Observe() callbacks."],
                        CodeSamples:
                        [
                            new("Observe.cs", """
                                using var sub = agent.Observe(change =>
                                    Console.WriteLine($"{change.Source}: {change.Label} {change.Previous} -> {change.Value}"));
                                """)
                        ])
                ]),
            new("undo-stack", "UndoStack<T>", "State & Browser",
                "Bounded generic undo/redo snapshot stack in Lux.Agent.Core (zero dependencies, NativeAOT).",
                [
                    new("undo", "Record, Undo & Redo",
                        CodeSamples:
                        [
                            new("UndoStackExample.cs", """
                                var history = new UndoStack<string>("Initial", limit: 50);
                                history.Record("Draft v2");
                                history.Record("Published v3");

                                history.Undo(); // Current == "Draft v2"
                                history.Redo(); // Current == "Published v3"
                                """)
                        ])
                ]),
            new("webmcp-and-behaviors", "WebMCP & Composable Behaviors", "State & Browser",
                "Expose semantic tools directly to browser AI agents via navigator.modelContext and compose native DOM behaviors.",
                [
                    new("webmcp", "Static Web Assets (_content/Lux.Agent/)",
                        CodeSamples:
                        [
                            new("BrowserWebMcp.js", """
                                // _content/Lux.Agent/lux-agent.js
                                const snap = window.Blux.agent.snapshot();
                                await window.Blux.agent.act([{ ref: 'Project name', action: 'setValue', args: ['Atlas'] }]);
                                const stop = window.Blux.agent.observe(evt => console.log(evt));
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Agent", "Lux.Agent.Core", "Lux.Agent", "builder.Services.AddLuxAgent();",
                "var history = new UndoStack<DocumentState>(initial, limit: 50);"),
            new("recipes", "Testing & Recipes", "Platforms",
                "Verify redaction, accent-insensitive queries, and policy gates in plain xUnit tests.",
                [
                    new("xunit", "xUnit Verification",
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
                "Complete public API reference for Lux.Agent.Core and Lux.Agent.",
                [],
                ApiTypes: [typeof(AgentBridgeService), typeof(AgentComponentSnapshot), typeof(AgentQueryRequest), typeof(AgentActStep), typeof(AgentActResult), typeof(ObservedChange), typeof(UndoStack<object>)])
        ]);
}
