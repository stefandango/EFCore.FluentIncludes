namespace EFCore.FluentIncludes.Sample.Entities;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public int? AddressId { get; set; }
    public Address? Address { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}
