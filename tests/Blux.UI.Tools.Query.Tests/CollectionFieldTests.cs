using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

/// <summary>Collection-valued fields: "at least one element" semantics for filters, search and facets; no sorting.</summary>
public class CollectionFieldTests
{
    private static readonly Employee[] Rows =
    [
        Employees[0] with { Tags = [new Tag("dotnet", 1), new Tag("blazor", 5)] },
        Employees[1] with { Tags = [new Tag("sales", 3)] },
        Employees[2] with { Tags = [] },
        Employees[3] with { Tags = null },
        Employees[4] with { Tags = [new Tag("dotnet", 2), new Tag(null)] },
        Employees[5] with { Tags = [new Tag("", 9), new Tag("Blazor", 4), null!] },
    ];

    private static readonly DataListSchema<Employee> TagSchema = DataListSchema.For<Employee>(s => s
        .RowKey(e => e.Id)
        .Field(e => e.Name, f => f.Sortable())
        .Collection(e => e.Tags, t => t!.Name, f => f.Filter(WithEmpty(DataListFilter.Option())))
        .Collection(e => e.Tags, t => t!.Name, f => f.Key("tagText").Filter(DataListFilter.Text()))
        .Collection(e => e.Tags, t => t!.Weight, f => f.Filter(DataListFilter.Number()))
        .SearchAny(e => e.Tags, t => t!.Name));

    private static int[] Run(FilterRule rule) =>
        TagSchema.ExecuteInMemory(Rows, Request(DataListFilterGroup.And(rule))).Ids();

    public static TheoryData<string, FilterRule, int[]> Cases() => new()
    {
        { "isAnyOf one element", Rule("tags.name", Op.IsAnyOf, List("dotnet")), [1, 5] },
        { "is is case sensitive", Rule("tags.name", Op.Is, Text("Blazor")), [6] },
        { "isAnyOf any element", Rule("tags.name", Op.IsAnyOf, List("dotnet", "sales")), [1, 2, 5] },
        { "isNot means no element is any of the values", Rule("tags.name", Op.IsNot, List("dotnet")), [2, 3, 4, 6] },
        { "isNot with a value only one element has", Rule("tags.name", Op.IsNot, List("Blazor")), [1, 2, 3, 4, 5] },
        { "isEmpty is no non-empty element, null collection included", Rule("tags.name", Op.IsEmpty), [3, 4] },
        { "isNotEmpty is some non-empty element", Rule("tags.name", Op.IsNotEmpty), [1, 2, 5, 6] },
        { "includeEmpty adds rows with no value", Rule("tags.name", Op.IsAnyOf, List("sales"), includeEmpty: true), [2, 3, 4] },
        { "only empty", Rule("tags.name", Op.IsAnyOf, List(), includeEmpty: true), [3, 4] },
        { "text contains any element ignoring case", Rule("tagText", Op.Contains, Text("LAZ")), [1, 6] },
        { "text is any element", Rule("tagText", Op.Is, Text("dotnet")), [1, 5] },
        { "text isNot no element is", Rule("tagText", Op.IsNot, Text("dotnet")), [2, 3, 4, 6] },
        { "number gt some element", Rule("tags.weight", Op.Gt, Num(4)), [1, 6] },
        { "number between some element", Rule("tags.weight", Op.Between, Range(Num(3), Num(4))), [2, 6] },
        { "number is zero", Rule("tags.weight", Op.Is, Num(0)), [5] },
        { "number isNot no element equals", Rule("tags.weight", Op.IsNot, Num(5)), [2, 3, 4, 5, 6] },
        { "number lte", Rule("tags.weight", Op.Lte, Num(1)), [1, 5] },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Rule_matches_at_least_one_element(string name, FilterRule rule, int[] expected)
    {
        _ = name;
        Assert.Equal(expected.Order(), Run(rule).Order());
    }

    [Theory]
    [InlineData("blaz", new[] { 1, 6 })]
    [InlineData("SALES", new[] { 2 })]
    [InlineData("zzz", new int[0])]
    public void Search_matches_when_any_element_matches(string term, int[] expected)
    {
        var result = TagSchema.ExecuteInMemory(Rows, Request(search: new DataListSearch(term)));
        Assert.Equal(expected.Order(), result.Ids().Order());
    }

    [Fact]
    public void Facets_count_rows_per_option_once_and_count_rows_without_values_as_missing()
    {
        var request = new DataListRequest(DataListPaging.Unpaged, [], DataListFilterGroup.Empty, null, [new DataListFacetRequest("tags.name", IncludeMissing: true)]);
        var facet = TagSchema.ExecuteInMemory(Rows, request).Facets!.Single();
        Assert.Equal(
            [new DataListFacetOption("dotnet", 2), new DataListFacetOption("Blazor", 1), new DataListFacetOption("blazor", 1), new DataListFacetOption("sales", 1)],
            facet.Options);
        Assert.Equal(2, facet.Missing);
        Assert.Equal(4, facet.Total);

        var duplicates = new[] { Rows[0] with { Tags = [new Tag("x"), new Tag("x"), new Tag("y")] } };
        var counted = TagSchema.ExecuteInMemory(duplicates, request).Facets!.Single();
        Assert.Equal([new DataListFacetOption("x", 1), new DataListFacetOption("y", 1)], counted.Options);
    }

    [Fact]
    public void A_facet_leaves_out_its_own_collection_filter()
    {
        var filter = DataListFilterGroup.And(Rule("tags.name", Op.IsAnyOf, List("sales")));
        var request = new DataListRequest(DataListPaging.Unpaged, [], filter, null, [new DataListFacetRequest("tags.name")]);
        var result = TagSchema.ExecuteInMemory(Rows, request);
        Assert.Equal([2], result.Ids());
        Assert.Equal(4, result.Facets!.Single().Total);
    }

    [Fact]
    public void Collection_fields_cannot_be_sorted()
    {
        Assert.DoesNotContain("tags.name", TagSchema.SortKeys);
        var sorted = TagSchema.ExecuteInMemory(Rows, Request(null, null, new DataListSort("tags.name"))).Ids();
        Assert.Equal([1, 2, 3, 4, 5, 6], sorted);
        Assert.Empty(TagSchema.Sanitize(Request(null, null, new DataListSort("tags.name"))).Sort);
        var ex = Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s
            .Collection(e => e.Tags, t => t!.Name).DefaultSort("tags.name")));
        Assert.Contains("collection", ex.Message);
    }

    [Fact]
    public void Keys_are_the_joined_camel_case_paths_and_must_be_distinct_and_suit_the_element_type()
    {
        Assert.Contains("tags.name", TagSchema.FilterKeys);
        Assert.Contains("tags.weight", TagSchema.FilterKeys);
        Assert.Contains("tagText", TagSchema.FilterKeys);
        Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s
            .Collection(e => e.Tags, t => t!.Name, f => f.Filter(DataListFilter.Option()))
            .Collection(e => e.Tags, t => t!.Name, f => f.Filter(DataListFilter.Option()))));
        Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s
            .Collection(e => e.Tags, t => t!.Name, f => f.Filter(DataListFilter.Number()))));
    }

    [Fact]
    public void The_predicate_is_an_Any_expression_that_also_runs_without_null_guards()
    {
        var filter = DataListFilterGroup.And(Rule("tags.name", Op.IsAnyOf, List("dotnet")));
        var raw = FilterExpressions.Rows(TagSchema, filter, null, null, nullSafe: false)!;
        Assert.Contains("Any(", raw.ToString());
        Assert.DoesNotContain("== null", raw.ToString());
        var compiled = raw.Compile();
        Assert.Equal([1, 5], Rows.Where(r => r.Tags is not null && r.Tags.All(t => t is not null)).Where(compiled).Select(r => r.Id));
    }

    [Fact]
    public void The_compiled_predicate_is_reused_for_the_same_sanitized_filter()
    {
        var filter = DataListFilterGroup.And(Rule("tags.name", Op.IsAnyOf, List("dotnet")));
        var first = TagSchema.GetPredicate(filter, null, null);
        var second = TagSchema.GetPredicate(DataListFilterGroup.And(Rule("tags.name", Op.IsAnyOf, List("dotnet"))), null, null);
        Assert.Same(first, second);
    }
}
