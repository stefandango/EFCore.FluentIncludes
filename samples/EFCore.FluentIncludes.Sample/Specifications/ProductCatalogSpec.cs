using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Specifications;

/// <summary>
/// Specification for loading products with their category hierarchy.
/// Demonstrates navigating nullable references with the To() marker.
/// </summary>
public class ProductCatalogSpec : IncludeSpec<Product>
{
    public ProductCatalogSpec()
    {
        AsNoTracking();
        Include(p => p.Category.To().ParentCategory);
    }
}
