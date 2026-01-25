using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for FI0004 (Where() on non-collection) and FI0005 (OrderBy() on non-collection).
/// </summary>
public class FilteringAndOrderingTests
{
    // ===================
    // FI0004: Where() on non-collection
    // ===================

    [Fact]
    public async Task WhereOnCollection_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Where() on collection property
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task WhereOnCollectionWithOrderBy_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Where() and OrderBy() on collection property
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive).OrderBy(li => li.UnitPrice).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task WhereOnNonCollection_CompilerCatchesIt()
    {
        // Note: The C# compiler catches this because the Where() extension method
        // only matches collection types, not single reference properties.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: Where() on non-collection (Customer is a single reference)
                    var query = context.Orders.IncludePaths(o => o.Customer!.{|#0:Where|}(c => c.Id > 0));
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Customer", "Where");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    // ===================
    // FI0005: OrderBy() on non-collection
    // ===================

    [Fact]
    public async Task OrderByOnCollection_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: OrderBy() on collection property
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.UnitPrice).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task OrderByDescendingOnCollection_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: OrderByDescending() on collection property
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderByDescending(li => li.Quantity).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ThenByOnCollection_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: OrderBy() followed by ThenBy() on collection
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.UnitPrice).ThenBy(li => li.Quantity).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ThenByDescendingOnCollection_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: OrderByDescending() followed by ThenByDescending() on collection
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderByDescending(li => li.UnitPrice).ThenByDescending(li => li.Quantity).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task OrderByOnNonCollection_CompilerCatchesIt()
    {
        // Note: The C# compiler catches this because the OrderBy() extension method
        // only matches collection types.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: OrderBy() on non-collection (Customer is a single reference)
                    var query = context.Orders.IncludePaths(o => o.Customer!.{|#0:OrderBy|}(c => c.Id));
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Customer", "OrderBy");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    // ===================
    // Combined filtering and ordering
    // ===================

    [Fact]
    public async Task WhereAndMultipleOrderings_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Where with multiple ordering clauses
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems
                            .Where(li => li.IsActive)
                            .OrderBy(li => li.UnitPrice)
                            .ThenByDescending(li => li.Quantity)
                            .Each()
                            .Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CollectionFilteredWithoutNavigation_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Just filtering the collection itself (no deeper navigation)
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive));
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CollectionOrderedWithoutNavigation_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Just ordering the collection itself (no deeper navigation)
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.UnitPrice));
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }
}
