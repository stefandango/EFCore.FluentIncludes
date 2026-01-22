namespace EFCore.FluentIncludes.Tests.TestEntities;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }

    // Single navigation
    public int? AddressId { get; set; }
    public Address? Address { get; set; }

    // Collection navigation
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = [];
}
