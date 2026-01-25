using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Specifications;

/// <summary>
/// Basic specification - includes only the customer for order summary display.
/// Demonstrates the simplest use of IncludeSpec.
/// </summary>
public class OrderSummarySpec : IncludeSpec<Order>
{
    public OrderSummarySpec()
    {
        Include(o => o.Customer);
    }
}
