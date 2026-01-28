using System.Linq.Expressions;
using EFCore.FluentIncludes.Internal;
using EFCore.FluentIncludes.Tests.Fixtures;
using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.Internal;

public class IncludeBuilderTests : IClassFixture<DatabaseFixture>
{
    private readonly TestDbContext _context;

    public IncludeBuilderTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
    }

    #region Single Navigation Includes

    [Fact]
    public void ApplyIncludes_SingleNavigation_AppliesInclude()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Customer?>> expr = o => o.Customer;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("JOIN");
        queryString.ShouldContain("Customer");
    }

    [Fact]
    public void ApplyIncludes_CollectionNavigation_AppliesInclude()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, ICollection<LineItem>>> expr = o => o.LineItems;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("JOIN");
        queryString.ShouldContain("LineItem");
    }

    #endregion

    #region Nested Navigation Includes

    [Fact]
    public void ApplyIncludes_NestedNavigation_AppliesThenInclude()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Address?>> expr = o => o.Customer!.Address;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("Customer");
        queryString.ShouldContain("Address");
    }

    [Fact]
    public void ApplyIncludes_ThroughCollection_AppliesThenIncludeAfterCollection()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Product?>> expr = o => o.LineItems.Each().Product;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("Product");
    }

    [Fact]
    public void ApplyIncludes_DeepNesting_AppliesAllThenIncludes()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Category?>> expr = o => o.LineItems.Each().Product!.Category;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("Product");
        queryString.ShouldContain("Categor");
    }

    #endregion

    #region Multiple Paths

    [Fact]
    public void ApplyIncludes_MultiplePaths_AppliesAllIncludes()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, ICollection<LineItem>>> expr2 = o => o.LineItems;
        var segments1 = PathParser.Parse(expr1);
        var segments2 = PathParser.Parse(expr2);

        var result = IncludeBuilder.ApplyIncludes(query, [segments1, segments2]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("Customer");
        queryString.ShouldContain("LineItem");
    }

    [Fact]
    public void ApplyIncludes_BranchingPaths_AppliesBothBranches()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Address?>> expr1 = o => o.Customer!.Address;
        Expression<Func<Order, ICollection<PaymentMethod>>> expr2 = o => o.Customer!.PaymentMethods;
        var segments1 = PathParser.Parse(expr1);
        var segments2 = PathParser.Parse(expr2);

        var result = IncludeBuilder.ApplyIncludes(query, [segments1, segments2]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("Address");
        queryString.ShouldContain("PaymentMethod");
    }

    #endregion

    #region Filtered Includes

    [Fact]
    public void ApplyIncludes_WithFilter_AppliesFilteredInclude()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("Quantity");
    }

    [Fact]
    public void ApplyIncludes_FilterWithNavigation_AppliesFilterThenNavigation()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Product?>> expr = o => o.LineItems.Where(li => li.Quantity > 0).Each().Product;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("Product");
        queryString.ShouldContain("Quantity");
    }

    #endregion

    #region Ordered Includes

    [Fact]
    public void ApplyIncludes_WithOrderBy_AppliesOrderedInclude()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.OrderBy(li => li.Quantity).Each();
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("ORDER BY");
    }

    [Fact]
    public void ApplyIncludes_WithOrderByDescending_AppliesDescendingOrder()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.OrderByDescending(li => li.Quantity).Each();
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("ORDER BY");
        queryString.ShouldContain("DESC");
    }

    [Fact]
    public void ApplyIncludes_WithThenBy_AppliesMultipleOrderings()
    {
        var query = _context.Orders.AsQueryable();
        // Use int properties only to avoid SQLite decimal ordering limitation
        Expression<Func<Order, LineItem>> expr = o => o.LineItems
            .OrderBy(li => li.Quantity)
            .ThenByDescending(li => li.Id)
            .Each();
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("ORDER BY");
    }

    [Fact]
    public void ApplyIncludes_FilterAndOrderBy_AppliesBoth()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, LineItem>> expr = o => o.LineItems
            .Where(li => li.Quantity > 0)
            .OrderBy(li => li.Quantity)
            .Each();
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("Quantity");
        queryString.ShouldContain("ORDER BY");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ApplyIncludes_EmptyPathList_ReturnsOriginalQuery()
    {
        var query = _context.Orders.AsQueryable();

        var result = IncludeBuilder.ApplyIncludes(query, []);

        result.ShouldBeSameAs(query);
    }

    [Fact]
    public void ApplyIncludes_EmptySegmentList_ReturnsOriginalQuery()
    {
        var query = _context.Orders.AsQueryable();

        var result = IncludeBuilder.ApplyIncludes(query, [[]]);

        result.ShouldBeSameAs(query);
    }

    [Fact]
    public void ApplyIncludes_MixedCollectionAndReference_HandlesCorrectly()
    {
        // Collection -> Reference -> Reference
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Category?>> expr = o => o.LineItems.Each().Product!.Category;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("LineItem");
        queryString.ShouldContain("Product");
        queryString.ShouldContain("Categor");
    }

    [Fact]
    public void ApplyIncludes_ReferenceToCollection_HandlesCorrectly()
    {
        // Reference -> Collection
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, ICollection<PaymentMethod>>> expr = o => o.Customer!.PaymentMethods;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        var queryString = result.ToQueryString();
        queryString.ShouldContain("Customer");
        queryString.ShouldContain("PaymentMethod");
    }

    #endregion

    #region Return Type Verification

    [Fact]
    public void ApplyIncludes_ReturnsIQueryableOfSameEntityType()
    {
        var query = _context.Orders.AsQueryable();
        Expression<Func<Order, Customer?>> expr = o => o.Customer;
        var segments = PathParser.Parse(expr);

        var result = IncludeBuilder.ApplyIncludes(query, [segments]);

        result.ShouldBeAssignableTo<IQueryable<Order>>();
    }

    #endregion
}
