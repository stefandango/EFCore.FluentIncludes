using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for IncludePaths extension methods.
/// These should produce identical results to the baseline StandardIncludeTests.
/// </summary>
[Collection("Database")]
public class IncludePathTests
{
    private readonly DatabaseFixture _fixture;

    public IncludePathTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleNavigation_LoadsRelatedEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - IncludePaths syntax
        var order = await context.Orders
            .IncludePaths(o => o.Customer)
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

        // Act - IncludePaths syntax (single path through nested properties)
        var order = await context.Orders
            .IncludePaths(o => o.Customer!.Address)
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

        // Act - IncludePaths syntax
        var order = await context.Orders
            .IncludePaths(o => o.LineItems)
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

        // Act - IncludePaths syntax with Each()
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product)
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

        // Act - IncludePaths syntax
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Category)
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

        // Act - IncludePaths syntax (4 levels)
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Category!.ParentCategory)
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

        // Act - IncludePaths syntax (multiple paths from Customer)
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.Customer!.PaymentMethods)
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

        // Act - IncludePaths syntax
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.ShippingAddress,
                o => o.BillingAddress)
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

        // Act - IncludePaths syntax
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Images)
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

        // Act - IncludePaths syntax (Product has both Images and Tags)
        var order = await context.Orders
            .IncludePaths(
                o => o.LineItems.Each().Product!.Images,
                o => o.LineItems.Each().Product!.Tags)
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

        // Act - IncludePaths syntax (the clean version!)
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.Customer!.PaymentMethods,
                o => o.ShippingAddress,
                o => o.BillingAddress,
                o => o.LineItems.Each().Product!.Category!.ParentCategory,
                o => o.LineItems.Each().Product!.Images,
                o => o.LineItems.Each().Product!.Supplier!.Address,
                o => o.LineItems.Each().Discounts,
                o => o.Payments.Each().PaymentMethod,
                o => o.Notes.Each().Author)
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

        // Act - IncludePaths syntax
        var category = await context.Categories
            .IncludePaths(c => c.ParentCategory!.ParentCategory)
            .FirstOrDefaultAsync(c => c.Name == "Smartphones");

        // Assert
        category.ShouldNotBeNull();
        category!.ParentCategory.ShouldNotBeNull();
        category.ParentCategory!.Name.ShouldBe("Phones");
        category.ParentCategory.ParentCategory.ShouldNotBeNull();
        category.ParentCategory.ParentCategory!.Name.ShouldBe("Electronics");
    }

    [Fact]
    public async Task IncludePathsIf_WhenTrue_AppliesIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeCustomer = true;

        // Act
        var order = await context.Orders
            .IncludePathsIf(includeCustomer, o => o.Customer!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
    }

    [Fact]
    public async Task IncludePathsIf_WhenFalse_DoesNotApplyIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeCustomer = false;

        // Act
        var order = await context.Orders
            .IncludePathsIf(includeCustomer, o => o.Customer!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldBeNull(); // Not loaded because condition was false
    }
}
