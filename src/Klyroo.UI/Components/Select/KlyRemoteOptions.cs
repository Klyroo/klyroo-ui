using Klyroo.UI.Internal;

namespace Klyroo.UI;

/// <summary>Where a <see cref="KlyRemoteOptions"/> stands: nothing asked yet, a request on its way, a page listed, or a failure.</summary>
public enum KlyRemoteStatus
{
    /// <summary>Nothing was asked yet (the list has never been opened).</summary>
    Idle,

    /// <summary>The first page of a search is on its way; the options of the previous search, if any, are still listed.</summary>
    Loading,

    /// <summary>A page is listed. <see cref="KlyRemoteOptions.HasMore"/> says whether another one exists.</summary>
    Loaded,

    /// <summary>The next page is on its way.</summary>
    LoadingMore,

    /// <summary>The last request failed; <see cref="KlyRemoteOptions.RetryAsync"/> repeats it.</summary>
    Error,

    /// <summary>The term is shorter than <see cref="KlyRemoteOptions.MinLength"/>: nothing is asked and nothing is listed.</summary>
    TooShort,
}

/// <summary>
/// Loads options from a <see cref="KlyOptionsProvider"/> one page at a time: the pages listed so far, the search term, the cancellation of stale requests and the error state.
/// It is the engine behind <see cref="KlySelectMenu{TValue}"/> and <see cref="KlyInputMenu{TValue}"/> when they have an <c>OptionsProvider</c>, public so that components of other Klyroo libraries
/// (such as filter editors) list remote options the same way. A plain class with no rendering: <c>changed</c> is called after every transition, possibly on another thread, so a component calls
/// <c>InvokeAsync(StateHasChanged)</c>. Dispose it with the component: it cancels the request in flight.
/// </summary>
public sealed class KlyRemoteOptions : IDisposable
{
    /// <summary>The page size when none is set: 25 options.</summary>
    public const int DefaultPageSize = 25;

    private readonly Action _changed;
    private readonly List<IKlySelectItem> _items = [];
    private readonly List<KlySelectItem> _texts = [];
    private readonly HashSet<string> _listed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IKlySelectItem> _known = new(StringComparer.Ordinal);
    private readonly HashSet<string> _attempted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failedResolve = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetime = new();
    // The resolutions of one source: cancelled and replaced by Reset, so a resolve that started for the old source stops, and is ignored if it finishes anyway.
    private CancellationTokenSource _resolving;
    private CancellationTokenSource? _request;
    private int _version;
    private int _received;
    private int _pageSize = DefaultPageSize;
    private string? _cursor;
    private bool _failedMore;
    private bool _disposed;

    /// <summary>Creates the engine.</summary>
    /// <param name="provider">Loads one page of options for a search.</param>
    /// <param name="changed">Called after every transition (a request starts, a page arrives, a failure); it may run on any thread.</param>
    public KlyRemoteOptions(KlyOptionsProvider provider, Action changed)
    {
        Provider = provider;
        _changed = changed;
        _resolving = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
    }

    /// <summary>The provider used for the next request. A parent that renders again with an equal one (a lambda that captures) keeps the state of the list.</summary>
    public KlyOptionsProvider Provider { get; set; }

    /// <summary>The number of options asked per page (at least 1).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Max(1, value);
    }

    /// <summary>Terms shorter than this (and not empty) are not searched. An empty term always lists the unfiltered options.</summary>
    public int MinLength { get; set; }

    /// <summary>Options given to the component as <c>Items</c>: not listed, but their labels are known.</summary>
    internal IReadOnlyList<IKlySelectItem> StaticItems { get; set; } = [];

    /// <summary>The options of the pages loaded so far, in the order of the source. The value of each is the text of the value the provider gave (a string as it is, a number or a date in the invariant culture, an enum by name), which is how the engine tells options apart. The list is live: read it on the thread that renders, and copy it to keep it.</summary>
    public IReadOnlyList<KlySelectItem> Items => _texts;

    /// <summary>The options as the provider returned them, with their typed values: what a menu bound to an int, an enum or a Guid lists and reads its choice from.</summary>
    internal IReadOnlyList<IKlySelectItem> Options => _items;

    /// <summary>Where the engine stands.</summary>
    public KlyRemoteStatus Status { get; private set; }

    /// <summary>The trimmed term of the current (or last) search.</summary>
    public string Term { get; private set; } = "";

    /// <summary>Whether the source has another page after the last one listed (<see cref="LoadMoreAsync"/> asks for it).</summary>
    public bool HasMore { get; private set; }

    /// <summary>Changes each time the listed options are replaced (not when a page is added): the browser goes back to the top and re-highlights.</summary>
    internal int Generation { get; private set; }

    /// <summary>Changes each time <see cref="Reset"/> starts the source over: the browser forgets what it kept (labels, the term it reported).</summary>
    internal int Epoch { get; private set; }

    /// <summary>A page is on its way: <see cref="Status"/> is <see cref="KlyRemoteStatus.Loading"/> or <see cref="KlyRemoteStatus.LoadingMore"/>.</summary>
    public bool Busy => Status is KlyRemoteStatus.Loading or KlyRemoteStatus.LoadingMore;

    /// <summary>Searches for a term (what the field reports). The same term again changes nothing, except after an error, where it asks again.</summary>
    public Task SearchAsync(string? raw)
    {
        if (_disposed) return Task.CompletedTask;
        var term = (raw ?? "").Trim();
        if (Status is not (KlyRemoteStatus.Idle or KlyRemoteStatus.Error) && term == Term) return Task.CompletedTask;
        Term = term;
        if (term.Length > 0 && term.Length < MinLength)
        {
            StopRequest();
            Replace();
            Status = KlyRemoteStatus.TooShort;
            _changed();
            return Task.CompletedTask;
        }
        return LoadAsync(more: false);
    }

    /// <summary>
    /// Starts over, as when the source changed (a new <c>OptionsKey</c>): the request in flight is dropped, the pages, the term, the cursor and the resolved
    /// labels are forgotten, and the next search (the next opening of the list) loads the first page again.
    /// </summary>
    internal void Reset()
    {
        if (_disposed) return;
        StopRequest();
        Replace();
        _known.Clear();
        _attempted.Clear();
        _failedResolve.Clear();
        Term = "";
        _failedMore = false;
        Status = KlyRemoteStatus.Idle;
        Epoch++;
        // The resolve of the old source in flight is cancelled; its answer, if it comes, is dropped (it checks the epoch).
        var old = _resolving;
        _resolving = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        old.Cancel();
        old.Dispose();
        _changed();
    }

    /// <summary>Loads the next page. Nothing happens while a request runs, after an error (use <see cref="RetryAsync"/>) or at the end.</summary>
    public Task LoadMoreAsync() => !_disposed && HasMore && Status == KlyRemoteStatus.Loaded ? LoadAsync(more: true) : Task.CompletedTask;

    /// <summary>Repeats the request that failed.</summary>
    public Task RetryAsync() => !_disposed && Status == KlyRemoteStatus.Error ? LoadAsync(_failedMore) : Task.CompletedTask;

    private void StopRequest()
    {
        _version++;
        var old = _request;
        _request = null;
        old?.Cancel();
        old?.Dispose();
    }

    private void Replace()
    {
        _items.Clear();
        _texts.Clear();
        _listed.Clear();
        _received = 0;
        _cursor = null;
        HasMore = false;
        Generation++;
    }

    private async Task LoadAsync(bool more)
    {
        StopRequest();
        var version = _version;
        var cts = _request = new CancellationTokenSource();
        var request = more
            ? new KlyOptionsRequest(Term, _received, PageSize, _cursor)
            : new KlyOptionsRequest(Term, 0, PageSize, null);
        Status = more ? KlyRemoteStatus.LoadingMore : KlyRemoteStatus.Loading;
        _changed();

        KlyOptionsResult? result;
        try
        {
            result = await Provider(request, cts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            // An answer that a newer search already replaced is of no interest, failed or not.
            if (version != _version || _disposed) return;
            if (!more) Replace();
            _failedMore = more;
            Status = KlyRemoteStatus.Error;
            _changed();
            return;
        }

        if (version != _version || _disposed) return;
        if (!more) Replace();
        foreach (var item in result?.Items ?? [])
        {
            _received++;
            var key = OptionValue.Key(item.Value);
            if (!_listed.Add(key)) continue;
            _items.Add(item);
            _texts.Add(new KlySelectItem(key, item.Label) { Disabled = item.Disabled, Count = item.Count, Icon = item.Icon, Group = item.Group });
        }
        _cursor = result?.NextCursor;
        HasMore = result is not null && (result.HasMore || result.NextCursor is not null);
        _failedMore = false;
        Status = KlyRemoteStatus.Loaded;
        _changed();
    }

    /// <summary>Whether the label of a value is known: listed now, remembered, given as an item, or resolved.</summary>
    internal bool Knows(string value) =>
        _known.ContainsKey(value) || _listed.Contains(value) || StaticItems.Any(i => OptionValue.Key(i.Value) == value);

    /// <summary>The option, listed now, remembered or given as an item, whose value has that text.</summary>
    internal IKlySelectItem? Find(string key) =>
        _items.FirstOrDefault(i => OptionValue.Key(i.Value) == key)
        ?? (_known.TryGetValue(key, out var known) ? known : null)
        ?? StaticItems.FirstOrDefault(i => OptionValue.Key(i.Value) == key);

    /// <summary>The label of a value, or the value itself when nothing knows it (the raw value is what an unresolved value shows).</summary>
    internal string LabelOf(string value) =>
        _known.TryGetValue(value, out var known) ? known.Label
        : _items.FirstOrDefault(i => OptionValue.Key(i.Value) == value)?.Label
        ?? StaticItems.FirstOrDefault(i => OptionValue.Key(i.Value) == value)?.Label
        ?? value;

    /// <summary>Keeps the options of the selected values that are listed now, so their labels survive the next search replacing the list.</summary>
    internal void RememberSelected(IEnumerable<string>? values)
    {
        if (values is null) return;
        foreach (var value in values)
        {
            if (_known.ContainsKey(value)) continue;
            var item = _items.FirstOrDefault(i => OptionValue.Key(i.Value) == value);
            if (item is not null) _known[value] = item;
        }
    }

    /// <summary>The labels of the selected values that are remembered or given, as value, label, value, label... for the browser.</summary>
    internal IEnumerable<string> KnownLabels(IEnumerable<string>? values)
    {
        foreach (var value in values ?? [])
        {
            if (_known.TryGetValue(value, out var item))
            {
                yield return value;
                yield return item.Label;
            }
            else if (StaticItems.FirstOrDefault(i => OptionValue.Key(i.Value) == value) is { } given)
            {
                yield return value;
                yield return given.Label;
            }
        }
    }

    /// <summary>Asks the resolver once for each selected value nobody knows. A failure leaves the raw values; they are asked again when <paramref name="retryFailed"/> is set (the list opened).</summary>
    internal async Task ResolveAsync(IReadOnlyList<string>? values, KlySelectedOptionsResolver resolver, bool retryFailed = false)
    {
        if (_disposed || values is not { Count: > 0 }) return;
        if (retryFailed)
        {
            // The list opened again: the values whose resolution threw get one more try.
            foreach (var value in _failedResolve) _attempted.Remove(value);
            _failedResolve.Clear();
        }
        var missing = values.Where(v => !Knows(v) && _attempted.Add(v)).ToList();
        if (missing.Count == 0) return;
        // What the answer is for: a Reset while it is on its way means another source, whose labels this one must not enter.
        var epoch = Epoch;
        IReadOnlyList<IKlySelectItem>? found;
        try
        {
            found = await resolver(missing, _resolving.Token).ConfigureAwait(true);
        }
        catch (Exception)
        {
            // A failure of the old source (often the cancellation Reset asked for) says nothing about the new one.
            if (_disposed || epoch != Epoch) return;
            foreach (var value in missing) _failedResolve.Add(value);
            return;
        }
        if (_disposed || epoch != Epoch || found is null) return;
        foreach (var item in found)
        {
            // Matched by the text of the value, so a resolver of numbers or dates can return the items it has.
            var key = OptionValue.Key(item.Value);
            if (missing.Contains(key)) _known[key] = item;
        }
        _changed();
    }

    /// <summary>Cancels the request in flight and the resolutions, and ignores any answer that still arrives. The engine does nothing afterwards.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _resolving.Dispose();
        _lifetime.Dispose();
        _request?.Cancel();
        _request?.Dispose();
        _request = null;
    }
}
