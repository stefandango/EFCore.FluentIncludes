using System.Collections.Immutable;
using System.Composition;
using EFCore.FluentIncludes.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EFCore.FluentIncludes.Analyzers;

/// <summary>
/// Provides code fixes for nullable navigation diagnostics (FI0006, FI0007).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableNavigationCodeFixProvider))]
[Shared]
public sealed class NullableNavigationCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.UnnecessaryTo.Id,      // FI0006
        DiagnosticDescriptors.MissingToOnNullable.Id // FI0007
    );

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            if (diagnostic.Id == DiagnosticDescriptors.UnnecessaryTo.Id)
            {
                // FI0006: Remove unnecessary To()
                RegisterRemoveToFix(context, diagnostic, node);
            }
            else if (diagnostic.Id == DiagnosticDescriptors.MissingToOnNullable.Id)
            {
                // FI0007: Insert To()
                RegisterInsertToFix(context, diagnostic, node);
            }
        }
    }

    private static void RegisterRemoveToFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode node)
    {
        // Find the To() invocation to remove
        var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Verify this is a To() call
        if (memberAccess.Name.Identifier.Text != "To")
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove unnecessary To()",
                createChangedDocument: ct => RemoveToCallAsync(context.Document, invocation, memberAccess, ct),
                equivalenceKey: "RemoveUnnecessaryTo"),
            diagnostic);
    }

    private static void RegisterInsertToFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode node)
    {
        // Find the member access expression where we need to insert To()
        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
        if (memberAccess == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Insert .To()",
                createChangedDocument: ct => InsertToCallAsync(context.Document, memberAccess, ct),
                equivalenceKey: "InsertTo"),
            diagnostic);
    }

    private static async Task<Document> RemoveToCallAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Replace the To() invocation with just the expression it was called on
        // e.g., "o.Notes.To()" -> "o.Notes"
        var newRoot = root.ReplaceNode(invocation, memberAccess.Expression.WithTriviaFrom(invocation));

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> InsertToCallAsync(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Create the .To() invocation
        // e.g., "o.Customer" -> "o.Customer.To()"
        var toAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            memberAccess,
            SyntaxFactory.IdentifierName("To"));

        var toInvocation = SyntaxFactory.InvocationExpression(toAccess)
            .WithTriviaFrom(memberAccess);

        var newRoot = root.ReplaceNode(memberAccess, toInvocation);

        return document.WithSyntaxRoot(newRoot);
    }
}
