using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Specifications;

/// <summary>
/// Full specification with query options.
/// Demonstrates UseSplitQuery(), AsNoTracking(), and deep navigation paths.
/// </summary>
public class OrderDetailSpec : IncludeSpec<Order>
{
    public OrderDetailSpec()
    {
        UseSplitQuery();
        AsNoTracking();
        Include(
            o => o.Customer!.Address,
            o => o.LineItems.Each().Product!.Category);
    }
}
