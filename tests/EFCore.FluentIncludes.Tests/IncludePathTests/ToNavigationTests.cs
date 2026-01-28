using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for the To() navigation marker method.
/// To() provides a semantic alternative to the ! operator for nullable navigations.
/// </summary>
[Collection("Database")]
public class ToNavigationTests
{
    private readonly DatabaseFixture _fixture;

    public ToNavigationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task To_SingleNavigation_LoadsRelatedEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Using To() instead of !
        var order = await context.Orders
            .IncludePaths(o => o.Customer.To().Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.Address!.City.ShouldBe("New York");
    }

    [Fact]
    public async Task To_DeepNavigation_LoadsAllLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Multiple To() calls in a chain
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product.To().Category.To().ParentCategory)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        var iPhone = order!.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Category!.ParentCategory.ShouldNotBeNull();
        iPhone.Product.Category.ParentCategory!.Name.ShouldBe("Phones");
    }

    [Fact]
    public async Task To_WithEach_CombinedCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Combining Each() and To()
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product.To().Supplier.To().Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        var iPhone = order!.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Supplier.ShouldNotBeNull();
        iPhone.Product.Supplier!.Address.ShouldNotBeNull();
        iPhone.Product.Supplier.Address!.City.ShouldBe("Detroit");
    }

    [Fact]
    public async Task To_MultiplePaths_AllLoaded()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Multiple paths with To()
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer.To().Address,
                o => o.Customer.To().PaymentMethods,
                o => o.LineItems.Each().Product.To().Category)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.PaymentMethods.Count.ShouldBe(2);
        order.LineItems.ShouldAllBe(li => li.Product!.Category != null);
    }

    [Fact]
    public async Task To_SelfReferencingNavigation_LoadsMultipleLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Self-referencing with To()
        var category = await context.Categories
            .IncludePaths(c => c.ParentCategory.To().ParentCategory)
            .FirstOrDefaultAsync(c => c.Name == "Smartphones");

        // Assert
        category.ShouldNotBeNull();
        category!.ParentCategory.ShouldNotBeNull();
        category.ParentCategory!.Name.ShouldBe("Phones");
        category.ParentCategory.ParentCategory.ShouldNotBeNull();
        category.ParentCategory.ParentCategory!.Name.ShouldBe("Electronics");
    }

    [Fact]
    public async Task To_NullNavigation_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Customer 3 has no address, using To() should still work
        var order = await context.Orders
            .IncludePaths(o => o.Customer.To().Address)
            .FirstOrDefaultAsync(o => o.Id == 4);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Name.ShouldBe("No Address Customer");
        order.Customer.Address.ShouldBeNull(); // Gracefully null
    }

    [Fact]
    public async Task To_ComplexQuery_AllDataLoaded()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Complex query using To() throughout
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer.To().Address,
                o => o.Customer.To().PaymentMethods,
                o => o.ShippingAddress,
                o => o.BillingAddress,
                o => o.LineItems.Each().Product.To().Category.To().ParentCategory,
                o => o.LineItems.Each().Product.To().Images,
                o => o.LineItems.Each().Product.To().Supplier.To().Address,
                o => o.LineItems.Each().Discounts,
                o => o.Payments.Each().PaymentMethod,
                o => o.Notes.Each().Author)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
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
    public async Task To_WithIncludePathsIf_WorksCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeDetails = true;

        // Act
        var order = await context.Orders
            .IncludePaths(o => o.Customer)
            .IncludePathsIf(includeDetails,
                o => o.Customer.To().Address,
                o => o.LineItems.Each().Product.To().Category)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.ShouldAllBe(li => li.Product!.Category != null);
    }
}
