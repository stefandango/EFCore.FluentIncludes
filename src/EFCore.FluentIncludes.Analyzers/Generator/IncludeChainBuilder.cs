using System.Collections.Immutable;
using System.Text;

namespace EFCore.FluentIncludes.Analyzers.Generator;

/// <summary>
/// Builds EF Core Include/ThenInclude method chains from parsed path segments.
/// </summary>
internal static class IncludeChainBuilder
{
    /// <summary>
    /// Builds a chain of Include/ThenInclude calls from the given segments.
    /// </summary>
    /// <param name="segments">The include path segments.</param>
    /// <param name="entityType">The fully qualified entity type name.</param>
    /// <returns>A string like ".Include(x => x.Customer).ThenInclude(c => c.Address)"</returns>
    public static string BuildIncludeChain(ImmutableArray<IncludeSegmentInfo> segments, string entityType)
    {
        if (segments.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var paramNames = new Stack<string>();
        paramNames.Push("x");

        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var isFirst = i == 0;
            var currentParam = paramNames.Peek();

            // Generate the lambda expression for this segment
            // For ThenInclude, add ! to suppress nullable warnings (EF Core handles null navigation internally)
            var lambdaExpr = BuildSegmentLambda(segment, currentParam, addNullForgiving: !isFirst);

            if (isFirst)
            {
                // Use Include for the first segment
                sb.AppendLine();
                sb.Append($"                .Include({lambdaExpr})");
            }
            else
            {
                // Use ThenInclude for subsequent segments
                sb.AppendLine();
                sb.Append($"                .ThenInclude({lambdaExpr})");
            }

            // Update parameter name for next level
            // Use a unique name based on the property
            var nextParam = GetNextParameterName(segment.PropertyName, paramNames);
            paramNames.Push(nextParam);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the lambda expression for a single segment.
    /// </summary>
    /// <param name="segment">The segment info.</param>
    /// <param name="paramName">The lambda parameter name.</param>
    /// <param name="addNullForgiving">If true, adds ! to the property access (for ThenInclude).</param>
    private static string BuildSegmentLambda(IncludeSegmentInfo segment, string paramName, bool addNullForgiving)
    {
        var sb = new StringBuilder();
        var nullForgiving = addNullForgiving ? "!" : "";
        sb.Append($"{paramName} => {paramName}{nullForgiving}.{segment.PropertyName}");

        // Add filter if present
        if (!string.IsNullOrEmpty(segment.FilterLambdaSyntax))
        {
            sb.Append($".Where({segment.FilterLambdaSyntax})");
        }

        // Add orderings if present
        if (!segment.Orderings.IsDefaultOrEmpty)
        {
            for (int i = 0; i < segment.Orderings.Length; i++)
            {
                var ordering = segment.Orderings[i];
                var methodName = GetOrderingMethodName(i == 0, ordering.IsDescending);
                sb.Append($".{methodName}({ordering.KeySelectorSyntax})");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the appropriate ordering method name.
    /// </summary>
    private static string GetOrderingMethodName(bool isFirst, bool isDescending)
    {
        return (isFirst, isDescending) switch
        {
            (true, false) => "OrderBy",
            (true, true) => "OrderByDescending",
            (false, false) => "ThenBy",
            (false, true) => "ThenByDescending"
        };
    }

    /// <summary>
    /// Gets a unique parameter name for the next level of navigation.
    /// </summary>
    private static string GetNextParameterName(string propertyName, Stack<string> usedNames)
    {
        // Use first letter of property name, lowercase
        var baseName = char.ToLowerInvariant(propertyName[0]).ToString();

        // If already used, add a number suffix
        var candidate = baseName;
        var suffix = 1;

        while (usedNames.Contains(candidate))
        {
            candidate = $"{baseName}{suffix}";
            suffix++;
        }

        return candidate;
    }
}
