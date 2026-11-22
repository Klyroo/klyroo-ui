using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The facets of a data list at the level of the controller: which filters ask for counts (and in which mode), and that the counts are asked for when the filters or the search change,
/// kept while the user pages or sorts, and asked for again on a refresh.
/// </summary>
public class DataListFacetControllerTests
{
    private static readonly DataListSchema<Ticket> Schema = DataListSchema.For<Ticket>(s => s
        .RowKey(t => t.Id)
        .Search(t => t.Title)
        .Field(t => t.Id, f => f.Sortable())
        .Field(t => t.Title, f => f.Sortable().Filter(DataListFilter.Text()))
        .Field(t => t.Status, f => f.Filter(DataListFilter.Option("open", "paused", "closed", "archived").WithFacets()))
        .Field(t => t.Active, f => f.Filter(DataListFilter.Boolean().WithFacets().WithFacetMode(FacetMode.IncludeSelf)))
        .Field(t => t.Owner, f => f.Filter(DataListFilter.Option()))
        .DefaultSort(t => t.Id)
        .DefaultPageSize(10));

    private sealed class Recorder
    {
        private readonly DataListProvider<Ticket> _inner = DataListController<Ticket>.CreateProvider(Schema, FilterData.All, null);

        public List<DataListRequest> Requests { get; } = [];

        public DataListProvider<Ticket> Provider => (request, token) =>
        {
            Requests.Add(request);
            return _inner(request, token);
        };
    }

    private static DataListState State => Schema.DefaultState;

    [Fact]
    public void Only_option_and_boolean_filters_with_counts_ask_for_them_in_their_own_mode()
    {
        using var controller = new DataListController<Ticket>(Schema, new Recorder().Provider);

        var facets = controller.BuildRequest(State).Facets;

        Assert.Equal(
            [new DataListFacetRequest("status", FacetMode.ExcludeSelf, IncludeMissing: true), new DataListFacetRequest("active", FacetMode.IncludeSelf, IncludeMissing: false)],
            facets);
    }

    [Fact]
    public async Task Counts_are_asked_for_when_the_filters_or_the_search_change_and_kept_while_paging_and_sorting()
    {
        var recorder = new Recorder();
        using var controller = new DataListController<Ticket>(Schema, recorder.Provider);

        await controller.LoadAsync(State);
        var first = controller.Result!.Facets;
        Assert.Equal(2, recorder.Requests[0].Facets.Count);
        Assert.NotNull(first);

        await controller.LoadAsync(State with { Page = 2 });
        Assert.Empty(recorder.Requests[1].Facets);
        Assert.Same(first, controller.Result!.Facets);
        Assert.Equal(10, controller.Rows.Count);
        Assert.Equal(11, controller.Rows[0].Id);

        await controller.LoadAsync(State with { Page = 2, Sort = [new DataListSort("title", true)] });
        Assert.Empty(recorder.Requests[2].Facets);
        Assert.Same(first, controller.Result!.Facets);

        await controller.LoadAsync(State with { Filters = [new FilterRule("status", FilterOperator.IsAnyOf, new FilterValue.List(["open"]))] });
        Assert.Equal(2, recorder.Requests[3].Facets.Count);
        var filtered = controller.Result!.Facets!;
        Assert.NotSame(first, filtered);
        // The facet of status leaves its own rule out: every status is still counted.
        Assert.Equal(4, filtered.Single(f => f.Key == "status").Options.Count);
        // The facet of active keeps the rule of status: only the open tickets are counted.
        Assert.Equal(8, filtered.Single(f => f.Key == "active").Options.Sum(o => o.Count));

        await controller.LoadAsync(State with { Filters = [new FilterRule("status", FilterOperator.IsAnyOf, new FilterValue.List(["open"]))], Page = 2, PageSize = 4 });
        Assert.Empty(recorder.Requests[4].Facets);
        Assert.Same(filtered, controller.Result!.Facets);

        await controller.LoadAsync(State with { Search = "ann" });
        Assert.Equal(2, recorder.Requests[5].Facets.Count);
        Assert.NotSame(filtered, controller.Result!.Facets);
    }

    [Fact]
    public async Task A_refresh_counts_again()
    {
        var recorder = new Recorder();
        using var controller = new DataListController<Ticket>(Schema, recorder.Provider);
        await controller.LoadAsync(State);
        await controller.LoadAsync(State with { Page = 2 });
        Assert.Empty(recorder.Requests[1].Facets);

        await controller.RefreshAsync();

        Assert.Equal(2, recorder.Requests[2].Facets.Count);
    }

    [Fact]
    public async Task Counts_are_asked_for_again_after_the_rows_of_the_list_changed()
    {
        var recorder = new Recorder();
        using var controller = new DataListController<Ticket>(Schema, recorder.Provider);
        await controller.LoadAsync(State);

        // What BxDataList does when it is given other items or another query.
        controller.DropFacets();
        await controller.LoadAsync(State);
        await controller.LoadAsync(State with { Page = 2 });

        Assert.Equal(2, recorder.Requests[1].Facets.Count);
        Assert.Empty(recorder.Requests[2].Facets);
    }

    [Fact]
    public async Task A_page_past_the_end_goes_back_to_the_last_page_with_the_counts_it_needs()
    {
        var recorder = new Recorder();
        using var controller = new DataListController<Ticket>(Schema, recorder.Provider);
        await controller.LoadAsync(State);

        // Page 9 does not exist (40 rows, 10 per page): the load goes to page 4 in the same call, and the facets stay those of the filters.
        await controller.LoadAsync(State with { Page = 9 });

        Assert.Equal(4, controller.LoadedState!.Page);
        Assert.NotNull(controller.Result!.Facets);
        Assert.All(recorder.Requests.Skip(1), r => Assert.Empty(r.Facets));
    }

    [Fact]
    public async Task A_page_past_the_end_after_a_filter_change_counts_once_and_reuses_the_first_answer()
    {
        var recorder = new Recorder();
        using var controller = new DataListController<Ticket>(Schema, recorder.Provider);
        await controller.LoadAsync(State);

        // Page 4 exists without the filter and not with it: the first call counts for the new filter, and the call that replaces it (the same filters and search) reuses those counts.
        await controller.LoadAsync(State with { Page = 4, Filters = [new FilterRule("status", FilterOperator.IsAnyOf, new FilterValue.List(["open"]))] });

        Assert.Equal(1, controller.LoadedState!.Page);
        Assert.Equal(3, recorder.Requests.Count);
        Assert.Equal(2, recorder.Requests[1].Facets.Count);
        Assert.Empty(recorder.Requests[2].Facets);
        Assert.Equal(8, controller.Result!.Facets!.Single(f => f.Key == "active").Options.Sum(o => o.Count));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_provider_that_answers_a_request_without_counts_with_an_empty_or_null_list_keeps_the_counts_on_screen(bool empty)
    {
        var inner = DataListController<Ticket>.CreateProvider(Schema, FilterData.All, null);
        DataListProvider<Ticket> provider = async (request, token) =>
        {
            var result = await inner(request, token);
            // The contract: Facets answer request.Facets. A provider that always puts a list there answers [] when it was asked for nothing.
            return request.Facets.Count == 0 ? result with { Facets = empty ? [] : null } : result;
        };
        using var controller = new DataListController<Ticket>(Schema, provider);
        await controller.LoadAsync(State);
        var counted = controller.Result!.Facets;

        await controller.LoadAsync(State with { Page = 2 });

        Assert.NotNull(counted);
        Assert.Same(counted, controller.Result!.Facets);
    }

    [Fact]
    public async Task The_counts_of_some_values_are_asked_with_a_one_row_page_of_the_state_on_screen()
    {
        var recorder = new Recorder();
        using var controller = new DataListController<Ticket>(Schema, recorder.Provider);
        var state = State with { Filters = [new FilterRule("active", FilterOperator.Is, new FilterValue.Boolean(true))], Page = 2 };
        await controller.LoadAsync(state);

        var facet = await controller.LoadFacetValuesAsync("status", ["open", "closed", "nope"], CancellationToken.None);

        var request = recorder.Requests.Last();
        Assert.Equal(new DataListPaging.Offset(0, 1), request.Paging);
        Assert.Equal([new DataListFacetRequest("status", FacetMode.ExcludeSelf, Values: ["open", "closed", "nope"])], request.Facets);
        Assert.Single(request.Filter.Children);
        // Ids that are multiples of three have an active ticket: of those, the open ones are 15 and 30, the closed ones 12 and 27, and nothing is "nope".
        Assert.Equal(["closed:2", "open:2"], facet!.Options.Select(o => $"{o.Value}:{o.Count}").Order());
    }

    [Fact]
    public async Task A_source_that_returns_no_counts_is_asked_every_time()
    {
        var requests = new List<DataListRequest>();
        DataListProvider<Ticket> plain = (request, _) =>
        {
            requests.Add(request);
            return ValueTask.FromResult(new DataListResult<Ticket>([.. FilterData.All.Take(10)], 40));
        };
        using var controller = new DataListController<Ticket>(Schema, plain);

        await controller.LoadAsync(State);
        await controller.LoadAsync(State with { Page = 2 });

        Assert.All(requests, r => Assert.Equal(2, r.Facets.Count));
        Assert.Null(controller.Result!.Facets);
    }

    [Fact]
    public async Task A_failed_load_keeps_the_counts_on_screen()
    {
        var calls = 0;
        var inner = DataListController<Ticket>.CreateProvider(Schema, FilterData.All, null);
        DataListProvider<Ticket> flaky = (request, token) => ++calls == 2 ? throw new InvalidOperationException("down") : inner(request, token);
        using var controller = new DataListController<Ticket>(Schema, flaky);
        await controller.LoadAsync(State);
        var facets = controller.Result!.Facets;

        await controller.LoadAsync(State with { Page = 2 });

        Assert.True(controller.HasError);
        Assert.Same(facets, controller.Result!.Facets);
    }
}
