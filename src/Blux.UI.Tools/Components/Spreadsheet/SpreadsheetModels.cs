using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Blux.UI.Tools;

/// <summary>Column data types in a <see cref="SpreadsheetSchema"/>.</summary>
public enum SpreadsheetColumnKind
{
    Text,
    Number,
    Date,
    Boolean,
    Select,
}

/// <summary>Policy for unknown select values in <see cref="SpreadsheetColumnSchema"/>.</summary>
public enum SpreadsheetUnknownValuePolicy
{
    Ask,
    Error,
    SkipRows,
    LeaveEmpty,
    Create,
}

/// <summary>Duplicate row key policy in <see cref="SpreadsheetSchema"/>.</summary>
public enum SpreadsheetDuplicatesPolicy
{
    Error,
    KeepFirst,
    KeepLast,
}

/// <summary>Action taken when an imported row matches an existing stored record.</summary>
public enum SpreadsheetExistingAction
{
    Update,
    Skip,
    Error,
}

/// <summary>Status of a column mapping in <see cref="SpreadsheetImporter"/>.</summary>
public enum SpreadsheetColumnStatus
{
    Matched,
    Default,
    Missing,
    Unmatched,
}

/// <summary>Severity level of a <see cref="SpreadsheetRowIssue"/>.</summary>
public enum SpreadsheetIssueLevel
{
    Info,
    Warning,
    Error,
}

/// <summary>Overall validation status of a <see cref="SpreadsheetRow"/>.</summary>
public enum SpreadsheetRowStatus
{
    Valid,
    Warning,
    Blocking,
    Discarded,
}

/// <summary>Reason why a <see cref="SpreadsheetRow"/> was discarded from import.</summary>
public enum SpreadsheetDiscardReason
{
    Manual,
    Value,
    Limit,
    Duplicate,
    Existing,
}

/// <summary>Import mode of a <see cref="SpreadsheetRow"/>.</summary>
public enum SpreadsheetRowMode
{
    Create,
    Update,
    Skip,
}

/// <summary>Review table tab filter in <see cref="SpreadsheetImporter"/>.</summary>
public enum SpreadsheetReviewTab
{
    All,
    Importable,
    Invalid,
    Discarded,
}

/// <summary>Review issue level filter in <see cref="SpreadsheetImporter"/>.</summary>
public enum SpreadsheetReviewLevel
{
    All,
    Blocking,
    Warnings,
}

/// <summary>Action chosen for an unrecognized select value in <see cref="SpreadsheetValueQuestion"/>.</summary>
public enum SpreadsheetValueAnswerAction
{
    Map,
    LeaveEmpty,
    SkipRows,
    Create,
}

/// <summary>Submission lifecycle state of <see cref="SpreadsheetImporter"/>.</summary>
public enum SpreadsheetSubmitStatus
{
    Idle,
    Running,
    Done,
    Error,
}

/// <summary>An allowed select option with optional aliases for exact normalized matching.</summary>
public sealed record SpreadsheetOption(
    string Value,
    string Label,
    IReadOnlyList<string>? Aliases = null);

/// <summary>A validation rule attached to a <see cref="SpreadsheetColumnSchema"/>.</summary>
public sealed record SpreadsheetRule(
    string Code,
    string Message,
    Func<object?, bool> Validate,
    SpreadsheetIssueLevel Level = SpreadsheetIssueLevel.Error)
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    public static SpreadsheetRule Email(string message = "Not an email address", SpreadsheetIssueLevel level = SpreadsheetIssueLevel.Error) =>
        new("email", message, v => v is string s && EmailRegex.IsMatch(s), level);

    public static SpreadsheetRule Pattern(string pattern, string message = "Unexpected format", SpreadsheetIssueLevel level = SpreadsheetIssueLevel.Error)
    {
        var rx = new Regex(pattern, RegexOptions.Compiled);
        return new("pattern", message, v => v is string s && rx.IsMatch(s), level);
    }

    public static SpreadsheetRule Min(double min, string? message = null, SpreadsheetIssueLevel level = SpreadsheetIssueLevel.Error) =>
        new("min", message ?? $"Value is below {min.ToString(CultureInfo.InvariantCulture)}",
            v => v is double d && d >= min, level);

    public static SpreadsheetRule Max(double max, string? message = null, SpreadsheetIssueLevel level = SpreadsheetIssueLevel.Error) =>
        new("max", message ?? $"Value is above {max.ToString(CultureInfo.InvariantCulture)}",
            v => v is double d && d <= max, level);

    public static SpreadsheetRule Between(double min, double max, string? message = null, SpreadsheetIssueLevel level = SpreadsheetIssueLevel.Error) =>
        new("between", message ?? $"Value is not between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)}",
            v => v is double d && d >= min && d <= max, level);
}

/// <summary>Declarative definition of a spreadsheet column.</summary>
public sealed class SpreadsheetColumnSchema
{
    public required string Key { get; init; }
    public string? Label { get; init; }
    public SpreadsheetColumnKind Kind { get; init; } = SpreadsheetColumnKind.Text;
    public IReadOnlyList<string>? Headers { get; init; }
    public string? Description { get; init; }
    public string? Example { get; init; }
    public string? Group { get; init; }
    public string? From { get; init; }
    public bool Required { get; init; }
    public bool Editable { get; init; } = true;
    public object? DefaultValue { get; init; }
    public bool HasDefault { get; init; }
    public char DecimalSeparator { get; init; } = '.';
    public IReadOnlyList<string>? DateFormats { get; init; }
    public IReadOnlyList<SpreadsheetOption>? Options { get; init; }
    public SpreadsheetUnknownValuePolicy UnknownPolicy { get; init; } = SpreadsheetUnknownValuePolicy.Ask;
    public Func<string, SpreadsheetOption>? OnCreateOption { get; init; }
    public IReadOnlyList<SpreadsheetRule>? Rules { get; init; }
    public Func<string, object?>? CustomParse { get; init; }

    public string EffectiveLabel => Label ?? SpreadsheetImporter.HumanizeKey(Key);
}

/// <summary>Top-level declarative schema for <see cref="SpreadsheetImporter"/> and <see cref="BxSpreadsheetImport"/>.</summary>
public sealed class SpreadsheetSchema
{
    public string Key { get; init; } = "import";
    public IReadOnlyList<string> Accept { get; init; } = [".xlsx", ".xls", ".csv", ".tsv"];
    public int? MaxRows { get; init; }
    public string? ForcedSheet { get; init; }
    public int? ForcedHeaderRow { get; init; }
    public IReadOnlyList<SpreadsheetColumnSchema> Columns { get; init; } = [];
    public string? RowKeyField { get; init; }
    public SpreadsheetDuplicatesPolicy Duplicates { get; init; } = SpreadsheetDuplicatesPolicy.Error;
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>>? ExistingRecords { get; init; }
    public SpreadsheetExistingAction ExistingAction { get; init; } = SpreadsheetExistingAction.Update;
    public Func<IReadOnlyDictionary<string, object?>, IReadOnlyList<SpreadsheetRowIssue>>? ValidateRow { get; init; }
    public int BatchSize { get; init; }
    public Func<IReadOnlyList<SpreadsheetRow>, Task<IReadOnlyList<SpreadsheetRejectedRow>?>>? OnSubmit { get; init; }
}

/// <summary>A server-side rejection reported for a specific row index during submit.</summary>
public sealed record SpreadsheetRejectedRow(int RowIndex, string Message, string? Field = null);

/// <summary>One parsed sheet in a <see cref="SpreadsheetWorkbook"/>.</summary>
public sealed record SpreadsheetSheet(
    string Name,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<int> RowNumbers);

/// <summary>A parsed workbook containing one or more sheets.</summary>
public sealed record SpreadsheetWorkbook(
    string Name,
    IReadOnlyList<SpreadsheetSheet> Sheets);

/// <summary>A column suggestion offered for a missing required field when an unused header's sample values pass validation.</summary>
public sealed record SpreadsheetColumnSuggestion(
    int HeaderIndex,
    string HeaderText,
    IReadOnlyList<string> Samples);

/// <summary>State of a column field in <see cref="SpreadsheetImporter"/>.</summary>
public sealed record SpreadsheetFieldState(
    SpreadsheetColumnSchema Column,
    SpreadsheetColumnStatus Status,
    int? HeaderIndex,
    string? HeaderText,
    string? AssignedBy,
    IReadOnlyList<string> Samples,
    SpreadsheetColumnSuggestion? Suggestion);

/// <summary>User or policy answer for an unrecognized select value.</summary>
public sealed record SpreadsheetValueAnswer(
    SpreadsheetValueAnswerAction Action,
    string? MappedValue = null,
    string? MappedLabel = null);

/// <summary>An open or answered question for an unknown select token in the imported file.</summary>
public sealed record SpreadsheetValueQuestion(
    string Id,
    string FieldKey,
    string FieldLabel,
    string RawValue,
    string NormalizedKey,
    IReadOnlyList<int> RowIndexes,
    IReadOnlyList<SpreadsheetOption> Choices,
    SpreadsheetValueAnswer? Answer,
    string? AnsweredBy,
    bool CanCreate)
{
    public bool IsOpen => Answer is null;
}

/// <summary>An issue attached to a specific field or row in <see cref="SpreadsheetRow"/>.</summary>
public sealed record SpreadsheetRowIssue(
    string? Field,
    string Code,
    SpreadsheetIssueLevel Level,
    string Message);

/// <summary>View model for one cell in a <see cref="SpreadsheetRow"/>.</summary>
public sealed record SpreadsheetCellView(
    string Text,
    string OriginalText,
    string DisplayText,
    bool Edited,
    bool Defaulted,
    bool Created,
    SpreadsheetRowIssue? Issue);

/// <summary>Parsed state of one data row in <see cref="SpreadsheetImporter"/>.</summary>
public sealed record SpreadsheetRow(
    int Index,
    int RowNumber,
    IReadOnlyDictionary<string, object?> Data,
    IReadOnlyDictionary<string, SpreadsheetCellView> Cells,
    IReadOnlyList<SpreadsheetRowIssue> Issues,
    SpreadsheetRowStatus Status,
    SpreadsheetDiscardReason? DiscardReason,
    SpreadsheetRowMode Mode,
    bool Edited,
    bool Importable);

/// <summary>Status of a wizard step in <see cref="SpreadsheetImporter"/>.</summary>
public sealed record SpreadsheetStepStatus(
    string Key,
    string Label,
    string Hint,
    string State,
    int Position,
    bool Ready,
    bool Skipped,
    int Blockers);

/// <summary>
/// Headless spreadsheet import engine: handles delimited file/clipboard parsing, automatic sheet &amp; header-row detection,
/// deterministic normalized column matching, 80%-pass column suggestions, unknown value resolution, duplicate &amp; existing key checks,
/// inline cell editing, review filtering, wizard steps, and batched submission.
/// </summary>
public sealed class SpreadsheetImporter
{
    public const int HeaderScanRows = 20;
    public const double SuggestionPassRate = 0.8;
    public const int SuggestionSampleSize = 30;

    private static readonly HashSet<string> DefaultTrueTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        "yes", "y", "oui", "o", "true", "vrai", "x", "1"
    };

    private static readonly HashSet<string> DefaultFalseTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        "no", "n", "non", "false", "faux", "0"
    };

    private static readonly string[] DefaultDateFormats =
    [
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "dd/MM/yyyy HH:mm"
    ];

    private readonly Dictionary<string, int?> _manualColumns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SpreadsheetValueAnswer> _valueAnswers = new(StringComparer.Ordinal);
    private readonly Dictionary<(int RowIndex, string FieldKey), string> _edits = new();
    private readonly HashSet<int> _manualDiscards = [];
    private readonly Dictionary<int, List<SpreadsheetRowIssue>> _serverRejections = [];
    private readonly HashSet<string> _visitedSteps = ["file"];
    private readonly HashSet<int> _selectedRows = [];

    private SpreadsheetWorkbook? _workbook;
    private string? _selectedSheetName;
    private int _headerRowIndex;
    private IReadOnlyList<SpreadsheetFieldState> _fields = [];
    private IReadOnlyList<SpreadsheetValueQuestion> _questions = [];
    private IReadOnlyList<SpreadsheetRow> _rows = [];

    public SpreadsheetImporter(SpreadsheetSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        Recompute();
    }

    public event Action? Changed;

    public SpreadsheetSchema Schema { get; }
    public bool FileLoaded => _workbook is not null && CurrentSheet is not null;
    public string? FileName => _workbook?.Name;
    public IReadOnlyList<SpreadsheetSheet> Sheets => _workbook?.Sheets ?? [];
    public SpreadsheetSheet? CurrentSheet =>
        _workbook?.Sheets.FirstOrDefault(s => s.Name == _selectedSheetName) ?? _workbook?.Sheets.FirstOrDefault();

    public int HeaderRowIndex => _headerRowIndex;
    public int HeaderRowNumber =>
        CurrentSheet is { } s && _headerRowIndex >= 0 && _headerRowIndex < s.RowNumbers.Count
            ? s.RowNumbers[_headerRowIndex]
            : 1;

    public bool AmbiguousDetection { get; private set; }
    public IReadOnlyList<string> Headers { get; private set; } = [];
    public IReadOnlyList<string> UnusedHeaders { get; private set; } = [];
    public IReadOnlyList<SpreadsheetFieldState> Fields => _fields;
    public IReadOnlyList<SpreadsheetFieldState> MissingFields =>
        _fields.Where(f => f.Status == SpreadsheetColumnStatus.Missing).ToList();

    public IReadOnlyList<SpreadsheetValueQuestion> Questions => _questions;
    public IReadOnlyList<SpreadsheetValueQuestion> OpenQuestions =>
        _questions.Where(q => q.IsOpen).ToList();

    public IReadOnlyList<SpreadsheetRow> AllRows => _rows;
    public IReadOnlyList<SpreadsheetRow> ImportableRows => _rows.Where(r => r.Importable).ToList();
    public IReadOnlyList<SpreadsheetRow> InvalidRows =>
        _rows.Where(r => r.DiscardReason is null && r.Status == SpreadsheetRowStatus.Blocking).ToList();
    public IReadOnlyList<SpreadsheetRow> DiscardedRows =>
        _rows.Where(r => r.DiscardReason is not null).ToList();

    public bool CanSubmit =>
        FileLoaded && MissingFields.Count == 0 && OpenQuestions.Count == 0 && ImportableRows.Count > 0;

    // Review state
    public SpreadsheetReviewTab ReviewTab { get; private set; } = SpreadsheetReviewTab.All;
    public SpreadsheetReviewLevel ReviewLevel { get; private set; } = SpreadsheetReviewLevel.All;
    public string ReviewIssueFilter { get; private set; } = "";
    public string ReviewSearch { get; private set; } = "";
    public int? InspectedRowIndex { get; private set; }
    public IReadOnlySet<int> SelectedRowIndexes => _selectedRows;

    public SpreadsheetRow? InspectedRow =>
        InspectedRowIndex is { } idx ? _rows.FirstOrDefault(r => r.Index == idx) : null;

    // Submit state
    public SpreadsheetSubmitStatus SubmitStatus { get; private set; } = SpreadsheetSubmitStatus.Idle;
    public int SubmitDone { get; private set; }
    public int SubmitTotal { get; private set; }
    public int ImportedCount { get; private set; }
    public int RejectedCount => _serverRejections.Count;
    public string? SubmitError { get; private set; }

    // Wizard steps state
    public string CurrentStepKey { get; private set; } = "file";

    /// <summary>Loads a CSV/TSV text file into the importer and auto-detects the sheet and header row.</summary>
    public void LoadText(string text, string fileName = "import.csv")
    {
        var sheetName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(sheetName)) sheetName = "Sheet 1";
        var sheet = ParseDelimitedSheet(text, sheetName);
        LoadWorkbook(new SpreadsheetWorkbook(fileName, [sheet]));
    }

    /// <summary>Pastes clipboard TSV/CSV text into the importer.</summary>
    public void Paste(string text, string fileName = "clipboard.tsv") =>
        LoadText(text, fileName);

    /// <summary>Loads a pre-parsed <see cref="SpreadsheetWorkbook"/> into the importer.</summary>
    public void LoadWorkbook(SpreadsheetWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        _workbook = workbook;
        _manualColumns.Clear();
        _valueAnswers.Clear();
        _edits.Clear();
        _manualDiscards.Clear();
        _serverRejections.Clear();
        _selectedRows.Clear();
        InspectedRowIndex = null;
        SubmitStatus = SpreadsheetSubmitStatus.Idle;
        DetectLayout();
        Recompute();
        Changed?.Invoke();
    }

    /// <summary>Clears the loaded file and resets the wizard to the <c>file</c> step.</summary>
    public void ClearFile()
    {
        _workbook = null;
        _selectedSheetName = null;
        _headerRowIndex = 0;
        _manualColumns.Clear();
        _valueAnswers.Clear();
        _edits.Clear();
        _manualDiscards.Clear();
        _serverRejections.Clear();
        _selectedRows.Clear();
        InspectedRowIndex = null;
        CurrentStepKey = "file";
        _visitedSteps.Clear();
        _visitedSteps.Add("file");
        SubmitStatus = SpreadsheetSubmitStatus.Idle;
        Recompute();
        Changed?.Invoke();
    }

    public void SetSheet(string sheetName)
    {
        if (_workbook is null || _workbook.Sheets.All(s => s.Name != sheetName)) return;
        _selectedSheetName = sheetName;
        _manualColumns.Clear();
        _manualDiscards.Clear();
        _serverRejections.Clear();
        _headerRowIndex = BestHeaderRowForSheet(CurrentSheet!).RowIndex;
        Recompute();
        Changed?.Invoke();
    }

    public void SetHeaderRow(int rowIndex)
    {
        if (CurrentSheet is null) return;
        _headerRowIndex = Math.Clamp(rowIndex, 0, Math.Max(0, CurrentSheet.Rows.Count - 1));
        _manualColumns.Clear();
        _manualDiscards.Clear();
        _serverRejections.Clear();
        Recompute();
        Changed?.Invoke();
    }

    public void AssignColumn(string fieldKey, int headerIndex)
    {
        foreach (var k in _manualColumns.Where(kv => kv.Value == headerIndex).Select(kv => kv.Key).ToList())
        {
            _manualColumns.Remove(k);
        }
        _manualColumns[fieldKey] = headerIndex;
        Recompute();
        Changed?.Invoke();
    }

    public void IgnoreColumn(string fieldKey)
    {
        _manualColumns[fieldKey] = null;
        Recompute();
        Changed?.Invoke();
    }

    public void ResetColumn(string fieldKey)
    {
        _manualColumns.Remove(fieldKey);
        Recompute();
        Changed?.Invoke();
    }

    public void AnswerValue(string fieldKey, string rawValue, SpreadsheetValueAnswer answer)
    {
        var key = $"{fieldKey}::{NormalizeText(rawValue)}";
        _valueAnswers[key] = answer;
        Recompute();
        Changed?.Invoke();
    }

    public void ClearAnswer(string fieldKey, string rawValue)
    {
        var key = $"{fieldKey}::{NormalizeText(rawValue)}";
        _valueAnswers.Remove(key);
        Recompute();
        Changed?.Invoke();
    }

    public void EditCell(int rowIndex, string fieldKey, string text)
    {
        _edits[(rowIndex, fieldKey)] = text.Trim();
        _serverRejections.Remove(rowIndex);
        Recompute();
        Changed?.Invoke();
    }

    public void RevertCell(int rowIndex, string fieldKey)
    {
        _edits.Remove((rowIndex, fieldKey));
        Recompute();
        Changed?.Invoke();
    }

    public void DiscardRows(IEnumerable<int> rowIndexes)
    {
        foreach (var idx in rowIndexes) _manualDiscards.Add(idx);
        _selectedRows.Clear();
        Recompute();
        Changed?.Invoke();
    }

    public void RestoreRows(IEnumerable<int> rowIndexes)
    {
        foreach (var idx in rowIndexes) _manualDiscards.Remove(idx);
        _selectedRows.Clear();
        Recompute();
        Changed?.Invoke();
    }

    public void SetReviewTab(SpreadsheetReviewTab tab)
    {
        ReviewTab = tab;
        _selectedRows.Clear();
        if (tab is SpreadsheetReviewTab.Importable or SpreadsheetReviewTab.Discarded)
        {
            ReviewLevel = SpreadsheetReviewLevel.All;
        }
        Changed?.Invoke();
    }

    public void SetReviewLevel(SpreadsheetReviewLevel level)
    {
        ReviewLevel = level;
        if (level != SpreadsheetReviewLevel.All && ReviewTab is SpreadsheetReviewTab.Importable or SpreadsheetReviewTab.Discarded)
        {
            ReviewTab = SpreadsheetReviewTab.All;
        }
        Changed?.Invoke();
    }

    public void SetReviewIssueFilter(string filter)
    {
        ReviewIssueFilter = filter;
        if (!string.IsNullOrEmpty(filter) && ReviewTab is SpreadsheetReviewTab.Importable or SpreadsheetReviewTab.Discarded)
        {
            ReviewTab = SpreadsheetReviewTab.All;
        }
        Changed?.Invoke();
    }

    public void SetReviewSearch(string search)
    {
        ReviewSearch = search;
        Changed?.Invoke();
    }

    public void ToggleRowSelection(int rowIndex, bool selected)
    {
        if (selected) _selectedRows.Add(rowIndex);
        else _selectedRows.Remove(rowIndex);
        Changed?.Invoke();
    }

    public void ClearSelection()
    {
        _selectedRows.Clear();
        Changed?.Invoke();
    }

    public void InspectRow(int? rowIndex)
    {
        InspectedRowIndex = rowIndex;
        Changed?.Invoke();
    }

    public void InspectNextIssueRow()
    {
        var withIssues = _rows.Where(r => r.DiscardReason is null && r.Issues.Any(i => i.Level != SpreadsheetIssueLevel.Info)).ToList();
        if (withIssues.Count == 0) return;
        var curIdx = withIssues.FindIndex(r => r.Index == InspectedRowIndex);
        InspectedRowIndex = withIssues[(curIdx + 1) % withIssues.Count].Index;
        Changed?.Invoke();
    }

    public void InspectPreviousIssueRow()
    {
        var withIssues = _rows.Where(r => r.DiscardReason is null && r.Issues.Any(i => i.Level != SpreadsheetIssueLevel.Info)).ToList();
        if (withIssues.Count == 0) return;
        var curIdx = withIssues.FindIndex(r => r.Index == InspectedRowIndex);
        InspectedRowIndex = withIssues[(curIdx - 1 + withIssues.Count) % withIssues.Count].Index;
        Changed?.Invoke();
    }

    public IReadOnlyList<SpreadsheetRow> VisibleReviewRows
    {
        get
        {
            IEnumerable<SpreadsheetRow> query = ReviewTab switch
            {
                SpreadsheetReviewTab.Importable => ImportableRows,
                SpreadsheetReviewTab.Invalid => InvalidRows,
                SpreadsheetReviewTab.Discarded => DiscardedRows,
                _ => _rows,
            };

            if (ReviewTab != SpreadsheetReviewTab.Discarded)
            {
                if (ReviewLevel == SpreadsheetReviewLevel.Blocking)
                {
                    query = query.Where(r => r.Issues.Any(i => i.Level == SpreadsheetIssueLevel.Error));
                }
                else if (ReviewLevel == SpreadsheetReviewLevel.Warnings)
                {
                    query = query.Where(r => r.Issues.Any(i => i.Level == SpreadsheetIssueLevel.Warning));
                }
            }

            if (!string.IsNullOrEmpty(ReviewIssueFilter))
            {
                var parts = ReviewIssueFilter.Split('|', 2);
                var fKey = parts[0];
                var code = parts.Length > 1 ? parts[1] : "*";
                query = query.Where(r => r.Issues.Any(i =>
                    i.Field == fKey && (code == "*" || i.Code == code)));
            }

            if (!string.IsNullOrWhiteSpace(ReviewSearch))
            {
                var term = ReviewSearch.Trim();
                query = query.Where(r => r.Cells.Values.Any(c =>
                    c.DisplayText.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || c.Text.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            return query.ToList();
        }
    }

    // Wizard step management
    public IReadOnlyList<SpreadsheetStepStatus> GetSteps()
    {
        var defs = new (string Key, string Label, string Hint, bool WhenNeeded, Func<bool> Needed, Func<bool> Ready, Func<int> Blockers)[]
        {
            ("file", "File", "The Excel or CSV file", false, () => true, () => FileLoaded, () => FileLoaded ? 0 : 1),
            ("columns", "Columns", "File columns → fields", true, () => MissingFields.Count > 0, () => MissingFields.Count == 0, () => MissingFields.Count),
            ("values", "Values", "Unknown values", true, () => _questions.Count > 0, () => OpenQuestions.Count == 0, () => OpenQuestions.Count),
            ("review", "Review", "Valid rows and issues", false, () => true, () => ImportableRows.Count > 0, () => ImportableRows.Count == 0 ? 1 : 0),
            ("submit", "Import", "Summary and import", false, () => true, () => SubmitStatus == SpreadsheetSubmitStatus.Done, () => 0),
        };

        var curPos = Array.FindIndex(defs, d => d.Key == CurrentStepKey);
        if (curPos < 0) curPos = 0;

        var list = new List<SpreadsheetStepStatus>(defs.Length);
        for (var i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            var skipped = d.WhenNeeded && !_visitedSteps.Contains(d.Key) && !d.Needed();
            var state = i == curPos ? "current"
                : skipped ? "skipped"
                : i < curPos ? "done"
                : "upcoming";
            list.Add(new SpreadsheetStepStatus(d.Key, d.Label, d.Hint, state, i + 1, d.Ready(), skipped, d.Blockers()));
        }
        return list;
    }

    public bool CanNextStep
    {
        get
        {
            var steps = GetSteps();
            var cur = steps.FirstOrDefault(s => s.Key == CurrentStepKey);
            return cur is { Ready: true } && cur.Key != "submit";
        }
    }

    public bool CanPreviousStep
    {
        get
        {
            var steps = GetSteps();
            var curIdx = steps.ToList().FindIndex(s => s.Key == CurrentStepKey);
            return curIdx > 0 && SubmitStatus != SpreadsheetSubmitStatus.Running;
        }
    }

    public bool NextStep()
    {
        if (!CanNextStep) return false;
        var steps = GetSteps();
        var curIdx = steps.ToList().FindIndex(s => s.Key == CurrentStepKey);
        for (var i = curIdx + 1; i < steps.Count; i++)
        {
            if (!steps[i].Skipped)
            {
                CurrentStepKey = steps[i].Key;
                _visitedSteps.Add(CurrentStepKey);
                Changed?.Invoke();
                return true;
            }
        }
        return false;
    }

    public bool PreviousStep()
    {
        if (!CanPreviousStep) return false;
        var steps = GetSteps();
        var curIdx = steps.ToList().FindIndex(s => s.Key == CurrentStepKey);
        for (var i = curIdx - 1; i >= 0; i--)
        {
            if (!steps[i].Skipped)
            {
                CurrentStepKey = steps[i].Key;
                Changed?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void GoToStep(string key)
    {
        var steps = GetSteps();
        var curIdx = steps.ToList().FindIndex(s => s.Key == CurrentStepKey);
        var targetIdx = steps.ToList().FindIndex(s => s.Key == key);
        if (targetIdx >= 0 && targetIdx <= curIdx && !steps[targetIdx].Skipped)
        {
            CurrentStepKey = key;
            Changed?.Invoke();
        }
    }

    public async Task SubmitAsync()
    {
        if (SubmitStatus == SpreadsheetSubmitStatus.Running) return;
        var rowsToSend = ImportableRows;
        await SendRowsAsync(rowsToSend);
    }

    public async Task RetryRejectedAsync()
    {
        if (SubmitStatus == SpreadsheetSubmitStatus.Running || _serverRejections.Count == 0) return;
        var rejectedIndexes = _serverRejections.Keys.ToHashSet();
        _serverRejections.Clear();
        Recompute();
        var rowsToRetry = _rows.Where(r => rejectedIndexes.Contains(r.Index) && r.Importable).ToList();
        await SendRowsAsync(rowsToRetry);
    }

    private async Task SendRowsAsync(IReadOnlyList<SpreadsheetRow> rowsToSend)
    {
        SubmitStatus = SpreadsheetSubmitStatus.Running;
        SubmitDone = 0;
        SubmitTotal = rowsToSend.Count;
        SubmitError = null;
        Changed?.Invoke();

        try
        {
            // Step 1: Run OnCreateOption handlers once per distinct answered-create question
            foreach (var q in _questions.Where(q => q.Answer?.Action == SpreadsheetValueAnswerAction.Create))
            {
                var col = Schema.Columns.FirstOrDefault(c => c.Key == q.FieldKey);
                col?.OnCreateOption?.Invoke(q.RawValue);
            }

            // Step 2: Send in batches
            var batchSize = Schema.BatchSize > 0 ? Schema.BatchSize : Math.Max(1, rowsToSend.Count);
            var rejectedAll = new List<SpreadsheetRejectedRow>();

            for (var offset = 0; offset < rowsToSend.Count; offset += batchSize)
            {
                var batch = rowsToSend.Skip(offset).Take(batchSize).ToList();
                if (Schema.OnSubmit is not null)
                {
                    var rejected = await Schema.OnSubmit(batch);
                    if (rejected is { Count: > 0 })
                    {
                        rejectedAll.AddRange(rejected);
                    }
                }
                SubmitDone = Math.Min(rowsToSend.Count, offset + batch.Count);
                Changed?.Invoke();
            }

            _serverRejections.Clear();
            foreach (var rej in rejectedAll)
            {
                if (!_serverRejections.TryGetValue(rej.RowIndex, out var list))
                {
                    list = [];
                    _serverRejections[rej.RowIndex] = list;
                }
                list.Add(new SpreadsheetRowIssue(rej.Field, "server", SpreadsheetIssueLevel.Error, rej.Message));
            }

            ImportedCount = rowsToSend.Count - _serverRejections.Count;
            SubmitStatus = SpreadsheetSubmitStatus.Done;
            Recompute();
        }
        catch (Exception ex)
        {
            SubmitError = ex.Message;
            SubmitStatus = SpreadsheetSubmitStatus.Error;
        }
        finally
        {
            Changed?.Invoke();
        }
    }

    public string ExportTemplateCsv()
    {
        var cols = Schema.Columns.Where(c => string.IsNullOrEmpty(c.From)).ToList();
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine(string.Join(',', cols.Select(c => $"\"{c.Headers?.FirstOrDefault() ?? c.EffectiveLabel}\"")));
        sb.AppendLine(string.Join(',', cols.Select(c => $"\"{c.Example ?? ""}\"")));
        return sb.ToString();
    }

    public string ExportInvalidRowsCsv()
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        var headers = Headers.Concat(["Issues"]).Select(h => $"\"{h.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
        sb.AppendLine(string.Join(',', headers));
        foreach (var row in InvalidRows)
        {
            var cells = new List<string>();
            foreach (var f in _fields)
            {
                row.Cells.TryGetValue(f.Column.Key, out var cv);
                cells.Add($"\"{(cv?.Text ?? "").Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
            }
            var issuesText = string.Join("; ", row.Issues.Select(i => i.Field is not null ? $"{i.Field}: {i.Message}" : i.Message));
            cells.Add($"\"{issuesText.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
            sb.AppendLine(string.Join(',', cells));
        }
        return sb.ToString();
    }

    private void DetectLayout()
    {
        if (_workbook is null || _workbook.Sheets.Count == 0)
        {
            _selectedSheetName = null;
            _headerRowIndex = 0;
            AmbiguousDetection = false;
            return;
        }

        if (!string.IsNullOrEmpty(Schema.ForcedSheet) && !string.Equals(Schema.ForcedSheet, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var forced = _workbook.Sheets.FirstOrDefault(s => s.Name == Schema.ForcedSheet) ?? _workbook.Sheets[0];
            _selectedSheetName = forced.Name;
            _headerRowIndex = Schema.ForcedHeaderRow is { } r
                ? Math.Max(0, forced.RowNumbers.ToList().IndexOf(r))
                : BestHeaderRowForSheet(forced).RowIndex;
            AmbiguousDetection = false;
            return;
        }

        var scores = _workbook.Sheets
            .Select(s => (Sheet: s, Best: BestHeaderRowForSheet(s)))
            .ToList();
        var maxMatches = scores.Max(x => x.Best.MatchCount);
        var winners = scores.Where(x => x.Best.MatchCount == maxMatches).ToList();
        AmbiguousDetection = maxMatches > 0 && winners.Count > 1;

        var chosen = winners[0];
        _selectedSheetName = chosen.Sheet.Name;
        _headerRowIndex = Schema.ForcedHeaderRow is { } fr
            ? Math.Max(0, chosen.Sheet.RowNumbers.ToList().IndexOf(fr))
            : chosen.Best.RowIndex;
    }

    private (int RowIndex, int MatchCount) BestHeaderRowForSheet(SpreadsheetSheet sheet)
    {
        var scanLimit = Math.Min(HeaderScanRows, sheet.Rows.Count);
        var bestRow = 0;
        var bestMatches = -1;
        var colFields = Schema.Columns.Where(c => string.IsNullOrEmpty(c.From)).ToList();

        for (var r = 0; r < scanLimit; r++)
        {
            var rowCells = sheet.Rows[r];
            var used = new HashSet<int>();
            var matches = 0;
            foreach (var col in colFields)
            {
                for (var c = 0; c < rowCells.Count; c++)
                {
                    if (used.Contains(c) || string.IsNullOrWhiteSpace(rowCells[c])) continue;
                    if (IsHeaderMatch(col, rowCells[c]))
                    {
                        used.Add(c);
                        matches++;
                        break;
                    }
                }
            }
            if (matches > bestMatches)
            {
                bestMatches = matches;
                bestRow = r;
            }
        }
        return (bestRow, Math.Max(0, bestMatches));
    }

    private void Recompute()
    {
        var sheet = CurrentSheet;
        if (sheet is null || sheet.Rows.Count == 0 || _headerRowIndex >= sheet.Rows.Count)
        {
            Headers = [];
            UnusedHeaders = [];
            _fields = Schema.Columns.Select(c => new SpreadsheetFieldState(
                c,
                c.HasDefault ? SpreadsheetColumnStatus.Default : c.Required ? SpreadsheetColumnStatus.Missing : SpreadsheetColumnStatus.Unmatched,
                null, null, null, [], null)).ToList();
            _questions = [];
            _rows = [];
            return;
        }

        Headers = sheet.Rows[_headerRowIndex];
        var dataRowSlice = sheet.Rows.Skip(_headerRowIndex + 1).ToList();
        var dataRowNumbers = sheet.RowNumbers.Skip(_headerRowIndex + 1).ToList();

        // 1. Match columns
        var usedHeaders = new HashSet<int>();
        var matchedIndexByField = new Dictionary<string, (int? Index, string? AssignedBy)>(StringComparer.Ordinal);

        foreach (var col in Schema.Columns.Where(c => string.IsNullOrEmpty(c.From)))
        {
            if (_manualColumns.TryGetValue(col.Key, out var manualIdx))
            {
                if (manualIdx is { } idx && idx >= 0 && idx < Headers.Count)
                {
                    usedHeaders.Add(idx);
                    matchedIndexByField[col.Key] = (idx, "user");
                }
                else
                {
                    matchedIndexByField[col.Key] = (null, null);
                }
            }
        }

        foreach (var col in Schema.Columns.Where(c => string.IsNullOrEmpty(c.From)))
        {
            if (matchedIndexByField.ContainsKey(col.Key)) continue;
            int? found = null;
            for (var h = 0; h < Headers.Count; h++)
            {
                if (usedHeaders.Contains(h) || string.IsNullOrWhiteSpace(Headers[h])) continue;
                if (IsHeaderMatch(col, Headers[h]))
                {
                    found = h;
                    usedHeaders.Add(h);
                    break;
                }
            }
            matchedIndexByField[col.Key] = (found, found.HasValue ? "file" : null);
        }

        var unusedHeaderIndices = Enumerable.Range(0, Headers.Count)
            .Where(i => !usedHeaders.Contains(i) && !string.IsNullOrWhiteSpace(Headers[i]))
            .ToList();
        UnusedHeaders = unusedHeaderIndices.Select(i => Headers[i]).ToList();

        // 2. Compute column states + 80% pass rate suggestions for missing columns
        var suggestedHeadersTaken = new HashSet<int>();
        var fieldStates = new List<SpreadsheetFieldState>(Schema.Columns.Count);
        foreach (var col in Schema.Columns)
        {
            int? hIdx = null;
            string? assignedBy = null;
            if (string.IsNullOrEmpty(col.From) && matchedIndexByField.TryGetValue(col.Key, out var m))
            {
                hIdx = m.Index;
                assignedBy = m.AssignedBy;
            }

            var status = hIdx.HasValue ? SpreadsheetColumnStatus.Matched
                : !string.IsNullOrEmpty(col.From) ? SpreadsheetColumnStatus.Matched
                : col.HasDefault ? SpreadsheetColumnStatus.Default
                : col.Required ? SpreadsheetColumnStatus.Missing
                : SpreadsheetColumnStatus.Unmatched;

            var samples = hIdx.HasValue
                ? CollectDistinctSamples(dataRowSlice, hIdx.Value, 3)
                : [];

            SpreadsheetColumnSuggestion? suggestion = null;
            if (status == SpreadsheetColumnStatus.Missing &&CanSuggestColumn(col))
            {
                foreach (var candidateIdx in unusedHeaderIndices)
                {
                    if (suggestedHeadersTaken.Contains(candidateIdx)) continue;
                    var sampleCells = CollectNonEmptyCells(dataRowSlice, candidateIdx, SuggestionSampleSize);
                    if (sampleCells.Count < 2) continue;
                    var passCount = sampleCells.Count(cell => PassesColumnCheck(col, cell));
                    if ((double)passCount / sampleCells.Count >= SuggestionPassRate)
                    {
                        suggestedHeadersTaken.Add(candidateIdx);
                        suggestion = new SpreadsheetColumnSuggestion(
                            candidateIdx,
                            Headers[candidateIdx],
                            sampleCells.Distinct(StringComparer.Ordinal).Take(3).ToList());
                        break;
                    }
                }
            }

            fieldStates.Add(new SpreadsheetFieldState(
                col,
                status,
                hIdx,
                hIdx.HasValue ? Headers[hIdx.Value] : null,
                assignedBy,
                samples,
                suggestion));
        }
        _fields = fieldStates;

        // 3. Parse rows, track unknown select tokens, check duplicates & existing records
        var unknownTokens = new Dictionary<string, (SpreadsheetColumnSchema Col, string RawValue, string NormKey, List<int> Rows)>(StringComparer.Ordinal);
        var parsedRows = new List<(int Index, int RowNumber, Dictionary<string, object?> Data, Dictionary<string, SpreadsheetCellView> Cells, List<SpreadsheetRowIssue> Issues, bool SkippedByValue, bool Edited)>();

        for (var rIdx = 0; rIdx < dataRowSlice.Count; rIdx++)
        {
            var rawCells = dataRowSlice[rIdx];
            var fileRowNumber = dataRowNumbers[rIdx];
            var rowData = new Dictionary<string, object?>(StringComparer.Ordinal);
            var cellViews = new Dictionary<string, SpreadsheetCellView>(StringComparer.Ordinal);
            var rowIssues = new List<SpreadsheetRowIssue>();
            var skippedByValue = false;
            var rowEdited = false;

            foreach (var fState in _fields)
            {
                var col = fState.Column;
                string originalText;
                if (!string.IsNullOrEmpty(col.From) && cellViews.TryGetValue(col.From, out var fromCell))
                {
                    originalText = fromCell.Text;
                }
                else if (fState.HeaderIndex is { } hi && hi < rawCells.Count)
                {
                    originalText = rawCells[hi];
                }
                else
                {
                    originalText = "";
                }

                var hasEdit = _edits.TryGetValue((rIdx, col.Key), out var editedText);
                var cellText = hasEdit ? editedText! : originalText;
                if (hasEdit) rowEdited = true;

                object? parsedVal = null;
                var displayText = cellText;
                var defaulted = false;
                var created = false;
                SpreadsheetRowIssue? fieldIssue = null;

                if (string.IsNullOrWhiteSpace(cellText))
                {
                    if (col.HasDefault)
                    {
                        parsedVal = col.DefaultValue;
                        displayText = col.DefaultValue?.ToString() ?? "";
                        defaulted = true;
                    }
                    else if (col.Required)
                    {
                        fieldIssue = new SpreadsheetRowIssue(col.Key, "required", SpreadsheetIssueLevel.Error, "Required value is missing");
                        rowIssues.Add(fieldIssue);
                    }
                }
                else if (col.Kind == SpreadsheetColumnKind.Select)
                {
                    var matchedOpt = MatchSelectOption(col.Options, cellText);
                    if (matchedOpt is not null)
                    {
                        parsedVal = matchedOpt.Value;
                        displayText = matchedOpt.Label;
                    }
                    else
                    {
                        var normKey = NormalizeText(cellText);
                        var qId = $"{col.Key}::{normKey}";
                        _valueAnswers.TryGetValue(qId, out var userAns);
                        var effectiveAns = userAns ?? PolicyToAnswer(col.UnknownPolicy);

                        if (col.UnknownPolicy != SpreadsheetUnknownValuePolicy.Error)
                        {
                            if (!unknownTokens.TryGetValue(qId, out var entry))
                            {
                                entry = (col, cellText, normKey, []);
                                unknownTokens[qId] = entry;
                            }
                            entry.Rows.Add(rIdx);
                        }

                        if (effectiveAns is not null)
                        {
                            switch (effectiveAns.Action)
                            {
                                case SpreadsheetValueAnswerAction.Map:
                                    parsedVal = effectiveAns.MappedValue;
                                    displayText = effectiveAns.MappedLabel ?? effectiveAns.MappedValue ?? cellText;
                                    break;
                                case SpreadsheetValueAnswerAction.Create:
                                    parsedVal = cellText;
                                    displayText = cellText;
                                    created = true;
                                    break;
                                case SpreadsheetValueAnswerAction.LeaveEmpty:
                                    parsedVal = null;
                                    displayText = "";
                                    break;
                                case SpreadsheetValueAnswerAction.SkipRows:
                                    skippedByValue = true;
                                    fieldIssue = new SpreadsheetRowIssue(col.Key, "value.skipped", SpreadsheetIssueLevel.Info, $"Left out: \"{cellText}\" is set to skip its rows");
                                    rowIssues.Add(fieldIssue);
                                    break;
                            }
                        }
                        else
                        {
                            var code = col.UnknownPolicy == SpreadsheetUnknownValuePolicy.Error ? "value.not-found" : "value.unknown";
                            var msg = col.UnknownPolicy == SpreadsheetUnknownValuePolicy.Error
                                ? $"\"{cellText}\" is not a known value"
                                : $"\"{cellText}\" is not a known value · choose it in Values";
                            fieldIssue = new SpreadsheetRowIssue(col.Key, code, SpreadsheetIssueLevel.Error, msg);
                            rowIssues.Add(fieldIssue);
                        }
                    }
                }
                else
                {
                    if (!TryParseScalar(col, cellText, out parsedVal, out var parseErr))
                    {
                        fieldIssue = new SpreadsheetRowIssue(col.Key, col.Kind.ToString().ToLowerInvariant(), SpreadsheetIssueLevel.Error, parseErr!);
                        rowIssues.Add(fieldIssue);
                    }
                    else
                    {
                        displayText = parsedVal?.ToString() ?? cellText;
                    }
                }

                // Run field rules on non-empty parsed values
                if (parsedVal is not null && col.Rules is { Count: > 0 })
                {
                    foreach (var rule in col.Rules)
                    {
                        if (!rule.Validate(parsedVal))
                        {
                            var ruleIssue = new SpreadsheetRowIssue(col.Key, rule.Code, rule.Level, rule.Message);
                            rowIssues.Add(ruleIssue);
                            fieldIssue ??= ruleIssue;
                        }
                    }
                }

                rowData[col.Key] = parsedVal;
                cellViews[col.Key] = new SpreadsheetCellView(cellText, originalText, displayText, hasEdit, defaulted, created, fieldIssue);
            }

            if (Schema.ValidateRow is not null)
            {
                var extraIssues = Schema.ValidateRow(rowData);
                if (extraIssues is { Count: > 0 })
                {
                    rowIssues.AddRange(extraIssues);
                }
            }

            if (_serverRejections.TryGetValue(rIdx, out var serverErrs))
            {
                rowIssues.AddRange(serverErrs);
            }

            parsedRows.Add((rIdx, fileRowNumber, rowData, cellViews, rowIssues, skippedByValue, rowEdited));
        }

        // 4. Duplicate key detection
        var duplicateDiscards = new HashSet<int>();
        if (!string.IsNullOrEmpty(Schema.RowKeyField))
        {
            var groups = parsedRows
                .Select(r => (Row: r, Key: r.Data.TryGetValue(Schema.RowKeyField, out var kv) ? kv?.ToString()?.Trim().ToLowerInvariant() : null))
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .GroupBy(x => x.Key!, StringComparer.Ordinal);

            foreach (var grp in groups)
            {
                var members = grp.Select(g => g.Row).ToList();
                if (members.Count <= 1) continue;
                if (Schema.Duplicates == SpreadsheetDuplicatesPolicy.Error)
                {
                    foreach (var m in members)
                    {
                        var other = members.First(o => o.Index != m.Index);
                        m.Issues.Add(new SpreadsheetRowIssue(Schema.RowKeyField, "row.duplicate", SpreadsheetIssueLevel.Error, $"Same key as row {other.RowNumber}"));
                    }
                }
                else if (Schema.Duplicates == SpreadsheetDuplicatesPolicy.KeepFirst)
                {
                    foreach (var m in members.Skip(1)) duplicateDiscards.Add(m.Index);
                }
                else if (Schema.Duplicates == SpreadsheetDuplicatesPolicy.KeepLast)
                {
                    foreach (var m in members.Take(members.Count - 1)) duplicateDiscards.Add(m.Index);
                }
            }
        }

        // 5. Finalize rows with discard reasons, existing record modes, and statuses
        var finalRows = new List<SpreadsheetRow>(parsedRows.Count);
        foreach (var pr in parsedRows)
        {
            var mode = SpreadsheetRowMode.Create;
            var existingDiscard = false;
            if (!string.IsNullOrEmpty(Schema.RowKeyField)
                && Schema.ExistingRecords is not null
                && pr.Data.TryGetValue(Schema.RowKeyField, out var keyObj)
                && keyObj?.ToString() is { Length: > 0 } keyStr
                && Schema.ExistingRecords.ContainsKey(keyStr))
            {
                if (Schema.ExistingAction == SpreadsheetExistingAction.Update)
                {
                    mode = SpreadsheetRowMode.Update;
                }
                else if (Schema.ExistingAction == SpreadsheetExistingAction.Skip)
                {
                    mode = SpreadsheetRowMode.Skip;
                    existingDiscard = true;
                }
                else if (Schema.ExistingAction == SpreadsheetExistingAction.Error)
                {
                    mode = SpreadsheetRowMode.Update;
                    pr.Issues.Add(new SpreadsheetRowIssue(Schema.RowKeyField, "row.exists", SpreadsheetIssueLevel.Error, "Already stored"));
                }
            }

            // Precedence: limit > value > duplicate > existing > manual
            SpreadsheetDiscardReason? discard =
                (Schema.MaxRows is { } maxR && pr.Index >= maxR) ? SpreadsheetDiscardReason.Limit
                : pr.SkippedByValue ? SpreadsheetDiscardReason.Value
                : duplicateDiscards.Contains(pr.Index) ? SpreadsheetDiscardReason.Duplicate
                : existingDiscard ? SpreadsheetDiscardReason.Existing
                : _manualDiscards.Contains(pr.Index) ? SpreadsheetDiscardReason.Manual
                : null;

            var hasError = pr.Issues.Any(i => i.Level == SpreadsheetIssueLevel.Error);
            var hasWarning = pr.Issues.Any(i => i.Level == SpreadsheetIssueLevel.Warning);

            var status = discard is not null ? SpreadsheetRowStatus.Discarded
                : hasError ? SpreadsheetRowStatus.Blocking
                : hasWarning ? SpreadsheetRowStatus.Warning
                : SpreadsheetRowStatus.Valid;

            var importable = discard is null && !hasError;
            finalRows.Add(new SpreadsheetRow(
                pr.Index,
                pr.RowNumber,
                pr.Data,
                pr.Cells,
                pr.Issues,
                status,
                discard,
                mode,
                pr.Edited,
                importable));
        }
        _rows = finalRows;

        // 6. Build value questions
        var questionList = new List<SpreadsheetValueQuestion>();
        foreach (var (qId, (col, rawVal, normKey, rowIndices)) in unknownTokens)
        {
            var hasUser = _valueAnswers.TryGetValue(qId, out var uAns);
            var pAns = PolicyToAnswer(col.UnknownPolicy);
            var effective = hasUser ? uAns : pAns;
            var answeredBy = hasUser ? "user" : pAns is not null ? "policy" : null;
            questionList.Add(new SpreadsheetValueQuestion(
                qId,
                col.Key,
                col.EffectiveLabel,
                rawVal,
                normKey,
                rowIndices,
                col.Options ?? [],
                effective,
                answeredBy,
                col.OnCreateOption is not null));
        }
        _questions = questionList;
    }

    private static SpreadsheetValueAnswer? PolicyToAnswer(SpreadsheetUnknownValuePolicy policy) => policy switch
    {
        SpreadsheetUnknownValuePolicy.LeaveEmpty => new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.LeaveEmpty),
        SpreadsheetUnknownValuePolicy.SkipRows => new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.SkipRows),
        SpreadsheetUnknownValuePolicy.Create => new SpreadsheetValueAnswer(SpreadsheetValueAnswerAction.Create),
        _ => null,
    };

    private static bool CanSuggestColumn(SpreadsheetColumnSchema col) =>
        col.Kind != SpreadsheetColumnKind.Text || col.CustomParse is not null || col.Rules is { Count: > 0 };

    private static bool PassesColumnCheck(SpreadsheetColumnSchema col, string sample)
    {
        if (col.Kind == SpreadsheetColumnKind.Select)
        {
            return MatchSelectOption(col.Options, sample) is not null;
        }
        if (!TryParseScalar(col, sample, out var val, out _)) return false;
        if (col.Rules is { Count: > 0 })
        {
            return col.Rules.All(r => r.Level != SpreadsheetIssueLevel.Error || r.Validate(val));
        }
        return true;
    }

    private static SpreadsheetOption? MatchSelectOption(IReadOnlyList<SpreadsheetOption>? options, string text)
    {
        if (options is null || options.Count == 0) return null;
        var norm = NormalizeText(text);
        foreach (var opt in options)
        {
            if (NormalizeText(opt.Label) == norm || NormalizeText(opt.Value) == norm) return opt;
            if (opt.Aliases is not null && opt.Aliases.Any(a => NormalizeText(a) == norm)) return opt;
        }
        return null;
    }

    private static bool TryParseScalar(SpreadsheetColumnSchema col, string text, out object? value, out string? error)
    {
        if (col.CustomParse is not null)
        {
            try
            {
                value = col.CustomParse(text);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                value = null;
                error = ex.Message;
                return false;
            }
        }

        switch (col.Kind)
        {
            case SpreadsheetColumnKind.Number:
                var cleaned = text.Replace(" ", "", StringComparison.Ordinal)
                    .Replace("\u00A0", "", StringComparison.Ordinal)
                    .Replace("'", "", StringComparison.Ordinal);
                cleaned = col.DecimalSeparator == ','
                    ? cleaned.Replace(".", "", StringComparison.Ordinal).Replace(',', '.')
                    : cleaned.Replace(",", "", StringComparison.Ordinal);
                if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    value = d;
                    error = null;
                    return true;
                }
                value = null;
                error = $"\"{text}\" is not a number";
                return false;

            case SpreadsheetColumnKind.Boolean:
                var norm = NormalizeText(text);
                if (DefaultTrueTexts.Contains(norm))
                {
                    value = true;
                    error = null;
                    return true;
                }
                if (DefaultFalseTexts.Contains(norm))
                {
                    value = false;
                    error = null;
                    return true;
                }
                value = null;
                error = $"\"{text}\" is not yes or no";
                return false;

            case SpreadsheetColumnKind.Date:
                var formats = col.DateFormats is { Count: > 0 } ? col.DateFormats.ToArray() : DefaultDateFormats;
                if (DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    value = dt.TimeOfDay == TimeSpan.Zero
                        ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : dt.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                    error = null;
                    return true;
                }
                value = null;
                error = $"\"{text}\" is not a date this column reads";
                return false;

            default:
                value = text.Trim();
                error = null;
                return true;
        }
    }

    private static IReadOnlyList<string> CollectDistinctSamples(IReadOnlyList<IReadOnlyList<string>> rows, int colIdx, int max)
    {
        var list = new List<string>(max);
        foreach (var row in rows)
        {
            if (colIdx >= row.Count) continue;
            var v = row[colIdx];
            if (string.IsNullOrWhiteSpace(v) || list.Contains(v, StringComparer.Ordinal)) continue;
            list.Add(v);
            if (list.Count >= max) break;
        }
        return list;
    }

    private static List<string> CollectNonEmptyCells(IReadOnlyList<IReadOnlyList<string>> rows, int colIdx, int max)
    {
        var list = new List<string>(max);
        foreach (var row in rows)
        {
            if (colIdx >= row.Count) continue;
            var v = row[colIdx];
            if (string.IsNullOrWhiteSpace(v)) continue;
            list.Add(v);
            if (list.Count >= max) break;
        }
        return list;
    }

    private static bool IsHeaderMatch(SpreadsheetColumnSchema col, string headerText)
    {
        var norm = NormalizeText(headerText);
        if (string.IsNullOrEmpty(norm)) return false;
        if (NormalizeText(col.Key) == norm || NormalizeText(col.EffectiveLabel) == norm) return true;
        if (col.Headers is not null)
        {
            return col.Headers.Any(h => NormalizeText(h) == norm);
        }
        return false;
    }

    /// <summary>
    /// Normalizes a spreadsheet header or option value: decomposes to NFD, strips diacritics,
    /// strips trailing <c>*</c> and <c>(required)</c>, collapses whitespace, and lowercases.
    /// </summary>
    public static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var text = input.Trim();
        if (text.EndsWith("(required)", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^10].TrimEnd();
        }
        text = text.TrimEnd('*', ' ');

        var formD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        var prevSpace = false;
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(char.ToLowerInvariant(ch));
                prevSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>Converts a camelCase or PascalCase identifier like <c>secureCode</c> into <c>Secure code</c>.</summary>
    public static string HumanizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i++)
        {
            var ch = key[i];
            if (i == 0)
            {
                sb.Append(char.ToUpperInvariant(ch));
            }
            else if (char.IsUpper(ch))
            {
                sb.Append(' ').Append(char.ToLowerInvariant(ch));
            }
            else if (ch is '_' or '-')
            {
                sb.Append(' ');
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    /// <summary>Parses CSV/TSV delimited text into a <see cref="SpreadsheetSheet"/> preserving 1-based non-empty row numbers.</summary>
    public static SpreadsheetSheet ParseDelimitedSheet(string? content, string sheetName = "Sheet 1")
    {
        if (string.IsNullOrEmpty(content))
        {
            return new SpreadsheetSheet(sheetName, [], []);
        }

        var text = content[0] == '\uFEFF' ? content[1..] : content;
        var sep = DetectSeparator(text);

        var rows = new List<IReadOnlyList<string>>();
        var rowNumbers = new List<int>();
        var current = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fileRowNum = 1;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }
            }
            else if (ch == '"' && field.Length == 0)
            {
                inQuotes = true;
            }
            else if (ch == sep)
            {
                current.Add(field.ToString().Trim());
                field.Clear();
            }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                current.Add(field.ToString().Trim());
                field.Clear();
                if (current.Any(c => c.Length > 0))
                {
                    rows.Add(current);
                    rowNumbers.Add(fileRowNum);
                }
                fileRowNum++;
                current = [];
            }
            else
            {
                field.Append(ch);
            }
        }

        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString().Trim());
            if (current.Any(c => c.Length > 0))
            {
                rows.Add(current);
                rowNumbers.Add(fileRowNum);
            }
        }

        return new SpreadsheetSheet(sheetName, rows, rowNumbers);
    }

    private static char DetectSeparator(string text)
    {
        // Order of precedence on tie: tab, semicolon, comma (spec section 2.2)
        ReadOnlySpan<char> candidates = ['\t', ';', ','];
        Span<int> counts = stackalloc int[3];
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"') inQuotes = !inQuotes;
            else if (!inQuotes)
            {
                if (ch is '\r' or '\n') break;
                for (var c = 0; c < candidates.Length; c++)
                {
                    if (ch == candidates[c]) counts[c]++;
                }
            }
        }

        var best = ',';
        var bestCount = 0;
        for (var c = 0; c < candidates.Length; c++)
        {
            if (counts[c] > bestCount)
            {
                bestCount = counts[c];
                best = candidates[c];
            }
        }
        return best;
    }
}
