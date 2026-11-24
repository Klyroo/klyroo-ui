using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildSpreadsheet() => new(
        "spreadsheet", "Lux.Spreadsheet", "#14b8a6",
        [
            new("", "Introduction", "Getting Started",
                "Headless NativeAOT CSV/TSV/workbook importer with 20-row header layout detection, NFD normalization, >=80% pass-rate column suggestions, unknown value questions, duplicate/upsert reconciliation, inline cell editing, CSV template/error exports, and batched submission with rejected-row retry.",
                [
                    new("overview", "What is Lux.Spreadsheet?",
                        Paragraphs:
                        [
                            "Production spreadsheet uploads from Excel, ERPs, and Google Sheets rarely match a clean schema: title rows appear above headers, accented column names carry asterisks or '(required)', European decimals use commas, and select values contain typos or legacy codes.",
                            "Lux.Spreadsheet puts the entire parsing, layout detection, NFD header normalization, >=80% sample validation scoring, reconciliation, review filtering, and batch retry state machine inside 100% NativeAOT C# (Lux.Spreadsheet.Core), paired with the 5-step BxSpreadsheetImport wizard for Blazor Web and .NET MAUI Blazor Hybrid."
                        ],
                        LiveDemoKey: "spreadsheet-wizard",
                        LiveDemoTitle: "Lux.Spreadsheet · Live 5-Step Import Wizard"),
                    new("pipeline", "5-Step Import Pipeline Overview",
                        TableHeaders: ["Step", "Capabilities", "Key Importer Members"],
                        TableRows:
                        [
                            new("1. Source & Layout", "CSV, TSV, Excel clipboard Paste, multi-sheet Workbook, 20-row header detection", "LoadText, Paste, LoadWorkbook, SetSheet, SetHeaderRow, HeaderRowNumber"),
                            new("2. Column Mapping", "NFD normalization, alias matching, >=80% sample-validated Suggestions", "Fields, MissingFields, AssignColumn, IgnoreColumn, ResetColumn"),
                            new("3. Unknown Values", "Collect unrecognized select values and apply Map/LeaveEmpty/SkipRows/Create", "Questions, OpenQuestions, AnswerValue, ClearAnswer"),
                            new("4. Review & Edit", "Duplicates, ExistingRecords upsert, tabs/filters, row inspector & EditCell", "AllRows, ImportableRows, VisibleReviewRows, EditCell, RevertCell, DiscardRows"),
                            new("5. Batch Submit", "Chunked async OnSubmit, progress tracking, RetryRejectedAsync & CSV exports", "SubmitAsync, RetryRejectedAsync, ExportTemplateCsv, ExportInvalidRowsCsv")
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
                                dotnet add package Lux.Spreadsheet        # 5-step Blazor Web & .NET MAUI wizard
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxSpreadsheet();")])
                ]),
            new("schema", "Schema, Columns & Rules", "Import Pipeline",
                "Configure column kinds, header aliases, decimal separators, date formats, built-in validation rules, custom parsers, and cross-field row validators.",
                [
                    new("kinds", "All 5 SpreadsheetColumnKind Values & Built-In SpreadsheetRule Factories",
                        TableHeaders: ["Column Kind / Rule", "Configuration", "Behavior"],
                        TableRows:
                        [
                            new("SpreadsheetColumnKind.Text", "SpreadsheetRule.Email(), Pattern(regex)", "Trimmed string with regex/email validation and Error or Warning IssueLevel"),
                            new("SpreadsheetColumnKind.Number", "DecimalSeparator ('.' or ','), Min, Max, Between", "Locale-aware number parsing with numeric bounds"),
                            new("SpreadsheetColumnKind.Date", "DateFormats (custom format list)", "Exact and ISO date parsing"),
                            new("SpreadsheetColumnKind.Boolean", "DefaultValue / HasDefault", "Parses true/false, yes/no, 1/0"),
                            new("SpreadsheetColumnKind.Select", "Options, UnknownPolicy, OnCreateOption", "Matches Value, Label, or Aliases case/accent-insensitively")
                        ]),
                    new("example", "Full SpreadsheetSchema with Custom Row Validation & Batch Handler",
                        CodeSamples:
                        [
                            new("RatesSchema.cs", """
                                var schema = new SpreadsheetSchema
                                {
                                    Key = "rates",
                                    MaxRows = 5000,
                                    RowKeyField = "code",
                                    Duplicates = SpreadsheetDuplicatesPolicy.Error,
                                    ExistingAction = SpreadsheetExistingAction.Update,
                                    BatchSize = 100,
                                    Columns =
                                    [
                                        new SpreadsheetColumnSchema { Key = "code", Label = "Code", Headers = ["SKU", "Item Code"], Required = true },
                                        new SpreadsheetColumnSchema
                                        {
                                            Key = "price",
                                            Label = "Unit Price",
                                            Kind = SpreadsheetColumnKind.Number,
                                            DecimalSeparator = ',',
                                            Required = true,
                                            Rules = [SpreadsheetRule.Between(0, 100_000, "Price must be between 0 and 100,000")]
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
                                    ],
                                    ValidateRow = data =>
                                        (double?)data.GetValueOrDefault("price") == 0
                                            ? [new SpreadsheetRowIssue("price", "Zero-price item will be marked promotional", SpreadsheetIssueLevel.Warning)]
                                            : [],
                                    OnSubmit = async batch => await api.UpsertRatesBatchAsync(batch)
                                };
                                """)
                        ])
                ]),
            new("layout-detection", "20-Row Header & Delimiter Detection", "Import Pipeline",
                "Parse CSV, TSV, clipboard pastes, or multi-sheet workbooks and locate the true header row automatically.",
                [
                    new("detect", "Heuristic Scanner, Multi-Sheet Workbooks & Manual Override",
                        Paragraphs:
                        [
                            "LoadText(csv, fileName) and Paste(tsv, fileName) use ParseDelimitedSheet() to auto-detect comma, semicolon, or tab delimiters and score the first 20 rows against your schema's column keys, labels, and Headers aliases.",
                            "If two rows tie, AmbiguousDetection becomes true and users can pick the exact header row via SetHeaderRow(rowIndex) or switch sheets on a SpreadsheetWorkbook via SetSheet(sheetName). You can also lock detection upfront using Schema.ForcedSheet and Schema.ForcedHeaderRow."
                        ],
                        CodeSamples:
                        [
                            new("LayoutDetection.cs", """
                                var importer = new SpreadsheetImporter(schema);
                                importer.LoadText("Export Report 2026;;\nCode;Amount;Currency\nA1;12,50;€\nA2;45,00;USD", "rates.csv");

                                Assert.True(importer.FileLoaded);
                                Assert.Equal(2, importer.HeaderRowNumber); // 1-based line number

                                // Generate a ready-to-fill CSV template matching the schema:
                                string templateCsv = importer.ExportTemplateCsv();
                                """)
                        ])
                ]),
            new("column-mapping", "NFD Normalization & 80% Suggestions", "Import Pipeline",
                "Match headers accent-insensitively and propose missing mappings only when >= 80% of sample values pass validation.",
                [
                    new("nfd", "NFD Header Normalization & Column Statuses",
                        Paragraphs:
                        [
                            "SpreadsheetImporter.NormalizeText() decomposes Unicode text (FormD), removes diacritics, strips '*' and '(required)' suffixes, and collapses whitespace (e.g. \"Secure Code * (required)\" -> \"secure code\", \"Prénom\" -> \"prenom\").",
                            "For any column still in MissingFields, the scorer evaluates every unused source column against sample data rows. Only columns where >= 80% of non-empty samples parse and pass validation rules are proposed as a Suggestion — never auto-applied silently."
                        ],
                        TableHeaders: ["SpreadsheetColumnStatus", "Meaning", "Importer Method"],
                        TableRows:
                        [
                            new("Matched", "Bound to a source column (exact, alias, or manual)", "ResetColumn(key) clears manual override"),
                            new("Default", "Unmapped column populated from DefaultValue", "AssignColumn(key, headerIndex) overrides default"),
                            new("Missing", "Required column still unmapped (check .Suggestion)", "AssignColumn(key, suggestion.HeaderIndex)"),
                            new("Unmatched", "Optional unmapped or explicitly ignored column", "IgnoreColumn(key)")
                        ],
                        CodeSamples:
                        [
                            new("ApplySuggestion.cs", """
                                // "Amount" passes 100% (>= 80%) of Number + Min(0) checks for "price"
                                var suggestion = importer.MissingFields[0].Suggestion;
                                Assert.Equal("Amount", suggestion?.HeaderText);

                                importer.AssignColumn("price", suggestion!.HeaderIndex);
                                Assert.Empty(importer.MissingFields);
                                """)
                        ])
                ]),
            new("unknown-values", "Unknown Value Questions", "Import Pipeline",
                "Resolve unrecognized Select values globally before validation and submission.",
                [
                    new("policies", "All 5 SpreadsheetUnknownValuePolicy & 4 Answer Actions",
                        TableHeaders: ["UnknownPolicy", "Behavior", "Resolution Actions"],
                        TableRows:
                        [
                            new("Ask", "Surfaces each distinct unknown string in OpenQuestions", "Answer with Map, LeaveEmpty, SkipRows, or Create"),
                            new("Error", "Marks affected rows as Blocking immediately", "Fix inline with EditCell"),
                            new("SkipRows", "Automatically discards affected rows (DiscardReason.Value)", "Restore via RestoreRows"),
                            new("LeaveEmpty", "Replaces unknown values with null", "Valid if column is optional"),
                            new("Create", "Accepts unknown strings and calls OnCreateOption if configured", "Creates new vocabulary items")
                        ],
                        CodeSamples:
                        [
                            new("ResolveQuestion.cs", """
                                // Map every occurrence of "C1-Advanced" to canonical option "B2"
                                importer.AnswerValue("level", "C1-Advanced",
                                    new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.Map, "B2", "B2"));

                                // Clear a previous answer to re-open the question:
                                importer.ClearAnswer("level", "C1-Advanced");
                                """)
                        ])
                ]),
            new("reconciliation", "Duplicates, Upsert & Inline Edits", "Import Pipeline",
                "Reconcile intra-file duplicates, existing database records, review filters, row inspection, and inline cell edits.",
                [
                    new("policies", "Reconciliation & Review Grid Enums",
                        TableHeaders: ["Enum", "Exact Values", "Purpose"],
                        TableRows:
                        [
                            new("SpreadsheetDuplicatesPolicy", "Error, KeepFirst, KeepLast", "Handles repeated RowKeyField keys inside the uploaded file"),
                            new("SpreadsheetExistingAction", "Update, Skip, Error", "Handles rows whose RowKeyField already exists in Schema.ExistingRecords"),
                            new("SpreadsheetRowStatus", "Valid, Warning, Blocking, Discarded", "Computed row validation state"),
                            new("SpreadsheetRowMode", "Create, Update, Skip", "Persistence operation for each importable row"),
                            new("SpreadsheetDiscardReason", "Manual, Value, Limit, Duplicate, Existing", "Exact reason a row was excluded from ImportableRows"),
                            new("SpreadsheetReviewTab / Level", "All, Importable, Invalid, Discarded / All, Blocking, Warnings", "Built-in review grid filtering via VisibleReviewRows")
                        ]),
                    new("editing", "Inline Edits, Revert, Discard & Row Inspector",
                        CodeSamples:
                        [
                            new("ReviewAndEdit.cs", """
                                // Filter review grid to invalid rows only and jump between issue rows
                                importer.SetReviewTab(SpreadsheetReviewTab.Invalid);
                                importer.SetReviewLevel(SpreadsheetReviewLevel.Blocking);
                                importer.InspectNextIssueRow();

                                // Edit a cell inline or revert it back to the original uploaded text
                                importer.EditCell(rowIndex: 1, fieldKey: "email", rawText: "alice.fixed@example.com");
                                importer.RevertCell(rowIndex: 1, fieldKey: "email");

                                // Bulk discard or restore rows, or export invalid rows for offline cleanup
                                importer.DiscardRows([4, 5]);
                                importer.RestoreRows([4]);
                                string invalidCsv = importer.ExportInvalidRowsCsv();
                                """)
                        ])
                ]),
            new("batch-submit", "Batch Submit & Retry Rejected", "Import Pipeline",
                "Execute chunked async submissions with progress counters and targeted retry for server-rejected rows.",
                [
                    new("submit", "SubmitAsync, Progress Tracking & RetryRejectedAsync",
                        Paragraphs:
                        [
                            "SubmitAsync() chunks ImportableRows by Schema.BatchSize and invokes Schema.OnSubmit for each batch. Monitor SubmitStatus (Idle, Running, Done, Error), SubmitDone, SubmitTotal, ImportedCount, RejectedCount, and SubmitError.",
                            "When a backend rejects specific rows by returning SpreadsheetRejectedRow(RowIndex, Message, Field), those rows are attached with server error messages while already-imported batches remain committed. Fix those rows with EditCell() and call RetryRejectedAsync()."
                        ],
                        CodeSamples:
                        [
                            new("BatchRetry.cs", """
                                await importer.SubmitAsync();

                                if (importer.RejectedCount > 0)
                                {
                                    // Fix only the rejected rows and retry them without re-sending succeeded batches
                                    importer.EditCell(rowIndex: 2, fieldKey: "code", rawText: "UNIQUE-CODE-2");
                                    await importer.RetryRejectedAsync();
                                }
                                Assert.Equal(SpreadsheetSubmitStatus.Done, importer.SubmitStatus);
                                """)
                        ])
                ]),
            new("wizard", "BxSpreadsheetImport Wizard", "Import Pipeline",
                "Drop-in 5-step Blazor Web & .NET MAUI Blazor Hybrid import wizard wired to SpreadsheetImporter.",
                [
                    new("component", "Interactive 5-Step Wizard",
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
            new("examples", "Live Examples", "Examples",
                "Interactive spreadsheet import pipelines demonstrating the 5-step BxSpreadsheetImport wizard, reference reconciliation, and Unicode NFD header matching.",
                [
                    new("import-wizard", "1. Full 5-Step BxSpreadsheetImport Wizard",
                        Paragraphs: ["Walk through Upload, Column Mapping, Inline Validation & Cell Editing, Dry-Run Preview, and Batched Commit directly in the browser with preloaded sample CSV data."],
                        LiveDemoKey: "spreadsheet-wizard",
                        LiveDemoTitle: "BxSpreadsheetImport · 5-Step Import Wizard",
                        CodeSamples:
                        [
                            new("RatesImporter.razor", """
                                <BxSpreadsheetImport Importer="@_importer"
                                                     Title="Import Product Rates"
                                                     Wizard="true" />
                                """)
                        ]),
                    new("reference-reconcile", "2. Reference Lookup Reconciliation & Unresolved Row Repair",
                        Paragraphs: ["Automatically reconcile incoming supplier or category labels against your canonical lookup list, surface unresolved rows, and repair them inline before commit."],
                        LiveDemoKey: "spreadsheet-reconcile",
                        LiveDemoTitle: "SpreadsheetImporter · Reference Reconciliation",
                        CodeSamples:
                        [
                            new("ReconcileLookup.cs", """
                                new SpreadsheetColumnSchema
                                {
                                    Key = "country",
                                    Label = "Country",
                                    Aliases = ["pays", "country_code"],
                                    ReferenceOptions =
                                    [
                                        new SpreadsheetReferenceOption("FR", "France", ["fr", "république française"]),
                                        new SpreadsheetReferenceOption("DE", "Germany", ["de", "deutschland"])
                                    ]
                                }
                                """)
                        ]),
                    new("unicode-nfd", "3. Accent-Insensitive (Unicode NFD) Header Auto-Mapping",
                        Paragraphs: ["Paste accented, snake_case, or localized French/Spanish/German CSV headers and watch SpreadsheetImporter auto-map every column deterministically via NFD normalization."],
                        LiveDemoKey: "spreadsheet-nfd",
                        LiveDemoTitle: "Unicode NFD · Multilingual Header Auto-Mapping",
                        CodeSamples:
                        [
                            new("NfdMapping.cs", """
                                // Automatically matches "Référence Produit" -> "sku" and "Prix Unitaire (€)" -> "price"
                                var importer = new SpreadsheetImporter(schema);
                                importer.LoadText("Référence Produit;Désignation;Prix Unitaire (€)\nSKU-101;Clavier Mécanique;149.00", "tarifs.csv");
                                // importer.ColumnMappings["sku"] == 0, importer.ColumnMappings["price"] == 2
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Spreadsheet.Core and Lux.Spreadsheet.",
                [],
                ApiTypes: [typeof(BxSpreadsheetImport), typeof(SpreadsheetImporter), typeof(SpreadsheetSchema), typeof(SpreadsheetColumnSchema), typeof(SpreadsheetRule)])
        ]);
}
