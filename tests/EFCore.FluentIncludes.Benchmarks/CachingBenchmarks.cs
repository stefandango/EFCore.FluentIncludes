using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using EFCore.FluentIncludes.Benchmarks.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Benchmarks;

/// <summary>
/// Benchmarks specifically for measuring expression caching performance.
/// These measure the raw parsing overhead with and without cache benefits.
/// </summary>
[MemoryDiagnoser]
public class CachingBenchmarks
{
    private BenchmarkDbContext _context = null!;

    // Pre-defined expression to reuse (simulates compile-time constants stored in fields)
    private static readonly Expression<Func<Order, object?>> SimplePathExpr = o => o.Customer;
    private static readonly Expression<Func<Order, object?>> DeepPathExpr = o => o.LineItems.Each().Product!.Category!.ParentCategory;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new BenchmarkDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        // Warm up the cache with pre-defined expressions
        _ = _context.Orders.IncludePaths(SimplePathExpr).ToQueryString();
        _ = _context.Orders.IncludePaths(DeepPathExpr).ToQueryString();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    // ===========================================
    // Repeated inline expressions (cache hit via structural equality)
    // ===========================================

    [Benchmark(Baseline = true)]
    public string Standard_RepeatedSimple()
    {
        // Standard EF Core - no caching benefit
        return _context.Orders
            .Include(o => o.Customer)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_RepeatedSimple()
    {
        // FluentIncludes with caching - should hit cache via structural equality
        return _context.Orders
            .IncludePaths(o => o.Customer)
            .ToQueryString();
    }

    [Benchmark]
    public string Standard_RepeatedDeep()
    {
        return _context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
                        .ThenInclude(c => c!.ParentCategory)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_RepeatedDeep()
    {
        return _context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Category!.ParentCategory)
            .ToQueryString();
    }

    // ===========================================
    // Field-stored expressions (guaranteed cache hit)
    // ===========================================

    [Benchmark]
    public string FluentIncludes_StoredSimple()
    {
        // Uses pre-stored expression - guaranteed cache hit
        return _context.Orders
            .IncludePaths(SimplePathExpr)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_StoredDeep()
    {
        // Uses pre-stored expression - guaranteed cache hit
        return _context.Orders
            .IncludePaths(DeepPathExpr)
            .ToQueryString();
    }

    // ===========================================
    // Multiple iterations to amplify difference
    // ===========================================

    [Benchmark]
    public void Standard_10Iterations()
    {
        for (int i = 0; i < 10; i++)
        {
            _ = _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.Address)
                .Include(o => o.LineItems)
                    .ThenInclude(li => li.Product)
                .ToQueryString();
        }
    }

    [Benchmark]
    public void FluentIncludes_10Iterations()
    {
        for (int i = 0; i < 10; i++)
        {
            _ = _context.Orders
                .IncludePaths(
                    o => o.Customer!.Address,
                    o => o.LineItems.Each().Product)
                .ToQueryString();
        }
    }
}
