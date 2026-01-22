namespace EFCore.FluentIncludes;

/// <summary>
/// Extension methods for navigating through nullable properties in include path expressions.
/// </summary>
public static class NavigationExtensions
{
    /// <summary>
    /// Marker method to indicate navigation through a nullable property in an include path expression.
    /// This method is never actually executed - it's used purely for expression tree analysis.
    /// </summary>
    /// <typeparam name="T">The navigation property type.</typeparam>
    /// <param name="value">The nullable navigation property.</param>
    /// <returns>The same value (for expression building purposes only).</returns>
    /// <example>
    /// <code>
    /// // Instead of using the null-forgiving operator:
    /// .IncludePaths(o => o.Customer!.Address)
    ///
    /// // Write:
    /// .IncludePaths(o => o.Customer.To().Address)
    /// </code>
    /// </example>
    /// <remarks>
    /// The lambda expression is never executed at runtime - it's only analyzed to extract
    /// the navigation path. This means there's no actual null dereference risk.
    /// The To() method provides a semantic alternative to the ! operator that's consistent
    /// with the Each() method used for collections.
    /// </remarks>
    public static T To<T>(this T? value) where T : class
    {
        // This method should never be called at runtime.
        // It exists purely for expression tree building.
        throw new InvalidOperationException(
            "The To() method is a marker for include path expressions and should not be called directly. " +
            "Use it only within IncludePaths() expressions.");
    }
}
