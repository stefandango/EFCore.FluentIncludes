using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for ordered includes using OrderBy/OrderByDescending/ThenBy/ThenByDescending in path expressions.
/// </summary>
[Collection("Database")]
public class OrderedIncludeTests
{
    private readonly DatabaseFixture _fixture;

    public OrderedIncludeTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OrderByAscending_OrdersItemsCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order LineItems by Id ascending
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderBy(li => li.Id).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        // Order 1 has LineItem Id=1 (iPhone, $999.99) and Id=2 (Phone Case, $29.99)
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInAscendingOrder();
        ids[0].Should().Be(1);
        ids[1].Should().Be(2);
    }

    [Fact]
    public async Task OrderByDescending_OrdersItemsCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order LineItems by Id descending
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderByDescending(li => li.Id).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInDescendingOrder();
        ids[0].Should().Be(2);
        ids[1].Should().Be(1);
    }

    [Fact]
    public async Task OrderBy_WithThenInclude_LoadsNestedNavigation()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order LineItems and include Product for each
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderBy(li => li.Id).Each().Product)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        var items = order.LineItems.ToList();
        items[0].Id.Should().Be(1);
        items[0].Product.Should().NotBeNull();
        items[0].Product!.Name.Should().Be("iPhone 15"); // Id=1 is iPhone
        items[1].Id.Should().Be(2);
        items[1].Product.Should().NotBeNull();
        items[1].Product!.Name.Should().Be("Phone Case"); // Id=2 is Phone Case
    }

    [Fact]
    public async Task OrderBy_WithThenBy_AppliesMultipleOrderings()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order by Quantity, then by Id
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderBy(li => li.Quantity).ThenBy(li => li.Id).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        // Both have Quantity=1, so should be ordered by Id
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OrderBy_WithThenByDescending_MixedOrderings()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Order by Quantity ascending, then by Id descending
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderBy(li => li.Quantity).ThenByDescending(li => li.Id).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        // Both have Quantity=1, so should be ordered by Id descending
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task FilteredAndOrdered_FilterAppliesFirst()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter then order (filter to Id > 1)
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Where(li => li.Id > 1).OrderBy(li => li.Id).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        // Only LineItem Id=2 (Phone Case) passes the filter
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().Id.Should().Be(2);
    }

    [Fact]
    public async Task OrderedNestedCollection_OrdersSecondLevel()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Include LineItems, then order Product.Images by Id
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product!.Images.OrderBy(i => i.Id).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);

        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Images.Should().HaveCount(2);
        var imageIds = iPhone.Product.Images.Select(i => i.Id).ToList();
        imageIds.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OrderedCollection_AsSplitQuery_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Ordered include with split query
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderByDescending(li => li.Id).Each().Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task OrderedCollection_InSpec_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Use a spec with ordered includes
        var order = await context.Orders
            .WithSpec(new OrderedLineItemsSpec())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task OrderedCollection_CombinedWithUnorderedPath_BothWork()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - One ordered path, one unordered
        var order = await context.Orders
            .IncludePaths(
                o => o.LineItems.OrderBy(li => li.Id).Each().Product,
                o => o.Customer!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInAscendingOrder();
        order.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
    }

    [Fact]
    public async Task OrderedAndFiltered_BothApplyCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter to items with Quantity=1, then order by Id descending
        var order = await context.Orders
            .IncludePaths(o => o.LineItems
                .Where(li => li.Quantity == 1)
                .OrderByDescending(li => li.Id)
                .Each()
                .Product)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        // Both items have Quantity=1, so both pass the filter
        order!.LineItems.Should().HaveCount(2);
        var ids = order.LineItems.Select(li => li.Id).ToList();
        ids.Should().BeInDescendingOrder();
        ids[0].Should().Be(2);
        ids[1].Should().Be(1);
    }

    [Fact]
    public async Task OrderByDescendingOnly_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Use only OrderByDescending (not OrderBy then ThenByDescending)
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.OrderByDescending(li => li.ProductId).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        // ProductIds are 1 (iPhone) and 3 (Phone Case)
        var productIds = order.LineItems.Select(li => li.ProductId).ToList();
        productIds.Should().BeInDescendingOrder();
        productIds[0].Should().Be(3);
        productIds[1].Should().Be(1);
    }

    private sealed class OrderedLineItemsSpec : IncludeSpec<Order>
    {
        public OrderedLineItemsSpec()
        {
            Include(o => o.LineItems.OrderByDescending(li => li.Id).Each().Product);
        }
    }
}
