using System.Linq.Expressions;
using EFCore.FluentIncludes.Internal;

namespace EFCore.FluentIncludes;

/// <summary>
/// Extension methods for IQueryable to support path-based includes.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Includes navigation properties using path expressions.
    /// Use <see cref="CollectionExtensions.Each{T}(IEnumerable{T})"/> to navigate through collections.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to apply includes to.</param>
    /// <param name="paths">One or more path expressions.</param>
    /// <returns>The queryable with includes applied.</returns>
    /// <example>
    /// <code>
    /// var orders = context.Orders.IncludePaths(
    ///     o => o.Customer.Address,
    ///     o => o.LineItems.Each().Product.Category);
    /// </code>
    /// </example>
    public static IQueryable<TEntity> IncludePaths<TEntity>(
        this IQueryable<TEntity> query,
        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
    {
        var parsedPaths = paths.Select(PathParser.Parse).ToList();
        return IncludeBuilder.ApplyIncludes(query, parsedPaths);
    }

    /// <summary>
    /// Includes a single navigation path.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TProperty">The final property type.</typeparam>
    /// <param name="query">The queryable to apply the include to.</param>
    /// <param name="path">The path expression.</param>
    /// <returns>The queryable with the include applied.</returns>
    public static IQueryable<TEntity> IncludePath<TEntity, TProperty>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, TProperty>> path) where TEntity : class
    {
        var parsedPath = PathParser.Parse(path);
        return IncludeBuilder.ApplyIncludes(query, [parsedPath]);
    }

    /// <summary>
    /// Conditionally includes navigation properties using path expressions.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to apply includes to.</param>
    /// <param name="condition">If true, applies the includes; otherwise, returns the query unchanged.</param>
    /// <param name="paths">One or more path expressions.</param>
    /// <returns>The queryable with includes applied if condition is true.</returns>
    public static IQueryable<TEntity> IncludePathsIf<TEntity>(
        this IQueryable<TEntity> query,
        bool condition,
        params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
    {
        return condition ? query.IncludePaths(paths) : query;
    }

    /// <summary>
    /// Applies an include specification to the query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TSpec">The specification type.</typeparam>
    /// <param name="query">The queryable to apply the specification to.</param>
    /// <returns>The queryable with the specification applied.</returns>
    /// <example>
    /// <code>
    /// var orders = context.Orders.WithSpec&lt;OrderFullSpec&gt;().ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<TEntity> WithSpec<TEntity, TSpec>(this IQueryable<TEntity> query)
        where TEntity : class
        where TSpec : IncludeSpec<TEntity>, new()
    {
        var spec = new TSpec();
        return spec.Apply(query);
    }

    /// <summary>
    /// Applies an include specification instance to the query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to apply the specification to.</param>
    /// <param name="spec">The specification instance.</param>
    /// <returns>The queryable with the specification applied.</returns>
    public static IQueryable<TEntity> WithSpec<TEntity>(
        this IQueryable<TEntity> query,
        IncludeSpec<TEntity> spec) where TEntity : class
    {
        return spec.Apply(query);
    }

    /// <summary>
    /// Applies multiple include specifications to the query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to apply the specifications to.</param>
    /// <param name="specs">The specification instances.</param>
    /// <returns>The queryable with all specifications applied.</returns>
    public static IQueryable<TEntity> WithSpecs<TEntity>(
        this IQueryable<TEntity> query,
        params IncludeSpec<TEntity>[] specs) where TEntity : class
    {
        foreach (var spec in specs)
        {
            query = spec.Apply(query);
        }
        return query;
    }

    /// <summary>
    /// Applies two include specifications to the query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TSpec1">The first specification type.</typeparam>
    /// <typeparam name="TSpec2">The second specification type.</typeparam>
    /// <param name="query">The queryable to apply the specifications to.</param>
    /// <returns>The queryable with both specifications applied.</returns>
    public static IQueryable<TEntity> WithSpecs<TEntity, TSpec1, TSpec2>(this IQueryable<TEntity> query)
        where TEntity : class
        where TSpec1 : IncludeSpec<TEntity>, new()
        where TSpec2 : IncludeSpec<TEntity>, new()
    {
        return query
            .WithSpec<TEntity, TSpec1>()
            .WithSpec<TEntity, TSpec2>();
    }

    /// <summary>
    /// Applies three include specifications to the query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TSpec1">The first specification type.</typeparam>
    /// <typeparam name="TSpec2">The second specification type.</typeparam>
    /// <typeparam name="TSpec3">The third specification type.</typeparam>
    /// <param name="query">The queryable to apply the specifications to.</param>
    /// <returns>The queryable with all specifications applied.</returns>
    public static IQueryable<TEntity> WithSpecs<TEntity, TSpec1, TSpec2, TSpec3>(this IQueryable<TEntity> query)
        where TEntity : class
        where TSpec1 : IncludeSpec<TEntity>, new()
        where TSpec2 : IncludeSpec<TEntity>, new()
        where TSpec3 : IncludeSpec<TEntity>, new()
    {
        return query
            .WithSpec<TEntity, TSpec1>()
            .WithSpec<TEntity, TSpec2>()
            .WithSpec<TEntity, TSpec3>();
    }

    /// <summary>
    /// Conditionally applies an include specification to the query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TSpec">The specification type.</typeparam>
    /// <param name="query">The queryable to apply the specification to.</param>
    /// <param name="condition">If true, applies the specification; otherwise, returns the query unchanged.</param>
    /// <returns>The queryable with the specification applied if condition is true.</returns>
    public static IQueryable<TEntity> WithSpecIf<TEntity, TSpec>(
        this IQueryable<TEntity> query,
        bool condition)
        where TEntity : class
        where TSpec : IncludeSpec<TEntity>, new()
    {
        return condition ? query.WithSpec<TEntity, TSpec>() : query;
    }

    /// <summary>
    /// Includes multiple navigation paths branching from a common base path.
    /// This reduces repetition when multiple paths share the same prefix, especially with filtered collections.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TNav">The navigation type at the end of the base path. For collections, use <see cref="CollectionExtensions.Each{T}(IEnumerable{T})"/> to get the element type.</typeparam>
    /// <param name="query">The queryable to apply includes to.</param>
    /// <param name="basePath">The base path expression. For collections, end with .Each(); for nullable references, optionally use .To().</param>
    /// <param name="subPaths">One or more sub-path expressions starting from the base path's target type.</param>
    /// <returns>The queryable with includes applied.</returns>
    /// <example>
    /// <code>
    /// // Instead of repeating the filter:
    /// // .IncludePaths(
    /// //     o => o.LineItems.Where(li => li.IsActive).Each().Product,
    /// //     o => o.LineItems.Where(li => li.IsActive).Each().Discounts)
    ///
    /// // Use IncludeFrom to define the base once:
    /// var orders = context.Orders.IncludeFrom(
    ///     o => o.LineItems.Where(li => li.IsActive).Each(),
    ///     li => li.Product,
    ///     li => li.Discounts.Each().Promotion);
    /// </code>
    /// </example>
    public static IQueryable<TEntity> IncludeFrom<TEntity, TNav>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, TNav>> basePath,
        params Expression<Func<TNav, object?>>[] subPaths) where TEntity : class
    {
        if (subPaths.Length == 0)
        {
            // No sub-paths, just include the base path itself
            var baseOnlyPath = PathParser.Parse(basePath);
            return IncludeBuilder.ApplyIncludes(query, [baseOnlyPath]);
        }

        var baseSegments = PathParser.Parse(basePath);
        var allPaths = new List<List<PathSegment>>(subPaths.Length);

        foreach (var subPath in subPaths)
        {
            var subSegments = PathParser.Parse(subPath);
            var combinedPath = new List<PathSegment>(baseSegments.Count + subSegments.Count);
            combinedPath.AddRange(baseSegments);
            combinedPath.AddRange(subSegments);
            allPaths.Add(combinedPath);
        }

        return IncludeBuilder.ApplyIncludes(query, allPaths);
    }

    /// <summary>
    /// Conditionally includes multiple navigation paths branching from a common base path.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TNav">The navigation type at the end of the base path.</typeparam>
    /// <param name="query">The queryable to apply includes to.</param>
    /// <param name="condition">If true, applies the includes; otherwise, returns the query unchanged.</param>
    /// <param name="basePath">The base path expression.</param>
    /// <param name="subPaths">One or more sub-path expressions starting from the base path's target type.</param>
    /// <returns>The queryable with includes applied if condition is true.</returns>
    public static IQueryable<TEntity> IncludeFromIf<TEntity, TNav>(
        this IQueryable<TEntity> query,
        bool condition,
        Expression<Func<TEntity, TNav>> basePath,
        params Expression<Func<TNav, object?>>[] subPaths) where TEntity : class
    {
        return condition ? query.IncludeFrom(basePath, subPaths) : query;
    }
}
