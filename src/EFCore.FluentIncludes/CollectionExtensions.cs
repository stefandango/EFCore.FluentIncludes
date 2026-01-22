namespace EFCore.FluentIncludes;

/// <summary>
/// Extension methods for navigating through collections in include path expressions.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Marker method to indicate navigation through a collection in an include path expression.
    /// This method is never actually executed - it's used purely for expression tree analysis.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to navigate through.</param>
    /// <returns>A single element (for expression building purposes only).</returns>
    /// <example>
    /// <code>
    /// // Instead of:
    /// .Include(o => o.LineItems).ThenInclude(li => li.Product)
    ///
    /// // Write:
    /// .IncludePaths(o => o.LineItems.Each().Product)
    /// </code>
    /// </example>
    public static T Each<T>(this IEnumerable<T> collection)
    {
        // This method should never be called at runtime.
        // It exists purely for expression tree building.
        throw new InvalidOperationException(
            "The Each() method is a marker for include path expressions and should not be called directly. " +
            "Use it only within IncludePaths() expressions.");
    }

    /// <summary>
    /// Marker method to indicate navigation through a collection in an include path expression.
    /// This method is never actually executed - it's used purely for expression tree analysis.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to navigate through.</param>
    /// <returns>A single element (for expression building purposes only).</returns>
    public static T Each<T>(this ICollection<T> collection)
    {
        throw new InvalidOperationException(
            "The Each() method is a marker for include path expressions and should not be called directly. " +
            "Use it only within IncludePaths() expressions.");
    }

    /// <summary>
    /// Marker method to indicate navigation through a list in an include path expression.
    /// This method is never actually executed - it's used purely for expression tree analysis.
    /// </summary>
    /// <typeparam name="T">The element type of the list.</typeparam>
    /// <param name="list">The list to navigate through.</param>
    /// <returns>A single element (for expression building purposes only).</returns>
    public static T Each<T>(this IList<T> list)
    {
        throw new InvalidOperationException(
            "The Each() method is a marker for include path expressions and should not be called directly. " +
            "Use it only within IncludePaths() expressions.");
    }

    /// <summary>
    /// Marker method to indicate navigation through a list in an include path expression.
    /// This method is never actually executed - it's used purely for expression tree analysis.
    /// </summary>
    /// <typeparam name="T">The element type of the list.</typeparam>
    /// <param name="list">The list to navigate through.</param>
    /// <returns>A single element (for expression building purposes only).</returns>
    public static T Each<T>(this List<T> list)
    {
        throw new InvalidOperationException(
            "The Each() method is a marker for include path expressions and should not be called directly. " +
            "Use it only within IncludePaths() expressions.");
    }
}
