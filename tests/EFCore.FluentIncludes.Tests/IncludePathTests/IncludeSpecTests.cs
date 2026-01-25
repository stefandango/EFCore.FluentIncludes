using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

#region Test Specifications

/// <summary>
/// Basic order spec - just customer info.
/// </summary>
public class OrderSummarySpec : IncludeSpec<Order>
{
    public OrderSummarySpec()
    {
        Include(o => o.Customer);
    }
}

/// <summary>
/// Extends OrderSummarySpec with customer details.
/// </summary>
public class OrderWithCustomerDetailsSpec : IncludeSpec<Order>
{
    public OrderWithCustomerDetailsSpec()
    {
        // Include base spec
        IncludeFrom<OrderSummarySpec>();

        // Add more includes
        Include(o => o.Customer!.Address);
        Include(o => o.Customer!.PaymentMethods);
    }
}

/// <summary>
/// Order with line items and products.
/// </summary>
public class OrderWithItemsSpec : IncludeSpec<Order>
{
    public OrderWithItemsSpec()
    {
        Include(o => o.LineItems.Each().Product);
    }
}

/// <summary>
/// Full order spec combining multiple base specs.
/// </summary>
public class OrderFullSpec : IncludeSpec<Order>
{
    public OrderFullSpec()
    {
        // Compose from other specs
        IncludeFrom<OrderWithCustomerDetailsSpec>();
        IncludeFrom<OrderWithItemsSpec>();

        // Add additional includes
        Include(
            o => o.ShippingAddress,
            o => o.BillingAddress,
            o => o.LineItems.Each().Product!.Category,
            o => o.LineItems.Each().Discounts,
            o => o.Payments.Each().PaymentMethod,
            o => o.Notes.Each().Author);
    }
}

/// <summary>
/// Spec for audit information.
/// </summary>
public class OrderAuditSpec : IncludeSpec<Order>
{
    public OrderAuditSpec()
    {
        Include(o => o.Notes.Each().Author);
    }
}

/// <summary>
/// Category spec with parent hierarchy.
/// </summary>
public class CategoryWithParentsSpec : IncludeSpec<Category>
{
    public CategoryWithParentsSpec()
    {
        Include(c => c.ParentCategory!.ParentCategory);
    }
}

/// <summary>
/// Order spec with split query enabled.
/// </summary>
public class OrderWithSplitQuerySpec : IncludeSpec<Order>
{
    public OrderWithSplitQuerySpec()
    {
        UseSplitQuery();
        Include(
            o => o.Customer!.Address,
            o => o.LineItems.Each().Product,
            o => o.Payments.Each().PaymentMethod);
    }
}

/// <summary>
/// Order spec with no tracking enabled.
/// </summary>
public class OrderNoTrackingSpec : IncludeSpec<Order>
{
    public OrderNoTrackingSpec()
    {
        AsNoTracking();
        Include(o => o.Customer!.Address);
    }
}

/// <summary>
/// Order spec with no tracking but identity resolution.
/// </summary>
public class OrderNoTrackingWithIdentityResolutionSpec : IncludeSpec<Order>
{
    public OrderNoTrackingWithIdentityResolutionSpec()
    {
        AsNoTrackingWithIdentityResolution();
        Include(o => o.Customer!.Address);
    }
}

/// <summary>
/// Order spec combining split query and no tracking.
/// </summary>
public class OrderSplitQueryNoTrackingSpec : IncludeSpec<Order>
{
    public OrderSplitQueryNoTrackingSpec()
    {
        UseSplitQuery();
        AsNoTracking();
        Include(
            o => o.Customer!.Address,
            o => o.LineItems.Each().Product);
    }
}

#endregion

/// <summary>
/// Tests for IncludeSpec functionality.
/// </summary>
[Collection("Database")]
public class IncludeSpecTests
{
    private readonly DatabaseFixture _fixture;

    public IncludeSpecTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BasicSpec_AppliesIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var order = await context.Orders
            .WithSpec<Order, OrderSummarySpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task SpecWithInheritance_IncludesBaseSpecPaths()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - OrderWithCustomerDetailsSpec extends OrderSummarySpec
        var order = await context.Orders
            .WithSpec<Order, OrderWithCustomerDetailsSpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.Customer.PaymentMethods.Should().HaveCount(2);
    }

    [Fact]
    public async Task ComposedSpec_CombinesMultipleSpecs()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - OrderFullSpec composes OrderWithCustomerDetailsSpec and OrderWithItemsSpec
        var order = await context.Orders
            .WithSpec<Order, OrderFullSpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();

        // From OrderWithCustomerDetailsSpec (via OrderSummarySpec)
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.Customer.PaymentMethods.Should().HaveCount(2);

        // From OrderWithItemsSpec
        order.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li => li.Product.Should().NotBeNull());

        // Additional includes from OrderFullSpec
        order.ShippingAddress.Should().NotBeNull();
        order.BillingAddress.Should().NotBeNull();
        order.Payments.First().PaymentMethod.Should().NotBeNull();
        order.Notes.First().Author.Should().NotBeNull();
    }

    [Fact]
    public async Task WithSpecInstance_AppliesIncludes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var spec = new OrderSummarySpec();

        // Act
        var order = await context.Orders
            .WithSpec(spec)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
    }

    [Fact]
    public async Task WithSpecs_CombinesMultipleSpecs()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Combine two specs at query time
        var order = await context.Orders
            .WithSpecs<Order, OrderWithCustomerDetailsSpec, OrderWithItemsSpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.LineItems.Should().HaveCount(2);
        order.LineItems.First().Product.Should().NotBeNull();
    }

    [Fact]
    public async Task WithSpecsParams_CombinesMultipleSpecInstances()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var spec1 = new OrderSummarySpec();
        var spec2 = new OrderWithItemsSpec();

        // Act
        var order = await context.Orders
            .WithSpecs(spec1, spec2)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.LineItems.Should().HaveCount(2);
        order.LineItems.First().Product.Should().NotBeNull();
    }

    [Fact]
    public async Task WithSpecIf_WhenTrue_AppliesSpec()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeCustomer = true;

        // Act
        var order = await context.Orders
            .WithSpecIf<Order, OrderSummarySpec>(includeCustomer)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
    }

    [Fact]
    public async Task WithSpecIf_WhenFalse_DoesNotApplySpec()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeCustomer = false;

        // Act
        var order = await context.Orders
            .WithSpecIf<Order, OrderSummarySpec>(includeCustomer)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().BeNull();
    }

    [Fact]
    public async Task MixSpecAndIncludePaths_WorksTogether()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Use spec for common includes, add one-off includes with IncludePaths
        var order = await context.Orders
            .WithSpec<Order, OrderSummarySpec>()
            .IncludePaths(o => o.LineItems.Each().Product!.Images)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull(); // From spec
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Images.Should().HaveCount(2); // From IncludePaths
    }

    [Fact]
    public async Task SelfReferencingSpec_LoadsMultipleLevels()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var category = await context.Categories
            .WithSpec<Category, CategoryWithParentsSpec>()
            .FirstOrDefaultAsync(c => c.Name == "Smartphones");

        // Assert
        category.Should().NotBeNull();
        category!.ParentCategory.Should().NotBeNull();
        category.ParentCategory!.Name.Should().Be("Phones");
        category.ParentCategory.ParentCategory.Should().NotBeNull();
        category.ParentCategory.ParentCategory!.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task SpecWithSplitQuery_AppliesSplitQueryBehavior()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - This spec has UseSplitQuery() configured
        var order = await context.Orders
            .WithSpec<Order, OrderWithSplitQuerySpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Verify includes were loaded
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li => li.Product.Should().NotBeNull());
        order.Payments.Should().HaveCount(1);
        order.Payments.Should().AllSatisfy(p => p.PaymentMethod.Should().NotBeNull());
    }

    [Fact]
    public async Task SpecWithNoTracking_EntitiesAreNotTracked()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - This spec has AsNoTracking() configured
        var order = await context.Orders
            .WithSpec<Order, OrderNoTrackingSpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Verify data was loaded
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();

        // Verify entity is not tracked
        var entry = context.Entry(order);
        entry.State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task SpecWithNoTrackingWithIdentityResolution_EntitiesAreNotTrackedButResolved()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - This spec has AsNoTrackingWithIdentityResolution() configured
        var order = await context.Orders
            .WithSpec<Order, OrderNoTrackingWithIdentityResolutionSpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Verify data was loaded
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();

        // Verify entity is not tracked
        var entry = context.Entry(order);
        entry.State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task SpecWithSplitQueryAndNoTracking_AppliesBothBehaviors()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - This spec has both UseSplitQuery() and AsNoTracking() configured
        var order = await context.Orders
            .WithSpec<Order, OrderSplitQueryNoTrackingSpec>()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Verify includes were loaded
        order.Should().NotBeNull();
        order!.Customer.Should().NotBeNull();
        order.Customer!.Address.Should().NotBeNull();
        order.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li => li.Product.Should().NotBeNull());

        // Verify entity is not tracked
        var entry = context.Entry(order);
        entry.State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task SpecQueryOptions_CanBeCombinedWithExternalOptions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act - Spec has includes, we add AsSplitQuery externally
        var order = await context.Orders
            .WithSpec<Order, OrderWithItemsSpec>()
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Verify data was loaded
        order.Should().NotBeNull();
        order!.LineItems.Should().HaveCount(2);
        order.LineItems.Should().AllSatisfy(li => li.Product.Should().NotBeNull());

        // Verify entity is not tracked
        var entry = context.Entry(order);
        entry.State.Should().Be(EntityState.Detached);
    }
}
