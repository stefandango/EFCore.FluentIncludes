using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for AsSplitQuery() compatibility.
/// </summary>
[Collection("Database")]
public class SplitQueryTests
{
    private readonly DatabaseFixture _fixture;

    public SplitQueryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IncludePaths_WithAsSplitQuery_LoadsAllData()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - IncludePaths with AsSplitQuery
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Category,
                o => o.Payments.Each().PaymentMethod)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - all data should be loaded
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.Count.ShouldBe(2);
        order.LineItems.ShouldAllBe(li => li.Product != null && li.Product.Category != null);
        order.Payments.Count.ShouldBe(1);
        order.Payments.First().PaymentMethod.ShouldNotBeNull();
    }

    [Fact]
    public async Task IncludePaths_WithAsSingleQuery_LoadsAllData()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - IncludePaths with explicit AsSingleQuery
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Category,
                o => o.Payments.Each().PaymentMethod)
            .AsSingleQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - all data should be loaded
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.Count.ShouldBe(2);
        order.Payments.Count.ShouldBe(1);
    }

    [Fact]
    public async Task WithSpec_WithAsSplitQuery_LoadsAllData()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Spec with AsSplitQuery
        var order = await context.Orders
            .WithSpec<Order, OrderFullSpec>()
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - all data should be loaded
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.Count.ShouldBe(2);
        order.Payments.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AsSplitQuery_BeforeIncludePaths_LoadsAllData()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - AsSplitQuery BEFORE IncludePaths (order shouldn't matter)
        var order = await context.Orders
            .AsSplitQuery()
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product!.Category)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SplitQuery_ProducesSameResults_AsSingleQuery()
    {
        // Arrange
        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        // Act - Get same data with split vs single query
        var singleQueryOrder = await context1.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.Customer!.PaymentMethods,
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Discounts)
            .AsSingleQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        var splitQueryOrder = await context2.Orders
            .IncludePaths(
                o => o.Customer!.Address,
                o => o.Customer!.PaymentMethods,
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Discounts)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - both should have identical data
        singleQueryOrder.ShouldNotBeNull();
        splitQueryOrder.ShouldNotBeNull();

        // Customer data
        singleQueryOrder!.Customer!.Id.ShouldBe(splitQueryOrder!.Customer!.Id);
        singleQueryOrder.Customer.Address!.Id.ShouldBe(splitQueryOrder.Customer.Address!.Id);
        singleQueryOrder.Customer.PaymentMethods.Count.ShouldBe(splitQueryOrder.Customer.PaymentMethods.Count);

        // LineItems data
        singleQueryOrder.LineItems.Count.ShouldBe(splitQueryOrder.LineItems.Count);

        var singleLineItem = singleQueryOrder.LineItems.First(li => li.Id == 1);
        var splitLineItem = splitQueryOrder.LineItems.First(li => li.Id == 1);

        singleLineItem.Product!.Id.ShouldBe(splitLineItem.Product!.Id);
        singleLineItem.Product.Category!.Id.ShouldBe(splitLineItem.Product.Category!.Id);
        singleLineItem.Discounts.Count.ShouldBe(splitLineItem.Discounts.Count);
    }
}
