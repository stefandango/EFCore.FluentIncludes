using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for different nullability syntax options in include paths.
/// </summary>
[Collection("Database")]
public class NullabilitySyntaxTests
{
    private readonly DatabaseFixture _fixture;

    public NullabilitySyntaxTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WithNullForgiving_WorksCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Using ! (null-forgiving operator)
        var order = await context.Orders
            .IncludePaths(o => o.Customer!.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
    }

    [Fact]
    public async Task WithoutNullForgiving_WorksCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Without ! (just ignore the compiler warning)
        var order = await context.Orders
            .IncludePaths(o => o.Customer.Address)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeepPath_WithoutNullForgiving_WorksCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Deep path without any ! operators
        var order = await context.Orders
            .IncludePaths(o => o.LineItems.Each().Product.Category.ParentCategory)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        var iPhone = order!.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Category!.ParentCategory.ShouldNotBeNull();
        iPhone.Product.Category.ParentCategory!.Name.ShouldBe("Phones");
    }

    [Fact]
    public async Task MultiplePaths_WithoutNullForgiving_WorksCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Multiple paths without ! operators
        var order = await context.Orders
            .IncludePaths(
                o => o.Customer.Address,
                o => o.Customer.PaymentMethods,
                o => o.LineItems.Each().Product.Category,
                o => o.LineItems.Each().Product.Images)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.PaymentMethods.Count.ShouldBe(2);
    }
}

#pragma warning restore CS8602
