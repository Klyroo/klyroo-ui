using System.Text.Json.Serialization;
using Blux.UI.Tools.Query;

namespace Blux.Playground.Client.Examples;

/// <summary>A row of the data list examples.</summary>
public sealed record Invoice(int Id, string Customer, string Status, decimal Amount, DateOnly Issued, bool Reviewed = false);

/// <summary>The rows, the schema and the JSON metadata the data list examples share.</summary>
public static class InvoiceData
{
    private static readonly string[] Customers =
        ["Acme Corp", "Globex", "Initech", "Umbrella", "Hooli", "Stark Industries", "Wayne Enterprises", "Wonka", "Cyberdyne", "Soylent", "Tyrell", "Aperture"];

    private static readonly string[] Statuses = ["Paid", "Pending", "Overdue", "Draft"];

    /// <summary>120 invoices, the same on the server and in the browser.</summary>
    public static readonly Invoice[] All =
    [
        .. Enumerable.Range(1, 120).Select(i => new Invoice(
            i,
            Customers[(i - 1) % Customers.Length],
            Statuses[(i * 7) % Statuses.Length],
            Math.Round(40m + (i * 37 % 500) * 1.25m, 2),
            new DateOnly(2026, 1, 1).AddDays(i * 2),
            i % 3 == 0)),
    ];

    /// <summary>Which fields search and sort, the key of a row, the default sort and page size.</summary>
    public static readonly DataListSchema<Invoice> Schema = DataListSchema.For<Invoice>(s => s
        .RowKey(i => i.Id)
        .RowLabel(i => $"Invoice {i.Id}")
        .Search(i => i.Customer, i => i.Status)
        .Field(i => i.Id, f => f.Sortable())
        .Field(i => i.Customer, f => f.Sortable())
        .Field(i => i.Status, f => f.Sortable())
        .Field(i => i.Amount, f => f.Sortable())
        .Field(i => i.Issued, f => f.Sortable())
        .DefaultSort(i => i.Id)
        .DefaultPageSize(10));
}

/// <summary>The schema of the filter examples: a text filter on the customer, an option filter on the status (declared options, counts) and a boolean one.</summary>
public static class InvoiceFilterData
{
    /// <summary>The invoice schema of <see cref="InvoiceData"/> plus filters: customer (text), status (options with counts) and reviewed (yes / no).</summary>
    public static readonly DataListSchema<Invoice> Schema = DataListSchema.For<Invoice>(s => s
        .RowKey(i => i.Id)
        .RowLabel(i => $"Invoice {i.Id}")
        .Search(i => i.Customer, i => i.Status)
        .Field(i => i.Id, f => f.Sortable())
        .Field(i => i.Customer, f => f.Sortable().Filter(DataListFilter.Text().WithLabel("Customer")))
        .Field(i => i.Status, f => f.Sortable().Filter(DataListFilter.Option(
            new DataListFilterOption("Paid", "Paid"), new DataListFilterOption("Pending", "Pending"),
            new DataListFilterOption("Overdue", "Overdue"), new DataListFilterOption("Draft", "Draft")).WithLabel("Status").WithFacets()))
        .Field(i => i.Amount, f => f.Sortable())
        .Field(i => i.Issued, f => f.Sortable())
        .Field(i => i.Reviewed, f => f.Filter(DataListFilter.Boolean().WithLabel("Reviewed")))
        .DefaultSort(i => i.Id)
        .DefaultPageSize(10));
}

/// <summary>
/// The metadata that serializes the result of a prerender: a data list with <c>ResultJson</c> hands its first result to the interactive render, which shows it
/// without loading it again. The attributes mirror the ones of <c>DataListJsonContext</c>.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DataListResult<Invoice>))]
public sealed partial class DemoJson : JsonSerializerContext
{
}

/// <summary>
/// A test hook: counts the calls a data list example makes to its provider and keeps the time zone the last request carried. One per circuit, browser tab
/// or request, so a list that shows the result of its prerender (and loads nothing) leaves it at zero.
/// </summary>
public sealed class ProviderCallLog
{
    /// <summary>The calls so far.</summary>
    public int Count { get; private set; }

    /// <summary>The time zone of the last request; <see langword="null"/> when it went out in UTC.</summary>
    public string? LastTimeZone { get; private set; }

    /// <summary>Raised after each call, for the example to show it.</summary>
    public event Action? Changed;

    /// <summary>Records one call.</summary>
    public void Add(DataListRequest request)
    {
        Count++;
        LastTimeZone = request.TimeZone;
        Changed?.Invoke();
    }
}
