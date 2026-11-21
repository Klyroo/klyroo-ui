using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Blux.UI.Tools.Query.EfTests;

public enum Status { Active, Pending, Archived }

public class Dept
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class Tag
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string? Name { get; set; }

    public int Weight { get; set; }
}

public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Email { get; set; }

    public int? Age { get; set; }

    public decimal Salary { get; set; }

    public double Score { get; set; }

    public bool Active { get; set; }

    public DateOnly Hired { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public int? DepartmentId { get; set; }

    public Dept? Department { get; set; }

    public Status Status { get; set; }

    public List<Tag> Tags { get; set; } = [];
}

public class Document
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";
}

/// <summary>One column of every type a cursor can carry, each holding values that differ only in the last unit of precision (a tick, one ulp, one step).</summary>
public class Reading
{
    public int Id { get; set; }

    public TimeOnly Time { get; set; }

    public TimeSpan Span { get; set; }

    public DateTime Stamp { get; set; }

    public DateTimeOffset Moment { get; set; }

    public DateOnly Day { get; set; }

    public decimal Amount { get; set; }

    public double Ratio { get; set; }

    public float Single { get; set; }

    public char Letter { get; set; }

    public byte Tiny { get; set; }

    public short Small { get; set; }

    public long Big { get; set; }

    public Status Level { get; set; }

    public bool Flag { get; set; }

    public string Text { get; set; } = "";

    /// <summary>The wall-clock time of a shift, stored as a local time.</summary>
    public DateTime ShiftStart { get; set; }
}

public class Activity
{
    public int Id { get; set; }

    public DateTimeOffset When { get; set; }

    public DateTime At { get; set; }

    public DateOnly On { get; set; }
}

/// <summary>
/// The context the applier is proven against. EF Core's SQLite provider cannot compare or order a <see cref="DateTimeOffset"/> ("could not be translated"),
/// so this is what a SQLite application declares for it: UTC ticks (and ticks for a <see cref="TimeSpan"/>, which it cannot order either). <see cref="decimal"/> needs nothing (EF Core 10 compares and orders it with its own
/// collation and functions, exactly). Other providers (SQL Server, PostgreSQL) need no converter.
/// </summary>
public abstract class QueryContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Dept> Depts => Set<Dept>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Reading> Readings => Set<Reading>();

    /// <summary>The collation of every text column, or <see langword="null"/> for the provider default (SQLite: binary).</summary>
    protected virtual string? TextCollation => null;

    protected virtual bool ConvertForSqlite => true;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (!ConvertForSqlite) return;
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        configurationBuilder.Properties<TimeSpan>().HaveConversion<long>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId);
        modelBuilder.Entity<Employee>().HasMany(e => e.Tags).WithOne().HasForeignKey(t => t.EmployeeId);
        if (TextCollation is not { } collation) return;
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(string)))
            property.SetCollation(collation);
    }
}

public sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

/// <summary>Default collation (SQLite: BINARY, case sensitive).</summary>
public sealed class BinaryContext(DbContextOptions<BinaryContext> options) : QueryContext(options);

/// <summary>Every text column compares ignoring ASCII case (SQLite NOCASE).</summary>
public sealed class NoCaseContext(DbContextOptions<NoCaseContext> options) : QueryContext(options)
{
    protected override string? TextCollation => "NOCASE";
}

/// <summary>No converter for <see cref="DateTimeOffset"/>: what a SQLite application gets when it forgets it.</summary>
public sealed class PlainContext(DbContextOptions<PlainContext> options) : QueryContext(options)
{
    protected override bool ConvertForSqlite => false;
}

/// <summary>A seeded in-memory SQLite database that lives as long as the process: one open connection per context type.</summary>
public static class Database
{
    private static readonly Lock Gate = new();
    private static readonly Dictionary<Type, SqliteConnection> Connections = [];

    public static TContext Open<TContext>(Func<DbContextOptions<TContext>, TContext> create) where TContext : QueryContext
    {
        SqliteConnection connection;
        var created = false;
        lock (Gate)
        {
            if (!Connections.TryGetValue(typeof(TContext), out connection!))
            {
                connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                Connections[typeof(TContext)] = connection;
                created = true;
            }
        }
        var options = new DbContextOptionsBuilder<TContext>().UseSqlite(connection).Options;
        var context = create(options);
        if (created)
        {
            context.Database.EnsureCreated();
            Seed.Fill(context);
        }
        return context;
    }

    public static BinaryContext Binary() => Open<BinaryContext>(o => new BinaryContext(o));

    public static NoCaseContext NoCase() => Open<NoCaseContext>(o => new NoCaseContext(o));

    public static PlainContext Plain() => Open<PlainContext>(o => new PlainContext(o));
}

public static class Seed
{
    /// <summary>The hand-made rows (edge cases) then generated ones (ties, nulls, three-digit keys), the same on every run.</summary>
    public static List<Employee> Employees()
    {
        var rows = new List<Employee>
        {
            Row(1, "Alice Johnson", "alice@acme.io", 30, 5000.50m, 1.5, true, "2020-01-15", "2024-03-01T10:00:00Z", "Engineering", Status.Active, ("dotnet", 1), ("blazor", 5)),
            Row(2, "bob smith", null, 25, 4200m, 2.5, true, "2021-06-01", null, "Sales", Status.Pending, ("sales", 3)),
            Row(3, "Carol White", "", null, 6100m, 3, false, "2019-12-31", "2024-03-02T00:00:00Z", null, Status.Archived),
            Row(4, "dave brown", "dave@acme.io", 41, 7000m, 0.5, true, "2020-01-15", "2024-03-02T23:59:59Z", "Engineering", Status.Active),
            Row(5, "Éric Durand", "eric@x.fr", 30, 3900.25m, 2, false, "2022-02-02", "2024-03-03T00:00:00Z", "Sales", Status.Pending, ("dotnet", 2), (null, 0)),
            Row(6, "Frank O'Neil", null, 18, 100m, 9, true, "2023-12-31", "2024-02-29T12:00:00+02:00", "", Status.Active, ("", 9), ("Blazor", 4)),
        };

        var random = new Random(7);
        string[] first = ["Anna", "Boris", "Chen", "Dina", "Emil", "Fatou", "Gus", "Hana", "Ivo", "Jana"];
        string[] last = ["Young", "Martin", "Lopez", "Novak", "Silva", "Weber", "Kumar", "Rossi"];
        string?[] depts = ["Engineering", "Sales", "Support", "", null, null];
        string?[] tagNames = ["dotnet", "blazor", "sales", "Blazor", "css", ""];
        for (var id = 7; id <= 130; id++)
        {
            var name = $"{first[random.Next(first.Length)]} {last[random.Next(last.Length)]}";
            var email = random.Next(5) switch { 0 => null, 1 => "", _ => $"{name.Replace(' ', '.').ToLowerInvariant()}@corp.test" };
            int? age = random.Next(10) == 0 ? null : 18 + random.Next(8) * 5;
            var tags = Enumerable.Range(0, random.Next(4)).Select(_ => (tagNames[random.Next(tagNames.Length)], random.Next(10))).ToArray();
            var login = random.Next(5) == 0
                ? null
                : DateTimeOffset.Parse("2024-03-01T00:00:00Z").AddSeconds(random.Next(0, 4) * 43200 + random.Next(0, 3)).ToString("O");
            rows.Add(Row(
                id,
                name,
                email,
                age,
                random.Next(10, 90) * 100 + random.Next(3) * 0.25m,
                random.Next(0, 8) * 0.5,
                random.Next(2) == 0,
                new DateOnly(2018, 1, 1).AddDays(random.Next(0, 2500)).ToString("yyyy-MM-dd"),
                login,
                depts[random.Next(depts.Length)],
                (Status)random.Next(3),
                tags));
        }
        return rows;
    }

    private static Employee Row(
        int id, string name, string? email, int? age, decimal salary, double score, bool active, string hired, string? login, string? dept, Status status, params (string? Name, int Weight)[] tags) =>
        new()
        {
            Id = id,
            Name = name,
            Email = email,
            Age = age,
            Salary = salary,
            Score = score,
            Active = active,
            Hired = DateOnly.Parse(hired),
            LastLogin = login is null ? null : DateTimeOffset.Parse(login),
            Department = dept is null ? null : new Dept { Name = dept },
            Status = status,
            Tags = [.. tags.Select(t => new Tag { Name = t.Name, Weight = t.Weight })],
        };

    /// <summary>36 rows; every column takes 12 values (in a scrambled order, three rows each) that are one tick, one ulp or one step apart.</summary>
    public static List<Reading> Readings()
    {
        var rows = new List<Reading>();
        for (var i = 0; i < 36; i++)
        {
            var k = i * 7 % 12;
            var ratio = 0.1;
            var single = 0.1f;
            for (var j = 0; j < k; j++)
            {
                ratio = Math.BitIncrement(ratio);
                single = MathF.BitIncrement(single);
            }
            rows.Add(new Reading
            {
                Id = i + 1,
                Time = new TimeOnly(14, 30, 45).Add(TimeSpan.FromTicks(k)),
                Span = TimeSpan.FromDays(1).Add(TimeSpan.FromTicks(k)),
                Stamp = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Unspecified).AddTicks(k),
                Moment = new DateTimeOffset(2024, 3, 1, 10, 0, 0, TimeSpan.Zero).AddTicks(k),
                Day = new DateOnly(2024, 3, 1).AddDays(k),
                Amount = 1.0000000m + k * 0.0000001m,
                Ratio = ratio,
                Single = single,
                Letter = (char)('a' + k),
                Tiny = (byte)(244 + k),
                Small = (short)(short.MaxValue - k),
                Big = long.MaxValue - k,
                Level = (Status)(k % 3),
                Flag = k % 2 == 0,
                Text = "t" + k.ToString("00"),
                ShiftStart = new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Unspecified).AddMinutes(k * 30 - 90),
            });
        }
        return rows;
    }

    public static void Fill(QueryContext context)
    {
        var rows = Employees();
        context.Employees.AddRange(rows);
        // Activities for the time zone tests: instants around local midnights of Paris (daylight saving change on 2024-03-31) and New York.
        context.Readings.AddRange(Seed.Readings());
        var instants = new[]
        {
            "2024-03-30T22:59:59Z", "2024-03-30T23:00:00Z", "2024-03-31T00:30:00Z", "2024-03-31T21:59:59Z", "2024-03-31T22:00:00Z",
            "2024-03-31T22:00:01Z", "2024-04-01T03:59:59Z", "2024-04-01T04:00:00Z", "2024-03-15T10:59:59Z", "2024-03-15T11:00:00Z", "2024-03-15T11:59:59.9999999Z", "2024-03-15T12:00:00Z",
        };
        var id = 1;
        foreach (var iso in instants)
        {
            var when = DateTimeOffset.Parse(iso);
            context.Activities.Add(new Activity
            {
                Id = id++,
                When = when,
                At = DateTime.SpecifyKind(when.UtcDateTime, DateTimeKind.Unspecified),
                On = DateOnly.FromDateTime(when.UtcDateTime),
            });
        }
        context.SaveChanges();
        context.ChangeTracker.Clear();
    }
}
