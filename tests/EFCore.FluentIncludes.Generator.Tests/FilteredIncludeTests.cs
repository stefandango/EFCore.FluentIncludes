using Shouldly;
using Xunit;
using static EFCore.FluentIncludes.Generator.Tests.GeneratorTestHelper;

namespace EFCore.FluentIncludes.Generator.Tests;

/// <summary>
/// Tests for filtered include generation.
/// </summary>
public class FilteredIncludeTests
{
    [Fact]
    public void FilteredCollection_WithInlineLambda_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive).Each().Product);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void FilteredCollection_WithPropertyComparison_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.Where(li => li.Id > 0).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void OrderedCollection_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.Id).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void OrderedDescending_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.OrderByDescending(li => li.Id).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void FilterAndOrder_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.Where(li => li.IsActive).OrderBy(li => li.Id).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void ThenByOrdering_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.OrderBy(li => li.IsActive).ThenBy(li => li.Id).Each());
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.ShouldBeEmpty();
    }
}
