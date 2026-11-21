using Microsoft.EntityFrameworkCore;

namespace Blux.UI.Tools.Query.EfTests;

public static class Schemas
{
    public static DataListFilter WithEmpty(DataListFilter filter) =>
        filter.WithOperators([.. filter.AllowedOperators, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty]);

    public static DataListSchema<Employee> Employees { get; } = DataListSchema.For<Employee>(s => s
        .RowKey(e => e.Id)
        .RowLabel(e => e.Name)
        .Search(e => e.Name, e => e.Email, e => e.Department!.Name)
        .SearchAny(e => e.Tags, t => t.Name)
        .Field(e => e.Id, f => f.Filter(DataListFilter.Option()))
        .Field(e => e.Name, f => f.Sortable().Filter(DataListFilter.Text()))
        .Field(e => e.Email, f => f.Filter(WithEmpty(DataListFilter.Text())))
        .Field(e => e.Age, f => f.Sortable().Filter(WithEmpty(DataListFilter.Number(0, 1))))
        .Field(e => e.Salary, f => f.Sortable().Filter(DataListFilter.Number()))
        .Field(e => e.Score, f => f.Sortable().Filter(DataListFilter.Number()))
        .Field(e => e.Active, f => f.Sortable().Filter(DataListFilter.Boolean()))
        .Field(e => e.Hired, f => f.Sortable().Filter(DataListFilter.Date()))
        .Field(e => e.LastLogin, f => f.Sortable().Filter(WithEmpty(DataListFilter.Date(DateGranularity.Second))))
        .Field(e => e.Department!.Name, f => f.Sortable().Filter(WithEmpty(DataListFilter.Option())))
        .Field(e => e.Status, f => f.Sortable().Filter(DataListFilter.Option()))
        .Collection(e => e.Tags, t => t.Name, f => f.Filter(WithEmpty(DataListFilter.Option())))
        .Collection(e => e.Tags, t => t.Name, f => f.Key("tagText").Filter(DataListFilter.Text()))
        .Collection(e => e.Tags, t => t.Weight, f => f.Filter(DataListFilter.Number()))
        .QueryFilter("archived", DataListFilter.Boolean(false))
        .MaxPageSize(500));

    public static DataListSchema<Activity> Activities { get; } = DataListSchema.For<Activity>(s => s
        .RowKey(e => e.Id)
        .Field(e => e.When, f => f.Sortable().Filter(DataListFilter.Date(DateGranularity.Hour)))
        .Field(e => e.At, f => f.Filter(DataListFilter.Date(DateGranularity.Hour)))
        .Field(e => e.On, f => f.Filter(DataListFilter.Date())));
}

/// <summary>Request builders and the two ways to answer a request: through the database, and in memory over the same rows.</summary>
public static class H
{
    public static FilterRule Rule(string key, FilterOperator op, FilterValue? value = null, bool includeEmpty = false) => new(key, op, value, includeEmpty);

    public static FilterValue Text(string v) => new FilterValue.Text(v);
    public static FilterValue Num(decimal v) => new FilterValue.Number(v);
    public static FilterValue Bool(bool v) => new FilterValue.Boolean(v);
    public static FilterValue List(params string[] v) => new FilterValue.List(v);
    public static FilterValue Day(string iso) => new FilterValue.Date(DateOnly.Parse(iso));
    public static FilterValue At(string iso) => new FilterValue.Moment(DateTimeOffset.Parse(iso));
    public static FilterValue Range(FilterValue? from, FilterValue? to) => new FilterValue.Range(from, to);

    public static DataListRequest Req(
        FilterRule? rule = null,
        DataListPaging? paging = null,
        DataListSort[]? sort = null,
        string? search = null,
        DataListFacetRequest[]? facets = null,
        string? zone = null,
        DataListFilterGroup? filter = null) =>
        new(paging ?? DataListPaging.None.Instance, sort ?? [], filter ?? (rule is null ? DataListFilterGroup.Empty : DataListFilterGroup.And(rule)), search is null ? null : new DataListSearch(search), facets, zone);

    public static DataListSort[] Sort(params (string Key, bool Desc)[] s) => [.. s.Select(x => new DataListSort(x.Key, x.Desc))];

    /// <summary>The rows loaded from the database: what the in-memory executor runs on, so both sides read the same values (converters included).</summary>
    public static List<Employee> Load(QueryContext db) =>
        [.. db.Employees.AsNoTracking().Include(e => e.Department).Include(e => e.Tags).OrderBy(e => e.Id)];

    public static List<Activity> LoadActivities(QueryContext db) => [.. db.Activities.AsNoTracking().OrderBy(e => e.Id)];

    /// <summary>The whole answer through the database: page, total, facets, next cursor.</summary>
    public static DataListResult<T> Run<T>(DataListSchema<T> schema, IQueryable<T> source, DataListRequest request)
    {
        var query = schema.Apply(source, request);
        var rows = query.Rows.ToList();
        int? total = query.WantsTotal ? query.Matching.Count() : null;
        IReadOnlyList<DataListFacetResult>? facets = null;
        var facetQueries = schema.FacetQueries(source, request);
        if (facetQueries.Count > 0)
            facets = [.. facetQueries.Select(q => q.ToResult(q.Buckets.ToList(), q.Missing?.Count()))];
        return schema.ToResult(request, rows, total, facets);
    }

    /// <summary>No Include: the sort values of a cursor come from the database (<see cref="DataListQuery{T}.Rows"/>), not from the loaded entity.</summary>
    public static DataListResult<Employee> Run(QueryContext db, DataListRequest request) => Run(Schemas.Employees, db.Employees.AsNoTracking(), request);

    public static DataListResult<Employee> Memory(List<Employee> rows, DataListRequest request) => Schemas.Employees.ExecuteInMemory(rows, request);

    public static int[] Ids(this DataListResult<Employee> r) => [.. r.Items.Select(e => e.Id)];

    /// <summary>Asserts that the database and the in-memory executor give the same page, total, next cursor and facets.</summary>
    public static void AssertSame(QueryContext db, List<Employee> rows, DataListRequest request)
    {
        var expected = Memory(rows, request);
        var actual = Run(db, request);
        Assert.Equal(expected.Ids(), actual.Ids());
        Assert.Equal(expected.TotalCount, actual.TotalCount);
        Assert.Equal(expected.NextCursor, actual.NextCursor);
        Assert.Equal(expected.Facets, actual.Facets);
    }
}
