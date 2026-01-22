using System.Reflection;

namespace EFCore.FluentIncludes.Internal;

/// <summary>
/// Represents a single segment in a navigation path.
/// </summary>
internal sealed class PathSegment
{
    public required PropertyInfo Property { get; init; }
    public required bool IsCollection { get; init; }
    public required Type SourceType { get; init; }
    public required Type TargetType { get; init; }
}
