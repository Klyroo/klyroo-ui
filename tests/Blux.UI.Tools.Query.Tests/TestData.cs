namespace Blux.UI.Tools.Query.Tests;

public enum Status { Active, Pending, Archived }

public sealed record Dept(string Name);

public sealed record Employee(
    int Id,
    string Name,
    string? Email,
    int? Age,
    decimal Salary,
    bool Active,
    DateOnly Hired,
    DateTimeOffset? LastLogin,
    Dept? Department,
    Status Status,
    double Score = 0);

internal static class TestData
{
    public static readonly Employee[] Employees =
    [
        new(1, "Alice Johnson", "alice@acme.io", 30, 5000.50m, true, new DateOnly(2020, 1, 15), DateTimeOffset.Parse("2024-03-01T10:00:00Z"), new Dept("Engineering"), Status.Active, 1.5),
        new(2, "bob smith", null, 25, 4200m, true, new DateOnly(2021, 6, 1), null, new Dept("Sales"), Status.Pending, 2.5),
        new(3, "Carol White", "", null, 6100m, false, new DateOnly(2019, 12, 31), DateTimeOffset.Parse("2024-03-02T00:00:00Z"), null, Status.Archived, 3),
        new(4, "dave brown", "dave@acme.io", 41, 7000m, true, new DateOnly(2020, 1, 15), DateTimeOffset.Parse("2024-03-02T23:59:59Z"), new Dept("Engineering"), Status.Active, 0.5),
        new(5, "Éric Durand", "eric@x.fr", 30, 3900.25m, false, new DateOnly(2022, 2, 2), DateTimeOffset.Parse("2024-03-03T00:00:00Z"), new Dept("Sales"), Status.Pending, 2),
        new(6, "Frank O'Neil", null, 18, 100m, true, new DateOnly(2023, 12, 31), DateTimeOffset.Parse("2024-02-29T12:00:00+02:00"), new Dept(""), Status.Active, 9),
    ];

    public static DataListSchema<Employee> Schema { get; } = DataListSchema.For<Employee>(s => s
        .RowKey(e => e.Id)
        .RowLabel(e => e.Name)
        .Search(e => e.Name, e => e.Email, e => e.Department!.Name)
        .Field(e => e.Id, f => f.Filter(DataListFilter.Option()))
        .Field(e => e.Name, f => f.Sortable().Filter(DataListFilter.Text()))
        .Field(e => e.Email, f => f.Filter(WithEmpty(DataListFilter.Text())))
        .Field(e => e.Age, f => f.Sortable().Filter(WithEmpty(DataListFilter.Number(0, 1))))
        .Field(e => e.Salary, f => f.Sortable().Filter(DataListFilter.Number()))
        .Field(e => e.Score, f => f.Filter(DataListFilter.Number()))
        .Field(e => e.Active, f => f.Filter(DataListFilter.Boolean()))
        .Field(e => e.Hired, f => f.Sortable().Filter(DataListFilter.Date()))
        .Field(e => e.LastLogin, f => f.Filter(WithEmpty(DataListFilter.Date(DateGranularity.Second))))
        .Field(e => e.Department!.Name, f => f.Filter(WithEmpty(DataListFilter.Option())))
        .Field(e => e.Status, f => f.Sortable().Filter(DataListFilter.Option()))
        .QueryFilter("archived", DataListFilter.Boolean(false))
        .MaxPageSize(500));

    /// <summary>The filter with <c>isEmpty</c> and <c>isNotEmpty</c> added: the per-kind sets of the spec do not include them.</summary>
    public static DataListFilter WithEmpty(DataListFilter filter) =>
        filter.WithOperators([.. filter.AllowedOperators, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty]);

    public static FilterRule Rule(string key, FilterOperator op, FilterValue? value = null, bool includeEmpty = false) =>
        new(key, op, value, includeEmpty);

    public static DataListRequest Request(DataListFilterGroup? filter = null, DataListSearch? search = null, params DataListSort[] sort) =>
        new(DataListPaging.Unpaged, sort, filter ?? DataListFilterGroup.Empty, search);

    public static int[] Ids(this DataListResult<Employee> result) => [.. result.Items.Select(e => e.Id)];

    public static FilterValue Text(string v) => new FilterValue.Text(v);
    public static FilterValue Num(decimal v) => new FilterValue.Number(v);
    public static FilterValue List(params string[] v) => new FilterValue.List(v);
    public static FilterValue Day(string iso) => new FilterValue.Date(DateOnly.Parse(iso));
    public static FilterValue At(string iso) => new FilterValue.Moment(DateTimeOffset.Parse(iso));
    public static FilterValue Range(FilterValue? from, FilterValue? to) => new FilterValue.Range(from, to);
}
