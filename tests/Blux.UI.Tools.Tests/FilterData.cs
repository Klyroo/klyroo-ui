using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

public sealed record Ticket(int Id, string Title, string? Status, bool Active, string Owner);

/// <summary>40 tickets and a schema with a text filter (title), option filters (status with options of its own, owner with counts only) and a boolean one (active).</summary>
public static class FilterData
{
    private static readonly string?[] Statuses = ["open", "paused", "closed", "archived", null];
    private static readonly string[] Owners = ["ann", "bob", "cyd"];

    public static readonly Ticket[] All =
        [.. Enumerable.Range(1, 40).Select(i => new Ticket(i, $"Ticket {i:00} {Owners[i % 3]}", Statuses[i % 5], i % 3 == 0, Owners[i % 3]))];

    public static DataListSchema<Ticket> Schema { get; } = DataListSchema.For<Ticket>(s => s
        .RowKey(t => t.Id)
        .Search(t => t.Title)
        .Field(t => t.Id, f => f.Sortable())
        .Field(t => t.Title, f => f.Sortable().Filter(DataListFilter.Text().WithLabel("Title")))
        .Field(t => t.Status, f => f.Filter(DataListFilter.Option(
            new DataListFilterOption("open", "Open"), new DataListFilterOption("paused", "Paused"),
            new DataListFilterOption("closed", "Closed"), new DataListFilterOption("archived", "Archived")).WithLabel("Status").WithFacets()))
        .Field(t => t.Active, f => f.Filter(DataListFilter.Boolean().WithLabel("Active")))
        .Field(t => t.Owner, f => f.Filter(DataListFilter.Option().WithFacets()))
        .DefaultSort(t => t.Id)
        .DefaultPageSize(10));
}
