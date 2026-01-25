using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Specifications;

/// <summary>
/// Simple specification for loading customer with address.
/// Used to demonstrate WithSpecIf() for conditional spec application.
/// </summary>
public class CustomerAddressSpec : IncludeSpec<Customer>
{
    public CustomerAddressSpec()
    {
        Include(c => c.Address);
    }
}
