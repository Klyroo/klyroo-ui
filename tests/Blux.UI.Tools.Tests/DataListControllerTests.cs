using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

public sealed record Person(int Id, string Name, int Age, string City);

/// <summary>The controller of a data list without a renderer: sources, requests, stale requests, kept rows, errors and out-of-range pages.</summary>
public class DataListControllerTests
{
    public static readonly Person[] People =
        [.. Enumerable.Range(1, 45).Select(i => new Person(i, $"Person {i:00}", 20 + i % 30, i % 2 == 0 ? "Paris" : "Lyon"))];

    internal static DataListSchema<Person> Schema { get; } = DataListSchema.For<Person>(s => s
        .RowKey(p => p.Id)
        .Field(p => p.Name, f => f.Sortable())
        .Field(p => p.Age, f => f.Sortable())
        .Field(p => p.City)
        .DefaultSort(p => p.Name)
        .DefaultPageSize(10));

    private static DataListController<Person> InMemory() => new(Schema, DataListController<Person>.CreateProvider(Schema, People, null));

    private static DataListState State(int page = 1, int size = 10) => Schema.DefaultState with { Page = page, PageSize = size };

    /// <summary>A provider that waits for the test to finish each call, recording the requests and the tokens it got.</summary>
    private sealed class Gate
    {
        public List<(DataListRequest Request, CancellationToken Token, TaskCompletionSource<DataListResult<Person>> Done)> Calls { get; } = [];

        public DataListProvider<Person> Provider => (request, token) =>
        {
            var done = new TaskCompletionSource<DataListResult<Person>>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (Calls) Calls.Add((request, token, done));
            token.Register(() => done.TrySetCanceled(token));
            return new ValueTask<DataListResult<Person>>(done.Task);
        };

        public static DataListResult<Person> Page(int total, params int[] ids) =>
            new([.. People.Where(p => ids.Contains(p.Id))], total);
    }

    [Fact]
    public async Task An_in_memory_source_is_sorted_and_paged_by_the_schema()
    {
        using var controller = InMemory();

        await controller.LoadAsync(State(page: 2));

        Assert.False(controller.HasError);
        Assert.Equal(Enumerable.Range(11, 10).Select(i => $"Person {i:00}"), controller.Rows.Select(p => p.Name));
        Assert.Equal(45, controller.Result!.TotalCount);
        Assert.Equal(2, controller.LoadedState!.Page);
    }

    [Fact]
    public async Task The_request_carries_the_default_sort_the_page_and_the_time_zone()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider) { TimeZone = "Europe/Paris" };

        var load = controller.LoadAsync(State(page: 3, size: 20));
        var request = gate.Calls.Single().Request;
        gate.Calls[0].Done.SetResult(Gate.Page(45, 41));
        await load;

        Assert.Equal(new DataListPaging.Offset(40, 20), request.Paging);
        Assert.Equal([new DataListSort("name")], request.Sort);
        Assert.Equal("Europe/Paris", request.TimeZone);
    }

    [Fact]
    public async Task A_request_is_sanitized_by_the_schema()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider);
        var state = State() with { Sort = [new DataListSort("city"), new DataListSort("nope"), new DataListSort("age", true)], PageSize = 100_000 };

        var load = controller.LoadAsync(state);
        var request = gate.Calls.Single().Request;
        gate.Calls[0].Done.SetResult(Gate.Page(0));
        await load;

        // city is not sortable and nope is unknown; the page size is the largest the schema allows.
        Assert.Equal([new DataListSort("age", true)], request.Sort);
        Assert.Equal(new DataListPaging.Offset(0, 500), request.Paging);
    }

    [Fact]
    public async Task The_first_load_is_initial_and_then_the_flags_settle()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider);
        Assert.True(controller.IsInitial);

        var load = controller.LoadAsync(State());
        Assert.True(controller.IsInitial);
        Assert.True(controller.IsLoading);
        Assert.False(controller.IsReplacing);
        Assert.False(controller.IsRefreshing);

        gate.Calls[0].Done.SetResult(Gate.Page(45, 1, 2));
        await load;

        Assert.False(controller.IsInitial);
        Assert.False(controller.IsLoading);
        Assert.Equal([1, 2], controller.Rows.Select(p => p.Id));
    }

    [Fact]
    public async Task The_previous_rows_stay_while_another_request_loads()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider);
        var first = controller.LoadAsync(State());
        gate.Calls[0].Done.SetResult(Gate.Page(45, 1, 2, 3));
        await first;

        var second = controller.LoadAsync(State(page: 2));

        Assert.True(controller.IsReplacing);
        Assert.False(controller.IsRefreshing);
        Assert.False(controller.IsInitial);
        Assert.Equal([1, 2, 3], controller.Rows.Select(p => p.Id));
        Assert.Equal(1, controller.LoadedState!.Page);

        gate.Calls[1].Done.SetResult(Gate.Page(45, 11, 12));
        await second;

        Assert.False(controller.IsReplacing);
        Assert.Equal([11, 12], controller.Rows.Select(p => p.Id));
        Assert.Equal(2, controller.LoadedState!.Page);
    }

    [Fact]
    public async Task A_newer_request_cancels_the_stale_one_and_its_late_result_is_ignored()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider);

        var stale = controller.LoadAsync(State(page: 1));
        var newer = controller.LoadAsync(State(page: 2));

        Assert.True(gate.Calls[0].Token.IsCancellationRequested);
        Assert.False(gate.Calls[1].Token.IsCancellationRequested);

        gate.Calls[1].Done.SetResult(Gate.Page(45, 11));
        await newer;
        // A provider that ignores its token and answers late must not overwrite the newer rows.
        gate.Calls[0].Done.TrySetResult(Gate.Page(45, 1));
        await stale;

        Assert.Equal([11], controller.Rows.Select(p => p.Id));
        Assert.Equal(2, controller.LoadedState!.Page);
        Assert.False(controller.IsLoading);
        Assert.False(controller.HasError);
    }

    [Fact]
    public async Task A_cancelled_stale_request_does_not_end_the_loading_of_the_newer_one()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider);

        var stale = controller.LoadAsync(State(page: 1));
        var newer = controller.LoadAsync(State(page: 2));
        await stale;

        Assert.True(controller.IsLoading);
        Assert.False(controller.HasError);

        gate.Calls[1].Done.SetResult(Gate.Page(45, 11));
        await newer;
        Assert.False(controller.IsLoading);
    }

    [Fact]
    public async Task A_failure_keeps_the_previous_rows_and_a_refresh_after_it_recovers()
    {
        var fail = false;
        var provider = Schema.InMemoryProvider(People);
        using var controller = new DataListController<Person>(Schema, (request, token) =>
            fail ? throw new InvalidOperationException("backend down") : provider(request, token));
        await controller.LoadAsync(State());
        var rows = controller.Rows;

        fail = true;
        await controller.LoadAsync(State(page: 2));

        Assert.True(controller.HasError);
        Assert.Equal("backend down", controller.Error!.Message);
        Assert.False(controller.IsLoading);
        Assert.Same(rows, controller.Rows);

        fail = false;
        await controller.RefreshAsync();

        Assert.False(controller.HasError);
        Assert.Equal(2, controller.LoadedState!.Page);
        Assert.Equal("Person 11", controller.Rows[0].Name);
    }

    [Fact]
    public async Task A_failure_of_the_first_load_is_not_initial_and_retry_loads_it()
    {
        var calls = 0;
        var provider = Schema.InMemoryProvider(People);
        using var controller = new DataListController<Person>(Schema, (request, token) =>
            ++calls == 1 ? throw new InvalidOperationException("first call fails") : provider(request, token));

        await controller.LoadAsync(State());

        Assert.True(controller.HasError);
        Assert.False(controller.IsInitial);
        Assert.Empty(controller.Rows);

        await controller.RefreshAsync();

        Assert.False(controller.HasError);
        Assert.Equal(10, controller.Rows.Count);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task A_refresh_of_what_is_on_screen_is_a_refresh_and_not_a_replacement()
    {
        var gate = new Gate();
        using var controller = new DataListController<Person>(Schema, gate.Provider);
        var first = controller.LoadAsync(State());
        gate.Calls[0].Done.SetResult(Gate.Page(45, 1));
        await first;

        var refresh = controller.RefreshAsync();

        Assert.True(controller.IsRefreshing);
        Assert.False(controller.IsReplacing);
        Assert.Equal(gate.Calls[0].Request, gate.Calls[1].Request);

        gate.Calls[1].Done.SetResult(Gate.Page(45, 1, 2));
        await refresh;
        Assert.False(controller.IsRefreshing);
        Assert.Equal([1, 2], controller.Rows.Select(p => p.Id));
    }

    [Theory]
    [InlineData(99, 5)]
    [InlineData(6, 5)]
    [InlineData(5, 5)]
    public async Task A_page_past_the_last_one_loads_the_last_page_in_the_same_load(int page, int loaded)
    {
        var requests = new List<DataListRequest>();
        var provider = Schema.InMemoryProvider(People);
        using var controller = new DataListController<Person>(Schema, (request, token) =>
        {
            requests.Add(request);
            return provider(request, token);
        });

        await controller.LoadAsync(State(page: page));

        Assert.Equal(loaded, controller.LoadedState!.Page);
        Assert.Equal(["Person 41", "Person 42", "Person 43", "Person 44", "Person 45"], controller.Rows.Select(p => p.Name));
        Assert.Equal(page > loaded ? 2 : 1, requests.Count);
        // What was asked for stays what was asked for: the component compares the two to fix the URL.
        Assert.Equal(page, controller.RequestedState!.Page);
    }

    [Fact]
    public async Task An_empty_list_is_not_a_page_past_the_end()
    {
        var provider = Schema.InMemoryProvider([]);
        using var controller = new DataListController<Person>(Schema, provider);

        await controller.LoadAsync(State(page: 4));

        // Page 4 of nothing is page 1 of nothing: the last page of an empty list is the first.
        Assert.Empty(controller.Rows);
        Assert.Equal(1, controller.LoadedState!.Page);
    }

    [Fact]
    public void Normalize_brings_page_and_page_size_into_range()
    {
        using var controller = InMemory();

        var state = controller.Normalize(State() with { Page = -3, PageSize = 0 });
        Assert.Equal(1, state.Page);
        Assert.Equal(10, state.PageSize);
        Assert.Equal(500, controller.Normalize(State(size: 9_999)).PageSize);
        var fine = State(page: 2, size: 25);
        Assert.Same(fine, controller.Normalize(fine));
    }

    [Fact]
    public void A_list_takes_one_source()
    {
        Assert.NotNull(DataListController<Person>.CreateProvider(Schema, People, null));
        Assert.Throws<InvalidOperationException>(() => DataListController<Person>.CreateProvider(Schema, null, null));
        Assert.Throws<InvalidOperationException>(() => DataListController<Person>.CreateProvider(Schema, People, Schema.InMemoryProvider(People)));
        var own = Schema.InMemoryProvider(People);
        Assert.Same(own, DataListController<Person>.CreateProvider(Schema, null, own));
    }

    [Fact]
    public async Task An_adopted_result_needs_no_provider_call_and_the_next_load_replaces_it()
    {
        var calls = 0;
        var provider = Schema.InMemoryProvider(People);
        using var controller = new DataListController<Person>(Schema, (request, token) =>
        {
            calls++;
            return provider(request, token);
        });
        var state = State();
        var request = controller.BuildRequest(state);
        var result = await provider(request, CancellationToken.None);

        controller.Adopt(state, state, request, result);

        Assert.Equal(0, calls);
        Assert.False(controller.IsInitial);
        Assert.Equal(10, controller.Rows.Count);
        await controller.LoadAsync(State(page: 2));
        Assert.Equal(1, calls);
        Assert.Equal(2, controller.LoadedState!.Page);
    }

    [Fact]
    public async Task Dispose_cancels_the_running_request()
    {
        var gate = new Gate();
        var controller = new DataListController<Person>(Schema, gate.Provider);
        var load = controller.LoadAsync(State());

        controller.Dispose();
        await load;

        Assert.True(gate.Calls[0].Token.IsCancellationRequested);
        Assert.False(controller.HasError);
    }

    [Fact]
    public async Task Changed_is_raised_when_a_load_starts_and_when_it_ends()
    {
        using var controller = InMemory();
        var raised = new List<(bool Loading, int Rows)>();
        controller.Changed += () => raised.Add((controller.IsLoading, controller.Rows.Count));

        await controller.LoadAsync(State());

        Assert.Equal([(true, 0), (false, 10)], raised);
    }
}
