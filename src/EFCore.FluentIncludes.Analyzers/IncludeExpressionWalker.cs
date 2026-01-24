using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EFCore.FluentIncludes.Analyzers;

/// <summary>
/// Walks an include path expression and validates each navigation step.
/// </summary>
internal sealed class IncludeExpressionWalker
{
    private readonly SyntaxNodeAnalysisContext _context;
    private readonly ITypeSymbol _rootEntityType;

    /// <summary>
    /// Marker method names from CollectionExtensions.
    /// </summary>
    private static readonly HashSet<string> CollectionMarkerMethods = new() { "Each" };

    /// <summary>
    /// Marker method names from NavigationExtensions.
    /// </summary>
    private static readonly HashSet<string> NavigationMarkerMethods = new() { "To" };

    /// <summary>
    /// LINQ filter method names.
    /// </summary>
    private static readonly HashSet<string> FilterMethods = new() { "Where" };

    /// <summary>
    /// LINQ ordering method names.
    /// </summary>
    private static readonly HashSet<string> OrderingMethods = new()
    {
        "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"
    };

    public IncludeExpressionWalker(SyntaxNodeAnalysisContext context, ITypeSymbol rootEntityType)
    {
        _context = context;
        _rootEntityType = rootEntityType;
    }

    /// <summary>
    /// Analyzes the expression body of an include path lambda.
    /// </summary>
    public void Analyze(ExpressionSyntax expression)
    {
        // Walk the expression from outside-in, collecting segments
        // Then validate the chain
        var segments = CollectSegments(expression).ToList();

        // Segments are collected outside-in, reverse for validation (root to leaf)
        segments.Reverse();

        ValidateSegments(segments);
    }

    /// <summary>
    /// Represents a segment in the navigation path.
    /// </summary>
    private sealed class PathSegmentInfo
    {
        public PathSegmentInfo(SyntaxNode node, SegmentKind kind)
        {
            Node = node;
            Kind = kind;
        }

        public SyntaxNode Node { get; }
        public SegmentKind Kind { get; }
        public string? PropertyName { get; set; }
        public ITypeSymbol? SourceType { get; set; }
        public ITypeSymbol? TargetType { get; set; }
        public IPropertySymbol? Property { get; set; }
        public bool IsCollection { get; set; }
        public bool IsNullable { get; set; }
        public string? MethodName { get; set; }
        public LambdaExpressionSyntax? Predicate { get; set; }
    }

    private enum SegmentKind
    {
        Parameter,
        PropertyAccess,
        EachCall,
        ToCall,
        WhereCall,
        OrderingCall,
        NullForgiving,
        Unknown
    }

    private IEnumerable<PathSegmentInfo> CollectSegments(ExpressionSyntax expression)
    {
        var current = expression;

        while (current != null)
        {
            switch (current)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    var propertyInfo = GetPropertyInfo(memberAccess);
                    yield return propertyInfo;
                    current = memberAccess.Expression;
                    break;

                case InvocationExpressionSyntax invocation:
                    var (segment, inner) = AnalyzeInvocation(invocation);
                    yield return segment;
                    current = inner;
                    break;

                case IdentifierNameSyntax identifier:
                    // This is the lambda parameter
                    yield return new PathSegmentInfo(identifier, SegmentKind.Parameter)
                    {
                        SourceType = _rootEntityType,
                        TargetType = _rootEntityType
                    };
                    current = null;
                    break;

                case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                    // Null-forgiving operator (!)
                    yield return new PathSegmentInfo(postfix, SegmentKind.NullForgiving);
                    current = postfix.Operand;
                    break;

                case ParenthesizedExpressionSyntax paren:
                    // Unwrap parentheses
                    current = paren.Expression;
                    break;

                case CastExpressionSyntax cast:
                    // Type cast - continue with inner expression
                    current = cast.Expression;
                    break;

                default:
                    // Unknown expression type - stop here
                    yield return new PathSegmentInfo(current, SegmentKind.Unknown);
                    current = null;
                    break;
            }
        }
    }

    private PathSegmentInfo GetPropertyInfo(MemberAccessExpressionSyntax memberAccess)
    {
        var symbolInfo = _context.SemanticModel.GetSymbolInfo(memberAccess, _context.CancellationToken);

        if (symbolInfo.Symbol is IPropertySymbol property)
        {
            var isCollection = IsCollectionType(property.Type);
            var isNullable = IsNullableType(property.Type);
            var targetType = isCollection ? GetCollectionElementType(property.Type) : property.Type;

            return new PathSegmentInfo(memberAccess, SegmentKind.PropertyAccess)
            {
                PropertyName = property.Name,
                SourceType = property.ContainingType,
                TargetType = targetType,
                Property = property,
                IsCollection = isCollection,
                IsNullable = isNullable
            };
        }

        // Could not resolve - still report it for potential FI0001
        return new PathSegmentInfo(memberAccess, SegmentKind.PropertyAccess)
        {
            PropertyName = memberAccess.Name.Identifier.Text,
            SourceType = null,
            TargetType = null
        };
    }

    private static (PathSegmentInfo segment, ExpressionSyntax? inner) AnalyzeInvocation(InvocationExpressionSyntax invocation)
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

        var kind = methodName switch
        {
            "Each" => SegmentKind.EachCall,
            "To" => SegmentKind.ToCall,
            "Where" => SegmentKind.WhereCall,
            "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" => SegmentKind.OrderingCall,
            _ => SegmentKind.Unknown
        };

        LambdaExpressionSyntax? predicate = null;
        if ((kind == SegmentKind.WhereCall || kind == SegmentKind.OrderingCall) &&
            invocation.ArgumentList.Arguments.Count > 0)
        {
            predicate = invocation.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        }

        return (new PathSegmentInfo(invocation, kind)
        {
            MethodName = methodName,
            Predicate = predicate
        }, inner);
    }

    private void ValidateSegments(List<PathSegmentInfo> segments)
    {
        ITypeSymbol? currentType = _rootEntityType;
        bool currentIsCollection = false;
        PathSegmentInfo? lastCollectionSegment = null;
        PathSegmentInfo? lastPropertySegment = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var nextSegment = i + 1 < segments.Count ? segments[i + 1] : null;

            switch (segment.Kind)
            {
                case SegmentKind.Parameter:
                    currentType = _rootEntityType;
                    currentIsCollection = false;
                    lastCollectionSegment = null;
                    lastPropertySegment = null;
                    break;

                case SegmentKind.PropertyAccess:
                    // FI0002: Check if we're accessing a property through a collection without Each()
                    if (currentIsCollection && lastCollectionSegment != null)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.MissingEachOnCollection,
                            lastCollectionSegment.Node.GetLocation(),
                            lastCollectionSegment.PropertyName ?? "collection"));
                    }

                    ValidatePropertyAccess(segment, currentType, nextSegment);
                    currentType = segment.TargetType;
                    currentIsCollection = segment.IsCollection;
                    lastCollectionSegment = segment.IsCollection ? segment : null;
                    lastPropertySegment = segment;
                    break;

                case SegmentKind.EachCall:
                    // FI0003: Each() should only be called on collections
                    ValidateEachCall(segment, currentIsCollection, lastCollectionSegment);
                    // After Each(), we're working with the element type, not a collection
                    currentIsCollection = false;
                    lastCollectionSegment = null;
                    break;

                case SegmentKind.ToCall:
                    // FI0006: To() on non-nullable is unnecessary
                    ValidateToCall(segment, lastPropertySegment);
                    break;

                case SegmentKind.NullForgiving:
                    // ! operator doesn't change collection state
                    break;

                case SegmentKind.WhereCall:
                    ValidateFilterCall(segment, currentIsCollection, "Where");
                    if (segment.Predicate != null && currentType != null)
                    {
                        ValidateFilterPredicate(segment.Predicate, currentType);
                    }
                    // Where() keeps us in collection context
                    break;

                case SegmentKind.OrderingCall:
                    ValidateFilterCall(segment, currentIsCollection, segment.MethodName ?? "OrderBy");
                    // Ordering keeps us in collection context
                    break;
            }
        }
    }

    private void ValidatePropertyAccess(
        PathSegmentInfo segment,
        ITypeSymbol? currentType,
        PathSegmentInfo? nextSegment)
    {
        // FI0001: Check property exists
        if (segment.Property == null && currentType != null && segment.PropertyName != null)
        {
            // Property could not be resolved - report error
            _context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PropertyNotFound,
                segment.Node.GetLocation(),
                segment.PropertyName,
                currentType.Name));
            return;
        }

        if (segment.Property == null)
        {
            return; // Can't validate further without property info
        }

        // FI0007: Check for nullable property without To() or !
        if (segment.IsNullable && !segment.IsCollection && nextSegment != null)
        {
            // There's navigation after this nullable property
            bool hasNullHandling = nextSegment.Kind == SegmentKind.ToCall ||
                                   nextSegment.Kind == SegmentKind.NullForgiving;

            if (!hasNullHandling)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MissingToOnNullable,
                    segment.Node.GetLocation(),
                    segment.PropertyName));
            }
        }
    }

    private void ValidateEachCall(PathSegmentInfo segment, bool currentIsCollection, PathSegmentInfo? lastCollectionSegment)
    {
        // FI0003: Each() should only be called on collections
        if (!currentIsCollection)
        {
            // Get property name for better error message
            var propertyName = lastCollectionSegment?.PropertyName ?? "property";
            _context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EachOnNonCollection,
                segment.Node.GetLocation(),
                propertyName));
        }
    }

    private void ValidateToCall(PathSegmentInfo segment, PathSegmentInfo? lastPropertySegment)
    {
        // FI0006: To() is unnecessary on non-nullable properties
        if (lastPropertySegment != null && !lastPropertySegment.IsNullable && !lastPropertySegment.IsCollection)
        {
            _context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnnecessaryTo,
                segment.Node.GetLocation(),
                lastPropertySegment.PropertyName ?? "property"));
        }
    }

    private void ValidateFilterCall(PathSegmentInfo segment, bool currentIsCollection, string methodName)
    {
        // FI0004/FI0005: Filter/ordering methods should only be on collections
        if (!currentIsCollection)
        {
            var descriptor = methodName == "Where"
                ? DiagnosticDescriptors.WhereOnNonCollection
                : DiagnosticDescriptors.OrderByOnNonCollection;

            _context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                segment.Node.GetLocation(),
                methodName));
        }
    }

    private void ValidateFilterPredicate(LambdaExpressionSyntax predicate, ITypeSymbol elementType)
    {
        // FI0008: Validate properties in the filter predicate exist on the element type
        var body = predicate switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax paren => paren.Body as ExpressionSyntax,
            _ => null
        };

        if (body == null)
        {
            return;
        }

        // Find all member access expressions in the predicate
        foreach (var memberAccess in body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
        {
            var symbolInfo = _context.SemanticModel.GetSymbolInfo(memberAccess, _context.CancellationToken);

            // If the symbol couldn't be resolved and it's accessing the element type, report error
            if (symbolInfo.Symbol == null && symbolInfo.CandidateSymbols.IsEmpty)
            {
                // Check if this is accessing the lambda parameter
                if (memberAccess.Expression is IdentifierNameSyntax)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidFilterProperty,
                        memberAccess.Name.GetLocation(),
                        memberAccess.Name.Identifier.Text,
                        elementType.Name));
                }
            }
        }
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        // Check for string (which implements IEnumerable<char> but isn't a collection for our purposes)
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        // Check if it implements IEnumerable<T>
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    }

    private static ITypeSymbol? GetCollectionElementType(ITypeSymbol collectionType)
    {
        if (collectionType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        // Find IEnumerable<T> and get T
        var enumerableInterface = collectionType.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        if (enumerableInterface != null && enumerableInterface.TypeArguments.Length > 0)
        {
            return enumerableInterface.TypeArguments[0];
        }

        // Check if the type itself is IEnumerable<T>
        if (collectionType is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
            namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    private static bool IsNullableType(ITypeSymbol type)
    {
        // Check for nullable reference type annotation
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        // Check for Nullable<T> value type
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }
}
