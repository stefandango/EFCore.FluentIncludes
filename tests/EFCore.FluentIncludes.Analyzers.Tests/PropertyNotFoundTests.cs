using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for FI0001: Property not found diagnostic.
/// </summary>
public class PropertyNotFoundTests
{
    [Fact]
    public async Task ValidProperty_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.Customer);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ValidNestedProperty_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.Customer!.Address);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task ValidCollectionProperty_NoDiagnostic()
    {
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.LineItems.Each().Product);
                }
            }
        }
        """;

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode);
    }

    [Fact]
    public async Task InvalidProperty_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS1061) catches invalid property access before our analyzer.
        // This test verifies the compiler error is reported.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.{|#0:NonExistent|});
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Order", "NonExistent");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task InvalidNestedProperty_CompilerCatchesIt()
    {
        // Note: The C# compiler (CS1061) catches invalid property access before our analyzer.
        var testCode = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.Customer!.{|#0:InvalidProp|});
                }
            }
        }
        """;

        var expected = DiagnosticResult.CompilerError("CS1061")
            .WithLocation(0)
            .WithArguments("TestNamespace.Customer", "InvalidProp");

        await VerifyAnalyzerAsync<IncludePathAnalyzer>(testCode, expected);
    }
}
