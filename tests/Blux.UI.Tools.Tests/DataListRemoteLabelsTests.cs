using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Blux.UI.Tools.Tests.DataListRemoteOptionsTests;

namespace Blux.UI.Tools.Tests;

/// <summary>A logger factory that keeps what was logged.</summary>
internal sealed class CaptureLoggerFactory : ILoggerFactory
{
    public List<(LogLevel Level, string Category, string Message, Exception? Exception)> Entries { get; } = [];

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName) => new Capture(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Capture(CaptureLoggerFactory owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (owner.Entries) owner.Entries.Add((logLevel, category, formatter(state, exception), exception));
        }
    }
}

/// <summary>
/// The labels of the values of remote option filters: asked for while the rows load, handed from the prerender to the interactive render (and counted in the persisted size), logged when the resolver
/// fails, and the counts of the options a remote editor loaded, asked for by value. Interactive and static.
/// </summary>
public partial class DataListRemoteLabelsTests : BluxToolsTestContext
{
    private readonly CaptureLoggerFactory _logs = new();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    private static string Text(IElement element) => Spaces().Replace(element.TextContent, " ").Trim();

    public DataListRemoteLabelsTests() : base(interactive: true, persistentState: true, configureServices: services => services.AddSingleton<ILoggerFactory>(new CaptureLoggerFactory()))
    {
        _logs = (CaptureLoggerFactory)Services.GetRequiredService<ILoggerFactory>();
    }

    // What the list logged (the renderer logs its own debug lines too).
    private List<(LogLevel Level, string Category, string Message, Exception? Exception)> Logged()
    {
        lock (_logs.Entries) return [.. _logs.Entries.Where(e => e.Category == "Blux.UI.Tools.DataList")];
    }

    private static DataListState Owner(params string[] values) =>
        FilterData.Schema.DefaultState with { Filters = [new FilterRule("owner", FilterOperator.IsAnyOf, new FilterValue.List(values))] };

    private static string[] Tags(IRenderedComponent<DataListRemoteHarness> cut) =>
        [.. cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text)];

    // ---- before the rows

    [Fact]
    public async Task The_labels_are_asked_for_while_the_rows_load()
    {
        var source = new Source();
        var gate = new TaskCompletionSource<DataListResult<Ticket>>();
        DataListProvider<Ticket> provider = (_, _) => new ValueTask<DataListResult<Ticket>>(gate.Task);

        var cut = Render<DataListRemoteHarness>(p => p.Add(h => h.Schema, Schema(source)).Add(h => h.Provider, provider).Add(h => h.State, Owner("ann")));

        // The provider has not answered: the resolver was asked, and the tag shows the label before any row is there.
        cut.WaitForAssertion(() => Assert.Equal(["Owner is Ann Lee"], Tags(cut)));
        Assert.Equal([["ann"]], source.Resolved);
        Assert.Empty(cut.FindAll("tbody tr[data-slot=row]"));
        gate.SetResult(new DataListResult<Ticket>([], 0));
        await Task.CompletedTask;
    }

    // ---- from the prerender

    private static readonly Dictionary<string, Dictionary<string, string>> Labels = new() { ["owner"] = new() { ["ann"] = "Ann (from the prerender)" } };

    private void Persist(DataListState state, Dictionary<string, Dictionary<string, string>>? labels)
    {
        var key = "bx-dl:" + typeof(Ticket).FullName + ":f";
        var persisted = Persisted!;
        var result = new DataListResult<Ticket>([.. FilterData.All.Take(10)], 40);
        persisted.Persist(key + ":state", JsonSerializer.Serialize(state, DataListJsonContext.Default.DataListState));
        persisted.Persist(key + ":loaded", JsonSerializer.Serialize(state, DataListJsonContext.Default.DataListState));
        persisted.Persist(key + ":result", JsonSerializer.Serialize(result, TicketJson.Default.DataListResultTicket));
        persisted.Persist(key + ":zone", "");
        if (labels is not null) persisted.Persist(key + ":labels", JsonSerializer.Serialize(labels, DataListJsonContext.Default.DictionaryStringDictionaryStringString));
    }

    [Fact]
    public void The_labels_a_prerender_resolved_show_at_once_and_the_resolver_is_not_asked_again()
    {
        var source = new Source();
        Persist(Owner("ann"), Labels);
        var requests = new List<DataListRequest>();
        DataListProvider<Ticket> provider = (request, _) =>
        {
            requests.Add(request);
            return ValueTask.FromResult(new DataListResult<Ticket>([], 0));
        };

        var cut = Render<DataListRemoteHarness>(p => p.Add(h => h.Schema, Schema(source)).Add(h => h.Provider, provider).Add(h => h.State, Owner("ann")));

        // Rendered once, with the label of the prerender: no raw value, no call.
        Assert.Equal(["Owner is Ann (from the prerender)"], Tags(cut));
        Assert.Empty(source.Resolved);
        Assert.Empty(requests);
    }

    [Fact]
    public void Labels_that_cover_only_some_values_leave_the_others_to_the_resolver()
    {
        var source = new Source();
        Persist(Owner("ann", "bob"), Labels);

        var cut = Render<DataListRemoteHarness>(p => p.Add(h => h.Schema, Schema(source)).Add(h => h.Provider, null).Add(h => h.State, Owner("ann", "bob")));

        cut.WaitForAssertion(() => Assert.Equal(["Owner is Ann (from the prerender), Bob Ray"], Tags(cut)));
        Assert.Equal([["bob"]], source.Resolved);
    }

    [Fact]
    public void The_labels_round_trip_as_json_and_are_part_of_what_the_budget_measures()
    {
        var text = JsonSerializer.Serialize(Labels, DataListJsonContext.Default.DictionaryStringDictionaryStringString);

        var back = JsonSerializer.Deserialize(text, DataListJsonContext.Default.DictionaryStringDictionaryStringString);

        Assert.Equal("Ann (from the prerender)", back!["owner"]["ann"]);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(text) > 20);
    }

    // ---- a resolver that fails

    [Fact]
    public void A_resolver_that_throws_is_logged_once_and_the_value_shows_as_it_is()
    {
        var source = new Source { FailResolve = true };

        var cut = Render<DataListRemoteHarness>(p => p.Add(h => h.Schema, Schema(source)).Add(h => h.Provider, null).Add(h => h.State, Owner("ann")));

        cut.WaitForAssertion(() => Assert.Single(Logged()));
        Assert.Equal(["Owner is ann"], Tags(cut));
        var entry = Logged().Single();
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("owner", entry.Message);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    public async Task A_resolver_that_fails_after_the_list_went_away_is_neither_logged_nor_left_unobserved()
    {
        var gate = new TaskCompletionSource();
        var source = new Source { BeforeResolve = _ => gate.Task, FailResolve = true };
        var cut = Render<DataListRemoteHarness>(p => p.Add(h => h.Schema, Schema(source)).Add(h => h.Provider, null).Add(h => h.State, Owner("ann")));
        cut.WaitForState(() => source.Resolved.Count == 1);
        var unobserved = new List<Exception>();
        void Watch(object? sender, UnobservedTaskExceptionEventArgs e) => unobserved.AddRange(e.Exception.InnerExceptions.Where(x => x.Message.Contains("the resolver is down")));
        TaskScheduler.UnobservedTaskException += Watch;
        try
        {
            await DisposeComponentsAsync();

            gate.SetResult();
            await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= Watch;
        }

        Assert.Empty(Logged());
        Assert.Empty(unobserved);
    }

    // ---- the counts of the options that are listed

    [Fact]
    public async Task The_counts_of_the_listed_options_are_asked_for_by_value_with_a_one_row_page()
    {
        var source = new Source();
        var inner = DataListController<Ticket>.CreateProvider(FilterData.Schema, FilterData.All, null);
        var requests = new List<DataListRequest>();
        DataListProvider<Ticket> provider = (request, token) =>
        {
            lock (requests) requests.Add(request);
            return inner(request, token);
        };
        var cut = Render<DataListRemoteHarness>(p => p.Add(h => h.Schema, Schema(source, facets: true, pageSize: 5)).Add(h => h.Provider, provider).Add(h => h.State, Owner("ann")));
        cut.WaitForAssertion(() => Assert.Equal(["Owner is Ann Lee"], Tags(cut)));

        await cut.Find("li[data-filter=owner] bx-popover").TriggerEventAsync("onbxtoggle", new BxToggleEventArgs { Open = true, Sequence = 1 });

        cut.WaitForAssertion(() =>
        {
            var counted = requests.Where(r => r.Facets.Any(f => f.Values is not null)).ToList();
            Assert.Single(counted);
            Assert.Equal(new DataListPaging.Offset(0, 1), counted[0].Paging);
            Assert.Equal(["ann", "bob", "cyd", "dee", "eve"], counted[0].Facets.Single().Values!.Order());
        });
        // The editor shows them, dee and eve with 0 (no row), and the page did not ask again for what it had counted.
        cut.WaitForAssertion(() => Assert.Equal(["13", "14", "13", "0", "0"], cut.FindAll("li[data-filter=owner] [data-slot=filter-options] > li[data-value] [data-slot=filter-option-count]").Select(c => Text(c))));
        Assert.Single(requests, r => r.Facets.Any(f => f.Values is not null));
    }
}
