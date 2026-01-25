using FluentAssertions;
using Xunit;
using static EFCore.FluentIncludes.Generator.Tests.GeneratorTestHelper;

namespace EFCore.FluentIncludes.Generator.Tests;

/// <summary>
/// Tests for fallback scenarios where the generator cannot generate code.
/// </summary>
public class FallbackTests
{
    [Fact]
    public void ExpressionAsVariable_FallsBackToRuntime()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    Expression<Func<Order, object?>> path = o => o.Customer;
                    var query = orders.IncludePaths(path);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        // Generator should run without errors, but not generate code for this pattern
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void CapturedVariable_InFilter_FallsBackToRuntime()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders, int minId)
                {
                    // This captures 'minId' - should fall back to runtime
                    var query = orders.IncludePaths(
                        o => o.LineItems.Where(li => li.Id > minId).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);

        // Generator should run without errors
        // The fallback should be handled gracefully
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void InstanceField_InFilter_FallsBackToRuntime()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                private bool _onlyActive = true;

                public void Test(IQueryable<Order> orders)
                {
                    // This captures '_onlyActive' - should fall back to runtime
                    var query = orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive == _onlyActive).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NoLambdaArguments_HandledGracefully()
    {
        // This is technically invalid usage but the generator should handle it gracefully
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths();
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }
}
