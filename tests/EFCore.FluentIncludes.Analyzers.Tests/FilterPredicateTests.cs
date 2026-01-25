using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for FI0008 (Invalid property in filter predicate).
/// </summary>
public class FilterPredicateTests
{
    // ===================
    // FI0008: Invalid property in filter predicate
    // ===================

    [Fact]
    public async Task ValidPropertyInWherePredicate_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Valid property reference in Where predicate
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ValidPropertyInOrderByPredicate_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Valid property reference in OrderBy predicate
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.UnitPrice).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task MultipleValidPropertiesInPredicate_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Multiple valid property references in predicate
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive && li.Quantity > 0).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task InvalidPropertyInWherePredicate_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS1061) catches invalid property access in predicates
        // before our analyzer. This test verifies the compiler error is reported.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: NonExistent property doesn't exist on LineItem
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.{|#0:NonExistent|}).Each().Product);
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.LineItem", "NonExistent");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task InvalidPropertyInOrderByPredicate_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS1061) catches invalid property access in predicates.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: InvalidProp property doesn't exist on LineItem
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.{|#0:InvalidProp|}).Each().Product);
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.LineItem", "InvalidProp");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task ValidNavigationInPredicate_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Valid navigation property access in predicate
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.Product != null).Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ValidNestedNavigationInPredicate_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Valid nested navigation in predicate
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.Product!.Name != "").Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task InvalidNestedNavigationInPredicate_CompilerCatchesIt()
    {
        // Note: The C# compiler catches invalid nested property access in predicates.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: InvalidProp doesn't exist on Product
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Where(li => li.Product!.{|#0:InvalidProp|} != "").Each().Product);
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Product", "InvalidProp");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task ComplexPredicateWithValidProperties_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Complex predicate with valid properties
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems
                            .Where(li => li.IsActive && li.UnitPrice > 0 && li.Quantity >= 1)
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
}
