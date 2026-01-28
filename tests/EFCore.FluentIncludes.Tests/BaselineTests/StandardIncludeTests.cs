using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.BaselineTests;

/// <summary>
/// Baseline tests using standard EF Core Include/ThenInclude syntax.
/// These establish the expected behavior that our IncludePath library must match.
/// </summary>
[Collection("Database")]
public class StandardIncludeTests
{
    private readonly DatabaseFixture _fixture;

    public StandardIncludeTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleNavigation_LoadsRelatedEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Name.ShouldBe("John Doe");
    }

    [Fact]
    public async Task TwoLevelNavigation_LoadsNestedEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.Address!.City.ShouldBe("New York");
    }

    [Fact]
    public async Task CollectionNavigation_LoadsAllItems()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.LineItems.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CollectionThenSingle_LoadsNestedFromCollection()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.LineItems.Count.ShouldBe(2);
        order.LineItems.ShouldAllBe(li => li.Product != null);
        order.LineItems.Select(li => li.Product!.Name).ShouldContain("iPhone 15");
    }

    [Fact]
    public async Task DeepNesting_ThreeLevels_LoadsAllLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        var iPhone = order!.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Category!.Name.ShouldBe("Smartphones");
    }

    [Fact]
    public async Task DeepNesting_FourLevels_LoadsAllLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core (4 levels: Order -> LineItems -> Product -> Category -> ParentCategory)
        var order = await context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
                        .ThenInclude(c => c!.ParentCategory)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        var iPhone = order!.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Category!.ParentCategory.ShouldNotBeNull();
        iPhone.Product.Category.ParentCategory!.Name.ShouldBe("Phones");
    }

    [Fact]
    public async Task MultipleBranches_SameRoot_LoadsBothBranches()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core (multiple branches from Customer)
        var order = await context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.PaymentMethods)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.PaymentMethods.Count.ShouldBe(2);
    }

    [Fact]
    public async Task MultipleBranches_DifferentRoots_LoadsAll()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.ShippingAddress.ShouldNotBeNull();
        order.BillingAddress.ShouldNotBeNull();
    }

    [Fact]
    public async Task CollectionWithNestedCollection_LoadsBothCollections()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core
        var order = await context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.LineItems.Count.ShouldBe(2);
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Images.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CollectionBranches_FromSameCollection_LoadsBothBranches()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core (Product has both Images and Tags)
        var order = await context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Images)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Tags)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        var iPhone = order!.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Images.Count.ShouldBe(2);
        iPhone.Product.Tags.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ComplexQuery_MultiplePathsAndDepths()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core (the ugly one we want to simplify!)
        var order = await context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.Address)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.PaymentMethods)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Category)
                        .ThenInclude(c => c!.ParentCategory)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Images)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
                    .ThenInclude(p => p!.Supplier)
                        .ThenInclude(s => s!.Address)
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Discounts)
            .Include(o => o.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .Include(o => o.Notes)
                .ThenInclude(n => n.Author)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - verify everything is loaded
        order.ShouldNotBeNull();

        // Customer branch
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.PaymentMethods.Count.ShouldBe(2);

        // Address branches
        order.ShippingAddress.ShouldNotBeNull();
        order.BillingAddress.ShouldNotBeNull();

        // LineItems -> Product -> Category -> ParentCategory
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Category!.ParentCategory.ShouldNotBeNull();

        // LineItems -> Product -> Images
        iPhone.Product.Images.Count.ShouldBe(2);

        // LineItems -> Product -> Supplier -> Address
        iPhone.Product.Supplier.ShouldNotBeNull();
        iPhone.Product.Supplier!.Address.ShouldNotBeNull();

        // LineItems -> Discounts
        iPhone.Discounts.Count.ShouldBe(1);

        // Payments -> PaymentMethod
        order.Payments.Count.ShouldBe(1);
        order.Payments.First().PaymentMethod.ShouldNotBeNull();

        // Notes -> Author
        order.Notes.Count.ShouldBe(1);
        order.Notes.First().Author.ShouldNotBeNull();
    }

    [Fact]
    public async Task SelfReferencingNavigation_LoadsMultipleLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Standard EF Core (Category -> ParentCategory -> ParentCategory)
        var category = await context.Categories
            .Include(c => c.ParentCategory)
                .ThenInclude(c => c!.ParentCategory)
            .FirstOrDefaultAsync(c => c.Name == "Smartphones");

        // Assert
        category.ShouldNotBeNull();
        category!.ParentCategory.ShouldNotBeNull();
        category.ParentCategory!.Name.ShouldBe("Phones");
        category.ParentCategory.ParentCategory.ShouldNotBeNull();
        category.ParentCategory.ParentCategory!.Name.ShouldBe("Electronics");
    }
}
