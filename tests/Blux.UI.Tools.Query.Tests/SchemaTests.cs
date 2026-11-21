using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

public class SchemaTests
{
    [Fact]
    public void Keys_default_to_the_camel_case_member_path()
    {
        var keys = Schema.FilterKeys;
        Assert.Contains("department.name", keys);
        Assert.Contains("lastLogin", keys);
        Assert.Contains("name", keys);
        Assert.Contains("archived", keys);
        Assert.Equal(["name", "age", "salary", "hired", "status"], Schema.SortKeys);
    }

    [Fact]
    public void A_key_can_be_overridden()
    {
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Department!.Name, f => f.Key("dept").Filter(DataListFilter.Option())));
        Assert.Equal(["dept"], schema.FilterKeys);
        var rows = TestData.Employees;
        var result = schema.ExecuteInMemory(rows, Request(DataListFilterGroup.And(Rule("dept", Op.Is, Text("Sales")))));
        Assert.Equal([2, 5], result.Ids());
    }

    [Fact]
    public void A_key_used_twice_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s
            .Field(e => e.Name, f => f.Sortable())
            .Field(e => e.Name, f => f.Sortable())));
        Assert.Contains("name", ex.Message);
        Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s
            .Field(e => e.Name, f => f.Sortable())
            .QueryFilter("name", DataListFilter.Text())));
    }

    [Fact]
    public void A_field_that_is_not_a_member_path_needs_a_key()
    {
        Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s.Field(e => e.Name.ToUpper(), f => f.Sortable())));
        var schema = DataListSchema.For<Employee>(s => s.Field(e => e.Name.ToUpper(), f => f.Key("upper").Sortable()));
        Assert.Equal(["upper"], schema.SortKeys);
    }

    [Theory]
    [InlineData("text on an int", FilterKind.Text, nameof(Employee.Age))]
    [InlineData("number on a string", FilterKind.Number, nameof(Employee.Name))]
    [InlineData("boolean on a string", FilterKind.Boolean, nameof(Employee.Name))]
    [InlineData("date on a number", FilterKind.Date, nameof(Employee.Salary))]
    public void A_filter_kind_must_suit_the_field_type(string name, FilterKind kind, string property)
    {
        _ = name;
        var filter = kind switch
        {
            FilterKind.Text => DataListFilter.Text(),
            FilterKind.Number => DataListFilter.Number(),
            FilterKind.Boolean => DataListFilter.Boolean(),
            _ => DataListFilter.Date(),
        };
        Assert.Throws<InvalidOperationException>(() => property switch
        {
            nameof(Employee.Age) => DataListSchema.For<Employee>(s => s.Field(e => e.Age, f => f.Filter(filter))),
            nameof(Employee.Name) => DataListSchema.For<Employee>(s => s.Field(e => e.Name, f => f.Filter(filter))),
            _ => DataListSchema.For<Employee>(s => s.Field(e => e.Salary, f => f.Filter(filter))),
        });
    }

    [Fact]
    public void Option_filters_accept_strings_enums_numbers_and_booleans()
    {
        var schema = DataListSchema.For<Employee>(s => s
            .Field(e => e.Status, f => f.Filter(DataListFilter.Option()))
            .Field(e => e.Id, f => f.Filter(DataListFilter.Option()))
            .Field(e => e.Active, f => f.Filter(DataListFilter.Option()))
            .Field(e => e.Name, f => f.Filter(DataListFilter.Option())));
        Assert.Equal(4, schema.FilterKeys.Count);
        Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s.Field(e => e.Department, f => f.Filter(DataListFilter.Option()))));
    }

    [Fact]
    public void Default_sort_registers_a_missing_field_as_sortable()
    {
        var schema = DataListSchema.For<Employee>(s => s.DefaultSort(e => e.Age, descending: true));
        Assert.Equal(["age"], schema.SortKeys);
        Assert.Equal([new DataListSort("age", true)], schema.DefaultSort);
        Assert.Throws<InvalidOperationException>(() => DataListSchema.For<Employee>(s => s.DefaultSort("nope")));
    }

    [Fact]
    public void Row_key_and_label_are_read_from_the_row()
    {
        Assert.True(Schema.HasRowKey);
        Assert.Equal("4", Schema.GetRowKey(Employees[3]));
        Assert.Equal("dave brown", Schema.GetRowLabel(Employees[3]));
        var none = DataListSchema.For<Employee>(_ => { });
        Assert.False(none.HasRowKey);
        Assert.Null(none.GetRowLabel(Employees[0]));
        Assert.Throws<InvalidOperationException>(() => none.GetRowKey(Employees[0]));
    }

    [Fact]
    public void Defaults_come_from_the_filters_sort_and_settings()
    {
        var schema = DataListSchema.For<Employee>(s => s
            .Field(e => e.Name, f => f.Sortable())
            .Field(e => e.Active, f => f.Filter(DataListFilter.Boolean(true)))
            .QueryFilter("archived", DataListFilter.Boolean(false))
            .DefaultSort(e => e.Name)
            .DefaultPageSize(25).MaxPageSize(100).DefaultLayout(DataListLayout.Grid));
        var state = schema.DefaultState;
        Assert.Equal(25, state.PageSize);
        Assert.Equal(1, state.Page);
        Assert.Equal(DataListLayout.Grid, state.Layout);
        Assert.Equal([new DataListSort("name")], state.Sort);
        Assert.Equal(
            [new FilterRule("active", Op.Is, new FilterValue.Bool(true)), new FilterRule("archived", Op.Is, new FilterValue.Bool(false))],
            state.Filters);
        Assert.Equal(25, DataListSchema.For<Employee>(s => s.DefaultPageSize(500).MaxPageSize(25)).DefaultPageSize);
    }

    // ---- allowed operators and defaults per kind (table.md section 1.7)

    public static TheoryData<FilterKind, Op[], Op> KindTable() => new()
    {
        { FilterKind.Text, [Op.Contains, Op.Is, Op.IsNot], Op.Contains },
        { FilterKind.Option, [Op.IsAnyOf, Op.Is, Op.IsNot], Op.IsAnyOf },
        { FilterKind.Boolean, [Op.Is, Op.IsNot], Op.Is },
        { FilterKind.Number, [Op.Is, Op.IsNot, Op.Gt, Op.Gte, Op.Lt, Op.Lte, Op.Between], Op.Is },
        { FilterKind.Date, [Op.Is, Op.IsNot, Op.Before, Op.After, Op.Between], Op.Is },
    };

    [Theory]
    [MemberData(nameof(KindTable))]
    public void Each_kind_allows_its_operators_and_has_its_default(FilterKind kind, Op[] allowed, Op defaultOperator)
    {
        var filter = kind switch
        {
            FilterKind.Text => DataListFilter.Text(),
            FilterKind.Option => DataListFilter.Option(),
            FilterKind.Boolean => DataListFilter.Boolean(),
            FilterKind.Number => DataListFilter.Number(),
            _ => DataListFilter.Date(),
        };
        Assert.Equal(kind, filter.Kind);
        Assert.Equal(allowed.Order(), filter.AllowedOperators.Order());
        Assert.Equal(defaultOperator, filter.DefaultOperator);
    }

    [Fact]
    public void Operators_can_be_narrowed_or_extended_with_the_empty_checks_only()
    {
        var number = DataListFilter.Number().WithOperators(Op.Gte, Op.Lte);
        Assert.Equal([Op.Gte, Op.Lte], number.AllowedOperators);
        Assert.Equal(Op.Gte, number.DefaultOperator);
        Assert.Throws<ArgumentException>(() => DataListFilter.Number().WithOperators(Op.Contains));
        Assert.Throws<ArgumentException>(() => DataListFilter.Text().WithOperators());
        Assert.Contains(Op.IsEmpty, DataListFilter.Text().WithOperators(Op.Contains, Op.IsEmpty).AllowedOperators);
        Assert.Equal(Op.Gt, DataListFilter.Number().WithDefaultOperator(Op.Gt).DefaultOperator);
        Assert.Throws<ArgumentException>(() => DataListFilter.Boolean().WithDefaultOperator(Op.Gt));
    }

    [Fact]
    public void Default_values_make_a_default_rule()
    {
        var filter = DataListFilter.Number(0, 1).WithDefault(new FilterValue.Number(5), Op.Gte);
        Assert.Equal(new FilterRule("age", Op.Gte, new FilterValue.Number(5)), filter.DefaultRule("age"));
        Assert.Null(DataListFilter.Number().DefaultRule("age"));
        Assert.Equal(0, filter.Min);
        Assert.Equal(1, filter.Step);
        Assert.True(filter.WithPersist().Persist);
        Assert.Throws<ArgumentException>(() => DataListFilter.Boolean().WithDefault(new FilterValue.Bool(true), Op.Gt));
    }

    [Theory]
    [InlineData(FilterKind.Text, Op.Contains, "x", true)]
    [InlineData(FilterKind.Text, Op.Contains, "", false)]
    [InlineData(FilterKind.Text, Op.Gt, "x", false)]
    [InlineData(FilterKind.Number, Op.Gt, "n", true)]
    [InlineData(FilterKind.Number, Op.Gt, "x", false)]
    [InlineData(FilterKind.Number, Op.Between, "r", true)]
    [InlineData(FilterKind.Number, Op.Between, "n", false)]
    [InlineData(FilterKind.Boolean, Op.Is, "b", true)]
    [InlineData(FilterKind.Boolean, Op.Is, "n", false)]
    [InlineData(FilterKind.Date, Op.Before, "d", true)]
    [InlineData(FilterKind.Date, Op.Before, "m", true)]
    [InlineData(FilterKind.Date, Op.Between, "rd", true)]
    [InlineData(FilterKind.Date, Op.Between, "rn", false)]
    [InlineData(FilterKind.Option, Op.IsAnyOf, "l", true)]
    [InlineData(FilterKind.Option, Op.IsAnyOf, "le", false)]
    public void IsValid_checks_operator_and_value_shape(FilterKind kind, Op op, string valueCode, bool valid)
    {
        FilterValue? value = valueCode switch
        {
            "x" => new FilterValue.Text("x"),
            "" => new FilterValue.Text(""),
            "n" => new FilterValue.Number(1),
            "b" => new FilterValue.Bool(true),
            "d" => new FilterValue.Date(new DateOnly(2024, 1, 1)),
            "m" => new FilterValue.Moment(DateTimeOffset.UnixEpoch),
            "r" => new FilterValue.Range(new FilterValue.Number(1), null),
            "rd" => new FilterValue.Range(null, new FilterValue.Date(new DateOnly(2024, 1, 1))),
            "rn" => new FilterValue.Range(new FilterValue.Number(1), null),
            "l" => new FilterValue.List(["a"]),
            "le" => new FilterValue.List([]),
            _ => null,
        };
        var filter = kind switch
        {
            FilterKind.Text => DataListFilter.Text(),
            FilterKind.Option => DataListFilter.Option(),
            FilterKind.Boolean => DataListFilter.Boolean(),
            FilterKind.Number => DataListFilter.Number(),
            _ => DataListFilter.Date(),
        };
        Assert.Equal(valid, filter.IsValid(new FilterRule("k", op, value)));
    }

    [Fact]
    public void A_range_needs_one_bound_and_the_right_bound_types()
    {
        var number = DataListFilter.Number();
        Assert.False(number.IsValid(new FilterRule("k", Op.Between, new FilterValue.Range(null, null))));
        Assert.False(number.IsValid(new FilterRule("k", Op.Between, new FilterValue.Range(new FilterValue.Text("a"), null))));
        Assert.True(number.IsValid(new FilterRule("k", Op.Between, new FilterValue.Range(null, new FilterValue.Number(3)))));
        Assert.False(DataListFilter.Date().IsValid(new FilterRule("k", Op.Between, new FilterValue.Range(new FilterValue.Number(1), null))));
        Assert.True(DataListFilter.Option().IsValid(new FilterRule("k", Op.IsAnyOf, new FilterValue.List([]), IncludeEmpty: true)));
    }
}

public class SanitizeTests
{
    private static DataListRequest Sanitized(DataListRequest request) => Schema.Sanitize(request);

    [Fact]
    public void Unknown_keys_disallowed_operators_and_wrong_values_are_dropped()
    {
        var good = Rule("age", Op.Gt, Num(3));
        var filter = DataListFilterGroup.And(
            good,
            Rule("nope", Op.Is, Text("x")),
            Rule("age", Op.Contains, Text("3")),
            Rule("age", Op.Gt, Text("x")),
            Rule("status", Op.IsAnyOf, List()),
            Rule("name", Op.Contains, Text("")));
        Assert.Equal([good], Sanitized(Request(filter)).Filter.Children);
    }

    [Fact]
    public void Nested_groups_are_cleaned_and_empty_ones_removed()
    {
        var good = Rule("active", Op.Is, new FilterValue.Bool(true));
        var filter = DataListFilterGroup.Or(
            DataListFilterGroup.And(Rule("nope", Op.Is, Text("x"))),
            DataListFilterGroup.And(good, Rule("nope", Op.Is, Text("y"))));
        var clean = Sanitized(Request(filter)).Filter;
        Assert.Equal(FilterCombinator.Or, clean.Combinator);
        Assert.Equal(DataListFilterGroup.And(good), Assert.Single(clean.Children));
    }

    [Fact]
    public void Query_only_rules_are_kept_for_the_provider()
    {
        var rule = Rule("archived", Op.Is, new FilterValue.Bool(true));
        Assert.Equal([rule], Sanitized(Request(DataListFilterGroup.And(rule))).Filter.Children);
    }

    [Fact]
    public void IncludeEmpty_is_cleared_on_the_empty_operators()
    {
        var clean = Sanitized(Request(DataListFilterGroup.And(Rule("email", Op.IsEmpty, null, includeEmpty: true))));
        Assert.Equal(new FilterRule("email", Op.IsEmpty), Assert.Single(clean.Filter.Children));
    }

    [Fact]
    public void Sort_keeps_known_sortable_distinct_keys_in_order()
    {
        var sort = new[]
        {
            new DataListSort("nope"), new DataListSort("email"), new DataListSort("age", true),
            new DataListSort("age"), new DataListSort("name"),
        };
        Assert.Equal([new DataListSort("age", true), new DataListSort("name")], Sanitized(Request(null, null, sort)).Sort);
    }

    [Fact]
    public void Facets_must_be_option_or_boolean_field_filters()
    {
        var facets = new[]
        {
            new DataListFacetRequest("status", Limit: 100000, Skip: -3),
            new DataListFacetRequest("age"), new DataListFacetRequest("archived"), new DataListFacetRequest("nope"),
            new DataListFacetRequest("active"), new DataListFacetRequest("status"),
        };
        var clean = Sanitized(new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.Empty, null, facets)).Facets;
        Assert.Equal(["status", "active"], clean.Select(f => f.Key));
        Assert.Equal(1000, clean[0].Limit);
        Assert.Equal(0, clean[0].Skip);
    }

    [Fact]
    public void Paging_is_clamped()
    {
        var schema = DataListSchema.For<Employee>(s => s.MaxPageSize(100).DefaultPageSize(20));
        DataListPaging Of(DataListPaging p) => schema.Sanitize(new DataListRequest(p, [], DataListFilterGroup.Empty)).Paging;
        Assert.Equal(new DataListPaging.Offset(0, 100), Of(new DataListPaging.Offset(-10, 5000)));
        Assert.Equal(new DataListPaging.Offset(40, 20), Of(new DataListPaging.Offset(40, 0)));
        Assert.Equal(new DataListPaging.Cursor("c", 100, true), Of(new DataListPaging.Cursor("c", 999, true)));
        Assert.Equal(DataListPaging.None.Instance, Of(DataListPaging.None.Instance));
    }

    [Fact]
    public void Search_is_dropped_when_blank_or_when_the_schema_has_no_search_fields()
    {
        Assert.Null(Sanitized(Request(null, new DataListSearch("  "))).Search);
        Assert.Equal(new DataListSearch("x"), Sanitized(Request(null, new DataListSearch("x"))).Search);
        var noSearch = DataListSchema.For<Employee>(s => s.Field(e => e.Name));
        Assert.Null(noSearch.Sanitize(Request(null, new DataListSearch("x"))).Search);
    }
}
