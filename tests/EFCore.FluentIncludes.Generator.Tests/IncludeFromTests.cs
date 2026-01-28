using Shouldly;
using Xunit;
using static EFCore.FluentIncludes.Generator.Tests.GeneratorTestHelper;

namespace EFCore.FluentIncludes.Generator.Tests;

/// <summary>
/// Tests for IncludeFrom and IncludeFromIf method generation.
/// </summary>
public class IncludeFromTests
{
    [Fact]
    public void IncludeFrom_WithSubPaths_GeneratesInterceptor()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludeFrom(
                        o => o.LineItems.Each(),
                        li => li.Product);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        // The generator should run without errors
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void IncludeFrom_WithMultipleSubPaths_GeneratesInterceptor()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludeFrom(
                        o => o.LineItems.Each(),
                        li => li.Product,
                        li => li.Product!.Category);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void IncludeFrom_WithFilteredBase_GeneratesInterceptor()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludeFrom(
                        o => o.LineItems.Where(li => li.IsActive).Each(),
                        li => li.Product);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void IncludeFrom_WithReferenceNavigation_GeneratesInterceptor()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludeFrom(
                        o => o.Customer.To(),
                        c => c.Address);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void IncludeFromIf_WithCondition_GeneratesInterceptor()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders, bool includeItems)
                {
                    var query = orders.IncludeFromIf(
                        includeItems,
                        o => o.LineItems.Each(),
                        li => li.Product);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void IncludeFromIf_WithMultipleSubPaths_GeneratesInterceptor()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders, bool includeDetails)
                {
                    var query = orders.IncludeFromIf(
                        includeDetails,
                        o => o.LineItems.Each(),
                        li => li.Product,
                        li => li.Product!.Category);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
    }
}
