using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for filtered includes using Where() in path expressions.
/// </summary>
[Collection("Database")]
public class FilteredIncludeTests
{
    private readonly DatabaseFixture _fixture;

    public FilteredIncludeTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FilteredCollection_FiltersItemsCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter LineItems to only include those with UnitPrice > 100
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Where(li => li.UnitPrice > 100).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        // Order 1 has 2 line items: iPhone ($999.99) and Phone Case ($29.99)
        // Only iPhone should be included
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().UnitPrice.Should().Be(999.99m);
    }

    [Fact]
    public async Task FilteredCollection_WithThenInclude_LoadsNestedNavigation()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter LineItems and include Product for each
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().Product.Should().NotBeNull();
        order.LineItems.First().Product!.Name.Should().Be("iPhone 15");
    }

    [Fact]
    public async Task FilteredCollection_DeepNesting_LoadsAllLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter LineItems and go deep: Product -> Category -> ParentCategory
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product!.Category!.ParentCategory)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        var lineItem = order.LineItems.First();
        lineItem.Product.Should().NotBeNull();
        lineItem.Product!.Category.Should().NotBeNull();
        lineItem.Product.Category!.ParentCategory.Should().NotBeNull();
        lineItem.Product.Category.ParentCategory!.Name.Should().Be("Phones");
    }

    [Fact]
    public async Task FilteredNestedCollection_FiltersSecondLevel()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Include LineItems, then filter Product.Images to only primary
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Images.Where(i => i.IsPrimary).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);

        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        // iPhone has 2 images, but only 1 is primary
        iPhone.Product!.Images.Should().HaveCount(1);
        iPhone.Product.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task FilteredCollection_EmptyResult_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter with condition that matches nothing
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Where(li => li.UnitPrice > 10000).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task FilteredCollection_WithConflictingFilters_ThrowsEFCoreException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act & Assert
        // EF Core does not allow different filters on the same navigation in one query
        var act = async () => await context.Orders
            .IncludePaths(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product,
                o => o.LineItems.Where(li => li.UnitPrice < 50).Each().Discounts)
            .FirstOrDefaultAsync(o => o.Id == 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only one unique filter per navigation*");
    }

    [Fact]
    public async Task FilteredCollection_SameFilterMultiplePaths_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Same filter on multiple paths is allowed
        var order = await context.Orders
            .IncludePaths(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product,
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Discounts)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().Product.Should().NotBeNull();
        order.LineItems.First().Discounts.Should().NotBeNull();
    }

    [Fact]
    public async Task FilteredCollection_WithStringComparison_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter products by tag
        var product = await context.Products
            .IncludePaths(p => p.Tags.Where(t => t.Tag == "premium").Each())
            .FirstOrDefaultAsync(p => p.Id == 1); // iPhone

        // Assert
        product.Should().NotBeNull();
        // iPhone has tags: "premium", "bestseller" - only "premium" should be loaded
        product!.Tags.Should().HaveCount(1);
        product.Tags.First().Tag.Should().Be("premium");
    }

    [Fact]
    public async Task FilteredCollection_AsSplitQuery_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filtered include with split query
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().Product.Should().NotBeNull();
    }

    [Fact]
    public async Task FilteredCollection_CombinedWithUnfilteredPath_BothWork()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - One filtered path, one unfiltered
        var order = await context.Orders
            .IncludePaths(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product,
                o => o.Customer!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().Product.Should().NotBeNull();
        order.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
    }

    [Fact]
    public async Task FilteredCollection_InSpec_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Use a spec with filtered includes
        var order = await context.Orders
            .WithSpec(new HighValueLineItemsSpec())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().UnitPrice.Should().BeGreaterThan(100);
    }

    private sealed class HighValueLineItemsSpec : IncludeSpec<Order>
    {
        public HighValueLineItemsSpec()
        {
            Include(o => o.LineItems.Where(li => li.UnitPrice > 100).Each().Product);
        }
    }
}
