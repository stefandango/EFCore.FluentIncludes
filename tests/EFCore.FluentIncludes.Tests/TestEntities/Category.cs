namespace EFCore.FluentIncludes.Tests.TestEntities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Self-referencing navigation (for deep nesting tests)
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    // Collection navigations
    public ICollection<Category> SubCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
