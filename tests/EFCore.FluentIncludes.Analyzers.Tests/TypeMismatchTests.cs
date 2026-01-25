using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for FI0009 (Type mismatch in navigation chain).
/// </summary>
public class TypeMismatchTests
{
    // Note: Most type mismatch scenarios are caught by the C# compiler (CS0030, CS0039)
    // before our analyzer runs. These tests verify behavior when cast validation occurs.

    [Fact]
    public async Task NoCast_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // No cast - simple navigation
                    var query = context.Orders.IncludePaths(o => o.Customer!.Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task CastToSameType_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Cast to same type (redundant but valid)
                    var query = context.Orders.IncludePaths(o => ((Customer)o.Customer!).Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task InvalidCast_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS0030) catches invalid casts before our analyzer.
        // This test verifies the compiler error is reported for completely unrelated types.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // ERROR: Cannot cast Customer to Product (unrelated types)
                    var query = context.Orders.IncludePaths(o => ({|#0:(Product)o.Customer!|}).Category);
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS0030")
            .WithLocation(0)
            .WithArguments("TestNamespace.Customer", "TestNamespace.Product");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task CastToObject_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Cast to object is always valid (upcast)
                    var query = context.Orders.IncludePaths(o => (object)o.Customer!);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task NavigationWithoutCast_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    // Deep navigation without any casts
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Each().Product!.Category);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }
}
