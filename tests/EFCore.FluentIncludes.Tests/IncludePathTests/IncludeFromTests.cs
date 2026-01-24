using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for IncludeFrom functionality - grouping multiple paths from a common base.
/// </summary>
[Collection("Database")]
public class IncludeFromTests
{
    private readonly DatabaseFixture _fixture;

    public IncludeFromTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    #region Basic Collection Grouping

    [Fact]
    public async Task IncludeFrom_Collection_GroupsMultiplePaths()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Group multiple paths from LineItems
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product,
                li => li.Discounts)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li =>
        {
            li.Product.Should().NotBeNull();
            li.Discounts.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task IncludeFrom_FilteredCollection_GroupsMultiplePaths()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter to high-value items and include multiple sub-paths
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each(),
                li => li.Product,
                li => li.Discounts)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        // Order 1 has iPhone ($999.99) and Phone Case ($29.99) - only iPhone should be included
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().UnitPrice.Should().Be(999.99m);
        order.LineItems.First().Product.Should().NotBeNull();
        order.LineItems.First().Discounts.Should().NotBeNull();
    }

    [Fact]
    public async Task IncludeFrom_Collection_WithDeepNesting()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Deep nesting in sub-paths
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product!.Category!.ParentCategory,
                li => li.Product!.Images.Each(),
                li => li.Product!.Supplier.To().Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);

        var iPhoneItem = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhoneItem.Product!.Category.Should().NotBeNull();
        iPhoneItem.Product.Category!.ParentCategory.Should().NotBeNull();
        iPhoneItem.Product.Images.Should().HaveCount(2);
        iPhoneItem.Product.Supplier.Should().NotBeNull();
        iPhoneItem.Product.Supplier!.Address.Should().NotBeNull();
    }

    #endregion

    #region Reference Navigation Grouping

    [Fact]
    public async Task IncludeFrom_ReferenceNavigation_GroupsMultiplePaths()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Group paths from Customer
        var order = await context.Orders
            .IncludeFrom(
                o => o.Customer.To(),
                c => c.Address,
                c => c.PaymentMethods.Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.Customer.PaymentMethods.Should().HaveCount(2);
    }

    [Fact]
    public async Task IncludeFrom_ReferenceNavigation_WithoutToMarker()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Without .To() marker (still works, just nullable)
        var order = await context.Orders
            .IncludeFrom(
                o => o.Customer!,
                c => c.Address,
                c => c.PaymentMethods.Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.Customer.PaymentMethods.Should().HaveCount(2);
    }

    #endregion

    #region Multiple IncludeFrom Calls

    [Fact]
    public async Task MultipleIncludeFromCalls_WorkTogether()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Multiple IncludeFrom calls on same query
        var order = await context.Orders
            .IncludeFrom(
                o => o.Customer.To(),
                c => c.Address,
                c => c.PaymentMethods.Each())
            .IncludeFrom(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each(),
                li => li.Product!.Category,
                li => li.Discounts.Each())
            .IncludeFrom(
                o => o.Payments.Each(),
                p => p.PaymentMethod)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();

        // Customer group
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.Customer.PaymentMethods.Should().HaveCount(2);

        // Filtered LineItems group
        order.LineItems.Should().HaveCount(1);
        order.LineItems.First().Product.Should().NotBeNull();
        order.LineItems.First().Product!.Category.Should().NotBeNull();

        // Payments group
        order.Payments.Should().HaveCount(1);
        order.Payments.First().PaymentMethod.Should().NotBeNull();
    }

    [Fact]
    public async Task IncludeFrom_MixedWithIncludePaths_WorksTogether()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Mix IncludeFrom with regular IncludePaths
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product,
                li => li.Discounts.Each())
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.ShippingAddress)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();

        // From IncludeFrom
        order!.LineItems.Should().HaveCount(2);
        order.LineItems.First().Product.Should().NotBeNull();

        // From IncludePaths
        order.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.ShippingAddress.Should().NotBeNull();
    }

    #endregion

    #region Nested Filters in Sub-paths

    [Fact]
    public async Task IncludeFrom_WithNestedFilterInSubPath()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Nested filter in sub-path
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product!.Images.Where(i => i.IsPrimary).Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);

        var iPhoneItem = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        // iPhone has 2 images but only 1 is primary
        iPhoneItem.Product!.Images.Should().HaveCount(1);
        iPhoneItem.Product.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task IncludeFrom_FilteredBase_WithNestedFilterInSubPath()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Both base and sub-path have filters
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each(),
                li => li.Product!.Images.Where(i => i.IsPrimary).Each(),
                li => li.Product!.Tags.Where(t => t.Tag == "premium").Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);

        var lineItem = order.LineItems.First();
        lineItem.Product!.Images.Should().HaveCount(1);
        lineItem.Product.Images.First().IsPrimary.Should().BeTrue();
        lineItem.Product.Tags.Should().HaveCount(1);
        lineItem.Product.Tags.First().Tag.Should().Be("premium");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task IncludeFrom_NoSubPaths_IncludesBaseOnly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - No sub-paths, just the base
        var order = await context.Orders
            .IncludeFrom(o => o.LineItems.Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        // Products should NOT be loaded since we didn't specify sub-paths
        order.LineItems.Should().AllSatisfy(li => li.Product.Should().BeNull());
    }

    [Fact]
    public async Task IncludeFrom_SingleSubPath_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Single sub-path
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li => li.Product.Should().NotBeNull());
    }

    [Fact]
    public async Task IncludeFrom_EmptyFilterResult_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Filter that matches nothing
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Where(li => li.UnitPrice > 10000).Each(),
                li => li.Product)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task IncludeFrom_AsSplitQuery_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - With split query
        var order = await context.Orders
            .IncludeFrom(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each(),
                li => li.Product,
                li => li.Discounts.Each())
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1);
        order.LineItems.First().Product.Should().NotBeNull();
    }

    #endregion

    #region Conditional IncludeFrom

    [Fact]
    public async Task IncludeFromIf_WhenTrue_AppliesIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeLineItems = true;

        // Act
        var order = await context.Orders
            .IncludeFromIf(
                includeLineItems,
                o => o.LineItems.Each(),
                li => li.Product,
                li => li.Discounts.Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        order.LineItems.First().Product.Should().NotBeNull();
    }

    [Fact]
    public async Task IncludeFromIf_WhenFalse_DoesNotApplyIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeLineItems = false;

        // Act
        var order = await context.Orders
            .IncludeFromIf(
                includeLineItems,
                o => o.LineItems.Each(),
                li => li.Product,
                li => li.Discounts.Each())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().BeEmpty(); // Not loaded
    }

    #endregion

    #region IncludeFrom in Specs

    [Fact]
    public async Task IncludeFrom_InSpec_Works()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var order = await context.Orders
            .WithSpec(new OrderWithGroupedIncludesSpec())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(1); // Filtered to > 100
        order.LineItems.First().Product.Should().NotBeNull();
        order.LineItems.First().Product!.Category.Should().NotBeNull();
        order.LineItems.First().Discounts.Should().NotBeNull();
    }

    [Fact]
    public async Task IncludeFrom_InSpec_MixedWithRegularIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var order = await context.Orders
            .WithSpec(new OrderMixedIncludesSpec())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();

        // From IncludeFrom
        order!.LineItems.Should().HaveCount(2);
        order.LineItems.First().Product.Should().NotBeNull();

        // From regular Include
        order.Customer.Should().NotBeNull();
        order.Payments.Should().HaveCount(1);
        order.Payments.First().PaymentMethod.Should().NotBeNull();
    }

    [Fact]
    public async Task IncludeFrom_InSpec_MultipleGroups()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var order = await context.Orders
            .WithSpec(new OrderFullWithGroupsSpec())
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();

        // Customer group
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.Customer.PaymentMethods.Should().HaveCount(2);

        // LineItems group
        order.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li =>
        {
            li.Product.Should().NotBeNull();
            li.Discounts.Should().NotBeNull();
        });

        // Payments group
        order.Payments.Should().HaveCount(1);
        order.Payments.First().PaymentMethod.Should().NotBeNull();
    }

    #endregion

    #region Test Specifications

    private sealed class OrderWithGroupedIncludesSpec : IncludeSpec<Order>
    {
        public OrderWithGroupedIncludesSpec()
        {
            IncludeFrom(
                o => o.LineItems.Where(li => li.UnitPrice > 100).Each(),
                li => li.Product!.Category,
                li => li.Discounts.Each());
        }
    }

    private sealed class OrderMixedIncludesSpec : IncludeSpec<Order>
    {
        public OrderMixedIncludesSpec()
        {
            // Regular includes
            Include(o => o.Customer);
            Include(o => o.Payments.Each().PaymentMethod);

            // Grouped includes
            IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product);
        }
    }

    private sealed class OrderFullWithGroupsSpec : IncludeSpec<Order>
    {
        public OrderFullWithGroupsSpec()
        {
            // Group 1: Customer details
            IncludeFrom(
                o => o.Customer.To(),
                c => c.Address,
                c => c.PaymentMethods.Each());

            // Group 2: Line items
            IncludeFrom(
                o => o.LineItems.Each(),
                li => li.Product,
                li => li.Discounts.Each());

            // Group 3: Payments
            IncludeFrom(
                o => o.Payments.Each(),
                p => p.PaymentMethod);
        }
    }

    #endregion
}
