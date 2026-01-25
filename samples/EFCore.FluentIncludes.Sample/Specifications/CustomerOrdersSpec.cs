using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Specifications;

/// <summary>
/// Specification for loading customer with orders and line items.
/// Used to demonstrate WithSpecIf() for conditional spec application.
/// </summary>
public class CustomerOrdersSpec : IncludeSpec<Customer>
{
    public CustomerOrdersSpec()
    {
        AsNoTrackingWithIdentityResolution();
        Include(c => c.Orders.Each().LineItems.Each().Product);
    }
}
