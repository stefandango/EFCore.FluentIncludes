using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Additional tests to improve code coverage for analyzer edge cases.
/// </summary>
public class AdditionalCoverageTests
{
    #region Cast Expression Tests

    [Fact]
    public async Task CastToBaseType_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Cast to base type (object upcast) is valid
                    var query = context.Orders.IncludePaths(o => (object)o.Customer!);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CastToDerivedType_NoDiagnostic()
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

                public class BaseCustomer
                {
                    public int Id { get; set; }
                    public Address? Address { get; set; }
                }

                public class PremiumCustomer : BaseCustomer
                {
                    public string MembershipLevel { get; set; } = "";
                }

                public class Address
                {
                    public int Id { get; set; }
                    public string Street { get; set; } = "";
                }

                public class Order
                {
                    public int Id { get; set; }
                    public BaseCustomer? Customer { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Cast to derived type (downcast) - valid if types are related
                        var query = context.Orders.IncludePaths(o => ((PremiumCustomer)o.Customer!).Address);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Array and Collection Type Tests

    [Fact]
    public async Task ArrayProperty_NoDiagnostic()
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
                    public static T Each<T>(this T[] source) => throw new InvalidOperationException();
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
                    public LineItem[] Items { get; set; } = Array.Empty<LineItem>();
                }

                public class LineItem
                {
                    public int Id { get; set; }
                    public Product? Product { get; set; }
                }

                public class Product
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Array property navigation
                        var query = context.Orders.IncludePaths(o => o.Items.Each().Product);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public async Task CastToImplementedInterface_NoDiagnostic()
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

                public static class NavigationExtensions
                {
                    public static T To<T>(this T? value) where T : class => throw new InvalidOperationException();
                }
            }

            namespace TestNamespace
            {
                using EFCore.FluentIncludes;
                using Microsoft.EntityFrameworkCore;

                public interface ICustomer
                {
                    int Id { get; }
                    string Name { get; }
                }

                public class Customer : ICustomer
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public class Order
                {
                    public int Id { get; set; }
                    public Customer? Customer { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Cast to implemented interface
                        var query = context.Orders.IncludePaths(o => (ICustomer)o.Customer!);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Parenthesized Expression Tests

    [Fact]
    public async Task ParenthesizedExpression_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Parenthesized expression with null-forgiving
                    var query = context.Orders.IncludePaths(o => ((o.Customer!)).Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task MultipleParentheses_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Multiple parentheses
                    var query = context.Orders.IncludePaths(o => (((o.Customer!).Address)));
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Array Initializer in Arguments

    [Fact]
    public async Task ArrayInitializerWithLambdas_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Array initializer syntax for multiple paths
                    var query = context.Orders.IncludePaths(
                        new System.Linq.Expressions.Expression<System.Func<Order, object?>>[]
                        {
                            o => o.Customer,
                            o => o.LineItems
                        });
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region IncludeSpec Base Class Tests

    [Fact]
    public async Task IncludeSpecDerived_NoDiagnostic()
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
                }

                public static class NavigationExtensions
                {
                    public static T To<T>(this T? value) where T : class => throw new InvalidOperationException();
                }

                public abstract class IncludeSpec<TEntity> where TEntity : class
                {
                    protected void Include<TProperty>(Expression<Func<TEntity, TProperty>> path) { }
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
                }

                public class Customer
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public class LineItem
                {
                    public int Id { get; set; }
                }

                public class OrderIncludeSpec : IncludeSpec<Order>
                {
                    public OrderIncludeSpec()
                    {
                        Include(o => o.Customer);
                        Include(o => o.LineItems);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Nullable Value Type Tests

    [Fact]
    public async Task NullableValueTypeProperty_NoDiagnostic()
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
            }

            namespace TestNamespace
            {
                using EFCore.FluentIncludes;
                using Microsoft.EntityFrameworkCore;

                public class Order
                {
                    public int Id { get; set; }
                    public int? OptionalQuantity { get; set; }
                    public DateTime? OptionalDate { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Nullable value type properties
                        var query = context.Orders.IncludePaths(o => o.OptionalQuantity);
                        var query2 = context.Orders.IncludePaths(o => o.OptionalDate);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region String Property Tests (Not Collection)

    [Fact]
    public async Task StringProperty_NotTreatedAsCollection_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // String property should not be treated as a collection
                    var query = context.Orders.IncludePaths(o => o.Notes);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Multiple Path Arguments

    [Fact]
    public async Task MultiplePaths_AllValid_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Multiple path arguments
                    var query = context.Orders.IncludePaths(
                        o => o.Customer,
                        o => o.LineItems,
                        o => o.LineItems.Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Lambda with Block Body (Should be skipped)

    [Fact]
    public async Task LambdaWithBlockBody_Skipped_NoDiagnostic()
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
                        Func<TEntity, object?> path) where TEntity : class
                        => source;
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
                        // Lambda with block body - should be skipped by analyzer
                        var query = context.Orders.IncludePaths(o => { return o.Customer; });
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Non-FluentIncludes Method (Should be skipped)

    [Fact]
    public async Task NonFluentIncludesMethod_Skipped_NoDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

            namespace TestNamespace
            {
                public class Order
                {
                    public int Id { get; set; }
                    public string? InvalidProperty { get; set; }
                }

                public static class OtherExtensions
                {
                    // Method with same name but different namespace
                    public static void IncludePaths<T>(this T source, Expression<Func<T, object?>> path) { }
                }

                public class TestClass
                {
                    public void Test()
                    {
                        var order = new Order();
                        // This should NOT be analyzed - it's not from FluentIncludes
                        order.IncludePaths(o => o.InvalidProperty);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region IEnumerable Direct Type Tests

    [Fact]
    public async Task IEnumerableDirectProperty_NoDiagnostic()
    {
        // Test that IEnumerable<T> direct properties are recognized as collections
        // Using ICollection<T> which the analyzer supports
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ICollection<T> property - recognized as collection
                    var query = context.Orders.IncludePaths(o => o.LineItems.Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Deep Inheritance Chain Tests

    [Fact]
    public async Task CastInDeepInheritanceChain_NoDiagnostic()
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

                public static class NavigationExtensions
                {
                    public static T To<T>(this T? value) where T : class => throw new InvalidOperationException();
                }
            }

            namespace TestNamespace
            {
                using EFCore.FluentIncludes;
                using Microsoft.EntityFrameworkCore;

                public class BaseEntity { public int Id { get; set; } }
                public class MiddleEntity : BaseEntity { public string Name { get; set; } = ""; }
                public class DerivedEntity : MiddleEntity { public Address? Address { get; set; } }

                public class Address { public string Street { get; set; } = ""; }

                public class Order
                {
                    public int Id { get; set; }
                    public MiddleEntity? Entity { get; set; }
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // Cast to derived type in deep inheritance chain
                        var query = context.Orders.IncludePaths(o => ((DerivedEntity)o.Entity!).Address);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Nullable Navigation with To() in Collection

    [Fact]
    public async Task NullableInCollectionChain_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Nullable navigation after collection with To()
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Each().Product.To().Category);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Complex Filter Predicates

    [Fact]
    public async Task FilterWithMultipleConditions_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Complex filter with multiple conditions
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive && li.Quantity > 0).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task FilterWithOrCondition_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Filter with OR condition
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive || li.Quantity > 10).Each());
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Ordering with Multiple Keys

    [Fact]
    public async Task OrderByThenByThenByDescending_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Multiple ordering keys
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems
                            .OrderBy(li => li.IsActive)
                            .ThenBy(li => li.Quantity)
                            .ThenByDescending(li => li.Id)
                            .Each());
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region IncludePath Single Expression

    [Fact]
    public async Task IncludePathSingle_NoDiagnostic()
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
                    public static IQueryable<TEntity> IncludePath<TEntity, TProperty>(
                        this IQueryable<TEntity> source,
                        Expression<Func<TEntity, TProperty>> path) where TEntity : class
                        => source;
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
                    public Address? Address { get; set; }
                }

                public class Address
                {
                    public string Street { get; set; } = "";
                }

                public class TestDbContext : DbContext
                {
                    public DbSet<Order> Orders { get; set; } = null!;
                }

                public class TestClass
                {
                    public void Test(TestDbContext context)
                    {
                        // IncludePath (single expression, not params)
                        var query = context.Orders.IncludePath(o => o.Customer!.Address);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion

    #region Method Symbol Without Containing Type (Edge Case)

    [Fact]
    public async Task LocalFunction_Skipped_NoDiagnostic()
    {
        var testCode = """
            using System;
            using System.Linq.Expressions;

            namespace TestNamespace
            {
                public class Order { public int Id { get; set; } }

                public class TestClass
                {
                    public void Test()
                    {
                        // Local function named IncludePaths - should be skipped
                        void IncludePaths(Expression<Func<Order, object?>> path) { }

                        IncludePaths(o => o.Id);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    #endregion
}
