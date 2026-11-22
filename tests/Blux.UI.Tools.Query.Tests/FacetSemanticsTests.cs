using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

/// <summary>
/// The facet counting rules of the table spec (section 2.5, <c>countFacetOptions</c>), one row of a table each: a row counts once per distinct value, empty values are the missing count, options sort by count
/// then value, the search is a case-insensitive substring, the window slices, and exclude-self leaves the facet's own rule out. Run by the in-memory executor, which the IQueryable applier is tested against
/// in the EF tests (differential facet tests).
/// </summary>
public class FacetSemanticsTests
{
    private sealed record Item(int Id, string? Color, IReadOnlyList<Tag>? Tags, bool? Flag);

    private static readonly Item[] Items =
    [
        new(1, "red", [new Tag("a"), new Tag("b"), new Tag("a")], true),
        new(2, "Red", [], false),
        new(3, "blue", null, null),
        new(4, "", [new Tag("b")], true),
        new(5, null, [new Tag("c")], true),
        new(6, "red", [new Tag("a")], true),
        new(7, "green", [new Tag("b"), new Tag("c")], false),
    ];

    private static readonly DataListSchema<Item> Schema = DataListSchema.For<Item>(s => s
        .RowKey(i => i.Id)
        .Search(i => i.Color)
        .Field(i => i.Color, f => f.Filter(DataListFilter.Option()))
        .Collection(i => i.Tags, t => t!.Name, f => f.Filter(DataListFilter.Option()))
        .Field(i => i.Flag, f => f.Filter(DataListFilter.Boolean())));

    private static DataListFilterGroup Where(params FilterRule[] rules) => DataListFilterGroup.And(rules);

    private static string Describe(DataListFacetResult facet) => string.Join(";", facet.Options.Select(o => $"{o.Value}:{o.Count}"));

    public static TheoryData<string, DataListFilterGroup?, DataListSearch?, DataListFacetRequest, string, int?, int> Cases() => new()
    {
        // name, filter, search, facet, options (value:count by count then value, ordinal), missing, total
        { "counts per value, the most counted first, ties by ordinal value", null, null, new("color"), "red:2;Red:1;blue:1;green:1", null, 4 },
        { "values that differ by case are different options", null, null, new("color", Search: "red"), "red:2;Red:1", null, 2 },
        { "a row counts once per distinct value even if several elements carry it", null, null, new("tags.name"), "b:3;a:2;c:2", null, 3 },
        { "an empty string, a null, an empty collection and a null collection are missing", null, null, new("color", IncludeMissing: true), "red:2;Red:1;blue:1;green:1", 2, 4 },
        { "rows without any element count as missing for a collection", null, null, new("tags.name", IncludeMissing: true), "b:3;a:2;c:2", 2, 3 },
        { "missing is left out unless asked for", null, null, new("tags.name"), "b:3;a:2;c:2", null, 3 },
        { "a boolean counts true and false, in that order of count", null, null, new("flag", IncludeMissing: true), "true:4;false:2", 1, 2 },
        { "the search of the facet is a case-insensitive substring of the value", null, null, new("color", Search: "RE"), "red:2;Red:1;green:1", null, 3 },
        { "the window slices the options and the total is the count before it", null, null, new("color", Limit: 2), "red:2;Red:1", null, 4 },
        { "skip moves the window", null, null, new("color", Skip: 2, Limit: 2), "blue:1;green:1", null, 4 },
        { "a window past the end is empty", null, null, new("color", Skip: 9), "", null, 4 },
        { "exclude-self removes the facet's own rule", Where(Rule("color", Op.IsAnyOf, List("red"))), null, new("color"), "red:2;Red:1;blue:1;green:1", null, 4 },
        { "include-self keeps the facet's own rule", Where(Rule("color", Op.IsAnyOf, List("red"))), null, new("color", FacetMode.IncludeSelf), "red:2", null, 1 },
        { "the other filters narrow the counts", Where(Rule("tags.name", Op.IsAnyOf, List("a"))), null, new("color"), "red:2", null, 1 },
        { "a filter on another field narrows the missing count too", Where(Rule("flag", Op.Is, new FilterValue.Boolean(true))), null, new("color", IncludeMissing: true), "red:2", 2, 1 },
        { "the search of the list narrows the counts", null, new DataListSearch("re"), new("color"), "red:2;Red:1;green:1", null, 3 },
        { "include-self on a collection keeps rows with the element", Where(Rule("tags.name", Op.IsAnyOf, List("c"))), null, new("tags.name", FacetMode.IncludeSelf), "c:2;b:1", null, 2 },
        { "exclude-self on a collection counts every element", Where(Rule("tags.name", Op.IsAnyOf, List("c"))), null, new("tags.name"), "b:3;a:2;c:2", null, 3 },
        { "a facet counts nothing when no row matches", Where(Rule("flag", Op.Is, new FilterValue.Boolean(false)), Rule("tags.name", Op.IsAnyOf, List("zz"))), null, new("color", IncludeMissing: true), "", 0, 0 },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Facets_count_as_the_spec_says(string name, DataListFilterGroup? filter, DataListSearch? search, DataListFacetRequest facet, string options, int? missing, int total)
    {
        var request = new DataListRequest(DataListPaging.None.Instance, [], filter ?? DataListFilterGroup.Empty, search, [facet]);

        var result = Schema.ExecuteInMemory(Items, request).Facets!.Single();

        Assert.True(options == Describe(result), $"{name}: expected {options}, got {Describe(result)}");
        Assert.Equal(missing, result.Missing);
        Assert.Equal(total, result.Total);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Several_facets_are_counted_independently_in_the_order_asked()
    {
        var request = new DataListRequest(DataListPaging.None.Instance, [], Where(Rule("color", Op.IsAnyOf, List("red"))), null,
            [new DataListFacetRequest("flag"), new DataListFacetRequest("color", FacetMode.IncludeSelf), new DataListFacetRequest("tags.name")]);

        var facets = Schema.ExecuteInMemory(Items, request).Facets!;

        Assert.Equal(["flag", "color", "tags.name"], facets.Select(f => f.Key));
        // The rows of the filter are red: ids 1 and 6.
        Assert.Equal("true:2", Describe(facets[0]));
        Assert.Equal("red:2", Describe(facets[1]));
        Assert.Equal("a:2;b:1", Describe(facets[2]));
    }
}

/// <summary>What the definition of a filter says about its counts, the order and the limit of its options and its remote source.</summary>
public class FacetFilterDefinitionTests
{
    [Fact]
    public void A_boolean_filter_can_ask_for_counts_and_an_option_filter_for_a_mode_a_sort_and_a_limit()
    {
        var boolean = DataListFilter.Boolean().WithFacets().WithFacetMode(FacetMode.IncludeSelf);
        var option = DataListFilter.Option("a", "b").WithFacets().WithOptionSort(DataListOptionSort.Alpha).WithOptionLimit(5);

        Assert.True(boolean.Facets);
        Assert.Equal(FacetMode.IncludeSelf, boolean.FacetMode);
        Assert.Equal(FacetMode.ExcludeSelf, option.FacetMode);
        Assert.Equal(DataListOptionSort.Alpha, option.OptionSort);
        Assert.Equal(5, option.OptionLimit);
        Assert.Null(DataListFilter.Option().OptionSort);
        Assert.Null(DataListFilter.Option().OptionLimit);
        // They survive every other copy of the definition.
        var copy = option.WithLabel("Kind").WithPersist();
        Assert.Equal((DataListOptionSort.Alpha, 5, true), (copy.OptionSort!.Value, copy.OptionLimit!.Value, copy.Facets));
    }

    [Fact]
    public void Counts_belong_to_option_and_boolean_filters_and_the_order_and_limit_to_option_filters()
    {
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Number().WithFacets());
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Date().WithFacetMode(FacetMode.IncludeSelf));
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Boolean().WithOptionSort(DataListOptionSort.Count));
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Boolean().WithOptionLimit(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => DataListFilter.Option().WithOptionLimit(0));
    }

    private static readonly DataListOptionsProvider Provider = (_, _) => ValueTask.FromResult(new DataListOptionsPage([]));

    [Fact]
    public void Remote_options_carry_the_source_the_page_size_and_the_minimum()
    {
        DataListOptionsResolver resolver = (values, _) => ValueTask.FromResult<IReadOnlyList<DataListFilterOption>>([.. values.Select(v => new DataListFilterOption(v, v))]);

        var filter = DataListFilter.Option().WithRemoteOptions(Provider, resolver, pageSize: 10, minLength: 2).WithFacets().WithLabel("Owner");

        Assert.Same(Provider, filter.RemoteOptions!.Provider);
        Assert.Same(resolver, filter.RemoteOptions.Resolver);
        Assert.Equal(10, filter.RemoteOptions.PageSize);
        Assert.Equal(2, filter.RemoteOptions.MinLength);
        Assert.Null(DataListFilter.Option().RemoteOptions);
        Assert.Equal(DataListRemoteOptions.DefaultPageSize, DataListFilter.Option().WithRemoteOptions(Provider).RemoteOptions!.PageSize);
    }

    [Fact]
    public void Remote_options_are_for_an_option_filter_without_declared_options()
    {
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Text().WithRemoteOptions(Provider));
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Option("a").WithRemoteOptions(Provider));
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Option().WithRemoteOptions(Provider).WithOptions(new DataListFilterOption("a", "A")));
        Assert.Throws<ArgumentOutOfRangeException>(() => DataListFilter.Option().WithRemoteOptions(Provider, pageSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DataListFilter.Option().WithRemoteOptions(Provider, minLength: -1));
        Assert.Throws<ArgumentNullException>(() => DataListFilter.Option().WithRemoteOptions(null!));
    }

    [Fact]
    public void A_remote_option_filter_accepts_any_value_in_a_rule_and_round_trips_through_the_url()
    {
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Id, f => f.Filter(DataListFilter.Option().WithRemoteOptions(Provider))));
        var rule = new FilterRule("id", Op.IsAnyOf, new FilterValue.List(["1", "42"]));
        var state = schema.DefaultState with { Filters = [rule] };

        var query = DataListStateQuery.ToQuery(state, schema);
        var back = DataListStateQuery.FromQuery(query.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)), schema);

        Assert.Equal("1,42", query["f.id"]);
        Assert.Equal([rule], back.Filters);
    }
}
