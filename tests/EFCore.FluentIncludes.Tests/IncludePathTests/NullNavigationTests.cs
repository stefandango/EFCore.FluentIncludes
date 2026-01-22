using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests verifying that null navigations are handled gracefully.
/// The ! operator in IncludePaths() is purely cosmetic - the lambda is never executed,
/// so null is never a problem at runtime. EF Core handles null data gracefully.
/// </summary>
[Collection("Database")]
public class NullNavigationTests
{
    private readonly DatabaseFixture _fixture;

    public NullNavigationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that a null navigation at the end of a path is handled gracefully.
    /// Supplier 2 (GadgetWorld) has no Address.
    /// Path: LineItems -> Product -> Supplier -> Address (Address is NULL)
    /// </summary>
    [Fact]
    public async Task NullNavigationInPath_SingleNav_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Product 3 (Phone Case) has Supplier 2 (GadgetWorld) which has no Address
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Supplier!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Should not throw
        order.Should().NotBeNull();
        var phoneCase = order!.LineItems.First(li => li.Product!.Name == "Phone Case");
        phoneCase.Product!.Supplier.Should().NotBeNull();
        phoneCase.Product.Supplier!.Name.Should().Be("GadgetWorld");
        phoneCase.Product.Supplier.Address.Should().BeNull(); // Gracefully null
    }

    /// <summary>
    /// Tests that a null navigation in the middle of a path is handled gracefully.
    /// Product 4 (Generic Item) has no Supplier (SupplierId = null).
    /// Path: LineItems -> Product -> Supplier -> Address (Supplier is NULL)
    /// </summary>
    [Fact]
    public async Task NullNavigationMidPath_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order 4 has Product 4 (Generic Item) which has no Supplier
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Supplier!.Address)
            .FirstOrDefaultAsync(o => o.Id == 4);

        // Assert - Should not throw
        order.Should().NotBeNull();
        var genericItem = order!.LineItems.First(li => li.Product!.Name == "Generic Item");
        genericItem.Product!.Supplier.Should().BeNull(); // Gracefully null - no supplier at all
    }

    /// <summary>
    /// Tests that a null navigation at the end of a path is handled gracefully.
    /// Customer 3 has no Address (AddressId = null).
    /// </summary>
    [Fact]
    public async Task NullAtEndOfPath_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order 4 has Customer 3 who has no Address
        var order = await context.Orders
            .IncludePaths(o => o.Customer!.Address)
            .FirstOrDefaultAsync(o => o.Id == 4);

        // Assert - Should not throw
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Name.Should().Be("No Address Customer");
        order.Customer.Address.Should().BeNull(); // Gracefully null
    }

    /// <summary>
    /// Tests that mixed null and non-null navigations in a collection are handled gracefully.
    /// Order 4 has: Product with Supplier (no address) and Product with no Supplier.
    /// </summary>
    [Fact]
    public async Task MixedNullAndNonNull_InCollection_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order 4 has mixed products
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Supplier!.Address)
            .FirstOrDefaultAsync(o => o.Id == 4);

        // Assert - Should not throw
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);

        // Phone Case has Supplier with NO Address
        var phoneCase = order.LineItems.First(li => li.Product!.Name == "Phone Case");
        phoneCase.Product!.Supplier.Should().NotBeNull();
        phoneCase.Product.Supplier!.Address.Should().BeNull();

        // Generic Item has NO Supplier at all
        var genericItem = order.LineItems.First(li => li.Product!.Name == "Generic Item");
        genericItem.Product!.Supplier.Should().BeNull();
    }

    /// <summary>
    /// Tests handling of products with and without suppliers in the same query.
    /// Order 1 has iPhone (with supplier+address) and Phone Case (with supplier, no address).
    /// </summary>
    [Fact]
    public async Task MixedSupplierStatus_InCollection_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Supplier!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);

        // iPhone has Supplier with Address
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Supplier.Should().NotBeNull();
        iPhone.Product.Supplier!.Address.Should().NotBeNull();

        // Phone Case has Supplier with NO Address
        var phoneCase = order.LineItems.First(li => li.Product!.Name == "Phone Case");
        phoneCase.Product!.Supplier.Should().NotBeNull();
        phoneCase.Product.Supplier!.Address.Should().BeNull();
    }

    /// <summary>
    /// Tests a deep null chain with self-referencing navigation.
    /// Category hierarchy: Smartphones -> Phones -> Electronics -> NULL
    /// </summary>
    [Fact]
    public async Task DeepNullChain_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Include 3 levels of parent categories
        var category = await context.Categories
            .IncludePaths(c => c.ParentCategory!.ParentCategory!.ParentCategory)
            .FirstOrDefaultAsync(c => c.Name == "Smartphones");

        // Assert - Should not throw
        category.Should().NotBeNull();
        category!.ParentCategory.Should().NotBeNull();
        category.ParentCategory!.Name.Should().Be("Phones");
        category.ParentCategory.ParentCategory.Should().NotBeNull();
        category.ParentCategory.ParentCategory!.Name.Should().Be("Electronics");
        category.ParentCategory.ParentCategory.ParentCategory.Should().BeNull(); // Top level - no parent
    }

    /// <summary>
    /// Tests multiple paths with varying null states in a complex query.
    /// Combines null at different levels across multiple paths.
    /// </summary>
    [Fact]
    public async Task ComplexQuery_WithNullsAtDifferentLevels_HandledGracefully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order 4 has customer with no address, and products with varying supplier status
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Supplier!.Address,
                o => o.LineItems.Each().Product!.Category!.ParentCategory)
            .FirstOrDefaultAsync(o => o.Id == 4);

        // Assert
        order.Should().NotBeNull();

        // Customer has no address
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().BeNull();

        // Generic Item has no supplier
        var genericItem = order.LineItems.First(li => li.Product!.Name == "Generic Item");
        genericItem.Product!.Supplier.Should().BeNull();
        genericItem.Product.Category.Should().NotBeNull();
        genericItem.Product.Category!.Name.Should().Be("Accessories");
        genericItem.Product.Category.ParentCategory.Should().NotBeNull();
        genericItem.Product.Category.ParentCategory!.Name.Should().Be("Electronics");
    }
}
