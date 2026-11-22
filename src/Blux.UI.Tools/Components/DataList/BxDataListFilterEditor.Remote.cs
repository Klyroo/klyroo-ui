using Blux.UI.Internal;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

// The options of a filter that come from a remote source (DataListFilter.WithRemoteOptions), and the "show more" of a long list of options. The paging, the search, the cancellation of stale requests and the
// error state are RemoteOptions of Blux.UI, the logic behind BxSelectMenu with an OptionsProvider; this part only adapts the filter's delegates to it and keeps its own state across renders.
public partial class BxDataListFilterEditor<TItem>
{
    // The pause of the typing before a search is sent to the remote source.
    private const int RemoteDebounce = 250;

    private RemoteOptions? _remote;
    private bool _remoteStarted;
    private bool _remoteSearchAgain;
    private bool _showAll;
    private string? _scrollerId;

    /// <summary>
    /// Whether the editor is on screen. A filter with remote options loads its first page the first time this is true (a tag's popover passes whether it is open), so a page does not call
    /// every remote source on load. True by default: the editor of the filter panel and of the add filter menu load when they render.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    private bool IsRemote => _filter?.RemoteOptions is not null;

    private string ScrollerId => _scrollerId ??= _id + "-options";

    // Only an interactive renderer can search and page: a static editor lists what the server knows (the selected values and the counts).
    private void SyncRemote()
    {
        if (_filter?.RemoteOptions is not { } definition || !Context.IsInteractive)
        {
            _remote?.Dispose();
            _remote = null;
            _remoteStarted = false;
            return;
        }
        _remote ??= new RemoteOptions(Adapt(definition), () => _ = InvokeAsync(StateHasChanged));
        // A schema built again with equal delegates (a lambda that captures) keeps the pages and the search: only the provider that the next request calls changes.
        _remote.Provider = Adapt(definition);
        _remote.PageSize = definition.PageSize;
        _remote.MinLength = definition.MinLength;
    }

    private static BxOptionsProvider Adapt(DataListRemoteOptions definition) => async (request, cancellationToken) =>
    {
        var page = await definition.Provider(new DataListOptionsRequest(request.Search, request.Skip, request.Take, request.Cursor), cancellationToken).ConfigureAwait(true);
        return new BxOptionsResult([.. page.Items.Select(o => new BxSelectItem(o.Value, o.Label))], page.HasMore, page.NextCursor);
    };

    // The first page, the first time the editor is on screen; the next search of an editor that was reset (the popover closed and opened again) starts from nothing typed.
    private Task StartRemoteAsync()
    {
        if (_remote is null || !Visible || !Context.IsInteractive) return Task.CompletedTask;
        if (!_remoteStarted)
        {
            _remoteStarted = true;
            _remoteSearchAgain = false;
            return _remote.SearchAsync(_search);
        }
        if (!_remoteSearchAgain) return Task.CompletedTask;
        _remoteSearchAgain = false;
        return _remote.SearchAsync(_search);
    }

    private async Task OnRemoteSearch(string? value)
    {
        _search = value ?? "";
        if (_remote is not null) await _remote.SearchAsync(_search);
    }

    private void OnRemoteToggle(FilterChoice choice, bool on)
    {
        // The label of a picked option is remembered at once, so the tag writes it and not the raw value.
        if (on) Context.LearnLabels(Key, [new DataListFilterOption(choice.Value, choice.Label)]);
        OnToggle(choice.Value, on);
    }

    // The options of the pages loaded so far, with their counts when the filter has them, after the selected values that no page holds (their label is the one the resolver gave).
    private IReadOnlyList<FilterChoice> RemoteChoices()
    {
        var remote = _remote!;
        var choices = new List<FilterChoice>();
        var listed = new HashSet<string>(remote.Items.Select(i => i.Value), StringComparer.Ordinal);
        foreach (var value in _selected)
        {
            if (!listed.Contains(value)) choices.Add(new FilterChoice(value, Context.OptionLabelOf(Key, value), Context.CountOf(Key, value)));
        }
        foreach (var item in remote.Items) choices.Add(new FilterChoice(item.Value, item.Label, Context.CountOf(Key, item.Value)));
        return choices;
    }

    private void ShowAllOptions() => _showAll = true;

    // The first options of a list with a limit, and how many are held back. A selected option is never held back.
    private (IReadOnlyList<FilterChoice> Shown, int Hidden) Limit(IReadOnlyList<FilterChoice> choices)
    {
        if (_filter!.OptionLimit is not { } limit || _showAll || choices.Count <= limit) return (choices, 0);
        var shown = new List<FilterChoice>(limit);
        for (var i = 0; i < choices.Count; i++)
        {
            if (i < limit || _selected.Contains(choices[i].Value)) shown.Add(choices[i]);
        }
        return (shown, choices.Count - shown.Count);
    }

    /// <summary>Stops a search or a page that is on its way.</summary>
    public override void Dispose()
    {
        _remote?.Dispose();
        _remote = null;
        base.Dispose();
    }
}
