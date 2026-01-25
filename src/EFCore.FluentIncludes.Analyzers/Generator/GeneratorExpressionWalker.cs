using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.FluentIncludes.Analyzers.Generator;

/// <summary>
/// Walks an include path expression and extracts information for code generation.
/// </summary>
internal sealed class GeneratorExpressionWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly ITypeSymbol _rootEntityType;
    private readonly CancellationToken _ct;

    private static readonly ImmutableHashSet<string> OrderingMethodNames = ImmutableHashSet.Create(
        "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"
    );

    public string? FallbackReason { get; private set; }

    public GeneratorExpressionWalker(SemanticModel semanticModel, ITypeSymbol rootEntityType, CancellationToken ct)
    {
        _semanticModel = semanticModel;
        _rootEntityType = rootEntityType;
        _ct = ct;
    }

    /// <summary>
    /// Walks the expression and returns a list of segments, or null if the expression cannot be generated.
    /// </summary>
    public List<IncludeSegmentInfo>? Walk(ExpressionSyntax expression)
    {
        var rawSegments = CollectSegments(expression).ToList();

        if (FallbackReason is not null)
        {
            return null;
        }

        // Reverse to get root-to-leaf order
        rawSegments.Reverse();

        // Convert to IncludeSegmentInfo
        var result = new List<IncludeSegmentInfo>();
        ITypeSymbol? currentType = _rootEntityType;

        foreach (var segment in rawSegments)
        {
            if (segment.Kind == SegmentKind.Parameter)
            {
                currentType = _rootEntityType;
                continue;
            }

            if (segment.Kind == SegmentKind.PropertyAccess)
            {
                if (segment.Property is null)
                {
                    FallbackReason = $"Could not resolve property '{segment.PropertyName}'";
                    return null;
                }

                result.Add(new IncludeSegmentInfo(
                    propertyName: segment.Property.Name,
                    sourceTypeName: segment.Property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    targetTypeName: (segment.IsCollection
                        ? GetCollectionElementType(segment.Property.Type)
                        : segment.Property.Type)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object",
                    isCollection: segment.IsCollection,
                    filterLambdaSyntax: segment.FilterLambdaSyntax,
                    orderings: segment.Orderings));

                currentType = segment.IsCollection
                    ? GetCollectionElementType(segment.Property.Type)
                    : segment.Property.Type;
            }
            // Each, To, NullForgiving don't add segments - they're markers
        }

        return result;
    }

    private IEnumerable<RawSegment> CollectSegments(ExpressionSyntax expression)
    {
        var current = expression;
        string? pendingFilterLambda = null;
        var pendingOrderings = new List<OrderingSegmentInfo>();

        while (current is not null)
        {
            _ct.ThrowIfCancellationRequested();

            switch (current)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    var segment = GetPropertySegment(memberAccess, pendingFilterLambda, pendingOrderings);
                    if (segment is not null)
                    {
                        yield return segment;
                    }
                    // Reset pending after applying to property
                    pendingFilterLambda = null;
                    pendingOrderings = new List<OrderingSegmentInfo>();
                    current = memberAccess.Expression;
                    break;

                case InvocationExpressionSyntax invocation:
                    var (invSegment, inner, filter, orderings) = AnalyzeInvocation(invocation, pendingFilterLambda, pendingOrderings);
                    if (invSegment is not null)
                    {
                        yield return invSegment;
                    }
                    pendingFilterLambda = filter ?? pendingFilterLambda;
                    if (orderings is not null)
                    {
                        // Prepend new orderings (we're walking backwards)
                        pendingOrderings.InsertRange(0, orderings);
                    }
                    current = inner;
                    break;

                case IdentifierNameSyntax:
                    // Lambda parameter - we're done
                    yield return new RawSegment(SegmentKind.Parameter);
                    current = null;
                    break;

                case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                    // Null-forgiving operator (!)
                    yield return new RawSegment(SegmentKind.NullForgiving);
                    current = postfix.Operand;
                    break;

                case ParenthesizedExpressionSyntax paren:
                    current = paren.Expression;
                    break;

                case CastExpressionSyntax cast:
                    // Type casts are transparent for generation purposes
                    current = cast.Expression;
                    break;

                default:
                    FallbackReason = $"Unsupported expression type: {current.Kind()}";
                    yield break;
            }
        }
    }

    private RawSegment? GetPropertySegment(
        MemberAccessExpressionSyntax memberAccess,
        string? filterLambda,
        List<OrderingSegmentInfo> orderings)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(memberAccess, _ct);

        if (symbolInfo.Symbol is not IPropertySymbol property)
        {
            FallbackReason = $"Member '{memberAccess.Name.Identifier.Text}' is not a property";
            return null;
        }

        var isCollection = IsCollectionType(property.Type);

        return new RawSegment(SegmentKind.PropertyAccess)
        {
            PropertyName = property.Name,
            Property = property,
            IsCollection = isCollection,
            FilterLambdaSyntax = filterLambda,
            Orderings = orderings.ToImmutableArray()
        };
    }

    private (RawSegment? segment, ExpressionSyntax? inner, string? filter, List<OrderingSegmentInfo>? orderings)
        AnalyzeInvocation(
            InvocationExpressionSyntax invocation,
            string? currentFilter,
            List<OrderingSegmentInfo> currentOrderings)
    {
        string? methodName = null;
        ExpressionSyntax? inner = null;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            methodName = memberAccess.Name.Identifier.Text;
            inner = memberAccess.Expression;
        }
        else if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            methodName = identifier.Identifier.Text;
        }

        switch (methodName)
        {
            case "Each":
                return (new RawSegment(SegmentKind.EachCall), inner, null, null);

            case "To":
                return (new RawSegment(SegmentKind.ToCall), inner, null, null);

            case "Where":
                var filterArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (filterArg is LambdaExpressionSyntax filterLambda)
                {
                    // Check if the filter captures any variables (closure)
                    if (CapturesVariables(filterLambda))
                    {
                        FallbackReason = "Filter expression captures variables (closure)";
                        return (null, null, null, null);
                    }

                    var filterSyntax = filterLambda.ToString();
                    return (null, inner, filterSyntax, null);
                }
                else
                {
                    FallbackReason = "Where argument is not a lambda expression";
                    return (null, null, null, null);
                }

            case "OrderBy":
            case "OrderByDescending":
            case "ThenBy":
            case "ThenByDescending":
                var keyArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (keyArg is LambdaExpressionSyntax keyLambda)
                {
                    if (CapturesVariables(keyLambda))
                    {
                        FallbackReason = "Ordering expression captures variables (closure)";
                        return (null, null, null, null);
                    }

                    var orderingInfo = new OrderingSegmentInfo(
                        keySelectorSyntax: keyLambda.ToString(),
                        isDescending: methodName.Contains("Descending"));

                    return (null, inner, null, new List<OrderingSegmentInfo> { orderingInfo });
                }
                else
                {
                    FallbackReason = $"{methodName} argument is not a lambda expression";
                    return (null, null, null, null);
                }

            default:
                FallbackReason = $"Unsupported method call: {methodName}";
                return (null, null, null, null);
        }
    }

    /// <summary>
    /// Checks if a lambda expression captures any external variables.
    /// </summary>
    private bool CapturesVariables(LambdaExpressionSyntax lambda)
    {
        var body = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body,
            ParenthesizedLambdaExpressionSyntax paren => paren.Body,
            _ => null
        };

        if (body is null)
        {
            return false;
        }

        // Get the lambda's declared parameters
        var parameterNames = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter.Identifier.Text },
            ParenthesizedLambdaExpressionSyntax paren => paren.ParameterList.Parameters.Select(p => p.Identifier.Text).ToArray(),
            _ => Array.Empty<string>()
        };

        // Find all identifier names in the body
        foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var name = identifier.Identifier.Text;

            // Skip if it's a parameter
            if (parameterNames.Contains(name))
            {
                continue;
            }

            // Check what this identifier refers to
            var symbol = _semanticModel.GetSymbolInfo(identifier, _ct).Symbol;

            if (symbol is ILocalSymbol or IParameterSymbol && !parameterNames.Contains(symbol.Name))
            {
                // This is a captured local variable or parameter from outer scope
                return true;
            }

            if (symbol is IFieldSymbol fieldSymbol && !fieldSymbol.IsStatic && !fieldSymbol.IsConst)
            {
                // Instance field capture (closure over 'this')
                return true;
            }
        }

        return false;
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    }

    private static ITypeSymbol? GetCollectionElementType(ITypeSymbol collectionType)
    {
        if (collectionType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        var enumerableInterface = collectionType.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        if (enumerableInterface is not null && enumerableInterface.TypeArguments.Length > 0)
        {
            return enumerableInterface.TypeArguments[0];
        }

        if (collectionType is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
            namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    private enum SegmentKind
    {
        Parameter,
        PropertyAccess,
        EachCall,
        ToCall,
        NullForgiving
    }

    private sealed class RawSegment
    {
        public RawSegment(SegmentKind kind)
        {
            Kind = kind;
        }

        public SegmentKind Kind { get; }
        public string? PropertyName { get; set; }
        public IPropertySymbol? Property { get; set; }
        public bool IsCollection { get; set; }
        public string? FilterLambdaSyntax { get; set; }
        public ImmutableArray<OrderingSegmentInfo> Orderings { get; set; } = ImmutableArray<OrderingSegmentInfo>.Empty;
    }
}
