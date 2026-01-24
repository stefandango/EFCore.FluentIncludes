using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.FluentIncludes.Internal;

/// <summary>
/// Represents ordering information for a collection navigation.
/// </summary>
internal sealed record OrderingInfo(LambdaExpression KeySelector, bool Descending);

/// <summary>
/// Represents a single segment in a navigation path.
/// </summary>
internal sealed class PathSegment
{
    public required PropertyInfo Property { get; init; }
    public required bool IsCollection { get; init; }
    public required Type SourceType { get; init; }
    public required Type TargetType { get; init; }

    /// <summary>
    /// Optional filter expression for filtered includes (e.g., Where(x => x.IsActive)).
    /// Only applicable when IsCollection is true.
    /// </summary>
    public LambdaExpression? Filter { get; init; }

    /// <summary>
    /// Optional ordering expressions for ordered includes (e.g., OrderBy(x => x.Name)).
    /// Only applicable when IsCollection is true. Applied after Filter if both are present.
    /// </summary>
    public IReadOnlyList<OrderingInfo>? Orderings { get; init; }
}
