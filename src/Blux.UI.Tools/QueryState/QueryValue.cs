using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>
/// One query key as a typed value. A key that is absent, or whose text the codec cannot read, gives the default; a value equal to the
/// default is written as no key at all, so the URL of an untouched page stays clean. Make one with <see cref="QueryState.Bind{T}"/>, and
/// dispose it with the component that owns it.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class QueryValue<T> : IDisposable
{
    private readonly QueryState _state;
    private readonly IQueryCodec<T> _codec;
    private readonly string? _defaultText;
    private readonly IDisposable _subscription;
    private T _known;

    internal QueryValue(QueryState state, string key, IQueryCodec<T> codec, T defaultValue, QueryHistory history)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(codec);
        _state = state;
        Key = key;
        _codec = codec;
        Default = defaultValue;
        History = history;
        _defaultText = codec.Format(defaultValue);
        _known = Read();
        _subscription = state.Subscribe(key, _ => Refresh());
    }

    /// <summary>The query key.</summary>
    public string Key { get; }

    /// <summary>The value of an absent or unreadable key.</summary>
    public T Default { get; }

    /// <summary>The history of changes made through <see cref="Value"/>.</summary>
    public QueryHistory History { get; }

    /// <summary>
    /// The value the URL holds, with the writes of the current handler on top. Setting it writes the URL after the handler yields (see
    /// <see cref="QueryState"/>); the getter shows the new value at once.
    /// </summary>
    public T Value
    {
        get => Read();
        set => Set(value, History);
    }

    /// <summary>Raised when the value changes, through <see cref="Set"/> or through the URL. It runs on the thread that raised the change: from a component, use <c>InvokeAsync</c> before rendering.</summary>
    public event Action? Changed;

    /// <summary>Sets the value with an explicit history.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="history">Whether the change adds a history entry.</param>
    /// <returns>False when nothing was written because the renderer is not interactive.</returns>
    public bool Set(T value, QueryHistory history) => _state.Set(Key, TextOf(value), history);

    /// <summary>The URL of the current page with this key set to <paramref name="value"/> (see <see cref="QueryState.HrefWith(IEnumerable{KeyValuePair{string, string?}})"/>).</summary>
    /// <param name="value">The value the link carries.</param>
    public string HrefWith(T value) => _state.HrefWith((Key, TextOf(value)));

    // A value equal to the default by value (1.0 is 1 for a decimal) or by text is left out.
    private string? TextOf(T value)
    {
        var text = _codec.Format(value);
        return text == _defaultText || QueryEquality.Equal(value, Default) ? null : text;
    }

    private T Read()
    {
        var text = _state.Get(Key);
        return text is not null && _codec.TryParse(text, out var parsed) ? parsed : Default;
    }

    private void Refresh()
    {
        var value = Read();
        if (QueryEquality.Equal(value, _known)) return;
        _known = value;
        Changed?.Invoke();
    }

    /// <summary>Stops following the URL.</summary>
    public void Dispose() => _subscription.Dispose();
}
