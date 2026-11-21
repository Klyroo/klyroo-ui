using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

/// <summary>What the filter editors read from the definition of a filter (label, options, facet flag), and what the schema and the URL do with the rules an editor writes.</summary>
public class FilterEditorModelTests
{
    [Fact]
    public void Option_takes_options_with_a_label_or_plain_values_and_keeps_their_order()
    {
        var labelled = DataListFilter.Option(new DataListFilterOption("a", "Alpha"), new DataListFilterOption("b", "Beta"));
        var plain = DataListFilter.Option("x", "y");

        Assert.Equal([("a", "Alpha"), ("b", "Beta")], labelled.Options.Select(o => (o.Value, o.Label)));
        Assert.Equal([("x", "x"), ("y", "y")], plain.Options.Select(o => (o.Value, o.Label)));
        Assert.Empty(DataListFilter.Option().Options);
        Assert.Equal(FilterKind.Option, labelled.Kind);
        Assert.Equal([Op.IsAnyOf, Op.Is, Op.IsNot], labelled.AllowedOperators);
    }

    [Fact]
    public void Label_options_and_facets_survive_every_copy_of_the_definition()
    {
        var filter = DataListFilter.Option("a").WithLabel("Kind").WithFacets()
            .WithOperators(Op.IsAnyOf, Op.IsNot).WithDefaultOperator(Op.IsNot).WithPersist().WithDefault(new FilterValue.List(["a"]), Op.IsNot, includeEmpty: true);

        Assert.Equal("Kind", filter.Label);
        Assert.Equal(["a"], filter.Options.Select(o => o.Value));
        Assert.True(filter.Facets);
        Assert.True(filter.Persist);
        Assert.True(filter.DefaultIncludeEmpty);
        Assert.False(DataListFilter.Option("a").Facets);
        Assert.Null(DataListFilter.Text().Label);
    }

    [Fact]
    public void Options_and_facets_belong_to_option_filters_and_values_must_be_distinct()
    {
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Text().WithFacets());
        Assert.Throws<InvalidOperationException>(() => DataListFilter.Boolean().WithOptions(new DataListFilterOption("a", "A")));
        Assert.Throws<ArgumentException>(() => DataListFilter.Option("a", "a"));
        Assert.Throws<ArgumentException>(() => DataListFilter.Text().WithLabel(" "));
    }

    [Fact]
    public void A_rule_with_only_include_empty_is_valid_for_an_option_filter_and_round_trips_through_the_url()
    {
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Status, f => f.Filter(DataListFilter.Option("Active", "Paused"))));
        var rule = new FilterRule("status", Op.IsAnyOf, new FilterValue.List([]), IncludeEmpty: true);
        var state = schema.DefaultState with { Filters = [rule] };

        var query = DataListStateQuery.ToQuery(state, schema);
        var back = DataListStateQuery.FromQuery(query.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)), schema);

        Assert.Equal("~", query["f.status"]);
        Assert.Equal([rule], back.Filters);
    }

    [Fact]
    public void A_url_with_filters_the_schema_does_not_allow_loses_them()
    {
        var schema = DataListSchema.For<Employee>(s => s
            .Field(e => e.Name, f => f.Filter(DataListFilter.Text()))
            .Field(e => e.Status, f => f.Filter(DataListFilter.Option("Active"))));
        var pairs = new[]
        {
            new KeyValuePair<string, string?>("f.name", "ann"),
            new KeyValuePair<string, string?>("f.unknown", "1"),
            new KeyValuePair<string, string?>("f.name.gt", "3"),
            new KeyValuePair<string, string?>("f.status.between", "a..b"),
        };

        var state = DataListStateQuery.FromQuery(pairs, schema);

        Assert.Equal([new FilterRule("name", Op.Contains, new FilterValue.Text("ann"))], state.Filters);
    }
}
