using System.Globalization;
using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

/// <summary>Table-driven tests of the filter semantics of table.md section 2.5, one row per kind, operator and edge case.</summary>
public class InMemoryFilterTests
{
    public static TheoryData<string, FilterRule, int[]> RuleCases() => new()
    {
        // Text: contains ignores case, is and isNot compare exactly, an empty value is no filter
        { "text contains ci", Rule("name", Op.Contains, Text("ALICE")), [1] },
        { "text contains several", Rule("name", Op.Contains, Text("o")), [1, 2, 3, 4, 6] },
        { "text contains accent folded by lower only", Rule("name", Op.Contains, Text("éric")), [5] },
        { "text contains ignores accents", Rule("name", Op.Contains, Text("ERIC")), [5] },
        { "text contains ignores accents the other way", Rule("name", Op.Contains, Text("DURÁND")), [5] },
        { "text contains no match", Rule("name", Op.Contains, Text("zzz")), [] },
        { "text is exact", Rule("name", Op.Is, Text("bob smith")), [2] },
        { "text is is case sensitive", Rule("name", Op.Is, Text("Bob Smith")), [] },
        { "text isNot keeps nulls out of the match", Rule("name", Op.IsNot, Text("bob smith")), [1, 3, 4, 5, 6] },
        { "text empty value is inactive", Rule("email", Op.Contains, Text("")), [1, 2, 3, 4, 5, 6] },
        { "text null never contains", Rule("email", Op.Contains, Text("acme")), [1, 4] },
        { "text isNot includes null and empty", Rule("email", Op.IsNot, Text("alice@acme.io")), [2, 3, 4, 5, 6] },
        { "text isEmpty is null or empty string", Rule("email", Op.IsEmpty), [2, 3, 6] },
        { "text isNotEmpty", Rule("email", Op.IsNotEmpty), [1, 4, 5] },
        { "text includeEmpty adds null and empty", Rule("email", Op.Contains, Text("acme"), includeEmpty: true), [1, 2, 3, 4, 6] },
        { "text apostrophe", Rule("name", Op.Contains, Text("o'n")), [6] },

        // Number: int?, decimal, double fields against decimal values
        { "number is", Rule("age", Op.Is, Num(30)), [1, 5] },
        { "number isNot keeps null", Rule("age", Op.IsNot, Num(30)), [2, 3, 4, 6] },
        { "number gt", Rule("age", Op.Gt, Num(30)), [4] },
        { "number gte", Rule("age", Op.Gte, Num(30)), [1, 4, 5] },
        { "number lt", Rule("age", Op.Lt, Num(30)), [2, 6] },
        { "number lte", Rule("age", Op.Lte, Num(30)), [1, 2, 5, 6] },
        { "number null never matches gt", Rule("age", Op.Gt, Num(-100)), [1, 2, 4, 5, 6] },
        { "number between inclusive", Rule("age", Op.Between, Range(Num(25), Num(30))), [1, 2, 5] },
        { "number between only upper bound", Rule("age", Op.Between, Range(null, Num(25))), [2, 6] },
        { "number between only lower bound", Rule("age", Op.Between, Range(Num(30), null)), [1, 4, 5] },
        { "number fraction bound on an int field", Rule("age", Op.Between, Range(Num(25.5m), Num(30))), [1, 5] },
        { "number gt fraction on an int field", Rule("age", Op.Gt, Num(29.5m)), [1, 4, 5] },
        { "number is fraction on an int field", Rule("age", Op.Is, Num(30.5m)), [] },
        { "number includeEmpty adds null", Rule("age", Op.Is, Num(30), includeEmpty: true), [1, 3, 5] },
        { "number isEmpty", Rule("age", Op.IsEmpty), [3] },
        { "number isNotEmpty", Rule("age", Op.IsNotEmpty), [1, 2, 4, 5, 6] },
        { "decimal is", Rule("salary", Op.Is, Num(4200)), [2] },
        { "decimal gt exact fraction", Rule("salary", Op.Gt, Num(5000.50m)), [3, 4] },
        { "decimal gte exact fraction", Rule("salary", Op.Gte, Num(5000.5m)), [1, 3, 4] },
        { "decimal between", Rule("salary", Op.Between, Range(Num(3900.25m), Num(4200))), [2, 5] },
        { "double is", Rule("score", Op.Is, Num(2.5m)), [2] },
        { "double gt", Rule("score", Op.Gt, Num(2)), [2, 3, 6] },
        { "double lte", Rule("score", Op.Lte, Num(1.5m)), [1, 4] },

        // Boolean
        { "bool is true", Rule("active", Op.Is, new FilterValue.Boolean(true)), [1, 2, 4, 6] },
        { "bool is false", Rule("active", Op.Is, new FilterValue.Boolean(false)), [3, 5] },
        { "bool isNot true", Rule("active", Op.IsNot, new FilterValue.Boolean(true)), [3, 5] },

        // Date on a DateOnly field: a day value is the whole day, before and after are strict
        { "dateonly is", Rule("hired", Op.Is, Day("2020-01-15")), [1, 4] },
        { "dateonly isNot", Rule("hired", Op.IsNot, Day("2020-01-15")), [2, 3, 5, 6] },
        { "dateonly before", Rule("hired", Op.Before, Day("2020-01-15")), [3] },
        { "dateonly after", Rule("hired", Op.After, Day("2021-06-01")), [5, 6] },
        { "dateonly between inclusive", Rule("hired", Op.Between, Range(Day("2020-01-15"), Day("2021-06-01"))), [1, 2, 4] },
        { "dateonly between only upper", Rule("hired", Op.Between, Range(null, Day("2019-12-31"))), [3] },
        { "dateonly between only lower", Rule("hired", Op.Between, Range(Day("2022-01-01"), null)), [5, 6] },
        { "dateonly with a moment reduces to its UTC day", Rule("hired", Op.Is, At("2020-01-15T23:00:00Z")), [1, 4] },

        // Date on a DateTimeOffset? field (UTC days, instants exact), null never matches
        { "dto is day", Rule("lastLogin", Op.Is, Day("2024-03-02")), [3, 4] },
        { "dto isNot day keeps null", Rule("lastLogin", Op.IsNot, Day("2024-03-02")), [1, 2, 5, 6] },
        { "dto before day", Rule("lastLogin", Op.Before, Day("2024-03-02")), [1, 6] },
        { "dto after day", Rule("lastLogin", Op.After, Day("2024-03-02")), [5] },
        { "dto between days", Rule("lastLogin", Op.Between, Range(Day("2024-03-01"), Day("2024-03-02"))), [1, 3, 4] },
        { "dto is instant", Rule("lastLogin", Op.Is, At("2024-03-01T10:00:00Z")), [1] },
        { "dto instant in another offset", Rule("lastLogin", Op.Is, At("2024-03-01T12:00:00+02:00")), [1] },
        { "dto after instant", Rule("lastLogin", Op.After, At("2024-03-02T00:00:00Z")), [4, 5] },
        { "dto before instant", Rule("lastLogin", Op.Before, At("2024-03-01T10:00:00Z")), [6] },
        { "dto between open upper", Rule("lastLogin", Op.Between, Range(At("2024-03-02T00:00:00Z"), null)), [3, 4, 5] },
        { "dto isEmpty", Rule("lastLogin", Op.IsEmpty), [2] },
        { "dto includeEmpty", Rule("lastLogin", Op.After, Day("2024-03-02"), includeEmpty: true), [2, 5] },

        // Option: strings, enums by name ignoring case, ints from text, nested paths with a null link
        { "enum isAnyOf", Rule("status", Op.IsAnyOf, List("Active", "Pending")), [1, 2, 4, 5, 6] },
        { "enum is ignores case of the name", Rule("status", Op.Is, Text("archived")), [3] },
        { "enum isNot", Rule("status", Op.IsNot, List("Active")), [2, 3, 5] },
        { "enum unknown name matches nothing", Rule("status", Op.IsAnyOf, List("Nope")), [] },
        { "enum unknown name in isNot matches everything", Rule("status", Op.IsNot, List("Nope")), [1, 2, 3, 4, 5, 6] },
        { "nested isAnyOf", Rule("department.name", Op.IsAnyOf, List("Sales")), [2, 5] },
        { "nested isAnyOf includeEmpty covers a null link and an empty name", Rule("department.name", Op.IsAnyOf, List("Sales"), includeEmpty: true), [2, 3, 5, 6] },
        { "nested isNot keeps the null link", Rule("department.name", Op.IsNot, List("Sales")), [1, 3, 4, 6] },
        { "nested only empty", Rule("department.name", Op.IsAnyOf, List(), includeEmpty: true), [3, 6] },
        { "nested isEmpty", Rule("department.name", Op.IsEmpty), [3, 6] },
        { "int option from text", Rule("id", Op.IsAnyOf, List("1", "4", "x")), [1, 4] },
        { "option number value", Rule("id", Op.Is, Num(5)), [5] },
        { "option is with a list", Rule("status", Op.Is, List("Archived", "Pending")), [2, 3, 5] },

        // Invalid or foreign rules never restrict
        { "unknown key is ignored", Rule("nope", Op.Is, Text("x")), [1, 2, 3, 4, 5, 6] },
        { "wrong value shape is ignored", Rule("age", Op.Gt, Text("x")), [1, 2, 3, 4, 5, 6] },
        { "disallowed operator is ignored", Rule("age", Op.Contains, Text("3")), [1, 2, 3, 4, 5, 6] },
        { "query only filter is skipped", Rule("archived", Op.Is, new FilterValue.Boolean(true)), [1, 2, 3, 4, 5, 6] },
        { "empty option list without includeEmpty is inactive", Rule("status", Op.IsAnyOf, List()), [1, 2, 3, 4, 5, 6] },
    };

    [Theory]
    [MemberData(nameof(RuleCases))]
    public void Rule_matches_the_expected_rows(string name, FilterRule rule, int[] expected)
    {
        _ = name;
        var result = Schema.ExecuteInMemory(Employees, Request(DataListFilterGroup.And(rule)));
        Assert.Equal(expected.Order(), result.Ids().Order());
        Assert.Equal(expected.Length, result.TotalCount);
    }

    [Fact]
    public void Or_group_matches_any_child()
    {
        var group = DataListFilterGroup.Or(
            Rule("status", Op.Is, Text("Pending")),
            Rule("age", Op.Lt, Num(20)));
        Assert.Equal([2, 5, 6], Schema.ExecuteInMemory(Employees, Request(group)).Ids().Order());
    }

    [Fact]
    public void Nested_groups_combine()
    {
        var group = DataListFilterGroup.And(
            Rule("active", Op.Is, new FilterValue.Boolean(true)),
            DataListFilterGroup.Or(Rule("age", Op.Gt, Num(40)), Rule("name", Op.Contains, Text("alice"))));
        Assert.Equal([1, 4], Schema.ExecuteInMemory(Employees, Request(group)).Ids().Order());
    }

    [Fact]
    public void Empty_group_matches_everything_and_an_ignored_or_child_stays_neutral()
    {
        Assert.Equal(6, Schema.ExecuteInMemory(Employees, Request()).Items.Count);
        var group = DataListFilterGroup.Or(Rule("nope", Op.Is, Text("x")), Rule("name", Op.Is, Text("bob smith")));
        Assert.Equal([2], Schema.ExecuteInMemory(Employees, Request(group)).Ids());
    }

    [Fact]
    public void Query_only_rules_are_returned_for_the_provider()
    {
        var archived = Rule("archived", Op.Is, new FilterValue.Boolean(true));
        var request = Request(DataListFilterGroup.And(Rule("age", Op.Gt, Num(1)), DataListFilterGroup.And(archived)));
        Assert.Equal([archived], Schema.QueryFilterRules(request));
    }

    [Fact]
    public void Text_filters_do_not_depend_on_the_current_culture()
    {
        var rows = new[] { TestData.Employees[0] with { Id = 9, Name = "IRMA" } };
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var request = Request(DataListFilterGroup.And(Rule("name", Op.Contains, Text("irma"))));
            Assert.Single(Schema.ExecuteInMemory(rows, request).Items);
            request = Request(DataListFilterGroup.And(Rule("name", Op.Contains, Text("IRMA"))));
            Assert.Single(Schema.ExecuteInMemory(rows, request).Items);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void Numbers_and_dates_do_not_depend_on_the_current_culture()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var result = Schema.ExecuteInMemory(Employees, Request(DataListFilterGroup.And(
                Rule("salary", Op.Gte, Num(5000.5m)),
                Rule("hired", Op.Before, Day("2021-01-01")))));
            Assert.Equal([1, 3, 4], result.Ids().Order());
            // option values are read in the invariant culture: a dot, never a comma
            var ids = Schema.ExecuteInMemory(Employees, Request(DataListFilterGroup.And(Rule("id", Op.IsAnyOf, List("1", "٣")))));
            Assert.Equal([1], ids.Ids());
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Theory]
    [InlineData("acme", new[] { 1, 4 })]
    [InlineData("ENGINE", new[] { 1, 4 })]
    [InlineData("sales", new[] { 2, 5 })]
    [InlineData("  alice  ", new[] { 1 })]
    [InlineData("durand", new[] { 5 })]
    [InlineData("DURAND", new[] { 5 })]
    [InlineData("eric", new[] { 5 })]
    [InlineData("ÉRIC", new[] { 5 })]
    [InlineData("nothing", new int[0])]
    [InlineData("", new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData("   ", new[] { 1, 2, 3, 4, 5, 6 })]
    public void Search_matches_any_search_field_ignoring_case(string term, int[] expected)
    {
        var result = Schema.ExecuteInMemory(Employees, Request(search: new DataListSearch(term)));
        Assert.Equal(expected.Order(), result.Ids().Order());
    }

    [Fact]
    public void Search_combines_with_filters_and_a_schema_without_search_fields_ignores_it()
    {
        var both = Request(DataListFilterGroup.And(Rule("age", Op.Gte, Num(30))), new DataListSearch("acme"));
        Assert.Equal([1, 4], Schema.ExecuteInMemory(Employees, both).Ids().Order());

        var noSearch = DataListSchema.For<Employee>(s => s.Field(e => e.Name, f => f.Filter(DataListFilter.Text())));
        Assert.Equal(6, noSearch.ExecuteInMemory(Employees, Request(search: new DataListSearch("zzz"))).Items.Count);
    }

    [Fact]
    public void Search_through_a_null_link_does_not_throw()
    {
        var result = Schema.ExecuteInMemory(Employees, Request(search: new DataListSearch("white")));
        Assert.Equal([3], result.Ids());
    }
}
