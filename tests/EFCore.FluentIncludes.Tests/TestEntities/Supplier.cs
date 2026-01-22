namespace EFCore.FluentIncludes.Tests.TestEntities;

public class Supplier
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ContactEmail { get; set; }

    // Single navigation
    public int? AddressId { get; set; }
    public Address? Address { get; set; }

    // Collection navigation
    public ICollection<Product> Products { get; set; } = [];
}
