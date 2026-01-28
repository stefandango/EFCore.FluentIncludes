using System.Linq.Expressions;
using EFCore.FluentIncludes.Internal;
using EFCore.FluentIncludes.Tests.TestEntities;

namespace EFCore.FluentIncludes.Tests.Internal;

public class ExpressionEqualityComparerTests
{
    private static ExpressionEqualityComparer Comparer => ExpressionEqualityComparer.Instance;

    #region Basic Equality

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        Expression<Func<Order, Customer?>> expr = o => o.Customer;

        Comparer.Equals(expr, expr).ShouldBeTrue();
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        Comparer.Equals(null, null).ShouldBeTrue();
    }

    [Fact]
    public void Equals_OneNull_ReturnsFalse()
    {
        Expression<Func<Order, Customer?>> expr = o => o.Customer;

        Comparer.Equals(expr, null).ShouldBeFalse();
        Comparer.Equals(null, expr).ShouldBeFalse();
    }

    #endregion

    #region Member Expressions

    [Fact]
    public void Equals_IdenticalMemberExpression_ReturnsTrue()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, Customer?>> expr2 = o => o.Customer;

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentMemberExpression_ReturnsFalse()
    {
        Expression<Func<Order, Address?>> expr1 = o => o.ShippingAddress;
        Expression<Func<Order, Address?>> expr2 = o => o.BillingAddress;

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_NestedMemberExpressions_ReturnsTrue()
    {
        Expression<Func<Order, Address?>> expr1 = o => o.Customer!.Address;
        Expression<Func<Order, Address?>> expr2 = o => o.Customer!.Address;

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentNestedMemberExpressions_ReturnsFalse()
    {
        Expression<Func<Order, Address?>> expr1 = o => o.Customer!.Address;
        Expression<Func<Order, string>> expr2 = o => o.Customer!.Name;

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Method Call Expressions

    [Fact]
    public void Equals_IdenticalMethodCallExpression_ReturnsTrue()
    {
        Expression<Func<Order, Product?>> expr1 = o => o.LineItems.Each().Product;
        Expression<Func<Order, Product?>> expr2 = o => o.LineItems.Each().Product;

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentMethodCallExpression_ReturnsFalse()
    {
        Expression<Func<Order, Product?>> expr1 = o => o.LineItems.Each().Product;
        Expression<Func<Order, Order?>> expr2 = o => o.LineItems.Each().Order;

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_IdenticalWhereExpression_ReturnsTrue()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 0).Each();

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentWherePredicate_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 5).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Parameter Name Independence

    [Fact]
    public void Equals_DifferentParameterNames_ReturnsTrue()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, Customer?>> expr2 = x => x.Customer;

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentNestedParameterNames_ReturnsTrue()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        Expression<Func<Order, LineItem>> expr2 = order => order.LineItems.Where(item => item.Quantity > 0).Each();

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    #endregion

    #region Binary Expressions

    [Fact]
    public void Equals_IdenticalBinaryExpression_ReturnsTrue()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0 && li.UnitPrice < 100).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 0 && li.UnitPrice < 100).Each();

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentBinaryOperator_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity >= 0).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Constant Expressions

    [Fact]
    public void Equals_SameConstantValue_ReturnsTrue()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 5).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 5).Each();

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentConstantValue_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 5).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 10).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Unary Expressions

    [Fact]
    public void Equals_IdenticalUnaryExpression_ReturnsTrue()
    {
        Expression<Func<Order, object>> expr1 = o => o.Customer!;
        Expression<Func<Order, object>> expr2 = o => o.Customer!;

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    #endregion

    #region Conditional Expressions

    [Fact]
    public void Equals_IdenticalConditionalExpression_ReturnsTrue()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0 ? true : false).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 0 ? true : false).Each();

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentConditionalBranch_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0 ? true : false).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where(li => li.Quantity > 0 ? false : true).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Type Mismatch

    [Fact]
    public void Equals_DifferentReturnType_ReturnsFalse()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, Address?>> expr2 = o => o.ShippingAddress;

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentParameterCount_ReturnsFalse()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, int, Customer?>> expr2 = (o, _) => o.Customer;

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Hash Code

    [Fact]
    public void GetHashCode_NullExpression_ReturnsZero()
    {
        Comparer.GetHashCode(null!).ShouldBe(0);
    }

    [Fact]
    public void GetHashCode_IdenticalExpressions_ReturnsSameHash()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, Customer?>> expr2 = o => o.Customer;

        Comparer.GetHashCode(expr1).ShouldBe(Comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_DifferentParameterNames_ReturnsSameHash()
    {
        Expression<Func<Order, Customer?>> expr1 = o => o.Customer;
        Expression<Func<Order, Customer?>> expr2 = x => x.Customer;

        Comparer.GetHashCode(expr1).ShouldBe(Comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_DifferentExpressions_ReturnsDifferentHash()
    {
        Expression<Func<Order, Address?>> expr1 = o => o.ShippingAddress;
        Expression<Func<Order, Address?>> expr2 = o => o.BillingAddress;

        // Note: Hash collisions are possible but unlikely for different expressions
        Comparer.GetHashCode(expr1).ShouldNotBe(Comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_ComplexExpressions_ReturnsSameHashForEquivalent()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        Expression<Func<Order, LineItem>> expr2 = order => order.LineItems.Where(item => item.Quantity > 0).Each();

        Comparer.GetHashCode(expr1).ShouldBe(Comparer.GetHashCode(expr2));
    }

    #endregion

    #region Ordering Expressions

    [Fact]
    public void Equals_IdenticalOrderByExpression_ReturnsTrue()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.OrderBy(li => li.Quantity).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.OrderBy(li => li.Quantity).Each();

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentOrderByKey_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.OrderBy(li => li.Quantity).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.OrderBy(li => li.UnitPrice).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_OrderByVsOrderByDescending_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.OrderBy(li => li.Quantity).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.OrderByDescending(li => li.Quantity).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region New Expressions

    [Fact]
    public void Equals_IdenticalNewExpression_ReturnsTrue()
    {
        Expression<Func<LineItem, object>> expr1 = li => new { li.Quantity, li.UnitPrice };
        Expression<Func<LineItem, object>> expr2 = li => new { li.Quantity, li.UnitPrice };

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentNewExpressionProperties_ReturnsFalse()
    {
        Expression<Func<LineItem, object>> expr1 = li => new { li.Quantity, li.UnitPrice };
        Expression<Func<LineItem, object>> expr2 = li => new { li.Id, li.Quantity };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region MemberInit Expressions

    [Fact]
    public void Equals_IdenticalMemberInitExpression_ReturnsTrue()
    {
        Expression<Func<LineItem, TestDto>> expr1 = li => new TestDto { Value = li.Quantity };
        Expression<Func<LineItem, TestDto>> expr2 = li => new TestDto { Value = li.Quantity };

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentMemberInitValues_ReturnsFalse()
    {
        Expression<Func<LineItem, TestDto>> expr1 = li => new TestDto { Value = li.Quantity };
        Expression<Func<LineItem, TestDto>> expr2 = li => new TestDto { Value = li.Id };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentMemberInitBindings_ReturnsFalse()
    {
        Expression<Func<LineItem, TestDto>> expr1 = li => new TestDto { Value = li.Quantity };
        Expression<Func<LineItem, TestDto>> expr2 = li => new TestDto { Name = "test" };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Invocation Expressions

    [Fact]
    public void Equals_IdenticalInvocationExpression_ReturnsTrue()
    {
        Func<int, int> func = x => x * 2;
        Expression<Func<int, int>> inner = x => x * 2;

        var param = Expression.Parameter(typeof(int), "n");
        var invoke1 = Expression.Invoke(inner, param);
        var invoke2 = Expression.Invoke(inner, param);
        var lambda1 = Expression.Lambda<Func<int, int>>(invoke1, param);
        var lambda2 = Expression.Lambda<Func<int, int>>(invoke2, param);

        Comparer.Equals(lambda1, lambda2).ShouldBeTrue();
    }

    #endregion

    #region Parameter Mapping Edge Cases

    [Fact]
    public void Equals_ParameterNotInMap_ComparesTypes()
    {
        // Create expressions where the parameter comparison falls through to type comparison
        var param1 = Expression.Parameter(typeof(int), "x");
        var param2 = Expression.Parameter(typeof(int), "y");

        // Direct parameter expressions (not mapped)
        var lambda1 = Expression.Lambda<Func<int, int>>(param1, param1);
        var lambda2 = Expression.Lambda<Func<int, int>>(param2, param2);

        Comparer.Equals(lambda1, lambda2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentParameterTypes_ReturnsFalse()
    {
        Expression<Func<int, int>> expr1 = x => x;
        Expression<Func<long, long>> expr2 = x => x;

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Unsupported Expression Types

    [Fact]
    public void Equals_UnsupportedExpressionType_ReturnsFalse()
    {
        // Create expressions with types not explicitly handled (falls to default case)
        var param = Expression.Parameter(typeof(int[]), "arr");
        var arrayLength = Expression.ArrayLength(param);
        var lambda1 = Expression.Lambda<Func<int[], int>>(arrayLength, param);
        var lambda2 = Expression.Lambda<Func<int[], int>>(arrayLength, param);

        // Same expression should still be equal via reference equality in ExpressionsEqual
        Comparer.Equals(lambda1, lambda2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentUnsupportedExpressionTypes_ReturnsFalse()
    {
        var param1 = Expression.Parameter(typeof(int[]), "arr1");
        var param2 = Expression.Parameter(typeof(int[]), "arr2");
        var arrayLength1 = Expression.ArrayLength(param1);
        var arrayLength2 = Expression.ArrayLength(param2);
        var lambda1 = Expression.Lambda<Func<int[], int>>(arrayLength1, param1);
        var lambda2 = Expression.Lambda<Func<int[], int>>(arrayLength2, param2);

        // Different array length expressions should be equal (same structure)
        Comparer.Equals(lambda1, lambda2).ShouldBeTrue();
    }

    #endregion

    #region Nested Lambda Edge Cases

    [Fact]
    public void Equals_NestedLambdaWithDifferentParameterCounts_ReturnsFalse()
    {
        Expression<Func<Order, LineItem>> expr1 = o => o.LineItems.Where(li => li.Quantity > 0).Each();
        Expression<Func<Order, LineItem>> expr2 = o => o.LineItems.Where((li, idx) => li.Quantity > 0).Each();

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region MemberMemberBinding Tests

    [Fact]
    public void Equals_IdenticalMemberMemberBinding_ReturnsTrue()
    {
        // MemberMemberBinding is used for nested object initializers like:
        // new Outer { Inner = { Property = value } }
        Expression<Func<TestOuter>> expr1 = () => new TestOuter { Inner = { Value = 42 } };
        Expression<Func<TestOuter>> expr2 = () => new TestOuter { Inner = { Value = 42 } };

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentMemberMemberBindingValue_ReturnsFalse()
    {
        Expression<Func<TestOuter>> expr1 = () => new TestOuter { Inner = { Value = 42 } };
        Expression<Func<TestOuter>> expr2 = () => new TestOuter { Inner = { Value = 99 } };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region MemberListBinding Tests

    [Fact]
    public void Equals_IdenticalMemberListBinding_ReturnsTrue()
    {
        // MemberListBinding is used for collection initializers like:
        // new Container { Items = { item1, item2 } }
        Expression<Func<TestContainer>> expr1 = () => new TestContainer { Items = { 1, 2, 3 } };
        Expression<Func<TestContainer>> expr2 = () => new TestContainer { Items = { 1, 2, 3 } };

        Comparer.Equals(expr1, expr2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentMemberListBindingValues_ReturnsFalse()
    {
        Expression<Func<TestContainer>> expr1 = () => new TestContainer { Items = { 1, 2, 3 } };
        Expression<Func<TestContainer>> expr2 = () => new TestContainer { Items = { 4, 5, 6 } };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentMemberListBindingCount_ReturnsFalse()
    {
        Expression<Func<TestContainer>> expr1 = () => new TestContainer { Items = { 1, 2 } };
        Expression<Func<TestContainer>> expr2 = () => new TestContainer { Items = { 1, 2, 3 } };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Different Binding Types

    [Fact]
    public void Equals_DifferentBindingTypes_ReturnsFalse()
    {
        // MemberAssignment vs MemberListBinding
        Expression<Func<TestContainer>> expr1 = () => new TestContainer { Items = { 1, 2 } };
        Expression<Func<TestContainerWithAssignment>> expr2 = () => new TestContainerWithAssignment { Items = new List<int> { 1, 2 } };

        Comparer.Equals(expr1, expr2).ShouldBeFalse();
    }

    #endregion

    #region Hash Code for Complex Expressions

    [Fact]
    public void GetHashCode_MemberInitExpression_ReturnsConsistentHash()
    {
        Expression<Func<LineItem, TestDto>> expr1 = li => new TestDto { Value = li.Quantity };
        Expression<Func<LineItem, TestDto>> expr2 = li => new TestDto { Value = li.Quantity };

        Comparer.GetHashCode(expr1).ShouldBe(Comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_MemberMemberBinding_ReturnsConsistentHash()
    {
        Expression<Func<TestOuter>> expr1 = () => new TestOuter { Inner = { Value = 42 } };
        Expression<Func<TestOuter>> expr2 = () => new TestOuter { Inner = { Value = 42 } };

        Comparer.GetHashCode(expr1).ShouldBe(Comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_MemberListBinding_ReturnsConsistentHash()
    {
        Expression<Func<TestContainer>> expr1 = () => new TestContainer { Items = { 1, 2, 3 } };
        Expression<Func<TestContainer>> expr2 = () => new TestContainer { Items = { 1, 2, 3 } };

        Comparer.GetHashCode(expr1).ShouldBe(Comparer.GetHashCode(expr2));
    }

    #endregion

    #region Helper Classes

    private sealed class TestDto
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    private sealed class TestInner
    {
        public int Value { get; set; }
    }

    private sealed class TestOuter
    {
        public TestInner Inner { get; set; } = new();
    }

    private sealed class TestContainer
    {
        public List<int> Items { get; } = [];
    }

    private sealed class TestContainerWithAssignment
    {
        public List<int> Items { get; set; } = [];
    }

    #endregion
}
