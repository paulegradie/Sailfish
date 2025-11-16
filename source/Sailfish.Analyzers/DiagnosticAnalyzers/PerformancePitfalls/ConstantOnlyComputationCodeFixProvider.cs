using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstantOnlyComputationCodeFixProvider))]
[Shared]
public sealed class ConstantOnlyComputationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ConstantOnlyComputationAnalyzer.Descriptor.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var constantExpr = node.FirstAncestorOrSelf<ExpressionSyntax>();
        var statement = constantExpr?.FirstAncestorOrSelf<StatementSyntax>();
        if (constantExpr is null || statement is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Consume constant result (reduce DCE risk)",
                c => InsertConsumeAfterAsync(context.Document, statement, constantExpr, c),
                nameof(ConstantOnlyComputationCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> InsertConsumeAfterAsync(Document document, StatementSyntax statement, ExpressionSyntax expr, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false) as CompilationUnitSyntax;
        if (root is null) return document;

        // Build: Consumer.Consume((<expr>));
        var consumeInvocation = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Consumer"),
                    SyntaxFactory.IdentifierName("Consume")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(Parenthesize(expr))))));

        CompilationUnitSyntax newRoot;
        if (statement.Parent is BlockSyntax block)
        {
            var statements = block.Statements;
            var index = statements.IndexOf(statement);
            var updatedStatements = statements.Insert(index + 1, consumeInvocation);
            var newBlock = block.WithStatements(updatedStatements);
            newRoot = (CompilationUnitSyntax)root.ReplaceNode(block, newBlock);
        }
        else
        {
            // Wrap embedded statement (e.g., in if/for/while without braces) in a block so we can add the consume call safely
            var newBlock = SyntaxFactory.Block(statement, consumeInvocation).WithTriviaFrom(statement);
            newRoot = (CompilationUnitSyntax)root.ReplaceNode(statement, newBlock);
        }

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

    private static ExpressionSyntax Parenthesize(ExpressionSyntax expr)
    {
        return expr is ParenthesizedExpressionSyntax ? expr : SyntaxFactory.ParenthesizedExpression(expr);
    }
}

