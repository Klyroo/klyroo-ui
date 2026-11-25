using System.Text.Json.Serialization;
using Klyroo.Query;

namespace Dashboard.Shared.Data;

/// <summary>A customer: a row of the table of the Customers page.</summary>
public sealed record Customer(
    int Id,
    string Name,
    string Email,
    string Company,
    string Country,
    string Plan,
    string Status,
    int Orders,
    decimal TotalSpent,
    DateOnly Joined,
    bool Newsletter)
{
    /// <summary>The initials shown in the avatar.</summary>
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => part[0]).Take(2));
}

/// <summary>The sample customers and the schema of their table: which fields search, sort and filter, the key of a row, the default sort and page size.</summary>
public static class CustomerData
{
    public static readonly string[] Statuses = ["Active", "Trial", "Paused", "Churned"];
    public static readonly string[] Plans = ["Free", "Pro", "Business", "Enterprise"];

    private static readonly string[] FirstNames =
    [
        "Maren", "Tobias", "Ines", "Kasper", "Lucia", "Odin", "Petra", "Rafael", "Sunniva", "Tomas", "Valentina", "Wiktor",
        "Aiko", "Bruno", "Celeste", "Dario", "Elin", "Fabian", "Greta", "Hugo", "Isla", "Jonas", "Katya", "Leander",
    ];

    private static readonly string[] LastNames =
    [
        "Albrecht", "Brandvold", "Castellan", "Dumont", "Eklund", "Fontaine", "Grimaldi", "Holloway", "Iversen", "Jarvinen", "Kowalczyk", "Lindqvist",
        "Marchetti", "Novak", "Okafor", "Petrov", "Quintero", "Rosenthal", "Sandoval", "Tanaka", "Ulmer", "Vasquez", "Whitlock", "Yilmaz",
    ];

    private static readonly string[] Companies =
    [
        "Brightloom", "Quillfeather", "Marrow and Finch", "Ostrander Labs", "Pinecrest Freight", "Tidewater", "Lumen Harbor", "Copperleaf",
        "Halcyon Works", "Driftwood Analytics", "Foxglove Studio", "Ironbark Supply", "Juniper Row", "Kestrel Logistics", "Larkspur Health", "Mossgate Foods",
        "Nimbus Atelier", "Oakhaven Trust",
    ];

    public static readonly string[] Countries =
    [
        "France", "Germany", "Spain", "Italy", "United Kingdom", "United States", "Canada", "Japan", "Brazil", "Australia", "Netherlands", "Sweden",
    ];

    /// <summary>240 customers, the same on the server and in the browser.</summary>
    public static readonly Customer[] All = Build();

    private static Customer[] Build()
    {
        var random = new DemoRandom(20260101);
        var customers = new Customer[240];
        for (var i = 0; i < customers.Length; i++)
        {
            var first = FirstNames[i % FirstNames.Length];
            var last = LastNames[(i * 7 + i / FirstNames.Length) % LastNames.Length];
            var company = random.Pick(Companies);
            var orders = random.Between(1, 64);
            var status = random.Next(10) switch { < 6 => "Active", < 8 => "Trial", 8 => "Paused", _ => "Churned" };
            var plan = status == "Trial" ? "Free" : Plans[random.Next(Plans.Length)];
            customers[i] = new Customer(
                i + 1,
                $"{first} {last}",
                $"{first}.{last}@{company.ToLowerInvariant().Replace(" ", "")}.example".ToLowerInvariant(),
                company,
                random.Pick(Countries),
                plan,
                status,
                orders,
                orders * random.Between(35, 310) + random.Between(0, 99) / 100m,
                DemoClock.Today.AddDays(-random.Between(2, 700)),
                random.Next(3) != 0);
        }
        return customers;
    }

    private static DataListFilterOption[] Options(IEnumerable<string> values) => [.. values.Select(v => new DataListFilterOption(v, v))];

    /// <summary>
    /// The schema of the table. The name, status, plan and country filters are text and option filters (the options come with the number of customers they match), the
    /// number of orders and the total spent are number filters, the day the customer joined is a date filter and the newsletter is a yes or no one. Search reads the name,
    /// the email and the company.
    /// </summary>
    public static readonly DataListSchema<Customer> Schema = DataListSchema.For<Customer>(s => s
        .RowKey(c => c.Id)
        .RowLabel(c => c.Name)
        .Search(c => c.Name, c => c.Email, c => c.Company)
        .Field(c => c.Id, f => f.Sortable())
        .Field(c => c.Name, f => f.Sortable().Filter(DataListFilter.Text().WithLabel("Name")))
        .Field(c => c.Email, f => f.Sortable())
        .Field(c => c.Company, f => f.Sortable().Filter(DataListFilter.Text().WithLabel("Company")))
        .Field(c => c.Country, f => f.Sortable().Filter(DataListFilter.Option(Options(Countries)).WithLabel("Country").WithFacets()))
        .Field(c => c.Plan, f => f.Sortable().Filter(DataListFilter.Option(Options(Plans)).WithLabel("Plan").WithFacets()))
        .Field(c => c.Status, f => f.Sortable().Filter(DataListFilter.Option(Options(Statuses)).WithLabel("Status").WithFacets()))
        .Field(c => c.Orders, f => f.Sortable().Filter(DataListFilter.Number(0, 1, 70).WithLabel("Orders")))
        .Field(c => c.TotalSpent, f => f.Sortable().Filter(DataListFilter.Number(0, 100, 20000).WithLabel("Total spent ($)").WithNumberDisplay(DataListNumberDisplay.InputAndSlider).WithNumberFormat("N0")))
        .Field(c => c.Joined, f => f.Sortable().Filter(DataListFilter.Date().WithLabel("Joined").WithMaxRangeDays(366)))
        .Field(c => c.Newsletter, f => f.Filter(DataListFilter.Boolean().WithLabel("Newsletter")))
        .DefaultSort(c => c.Joined, descending: true)
        .DefaultPageSize(10)
        .MaxPageSize(50));
}

/// <summary>
/// The metadata that serializes the result of a prerender: a data list with <c>ResultJson</c> hands its first result to the interactive render, which shows it
/// without loading it again. Without it the table would load its rows a second time when the page becomes interactive.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DataListResult<Customer>))]
public sealed partial class DashboardJson : JsonSerializerContext
{
}
