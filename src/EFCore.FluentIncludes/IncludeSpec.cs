using System.Linq.Expressions;
using EFCore.FluentIncludes.Internal;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes;

/// <summary>
/// Base class for defining reusable include specifications.
/// Inherit from this class to create reusable include patterns that can be applied to queries.
/// </summary>
/// <typeparam name="TEntity">The entity type this specification applies to.</typeparam>
/// <example>
/// <code>
/// public class OrderFullSpec : IncludeSpec&lt;Order&gt;
/// {
///     public OrderFullSpec()
///     {
///         Include(o => o.Customer.Address);
///         Include(o => o.LineItems.Each().Product.Category);
///     }
/// }
///
/// // Usage:
/// var orders = context.Orders.WithSpec&lt;OrderFullSpec&gt;().ToListAsync();
/// </code>
/// </example>
public abstract class IncludeSpec<TEntity> where TEntity : class
{
    private readonly List<List<PathSegment>> _paths = [];
    private bool _useSplitQuery;
    private bool _asNoTracking;
    private bool _asNoTrackingWithIdentityResolution;

    /// <summary>
    /// Gets the parsed paths defined in this specification.
    /// </summary>
    internal IReadOnlyList<List<PathSegment>> Paths => _paths;

    /// <summary>
    /// Adds an include path to this specification.
    /// </summary>
    /// <typeparam name="TProperty">The type of the included property.</typeparam>
    /// <param name="path">The path expression.</param>
    /// <returns>This specification for chaining.</returns>
    protected IncludeSpec<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> path)
    {
        var parsedPath = PathParser.Parse(path);
        _paths.Add(parsedPath);
        return this;
    }

    /// <summary>
    /// Adds multiple include paths to this specification.
    /// </summary>
    /// <param name="paths">The path expressions.</param>
    /// <returns>This specification for chaining.</returns>
    protected IncludeSpec<TEntity> Include(params Expression<Func<TEntity, object?>>[] paths)
    {
        foreach (var path in paths)
        {
            var parsedPath = PathParser.Parse(path);
            _paths.Add(parsedPath);
        }
        return this;
    }

    /// <summary>
    /// Includes all paths from another specification (inheritance support).
    /// Call this in derived class constructors to include base spec paths.
    /// </summary>
    /// <typeparam name="TSpec">The specification type to include.</typeparam>
    /// <returns>This specification for chaining.</returns>
    protected IncludeSpec<TEntity> IncludeFrom<TSpec>() where TSpec : IncludeSpec<TEntity>, new()
    {
        var spec = new TSpec();
        _paths.AddRange(spec.Paths);
        return this;
    }

    /// <summary>
    /// Includes all paths from another specification instance.
    /// </summary>
    /// <param name="spec">The specification to include.</param>
    /// <returns>This specification for chaining.</returns>
    protected IncludeSpec<TEntity> IncludeFrom(IncludeSpec<TEntity> spec)
    {
        _paths.AddRange(spec.Paths);
        return this;
    }

    /// <summary>
    /// Includes multiple navigation paths branching from a common base path.
    /// This reduces repetition when multiple paths share the same prefix, especially with filtered collections.
    /// </summary>
    /// <typeparam name="TNav">The navigation type at the end of the base path. For collections, use <see cref="CollectionExtensions.Each{T}(IEnumerable{T})"/> to get the element type.</typeparam>
    /// <param name="basePath">The base path expression. For collections, end with .Each(); for nullable references, optionally use .To().</param>
    /// <param name="subPaths">One or more sub-path expressions starting from the base path's target type.</param>
    /// <returns>This specification for chaining.</returns>
    /// <example>
    /// <code>
    /// public class OrderDetailsSpec : IncludeSpec&lt;Order&gt;
    /// {
    ///     public OrderDetailsSpec()
    ///     {
    ///         IncludeFrom(
    ///             o => o.LineItems.Where(li => li.IsActive).Each(),
    ///             li => li.Product!.Category,
    ///             li => li.Discounts.Each().Promotion);
    ///     }
    /// }
    /// </code>
    /// </example>
    protected IncludeSpec<TEntity> IncludeFrom<TNav>(
        Expression<Func<TEntity, TNav>> basePath,
        params Expression<Func<TNav, object?>>[] subPaths)
    {
        if (subPaths.Length == 0)
        {
            // No sub-paths, just include the base path itself
            var baseOnlyPath = PathParser.Parse(basePath);
            _paths.Add(baseOnlyPath);
            return this;
        }

        var baseSegments = PathParser.Parse(basePath);

        foreach (var subPath in subPaths)
        {
            var subSegments = PathParser.Parse(subPath);
            var combinedPath = new List<PathSegment>(baseSegments.Count + subSegments.Count);
            combinedPath.AddRange(baseSegments);
            combinedPath.AddRange(subSegments);
            _paths.Add(combinedPath);
        }

        return this;
    }

    /// <summary>
    /// Configures the specification to use split queries.
    /// When applied, the query will execute as multiple SQL queries instead of a single query with JOINs.
    /// This can improve performance when loading entities with multiple collection navigations.
    /// </summary>
    /// <returns>This specification for chaining.</returns>
    /// <remarks>
    /// Split queries avoid the "cartesian explosion" problem that can occur when loading multiple
    /// collection navigations in a single query. However, they require multiple database round-trips.
    /// </remarks>
    protected IncludeSpec<TEntity> UseSplitQuery()
    {
        _useSplitQuery = true;
        return this;
    }

    /// <summary>
    /// Configures the specification to disable change tracking.
    /// Entities returned by the query will not be tracked by the DbContext.
    /// </summary>
    /// <returns>This specification for chaining.</returns>
    /// <remarks>
    /// Use this for read-only scenarios to improve performance. Changes to returned entities
    /// will not be persisted when SaveChanges is called.
    /// </remarks>
    protected IncludeSpec<TEntity> AsNoTracking()
    {
        _asNoTracking = true;
        _asNoTrackingWithIdentityResolution = false;
        return this;
    }

    /// <summary>
    /// Configures the specification to disable change tracking but preserve identity resolution.
    /// Entities returned by the query will not be tracked, but duplicate entities in the result
    /// will resolve to the same instance.
    /// </summary>
    /// <returns>This specification for chaining.</returns>
    /// <remarks>
    /// This is useful when you need read-only entities but want to maintain referential integrity
    /// within the result set (e.g., multiple orders referencing the same customer).
    /// </remarks>
    protected IncludeSpec<TEntity> AsNoTrackingWithIdentityResolution()
    {
        _asNoTrackingWithIdentityResolution = true;
        _asNoTracking = false;
        return this;
    }

    /// <summary>
    /// Applies this specification to a queryable.
    /// </summary>
    /// <param name="query">The queryable to apply includes to.</param>
    /// <returns>The queryable with includes applied.</returns>
    public IQueryable<TEntity> Apply(IQueryable<TEntity> query)
    {
        query = IncludeBuilder.ApplyIncludes(query, _paths);

        if (_useSplitQuery)
            query = query.AsSplitQuery();

        if (_asNoTracking)
            query = query.AsNoTracking();
        else if (_asNoTrackingWithIdentityResolution)
            query = query.AsNoTrackingWithIdentityResolution();

        return query;
    }
}
