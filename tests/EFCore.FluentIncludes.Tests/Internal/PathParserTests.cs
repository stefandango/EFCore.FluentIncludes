using System.Linq.Expressions;
using EFCore.FluentIncludes.Internal;
using EFCore.FluentIncludes.Tests.TestEntities;

namespace EFCore.FluentIncludes.Tests.Internal;

public class PathParserTests
{
    #region Simple Property Access

    [Fact]
    public void Parse_SingleProperty_ReturnsSingleSegment()
    {
        Expression<Func<Order, Customer?>> expr = o => o.Customer;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Property.Name.ShouldBe("Customer");
        segments[0].IsCollection.ShouldBeFalse();
        segments[0].SourceType.ShouldBe(typeof(Order));
        segments[0].TargetType.ShouldBe(typeof(Customer));
    }

    [Fact]
    public void Parse_CollectionProperty_ReturnsCollectionSegment()
    {
        Expression<Func<Order, ICollection<LineItem>>> expr = o => o.LineItems;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].IsCollection.ShouldBeTrue();
        segments[0].SourceType.ShouldBe(typeof(Order));
        segments[0].TargetType.ShouldBe(typeof(LineItem));
    }

    [Fact]
    public void Parse_NestedProperty_ReturnsMultipleSegments()
    {
        Expression<Func<Order, Address?>> expr = o => o.Customer!.Address;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(2);
        segments[0].Property.Name.ShouldBe("Customer");
        segments[1].Property.Name.ShouldBe("Address");
    }

    [Fact]
    public void Parse_DeepNestedProperty_ReturnsAllSegments()
    {
        Expression<Func<Order, Category?>> expr = o => o.LineItems.Each().Product!.Category;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(3);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].IsCollection.ShouldBeTrue();
        segments[1].Property.Name.ShouldBe("Product");
        segments[2].Property.Name.ShouldBe("Category");
    }

    #endregion

    #region Each() Method

    [Fact]
    public void Parse_EachOnCollection_IsMarkerOnly()
    {
        Expression<Func<Order, Product?>> expr = o => o.LineItems.Each().Product;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(2);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].IsCollection.ShouldBeTrue();
        segments[1].Property.Name.ShouldBe("Product");
    }

    [Fact]
    public void Parse_MultipleCollectionsInChain_HandlesCorrectly()
    {
        Expression<Func<Order, ICollection<ProductImage>>> expr = o => o.LineItems.Each().Product!.Images;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(3);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].IsCollection.ShouldBeTrue();
        segments[1].Property.Name.ShouldBe("Product");
        segments[1].IsCollection.ShouldBeFalse();
        segments[2].Property.Name.ShouldBe("Images");
        segments[2].IsCollection.ShouldBeTrue();
    }

    #endregion

    #region To() Method

    [Fact]
    public void Parse_ToOnNullable_IsMarkerOnly()
    {
        Expression<Func<Order, Address?>> expr = o => o.Customer.To().Address;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(2);
        segments[0].Property.Name.ShouldBe("Customer");
        segments[1].Property.Name.ShouldBe("Address");
    }

    [Fact]
    public void Parse_ChainedTo_HandlesCorrectly()
    {
        Expression<Func<Order, Category?>> expr = o => o.LineItems.Each().Product.To().Category.To().ParentCategory;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(4);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[1].Property.Name.ShouldBe("Product");
        segments[2].Property.Name.ShouldBe("Category");
        segments[3].Property.Name.ShouldBe("ParentCategory");
    }

    #endregion

    #region Filtered Includes (Where)

    [Fact]
    public void Parse_WhereOnCollection_CapturesFilter()
    {
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.Where(li => li.Quantity > 0).Each();

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].Filter.ShouldNotBeNull();
        segments[0].IsCollection.ShouldBeTrue();
    }

    [Fact]
    public void Parse_WhereWithComplexPredicate_CapturesFilter()
    {
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.Where(li => li.Quantity > 0 && li.UnitPrice < 100).Each();

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Filter.ShouldNotBeNull();
    }

    [Fact]
    public void Parse_WhereFollowedByNavigation_AppliesFilterToCollection()
    {
        Expression<Func<Order, Product?>> expr = o => o.LineItems.Where(li => li.Quantity > 0).Each().Product;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(2);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].Filter.ShouldNotBeNull();
        segments[1].Property.Name.ShouldBe("Product");
        segments[1].Filter.ShouldBeNull();
    }

    #endregion

    #region Ordered Includes

    [Fact]
    public void Parse_OrderByOnCollection_CapturesOrdering()
    {
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.OrderBy(li => li.Quantity).Each();

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Property.Name.ShouldBe("LineItems");
        segments[0].Orderings.ShouldNotBeNull();
        segments[0].Orderings!.Count.ShouldBe(1);
        segments[0].Orderings![0].Descending.ShouldBeFalse();
    }

    [Fact]
    public void Parse_OrderByDescendingOnCollection_CapturesDescending()
    {
        Expression<Func<Order, LineItem>> expr = o => o.LineItems.OrderByDescending(li => li.Quantity).Each();

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Orderings.ShouldNotBeNull();
        segments[0].Orderings![0].Descending.ShouldBeTrue();
    }

    [Fact]
    public void Parse_ThenByOnCollection_CapturesMultipleOrderings()
    {
        Expression<Func<Order, LineItem>> expr = o => o.LineItems
            .OrderBy(li => li.Quantity)
            .ThenByDescending(li => li.UnitPrice)
            .Each();

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Orderings!.Count.ShouldBe(2);
        segments[0].Orderings![0].Descending.ShouldBeFalse();
        segments[0].Orderings![1].Descending.ShouldBeTrue();
    }

    [Fact]
    public void Parse_WhereAndOrderBy_CapturesBoth()
    {
        Expression<Func<Order, LineItem>> expr = o => o.LineItems
            .Where(li => li.Quantity > 0)
            .OrderBy(li => li.Quantity)
            .Each();

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Filter.ShouldNotBeNull();
        segments[0].Orderings.ShouldNotBeNull();
        segments[0].Orderings!.Count.ShouldBe(1);
    }

    #endregion

    #region Caching

    [Fact]
    public void Parse_SameExpression_ReturnsCachedResult()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, Customer?>> expr2 = o => o.Customer;

        var segments1 = PathParser.Parse(expr1);
        var segments2 = PathParser.Parse(expr2);

        // Both should return equivalent results (content equality)
        segments1.ShouldBe(segments2);
    }

    [Fact]
    public void Parse_ReturnsNewListInstance()
    {
        // Parser should return a copy so callers can modify without affecting cache
        Expression<Func<Order, Customer?>> expr = o => o.Customer;

        var segments1 = PathParser.Parse(expr);
        var segments2 = PathParser.Parse(expr);

        segments1.ShouldNotBeSameAs(segments2);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void Parse_UnsupportedExpressionType_ThrowsInvalidOperationException()
    {
        // Test that parsing a constant expression throws
        var param = Expression.Parameter(typeof(Order), "o");
        var constExpr = Expression.Constant(42);
        var lambda = Expression.Lambda<Func<Order, int>>(constExpr, param);

        var action = () => PathParser.Parse(lambda);

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Unsupported expression type");
    }

    [Fact]
    public void Parse_FieldAccess_ThrowsInvalidOperationException()
    {
        // PathParser only supports property access, not field access
        var param = Expression.Parameter(typeof(Order), "o");

        // Create a field access expression using a helper class with a public field
        var fieldInfo = typeof(TestClassWithField).GetField("PublicField")!;
        var fieldAccess = Expression.Field(Expression.Constant(new TestClassWithField()), fieldInfo);
        var lambda = Expression.Lambda<Func<Order, string>>(fieldAccess, param);

        var action = () => PathParser.Parse(lambda);

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Only property access");
    }

    [Fact]
    public void Parse_UnsupportedMethodCall_ThrowsInvalidOperationException()
    {
        // Create an expression with an unsupported method like Select()
        Expression<Func<Order, IEnumerable<int>>> expr = o => o.LineItems.Select(li => li.Quantity);

        var action = () => PathParser.Parse(expr);

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Unsupported method call 'Select'");
    }

    [Fact]
    public void Parse_IEnumerableInterface_DetectedAsCollection()
    {
        // Test that IEnumerable<T> itself (not just classes implementing it) is detected as collection
        var param = Expression.Parameter(typeof(TestEntityWithIEnumerable), "e");
        var propAccess = Expression.Property(param, "Items");
        var lambda = Expression.Lambda<Func<TestEntityWithIEnumerable, IEnumerable<string>>>(propAccess, param);

        var segments = PathParser.Parse(lambda);

        segments.Count.ShouldBe(1);
        segments[0].IsCollection.ShouldBeTrue();
        segments[0].TargetType.ShouldBe(typeof(string));
    }

    #endregion

    #region Helper Classes

    private sealed class TestClassWithField
    {
        public string PublicField = "test";
    }

    private sealed class TestEntityWithIEnumerable
    {
        public IEnumerable<string> Items { get; set; } = [];
    }

    #endregion

    #region Type Conversion

    [Fact]
    public void Parse_WithTypeConversion_HandlesConvertExpression()
    {
        // Some expressions include Convert nodes (e.g., when dealing with derived types)
        Expression<Func<Order, object>> expr = o => o.Customer!;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Property.Name.ShouldBe("Customer");
    }

    #endregion

    #region String Property

    [Fact]
    public void Parse_StringProperty_IsNotCollection()
    {
        Expression<Func<Customer, string>> expr = c => c.Name;

        var segments = PathParser.Parse(expr);

        segments.Count.ShouldBe(1);
        segments[0].Property.Name.ShouldBe("Name");
        segments[0].IsCollection.ShouldBeFalse();
    }

    #endregion
}
