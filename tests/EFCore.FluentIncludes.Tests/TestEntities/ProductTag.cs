namespace EFCore.FluentIncludes.Tests.TestEntities;

public class ProductTag
{
    public int Id { get; set; }
    public required string Tag { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }
}
