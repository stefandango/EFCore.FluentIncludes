using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.FluentIncludes.Internal;

/// <summary>
/// Parses include path expressions to extract navigation path segments.
/// </summary>
internal static class PathParser
{
    private static readonly HashSet<string> OrderingMethodNames = ["OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"];

    /// <summary>
    /// Parses a lambda expression and extracts the navigation path segments.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <typeparam name="TProperty">The final property type.</typeparam>
    /// <param name="pathExpression">The path expression to parse.</param>
    /// <returns>A list of path segments representing the navigation path.</returns>
    public static List<PathSegment> Parse<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> pathExpression)
    {
        var segments = new List<PathSegment>();
        ParseExpression(pathExpression.Body, segments, pendingFilter: null, pendingOrderings: null);
        segments.Reverse(); // Segments are collected in reverse order
        return segments;
    }

    private static void ParseExpression(
        Expression expression,
        List<PathSegment> segments,
        LambdaExpression? pendingFilter,
        List<OrderingInfo>? pendingOrderings)
    {
        switch (expression)
        {
            case MemberExpression memberExpr:
                ParseMemberExpression(memberExpr, segments, pendingFilter, pendingOrderings);
                break;

            case MethodCallExpression methodCallExpr:
                ParseMethodCallExpression(methodCallExpr, segments, pendingFilter, pendingOrderings);
                break;

            case ParameterExpression:
                // Reached the root parameter - stop parsing
                break;

            case UnaryExpression unaryExpr when unaryExpr.NodeType == ExpressionType.Convert:
                // Handle type conversions
                ParseExpression(unaryExpr.Operand, segments, pendingFilter, pendingOrderings);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported expression type '{expression.NodeType}' in include path. " +
                    $"Expression: {expression}");
        }
    }

    private static void ParseMemberExpression(
        MemberExpression memberExpr,
        List<PathSegment> segments,
        LambdaExpression? pendingFilter,
        List<OrderingInfo>? pendingOrderings)
    {
        if (memberExpr.Member is not PropertyInfo propertyInfo)
        {
            throw new InvalidOperationException(
                $"Only property access is supported in include paths. Found: {memberExpr.Member.MemberType}");
        }

        var sourceType = memberExpr.Expression?.Type
            ?? throw new InvalidOperationException("Member expression has no source type.");

        var targetType = propertyInfo.PropertyType;
        var isCollection = IsCollectionType(targetType, out var elementType);

        // Apply pending filter/orderings only to collections
        if (pendingFilter is not null && !isCollection)
        {
            throw new InvalidOperationException(
                $"Where() can only be applied to collection navigation properties. " +
                $"'{propertyInfo.Name}' is not a collection.");
        }

        if (pendingOrderings is not null && !isCollection)
        {
            throw new InvalidOperationException(
                $"OrderBy() can only be applied to collection navigation properties. " +
                $"'{propertyInfo.Name}' is not a collection.");
        }

        segments.Add(new PathSegment
        {
            Property = propertyInfo,
            IsCollection = isCollection,
            SourceType = sourceType,
            TargetType = isCollection ? elementType! : targetType,
            Filter = pendingFilter,
            Orderings = pendingOrderings
        });

        // Continue parsing the chain (no pending filter/orderings for previous segments)
        if (memberExpr.Expression is not null)
        {
            ParseExpression(memberExpr.Expression, segments, pendingFilter: null, pendingOrderings: null);
        }
    }

    private static void ParseMethodCallExpression(
        MethodCallExpression methodCallExpr,
        List<PathSegment> segments,
        LambdaExpression? pendingFilter,
        List<OrderingInfo>? pendingOrderings)
    {
        // Check if this is an Each() call (for collections)
        if (methodCallExpr.Method.Name == "Each" &&
            methodCallExpr.Method.DeclaringType == typeof(CollectionExtensions))
        {
            // Each() is just a marker - continue parsing the collection expression
            // Pass along any pending filter/orderings
            if (methodCallExpr.Arguments.Count > 0)
            {
                ParseExpression(methodCallExpr.Arguments[0], segments, pendingFilter, pendingOrderings);
            }
            else if (methodCallExpr.Object is not null)
            {
                ParseExpression(methodCallExpr.Object, segments, pendingFilter, pendingOrderings);
            }
        }
        // Check if this is a To() call (for nullable navigations)
        else if (methodCallExpr.Method.Name == "To" &&
                 methodCallExpr.Method.DeclaringType == typeof(NavigationExtensions))
        {
            // To() is just a marker - continue parsing the navigation expression
            if (methodCallExpr.Arguments.Count > 0)
            {
                ParseExpression(methodCallExpr.Arguments[0], segments, pendingFilter, pendingOrderings);
            }
            else if (methodCallExpr.Object is not null)
            {
                ParseExpression(methodCallExpr.Object, segments, pendingFilter, pendingOrderings);
            }
        }
        // Check if this is a Where() call (for filtered includes)
        else if (methodCallExpr.Method.Name == "Where" && IsLinqWhereMethod(methodCallExpr.Method))
        {
            // Extract the filter predicate (second argument)
            if (methodCallExpr.Arguments.Count < 2)
            {
                throw new InvalidOperationException("Where() call must have a predicate argument.");
            }

            var filterArg = methodCallExpr.Arguments[1];
            var filter = filterArg switch
            {
                UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda,
                LambdaExpression lambda => lambda,
                _ => throw new InvalidOperationException(
                    $"Where() predicate must be a lambda expression. Found: {filterArg.NodeType}")
            };

            // Continue parsing from the collection (first argument), with the filter pending
            // Orderings come after Where, so pass them along
            ParseExpression(methodCallExpr.Arguments[0], segments, pendingFilter: filter, pendingOrderings);
        }
        // Check if this is an ordering method call (OrderBy, OrderByDescending, ThenBy, ThenByDescending)
        else if (OrderingMethodNames.Contains(methodCallExpr.Method.Name) && IsLinqOrderingMethod(methodCallExpr.Method))
        {
            // Extract the key selector (second argument)
            if (methodCallExpr.Arguments.Count < 2)
            {
                throw new InvalidOperationException($"{methodCallExpr.Method.Name}() call must have a key selector argument.");
            }

            var keySelectorArg = methodCallExpr.Arguments[1];
            var keySelector = keySelectorArg switch
            {
                UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda,
                LambdaExpression lambda => lambda,
                _ => throw new InvalidOperationException(
                    $"{methodCallExpr.Method.Name}() key selector must be a lambda expression. Found: {keySelectorArg.NodeType}")
            };

            var isDescending = methodCallExpr.Method.Name is "OrderByDescending" or "ThenByDescending";
            var ordering = new OrderingInfo(keySelector, isDescending);

            // Orderings are collected in reverse order (we parse from end to start)
            // so we prepend to maintain correct order
            var orderings = pendingOrderings is null
                ? [ordering]
                : pendingOrderings.Prepend(ordering).ToList();

            // Continue parsing from the collection (first argument)
            ParseExpression(methodCallExpr.Arguments[0], segments, pendingFilter, pendingOrderings: orderings);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported method call '{methodCallExpr.Method.Name}' in include path. " +
                "Only property access, Each(), To(), Where(), and ordering methods (OrderBy, OrderByDescending, ThenBy, ThenByDescending) are supported.");
        }
    }

    private static bool IsLinqWhereMethod(MethodInfo method)
    {
        // Check if this is System.Linq.Enumerable.Where or System.Linq.Queryable.Where
        var declaringType = method.DeclaringType;
        return declaringType == typeof(Enumerable) || declaringType == typeof(Queryable);
    }

    private static bool IsLinqOrderingMethod(MethodInfo method)
    {
        // Check if this is System.Linq.Enumerable or System.Linq.Queryable ordering method
        var declaringType = method.DeclaringType;
        return declaringType == typeof(Enumerable) || declaringType == typeof(Queryable);
    }

    private static bool IsCollectionType(Type type, out Type? elementType)
    {
        elementType = null;

        // Check for IEnumerable<T> (but not string)
        if (type == typeof(string))
        {
            return false;
        }

        // Check generic collection interfaces
        var enumerableInterface = type.GetInterfaces()
            .Concat(type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? [type]
                : [])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface is not null)
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        return false;
    }
}
