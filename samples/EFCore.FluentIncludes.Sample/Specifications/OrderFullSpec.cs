using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Specifications;

/// <summary>
/// Full order specification that inherits from OrderDetailSpec and adds more includes.
/// Demonstrates: IncludeFrom&lt;TSpec&gt;() for spec inheritance.
/// </summary>
public class OrderFullSpec : IncludeSpec<Order>
{
    public OrderFullSpec()
    {
        // Inherit all includes from OrderDetailSpec
        IncludeFrom<OrderDetailSpec>();

        // Add additional includes: category parent hierarchy
        Include(o => o.LineItems.Each().Product!.Category!.ParentCategory);
    }
}
