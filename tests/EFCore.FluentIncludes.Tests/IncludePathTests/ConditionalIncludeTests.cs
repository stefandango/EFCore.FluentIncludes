using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.IncludePathTests;

/// <summary>
/// Tests for conditional include scenarios using IncludePathsIf.
/// </summary>
[Collection("Database")]
public class ConditionalIncludeTests
{
    private readonly DatabaseFixture _fixture;

    public ConditionalIncludeTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IncludePaths_CombinedWith_IncludePathsIf_WhenTrue()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeProducts = true;

        // Act - Base includes + conditional includes
        var order = await context.Orders
            .IncludePaths(o => o.Customer!.Address)
            .IncludePathsIf(includeProducts,
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Product!.Images)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.Count.ShouldBe(2);
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Images.Count.ShouldBe(2);
    }

    [Fact]
    public async Task IncludePaths_CombinedWith_IncludePathsIf_WhenFalse()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeProducts = false;

        // Act - Base includes + conditional includes (disabled)
        var order = await context.Orders
            .IncludePaths(o => o.Customer!.Address)
            .IncludePathsIf(includeProducts,
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Product!.Images)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Customer loaded, but not LineItems/Products
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.ShouldBeEmpty(); // Not loaded
    }

    [Fact]
    public async Task MultipleConditionalIncludes_IndependentFlags()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeCustomerDetails = true;
        var includeProducts = true;
        var includePayments = false;

        // Act - Multiple conditional sections
        var order = await context.Orders
            .IncludePaths(o => o.Customer) // Always include customer
            .IncludePathsIf(includeCustomerDetails,
                o => o.Customer!.Address,
                o => o.Customer!.PaymentMethods)
            .IncludePathsIf(includeProducts,
                o => o.LineItems.Each().Product)
            .IncludePathsIf(includePayments,
                o => o.Payments.Each().PaymentMethod)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull(); // Included
        order.Customer.PaymentMethods.Count.ShouldBe(2); // Included
        order.LineItems.Count.ShouldBe(2); // Included
        order.LineItems.First().Product.ShouldNotBeNull();
        order.Payments.ShouldBeEmpty(); // NOT included (flag was false)
    }

    [Fact]
    public async Task ConditionalIncludes_BasedOnUserRole()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Simulate different user roles
        var userRole = "Admin"; // Could be "Customer", "Staff", "Admin"
        var isAdmin = userRole == "Admin";
        var isStaffOrAdmin = userRole is "Staff" or "Admin";

        // Act - Role-based includes
        var order = await context.Orders
            .IncludePaths(o => o.Customer) // Everyone sees customer
            .IncludePathsIf(isStaffOrAdmin,
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product)
            .IncludePathsIf(isAdmin,
                o => o.Customer!.PaymentMethods,
                o => o.Payments.Each().PaymentMethod,
                o => o.Notes.Each().Author)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Admin sees everything
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.PaymentMethods.Count.ShouldBe(2);
        order.LineItems.Count.ShouldBe(2);
        order.Payments.Count.ShouldBe(1);
        order.Notes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConditionalIncludes_BasedOnQueryParameters()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Simulate API query parameters: ?expand=customer,products
        var expandOptions = new HashSet<string> { "customer", "products" };

        // Act
        var order = await context.Orders
            .IncludePathsIf(expandOptions.Contains("customer"),
                o => o.Customer!.Address,
                o => o.Customer!.PaymentMethods)
            .IncludePathsIf(expandOptions.Contains("products"),
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Product!.Images)
            .IncludePathsIf(expandOptions.Contains("payments"),
                o => o.Payments.Each().PaymentMethod)
            .IncludePathsIf(expandOptions.Contains("audit"),
                o => o.Notes.Each().Author)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Only customer and products expanded
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.LineItems.Count.ShouldBe(2);
        order.LineItems.First().Product.ShouldNotBeNull();
        order.Payments.ShouldBeEmpty(); // Not requested
        order.Notes.ShouldBeEmpty(); // Not requested
    }

    [Fact]
    public async Task ConditionalIncludes_WithSpecs_MixedApproach()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var includeFullDetails = true;
        var includeAudit = false;

        // Act - Combine specs with conditional paths
        var order = await context.Orders
            .WithSpec<Order, OrderSummarySpec>() // Base spec
            .WithSpecIf<Order, OrderWithItemsSpec>(includeFullDetails)
            .IncludePathsIf(includeFullDetails,
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Product!.Images)
            .IncludePathsIf(includeAudit,
                o => o.Notes.Each().Author)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull(); // From OrderSummarySpec
        order.LineItems.Count.ShouldBe(2); // From OrderWithItemsSpec
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull(); // From IncludePathsIf
        iPhone.Product.Images.Count.ShouldBe(2); // From IncludePathsIf
        order.Notes.ShouldBeEmpty(); // Audit was false
    }

    [Fact]
    public async Task ConditionalIncludes_DetailLevel_Enum()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        var detailLevel = DetailLevel.Full; // Could be Minimal, Standard, Full

        // Act
        var order = await context.Orders
            .IncludePaths(o => o.Customer) // Always
            .IncludePathsIf(detailLevel >= DetailLevel.Standard,
                o => o.Customer!.Address,
                o => o.LineItems.Each().Product)
            .IncludePathsIf(detailLevel >= DetailLevel.Full,
                o => o.Customer!.PaymentMethods,
                o => o.LineItems.Each().Product!.Category,
                o => o.LineItems.Each().Product!.Images,
                o => o.LineItems.Each().Discounts,
                o => o.Payments.Each().PaymentMethod)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert - Full detail level
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();
        order.Customer!.Address.ShouldNotBeNull();
        order.Customer.PaymentMethods.Count.ShouldBe(2);
        order.LineItems.Count.ShouldBe(2);
        var iPhone = order.LineItems.First(li => li.Product!.Name == "iPhone 15");
        iPhone.Product!.Category.ShouldNotBeNull();
        iPhone.Product.Images.Count.ShouldBe(2);
        iPhone.Discounts.Count.ShouldBe(1);
        order.Payments.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(DetailLevel.Minimal, false, false)]
    [InlineData(DetailLevel.Standard, true, false)]
    [InlineData(DetailLevel.Full, true, true)]
    public async Task ConditionalIncludes_TheoryTest_DetailLevels(
        DetailLevel level,
        bool expectAddress,
        bool expectPaymentMethods)
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var order = await context.Orders
            .IncludePaths(o => o.Customer)
            .IncludePathsIf(level >= DetailLevel.Standard,
                o => o.Customer!.Address)
            .IncludePathsIf(level >= DetailLevel.Full,
                o => o.Customer!.PaymentMethods)
            .FirstOrDefaultAsync(o => o.Id == 1);

        // Assert
        order.ShouldNotBeNull();
        order!.Customer.ShouldNotBeNull();

        if (expectAddress)
            order.Customer!.Address.ShouldNotBeNull();
        else
            order.Customer!.Address.ShouldBeNull();

        if (expectPaymentMethods)
            order.Customer!.PaymentMethods.Count.ShouldBe(2);
        else
            order.Customer!.PaymentMethods.ShouldBeEmpty();
    }

}

public enum DetailLevel
{
    Minimal = 0,
    Standard = 1,
    Full = 2
}
