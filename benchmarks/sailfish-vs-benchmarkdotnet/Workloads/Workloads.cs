using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ToolCompare;

// The single source of truth for the code under test. BOTH tools (Sailfish and BenchmarkDotNet)
// invoke these exact same static methods so any difference in reported numbers comes from the
// measurement methodology, not the workload.
public static class Workloads
{
    // ~100-300ns: small integer sum. Deliberately near/below timer resolution to expose
    // how each tool handles sub-microsecond operations.
    private static readonly int[] TinyArray = Enumerable.Range(0, 256).ToArray();

    // ~40-80us: SHA256 over a fixed 64KB buffer. Representative of "sometimes code" CPU work.
    private static readonly byte[] HashBuffer = CreateDeterministicBuffer(64 * 1024);

    public static volatile int Sink; // defeat dead-code elimination

    private static byte[] CreateDeterministicBuffer(int size)
    {
        var buffer = new byte[size];
        var rng = new Random(42);
        rng.NextBytes(buffer);
        return buffer;
    }

    public static int TinyOp()
    {
        var sum = 0;
        var arr = TinyArray;
        for (var i = 0; i < arr.Length; i++) sum += arr[i];
        Sink = sum;
        return sum;
    }

    public static int CpuHash()
    {
        var hash = SHA256.HashData(HashBuffer);
        Sink = hash[0];
        return hash[0];
    }

    public static int EfCoreQuery()
    {
        var db = EfFixture.Instance.NewContext();
        var results = db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == 57 && o.Total > 250m)
            .OrderByDescending(o => o.PlacedAt)
            .Take(20)
            .ToList();
        Sink = results.Count;
        return results.Count;
    }
}

// Shared, seeded SQLite in-memory database. The connection is held open for the process
// lifetime (closing the last connection to a :memory: SQLite db drops it).
public sealed class EfFixture
{
    private static readonly Lazy<EfFixture> Lazy = new(() => new EfFixture());
    public static EfFixture Instance => Lazy.Value;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<BenchDbContext> _options;

    private EfFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<BenchDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new BenchDbContext(_options);
        db.Database.EnsureCreated();
        Seed(db);
    }

    public BenchDbContext NewContext() => new(_options);

    private static void Seed(BenchDbContext db)
    {
        var rng = new Random(1234);
        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = new List<Order>(5000);
        for (var i = 0; i < 5000; i++)
        {
            orders.Add(new Order
            {
                CustomerId = rng.Next(0, 200),
                Total = (decimal)(rng.NextDouble() * 500.0),
                PlacedAt = baseDate.AddMinutes(rng.Next(0, 60 * 24 * 365)),
                Status = (byte)rng.Next(0, 4)
            });
        }

        db.Orders.AddRange(orders);
        db.SaveChanges();
    }
}

public sealed class BenchDbContext(DbContextOptions<BenchDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.CustomerId);
        });
    }
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime PlacedAt { get; set; }
    public byte Status { get; set; }
}
