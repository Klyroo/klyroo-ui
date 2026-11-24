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
            Description: "Design system complet pour Blazor (Server, WASM, Auto, SSR) et .NET MAUI Blazor Hybrid. Les menus, dialogues, popovers et onglets s'exécutent à 60fps dans le navigateur via Web Components natifs et Behaviors composables (dialog, popup, rovingTabindex, formAssociated).",
            AccentHex: "#10b981",
            BadgeColor: BxColor.Success,
            RepoName: "lux-ui",
            RepoPath: "/home/gpoumarat/lux-ui",
            CorePackage: "Blux.UI.Build.Tasks",
            UiPackage: "Blux.UI",
            Platforms: ["Blazor Server", "Blazor WASM", "Blazor Auto / SSR", ".NET MAUI Hybrid (iOS, Android, Mac, Win)", "Android TV (D-Pad)"],
            Features:
            [
                ("Zero-Roundtrip Popovers & Dialogs", "Basé sur les API natives HTML Popover et <dialog> avec light-dismiss et pile top-layer gérée directement par le navigateur.", "DOM Native"),
                ("Composable Behaviors (ui-sdk.dev)", "Primitives JS déclaratives : dialog(), popup(), rovingTabindex(), formAssociated() et dismissable() sans dépendance externe.", "Behaviors"),
                ("Tailwind v4 & Design Tokens", "Un seul thème piloté par variables CSS (--blux-primary, --blux-bg, --blux-border), 12 locales intégrées et mode sombre instantané.", "Tailwind v4"),
                ("Mobile 44px Touch & TV D-Pad", "Zones tactiles 44px calibrées pour iOS/Android, safe areas et anneau de focus directionnel pour Android TV.", "Multiplatform")
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
            Description: "Moteur de requêtage fortement typé et 100% NativeAOT : AST de filtres composables (DataListSchema, DataListRequest, DataListFilterGroup), codecs d'URL, exécuteur InMemory et traduction SQL Entity Framework Core (EfAsyncQueryExecutor), accompagné de QueryState pour synchroniser l'URL dans Blazor et MAUI Hybrid.",
            AccentHex: "#ff4154",
            BadgeColor: BxColor.Error,
            RepoName: "lux-query",
            RepoPath: "/home/gpoumarat/lux-query",
            CorePackage: "Lux.Query.Core",
            UiPackage: "Lux.Query",
            Platforms: ["Headless C# / NativeAOT", ".NET MAUI Native C#", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid", "Entity Framework Core"],
            Features:
            [
                ("Typed Schema & Filter AST", "Décrivez champs triables, filtres typés et recherche plein texte avec DataListSchema.For<T>() sans aucune chaîne magique.", "Typed AST"),
                ("InMemory, IQueryable & EF Core", "Exécutez le même DataListRequest en mémoire (ExecuteInMemory), sur LINQ IQueryable ou en SQL asynchrone via EfAsyncQueryExecutor.", "Any Backend"),
                ("URL Query Codecs & QueryState", "Sérialisation réversible des filtres, tris et pages dans la query-string avec gestion de l'historique et support mobile WebView.", "URL Sync"),
                ("100% Headless & NativeAOT", "Lux.Query.Core n'a aucune dépendance UI : compilez-le en NativeAOT, dans vos APIs minimales, Workers ou ViewModels MAUI.", "NativeAOT")
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
                new("DataListSchema", "Fluent typed schema builder for searchable, sortable and filterable fields.", "/docs/datalist-sources", "Core"),
                new("DataListRequest", "Immutable paging (Offset/Cursor), sorting, filtering & search AST.", "/docs/datalist-sources", "Core"),
                new("QueryCodecs", "Reversible URL search parameter serialization for complex filter trees.", "/docs/url-state", "Core"),
                new("EfAsyncQueryExecutor", "Translates DataListRequest directly to async SQL via Entity Framework Core.", "/docs/datalist-sources", "Core"),
                new("QueryState", "Reactive URL state manager for Blazor Web and .NET MAUI BlazorWebView.", "/docs/url-state", "UI")
            ]),

        new(
            Slug: "table",
            Name: "Lux.Table",
            ShortName: "Table",
            Tagline: "Headless tree/grouping models & high-density enterprise BxDataList",
            HeroHeadline: "Supercharged tables",
            HeroAccent: "& headless datagrids.",
            Description: "L'équivalent .NET de TanStack Table combiné à la finition de Nuxt UI Tools : table, grille de cartes, hiérarchie arborescente (DataListTree), regroupement, agrégations de pied de page, barre de progression fine 2px, menu d'en-tête Tri/Masquer, virtualisation 60fps et vues sauvegardées.",
            AccentHex: "#00d8ff",
            BadgeColor: BxColor.Info,
            RepoName: "lux-table",
            RepoPath: "/home/gpoumarat/lux-table",
            CorePackage: "Lux.Table.Core",
            UiPackage: "Lux.Table",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid", "Entity Framework Core"],
            Features:
            [
                ("Headless Tree Hierarchy (NativeAOT)", "Lux.Table.Core construit et aplatit des arbres hiérarchiques (DataListTree<T>) avec profondeur et rails visuels, sans dépendance UI.", "Headless Tree"),
                ("Nuxt UI Tools Polish Included", "Barre de progression 2px pendant le chargement, menu d'en-tête auto (Tri Asc/Desc/Masquer), skeletons adaptatifs et EmptyState 1er usage.", "UX Polish"),
                ("Grouping, Summaries & Pinning", "Regroupement visuel (DataListGroupBy), agrégations Sum/Avg/Min/Max en pied de tableau, redimensionnement et épinglage de colonnes.", "Enterprise Grid"),
                ("Responsive Summary Rows on Phone", "Bascule automatiquement en lignes résumées scannables (DataListRowSummary) sous 768px sur smartphones iOS/Android.", "Mobile Ready")
            ],
            CoreCodeTitle: "Lux.Table.Core · Headless DataListTree<T>",
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
                new("DataListTree<TItem>", "Pure headless NativeAOT hierarchy builder with rail continuations & expansion.", "/components/data-list", "Core"),
                new("BxDataList<TItem>", "Full-featured enterprise datagrid with table, grid, filter bar & pagination.", "/components/data-list", "UI"),
                new("BxDataListTable<TItem>", "High-density table with HeaderMenu, summaries, tree view and column resizing.", "/components/data-list", "UI"),
                new("DataListRowSummary<TItem>", "Automatic mobile summary-row presentation below the md breakpoint.", "/components/data-list", "UI")
            ]),

        new(
            Slug: "form",
            Name: "Lux.Form",
            ShortName: "Form",
            Tagline: "31-kind schema-driven form engine, reactive conditions, dirty diffing & wizards",
            HeroHeadline: "Schema-driven forms",
            HeroAccent: "without boilerplate.",
            Description: "Moteur de formulaires déclaratif 100% NativeAOT dans Lux.Form.Core : 31 types de champs (FormFieldKind), visibilité conditionnelle réactive, suivi dirty/touched précis, transformations de sortie, assistants multi-étapes avec SkipWhen, FormPage et modales programmatiques via FormService.",
            AccentHex: "#f59e0b",
            BadgeColor: BxColor.Warning,
            RepoName: "lux-form",
            RepoPath: "/home/gpoumarat/lux-form",
            CorePackage: "Lux.Form.Core",
            UiPackage: "Lux.Form",
            Platforms: ["Headless C# / NativeAOT", ".NET MAUI Native C#", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid"],
            Features:
            [
                ("31 Declarative Field Kinds", "Text, Password (avec jauge d'exigences), Number, Select, AutoComplete, Checkbox, Switch, Slider, Repeatable Arrays et Tabs pilotés par FormSchema.", "31 Field Kinds"),
                ("Reactive Conditions & Output Transforms", "Évalue Condition = f => ... en temps réel, ignore les champs masqués à la validation et applique TransformOutput au submit.", "Reactive Engine"),
                ("Multi-Step Wizards with SkipWhen", "Découpage en FormStepSchema avec saut conditionnel d'étapes (SkipWhen), validation par palier et re-baselining dirty après soumission.", "Wizard Flow"),
                ("FormPage & Overlay FormService", "Sections suivies (BxFormPage) et ouverture programmatique de formulaires en modale ou tiroir via FormService + BxFormProvider.", "Overlay Host")
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
                new("FormSchema", "Declarative schema with 31 field kinds, steps, sections, rules & transforms.", "/components/form", "Core"),
                new("FormController", "Headless NativeAOT state machine for values, errors, dirty paths & steps.", "/components/form", "Core"),
                new("FormService", "Programmatic overlay service to open forms in modals or drawers.", "/components/form", "Core"),
                new("BxFormEngine", "Automatic Blazor/MAUI renderer for FormSchema & multi-step wizards.", "/components/form", "UI"),
                new("BxFormPage", "Sectioned form page layout with live completion & dirty status.", "/components/form", "UI")
            ]),

        new(
            Slug: "dashboard",
            Name: "Lux.Dashboard",
            ShortName: "Dashboard",
            Tagline: "Staged 3-wave analytical queries, global filters, comparison ranges & 13 KPI blocks",
            HeroHeadline: "Analytical dashboards",
            HeroAccent: "staged for speed.",
            Description: "Orchestrateur de tableaux de bord analytiques avec chargement étagé en 3 vagues (Essential → Background → Deferred), barre de filtres et vues réactives, plages de comparaison (année bissextile gérée), export CSV RFC-4180 et 13 composants analytiques prêts à l'emploi.",
            AccentHex: "#8b5cf6",
            BadgeColor: BxColor.Primary,
            RepoName: "lux-dashboard",
            RepoPath: "/home/gpoumarat/lux-dashboard",
            CorePackage: "Lux.Dashboard.Core",
            UiPackage: "Lux.Dashboard",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid (Tablet, Desktop, Mobile)"],
            Features:
            [
                ("3-Wave Staged Query Orchestration", "DashboardController exécute d'abord Essential (KPI critiques), puis Background (graphes), et garde Deferred en chargement paresseux.", "Staged Waves"),
                ("Global Filters, Presets & Views", "Filtres synchronisés (DashboardFilterControl) avec bouton Reset automatique et onglets de vues (DashboardViewMeta) avec badges.", "Reactive Filters"),
                ("13 Analytical Visualization Blocks", "BxDashboardPage, Grid, Card, Stat, Stats, Gauge SVG, Details, Bars, List, Table triable avec export CSV, Alerts, Feed et Chart.", "13 Widgets"),
                ("Smart Formatters & CSV Export", "DashboardFormat formate Compact (12.6K), Delta (+14.2%), Points (+2.1 pts), Ratio (57%), plages de comparaison et CSV UTF-8 BOM.", "Analytics Core")
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
                new("DashboardController", "Staged 3-wave query coordinator (Essential/Background/Deferred), filters & views.", "/components/dashboard-panel", "Core"),
                new("DashboardFormat", "Compact/Delta/Points/Ratio formatters, leap-year ranges & BOM CSV exporter.", "/components/dashboard-panel", "Core"),
                new("BxDashboardPage", "Full analytical dashboard shell with filter toolbar, views & refresh state.", "/components/dashboard-panel", "UI"),
                new("BxDashboardStats", "Multi-KPI summary strip with delta badges, progress and status pills.", "/components/dashboard-panel", "UI"),
                new("BxDashboardGauge", "SVG circular gauge with target marker and formatted center value.", "/components/dashboard-panel", "UI"),
                new("BxDashboardTable", "Sortable analytical table block with inline bars, deltas & CSV export.", "/components/dashboard-panel", "UI")
            ]),

        new(
            Slug: "display",
            Name: "Lux.Display",
            ShortName: "Display",
            Tagline: "19 read-only display blocks, constrained audio/video players & modal file preview",
            HeroHeadline: "Rich display blocks",
            HeroAccent: "& constrained media.",
            Description: "19 blocs d'affichage en lecture seule dans <BxDisplay> (Heading, Alert, Facts, Stat avec sparkline, Breakdown, Meter avec seuils, Diff, Code/Secret masqué, Timeline, Accordion…), lecteurs Audio/Vidéo avec découpage temporel et quota d'écoutes (MaxPlays, Seekable=false), et prévisualiseur modal de fichiers.",
            AccentHex: "#ec4899",
            BadgeColor: BxColor.Secondary,
            RepoName: "lux-display",
            RepoPath: "/home/gpoumarat/lux-display",
            CorePackage: "Lux.Display.Core",
            UiPackage: "Lux.Display",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid"],
            Features:
            [
                ("19 Read-Only Display Blocks", "Heading, Text, Alert dépliable, Callout, Facts copiables, Stat avec sparkline, Breakdown, Meter à seuils, Diff avant/après, Timeline, Code/Secret.", "19 Blocks"),
                ("Constrained Audio & Video Players", "Supporte segment Start/End, quota MaxPlays (idéal pour examens/certifications), verrouillage du seek (Seekable=false) et pistes sous-titres.", "Media Quotas"),
                ("Multi-Format FilePreviewService", "Détection MIME/extension headless dans .Core, parseur CSV RFC-4180 intégré et lightbox modale zoomable (Image, PDF, Audio, Vidéo, CSV).", "File Lightbox"),
                ("Secret Masking & One-Click Reveal", "Mode DisplayKind.Code + Variant=\"secret\" qui masque les clés API (••••••••3210) avec bouton Révéler/Masquer et copie.", "Security UX")
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
                new("FilePreviewService", "Headless MIME detector, RFC 4180 CSV parser and lightbox queue state.", "/components/modal", "Core"),
                new("BxDisplay", "19-kind read-only block component (Stat, Facts, Meter, Diff, Code/Secret, Timeline).", "/components/card", "UI"),
                new("BxAudioPlayer", "Constrained audio player with Start/End clip bounds, MaxPlays & Seekable guard.", "/components/card", "UI"),
                new("BxVideoPlayer", "Constrained video player with tracks, clip bounds and play-limit locking.", "/components/card", "UI"),
                new("BxFilePreviewProvider", "Modal lightbox host for images, PDFs, audio, video and parsed CSV files.", "/components/modal", "UI")
            ]),

        new(
            Slug: "spreadsheet",
            Name: "Lux.Spreadsheet",
            ShortName: "Spreadsheet",
            Tagline: "Headless CSV/TSV importer, 20-row header detection, 80%-pass suggestions & batch retry",
            HeroHeadline: "Intelligent CSV import",
            HeroAccent: "& row reconciliation.",
            Description: "Moteur d'importation de tableurs 100% NativeAOT dans Lux.Spreadsheet.Core : détection heuristique de la ligne d'en-tête sur 20 lignes, normalisation NFD sans accents, suggestion de colonnes validées à ≥80 %, questions de valeurs inconnues, gestion des doublons/upserts, édition en ligne et soumission par lots avec relance des rejets.",
            AccentHex: "#14b8a6",
            BadgeColor: BxColor.Success,
            RepoName: "lux-spreadsheet",
            RepoPath: "/home/gpoumarat/lux-spreadsheet",
            CorePackage: "Lux.Spreadsheet.Core",
            UiPackage: "Lux.Spreadsheet",
            Platforms: ["Headless C# / NativeAOT", ".NET MAUI Native C#", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid"],
            Features:
            [
                ("20-Row Heuristic Header Detection", "Ignore automatiquement les lignes de titre ou métadonnées d'export Excel/CSV pour localiser la vraie ligne d'en-tête.", "Auto Header"),
                ("NFD Normalization & 80%-Pass Matching", "Normalise les en-têtes (accents, *, (required)) et suggère les colonnes manquantes uniquement si ≥80 % des lignes respectent le type et les règles.", "Smart Mapping"),
                ("Unknown Value Questions & Upsert Rules", "Invite l'utilisateur à mapper ou ignorer les valeurs inconnues et détecte doublons intra-fichier ou mises à jour d'enregistrements existants.", "Reconciliation"),
                ("Batch Submit & Retry Rejected Rows", "Exécute l'envoi par lots configurables et isole les lignes rejetées par l'API pour édition en ligne et relance ciblée.", "Batch Retry")
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
                new("SpreadsheetSchema", "Column definitions, rules, options, duplicate policies & existing record lookup.", "/components/file-upload", "Core"),
                new("SpreadsheetImporter", "Headless NativeAOT parser, layout detector, column suggester & batch runner.", "/components/file-upload", "Core"),
                new("BxSpreadsheetImport", "Interactive 5-step Blazor/MAUI import wizard with inline cell editing.", "/components/file-upload", "UI")
            ]),

        new(
            Slug: "agent",
            Name: "Lux.Agent",
            ShortName: "Agent",
            Tagline: "Semantic AI Agent Bridge (snapshot/query/act), WebMCP tools, observe stream & UndoStack",
            HeroHeadline: "AI-ready interfaces",
            HeroAccent: "& semantic WebMCP.",
            Description: "Inspiré de ui-sdk.dev : rend vos formulaires (Lux.Form), tableaux de bord (Lux.Dashboard) et imports (Lux.Spreadsheet) directement pilotables par des agents IA sans sélecteurs CSS fragiles. Expose Snapshot/Query/Act sémantique, politique d'autorisation, redaction [redacted] des secrets, WebMCP et UndoStack<T>.",
            AccentHex: "#6366f1",
            BadgeColor: BxColor.Info,
            RepoName: "lux-agent",
            RepoPath: "/home/gpoumarat/lux-agent",
            CorePackage: "Lux.Agent.Core",
            UiPackage: "Lux.Agent",
            Platforms: ["Headless C# / NativeAOT", "Blazor Server / WASM / Auto", ".NET MAUI Blazor Hybrid", "Browser WebMCP (navigator.modelContext)"],
            Features:
            [
                ("Semantic Snapshot, Query & Act", "Expose jusqu'à 100 contrôles avec ref stable (c1, c2…), état, choix interrogeables sans ouvrir de popup et actions sémantiques (setValue, toggle, submit).", "Semantic Bridge"),
                ("Permission Gate & Confirmation Policy", "AllowPolicy global ou par appel avec indicateur ConfirmRequired pour bloquer ou valider les actions sensibles avant exécution.", "Policy Guard"),
                ("Observe Stream & Secret Redaction", "Flux unifié ObservedChange (source Agent/User) où les champs Password/OneTimeCode sont systématiquement masqués en [redacted].", "Privacy First"),
                ("WebMCP JS Assets & UndoStack<T>", "Inclut _content/Lux.Agent/lux-agent.js + lux-behaviors.js pour WebMCP et UndoStack<T> borné dans Lux.Agent.Core (NativeAOT).", "WebMCP + Undo")
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
                new("AgentComponentSnapshot", "Compact NativeAOT semantic snapshot model with state, actions & flags.", "/docs/mcp", "Core"),
                new("UndoStack<T>", "Generic bounded Undo/Redo stack with labels and state notifications.", "/docs/mcp", "Core"),
                new("AgentBridgeService", "Connects FormController, DashboardController & SpreadsheetImporter to AI agents.", "/docs/mcp", "UI"),
                new("lux-agent.js & lux-behaviors.js", "Static web assets for WebMCP (navigator.modelContext) and composable DOM behaviors.", "/docs/mcp", "UI")
            ])
    ];

    public static LuxProductInfo? Find(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : All.FirstOrDefault(p => string.Equals(p.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
}
