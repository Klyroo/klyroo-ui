using Blux.UI.Tools.Query;

namespace Blux.Playground.Client.Examples;

/// <summary>
/// The schema of the facets example: counts next to the options of three filters. The customer options come from the counts (most counted first, five before "Show more"), the status options are
/// declared and sorted by count, and Reviewed shows how many invoices are Yes and No. Every count leaves out the rule of its own filter, so the other options stay visible.
/// </summary>
public static class InvoiceFacetData
{
    /// <summary>The invoice schema with a customer filter (counts, sorted by count, limit of five), a status filter (declared options, counts, sorted by count) and a reviewed filter (counts).</summary>
    public static readonly DataListSchema<Invoice> Schema = DataListSchema.For<Invoice>(s => s
        .RowKey(i => i.Id)
        .RowLabel(i => $"Invoice {i.Id}")
        .Search(i => i.Customer, i => i.Status)
        .Field(i => i.Id, f => f.Sortable())
        .Field(i => i.Customer, f => f.Sortable().Filter(DataListFilter.Option().WithLabel("Customer").WithFacets().WithOptionSort(DataListOptionSort.Count).WithOptionLimit(5)))
        .Field(i => i.Status, f => f.Sortable().Filter(DataListFilter.Option(
            new DataListFilterOption("Paid", "Paid"), new DataListFilterOption("Pending", "Pending"),
            new DataListFilterOption("Overdue", "Overdue"), new DataListFilterOption("Draft", "Draft")).WithLabel("Status").WithFacets().WithOptionSort(DataListOptionSort.Count)))
        .Field(i => i.Amount, f => f.Sortable())
        .Field(i => i.Issued, f => f.Sortable())
        .Field(i => i.Reviewed, f => f.Filter(DataListFilter.Boolean().WithLabel("Reviewed").WithFacets()))
        .DefaultSort(i => i.Id)
        .DefaultPageSize(10));
}

/// <summary>
/// A customer directory that answers after a delay, as a remote service would: a page of customers for a search, and the labels of customers by value. The value of an option is the customer name that the
/// invoices carry; the label adds the city, so a tag that shows a label proves the resolver ran. <see cref="Broken"/> makes every call fail.
/// </summary>
public static class CustomerDirectory
{
    private static readonly (string Name, string City)[] Customers =
    [
        ("Acme Corp", "Phoenix"), ("Globex", "Cypress Creek"), ("Initech", "Austin"), ("Umbrella", "Raccoon City"), ("Hooli", "Palo Alto"), ("Stark Industries", "New York"),
        ("Wayne Enterprises", "Gotham"), ("Wonka", "London"), ("Cyberdyne", "Sunnyvale"), ("Soylent", "Chicago"), ("Tyrell", "Los Angeles"), ("Aperture", "Cleveland"),
    ];

    /// <summary>The time every call takes.</summary>
    public const int LatencyMilliseconds = 400;

    private static DataListFilterOption Option((string Name, string City) customer) => new(customer.Name, $"{customer.Name} ({customer.City})");

    /// <summary>One page of customers whose name contains the search, after a delay.</summary>
    /// <param name="request">The search and the page.</param>
    /// <param name="broken">Whether the directory is down.</param>
    /// <param name="cancellationToken">Cancelled when the request is stale.</param>
    public static async ValueTask<DataListOptionsPage> SearchAsync(DataListOptionsRequest request, bool broken, CancellationToken cancellationToken)
    {
        await Task.Delay(LatencyMilliseconds, cancellationToken);
        if (broken) throw new InvalidOperationException("The customer directory is down.");
        var matching = Customers.Where(c => request.Search.Length == 0 || c.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        var page = matching.Skip(request.Skip).Take(request.Take).Select(Option).ToList();
        return new DataListOptionsPage(page, HasMore: request.Skip + request.Take < matching.Count);
    }

    /// <summary>The labels of customers by name, after a delay: what a tag shows for a value restored from the address.</summary>
    /// <param name="values">The customer names.</param>
    /// <param name="cancellationToken">Cancelled when the list goes away.</param>
    public static async ValueTask<IReadOnlyList<DataListFilterOption>> ResolveAsync(IReadOnlyList<string> values, CancellationToken cancellationToken)
    {
        await Task.Delay(LatencyMilliseconds / 2, cancellationToken);
        return [.. Customers.Where(c => values.Contains(c.Name)).Select(Option)];
    }
}

/// <summary>The schema of the remote options example: the customer filter loads its options from <see cref="CustomerDirectory"/> (five per page, with a search) and counts them.</summary>
public static class InvoiceRemoteData
{
    /// <summary>The invoice schema with a customer filter whose options come from the directory, built for a directory that is up or down.</summary>
    /// <param name="isBroken">Tells, at the time of each call, whether the directory is down.</param>
    public static DataListSchema<Invoice> CreateSchema(Func<bool> isBroken) => DataListSchema.For<Invoice>(s => s
        .RowKey(i => i.Id)
        .RowLabel(i => $"Invoice {i.Id}")
        .Search(i => i.Customer, i => i.Status)
        .Field(i => i.Id, f => f.Sortable())
        .Field(i => i.Customer, f => f.Sortable().Filter(DataListFilter.Option().WithLabel("Customer")
            .WithRemoteOptions((request, ct) => CustomerDirectory.SearchAsync(request, isBroken(), ct), CustomerDirectory.ResolveAsync, pageSize: 5).WithFacets()))
        .Field(i => i.Status, f => f.Sortable().Filter(DataListFilter.Option(
            new DataListFilterOption("Paid", "Paid"), new DataListFilterOption("Pending", "Pending"),
            new DataListFilterOption("Overdue", "Overdue"), new DataListFilterOption("Draft", "Draft")).WithLabel("Status")))
        .Field(i => i.Amount, f => f.Sortable())
        .Field(i => i.Issued, f => f.Sortable())
        .DefaultSort(i => i.Id)
        .DefaultPageSize(10));
}
