using BenchmarkDotNet.Attributes;
using EFCore.FluentIncludes.Benchmarks.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Benchmarks;

/// <summary>
/// Benchmarks comparing FluentIncludes IncludePaths vs standard EF Core Include/ThenInclude.
/// These benchmarks measure the expression building overhead by generating SQL.
/// </summary>
[MemoryDiagnoser]
public class IncludeBenchmarks
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
    // Simple single navigation
    // ===========================================

    [Benchmark(Baseline = true)]
    public string Standard_SingleNavigation()
    {
        return _context.Orders
            .Include(o => o.Customer)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_SingleNavigation()
    {
        return _context.Orders
            .IncludePaths(o => o.Customer)
            .ToQueryString();
    }

    // ===========================================
    // Two-level navigation
    // ===========================================

    [Benchmark]
    public string Standard_TwoLevelNavigation()
    {
        return _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_TwoLevelNavigation()
    {
        return _context.Orders
            .IncludePaths(o => o.Customer!.Address)
            .ToQueryString();
    }

    // ===========================================
    // Deep navigation (4 levels)
    // ===========================================

    [Benchmark]
    public string Standard_DeepNavigation()
    {
        return _context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
                        .ThenInclude(c => c!.ParentCategory)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_DeepNavigation()
    {
        return _context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Category!.ParentCategory)
            .ToQueryString();
    }

    // ===========================================
    // Multiple paths
    // ===========================================

    [Benchmark]
    public string Standard_MultiplePaths()
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
    public string FluentIncludes_MultiplePaths()
    {
        return _context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Category)
            .ToQueryString();
    }

    // ===========================================
    // Complex scenario (multiple deep paths)
    // ===========================================

    [Benchmark]
    public string Standard_ComplexScenario()
    {
        return _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
                        .ThenInclude(c => c!.ParentCategory)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Tags)
            .ToQueryString();
    }

    [Benchmark]
    public string FluentIncludes_ComplexScenario()
    {
        return _context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Category!.ParentCategory,
                o => o.LineItems.Each().Product!.Tags)
            .ToQueryString();
    }
}
