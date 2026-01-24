using BenchmarkDotNet.Attributes;
using EFCore.FluentIncludes.Benchmarks.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Benchmarks;

/// <summary>
/// Benchmarks that verify SQL equivalence and measure ToQueryString() overhead.
/// These help validate that FluentIncludes generates identical SQL to standard EF Core.
/// </summary>
[MemoryDiagnoser]
public class SqlEquivalenceBenchmarks
{
    private BenchmarkDbContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new BenchmarkDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    // ===========================================
    // SQL Generation comparison
    // ===========================================

    [Benchmark(Baseline = true)]
    public string Standard_GenerateSql()
    {
        return _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_GenerateSql()
    {
        return _context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Category)
            .ToQueryString();
    }
}
