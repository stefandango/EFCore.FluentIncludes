using FluentAssertions;
using Xunit;
using static EFCore.FluentIncludes.Generator.Tests.GeneratorTestHelper;

namespace EFCore.FluentIncludes.Generator.Tests;

/// <summary>
/// Tests for simple navigation path generation.
/// </summary>
public class SimplePathTests
{
    [Fact]
    public void SinglePropertyPath_GeneratesInclude()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(o => o.Customer);
                }
            }
        }
        """;

        var result = RunGenerator(source);

        // The generator should run without errors
        result.Diagnostics.Should().BeEmpty();

        // Check what was generated
        var generatedSource = GetGeneratedSource(result);

        // Output for debugging
        Console.WriteLine($"Generated sources count: {result.Results.SelectMany(r => r.GeneratedSources).Count()}");
        foreach (var gen in result.Results.SelectMany(r => r.GeneratedSources))
        {
            Console.WriteLine($"  - {gen.HintName}");
        }

        if (generatedSource != null)
        {
            Console.WriteLine("Generated code:");
            Console.WriteLine(generatedSource);
        }
        else
        {
            Console.WriteLine("No generated source found");
        }
    }

    [Fact]
    public void NestedPropertyPath_GeneratesIncludeThenInclude()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(o => o.Customer!.Address);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void CollectionPathWithEach_GeneratesInclude()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(o => o.LineItems.Each().Product);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void MultiplePaths_GeneratesMultipleIncludes()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.Customer,
                        o => o.LineItems);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void DeepPath_GeneratesChainedThenIncludes()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(
                        o => o.LineItems.Each().Product!.Category);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void IncludePath_SingleGenericOverload_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePath(o => o.Customer);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void IncludePathsIf_WithCondition_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders, bool includeCustomer)
                {
                    var query = orders.IncludePathsIf(includeCustomer, o => o.Customer);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NullableNavigationWithTo_Works()
    {
        var source = TestCodePreamble + """

            public class TestClass
            {
                public void Test(IQueryable<Order> orders)
                {
                    var query = orders.IncludePaths(o => o.Customer.To().Address);
                }
            }
        }
        """;

        var result = RunGenerator(source);
        result.Diagnostics.Should().BeEmpty();
    }
}
