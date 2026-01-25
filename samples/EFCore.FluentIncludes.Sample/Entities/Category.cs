namespace EFCore.FluentIncludes.Sample.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
