using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Helper class for setting up analyzer tests with required references.
/// </summary>
public static class AnalyzerTestHelper
{
    /// <summary>
    /// Creates and runs an analyzer test with the FluentIncludes library signatures.
    /// </summary>
    public static async Task VerifyAnalyzerAsync<TAnalyzer>(
        string source,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }

    /// <summary>
    /// Creates a diagnostic result for the specified rule.
    /// </summary>
    public static DiagnosticResult Diagnostic(string diagnosticId)
    {
        return new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Creates a warning diagnostic result.
    /// </summary>
    public static DiagnosticResult Warning(string diagnosticId)
    {
        return new DiagnosticResult(diagnosticId, DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Common test code preamble with usings, stub types, and test entities.
    /// Includes minimal stubs for EF Core and FluentIncludes to avoid version conflicts.
    /// Note: Does NOT close the namespace - tests should close it.
    /// </summary>
    public const string TestCodePreamble = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Linq.Expressions;

        // Minimal EF Core stubs
        namespace Microsoft.EntityFrameworkCore
        {
            public class DbContext { }
            public abstract class DbSet<TEntity> : IQueryable<TEntity> where TEntity : class
            {
                public Type ElementType => typeof(TEntity);
                public Expression Expression => Expression.Constant(this);
                public IQueryProvider Provider => null!;
                public IEnumerator<TEntity> GetEnumerator() => throw new NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }

        // Minimal FluentIncludes stubs
        namespace EFCore.FluentIncludes
        {
            public static class QueryableExtensions
            {
                public static IQueryable<TEntity> IncludePaths<TEntity>(
                    this IQueryable<TEntity> source,
                    params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                    => source;

                public static IQueryable<TEntity> IncludePath<TEntity, TProperty>(
                    this IQueryable<TEntity> source,
                    Expression<Func<TEntity, TProperty>> path) where TEntity : class
                    => source;
            }

            public static class CollectionExtensions
            {
                public static T Each<T>(this IEnumerable<T> source) => throw new InvalidOperationException();
                public static T Each<T>(this ICollection<T> source) => throw new InvalidOperationException();

                // Where for filtered includes (returns collection type for chaining)
                public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
                    => throw new InvalidOperationException();
                public static ICollection<T> Where<T>(this ICollection<T> source, Func<T, bool> predicate)
                    => throw new InvalidOperationException();

                // OrderBy for ordered includes (returns collection type for chaining)
                public static IEnumerable<T> OrderBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();
                public static ICollection<T> OrderBy<T, TKey>(this ICollection<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();

                public static IEnumerable<T> OrderByDescending<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();
                public static ICollection<T> OrderByDescending<T, TKey>(this ICollection<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();

                public static IEnumerable<T> ThenBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();
                public static ICollection<T> ThenBy<T, TKey>(this ICollection<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();

                public static IEnumerable<T> ThenByDescending<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();
                public static ICollection<T> ThenByDescending<T, TKey>(this ICollection<T> source, Func<T, TKey> keySelector)
                    => throw new InvalidOperationException();
            }

            public static class NavigationExtensions
            {
                public static T To<T>(this T? value) where T : class => throw new InvalidOperationException();
            }
        }

        namespace TestNamespace
        {
            using EFCore.FluentIncludes;
            using Microsoft.EntityFrameworkCore;

            public class Order
            {
                public int Id { get; set; }
                public Customer? Customer { get; set; }
                public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
                public string Notes { get; set; } = "";
            }

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public Address? Address { get; set; }
            }

            public class Address
            {
                public int Id { get; set; }
                public string Street { get; set; } = "";
                public string City { get; set; } = "";
            }

            public class LineItem
            {
                public int Id { get; set; }
                public decimal UnitPrice { get; set; }
                public int Quantity { get; set; }
                public bool IsActive { get; set; }
                public Product? Product { get; set; }
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public Category? Category { get; set; }
            }

            public class Category
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
            }

            public class TestDbContext : DbContext
            {
                public DbSet<Order> Orders { get; set; } = null!;
            }
        """;
}
