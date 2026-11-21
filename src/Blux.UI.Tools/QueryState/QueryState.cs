using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Blux.UI.Tools;

/// <summary>Whether a change of the URL adds a history entry (the Back button undoes it) or replaces the current one.</summary>
public enum QueryHistory
{
    /// <summary>A new history entry: a page, a sort, an applied filter.</summary>
    Push,

    /// <summary>The current entry is rewritten: typing in a search box, a normalized value.</summary>
    Replace,
}

/// <summary>
/// The query string of the current URL as state: read it, change it, follow it when it changes. The URL is the single source of truth, so a
/// link, the Back button and a URL typed in the address bar all change what the components bound to it show, and a page can be shared
/// with its state. One instance per circuit or per browser tab (it is scoped), registered by <c>AddBluxUiTools</c>.
/// </summary>
/// <remarks>
/// <para>
/// Writes made in the same event handler merge and become a single navigation, started after the handler yields; if any of them asks for
/// <see cref="QueryHistory.Push"/> the whole batch pushes. A value equal to what the URL already holds writes nothing. Parameters this service
/// never wrote (<c>utm_source</c>, a parameter of another component) stay in the URL as they were.
/// </para>
/// <para>
/// Nothing is written while the page is prerendered or rendered statically, because no navigation can happen there: a link carries the change
/// instead (<see cref="HrefWith(IEnumerable{KeyValuePair{string, string?}})"/>). A component tells the service which renderer it runs on with
/// <see cref="UseRenderer"/>; without it, a write that cannot navigate is dropped quietly.
/// </para>
/// <para>A query key matches a prefix when it equals it or starts with the prefix and a dot (<c>f</c> matches <c>f.name</c>); the empty prefix matches every key.</para>
/// </remarks>
public sealed class QueryState : IDisposable
{
    private readonly NavigationManager _navigation;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, string?> _pending = new(StringComparer.Ordinal);
    private readonly List<(string Canonical, long At)> _inflight = [];
    private readonly Dictionary<string, PrefixClaim> _claims = new(StringComparer.Ordinal);
    private Subscription[] _subscriptions = [];
    private List<QueryPair>? _current;
    private bool _pendingPush;
    private bool _flushScheduled;
    private bool? _interactive;
    private bool _disposed;

    // Navigations this service started are expected back through LocationChanged; those older than this are forgotten, so that going Back to
    // a URL whose event never came is read as the external navigation it is.
    private const long InflightLifetimeMilliseconds = 10_000;

    /// <summary>Creates the service. <c>AddBluxUiTools</c> registers it scoped; create it yourself only in a test.</summary>
    public QueryState(NavigationManager navigation)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        _navigation = navigation;
        _navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Tells the service whether the component calling it runs on an interactive renderer. Only an interactive renderer can navigate, so while
    /// this is false a write changes nothing (see the remarks of the class). Call it from <c>OnInitialized</c> with the <c>RendererInfo</c> of the component.
    /// </summary>
    public void UseRenderer(RendererInfo renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _interactive = renderer.IsInteractive;
    }

    /// <summary>True when a write can navigate, false when the renderer is known not to be interactive, null when no component said (<see cref="UseRenderer"/>).</summary>
    public bool? Interactive => _interactive;

    /// <summary>The first value of a query key, including the writes of the current handler that have not reached the URL yet; null when the key is absent.</summary>
    public string? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_gate)
        {
            return Effective(key);
        }
    }

    /// <summary>Sets a query key; a null value removes it. Returns false when nothing was written because the renderer is not interactive.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The new text, or null to remove the key.</param>
    /// <param name="history">Whether the change adds a history entry.</param>
    public bool Set(string key, string? value, QueryHistory history = QueryHistory.Replace) =>
        SetMany([new KeyValuePair<string, string?>(key, value)], history);

    /// <summary>Sets several keys in one change: one navigation, one history entry. A null value removes its key.</summary>
    /// <param name="changes">The keys and their new text.</param>
    /// <param name="history">Whether the change adds a history entry.</param>
    /// <returns>False when nothing was written because the renderer is not interactive.</returns>
    public bool SetMany(IEnumerable<KeyValuePair<string, string?>> changes, QueryHistory history = QueryHistory.Replace)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var requested = changes.ToList();
        if (_interactive == false) return false;
        List<string> changed = [];
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureCurrent();
            foreach (var (key, value) in requested)
            {
                ArgumentNullException.ThrowIfNull(key);
                if (Effective(key) != value) changed.Add(key);
                _pending[key] = value;
            }
            if (history == QueryHistory.Push) _pendingPush = true;
            if (!_flushScheduled)
            {
                _flushScheduled = true;
                _ = FlushAfterYieldAsync();
            }
        }
        if (changed.Count > 0) Notify(changed);
        return true;
    }

    /// <summary>
    /// Sends the changes made so far to the URL now instead of after the current handler yields. Useful before leaving the page by another
    /// means; the changes of a handler normally need no call.
    /// </summary>
    public Task FlushAsync()
    {
        Flush();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Calls <paramref name="handler"/> with the keys that changed, when a key matching <paramref name="prefix"/> changes: through a write of
    /// this service, or through the URL (Back and Forward, a link, a typed URL). The handler runs on the thread that raised the change: from a
    /// component, go through <c>InvokeAsync</c> before rendering. It is not called for a navigation that leaves the state as it already is.
    /// </summary>
    /// <param name="prefix">The prefix of the keys to follow, or the empty text for all of them.</param>
    /// <param name="handler">Receives the changed keys that match, in no particular order.</param>
    /// <returns>Dispose it to stop.</returns>
    public IDisposable Subscribe(string prefix, Action<IReadOnlyList<string>> handler)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(handler);
        var subscription = new Subscription(this, prefix, handler);
        lock (_gate)
        {
            _subscriptions = [.. _subscriptions, subscription];
        }
        return subscription;
    }

    /// <summary>
    /// The URL (path and query) of the current page with the changes applied, for a link or a form that carries a state change without
    /// interactivity (static server rendering): the same keys as <see cref="Set"/> writes. A null value removes its key.
    /// </summary>
    /// <param name="changes">The keys and their new text.</param>
    public string HrefWith(IEnumerable<KeyValuePair<string, string?>> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var overlay = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in changes) overlay[key] = value;
        lock (_gate)
        {
            EnsureCurrent();
            var pairs = QueryPairs.Apply(QueryPairs.Apply(_current!, _pending), overlay);
            return new Uri(_navigation.Uri).AbsolutePath + QueryPairs.ToQuery(pairs);
        }
    }

    /// <inheritdoc cref="HrefWith(IEnumerable{KeyValuePair{string, string?}})"/>
    public string HrefWith(params (string Key, string? Value)[] changes) =>
        HrefWith(changes.Select(c => new KeyValuePair<string, string?>(c.Key, c.Value)));

    /// <summary>
    /// Declares that a component owns a prefix of the query keys (<c>users</c> owns <c>users.q</c>, <c>users.p</c>), or all of them with the
    /// empty prefix. Two components on one page that own the same prefix would write over each other, so the second claim throws and names both owners.
    /// A claim made on another page than the current claim's page replaces it: the router creates the next page before it disposes the previous one.
    /// </summary>
    /// <param name="prefix">The prefix, or the empty text.</param>
    /// <param name="owner">A name for the claiming component, used in the message.</param>
    /// <returns>Dispose it to release the prefix.</returns>
    /// <exception cref="InvalidOperationException">The prefix is already claimed on this page.</exception>
    public IDisposable Claim(string prefix, string owner)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        var path = new Uri(_navigation.Uri).AbsolutePath;
        lock (_gate)
        {
            if (_claims.TryGetValue(prefix, out var existing) && existing.Path == path)
            {
                var label = prefix.Length == 0 ? "the empty prefix" : $"the prefix '{prefix}'";
                throw new InvalidOperationException($"{label} of the query string is already claimed by '{existing.Owner}', and '{owner}' claims it too: give one of them another prefix (for example UrlKey=\"...\"), or turn the URL sync of one of them off.");
            }
            var claim = new PrefixClaim(this, prefix, owner, path);
            _claims[prefix] = claim;
            return claim;
        }
    }

    /// <summary>Binds one query key to a typed value. See <see cref="QueryValue{T}"/>.</summary>
    /// <param name="key">The query key.</param>
    /// <param name="codec">Converts the value to and from text.</param>
    /// <param name="defaultValue">The value when the key is absent or does not parse; a value equal to it is left out of the URL.</param>
    /// <param name="history">The history of changes made through <see cref="QueryValue{T}.Value"/>.</param>
    public QueryValue<T> Bind<T>(string key, IQueryCodec<T> codec, T defaultValue, QueryHistory history = QueryHistory.Replace) =>
        new(this, key, codec, defaultValue, history);

    /// <summary>Binds a record whose properties are query keys. See <see cref="QueryGroup{TRecord}"/>.</summary>
    /// <param name="defaultValue">The value when the URL holds none of the keys; each property left out of the URL when equal to its value here.</param>
    /// <param name="owner">A name for the claiming component, used in the message when two groups claim the same prefix.</param>
    /// <param name="prefix">The prefix of the keys (<c>users</c> gives <c>users.q</c>), or null for none; it is claimed with <see cref="Claim"/>.</param>
    /// <param name="history">The history of changes made through <see cref="QueryGroup{TRecord}.Set(TRecord)"/>.</param>
    public QueryGroup<TRecord> Group<TRecord>(TRecord defaultValue, string owner, string? prefix = null, QueryHistory history = QueryHistory.Replace) where TRecord : class =>
        new(this, defaultValue, owner, prefix, history);

    /// <summary>Stops following the URL. The scope does it at its end.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _subscriptions = [];
        }
        _navigation.LocationChanged -= OnLocationChanged;
    }

    // The effective value is what the URL holds with the writes of the current batch on top.
    private string? Effective(string key)
    {
        EnsureCurrent();
        return _pending.TryGetValue(key, out var pending) ? pending : QueryPairs.First(_current!, key);
    }

    private void EnsureCurrent() => _current ??= QueryPairs.Parse(_navigation.Uri);

    private async Task FlushAfterYieldAsync()
    {
        await Task.Yield();
        Flush();
    }

    private void Flush()
    {
        string target;
        bool push;
        lock (_gate)
        {
            _flushScheduled = false;
            if (_disposed || _pending.Count == 0)
            {
                _pending.Clear();
                _pendingPush = false;
                return;
            }
            EnsureCurrent();
            var next = QueryPairs.Apply(_current!, _pending);
            push = _pendingPush;
            _pending.Clear();
            _pendingPush = false;
            if (QueryPairs.Canonical(next) == QueryPairs.Canonical(_current!))
            {
                return;
            }
            _current = next;
            var now = Environment.TickCount64;
            _inflight.RemoveAll(e => now - e.At > InflightLifetimeMilliseconds);
            _inflight.Add((QueryPairs.Canonical(next), now));
            target = QueryPairs.WithoutQuery(_navigation.Uri) + QueryPairs.ToQuery(next) + QueryPairs.Fragment(_navigation.Uri);
        }
        try
        {
            // The event this raises is ours: OnLocationChanged finds it in _inflight.
            _navigation.NavigateTo(target, new NavigationOptions { ReplaceHistoryEntry = !push });
        }
        catch (NavigationException)
        {
            // A renderer that cannot navigate (prerendering, static rendering) turns the call into a redirect: nothing to do.
        }
        catch (ObjectDisposedException)
        {
            // The request or the circuit ended between the write and its flush.
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        List<string> changed;
        lock (_gate)
        {
            if (_disposed || _current is null) return;
            var parsed = QueryPairs.Parse(e.Location);
            var canonical = QueryPairs.Canonical(parsed);
            var at = _inflight.FindIndex(entry => entry.Canonical == canonical);
            if (at >= 0)
            {
                // The echo of a navigation of ours, perhaps an older one while newer writes are already in: those are the state, not this.
                _inflight.RemoveRange(0, at + 1);
                return;
            }
            if (canonical == QueryPairs.Canonical(_current))
            {
                return;
            }
            // A navigation we did not start: the URL wins.
            _inflight.Clear();
            changed = QueryPairs.ChangedKeys(_current, parsed);
            foreach (var key in changed) _pending.Remove(key);
            _current = parsed;
        }
        if (changed.Count > 0) Notify(changed);
    }

    private void Notify(List<string> changed)
    {
        Subscription[] subscriptions;
        lock (_gate)
        {
            subscriptions = _subscriptions;
        }
        foreach (var subscription in subscriptions)
        {
            var matching = changed.Where(subscription.Matches).ToList();
            if (matching.Count > 0) subscription.Handler(matching);
        }
    }

    internal static bool KeyMatches(string prefix, string key) =>
        prefix.Length == 0 || key == prefix || (key.Length > prefix.Length && key[prefix.Length] == '.' && key.StartsWith(prefix, StringComparison.Ordinal));

    private void Unsubscribe(Subscription subscription)
    {
        lock (_gate)
        {
            _subscriptions = [.. _subscriptions.Where(s => s != subscription)];
        }
    }

    private void Release(PrefixClaim claim)
    {
        lock (_gate)
        {
            if (_claims.TryGetValue(claim.Prefix, out var current) && current == claim) _claims.Remove(claim.Prefix);
        }
    }

    private sealed class Subscription(QueryState owner, string prefix, Action<IReadOnlyList<string>> handler) : IDisposable
    {
        public Action<IReadOnlyList<string>> Handler { get; } = handler;

        public bool Matches(string key) => KeyMatches(prefix, key);

        public void Dispose() => owner.Unsubscribe(this);
    }

    private sealed class PrefixClaim(QueryState owner, string prefix, string name, string path) : IDisposable
    {
        public string Prefix { get; } = prefix;

        public string Owner { get; } = name;

        public string Path { get; } = path;

        public void Dispose() => owner.Release(this);
    }
}
