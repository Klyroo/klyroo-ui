using System.Text.Json;
using static Blux.UI.Tools.Query.Tests.TestData;

namespace Blux.UI.Tools.Query.Tests;

public class FacetCapAndWireTests
{
    private sealed record Item(int Id, string Group);

    private static DataListSchema<Item> SchemaWith(int? cap) => DataListSchema.For<Item>(s =>
    {
        s.RowKey(e => e.Id).Field(e => e.Group, f => f.Filter(DataListFilter.Option()));
        if (cap is { } c) s.MaxFacetBuckets(c);
    });

    // groups: g1 x4, g2 x3, g3 x3, g4 x2, g5 x1, g6 x1
    private static readonly Item[] Items =
    [
        .. Enumerable.Range(0, 4).Select(i => new Item(i, "g1")),
        .. Enumerable.Range(10, 3).Select(i => new Item(i, "g2")),
        .. Enumerable.Range(20, 3).Select(i => new Item(i, "g3")),
        .. Enumerable.Range(30, 2).Select(i => new Item(i, "g4")),
        new Item(40, "g5"),
        new Item(41, "g6"),
    ];

    private static DataListFacetResult Facet(int? cap, DataListFacetRequest facet) =>
        SchemaWith(cap).ExecuteInMemory(Items, new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.Empty, null, [facet])).Facets!.Single();

    [Fact]
    public void The_default_cap_is_one_thousand_and_a_small_facet_is_not_truncated()
    {
        Assert.Equal(1000, SchemaWith(null).MaxFacetBuckets);
        var facet = Facet(null, new DataListFacetRequest("group"));
        Assert.False(facet.Truncated);
        Assert.Equal(6, facet.Total);
    }

    [Fact]
    public void A_facet_keeps_the_most_counted_options_ties_by_value_and_says_it_was_cut()
    {
        var facet = Facet(3, new DataListFacetRequest("group"));
        Assert.True(facet.Truncated);
        Assert.Equal(["g1", "g2", "g3"], facet.Options.Select(o => o.Value));
        Assert.Equal(3, facet.Total);
    }

    [Fact]
    public void A_facet_exactly_at_the_cap_is_not_truncated()
    {
        var facet = Facet(6, new DataListFacetRequest("group"));
        Assert.False(facet.Truncated);
        Assert.Equal(6, facet.Options.Count);
    }

    [Fact]
    public void Search_and_the_window_apply_among_the_kept_options()
    {
        // g5 and g6 are cut, so a search for them finds nothing
        var search = Facet(4, new DataListFacetRequest("group", Search: "g5"));
        Assert.Empty(search.Options);
        Assert.True(search.Truncated);

        var window = Facet(4, new DataListFacetRequest("group", Skip: 1, Limit: 2));
        Assert.Equal(["g2", "g3"], window.Options.Select(o => o.Value));
        Assert.Equal(4, window.Total);
    }

    [Fact]
    public void The_cap_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SchemaWith(0));
    }

    [Fact]
    public void Truncated_takes_part_in_equality_and_the_wire_form()
    {
        var a = new DataListFacetResult("k", [new DataListFacetOption("x", 1)], null, 1);
        Assert.NotEqual(a, a with { Truncated = true });
        var json = JsonSerializer.Serialize(a with { Truncated = true }, DataListJsonContext.Default.DataListFacetResult);
        Assert.Contains("\"truncated\":true", json);
        Assert.True(JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListFacetResult)!.Truncated);
    }

    [Fact]
    public void A_boolean_filter_value_is_written_with_the_boolean_discriminator_and_read_back()
    {
        var request = new DataListRequest(
            DataListPaging.None.Instance,
            [],
            DataListFilterGroup.And(Rule("active", FilterOperator.Is, new FilterValue.Boolean(true))));
        var json = JsonSerializer.Serialize(request, DataListJsonContext.Default.DataListRequest);
        Assert.Contains("\"type\":\"boolean\"", json);
        Assert.Equal(request, JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListRequest));
    }
}
