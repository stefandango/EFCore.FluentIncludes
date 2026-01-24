using System.Collections.Immutable;
using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EFCore.FluentIncludes.Analyzers;

/// <summary>
/// Analyzes FluentIncludes path expressions for compile-time validation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IncludePathAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Method names from QueryableExtensions that accept path expressions.
    /// </summary>
    private static readonly ImmutableHashSet<string> TargetMethodNames = ImmutableHashSet.Create(
        "IncludePaths",
        "IncludePath",
        "IncludePathsIf",
        "IncludeFrom",
        "IncludeFromIf",
        "Include" // From IncludeSpec
    );

    /// <summary>
    /// The fully qualified type name for our extension methods.
    /// </summary>
    private const string QueryableExtensionsTypeName = "EFCore.FluentIncludes.QueryableExtensions";
    private const string IncludeSpecTypeName = "EFCore.FluentIncludes.IncludeSpec";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.PropertyNotFound,
        DiagnosticDescriptors.MissingEachOnCollection,
        DiagnosticDescriptors.EachOnNonCollection,
        DiagnosticDescriptors.WhereOnNonCollection,
        DiagnosticDescriptors.OrderByOnNonCollection,
        DiagnosticDescriptors.UnnecessaryTo,
        DiagnosticDescriptors.MissingToOnNullable,
        DiagnosticDescriptors.InvalidFilterProperty,
        DiagnosticDescriptors.TypeMismatch
    );

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Get the method name
        var methodName = GetMethodName(invocation);
        if (methodName == null || !TargetMethodNames.Contains(methodName))
        {
            return;
        }

        // Check if this is our method (from QueryableExtensions or IncludeSpec)
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!IsFluentIncludesMethod(methodSymbol))
        {
            return;
        }

        // Extract and analyze lambda expressions from arguments
        AnalyzeLambdaArguments(context, invocation, methodSymbol);
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static bool IsFluentIncludesMethod(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return false;
        }

        var fullTypeName = containingType.ToDisplayString();

        // Check if it's from QueryableExtensions
        if (fullTypeName == QueryableExtensionsTypeName)
        {
            return true;
        }

        // Check if it's from IncludeSpec<T> (or derived class)
        if (fullTypeName.StartsWith(IncludeSpecTypeName, StringComparison.Ordinal))
        {
            return true;
        }

        // Check base types for IncludeSpec<T>
        var baseType = containingType.BaseType;
        while (baseType != null)
        {
            var baseTypeName = baseType.OriginalDefinition.ToDisplayString();
            if (baseTypeName.StartsWith(IncludeSpecTypeName, StringComparison.Ordinal))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private static void AnalyzeLambdaArguments(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            // Handle params array: can be multiple lambdas or an array
            var expression = argument.Expression;

            // Skip non-lambda arguments (like boolean conditions)
            if (expression is not LambdaExpressionSyntax lambda)
            {
                // Could be an array initializer with lambdas
                if (expression is ImplicitArrayCreationExpressionSyntax arrayCreation)
                {
                    foreach (var element in arrayCreation.Initializer.Expressions)
                    {
                        if (element is LambdaExpressionSyntax arrayLambda)
                        {
                            AnalyzeLambdaExpression(context, arrayLambda);
                        }
                    }
                }
                continue;
            }

            AnalyzeLambdaExpression(context, lambda);
        }
    }

    private static void AnalyzeLambdaExpression(
        SyntaxNodeAnalysisContext context,
        LambdaExpressionSyntax lambda)
    {
        // Get the lambda body (the expression to analyze)
        var body = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax paren => paren.Body as ExpressionSyntax,
            _ => null
        };

        if (body == null)
        {
            return;
        }

        // Get the parameter type (the entity type)
        ITypeSymbol? entityType = null;
        var parameter = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax paren => paren.ParameterList.Parameters.FirstOrDefault(),
            _ => null
        };

        if (parameter != null)
        {
            var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken);
            entityType = parameterSymbol?.Type;
        }

        if (entityType == null)
        {
            return;
        }

        // Walk the expression and validate
        var walker = new IncludeExpressionWalker(context, entityType);
        walker.Analyze(body);
    }
}
