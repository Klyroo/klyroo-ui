using System.Text.Json;
using System.Text.Json.Serialization;
using Blux.UI.Tools.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

[JsonSerializable(typeof(DataListResult<Ticket>))]
public sealed partial class TicketJson : JsonSerializerContext
{
}

/// <summary>The counts of a prerendered result: they travel with it (and count in its size), the interactive render shows them without a load, and paging does not ask for them again.</summary>
public class DataListFacetPersistTests : BluxToolsTestContext
{
    private static readonly DataListState State = FilterData.Schema.DefaultState with { Filters = [new FilterRule("owner", FilterOperator.IsAnyOf, new FilterValue.List(["ann"]))] };

    // Counts no database would give (bob 99), so the test knows they came from the persisted result.
    private static readonly DataListFacetResult[] Facets =
    [
        new("owner", [new DataListFacetOption("bob", 99), new DataListFacetOption("ann", 13)], missing: 4, total: 2),
    ];

    public DataListFacetPersistTests() : base(interactive: true, persistentState: true)
    {
    }

    private void Persist(DataListResult<Ticket> result)
    {
        var key = "bx-dl:" + typeof(Ticket).FullName + ":f";
        var persisted = Persisted!;
        persisted.Persist(key + ":state", JsonSerializer.Serialize(State, DataListJsonContext.Default.DataListState));
        persisted.Persist(key + ":loaded", JsonSerializer.Serialize(State, DataListJsonContext.Default.DataListState));
        persisted.Persist(key + ":result", JsonSerializer.Serialize(result, TicketJson.Default.DataListResultTicket));
        persisted.Persist(key + ":zone", "");
    }

    [Fact]
    public void The_result_round_trips_with_its_counts()
    {
        var result = new DataListResult<Ticket>([FilterData.All[0]], 40, null, Facets);

        var text = JsonSerializer.Serialize(result, TicketJson.Default.DataListResultTicket);
        var back = JsonSerializer.Deserialize(text, TicketJson.Default.DataListResultTicket);

        Assert.Equal(result, back);
        Assert.Contains("\"Facets\"", text);
        // The counts are part of what the budget measures: the same result without them is shorter.
        Assert.True(text.Length > JsonSerializer.Serialize(result with { Facets = null }, TicketJson.Default.DataListResultTicket).Length);
    }

    [Fact]
    public void An_adopted_result_shows_its_counts_without_a_load_and_paging_keeps_them()
    {
        Persist(new DataListResult<Ticket>([.. FilterData.All.Take(10)], 40, null, Facets));
        var requests = new List<DataListRequest>();
        var inner = DataListController<Ticket>.CreateProvider(FilterData.Schema, FilterData.All, null);
        DataListProvider<Ticket> provider = (request, token) =>
        {
            requests.Add(request);
            return inner(request, token);
        };

        var cut = Render<DataListFacetPersistHarness>(p => p.Add(h => h.Provider, provider).Add(h => h.State, State));

        cut.WaitForAssertion(() => Assert.Contains("Ticket 01", cut.Markup));
        Assert.Empty(requests);
        string Count(string value) => cut.Find($"li[data-filter=owner] [data-slot=filter-options] > li[data-value={value}] [data-slot=filter-option-count]").TextContent.Trim(',', ' ');
        Assert.Equal("99", Count("bob"));
        Assert.Equal("4", cut.Find("li[data-filter=owner] [data-slot=filter-include-empty-row] [data-slot=filter-option-count]").TextContent.Trim(',', ' '));

        cut.Find("button[aria-label='Page 2']").Click();

        // The second page of the tickets of ann (a multiple of three each).
        cut.WaitForAssertion(() => Assert.Contains("Ticket 33", cut.Markup));
        // The page is asked for without the counts, and the persisted ones stay.
        Assert.Single(requests);
        Assert.Empty(requests[0].Facets);
        Assert.Equal("99", Count("bob"));
    }
}
