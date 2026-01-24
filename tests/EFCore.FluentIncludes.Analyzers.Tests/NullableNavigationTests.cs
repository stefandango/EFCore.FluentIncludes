using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for FI0006 (Unnecessary To()) and FI0007 (Missing To() on nullable).
/// </summary>
public class NullableNavigationTests
{
    // ===================
    // FI0006: Unnecessary To()
    // ===================

    [Fact]
    public async Task ToOnNullableProperty_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: To() on nullable property
                    var query = context.Orders.IncludePaths(o => o.Customer.To().Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ToOnNonNullableProperty_ReportsFI0006()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // WARNING: To() is unnecessary on non-nullable 'Notes' property
                    var query = context.Orders.IncludePaths(o => {|#0:o.Notes.To()|});
                }
            }
        }
        """;

        var expected = Warning(DiagnosticDescriptors.UnnecessaryTo.Id)
            .WithLocation(0)
            .WithArguments("Notes");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    // ===================
    // FI0007: Missing To() on nullable
    // ===================

    [Fact]
    public async Task NullablePropertyWithTo_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Using To() on nullable before further navigation
                    var query = context.Orders.IncludePaths(o => o.Customer.To().Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task NullablePropertyWithNullForgiving_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Using ! operator on nullable before further navigation
                    var query = context.Orders.IncludePaths(o => o.Customer!.Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task NullablePropertyAlone_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Just including the nullable property (no deeper navigation)
                    var query = context.Orders.IncludePaths(o => o.Customer);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task NullablePropertyWithoutTo_ReportsFI0007()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // WARNING: Navigating through nullable without To() or !
                    var query = context.Orders.IncludePaths(o => {|#0:o.Customer|}.Address);
                }
            }
        }
        """;

        var expected = Warning(DiagnosticDescriptors.MissingToOnNullable.Id)
            .WithLocation(0)
            .WithArguments("Customer");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task DeepNullableChain_ReportsFI0007()
    {
        // Note: Only Product is flagged because Category is at the end of the path
        // (no navigation AFTER Category). FI0007 only fires when there's subsequent navigation.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // WARNING: Nullable navigation without handling (Product has Category after it)
                    var query = context.Orders.IncludePaths(
                        o => {|#0:o.LineItems.Each().Product|}.Category);
                }
            }
        }
        """;

        var expected = Warning(DiagnosticDescriptors.MissingToOnNullable.Id)
            .WithLocation(0)
            .WithArguments("Product");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task DeepNullableChainWithTo_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Proper To() usage throughout
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Each().Product.To().Category);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task NonNullableNavigation_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Correct: Non-nullable property doesn't need To()
                    var query = context.Orders.IncludePaths(o => o.LineItems);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }
}
