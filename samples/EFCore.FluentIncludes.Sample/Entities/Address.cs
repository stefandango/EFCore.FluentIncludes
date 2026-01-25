namespace EFCore.FluentIncludes.Sample.Entities;

public class Address
{
    public int Id { get; set; }
    public required string Street { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
}
