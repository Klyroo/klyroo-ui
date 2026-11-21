using System.Text.Json;
using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

public sealed record Event(int Id, DateTimeOffset When, DateTime At, DateOnly On);

public class DateGranularityAndZoneTests
{
    private static DataListSchema<Event> SchemaFor(DateGranularity granularity) => DataListSchema.For<Event>(s => s
        .RowKey(e => e.Id)
        .Field(e => e.When, f => f.Filter(DataListFilter.Date(granularity)))
        .Field(e => e.At, f => f.Filter(DataListFilter.Date(granularity)))
        .Field(e => e.On, f => f.Filter(DataListFilter.Date())));

    private static Event E(int id, string utc) =>
        new(id, DateTimeOffset.Parse(utc), DateTime.SpecifyKind(DateTime.Parse(utc).ToUniversalTime(), DateTimeKind.Unspecified), DateOnly.FromDateTime(DateTime.Parse(utc).ToUniversalTime()));

    private static int[] Run(DataListSchema<Event> schema, Event[] rows, string key, Op op, FilterValue value, string? zone = null)
    {
        var request = new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.And(Rule(key, op, value)), null, null, zone);
        return [.. schema.ExecuteInMemory(rows, request).Items.Select(e => e.Id)];
    }

    // ---- granularity: the whole hour, minute or second

    private static readonly Event[] Clock =
    [
        E(1, "2024-03-01T09:59:59.9999999Z"),
        E(2, "2024-03-01T10:00:00Z"),
        E(3, "2024-03-01T10:00:59.9999999Z"),
        E(4, "2024-03-01T10:01:00Z"),
        E(5, "2024-03-01T10:59:59.9999999Z"),
        E(6, "2024-03-01T11:00:00Z"),
    ];

    [Theory]
    [InlineData(DateGranularity.Hour, Op.Is, "2024-03-01T10:30:15Z", new[] { 2, 3, 4, 5 })]
    [InlineData(DateGranularity.Hour, Op.IsNot, "2024-03-01T10:30:15Z", new[] { 1, 6 })]
    [InlineData(DateGranularity.Hour, Op.Before, "2024-03-01T10:30:15Z", new[] { 1 })]
    [InlineData(DateGranularity.Hour, Op.After, "2024-03-01T10:30:15Z", new[] { 6 })]
    [InlineData(DateGranularity.Minute, Op.Is, "2024-03-01T10:00:30Z", new[] { 2, 3 })]
    [InlineData(DateGranularity.Minute, Op.Before, "2024-03-01T10:00:30Z", new[] { 1 })]
    [InlineData(DateGranularity.Minute, Op.After, "2024-03-01T10:00:30Z", new[] { 4, 5, 6 })]
    [InlineData(DateGranularity.Second, Op.Is, "2024-03-01T10:00:59Z", new[] { 3 })]
    [InlineData(DateGranularity.Second, Op.Is, "2024-03-01T10:00:00.5Z", new[] { 2 })]
    [InlineData(DateGranularity.Second, Op.After, "2024-03-01T10:00:00Z", new[] { 3, 4, 5, 6 })]
    public void A_unit_matches_as_a_whole(DateGranularity granularity, Op op, string moment, int[] expected)
    {
        var schema = SchemaFor(granularity);
        Assert.Equal(expected, Run(schema, Clock, "when", op, At(moment)));
        Assert.Equal(expected, Run(schema, Clock, "at", op, At(moment)));
    }

    [Fact]
    public void Between_covers_both_end_units()
    {
        var schema = SchemaFor(DateGranularity.Minute);
        var range = Range(At("2024-03-01T10:00:20Z"), At("2024-03-01T10:00:40Z"));
        Assert.Equal([2, 3], Run(schema, Clock, "when", Op.Between, range));
        var wide = Range(At("2024-03-01T10:00:20Z"), At("2024-03-01T10:01:10Z"));
        Assert.Equal([2, 3, 4], Run(schema, Clock, "when", Op.Between, wide));
        Assert.Equal([4, 5, 6], Run(schema, Clock, "when", Op.Between, Range(At("2024-03-01T10:01:30Z"), null)));
        Assert.Equal([1, 2, 3], Run(schema, Clock, "when", Op.Between, Range(null, At("2024-03-01T10:00:01Z"))));
    }

    [Fact]
    public void A_half_hour_zone_gets_local_hours()
    {
        var schema = SchemaFor(DateGranularity.Hour);
        // 10:30Z is 16:00 in Kolkata (+5:30): the local hour 16:00-17:00 is 10:30Z-11:30Z
        var rows = new[] { E(1, "2024-03-01T10:29:59Z"), E(2, "2024-03-01T10:30:00Z"), E(3, "2024-03-01T11:29:59Z"), E(4, "2024-03-01T11:30:00Z") };
        Assert.Equal([2, 3], Run(schema, rows, "when", Op.Is, At("2024-03-01T10:45:00Z"), "Asia/Kolkata"));
    }

    // ---- days are the user's local days

    [Fact]
    public void A_day_is_the_local_day_of_the_request_zone()
    {
        var schema = SchemaFor(DateGranularity.Day);
        var rows = new[] { E(1, "2024-03-01T14:59:59Z"), E(2, "2024-03-01T15:00:00Z"), E(3, "2024-03-02T14:59:59Z"), E(4, "2024-03-02T15:00:00Z") };
        Assert.Equal([2, 3], Run(schema, rows, "when", Op.Is, Day("2024-03-02"), "Asia/Tokyo"));
        Assert.Equal([2, 3], Run(schema, rows, "at", Op.Is, Day("2024-03-02"), "Asia/Tokyo"));
        Assert.Equal([1, 4], Run(schema, rows, "when", Op.IsNot, Day("2024-03-02"), "Asia/Tokyo"));
        Assert.Equal([2, 3, 4], Run(schema, rows, "when", Op.After, Day("2024-03-01"), "Asia/Tokyo"));
        Assert.Equal([1], Run(schema, rows, "when", Op.Before, Day("2024-03-02"), "Asia/Tokyo"));
        Assert.Equal([2, 3, 4], Run(schema, rows, "when", Op.Between, Range(Day("2024-03-02"), Day("2024-03-03")), "Asia/Tokyo"));
    }

    [Fact]
    public void Without_a_zone_a_day_is_the_utc_day()
    {
        var schema = SchemaFor(DateGranularity.Day);
        var rows = new[] { E(1, "2024-03-01T23:59:59.9999999Z"), E(2, "2024-03-02T00:00:00Z"), E(3, "2024-03-02T23:59:59.9999999Z"), E(4, "2024-03-03T00:00:00Z") };
        Assert.Equal([2, 3], Run(schema, rows, "when", Op.Is, Day("2024-03-02")));
        Assert.Equal([2, 3], Run(schema, rows, "when", Op.Is, Day("2024-03-02"), "Not/AZone"));
    }

    [Fact]
    public void Daylight_saving_days_are_23_and_25_hours_long()
    {
        var schema = SchemaFor(DateGranularity.Day);
        // New York, 2024-03-10: clocks jump forward at 02:00, the local day is 05:00Z to 04:00Z the next day
        var spring = new[] { E(1, "2024-03-10T04:59:59Z"), E(2, "2024-03-10T05:00:00Z"), E(3, "2024-03-11T03:59:59Z"), E(4, "2024-03-11T04:00:00Z") };
        Assert.Equal([2, 3], Run(schema, spring, "when", Op.Is, Day("2024-03-10"), "America/New_York"));
        // 2024-11-03: clocks go back, the local day is 04:00Z to 05:00Z the next day
        var fall = new[] { E(1, "2024-11-03T03:59:59Z"), E(2, "2024-11-03T04:00:00Z"), E(3, "2024-11-04T04:59:59Z"), E(4, "2024-11-04T05:00:00Z") };
        Assert.Equal([2, 3], Run(schema, fall, "when", Op.Is, Day("2024-11-03"), "America/New_York"));
        Assert.Equal([1, 4], Run(schema, fall, "when", Op.IsNot, Day("2024-11-03"), "America/New_York"));
    }

    [Fact]
    public void A_midnight_that_does_not_exist_starts_the_day_at_the_first_valid_time()
    {
        var schema = SchemaFor(DateGranularity.Day);
        // Havana, 2024-03-10: 00:00 does not exist, the day starts at 01:00 (-04:00) = 05:00Z
        var rows = new[] { E(1, "2024-03-10T04:59:59Z"), E(2, "2024-03-10T05:00:00Z") };
        Assert.Equal([2], Run(schema, rows, "when", Op.Is, Day("2024-03-10"), "America/Havana"));
    }

    [Fact]
    public void Date_only_fields_do_not_depend_on_the_zone()
    {
        var schema = SchemaFor(DateGranularity.Day);
        var rows = new[] { E(1, "2024-03-01T23:30:00Z"), E(2, "2024-03-02T00:30:00Z") };
        Assert.Equal([2], Run(schema, rows, "on", Op.Is, Day("2024-03-02"), "Asia/Tokyo"));
        Assert.Equal([2], Run(schema, rows, "on", Op.Is, Day("2024-03-02"), "America/New_York"));
        Assert.Equal([1], Run(schema, rows, "on", Op.Before, Day("2024-03-02")));
    }

    [Fact]
    public void Date_time_fields_are_read_as_utc()
    {
        var schema = SchemaFor(DateGranularity.Day);
        var local = new Event(1, default, new DateTime(2024, 3, 1, 15, 0, 0, DateTimeKind.Local), default);
        var utc = new Event(2, default, new DateTime(2024, 3, 1, 15, 0, 0, DateTimeKind.Utc), default);
        var unspecified = new Event(3, default, new DateTime(2024, 3, 1, 14, 59, 59), default);
        // Tokyo's 2024-03-02 starts at 2024-03-01T15:00 UTC; the Kind of the stored value never changes the comparison
        Assert.Equal([1, 2], Run(schema, [local, utc, unspecified], "at", Op.Is, Day("2024-03-02"), "Asia/Tokyo"));
    }

    [Fact]
    public void The_zone_is_not_part_of_the_state_url_and_unknown_ids_are_cleared()
    {
        var clean = Schema.Sanitize(new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.Empty, null, null, "Nope/Zone"));
        Assert.Null(clean.TimeZone);
        Assert.Equal("Europe/Paris", Schema.Sanitize(new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.Empty, null, null, "Europe/Paris")).TimeZone);
        Assert.Null(Schema.DefaultState.ToRequest().TimeZone);
        Assert.Equal("Asia/Tokyo", Schema.DefaultState.ToRequest(timeZone: "Asia/Tokyo").TimeZone);
        Assert.Empty(DataListStateQuery.ToQuery(Schema.DefaultState, Schema));
    }
}

public class ExpressionShapeTests
{
    [Fact]
    public void Raw_expressions_have_no_ToLower_use_parameters_and_in_lists()
    {
        var filter = DataListFilterGroup.And(
            Rule("name", Op.Contains, Text("alice")),
            Rule("status", Op.IsAnyOf, List("Active", "Pending", "Archived")),
            Rule("age", Op.Gt, Num(30)));
        var text = FilterExpressions.Rows(Schema, filter, new DataListSearch("acme"), null, inMemory: false)!.ToString();
        Assert.DoesNotContain("ToLower", text);
        Assert.DoesNotContain("IndexOf", text);
        Assert.Contains(".Contains(", text);
        Assert.DoesNotContain("\"alice\"", text);
        Assert.DoesNotContain("\"Active\"", text);
        Assert.Contains("ValueBox", text);
        Assert.DoesNotContain("30", text.Replace("ValueBox", ""));
    }

    [Fact]
    public void In_memory_expressions_fold_case_and_accents()
    {
        var text = FilterExpressions.Rows(Schema, DataListFilterGroup.And(Rule("name", Op.Contains, Text("alice"))), null, null, inMemory: true)!.ToString();
        Assert.Contains("IndexOf", text);
        Assert.DoesNotContain("ToLower", text);
    }

    [Fact]
    public void A_list_of_one_is_an_equality_and_a_longer_one_a_single_Contains()
    {
        var one = FilterExpressions.Rows(Schema, DataListFilterGroup.And(Rule("status", Op.IsAnyOf, List("Active"))), null, null, inMemory: false)!.ToString();
        Assert.DoesNotContain("Contains", one);
        var many = FilterExpressions.Rows(Schema, DataListFilterGroup.And(Rule("id", Op.IsAnyOf, List("1", "2", "3", "4", "5", "6", "7"))), null, null, inMemory: false)!.ToString();
        Assert.Contains("Contains", many);
        Assert.DoesNotContain("OrElse", many);
        var ids = Schema.ExecuteInMemory(Employees, Request(DataListFilterGroup.And(Rule("id", Op.IsAnyOf, List("1", "2", "3", "4", "5", "6", "7"))))).Ids();
        Assert.Equal(6, ids.Length);
    }

    [Fact]
    public void Equal_numbers_with_different_scales_build_the_same_predicate()
    {
        var a = DataListFilterGroup.And(Rule("id", Op.Is, new FilterValue.Number(1.0m)));
        var b = DataListFilterGroup.And(Rule("id", Op.Is, new FilterValue.Number(1m)));
        Assert.Equal(a, b);
        Assert.Equal([1], Schema.ExecuteInMemory(Employees, Request(a)).Ids());
        Assert.Equal([1], Schema.ExecuteInMemory(Employees, Request(b)).Ids());
        Assert.Same(Schema.GetPredicate(a, null, null), Schema.GetPredicate(b, null, null));

        var text = DataListSchema.For<Employee>(s => s.Field(e => e.Name, f => f.Filter(DataListFilter.Option())));
        var rows = new[] { Employees[0] with { Name = "1" }, Employees[1] with { Name = "1.0" } };
        var viaScale = new FilterValue.Number(1.00m);
        Assert.Equal([1], text.ExecuteInMemory(rows, Request(DataListFilterGroup.And(Rule("name", Op.Is, viaScale)))).Ids());
    }

    [Fact]
    public void A_different_time_zone_is_a_different_cached_predicate()
    {
        var filter = DataListFilterGroup.And(Rule("lastLogin", Op.Is, Day("2024-03-02")));
        var utc = Schema.GetPredicate(filter, null, null, TimeZoneInfo.Utc);
        var tokyo = Schema.GetPredicate(filter, null, null, TimeZones.Resolve("Asia/Tokyo"));
        Assert.NotSame(utc, tokyo);
        Assert.Same(utc, Schema.GetPredicate(filter, null, null, TimeZoneInfo.Utc));
    }
}

/// <summary>Requests that arrive from the network are hostile: whatever the JSON says, sanitizing and running never throw.</summary>
public class HostileInputTests
{
    private static DataListRequest? Parse(string json) =>
        JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListRequest);

    private static void RunSafely(string json)
    {
        DataListRequest? request;
        try { request = Parse(json); }
        catch (JsonException) { return; }
        if (request is null) return;
        var clean = Schema.Sanitize(request);
        Schema.ExecuteInMemory(Employees, clean);
        Schema.ExecuteInMemory(Employees, request);
    }

    [Theory]
    [InlineData("""{"filter":{"children":[{"type":"rule"}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":null,"operator":"is"}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"","operator":"is","value":{"type":"text","value":"x"}}]}}""")]
    [InlineData("""{"filter":{"children":[null]}}""")]
    [InlineData("""{"filter":{"children":null}}""")]
    [InlineData("""{"filter":null}""")]
    [InlineData("""{"filter":{"children":[{"type":"group"}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"age","operator":"gt","value":null}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"age","operator":"between","value":{"type":"range"}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"age","operator":"between","value":{"type":"range","from":{"type":"range"}}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"status","operator":"isAnyOf","value":{"type":"list","values":[null,"Active",null]}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"status","operator":"isAnyOf","value":{"type":"list","values":null}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"name","operator":"contains","value":{"type":"text","value":null}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"hired","operator":"is","value":{"type":"date","value":"0001-01-01"}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"hired","operator":"after","value":{"type":"date","value":"9999-12-31"}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"lastLogin","operator":"is","value":{"type":"moment","value":"0001-01-01T00:00:00+00:00"}}]}}""")]
    [InlineData("""{"filter":{"children":[{"type":"rule","key":"lastLogin","operator":"is","value":{"type":"moment","value":"9999-12-31T23:59:59.9999999+00:00"}}]}}""")]
    [InlineData("""{"sort":[null,{"key":null},{"key":"name"}],"facets":[null,{"key":null},{"key":"status","limit":-5,"skip":-9}]}""")]
    [InlineData("""{"paging":{"type":"offset","skip":-1,"take":-1}}""")]
    [InlineData("""{"paging":{"type":"offset","skip":2147483647,"take":2147483647}}""")]
    [InlineData("""{"paging":{"type":"cursor","after":"x","take":0}}""")]
    [InlineData("""{"search":{"term":null}}""")]
    [InlineData("""{"timeZone":"\u0000\u0000"}""")]
    [InlineData("""{"timeZone":"Europe/Paris"}""")]
    [InlineData("""{"filter":{"combinator":"nope"}}""")]
    [InlineData("""{"filter":{"combinator":3}}""")]
    [InlineData("""not json""")]
    [InlineData("""[]""")]
    [InlineData("""null""")]
    public void Hostile_json_never_throws_beyond_a_JsonException(string json) => RunSafely(json);

    [Fact]
    public void A_rule_without_a_key_is_dropped()
    {
        var request = Parse("""{"filter":{"children":[{"type":"rule","operator":"is","value":{"type":"text","value":"x"}},{"type":"rule","key":"name","operator":"is","value":{"type":"text","value":"bob smith"}}]}}""")!;
        var clean = Schema.Sanitize(request);
        Assert.Equal([Rule("name", Op.Is, Text("bob smith"))], clean.Filter.Children);
    }

    [Fact]
    public void Integers_and_unknown_names_are_rejected_for_enums()
    {
        Assert.Throws<JsonException>(() => Parse("""{"filter":{"children":[{"type":"rule","key":"a","operator":1}]}}"""));
        Assert.Throws<JsonException>(() => Parse("""{"filter":{"children":[{"type":"rule","key":"a","operator":"Nope"}]}}"""));
        Assert.Throws<JsonException>(() => Parse("""{"filter":{"combinator":0}}"""));
    }

    [Fact]
    public void Huge_lists_are_capped_and_deduplicated()
    {
        var values = string.Join(',', Enumerable.Range(0, 20_000).Select(i => $"\"v{i}\""));
        var request = Parse("{\"filter\":{\"children\":[{\"type\":\"rule\",\"key\":\"status\",\"operator\":\"isAnyOf\",\"value\":{\"type\":\"list\",\"values\":[" + values + "]}}]}}")!;
        var clean = Schema.Sanitize(request);
        var list = (FilterValue.List)((FilterRule)clean.Filter.Children.Single()).Value!;
        Assert.Equal(500, list.Values.Count);
        Schema.ExecuteInMemory(Employees, request);

        var small = DataListSchema.For<Employee>(s => s.Field(e => e.Status, f => f.Filter(DataListFilter.Option())).MaxListValues(2));
        var three = Rule("status", Op.IsAnyOf, List("Active", "Active", "Pending", "Archived"));
        var kept = (FilterValue.List)((FilterRule)small.Sanitize(Request(DataListFilterGroup.And(three))).Filter.Children.Single()).Value!;
        Assert.Equal(["Active", "Pending"], kept.Values);
    }

    [Fact]
    public void The_number_of_rules_and_the_depth_of_groups_are_capped()
    {
        var rules = Enumerable.Range(0, 1000).Select(i => (DataListFilterNode)Rule("age", Op.Gt, Num(i))).ToArray();
        Assert.Equal(100, Schema.Sanitize(Request(DataListFilterGroup.And(rules))).Filter.Children.Count);

        var small = DataListSchema.For<Employee>(s => s.Field(e => e.Age, f => f.Filter(DataListFilter.Number())).MaxFilterRules(3));
        var tree = DataListFilterGroup.And(rules[0], DataListFilterGroup.Or(rules[1], rules[2], rules[3]), rules[4]);
        var kept = small.Sanitize(Request(tree)).Filter;
        Assert.Equal(2, kept.Children.Count);
        Assert.Equal(2, ((DataListFilterGroup)kept.Children[1]).Children.Count);

        DataListFilterGroup deep = DataListFilterGroup.And(Rule("age", Op.Gt, Num(1)));
        for (var i = 0; i < 40; i++) deep = DataListFilterGroup.And(deep);
        var depth = 0;
        for (var g = Schema.Sanitize(Request(deep)).Filter; g.Children.Count > 0 && g.Children[0] is DataListFilterGroup n; g = n) depth++;
        Assert.True(depth <= DataListSchema.MaxFilterDepth);
        Schema.ExecuteInMemory(Employees, Request(deep));
    }

    [Fact]
    public void Deeply_nested_json_is_rejected_by_the_serializer_not_by_a_stack_overflow()
    {
        var json = string.Concat(Enumerable.Repeat("""{"type":"group","children":[""", 500)) + string.Concat(Enumerable.Repeat("]}", 500));
        Assert.Throws<JsonException>(() => Parse("{\"filter\":{\"children\":[" + json + "]}}"));
    }

    [Fact]
    public void Wire_enums_are_camel_case_like_the_url()
    {
        var state = new DataListState("", [], [], 1, 10, DataListLayout.Grid);
        Assert.Contains("\"layout\":\"grid\"", JsonSerializer.Serialize(state, DataListJsonContext.Default.DataListState));
        var facet = JsonSerializer.Serialize(new DataListFacetRequest("k", FacetMode.IncludeSelf), DataListJsonContext.Default.DataListFacetRequest);
        Assert.Contains("\"mode\":\"includeSelf\"", facet);
    }

    [Fact]
    public void Results_have_value_equality()
    {
        var a = new DataListResult<Employee>([Employees[0], Employees[1]], 2, null, [new DataListFacetResult("s", [new DataListFacetOption("x", 1)])]);
        var b = new DataListResult<Employee>([Employees[0], Employees[1]], 2, null, [new DataListFacetResult("s", [new DataListFacetOption("x", 1)])]);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, b with { Items = [Employees[0]] });
        Assert.NotEqual(a, b with { Facets = null });
    }
}
