using System.Globalization;
using static Blux.UI.Tools.Query.Tests.TestData;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.Tests;

public class StateQueryTests
{
    private static readonly DataListSchema<Employee> UrlSchema = DataListSchema.For<Employee>(s => s
        .RowKey(e => e.Id)
        .Search(e => e.Name)
        .Field(e => e.Name, f => f.Sortable().Filter(DataListFilter.Text()))
        .Field(e => e.Email, f => f.Filter(WithEmpty(DataListFilter.Text())))
        .Field(e => e.Age, f => f.Sortable().Filter(DataListFilter.Number(0, 1)))
        .Field(e => e.Active, f => f.Filter(DataListFilter.Boolean()))
        .Field(e => e.Hired, f => f.Filter(DataListFilter.Date()))
        .Field(e => e.LastLogin, f => f.Filter(DataListFilter.Date(DateGranularity.Second)))
        .Field(e => e.Department!.Name, f => f.Filter(DataListFilter.Option()))
        .Field(e => e.Status, f => f.Sortable().Filter(DataListFilter.Option()))
        .QueryFilter("archived", DataListFilter.Boolean(false))
        .DefaultSort(e => e.Name)
        .DefaultPageSize(25)
        .MaxPageSize(100));

    private static DataListState Parse(params (string Key, string? Value)[] pairs) =>
        DataListStateQuery.FromQuery(pairs.Select(p => KeyValuePair.Create(p.Key, p.Value)), UrlSchema);

    private static IReadOnlyDictionary<string, string> Write(DataListState state, string? prefix = null) =>
        DataListStateQuery.ToQuery(state, UrlSchema, prefix);

    private static DataListState RoundTrip(DataListState state, string? prefix = null) =>
        DataListStateQuery.FromQuery(Write(state, prefix)!.Select(p => KeyValuePair.Create(p.Key, (string?)p.Value)), UrlSchema, prefix);

    [Fact]
    public void The_default_state_has_an_empty_query_and_an_empty_query_gives_the_default_state()
    {
        var defaults = UrlSchema.DefaultState;
        Assert.Empty(Write(defaults));
        Assert.Equal(defaults, Parse());
        Assert.Equal([new DataListSort("name")], defaults.Sort);
        Assert.Equal(25, defaults.PageSize);
        Assert.Equal([new FilterRule("archived", Op.Is, new FilterValue.Boolean(false))], defaults.Filters);
    }

    private static DataListState Rich() => new(
        "a,b~c%",
        [
            new FilterRule("name", Op.Contains, Text("x,y~z%")),
            new FilterRule("age", Op.Between, Range(Num(10), Num(20.5m))),
            new FilterRule("hired", Op.Between, Range(Day("2020-01-01"), null)),
            new FilterRule("lastLogin", Op.After, At("2024-03-02T10:30:00+02:00")),
            new FilterRule("status", Op.IsAnyOf, List("Active", "we,ird~"), IncludeEmpty: true),
            new FilterRule("department.name", Op.IsNot, List("Sales")),
            new FilterRule("active", Op.Is, new FilterValue.Boolean(true)),
            new FilterRule("email", Op.IsEmpty),
            new FilterRule("archived", Op.Is, new FilterValue.Boolean(true)),
        ],
        [new DataListSort("age", true), new DataListSort("name")],
        3, 50, DataListLayout.Grid);

    [Fact]
    public void A_rich_state_round_trips()
    {
        var state = Rich();
        Assert.Equal(state, RoundTrip(state));
        Assert.Equal(state, RoundTrip(state, "users"));
    }

    [Fact]
    public void The_written_keys_are_the_documented_ones()
    {
        var query = Write(Rich());
        Assert.Equal("a,b~c%", query["q"]);
        Assert.Equal("3", query["p"]);
        Assert.Equal("50", query["ps"]);
        Assert.Equal("-age|name", query["s"]);
        Assert.Equal("grid", query["l"]);
        Assert.Equal("x%2Cy%7Ez%25", query["f.name"]);
        Assert.Equal("10..20.5", query["f.age.between"]);
        Assert.Equal("2020-01-01..", query["f.hired.between"]);
        Assert.Equal("2024-03-02T10:30:00.0000000+02:00", query["f.lastLogin.after"]);
        Assert.Equal("Active,we%2Cird%7E,~", query["f.status"]);
        Assert.Equal("Sales", query["f.department.name.isNot"]);
        Assert.Equal("true", query["f.active"]);
        Assert.Equal("~", query["f.email.isEmpty"]);
        Assert.Equal("true", query["f.archived"]);
    }

    [Fact]
    public void Defaults_are_omitted()
    {
        var state = UrlSchema.DefaultState with { Page = 1, PageSize = 25, Search = "" };
        Assert.Empty(Write(state));
        var withSort = state with { Sort = [new DataListSort("name", true)] };
        Assert.Equal("-name", Write(withSort)["s"]);
        Assert.Equal("", Write(state with { Sort = [] })["s"]);
        Assert.Empty(Parse(("s", "")).Sort);
        var onlyPage = Write(state with { Page = 2 });
        Assert.Equal(["p"], onlyPage.Keys);
    }

    [Fact]
    public void The_page_is_left_out_in_cursor_mode_and_the_cursor_round_trips()
    {
        var state = UrlSchema.DefaultState with { Page = 4, Cursor = "abc_-9" };
        var query = Write(state);
        Assert.False(query.ContainsKey("p"));
        Assert.Equal("abc_-9", query["c"]);
        Assert.Equal("abc_-9", RoundTrip(state).Cursor);
    }

    [Fact]
    public void A_prefix_scopes_every_key_and_other_keys_are_ignored()
    {
        var state = UrlSchema.DefaultState with
        {
            Search = "x", Page = 2, Layout = DataListLayout.Grid,
            Filters = [new FilterRule("archived", Op.Is, new FilterValue.Boolean(false)), new FilterRule("age", Op.Gt, Num(1))],
        };
        var query = Write(state, "users");
        Assert.All(query.Keys, k => Assert.StartsWith("users.", k));
        Assert.Equal(["users.q", "users.p", "users.f.age.gt", "users.l"], query.Keys);

        var read = DataListStateQuery.FromQuery(
            query.Select(p => KeyValuePair.Create(p.Key, (string?)p.Value)).Append(KeyValuePair.Create("q", (string?)"other")).Append(KeyValuePair.Create("orders.p", (string?)"9")),
            UrlSchema, "users");
        Assert.Equal(state, read);
        Assert.Equal(UrlSchema.DefaultState, DataListStateQuery.FromQuery([KeyValuePair.Create("q", (string?)"x")], UrlSchema, "users"));
    }

    [Fact]
    public void A_default_filter_removed_by_the_user_is_written_as_an_empty_value_and_read_back()
    {
        var cleared = UrlSchema.DefaultState with { Filters = [] };
        var query = Write(cleared);
        Assert.Equal("", query["f.archived"]);
        Assert.Empty(RoundTrip(cleared).Filters);
        Assert.Equal(UrlSchema.DefaultState, Parse(("f.archived", null)));
    }

    [Fact]
    public void A_url_rule_replaces_the_default_rule_of_its_key()
    {
        var state = Parse(("f.archived", "true"));
        Assert.Equal([new FilterRule("archived", Op.Is, new FilterValue.Boolean(true))], state.Filters);
        var other = Parse(("f.archived.isNot", "false"));
        Assert.Equal([new FilterRule("archived", Op.IsNot, new FilterValue.Boolean(false))], other.Filters);
    }

    // ---- invalid input never throws and falls back to the defaults

    [Fact]
    public void Unknown_keys_and_invalid_operators_are_dropped()
    {
        var state = Parse(
            ("f.nope", "1"),
            ("f.age.bogus", "1"),
            ("f.age.contains", "3"),
            ("f.age.gt", "abc"),
            ("f.name.between", "1..2"),
            ("f.active", "maybe"),
            ("f.hired", "yesterday"),
            ("f.age.between", "5"),
            ("f.age.between", ".."),
            ("f.status.gt", "1"),
            ("f.age.isEmpty", "~"),
            ("f.name", "kept"),
            ("zzz", "1"));
        Assert.Equal(
            [new FilterRule("archived", Op.Is, new FilterValue.Boolean(false)), new FilterRule("name", Op.Contains, Text("kept"))],
            state.Filters);
    }

    [Fact]
    public void Operators_are_matched_ignoring_case_and_numeric_operators_are_not_accepted()
    {
        Assert.Equal(new FilterRule("age", Op.Gte, Num(3)), Parse(("f.age.GTE", "3")).Filters.Last());
        Assert.Equal(new FilterRule("age", Op.Gte, Num(3)), Parse(("f.age.gte", "3")).Filters.Last());
        Assert.DoesNotContain(Parse(("f.age.5", "3")).Filters, r => r.Key == "age");
        Assert.Equal(new FilterRule("active", Op.Is, new FilterValue.Boolean(true)), Parse(("f.active", "TRUE")).Filters.Last());
    }

    [Fact]
    public void The_first_of_repeated_keys_counts_and_one_rule_per_key_and_operator()
    {
        var state = Parse(("f.age", "30"), ("f.age", "31"), ("f.age.is", "32"));
        Assert.Equal([new FilterRule("age", Op.Is, Num(30))], state.Filters.Where(r => r.Key == "age"));
    }

    [Theory]
    [InlineData("-5", 1)]
    [InlineData("0", 1)]
    [InlineData("1", 1)]
    [InlineData("7", 7)]
    [InlineData("2000000", DataListState.MaxPage)]
    [InlineData("99999999999", 1)]
    [InlineData("abc", 1)]
    [InlineData("", 1)]
    [InlineData("3.5", 1)]
    public void The_page_is_clamped(string text, int expected) => Assert.Equal(expected, Parse(("p", text)).Page);

    [Theory]
    [InlineData("0", 1)]
    [InlineData("-3", 1)]
    [InlineData("10", 10)]
    [InlineData("100", 100)]
    [InlineData("100000", 100)]
    [InlineData("abc", 25)]
    [InlineData("", 25)]
    public void The_page_size_is_clamped_to_the_schema_maximum(string text, int expected) =>
        Assert.Equal(expected, Parse(("ps", text)).PageSize);

    [Theory]
    [InlineData("age", "age")]
    [InlineData("-age", "-age")]
    [InlineData("unknown|-age|age", "-age")]
    [InlineData("age|name", "age|name")]
    [InlineData(" age | -name ", "age|-name")]
    [InlineData("email", "name")]
    [InlineData("zzz", "name")]
    [InlineData("|", "name")]
    [InlineData("-", "name")]
    public void Sort_keeps_known_distinct_keys_and_falls_back_to_the_default(string text, string expected)
    {
        var sort = Parse(("s", text)).Sort;
        Assert.Equal(expected, string.Join('|', sort.Select(s => s.Descending ? "-" + s.Key : s.Key)));
    }

    [Theory]
    [InlineData("grid", DataListLayout.Grid)]
    [InlineData("GRID", DataListLayout.Grid)]
    [InlineData("table", DataListLayout.Table)]
    [InlineData("bogus", DataListLayout.Table)]
    [InlineData("1", DataListLayout.Table)]
    [InlineData("", DataListLayout.Table)]
    public void The_layout_falls_back_to_the_default(string text, DataListLayout expected) =>
        Assert.Equal(expected, Parse(("l", text)).Layout);

    [Fact]
    public void Invalid_rules_in_a_state_are_left_out_of_the_query()
    {
        var state = UrlSchema.DefaultState with
        {
            Filters = [new FilterRule("nope", Op.Is, Text("x")), new FilterRule("age", Op.Contains, Text("x")), new FilterRule("name", Op.Contains, Text("ok"))],
            Sort = [new DataListSort("email"), new DataListSort("age")],
            PageSize = 5000,
        };
        var query = Write(state);
        Assert.Equal("age", query["s"]);
        Assert.Equal("100", query["ps"]);
        Assert.Equal("ok", query["f.name"]);
        Assert.DoesNotContain(query.Keys, k => k.Contains("nope") || k.Contains("age.contains"));
    }

    [Fact]
    public void Operator_names_in_the_query_are_camel_case_for_every_operator()
    {
        var names = Enum.GetValues<Op>().Select(FilterRuleText.OperatorName).ToArray();
        Assert.Equal(
            ["contains", "is", "isAnyOf", "isNot", "gt", "gte", "lt", "lte", "between", "before", "after", "isEmpty", "isNotEmpty"],
            names);
    }

    // ---- value formats

    [Theory]
    [InlineData("plain")]
    [InlineData("a,b")]
    [InlineData("100%")]
    [InlineData("~")]
    [InlineData("~~")]
    [InlineData("a~b,c%2C")]
    [InlineData("%7E")]
    [InlineData("héllo wörld & co = 1 + 1")]
    [InlineData("..")]
    public void Text_values_round_trip_through_the_escaping(string value)
    {
        var state = UrlSchema.DefaultState with { Filters = [new FilterRule("name", Op.Is, Text(value))] };
        Assert.Equal(state.Filters, RoundTrip(state).Filters.Where(f => f.Key == "name"));
    }

    [Fact]
    public void Numbers_use_the_invariant_culture()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var state = UrlSchema.DefaultState with { Filters = [new FilterRule("age", Op.Gte, Num(1234.5m)), new FilterRule("age", Op.Lt, Num(-0.25m))] };
            var query = Write(state);
            Assert.Equal("1234.5", query["f.age.gte"]);
            Assert.Equal("-0.25", query["f.age.lt"]);
            Assert.Equal(state.Filters, RoundTrip(state).Filters.Where(f => f.Key == "age"));
            Assert.Equal(Num(1.5m), Parse(("f.age", "1.5")).Filters.Single(f => f.Key == "age").Value);
            Assert.DoesNotContain(Parse(("f.age", "1,5")).Filters, f => f.Key == "age" && f.Value is FilterValue.Number { Value: 1.5m });
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void Dates_and_instants_use_iso_formats()
    {
        var day = new FilterRule("hired", Op.Is, Day("2024-02-29"));
        var moment = new FilterRule("lastLogin", Op.Before, At("2024-02-29T23:59:59.1234567-05:00"));
        var state = UrlSchema.DefaultState with { Filters = [day, moment] };
        var query = Write(state);
        Assert.Equal("2024-02-29", query["f.hired"]);
        Assert.Equal("2024-02-29T23:59:59.1234567-05:00", query["f.lastLogin.before"]);
        Assert.Equal(state.Filters, RoundTrip(state).Filters.Where(f => f.Key is "hired" or "lastLogin"));
    }

    [Fact]
    public void Include_empty_is_a_bare_token_and_a_lone_token_means_only_empty_rows()
    {
        var onlyEmpty = new FilterRule("status", Op.IsAnyOf, List(), IncludeEmpty: true);
        var state = UrlSchema.DefaultState with { Filters = [onlyEmpty] };
        Assert.Equal("~", Write(state)["f.status"]);
        Assert.Equal([onlyEmpty], RoundTrip(state).Filters);

        var text = new FilterRule("email", Op.Contains, Text("acme"), IncludeEmpty: true);
        state = UrlSchema.DefaultState with { Filters = [text] };
        Assert.Equal("acme,~", Write(state)["f.email"]);
        Assert.Equal([text], RoundTrip(state).Filters);

        // a literal tilde in a value is escaped, so it never reads as the token
        var literal = new FilterRule("name", Op.Contains, Text("~"));
        Assert.Equal("%7E", Write(UrlSchema.DefaultState with { Filters = [literal] })["f.name"]);
    }

    [Fact]
    public void Ranges_may_have_one_bound()
    {
        var upper = new FilterRule("age", Op.Between, Range(null, Num(5)));
        var lower = new FilterRule("hired", Op.Between, Range(Day("2020-01-01"), null));
        var state = UrlSchema.DefaultState with { Filters = [upper, lower] };
        var query = Write(state);
        Assert.Equal("..5", query["f.age.between"]);
        Assert.Equal("2020-01-01..", query["f.hired.between"]);
        Assert.Equal([upper, lower], RoundTrip(state).Filters.Where(f => f.Key is "age" or "hired"));
    }

    [Fact]
    public void Option_is_with_one_value_reads_back_as_text_and_lists_stay_lists()
    {
        var one = new FilterRule("status", Op.Is, Text("Active"));
        var many = new FilterRule("status", Op.IsNot, List("Active", "Pending"));
        Assert.Equal([one], RoundTrip(UrlSchema.DefaultState with { Filters = [one] }).Filters);
        Assert.Equal([many], RoundTrip(UrlSchema.DefaultState with { Filters = [many] }).Filters);
    }

    [Fact]
    public void Filter_keys_with_dots_resolve_with_and_without_an_operator()
    {
        Assert.Equal(new FilterRule("department.name", Op.IsAnyOf, List("Sales", "IT")), Parse(("f.department.name", "Sales,IT")).Filters.Last());
        Assert.Equal(new FilterRule("department.name", Op.IsNot, List("Sales")), Parse(("f.department.name.isNot", "Sales")).Filters.Last());
    }

    [Fact]
    public void ToRequest_builds_offset_or_cursor_paging_and_hides_a_blank_search()
    {
        var state = UrlSchema.DefaultState with { Page = 3, PageSize = 20, Search = " ", Cursor = "c1" };
        var offset = state.ToRequest();
        Assert.Equal(new DataListPaging.Offset(40, 20), offset.Paging);
        Assert.Null(offset.Search);
        var cursor = state.ToRequest(cursorPaging: true);
        Assert.Equal(new DataListPaging.Cursor("c1", 20, true), cursor.Paging);
        Assert.Equal(state.Filters, cursor.Filter.Children);
    }

    // ---- a range sent by a GET form as two keys

    [Fact]
    public void A_range_sent_as_a_from_key_and_a_to_key_reads_as_between()
    {
        var number = Parse(("f.age.from", "20"), ("f.age.to", "30.5"));
        var date = Parse(("f.hired.from", "2020-01-01"), ("f.hired.to", "2020-12-31"));

        Assert.Equal(new FilterRule("age", Op.Between, new FilterValue.Range(new FilterValue.Number(20), new FilterValue.Number(30.5m))), number.Filters.Single(r => r.Key == "age"));
        Assert.Equal(new FilterRule("hired", Op.Between, new FilterValue.Range(new FilterValue.Date(new DateOnly(2020, 1, 1)), new FilterValue.Date(new DateOnly(2020, 12, 31)))), date.Filters.Single(r => r.Key == "hired"));
        // Written back, it is the key the app owns.
        Assert.Equal("20..30.5", Write(number)["f.age.between"]);
    }

    [Fact]
    public void An_empty_side_of_a_form_range_is_an_open_end_and_two_empty_sides_clear_the_filter()
    {
        var open = Parse(("f.age.from", ""), ("f.age.to", "30"));
        var none = Parse(("f.age.from", ""), ("f.age.to", ""));

        Assert.Equal(new FilterRule("age", Op.Between, new FilterValue.Range(null, new FilterValue.Number(30))), open.Filters.Single(r => r.Key == "age"));
        Assert.DoesNotContain(none.Filters, r => r.Key == "age");
    }

    [Fact]
    public void A_form_range_that_does_not_parse_or_whose_filter_has_no_between_is_dropped_and_between_wins()
    {
        Assert.DoesNotContain(Parse(("f.age.from", "x"), ("f.age.to", "3")).Filters, r => r.Key == "age");
        Assert.DoesNotContain(Parse(("f.name.from", "a"), ("f.name.to", "b")).Filters, r => r.Key == "name");
        Assert.DoesNotContain(Parse(("f.nothing.from", "1")).Filters, r => r.Key == "nothing");

        var both = Parse(("f.age.between", "1..2"), ("f.age.from", "5"), ("f.age.to", "9"));
        Assert.Equal(new FilterRule("age", Op.Between, new FilterValue.Range(new FilterValue.Number(1), new FilterValue.Number(2))), both.Filters.Single(r => r.Key == "age"));
    }

    [Fact]
    public void A_form_range_keeps_the_prefix_of_the_list()
    {
        var state = DataListStateQuery.FromQuery([KeyValuePair.Create("x.f.age.from", (string?)"3"), KeyValuePair.Create("f.age.from", (string?)"9")], UrlSchema, "x");

        Assert.Equal(new FilterRule("age", Op.Between, new FilterValue.Range(new FilterValue.Number(3), null)), state.Filters.Single(r => r.Key == "age"));
    }
}
