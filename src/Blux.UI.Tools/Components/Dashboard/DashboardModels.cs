using System.Globalization;
using System.Text;

namespace Blux.UI.Tools;

/// <summary>Lifecycle state of a dashboard query or derived resource.</summary>
public enum DashboardResourceState
{
    Idle,
    Loading,
    Ready,
    Error,
    Disabled,
}

/// <summary>Execution stage of a dashboard query.</summary>
public enum DashboardQueryStage
{
    Essential,
    Background,
    Deferred,
}

/// <summary>Block rendering phase determined by <see cref="BxDashboardCard"/>.</summary>
public enum DashboardBlockPhase
{
    Idle,
    Loading,
    Error,
    Empty,
    Content,
}

/// <summary>Skeleton geometry kind rendered while a <see cref="BxDashboardCard"/> is loading.</summary>
public enum DashboardSkeletonKind
{
    Rows,
    Stat,
    Stats,
    Gauge,
    Details,
    Bars,
    Paired,
    Funnel,
    Stack,
    Table,
    Feed,
    Lines,
    Donut,
}

/// <summary>Preset number formats supported by <see cref="DashboardFormat"/>.</summary>
public enum DashboardFormatPreset
{
    Number,
    Integer,
    Decimal,
    Compact,
    Percent,
    Ratio,
    Delta,
    Points,
    Signed,
}

/// <summary>Comparison period mode for date ranges and KPI comparisons.</summary>
public enum DashboardComparisonMode
{
    None,
    Previous,
    Year,
}

/// <summary>Filter control kind in a <see cref="DashboardController"/>.</summary>
public enum DashboardFilterKind
{
    String,
    Number,
    Boolean,
    Date,
    DateRange,
    Enum,
    Options,
    Remote,
    Comparison,
    Custom,
}

/// <summary>Alert severity level for <see cref="BxDashboardAlerts"/>.</summary>
public enum DashboardAlertSeverity
{
    Error,
    Warning,
    Info,
    Success,
}

/// <summary>Common interface implemented by dashboard queries and derived resources.</summary>
public interface IDashboardSource
{
    string Key { get; }
    DashboardQueryStage Stage { get; }
    DashboardResourceState State { get; }
    string? Error { get; }
    bool Refreshing { get; }
    bool Fetching { get; }
    DateTimeOffset? UpdatedAt { get; }
    bool Active { get; }
    Task ActivateAsync();
    Task RefreshAsync();
}

/// <summary>
/// A staged, optionally gated asynchronous data resource managed by a <see cref="DashboardController"/>.
/// </summary>
public sealed class DashboardResource<TData> : IDashboardSource
{
    private readonly Func<CancellationToken, Task<TData>>? _loader;
    private readonly Func<bool>? _enabled;
    private readonly Func<bool>? _requires;

    public DashboardResource(
        string key,
        Func<CancellationToken, Task<TData>>? loader = null,
        DashboardQueryStage stage = DashboardQueryStage.Essential,
        TData? defaultValue = default,
        Func<bool>? enabled = null,
        Func<bool>? requires = null)
    {
        Key = key;
        _loader = loader;
        Stage = stage;
        DefaultValue = defaultValue;
        Data = defaultValue;
        _enabled = enabled;
        _requires = requires;
        if (_enabled is not null && !_enabled())
        {
            State = DashboardResourceState.Disabled;
        }
    }

    public event Action? Changed;

    public string Key { get; }
    public DashboardQueryStage Stage { get; }
    public TData? DefaultValue { get; }
    public TData? Data { get; private set; }
    public DashboardResourceState State { get; private set; } = DashboardResourceState.Idle;
    public string? Error { get; private set; }
    public bool Refreshing { get; private set; }
    public bool Fetching { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public bool Active { get; private set; }

    /// <summary>Sets the resource data directly and transitions to <see cref="DashboardResourceState.Ready"/>.</summary>
    public void SetData(TData? data)
    {
        Data = data;
        Error = null;
        State = DashboardResourceState.Ready;
        Refreshing = false;
        Fetching = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        Active = true;
        Changed?.Invoke();
    }

    /// <summary>Transitions the resource to <see cref="DashboardResourceState.Error"/>.</summary>
    public void SetError(string message)
    {
        Error = message;
        State = DashboardResourceState.Error;
        Refreshing = false;
        Fetching = false;
        Changed?.Invoke();
    }

    public async Task ActivateAsync()
    {
        if (_enabled is not null && !_enabled())
        {
            State = DashboardResourceState.Disabled;
            Data = DefaultValue;
            Changed?.Invoke();
            return;
        }
        Active = true;
        if (State == DashboardResourceState.Idle)
        {
            await RefreshAsync();
        }
    }

    public async Task RefreshAsync()
    {
        if (_enabled is not null && !_enabled())
        {
            State = DashboardResourceState.Disabled;
            Data = DefaultValue;
            Changed?.Invoke();
            return;
        }

        if (_requires is not null && !_requires())
        {
            State = DefaultValue is not null ? DashboardResourceState.Ready : DashboardResourceState.Idle;
            Data = DefaultValue;
            Changed?.Invoke();
            return;
        }

        if (_loader is null)
        {
            State = DashboardResourceState.Ready;
            UpdatedAt ??= DateTimeOffset.UtcNow;
            Changed?.Invoke();
            return;
        }

        Active = true;
        var wasReady = State == DashboardResourceState.Ready;
        Refreshing = wasReady;
        Fetching = true;
        if (!wasReady)
        {
            State = DashboardResourceState.Loading;
        }
        Error = null;
        Changed?.Invoke();

        try
        {
            Data = await _loader(CancellationToken.None);
            State = DashboardResourceState.Ready;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            State = DashboardResourceState.Error;
        }
        finally
        {
            Refreshing = false;
            Fetching = false;
            Changed?.Invoke();
        }
    }
}

/// <summary>An option item in a <see cref="DashboardFilterControl"/>.</summary>
public sealed record DashboardOption(
    string Value,
    string Label,
    string? Description = null,
    string? Icon = null,
    bool Disabled = false);

/// <summary>A preset value in a <see cref="DashboardFilterControl"/>.</summary>
public sealed record DashboardFilterPreset(
    string Label,
    IReadOnlyList<string> Values,
    string? Icon = null);

/// <summary>Interactive filter control state managed by <see cref="DashboardController"/>.</summary>
public sealed class DashboardFilterControl
{
    private readonly List<string> _values = [];
    private readonly List<string> _defaultValues = [];

    public DashboardFilterControl(
        string key,
        string label,
        DashboardFilterKind kind = DashboardFilterKind.Options,
        IReadOnlyList<DashboardOption>? items = null,
        IReadOnlyList<string>? defaultValues = null,
        bool multiple = false,
        bool searchable = false,
        int? max = null,
        string placeholder = "All",
        bool headless = false,
        IReadOnlyList<DashboardFilterPreset>? presets = null)
    {
        Key = key;
        Label = label;
        Kind = kind;
        Items = items ?? [];
        Multiple = multiple;
        Searchable = searchable;
        Max = max;
        Placeholder = placeholder;
        Headless = headless;
        Presets = presets ?? [];

        if (defaultValues is { Count: > 0 })
        {
            _defaultValues.AddRange(defaultValues);
            _values.AddRange(defaultValues);
        }
    }

    public event Action? ChangedEvent;

    public string Key { get; }
    public string Label { get; }
    public DashboardFilterKind Kind { get; }
    public bool Multiple { get; }
    public bool Searchable { get; }
    public int? Max { get; }
    public string Placeholder { get; }
    public bool Headless { get; }
    public bool Enabled { get; set; } = true;
    public IReadOnlyList<DashboardOption> Items { get; set; }
    public IReadOnlyList<DashboardFilterPreset> Presets { get; }
    public string Search { get; set; } = "";
    public bool Open { get; set; }

    public IReadOnlyList<string> Values => Enabled ? _values : _defaultValues;
    public string? Value
    {
        get => Values.Count > 0 ? Values[0] : null;
        set
        {
            if (!Enabled) return;
            _values.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                _values.Add(value);
            }
            else if (_defaultValues.Count > 0)
            {
                _values.AddRange(_defaultValues);
            }
            ChangedEvent?.Invoke();
        }
    }

    public bool Changed =>
        Enabled && !Headless && !_values.SequenceEqual(_defaultValues, StringComparer.Ordinal);

    public string Display
    {
        get
        {
            var current = Values;
            if (current.Count == 0) return Placeholder;
            var labels = current
                .Select(v => Items.FirstOrDefault(i => i.Value == v)?.Label ?? v)
                .ToList();
            if (labels.Count <= 2) return string.Join(", ", labels);
            return $"{labels[0]} +{labels.Count - 1}";
        }
    }

    public IReadOnlyList<DashboardOption> FilteredItems =>
        string.IsNullOrWhiteSpace(Search)
            ? Items
            : Items.Where(i => i.Label.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

    public bool IsSelected(string value) => Values.Contains(value, StringComparer.Ordinal);

    public void Toggle(string value)
    {
        if (!Enabled) return;
        if (!Multiple)
        {
            _values.Clear();
            _values.Add(value);
            Open = false;
            ChangedEvent?.Invoke();
            return;
        }

        if (_values.Contains(value, StringComparer.Ordinal))
        {
            _values.Remove(value);
        }
        else if (Max is null || _values.Count < Max.Value)
        {
            _values.Add(value);
        }
        ChangedEvent?.Invoke();
    }

    public void ApplyPreset(DashboardFilterPreset preset)
    {
        if (!Enabled) return;
        _values.Clear();
        _values.AddRange(preset.Values);
        ChangedEvent?.Invoke();
    }

    public void Reset()
    {
        if (!Enabled) return;
        _values.Clear();
        _values.AddRange(_defaultValues);
        ChangedEvent?.Invoke();
    }
}

/// <summary>Metadata for a named dashboard view tab.</summary>
public sealed class DashboardViewMeta
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Badge { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Opened { get; set; }
}

/// <summary>
/// Central controller for an analytical dashboard: coordinates filters, views, staged resources
/// (<see cref="DashboardQueryStage.Essential"/>, <see cref="DashboardQueryStage.Background"/>, <see cref="DashboardQueryStage.Deferred"/>),
/// refresh and auto-refresh state.
/// </summary>
public sealed class DashboardController
{
    private readonly List<DashboardFilterControl> _controls = [];
    private readonly List<DashboardViewMeta> _views = [];
    private readonly List<IDashboardSource> _resources = [];
    private string? _currentView;

    public DashboardController(string key = "dashboard")
    {
        Key = key;
    }

    public event Action? Changed;

    public string Key { get; }
    public IReadOnlyList<DashboardFilterControl> Controls => _controls;
    public IReadOnlyList<DashboardViewMeta> Views => _views.Where(v => v.Enabled).ToList();
    public IReadOnlyList<IDashboardSource> Resources => _resources;
    public int AutoRefresh { get; set; }

    public string? CurrentView
    {
        get => _currentView;
        set
        {
            var target = _views.FirstOrDefault(v => v.Key == value && v.Enabled);
            if (target is null) return;
            _currentView = target.Key;
            target.Opened = true;
            Changed?.Invoke();
        }
    }

    public bool Filtered => _controls.Any(c => c.Changed);
    public bool Refreshing => _resources.Any(r => r.Refreshing || r.Fetching);

    public DateTimeOffset? UpdatedAt
    {
        get
        {
            var dates = _resources.Where(r => r.UpdatedAt.HasValue).Select(r => r.UpdatedAt!.Value).ToList();
            return dates.Count > 0 ? dates.Min() : null;
        }
    }

    public DashboardResourceState State =>
        CombineStates(_resources.Where(r => r.Stage == DashboardQueryStage.Essential).Select(r => r.State));

    public DashboardFilterControl AddFilter(DashboardFilterControl filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.ChangedEvent += () => Changed?.Invoke();
        _controls.Add(filter);
        return filter;
    }

    public DashboardViewMeta AddView(string key, string label, string? badge = null, bool enabled = true)
    {
        var meta = new DashboardViewMeta { Key = key, Label = label, Badge = badge, Enabled = enabled };
        _views.Add(meta);
        if (_currentView is null && enabled)
        {
            _currentView = key;
            meta.Opened = true;
        }
        return meta;
    }

    public DashboardResource<TData> RegisterResource<TData>(DashboardResource<TData> resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        resource.Changed += () => Changed?.Invoke();
        _resources.Add(resource);
        return resource;
    }

    public void ResetFilters()
    {
        foreach (var control in _controls)
        {
            if (control.Enabled && !control.Headless)
            {
                control.Reset();
            }
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// Runs staged queries in order: all <see cref="DashboardQueryStage.Essential"/> resources first,
    /// then <see cref="DashboardQueryStage.Background"/> resources once essential ones settle, plus any already active <see cref="DashboardQueryStage.Deferred"/> resources.
    /// </summary>
    public async Task RefreshAsync()
    {
        var essentials = _resources.Where(r => r.Stage == DashboardQueryStage.Essential).ToList();
        await Task.WhenAll(essentials.Select(r => r.RefreshAsync()));

        var backgrounds = _resources.Where(r => r.Stage == DashboardQueryStage.Background).ToList();
        var activeDeferred = _resources.Where(r => r.Stage == DashboardQueryStage.Deferred && r.Active).ToList();
        await Task.WhenAll(backgrounds.Concat(activeDeferred).Select(r => r.RefreshAsync()));
        Changed?.Invoke();
    }

    /// <summary>Combines multiple resource states according to the dashboard state precedence rules.</summary>
    public static DashboardResourceState CombineStates(IEnumerable<DashboardResourceState> states)
    {
        var list = states.ToList();
        if (list.Count == 0) return DashboardResourceState.Ready;
        var active = list.Where(s => s != DashboardResourceState.Disabled).ToList();
        if (active.Count == 0) return DashboardResourceState.Disabled;
        if (active.Contains(DashboardResourceState.Error)) return DashboardResourceState.Error;
        if (active.Contains(DashboardResourceState.Loading)) return DashboardResourceState.Loading;
        if (active.All(s => s == DashboardResourceState.Idle)) return DashboardResourceState.Idle;
        return DashboardResourceState.Ready;
    }
}

/// <summary>Tabular view and CSV export representation of a dashboard block.</summary>
public sealed record DashboardDataColumn(string Key, string Label, bool Numeric = false);

/// <summary>Tabular representation of a dashboard block used by "View as table" and "Download CSV".</summary>
public sealed record DashboardDataTable(
    IReadOnlyList<DashboardDataColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows);

/// <summary>Locale-aware number, delta, relative-time, comparison-range, and CSV formatting utilities for the Dashboard domain.</summary>
public static class DashboardFormat
{
    public const string MissingValue = "—";

    public static string Format(double? value, DashboardFormatPreset preset = DashboardFormatPreset.Number, CultureInfo? culture = null)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return MissingValue;
        }
        var v = value.Value;
        var c = culture ?? CultureInfo.InvariantCulture;
        return preset switch
        {
            DashboardFormatPreset.Integer => Math.Round(v).ToString("#,0", c),
            DashboardFormatPreset.Decimal => v.ToString("#,0.#", c),
            DashboardFormatPreset.Compact => FormatCompact(v, c),
            DashboardFormatPreset.Percent => v.ToString("0.#", c) + "%",
            DashboardFormatPreset.Ratio => (v * 100.0).ToString("0.#", c) + "%",
            DashboardFormatPreset.Delta => (v > 0 ? "+" : "") + v.ToString("0.#", c) + "%",
            DashboardFormatPreset.Points => (v > 0 ? "+" : "") + v.ToString("0.#", c) + (Math.Abs(Math.Abs(v) - 1.0) < 0.0001 ? " pt" : " pts"),
            DashboardFormatPreset.Signed => (v > 0 ? "+" : "") + v.ToString("#,0.#", c),
            _ => Math.Abs(v) >= 10_000 ? FormatCompact(v, c) : v.ToString("#,0.#", c),
        };
    }

    public static string FormatCompact(double? value, CultureInfo? culture = null)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return MissingValue;
        var v = value.Value;
        var abs = Math.Abs(v);
        var c = culture ?? CultureInfo.InvariantCulture;
        if (abs >= 1_000_000_000) return (v / 1_000_000_000.0).ToString("0.#", c) + "B";
        if (abs >= 1_000_000) return (v / 1_000_000.0).ToString("0.#", c) + "M";
        if (abs >= 1_000) return (v / 1_000.0).ToString("0.#", c) + "K";
        return v.ToString("0.#", c);
    }

    public static string FormatCurrency(double? value, string currencySymbol = "€", CultureInfo? culture = null)
    {
        if (value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return MissingValue;
        var c = culture ?? CultureInfo.InvariantCulture;
        return $"{currencySymbol}{value.Value.ToString("#,0.##", c)}";
    }

    public static string FormatRelativeTime(DateTimeOffset? value, DateTimeOffset? now = null)
    {
        if (value is null) return "";
        var reference = now ?? DateTimeOffset.UtcNow;
        var diff = reference - value.Value;
        if (diff.TotalSeconds < 45) return "just now";
        if (diff.TotalMinutes < 45) return $"{Math.Max(1, (int)Math.Round(diff.TotalMinutes))} min ago";
        if (diff.TotalHours < 22) return $"{Math.Max(1, (int)Math.Round(diff.TotalHours))} hr ago";
        if (diff.TotalDays < 1.5) return "yesterday";
        if (diff.TotalDays < 7) return $"{(int)Math.Round(diff.TotalDays)} days ago";
        return value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Computes the comparison date range for <see cref="DashboardComparisonMode.Previous"/> or <see cref="DashboardComparisonMode.Year"/>.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End)? ResolveComparisonRange(
        (DateTimeOffset Start, DateTimeOffset End)? range,
        DashboardComparisonMode comparison)
    {
        if (range is null || comparison == DashboardComparisonMode.None) return null;
        var (start, end) = range.Value;
        if (comparison == DashboardComparisonMode.Year)
        {
            return (ShiftYearBack(start), ShiftYearBack(end));
        }

        var days = Math.Max(1, (int)Math.Round((end.UtcDateTime.Date - start.UtcDateTime.Date).TotalDays) + 1);
        return (start.AddDays(-days), start.AddDays(-1));
    }

    private static DateTimeOffset ShiftYearBack(DateTimeOffset dt)
    {
        var year = dt.Year - 1;
        var day = Math.Min(dt.Day, DateTime.DaysInMonth(year, dt.Month));
        return new DateTimeOffset(year, dt.Month, day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
    }

    /// <summary>Normalizes a block title into a slug suitable for a CSV download filename.</summary>
    public static string ResolveFileName(string? title, string extension = "csv")
    {
        if (string.IsNullOrWhiteSpace(title)) return $"dashboard.{extension}";
        var formD = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        var prevHyphen = false;
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                prevHyphen = false;
            }
            else if (!prevHyphen && sb.Length > 0)
            {
                sb.Append('-');
                prevHyphen = true;
            }
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? $"dashboard.{extension}" : $"{slug}.{extension}";
    }

    /// <summary>Serializes a <see cref="DashboardDataTable"/> to CSV with a UTF-8 BOM and locale-appropriate delimiter.</summary>
    public static string ExportCsv(DashboardDataTable table, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        var c = culture ?? CultureInfo.InvariantCulture;
        var delimiter = c.NumberFormat.NumberDecimalSeparator == "," ? ';' : ',';
        var sb = new StringBuilder();
        sb.Append('\uFEFF');

        sb.AppendLine(string.Join(delimiter, table.Columns.Select(col => QuoteCsv(col.Label))));
        foreach (var row in table.Rows)
        {
            var cells = new List<string>(table.Columns.Count);
            foreach (var col in table.Columns)
            {
                row.TryGetValue(col.Key, out var val);
                cells.Add(val is null ? "" : col.Numeric ? val : QuoteCsv(val));
            }
            sb.Append(string.Join(delimiter, cells)).Append("\r\n");
        }
        return sb.ToString();
    }

    private static string QuoteCsv(string text) =>
        $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

/// <summary>Status pill shown on a <see cref="BxDashboardStat"/> or <see cref="DashboardStatsItem"/>.</summary>
public sealed record DashboardStatus(string Label, BxColor? Color = null);

/// <summary>One metric entry inside <see cref="BxDashboardStats"/>.</summary>
public sealed record DashboardStatsItem(
    string Key,
    string Label,
    double? Value,
    DashboardFormatPreset Format = DashboardFormatPreset.Number,
    double? Delta = null,
    bool InvertDelta = false,
    string? Caption = null,
    double? Progress = null,
    DashboardStatus? Status = null);

/// <summary>One definition row inside <see cref="BxDashboardDetails"/>.</summary>
public sealed record DashboardDetailsItem(
    string Key,
    string Label,
    string? Value,
    string? Hint = null,
    bool Mono = false,
    bool Copy = false,
    string? Href = null,
    bool FullSpan = false,
    bool Hidden = false);

/// <summary>One horizontal bar entry in <see cref="BxDashboardBars"/>.</summary>
public sealed record DashboardBarItem(
    string Label,
    double Value,
    string? Tag = null,
    string? Meta = null,
    bool Selected = false);

/// <summary>One ranked row in <see cref="BxDashboardList"/>.</summary>
public sealed record DashboardListItem(
    string Label,
    string? Description = null,
    double? Value = null,
    double? Percent = null,
    double? Delta = null,
    string? LeadingText = null,
    string? Href = null,
    bool Selected = false);

/// <summary>Column definition for <see cref="BxDashboardTable"/>.</summary>
public sealed record DashboardTableColumn(
    string Key,
    string Label,
    string Type = "text",
    DashboardFormatPreset Format = DashboardFormatPreset.Number,
    bool InvertDelta = false,
    bool Sortable = true);

/// <summary>Active sort state of <see cref="BxDashboardTable"/>.</summary>
public sealed record DashboardTableSort(string Key, bool Descending);

/// <summary>One alert row in <see cref="BxDashboardAlerts"/>.</summary>
public sealed record DashboardAlertItem(
    DashboardAlertSeverity Severity,
    string Label,
    string? Description = null,
    string? Value = null,
    string? ActionLabel = null,
    string? ActionHref = null);

/// <summary>One timeline activity event in <see cref="BxDashboardFeed"/>.</summary>
public sealed record DashboardFeedItem(
    string Label,
    DateTimeOffset Time,
    string? Description = null,
    string? AvatarText = null);

/// <summary>One series definition for <see cref="BxDashboardChart"/>.</summary>
public sealed record DashboardChartSeries(
    string Key,
    string Label,
    IReadOnlyList<double> Values,
    string Type = "bar",
    bool IsComparison = false);
