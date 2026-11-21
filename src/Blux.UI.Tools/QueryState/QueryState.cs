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
/// Writes made synchronously in one event handler, before it first yields to the renderer (an <c>await</c> that does not complete at once), merge
/// and become a single navigation started right after; a write made after that point is another batch. If any write of a batch asks for
/// <see cref="QueryHistory.Push"/> the whole batch pushes. A value equal to what the URL already holds writes nothing. Parameters this service
/// never wrote (<c>utm_source</c>, a parameter of another component) stay in the URL as they were. A handler that writes and then navigates to
/// another page (<c>Query.Set(...); Navigation.NavigateTo("/details/5")</c>) loses the write: the navigation wins.
/// </para>
/// <para>
/// <b>Prerendering and static rendering.</b> No navigation can happen there, and a navigation manager that is configured not to throw
/// (<c>BlazorDisableThrowNavigationException</c>) turns the attempt into a real HTTP redirect. So a write is never sent to the navigation manager
/// unless the renderer is known to be interactive: <see cref="QueryState.Bind{T}"/> and <see cref="QueryState.Group{TRecord}"/> take the
/// <see cref="RendererInfo"/> of the component for this, and a write from a non-interactive renderer changes nothing and returns false. A page offers
/// its changes as links instead (<see cref="HrefWith(IEnumerable{KeyValuePair{string, string?}})"/>). The raw <see cref="Set"/> and <see cref="SetMany"/>
/// need the same knowledge, from <see cref="UseRenderer"/>; without it they throw <see cref="InvalidOperationException"/> on the server (in WebAssembly the renderer is always interactive).
/// </para>
/// <para>
/// A navigation of this service that never comes back (a <c>NavigationLock</c> cancelled it) is rolled back after a few seconds to what the URL really holds.
/// A key matches a prefix when it equals it or starts with the prefix and a dot (<c>f</c> matches <c>f.name</c>); the empty prefix matches every key.
/// </para>
/// </remarks>
public sealed class QueryState : IDisposable
{
    private readonly NavigationManager _navigation;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, string?> _pending = new(StringComparer.Ordinal);
    private readonly List<Inflight> _inflight = [];
    private readonly List<QueryClaim> _claims = [];
    private readonly List<(QueryClaim Claim, QueryClaim Other, string Message)> _deferredClaims = [];
    private readonly CancellationTokenSource _lifetime = new();
    private Subscription[] _subscriptions = [];
    private List<QueryPair>? _current;
    private IDisposable? _changing;
    private string? _lastPath;
    private long _generation;
    private long _nextInflightId;
    private bool _pendingPush;
    private bool _flushScheduled;
    private bool _claimCheckScheduled;
    private bool? _interactive;
    private bool _disposed;

    private sealed record Inflight(long Id, string Canonical, long At);

    /// <summary>How long an own navigation can come back and still be recognized as ours; a URL reached later by Back is an external navigation.</summary>
    internal TimeSpan EchoLifetime { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>How long to wait for an own navigation before concluding it was cancelled and showing what the URL holds again.</summary>
    internal TimeSpan RollbackDelay { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>Creates the service. <c>AddBluxUiTools</c> registers it scoped; create it yourself only in a test.</summary>
    public QueryState(NavigationManager navigation)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        _navigation = navigation;
        _navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Tells the service whether the renderer is interactive. Only an interactive renderer can navigate, so while this is false a write changes
    /// nothing (see the remarks of the class). <see cref="Bind{T}"/> and <see cref="Group{TRecord}"/> call it with the renderer you pass; call it
    /// yourself before the raw <see cref="Set"/> and <see cref="SetMany"/>, from <c>OnInitialized</c> with the <c>RendererInfo</c> of the component.
    /// </summary>
    public void UseRenderer(RendererInfo renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _interactive = renderer.IsInteractive;
    }

    /// <summary>True when writes can navigate, false when the renderer is known not to be interactive, null when nothing said so yet (<see cref="UseRenderer"/>).</summary>
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

    /// <summary>Every key and value of the query whose key matches <paramref name="prefix"/> (all of them for the empty prefix), with the writes of the current handler on top: what a component that owns a prefix reads its state from.</summary>
    internal IReadOnlyList<KeyValuePair<string, string?>> Snapshot(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        lock (_gate)
        {
            EnsureCurrent();
            return [.. QueryPairs.Apply(_current!, _pending).Where(p => KeyMatches(prefix, p.Key)).Select(p => new KeyValuePair<string, string?>(p.Key, p.Value))];
        }
    }

    /// <summary>Sets a query key; a null value removes it. Returns false when nothing was written because the renderer is not interactive.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The new text, or null to remove the key.</param>
    /// <param name="history">Whether the change adds a history entry.</param>
    /// <exception cref="InvalidOperationException">The renderer is not known (<see cref="UseRenderer"/>) on the server.</exception>
    public bool Set(string key, string? value, QueryHistory history = QueryHistory.Replace) =>
        SetMany([new KeyValuePair<string, string?>(key, value)], history);

    /// <summary>Sets several keys in one change: one navigation, one history entry. A null value removes its key.</summary>
    /// <param name="changes">The keys and their new text.</param>
    /// <param name="history">Whether the change adds a history entry.</param>
    /// <returns>False when nothing was written because the renderer is not interactive.</returns>
    /// <exception cref="InvalidOperationException">The renderer is not known (<see cref="UseRenderer"/>) on the server.</exception>
    public bool SetMany(IEnumerable<KeyValuePair<string, string?>> changes, QueryHistory history = QueryHistory.Replace)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var requested = changes.ToList();
        if (_interactive == false) return false;
        if (_interactive is null && !OperatingSystem.IsBrowser())
        {
            throw new InvalidOperationException("QueryState does not know whether the renderer is interactive, and writing from a prerender or a static render would redirect the request. Call QueryState.UseRenderer(RendererInfo) first, or write through Bind and Group, which take the RendererInfo of the component.");
        }
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
                // Until the flush, a navigation to another page drops the batch: the write belongs to the page being left.
                RegisterChanging();
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
    /// empty prefix. A second claim of the same prefix throws and names both owners. A claim that meets one made before the last change of page
    /// (a layout, a page reused for another route parameter) is judged once the render that is under way has finished and the previous page is
    /// disposed: <see cref="QueryClaim.ThrowIfConflicted"/> then reports it (<see cref="QueryGroup{TRecord}"/> calls it on every use).
    /// </summary>
    /// <param name="prefix">The prefix, or the empty text.</param>
    /// <param name="owner">A name for the claiming component, used in the message.</param>
    /// <returns>Dispose it to release the prefix.</returns>
    /// <exception cref="InvalidOperationException">The prefix is already claimed on this page.</exception>
    public QueryClaim Claim(string prefix, string owner)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        return AddClaim(new QueryClaim(this, prefix, owner, wholePrefix: true));
    }

    /// <summary>A claim on keys added one by one (what a group does): two claims conflict when they share a key.</summary>
    internal QueryClaim ClaimKeys(string prefix, string owner) => AddClaim(new QueryClaim(this, prefix, owner, wholePrefix: false));

    private QueryClaim AddClaim(QueryClaim claim)
    {
        lock (_gate)
        {
            EnsureCurrent();
            claim.Generation = _generation;
            if (claim.WholePrefix) CheckClaim(claim, null);
            _claims.Add(claim);
        }
        return claim;
    }

    internal void AddClaimKey(QueryClaim claim, string key)
    {
        lock (_gate)
        {
            CheckClaim(claim, key);
            claim.Keys.Add(key);
        }
    }

    // Throws for a conflict with a claim of this page, which cannot be a page on its way out; defers one with an older claim until the render is over.
    private void CheckClaim(QueryClaim claim, string? key)
    {
        foreach (var other in _claims)
        {
            if (other == claim || !QueryClaim.Overlap(claim, key, other, out var shared)) continue;
            var message = shared is null
                ? $"{(claim.Prefix.Length == 0 ? "the empty prefix" : $"the prefix '{claim.Prefix}'")} of the query string is already claimed by '{other.Owner}', and '{claim.Owner}' claims it too: give one of them another prefix (for example UrlKey=\"...\"), or turn the URL sync of one of them off."
                : $"the query key '{shared}' is used by '{other.Owner}' and by '{claim.Owner}': give one of them another prefix or another key, or turn the URL sync of one of them off.";
            if (other.Generation == _generation) throw new InvalidOperationException(char.ToUpperInvariant(message[0]) + message[1..]);
            _deferredClaims.Add((claim, other, message));
            if (!_claimCheckScheduled)
            {
                _claimCheckScheduled = true;
                _ = VerifyClaimsAfterYieldAsync();
            }
        }
    }

    private async Task VerifyClaimsAfterYieldAsync()
    {
        await Task.Yield();
        lock (_gate)
        {
            _claimCheckScheduled = false;
            foreach (var (claim, other, message) in _deferredClaims)
            {
                if (!other.Released && !claim.Released)
                {
                    claim.Conflict = new InvalidOperationException(char.ToUpperInvariant(message[0]) + message[1..]);
                }
            }
            _deferredClaims.Clear();
        }
    }

    /// <summary>Binds one query key to a typed value. See <see cref="QueryValue{T}"/>.</summary>
    /// <param name="key">The query key.</param>
    /// <param name="codec">Converts the value to and from text.</param>
    /// <param name="defaultValue">The value when the key is absent or does not parse; a value equal to it is left out of the URL.</param>
    /// <param name="renderer">The <c>RendererInfo</c> of the component: nothing is written unless it is interactive.</param>
    /// <param name="history">The history of changes made through <see cref="QueryValue{T}.Value"/>.</param>
    public QueryValue<T> Bind<T>(string key, IQueryCodec<T> codec, T defaultValue, RendererInfo renderer, QueryHistory history = QueryHistory.Replace)
    {
        UseRenderer(renderer);
        return new(this, key, codec, defaultValue, history);
    }

    /// <summary>Binds a record whose properties are query keys. See <see cref="QueryGroup{TRecord}"/>.</summary>
    /// <param name="defaultValue">The value when the URL holds none of the keys; each property left out of the URL when equal to its value here.</param>
    /// <param name="owner">A name for the claiming component, used in the message when two groups use the same key.</param>
    /// <param name="renderer">The <c>RendererInfo</c> of the component: nothing is written unless it is interactive.</param>
    /// <param name="prefix">The prefix of the keys (<c>users</c> gives <c>users.q</c>), or null for none.</param>
    /// <param name="history">The history of changes made through <see cref="QueryGroup{TRecord}.Set(TRecord)"/>.</param>
    public QueryGroup<TRecord> Group<TRecord>(TRecord defaultValue, string owner, RendererInfo renderer, string? prefix = null, QueryHistory history = QueryHistory.Replace) where TRecord : class
    {
        UseRenderer(renderer);
        return new(this, defaultValue, owner, prefix, history);
    }

    /// <summary>Stops following the URL. The scope does it at its end.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _subscriptions = [];
            UnregisterChanging();
        }
        _navigation.LocationChanged -= OnLocationChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    // The effective value is what the URL holds with the writes of the current batch on top.
    private string? Effective(string key)
    {
        EnsureCurrent();
        return _pending.TryGetValue(key, out var pending) ? pending : QueryPairs.First(_current!, key);
    }

    private void EnsureCurrent()
    {
        if (_current is not null) return;
        _current = QueryPairs.Parse(_navigation.Uri);
        _lastPath = new Uri(_navigation.Uri).AbsolutePath;
    }

    private void RegisterChanging()
    {
        if (_changing is not null) return;
        try
        {
            _changing = _navigation.RegisterLocationChangingHandler(OnLocationChanging);
        }
        catch (NotSupportedException)
        {
            // A navigation manager without location changing support: the write then survives a navigation started in the same handler.
        }
    }

    private void UnregisterChanging()
    {
        var registration = _changing;
        _changing = null;
        registration?.Dispose();
    }

    // A navigation to another page started while writes wait: they were meant for the page being left, and flushing them afterwards would
    // send the user back to it (the URI of the navigation manager can still be the old one on the server).
    private ValueTask OnLocationChanging(LocationChangingContext context)
    {
        var target = new Uri(_navigation.ToAbsoluteUri(context.TargetLocation).AbsoluteUri).AbsolutePath;
        lock (_gate)
        {
            if (!_disposed && _pending.Count > 0 && target != new Uri(_navigation.Uri).AbsolutePath)
            {
                _pending.Clear();
                _pendingPush = false;
            }
        }
        return ValueTask.CompletedTask;
    }

    private async Task FlushAfterYieldAsync()
    {
        await Task.Yield();
        Flush();
    }

    private void Flush()
    {
        string target;
        bool push;
        Inflight entry;
        lock (_gate)
        {
            _flushScheduled = false;
            UnregisterChanging();
            if (_disposed || _pending.Count == 0 || _interactive == false)
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
            _inflight.RemoveAll(e => Expired(e, now));
            entry = new Inflight(++_nextInflightId, QueryPairs.Canonical(next), now);
            _inflight.Add(entry);
            target = QueryPairs.WithoutQuery(_navigation.Uri) + QueryPairs.ToQuery(next) + QueryPairs.Fragment(_navigation.Uri);
        }
        try
        {
            // The event this raises is ours: OnLocationChanged finds it in _inflight.
            _navigation.NavigateTo(target, new NavigationOptions { ReplaceHistoryEntry = !push });
        }
        catch (Exception e) when (e is NavigationException or ObjectDisposedException)
        {
            // The request or the circuit ended, or a renderer turned the call into a redirect: the URL did not change.
            Rollback(entry);
            return;
        }
        _ = RollbackAfterDelayAsync(entry);
    }

    private bool Expired(Inflight entry, long now) => now - entry.At > EchoLifetime.TotalMilliseconds;

    // A NavigationLock can cancel the navigation, and then no event comes: the state shown must come back to what the URL holds.
    private async Task RollbackAfterDelayAsync(Inflight entry)
    {
        try
        {
            await Task.Delay(RollbackDelay, _lifetime.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        Rollback(entry);
    }

    private void Rollback(Inflight entry)
    {
        List<string> changed;
        lock (_gate)
        {
            // Only a navigation that is still the newest one and has not come back: older ones are covered by the newer one.
            if (_disposed || _current is null || _inflight.Count == 0 || _inflight[^1].Id != entry.Id) return;
            _inflight.RemoveAt(_inflight.Count - 1);
            var actual = QueryPairs.Parse(_navigation.Uri);
            if (QueryPairs.Canonical(actual) == QueryPairs.Canonical(_current)) return;
            changed = QueryPairs.ChangedKeys(_current, actual);
            foreach (var key in changed) _pending.Remove(key);
            _current = actual;
        }
        if (changed.Count > 0) Notify(changed);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        List<string> changed;
        lock (_gate)
        {
            var path = new Uri(e.Location).AbsolutePath;
            if (_lastPath is not null && path != _lastPath) _generation++;
            _lastPath = path;
            if (_disposed || _current is null) return;
            var parsed = QueryPairs.Parse(e.Location);
            var canonical = QueryPairs.Canonical(parsed);
            var now = Environment.TickCount64;
            _inflight.RemoveAll(entry => Expired(entry, now));
            var at = _inflight.FindIndex(entry => entry.Canonical == canonical);
            if (at >= 0)
            {
                // The echo of a navigation of ours, perhaps an older one while newer writes are already in: those are the state, not this.
                var newest = at == _inflight.Count - 1;
                _inflight.RemoveRange(0, at + 1);
                if (!newest || canonical == QueryPairs.Canonical(_current)) return;
                // The newest echo of a navigation that was rolled back before it arrived: the URL says what it says.
            }
            else if (canonical == QueryPairs.Canonical(_current))
            {
                return;
            }
            else
            {
                // A navigation we did not start: the URL wins.
                _inflight.Clear();
            }
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

    internal void Release(QueryClaim claim)
    {
        lock (_gate)
        {
            claim.Released = true;
            _claims.Remove(claim);
        }
    }

    private sealed class Subscription(QueryState owner, string prefix, Action<IReadOnlyList<string>> handler) : IDisposable
    {
        public Action<IReadOnlyList<string>> Handler { get; } = handler;

        public bool Matches(string key) => KeyMatches(prefix, key);

        public void Dispose() => owner.Unsubscribe(this);
    }
}

/// <summary>A claim on a prefix of the query keys, or on the keys of a group. Dispose it to release them.</summary>
public sealed class QueryClaim : IDisposable
{
    private readonly QueryState _state;

    internal QueryClaim(QueryState state, string prefix, string owner, bool wholePrefix)
    {
        _state = state;
        Prefix = prefix;
        Owner = owner;
        WholePrefix = wholePrefix;
    }

    /// <summary>The claimed prefix.</summary>
    public string Prefix { get; }

    /// <summary>The name of the claiming component.</summary>
    public string Owner { get; }

    internal bool WholePrefix { get; }

    internal HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

    internal long Generation { get; set; }

    internal volatile bool Released;

    internal volatile Exception? Conflict;

    /// <summary>Throws when this claim turned out to overlap one that is still alive once the render that created it was over.</summary>
    /// <exception cref="InvalidOperationException">The conflict, naming both owners.</exception>
    public void ThrowIfConflicted()
    {
        if (Conflict is { } conflict) throw new InvalidOperationException(conflict.Message);
    }

    internal void AddKey(string key) => _state.AddClaimKey(this, key);

    /// <summary>Releases the claim.</summary>
    public void Dispose() => _state.Release(this);

    // Whole prefixes overlap when they are the same; keys overlap when they are shared; a whole prefix overlaps the keys it matches.
    internal static bool Overlap(QueryClaim claim, string? key, QueryClaim other, out string? sharedKey)
    {
        sharedKey = null;
        if (claim.WholePrefix && other.WholePrefix) return claim.Prefix == other.Prefix;
        if (claim.WholePrefix)
        {
            sharedKey = other.Keys.FirstOrDefault(k => QueryState.KeyMatches(claim.Prefix, k));
            return sharedKey is not null;
        }
        var keys = key is null ? claim.Keys : [key];
        if (other.WholePrefix)
        {
            sharedKey = keys.FirstOrDefault(k => QueryState.KeyMatches(other.Prefix, k));
            return sharedKey is not null;
        }
        sharedKey = keys.FirstOrDefault(other.Keys.Contains);
        return sharedKey is not null;
    }
}
