using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EFCore.FluentIncludes.Analyzers.Generator;

/// <summary>
/// Source generator that intercepts FluentIncludes method calls and generates
/// direct EF Core Include/ThenInclude chains at compile time.
/// </summary>
/// <remarks>
/// This generator only emits interceptors for .NET 10+ where interceptors are stable.
/// For .NET 8/9, the runtime implementation continues to work with expression parsing.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class FluentIncludesGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Method names that can be intercepted.
    /// </summary>
    private static readonly ImmutableHashSet<string> TargetMethodNames = ImmutableHashSet.Create(
        "IncludePaths",
        "IncludePath",
        "IncludePathsIf",
        "IncludeFrom",
        "IncludeFromIf"
    );

    private const string QueryableExtensionsTypeName = "EFCore.FluentIncludes.QueryableExtensions";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Check if we should generate interceptors.
        // We generate for .NET 9+ since interceptors are available (though preview in .NET 9).
        // The interceptor attribute we define ourselves, so it won't cause errors on older targets.
        var shouldGenerate = context.CompilationProvider.Select((compilation, _) =>
        {
            // Check System.Runtime version to determine .NET version
            // .NET 9 = System.Runtime 9.x, .NET 10 = System.Runtime 10.x
            var runtimeRef = compilation.ReferencedAssemblyNames
                .FirstOrDefault(a => a.Name == "System.Runtime");

            if (runtimeRef is not null && runtimeRef.Version.Major >= 9)
            {
                return true;
            }

            // Also check netstandard/mscorlib for edge cases
            var mscorlibRef = compilation.ReferencedAssemblyNames
                .FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "System.Private.CoreLib");

            if (mscorlibRef is not null && mscorlibRef.Version.Major >= 9)
            {
                return true;
            }

            return false;
        });

        // Find all invocations of our target methods
        var invocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateInvocation(node),
                transform: static (ctx, ct) => GetIncludeCallInfo(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        // Combine with the .NET 10 check
        var combined = invocations.Collect().Combine(shouldGenerate);

        // Generate interceptors
        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var (calls, shouldGen) = source;

            if (!shouldGen || calls.IsDefaultOrEmpty)
            {
                return;
            }

            var generatedCode = InterceptorEmitter.GenerateInterceptors(calls, spc.CancellationToken);

            if (!string.IsNullOrEmpty(generatedCode))
            {
                spc.AddSource("FluentIncludesInterceptors.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
            }
        });
    }

    /// <summary>
    /// Quick syntactic check to see if this is potentially a target method invocation.
    /// </summary>
    private static bool IsCandidateInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

        return methodName is not null && TargetMethodNames.Contains(methodName);
    }

    /// <summary>
    /// Extracts detailed information about an IncludePaths/etc. call for code generation.
    /// </summary>
    private static IncludeCallInfo? GetIncludeCallInfo(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Get method symbol
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, ct);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        // Verify this is our method
        var containingType = methodSymbol.ContainingType?.ToDisplayString();
        if (containingType != QueryableExtensionsTypeName)
        {
            return null;
        }

        // Get the entity type from the method's type arguments
        if (methodSymbol.TypeArguments.Length == 0)
        {
            return null;
        }

        var entityType = methodSymbol.TypeArguments[0];

        // Extract lambda expressions from arguments
        var lambdaInfos = new List<IncludeLambdaInfo>();
        var canGenerate = true;
        string? fallbackReason = null;

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is LambdaExpressionSyntax lambda)
            {
                var lambdaInfo = AnalyzeLambda(context.SemanticModel, lambda, entityType, ct);
                if (lambdaInfo is null)
                {
                    canGenerate = false;
                    fallbackReason = "Unable to analyze lambda expression";
                    break;
                }

                if (!lambdaInfo.CanGenerate)
                {
                    canGenerate = false;
                    fallbackReason = lambdaInfo.FallbackReason;
                    break;
                }

                lambdaInfos.Add(lambdaInfo);
            }
            else if (argument.Expression is not LiteralExpressionSyntax)
            {
                // Non-inline expression (e.g., variable) - can't generate
                canGenerate = false;
                fallbackReason = "Expression passed as variable rather than inline lambda";
                break;
            }
        }

        // Get location for interceptor
        var location = invocation.GetLocation();
        var lineSpan = location.GetLineSpan();

        return new IncludeCallInfo(
            methodName: methodSymbol.Name,
            entityType: entityType,
            entityTypeFullName: entityType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            lambdas: lambdaInfos.ToImmutableArray(),
            canGenerate: canGenerate,
            fallbackReason: fallbackReason,
            filePath: lineSpan.Path,
            line: lineSpan.StartLinePosition.Line + 1,
            column: lineSpan.StartLinePosition.Character + 1,
            syntaxTree: invocation.SyntaxTree,
            invocationSpan: invocation.Span);
    }

    /// <summary>
    /// Analyzes a lambda expression to extract include path information.
    /// </summary>
    private static IncludeLambdaInfo? AnalyzeLambda(
        SemanticModel semanticModel,
        LambdaExpressionSyntax lambda,
        ITypeSymbol entityType,
        CancellationToken ct)
    {
        var body = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax paren => paren.Body as ExpressionSyntax,
            _ => null
        };

        if (body is null)
        {
            return null;
        }

        var walker = new GeneratorExpressionWalker(semanticModel, entityType, ct);
        var segments = walker.Walk(body);

        if (segments is null)
        {
            return new IncludeLambdaInfo(
                canGenerate: false,
                fallbackReason: walker.FallbackReason ?? "Unable to analyze expression",
                segments: ImmutableArray<IncludeSegmentInfo>.Empty,
                originalLambdaSyntax: lambda.ToString());
        }

        return new IncludeLambdaInfo(
            canGenerate: true,
            fallbackReason: null,
            segments: segments.ToImmutableArray(),
            originalLambdaSyntax: lambda.ToString());
    }
}

/// <summary>
/// Information about an IncludePaths/etc. invocation.
/// </summary>
internal sealed class IncludeCallInfo
{
    public IncludeCallInfo(
        string methodName,
        ITypeSymbol entityType,
        string entityTypeFullName,
        ImmutableArray<IncludeLambdaInfo> lambdas,
        bool canGenerate,
        string? fallbackReason,
        string filePath,
        int line,
        int column,
        SyntaxTree syntaxTree,
        TextSpan invocationSpan)
    {
        MethodName = methodName;
        EntityType = entityType;
        EntityTypeFullName = entityTypeFullName;
        Lambdas = lambdas;
        CanGenerate = canGenerate;
        FallbackReason = fallbackReason;
        FilePath = filePath;
        Line = line;
        Column = column;
        SyntaxTree = syntaxTree;
        InvocationSpan = invocationSpan;
    }

    public string MethodName { get; }
    public ITypeSymbol EntityType { get; }
    public string EntityTypeFullName { get; }
    public ImmutableArray<IncludeLambdaInfo> Lambdas { get; }
    public bool CanGenerate { get; }
    public string? FallbackReason { get; }
    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }
    public SyntaxTree SyntaxTree { get; }
    public TextSpan InvocationSpan { get; }
}

/// <summary>
/// Information about a single lambda expression in an IncludePaths call.
/// </summary>
internal sealed class IncludeLambdaInfo
{
    public IncludeLambdaInfo(
        bool canGenerate,
        string? fallbackReason,
        ImmutableArray<IncludeSegmentInfo> segments,
        string originalLambdaSyntax)
    {
        CanGenerate = canGenerate;
        FallbackReason = fallbackReason;
        Segments = segments;
        OriginalLambdaSyntax = originalLambdaSyntax;
    }

    public bool CanGenerate { get; }
    public string? FallbackReason { get; }
    public ImmutableArray<IncludeSegmentInfo> Segments { get; }
    public string OriginalLambdaSyntax { get; }
}

/// <summary>
/// Information about a single segment in an include path.
/// </summary>
internal sealed class IncludeSegmentInfo
{
    public IncludeSegmentInfo(
        string propertyName,
        string sourceTypeName,
        string targetTypeName,
        bool isCollection,
        string? filterLambdaSyntax,
        ImmutableArray<OrderingSegmentInfo> orderings)
    {
        PropertyName = propertyName;
        SourceTypeName = sourceTypeName;
        TargetTypeName = targetTypeName;
        IsCollection = isCollection;
        FilterLambdaSyntax = filterLambdaSyntax;
        Orderings = orderings;
    }

    public string PropertyName { get; }
    public string SourceTypeName { get; }
    public string TargetTypeName { get; }
    public bool IsCollection { get; }

    /// <summary>
    /// The original filter lambda syntax (e.g., "li => li.IsActive").
    /// Null if no filter.
    /// </summary>
    public string? FilterLambdaSyntax { get; }

    /// <summary>
    /// The original ordering lambda syntax(es).
    /// </summary>
    public ImmutableArray<OrderingSegmentInfo> Orderings { get; }
}

/// <summary>
/// Information about an ordering expression in an include path.
/// </summary>
internal sealed class OrderingSegmentInfo
{
    public OrderingSegmentInfo(string keySelectorSyntax, bool isDescending)
    {
        KeySelectorSyntax = keySelectorSyntax;
        IsDescending = isDescending;
    }

    public string KeySelectorSyntax { get; }
    public bool IsDescending { get; }
}
