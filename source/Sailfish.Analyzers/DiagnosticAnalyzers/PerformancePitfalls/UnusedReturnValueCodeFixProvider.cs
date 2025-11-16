using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedReturnValueCodeFixProvider))]
[Shared]
public sealed class UnusedReturnValueCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(UnusedReturnValueAnalyzer.Descriptor.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        var exprStmt = invocation?.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (invocation is null || exprStmt is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Consume return value (Anti-DCE)",
                c => ConsumeInvocationAsync(context.Document, exprStmt, invocation, c),
                nameof(UnusedReturnValueCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ConsumeInvocationAsync(Document document, ExpressionStatementSyntax exprStmt, InvocationExpressionSyntax invocation, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false) as CompilationUnitSyntax;
        if (root is null) return document;

        // Build Consumer.Consume(invocation)
        var consumerId = SyntaxFactory.IdentifierName("Consumer");
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            consumerId,
            SyntaxFactory.IdentifierName("Consume"));
        var newInvocation = SyntaxFactory.InvocationExpression(
            memberAccess,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(invocation))));
        var newExprStmt = exprStmt.WithExpression(newInvocation);

        var newRoot = root.ReplaceNode(exprStmt, newExprStmt) as CompilationUnitSyntax ?? root;

        // Ensure using Sailfish.Utilities exists
        if (!newRoot.Usings.Any(u => u.Name?.ToString() == "Sailfish.Utilities"))
        {
            var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Sailfish.Utilities"));
            // Preserve trivia from the last using directive
            if (newRoot.Usings.Count > 0)
            {
                var lastUsing = newRoot.Usings[newRoot.Usings.Count - 1];
                newUsing = newUsing.WithTrailingTrivia(lastUsing.GetTrailingTrivia());
            }
            newRoot = newRoot.AddUsings(newUsing);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}

