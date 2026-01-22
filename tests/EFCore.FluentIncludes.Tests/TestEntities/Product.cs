namespace EFCore.FluentIncludes.Tests.TestEntities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Sku { get; set; }
    public decimal Price { get; set; }

    // Single navigations
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // Collection navigation
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductTag> Tags { get; set; } = [];
}
