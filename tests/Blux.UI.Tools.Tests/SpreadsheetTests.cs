using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blux.UI.Tools.Tests;

public sealed class SpreadsheetTests : BunitContext
{
    public SpreadsheetTests()
    {
        Services.AddBluxUiTools();
    }

    [Fact]
    public void SpreadsheetImporter_normalizes_headers_detects_layout_and_suggests_columns_at_80_percent_pass_rate()
    {
        Assert.Equal("secure code", SpreadsheetImporter.NormalizeText("Secure Code * (required)"));
        Assert.Equal("prenom", SpreadsheetImporter.NormalizeText("Prénom"));
        Assert.Equal("Secure code", SpreadsheetImporter.HumanizeKey("secureCode"));

        var schema = new SpreadsheetSchema
        {
            Key = "rates",
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
                    Options =
                    [
                        new SpreadsheetOption("EUR", "Euro", ["€", "EUR"]),
                        new SpreadsheetOption("USD", "US Dollar", ["$", "USD"])
                    ]
                }
            ]
        };

        var importer = new SpreadsheetImporter(schema);

        // Title row on line 1, headers on line 2, "Amount" instead of "Unit Price" so price is missing and gets a suggestion!
        var csv = "Export Report 2026;;\nCode;Amount;Currency\nA1;12,50;€\nA2;45,00;USD";
        importer.LoadText(csv, "rates.csv");

        Assert.True(importer.FileLoaded);
        Assert.Equal(2, importer.HeaderRowNumber);
        Assert.Single(importer.MissingFields);
        Assert.Equal("price", importer.MissingFields[0].Column.Key);

        // Suggestion must identify "Amount" (since 100% >= 80% of samples parse as valid numbers >= 0) without auto-applying
        var suggestion = importer.MissingFields[0].Suggestion;
        Assert.NotNull(suggestion);
        Assert.Equal("Amount", suggestion.HeaderText);

        // Apply suggestion
        importer.AssignColumn("price", suggestion.HeaderIndex);
        Assert.Empty(importer.MissingFields);
        Assert.Equal(2, importer.ImportableRows.Count);
        Assert.Equal(12.5, importer.AllRows[0].Data["price"]);
        Assert.Equal("EUR", importer.AllRows[0].Data["currency"]);
    }

    [Fact]
    public void SpreadsheetImporter_handles_unknown_value_questions_duplicates_existing_records_and_inline_edits()
    {
        var schema = new SpreadsheetSchema
        {
            Key = "candidates",
            RowKeyField = "email",
            Duplicates = SpreadsheetDuplicatesPolicy.Error,
            ExistingRecords = new Dictionary<string, IReadOnlyDictionary<string, object?>>
            {
                ["bob@example.com"] = new Dictionary<string, object?> { ["email"] = "bob@example.com", ["level"] = "B2" }
            },
            ExistingAction = SpreadsheetExistingAction.Update,
            Columns =
            [
                new SpreadsheetColumnSchema
                {
                    Key = "email",
                    Label = "Email",
                    Required = true,
                    Rules = [SpreadsheetRule.Email()]
                },
                new SpreadsheetColumnSchema
                {
                    Key = "level",
                    Label = "Level",
                    Kind = SpreadsheetColumnKind.Select,
                    Required = true,
                    Options = [new SpreadsheetOption("B1", "B1"), new SpreadsheetOption("B2", "B2")],
                    UnknownPolicy = SpreadsheetUnknownValuePolicy.Ask
                }
            ]
        };

        var importer = new SpreadsheetImporter(schema);
        importer.Paste("Email\tLevel\nalice@example.com\tC1-Advanced\nalice@example.com\tB1\nbob@example.com\tB2");

        // 1 open question for "C1-Advanced", and duplicate key error on rows 0 and 1 ("alice@example.com")
        Assert.Single(importer.OpenQuestions);
        Assert.Equal("C1-Advanced", importer.OpenQuestions[0].RawValue);
        Assert.False(importer.CanSubmit);
        Assert.Equal(SpreadsheetRowMode.Update, importer.AllRows[2].Mode);

        // Answer question by mapping "C1-Advanced" -> "B2"
        importer.AnswerValue("level", "C1-Advanced", new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.Map, "B2", "B2"));
        Assert.Empty(importer.OpenQuestions);

        // Fix duplicate email on row 1 via inline cell edit
        importer.EditCell(1, "email", "charlie@example.com");
        Assert.True(importer.AllRows[1].Edited);
        Assert.Equal(3, importer.ImportableRows.Count);
        Assert.True(importer.CanSubmit);
    }

    [Fact]
    public async Task BxSpreadsheetImport_drives_wizard_steps_skips_resolved_steps_and_handles_batch_submit_rejections()
    {
        var badRowFailures = 0;
        var schema = new SpreadsheetSchema
        {
            Key = "users",
            BatchSize = 1,
            Columns =
            [
                new SpreadsheetColumnSchema { Key = "name", Label = "Name", Required = true }
            ],
            OnSubmit = rows =>
            {
                if (rows[0].Data["name"]?.ToString() == "BadRow" && badRowFailures++ == 0)
                {
                    return Task.FromResult<IReadOnlyList<SpreadsheetRejectedRow>?>([
                        new SpreadsheetRejectedRow(rows[0].Index, "Server rejected name", "name")
                    ]);
                }
                return Task.FromResult<IReadOnlyList<SpreadsheetRejectedRow>?>([]);
            }
        };

        var importer = new SpreadsheetImporter(schema);
        var cut = Render<BxSpreadsheetImport>(p => p.Add(x => x.Importer, importer));

        Assert.Equal("file", importer.CurrentStepKey);
        importer.LoadText("Name\nGoodRow\nBadRow", "users.csv");
        cut.Render();

        // Columns and Values steps have no blockers -> NextStep skips both and lands directly on "review"!
        cut.Find("[data-slot=spreadsheet-nav-continue]").Click();
        Assert.Equal("review", importer.CurrentStepKey);

        // Continue to "submit" step
        cut.Find("[data-slot=spreadsheet-nav-continue]").Click();
        Assert.Equal("submit", importer.CurrentStepKey);

        // Run submit: GoodRow succeeds, BadRow is rejected by server on attempt 1
        await cut.InvokeAsync(() => importer.SubmitAsync());
        cut.Render();
        Assert.Equal(SpreadsheetSubmitStatus.Done, importer.SubmitStatus);
        Assert.Equal(1, importer.ImportedCount);
        Assert.Equal(1, importer.RejectedCount);
        Assert.NotNull(cut.Find("[data-slot=spreadsheet-rejected-box]"));

        // Retry rejected rows -> attempt > 1 succeeds
        await cut.InvokeAsync(() => importer.RetryRejectedAsync());
        cut.Render();
        Assert.Equal(0, importer.RejectedCount);
        Assert.Equal(1, importer.ImportedCount);
    }
}
