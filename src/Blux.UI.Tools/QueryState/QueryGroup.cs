using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>
/// A record whose properties are query keys, read and written together: one write is one navigation and one history entry. Each property has
/// a codec and, like <see cref="QueryValue{T}"/>, is left out of the URL when it equals the matching property of the default record. Make one with
/// <see cref="QueryState.Group{TRecord}"/>, add its properties with <see cref="Add{TValue}"/>, and dispose it with the component that owns it
/// (which also releases its prefix).
/// </summary>
/// <typeparam name="TRecord">An immutable record; its properties are changed with <c>with</c> expressions.</typeparam>
public sealed class QueryGroup<TRecord> : IDisposable where TRecord : class
{
    private readonly QueryState _state;
    private readonly TRecord _default;
    private readonly string _prefix;
    private readonly List<Property> _properties = [];
    private readonly QueryClaim _claim;
    private readonly IDisposable _subscription;
    private TRecord _known;

    internal QueryGroup(QueryState state, TRecord defaultValue, string owner, string? prefix, QueryHistory history)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(defaultValue);
        _state = state;
        _default = defaultValue;
        _known = defaultValue;
        _prefix = prefix ?? "";
        History = history;
        _claim = state.ClaimKeys(_prefix, owner);
        _subscription = state.Subscribe(_prefix, OnKeysChanged);
    }

    /// <summary>Throws when a key of this group is also used by a claim that outlived the page change this group was created in (see <see cref="QueryState.Claim"/>). Every use of the group calls it.</summary>
    /// <exception cref="InvalidOperationException">The conflict, naming both owners.</exception>
    public void ThrowIfConflicted() => _claim.ThrowIfConflicted();

    /// <summary>The history of changes made through <see cref="Set(TRecord)"/> and <see cref="Update"/> without a history of their own.</summary>
    public QueryHistory History { get; }

    /// <summary>Raised when a property of the group changes, through a write or through the URL. It runs on the thread that raised the change: from a component, use <c>InvokeAsync</c> before rendering.</summary>
    public event Action? Changed;

    /// <summary>
    /// Adds a property. The default of the property is the one of the default record.
    /// </summary>
    /// <param name="key">The query key of the property; with a prefix, <c>prefix.key</c>.</param>
    /// <param name="get">Reads the property of a record.</param>
    /// <param name="with">Returns the record with the property set, usually <c>(r, v) =&gt; r with { Page = v }</c>.</param>
    /// <param name="codec">Converts the property to and from text.</param>
    /// <returns>The group, to chain.</returns>
    public QueryGroup<TRecord> Add<TValue>(string key, Func<TRecord, TValue> get, Func<TRecord, TValue, TRecord> with, IQueryCodec<TValue> codec)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(with);
        ArgumentNullException.ThrowIfNull(codec);
        var fullKey = _prefix.Length == 0 ? key : _prefix + "." + key;
        if (_properties.Any(p => p.Key == fullKey)) throw new ArgumentException($"The key '{fullKey}' is already in the group.", nameof(key));
        _claim.AddKey(fullKey);
        var defaultValue = get(_default);
        _properties.Add(new Property(fullKey, record => codec.Format(get(record)), codec.Format(defaultValue), record => QueryEquality.Equal(get(record), defaultValue),
            (record, text) => text is not null && codec.TryParse(text, out var parsed) ? with(record, parsed) : with(record, get(_default))));
        _known = Value;
        return this;
    }

    /// <summary>The record the URL describes: the default with each property whose key is present and readable.</summary>
    public TRecord Value
    {
        get
        {
            _claim.ThrowIfConflicted();
            var record = _default;
            foreach (var property in _properties) record = property.Read(record, _state.Get(property.Key));
            return record;
        }
    }

    /// <summary>Writes every property of <paramref name="value"/> in one change, with the history of the group. A property equal to its default is removed.</summary>
    /// <param name="value">The new record.</param>
    /// <returns>False when nothing was written because the renderer is not interactive.</returns>
    public bool Set(TRecord value) => Set(value, History);

    /// <summary>Writes every property of <paramref name="value"/> in one change.</summary>
    /// <param name="value">The new record.</param>
    /// <param name="history">Whether the change adds a history entry.</param>
    /// <returns>False when nothing was written because the renderer is not interactive.</returns>
    public bool Set(TRecord value, QueryHistory history)
    {
        ArgumentNullException.ThrowIfNull(value);
        _claim.ThrowIfConflicted();
        return _state.SetMany(Changes(value), history);
    }

    /// <summary>Changes the current record with <paramref name="change"/> and writes it, for example <c>g.Update(f =&gt; f with { Page = 1 }, QueryHistory.Push)</c>.</summary>
    /// <param name="change">Returns the new record from the current one.</param>
    /// <param name="history">Whether the change adds a history entry; the history of the group when null.</param>
    public bool Update(Func<TRecord, TRecord> change, QueryHistory? history = null)
    {
        ArgumentNullException.ThrowIfNull(change);
        return Set(change(Value), history ?? History);
    }

    /// <summary>The URL of the current page with the properties of <paramref name="value"/> written, for a link in static server rendering.</summary>
    /// <param name="value">The record the link carries.</param>
    public string HrefWith(TRecord value)
    {
        _claim.ThrowIfConflicted();
        return _state.HrefWith(Changes(value));
    }

    private List<KeyValuePair<string, string?>> Changes(TRecord value) =>
        [.. _properties.Select(p =>
        {
            var text = p.Format(value);
            return new KeyValuePair<string, string?>(p.Key, text == p.DefaultText || p.IsDefault(value) ? null : text);
        })];

    private void OnKeysChanged(IReadOnlyList<string> keys)
    {
        if (!keys.Any(key => _properties.Any(p => p.Key == key))) return;
        var value = Value;
        if (value.Equals(_known)) return;
        _known = value;
        Changed?.Invoke();
    }

    /// <summary>Stops following the URL and releases the prefix.</summary>
    public void Dispose()
    {
        _subscription.Dispose();
        _claim.Dispose();
    }

    private sealed record Property(string Key, Func<TRecord, string?> Format, string? DefaultText, Func<TRecord, bool> IsDefault, Func<TRecord, string?, TRecord> Read);
}
