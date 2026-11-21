using System.Text.Json;
using System.Text.Json.Serialization;
using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

public sealed record Row(int Id, string Name);

/// <summary>An application context: the generic result type is declared by the app, the wire records come from the library context.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DataListResult<Row>))]
public sealed partial class AppJsonContext : JsonSerializerContext;

public class JsonTests
{
    private static DataListRequest FullRequest() => new(
        new DataListPaging.Cursor("abc", 25, true),
        [new DataListSort("name"), new DataListSort("age", true)],
        DataListFilterGroup.And(
            Rule("name", Op.Contains, Text("a\"b\\cé😀")),
            Rule("age", Op.Between, Range(Num(1.25m), null)),
            Rule("active", Op.IsNot, new FilterValue.Bool(false)),
            Rule("hired", Op.After, Day("2024-02-29")),
            Rule("lastLogin", Op.Between, Range(At("2024-03-01T10:00:00+02:00"), At("2024-03-02T00:00:00Z"))),
            Rule("status", Op.IsAnyOf, List("Active", "x,y"), includeEmpty: true),
            Rule("email", Op.IsEmpty),
            DataListFilterGroup.Or(Rule("id", Op.Is, Num(79228162514264337593543950335m)), Rule("name", Op.Is, Text("")))),
        new DataListSearch("find me"),
        [new DataListFacetRequest("status", FacetMode.IncludeSelf, 10, 5, "act", true), new DataListFacetRequest("active")]);

    [Fact]
    public void A_request_round_trips()
    {
        var request = FullRequest();
        var json = JsonSerializer.Serialize(request, DataListJsonContext.Default.DataListRequest);
        var back = JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListRequest);
        Assert.Equal(request, back);
    }

    [Fact]
    public void Each_paging_shape_round_trips_with_its_discriminator()
    {
        foreach (var (paging, tag) in new (DataListPaging, string)[]
        {
            (new DataListPaging.Offset(40, 20), "offset"),
            (new DataListPaging.Cursor(null, 10), "cursor"),
            (new DataListPaging.None(), "none"),
        })
        {
            var json = JsonSerializer.Serialize(paging, DataListJsonContext.Default.DataListPaging);
            Assert.Contains($"\"type\":\"{tag}\"", json);
            Assert.Equal(paging, JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListPaging));
        }
        var offset = JsonSerializer.Serialize(new DataListPaging.Offset(40, 20), DataListJsonContext.Default.DataListPaging);
        Assert.DoesNotContain("number", offset);
    }

    [Fact]
    public void Each_value_shape_round_trips()
    {
        FilterValue[] values =
        [
            new FilterValue.Text("t"), new FilterValue.Number(-3.50m), new FilterValue.Bool(true), new FilterValue.Date(new DateOnly(2024, 2, 29)),
            new FilterValue.Moment(DateTimeOffset.Parse("2024-03-01T10:00:00.1234567+05:30")), new FilterValue.List(["a", "b"]), new FilterValue.List([]),
            new FilterValue.Range(new FilterValue.Number(1), new FilterValue.Number(2)), new FilterValue.Range(null, new FilterValue.Date(new DateOnly(2024, 1, 1))),
        ];
        foreach (var value in values)
        {
            var json = JsonSerializer.Serialize(value, DataListJsonContext.Default.FilterValue);
            Assert.Equal(value, JsonSerializer.Deserialize(json, DataListJsonContext.Default.FilterValue));
        }
    }

    [Fact]
    public void The_wire_format_is_camel_case_with_string_enums_and_a_type_discriminator()
    {
        var request = new DataListRequest(
            new DataListPaging.Offset(0, 10), [new DataListSort("name", true)],
            DataListFilterGroup.And(Rule("status", Op.IsAnyOf, List("a"), true)), new DataListSearch("q"));
        var json = JsonSerializer.Serialize(request, DataListJsonContext.Default.DataListRequest);
        Assert.Equal(
            """{"paging":{"type":"offset","skip":0,"take":10},"sort":[{"key":"name","descending":true}],"filter":{"combinator":"And","children":[{"type":"rule","key":"status","operator":"IsAnyOf","value":{"type":"list","values":["a"]},"includeEmpty":true}]},"search":{"term":"q"},"facets":[]}""",
            json);
    }

    [Fact]
    public void Missing_lists_and_unknown_properties_are_tolerated()
    {
        var back = JsonSerializer.Deserialize("""{"paging":{"type":"none"},"extra":1}""", DataListJsonContext.Default.DataListRequest)!;
        Assert.Empty(back.Sort);
        Assert.Empty(back.Facets);
        Assert.Empty(back.Filter.Children);
        Assert.Null(back.Search);
        Assert.IsType<DataListPaging.None>(back.Paging);
        Assert.Equal(DataListRequest.Empty, JsonSerializer.Deserialize("{}", DataListJsonContext.Default.DataListRequest));
    }

    [Fact]
    public void An_unknown_discriminator_or_enum_value_is_rejected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("""{"type":"bogus"}""", DataListJsonContext.Default.FilterValue));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("""{"type":"rule","key":"a","operator":"Nope"}""", DataListJsonContext.Default.DataListFilterNode));
    }

    [Fact]
    public void Facet_results_and_state_round_trip()
    {
        var facet = new DataListFacetResult("status", [new DataListFacetOption("Active", 3)], 2, 7);
        var json = JsonSerializer.Serialize(facet, DataListJsonContext.Default.DataListFacetResult);
        Assert.Equal(facet, JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListFacetResult));

        var state = new DataListState("q", [Rule("age", Op.Gt, Num(1))], [new DataListSort("name")], 3, 25, DataListLayout.Grid, "cur");
        var stateJson = JsonSerializer.Serialize(state, DataListJsonContext.Default.DataListState);
        Assert.Equal(state, JsonSerializer.Deserialize(stateJson, DataListJsonContext.Default.DataListState));
    }

    [Fact]
    public void A_generic_result_serializes_through_the_app_context()
    {
        var result = new DataListResult<Row>(
            [new Row(1, "a"), new Row(2, "b")], 10, "next", [new DataListFacetResult("s", [new DataListFacetOption("x", 1)])]);
        var json = JsonSerializer.Serialize(result, AppJsonContext.Default.DataListResultRow);
        var back = JsonSerializer.Deserialize(json, AppJsonContext.Default.DataListResultRow)!;
        Assert.Equal(result.Items, back.Items);
        Assert.Equal(10, back.TotalCount);
        Assert.Equal("next", back.NextCursor);
        Assert.Equal(result.Facets, back.Facets);
        var empty = JsonSerializer.Serialize(new DataListResult<Row>([]), AppJsonContext.Default.DataListResultRow);
        Assert.Equal("""{"items":[]}""", empty);
    }

    [Fact]
    public void Contexts_work_without_reflection_defaults()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = DataListJsonContext.Default };
        var request = FullRequest();
        var json = JsonSerializer.Serialize(request, options);
        Assert.Equal(request, JsonSerializer.Deserialize<DataListRequest>(json, options));
    }
}
