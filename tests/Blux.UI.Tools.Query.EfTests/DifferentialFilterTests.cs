using static Blux.UI.Tools.Query.EfTests.H;
using Op = Blux.UI.Tools.Query.FilterOperator;

namespace Blux.UI.Tools.Query.EfTests;

/// <summary>
/// Differential tests: every operator of every filter kind, search, groups and collection fields run against SQLite through the applier and in memory through
/// the executor, over the same 130 rows, and must select the same rows. Text terms are chosen so that case and accents cannot matter (they follow the
/// database, see <see cref="CollationTests"/>).
/// </summary>
[Collection("Database")]
public class DifferentialFilterTests
{
    private static readonly Dictionary<string, DataListRequest> Cases = Build();

    public static TheoryData<string> Names() => [.. Cases.Keys];

    private static Dictionary<string, DataListRequest> Build()
    {
        var cases = new Dictionary<string, DataListRequest>();
        void Add(string name, FilterRule rule, string? zone = null, string? search = null) => cases[name] = Req(rule, zone: zone, search: search);

        // text
        Add("text contains", Rule("name", Op.Contains, Text("Young")));
        Add("text contains none", Rule("name", Op.Contains, Text("Zzzz")));
        Add("text is", Rule("name", Op.Is, Text("bob smith")));
        Add("text isNot", Rule("name", Op.IsNot, Text("bob smith")));
        Add("text email contains", Rule("email", Op.Contains, Text("corp.test")));
        Add("text email is empty string", Rule("email", Op.Is, Text("")));
        Add("text email isEmpty", Rule("email", Op.IsEmpty));
        Add("text email isNotEmpty", Rule("email", Op.IsNotEmpty));
        Add("text email isNot includeEmpty", Rule("email", Op.IsNot, Text("alice@acme.io"), includeEmpty: true));
        Add("text with a percent and an underscore", Rule("name", Op.Contains, Text("50%_off")));

        // option
        Add("option id isAnyOf", Rule("id", Op.IsAnyOf, List("1", "2", "3", "99", "128", "x")));
        Add("option id is", Rule("id", Op.Is, Text("42")));
        Add("option id isNot", Rule("id", Op.IsNot, Text("42")));
        Add("option status isAnyOf", Rule("status", Op.IsAnyOf, List("Active", "pending")));
        Add("option status is", Rule("status", Op.Is, Text("Archived")));
        Add("option status isNot", Rule("status", Op.IsNot, Text("Active")));
        Add("option dept isAnyOf", Rule("department.name", Op.IsAnyOf, List("Sales", "Support")));
        Add("option dept isNot keeps rows without a department", Rule("department.name", Op.IsNot, Text("Sales")));
        Add("option dept isNot list", Rule("department.name", Op.IsNot, List("Sales", "Engineering")));
        Add("option dept isEmpty", Rule("department.name", Op.IsEmpty));
        Add("option dept isNotEmpty", Rule("department.name", Op.IsNotEmpty));
        Add("option dept includeEmpty", Rule("department.name", Op.IsAnyOf, List("Sales"), includeEmpty: true));
        Add("option dept empty value", Rule("department.name", Op.Is, Text("")));
        Add("option empty list", Rule("id", Op.IsAnyOf, List()));

        // boolean
        Add("bool is true", Rule("active", Op.Is, Bool(true)));
        Add("bool is false", Rule("active", Op.Is, Bool(false)));
        Add("bool isNot true", Rule("active", Op.IsNot, Bool(true)));

        // number: int?, decimal, double
        Add("number is", Rule("age", Op.Is, Num(30)));
        Add("number isNot keeps null ages", Rule("age", Op.IsNot, Num(30)));
        Add("number gt", Rule("age", Op.Gt, Num(30)));
        Add("number gte", Rule("age", Op.Gte, Num(30)));
        Add("number lt", Rule("age", Op.Lt, Num(30)));
        Add("number lte", Rule("age", Op.Lte, Num(30)));
        Add("number between", Rule("age", Op.Between, Range(Num(25), Num(40))));
        Add("number between open start", Rule("age", Op.Between, Range(null, Num(25))));
        Add("number between open end", Rule("age", Op.Between, Range(Num(40), null)));
        Add("number isEmpty", Rule("age", Op.IsEmpty));
        Add("number isNotEmpty", Rule("age", Op.IsNotEmpty));
        Add("number includeEmpty", Rule("age", Op.Gt, Num(55), includeEmpty: true));
        Add("number fraction is", Rule("age", Op.Is, Num(30.5m)));
        Add("number fraction isNot", Rule("age", Op.IsNot, Num(30.5m)));
        Add("number fraction gt", Rule("age", Op.Gt, Num(30.5m)));
        Add("number fraction gte", Rule("age", Op.Gte, Num(30.5m)));
        Add("number fraction lt", Rule("age", Op.Lt, Num(30.5m)));
        Add("number fraction lte", Rule("age", Op.Lte, Num(30.5m)));
        Add("number beyond int", Rule("age", Op.Lt, Num(99999999999m)));
        Add("decimal is", Rule("salary", Op.Is, Num(4200)));
        Add("decimal with cents is", Rule("salary", Op.Is, Num(5000.50m)));
        Add("decimal gt", Rule("salary", Op.Gt, Num(5000)));
        Add("decimal between", Rule("salary", Op.Between, Range(Num(4000), Num(6100))));
        Add("decimal lte", Rule("salary", Op.Lte, Num(3900.25m)));
        Add("double is", Rule("score", Op.Is, Num(0.5m)));
        Add("double gte", Rule("score", Op.Gte, Num(2.5m)));
        Add("double between", Rule("score", Op.Between, Range(Num(1), Num(3))));

        // date: DateOnly and DateTimeOffset?
        Add("date is", Rule("hired", Op.Is, Day("2020-01-15")));
        Add("date isNot", Rule("hired", Op.IsNot, Day("2020-01-15")));
        Add("date before", Rule("hired", Op.Before, Day("2020-01-15")));
        Add("date after", Rule("hired", Op.After, Day("2022-02-02")));
        Add("date between", Rule("hired", Op.Between, Range(Day("2019-12-31"), Day("2021-06-01"))));
        Add("date between open end", Rule("hired", Op.Between, Range(Day("2023-12-31"), null)));
        Add("date the last day of time", Rule("hired", Op.After, Day("9999-12-31")));
        Add("moment is the whole second", Rule("lastLogin", Op.Is, At("2024-03-02T23:59:59.5Z")));
        Add("moment isNot", Rule("lastLogin", Op.IsNot, At("2024-03-02T23:59:59Z")));
        Add("moment before", Rule("lastLogin", Op.Before, At("2024-03-02T00:00:00Z")));
        Add("moment after", Rule("lastLogin", Op.After, At("2024-03-02T00:00:00Z")));
        Add("moment between", Rule("lastLogin", Op.Between, Range(At("2024-03-02T00:00:00Z"), At("2024-03-02T23:59:59Z"))));
        Add("moment isEmpty", Rule("lastLogin", Op.IsEmpty));
        Add("moment isNotEmpty", Rule("lastLogin", Op.IsNotEmpty));
        Add("moment includeEmpty", Rule("lastLogin", Op.After, At("2024-03-02T00:00:00Z"), includeEmpty: true));
        Add("day on an instant, UTC", Rule("lastLogin", Op.Is, Day("2024-03-02")));
        Add("day on an instant, Paris", Rule("lastLogin", Op.Is, Day("2024-03-02")), zone: "Europe/Paris");
        Add("day before on an instant, New York", Rule("lastLogin", Op.Before, Day("2024-03-02")), zone: "America/New_York");
        Add("day between on an instant, Kolkata", Rule("lastLogin", Op.Between, Range(Day("2024-03-01"), Day("2024-03-02"))), zone: "Asia/Kolkata");
        Add("day after on an instant, Paris", Rule("lastLogin", Op.After, Day("2024-03-01")), zone: "Europe/Paris");

        // collections
        Add("tags isAnyOf", Rule("tags.name", Op.IsAnyOf, List("dotnet")));
        Add("tags is is case sensitive", Rule("tags.name", Op.Is, Text("Blazor")));
        Add("tags isAnyOf several", Rule("tags.name", Op.IsAnyOf, List("dotnet", "sales")));
        Add("tags isNot means no element", Rule("tags.name", Op.IsNot, List("dotnet")));
        Add("tags isEmpty", Rule("tags.name", Op.IsEmpty));
        Add("tags isNotEmpty", Rule("tags.name", Op.IsNotEmpty));
        Add("tags includeEmpty", Rule("tags.name", Op.IsAnyOf, List("sales"), includeEmpty: true));
        Add("tags only empty", Rule("tags.name", Op.IsAnyOf, List(), includeEmpty: true));
        Add("tags text contains", Rule("tagText", Op.Contains, Text("lazor")));
        Add("tags text is", Rule("tagText", Op.Is, Text("dotnet")));
        Add("tags text isNot", Rule("tagText", Op.IsNot, Text("dotnet")));
        Add("tags number gt", Rule("tags.weight", Op.Gt, Num(8)));
        Add("tags number between", Rule("tags.weight", Op.Between, Range(Num(3), Num(4))));
        Add("tags number is zero", Rule("tags.weight", Op.Is, Num(0)));
        Add("tags number isNot", Rule("tags.weight", Op.IsNot, Num(5)));

        // search: accent and case free terms
        Add("filter and search", Rule("active", Op.IsNot, Bool(true)), search: "Young");
        cases["search alone"] = Req(search: "corp.test");
        cases["search in the department"] = Req(search: "Support");
        cases["search in the tags"] = Req(search: "css");
        cases["search without hit"] = Req(search: "Qqqq");
        cases["search with wildcards"] = Req(search: "%");
        cases["search trims"] = Req(search: "  Young  ");

        // trees
        cases["or group"] = Req(filter: new DataListFilterGroup(FilterCombinator.Or,
            [Rule("status", Op.Is, Text("Archived")), Rule("age", Op.Gt, Num(55)), Rule("tags.name", Op.Is, Text("css"))]));
        cases["nested groups"] = Req(filter: new DataListFilterGroup(FilterCombinator.And,
        [
            Rule("active", Op.Is, Bool(true)),
            new DataListFilterGroup(FilterCombinator.Or, [Rule("age", Op.Lt, Num(25)), Rule("department.name", Op.IsEmpty)]),
        ]));
        cases["and of several kinds with search"] = Req(
            filter: DataListFilterGroup.And(Rule("status", Op.IsNot, Text("Pending")), Rule("hired", Op.After, Day("2021-01-01")), Rule("salary", Op.Gte, Num(2000))),
            search: "ng");
        cases["query-only filters are skipped"] = Req(filter: DataListFilterGroup.And(Rule("archived", Op.Is, Bool(true)), Rule("status", Op.Is, Text("Active"))));
        cases["unknown keys and bad values are dropped"] = Req(filter: DataListFilterGroup.And(Rule("nope", Op.Is, Text("x")), Rule("age", Op.Contains, Text("x")), Rule("age", Op.Gt, Num(40))));
        return cases;
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void The_database_selects_the_same_rows_as_the_executor(string name)
    {
        using var db = Database.Binary();
        var request = Cases[name];
        var expected = Memory(Load(db), request);
        var actual = Run(db, request);
        Assert.Equal(expected.Ids(), actual.Ids());
        Assert.Equal(expected.TotalCount, actual.TotalCount);
    }

    [Fact]
    public void The_cases_are_not_vacuous()
    {
        using var db = Database.Binary();
        var rows = Load(db);
        var sizes = Cases.ToDictionary(c => c.Key, c => Memory(rows, c.Value).Items.Count);
        // a case that selects nothing, or everything, proves little: only these are meant to
        HashSet<string> none = ["date the last day of time", "search with wildcards", "search without hit", "text contains none", "text with a percent and an underscore", "tags only empty", "number fraction is"];
        HashSet<string> all = ["query-only filters are skipped", "number beyond int", "text email is empty string", "option dept empty value", "option empty list", "number fraction isNot"];
        Assert.Empty(sizes.Where(s => s.Value == 0 && !none.Contains(s.Key)).Select(s => s.Key));
        Assert.Empty(sizes.Where(s => s.Value == rows.Count && !all.Contains(s.Key)).Select(s => s.Key));
    }
}
