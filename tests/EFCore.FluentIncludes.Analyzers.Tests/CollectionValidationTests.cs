using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for FI0002 (Missing Each() on collection) and FI0003 (Each() on non-collection).
/// </summary>
public class CollectionValidationTests
{
    [Fact]
    public async Task CollectionWithEach_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Using Each() on collection before navigating deeper
                    var query = context.Orders.IncludePaths(o => o.LineItems.Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CollectionWithWhereAndEach_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Where() followed by Each() on collection
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CollectionAlone_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Just including the collection itself (no deeper navigation)
                    var query = context.Orders.IncludePaths(o => o.LineItems);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CollectionWithEachAlone_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Collection with Each() but no further navigation
                    var query = context.Orders.IncludePaths(o => o.LineItems.Each());
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CollectionWithoutEach_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS1061) catches this because ICollection<T>
        // doesn't have the 'Product' member directly - you need Each() to get the element type.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: Navigating through collection without Each()
                    var query = context.Orders.IncludePaths(o => o.LineItems.{|#0:Product|});
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("System.Collections.Generic.ICollection<TestNamespace.LineItem>", "Product");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task EachOnNonCollection_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS1061) catches this because the Each() extension
        // method is only defined for collection types.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: Using Each() on a non-collection property
                    var query = context.Orders.IncludePaths(o => o.Customer!.{|#0:Each|}());
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Customer", "Each");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task DeepCollectionWithEach_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Deep navigation with proper Each() usage
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Each().Product!.Category);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ReferenceNavigation_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Reference navigation (not a collection)
                    var query = context.Orders.IncludePaths(o => o.Customer);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task DeepReferenceNavigation_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Deep reference navigation
                    var query = context.Orders.IncludePaths(o => o.Customer!.Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }
}
