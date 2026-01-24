using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static EFCore.FluentIncludes.Analyzers.Tests.AnalyzerTestHelper;

namespace EFCore.FluentIncludes.Analyzers.Tests;

/// <summary>
/// Tests for the NullableNavigationCodeFixProvider.
/// </summary>
public class NullableNavigationCodeFixTests
{
    /// <summary>
    /// Verifies a code fix transforms the source code correctly.
    /// </summary>
    private static async Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        DiagnosticResult expected)
    {
        var test = new CSharpCodeFixTest<IncludePathAnalyzer, NullableNavigationCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }

    // ===================
    // FI0006: Remove unnecessary To()
    // ===================

    [Fact]
    public async Task RemoveUnnecessaryTo_SimpleFix()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => {|#0:o.Notes.To()|});
                }
            }
        }
        """;

        var fixedSource = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.Notes);
                }
            }
        }
        """;

        var expected = Warning(DiagnosticDescriptors.UnnecessaryTo.Id)
            .WithLocation(0)
            .WithArguments("Notes");

        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    // ===================
    // FI0007: Insert missing To()
    // ===================

    [Fact]
    public async Task InsertMissingTo_SimpleFix()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => {|#0:o.Customer|}.Address);
                }
            }
        }
        """;

        var fixedSource = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(o => o.Customer.To().Address);
                }
            }
        }
        """;

        var expected = Warning(DiagnosticDescriptors.MissingToOnNullable.Id)
            .WithLocation(0)
            .WithArguments("Customer");

        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Fact]
    public async Task InsertMissingTo_DeepNavigation()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(
                        o => {|#0:o.LineItems.Each().Product|}.Category);
                }
            }
        }
        """;

        var fixedSource = TestCodePreamble + """

            public class TestClass
            {
                public void Test(TestDbContext context)
                {
                    var query = context.Orders.IncludePaths(
                        o => o.LineItems.Each().Product.To().Category);
                }
            }
        }
        """;

        var expected = Warning(DiagnosticDescriptors.MissingToOnNullable.Id)
            .WithLocation(0)
            .WithArguments("Product");

        await VerifyCodeFixAsync(source, fixedSource, expected);
    }
}
