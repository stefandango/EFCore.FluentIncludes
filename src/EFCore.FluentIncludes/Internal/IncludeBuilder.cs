using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EFCore.FluentIncludes.Internal;

/// <summary>
/// Builds EF Core Include/ThenInclude chains from parsed path segments.
/// </summary>
internal static class IncludeBuilder
{
    private static readonly MethodInfo IncludeMethod;
    private static readonly MethodInfo ThenIncludeAfterReferenceMethod;
    private static readonly MethodInfo ThenIncludeAfterCollectionMethod;

    static IncludeBuilder()
    {
        // Get the Include method
        IncludeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include)
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType.IsGenericType
                        && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

        // Get both ThenInclude overloads
        var thenIncludeMethods = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude)
                        && m.GetParameters().Length == 2)
            .ToList();

        // ThenInclude after reference navigation: IIncludableQueryable<TEntity, TProperty>
        ThenIncludeAfterReferenceMethod = thenIncludeMethods.First(m =>
        {
            var firstParam = m.GetParameters()[0].ParameterType;
            return firstParam.IsGenericType
                   && firstParam.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>)
                   && !IsEnumerableType(firstParam.GetGenericArguments()[1]);
        });

        // ThenInclude after collection navigation: IIncludableQueryable<TEntity, IEnumerable<TProperty>>
        ThenIncludeAfterCollectionMethod = thenIncludeMethods.First(m =>
        {
            var firstParam = m.GetParameters()[0].ParameterType;
            if (!firstParam.IsGenericType || firstParam.GetGenericTypeDefinition() != typeof(IIncludableQueryable<,>))
                return false;

            var secondGenericArg = firstParam.GetGenericArguments()[1];
            return IsEnumerableType(secondGenericArg);
        });
    }

    private static bool IsEnumerableType(Type type)
    {
        if (type == typeof(string)) return false;
        return type.IsGenericType && type.GetInterfaces()
            .Concat([type])
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    /// <summary>
    /// Applies include paths to a queryable.
    /// </summary>
    public static IQueryable<TEntity> ApplyIncludes<TEntity>(
        IQueryable<TEntity> query,
        IEnumerable<List<PathSegment>> paths) where TEntity : class
    {
        foreach (var path in paths)
        {
            query = ApplyIncludePath(query, path);
        }

        return query;
    }

    private static IQueryable<TEntity> ApplyIncludePath<TEntity>(
        IQueryable<TEntity> query,
        List<PathSegment> segments) where TEntity : class
    {
        if (segments.Count == 0)
            return query;

        object currentQuery = query;
        Type entityType = typeof(TEntity);
        bool previousWasCollection = false;
        Type? previousPropertyType = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var isFirst = i == 0;

            // Build the lambda expression for this segment (with optional filter)
            var lambdaExpression = BuildPropertyLambda(segment.SourceType, segment.Property, segment.Filter);

            // Determine the result type (filtered collections remain IEnumerable<T>)
            var resultType = segment.Filter is not null
                ? typeof(IEnumerable<>).MakeGenericType(segment.TargetType)
                : segment.Property.PropertyType;

            if (isFirst)
            {
                // Use Include for the first segment
                var includeMethodGeneric = IncludeMethod.MakeGenericMethod(entityType, resultType);
                currentQuery = includeMethodGeneric.Invoke(null, [currentQuery, lambdaExpression])!;
            }
            else
            {
                // Use ThenInclude for subsequent segments
                MethodInfo thenIncludeMethod;

                if (previousWasCollection)
                {
                    // Previous was a collection, use ThenInclude<TEntity, TPreviousProperty, TProperty>
                    // where the queryable is IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>>
                    thenIncludeMethod = ThenIncludeAfterCollectionMethod.MakeGenericMethod(
                        entityType,
                        segment.SourceType,
                        resultType);
                }
                else
                {
                    // Previous was a reference, use ThenInclude<TEntity, TPreviousProperty, TProperty>
                    thenIncludeMethod = ThenIncludeAfterReferenceMethod.MakeGenericMethod(
                        entityType,
                        previousPropertyType!,
                        resultType);
                }

                currentQuery = thenIncludeMethod.Invoke(null, [currentQuery, lambdaExpression])!;
            }

            previousWasCollection = segment.IsCollection;
            previousPropertyType = resultType;
        }

        return (IQueryable<TEntity>)currentQuery;
    }

    private static LambdaExpression BuildPropertyLambda(Type sourceType, PropertyInfo property, LambdaExpression? filter)
    {
        var parameter = Expression.Parameter(sourceType, "x");
        Expression body = Expression.Property(parameter, property);

        // If there's a filter, wrap the property access in a Where() call
        if (filter is not null)
        {
            // Get the element type from the collection
            var collectionType = property.PropertyType;
            if (!IsEnumerableType(collectionType))
            {
                throw new InvalidOperationException(
                    $"Cannot apply filter to non-collection property '{property.Name}'.");
            }

            var elementType = collectionType.GetInterfaces()
                .Concat([collectionType])
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .GetGenericArguments()[0];

            // Find the Enumerable.Where method
            var whereMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Where"
                            && m.GetParameters().Length == 2
                            && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                .MakeGenericMethod(elementType);

            // Call Where(collection, filter)
            body = Expression.Call(whereMethod, body, filter);
        }

        return Expression.Lambda(body, parameter);
    }
}
