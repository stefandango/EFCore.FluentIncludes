using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests targeting specific uncovered code paths in IncludeExpressionWalker.
/// </summary>
public class UncoveredPathsTests
{
    #region FI0001 - Property Not Found (lines 181-186, 320-328)

    [Fact]
    public async Task PropertyNotFound_ReportsCompilerError()
    {
        // Note: When a property doesn't exist, the C# compiler catches it first with CS1061
        // Our FI0001 diagnostic handles edge cases where semantic model can't resolve but code compiles
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: NonExistentProperty doesn't exist on Order
                    var query = context.Orders.IncludePaths(o => o.{|#0:NonExistentProperty|});
                }
            }
        }
        """;

        // The compiler catches this as CS1061 before our analyzer
        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Order", "NonExistentProperty");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task NestedPropertyNotFound_ReportsCompilerError()
    {
        // Note: When a property doesn't exist, the C# compiler catches it first with CS1061
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: FakeProperty doesn't exist on Customer
                    var query = context.Orders.IncludePaths(o => o.Customer!.{|#0:FakeProperty|});
                }
            }
        }
        """;

        // The compiler catches this as CS1061 before our analyzer
        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Customer", "FakeProperty");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    #endregion

    #region FI0002 - Missing Each on Collection (lines 250-255)

    [Fact]
    public async Task CollectionPropertyFollowedByProperty_WithoutEach_ReportsDiagnostic()
    {
        // This tests the case where we access a property through a collection without Each()
        // The compiler usually catches this (CS1061), but we want to cover the analyzer path
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: Accessing Product through collection without Each()
                    // This will also trigger CS1061 from the compiler
                    var query = context.Orders.IncludePaths(o => o.LineItems.{|#0:Product|});
                }
            }
        }
        """;

        // The compiler catches this as CS1061 before our analyzer
        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("System.Collections.Generic.ICollection<TestNamespace.LineItem>", "Product");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    #endregion

    #region FI0004 - Where on Non-Collection (line 307 partial - WhereCall)

    [Fact]
    public async Task WhereOnNonCollection_ReportsDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

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

            namespace EFCore.FluentIncludes
            {
                public static class QueryableExtensions
                {
                    public static IQueryable<TEntity> IncludePaths<TEntity>(
                        this IQueryable<TEntity> source,
                        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                        => source;
                }

                public static class CollectionExtensions
                {
                    // Fake Where that works on any type (to bypass compiler check)
                    public static T Where<T>(this T source, Func<T, bool> predicate) => source;
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
                }

                public class Customer
                {
                    public int Id { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // ERROR: Where() on non-collection (Customer is not a collection)
                        var query = context.Orders.IncludePaths(o => {|#0:o.Customer!.Where(c => true)|});
                    }
                }
            }
            """;

        var expected = Diagnostic("FI0004")
            .WithLocation(0)
            .WithArguments("Where");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    #endregion

    #region FI0005 - OrderBy on Non-Collection (line 307)

    [Fact]
    public async Task OrderByOnNonCollection_ReportsDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

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

            namespace EFCore.FluentIncludes
            {
                public static class QueryableExtensions
                {
                    public static IQueryable<TEntity> IncludePaths<TEntity>(
                        this IQueryable<TEntity> source,
                        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                        => source;
                }

                public static class CollectionExtensions
                {
                    // Fake OrderBy that works on any type (to bypass compiler check)
                    public static T OrderBy<T, TKey>(this T source, Func<T, TKey> keySelector) => source;
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
                }

                public class Customer
                {
                    public int Id { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // ERROR: OrderBy() on non-collection (Customer is not a collection)
                        var query = context.Orders.IncludePaths(o => {|#0:o.Customer!.OrderBy(c => c.Id)|});
                    }
                }
            }
            """;

        var expected = Diagnostic("FI0005")
            .WithLocation(0)
            .WithArguments("OrderBy");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    #endregion

    #region Cast to Collection Type (line 292)

    [Fact]
    public async Task CastToCollectionType_NoDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

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

            namespace EFCore.FluentIncludes
            {
                public static class QueryableExtensions
                {
                    public static IQueryable<TEntity> IncludePaths<TEntity>(
                        this IQueryable<TEntity> source,
                        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                        => source;
                }

                public static class CollectionExtensions
                {
                    public static T Each<T>(this IEnumerable<T> source) => throw new InvalidOperationException();
                    public static T Each<T>(this ICollection<T> source) => throw new InvalidOperationException();
                    public static T Each<T>(this IList<T> source) => throw new InvalidOperationException();
                }
            }

            namespace TestNamespace
            {
                using EFCore.FluentIncludes;
                using Microsoft.EntityFrameworkCore;

                public class Order
                {
                    public int Id { get; set; }
                    public IEnumerable<LineItem> Items { get; set; } = new List<LineItem>();
                }

                public class LineItem
                {
                    public int Id { get; set; }
                    public Product? Product { get; set; }
                }

                public class Product
                {
                    public int Id { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Cast to IList<T> (a collection type)
                        var query = context.Orders.IncludePaths(o => ((IList<LineItem>)o.Items).Each().Product);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Unknown Segment Kind (line 210)

    [Fact]
    public async Task UnknownMethodCall_NoDiagnostic()
    {
        // Test an unknown method call that should result in SegmentKind.Unknown
        var testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

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

            namespace EFCore.FluentIncludes
            {
                public static class QueryableExtensions
                {
                    public static IQueryable<TEntity> IncludePaths<TEntity>(
                        this IQueryable<TEntity> source,
                        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                        => source;
                }

                public static class MyExtensions
                {
                    // Custom method that's not Each/To/Where/OrderBy
                    public static T CustomMethod<T>(this T source) => source;
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
                }

                public class Customer
                {
                    public int Id { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Unknown method call (CustomMethod is not recognized)
                        var query = context.Orders.IncludePaths(o => o.Customer!.CustomMethod());
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Null-Forgiving Operator (line 128)

    [Fact]
    public async Task NullForgivingOperator_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Using null-forgiving operator (!)
                    var query = context.Orders.IncludePaths(o => o.Customer!.Address!);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task MultipleNullForgivingOperators_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Multiple null-forgiving operators in chain
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Each().Product!.Category!);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Static HashSet Fields Coverage (lines 20, 25, 30)

    [Fact]
    public async Task AllMarkerMethodsUsed_NoDiagnostic()
    {
        // This test ensures the static HashSets are accessed
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Uses Each (CollectionMarkerMethods)
                    var q1 = context.Orders.IncludePaths(o => o.LineItems.Each().Product);

                    // Uses To (NavigationMarkerMethods)
                    var q2 = context.Orders.IncludePaths(o => o.Customer.To().Address);

                    // Uses Where (FilterMethods)
                    var q3 = context.Orders.IncludePaths(o => o.LineItems.Where(li => li.IsActive).Each());

                    // Uses OrderBy (OrderingMethods)
                    var q4 = context.Orders.IncludePaths(o => o.LineItems.OrderBy(li => li.Id).Each());
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region FI0003 - Each on Non-Collection

    [Fact]
    public async Task EachOnNonCollectionProperty_ReportsDiagnostic()
    {
        // Tests ValidateEachCall when currentIsCollection is false
        var testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

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

            namespace EFCore.FluentIncludes
            {
                public static class QueryableExtensions
                {
                    public static IQueryable<TEntity> IncludePaths<TEntity>(
                        this IQueryable<TEntity> source,
                        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                        => source;
                }

                public static class CollectionExtensions
                {
                    // Fake Each that works on any type (to bypass compiler check)
                    public static T Each<T>(this T source) => source;
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
                }

                public class Customer
                {
                    public int Id { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // ERROR: Each() on non-collection (Customer is not a collection)
                        var query = context.Orders.IncludePaths(o => {|#0:o.Customer!.Each()|});
                    }
                }
            }
            """;

        var expected = Diagnostic("FI0003")
            .WithLocation(0)
            .WithArguments("property");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    #endregion
}
