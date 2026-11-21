using Blux.UI.Tools.Query;

// Exit code 0 when every check passes.
// Run from a NativeAOT publish: tests/Blux.UI.Tools.Query.AotSmoke/bin/Release/net10.0/<rid>/publish/Blux.UI.Tools.Query.AotSmoke

var failures = new List<string>();

void Check(string name, bool ok)
{
    Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {name}");
    if (!ok) failures.Add(name);
}

Person[] people =
[
    new(1, "Alice Johnson", 30, Role.Admin, new Team("Core"), ["a", "b"], true, new DateOnly(2020, 1, 15), DateTimeOffset.Parse("2024-03-31T00:30:00Z"), 10.5m),
    new(2, "bob smith", null, Role.User, null, [], false, new DateOnly(2021, 6, 1), null, 20m),
    new(3, "Carol White", 25, Role.User, new Team("Docs"), ["b"], true, new DateOnly(2019, 12, 31), DateTimeOffset.Parse("2024-03-30T23:30:00Z"), 5m),
    new(4, "Éric Durand", 30, Role.Guest, new Team("Core"), ["c"], false, new DateOnly(2022, 2, 2), DateTimeOffset.Parse("2024-04-01T10:00:00Z"), 7.25m),
];

var schema = DataListSchema.For<Person>(s => s
    .RowKey(p => p.Id)
    .Search(p => p.Name)
    .Field(p => p.Name, f => f.Sortable().Filter(DataListFilter.Text()))
    .Field(p => p.Age, f => f.Sortable().Filter(DataListFilter.Number(0, 1)))
    .Field(p => p.Role, f => f.Sortable().Filter(DataListFilter.Option()))
    .Field(p => p.Team!.Name, f => f.Filter(DataListFilter.Option()))
    .Field(p => p.Active, f => f.Sortable().Filter(DataListFilter.Boolean()))
    .Field(p => p.Hired, f => f.Sortable().Filter(DataListFilter.Date()))
    .Field(p => p.LastLogin, f => f.Sortable().Filter(DataListFilter.Date(DateGranularity.Hour)))
    .Field(p => p.Salary, f => f.Sortable().Filter(DataListFilter.Number()))
    .Collection(p => p.Tags, t => t, f => f.Key("tags").Filter(DataListFilter.Option()))
    .DefaultSort(p => p.Name));

var request = new DataListRequest(
    new DataListPaging.Offset(0, 10),
    [new DataListSort("age", Descending: true)],
    DataListFilterGroup.And(
        new FilterRule("role", FilterOperator.IsAnyOf, new FilterValue.List(["Admin", "User", "bogus"])),
        new FilterRule("tags", FilterOperator.IsAnyOf, new FilterValue.List(["b"])),
        new FilterRule("age", FilterOperator.Gte, new FilterValue.Number(25), IncludeEmpty: true)),
    new DataListSearch("a"),
    [new DataListFacetRequest("team.name", IncludeMissing: true)],
    timeZone: "Europe/Paris");

var clean = schema.Sanitize(request);
Check("sanitize keeps the rules", clean.Filter.Children.Count == 3);

var result = schema.ExecuteInMemory(people, request);
Check("filter, search and sort", result.Items.Select(p => p.Id).SequenceEqual([1, 3]));
Check("total", result.TotalCount == 2);
Check("facets", result.Facets is [{ Key: "team.name", Options: [{ Value: "Core", Count: 1 }, { Value: "Docs", Count: 1 }] }]);

var accents = schema.ExecuteInMemory(people, new DataListRequest(DataListPaging.None.Instance, [], DataListFilterGroup.And(
    new FilterRule("name", FilterOperator.Contains, new FilterValue.Text("eric")))));
Check("text contains ignores accents", accents.Items.Select(p => p.Id).SequenceEqual([4]));

var first = schema.ExecuteInMemory(people, new DataListRequest(new DataListPaging.Cursor(null, 2), [new DataListSort("age")], DataListFilterGroup.Empty));
var second = schema.ExecuteInMemory(people, new DataListRequest(new DataListPaging.Cursor(first.NextCursor, 2), [new DataListSort("age")], DataListFilterGroup.Empty));
Check("cursor pages", first.Items.Select(p => p.Id).SequenceEqual([3, 1]) && second.Items.Select(p => p.Id).SequenceEqual([4, 2]) && second.NextCursor is null);

var dates = schema.ExecuteInMemory(people, new DataListRequest(
    DataListPaging.None.Instance,
    [new DataListSort("hired", Descending: true)],
    DataListFilterGroup.And(
        new FilterRule("active", FilterOperator.Is, new FilterValue.Boolean(true)),
        new FilterRule("hired", FilterOperator.Before, new FilterValue.Date(new DateOnly(2021, 1, 1))),
        new FilterRule("lastLogin", FilterOperator.Is, new FilterValue.Date(new DateOnly(2024, 3, 31)))),
    timeZone: "Europe/Paris"));
Check("bool, date and day-in-zone filters", dates.Items.Select(p => p.Id).SequenceEqual([1, 3]));

var numbers = schema.ExecuteInMemory(people, new DataListRequest(
    DataListPaging.None.Instance,
    [new DataListSort("salary")],
    DataListFilterGroup.And(
        new FilterRule("salary", FilterOperator.Gt, new FilterValue.Number(5.5m)),
        new FilterRule("age", FilterOperator.Lt, new FilterValue.Number(30.5m), IncludeEmpty: true))));
Check("decimal and fractional filters", numbers.Items.Select(p => p.Id).SequenceEqual([4, 1, 2]) || numbers.Items.Select(p => p.Id).SequenceEqual([1, 4, 2]));

var byRole = schema.ExecuteInMemory(people, new DataListRequest(new DataListPaging.Cursor(null, 3), [new DataListSort("role", Descending: true), new DataListSort("active")], DataListFilterGroup.Empty));
var byRoleNext = schema.ExecuteInMemory(people, new DataListRequest(new DataListPaging.Cursor(byRole.NextCursor, 3), [new DataListSort("role", Descending: true), new DataListSort("active")], DataListFilterGroup.Empty));
Check("cursor on an enum and a bool", byRole.Items.Select(p => p.Id).SequenceEqual([4, 2, 3]) && byRoleNext.Items.Select(p => p.Id).SequenceEqual([1]));

var query = DataListStateQuery.ToQuery(schema.DefaultState, schema);
Check("state to query", query is not null);

var wire = new DataListRequest(
    new DataListPaging.Cursor("abc", 5, Count: true),
    [new DataListSort("age", Descending: true)],
    DataListFilterGroup.And(
        new FilterRule("active", FilterOperator.Is, new FilterValue.Boolean(true)),
        new FilterRule("name", FilterOperator.Contains, new FilterValue.Text("a")),
        new FilterRule("age", FilterOperator.Between, new FilterValue.Range(new FilterValue.Number(1), new FilterValue.Number(9))),
        new FilterRule("hired", FilterOperator.Is, new FilterValue.Date(new DateOnly(2020, 1, 15))),
        new FilterRule("lastLogin", FilterOperator.After, new FilterValue.Moment(DateTimeOffset.Parse("2024-03-31T00:30:00Z"))),
        new FilterRule("role", FilterOperator.IsAnyOf, new FilterValue.List(["Admin", "User"]))),
    new DataListSearch("a"),
    [new DataListFacetRequest("role", Limit: 5, IncludeMissing: true)],
    "Europe/Paris");
var json = System.Text.Json.JsonSerializer.Serialize(wire, DataListJsonContext.Default.DataListRequest);
var back = System.Text.Json.JsonSerializer.Deserialize(json, DataListJsonContext.Default.DataListRequest);
Check("json round trip of a request", back is not null && back.Equals(wire) && json.Contains("\"type\":\"boolean\""));
var facetJson = System.Text.Json.JsonSerializer.Serialize(new DataListFacetResult("role", [new DataListFacetOption("Admin", 1)], 0, 1) { Truncated = true }, DataListJsonContext.Default.DataListFacetResult);
Check("json of a truncated facet", facetJson.Contains("\"truncated\":true"));

Console.WriteLine(failures.Count == 0 ? "all checks passed" : $"{failures.Count} check(s) failed");
return failures.Count == 0 ? 0 : 1;

internal enum Role { Admin, User, Guest }

internal sealed record Team(string Name);

internal sealed record Person(int Id, string Name, int? Age, Role Role, Team? Team, string[] Tags, bool Active, DateOnly Hired, DateTimeOffset? LastLogin, decimal Salary);
